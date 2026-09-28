using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services.Intelligence
{
    public sealed class QualityLatencyCostCell
    {
        public string Architecture { get; init; } = string.Empty;
        public string QualityStatus { get; init; } = "NOT_TESTED";
        public double? QualityScore { get; init; }
        public string LatencyStatus { get; init; } = "NOT_TESTED";
        public double? LatencyMsP50 { get; init; }
        public string CostStatus { get; init; } = "PRICE_NOT_VERIFIED";
        public decimal? CostPerQuery { get; init; }
        public string Notes { get; init; } = string.Empty;
    }

    /// <summary>C6.10 — honest Q/L/C matrix. Never fabricates scores.</summary>
    public sealed class QualityLatencyCostMatrixBuilder
    {
        public IReadOnlyList<QualityLatencyCostCell> BuildBaselineHonestyMatrix(
            IReadOnlyList<ScenarioRunResult>? measuredScenarioResults = null)
        {
            var cells = new List<QualityLatencyCostCell>
            {
                new()
                {
                    Architecture = "A_Rules",
                    QualityStatus = measuredScenarioResults == null ? "NOT_TESTED" : "OBSERVATION_ONLY",
                    LatencyStatus = measuredScenarioResults == null ? "NOT_TESTED" : "OBSERVATION_ONLY",
                    CostStatus = "PRICE_NOT_VERIFIED",
                    Notes = "Local rules — infra cost deferred; quality via evaluator when run"
                },
                new()
                {
                    Architecture = "B_Knowledge",
                    QualityStatus = "NOT_TESTED",
                    LatencyStatus = "NOT_TESTED",
                    CostStatus = "PRICE_NOT_VERIFIED",
                    Notes = "Knowledge retrieval path"
                },
                new()
                {
                    Architecture = "C_KnowledgePlusContext",
                    QualityStatus = "NOT_TESTED",
                    LatencyStatus = "NOT_TESTED",
                    CostStatus = "PRICE_NOT_VERIFIED"
                },
                new()
                {
                    Architecture = "D_ModelNoPrimoxCtx",
                    QualityStatus = "NOT_TESTED",
                    LatencyStatus = "NOT_TESTED",
                    CostStatus = "PRICE_NOT_VERIFIED",
                    Notes = "Requires LocalModel or External — LIVE_NOT_TESTED / ENVIRONMENT_DEPENDENCY"
                },
                new()
                {
                    Architecture = "E_ModelPlusCtx",
                    QualityStatus = "NOT_TESTED",
                    LatencyStatus = "NOT_TESTED",
                    CostStatus = "PRICE_NOT_VERIFIED"
                },
                new()
                {
                    Architecture = "F_ModelPlusEvidence",
                    QualityStatus = "NOT_TESTED",
                    LatencyStatus = "NOT_TESTED",
                    CostStatus = "PRICE_NOT_VERIFIED"
                },
                new()
                {
                    Architecture = "G_ModelPlusCtxEvidenceRules",
                    QualityStatus = "NOT_TESTED",
                    LatencyStatus = "NOT_TESTED",
                    CostStatus = "PRICE_NOT_VERIFIED",
                    Notes = "Candidate hybrid — unmeasured until models available"
                }
            };

            if (measuredScenarioResults == null || measuredScenarioResults.Count == 0)
                return cells;

            foreach (var group in measuredScenarioResults.GroupBy(r => r.Scenario))
            {
                var arch = group.Key.ToString();
                var cell = cells.Find(c => c.Architecture == arch.Replace("KnowledgeVsModelScenario.", "") ||
                                           arch.EndsWith(c.Architecture.Split('_')[0], StringComparison.OrdinalIgnoreCase) ||
                                           c.Architecture.StartsWith(group.Key.ToString().Split('_')[0], StringComparison.Ordinal));
                // Map by enum name suffix style A_Rules etc.
                cell = cells.FirstOrDefault(c => c.Architecture == Map(group.Key));
                if (cell == null) continue;
                var idx = cells.IndexOf(cell);
                var latencies = group.Select(g => g.Metrics?.LatencyMs).Where(x => x.HasValue).Select(x => x!.Value).ToList();
                var evals = group.Select(g => g.Evaluation?.DeterministicScore).Where(x => x.HasValue).Select(x => x!.Value).ToList();
                var status = group.Select(g => g.Status).FirstOrDefault() ?? "NOT_TESTED";
                cells[idx] = new QualityLatencyCostCell
                {
                    Architecture = cell.Architecture,
                    QualityStatus = evals.Count == 0 ? (status.Contains("STUB") || status.Contains("MOCK") ? status : "HUMAN_REVIEW_REQUIRED") : "OBSERVATION_ONLY",
                    QualityScore = evals.Count == 0 ? null : evals.Average(),
                    LatencyStatus = latencies.Count == 0 ? "NOT_TESTED" : "OBSERVATION_ONLY",
                    LatencyMsP50 = latencies.Count == 0 ? null : Percentile(latencies, 0.5),
                    CostStatus = "PRICE_NOT_VERIFIED",
                    CostPerQuery = null,
                    Notes = cell.Notes + $" | n={group.Count()}; status={status}"
                };
            }

            return cells;
        }

        private static string Map(KnowledgeVsModelScenario s) => s switch
        {
            KnowledgeVsModelScenario.A_Rules => "A_Rules",
            KnowledgeVsModelScenario.B_Knowledge => "B_Knowledge",
            KnowledgeVsModelScenario.C_KnowledgePlusContext => "C_KnowledgePlusContext",
            KnowledgeVsModelScenario.D_ModelNoPrimoxCtx => "D_ModelNoPrimoxCtx",
            KnowledgeVsModelScenario.E_ModelPlusCtx => "E_ModelPlusCtx",
            KnowledgeVsModelScenario.F_ModelPlusEvidence => "F_ModelPlusEvidence",
            KnowledgeVsModelScenario.G_ModelPlusCtxEvidenceRules => "G_ModelPlusCtxEvidenceRules",
            _ => s.ToString()
        };

        private static double Percentile(List<double> values, double p)
        {
            var sorted = values.OrderBy(x => x).ToList();
            if (sorted.Count == 1) return sorted[0];
            var idx = (int)Math.Clamp(Math.Round((sorted.Count - 1) * p), 0, sorted.Count - 1);
            return sorted[idx];
        }
    }
}