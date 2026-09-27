using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    // ========== C4.4 Client 360 Intelligence ==========
    public sealed class Client360IntelligenceResult
    {
        public Guid ClienteId { get; init; }
        public bool Found { get; init; }
        public SourceTaggedContextPackage? Package { get; init; }
        public Intelligence360Overlay? Overlay { get; init; }
        public IReadOnlyList<string> OperationalFacts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public bool InventedHistory => false;
        public string Provider { get; init; } = "PRIMOX_CLIENT_360_C4_4";
    }

    public interface IClient360IntelligenceService
    {
        Task<Client360IntelligenceResult> BuildAsync(Guid clienteId, Guid? sessionClienteId = null, SourceTaggedActorContext? actor = null, CancellationToken ct = default);
    }

    public sealed class Client360IntelligenceService : IClient360IntelligenceService
    {
        public const string WarningCrossClient = "CROSS_CLIENT_DENIED";
        private readonly ISourceTaggedContextPackageBuilder _packageBuilder;
        private readonly IIntelligence360Enricher _enricher;

        public Client360IntelligenceService(ISourceTaggedContextPackageBuilder packageBuilder, IIntelligence360Enricher enricher)
        {
            _packageBuilder = packageBuilder ?? throw new ArgumentNullException(nameof(packageBuilder));
            _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        }

        public async Task<Client360IntelligenceResult> BuildAsync(Guid clienteId, Guid? sessionClienteId = null, SourceTaggedActorContext? actor = null, CancellationToken ct = default)
        {
            if (sessionClienteId.HasValue && sessionClienteId.Value != Guid.Empty && sessionClienteId.Value != clienteId)
            {
                return new Client360IntelligenceResult
                {
                    ClienteId = clienteId,
                    Found = false,
                    Warnings = new[] { WarningCrossClient }
                };
            }

            var actorEff = actor ?? new SourceTaggedActorContext { SessionClienteId = sessionClienteId ?? clienteId };
            var package = await _packageBuilder.BuildAsync(
                new ContextCompositionRequest { ClienteId = clienteId, SessionClienteId = sessionClienteId ?? clienteId, Query = "client360" },
                actor: actorEff, ct: ct).ConfigureAwait(false);
            var overlay = await _enricher.ForClientAsync(clienteId, ct).ConfigureAwait(false);

            var facts = package.ByTag(ContextSourceTag.CLIENTE)
                .Concat(package.ByTag(ContextSourceTag.HISTORY))
                .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                .Select(i => $"{i.Key}={i.Value}")
                .ToList();

            return new Client360IntelligenceResult
            {
                ClienteId = clienteId,
                Found = package.HasAnyProvenAnchor || overlay.Found,
                Package = package,
                Overlay = overlay,
                OperationalFacts = facts,
                Warnings = package.Warnings.Concat(overlay.Alerts).Distinct().ToList()
            };
        }
    }

    // ========== C4.5 Vehicle 360 ==========
    public sealed class Vehicle360IntelligenceResult
    {
        public Guid VehicleId { get; init; }
        public Guid? ClienteId { get; init; }
        public bool Found { get; init; }
        public SourceTaggedContextPackage? Package { get; init; }
        public Intelligence360Overlay? Overlay { get; init; }
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public bool InventedMaintenance => false;
        public string Provider { get; init; } = "PRIMOX_VEHICLE_360_C4_5";
    }

    public interface IVehicle360IntelligenceService
    {
        Task<Vehicle360IntelligenceResult> BuildAsync(Guid vehicleId, Guid? sessionClienteId = null, Guid? expectedClienteId = null, CancellationToken ct = default);
    }

    public sealed class Vehicle360IntelligenceService : IVehicle360IntelligenceService
    {
        public const string WarningMultiVehicleLeak = "MULTI_VEHICLE_CLIENT_ISOLATION";
        private readonly ISourceTaggedContextPackageBuilder _packageBuilder;
        private readonly IIntelligence360Enricher _enricher;

        public Vehicle360IntelligenceService(ISourceTaggedContextPackageBuilder packageBuilder, IIntelligence360Enricher enricher)
        {
            _packageBuilder = packageBuilder;
            _enricher = enricher;
        }

        public async Task<Vehicle360IntelligenceResult> BuildAsync(Guid vehicleId, Guid? sessionClienteId = null, Guid? expectedClienteId = null, CancellationToken ct = default)
        {
            var package = await _packageBuilder.BuildAsync(
                new ContextCompositionRequest
                {
                    VehicleId = vehicleId,
                    ClienteId = expectedClienteId,
                    SessionClienteId = sessionClienteId,
                    Query = "vehicle360"
                }, ct: ct).ConfigureAwait(false);

            if (expectedClienteId.HasValue &&
                package.ScopedClienteId.HasValue &&
                package.ScopedClienteId.Value != Guid.Empty &&
                expectedClienteId.Value != Guid.Empty &&
                package.ScopedClienteId.Value != expectedClienteId.Value)
            {
                return new Vehicle360IntelligenceResult
                {
                    VehicleId = vehicleId,
                    Found = false,
                    Warnings = new[] { WarningMultiVehicleLeak }
                };
            }

            var overlay = await _enricher.ForVehicleAsync(vehicleId, ct).ConfigureAwait(false);
            return new Vehicle360IntelligenceResult
            {
                VehicleId = vehicleId,
                ClienteId = package.ScopedClienteId,
                Found = package.HasAnyProvenAnchor || overlay.Found,
                Package = package,
                Overlay = overlay,
                Warnings = package.Warnings.Concat(overlay.Alerts).Distinct().ToList()
            };
        }
    }

    // ========== C4.6 WorkOrder Intelligence ==========
    public sealed class WorkOrderIntelligenceAssistResult
    {
        public Guid WorkOrderId { get; init; }
        public SourceTaggedContextPackage? Package { get; init; }
        public IReadOnlyList<RankedEvidenceItem> RankedEvidence { get; init; } = Array.Empty<RankedEvidenceItem>();
        public IReadOnlyList<DiagnosticHypothesis> Hypotheses { get; init; } = Array.Empty<DiagnosticHypothesis>();
        public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public bool AutoAlteredWorkOrder => false;
        public string Provider { get; init; } = "PRIMOX_WO_INTELLIGENCE_C4_6";
    }

    public interface IWorkOrderIntelligenceAssistService
    {
        Task<WorkOrderIntelligenceAssistResult> AssistAsync(Guid workOrderId, string? query = null, Guid? sessionClienteId = null, CancellationToken ct = default);
    }

    public sealed class WorkOrderIntelligenceAssistService : IWorkOrderIntelligenceAssistService
    {
        private readonly ISourceTaggedContextPackageBuilder _packages;
        private readonly IEvidenceRankingService _ranking;
        private readonly IDiagnosticReasoningService _diagnostics;

        public WorkOrderIntelligenceAssistService(
            ISourceTaggedContextPackageBuilder packages,
            IEvidenceRankingService ranking,
            IDiagnosticReasoningService diagnostics)
        {
            _packages = packages;
            _ranking = ranking;
            _diagnostics = diagnostics;
        }

        public async Task<WorkOrderIntelligenceAssistResult> AssistAsync(Guid workOrderId, string? query = null, Guid? sessionClienteId = null, CancellationToken ct = default)
        {
            var package = await _packages.BuildAsync(
                new ContextCompositionRequest { WorkOrderId = workOrderId, SessionClienteId = sessionClienteId, Query = query ?? "wo-assist" },
                ct: ct).ConfigureAwait(false);

            var symptom = package.Items.FirstOrDefault(i => i.Key.Contains("problema", StringComparison.OrdinalIgnoreCase))?.Value
                          ?? query
                          ?? string.Empty;

            var ranked = await _ranking.RankAsync(new EvidenceRankingRequest
            {
                Query = string.IsNullOrWhiteSpace(symptom) ? "os" : symptom,
                SessionClienteId = sessionClienteId,
                ScopedClienteId = package.ScopedClienteId,
                ScopedVeiculoId = package.ScopedVeiculoId,
                ScopedWorkOrderId = workOrderId,
                ContextPackage = package
            }, ct).ConfigureAwait(false);

            var diag = await _diagnostics.ReasonAsync(new DiagnosticReasoningRequest
            {
                Symptom = symptom,
                WorkOrderId = workOrderId,
                ClienteId = package.ScopedClienteId,
                VehicleId = package.ScopedVeiculoId,
                SessionClienteId = sessionClienteId,
                RankedEvidence = ranked.Ranked,
                ContextPackage = package
            }, ct).ConfigureAwait(false);

            var suggestions = diag.Hypotheses
                .Select(h => $"SUGGEST check: {h.RecommendedCheck} (cause hyp: {h.PossibleCause}) [{h.Confidence}]")
                .ToList();

            return new WorkOrderIntelligenceAssistResult
            {
                WorkOrderId = workOrderId,
                Package = package,
                RankedEvidence = ranked.Ranked,
                Hypotheses = diag.Hypotheses,
                Suggestions = suggestions,
                Warnings = package.Warnings.Concat(ranked.Warnings).Concat(diag.Warnings).Distinct().ToList()
            };
        }
    }

    // ========== C4.7 Knowledge Promotion Pipeline ==========
    public enum KnowledgePromotionStage
    {
        Candidate = 0,
        Review = 1,
        Draft = 2,
        Approved = 3,
        Published = 4,
        Rejected = 99
    }

    public sealed class KnowledgePromotionPipelineItem
    {
        public Guid ItemId { get; init; } = Guid.NewGuid();
        public Guid? SourceWorkOrderId { get; init; }
        public string Title { get; init; } = string.Empty;
        public KnowledgePromotionStage Stage { get; set; } = KnowledgePromotionStage.Candidate;
        public int AuthorUserId { get; init; }
        public string AuthorName { get; init; } = string.Empty;
        public int? ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public string? Note { get; set; }
        public bool AutoPublishedFromOs => false;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    }

    public interface IKnowledgePromotionPipeline
    {
        KnowledgePromotionPipelineItem CreateCandidate(Guid? sourceWorkOrderId, string title, int authorUserId, string authorName);
        bool Advance(Guid itemId, KnowledgePromotionStage target, int actorUserId, string actorName, IReadOnlyList<string> actorRoles, string? note = null);
        IReadOnlyList<KnowledgePromotionPipelineItem> ListAll();
    }

    public sealed class KnowledgePromotionPipeline : IKnowledgePromotionPipeline
    {
        public const string RoleReviewer = "KNOWLEDGE_REVIEWER";
        public const string RolePublisher = "KNOWLEDGE_PUBLISHER";
        public const string RoleAuthor = "KNOWLEDGE_AUTHOR";

        private readonly List<KnowledgePromotionPipelineItem> _items = new();
        private readonly object _gate = new();

        public KnowledgePromotionPipelineItem CreateCandidate(Guid? sourceWorkOrderId, string title, int authorUserId, string authorName)
        {
            var item = new KnowledgePromotionPipelineItem
            {
                SourceWorkOrderId = sourceWorkOrderId,
                Title = title ?? string.Empty,
                Stage = KnowledgePromotionStage.Candidate,
                AuthorUserId = authorUserId,
                AuthorName = authorName ?? string.Empty
            };
            lock (_gate) { _items.Add(item); }
            return item;
        }

        public bool Advance(Guid itemId, KnowledgePromotionStage target, int actorUserId, string actorName, IReadOnlyList<string> actorRoles, string? note = null)
        {
            actorRoles ??= Array.Empty<string>();
            lock (_gate)
            {
                var item = _items.FirstOrDefault(i => i.ItemId == itemId);
                if (item == null) return false;

                // Never auto-publish from OS — Published requires explicit RolePublisher + Approved prior
                if (target == KnowledgePromotionStage.Published)
                {
                    if (item.Stage != KnowledgePromotionStage.Approved) return false;
                    if (!actorRoles.Contains(RolePublisher, StringComparer.OrdinalIgnoreCase)) return false;
                }
                else if (target == KnowledgePromotionStage.Approved)
                {
                    if (item.Stage is not (KnowledgePromotionStage.Review or KnowledgePromotionStage.Draft)) return false;
                    if (!actorRoles.Contains(RoleReviewer, StringComparer.OrdinalIgnoreCase) &&
                        !actorRoles.Contains(RolePublisher, StringComparer.OrdinalIgnoreCase)) return false;
                }
                else if (target == KnowledgePromotionStage.Review || target == KnowledgePromotionStage.Draft)
                {
                    if (item.Stage is not (KnowledgePromotionStage.Candidate or KnowledgePromotionStage.Review or KnowledgePromotionStage.Draft)) return false;
                    if (!actorRoles.Contains(RoleAuthor, StringComparer.OrdinalIgnoreCase) &&
                        !actorRoles.Contains(RoleReviewer, StringComparer.OrdinalIgnoreCase)) return false;
                }
                else if (target == KnowledgePromotionStage.Rejected)
                {
                    if (!actorRoles.Contains(RoleReviewer, StringComparer.OrdinalIgnoreCase) &&
                        !actorRoles.Contains(RolePublisher, StringComparer.OrdinalIgnoreCase)) return false;
                }
                else
                {
                    return false;
                }

                item.Stage = target;
                item.ActorUserId = actorUserId;
                item.ActorName = actorName;
                item.Note = note;
                item.UpdatedAt = DateTimeOffset.Now;
                return true;
            }
        }

        public IReadOnlyList<KnowledgePromotionPipelineItem> ListAll()
        {
            lock (_gate) { return _items.ToList(); }
        }
    }

    // ========== C4.8 Operational Recommendations ==========
    public sealed class OperationalRecommendation
    {
        public string RecommendationId { get; init; } = Guid.NewGuid().ToString("N");
        public string Text { get; init; } = string.Empty;
        public string EvidenceId { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public bool IsSuggestionOnly => true;
        public bool AutoAction => false;
    }

    public sealed class OperationalRecommendationsResult
    {
        public IReadOnlyList<OperationalRecommendation> Recommendations { get; init; } = Array.Empty<OperationalRecommendation>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public bool AnyAutoAction => false;
    }

    public interface IOperationalRecommendationsService
    {
        Task<OperationalRecommendationsResult> RecommendAsync(
            SourceTaggedContextPackage? package,
            IReadOnlyList<RankedEvidenceItem>? ranked,
            IReadOnlyList<DiagnosticHypothesis>? hypotheses,
            CancellationToken ct = default);
    }

    public sealed class OperationalRecommendationsService : IOperationalRecommendationsService
    {
        public Task<OperationalRecommendationsResult> RecommendAsync(
            SourceTaggedContextPackage? package,
            IReadOnlyList<RankedEvidenceItem>? ranked,
            IReadOnlyList<DiagnosticHypothesis>? hypotheses,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var list = new List<OperationalRecommendation>();
            var warnings = new List<string> { "SUGGESTIONS_ONLY_NO_AUTO_ACTION" };

            foreach (var h in hypotheses ?? Array.Empty<DiagnosticHypothesis>())
            {
                list.Add(new OperationalRecommendation
                {
                    Text = h.RecommendedCheck,
                    EvidenceId = h.EvidenceIds.FirstOrDefault() ?? string.Empty,
                    Source = h.Source,
                    Reason = $"Hypothesis {h.HypothesisId}: {h.PossibleCause} (confidence={h.Confidence})"
                });
            }

            foreach (var e in (ranked ?? Array.Empty<RankedEvidenceItem>()).Take(5))
            {
                if (list.Any(r => r.EvidenceId == e.EvidenceId)) continue;
                list.Add(new OperationalRecommendation
                {
                    Text = $"Revisar evidência: {e.Title}",
                    EvidenceId = e.EvidenceId,
                    Source = $"{e.SourceType}:{e.SourceId}",
                    Reason = e.RankingReason
                });
            }

            if (package?.MissingData?.Count > 0)
            {
                list.Add(new OperationalRecommendation
                {
                    Text = "Completar dados operacionais ausentes antes de decidir",
                    Source = "CONTEXT_MISSING",
                    Reason = string.Join(",", package.MissingData.Take(5)),
                    EvidenceId = string.Empty
                });
            }

            return Task.FromResult(new OperationalRecommendationsResult
            {
                Recommendations = list,
                Warnings = warnings
            });
        }
    }

    // ========== C4.9 Explainability ==========
    public sealed class IntelligenceExplanation
    {
        public string ContextSummary { get; init; } = string.Empty;
        public IReadOnlyList<string> EvidenceOrigins { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> RankingReasons { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Limits { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Conflicts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Uncertainties { get; init; } = Array.Empty<string>();
        public string HumanDecisionRequired { get; init; } = "HUMAN_DECISION_REQUIRED";
    }

    public interface IExplainabilityService
    {
        IntelligenceExplanation Explain(
            SourceTaggedContextPackage? package,
            EvidenceRankingResult? ranking,
            DiagnosticReasoningResult? diagnostics,
            OperationalRecommendationsResult? recommendations);
    }

    public sealed class ExplainabilityService : IExplainabilityService
    {
        public IntelligenceExplanation Explain(
            SourceTaggedContextPackage? package,
            EvidenceRankingResult? ranking,
            DiagnosticReasoningResult? diagnostics,
            OperationalRecommendationsResult? recommendations)
        {
            var origins = new List<string>();
            if (package != null)
            {
                origins.AddRange(package.Items.Select(i => $"{i.SourceTag}:{i.SourceType}:{i.SourceId}").Distinct());
            }
            if (ranking != null)
            {
                origins.AddRange(ranking.Ranked.Select(r => $"EVIDENCE:{r.SourceType}:{r.SourceId}"));
            }

            var limits = new List<string>
            {
                "AI suggests only — no buy/pay/stock/OS/client/vehicle/fiscal/WhatsApp autonomous actions",
                "Soft FKs may be RELATIONSHIP_NOT_PROVEN",
                "LIVE external provider LIVE_NOT_TESTED when keys ABSENT",
                "Durable audit NOT_IMPLEMENTED"
            };
            if (package?.MissingData != null) limits.AddRange(package.MissingData.Select(m => "MISSING:" + m));

            var conflicts = package?.Conflicts?.ToList() ?? new List<string>();
            var uncertainties = new List<string>();
            if (diagnostics?.Hypotheses != null)
            {
                uncertainties.AddRange(diagnostics.Hypotheses.Select(h =>
                    $"HYPOTHESIS {h.HypothesisId} confidence={h.Confidence} (not confirmed)"));
            }
            if (diagnostics?.Warnings != null) uncertainties.AddRange(diagnostics.Warnings);

            return new IntelligenceExplanation
            {
                ContextSummary = package == null
                    ? "NO_CONTEXT_PACKAGE"
                    : $"items={package.Items.Count} anchor={package.HasAnyProvenAnchor} isolation={package.IsolationStatus}",
                EvidenceOrigins = origins.Distinct().Take(50).ToList(),
                RankingReasons = ranking?.Ranked.Select(r => r.RankingReason).Take(20).ToList() ?? new List<string>(),
                Limits = limits.Distinct().ToList(),
                Conflicts = conflicts,
                Uncertainties = uncertainties.Distinct().ToList()
            };
        }
    }
}
