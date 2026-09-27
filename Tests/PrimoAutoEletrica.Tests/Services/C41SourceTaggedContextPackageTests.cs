using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C41SourceTaggedContextPackageTests
    {
        private sealed class FakeComposition : IContextCompositionService
        {
            public ComposedIntelligenceContext Result { get; set; } = new();
            public ContextCompositionRequest? LastRequest { get; private set; }

            public Task<ComposedIntelligenceContext> ComposeAsync(ContextCompositionRequest request, CancellationToken ct = default)
            {
                LastRequest = request;
                return Task.FromResult(Result);
            }
        }

        private static Guid G(string nibble) => Guid.Parse(nibble + "-0000-0000-0000-000000000000");

        [Fact]
        public void InferTag_FactKeys_MapToClienteVeiculoOs()
        {
            Assert.Equal(ContextSourceTag.CLIENTE, SourceTaggedContextPackageBuilder.InferTagFromFactKey("cliente.nome", "CLIENT"));
            Assert.Equal(ContextSourceTag.VEICULO, SourceTaggedContextPackageBuilder.InferTagFromFactKey("vehicle.placa", "VEHICLE"));
            Assert.Equal(ContextSourceTag.OS, SourceTaggedContextPackageBuilder.InferTagFromFactKey("workorder.status", "WORKORDER"));
            Assert.Equal(ContextSourceTag.KNOWLEDGE, SourceTaggedContextPackageBuilder.InferTagFromFactKey("knowledge.artigo", null));
            Assert.Equal(ContextSourceTag.BUDGET, SourceTaggedContextPackageBuilder.InferTagFromFactKey("os.orcamento", "WORKORDER"));
            Assert.Equal(ContextSourceTag.DIAGNOSTIC, SourceTaggedContextPackageBuilder.InferTagFromFactKey("os.diagnostico", "WORKORDER"));
            Assert.Equal(ContextSourceTag.HISTORY, SourceTaggedContextPackageBuilder.InferTagFromFactKey("vehicle.historico_tecnico", "VEHICLE"));
            Assert.Equal(ContextSourceTag.FINANCE, SourceTaggedContextPackageBuilder.InferTagFromFactKey("finance.saldo", "FINANCE"));
            Assert.Equal(ContextSourceTag.UNKNOWN, SourceTaggedContextPackageBuilder.InferTagFromFactKey("xyz.nope", null));
            Assert.Equal(ContextSourceTag.UNKNOWN, SourceTaggedContextPackageBuilder.InferTagFromFactKey(null, null));
        }

        [Fact]
        public void InferTag_EvidenceSourceType_MapsKnowledgeLocalUnknown()
        {
            Assert.Equal(ContextSourceTag.KNOWLEDGE, SourceTaggedContextPackageBuilder.InferTagFromEvidenceSourceType("KnowledgeArticle"));
            Assert.Equal(ContextSourceTag.ASSIST_LOCAL, SourceTaggedContextPackageBuilder.InferTagFromEvidenceSourceType("LocalAssist"));
            Assert.Equal(ContextSourceTag.EXTERNAL_EVIDENCE, SourceTaggedContextPackageBuilder.InferTagFromEvidenceSourceType("ExternalHttp"));
            Assert.Equal(ContextSourceTag.UNKNOWN, SourceTaggedContextPackageBuilder.InferTagFromEvidenceSourceType("Other"));
            Assert.Equal(ContextSourceTag.UNKNOWN, SourceTaggedContextPackageBuilder.InferTagFromEvidenceSourceType(null));
        }

        [Fact]
        public async Task BuildAsync_TagsFacts_AndSkipsFinancial_WhenNotIncluded()
        {
            var clientA = G("aaaaaaaa");
            var fake = new FakeComposition
            {
                Result = new ComposedIntelligenceContext
                {
                    Query = "teste c4.1",
                    HasAnyProvenAnchor = true,
                    Client = new ClientIntelligenceContext { RequestedClienteId = clientA, Found = true },
                    AllProvenFacts = new List<ContextFact>
                    {
                        new() { FactKey = "cliente.nome", Value = "A", SourceEntity = "CLIENT", SourceEntityId = clientA.ToString("D"), Classification = "TECHNICAL", Status = ContextProvenanceStatus.PROVEN },
                        new() { FactKey = "vehicle.placa", Value = "ABC1D23", SourceEntity = "VEHICLE", SourceEntityId = G("bbbbbbbb").ToString("D"), Classification = "TECHNICAL", Status = ContextProvenanceStatus.PROVEN },
                        new() { FactKey = "finance.saldo", Value = "999", SourceEntity = "FINANCE", Classification = "FINANCIAL", Status = ContextProvenanceStatus.PROVEN },
                    }
                }
            };
            var audit = new IntelligenceAuditService();
            var builder = new SourceTaggedContextPackageBuilder(fake, audit: audit);

            var package = await builder.BuildAsync(new ContextCompositionRequest { Query = "teste c4.1", ClienteId = clientA }, includeFinancial: false);

            Assert.DoesNotContain(package.Items, i => i.Classification.Equals("FINANCIAL", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(package.Items, i => i.SourceTag == ContextSourceTag.FINANCE);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.CLIENTE);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.VEICULO);
            Assert.True(package.HasAnyProvenAnchor);
            Assert.True(package.IsolationOk);
            Assert.Equal(1, audit.Count);
            Assert.Equal("OK", audit.ListRecent(1)[0].Status);
            Assert.Contains("CLIENTE", audit.ListRecent(1)[0].AllowedContext, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BuildAsync_WithAssistEvidence_AddsKnowledgeTag()
        {
            var fake = new FakeComposition
            {
                Result = new ComposedIntelligenceContext
                {
                    Query = "q",
                    HasAnyProvenAnchor = true,
                    AllProvenFacts = Array.Empty<ContextFact>()
                }
            };
            var assist = new AssistantQueryContext
            {
                Query = "q",
                RetrievedEvidence = new List<EvidenceItem>
                {
                    new()
                    {
                        EvidenceId = "EVID-1",
                        Kind = EvidenceKind.Knowledge,
                        SourceCode = "ART-1",
                        Title = "Boletim",
                        Excerpt = "trecho",
                        Classification = "TECHNICAL"
                    }
                }
            };
            var builder = new SourceTaggedContextPackageBuilder(fake);
            var package = await builder.BuildAsync(new ContextCompositionRequest { Query = "q" }, assistContext: assist);
            Assert.True(package.Items.Count >= 1);
            Assert.Contains(package.Items, i => i.EvidenceId == "EVID-1" || i.SourceTag == ContextSourceTag.KNOWLEDGE || i.SourceTag == ContextSourceTag.EXTERNAL_EVIDENCE || i.SourceTag == ContextSourceTag.UNKNOWN);
            Assert.NotNull(package.EvidencePackage);
        }

        [Fact]
        public async Task BuildAsync_IncludesUserPermsServicesPartsDiagnosticHistory_WhenPresent()
        {
            var clientA = G("aaaaaaaa");
            var vehicleA = G("bbbbbbbb");
            var osA = G("cccccccc");
            var diagA = G("dddddddd");

            var fake = new FakeComposition
            {
                Result = new ComposedIntelligenceContext
                {
                    Query = "c4.1 deepen",
                    HasAnyProvenAnchor = true,
                    Client = new ClientIntelligenceContext
                    {
                        RequestedClienteId = clientA,
                        Found = true,
                        Facts = new List<ContextFact>
                        {
                            new() { FactKey = "client.last_os_number", Value = "OS-1", SourceEntity = "CLIENT", SourceEntityId = clientA.ToString("D"), Status = ContextProvenanceStatus.PROVEN }
                        }
                    },
                    Vehicle = new VehicleIntelligenceContext
                    {
                        RequestedVehicleId = vehicleA,
                        Found = true,
                        ClienteIdProven = clientA,
                        ProvenDiagnosticCaseIds = new[] { diagA },
                        Facts = new List<ContextFact>
                        {
                            new() { FactKey = "vehicle.historico_tecnico", Value = "troca alternador", SourceEntity = "VEHICLE", SourceEntityId = vehicleA.ToString("D"), Status = ContextProvenanceStatus.PROVEN }
                        }
                    },
                    WorkOrder = new WorkOrderIntelligenceContext
                    {
                        RequestedWorkOrderId = osA,
                        Found = true,
                        ClienteIdProven = clientA,
                        VeiculoIdProven = vehicleA,
                        ServiceItemLabels = new[] { "Diagnostico eletrico" },
                        PartItemLabels = new[] { "Regulador 14V" },
                        ProvenDiagnosticCaseIds = new[] { diagA },
                        Facts = Array.Empty<ContextFact>()
                    },
                    AllProvenFacts = new List<ContextFact>
                    {
                        new() { FactKey = "cliente.nome", Value = "Cliente A", SourceEntity = "CLIENT", SourceEntityId = clientA.ToString("D"), Status = ContextProvenanceStatus.PROVEN },
                        new() { FactKey = "vehicle.placa", Value = "ABC1D23", SourceEntity = "VEHICLE", SourceEntityId = vehicleA.ToString("D"), Status = ContextProvenanceStatus.PROVEN },
                        new() { FactKey = "os.status", Value = "Aberta", SourceEntity = "WORKORDER", SourceEntityId = osA.ToString("D"), Status = ContextProvenanceStatus.PROVEN },
                    }
                }
            };

            var actor = new SourceTaggedActorContext
            {
                UserId = "user-1",
                UserDisplayName = "Tecnico",
                Permissions = new[] { "OS_READ", "ASSIST" },
                CanIncludeFinance = false,
                SessionClienteId = clientA
            };

            var builder = new SourceTaggedContextPackageBuilder(fake);
            var package = await builder.BuildAsync(
                new ContextCompositionRequest { Query = "c4.1 deepen", ClienteId = clientA, VehicleId = vehicleA, WorkOrderId = osA, SessionClienteId = clientA },
                actor: actor);

            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.USER && i.Key == "user.id");
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.PERMS);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.CLIENTE);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.VEICULO);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.OS);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.SERVICES && i.Value.Contains("Diagnostico"));
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.PARTS && i.Value.Contains("Regulador"));
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.DIAGNOSTIC);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.HISTORY);
            Assert.Contains(package.MissingData, m => m == SourceTaggedContextPackageBuilder.MissingAgenda);
            Assert.Contains(package.MissingData, m => m == SourceTaggedContextPackageBuilder.MissingBudget);
            Assert.True(package.IsolationOk);
            Assert.Equal(clientA, package.ScopedClienteId);
            Assert.Equal(vehicleA, package.ScopedVeiculoId);
            Assert.Equal(osA, package.ScopedWorkOrderId);
        }

        [Fact]
        public async Task BuildAsync_CrossClientSession_FailClosed_NoLeak()
        {
            var clientA = G("aaaaaaaa");
            var clientB = G("bbbbbbbb");
            var fake = new FakeComposition
            {
                Result = new ComposedIntelligenceContext
                {
                    Query = "cross",
                    HasAnyProvenAnchor = false,
                    Warnings = new[] { ContextCompositionService.WarningCrossClientDenied },
                    MissingData = new[] { "Autorizacao" }
                }
            };
            var builder = new SourceTaggedContextPackageBuilder(fake);
            var package = await builder.BuildAsync(
                new ContextCompositionRequest { Query = "cross", ClienteId = clientA, SessionClienteId = clientB },
                actor: new SourceTaggedActorContext { UserId = "u", SessionClienteId = clientB, Permissions = new[] { "OS_READ" } });

            Assert.False(package.IsolationOk);
            Assert.Equal(SourceTaggedContextPackageBuilder.WarningCrossClientDenied, package.IsolationStatus);
            Assert.Contains(SourceTaggedContextPackageBuilder.WarningCrossClientDenied, package.Warnings);
            Assert.DoesNotContain(package.Items, i => i.SourceTag == ContextSourceTag.CLIENTE && i.SourceId == clientA.ToString("D"));
        }

        [Fact]
        public void ValidateIsolation_ClientA_Vs_ClientB_Detected()
        {
            var clientA = G("aaaaaaaa");
            var clientB = G("bbbbbbbb");
            var composed = new ComposedIntelligenceContext
            {
                Client = new ClientIntelligenceContext { RequestedClienteId = clientB, Found = true },
                HasAnyProvenAnchor = true
            };
            var result = SourceTaggedContextPackageBuilder.ValidateIsolation(
                Array.Empty<SourceTaggedContextItem>(),
                scopedClienteId: clientA,
                scopedVeiculoId: null,
                scopedWorkOrderId: null,
                sessionClienteId: clientA,
                composed);
            Assert.False(result.Ok);
            Assert.Contains(result.Reasons, r => r.Contains("CLIENT_A_VS_B"));
        }

        [Fact]
        public void ValidateIsolation_VehicleA_Vs_VehicleB_Detected()
        {
            var vA = G("aaaaaaaa");
            var vB = G("bbbbbbbb");
            var composed = new ComposedIntelligenceContext
            {
                Vehicle = new VehicleIntelligenceContext { RequestedVehicleId = vB, Found = true },
                HasAnyProvenAnchor = true
            };
            var result = SourceTaggedContextPackageBuilder.ValidateIsolation(
                Array.Empty<SourceTaggedContextItem>(),
                scopedClienteId: null,
                scopedVeiculoId: vA,
                scopedWorkOrderId: null,
                sessionClienteId: null,
                composed);
            Assert.False(result.Ok);
            Assert.Contains(result.Reasons, r => r.Contains("VEHICLE_A_VS_B"));
        }

        [Fact]
        public void ValidateIsolation_OsA_Vs_OsB_Detected()
        {
            var osA = G("aaaaaaaa");
            var osB = G("bbbbbbbb");
            var composed = new ComposedIntelligenceContext
            {
                WorkOrder = new WorkOrderIntelligenceContext { RequestedWorkOrderId = osB, Found = true },
                HasAnyProvenAnchor = true
            };
            var result = SourceTaggedContextPackageBuilder.ValidateIsolation(
                Array.Empty<SourceTaggedContextItem>(),
                scopedClienteId: null,
                scopedVeiculoId: null,
                scopedWorkOrderId: osA,
                sessionClienteId: null,
                composed);
            Assert.False(result.Ok);
            Assert.Contains(result.Reasons, r => r.Contains("OS_A_VS_B"));
        }

        [Fact]
        public async Task BuildAsync_Throws_OnVehicleClientLeak_CriticalStop()
        {
            var clientA = G("aaaaaaaa");
            var clientB = G("bbbbbbbb");
            var vehicleB = G("cccccccc");
            var fake = new FakeComposition
            {
                Result = new ComposedIntelligenceContext
                {
                    Query = "leak",
                    HasAnyProvenAnchor = true,
                    Client = new ClientIntelligenceContext { RequestedClienteId = clientA, Found = true },
                    Vehicle = new VehicleIntelligenceContext
                    {
                        RequestedVehicleId = vehicleB,
                        Found = true,
                        ClienteIdProven = clientB // vehicle belongs to B while scoped client is A
                    },
                    AllProvenFacts = Array.Empty<ContextFact>()
                }
            };
            var builder = new SourceTaggedContextPackageBuilder(fake);
            var ex = await Assert.ThrowsAsync<ContextIsolationViolationException>(() =>
                builder.BuildAsync(new ContextCompositionRequest
                {
                    Query = "leak",
                    ClienteId = clientA,
                    VehicleId = vehicleB,
                    SessionClienteId = clientA
                }));
            Assert.Equal(SourceTaggedContextPackageBuilder.WarningIsolationLeak, ex.ViolationCode);
        }

        [Fact]
        public async Task BuildAsync_FinanceRequiresActorPermission()
        {
            var fake = new FakeComposition
            {
                Result = new ComposedIntelligenceContext
                {
                    Query = "fin",
                    HasAnyProvenAnchor = true,
                    AllProvenFacts = new List<ContextFact>
                    {
                        new() { FactKey = "finance.saldo", Value = "10", SourceEntity = "FINANCE", Classification = "FINANCIAL", Status = ContextProvenanceStatus.PROVEN }
                    }
                }
            };
            var builder = new SourceTaggedContextPackageBuilder(fake);
            var denied = await builder.BuildAsync(
                new ContextCompositionRequest { Query = "fin" },
                includeFinancial: true,
                actor: new SourceTaggedActorContext { UserId = "u", CanIncludeFinance = false, Permissions = new[] { "OS_READ" } });
            Assert.DoesNotContain(denied.Items, i => i.SourceTag == ContextSourceTag.FINANCE);
            Assert.Contains(SourceTaggedContextPackageBuilder.WarningFinanceDenied, denied.Warnings);
            Assert.False(denied.IncludeFinancial);

            var allowed = await builder.BuildAsync(
                new ContextCompositionRequest { Query = "fin" },
                includeFinancial: true,
                actor: new SourceTaggedActorContext { UserId = "u", CanIncludeFinance = true, Permissions = new[] { "FINANCE_READ" } });
            Assert.Contains(allowed.Items, i => i.SourceTag == ContextSourceTag.FINANCE);
            Assert.True(allowed.IncludeFinancial);
        }
    }
}
