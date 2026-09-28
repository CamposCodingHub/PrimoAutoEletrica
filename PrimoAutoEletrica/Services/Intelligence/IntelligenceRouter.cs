using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Intelligence
{
    public enum IntelligenceRouteDecision
    {
        DeterministicRules = 0,
        LocalModel = 1,
        ExternalModel = 2,
        SafeFallback = 3,
        BlockedUnsafeIntent = 4
    }

    public sealed class IntelligenceRouteResult
    {
        public IntelligenceRouteDecision Decision { get; init; }
        public string Reason { get; init; } = string.Empty;
        public AssistantResponse Response { get; init; } = new();
        public ProviderBenchmarkRecord? Metrics { get; init; }
    }

    /// <summary>
    /// C6.11 — Intelligence Router prototype:
    /// deterministic first → local model → external → safe fallback.
    /// Blocks buy/finance/stock/irreversible/external message/critical data auto-change.
    /// Assistive only.
    /// </summary>
    public sealed class IntelligenceRouter : IAssistantProvider
    {
        private static readonly string[] UnsafeIntentMarkers =
        {
            "compre automaticamente", "comprar agora", "baixa estoque", "alterar estoque",
            "pagar fornecedor", "emitir nfe", "enviar whatsapp", "enviar email ao cliente",
            "apagar cliente", "delete from", "drop table", "transferir dinheiro",
            "approve purchase", "auto-buy", "mutate stock", "send message to customer"
        };

        private readonly IAssistantProvider _deterministic;
        private readonly IAssistantProvider? _localModel;
        private readonly IAssistantProvider? _externalModel;

        public IntelligenceRouter(
            IAssistantProvider deterministic,
            IAssistantProvider? localModel = null,
            IAssistantProvider? externalModel = null)
        {
            _deterministic = deterministic ?? throw new ArgumentNullException(nameof(deterministic));
            _localModel = localModel;
            _externalModel = externalModel;
        }

        public string ProviderId => "PRIMOX_INTELLIGENCE_ROUTER";
        public string DisplayName => "PRIMOX Intelligence Router (deterministic→local→external→fallback)";
        public bool IsConfigured => true;

        public async Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            var routed = await RouteAsync(context, cancellationToken).ConfigureAwait(false);
            return routed.Response;
        }

        public async Task<IntelligenceRouteResult> RouteAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            var q = context.Query ?? string.Empty;

            if (IsUnsafeIntent(q))
            {
                var blocked = new AssistantResponse
                {
                    AnswerMarkdown = "### Acao bloqueada\n\nPedido parece envolver compra/estoque/financeiro/mensagem externa ou mudanca critica automatica. O PRIMOX Assist e **apenas consultivo** e nao executa essas acoes.",
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Warnings = new[] { "ROUTER_BLOCKED_UNSAFE_INTENT" },
                    Provider = ProviderId,
                    CorrelationId = context.CorrelationId,
                    Timestamp = DateTimeOffset.UtcNow,
                    RecommendedActions = new[] { "Use fluxos operacionais PRIMOX com RBAC humano para compra/estoque/financeiro" }
                };
                return new IntelligenceRouteResult
                {
                    Decision = IntelligenceRouteDecision.BlockedUnsafeIntent,
                    Reason = "Unsafe intent markers — assistive only",
                    Response = blocked
                };
            }

            // 1) Deterministic first
            var detWrap = new BenchmarkingAssistantProvider(_deterministic, IntelligenceExecutionMode.GroundedLocalRule);
            var detResp = await detWrap.AskAsync(context, cancellationToken).ConfigureAwait(false);
            if (detResp.ConfidenceLevel != AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE &&
                !string.IsNullOrWhiteSpace(detResp.AnswerMarkdown))
            {
                return new IntelligenceRouteResult
                {
                    Decision = IntelligenceRouteDecision.DeterministicRules,
                    Reason = "Deterministic grounded rules produced usable assistive answer",
                    Response = Stamp(detResp),
                    Metrics = detWrap.LastRecord
                };
            }

            // 2) Local model
            if (_localModel != null && _localModel.IsConfigured)
            {
                var localWrap = new BenchmarkingAssistantProvider(_localModel, IntelligenceExecutionMode.LocalModel);
                var localResp = await localWrap.AskAsync(context, cancellationToken).ConfigureAwait(false);
                if (!HasFatal(localResp))
                {
                    return new IntelligenceRouteResult
                    {
                        Decision = IntelligenceRouteDecision.LocalModel,
                        Reason = "Escalated to local model after insufficient deterministic evidence",
                        Response = Stamp(localResp),
                        Metrics = localWrap.LastRecord
                    };
                }
            }

            // 3) External model
            if (_externalModel != null && _externalModel.IsConfigured)
            {
                var extWrap = new BenchmarkingAssistantProvider(_externalModel, IntelligenceExecutionMode.ExternalModel);
                var extResp = await extWrap.AskAsync(context, cancellationToken).ConfigureAwait(false);
                if (!HasFatal(extResp))
                {
                    return new IntelligenceRouteResult
                    {
                        Decision = IntelligenceRouteDecision.ExternalModel,
                        Reason = "Escalated to external model (experimental) after local unavailable/insufficient",
                        Response = Stamp(extResp),
                        Metrics = extWrap.LastRecord
                    };
                }
            }

            // 4) Safe fallback — return deterministic insufficient answer
            return new IntelligenceRouteResult
            {
                Decision = IntelligenceRouteDecision.SafeFallback,
                Reason = "Safe fallback to deterministic insufficient-evidence response",
                Response = Stamp(detResp),
                Metrics = detWrap.LastRecord
            };
        }

        public static bool IsUnsafeIntent(string query)
        {
            var n = (query ?? string.Empty).ToLowerInvariant();
            return UnsafeIntentMarkers.Any(m => n.Contains(m));
        }

        private static bool HasFatal(AssistantResponse r) =>
            r.Warnings != null && r.Warnings.Any(w =>
                w != null && (
                    w.Contains("ENVIRONMENT_DEPENDENCY", StringComparison.OrdinalIgnoreCase) ||
                    w.Contains("EXTERNAL_NO_KEY", StringComparison.OrdinalIgnoreCase) ||
                    w.Contains("HTTP_", StringComparison.OrdinalIgnoreCase)));

        private AssistantResponse Stamp(AssistantResponse r) => new()
        {
            AnswerMarkdown = r.AnswerMarkdown,
            Hypotheses = r.Hypotheses,
            RecommendedActions = r.RecommendedActions,
            CitedSources = r.CitedSources,
            Evidence = r.Evidence,
            Warnings = (r.Warnings ?? Array.Empty<string>()).Append("ROUTER_ASSISTIVE_ONLY").ToArray(),
            MissingInformation = r.MissingInformation,
            Provider = ProviderId,
            CorrelationId = r.CorrelationId,
            Timestamp = r.Timestamp ?? DateTimeOffset.UtcNow,
            ConfidenceLevel = r.ConfidenceLevel,
            Disclaimers = r.Disclaimers
        };
    }
}