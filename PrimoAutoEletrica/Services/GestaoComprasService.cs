using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public sealed class GestaoComprasService : IGestaoComprasService
    {
        private readonly DatabaseService _databaseService;
        private readonly IProdutoRepository _produtoRepository;
        private readonly IFornecedorRepository _fornecedorRepository;
        private readonly EstoqueOperationalService _estoqueOperationalService;
        private readonly LoggerService? _logger;

        public GestaoComprasService(
            DatabaseService databaseService,
            IProdutoRepository produtoRepository,
            IFornecedorRepository fornecedorRepository,
            EstoqueOperationalService estoqueOperationalService,
            LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _produtoRepository = produtoRepository ?? throw new ArgumentNullException(nameof(produtoRepository));
            _fornecedorRepository = fornecedorRepository ?? throw new ArgumentNullException(nameof(fornecedorRepository));
            _estoqueOperationalService = estoqueOperationalService ?? throw new ArgumentNullException(nameof(estoqueOperationalService));
            _logger = logger;
        }

        public async Task<List<ItemFaltaEstoque>> ObterNecessidadesReposicaoAsync(
            string? termoBusca = null,
            NivelUrgenciaFalta? filtroUrgencia = null,
            Guid? fornecedorId = null,
            string? curvaAbc = null)
        {
            return await Task.Run(() =>
            {
                var produtos = _produtoRepository.ObterTodos()
                    .Where(p => p.Ativo)
                    .ToList();

                if (produtos.Count == 0)
                {
                    return new List<ItemFaltaEstoque>();
                }

                _estoqueOperationalService.EnriquecerProdutosComReservas(produtos);
                var fornecedores = _fornecedorRepository.ObterTodos()
                    .ToDictionary(f => f.Id, f => f);

                var consumos60Dias = ObterConsumoProdutosUltimosDias(60);

                var itensNecessidade = new List<ItemFaltaEstoque>();

                foreach (var prod in produtos)
                {
                    var saldoReal = prod.QuantidadeDisponivel;
                    var consumoTotal = consumos60Dias.TryGetValue(prod.Id, out var totalSaidas) ? totalSaidas : 0;
                    var cmd = (decimal)consumoTotal / 60.0m;

                    var leadTime = prod.LeadTimeDias > 0 ? prod.LeadTimeDias : 3;
                    var estoqueSeguranca = prod.EstoqueSeguranca > 0 ? prod.EstoqueSeguranca : Math.Max(2, prod.QuantidadeMinima / 2);
                    
                    var pontoPedidoCalculado = (int)Math.Ceiling((cmd * leadTime) + estoqueSeguranca);
                    var pontoDePedido = Math.Max(pontoPedidoCalculado, prod.QuantidadeMinima);

                    // Verifica se o item atingiu condicao de reposicao
                    bool precisaReposicao = saldoReal <= pontoDePedido || saldoReal <= prod.QuantidadeMinima || saldoReal <= 0;

                    if (!precisaReposicao)
                    {
                        continue;
                    }

                    // Classifica urgencia
                    NivelUrgenciaFalta urgencia;
                    if (saldoReal <= 0 && prod.QuantidadeReservada > 0)
                    {
                        urgencia = NivelUrgenciaFalta.Critica;
                    }
                    else if (saldoReal <= 0)
                    {
                        urgencia = NivelUrgenciaFalta.Critica;
                    }
                    else if (saldoReal <= prod.QuantidadeMinima)
                    {
                        urgencia = NivelUrgenciaFalta.Alta;
                    }
                    else if (saldoReal <= pontoDePedido)
                    {
                        urgencia = NivelUrgenciaFalta.Media;
                    }
                    else
                    {
                        urgencia = NivelUrgenciaFalta.Preventiva;
                    }

                    // Calculo da quantidade sugerida baseada na Curva ABC
                    var curva = string.IsNullOrWhiteSpace(prod.CurvaAbc) ? "B" : prod.CurvaAbc.Trim().ToUpperInvariant();
                    int diasGiroAlvo = curva switch
                    {
                        "A" => 30,
                        "B" => 45,
                        _ => 15
                    };

                    int consumoAlvo = (int)Math.Ceiling(cmd * diasGiroAlvo);
                    int baseEstoqueAlvo = Math.Max(consumoAlvo, Math.Max(prod.QuantidadeMaxima > 0 ? prod.QuantidadeMaxima : prod.QuantidadeMinima * 2, 5));
                    int quantidadeSugerida = Math.Max(1, baseEstoqueAlvo - saldoReal);

                    // Dados do fornecedor preferencial
                    Fornecedor? fornecedor = null;
                    if (prod.FornecedorId.HasValue && fornecedores.TryGetValue(prod.FornecedorId.Value, out var fEncontrado))
                    {
                        fornecedor = fEncontrado;
                    }

                    itensNecessidade.Add(new ItemFaltaEstoque
                    {
                        ProdutoId = prod.Id,
                        Codigo = prod.Codigo,
                        Nome = prod.Nome,
                        Categoria = prod.Categoria,
                        Marca = prod.Marca,
                        Localizacao = !string.IsNullOrWhiteSpace(prod.Localizacao) ? prod.Localizacao : $"{prod.Prateleira} {prod.Gaveta}".Trim(),
                        QuantidadeEstoque = prod.QuantidadeEstoque,
                        QuantidadeMinima = prod.QuantidadeMinima,
                        QuantidadeReservadaOS = prod.QuantidadeReservada,
                        ConsumoMedioDiario = Math.Round(cmd, 2),
                        LeadTimeDias = leadTime,
                        EstoqueSeguranca = estoqueSeguranca,
                        PontoDePedido = pontoDePedido,
                        QuantidadeSugeridaCompra = quantidadeSugerida,
                        UltimoCustoCompra = prod.PrecoCompra > 0 ? prod.PrecoCompra : 0m,
                        PrecoVendaAtual = prod.PrecoVenda,
                        Urgencia = urgencia,
                        CurvaAbc = curva,
                        FornecedorPreferencialId = fornecedor?.Id ?? prod.FornecedorId,
                        FornecedorPreferencialNome = fornecedor?.NomeFantasia ?? prod.Fornecedor,
                        FornecedorTelefone = fornecedor?.Telefone ?? prod.TelefoneFornecedor,
                        FornecedorWhatsApp = fornecedor?.WhatsAppVendedor ?? fornecedor?.Celular ?? prod.TelefoneFornecedor
                    });
                }

                // Aplicar filtros
                var consulta = itensNecessidade.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(termoBusca))
                {
                    var termo = termoBusca.Trim().ToLowerInvariant();
                    consulta = consulta.Where(i =>
                        (i.Codigo != null && i.Codigo.ToLowerInvariant().Contains(termo)) ||
                        (i.Nome != null && i.Nome.ToLowerInvariant().Contains(termo)) ||
                        (i.Categoria != null && i.Categoria.ToLowerInvariant().Contains(termo)) ||
                        (i.Marca != null && i.Marca.ToLowerInvariant().Contains(termo)));
                }

                if (filtroUrgencia.HasValue)
                {
                    consulta = consulta.Where(i => i.Urgencia == filtroUrgencia.Value);
                }

                if (fornecedorId.HasValue && fornecedorId.Value != Guid.Empty)
                {
                    consulta = consulta.Where(i => i.FornecedorPreferencialId == fornecedorId.Value);
                }

                if (!string.IsNullOrWhiteSpace(curvaAbc) && curvaAbc != "Todas")
                {
                    consulta = consulta.Where(i => string.Equals(i.CurvaAbc, curvaAbc, StringComparison.OrdinalIgnoreCase));
                }

                return consulta
                    .OrderBy(i => (int)i.Urgencia)
                    .ThenBy(i => i.CurvaAbc)
                    .ThenByDescending(i => i.ValorTotalEstimado)
                    .ToList();
            });
        }

        public async Task<ResumoNecessidadeCompras> ObterResumoNecessidadesAsync()
        {
            var todas = await ObterNecessidadesReposicaoAsync();
            return new ResumoNecessidadeCompras
            {
                TotalItensEmFalta = todas.Count,
                RupturasCriticas = todas.Count(i => i.Urgencia == NivelUrgenciaFalta.Critica),
                ItensAbaixoMinimo = todas.Count(i => i.Urgencia <= NivelUrgenciaFalta.Alta),
                CustoTotalEstimado = todas.Sum(i => i.ValorTotalEstimado),
                FornecedoresImpactados = todas
                    .Where(i => i.FornecedorPreferencialId.HasValue && i.FornecedorPreferencialId != Guid.Empty)
                    .Select(i => i.FornecedorPreferencialId!.Value)
                    .Distinct()
                    .Count()
            };
        }

        public Task<PedidoCompra> GerarRascunhoPedidoAsync(Guid fornecedorId, IEnumerable<ItemFaltaEstoque> itens)
        {
            return Task.Run(() =>
            {
                var fornecedor = _fornecedorRepository.ObterPorId(fornecedorId);
                var listaItens = itens.ToList();

                var proximoNumero = ObterProximoNumeroPedido();

                var pedido = new PedidoCompra
                {
                    Id = Guid.NewGuid(),
                    Numero = proximoNumero,
                    FornecedorId = fornecedorId,
                    FornecedorNome = fornecedor?.NomeFantasia ?? fornecedor?.RazaoSocial ?? "Fornecedor Geral",
                    FornecedorCNPJ = fornecedor?.CNPJ ?? string.Empty,
                    FornecedorTelefone = fornecedor?.Telefone ?? string.Empty,
                    FornecedorEmail = fornecedor?.Email ?? string.Empty,
                    Status = StatusPedidoCompra.Rascunho,
                    DataCriacao = DateTime.Now,
                    PrevisaoEntrega = DateTime.Now.AddDays(fornecedor?.PrazoMedioEntregaDias > 0 ? fornecedor.PrazoMedioEntregaDias : 3),
                    FormaPagamento = fornecedor?.FormaPagamento ?? "Boleto Faturado",
                    CondicaoPagamento = fornecedor?.PrazoPagamento ?? "30 dias"
                };

                foreach (var item in listaItens)
                {
                    var qtd = item.QuantidadeSugeridaCompra > 0 ? item.QuantidadeSugeridaCompra : 1;
                    var valorUnit = item.UltimoCustoCompra > 0 ? item.UltimoCustoCompra : 0m;

                    pedido.Itens.Add(new PedidoCompraItem
                    {
                        Id = Guid.NewGuid(),
                        PedidoCompraId = pedido.Id,
                        ProdutoId = item.ProdutoId,
                        Codigo = item.Codigo,
                        Descricao = item.Nome,
                        QuantidadePedida = qtd,
                        QuantidadeRecebida = 0,
                        ValorUnitario = valorUnit
                    });
                }

                pedido.ValorTotal = pedido.Itens.Sum(i => i.Subtotal);
                return pedido;
            });
        }

        public async Task SalvarPedidoCompraAsync(PedidoCompra pedido)
        {
            if (pedido == null) throw new ArgumentNullException(nameof(pedido));

            await Task.Run(() =>
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                            INSERT OR REPLACE INTO PedidosCompra
                            (
                                Id, Numero, FornecedorId, FornecedorNome, FornecedorCNPJ,
                                FornecedorTelefone, FornecedorEmail, Status, ValorTotal,
                                DataCriacao, DataEnvioCotacao, PrevisaoEntrega, DataRecebimento,
                                ChaveNFeVinculada, NumeroNFe, FormaPagamento, CondicaoPagamento,
                                Observacoes, CriadoPor
                            )
                            VALUES
                            (
                                @Id, @Numero, @FornecedorId, @FornecedorNome, @FornecedorCNPJ,
                                @FornecedorTelefone, @FornecedorEmail, @Status, @ValorTotal,
                                @DataCriacao, @DataEnvioCotacao, @PrevisaoEntrega, @DataRecebimento,
                                @ChaveNFeVinculada, @NumeroNFe, @FormaPagamento, @CondicaoPagamento,
                                @Observacoes, @CriadoPor
                            );";

                        cmd.Parameters.AddWithValue("@Id", pedido.Id.ToString());
                        cmd.Parameters.AddWithValue("@Numero", pedido.Numero ?? string.Empty);
                        cmd.Parameters.AddWithValue("@FornecedorId", pedido.FornecedorId.ToString());
                        cmd.Parameters.AddWithValue("@FornecedorNome", pedido.FornecedorNome ?? string.Empty);
                        cmd.Parameters.AddWithValue("@FornecedorCNPJ", pedido.FornecedorCNPJ ?? string.Empty);
                        cmd.Parameters.AddWithValue("@FornecedorTelefone", pedido.FornecedorTelefone ?? string.Empty);
                        cmd.Parameters.AddWithValue("@FornecedorEmail", pedido.FornecedorEmail ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Status", (int)pedido.Status);
                        cmd.Parameters.AddWithValue("@ValorTotal", (double)pedido.ValorTotal);
                        cmd.Parameters.AddWithValue("@DataCriacao", pedido.DataCriacao.ToString("o", CultureInfo.InvariantCulture));
                        cmd.Parameters.AddWithValue("@DataEnvioCotacao", (object?)pedido.DataEnvioCotacao?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PrevisaoEntrega", (object?)pedido.PrevisaoEntrega?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DataRecebimento", (object?)pedido.DataRecebimento?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChaveNFeVinculada", (object?)pedido.ChaveNFeVinculada ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NumeroNFe", (object?)pedido.NumeroNFe ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FormaPagamento", pedido.FormaPagamento ?? string.Empty);
                        cmd.Parameters.AddWithValue("@CondicaoPagamento", pedido.CondicaoPagamento ?? string.Empty);
                        cmd.Parameters.AddWithValue("@Observacoes", pedido.Observacoes ?? string.Empty);
                        cmd.Parameters.AddWithValue("@CriadoPor", pedido.CriadoPor ?? "Sistema");

                        cmd.ExecuteNonQuery();
                    }

                    // Remover itens antigos se for atualizacao
                    using (var deleteCmd = connection.CreateCommand())
                    {
                        deleteCmd.Transaction = transaction;
                        deleteCmd.CommandText = "DELETE FROM PedidosCompraItens WHERE PedidoCompraId = @PedidoId;";
                        deleteCmd.Parameters.AddWithValue("@PedidoId", pedido.Id.ToString());
                        deleteCmd.ExecuteNonQuery();
                    }

                    // Inserir itens atuais
                    foreach (var item in pedido.Itens)
                    {
                        using var itemCmd = connection.CreateCommand();
                        itemCmd.Transaction = transaction;
                        itemCmd.CommandText = @"
                            INSERT INTO PedidosCompraItens
                            (
                                Id, PedidoCompraId, ProdutoId, Codigo, Descricao,
                                QuantidadePedida, QuantidadeRecebida, ValorUnitario
                            )
                            VALUES
                            (
                                @Id, @PedidoCompraId, @ProdutoId, @Codigo, @Descricao,
                                @QuantidadePedida, @QuantidadeRecebida, @ValorUnitario
                            );";

                        itemCmd.Parameters.AddWithValue("@Id", item.Id.ToString());
                        itemCmd.Parameters.AddWithValue("@PedidoCompraId", pedido.Id.ToString());
                        itemCmd.Parameters.AddWithValue("@ProdutoId", item.ProdutoId.ToString());
                        itemCmd.Parameters.AddWithValue("@Codigo", item.Codigo ?? string.Empty);
                        itemCmd.Parameters.AddWithValue("@Descricao", item.Descricao ?? string.Empty);
                        itemCmd.Parameters.AddWithValue("@QuantidadePedida", item.QuantidadePedida);
                        itemCmd.Parameters.AddWithValue("@QuantidadeRecebida", item.QuantidadeRecebida);
                        itemCmd.Parameters.AddWithValue("@ValorUnitario", (double)item.ValorUnitario);

                        itemCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _logger?.LogInfo($"Pedido de compra '{pedido.Numero}' salvo com sucesso ({pedido.Itens.Count} itens).");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger?.LogError($"Erro ao salvar pedido de compra '{pedido.Numero}'.", ex);
                    throw;
                }
            });
        }

        public async Task<List<PedidoCompra>> ListarPedidosCompraAsync(StatusPedidoCompra? status = null, Guid? fornecedorId = null)
        {
            return await Task.Run(() =>
            {
                var pedidos = new List<PedidoCompra>();

                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                var sql = "SELECT * FROM PedidosCompra WHERE 1=1";
                if (status.HasValue)
                {
                    sql += " AND Status = @Status";
                    cmd.Parameters.AddWithValue("@Status", (int)status.Value);
                }
                if (fornecedorId.HasValue && fornecedorId.Value != Guid.Empty)
                {
                    sql += " AND FornecedorId = @FornecedorId";
                    cmd.Parameters.AddWithValue("@FornecedorId", fornecedorId.Value.ToString());
                }
                sql += " ORDER BY DataCriacao DESC;";
                cmd.CommandText = sql;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        pedidos.Add(MapearPedido(reader));
                    }
                }

                // Carregar itens para cada pedido
                foreach (var pedido in pedidos)
                {
                    pedido.Itens = ObterItensDoPedido(connection, pedido.Id);
                }

                return pedidos;
            });
        }

        public async Task<PedidoCompra?> ObterPedidoCompraPorIdAsync(Guid pedidoId)
        {
            return await Task.Run(() =>
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM PedidosCompra WHERE Id = @Id LIMIT 1;";
                cmd.Parameters.AddWithValue("@Id", pedidoId.ToString());

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                var pedido = MapearPedido(reader);
                reader.Close();

                pedido.Itens = ObterItensDoPedido(connection, pedido.Id);
                return pedido;
            });
        }

        public async Task AtualizarStatusPedidoAsync(Guid pedidoId, StatusPedidoCompra novoStatus, string? chaveNFe = null)
        {
            await Task.Run(() =>
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                var sql = "UPDATE PedidosCompra SET Status = @Status";
                cmd.Parameters.AddWithValue("@Status", (int)novoStatus);

                if (novoStatus == StatusPedidoCompra.CotacaoEnviada)
                {
                    sql += ", DataEnvioCotacao = @Agora";
                    cmd.Parameters.AddWithValue("@Agora", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
                }
                else if (novoStatus == StatusPedidoCompra.RecebidoTotal || novoStatus == StatusPedidoCompra.RecebidoParcial)
                {
                    sql += ", DataRecebimento = @Agora";
                    cmd.Parameters.AddWithValue("@Agora", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
                }

                if (!string.IsNullOrWhiteSpace(chaveNFe))
                {
                    sql += ", ChaveNFeVinculada = @ChaveNFe";
                    cmd.Parameters.AddWithValue("@ChaveNFe", chaveNFe);
                }

                sql += " WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", pedidoId.ToString());
                cmd.CommandText = sql;

                cmd.ExecuteNonQuery();
            });
        }

        public string FormatarMensagemCotacaoWhatsApp(PedidoCompra pedido, string nomeOficina = "PRIMOX Auto Elétrica")
        {
            if (pedido == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"⚡ *SOLICITAÇÃO DE COTAÇÃO — {nomeOficina.ToUpperInvariant()}* ⚡");
            sb.AppendLine($"📋 Pedido: *{pedido.Numero}*");
            sb.AppendLine($"📅 Data: {pedido.DataCriacao:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"🏢 Para: *{pedido.FornecedorNome}*");
            sb.AppendLine();
            sb.AppendLine("Olá! Gostaria de consultar a disponibilidade e cotação para os itens abaixo:");
            sb.AppendLine("─────────────────────────");

            int index = 1;
            foreach (var item in pedido.Itens)
            {
                sb.AppendLine($"{index:00}. *[{item.Codigo}]* {item.Descricao}");
                sb.AppendLine($"    • Quantidade: *{item.QuantidadePedida} un*");
                index++;
            }

            sb.AppendLine("─────────────────────────");
            sb.AppendLine("Favor informar valor unitário, prazo de entrega e condições de pagamento.");
            sb.AppendLine("Agradecemos a atenção e aguardamos o retorno!");

            return sb.ToString();
        }

        public async Task<bool> BaixarPedidoComNFeAsync(string chaveNfe, Guid fornecedorId, Dictionary<string, int> itensEntregues)
        {
            if (itensEntregues == null || itensEntregues.Count == 0)
            {
                return false;
            }

            return await Task.Run(() =>
            {
                var pedidosAbertos = ListarPedidosCompraAsync(StatusPedidoCompra.AprovadoAguardandoEntrega, fornecedorId).Result;
                if (pedidosAbertos.Count == 0)
                {
                    // Tenta localizar tambem em cotacao enviada
                    pedidosAbertos = ListarPedidosCompraAsync(StatusPedidoCompra.CotacaoEnviada, fornecedorId).Result;
                }

                if (pedidosAbertos.Count == 0)
                {
                    return false;
                }

                using var connection = _databaseService.GetConnection();
                connection.Open();

                foreach (var pedido in pedidosAbertos)
                {
                    bool houveBaixa = false;
                    foreach (var item in pedido.Itens)
                    {
                        var chaveCodigo = item.Codigo?.Trim();
                        if (!string.IsNullOrEmpty(chaveCodigo) && itensEntregues.TryGetValue(chaveCodigo, out var qtdEntregue) && qtdEntregue > 0)
                        {
                            item.QuantidadeRecebida = Math.Min(item.QuantidadePedida, item.QuantidadeRecebida + qtdEntregue);
                            houveBaixa = true;

                            using var updateItemCmd = connection.CreateCommand();
                            updateItemCmd.CommandText = "UPDATE PedidosCompraItens SET QuantidadeRecebida = @Qtd WHERE Id = @Id;";
                            updateItemCmd.Parameters.AddWithValue("@Qtd", item.QuantidadeRecebida);
                            updateItemCmd.Parameters.AddWithValue("@Id", item.Id.ToString());
                            updateItemCmd.ExecuteNonQuery();
                        }
                    }

                    if (houveBaixa)
                    {
                        bool tudoEntregue = pedido.Itens.All(i => i.QuantidadeRecebida >= i.QuantidadePedida);
                        var novoStatus = tudoEntregue ? StatusPedidoCompra.RecebidoTotal : StatusPedidoCompra.RecebidoParcial;

                        AtualizarStatusPedidoAsync(pedido.Id, novoStatus, chaveNfe).Wait();
                        _logger?.LogInfo($"Baixa do Pedido '{pedido.Numero}' realizada via NFe '{chaveNfe}'. Status: {novoStatus}.");
                        return true;
                    }
                }

                return false;
            });
        }

        private string ObterProximoNumeroPedido()
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM PedidosCompra;";
                var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                var proximo = count + 1;
                return $"PC-{DateTime.Now:yyyy}-{proximo:D4}";
            }
            catch
            {
                return $"PC-{DateTime.Now:yyyy}-{new Random().Next(1000, 9999)}";
            }
        }

        private Dictionary<Guid, int> ObterConsumoProdutosUltimosDias(int dias)
        {
            var resultado = new Dictionary<Guid, int>();
            try
            {
                var dataLimite = DateTime.Now.AddDays(-dias).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                using var connection = _databaseService.GetConnection();
                connection.Open();

                // Consumo em Ordens de Servico
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT osi.ProdutoId, SUM(osi.Quantidade) as TotalQtd
                        FROM OrdemServicoItens osi
                        INNER JOIN OrdensServico os ON osi.OrdemServicoId = os.Id
                        WHERE os.DataCriacao >= @DataLimite AND osi.ProdutoId IS NOT NULL AND osi.ProdutoId != ''
                        GROUP BY osi.ProdutoId;";
                    cmd.Parameters.AddWithValue("@DataLimite", dataLimite);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (Guid.TryParse(reader.GetString(0), out var prodId))
                        {
                            var qtd = Convert.ToInt32(reader.GetValue(1));
                            resultado[prodId] = qtd;
                        }
                    }
                }

                // Consumo em Vendas
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT vi.ProdutoId, SUM(vi.Quantidade) as TotalQtd
                        FROM VendaItens vi
                        INNER JOIN Vendas v ON vi.VendaId = v.Id
                        WHERE v.DataVenda >= @DataLimite AND vi.ProdutoId IS NOT NULL AND vi.ProdutoId != ''
                        GROUP BY vi.ProdutoId;";
                    cmd.Parameters.AddWithValue("@DataLimite", dataLimite);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (Guid.TryParse(reader.GetString(0), out var prodId))
                        {
                            var qtd = Convert.ToInt32(reader.GetValue(1));
                            if (resultado.TryGetValue(prodId, out var anterior))
                            {
                                resultado[prodId] = anterior + qtd;
                            }
                            else
                            {
                                resultado[prodId] = qtd;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Não foi possível consolidar histórico de saídas para ROP: {ex.Message}");
            }

            return resultado;
        }

        private static List<PedidoCompraItem> ObterItensDoPedido(DbConnection connection, Guid pedidoId)
        {
            var itens = new List<PedidoCompraItem>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM PedidosCompraItens WHERE PedidoCompraId = @PedidoId ORDER BY Codigo;";
            cmd.Parameters.AddWithValue("@PedidoId", pedidoId.ToString());

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(new PedidoCompraItem
                {
                    Id = Guid.TryParse(reader["Id"]?.ToString(), out var id) ? id : Guid.NewGuid(),
                    PedidoCompraId = pedidoId,
                    ProdutoId = Guid.TryParse(reader["ProdutoId"]?.ToString(), out var prodId) ? prodId : Guid.Empty,
                    Codigo = reader["Codigo"]?.ToString() ?? string.Empty,
                    Descricao = reader["Descricao"]?.ToString() ?? string.Empty,
                    QuantidadePedida = Convert.ToInt32(reader["QuantidadePedida"] ?? 0),
                    QuantidadeRecebida = Convert.ToInt32(reader["QuantidadeRecebida"] ?? 0),
                    ValorUnitario = Convert.ToDecimal(reader["ValorUnitario"] ?? 0)
                });
            }

            return itens;
        }

        private static PedidoCompra MapearPedido(DbDataReader reader)
        {
            var pedido = new PedidoCompra
            {
                Id = Guid.TryParse(reader["Id"]?.ToString(), out var id) ? id : Guid.NewGuid(),
                Numero = reader["Numero"]?.ToString() ?? string.Empty,
                FornecedorId = Guid.TryParse(reader["FornecedorId"]?.ToString(), out var fId) ? fId : Guid.Empty,
                FornecedorNome = reader["FornecedorNome"]?.ToString() ?? string.Empty,
                FornecedorCNPJ = reader["FornecedorCNPJ"]?.ToString() ?? string.Empty,
                FornecedorTelefone = reader["FornecedorTelefone"]?.ToString() ?? string.Empty,
                FornecedorEmail = reader["FornecedorEmail"]?.ToString() ?? string.Empty,
                Status = Enum.TryParse<StatusPedidoCompra>(reader["Status"]?.ToString(), out var st) ? st : StatusPedidoCompra.Rascunho,
                ValorTotal = Convert.ToDecimal(reader["ValorTotal"] ?? 0),
                FormaPagamento = reader["FormaPagamento"]?.ToString() ?? string.Empty,
                CondicaoPagamento = reader["CondicaoPagamento"]?.ToString() ?? string.Empty,
                Observacoes = reader["Observacoes"]?.ToString() ?? string.Empty,
                CriadoPor = reader["CriadoPor"]?.ToString() ?? "Sistema",
                ChaveNFeVinculada = reader["ChaveNFeVinculada"]?.ToString(),
                NumeroNFe = reader["NumeroNFe"]?.ToString()
            };

            if (DateTime.TryParse(reader["DataCriacao"]?.ToString(), out var dc))
                pedido.DataCriacao = dc;
            if (DateTime.TryParse(reader["DataEnvioCotacao"]?.ToString(), out var de))
                pedido.DataEnvioCotacao = de;
            if (DateTime.TryParse(reader["PrevisaoEntrega"]?.ToString(), out var pe))
                pedido.PrevisaoEntrega = pe;
            if (DateTime.TryParse(reader["DataRecebimento"]?.ToString(), out var dr))
                pedido.DataRecebimento = dr;

            return pedido;
        }
    }
}
