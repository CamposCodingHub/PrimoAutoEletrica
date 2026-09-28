using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// C5.1 — Durable intelligence audit on SQLite (same Microsoft.Data.Sqlite + AppData pattern as AuditLogService).
    /// Append-oriented INSERT only. No keys/tokens/full prompts/responses/finance dumps.
    /// IsDurable=true. CRITICAL: survives dispose/reopen of the connection/service.
    /// </summary>
    public sealed class PersistentIntelligenceAuditService : IIntelligenceAuditService
    {
        public const string TableName = "IntelligenceAuditLogs";
        public const string DefaultFileName = "intelligence-audit.db";

        private readonly string _databasePath;
        private readonly object _gate = new();

        public PersistentIntelligenceAuditService(string? databasePath = null)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PrimoAutoEletrica");
                Directory.CreateDirectory(root);
                _databasePath = Path.Combine(root, DefaultFileName);
            }
            else
            {
                _databasePath = databasePath;
                var dir = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);
            }

            EnsureSchema();
        }

        public string DatabasePath => _databasePath;
        public bool IsDurable => true;

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    using var connection = Open();
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $"SELECT COUNT(*) FROM {TableName};";
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void Record(IntelligenceAuditEntry entry)
        {
            if (entry == null) return;
            var safe = IntelligenceAuditService.Sanitize(entry);

            lock (_gate)
            {
                using var connection = Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
INSERT INTO {TableName}
(
    AuditId, Timestamp, UserId, SessionId, Action, Provider, ProviderMode,
    ContextType, ContextId, CorrelationId, EvidenceCount, EvidenceIds,
    GroundingStatus, ResultStatus, FailureReason, DurationMs, FallbackUsed, SecurityDecision,
    Question, UserName, ContextSummary, AllowedContext, EvidenceSummary, AnswerSummary,
    Model, Result, Status, Failure, RejectReason, MissingEvidence, Error
)
VALUES
(
    @AuditId, @Timestamp, @UserId, @SessionId, @Action, @Provider, @ProviderMode,
    @ContextType, @ContextId, @CorrelationId, @EvidenceCount, @EvidenceIds,
    @GroundingStatus, @ResultStatus, @FailureReason, @DurationMs, @FallbackUsed, @SecurityDecision,
    @Question, @UserName, @ContextSummary, @AllowedContext, @EvidenceSummary, @AnswerSummary,
    @Model, @Result, @Status, @Failure, @RejectReason, @MissingEvidence, @Error
);";

                cmd.Parameters.AddWithValue("@AuditId", safe.AuditId.ToString("N"));
                cmd.Parameters.AddWithValue("@Timestamp", safe.Timestamp.ToString("o"));
                cmd.Parameters.AddWithValue("@UserId", (object?)safe.UserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SessionId", Db(safe.SessionId));
                cmd.Parameters.AddWithValue("@Action", Db(safe.Action));
                cmd.Parameters.AddWithValue("@Provider", Db(safe.Provider));
                cmd.Parameters.AddWithValue("@ProviderMode", Db(safe.ProviderMode));
                cmd.Parameters.AddWithValue("@ContextType", Db(safe.ContextType));
                cmd.Parameters.AddWithValue("@ContextId", Db(safe.ContextId));
                cmd.Parameters.AddWithValue("@CorrelationId", Db(safe.CorrelationId));
                cmd.Parameters.AddWithValue("@EvidenceCount", safe.EvidenceCount);
                cmd.Parameters.AddWithValue("@EvidenceIds", Db(safe.EvidenceIds));
                cmd.Parameters.AddWithValue("@GroundingStatus", Db(safe.GroundingStatus));
                cmd.Parameters.AddWithValue("@ResultStatus", Db(safe.ResultStatus));
                cmd.Parameters.AddWithValue("@FailureReason", Db(safe.FailureReason));
                cmd.Parameters.AddWithValue("@DurationMs", safe.DurationMs);
                cmd.Parameters.AddWithValue("@FallbackUsed", safe.FallbackUsed ? 1 : 0);
                cmd.Parameters.AddWithValue("@SecurityDecision", Db(safe.SecurityDecision));
                cmd.Parameters.AddWithValue("@Question", Db(safe.Question));
                cmd.Parameters.AddWithValue("@UserName", Db(safe.UserName));
                cmd.Parameters.AddWithValue("@ContextSummary", Db(safe.ContextSummary));
                cmd.Parameters.AddWithValue("@AllowedContext", Db(safe.AllowedContext));
                cmd.Parameters.AddWithValue("@EvidenceSummary", Db(safe.EvidenceSummary));
                cmd.Parameters.AddWithValue("@AnswerSummary", Db(safe.AnswerSummary));
                cmd.Parameters.AddWithValue("@Model", Db(safe.Model));
                cmd.Parameters.AddWithValue("@Result", Db(safe.Result));
                cmd.Parameters.AddWithValue("@Status", Db(safe.Status));
                cmd.Parameters.AddWithValue("@Failure", Db(safe.Failure));
                cmd.Parameters.AddWithValue("@RejectReason", Db(safe.RejectReason));
                cmd.Parameters.AddWithValue("@MissingEvidence", Db(safe.MissingEvidence));
                cmd.Parameters.AddWithValue("@Error", Db(safe.Error));
                cmd.ExecuteNonQuery();
            }
        }

        public IReadOnlyList<IntelligenceAuditEntry> ListRecent(int take = 100)
            => Filter(take: take);

        public IReadOnlyList<IntelligenceAuditEntry> Filter(
            string? provider = null,
            string? status = null,
            int? userId = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            int take = 100)
            => Query(provider, status, userId, null, from, to, take);

        public IReadOnlyList<IntelligenceAuditEntry> FilterForReader(
            int? readerUserId,
            string? readerProfile,
            string? provider = null,
            string? status = null,
            int? userId = null,
            string? correlationId = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            int take = 100)
        {
            if (!IntelligenceAuditService.CanReadAudit(readerProfile))
                return Array.Empty<IntelligenceAuditEntry>();

            var scopedUserId = IntelligenceAuditService.IsAdmin(readerProfile) ? userId : readerUserId;
            if (!IntelligenceAuditService.IsAdmin(readerProfile) && !readerUserId.HasValue)
                return Array.Empty<IntelligenceAuditEntry>();

            return Query(provider, status, scopedUserId, correlationId, from, to, take);
        }

        public void ClearForTests()
        {
            lock (_gate)
            {
                using var connection = Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"DELETE FROM {TableName};";
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Shared schema DDL — also invoked from DatabaseService.InitializeAuditSchema.</summary>
        public static void EnsureSchema(DbConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"
CREATE TABLE IF NOT EXISTS {TableName} (
    AuditId TEXT PRIMARY KEY,
    Timestamp TEXT NOT NULL,
    UserId INTEGER,
    SessionId TEXT,
    Action TEXT,
    Provider TEXT,
    ProviderMode TEXT,
    ContextType TEXT,
    ContextId TEXT,
    CorrelationId TEXT,
    EvidenceCount INTEGER NOT NULL DEFAULT 0,
    EvidenceIds TEXT,
    GroundingStatus TEXT,
    ResultStatus TEXT,
    FailureReason TEXT,
    DurationMs INTEGER NOT NULL DEFAULT 0,
    FallbackUsed INTEGER NOT NULL DEFAULT 0,
    SecurityDecision TEXT,
    Question TEXT,
    UserName TEXT,
    ContextSummary TEXT,
    AllowedContext TEXT,
    EvidenceSummary TEXT,
    AnswerSummary TEXT,
    Model TEXT,
    Result TEXT,
    Status TEXT,
    Failure TEXT,
    RejectReason TEXT,
    MissingEvidence TEXT,
    Error TEXT
);
CREATE INDEX IF NOT EXISTS IX_IntelligenceAudit_Timestamp ON {TableName} (Timestamp DESC);
CREATE INDEX IF NOT EXISTS IX_IntelligenceAudit_CorrelationId ON {TableName} (CorrelationId);
CREATE INDEX IF NOT EXISTS IX_IntelligenceAudit_UserId ON {TableName} (UserId, Timestamp DESC);
CREATE INDEX IF NOT EXISTS IX_IntelligenceAudit_ProviderMode ON {TableName} (ProviderMode, ResultStatus);
";
            cmd.ExecuteNonQuery();
        }

        private void EnsureSchema()
        {
            lock (_gate)
            {
                using var connection = Open();
                EnsureSchema(connection);
            }
        }

        private SqliteConnection Open()
        {
            var cs = new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
            var connection = new SqliteConnection(cs);
            connection.Open();
            return connection;
        }

        private IReadOnlyList<IntelligenceAuditEntry> Query(
            string? provider,
            string? status,
            int? userId,
            string? correlationId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int take)
        {
            lock (_gate)
            {
                using var connection = Open();
                using var cmd = connection.CreateCommand();
                var where = new List<string>();
                if (!string.IsNullOrWhiteSpace(provider))
                {
                    where.Add("(Provider = @Provider OR Provider LIKE @ProviderLike)");
                    cmd.Parameters.AddWithValue("@Provider", provider.Trim());
                    cmd.Parameters.AddWithValue("@ProviderLike", "%" + provider.Trim() + "%");
                }
                if (!string.IsNullOrWhiteSpace(status))
                {
                    where.Add("(Status = @Status OR ResultStatus = @Status)");
                    cmd.Parameters.AddWithValue("@Status", status.Trim());
                }
                if (userId.HasValue)
                {
                    where.Add("UserId = @UserId");
                    cmd.Parameters.AddWithValue("@UserId", userId.Value);
                }
                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    where.Add("CorrelationId = @CorrelationId");
                    cmd.Parameters.AddWithValue("@CorrelationId", correlationId.Trim());
                }
                if (from.HasValue)
                {
                    where.Add("Timestamp >= @From");
                    cmd.Parameters.AddWithValue("@From", from.Value.ToString("o"));
                }
                if (to.HasValue)
                {
                    where.Add("Timestamp <= @To");
                    cmd.Parameters.AddWithValue("@To", to.Value.ToString("o"));
                }

                var sql = $"SELECT * FROM {TableName}";
                if (where.Count > 0) sql += " WHERE " + string.Join(" AND ", where);
                sql += " ORDER BY Timestamp DESC LIMIT @Take;";
                cmd.Parameters.AddWithValue("@Take", Math.Max(1, take));
                cmd.CommandText = sql;

                var list = new List<IntelligenceAuditEntry>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(Map(reader));
                return list;
            }
        }

        private static IntelligenceAuditEntry Map(DbDataReader reader)
        {
            Guid.TryParse(reader["AuditId"]?.ToString(), out var auditId);
            DateTimeOffset.TryParse(reader["Timestamp"]?.ToString(), out var ts);
            int? userId = reader["UserId"] is DBNull ? null : Convert.ToInt32(reader["UserId"]);
            return new IntelligenceAuditEntry
            {
                EntryId = auditId == Guid.Empty ? Guid.NewGuid() : auditId,
                Timestamp = ts == default ? DateTimeOffset.Now : ts,
                UserId = userId,
                SessionId = reader["SessionId"] as string ?? string.Empty,
                Action = reader["Action"] as string ?? string.Empty,
                Provider = reader["Provider"] as string ?? string.Empty,
                ProviderMode = reader["ProviderMode"] as string ?? string.Empty,
                ContextType = reader["ContextType"] as string ?? string.Empty,
                ContextId = reader["ContextId"] as string ?? string.Empty,
                CorrelationId = reader["CorrelationId"] as string ?? string.Empty,
                EvidenceCount = reader["EvidenceCount"] is DBNull ? 0 : Convert.ToInt32(reader["EvidenceCount"]),
                EvidenceIds = reader["EvidenceIds"] as string ?? string.Empty,
                GroundingStatus = reader["GroundingStatus"] as string ?? string.Empty,
                ResultStatus = reader["ResultStatus"] as string ?? string.Empty,
                FailureReason = reader["FailureReason"] as string,
                DurationMs = reader["DurationMs"] is DBNull ? 0 : Convert.ToInt64(reader["DurationMs"]),
                FallbackUsed = reader["FallbackUsed"] is not DBNull && Convert.ToInt32(reader["FallbackUsed"]) != 0,
                SecurityDecision = reader["SecurityDecision"] as string ?? string.Empty,
                Question = reader["Question"] as string ?? string.Empty,
                UserName = reader["UserName"] as string ?? string.Empty,
                ContextSummary = reader["ContextSummary"] as string ?? string.Empty,
                AllowedContext = reader["AllowedContext"] as string ?? string.Empty,
                EvidenceSummary = reader["EvidenceSummary"] as string ?? string.Empty,
                AnswerSummary = reader["AnswerSummary"] as string ?? string.Empty,
                Model = reader["Model"] as string ?? string.Empty,
                Result = reader["Result"] as string ?? string.Empty,
                Status = reader["Status"] as string ?? string.Empty,
                Failure = reader["Failure"] as string,
                RejectReason = reader["RejectReason"] as string,
                MissingEvidence = reader["MissingEvidence"] as string,
                Error = reader["Error"] as string
            };
        }

        private static object Db(string? value)
            => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }
}
