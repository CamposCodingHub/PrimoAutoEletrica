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
    /// ContextBuilder parcial C2.1: monta AssistantQueryContext com retrieval determinístico
    /// e filtro mínimo de classificação (sem dump financeiro/PII).
    /// </summary>
    public sealed class AssistContextBuilder
    {
        private readonly IKnowledgeRetrievalService _retrieval;
        private readonly IOrdemServicoRepository? _ordemServicoRepository;

        public AssistContextBuilder(
            IKnowledgeRetrievalService retrieval,
            IOrdemServicoRepository? ordemServicoRepository = null)
        {
            _retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _ordemServicoRepository = ordemServicoRepository;
        }

        public async Task<AssistantQueryContext> BuildAsync(
            string query,
            Guid? veiculoId = null,
            Guid? osId = null,
            Guid? clienteId = null,
            IReadOnlyList<string>? allowedClasses = null,
            int maxEvidence = 12,
            CancellationToken ct = default)
        {
            var search = await _retrieval.SearchAsync(query, maxResults: maxEvidence, ct: ct).ConfigureAwait(false);
            var evidence = search.Hits.Select(KnowledgeItemAdapters.ToEvidence).ToList();

            // Map hits back to legacy entity slots when SourceEntity matches (minimum necessary).
            var knowledge = new List<TechnicalKnowledgeEntry>();
            var cases = new List<DiagnosticCase>();
            // We do not re-hydrate full entities from index alone; AssistantService may still
            // pass repository entities. Here we only supply Evidence candidates.

            AssistantVehicleContext? vehicle = null;
            AssistantWorkOrderContext? workOrder = null;

            if (osId.HasValue && _ordemServicoRepository != null)
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

                    if (!string.IsNullOrWhiteSpace(os.VeiculoDescricaoSnapshot))
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
                    }

                    // Prefer OS cliente for authZ scope if caller omitted
                    clienteId ??= os.ClienteId == Guid.Empty ? null : os.ClienteId;
                }
            }

            var classes = allowedClasses ?? new[] { "TECHNICAL", "OPERATIONAL" };

            return new AssistantQueryContext
            {
                Query = query,
                Vehicle = vehicle,
                WorkOrder = workOrder,
                RetrievedKnowledge = knowledge,
                RetrievedCases = cases,
                RetrievedEvidence = evidence,
                ClienteId = clienteId,
                AllowedClasses = classes
            };
        }
    }
}
