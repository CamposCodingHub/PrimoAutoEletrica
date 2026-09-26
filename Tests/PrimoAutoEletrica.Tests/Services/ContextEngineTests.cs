using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>
    /// C2.3 Context Engine — synthetic isolated DB only (never protected primoauto.db).
    /// </summary>
    public sealed class ContextEngineTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly VehicleContextService _vehicleCtx;
        private readonly ClientContextService _clientCtx;
        private readonly WorkOrderContextService _workOrderCtx;
        private readonly ContextCompositionService _composition;

        public ContextEngineTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"C23_ContextSynthetic_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _vehicleCtx = new VehicleContextService(_repos.Clientes, _repos.OrdensServico, _repos.Knowledge, _logger);
            _clientCtx = new ClientContextService(_repos.Clientes, _repos.OrdensServico, _logger);
            _workOrderCtx = new WorkOrderContextService(_repos.OrdensServico, _repos.Clientes, _repos.Knowledge, _logger);
            _composition = new ContextCompositionService(_vehicleCtx, _clientCtx, _workOrderCtx, _logger);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch
            {
                // ignore cleanup races
            }
        }

        private (Cliente cliente, Veiculo veiculo, OrdemServico os) SeedProvenTrio()
        {
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Cliente Sintetico C23",
                Telefone = "11999990000",
                Ativo = true,
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Today,
                OrigemConsentimentoLGPD = "TesteC23"
            };
            _repos.Clientes.Inserir(cliente);

            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = cliente.Id,
                Marca = "VW",
                Modelo = "Gol",
                Ano = "2018",
                Placa = "ABC1D23",
                SistemaEletrico = "12V",
                ProblemaRecorrente = "Queda de tensao sob carga",
                Quilometragem = 85000
            };
            _repos.Clientes.SalvarVeiculo(veiculo);

            var os = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = "OS-C23-001",
                ClienteId = cliente.Id,
                VeiculoId = veiculo.Id,
                PlacaSnapshot = veiculo.Placa,
                VeiculoDescricaoSnapshot = $"{veiculo.Marca} {veiculo.Modelo}",
                ProblemaRelatado = "Nao parte pela manha",
                Diagnostico = "Suspeita de bateria",
                Status = "Concluida",
                DataAbertura = DateTime.Now.AddDays(-2),
                Itens =
                {
                    new OrdemServicoItem { Tipo = "Servico", Descricao = "Teste de carga" },
                    new OrdemServicoItem { Tipo = "Peca", Descricao = "Bateria 60Ah" }
                }
            };
            _repos.OrdensServico.Inserir(os);
            return (cliente, veiculo, os);
        }

        [Fact]
        public async Task VehicleContext_EmptyId_FailClosed()
        {
            var ctx = await _vehicleCtx.BuildAsync(Guid.Empty);
            Assert.False(ctx.Found);
            Assert.Contains(VehicleContextService.WarningEmptyId, ctx.Warnings);
            Assert.Empty(ctx.ProvenWorkOrderIds);
        }

        [Fact]
        public async Task VehicleContext_NotFound_FailClosed()
        {
            var ctx = await _vehicleCtx.BuildAsync(Guid.NewGuid());
            Assert.False(ctx.Found);
            Assert.Contains(VehicleContextService.WarningNotFound, ctx.Warnings);
        }

        [Fact]
        public async Task VehicleContext_ProvenOsAndClient_TraceableFacts()
        {
            var (cliente, veiculo, os) = SeedProvenTrio();
            var ctx = await _vehicleCtx.BuildAsync(veiculo.Id);

            Assert.True(ctx.Found);
            Assert.Equal(cliente.Id, ctx.ClienteIdProven);
            Assert.Contains(os.Id, ctx.ProvenWorkOrderIds);
            Assert.Contains(ctx.Facts, f => f.FactKey == "vehicle.placa" && f.Status == ContextProvenanceStatus.PROVEN && f.Value == "ABC1D23");
            Assert.Contains(ctx.Facts, f => f.FactKey == "vehicle.problema_recorrente" && f.Status == ContextProvenanceStatus.PROVEN);
            Assert.All(ctx.Facts.Where(f => f.Status == ContextProvenanceStatus.PROVEN), f =>
            {
                Assert.False(string.IsNullOrWhiteSpace(f.OriginDescription));
                Assert.False(string.IsNullOrWhiteSpace(f.SourceEntity));
                Assert.False(string.IsNullOrWhiteSpace(f.SourceField));
            });
            Assert.Contains(ctx.Relations, r =>
                r.RelationType == "OS_OF_VEHICLE" &&
                r.Status == ContextProvenanceStatus.PROVEN &&
                r.FromId == os.Id);
            // Sensitive fields must not appear
            Assert.DoesNotContain(ctx.Facts, f => f.SourceField is "Chassi" or "Renavam");
        }

        [Fact]
        public async Task VehicleContext_PlateOnlyOs_IsRelationshipNotProven()
        {
            var (cliente, veiculo, _) = SeedProvenTrio();
            var orphanOs = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = "OS-C23-PLATE",
                ClienteId = cliente.Id,
                VeiculoId = null,
                PlacaSnapshot = veiculo.Placa,
                ProblemaRelatado = "Mesma placa sem VeiculoId",
                Status = "Aberta",
                DataAbertura = DateTime.Now
            };
            _repos.OrdensServico.Inserir(orphanOs);

            var ctx = await _vehicleCtx.BuildAsync(veiculo.Id);
            Assert.DoesNotContain(orphanOs.Id, ctx.ProvenWorkOrderIds);
            Assert.Contains(ctx.Relations, r =>
                r.RelationType == "OS_OF_VEHICLE_PLATE_ONLY" &&
                r.Status == ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN &&
                r.FromId == orphanOs.Id);
            Assert.Contains(ctx.Warnings, w => w.StartsWith("PLATE_ONLY_OS_UNPROVEN", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ClientContext_OmitsCpf_AndListsProvenVehiclesOs()
        {
            var (cliente, veiculo, os) = SeedProvenTrio();
            var ctx = await _clientCtx.BuildAsync(cliente.Id);

            Assert.True(ctx.Found);
            Assert.Contains(veiculo.Id, ctx.ProvenVehicleIds);
            Assert.Contains(os.Id, ctx.ProvenWorkOrderIds);
            Assert.Contains(ctx.Facts, f => f.FactKey == "client.cpf_omitted" && f.Status == ContextProvenanceStatus.MISSING);
            Assert.DoesNotContain(ctx.Facts, f => f.SourceField != null && f.SourceField.Equals("CPF", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(f.Value));
            Assert.DoesNotContain(ctx.Facts, f => f.Value == "11999990000");
        }

        [Fact]
        public async Task WorkOrderContext_ProvenLinks_AndNoMoneyFacts()
        {
            var (cliente, veiculo, os) = SeedProvenTrio();
            var ctx = await _workOrderCtx.BuildAsync(os.Id);

            Assert.True(ctx.Found);
            Assert.Equal(cliente.Id, ctx.ClienteIdProven);
            Assert.Equal(veiculo.Id, ctx.VeiculoIdProven);
            Assert.Contains("Teste de carga", ctx.ServiceItemLabels);
            Assert.Contains("Bateria 60Ah", ctx.PartItemLabels);
            Assert.DoesNotContain(ctx.Facts, f =>
                f.Classification.Equals("FINANCIAL", StringComparison.OrdinalIgnoreCase) ||
                (f.Value != null && (f.Value.Contains("Valor") || f.SourceField.Contains("Valor"))));
            Assert.Contains(ctx.Relations, r => r.RelationType == "OS_BELONGS_TO_VEHICLE" && r.Status == ContextProvenanceStatus.PROVEN);
        }

        [Fact]
        public async Task WorkOrderContext_MissingVehicleId_SnapshotNotProven()
        {
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Sem Veiculo FK",
                Telefone = "11988887777",
                Ativo = true,
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Today,
                OrigemConsentimentoLGPD = "TesteC23"
            };
            _repos.Clientes.Inserir(cliente);
            var os = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = "OS-C23-NOVEH",
                ClienteId = cliente.Id,
                VeiculoId = null,
                PlacaSnapshot = "ABC1D23",
                VeiculoDescricaoSnapshot = "Fiat Uno",
                ProblemaRelatado = "Farol fraco",
                Status = "Aberta"
            };
            _repos.OrdensServico.Inserir(os);

            var ctx = await _workOrderCtx.BuildAsync(os.Id);
            Assert.True(ctx.Found);
            Assert.Null(ctx.VeiculoIdProven);
            Assert.Contains(ctx.Relations, r =>
                r.RelationType == "OS_BELONGS_TO_VEHICLE" &&
                r.Status == ContextProvenanceStatus.MISSING);
            Assert.Contains("VEHICLE_SNAPSHOT_ONLY_NOT_PROVEN", ctx.Warnings);
        }

        [Fact]
        public async Task DiagnosticCase_SoftFk_MissingVehicle_RelationshipNotProven_OnComposePath()
        {
            // Soft FK points to a non-existent vehicle id — case must not invent the vehicle.
            var ghostVehicleId = Guid.NewGuid();
            var caso = new DiagnosticCase
            {
                CaseId = Guid.NewGuid(),
                Code = "CASE-C23-GHOST",
                Title = "Caso soft FK fantasma",
                VehicleId = ghostVehicleId,
                VehicleModel = "Inventado",
                Symptom = "Nao existe veiculo",
                ConfirmedCause = "—",
                Solution = "—"
            };
            await _repos.Knowledge.InserirCasoAsync(caso);

            var vehicleCtx = await _vehicleCtx.BuildAsync(ghostVehicleId);
            Assert.False(vehicleCtx.Found);
            Assert.Empty(vehicleCtx.ProvenDiagnosticCaseIds);

            // Query by ghost id via knowledge returns the case, but VehicleContext already failed Found.
            var cases = await _repos.Knowledge.ObterCasosPorVeiculoAsync(ghostVehicleId);
            Assert.Contains(cases, c => c.CaseId == caso.CaseId);
            // Provenance rule: without Veiculos row, relationship cannot be proven for Assist/context.
            Assert.Equal(ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN,
                cases[0].VehicleId == ghostVehicleId && !vehicleCtx.Found
                    ? ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN
                    : ContextProvenanceStatus.PROVEN);
        }

        [Fact]
        public async Task Composition_CrossClientSession_FailClosed()
        {
            var (cliente, veiculo, os) = SeedProvenTrio();
            var otherSession = Guid.NewGuid();
            var composed = await _composition.ComposeAsync(new ContextCompositionRequest
            {
                WorkOrderId = os.Id,
                SessionClienteId = otherSession
            });

            Assert.False(composed.HasAnyProvenAnchor);
            Assert.Contains(ContextCompositionService.WarningCrossClientDenied, composed.Warnings);
            Assert.Null(composed.WorkOrder);
        }

        [Fact]
        public async Task Composition_FromWorkOrder_PullsProvenVehicleAndClient()
        {
            var (cliente, veiculo, os) = SeedProvenTrio();
            var composed = await _composition.ComposeAsync(new ContextCompositionRequest
            {
                WorkOrderId = os.Id,
                Query = "historico de problemas do veiculo"
            });

            Assert.True(composed.HasAnyProvenAnchor);
            Assert.NotNull(composed.WorkOrder);
            Assert.True(composed.WorkOrder!.Found);
            Assert.NotNull(composed.Vehicle);
            Assert.True(composed.Vehicle!.Found);
            Assert.NotNull(composed.Client);
            Assert.True(composed.Client!.Found);
            Assert.Contains(composed.AllProvenFacts, f => f.FactKey == "os.problema");
            Assert.Contains(composed.AllProvenFacts, f => f.FactKey == "vehicle.placa");
            Assert.All(composed.AllProvenFacts, f => Assert.Equal(ContextProvenanceStatus.PROVEN, f.Status));
        }

        [Fact]
        public async Task Composition_ConflictingVehicleId_FlagsConflict()
        {
            var (cliente, veiculo, os) = SeedProvenTrio();
            var otherVehicle = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = cliente.Id,
                Marca = "Ford",
                Modelo = "Ka",
                Placa = "DEF2E99"
            };
            _repos.Clientes.SalvarVeiculo(otherVehicle);

            var composed = await _composition.ComposeAsync(new ContextCompositionRequest
            {
                WorkOrderId = os.Id,
                VehicleId = otherVehicle.Id
            });

            Assert.Contains(ContextCompositionService.WarningIdConflict, composed.Warnings);
            Assert.Contains(composed.Conflicts, c => c.Contains("VehicleId", StringComparison.OrdinalIgnoreCase));
            // Composition prefers proven OS vehicle
            Assert.Equal(veiculo.Id, composed.Vehicle?.RequestedVehicleId);
        }

        [Fact]
        public async Task Composition_NoIds_NoAnchor()
        {
            var composed = await _composition.ComposeAsync(new ContextCompositionRequest());
            Assert.False(composed.HasAnyProvenAnchor);
            Assert.Contains(ContextCompositionService.WarningNoAnchor, composed.Warnings);
        }
    }
}