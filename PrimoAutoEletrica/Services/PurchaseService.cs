using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services
{
    public sealed class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IProdutoRepository _produtoRepository;
        private readonly FinanceiroDatabaseService _financeiroService;
        private readonly PermissionService _permissionService;
        private readonly LoggerService _logger;
        private readonly AppSessionService _sessionService;
        private readonly AuditLogService _auditLogService;

        public PurchaseService(
            IPurchaseRepository? purchaseRepository = null,
            IProdutoRepository? produtoRepository = null,
            FinanceiroDatabaseService? financeiroService = null,
            PermissionService? permissionService = null,
            LoggerService? logger = null,
            AppSessionService? sessionService = null,
            AuditLogService? auditLogService = null)
        {
            _purchaseRepository = purchaseRepository ?? App.Repositories.Purchases;
            _produtoRepository = produtoRepository ?? App.Repositories.Produtos;
            _logger = logger ?? App.Logger;
            _sessionService = sessionService ?? App.Session;
            _financeiroService = financeiroService ?? new FinanceiroDatabaseService(App.Database);
            _permissionService = permissionService ?? PermissionService.CriarParaSessaoAtual(_logger);
            _auditLogService = auditLogService ?? App.Audit;
        }

        private void ExigirPermissao(string codigoPermissao, string operacao)
        {
            if (!_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                var usuario = _sessionService.CurrentUser?.Nome ?? "Anônimo";
                _logger.LogWarning($"Permissão negada para '{operacao}'. Usuário: {usuario}, Permissão requerida: {codigoPermissao}");
                throw new UnauthorizedAccessException($"Acesso negado: o perfil atual não possui permissão para {operacao}. ({codigoPermissao})");
            }
        }

        public async Task<IReadOnlyList<PurchaseRequest>> ListarRequisicoesAsync(string? busca = null, PurchasePriority? prioridade = null, PurchaseRequestStatus? status = null, int? solicitanteId = null, CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_VER", "visualizar requisições de compra");
            return await _purchaseRepository.ObterTodosAsync(busca, prioridade, status, solicitanteId, ct).ConfigureAwait(false);
        }

        public async Task<PurchaseRequest?> ObterPorIdAsync(Guid requestId, CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_VER", "visualizar detalhes da requisição de compra");
            return await _purchaseRepository.ObterPorIdAsync(requestId, ct).ConfigureAwait(false);
        }

        public async Task<PurchaseRequest?> ObterPorNumeroAsync(string numero, CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_VER", "visualizar requisição de compra por número");
            return await _purchaseRepository.ObterPorNumeroAsync(numero, ct).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Produto>> ObterSugestoesEstoqueMinimoAsync(CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_VER", "consultar necessidades de reposição do estoque");
            return await _purchaseRepository.ObterProdutosAbaixoEstoqueMinimoAsync(ct).ConfigureAwait(false);
        }

        public async Task<PurchaseRequest> CriarRequisicaoAsync(
            int usuarioId,
            string usuarioNome,
            PurchasePriority prioridade,
            PurchaseReason motivo,
            string? observacoes = null,
            List<PurchaseRequestItem>? itensIniciais = null,
            CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_SOLICITAR", "criar solicitação de compra");

            var timestamp = DateTime.Now;
            var numero = $"REQ-{timestamp:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

            var request = new PurchaseRequest
            {
                PurchaseRequestId = Guid.NewGuid(),
                Number = numero,
                RequestedByUserId = usuarioId,
                RequestedByUserName = usuarioNome,
                RequestedAt = timestamp,
                Priority = prioridade,
                Reason = motivo,
                Status = PurchaseRequestStatus.REQUESTED,
                Notes = observacoes,
                RowVersion = 1,
                CreatedAt = timestamp,
                UpdatedAt = timestamp,
                Items = itensIniciais ?? new List<PurchaseRequestItem>()
            };

            // Calcular estimativa inicial se houver itens
            long totalCents = 0;
            foreach (var item in request.Items)
            {
                item.PurchaseRequestId = request.PurchaseRequestId;
                totalCents += (long)(item.EstimatedUnitCostCents * item.RequestedQuantity);
            }
            request.TotalEstimatedCostCents = totalCents;

            var sucesso = await _purchaseRepository.InserirAsync(request, ct).ConfigureAwait(false);
            if (!sucesso)
            {
                throw new InvalidOperationException("Não foi possível registrar a requisição de compras.");
            }

            var custoEstimadoFmt = request.TotalEstimatedCost.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
            _auditLogService.Registrar(
                categoria: "Compras",
                acao: "Solicitacao",
                entidade: "PurchaseRequest",
                entidadeId: request.PurchaseRequestId.ToString(),
                detalhes: $"Requisição '{request.Number}' criada por '{usuarioNome}' com prioridade '{request.PriorityDisplay}'. Motivo: {motivo}. Total Estimado: {custoEstimadoFmt}.");

            return request;
        }

        public async Task<bool> AdicionarItemAsync(
            Guid requestId,
            Guid produtoId,
            decimal quantidade,
            PurchasePriority prioridade,
            string? motivo = null,
            CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_SOLICITAR", "adicionar item na requisição de compra");

            var req = await _purchaseRepository.ObterPorIdAsync(requestId, ct).ConfigureAwait(false);
            if (req == null)
            {
                throw new InvalidOperationException("Requisição não localizada.");
            }

            if (req.Status != PurchaseRequestStatus.DRAFT && req.Status != PurchaseRequestStatus.REQUESTED && req.Status != PurchaseRequestStatus.QUOTING)
            {
                throw new InvalidOperationException($"Não é permitido alterar itens de uma requisição com status '{req.StatusDisplay}'.");
            }

            var produto = _produtoRepository.ObterPorId(produtoId);
            if (produto == null)
            {
                throw new InvalidOperationException("Produto não localizado no cadastro.");
            }

            var custoEstimadoCents = MoneyCents.FromDecimal(produto.PrecoCompra).Cents;
            var item = new PurchaseRequestItem
            {
                ItemId = Guid.NewGuid(),
                PurchaseRequestId = requestId,
                ProductId = produtoId,
                ProductCode = produto.Codigo,
                ProductName = produto.Nome,
                RequestedQuantity = quantidade,
                SuggestedQuantity = Math.Max(0, produto.QuantidadeMinima - produto.QuantidadeEstoque),
                CurrentStock = produto.QuantidadeEstoque,
                MinimumStock = produto.QuantidadeMinima,
                IdealStock = produto.QuantidadeMinima * 2,
                EstimatedUnitCostCents = custoEstimadoCents,
                ActualUnitCostCents = custoEstimadoCents,
                Priority = prioridade,
                Reason = motivo,
                Status = PurchaseItemStatus.PENDING,
                CreatedAt = DateTime.Now
            };

            var sucesso = await _purchaseRepository.InserirItemAsync(item, ct).ConfigureAwait(false);
            if (sucesso)
            {
                req.TotalEstimatedCostCents += (long)(item.EstimatedUnitCostCents * item.RequestedQuantity);
                await _purchaseRepository.AtualizarAsync(req, ct).ConfigureAwait(false);
            }

            return sucesso;
        }

        public async Task<bool> RemoverItemAsync(Guid itemId, CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_SOLICITAR", "remover item da requisição de compra");
            return await _purchaseRepository.ExcluirItemAsync(itemId, ct).ConfigureAwait(false);
        }

        public async Task<bool> AprovarRequisicaoAsync(Guid requestId, int usuarioId, string usuarioNome, CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_APROVAR", "aprovar requisição de compra");

            var req = await _purchaseRepository.ObterPorIdAsync(requestId, ct).ConfigureAwait(false);
            if (req == null)
            {
                throw new InvalidOperationException("Requisição não localizada.");
            }

            if (req.Status == PurchaseRequestStatus.APPROVED)
            {
                throw new InvalidOperationException("Esta requisição de compra já foi aprovada.");
            }

            if (req.Status == PurchaseRequestStatus.CANCELLED)
            {
                throw new InvalidOperationException("Esta requisição de compra foi cancelada e não pode ser aprovada.");
            }

            var sucesso = await _purchaseRepository.AtualizarStatusAsync(requestId, PurchaseRequestStatus.APPROVED, usuarioId, null, ct).ConfigureAwait(false);
            if (sucesso)
            {
                var estCustoFmt = req.TotalEstimatedCost.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
                _auditLogService.Registrar(
                    categoria: "Compras",
                    acao: "Aprovacao",
                    entidade: "PurchaseRequest",
                    entidadeId: req.PurchaseRequestId.ToString(),
                    detalhes: $"Requisição '{req.Number}' aprovada pelo gestor '{usuarioNome}'. Valor estimado: {estCustoFmt}.");
            }

            return sucesso;
        }

        public async Task<bool> CancelarRequisicaoAsync(Guid requestId, int usuarioId, string motivo, CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_CANCELAR", "cancelar requisição de compra");

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw new ArgumentException("A justificativa de cancelamento é obrigatória.", nameof(motivo));
            }

            var req = await _purchaseRepository.ObterPorIdAsync(requestId, ct).ConfigureAwait(false);
            if (req == null)
            {
                throw new InvalidOperationException("Requisição não localizada.");
            }

            if (req.Status == PurchaseRequestStatus.RECEIVED)
            {
                throw new InvalidOperationException("Não é possível cancelar uma compra já recebida fisicamente no estoque.");
            }

            var sucesso = await _purchaseRepository.AtualizarStatusAsync(requestId, PurchaseRequestStatus.CANCELLED, usuarioId, motivo.Trim(), ct).ConfigureAwait(false);
            if (sucesso)
            {
                _auditLogService.Registrar(
                    categoria: "Compras",
                    acao: "Cancelamento",
                    entidade: "PurchaseRequest",
                    entidadeId: req.PurchaseRequestId.ToString(),
                    detalhes: $"Requisição '{req.Number}' cancelada pelo usuário ID {usuarioId}. Motivo: '{motivo}'.");
            }

            return sucesso;
        }

        public async Task<bool> FormalizarPedidoAsync(Guid requestId, Guid? fornecedorId, string fornecedorNome, decimal custoTotalEstimado, CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_PEDIR", "formalizar pedido de compra com fornecedor");

            var req = await _purchaseRepository.ObterPorIdAsync(requestId, ct).ConfigureAwait(false);
            if (req == null)
            {
                throw new InvalidOperationException("Requisição não localizada.");
            }

            if (req.Status != PurchaseRequestStatus.APPROVED && req.Status != PurchaseRequestStatus.QUOTING)
            {
                throw new InvalidOperationException($"Apenas requisições aprovadas podem ser formalizadas como pedido. (Situação atual: {req.StatusDisplay})");
            }

            req.SupplierId = fornecedorId;
            req.SupplierName = fornecedorNome;
            req.TotalEstimatedCostCents = MoneyCents.FromDecimal(custoTotalEstimado).Cents;
            req.Status = PurchaseRequestStatus.ORDERED;
            req.UpdatedAt = DateTime.Now;

            var sucesso = await _purchaseRepository.AtualizarAsync(req, ct).ConfigureAwait(false);
            if (sucesso)
            {
                var valFmt = custoTotalEstimado.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
                _auditLogService.Registrar(
                    categoria: "Compras",
                    acao: "Pedido",
                    entidade: "PurchaseRequest",
                    entidadeId: req.PurchaseRequestId.ToString(),
                    detalhes: $"Pedido formalizado para requisição '{req.Number}' com fornecedor '{fornecedorNome}'. Valor: {valFmt}.");
            }

            return sucesso;
        }

        public async Task<bool> ReceberMercadoriaAsync(
            Guid requestId,
            int usuarioId,
            string? documentoFiscal,
            decimal? custoEfetivoTotal = null,
            Dictionary<Guid, decimal>? quantidadesRecebidas = null,
            CancellationToken ct = default)
        {
            ExigirPermissao("COMPRAS_RECEBER", "registrar recebimento de mercadoria e entrada em estoque");

            var req = await _purchaseRepository.ObterPorIdAsync(requestId, ct).ConfigureAwait(false);
            if (req == null)
            {
                throw new InvalidOperationException("Requisição de compra não localizada.");
            }

            if (req.Status != PurchaseRequestStatus.ORDERED && req.Status != PurchaseRequestStatus.PARTIALLY_RECEIVED && req.Status != PurchaseRequestStatus.APPROVED)
            {
                throw new InvalidOperationException($"A requisição '{req.Number}' não está em status aguardando entrega (Situação: {req.StatusDisplay}).");
            }

            // 1. Atualizar itens e dar entrada física no estoque
            var itens = await _purchaseRepository.ObterItensAsync(requestId, ct).ConfigureAwait(false);
            decimal valorTotalItens = 0;

            foreach (var item in itens)
            {
                var qtd = (quantidadesRecebidas != null && quantidadesRecebidas.TryGetValue(item.ItemId, out var q))
                    ? q
                    : item.RequestedQuantity;

                item.ReceivedQuantity = qtd;
                item.Status = PurchaseItemStatus.RECEIVED;
                await _purchaseRepository.AtualizarItemAsync(item, ct).ConfigureAwait(false);

                // Entrada de estoque no produto
                if (item.ProductId.HasValue)
                {
                    var prod = _produtoRepository.ObterPorId(item.ProductId.Value);
                    if (prod != null)
                    {
                        prod.QuantidadeEstoque += (int)Math.Round(qtd, MidpointRounding.AwayFromZero);
                        _produtoRepository.Atualizar(prod);
                    }
                }

                valorTotalItens += item.ActualUnitCost * qtd;
            }

            var custoFinal = custoEfetivoTotal ?? (valorTotalItens > 0 ? valorTotalItens : req.TotalEstimatedCost);
            req.TotalActualCostCents = MoneyCents.FromDecimal(custoFinal).Cents;
            req.FiscalDocumentNumber = documentoFiscal;
            req.Status = PurchaseRequestStatus.RECEIVED;
            req.UpdatedAt = DateTime.Now;

            var sucesso = await _purchaseRepository.AtualizarAsync(req, ct).ConfigureAwait(false);
            if (!sucesso)
            {
                throw new InvalidOperationException("Não foi possível finalizar o recebimento da compra.");
            }

            // 2. Integração com o Financeiro (Contas a Pagar em CentsV1 sem duplicação)
            if (custoFinal > 0)
            {
                try
                {
                    var fornecedorNome = string.IsNullOrWhiteSpace(req.SupplierName) ? "Fornecedor Geral" : req.SupplierName;
                    var dataVencimento = DateTime.Now.AddDays(30);

                    _financeiroService.AdicionarContaPagar(
                        fornecedor: fornecedorNome,
                        descricao: $"Compra Recebida - Req {req.Number} (NF {documentoFiscal ?? "N/A"})",
                        valor: custoFinal,
                        dataVencimento: dataVencimento,
                        categoria: "Peças e Insumos",
                        observacoes: $"Origem: PRIMOX Purchasing. Requisição {req.Number}.",
                        origem: "Compras",
                        referenciaExterna: req.Number);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Recebimento registrado, mas falha ao integrar com Contas a Pagar: {ex.Message}");
                }
            }

            var custoFinalFmt = custoFinal.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
            _auditLogService.Registrar(
                categoria: "Compras",
                acao: "Recebimento",
                entidade: "PurchaseRequest",
                entidadeId: req.PurchaseRequestId.ToString(),
                detalhes: $"Mercadoria da requisição '{req.Number}' recebida e conferida no estoque por usuário ID {usuarioId}. NF: {documentoFiscal ?? "N/A"}, Custo Total: {custoFinalFmt}.");

            return true;
        }
    }
}
