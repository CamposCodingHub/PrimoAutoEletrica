using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public class VendaService
    {
        private readonly DatabaseService _databaseService;
        private readonly Repositories.VendaRepository _vendaRepository;
        private readonly CaixaService _caixaService;
        private readonly EstoqueOperationalService _estoqueOperationalService;
        private static LoggerService Logger => global::PrimoAutoEletrica.App.Logger;
        private sealed record EstoqueAuditoriaItem(
            Guid ProdutoId,
            string Codigo,
            string Nome,
            decimal PrecoCompra,
            int EstoqueAnterior,
            int EstoqueNovo,
            int ReservadoAnterior,
            int ReservadoNovo,
            int Quantidade,
            string Acao);

        public VendaService(DatabaseService? databaseService = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
            _vendaRepository = new Repositories.VendaRepository(_databaseService);
            _caixaService = new CaixaService(_databaseService);
            _estoqueOperationalService = new EstoqueOperationalService(_databaseService, Logger);
        }

        public void RegistrarVenda(Venda venda, bool atualizarEstoque = true)
        {
            ArgumentNullException.ThrowIfNull(venda);

            var reservasAtivas = _estoqueOperationalService.ObterReservasAtivasPorProduto(
                venda.Itens?
                    .Where(item => item.UsaEstoque && item.ProdutoId.HasValue)
                    .Select(item => item.ProdutoId!.Value)
                    ?? Enumerable.Empty<Guid>());
            PrepararEValidarVenda(venda, reservasAtivas);
            var itensVenda = venda.Itens ?? new List<ItemVenda>();
            venda.Itens = itensVenda;
            var auditoriasEstoque = new List<EstoqueAuditoriaItem>();

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                _vendaRepository.InserirVenda(connection, transaction, venda);

                foreach (var item in itensVenda)
                {
                    _vendaRepository.InserirItemVenda(connection, transaction, venda.Id, item);
                    if (atualizarEstoque && item.UsaEstoque)
                    {
                        auditoriasEstoque.Add(AtualizarEstoqueProduto(connection, transaction, item));
                    }
                }

                _caixaService.RegistrarVendaNaSessao(connection, transaction, venda);

                transaction.Commit();
                RegistrarAuditoriaEstoque(auditoriasEstoque, venda.Id, "SaidaVendaProduto", $"Venda={venda.Id}; Cliente={venda.Cliente?.Nome ?? "Consumidor final"}");
                Logger.LogInfo($"Venda '{venda.Id}' registrada com {venda.Itens.Count} item(ns) e total {venda.Total:C}.");
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "PDV",
                    "VendaRegistrada",
                    "Venda",
                    venda.Id.ToString(),
                    $"Total={venda.Total:C}; Itens={venda.Itens.Count}; Cliente={venda.Cliente?.Nome ?? "Nao informado"}; FormaPagamento={venda.FormaPagamento}; CaixaSessao={venda.CaixaSessaoId?.ToString() ?? "Nao vinculada"}; EstoqueAtualizado={atualizarEstoque}");

                try
                {
                    App.SendLocalSyncMessage($"VendaRegistrada:{venda.Id}:{venda.Total}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.LogError($"Falha ao registrar venda '{venda.Id}'.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("PDV", "FalhaVenda", ex, "Venda", venda.Id.ToString());
                throw;
            }
        }

        public void CancelarVenda(Guid vendaId, string motivo, bool reverterEstoque = true)
        {
            ComercialValidationHelper.GarantirTextoObrigatorio(motivo, "o motivo do cancelamento");
            var auditoriasEstoque = new List<EstoqueAuditoriaItem>();

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var venda = ObterVendaInterna(connection, transaction, vendaId)
                ?? throw new InvalidOperationException("Venda nao encontrada para cancelamento.");

            if (string.Equals(venda.Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Esta venda ja foi cancelada.");
            }

            if (reverterEstoque)
            {
                foreach (var item in venda.Itens.Where(item => item.UsaEstoque))
                {
                    auditoriasEstoque.Add(ReverterEstoqueProduto(connection, transaction, item));
                }
            }

            _caixaService.CancelarVendaNaSessao(connection, transaction, venda, motivo.Trim());

            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = @"
                UPDATE Vendas
                SET Status = 'Cancelada',
                    DataCancelamento = @dataCancelamento,
                    CanceladoPor = @canceladoPor,
                    MotivoCancelamento = @motivoCancelamento
                WHERE Id = @id;";
            update.Parameters.AddWithValue("@id", vendaId.ToString());
            update.Parameters.AddWithValue("@dataCancelamento", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            update.Parameters.AddWithValue("@canceladoPor", global::PrimoAutoEletrica.App.Session.UserName);
            update.Parameters.AddWithValue("@motivoCancelamento", motivo.Trim());
            update.ExecuteNonQuery();

            transaction.Commit();
            RegistrarAuditoriaEstoque(auditoriasEstoque, venda.Id, "EstornoVendaProduto", $"Venda={venda.Id}; Motivo={motivo}");
            Logger.LogInfo($"Venda '{vendaId}' cancelada com sucesso.");
            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "PDV",
                acao: "VendaCanceladaCompleta",
                entidade: "Venda",
                entidadeId: vendaId.ToString(),
                detalhes: $"Total={venda.Total:C}; Motivo={motivo}; ReverteuEstoque={reverterEstoque}; FormaPagamento={venda.FormaPagamento}",
                valorAnterior: venda.Total.ToString("F2"),
                valorNovo: "0.00");

            try
            {
                App.SendLocalSyncMessage($"VendaCancelada:{vendaId}:{venda.Total}");
            }
            catch { }
        }

        public List<Venda> ObterVendas(DateTime? inicio = null, DateTime? fim = null)
        {
            return _vendaRepository.ObterVendas(inicio, fim);
        }

        public List<Venda> ObterHistoricoOperacional(
            int limite = 50,
            Guid? caixaSessaoId = null,
            DateTime? inicio = null,
            bool incluirCanceladas = true)
        {
            return _vendaRepository.ObterHistoricoOperacional(limite, caixaSessaoId, inicio, incluirCanceladas);
        }

        public Venda? ObterVendaPorId(Guid vendaId)
        {
            return _vendaRepository.ObterVendaInterna(_databaseService.GetConnection(), null, vendaId);
        }

        private static void PrepararEValidarVenda(Venda venda, IReadOnlyDictionary<Guid, int> reservasAtivas)
        {
            if (venda == null)
            {
                throw new ArgumentNullException(nameof(venda));
            }

            if (venda.Id == Guid.Empty)
            {
                venda.Id = Guid.NewGuid();
            }

            venda.Data = venda.Data == default ? DateTime.Now : venda.Data;
            venda.FormaPagamento = ComercialValidationHelper.NormalizarTexto(venda.FormaPagamento, "Dinheiro");
            venda.Usuario = ComercialValidationHelper.NormalizarTexto(venda.Usuario, "Sistema");
            venda.Status = ComercialValidationHelper.NormalizarTexto(venda.Status, "Concluida");
            venda.Itens ??= new List<ItemVenda>();

            if (venda.Itens.Count == 0)
            {
                throw new InvalidOperationException("Nao e possivel registrar uma venda sem itens.");
            }

            decimal subtotalItens = 0;

            foreach (var item in venda.Itens)
            {
                item.Tipo = NormalizarTipoVendaItem(item.Tipo);
                item.Descricao = ComercialValidationHelper.NormalizarTexto(item.Descricao, item.Produto?.Nome ?? "Item");
                item.ProdutoId ??= item.Produto?.Id;
                item.CustoUnitario = Math.Max(0m, item.CustoUnitario);

                ComercialValidationHelper.GarantirQuantidadeInteiraPositiva(item.Quantidade, $"a quantidade do item '{item.NomeExibicao}'");
                ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.PrecoUnitario, $"o preco unitario do item '{item.NomeExibicao}'");
                ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.Desconto, $"o desconto do item '{item.NomeExibicao}'");
                ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.CustoUnitario, $"o custo do item '{item.NomeExibicao}'");

                var subtotalItem = ComercialValidationHelper.CalcularSubtotal(item.Quantidade, item.PrecoUnitario, item.Desconto);
                subtotalItens += subtotalItem;

                if (!item.UsaEstoque)
                {
                    continue;
                }

                if (item.Produto == null || !item.ProdutoId.HasValue)
                {
                    throw new InvalidOperationException("Existe item de venda com estoque sem produto associado.");
                }

                item.Produto.Nome = ComercialValidationHelper.NormalizarTexto(item.Produto.Nome, "Produto");

                var quantidadeReservada = reservasAtivas.TryGetValue(item.ProdutoId.Value, out var reservada)
                    ? reservada
                    : 0;
                var quantidadeDisponivel = item.Produto.QuantidadeEstoque - quantidadeReservada;
                item.Produto.QuantidadeReservada = quantidadeReservada;

                if (item.Quantidade > quantidadeDisponivel)
                {
                    throw new InvalidOperationException($"Estoque insuficiente para o produto '{item.Produto.Nome}'. Disponivel: {quantidadeDisponivel}.");
                }
            }

            ComercialValidationHelper.GarantirDescontoValido(venda.Desconto, subtotalItens, "O desconto geral");
            venda.Total = subtotalItens - venda.Desconto;
            ComercialValidationHelper.GarantirValorMaiorQueZero(venda.Total, "o total da venda");
        }

        private static void InserirItemVenda(SqliteConnection connection, SqliteTransaction transaction, Guid vendaId, ItemVenda item)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO VendaItens
                (
                    Id,
                    VendaId,
                    ProdutoId,
                    Tipo,
                    DescricaoItem,
                    ProdutoNome,
                    Quantidade,
                    PrecoUnitario,
                    CustoUnitario,
                    Desconto,
                    Subtotal
                )
                VALUES
                (
                    @Id,
                    @VendaId,
                    @ProdutoId,
                    @Tipo,
                    @DescricaoItem,
                    @ProdutoNome,
                    @Quantidade,
                    @PrecoUnitario,
                    @CustoUnitario,
                    @Desconto,
                    @Subtotal
                );";

            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("@VendaId", vendaId.ToString());
            command.Parameters.AddWithValue("@ProdutoId", item.ProdutoId.HasValue ? item.ProdutoId.Value.ToString() : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Tipo", NormalizarTipoVendaItem(item.Tipo));
            command.Parameters.AddWithValue("@DescricaoItem", string.IsNullOrWhiteSpace(item.Descricao) ? (object)DBNull.Value : item.Descricao);
            command.Parameters.AddWithValue("@ProdutoNome", item.NomeExibicao);
            command.Parameters.AddWithValue("@Quantidade", item.Quantidade);
            command.Parameters.AddWithValue("@PrecoUnitario", item.PrecoUnitario);
            command.Parameters.AddWithValue("@CustoUnitario", item.CustoUnitario);
            command.Parameters.AddWithValue("@Desconto", item.Desconto);
            command.Parameters.AddWithValue("@Subtotal", item.Subtotal);
            command.ExecuteNonQuery();
        }

        private static EstoqueAuditoriaItem AtualizarEstoqueProduto(SqliteConnection connection, SqliteTransaction transaction, ItemVenda item)
        {
            if (item.Produto == null || !item.ProdutoId.HasValue)
            {
                throw new InvalidOperationException("Item de venda sem produto valido para atualizar estoque.");
            }

            var produtoAtual = ObterProdutoEstoqueAtual(connection, transaction, item.ProdutoId.Value)
                ?? throw new InvalidOperationException($"Produto '{item.NomeExibicao}' nao foi localizado para atualizar estoque.");

            var quantidadeDisponivel = produtoAtual.quantidadeEstoque - produtoAtual.quantidadeReservada;
            if (item.Quantidade > quantidadeDisponivel)
            {
                throw new InvalidOperationException($"Estoque insuficiente para o produto '{produtoAtual.nome}'. Disponivel: {quantidadeDisponivel}.");
            }

            var novaQuantidade = produtoAtual.quantidadeEstoque - item.Quantidade;
            var valorTotalEstoque = novaQuantidade * produtoAtual.precoCompra;

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Produtos
                SET
                    QuantidadeEstoque = @QuantidadeEstoque,
                    ValorTotalEstoque = @ValorTotalEstoque,
                    DataUltimaVenda = @DataUltimaVenda,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao,
                    TotalVendas = TotalVendas + @QuantidadeVendida,
                    VendasUltimoMes = VendasUltimoMes + @QuantidadeVendida,
                    VendasUltimoTrimestre = VendasUltimoTrimestre + @QuantidadeVendida
                WHERE Id = @Id;";

            var dataAtual = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            command.Parameters.AddWithValue("@QuantidadeEstoque", novaQuantidade);
            command.Parameters.AddWithValue("@ValorTotalEstoque", valorTotalEstoque);
            command.Parameters.AddWithValue("@DataUltimaVenda", dataAtual);
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", dataAtual);
            command.Parameters.AddWithValue("@QuantidadeVendida", item.Quantidade);
            command.Parameters.AddWithValue("@Id", item.ProdutoId.Value.ToString());
            command.ExecuteNonQuery();

            return new EstoqueAuditoriaItem(
                item.ProdutoId.Value,
                produtoAtual.codigo,
                produtoAtual.nome,
                produtoAtual.precoCompra,
                produtoAtual.quantidadeEstoque,
                novaQuantidade,
                produtoAtual.quantidadeReservada,
                produtoAtual.quantidadeReservada,
                item.Quantidade,
                "SaidaVendaProduto");
        }

        private static EstoqueAuditoriaItem ReverterEstoqueProduto(SqliteConnection connection, SqliteTransaction transaction, ItemVenda item)
        {
            if (item.Produto == null || !item.ProdutoId.HasValue)
            {
                throw new InvalidOperationException("Item de venda sem produto valido para reverter estoque.");
            }

            var produtoAtual = ObterProdutoEstoqueAtual(connection, transaction, item.ProdutoId.Value)
                ?? throw new InvalidOperationException($"Produto '{item.NomeExibicao}' nao foi localizado para reverter estoque.");

            var novaQuantidade = produtoAtual.quantidadeEstoque + item.Quantidade;
            var valorTotalEstoque = novaQuantidade * produtoAtual.precoCompra;

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Produtos
                SET
                    QuantidadeEstoque = @QuantidadeEstoque,
                    ValorTotalEstoque = @ValorTotalEstoque,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao
                WHERE Id = @Id;";

            command.Parameters.AddWithValue("@QuantidadeEstoque", novaQuantidade);
            command.Parameters.AddWithValue("@ValorTotalEstoque", valorTotalEstoque);
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Id", item.ProdutoId.Value.ToString());
            command.ExecuteNonQuery();

            return new EstoqueAuditoriaItem(
                item.ProdutoId.Value,
                produtoAtual.codigo,
                produtoAtual.nome,
                produtoAtual.precoCompra,
                produtoAtual.quantidadeEstoque,
                novaQuantidade,
                produtoAtual.quantidadeReservada,
                produtoAtual.quantidadeReservada,
                item.Quantidade,
                "EstornoVendaProduto");
        }

        private static List<ItemVenda> ObterItensDaVenda(SqliteConnection connection, Guid vendaId)
        {
            var itens = new List<ItemVenda>();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    ProdutoId,
                    COALESCE(Tipo, 'Produto'),
                    DescricaoItem,
                    ProdutoNome,
                    Quantidade,
                    PrecoUnitario,
                    CustoUnitario,
                    Desconto
                FROM VendaItens
                WHERE VendaId = @VendaId
                ORDER BY ProdutoNome;";

            command.Parameters.AddWithValue("@VendaId", vendaId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                Guid? produtoId = reader.IsDBNull(0) ? null : Guid.Parse(reader.GetString(0));
                var tipo = NormalizarTipoVendaItem(reader.IsDBNull(1) ? "Produto" : reader.GetString(1));
                var descricao = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var nomeProduto = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);

                itens.Add(new ItemVenda
                {
                    ProdutoId = produtoId,
                    Tipo = tipo,
                    Descricao = string.IsNullOrWhiteSpace(descricao) ? nomeProduto : descricao,
                    Produto = produtoId.HasValue
                        ? new Produto
                        {
                            Id = produtoId.Value,
                            Nome = nomeProduto,
                            PrecoCompra = reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetDouble(6))
                        }
                        : null,
                    Quantidade = reader.GetInt32(4),
                    PrecoUnitario = Convert.ToDecimal(reader.GetDouble(5)),
                    CustoUnitario = reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetDouble(6)),
                    Desconto = Convert.ToDecimal(reader.GetDouble(7))
                });
            }

            return itens;
        }

        private static Venda? ObterVendaInterna(SqliteConnection connection, SqliteTransaction? transaction, Guid vendaId)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    Id,
                    Data,
                    ClienteId,
                    ClienteNome,
                    Total,
                    FormaPagamento,
                    Desconto,
                    Usuario,
                    COALESCE(Status, 'Concluida'),
                    CaixaSessaoId,
                    DataCancelamento,
                    CanceladoPor,
                    MotivoCancelamento
                FROM Vendas
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", vendaId.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new Venda
            {
                Id = Guid.Parse(reader.GetString(0)),
                Data = DateTime.Parse(reader.GetString(1)),
                Cliente = reader.IsDBNull(3)
                    ? null
                    : new Cliente
                    {
                        Id = reader.IsDBNull(2) ? Guid.Empty : Guid.Parse(reader.GetString(2)),
                        Nome = reader.GetString(3)
                    },
                Itens = ObterItensDaVenda(connection, vendaId),
                Total = Convert.ToDecimal(reader.GetDouble(4)),
                FormaPagamento = reader.GetString(5),
                Desconto = Convert.ToDecimal(reader.GetDouble(6)),
                Usuario = reader.GetString(7),
                Status = reader.IsDBNull(8) ? "Concluida" : reader.GetString(8),
                CaixaSessaoId = reader.IsDBNull(9) ? null : Guid.Parse(reader.GetString(9)),
                DataCancelamento = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10)),
                CanceladoPor = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                MotivoCancelamento = reader.IsDBNull(12) ? string.Empty : reader.GetString(12)
            };
        }

        private static void RegistrarAuditoriaEstoque(
            IEnumerable<EstoqueAuditoriaItem> auditorias,
            Guid vendaId,
            string acao,
            string detalhesBase)
        {
            foreach (var auditoria in auditorias)
            {
                global::PrimoAutoEletrica.App.Audit.Registrar(
                    categoria: "Estoque",
                    acao: acao,
                    entidade: "Produto",
                    entidadeId: auditoria.ProdutoId.ToString(),
                    detalhes: $"{detalhesBase}; Produto={auditoria.Nome}; Quantidade={auditoria.Quantidade}",
                    valorAnterior: EstoqueOperationalService.CriarSnapshot(auditoria.Codigo, auditoria.Nome, auditoria.PrecoCompra, auditoria.EstoqueAnterior, auditoria.ReservadoAnterior),
                    valorNovo: EstoqueOperationalService.CriarSnapshot(auditoria.Codigo, auditoria.Nome, auditoria.PrecoCompra, auditoria.EstoqueNovo, auditoria.ReservadoNovo),
                    correlationId: vendaId.ToString("N"));
            }
        }

        private static string NormalizarTipoVendaItem(string? tipo)
        {
            return string.Equals(tipo, "Servico", StringComparison.OrdinalIgnoreCase)
                ? "Servico"
                : "Produto";
        }

        private static (int quantidadeEstoque, decimal precoCompra, int quantidadeReservada, string codigo, string nome)? ObterProdutoEstoqueAtual(SqliteConnection connection, SqliteTransaction transaction, Guid produtoId)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT QuantidadeEstoque, PrecoCompra, Codigo, Nome
                FROM Produtos
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", produtoId.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var quantidade = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var precoCompra = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetDouble(1));
            var codigo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var nome = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            var quantidadeReservada = ObterQuantidadeReservadaAtiva(connection, transaction, produtoId);
            return (quantidade, precoCompra, quantidadeReservada, codigo, nome);
        }

        private static int ObterQuantidadeReservadaAtiva(SqliteConnection connection, SqliteTransaction transaction, Guid produtoId)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT COALESCE(SUM(ap.Quantidade), 0)
                FROM AgendamentoProdutos ap
                INNER JOIN Agendamentos a ON a.Id = ap.AgendamentoId
                WHERE ap.ProdutoId = @ProdutoId
                  AND ap.Reservado = 1
                  AND ap.Quantidade > 0
                  AND COALESCE(a.Status, '') NOT IN ('Cancelado', 'Finalizado');";
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }
    }
}
