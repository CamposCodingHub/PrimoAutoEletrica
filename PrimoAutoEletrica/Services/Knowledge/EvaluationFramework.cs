using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// C4.10 Evaluation Framework — controlled dataset EVAL-001+ across mandatory categories.
    /// Labels use truth vocabulary only. Never invent LIVE/mock→live.
    /// </summary>
    public enum EvalCategory
    {
        Grounding = 0,
        Context = 1,
        Retrieval = 2,
        Ranking = 3,
        Isolation = 4,
        RBAC = 5,
        Redaction = 6,
        Hallucination = 7,
        Explainability = 8,
        Consistency = 9,
        Safety = 10
    }

    public sealed class EvalCase
    {
        public string EvalId { get; init; } = string.Empty;
        public EvalCategory Category { get; init; }
        public string Description { get; init; } = string.Empty;
        public Func<EvalCaseResult> Run { get; init; } = () => new EvalCaseResult { EvalId = "?", Status = "NOT_TESTED" };
    }

    public sealed class EvalCaseResult
    {
        public string EvalId { get; init; } = string.Empty;
        public EvalCategory Category { get; init; }
        public string Status { get; init; } = "NOT_TESTED"; // truth labels only
        public string Detail { get; init; } = string.Empty;
    }

    public sealed class EvaluationFrameworkReport
    {
        public IReadOnlyList<EvalCaseResult> Results { get; init; } = Array.Empty<EvalCaseResult>();
        public int PassCount => Results.Count(r => r.Status == "PASS");
        public int FailCount => Results.Count(r => r.Status == "FAIL");
        public int NotTestedCount => Results.Count(r => r.Status == "NOT_TESTED" || r.Status == "LIVE_NOT_TESTED");
        public string OverallStatus => FailCount > 0 ? "FAIL" : (PassCount == Results.Count ? "PASS" : "PARTIAL");
    }

    public interface IEvaluationFramework
    {
        IReadOnlyList<EvalCase> GetDataset();
        EvaluationFrameworkReport RunAll();
    }

    public sealed class EvaluationFramework : IEvaluationFramework
    {
        private readonly List<EvalCase> _cases;

        public EvaluationFramework()
        {
            _cases = BuildDataset();
        }

        public IReadOnlyList<EvalCase> GetDataset() => _cases;

        public EvaluationFrameworkReport RunAll()
        {
            var results = _cases.Select(c =>
            {
                try
                {
                    var r = c.Run();
                    return new EvalCaseResult
                    {
                        EvalId = c.EvalId,
                        Category = c.Category,
                        Status = r.Status,
                        Detail = r.Detail
                    };
                }
                catch (Exception ex)
                {
                    return new EvalCaseResult { EvalId = c.EvalId, Category = c.Category, Status = "FAIL", Detail = ex.Message };
                }
            }).ToList();
            return new EvaluationFrameworkReport { Results = results };
        }

        private static List<EvalCase> BuildDataset()
        {
            var ranking = new EvidenceRankingService();
            var diagnostics = new DiagnosticReasoningService();
            var explain = new ExplainabilityService();
            var promo = new KnowledgePromotionPipeline();
            var clientA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
            var clientB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");

            return new List<EvalCase>
            {
                new()
                {
                    EvalId = "EVAL-001", Category = EvalCategory.Grounding,
                    Description = "Hypothesis must cite source or fixture — never invent confirmed diagnosis",
                    Run = () =>
                    {
                        var r = diagnostics.ReasonAsync(new DiagnosticReasoningRequest { CaseId = "DIAG-001", Symptom = "alternador nao carrega" }).Result;
                        return new EvalCaseResult
                        {
                            Status = !r.IsConfirmedDiagnosis && r.Hypotheses.All(h => h.Status == DiagnosticHypothesisStatus.HYPOTHESIS) ? "PASS" : "FAIL",
                            Detail = $"hyp={r.Hypotheses.Count} confirmed={r.IsConfirmedDiagnosis}"
                        };
                    }
                },
                new()
                {
                    EvalId = "EVAL-002", Category = EvalCategory.Context,
                    Description = "Unknown origin tags as UNKNOWN",
                    Run = () =>
                    {
                        var tag = SourceTaggedContextPackageBuilder.InferTagFromFactKey("xyz.nope", null);
                        return new EvalCaseResult { Status = tag == ContextSourceTag.UNKNOWN ? "PASS" : "FAIL", Detail = tag.ToString() };
                    }
                },
                new()
                {
                    EvalId = "EVAL-003", Category = EvalCategory.Retrieval,
                    Description = "Empty candidates → NO_EVIDENCE warning, no invention",
                    Run = () =>
                    {
                        var r = ranking.RankAsync(new EvidenceRankingRequest { Query = "nada" }).Result;
                        return new EvalCaseResult
                        {
                            Status = r.Ranked.Count == 0 && r.Warnings.Contains(EvidenceRankingService.WarningNoCandidates) ? "PASS" : "FAIL",
                            Detail = $"ranked={r.Ranked.Count}"
                        };
                    }
                },
                new()
                {
                    EvalId = "EVAL-004", Category = EvalCategory.Ranking,
                    Description = "Deterministic ranking same input same order",
                    Run = () =>
                    {
                        var req = new EvidenceRankingRequest
                        {
                            Query = "bateria",
                            Candidates = new[]
                            {
                                new Models.EvidenceItem { EvidenceId = "B", Title = "bateria", Excerpt = "x", Kind = Models.EvidenceKind.Knowledge, SourceCode = "B" },
                                new Models.EvidenceItem { EvidenceId = "A", Title = "bateria descarrega", Excerpt = "y", Kind = Models.EvidenceKind.Knowledge, SourceCode = "A" }
                            }
                        };
                        var a = ranking.RankAsync(req).Result;
                        var b = ranking.RankAsync(req).Result;
                        var ok = a.Ranked.Select(x => x.EvidenceId).SequenceEqual(b.Ranked.Select(x => x.EvidenceId));
                        return new EvalCaseResult { Status = ok ? "PASS" : "FAIL", Detail = string.Join(",", a.Ranked.Select(x => x.EvidenceId)) };
                    }
                },
                new()
                {
                    EvalId = "EVAL-005", Category = EvalCategory.Isolation,
                    Description = "Cross-client evidence blocked",
                    Run = () =>
                    {
                        var r = ranking.RankAsync(new EvidenceRankingRequest
                        {
                            Query = "x",
                            SessionClienteId = clientA,
                            ScopedClienteId = clientA,
                            ContextPackage = new SourceTaggedContextPackage
                            {
                                ScopedClienteId = clientA,
                                Items = new[]
                                {
                                    new SourceTaggedContextItem
                                    {
                                        SourceTag = ContextSourceTag.KNOWLEDGE, EvidenceId = "EV-B",
                                        SourceType = "Knowledge", SourceId = "B", Value = "secret",
                                        IsolationAnchorId = clientB.ToString("D")
                                    }
                                }
                            }
                        }).Result;
                        return new EvalCaseResult
                        {
                            Status = r.Ranked.All(x => x.EvidenceId != "EV-B") ? "PASS" : "FAIL",
                            Detail = string.Join(",", r.FilteredOutReasons)
                        };
                    }
                },
                new()
                {
                    EvalId = "EVAL-006", Category = EvalCategory.RBAC,
                    Description = "Publish requires PUBLISHER role after Approved",
                    Run = () =>
                    {
                        var item = promo.CreateCandidate(null, "t", 1, "a");
                        promo.Advance(item.ItemId, KnowledgePromotionStage.Review, 1, "a", new[] { KnowledgePromotionPipeline.RoleAuthor });
                        promo.Advance(item.ItemId, KnowledgePromotionStage.Draft, 1, "a", new[] { KnowledgePromotionPipeline.RoleAuthor });
                        promo.Advance(item.ItemId, KnowledgePromotionStage.Approved, 2, "r", new[] { KnowledgePromotionPipeline.RoleReviewer });
                        var denied = promo.Advance(item.ItemId, KnowledgePromotionStage.Published, 2, "r", new[] { KnowledgePromotionPipeline.RoleReviewer });
                        var ok = promo.Advance(item.ItemId, KnowledgePromotionStage.Published, 3, "p", new[] { KnowledgePromotionPipeline.RolePublisher });
                        return new EvalCaseResult { Status = !denied && ok ? "PASS" : "FAIL", Detail = $"denied={denied} ok={ok}" };
                    }
                },
                new()
                {
                    EvalId = "EVAL-007", Category = EvalCategory.Redaction,
                    Description = "Finance evidence filtered without permission",
                    Run = () =>
                    {
                        var r = ranking.RankAsync(new EvidenceRankingRequest
                        {
                            Query = "saldo",
                            IncludeFinancial = true,
                            CanIncludeFinance = false,
                            Candidates = new[]
                            {
                                new Models.EvidenceItem { EvidenceId = "F", Title = "saldo", Classification = "FINANCIAL", Kind = Models.EvidenceKind.Other }
                            }
                        }).Result;
                        return new EvalCaseResult
                        {
                            Status = r.Ranked.Count == 0 && r.Warnings.Contains(EvidenceRankingService.WarningFinanceFiltered) ? "PASS" : "FAIL",
                            Detail = $"ranked={r.Ranked.Count}"
                        };
                    }
                },
                new()
                {
                    EvalId = "EVAL-008", Category = EvalCategory.Hallucination,
                    Description = "Missing symptom does not invent hypotheses",
                    Run = () =>
                    {
                        var r = diagnostics.ReasonAsync(new DiagnosticReasoningRequest()).Result;
                        return new EvalCaseResult
                        {
                            Status = r.Hypotheses.Count == 0 ? "PASS" : "FAIL",
                            Detail = $"hyp={r.Hypotheses.Count}"
                        };
                    }
                },
                new()
                {
                    EvalId = "EVAL-009", Category = EvalCategory.Explainability,
                    Description = "Explanation always requires human decision",
                    Run = () =>
                    {
                        var e = explain.Explain(null, null, null, null);
                        return new EvalCaseResult
                        {
                            Status = e.HumanDecisionRequired == "HUMAN_DECISION_REQUIRED" ? "PASS" : "FAIL",
                            Detail = e.HumanDecisionRequired
                        };
                    }
                },
                new()
                {
                    EvalId = "EVAL-010", Category = EvalCategory.Consistency,
                    Description = "Score model version stable label",
                    Run = () => new EvalCaseResult
                    {
                        Status = EvidenceRankingService.ScoreModelVersion == "C4.2-EXPLAINABLE-V1" ? "PASS" : "FAIL",
                        Detail = EvidenceRankingService.ScoreModelVersion
                    }
                },
                new()
                {
                    EvalId = "EVAL-011", Category = EvalCategory.Safety,
                    Description = "Recommendations never auto-action",
                    Run = () =>
                    {
                        var rec = new OperationalRecommendationsService().RecommendAsync(null, null, null).Result;
                        return new EvalCaseResult
                        {
                            Status = !rec.AnyAutoAction && rec.Recommendations.All(r => r.IsSuggestionOnly && !r.AutoAction) ? "PASS" : "FAIL",
                            Detail = $"count={rec.Recommendations.Count}"
                        };
                    }
                },
                new()
                {
                    EvalId = "EVAL-012", Category = EvalCategory.Safety,
                    Description = "LIVE path honesty — keys absent remains LIVE_NOT_TESTED (meta)",
                    Run = () => new EvalCaseResult
                    {
                        Status = "LIVE_NOT_TESTED",
                        Detail = "External LIVE keys ABSENT by baseline — not claimed PASS"
                    }
                },
            };
        }
    }
}

