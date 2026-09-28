using System;
using System.IO;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>C5.2 — Audit integrity: append-oriented create/read; UPDATE/DELETE/tamper denied; cross-user RBAC.</summary>
    public sealed class C52AuditIntegrityTests
    {
        private static string TempDbPath()
        {
            var dir = Path.Combine(Path.GetTempPath(), "primox-c52-audit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "intelligence-audit.db");
        }

        private static void TryDelete(string dbPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch { }
        }

        [Fact]
        public void C52_Create_And_Read_Ok()
        {
            var path = TempDbPath();
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                var corr = Guid.NewGuid().ToString("N");
                audit.Record(new IntelligenceAuditEntry
                {
                    UserId = 10,
                    Action = "Consulta",
                    Provider = "LOCAL",
                    ProviderMode = "LOCAL",
                    CorrelationId = corr,
                    ResultStatus = "OK",
                    Status = "OK",
                    Result = "LOCAL",
                    Question = "integrity-create"
                });
                var rows = audit.FilterForReader(10, "Tecnico", correlationId: corr);
                Assert.Single(rows);
                Assert.Equal("integrity-create", rows[0].Question);
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public void C52_Attempt_Update_Denied_By_Trigger()
        {
            var path = TempDbPath();
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                var id = Guid.NewGuid();
                audit.Record(new IntelligenceAuditEntry
                {
                    EntryId = id,
                    UserId = 10,
                    Action = "Consulta",
                    Provider = "LOCAL",
                    ProviderMode = "LOCAL",
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    ResultStatus = "OK",
                    Status = "OK",
                    Result = "LOCAL",
                    Question = "original"
                });

                using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE IntelligenceAuditLogs SET Question = @q WHERE AuditId = @id;";
                cmd.Parameters.AddWithValue("@q", "TAMPERED");
                cmd.Parameters.AddWithValue("@id", id.ToString("N"));
                var ex = Assert.ThrowsAny<Exception>(() => cmd.ExecuteNonQuery());
                Assert.Contains("append-only", ex.Message, StringComparison.OrdinalIgnoreCase);

                var reader = new PersistentIntelligenceAuditService(path);
                var row = reader.ListRecent(5)[0];
                Assert.Equal("original", row.Question);
                Assert.NotEqual("TAMPERED", row.Question);
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public void C52_Attempt_Delete_Denied_By_Trigger()
        {
            var path = TempDbPath();
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                var id = Guid.NewGuid();
                audit.Record(new IntelligenceAuditEntry
                {
                    EntryId = id,
                    UserId = 11,
                    Action = "Consulta",
                    Provider = "LOCAL",
                    ProviderMode = "LOCAL",
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    ResultStatus = "OK",
                    Status = "OK",
                    Result = "LOCAL",
                    Question = "keep-me"
                });

                using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM IntelligenceAuditLogs WHERE AuditId = @id;";
                cmd.Parameters.AddWithValue("@id", id.ToString("N"));
                var ex = Assert.ThrowsAny<Exception>(() => cmd.ExecuteNonQuery());
                Assert.Contains("append-only", ex.Message, StringComparison.OrdinalIgnoreCase);

                Assert.Equal(1, new PersistentIntelligenceAuditService(path).Count);
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public void C52_Tamper_ResultStatus_Denied_Row_Unchanged()
        {
            var path = TempDbPath();
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                var id = Guid.NewGuid();
                audit.Record(new IntelligenceAuditEntry
                {
                    EntryId = id,
                    UserId = 12,
                    Action = "Consulta",
                    Provider = "LOCAL",
                    ProviderMode = "LOCAL",
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    ResultStatus = "OK",
                    Status = "OK",
                    Result = "LOCAL",
                    SecurityDecision = "ALLOW",
                    Question = "status-row"
                });

                using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE IntelligenceAuditLogs SET ResultStatus = 'OK_FAKE', SecurityDecision = 'ALLOW_FAKE' WHERE AuditId = @id;";
                cmd.Parameters.AddWithValue("@id", id.ToString("N"));
                Assert.ThrowsAny<Exception>(() => cmd.ExecuteNonQuery());

                var row = new PersistentIntelligenceAuditService(path).ListRecent(1)[0];
                Assert.Equal("OK", row.ResultStatus);
                Assert.Equal("ALLOW", row.SecurityDecision);
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public void C52_CrossUser_Read_Denied_For_NonAdmin()
        {
            var path = TempDbPath();
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                audit.Record(new IntelligenceAuditEntry
                {
                    UserId = 1,
                    Action = "Consulta",
                    Provider = "LOCAL",
                    ProviderMode = "LOCAL",
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    ResultStatus = "OK",
                    Status = "OK",
                    Result = "LOCAL",
                    Question = "u1"
                });
                audit.Record(new IntelligenceAuditEntry
                {
                    UserId = 2,
                    Action = "Consulta",
                    Provider = "LOCAL",
                    ProviderMode = "LOCAL",
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    ResultStatus = "OK",
                    Status = "OK",
                    Result = "LOCAL",
                    Question = "u2"
                });

                var as2 = audit.FilterForReader(2, "Mecanico", take: 20);
                Assert.All(as2, e => Assert.Equal(2, e.UserId));
                Assert.DoesNotContain(as2, e => e.Question == "u1");
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public void C52_Service_Has_No_Public_Update_Or_Delete_API()
        {
            var t = typeof(PersistentIntelligenceAuditService);
            Assert.Null(t.GetMethod("Update"));
            Assert.Null(t.GetMethod("Delete"));
            Assert.Null(t.GetMethod("UpdateEntry"));
            Assert.Null(t.GetMethod("DeleteEntry"));
            Assert.NotNull(t.GetMethod("Record"));
            Assert.NotNull(t.GetMethod("ClearForTests")); // test-only wipe
        }
    }
}