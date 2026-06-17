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

        private static void InserirItemVenda(DbConnection connection, DbTransaction transaction, Guid vendaId, ItemVenda item)
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

        private static EstoqueAuditoriaItem AtualizarEstoqueProduto(DbConnection connection, DbTransaction transaction, ItemVenda item)
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

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Produtos
                SET
                    QuantidadeEstoque = QuantidadeEstoque - @QuantidadeVendida,
                    ValorTotalEstoque = (QuantidadeEstoque - @QuantidadeVendida) * @PrecoCompra,
                    DataUltimaVenda = @DataUltimaVenda,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao,
                    TotalVendas = TotalVendas + @QuantidadeVendida,
                    VendasUltimoMes = VendasUltimoMes + @QuantidadeVendida,
                    VendasUltimoTrimestre = VendasUltimoTrimestre + @QuantidadeVendida,
                    RowVersion = COALESCE(RowVersion, 0) + 1,
                    DataUltimaAlteracao = @DataUltimaAtualizacao
                WHERE Id = @Id
                  AND QuantidadeEstoque >= (@QuantidadeVendida + @QuantidadeReservadaAtual);";

            var dataAtual = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            command.Parameters.AddWithValue("@PrecoCompra", produtoAtual.precoCompra);
            command.Parameters.AddWithValue("@DataUltimaVenda", dataAtual);
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", dataAtual);
            command.Parameters.AddWithValue("@QuantidadeVendida", item.Quantidade);
            command.Parameters.AddWithValue("@QuantidadeReservadaAtual", produtoAtual.quantidadeReservada);
            command.Parameters.AddWithValue("@Id", item.ProdutoId.Value.ToString());
            var linhasAfetadas = command.ExecuteNonQuery();
            if (linhasAfetadas != 1)
            {
                throw new InvalidOperationException($"Estoque insuficiente ou alterado por outra estacao para o produto '{produtoAtual.nome}'. Atualize o PDV e tente novamente.");
            }

            var novaQuantidade = produtoAtual.quantidadeEstoque - item.Quantidade;

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

        private static EstoqueAuditoriaItem ReverterEstoqueProduto(DbConnection connection, DbTransaction transaction, ItemVenda item)
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

        private static List<ItemVenda> ObterItensDaVenda(DbConnection connection, Guid vendaId, DbTransaction? transaction = null)
        {
            var itens = new List<ItemVenda>();

            var command = connection.CreateCommand();
            command.Transaction = transaction;
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
                Guid? produtoId = ReadNullableGuid(reader, 0);
                var tipo = NormalizarTipoVendaItem(ReadString(reader, 1, "Produto"));
                var descricao = ReadString(reader, 2);
                var nomeProduto = ReadString(reader, 3);

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
                            PrecoCompra = ReadDecimal(reader, 6)
                        }
                        : null,
                    Quantidade = ReadInt(reader, 4),
                    PrecoUnitario = ReadDecimal(reader, 5),
                    CustoUnitario = ReadDecimal(reader, 6),
                    Desconto = ReadDecimal(reader, 7)
                });
            }

            return itens;
        }

        private static Venda? ObterVendaInterna(DbConnection connection, DbTransaction? transaction, Guid vendaId)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT {TopOne(connection)}
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
                {LimitOne(connection)};";
            command.Parameters.AddWithValue("@Id", vendaId.ToString());

            Venda venda;
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                venda = new Venda
                {
                    Id = ReadGuid(reader, 0),
                    Data = ReadDate(reader, 1),
                    Cliente = reader.IsDBNull(3)
                        ? null
                        : new Cliente
                        {
                            Id = ReadGuid(reader, 2),
                            Nome = ReadString(reader, 3)
                        },
                    Total = ReadDecimal(reader, 4),
                    FormaPagamento = ReadString(reader, 5),
                    Desconto = ReadDecimal(reader, 6),
                    Usuario = ReadString(reader, 7),
                    Status = ReadString(reader, 8, "Concluida"),
                    CaixaSessaoId = ReadNullableGuid(reader, 9),
                    DataCancelamento = ReadNullableDate(reader, 10),
                    CanceladoPor = ReadString(reader, 11),
                    MotivoCancelamento = ReadString(reader, 12)
                };
            }

            venda.Itens = ObterItensDaVenda(connection, vendaId, transaction);
            return venda;
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

        private static (int quantidadeEstoque, decimal precoCompra, int quantidadeReservada, string codigo, string nome)? ObterProdutoEstoqueAtual(DbConnection connection, DbTransaction transaction, Guid produtoId)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT {TopOne(connection)} QuantidadeEstoque, PrecoCompra, Codigo, Nome
                FROM Produtos
                WHERE Id = @Id
                {LimitOne(connection)};";
            command.Parameters.AddWithValue("@Id", produtoId.ToString());

            int quantidade;
            decimal precoCompra;
            string codigo;
            string nome;
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                quantidade = ReadInt(reader, 0);
                precoCompra = ReadDecimal(reader, 1);
                codigo = ReadString(reader, 2);
                nome = ReadString(reader, 3);
            }

            var quantidadeReservada = ObterQuantidadeReservadaAtiva(connection, transaction, produtoId);
            return (quantidade, precoCompra, quantidadeReservada, codigo, nome);
        }

        private static int ObterQuantidadeReservadaAtiva(DbConnection connection, DbTransaction transaction, Guid produtoId)
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

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            var typeName = connection.GetType().FullName ?? string.Empty;
            return typeName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        private static string TopOne(DbConnection connection)
        {
            return IsSqlServerConnection(connection) ? "TOP (1)" : string.Empty;
        }

        private static string LimitOne(DbConnection connection)
        {
            return IsSqlServerConnection(connection) ? string.Empty : "LIMIT 1";
        }

        private static Guid ReadGuid(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return Guid.Empty;
            }

            var value = reader.GetValue(ordinal);
            return value is Guid guid
                ? guid
                : Guid.TryParse(Convert.ToString(value), out var parsed)
                    ? parsed
                    : Guid.Empty;
        }

        private static Guid? ReadNullableGuid(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetValue(ordinal);
            return value is Guid guid
                ? guid
                : Guid.TryParse(Convert.ToString(value), out var parsed)
                    ? parsed
                    : null;
        }

        private static int ReadInt(DbDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static decimal ReadDecimal(DbDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static DateTime ReadDate(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return DateTime.MinValue;
            }

            var value = reader.GetValue(ordinal);
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(Convert.ToString(value), out var parsed) ? parsed : DateTime.MinValue;
        }

        private static DateTime? ReadNullableDate(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetValue(ordinal);
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(Convert.ToString(value), out var parsed) ? parsed : null;
        }

        private static string ReadString(DbDataReader reader, int ordinal, string fallback = "")
        {
            if (reader.IsDBNull(ordinal))
            {
                return fallback;
            }

            var value = Convert.ToString(reader.GetValue(ordinal));
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
