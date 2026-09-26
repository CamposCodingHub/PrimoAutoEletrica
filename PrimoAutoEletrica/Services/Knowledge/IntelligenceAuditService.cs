using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IIntelligenceAuditService
    {
        void Record(IntelligenceAuditEntry entry);
        IReadOnlyList<IntelligenceAuditEntry> ListRecent(int take = 100);
    }

    public sealed class IntelligenceAuditEntry
    {
        public Guid EntryId { get; init; } = Guid.NewGuid();
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
        public string Question { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public int? UserId { get; init; }
        public string ContextSummary { get; init; } = string.Empty;
        public string EvidenceSummary { get; init; } = string.Empty;
        public string AnswerSummary { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string Result { get; init; } = string.Empty;
        public string? Failure { get; init; }
        public string? MissingEvidence { get; init; }
    }

    /// <summary>
    /// C2.9 — audit log for intelligence consults. No secrets/credentials/tokens.
    /// In-memory ring buffer (no protected DB write).
    /// </summary>
    public sealed class IntelligenceAuditService : IIntelligenceAuditService
    {
        private readonly List<IntelligenceAuditEntry> _entries = new();
        private readonly object _gate = new();
        private const int MaxEntries = 500;

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
                EvidenceSummary = Redact(entry.EvidenceSummary),
                AnswerSummary = Redact(entry.AnswerSummary),
                Provider = entry.Provider ?? string.Empty,
                Result = entry.Result ?? string.Empty,
                Failure = Redact(entry.Failure),
                MissingEvidence = Redact(entry.MissingEvidence)
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

        private static string Redact(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var v = value;
            // Never persist obvious secrets
            string[] banned = { "api_key", "apikey", "authorization", "bearer ", "password", "senha", "token=", "secret" };
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