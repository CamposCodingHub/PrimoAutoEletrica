using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Repositories
{
    public class VendaRepository
    {
        private readonly Services.DatabaseService? _databaseService;

        public VendaRepository(Services.DatabaseService? databaseService = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
        }

        public void InserirVenda(SqliteConnection connection, SqliteTransaction transaction, Venda venda)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            var insertVenda = connection.CreateCommand();
            insertVenda.Transaction = transaction;
            insertVenda.CommandText = @"
                    INSERT INTO Vendas
                    (
                        Id,
                        Data,
                        ClienteId,
                        ClienteNome,
                        Total,
                        FormaPagamento,
                        Desconto,
                        Usuario,
                        QuantidadeItens,
                        Status,
                        CaixaSessaoId,
                        DataCancelamento,
                        CanceladoPor,
                        MotivoCancelamento
                    )
                    VALUES
                    (
                        @Id,
                        @Data,
                        @ClienteId,
                        @ClienteNome,
                        @Total,
                        @FormaPagamento,
                        @Desconto,
                        @Usuario,
                        @QuantidadeItens,
                        @Status,
                        @CaixaSessaoId,
                        @DataCancelamento,
                        @CanceladoPor,
                        @MotivoCancelamento
                    );";

            insertVenda.Parameters.AddWithValue("@Id", venda.Id.ToString());
            insertVenda.Parameters.AddWithValue("@Data", venda.Data.ToString("yyyy-MM-dd HH:mm:ss"));
            insertVenda.Parameters.AddWithValue("@ClienteId", venda.Cliente?.Id.ToString() ?? (object)DBNull.Value);
            insertVenda.Parameters.AddWithValue("@ClienteNome", string.IsNullOrWhiteSpace(venda.Cliente?.Nome) ? (object)DBNull.Value : venda.Cliente.Nome);
            insertVenda.Parameters.AddWithValue("@Total", venda.Total);
            insertVenda.Parameters.AddWithValue("@FormaPagamento", venda.FormaPagamento);
            insertVenda.Parameters.AddWithValue("@Desconto", venda.Desconto);
            insertVenda.Parameters.AddWithValue("@Usuario", venda.Usuario);
            insertVenda.Parameters.AddWithValue("@QuantidadeItens", venda.Itens?.Sum(i => i.Quantidade) ?? 0);
            insertVenda.Parameters.AddWithValue("@Status", venda.Status);
            insertVenda.Parameters.AddWithValue("@CaixaSessaoId", venda.CaixaSessaoId.HasValue ? venda.CaixaSessaoId.Value.ToString() : (object)DBNull.Value);
            insertVenda.Parameters.AddWithValue("@DataCancelamento", venda.DataCancelamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            insertVenda.Parameters.AddWithValue("@CanceladoPor", string.IsNullOrWhiteSpace(venda.CanceladoPor) ? (object)DBNull.Value : venda.CanceladoPor);
            insertVenda.Parameters.AddWithValue("@MotivoCancelamento", string.IsNullOrWhiteSpace(venda.MotivoCancelamento) ? (object)DBNull.Value : venda.MotivoCancelamento);
            insertVenda.ExecuteNonQuery();
        }

        public void InserirItemVenda(SqliteConnection connection, SqliteTransaction transaction, Guid vendaId, ItemVenda item)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
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
            command.Parameters.AddWithValue("@Tipo", string.Equals(item.Tipo, "Servico", StringComparison.OrdinalIgnoreCase) ? "Servico" : "Produto");
            command.Parameters.AddWithValue("@DescricaoItem", string.IsNullOrWhiteSpace(item.Descricao) ? (object)DBNull.Value : item.Descricao);
            command.Parameters.AddWithValue("@ProdutoNome", item.NomeExibicao);
            command.Parameters.AddWithValue("@Quantidade", item.Quantidade);
            command.Parameters.AddWithValue("@PrecoUnitario", item.PrecoUnitario);
            command.Parameters.AddWithValue("@CustoUnitario", item.CustoUnitario);
            command.Parameters.AddWithValue("@Desconto", item.Desconto);
            command.Parameters.AddWithValue("@Subtotal", item.Subtotal);
            command.ExecuteNonQuery();
        }

        public List<ItemVenda> ObterItensDaVenda(SqliteConnection connection, Guid vendaId)
        {
            var itens = new List<ItemVenda>();

            if (connection == null || connection.State != System.Data.ConnectionState.Open)
            {
                using var conn = _databaseService!.GetConnection();
                conn.Open();
                var commandLocal = conn.CreateCommand();
                commandLocal.CommandText = @"
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
                commandLocal.Parameters.AddWithValue("@VendaId", vendaId.ToString());
                using var readerLocal = commandLocal.ExecuteReader();
                while (readerLocal.Read())
                {
                    Guid? produtoId = readerLocal.IsDBNull(0) ? null : Guid.Parse(readerLocal.GetString(0));
                    var tipo = readerLocal.IsDBNull(1) ? "Produto" : readerLocal.GetString(1);
                    var descricao = readerLocal.IsDBNull(2) ? string.Empty : readerLocal.GetString(2);
                    var nomeProduto = readerLocal.IsDBNull(3) ? string.Empty : readerLocal.GetString(3);

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
                                PrecoCompra = readerLocal.IsDBNull(6) ? 0m : Convert.ToDecimal(readerLocal.GetDouble(6))
                            }
                            : null,
                        Quantidade = readerLocal.IsDBNull(4) ? 0 : readerLocal.GetInt32(4),
                        PrecoUnitario = readerLocal.IsDBNull(5) ? 0m : Convert.ToDecimal(readerLocal.GetDouble(5)),
                        CustoUnitario = readerLocal.IsDBNull(6) ? 0m : Convert.ToDecimal(readerLocal.GetDouble(6)),
                        Desconto = readerLocal.IsDBNull(7) ? 0m : Convert.ToDecimal(readerLocal.GetDouble(7))
                    });
                }
                return itens;
            }

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
                var tipo = reader.IsDBNull(1) ? "Produto" : reader.GetString(1);
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
                    Quantidade = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    PrecoUnitario = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetDouble(5)),
                    CustoUnitario = reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetDouble(6)),
                    Desconto = reader.IsDBNull(7) ? 0m : Convert.ToDecimal(reader.GetDouble(7))
                });
            }

            return itens;
        }

        public Venda? ObterVendaInterna(SqliteConnection connection, SqliteTransaction? transaction, Guid vendaId)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
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

        public List<Venda> ObterVendas(DateTime? inicio = null, DateTime? fim = null)
        {
            var vendas = new List<Venda>();

            using var connection = _databaseService!.GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
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
                WHERE (@Inicio IS NULL OR Data >= @Inicio)
                  AND (@Fim IS NULL OR Data <= @Fim)
                ORDER BY Data DESC;";

            command.Parameters.AddWithValue("@Inicio", inicio.HasValue ? inicio.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Fim", fim.HasValue ? fim.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var vendaId = Guid.Parse(reader.GetString(0));
                vendas.Add(new Venda
                {
                    Id = vendaId,
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
                });
            }

            return vendas;
        }

        public List<Venda> ObterHistoricoOperacional(int limite = 50, Guid? caixaSessaoId = null, DateTime? inicio = null, bool incluirCanceladas = true)
        {
            var vendas = new List<Venda>();
            if (limite <= 0)
            {
                return vendas;
            }

            using var connection = _databaseService!.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
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
                WHERE (@Inicio IS NULL OR Data >= @Inicio)
                  AND (@CaixaSessaoId IS NULL OR CaixaSessaoId = @CaixaSessaoId)
                  AND (@IncluirCanceladas = 1 OR COALESCE(Status, 'Concluida') <> 'Cancelada')
                ORDER BY Data DESC
                LIMIT @Limite;";
            command.Parameters.AddWithValue("@Inicio", inicio.HasValue
                ? inicio.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : (object)DBNull.Value);
            command.Parameters.AddWithValue("@CaixaSessaoId", caixaSessaoId.HasValue
                ? caixaSessaoId.Value.ToString()
                : (object)DBNull.Value);
            command.Parameters.AddWithValue("@IncluirCanceladas", incluirCanceladas ? 1 : 0);
            command.Parameters.AddWithValue("@Limite", limite);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var vendaId = Guid.Parse(reader.GetString(0));
                vendas.Add(new Venda
                {
                    Id = vendaId,
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
                });
            }

            return vendas;
        }
    }
}
