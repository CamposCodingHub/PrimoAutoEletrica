using System;
using System.Collections.Generic;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3.2 — builds ExternalEvidencePackage from AssistantQueryContext (Context Engine + Knowledge retrieval).
    /// Traces SourceType/SourceId. Never invents evidence.
    /// </summary>
    public sealed class ExternalEvidencePackageBuilder
    {
        private readonly ExternalFinanceRedactor _redactor;

        public ExternalEvidencePackageBuilder(ExternalFinanceRedactor? redactor = null)
        {
            _redactor = redactor ?? new ExternalFinanceRedactor();
        }

        public ExternalEvidencePackage Build(AssistantQueryContext context, bool includeFinancial = false)
        {
            ArgumentNullException.ThrowIfNull(context);

            var sources = new List<ExternalEvidenceSource>();
            var redacted = new List<string>();

            if (context.RetrievedEvidence != null)
            {
                foreach (var ev in context.RetrievedEvidence)
                {
                    if (ev == null) continue;
                    if (_redactor.ShouldRedact(ev.Classification, includeFinancial))
                    {
                        redacted.Add($"evidence:{ev.EvidenceId}:class={ev.Classification}");
                        continue;
                    }

                    sources.Add(new ExternalEvidenceSource
                    {
                        EvidenceId = string.IsNullOrWhiteSpace(ev.EvidenceId) ? Guid.NewGuid().ToString("N") : ev.EvidenceId,
                        SourceType = ev.Kind.ToString(),
                        SourceId = string.IsNullOrWhiteSpace(ev.SourceCode) ? ev.EvidenceId : ev.SourceCode,
                        Title = Sanitize(ev.Title),
                        Excerpt = Sanitize(_redactor.RedactText(ev.Excerpt, includeFinancial)),
                        Classification = string.IsNullOrWhiteSpace(ev.Classification) ? "TECHNICAL" : ev.Classification,
                        RelevanceLabel = ev.RelevanceLabel
                    });
                }
            }

            // Legacy slots → evidence when RetrievedEvidence empty
            if (sources.Count == 0 && context.RetrievedKnowledge != null)
            {
                foreach (var k in context.RetrievedKnowledge)
                {
                    if (k == null) continue;
                    var classif = "TECHNICAL";
                    if (_redactor.ShouldRedact(classif, includeFinancial)) continue;
                    var id = k.KnowledgeId.ToString("N");
                    sources.Add(new ExternalEvidenceSource
                    {
                        EvidenceId = id,
                        SourceType = nameof(EvidenceKind.Knowledge),
                        SourceId = string.IsNullOrWhiteSpace(k.Code) ? id : k.Code,
                        Title = Sanitize(k.Title),
                        Excerpt = Sanitize(_redactor.RedactText(string.Join(" | ", new[] { k.Symptom, k.DiagnosticProcedure, k.Solution }.Where(x => !string.IsNullOrWhiteSpace(x))), includeFinancial)),
                        Classification = classif,
                        RelevanceLabel = "legacy-knowledge"
                    });
                }
            }

            if (sources.Count == 0 && context.RetrievedCases != null)
            {
                foreach (var c in context.RetrievedCases)
                {
                    if (c == null) continue;
                    var id = c.CaseId.ToString("N");
                    sources.Add(new ExternalEvidenceSource
                    {
                        EvidenceId = id,
                        SourceType = nameof(EvidenceKind.DiagnosticCase),
                        SourceId = string.IsNullOrWhiteSpace(c.Code) ? id : c.Code,
                        Title = Sanitize(c.Title),
                        Excerpt = Sanitize(_redactor.RedactText(c.Symptom ?? c.Solution ?? string.Empty, includeFinancial)),
                        Classification = "TECHNICAL",
                        RelevanceLabel = "legacy-case"
                    });
                }
            }

            // Strip any finance-like text from query before external send
            var safeQuery = _redactor.RedactText(context.Query ?? string.Empty, includeFinancial);
            if (!string.Equals(safeQuery, context.Query ?? string.Empty, StringComparison.Ordinal))
            {
                redacted.Add("query:finance-patterns");
            }

            return new ExternalEvidencePackage
            {
                RequestId = string.IsNullOrWhiteSpace(context.CorrelationId) ? Guid.NewGuid().ToString("N") : context.CorrelationId.Trim(),
                Query = safeQuery,
                Sources = sources,
                RedactedFields = redacted
            };
        }

        private static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var v = value.Trim();
            return v.Length > 2000 ? v.Substring(0, 2000) + "…" : v;
        }
    }
}