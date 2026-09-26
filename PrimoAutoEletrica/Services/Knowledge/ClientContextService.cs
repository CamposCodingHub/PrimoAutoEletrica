using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IClientContextService
    {
        Task<ClientIntelligenceContext> BuildAsync(Guid clienteId, CancellationToken ct = default);
    }

    /// <summary>
    /// C2.3 — client intelligence context. PERSONAL/SENSITIVE (CPF, contacts) are NOT exposed as facts.
    /// Only operational anchors: Id, display name (min), proven vehicle/OS ids via FK.
    /// Financial rollups are intentionally omitted (use Primox360 with finance permission instead).
    /// </summary>
    public sealed class ClientContextService : IClientContextService
    {
        public const string WarningNotFound = "CLIENT_NOT_FOUND";
        public const string WarningEmptyId = "CLIENT_ID_EMPTY";

        private readonly IClienteRepository _clientes;
        private readonly IOrdemServicoRepository _ordens;
        private readonly LoggerService? _logger;

        public ClientContextService(
            IClienteRepository clientes,
            IOrdemServicoRepository ordens,
            LoggerService? logger = null)
        {
            _clientes = clientes ?? throw new ArgumentNullException(nameof(clientes));
            _ordens = ordens ?? throw new ArgumentNullException(nameof(ordens));
            _logger = logger;
        }

        public Task<ClientIntelligenceContext> BuildAsync(Guid clienteId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (clienteId == Guid.Empty)
            {
                return Task.FromResult(new ClientIntelligenceContext
                {
                    RequestedClienteId = clienteId,
                    Found = false,
                    Warnings = new[] { WarningEmptyId },
                    MissingData = new[] { "ClienteId válido" },
                    BuiltAt = DateTimeOffset.Now
                });
            }

            var cliente = _clientes.ObterPorId(clienteId);
            if (cliente == null)
            {
                return Task.FromResult(new ClientIntelligenceContext
                {
                    RequestedClienteId = clienteId,
                    Found = false,
                    Warnings = new[] { WarningNotFound },
                    MissingData = new[] { $"Cliente {clienteId} não encontrado em Clientes" },
                    BuiltAt = DateTimeOffset.Now
                });
            }

            var facts = new List<ContextFact>();
            var relations = new List<ContextRelation>();
            var missing = new List<string>();

            // Minimum-necessary: Id + first-name-ish display only (no CPF/phone/address).
            var displayName = string.IsNullOrWhiteSpace(cliente.Nome) ? null : cliente.Nome.Trim();
            facts.Add(new ContextFact
            {
                FactKey = "client.id",
                DisplayLabel = "ClienteId",
                Value = cliente.Id.ToString("D"),
                Status = ContextProvenanceStatus.PROVEN,
                SourceEntity = "Clientes",
                SourceEntityId = cliente.Id.ToString("N"),
                SourceField = "Id",
                OriginDescription = $"Clientes.Id={cliente.Id:D}",
                Classification = "OPERATIONAL"
            });
            facts.Add(new ContextFact
            {
                FactKey = "client.display_name",
                DisplayLabel = "Nome (mínimo necessário)",
                Value = displayName,
                Status = displayName == null ? ContextProvenanceStatus.MISSING : ContextProvenanceStatus.PROVEN,
                SourceEntity = "Clientes",
                SourceEntityId = cliente.Id.ToString("N"),
                SourceField = "Nome",
                OriginDescription = $"Clientes.Nome WHERE Id={cliente.Id:D} (PII minimized; CPF/contacts omitted)",
                Classification = "PERSONAL"
            });
            if (displayName == null)
            {
                missing.Add("Nome do cliente");
            }

            // Explicit omission of SENSITIVE/FINANCIAL as MISSING-for-Assist (not a data bug).
            facts.Add(new ContextFact
            {
                FactKey = "client.cpf_omitted",
                DisplayLabel = "CPF/CNPJ",
                Value = null,
                Status = ContextProvenanceStatus.MISSING,
                SourceEntity = "Clientes",
                SourceEntityId = cliente.Id.ToString("N"),
                SourceField = "CPF",
                OriginDescription = "Omitted by C2 classification (SENSITIVE) — never enters Assist context",
                Classification = "SENSITIVE"
            });

            var veiculos = _clientes.ObterVeiculosPorClienteId(clienteId);
            var vehicleIds = veiculos.Select(v => v.Id).ToList();
            foreach (var v in veiculos)
            {
                relations.Add(new ContextRelation
                {
                    RelationType = "VEHICLE_OF_CLIENT",
                    FromId = v.Id,
                    ToId = clienteId,
                    Status = ContextProvenanceStatus.PROVEN,
                    Reason = "Veiculos.ClienteId == Clientes.Id",
                    SourceEntity = "Veiculos",
                    SourceField = "ClienteId"
                });
            }

            if (vehicleIds.Count == 0)
            {
                missing.Add("Veículos com ClienteId comprovado");
            }

            var ordens = _ordens.ObterPorClienteId(clienteId, incluirInativas: true)
                .OrderByDescending(o => o.DataAbertura)
                .ToList();
            var osIds = ordens.Select(o => o.Id).ToList();
            foreach (var os in ordens)
            {
                relations.Add(new ContextRelation
                {
                    RelationType = "OS_OF_CLIENT",
                    FromId = os.Id,
                    ToId = clienteId,
                    Status = ContextProvenanceStatus.PROVEN,
                    Reason = "OrdensServico.ClienteId == Clientes.Id",
                    SourceEntity = "OrdensServico",
                    SourceField = "ClienteId"
                });
            }

            if (osIds.Count == 0)
            {
                missing.Add("Histórico de OS com ClienteId comprovado");
            }
            else
            {
                var last = ordens[0];
                facts.Add(new ContextFact
                {
                    FactKey = "client.last_os_number",
                    DisplayLabel = "Última OS",
                    Value = last.Numero,
                    Status = ContextProvenanceStatus.PROVEN,
                    SourceEntity = "OrdensServico",
                    SourceEntityId = last.Id.ToString("N"),
                    SourceField = "Numero",
                    OriginDescription = $"OrdensServico.Numero WHERE ClienteId={clienteId:D} ORDER BY DataAbertura DESC",
                    Classification = "OPERATIONAL"
                });
            }

            facts.Add(new ContextFact
            {
                FactKey = "client.vehicle_count",
                DisplayLabel = "Qtd veículos (FK)",
                Value = vehicleIds.Count.ToString(),
                Status = ContextProvenanceStatus.PROVEN,
                SourceEntity = "Veiculos",
                SourceEntityId = null,
                SourceField = "ClienteId",
                OriginDescription = $"COUNT Veiculos WHERE ClienteId={clienteId:D}",
                Classification = "OPERATIONAL"
            });
            facts.Add(new ContextFact
            {
                FactKey = "client.os_count",
                DisplayLabel = "Qtd OS (FK)",
                Value = osIds.Count.ToString(),
                Status = ContextProvenanceStatus.PROVEN,
                SourceEntity = "OrdensServico",
                SourceEntityId = null,
                SourceField = "ClienteId",
                OriginDescription = $"COUNT OrdensServico WHERE ClienteId={clienteId:D}",
                Classification = "OPERATIONAL"
            });

            _logger?.LogInfo($"Built client context {clienteId:D}: vehicles={vehicleIds.Count} os={osIds.Count}", "ClientContext");

            return Task.FromResult(new ClientIntelligenceContext
            {
                RequestedClienteId = clienteId,
                Found = true,
                Facts = facts,
                Relations = relations,
                ProvenVehicleIds = vehicleIds,
                ProvenWorkOrderIds = osIds,
                MissingData = missing,
                Conflicts = Array.Empty<string>(),
                Warnings = Array.Empty<string>(),
                BuiltAt = DateTimeOffset.Now
            });
        }
    }
}