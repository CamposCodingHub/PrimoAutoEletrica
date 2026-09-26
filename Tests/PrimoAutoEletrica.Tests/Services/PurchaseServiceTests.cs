using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class PurchaseServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly AppSessionService _session;
        private readonly AuditLogService _audit;
        private readonly PermissionService _permissionService;
        private readonly FinanceiroDatabaseService _financeiro;
        private readonly PurchaseService _purchaseService;
        private readonly Funcionario _adminUser;

        public PurchaseServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"PurchaseTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _session = new AppSessionService();
            _audit = new AuditLogService(_database, _logger, _session);

            _adminUser = new Funcionario
            {
                Id = 1,
                Nome = "Administrador Compras",
                Email = "compras@primoauto.com",
                PerfilAcesso = "Administrador"
            };
            _session.StartSession(_adminUser);
            _permissionService = new PermissionService(_adminUser, _logger);
            _financeiro = new FinanceiroDatabaseService(_database);

            _purchaseService = new PurchaseService(
                _repos.Purchases,
                _repos.Produtos,
                _financeiro,
                _permissionService,
                _logger,
                _session,
                _audit);
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
                // ignore
            }
        }

        [Fact]
        public async Task CriarRequisicao_ComItens_CalculaCustoEstimadoCentsV1()
        {
            var produto = new Produto
            {
                Codigo = "FUS-40A",
                Nome = "Fusível Lâmina Maxi 40A",
                QuantidadeEstoque = 2,
                QuantidadeMinima = 10,
                PrecoCompra = 5.50m, // 550 cents
                PrecoVenda = 12.00m,
                Ativo = true
            };
            _repos.Produtos.Inserir(produto);

            var itens = new List<PurchaseRequestItem>
            {
                new PurchaseRequestItem
                {
                    ProductId = produto.Id,
                    ProductCode = produto.Codigo,
                    ProductName = produto.Nome,
                    RequestedQuantity = 20,
                    EstimatedUnitCost = 5.50m // 550 cents
                }
            };

            var req = await _purchaseService.CriarRequisicaoAsync(
                1,
                "Almoxarife Teste",
                PurchasePriority.URGENT,
                PurchaseReason.LOW_STOCK,
                "Reposição urgente de fusíveis",
                itens);

            Assert.NotNull(req);
            Assert.StartsWith("REQ-", req.Number);
            Assert.Equal(PurchasePriority.URGENT, req.Priority);
            Assert.Equal(PurchaseRequestStatus.REQUESTED, req.Status);
            // 20 * 5.50 = 110.00 -> 11000 cents
            Assert.Equal(11000, req.TotalEstimatedCostCents);
            Assert.Equal(110.00m, req.TotalEstimatedCost);
        }

        [Fact]
        public async Task ObterSugestoesEstoqueMinimo_DetectaItensAbaixoDoMinimo()
        {
            var p1 = new Produto { Codigo = "CAB-10MM", Nome = "Cabo Bateria 10mm", QuantidadeEstoque = 3, QuantidadeMinima = 15, PrecoCompra = 15m, PrecoVenda = 35m, Ativo = true };
            var p2 = new Produto { Codigo = "REL-12V", Nome = "Relé Auxiliar 12V 40A", QuantidadeEstoque = 20, QuantidadeMinima = 10, PrecoCompra = 12m, PrecoVenda = 25m, Ativo = true };
            _repos.Produtos.Inserir(p1);
            _repos.Produtos.Inserir(p2);

            var sugestoes = await _purchaseService.ObterSugestoesEstoqueMinimoAsync();
            Assert.Contains(sugestoes, p => p.Codigo == "CAB-10MM");
            Assert.DoesNotContain(sugestoes, p => p.Codigo == "REL-12V");
        }

        [Fact]
        public async Task AprovarRequisicao_RequisicaoValida_AtualizaStatusParaApproved()
        {
            var req = await _purchaseService.CriarRequisicaoAsync(1, "Solicitante", PurchasePriority.HIGH, PurchaseReason.LOW_STOCK);

            var aprovado = await _purchaseService.AprovarRequisicaoAsync(req.PurchaseRequestId, 1, "Diretor Operacional");
            Assert.True(aprovado);

            var recuperada = await _purchaseService.ObterPorIdAsync(req.PurchaseRequestId);
            Assert.NotNull(recuperada);
            Assert.Equal(PurchaseRequestStatus.APPROVED, recuperada.Status);
            Assert.Equal(1, recuperada.ApprovedByUserId);
            Assert.NotNull(recuperada.ApprovedAt);
        }

        [Fact]
        public async Task AprovarRequisicao_JaAprovada_LancaInvalidOperationException()
        {
            var req = await _purchaseService.CriarRequisicaoAsync(1, "Solicitante", PurchasePriority.NORMAL, PurchaseReason.PREVENTIVE);
            await _purchaseService.AprovarRequisicaoAsync(req.PurchaseRequestId, 1, "Diretor");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _purchaseService.AprovarRequisicaoAsync(req.PurchaseRequestId, 1, "Outro Diretor"));

            Assert.Contains("já foi aprovada", ex.Message);
        }

        [Fact]
        public async Task CancelarRequisicao_ComMotivo_AtualizaParaCancelledEAudita()
        {
            var req = await _purchaseService.CriarRequisicaoAsync(1, "Solicitante", PurchasePriority.LOW, PurchaseReason.OTHER);

            var cancelado = await _purchaseService.CancelarRequisicaoAsync(req.PurchaseRequestId, 1, "Item encontrado em estoque secundário.");
            Assert.True(cancelado);

            var recuperada = await _purchaseService.ObterPorIdAsync(req.PurchaseRequestId);
            Assert.NotNull(recuperada);
            Assert.Equal(PurchaseRequestStatus.CANCELLED, recuperada.Status);
            Assert.Equal("Item encontrado em estoque secundário.", recuperada.CancellationReason);
        }

        [Fact]
        public async Task FormalizarPedido_RequisicaoAprovada_MudaParaOrdered()
        {
            var req = await _purchaseService.CriarRequisicaoAsync(1, "Solicitante", PurchasePriority.NORMAL, PurchaseReason.LOW_STOCK);
            await _purchaseService.AprovarRequisicaoAsync(req.PurchaseRequestId, 1, "Diretor");

            var formalizado = await _purchaseService.FormalizarPedidoAsync(req.PurchaseRequestId, null, "Distribuidora Elétrica ABC", 450.00m);
            Assert.True(formalizado);

            var recuperada = await _purchaseService.ObterPorIdAsync(req.PurchaseRequestId);
            Assert.NotNull(recuperada);
            Assert.Equal(PurchaseRequestStatus.ORDERED, recuperada.Status);
            Assert.Equal("Distribuidora Elétrica ABC", recuperada.SupplierName);
            Assert.Equal(45000, recuperada.TotalEstimatedCostCents);
        }

        [Fact]
        public async Task ReceberMercadoria_AtualizaEstoqueEIntegraComContasPagarCentsV1()
        {
            var prod = new Produto { Codigo = "DISJ-50A", Nome = "Disjuntor Resetável 50A", QuantidadeEstoque = 0, QuantidadeMinima = 5, PrecoCompra = 45m, PrecoVenda = 90m, Ativo = true };
            _repos.Produtos.Inserir(prod);

            var itens = new List<PurchaseRequestItem>
            {
                new PurchaseRequestItem
                {
                    ProductId = prod.Id,
                    ProductCode = prod.Codigo,
                    ProductName = prod.Nome,
                    RequestedQuantity = 10,
                    EstimatedUnitCost = 45.00m // 4500 cents
                }
            };

            var req = await _purchaseService.CriarRequisicaoAsync(1, "Solicitante", PurchasePriority.URGENT, PurchaseReason.OUT_OF_STOCK, "Reposição imediata", itens);
            await _purchaseService.AprovarRequisicaoAsync(req.PurchaseRequestId, 1, "Diretor");
            await _purchaseService.FormalizarPedidoAsync(req.PurchaseRequestId, null, "Fornecedor Teste", 450.00m);

            var recebido = await _purchaseService.ReceberMercadoriaAsync(req.PurchaseRequestId, 1, "NF-89210", 450.00m);
            Assert.True(recebido);

            // Verificar atualização de estoque físico
            var prodAtualizado = _repos.Produtos.ObterPorId(prod.Id);
            Assert.NotNull(prodAtualizado);
            Assert.Equal(10, prodAtualizado.QuantidadeEstoque);

            // Verificar status do pedido
            var reqAtualizada = await _purchaseService.ObterPorIdAsync(req.PurchaseRequestId);
            Assert.NotNull(reqAtualizada);
            Assert.Equal(PurchaseRequestStatus.RECEIVED, reqAtualizada.Status);
            Assert.Equal(45000, reqAtualizada.TotalActualCostCents);
            Assert.Equal("NF-89210", reqAtualizada.FiscalDocumentNumber);

            // Verificar integração financeira com Contas a Pagar
            using var conn = _database.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM ContasPagar WHERE Origem = 'Compras' AND ReferenciaExterna = @Ref;";
            var p = cmd.CreateParameter();
            p.ParameterName = "@Ref";
            p.Value = req.Number;
            cmd.Parameters.Add(p);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.True(count > 0);
        }
    }
}
