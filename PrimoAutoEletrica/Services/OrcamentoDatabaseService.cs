using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public class OrcamentoDatabaseService
    {
        private const string OrcamentoColumns = @"
            Id,
            ClienteId,
            VendedorId,
            Numero,
            Status,
            DataCriacao,
            DataValidade,
            DataAprovacao,
            DataConversaoVenda,
            DataConversaoOrdemServico,
            OrdemServicoId,
            Subtotal,
            Desconto,
            Acrescimo,
            Total,
            MargemLucro,
            LucroEstimado,
            ComissaoVendedor,
            ImpostosEstimados,
            Observacoes,
            CondicoesPagamento,
            PrazoEntrega";

        private const string OrcamentoItemColumns = @"
            Id,
            OrcamentoId,
            ProdutoId,
            Tipo,
            ProdutoNome,
            ProdutoCodigo,
            ProdutoCategoria,
            ProdutoMarca,
            ProdutoAplicacao,
            Quantidade,
            PrecoUnitario,
            PrecoCusto,
            Desconto,
            Subtotal,
            LucroEstimado,
            MargemLucro,
            EstoqueDisponivel,
            Observacoes";

        private readonly DatabaseService _databaseService;

        public OrcamentoDatabaseService()
        {
            _databaseService = global::PrimoAutoEletrica.App.Database;

            using var connection = GetConnection();
            connection.Open();

            InicializarBancoDeDados(connection);
            MigrarDadosLegados(connection);
            NormalizarNumeracaoOrcamentos(connection);
        }

        private SqliteConnection GetConnection()
        {
            return _databaseService.GetConnection();
        }

        private void InicializarBancoDeDados(SqliteConnection connection)
        {
            using var commandOrcamentos = connection.CreateCommand();
            commandOrcamentos.CommandText = @"
                CREATE TABLE IF NOT EXISTS Orcamentos (
                    Id TEXT PRIMARY KEY,
                    ClienteId TEXT,
                    VendedorId TEXT,
                    Numero TEXT,
                    Status TEXT,
                    DataCriacao TEXT,
                    DataValidade TEXT,
                    DataAprovacao TEXT,
                    DataConversaoVenda TEXT,
                    DataConversaoOrdemServico TEXT,
                    OrdemServicoId TEXT,
                    Subtotal REAL,
                    Desconto REAL,
                    Acrescimo REAL,
                    Total REAL,
                    MargemLucro REAL,
                    LucroEstimado REAL,
                    ComissaoVendedor REAL,
                    ImpostosEstimados REAL,
                    Observacoes TEXT,
                    CondicoesPagamento TEXT,
                    PrazoEntrega TEXT
                )";
            commandOrcamentos.ExecuteNonQuery();

            EnsureColumnExists(connection, "Orcamentos", "DataConversaoOrdemServico", "ALTER TABLE Orcamentos ADD COLUMN DataConversaoOrdemServico TEXT;");
            EnsureColumnExists(connection, "Orcamentos", "OrdemServicoId", "ALTER TABLE Orcamentos ADD COLUMN OrdemServicoId TEXT;");

            using var commandItens = connection.CreateCommand();
            commandItens.CommandText = @"
                CREATE TABLE IF NOT EXISTS OrcamentoItens (
                    Id TEXT PRIMARY KEY,
                    OrcamentoId TEXT,
                    ProdutoId TEXT,
                    Tipo TEXT NOT NULL DEFAULT 'Produto',
                    ProdutoNome TEXT,
                    ProdutoCodigo TEXT,
                    ProdutoCategoria TEXT,
                    ProdutoMarca TEXT,
                    ProdutoAplicacao TEXT,
                    Quantidade INTEGER,
                    PrecoUnitario REAL,
                    PrecoCusto REAL,
                    Desconto REAL,
                    Subtotal REAL,
                    LucroEstimado REAL,
                    MargemLucro REAL,
                    EstoqueDisponivel INTEGER,
                    Observacoes TEXT,
                    FOREIGN KEY (OrcamentoId) REFERENCES Orcamentos(Id)
                )";
            commandItens.ExecuteNonQuery();

            EnsureColumnExists(connection, "OrcamentoItens", "Tipo", "ALTER TABLE OrcamentoItens ADD COLUMN Tipo TEXT NOT NULL DEFAULT 'Produto';");
        }

        private void MigrarDadosLegados(SqliteConnection connection)
        {
            var legacyPath = ObterCaminhoBancoLegado();
            if (string.IsNullOrWhiteSpace(legacyPath) || !File.Exists(legacyPath))
            {
                return;
            }

            if (string.Equals(
                Path.GetFullPath(connection.DataSource),
                Path.GetFullPath(legacyPath),
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using var attachCommand = connection.CreateCommand();
            attachCommand.CommandText = "ATTACH DATABASE @path AS legado;";
            attachCommand.Parameters.AddWithValue("@path", legacyPath);
            attachCommand.ExecuteNonQuery();

            try
            {
                CopiarTabelaLegada(connection, "Orcamentos", new[]
                {
                    "Id",
                    "ClienteId",
                    "VendedorId",
                    "Numero",
                    "Status",
                    "DataCriacao",
                    "DataValidade",
                    "DataAprovacao",
                    "DataConversaoVenda",
                    "DataConversaoOrdemServico",
                    "OrdemServicoId",
                    "Subtotal",
                    "Desconto",
                    "Acrescimo",
                    "Total",
                    "MargemLucro",
                    "LucroEstimado",
                    "ComissaoVendedor",
                    "ImpostosEstimados",
                    "Observacoes",
                    "CondicoesPagamento",
                    "PrazoEntrega"
                });

                CopiarTabelaLegada(connection, "OrcamentoItens", new[]
                {
                    "Id",
                    "OrcamentoId",
                    "ProdutoId",
                    "ProdutoNome",
                    "ProdutoCodigo",
                    "ProdutoCategoria",
                    "ProdutoMarca",
                    "ProdutoAplicacao",
                    "Quantidade",
                    "PrecoUnitario",
                    "PrecoCusto",
                    "Desconto",
                    "Subtotal",
                    "LucroEstimado",
                    "MargemLucro",
                    "EstoqueDisponivel",
                    "Observacoes"
                });
            }
            finally
            {
                using var detachCommand = connection.CreateCommand();
                detachCommand.CommandText = "DETACH DATABASE legado;";
                detachCommand.ExecuteNonQuery();
            }
        }

        private static void CopiarTabelaLegada(SqliteConnection connection, string tableName, IReadOnlyCollection<string> preferredColumns)
        {
            if (!TabelaExiste(connection, "legado", tableName) ||
                !TabelaTemDados(connection, "legado", tableName) ||
                TabelaTemDados(connection, "main", tableName))
            {
                return;
            }

            var colunasMain = ObterColunasTabela(connection, "main", tableName);
            var colunasLegado = ObterColunasTabela(connection, "legado", tableName);
            var colunasComuns = preferredColumns
                .Where(coluna =>
                    colunasMain.Contains(coluna, StringComparer.OrdinalIgnoreCase) &&
                    colunasLegado.Contains(coluna, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (colunasComuns.Count == 0)
            {
                return;
            }

            var listaColunas = string.Join(", ", colunasComuns);
            using var copyCommand = connection.CreateCommand();
            copyCommand.CommandText = $@"
                INSERT INTO main.{tableName} ({listaColunas})
                SELECT {listaColunas}
                FROM legado.{tableName};";
            copyCommand.ExecuteNonQuery();
        }

        private static List<string> ObterColunasTabela(SqliteConnection connection, string schema, string tableName)
        {
            var colunas = new List<string>();
            using var pragma = connection.CreateCommand();
            pragma.CommandText = $"PRAGMA {schema}.table_info({tableName});";

            using var reader = pragma.ExecuteReader();
            while (reader.Read())
            {
                colunas.Add(reader.GetString(1));
            }

            return colunas;
        }

        private static string? ObterCaminhoBancoLegado()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "primoeletrica.db"),
                Path.Combine(Environment.CurrentDirectory, "primoeletrica.db")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool TabelaExiste(SqliteConnection connection, string schema, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT 1
                FROM {schema}.sqlite_master
                WHERE type = 'table'
                  AND name = @name
                LIMIT 1;";
            command.Parameters.AddWithValue("@name", tableName);
            return command.ExecuteScalar() != null;
        }

        private static bool TabelaTemDados(SqliteConnection connection, string schema, string tableName)
        {
            if (!TabelaExiste(connection, schema, tableName))
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1 FROM {schema}.{tableName} LIMIT 1;";
            return command.ExecuteScalar() != null;
        }

        public void AdicionarOrcamento(Orcamento orcamento)
        {
            PrepararEValidarOrcamento(orcamento);

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                GarantirNumeroUnico(connection, transaction, orcamento);

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $@"
                    INSERT INTO Orcamentos
                    (
                        {OrcamentoColumns}
                    )
                    VALUES
                    (
                        @Id,
                        @ClienteId,
                        @VendedorId,
                        @Numero,
                        @Status,
                        @DataCriacao,
                        @DataValidade,
                        @DataAprovacao,
                        @DataConversaoVenda,
                        @DataConversaoOrdemServico,
                        @OrdemServicoId,
                        @Subtotal,
                        @Desconto,
                        @Acrescimo,
                        @Total,
                        @MargemLucro,
                        @LucroEstimado,
                        @ComissaoVendedor,
                        @ImpostosEstimados,
                        @Observacoes,
                        @CondicoesPagamento,
                        @PrazoEntrega
                    );";

                AddOrcamentoParameters(command, orcamento);
                command.ExecuteNonQuery();

                foreach (var item in orcamento.Itens)
                {
                    InserirOrcamentoItem(connection, transaction, item);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void AdicionarOrcamentoItem(OrcamentoItem item)
        {
            PrepararEValidarItem(item);

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                InserirOrcamentoItem(connection, transaction, item);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<Orcamento> ObterTodosOrcamentos()
        {
            using var connection = GetConnection();
            connection.Open();
            var clientesPorId = CarregarClientesPorId();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {OrcamentoColumns}
                FROM Orcamentos
                ORDER BY DataCriacao DESC;";

            var orcamentos = new List<Orcamento>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var orcamento = MaterializarOrcamento(reader);
                orcamento.Itens = ObterItensDoOrcamento(orcamento.Id);
                HidratarCliente(orcamento, clientesPorId);
                orcamentos.Add(orcamento);
            }

            return orcamentos;
        }

        public List<OrcamentoItem> ObterItensDoOrcamento(Guid orcamentoId)
        {
            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {OrcamentoItemColumns}
                FROM OrcamentoItens
                WHERE OrcamentoId = @OrcamentoId
                ORDER BY ProdutoNome, Id;";
            command.Parameters.AddWithValue("@OrcamentoId", orcamentoId.ToString());

            var itens = new List<OrcamentoItem>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(MaterializarOrcamentoItem(reader));
            }

            return itens;
        }

        public Orcamento? ObterOrcamentoPorId(Guid id)
        {
            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {OrcamentoColumns}
                FROM Orcamentos
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var orcamento = MaterializarOrcamento(reader);
            orcamento.Itens = ObterItensDoOrcamento(orcamento.Id);
            HidratarCliente(orcamento);
            return orcamento;
        }

        public void AtualizarOrcamento(Orcamento orcamento)
        {
            PrepararEValidarOrcamento(orcamento);

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                GarantirNumeroUnico(connection, transaction, orcamento);

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE Orcamentos
                    SET
                        ClienteId = @ClienteId,
                        VendedorId = @VendedorId,
                        Numero = @Numero,
                        Status = @Status,
                        DataValidade = @DataValidade,
                        DataAprovacao = @DataAprovacao,
                        DataConversaoVenda = @DataConversaoVenda,
                        DataConversaoOrdemServico = @DataConversaoOrdemServico,
                        OrdemServicoId = @OrdemServicoId,
                        Subtotal = @Subtotal,
                        Desconto = @Desconto,
                        Acrescimo = @Acrescimo,
                        Total = @Total,
                        MargemLucro = @MargemLucro,
                        LucroEstimado = @LucroEstimado,
                        ComissaoVendedor = @ComissaoVendedor,
                        ImpostosEstimados = @ImpostosEstimados,
                        Observacoes = @Observacoes,
                        CondicoesPagamento = @CondicoesPagamento,
                        PrazoEntrega = @PrazoEntrega
                    WHERE Id = @Id;";

                AddOrcamentoParameters(command, orcamento);
                command.ExecuteNonQuery();

                RemoverItensDoOrcamento(connection, transaction, orcamento.Id);
                foreach (var item in orcamento.Itens)
                {
                    InserirOrcamentoItem(connection, transaction, item);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void ExcluirOrcamento(Guid id)
        {
            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                RemoverItensDoOrcamento(connection, transaction, id);

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM Orcamentos WHERE Id = @Id;";
                command.Parameters.AddWithValue("@Id", id.ToString());
                command.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public string GerarNumeroOrcamento()
        {
            using var connection = GetConnection();
            connection.Open();
            return GerarNumeroOrcamento(connection, DateTime.Now);
        }

        private static void InserirOrcamentoItem(SqliteConnection connection, SqliteTransaction transaction, OrcamentoItem item)
        {
            PrepararEValidarItem(item);

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                INSERT INTO OrcamentoItens
                (
                    {OrcamentoItemColumns}
                )
                VALUES
                (
                    @Id,
                    @OrcamentoId,
                    @ProdutoId,
                    @Tipo,
                    @ProdutoNome,
                    @ProdutoCodigo,
                    @ProdutoCategoria,
                    @ProdutoMarca,
                    @ProdutoAplicacao,
                    @Quantidade,
                    @PrecoUnitario,
                    @PrecoCusto,
                    @Desconto,
                    @Subtotal,
                    @LucroEstimado,
                    @MargemLucro,
                    @EstoqueDisponivel,
                    @Observacoes
                );";

            command.Parameters.AddWithValue("@Id", item.Id.ToString());
            command.Parameters.AddWithValue("@OrcamentoId", item.OrcamentoId.ToString());
            command.Parameters.AddWithValue("@ProdutoId", item.ProdutoId.HasValue ? item.ProdutoId.Value.ToString() : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Tipo", NormalizarTipoItem(item.Tipo));
            command.Parameters.AddWithValue("@ProdutoNome", ToDbNullableString(item.ProdutoNome));
            command.Parameters.AddWithValue("@ProdutoCodigo", ToDbNullableString(item.ProdutoCodigo));
            command.Parameters.AddWithValue("@ProdutoCategoria", ToDbNullableString(item.ProdutoCategoria));
            command.Parameters.AddWithValue("@ProdutoMarca", ToDbNullableString(item.ProdutoMarca));
            command.Parameters.AddWithValue("@ProdutoAplicacao", ToDbNullableString(item.ProdutoAplicacao));
            command.Parameters.AddWithValue("@Quantidade", item.Quantidade);
            command.Parameters.AddWithValue("@PrecoUnitario", item.PrecoUnitario);
            command.Parameters.AddWithValue("@PrecoCusto", item.PrecoCusto);
            command.Parameters.AddWithValue("@Desconto", item.Desconto);
            command.Parameters.AddWithValue("@Subtotal", item.Subtotal);
            command.Parameters.AddWithValue("@LucroEstimado", item.LucroEstimado);
            command.Parameters.AddWithValue("@MargemLucro", item.MargemLucro);
            command.Parameters.AddWithValue("@EstoqueDisponivel", item.EstoqueDisponivel);
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(item.Observacoes));
            command.ExecuteNonQuery();
        }

        private static void RemoverItensDoOrcamento(SqliteConnection connection, SqliteTransaction transaction, Guid orcamentoId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM OrcamentoItens WHERE OrcamentoId = @OrcamentoId;";
            command.Parameters.AddWithValue("@OrcamentoId", orcamentoId.ToString());
            command.ExecuteNonQuery();
        }

        private static Orcamento MaterializarOrcamento(SqliteDataReader reader)
        {
            return new Orcamento
            {
                Id = ReadGuid(reader, 0),
                ClienteId = ReadNullableGuid(reader, 1),
                VendedorId = ReadNullableGuid(reader, 2),
                Numero = ReadString(reader, 3),
                Status = ReadString(reader, 4),
                DataCriacao = ReadDate(reader, 5, DateTime.Now),
                DataValidade = ReadNullableDate(reader, 6),
                DataAprovacao = ReadNullableDate(reader, 7),
                DataConversaoVenda = ReadNullableDate(reader, 8),
                DataConversaoOrdemServico = ReadNullableDate(reader, 9),
                OrdemServicoId = ReadNullableGuid(reader, 10),
                Subtotal = ReadDecimal(reader, 11),
                Desconto = ReadDecimal(reader, 12),
                Acrescimo = ReadDecimal(reader, 13),
                Total = ReadDecimal(reader, 14),
                MargemLucro = ReadDecimal(reader, 15),
                LucroEstimado = ReadDecimal(reader, 16),
                ComissaoVendedor = ReadDecimal(reader, 17),
                ImpostosEstimados = ReadDecimal(reader, 18),
                Observacoes = ReadString(reader, 19),
                CondicoesPagamento = ReadString(reader, 20),
                PrazoEntrega = ReadString(reader, 21)
            };
        }

        private static OrcamentoItem MaterializarOrcamentoItem(SqliteDataReader reader)
        {
            return new OrcamentoItem
            {
                Id = ReadGuid(reader, 0),
                OrcamentoId = ReadGuid(reader, 1),
                ProdutoId = ReadNullableGuid(reader, 2),
                Tipo = NormalizarTipoItem(ReadString(reader, 3)),
                ProdutoNome = ReadString(reader, 4),
                ProdutoCodigo = ReadString(reader, 5),
                ProdutoCategoria = ReadString(reader, 6),
                ProdutoMarca = ReadString(reader, 7),
                ProdutoAplicacao = ReadString(reader, 8),
                Quantidade = ReadInt(reader, 9),
                PrecoUnitario = ReadDecimal(reader, 10),
                PrecoCusto = ReadDecimal(reader, 11),
                Desconto = ReadDecimal(reader, 12),
                Subtotal = ReadDecimal(reader, 13),
                LucroEstimado = ReadDecimal(reader, 14),
                MargemLucro = ReadDecimal(reader, 15),
                EstoqueDisponivel = ReadInt(reader, 16),
                Observacoes = ReadString(reader, 17)
            };
        }

        private static void AddOrcamentoParameters(SqliteCommand command, Orcamento orcamento)
        {
            command.Parameters.AddWithValue("@Id", orcamento.Id.ToString());
            command.Parameters.AddWithValue("@ClienteId", ToDbNullableString(orcamento.ClienteId?.ToString()));
            command.Parameters.AddWithValue("@VendedorId", ToDbNullableString(orcamento.VendedorId?.ToString()));
            command.Parameters.AddWithValue("@Numero", orcamento.Numero);
            command.Parameters.AddWithValue("@Status", orcamento.Status);
            command.Parameters.AddWithValue("@DataCriacao", orcamento.DataCriacao.ToString("o"));
            command.Parameters.AddWithValue("@DataValidade", ToDbNullableString(orcamento.DataValidade?.ToString("o")));
            command.Parameters.AddWithValue("@DataAprovacao", ToDbNullableString(orcamento.DataAprovacao?.ToString("o")));
            command.Parameters.AddWithValue("@DataConversaoVenda", ToDbNullableString(orcamento.DataConversaoVenda?.ToString("o")));
            command.Parameters.AddWithValue("@DataConversaoOrdemServico", ToDbNullableString(orcamento.DataConversaoOrdemServico?.ToString("o")));
            command.Parameters.AddWithValue("@OrdemServicoId", ToDbNullableString(orcamento.OrdemServicoId?.ToString()));
            command.Parameters.AddWithValue("@Subtotal", orcamento.Subtotal);
            command.Parameters.AddWithValue("@Desconto", orcamento.Desconto);
            command.Parameters.AddWithValue("@Acrescimo", orcamento.Acrescimo);
            command.Parameters.AddWithValue("@Total", orcamento.Total);
            command.Parameters.AddWithValue("@MargemLucro", orcamento.MargemLucro);
            command.Parameters.AddWithValue("@LucroEstimado", orcamento.LucroEstimado);
            command.Parameters.AddWithValue("@ComissaoVendedor", orcamento.ComissaoVendedor);
            command.Parameters.AddWithValue("@ImpostosEstimados", orcamento.ImpostosEstimados);
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(orcamento.Observacoes));
            command.Parameters.AddWithValue("@CondicoesPagamento", ToDbNullableString(orcamento.CondicoesPagamento));
            command.Parameters.AddWithValue("@PrazoEntrega", ToDbNullableString(orcamento.PrazoEntrega));
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static Dictionary<Guid, Cliente> CarregarClientesPorId()
        {
            try
            {
                return global::PrimoAutoEletrica.App.Repositories.Clientes
                    .ObterTodos()
                    .ToDictionary(cliente => cliente.Id, cliente => cliente);
            }
            catch
            {
                return new Dictionary<Guid, Cliente>();
            }
        }

        private static void HidratarCliente(Orcamento orcamento, IReadOnlyDictionary<Guid, Cliente> clientesPorId)
        {
            if (orcamento.Cliente != null || !orcamento.ClienteId.HasValue)
            {
                return;
            }

            if (clientesPorId.TryGetValue(orcamento.ClienteId.Value, out var cliente))
            {
                orcamento.Cliente = cliente;
            }
        }

        private static void HidratarCliente(Orcamento orcamento)
        {
            if (orcamento.Cliente != null || !orcamento.ClienteId.HasValue)
            {
                return;
            }

            try
            {
                orcamento.Cliente = global::PrimoAutoEletrica.App.Repositories.Clientes.ObterPorId(orcamento.ClienteId.Value);
            }
            catch
            {
                // Ignora falhas de hidratacao para nao bloquear a listagem comercial.
            }
        }

        private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string alterSql)
        {
            using var pragma = connection.CreateCommand();
            pragma.CommandText = $"PRAGMA table_info({tableName});";

            using var reader = pragma.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = alterSql;
            alterCommand.ExecuteNonQuery();
        }

        private static void NormalizarNumeracaoOrcamentos(SqliteConnection connection)
        {
            var registros = new List<(Guid Id, DateTime DataCriacao, string Numero)>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT Id, Numero, DataCriacao
                    FROM Orcamentos
                    ORDER BY DataCriacao, Id;";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    registros.Add((
                        Guid.Parse(reader.GetString(0)),
                        reader.IsDBNull(2) ? DateTime.Now : DateTime.Parse(reader.GetString(2)),
                        reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
                }
            }

            if (registros.Count == 0)
            {
                return;
            }

            var usadosPorPrefixo = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            var maximosPorPrefixo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var atualizacoes = new List<(Guid Id, string Numero)>();

            foreach (var registro in registros)
            {
                var prefixo = GerarPrefixoNumero(registro.DataCriacao);
                if (!usadosPorPrefixo.TryGetValue(prefixo, out var usados))
                {
                    usados = new HashSet<int>();
                    usadosPorPrefixo[prefixo] = usados;
                    maximosPorPrefixo[prefixo] = 0;
                }

                if (NumeroCompativelComData(registro.Numero, registro.DataCriacao, out var sequenciaAtual) &&
                    !usados.Contains(sequenciaAtual))
                {
                    usados.Add(sequenciaAtual);
                    if (sequenciaAtual > maximosPorPrefixo[prefixo])
                    {
                        maximosPorPrefixo[prefixo] = sequenciaAtual;
                    }

                    continue;
                }

                var proximaSequencia = maximosPorPrefixo[prefixo] + 1;
                while (usados.Contains(proximaSequencia))
                {
                    proximaSequencia++;
                }

                usados.Add(proximaSequencia);
                maximosPorPrefixo[prefixo] = proximaSequencia;
                atualizacoes.Add((registro.Id, $"{prefixo}{proximaSequencia:0000}"));
            }

            if (atualizacoes.Count == 0)
            {
                return;
            }

            using var transaction = connection.BeginTransaction();
            foreach (var atualizacao in atualizacoes)
            {
                using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = @"
                    UPDATE Orcamentos
                    SET Numero = @Numero
                    WHERE Id = @Id;";
                update.Parameters.AddWithValue("@Numero", atualizacao.Numero);
                update.Parameters.AddWithValue("@Id", atualizacao.Id.ToString());
                update.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void GarantirNumeroUnico(SqliteConnection connection, SqliteTransaction transaction, Orcamento orcamento)
        {
            var dataReferencia = orcamento.DataCriacao == default ? DateTime.Now : orcamento.DataCriacao;
            if (string.IsNullOrWhiteSpace(orcamento.Numero) ||
                NumeroJaExiste(connection, transaction, orcamento.Numero, orcamento.Id))
            {
                orcamento.Numero = GerarNumeroOrcamento(connection, dataReferencia, transaction);
            }
        }

        private static bool NumeroJaExiste(SqliteConnection connection, SqliteTransaction transaction, string numero, Guid orcamentoId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT 1
                FROM Orcamentos
                WHERE Numero = @Numero
                  AND Id <> @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Numero", numero.Trim());
            command.Parameters.AddWithValue("@Id", orcamentoId.ToString());
            return command.ExecuteScalar() != null;
        }

        private static string GerarNumeroOrcamento(SqliteConnection connection, DateTime dataReferencia, SqliteTransaction? transaction = null)
        {
            var prefixo = GerarPrefixoNumero(dataReferencia);

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT COALESCE(MAX(CAST(SUBSTR(Numero, @InicioSequencia) AS INTEGER)), 0)
                FROM Orcamentos
                WHERE Numero LIKE @Padrao;";
            command.Parameters.AddWithValue("@InicioSequencia", prefixo.Length + 1);
            command.Parameters.AddWithValue("@Padrao", $"{prefixo}%");

            var maximo = Convert.ToInt32(command.ExecuteScalar());
            return $"{prefixo}{(maximo + 1):0000}";
        }

        private static string GerarPrefixoNumero(DateTime dataReferencia)
        {
            return $"ORC-{dataReferencia:yyyyMMdd}-";
        }

        private static bool NumeroCompativelComData(string? numero, DateTime dataReferencia, out int sequencia)
        {
            sequencia = 0;
            if (string.IsNullOrWhiteSpace(numero))
            {
                return false;
            }

            var prefixo = GerarPrefixoNumero(dataReferencia);
            if (!numero.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var sufixo = numero.Substring(prefixo.Length);
            return sufixo.Length == 4 &&
                   int.TryParse(sufixo, out sequencia) &&
                   sequencia > 0;
        }

        private void PrepararEValidarOrcamento(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento);

            if (orcamento.Id == Guid.Empty)
            {
                orcamento.Id = Guid.NewGuid();
            }

            orcamento.Numero = ComercialValidationHelper.NormalizarTexto(orcamento.Numero);
            orcamento.Status = ComercialValidationHelper.NormalizarTexto(orcamento.Status, "Em Aberto");
            orcamento.Observacoes = ComercialValidationHelper.NormalizarTexto(orcamento.Observacoes);
            orcamento.CondicoesPagamento = ComercialValidationHelper.NormalizarTexto(orcamento.CondicoesPagamento);
            orcamento.PrazoEntrega = ComercialValidationHelper.NormalizarTexto(orcamento.PrazoEntrega);
            orcamento.DataCriacao = orcamento.DataCriacao == default ? DateTime.Now : orcamento.DataCriacao;
            orcamento.Itens ??= new List<OrcamentoItem>();

            if (string.IsNullOrWhiteSpace(orcamento.Numero))
            {
                orcamento.Numero = GerarNumeroOrcamento();
            }

            if (orcamento.Itens.Count == 0)
            {
                throw new InvalidOperationException("Adicione pelo menos um item ao orcamento.");
            }

            var erroDatas = CadastroValidationHelper.ValidarIntervaloDatas(
                orcamento.DataCriacao,
                orcamento.DataValidade,
                "a data de criacao",
                "a data de validade",
                obrigatorioInicial: true,
                obrigatorioFinal: false);

            if (!string.IsNullOrWhiteSpace(erroDatas))
            {
                throw new InvalidOperationException(erroDatas);
            }

            if (UiTextSanitizer.EqualsNormalized(orcamento.Status, "Aprovado") && !orcamento.DataAprovacao.HasValue)
            {
                orcamento.DataAprovacao = DateTime.Now;
            }

            decimal subtotal = 0m;
            decimal lucro = 0m;

            foreach (var item in orcamento.Itens)
            {
                item.OrcamentoId = orcamento.Id;
                PrepararEValidarItem(item);
                subtotal += item.Subtotal;
                lucro += item.LucroEstimado;
            }

            ComercialValidationHelper.GarantirDescontoValido(orcamento.Desconto, subtotal, "O desconto");
            ComercialValidationHelper.GarantirValorMaiorOuIgualZero(orcamento.Acrescimo, "o acrescimo");
            ComercialValidationHelper.GarantirValorMaiorOuIgualZero(orcamento.ComissaoVendedor, "a comissao do vendedor");
            ComercialValidationHelper.GarantirValorMaiorOuIgualZero(orcamento.ImpostosEstimados, "os impostos estimados");

            orcamento.Subtotal = subtotal;
            orcamento.Total = Math.Max(0m, subtotal - orcamento.Desconto + orcamento.Acrescimo);
            orcamento.LucroEstimado = lucro;
            orcamento.MargemLucro = orcamento.Total > 0
                ? (lucro / orcamento.Total) * 100m
                : 0m;
        }

        private static void PrepararEValidarItem(OrcamentoItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            item.Tipo = NormalizarTipoItem(item.Tipo);
            item.ProdutoNome = ComercialValidationHelper.NormalizarTexto(item.ProdutoNome);
            item.ProdutoCodigo = ComercialValidationHelper.NormalizarTexto(item.ProdutoCodigo);
            item.ProdutoCategoria = ComercialValidationHelper.NormalizarTexto(item.ProdutoCategoria);
            item.ProdutoMarca = ComercialValidationHelper.NormalizarTexto(item.ProdutoMarca);
            item.ProdutoAplicacao = ComercialValidationHelper.NormalizarTexto(item.ProdutoAplicacao);
            item.Observacoes = ComercialValidationHelper.NormalizarTexto(item.Observacoes);

            if (item.UsaEstoque)
            {
                if (!item.ProdutoId.HasValue || item.ProdutoId == Guid.Empty)
                {
                    throw new InvalidOperationException($"O item '{item.ProdutoNome}' precisa estar vinculado a um produto valido.");
                }
            }
            else
            {
                item.ProdutoId = null;
                item.ProdutoCodigo = string.IsNullOrWhiteSpace(item.ProdutoCodigo) ? "SERVICO" : item.ProdutoCodigo;
                item.ProdutoCategoria = string.IsNullOrWhiteSpace(item.ProdutoCategoria) ? "Mao de obra" : item.ProdutoCategoria;
                item.EstoqueDisponivel = 0;
            }

            ComercialValidationHelper.GarantirTextoObrigatorio(item.ProdutoNome, "o nome do item do orcamento");
            ComercialValidationHelper.GarantirQuantidadeInteiraPositiva(item.Quantidade, $"a quantidade do item '{item.ProdutoNome}'");
            ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.PrecoUnitario, $"o preco unitario do item '{item.ProdutoNome}'");
            ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.PrecoCusto, $"o preco de custo do item '{item.ProdutoNome}'");
            ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.EstoqueDisponivel, $"o estoque disponivel do item '{item.ProdutoNome}'");

            item.Subtotal = ComercialValidationHelper.CalcularSubtotal(item.Quantidade, item.PrecoUnitario, item.Desconto);
            item.LucroEstimado = item.Subtotal - (item.Quantidade * item.PrecoCusto);
            item.MargemLucro = item.Subtotal > 0
                ? (item.LucroEstimado / item.Subtotal) * 100m
                : 0m;
        }

        private static string NormalizarTipoItem(string? tipo)
        {
            return string.Equals(tipo, "Servico", StringComparison.OrdinalIgnoreCase)
                ? "Servico"
                : "Produto";
        }

        private static string ReadString(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static Guid ReadGuid(SqliteDataReader reader, int ordinal)
        {
            return Guid.Parse(reader.GetString(ordinal));
        }

        private static Guid? ReadNullableGuid(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var valor = reader.GetString(ordinal);
            return string.IsNullOrWhiteSpace(valor) ? null : Guid.Parse(valor);
        }

        private static DateTime ReadDate(SqliteDataReader reader, int ordinal, DateTime fallback)
        {
            if (reader.IsDBNull(ordinal))
            {
                return fallback;
            }

            return DateTime.Parse(reader.GetString(ordinal));
        }

        private static DateTime? ReadNullableDate(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var valor = reader.GetString(ordinal);
            return string.IsNullOrWhiteSpace(valor) ? null : DateTime.Parse(valor);
        }

        private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return 0m;
            }

            return reader.GetFieldType(ordinal) == typeof(long)
                ? Convert.ToDecimal(reader.GetInt64(ordinal))
                : Convert.ToDecimal(reader.GetDouble(ordinal));
        }

        private static int ReadInt(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }
    }
}
