using System;
using System.Collections.Generic;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// Mapper aditivo: AssistantResponse legado → campos C2 (Evidence/Warnings/Provider/Timestamp)
    /// sem quebrar consumidores existentes.
    /// </summary>
    public static class AssistantContractMapper
    {
        public static AssistantResponse Enrich(
            AssistantResponse source,
            string? providerId = null,
            IReadOnlyList<EvidenceItem>? evidence = null,
            IReadOnlyList<string>? warnings = null,
            IReadOnlyList<string>? missingInformation = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            var mappedEvidence = evidence ?? source.Evidence;
            if ((mappedEvidence == null || mappedEvidence.Count == 0) && source.CitedSources.Count > 0)
            {
                mappedEvidence = source.CitedSources.Select(c => new EvidenceItem
                {
                    SourceCode = c.SourceCode,
                    Title = c.SourceTitle,
                    Excerpt = c.RelevanceExplanation,
                    RelevanceScore = null,
                    RelevanceLabel = "manual-link",
                    ConfidenceContribution = source.ConfidenceLevel,
                    Classification = "TECHNICAL",
                    Kind = GuessKind(c.SourceCode)
                }).ToList();
            }

            var missing = missingInformation ?? source.MissingInformation;
            if ((missing == null || missing.Count == 0) &&
                source.ConfidenceLevel == AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE)
            {
                missing = new[]
                {
                    "Medições elétricas relevantes ao sintoma",
                    "Identificação do veículo (modelo/tensão)"
                };
            }

            return new AssistantResponse
            {
                AnswerMarkdown = source.AnswerMarkdown,
                Hypotheses = source.Hypotheses,
                RecommendedActions = source.RecommendedActions,
                CitedSources = source.CitedSources,
                ConfidenceLevel = source.ConfidenceLevel,
                Disclaimers = source.Disclaimers,
                Evidence = mappedEvidence ?? Array.Empty<EvidenceItem>(),
                Warnings = warnings ?? source.Warnings ?? Array.Empty<string>(),
                MissingInformation = missing ?? Array.Empty<string>(),
                Provider = providerId ?? source.Provider,
                Timestamp = source.Timestamp ?? DateTimeOffset.Now
            };
        }

        public static EvidenceItem FromSearchHit(KnowledgeSearchHit hit) => KnowledgeItemAdapters.ToEvidence(hit);

        private static EvidenceKind GuessKind(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return EvidenceKind.Other;
            if (code.StartsWith("KB-", StringComparison.OrdinalIgnoreCase)) return EvidenceKind.Knowledge;
            if (code.StartsWith("CASO-", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("CASE-", StringComparison.OrdinalIgnoreCase)) return EvidenceKind.DiagnosticCase;
            if (code.Length == 3 && code.StartsWith("D", StringComparison.OrdinalIgnoreCase)) return EvidenceKind.Procedure;
            return EvidenceKind.Other;
        }
    }
}
