using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IVehicleContextService
    {
        Task<VehicleIntelligenceContext> BuildAsync(Guid vehicleId, CancellationToken ct = default);
    }

    /// <summary>
    /// C2.3 — builds vehicle intelligence context from proven relations only.
    /// OS linked only when OrdensServico.VeiculoId == vehicleId.
    /// DiagnosticCase.VehicleId is a soft FK: proven only when the vehicle row exists
    /// AND Case.VehicleId equals the requested id; otherwise RELATIONSHIP_NOT_PROVEN.
    /// Plate-only / model-only matches are never treated as proven ownership.
    /// </summary>
    public sealed class VehicleContextService : IVehicleContextService
    {
        public const string WarningNotFound = "VEHICLE_NOT_FOUND";
        public const string WarningEmptyId = "VEHICLE_ID_EMPTY";
        public const string WarningSoftFkUnproven = "DIAGNOSTIC_CASE_SOFT_FK_UNPROVEN";

        private readonly IClienteRepository _clientes;
        private readonly IOrdemServicoRepository _ordens;
        private readonly IKnowledgeRepository? _knowledge;
        private readonly LoggerService? _logger;

        public VehicleContextService(
            IClienteRepository clientes,
            IOrdemServicoRepository ordens,
            IKnowledgeRepository? knowledge = null,
            LoggerService? logger = null)
        {
            _clientes = clientes ?? throw new ArgumentNullException(nameof(clientes));
            _ordens = ordens ?? throw new ArgumentNullException(nameof(ordens));
            _knowledge = knowledge;
            _logger = logger;
        }

        public async Task<VehicleIntelligenceContext> BuildAsync(Guid vehicleId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (vehicleId == Guid.Empty)
            {
                return new VehicleIntelligenceContext
                {
                    RequestedVehicleId = vehicleId,
                    Found = false,
                    Warnings = new[] { WarningEmptyId },
                    MissingData = new[] { "VehicleId válido" },
                    BuiltAt = DateTimeOffset.Now
                };
            }

            var veiculo = _clientes.ObterTodosVeiculos().FirstOrDefault(v => v.Id == vehicleId);
            if (veiculo == null)
            {
                return new VehicleIntelligenceContext
                {
                    RequestedVehicleId = vehicleId,
                    Found = false,
                    Warnings = new[] { WarningNotFound },
                    MissingData = new[] { $"Veículo {vehicleId} não encontrado em Veiculos" },
                    BuiltAt = DateTimeOffset.Now
                };
            }

            var facts = new List<ContextFact>();
            var relations = new List<ContextRelation>();
            var missing = new List<string>();
            var conflicts = new List<string>();
            var warnings = new List<string>();

            void AddFact(string key, string label, string? value, string field, string classification = "TECHNICAL")
            {
                var has = !string.IsNullOrWhiteSpace(value);
                facts.Add(new ContextFact
                {
                    FactKey = key,
                    DisplayLabel = label,
                    Value = has ? value!.Trim() : null,
                    Status = has ? ContextProvenanceStatus.PROVEN : ContextProvenanceStatus.MISSING,
                    SourceEntity = "Veiculos",
                    SourceEntityId = veiculo.Id.ToString("N"),
                    SourceField = field,
                    OriginDescription = $"Veiculos.{field} WHERE Id={veiculo.Id:D}",
                    Classification = classification
                });
                if (!has)
                {
                    missing.Add($"{label} (Veiculos.{field})");
                }
            }

            AddFact("vehicle.marca", "Marca", veiculo.Marca, "Marca");
            AddFact("vehicle.modelo", "Modelo", veiculo.Modelo, "Modelo");
            AddFact("vehicle.ano", "Ano", veiculo.Ano, "Ano", "OPERATIONAL");
            AddFact("vehicle.placa", "Placa", veiculo.Placa, "Placa", "OPERATIONAL");
            AddFact("vehicle.sistema_eletrico", "Sistema elétrico", veiculo.SistemaEletrico, "SistemaEletrico");
            AddFact("vehicle.tipo", "Tipo", veiculo.TipoVeiculo, "TipoVeiculo", "OPERATIONAL");
            AddFact("vehicle.problema_recorrente", "Problema recorrente", veiculo.ProblemaRecorrente, "ProblemaRecorrente");
            AddFact("vehicle.historico_tecnico", "Histórico técnico", veiculo.HistoricoTecnico, "HistoricoTecnico");
            AddFact("vehicle.km", "Quilometragem", veiculo.Quilometragem > 0 ? veiculo.Quilometragem.ToString() : null, "Quilometragem", "OPERATIONAL");
            // Chassi/Renavam = SENSITIVE-adjacent — omitted from Assist context by design.

            Guid? clienteIdProven = null;
            if (veiculo.ClienteId.HasValue && veiculo.ClienteId.Value != Guid.Empty)
            {
                var cliente = _clientes.ObterPorId(veiculo.ClienteId.Value);
                if (cliente != null)
                {
                    clienteIdProven = cliente.Id;
                    relations.Add(new ContextRelation
                    {
                        RelationType = "VEHICLE_BELONGS_TO_CLIENT",
                        FromId = veiculo.Id,
                        ToId = cliente.Id,
                        Status = ContextProvenanceStatus.PROVEN,
                        Reason = "Veiculos.ClienteId FK matches Clientes.Id",
                        SourceEntity = "Veiculos",
                        SourceField = "ClienteId"
                    });
                    facts.Add(new ContextFact
                    {
                        FactKey = "vehicle.cliente_id",
                        DisplayLabel = "ClienteId",
                        Value = cliente.Id.ToString("D"),
                        Status = ContextProvenanceStatus.PROVEN,
                        SourceEntity = "Veiculos",
                        SourceEntityId = veiculo.Id.ToString("N"),
                        SourceField = "ClienteId",
                        OriginDescription = $"Veiculos.ClienteId={cliente.Id:D} proven via Clientes.Id",
                        Classification = "OPERATIONAL"
                    });
                }
                else
                {
                    relations.Add(new ContextRelation
                    {
                        RelationType = "VEHICLE_BELONGS_TO_CLIENT",
                        FromId = veiculo.Id,
                        ToId = veiculo.ClienteId,
                        Status = ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN,
                        Reason = "Veiculos.ClienteId set but Clientes row missing",
                        SourceEntity = "Veiculos",
                        SourceField = "ClienteId"
                    });
                    warnings.Add("CLIENT_FK_TARGET_MISSING");
                    missing.Add("Cliente referenciado por Veiculos.ClienteId não existe");
                }
            }
            else
            {
                missing.Add("ClienteId do veículo");
                relations.Add(new ContextRelation
                {
                    RelationType = "VEHICLE_BELONGS_TO_CLIENT",
                    FromId = veiculo.Id,
                    ToId = null,
                    Status = ContextProvenanceStatus.MISSING,
                    Reason = "Veiculos.ClienteId empty",
                    SourceEntity = "Veiculos",
                    SourceField = "ClienteId"
                });
            }

            // Proven OS: OrdensServico.VeiculoId == vehicleId only (no plate fallback).
            var todasOs = _ordens.ObterTodos(incluirInativas: true);
            var provenOs = todasOs.Where(o => o.VeiculoId == vehicleId).OrderByDescending(o => o.DataAbertura).ToList();
            var provenOsIds = provenOs.Select(o => o.Id).ToList();

            foreach (var os in provenOs)
            {
                relations.Add(new ContextRelation
                {
                    RelationType = "OS_OF_VEHICLE",
                    FromId = os.Id,
                    ToId = vehicleId,
                    Status = ContextProvenanceStatus.PROVEN,
                    Reason = "OrdensServico.VeiculoId == Veiculos.Id",
                    SourceEntity = "OrdensServico",
                    SourceField = "VeiculoId"
                });
            }

            // Plate-only OS (same plate, different/null VeiculoId) → NOT proven
            var placa = (veiculo.Placa ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(placa))
            {
                var plateOnly = todasOs.Where(o =>
                    o.VeiculoId != vehicleId &&
                    !string.IsNullOrWhiteSpace(o.PlacaSnapshot) &&
                    string.Equals(o.PlacaSnapshot.Trim(), placa, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var os in plateOnly)
                {
                    relations.Add(new ContextRelation
                    {
                        RelationType = "OS_OF_VEHICLE_PLATE_ONLY",
                        FromId = os.Id,
                        ToId = vehicleId,
                        Status = ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN,
                        Reason = "PlacaSnapshot match without OrdensServico.VeiculoId proof",
                        SourceEntity = "OrdensServico",
                        SourceField = "PlacaSnapshot"
                    });
                }
                if (plateOnly.Count > 0)
                {
                    warnings.Add($"PLATE_ONLY_OS_UNPROVEN:{plateOnly.Count}");
                }
            }

            if (provenOs.Count == 0)
            {
                missing.Add("Histórico de OS com VeiculoId comprovado");
            }
            else
            {
                var last = provenOs[0];
                facts.Add(new ContextFact
                {
                    FactKey = "vehicle.last_os_number",
                    DisplayLabel = "Última OS (número)",
                    Value = last.Numero,
                    Status = ContextProvenanceStatus.PROVEN,
                    SourceEntity = "OrdensServico",
                    SourceEntityId = last.Id.ToString("N"),
                    SourceField = "Numero",
                    OriginDescription = $"OrdensServico.Numero WHERE VeiculoId={vehicleId:D} ORDER BY DataAbertura DESC",
                    Classification = "OPERATIONAL"
                });
                facts.Add(new ContextFact
                {
                    FactKey = "vehicle.last_os_symptom",
                    DisplayLabel = "Último sintoma relatado",
                    Value = string.IsNullOrWhiteSpace(last.ProblemaRelatado) ? null : last.ProblemaRelatado.Trim(),
                    Status = string.IsNullOrWhiteSpace(last.ProblemaRelatado) ? ContextProvenanceStatus.MISSING : ContextProvenanceStatus.PROVEN,
                    SourceEntity = "OrdensServico",
                    SourceEntityId = last.Id.ToString("N"),
                    SourceField = "ProblemaRelatado",
                    OriginDescription = $"OrdensServico.ProblemaRelatado WHERE Id={last.Id:D}",
                    Classification = "TECHNICAL"
                });
            }

            // Soft FK DiagnosticCases.VehicleId
            var provenCaseIds = new List<Guid>();
            if (_knowledge != null)
            {
                var casos = await _knowledge.ObterCasosPorVeiculoAsync(vehicleId, ct).ConfigureAwait(false);
                foreach (var c in casos)
                {
                    if (c.VehicleId == vehicleId)
                    {
                        // Soft FK points here and we already proved vehicle exists → treat as proven link for this vehicle.
                        provenCaseIds.Add(c.CaseId);
                        relations.Add(new ContextRelation
                        {
                            RelationType = "DIAGNOSTIC_CASE_OF_VEHICLE",
                            FromId = c.CaseId,
                            ToId = vehicleId,
                            Status = ContextProvenanceStatus.PROVEN,
                            Reason = "DiagnosticCases.VehicleId equals existing Veiculos.Id",
                            SourceEntity = "DiagnosticCases",
                            SourceField = "VehicleId"
                        });
                    }
                    else
                    {
                        relations.Add(new ContextRelation
                        {
                            RelationType = "DIAGNOSTIC_CASE_OF_VEHICLE",
                            FromId = c.CaseId,
                            ToId = c.VehicleId,
                            Status = ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN,
                            Reason = "Query returned case whose VehicleId does not match requested vehicle",
                            SourceEntity = "DiagnosticCases",
                            SourceField = "VehicleId"
                        });
                        warnings.Add(WarningSoftFkUnproven);
                    }
                }

                // Cases that only share plate/model without VehicleId are NOT loaded (would invent).
            }

            // Conflict: OS ClienteId vs vehicle ClienteId when both proven
            if (clienteIdProven.HasValue)
            {
                foreach (var os in provenOs.Where(o => o.ClienteId != Guid.Empty && o.ClienteId != clienteIdProven.Value))
                {
                    conflicts.Add($"OS {os.Numero} ClienteId={os.ClienteId:D} diverge de Veiculos.ClienteId={clienteIdProven:D}");
                    relations.Add(new ContextRelation
                    {
                        RelationType = "OS_CLIENT_VS_VEHICLE_CLIENT",
                        FromId = os.Id,
                        ToId = clienteIdProven,
                        Status = ContextProvenanceStatus.CONFLICT,
                        Reason = "OrdensServico.ClienteId != Veiculos.ClienteId",
                        SourceEntity = "OrdensServico",
                        SourceField = "ClienteId"
                    });
                }
            }

            _logger?.LogInfo($"Built vehicle context {vehicleId:D}: facts={facts.Count} os={provenOsIds.Count} cases={provenCaseIds.Count}", "VehicleContext");

            return new VehicleIntelligenceContext
            {
                RequestedVehicleId = vehicleId,
                Found = true,
                ClienteIdProven = clienteIdProven,
                Facts = facts,
                Relations = relations,
                ProvenWorkOrderIds = provenOsIds,
                ProvenDiagnosticCaseIds = provenCaseIds,
                MissingData = missing.Distinct().ToList(),
                Conflicts = conflicts,
                Warnings = warnings.Distinct().ToList(),
                BuiltAt = DateTimeOffset.Now
            };
        }
    }
}