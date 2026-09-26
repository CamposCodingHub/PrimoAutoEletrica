using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// ContextBuilder C2.1/C2.5: retrieval determinístico + Context Engine + classificação mínima.
    /// </summary>
    public sealed class AssistContextBuilder
    {
        private readonly IKnowledgeRetrievalService _retrieval;
        private readonly IOrdemServicoRepository? _ordemServicoRepository;
        private readonly IContextCompositionService? _composition;

        public AssistContextBuilder(
            IKnowledgeRetrievalService retrieval,
            IOrdemServicoRepository? ordemServicoRepository = null,
            IContextCompositionService? composition = null)
        {
            _retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _ordemServicoRepository = ordemServicoRepository;
            _composition = composition;
        }

        public async Task<AssistantQueryContext> BuildAsync(
            string query,
            Guid? veiculoId = null,
            Guid? osId = null,
            Guid? clienteId = null,
            IReadOnlyList<string>? allowedClasses = null,
            int maxEvidence = 12,
            Guid? sessionClienteId = null,
            bool includeFinancial = false,
            CancellationToken ct = default)
        {
            var tokens = KnowledgeQueryNormalizer.Tokenize(query);
            var search = await _retrieval.SearchAsync(query, maxResults: Math.Max(maxEvidence * 3, 36), ct: ct).ConfigureAwait(false);
            var groundedHits = search.Hits
                .Where(h => HitContainsAllTokens(h.Item, tokens))
                .Take(maxEvidence)
                .ToList();
            var evidence = groundedHits.Select(KnowledgeItemAdapters.ToEvidence).ToList();

            var knowledge = new List<TechnicalKnowledgeEntry>();
            var cases = new List<DiagnosticCase>();

            AssistantVehicleContext? vehicle = null;
            AssistantWorkOrderContext? workOrder = null;
            var missingFromContext = new List<string>();
            var warningsFromContext = new List<string>();
            ComposedIntelligenceContext? composed = null;

            if (_composition != null &&
                ((veiculoId.HasValue && veiculoId.Value != Guid.Empty) ||
                 (osId.HasValue && osId.Value != Guid.Empty) ||
                 (clienteId.HasValue && clienteId.Value != Guid.Empty)))
            {
                composed = await _composition.ComposeAsync(new ContextCompositionRequest
                {
                    Query = query,
                    VehicleId = veiculoId,
                    WorkOrderId = osId,
                    ClienteId = clienteId,
                    SessionClienteId = sessionClienteId,
                    IncludeFinancial = includeFinancial
                }, ct).ConfigureAwait(false);

                if (composed.Warnings.Any(w => w == ContextCompositionService.WarningCrossClientDenied))
                {
                    warningsFromContext.Add(AssistFailClosedPolicy.WarningCrossClientDenied);
                }

                missingFromContext.AddRange(composed.MissingData);
                warningsFromContext.AddRange(composed.Warnings);
                warningsFromContext.AddRange(composed.Conflicts.Select(c => "CONTEXT_CONFLICT:" + c));

                if (composed.Vehicle is { Found: true } vctx)
                {
                    string? Fact(string key) =>
                        vctx.Facts.FirstOrDefault(f => f.FactKey == key && f.Status == ContextProvenanceStatus.PROVEN)?.Value;

                    vehicle = new AssistantVehicleContext
                    {
                        Plate = Fact("vehicle.placa"),
                        Make = Fact("vehicle.marca"),
                        Model = Fact("vehicle.modelo"),
                        Year = int.TryParse(Fact("vehicle.ano"), out var y) ? y : null,
                        Voltage = Fact("vehicle.sistema_eletrico") ?? "12V"
                    };
                    clienteId ??= vctx.ClienteIdProven;
                }

                if (composed.WorkOrder is { Found: true } wctx)
                {
                    string? Fact(string key) =>
                        wctx.Facts.FirstOrDefault(f => f.FactKey == key && f.Status == ContextProvenanceStatus.PROVEN)?.Value;

                    workOrder = new AssistantWorkOrderContext
                    {
                        Number = Fact("os.numero"),
                        Symptom = Fact("os.problema"),
                        Status = Fact("os.status"),
                        CurrentItems = wctx.ServiceItemLabels.Concat(wctx.PartItemLabels).ToList()
                    };
                    clienteId ??= wctx.ClienteIdProven;
                }
            }

            if (workOrder == null && osId.HasValue && _ordemServicoRepository != null)
            {
                var os = _ordemServicoRepository.ObterPorId(osId.Value);
                if (os != null)
                {
                    workOrder = new AssistantWorkOrderContext
                    {
                        Number = os.Numero,
                        Symptom = os.ProblemaRelatado,
                        Status = os.Status,
                        CurrentItems = os.Itens?.Select(i => i.Descricao).ToList() ?? new List<string>()
                    };

                    if (vehicle == null && !string.IsNullOrWhiteSpace(os.VeiculoDescricaoSnapshot))
                    {
                        var tensao = os.VeiculoDescricaoSnapshot.Contains("24V", StringComparison.OrdinalIgnoreCase) ||
                                     os.VeiculoDescricaoSnapshot.Contains("Actros", StringComparison.OrdinalIgnoreCase) ||
                                     os.VeiculoDescricaoSnapshot.Contains("Scania", StringComparison.OrdinalIgnoreCase)
                            ? "24V"
                            : "12V";

                        vehicle = new AssistantVehicleContext
                        {
                            Plate = os.PlacaSnapshot,
                            Model = os.VeiculoDescricaoSnapshot,
                            Voltage = tensao
                        };
                        warningsFromContext.Add("VEHICLE_FROM_OS_SNAPSHOT_NOT_FK_PROVEN");
                    }

                    clienteId ??= os.ClienteId == Guid.Empty ? null : os.ClienteId;
                }
            }

            var classes = allowedClasses ?? new[] { "TECHNICAL", "OPERATIONAL" };
            var parameters = new Dictionary<string, object>
            {
                ["ContextMissingData"] = missingFromContext.Distinct().ToList(),
                ["ContextWarnings"] = warningsFromContext.Distinct().ToList(),
                ["HasProvenContextAnchor"] = composed?.HasAnyProvenAnchor ?? false,
                ["ContextProvider"] = composed?.Provider ?? "NONE"
            };

            return new AssistantQueryContext
            {
                Query = query,
                Vehicle = vehicle,
                WorkOrder = workOrder,
                RetrievedKnowledge = knowledge,
                RetrievedCases = cases,
                RetrievedEvidence = evidence,
                ClienteId = clienteId,
                AllowedClasses = classes,
                Parameters = parameters
            };
        }

        private static bool HitContainsAllTokens(KnowledgeItem item, IReadOnlyList<string> tokens)
        {
            if (tokens == null || tokens.Count == 0) return false;
            foreach (var token in tokens)
            {
                if (!ContainsToken(item.Code, token) &&
                    !ContainsToken(item.Title, token) &&
                    !ContainsToken(item.Symptom, token) &&
                    !ContainsToken(item.Diagnosis, token) &&
                    !ContainsToken(item.Solution, token) &&
                    !ContainsToken(item.BodyText, token) &&
                    !ContainsToken(item.System, token) &&
                    !(item.Tags?.Any(tag => ContainsToken(tag, token)) ?? false))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool ContainsToken(string? hay, string token)
        {
            if (string.IsNullOrWhiteSpace(hay) || string.IsNullOrWhiteSpace(token)) return false;
            var h = DeterministicKnowledgeIndex.RemoveDiacritics(hay.ToLowerInvariant());
            var t = DeterministicKnowledgeIndex.RemoveDiacritics(token.ToLowerInvariant());
            return h.Contains(t, StringComparison.Ordinal);
        }
    }
}