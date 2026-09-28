using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IIntelligenceAuditService
    {
        void Record(IntelligenceAuditEntry entry);
        IReadOnlyList<IntelligenceAuditEntry> ListRecent(int take = 100);
        IReadOnlyList<IntelligenceAuditEntry> Filter(
            string? provider = null,
            string? status = null,
            int? userId = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            int take = 100);

        /// <summary>C5.1 — RBAC-gated read. Non-admin readers only see their own UserId rows.</summary>
        IReadOnlyList<IntelligenceAuditEntry> FilterForReader(
            int? readerUserId,
            string? readerProfile,
            string? provider = null,
            string? status = null,
            int? userId = null,
            string? correlationId = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            int take = 100);

        int Count { get; }

        /// <summary>True when entries survive process/connection dispose (SQLite). In-memory = false.</summary>
        bool IsDurable { get; }

        void ClearForTests();
    }

    public sealed class IntelligenceAuditEntry
    {
        public Guid EntryId { get; init; } = Guid.NewGuid();
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

        // --- C5.1 justified durable fields ---
        public Guid AuditId => EntryId;
        public int? UserId { get; init; }
        public string? SessionId { get; init; }
        public string Action { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string ProviderMode { get; init; } = string.Empty;
        public string ContextType { get; init; } = string.Empty;
        public string ContextId { get; init; } = string.Empty;
        public string CorrelationId { get; init; } = string.Empty;
        public int EvidenceCount { get; init; }
        public string EvidenceIds { get; init; } = string.Empty;
        public string GroundingStatus { get; init; } = string.Empty;
        public string ResultStatus { get; init; } = string.Empty;
        public string? FailureReason { get; init; }
        public long DurationMs { get; init; }
        public bool FallbackUsed { get; init; }
        public string SecurityDecision { get; init; } = string.Empty;

        // --- Legacy / summary fields (minimized; never keys/tokens/full prompts) ---
        public string Question { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string ContextSummary { get; init; } = string.Empty;
        public string AllowedContext { get; init; } = string.Empty;
        public string EvidenceSummary { get; init; } = string.Empty;
        public string AnswerSummary { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public string Result { get; init; } = string.Empty;
        /// <summary>OK / REJECTED / FAIL_CLOSED / FALLBACK / ERROR</summary>
        public string Status { get; init; } = string.Empty;
        public string? Failure { get; init; }
        public string? RejectReason { get; init; }
        public string? MissingEvidence { get; init; }
        public string? Error { get; init; }
    }

    /// <summary>
    /// C2.9 / C3.14 - in-memory ring audit. IsDurable=false.
    /// C5.1 durable path: PersistentIntelligenceAuditService (SQLite).
    /// No secrets/credentials/tokens/full prompts/responses/finance.
    /// </summary>
    public sealed class IntelligenceAuditService : IIntelligenceAuditService
    {
        private readonly List<IntelligenceAuditEntry> _entries = new();
        private readonly object _gate = new();
        private const int MaxEntries = 500;

        public int Count
        {
            get { lock (_gate) return _entries.Count; }
        }

        public bool IsDurable => false;

        public void Record(IntelligenceAuditEntry entry)
        {
            if (entry == null) return;
            var safe = Sanitize(entry);
            lock (_gate)
            {
                _entries.Add(safe);
                while (_entries.Count > MaxEntries) _entries.RemoveAt(0);
            }
        }

        public IReadOnlyList<IntelligenceAuditEntry> ListRecent(int take = 100)
        {
            lock (_gate) return _entries.AsEnumerable().Reverse().Take(Math.Max(1, take)).ToList();
        }

        public IReadOnlyList<IntelligenceAuditEntry> Filter(
            string? provider = null,
            string? status = null,
            int? userId = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            int take = 100)
        {
            lock (_gate)
            {
                return ApplyFilter(_entries, provider, status, userId, null, from, to, take);
            }
        }

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
            if (!CanReadAudit(readerProfile))
                return Array.Empty<IntelligenceAuditEntry>();

            var scopedUserId = IsAdmin(readerProfile) ? userId : readerUserId;
            if (!IsAdmin(readerProfile) && !readerUserId.HasValue)
                return Array.Empty<IntelligenceAuditEntry>();

            lock (_gate)
            {
                return ApplyFilter(_entries, provider, status, scopedUserId, correlationId, from, to, take);
            }
        }

        public void ClearForTests()
        {
            lock (_gate) _entries.Clear();
        }

        internal static bool IsAdmin(string? profile)
            => string.Equals(profile?.Trim(), "Administrador", StringComparison.OrdinalIgnoreCase);

        /// <summary>Administrador always; other profiles need explicit non-empty profile (own rows only).</summary>
        internal static bool CanReadAudit(string? profile)
            => IsAdmin(profile) || !string.IsNullOrWhiteSpace(profile);

        internal static IntelligenceAuditEntry Sanitize(IntelligenceAuditEntry entry)
        {
            var status = string.IsNullOrWhiteSpace(entry.Status) ? InferStatus(entry) : entry.Status.Trim();
            var resultStatus = string.IsNullOrWhiteSpace(entry.ResultStatus) ? status : entry.ResultStatus.Trim();
            var failureReason = entry.FailureReason ?? entry.Failure ?? entry.Error ?? entry.RejectReason;

            return new IntelligenceAuditEntry
            {
                EntryId = entry.EntryId == Guid.Empty ? Guid.NewGuid() : entry.EntryId,
                Timestamp = entry.Timestamp == default ? DateTimeOffset.Now : entry.Timestamp,
                UserId = entry.UserId,
                SessionId = Redact(entry.SessionId),
                Action = string.IsNullOrWhiteSpace(entry.Action) ? InferAction(entry) : entry.Action.Trim(),
                Provider = entry.Provider ?? string.Empty,
                ProviderMode = string.IsNullOrWhiteSpace(entry.ProviderMode) ? InferProviderMode(entry) : entry.ProviderMode.Trim(),
                ContextType = entry.ContextType ?? string.Empty,
                ContextId = Redact(entry.ContextId),
                CorrelationId = string.IsNullOrWhiteSpace(entry.CorrelationId) ? Guid.NewGuid().ToString("N") : entry.CorrelationId.Trim(),
                EvidenceCount = entry.EvidenceCount > 0
                    ? entry.EvidenceCount
                    : CountEvidenceIds(entry.EvidenceIds),
                EvidenceIds = Redact(entry.EvidenceIds),
                GroundingStatus = entry.GroundingStatus ?? string.Empty,
                ResultStatus = resultStatus,
                FailureReason = Redact(failureReason),
                DurationMs = entry.DurationMs < 0 ? 0 : entry.DurationMs,
                FallbackUsed = entry.FallbackUsed || string.Equals(entry.Result, "LOCAL_FALLBACK", StringComparison.OrdinalIgnoreCase),
                SecurityDecision = entry.SecurityDecision ?? string.Empty,
                Question = Redact(entry.Question),
                UserName = Redact(entry.UserName),
                ContextSummary = Redact(entry.ContextSummary),
                AllowedContext = Redact(entry.AllowedContext),
                EvidenceSummary = Redact(entry.EvidenceSummary),
                AnswerSummary = Redact(entry.AnswerSummary),
                Model = string.IsNullOrWhiteSpace(entry.Model) ? string.Empty : entry.Model.Trim(),
                Result = entry.Result ?? string.Empty,
                Status = status,
                Failure = Redact(entry.Failure),
                RejectReason = Redact(entry.RejectReason),
                MissingEvidence = Redact(entry.MissingEvidence),
                Error = Redact(entry.Error)
            };
        }

        private static IReadOnlyList<IntelligenceAuditEntry> ApplyFilter(
            IEnumerable<IntelligenceAuditEntry> source,
            string? provider,
            string? status,
            int? userId,
            string? correlationId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int take)
        {
            IEnumerable<IntelligenceAuditEntry> q = source;
            if (!string.IsNullOrWhiteSpace(provider))
                q = q.Where(e => string.Equals(e.Provider, provider, StringComparison.OrdinalIgnoreCase)
                                 || e.Provider.Contains(provider, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(e => string.Equals(e.Status, status, StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(e.ResultStatus, status, StringComparison.OrdinalIgnoreCase));
            if (userId.HasValue)
                q = q.Where(e => e.UserId == userId);
            if (!string.IsNullOrWhiteSpace(correlationId))
                q = q.Where(e => string.Equals(e.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase));
            if (from.HasValue)
                q = q.Where(e => e.Timestamp >= from.Value);
            if (to.HasValue)
                q = q.Where(e => e.Timestamp <= to.Value);
            return q.Reverse().Take(Math.Max(1, take)).ToList();
        }

        private static string InferStatus(IntelligenceAuditEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.RejectReason)) return "REJECTED";
            if (!string.IsNullOrWhiteSpace(entry.Failure) || !string.IsNullOrWhiteSpace(entry.Error)) return "ERROR";
            if (string.Equals(entry.Result, "LOCAL_FALLBACK", StringComparison.OrdinalIgnoreCase)) return "FALLBACK";
            if (string.Equals(entry.Result, "EXTERNAL", StringComparison.OrdinalIgnoreCase)) return "OK";
            if (string.Equals(entry.Result, "LOCAL", StringComparison.OrdinalIgnoreCase)) return "OK";
            return string.IsNullOrWhiteSpace(entry.Result) ? "UNKNOWN" : entry.Result;
        }

        private static string InferAction(IntelligenceAuditEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.RejectReason)) return "Reject";
            if (string.Equals(entry.Result, "LOCAL_FALLBACK", StringComparison.OrdinalIgnoreCase)) return "Fallback";
            if (string.Equals(entry.Result, "EXTERNAL", StringComparison.OrdinalIgnoreCase)) return "ExternalConsulta";
            return "Consulta";
        }

        private static string InferProviderMode(IntelligenceAuditEntry entry)
        {
            if (string.Equals(entry.Result, "LOCAL_FALLBACK", StringComparison.OrdinalIgnoreCase)) return "FALLBACK";
            if (string.Equals(entry.Result, "EXTERNAL", StringComparison.OrdinalIgnoreCase)) return "EXTERNAL";
            if (string.Equals(entry.Status, "ERROR", StringComparison.OrdinalIgnoreCase)) return "ERROR";
            if (string.Equals(entry.Result, "LOCAL", StringComparison.OrdinalIgnoreCase)) return "LOCAL";
            return string.IsNullOrWhiteSpace(entry.ProviderMode) ? "LOCAL" : entry.ProviderMode;
        }

        private static int CountEvidenceIds(string? evidenceIds)
        {
            if (string.IsNullOrWhiteSpace(evidenceIds)) return 0;
            return evidenceIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        }

        private static string Redact(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var v = value;
            string[] banned = { "api_key", "apikey", "authorization", "bearer ", "password", "senha", "token=", "secret", "sk-" };
            foreach (var b in banned)
            {
                if (v.Contains(b, StringComparison.OrdinalIgnoreCase))
                {
                    return "[REDACTED]";
                }
            }
            return v.Length > 2000 ? v.Substring(0, 2000) + "…" : v;
        }
    }
}
