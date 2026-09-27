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
        int Count { get; }
        void ClearForTests();
    }

    public sealed class IntelligenceAuditEntry
    {
        public Guid EntryId { get; init; } = Guid.NewGuid();
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
        public string Question { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public int? UserId { get; init; }
        public string ContextSummary { get; init; } = string.Empty;
        /// <summary>C3.14 — allowed context ids (VehicleId/WorkOrderId/Knowledge/Evidence) — no PII/finance.</summary>
        public string AllowedContext { get; init; } = string.Empty;
        public string EvidenceSummary { get; init; } = string.Empty;
        /// <summary>C3.14 — concrete evidence IDs cited or packaged (no secrets).</summary>
        public string EvidenceIds { get; init; } = string.Empty;
        public string AnswerSummary { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
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
    /// C2.9 / C3.14 — audit log for intelligence consults. No secrets/credentials/tokens.
    /// In-memory ring buffer (no protected DB write). Retention = MaxEntries FIFO.
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

        public void Record(IntelligenceAuditEntry entry)
        {
            if (entry == null) return;
            var safe = new IntelligenceAuditEntry
            {
                EntryId = entry.EntryId,
                Timestamp = entry.Timestamp == default ? DateTimeOffset.Now : entry.Timestamp,
                Question = Redact(entry.Question),
                UserName = Redact(entry.UserName),
                UserId = entry.UserId,
                ContextSummary = Redact(entry.ContextSummary),
                AllowedContext = Redact(entry.AllowedContext),
                EvidenceSummary = Redact(entry.EvidenceSummary),
                EvidenceIds = Redact(entry.EvidenceIds),
                AnswerSummary = Redact(entry.AnswerSummary),
                Provider = entry.Provider ?? string.Empty,
                Model = string.IsNullOrWhiteSpace(entry.Model) ? string.Empty : entry.Model.Trim(),
                Result = entry.Result ?? string.Empty,
                Status = string.IsNullOrWhiteSpace(entry.Status) ? InferStatus(entry) : entry.Status.Trim(),
                Failure = Redact(entry.Failure),
                RejectReason = Redact(entry.RejectReason),
                MissingEvidence = Redact(entry.MissingEvidence),
                Error = Redact(entry.Error)
            };
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
                IEnumerable<IntelligenceAuditEntry> q = _entries;
                if (!string.IsNullOrWhiteSpace(provider))
                    q = q.Where(e => string.Equals(e.Provider, provider, StringComparison.OrdinalIgnoreCase)
                                     || e.Provider.Contains(provider, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(status))
                    q = q.Where(e => string.Equals(e.Status, status, StringComparison.OrdinalIgnoreCase));
                if (userId.HasValue)
                    q = q.Where(e => e.UserId == userId);
                if (from.HasValue)
                    q = q.Where(e => e.Timestamp >= from.Value);
                if (to.HasValue)
                    q = q.Where(e => e.Timestamp <= to.Value);
                return q.Reverse().Take(Math.Max(1, take)).ToList();
            }
        }

        public void ClearForTests()
        {
            lock (_gate) _entries.Clear();
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
