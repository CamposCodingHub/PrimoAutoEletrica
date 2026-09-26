using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IWorkOrderContextService
    {
        Task<WorkOrderIntelligenceContext> BuildAsync(Guid workOrderId, CancellationToken ct = default);
    }

    /// <summary>
    /// C2.3 — work-order intelligence context. Money fields never enter facts.
    /// Vehicle link proven only when OrdensServico.VeiculoId points to an existing Veiculos row.
    /// DiagnosticCase.WorkOrderId is a soft FK — proven only when case.WorkOrderId == OS.Id and OS exists.
    /// </summary>
    public sealed class WorkOrderContextService : IWorkOrderContextService
    {
        public const string WarningNotFound = "WORKORDER_NOT_FOUND";
        public const string WarningEmptyId = "WORKORDER_ID_EMPTY";
        public const string WarningVehicleUnproven = "VEHICLE_LINK_NOT_PROVEN";
        public const string WarningClientUnproven = "CLIENT_LINK_NOT_PROVEN";

        private readonly IOrdemServicoRepository _ordens;
        private readonly IClienteRepository _clientes;
        private readonly IKnowledgeRepository? _knowledge;
        private readonly LoggerService? _logger;

        public WorkOrderContextService(
            IOrdemServicoRepository ordens,
            IClienteRepository clientes,
            IKnowledgeRepository? knowledge = null,
            LoggerService? logger = null)
        {
            _ordens = ordens ?? throw new ArgumentNullException(nameof(ordens));
            _clientes = clientes ?? throw new ArgumentNullException(nameof(clientes));
            _knowledge = knowledge;
            _logger = logger;
        }

        public async Task<WorkOrderIntelligenceContext> BuildAsync(Guid workOrderId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (workOrderId == Guid.Empty)
            {
                return new WorkOrderIntelligenceContext
                {
                    RequestedWorkOrderId = workOrderId,
                    Found = false,
                    Warnings = new[] { WarningEmptyId },
                    MissingData = new[] { "WorkOrderId válido" },
                    BuiltAt = DateTimeOffset.Now
                };
            }

            var os = _ordens.ObterPorId(workOrderId);
            if (os == null)
            {
                return new WorkOrderIntelligenceContext
                {
                    RequestedWorkOrderId = workOrderId,
                    Found = false,
                    Warnings = new[] { WarningNotFound },
                    MissingData = new[] { $"OS {workOrderId} não encontrada em OrdensServico" },
                    BuiltAt = DateTimeOffset.Now
                };
            }

            var facts = new List<ContextFact>();
            var relations = new List<ContextRelation>();
            var missing = new List<string>();
            var conflicts = new List<string>();
            var warnings = new List<string>();

            void AddOsFact(string key, string label, string? value, string field, string classification = "TECHNICAL")
            {
                var has = !string.IsNullOrWhiteSpace(value);
                facts.Add(new ContextFact
                {
                    FactKey = key,
                    DisplayLabel = label,
                    Value = has ? value!.Trim() : null,
                    Status = has ? ContextProvenanceStatus.PROVEN : ContextProvenanceStatus.MISSING,
                    SourceEntity = "OrdensServico",
                    SourceEntityId = os.Id.ToString("N"),
                    SourceField = field,
                    OriginDescription = $"OrdensServico.{field} WHERE Id={os.Id:D}",
                    Classification = classification
                });
                if (!has) missing.Add($"{label} (OrdensServico.{field})");
            }

            AddOsFact("os.numero", "Número OS", os.Numero, "Numero", "OPERATIONAL");
            AddOsFact("os.status", "Status", os.Status, "Status", "OPERATIONAL");
            AddOsFact("os.problema", "Problema relatado", os.ProblemaRelatado, "ProblemaRelatado");
            AddOsFact("os.diagnostico", "Diagnóstico", FirstNonEmpty(os.DiagnosticoFinal, os.Diagnostico, os.DiagnosticoInicial), "Diagnostico");
            AddOsFact("os.prioridade", "Prioridade", os.Prioridade, "Prioridade", "OPERATIONAL");
            // Snapshots are denormalized — still proven as OS-row fields, but vehicle/client identity needs FK proof separately.
            AddOsFact("os.placa_snapshot", "Placa (snapshot OS)", os.PlacaSnapshot, "PlacaSnapshot", "OPERATIONAL");
            AddOsFact("os.veiculo_snapshot", "Veículo (snapshot OS)", os.VeiculoDescricaoSnapshot, "VeiculoDescricaoSnapshot", "OPERATIONAL");

            Guid? clienteProven = null;
            if (os.ClienteId != Guid.Empty)
            {
                var cliente = _clientes.ObterPorId(os.ClienteId);
                if (cliente != null)
                {
                    clienteProven = cliente.Id;
                    relations.Add(new ContextRelation
                    {
                        RelationType = "OS_BELONGS_TO_CLIENT",
                        FromId = os.Id,
                        ToId = cliente.Id,
                        Status = ContextProvenanceStatus.PROVEN,
                        Reason = "OrdensServico.ClienteId matches Clientes.Id",
                        SourceEntity = "OrdensServico",
                        SourceField = "ClienteId"
                    });
                }
                else
                {
                    relations.Add(new ContextRelation
                    {
                        RelationType = "OS_BELONGS_TO_CLIENT",
                        FromId = os.Id,
                        ToId = os.ClienteId,
                        Status = ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN,
                        Reason = "OrdensServico.ClienteId set but Clientes row missing",
                        SourceEntity = "OrdensServico",
                        SourceField = "ClienteId"
                    });
                    warnings.Add(WarningClientUnproven);
                    missing.Add("Cliente da OS (FK alvo ausente)");
                }
            }
            else
            {
                relations.Add(new ContextRelation
                {
                    RelationType = "OS_BELONGS_TO_CLIENT",
                    FromId = os.Id,
                    ToId = null,
                    Status = ContextProvenanceStatus.MISSING,
                    Reason = "OrdensServico.ClienteId empty",
                    SourceEntity = "OrdensServico",
                    SourceField = "ClienteId"
                });
                missing.Add("ClienteId da OS");
            }

            Guid? veiculoProven = null;
            if (os.VeiculoId.HasValue && os.VeiculoId.Value != Guid.Empty)
            {
                var veiculo = _clientes.ObterTodosVeiculos().FirstOrDefault(v => v.Id == os.VeiculoId.Value);
                if (veiculo != null)
                {
                    veiculoProven = veiculo.Id;
                    relations.Add(new ContextRelation
                    {
                        RelationType = "OS_BELONGS_TO_VEHICLE",
                        FromId = os.Id,
                        ToId = veiculo.Id,
                        Status = ContextProvenanceStatus.PROVEN,
                        Reason = "OrdensServico.VeiculoId matches Veiculos.Id",
                        SourceEntity = "OrdensServico",
                        SourceField = "VeiculoId"
                    });

                    if (clienteProven.HasValue &&
                        veiculo.ClienteId.HasValue &&
                        veiculo.ClienteId.Value != Guid.Empty &&
                        veiculo.ClienteId.Value != clienteProven.Value)
                    {
                        conflicts.Add($"Veiculos.ClienteId={veiculo.ClienteId:D} diverge de OrdensServico.ClienteId={clienteProven:D}");
                    }
                }
                else
                {
                    relations.Add(new ContextRelation
                    {
                        RelationType = "OS_BELONGS_TO_VEHICLE",
                        FromId = os.Id,
                        ToId = os.VeiculoId,
                        Status = ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN,
                        Reason = "OrdensServico.VeiculoId set but Veiculos row missing",
                        SourceEntity = "OrdensServico",
                        SourceField = "VeiculoId"
                    });
                    warnings.Add(WarningVehicleUnproven);
                    missing.Add("Veículo da OS (FK alvo ausente)");
                }
            }
            else
            {
                relations.Add(new ContextRelation
                {
                    RelationType = "OS_BELONGS_TO_VEHICLE",
                    FromId = os.Id,
                    ToId = null,
                    Status = ContextProvenanceStatus.MISSING,
                    Reason = "OrdensServico.VeiculoId empty — snapshot placa/descrição is not a proven link",
                    SourceEntity = "OrdensServico",
                    SourceField = "VeiculoId"
                });
                missing.Add("VeiculoId comprovado na OS");
                if (!string.IsNullOrWhiteSpace(os.PlacaSnapshot))
                {
                    warnings.Add("VEHICLE_SNAPSHOT_ONLY_NOT_PROVEN");
                }
            }

            var serviceLabels = new List<string>();
            var partLabels = new List<string>();
            foreach (var item in os.Itens ?? Enumerable.Empty<OrdemServicoItem>())
            {
                var label = string.IsNullOrWhiteSpace(item.Descricao) ? null : item.Descricao.Trim();
                if (label == null) continue;
                // Heuristic on Tipo when present; never invent prices.
                var tipo = (item.Tipo ?? string.Empty).Trim();
                if (tipo.Contains("peca", StringComparison.OrdinalIgnoreCase) ||
                    tipo.Contains("peça", StringComparison.OrdinalIgnoreCase) ||
                    tipo.Contains("produto", StringComparison.OrdinalIgnoreCase))
                {
                    partLabels.Add(label);
                }
                else
                {
                    serviceLabels.Add(label);
                }

                facts.Add(new ContextFact
                {
                    FactKey = $"os.item.{item.Id:N}",
                    DisplayLabel = "Item OS",
                    Value = label,
                    Status = ContextProvenanceStatus.PROVEN,
                    SourceEntity = "OrdemServicoItens",
                    SourceEntityId = item.Id.ToString("N"),
                    SourceField = "Descricao",
                    OriginDescription = $"OrdemServicoItens.Descricao WHERE OrdemServicoId={os.Id:D}",
                    Classification = "OPERATIONAL"
                });
            }

            if (serviceLabels.Count == 0 && partLabels.Count == 0)
            {
                missing.Add("Itens de serviço/peças na OS");
            }

            var provenCaseIds = new List<Guid>();
            if (_knowledge != null)
            {
                var casos = await _knowledge.ObterCasosAsync(osId: workOrderId, ct: ct).ConfigureAwait(false);
                foreach (var c in casos)
                {
                    if (c.WorkOrderId == workOrderId)
                    {
                        provenCaseIds.Add(c.CaseId);
                        relations.Add(new ContextRelation
                        {
                            RelationType = "DIAGNOSTIC_CASE_OF_OS",
                            FromId = c.CaseId,
                            ToId = workOrderId,
                            Status = ContextProvenanceStatus.PROVEN,
                            Reason = "DiagnosticCases.WorkOrderId equals existing OrdensServico.Id",
                            SourceEntity = "DiagnosticCases",
                            SourceField = "WorkOrderId"
                        });
                    }
                    else
                    {
                        relations.Add(new ContextRelation
                        {
                            RelationType = "DIAGNOSTIC_CASE_OF_OS",
                            FromId = c.CaseId,
                            ToId = c.WorkOrderId,
                            Status = ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN,
                            Reason = "SOFT_FK mismatch after query",
                            SourceEntity = "DiagnosticCases",
                            SourceField = "WorkOrderId"
                        });
                    }
                }
            }

            _logger?.LogInfo($"Built OS context {workOrderId:D}: client={clienteProven} vehicle={veiculoProven} cases={provenCaseIds.Count}", "WorkOrderContext");

            return new WorkOrderIntelligenceContext
            {
                RequestedWorkOrderId = workOrderId,
                Found = true,
                ClienteIdProven = clienteProven,
                VeiculoIdProven = veiculoProven,
                Facts = facts,
                Relations = relations,
                ProvenDiagnosticCaseIds = provenCaseIds,
                ServiceItemLabels = serviceLabels,
                PartItemLabels = partLabels,
                MissingData = missing.Distinct().ToList(),
                Conflicts = conflicts,
                Warnings = warnings.Distinct().ToList(),
                BuiltAt = DateTimeOffset.Now
            };
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            return null;
        }
    }
}