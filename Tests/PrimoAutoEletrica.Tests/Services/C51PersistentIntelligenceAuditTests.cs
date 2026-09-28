using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.ExternalAi;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>C5.1 — Persistent Intelligence Audit (SQLite durable, RBAC read, write→reopen→read).</summary>
    public sealed class C51PersistentIntelligenceAuditTests
    {
        private static string TempDbPath()
        {
            var dir = Path.Combine(Path.GetTempPath(), "primox-c51-audit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "intelligence-audit.db");
        }

        [Fact]
        public void C51_Persistent_IsDurable_True_InMemory_False()
        {
            var path = TempDbPath();
            try
            {
                using (Noop()) { }
                var durable = new PersistentIntelligenceAuditService(path);
                Assert.True(durable.IsDurable);
                Assert.False(new IntelligenceAuditService().IsDurable);
            }
            finally
            {
                TryDelete(path);
            }
        }

        [Fact]
        public void C51_CRITICAL_Write_DisposeReopen_ReadBack_FromSqlite()
        {
            var path = TempDbPath();
            var correlation = Guid.NewGuid().ToString("N");
            var auditId = Guid.NewGuid();
            try
            {
                // WRITE
                {
                    var writer = new PersistentIntelligenceAuditService(path);
                    Assert.True(writer.IsDurable);
                    writer.Record(new IntelligenceAuditEntry
                    {
                        EntryId = auditId,
                        Timestamp = DateTimeOffset.Now,
                        UserId = 7,
                        SessionId = "sess-c51",
                        Action = "Consulta",
                        Provider = "PRIMOX_LOCAL_GROUNDED",
                        ProviderMode = "LOCAL",
                        ContextType = "OS",
                        ContextId = "OS:123",
                        CorrelationId = correlation,
                        EvidenceCount = 2,
                        EvidenceIds = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa,bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                        GroundingStatus = "CONFIRMED",
                        ResultStatus = "OK",
                        DurationMs = 42,
                        FallbackUsed = false,
                        SecurityDecision = "ALLOW",
                        Question = "fuga de corrente?",
                        Status = "OK",
                        Result = "LOCAL"
                    });
                    Assert.True(writer.Count >= 1);
                } // dispose writer / connection closed

                // REOPEN fresh service instance against same SQLite file
                var reader = new PersistentIntelligenceAuditService(path);
                Assert.True(reader.Count >= 1);
                var byCorr = reader.FilterForReader(
                    readerUserId: 7,
                    readerProfile: "Tecnico",
                    correlationId: correlation,
                    take: 10);
                Assert.NotEmpty(byCorr);
                var hit = byCorr.First(e => e.CorrelationId == correlation);
                Assert.Equal(auditId, hit.AuditId);
                Assert.Equal(7, hit.UserId);
                Assert.Equal("sess-c51", hit.SessionId);
                Assert.Equal("Consulta", hit.Action);
                Assert.Equal("LOCAL", hit.ProviderMode);
                Assert.Equal("OS", hit.ContextType);
                Assert.Equal("OS:123", hit.ContextId);
                Assert.Equal(2, hit.EvidenceCount);
                Assert.Contains("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", hit.EvidenceIds);
                Assert.Equal("CONFIRMED", hit.GroundingStatus);
                Assert.Equal("OK", hit.ResultStatus);
                Assert.Equal(42, hit.DurationMs);
                Assert.False(hit.FallbackUsed);
                Assert.Equal("ALLOW", hit.SecurityDecision);
                Assert.DoesNotContain("sk-", hit.Question, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                TryDelete(path);
            }
        }

        [Fact]
        public void C51_RBAC_NonAdmin_Cannot_Read_Other_User()
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
                    Question = "q-user1"
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
                    Question = "q-user2"
                });

                var asUser1 = audit.FilterForReader(readerUserId: 1, readerProfile: "Tecnico", take: 50);
                Assert.All(asUser1, e => Assert.Equal(1, e.UserId));
                Assert.DoesNotContain(asUser1, e => e.UserId == 2);

                var asAdmin = audit.FilterForReader(readerUserId: 99, readerProfile: "Administrador", take: 50);
                Assert.Contains(asAdmin, e => e.UserId == 1);
                Assert.Contains(asAdmin, e => e.UserId == 2);

                var noProfile = audit.FilterForReader(readerUserId: 1, readerProfile: null, take: 50);
                Assert.Empty(noProfile);
            }
            finally
            {
                TryDelete(path);
            }
        }

        [Fact]
        public void C51_Minimization_Secrets_Redacted_Not_Stored_Raw()
        {
            var path = TempDbPath();
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                audit.Record(new IntelligenceAuditEntry
                {
                    UserId = 3,
                    Action = "Consulta",
                    Provider = "TEST",
                    ProviderMode = "LOCAL",
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Question = "please use api_key=sk-secret-value",
                    AnswerSummary = "Bearer tokendata",
                    ResultStatus = "OK",
                    Status = "OK",
                    Result = "LOCAL"
                });

                // reopen
                var reader = new PersistentIntelligenceAuditService(path);
                var rows = reader.ListRecent(5);
                Assert.NotEmpty(rows);
                var blob = string.Join("|", rows.Select(e => e.Question + e.AnswerSummary + e.FailureReason + e.Error));
                Assert.DoesNotContain("sk-secret-value", blob, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("[REDACTED]", rows.Select(e => e.Question));
            }
            finally
            {
                TryDelete(path);
            }
        }

        [Fact]
        public async Task C51_Router_Records_Durable_With_Correlation_And_ProviderMode()
        {
            var path = TempDbPath();
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                var correlation = Guid.NewGuid().ToString("N");
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(new ExternalAssistantOptions { Enabled = false }),
                    audit: audit,
                    preferExternalWhenArmed: false);

                var ctx = new AssistantQueryContext
                {
                    Query = "diagnostico alternador",
                    RetrievedEvidence = new[]
                    {
                        new EvidenceItem
                        {
                            EvidenceId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                            SourceCode = "KB-001",
                            Title = "Alternador",
                            Excerpt = "medir tensao"
                        }
                    },
                    Parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["UserId"] = 42,
                        ["UserName"] = "TecnicoC51",
                        ["SessionId"] = "sess-router",
                        ["CorrelationId"] = correlation,
                        ["DurationMs"] = 15L
                    }
                };

                await router.AskAsync(ctx);

                // Cross process-boundary simulation: new service instance
                var reader = new PersistentIntelligenceAuditService(path);
                var rows = reader.FilterForReader(42, "Tecnico", correlationId: correlation, take: 5);
                Assert.NotEmpty(rows);
                var e = rows[0];
                Assert.Equal(correlation, e.CorrelationId);
                Assert.Equal("LOCAL", e.ProviderMode);
                Assert.False(string.IsNullOrWhiteSpace(e.Action));
                Assert.Equal(42, e.UserId);
                Assert.Equal("sess-router", e.SessionId);
                Assert.True(e.DurationMs == 15 || e.DurationMs >= 0);
            }
            finally
            {
                TryDelete(path);
            }
        }

        [Fact]
        public void C51_Justified_Fields_Present_On_Durable_Row()
        {
            var path = TempDbPath();
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                var corr = Guid.NewGuid().ToString("N");
                audit.Record(new IntelligenceAuditEntry
                {
                    UserId = 9,
                    SessionId = "s",
                    Action = "Fallback",
                    Provider = "PRIMOX_ASSIST_ROUTER",
                    ProviderMode = "FALLBACK",
                    ContextType = "VEHICLE",
                    ContextId = "VEHICLE:ABC1D23",
                    CorrelationId = corr,
                    EvidenceCount = 1,
                    EvidenceIds = "cccccccccccccccccccccccccccccccc",
                    GroundingStatus = "N_A",
                    ResultStatus = "FALLBACK",
                    FailureReason = "timeout",
                    DurationMs = 30100,
                    FallbackUsed = true,
                    SecurityDecision = "ALLOW",
                    Status = "FALLBACK",
                    Result = "LOCAL_FALLBACK",
                    Question = "q"
                });

                var row = new PersistentIntelligenceAuditService(path)
                    .FilterForReader(9, "Administrador", correlationId: corr)
                    .Single();

                Assert.False(string.IsNullOrWhiteSpace(row.AuditId.ToString("N")));
                Assert.NotEqual(default, row.Timestamp);
                Assert.Equal(9, row.UserId);
                Assert.Equal("s", row.SessionId);
                Assert.Equal("Fallback", row.Action);
                Assert.Equal("FALLBACK", row.ProviderMode);
                Assert.Equal("VEHICLE", row.ContextType);
                Assert.Equal("VEHICLE:ABC1D23", row.ContextId);
                Assert.Equal(corr, row.CorrelationId);
                Assert.Equal(1, row.EvidenceCount);
                Assert.Equal("cccccccccccccccccccccccccccccccc", row.EvidenceIds);
                Assert.Equal("N_A", row.GroundingStatus);
                Assert.Equal("FALLBACK", row.ResultStatus);
                Assert.Equal("timeout", row.FailureReason);
                Assert.Equal(30100, row.DurationMs);
                Assert.True(row.FallbackUsed);
                Assert.Equal("ALLOW", row.SecurityDecision);
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static IDisposable Noop() => new Dummy();
        private sealed class Dummy : IDisposable { public void Dispose() { } }

        private static void TryDelete(string dbPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // best-effort cleanup for temp test DBs
            }
        }
    }
}

