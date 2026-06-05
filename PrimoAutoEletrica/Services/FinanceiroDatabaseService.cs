using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public class FinanceiroDatabaseService
    {
        private readonly DatabaseService _databaseService;
        private static LoggerService Logger => global::PrimoAutoEletrica.App.Logger;

        public FinanceiroDatabaseService()
        {
            _databaseService = global::PrimoAutoEletrica.App.Database;

            using var connection = GetConnection();
            connection.Open();

            InicializarTabelas(connection);
            MigrarDadosLegados(connection);
        }

        private SqliteConnection GetConnection()
        {
            return _databaseService.GetConnection();
        }

        private static void InicializarTabelas(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ContasPagar (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fornecedor TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    Valor DECIMAL NOT NULL,
                    DataVencimento TEXT NOT NULL,
                    DataPagamento TEXT,
                    Status TEXT NOT NULL DEFAULT 'Pendente',
                    Categoria TEXT,
                    Observacoes TEXT,
                    DataCriacao TEXT NOT NULL
                )";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ContasReceber (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Cliente TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    Valor DECIMAL NOT NULL,
                    DataVencimento TEXT NOT NULL,
                    DataPagamento TEXT,
                    Status TEXT NOT NULL DEFAULT 'Pendente',
                    FormaPagamento TEXT,
                    Observacoes TEXT,
                    DataCriacao TEXT NOT NULL,
                    Origem TEXT,
                    ReferenciaExterna TEXT
                )";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS MovimentacoesFinanceiras (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Tipo TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    Valor DECIMAL NOT NULL,
                    Data TEXT NOT NULL,
                    Categoria TEXT,
                    FormaPagamento TEXT,
                    ReferenciaId INTEGER,
                    Observacoes TEXT,
                    DataCriacao TEXT NOT NULL,
                    Origem TEXT,
                    ReferenciaExterna TEXT
                )";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS MetasFinanceiras (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nome TEXT NOT NULL,
                    ValorMeta DECIMAL NOT NULL,
                    ValorAtual DECIMAL DEFAULT 0,
                    DataInicio TEXT NOT NULL,
                    DataFim TEXT NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'Em Andamento',
                    Descricao TEXT
                )";
            command.ExecuteNonQuery();

            EnsureColumnExists(connection, "ContasReceber", "Origem", "ALTER TABLE ContasReceber ADD COLUMN Origem TEXT;");
            EnsureColumnExists(connection, "ContasReceber", "ReferenciaExterna", "ALTER TABLE ContasReceber ADD COLUMN ReferenciaExterna TEXT;");
            EnsureColumnExists(connection, "MovimentacoesFinanceiras", "Origem", "ALTER TABLE MovimentacoesFinanceiras ADD COLUMN Origem TEXT;");
            EnsureColumnExists(connection, "MovimentacoesFinanceiras", "ReferenciaExterna", "ALTER TABLE MovimentacoesFinanceiras ADD COLUMN ReferenciaExterna TEXT;");

            CreateIndexIfNeeded(
                connection,
                "IX_ContasReceber_Integracao",
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_ContasReceber_Integracao ON ContasReceber (Origem, ReferenciaExterna) WHERE Origem IS NOT NULL AND ReferenciaExterna IS NOT NULL;");

            CreateIndexIfNeeded(
                connection,
                "IX_MovimentacoesFinanceiras_Integracao",
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_MovimentacoesFinanceiras_Integracao ON MovimentacoesFinanceiras (Origem, ReferenciaExterna) WHERE Origem IS NOT NULL AND ReferenciaExterna IS NOT NULL;");
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

            var attachCommand = connection.CreateCommand();
            attachCommand.CommandText = "ATTACH DATABASE @path AS legado;";
            attachCommand.Parameters.AddWithValue("@path", legacyPath);
            attachCommand.ExecuteNonQuery();

            try
            {
                foreach (var tableName in new[]
                {
                    "ContasPagar",
                    "ContasReceber",
                    "MovimentacoesFinanceiras",
                    "MetasFinanceiras"
                })
                {
                    if (!TabelaExiste(connection, "legado", tableName) ||
                        !TabelaTemDados(connection, "legado", tableName) ||
                        TabelaTemDados(connection, "main", tableName))
                    {
                        continue;
                    }

                    CopiarTabelaLegada(connection, tableName);
                }
            }
            finally
            {
                var detachCommand = connection.CreateCommand();
                detachCommand.CommandText = "DETACH DATABASE legado;";
                detachCommand.ExecuteNonQuery();
            }
        }

        private static void CopiarTabelaLegada(SqliteConnection connection, string tableName)
        {
            var legacyColumns = ObterColunasTabela(connection, "legado", tableName);
            var currentColumns = ObterColunasTabela(connection, "main", tableName);
            var commonColumns = currentColumns
                .Where(column => legacyColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (commonColumns.Count == 0)
            {
                return;
            }

            var columnList = string.Join(", ", commonColumns);
            var copyCommand = connection.CreateCommand();
            copyCommand.CommandText = $@"
                INSERT INTO main.{tableName} ({columnList})
                SELECT {columnList}
                FROM legado.{tableName};";
            copyCommand.ExecuteNonQuery();
        }

        private static List<string> ObterColunasTabela(SqliteConnection connection, string schema, string tableName)
        {
            var columns = new List<string>();

            using var pragma = connection.CreateCommand();
            pragma.CommandText = $"PRAGMA {schema}.table_info({tableName});";

            using var reader = pragma.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(reader.GetString(1));
            }

            return columns;
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
            var command = connection.CreateCommand();
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

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1 FROM {schema}.{tableName} LIMIT 1;";
            return command.ExecuteScalar() != null;
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

        private static void CreateIndexIfNeeded(SqliteConnection connection, string indexName, string createSql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 1
                FROM sqlite_master
                WHERE type = 'index'
                  AND name = @name
                LIMIT 1;";
            command.Parameters.AddWithValue("@name", indexName);

            if (command.ExecuteScalar() != null)
            {
                return;
            }

            using var createCommand = connection.CreateCommand();
            createCommand.CommandText = createSql;
            createCommand.ExecuteNonQuery();
        }

        public void AdicionarContaPagar(string fornecedor, string descricao, decimal valor, DateTime dataVencimento, string categoria = "", string observacoes = "")
        {
            ValidarContaPagar(fornecedor, descricao, valor, dataVencimento);

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ContasPagar (Fornecedor, Descricao, Valor, DataVencimento, Status, Categoria, Observacoes, DataCriacao)
                VALUES (@fornecedor, @descricao, @valor, @dataVencimento, 'Pendente', @categoria, @observacoes, @dataCriacao)";

            command.Parameters.AddWithValue("@fornecedor", fornecedor);
            command.Parameters.AddWithValue("@descricao", descricao);
            command.Parameters.AddWithValue("@valor", valor);
            command.Parameters.AddWithValue("@dataVencimento", dataVencimento.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@categoria", ToDbNullableString(categoria));
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(observacoes));
            command.Parameters.AddWithValue("@dataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            command.ExecuteNonQuery();
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Financeiro",
                "ContaPagarCriada",
                "ContasPagar",
                string.Empty,
                $"Fornecedor={fornecedor}; Descricao={descricao}; Valor={valor:C}; Vencimento={dataVencimento:dd/MM/yyyy}");
        }

        public List<dynamic> ObterContasPagar()
        {
            var contas = new List<dynamic>();
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM ContasPagar ORDER BY DataVencimento ASC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                contas.Add(new
                {
                    Id = reader.GetInt32(0),
                    Fornecedor = reader.GetString(1),
                    Descricao = reader.GetString(2),
                    Valor = reader.GetDecimal(3),
                    DataVencimento = reader.GetString(4),
                    DataPagamento = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Status = reader.GetString(6),
                    Categoria = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Observacoes = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    DataCriacao = reader.GetString(9)
                });
            }

            return contas;
        }

        public void BaixarContaPagar(
            int id,
            DateTime dataPagamento,
            string formaPagamento = "",
            string observacoes = "")
        {
            var erroData = CadastroValidationHelper.ValidarDataNaoFutura(dataPagamento, "a data do pagamento", obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroData))
            {
                throw new InvalidOperationException(erroData);
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var conta = ObterContaPagarPorId(connection, transaction, id)
                    ?? throw new InvalidOperationException("Conta a pagar nao localizada para liquidacao.");

                if (StatusContaPagarLiquidada(conta.Status))
                {
                    return;
                }

                using (var updateCommand = connection.CreateCommand())
                {
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText = @"
                        UPDATE ContasPagar
                        SET
                            Status = 'Paga',
                            DataPagamento = @dataPagamento,
                            Observacoes = CASE
                                WHEN COALESCE(Observacoes, '') = '' THEN @observacoes
                                WHEN @observacoes = '' THEN Observacoes
                                ELSE Observacoes || ' | ' || @observacoes
                            END
                        WHERE Id = @id;";
                    updateCommand.Parameters.AddWithValue("@dataPagamento", dataPagamento.ToString("yyyy-MM-dd"));
                    updateCommand.Parameters.AddWithValue("@observacoes", string.IsNullOrWhiteSpace(observacoes) ? "Pagamento registrado manualmente." : observacoes.Trim());
                    updateCommand.Parameters.AddWithValue("@id", id);
                    updateCommand.ExecuteNonQuery();
                }

                InserirMovimentacao(
                    connection,
                    transaction,
                    "Despesa",
                    $"Pagamento: {conta.Descricao}",
                    conta.Valor,
                    dataPagamento,
                    string.IsNullOrWhiteSpace(conta.Categoria) ? "Contas a Pagar" : conta.Categoria,
                    formaPagamento,
                    id,
                    string.IsNullOrWhiteSpace(observacoes) ? $"Conta a pagar liquidada: {conta.Fornecedor}" : observacoes,
                    "ContaPagarLiquidacao",
                    id.ToString(CultureInfo.InvariantCulture));

                transaction.Commit();
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Financeiro",
                    "ContaPagarLiquidada",
                    "ContasPagar",
                    id.ToString(CultureInfo.InvariantCulture),
                    $"Fornecedor={conta.Fornecedor}; Valor={conta.Valor:C}; DataPagamento={dataPagamento:dd/MM/yyyy}");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void AdicionarContaReceber(
            string cliente,
            string descricao,
            decimal valor,
            DateTime dataVencimento,
            string formaPagamento = "",
            string observacoes = "",
            string status = "Pendente",
            DateTime? dataPagamento = null,
            string origem = "",
            string referenciaExterna = "")
        {
            ValidarContaReceber(cliente, descricao, valor, dataVencimento, dataPagamento, status);

            using var connection = GetConnection();
            connection.Open();

            InserirContaReceber(
                connection,
                transaction: null,
                cliente,
                descricao,
                valor,
                dataVencimento,
                formaPagamento,
                observacoes,
                status,
                dataPagamento,
                origem,
                referenciaExterna);

            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Financeiro",
                "ContaReceberCriada",
                "ContasReceber",
                referenciaExterna,
                $"Cliente={cliente}; Descricao={descricao}; Valor={valor:C}; Vencimento={dataVencimento:dd/MM/yyyy}; Origem={origem}");
        }

        public List<dynamic> ObterContasReceber()
        {
            var contas = new List<dynamic>();
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM ContasReceber ORDER BY DataVencimento ASC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                contas.Add(new
                {
                    Id = reader.GetInt32(0),
                    Cliente = reader.GetString(1),
                    Descricao = reader.GetString(2),
                    Valor = reader.GetDecimal(3),
                    DataVencimento = reader.GetString(4),
                    DataPagamento = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Status = reader.GetString(6),
                    FormaPagamento = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Observacoes = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    DataCriacao = reader.GetString(9),
                    Origem = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    ReferenciaExterna = reader.IsDBNull(11) ? "" : reader.GetString(11)
                });
            }

            return contas;
        }

        public void BaixarContaReceber(
            int id,
            DateTime dataPagamento,
            string formaPagamento,
            string observacoes = "")
        {
            var erroData = CadastroValidationHelper.ValidarDataNaoFutura(dataPagamento, "a data do recebimento", obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroData))
            {
                throw new InvalidOperationException(erroData);
            }

            ComercialValidationHelper.GarantirTextoObrigatorio(formaPagamento, "a forma de pagamento do recebimento");

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var conta = ObterContaReceberPorId(connection, transaction, id)
                    ?? throw new InvalidOperationException("Conta a receber nao localizada para liquidacao.");

                if (StatusContaReceberLiquidada(conta.Status))
                {
                    return;
                }

                using (var updateCommand = connection.CreateCommand())
                {
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText = @"
                        UPDATE ContasReceber
                        SET
                            Status = 'Pago',
                            DataPagamento = @dataPagamento,
                            FormaPagamento = @formaPagamento,
                            Observacoes = CASE
                                WHEN COALESCE(Observacoes, '') = '' THEN @observacoes
                                WHEN @observacoes = '' THEN Observacoes
                                ELSE Observacoes || ' | ' || @observacoes
                            END
                        WHERE Id = @id;";
                    updateCommand.Parameters.AddWithValue("@dataPagamento", dataPagamento.ToString("yyyy-MM-dd"));
                    updateCommand.Parameters.AddWithValue("@formaPagamento", formaPagamento.Trim());
                    updateCommand.Parameters.AddWithValue("@observacoes", string.IsNullOrWhiteSpace(observacoes) ? "Recebimento registrado manualmente." : observacoes.Trim());
                    updateCommand.Parameters.AddWithValue("@id", id);
                    updateCommand.ExecuteNonQuery();
                }

                InserirMovimentacao(
                    connection,
                    transaction,
                    "Entrada",
                    $"Recebimento: {conta.Descricao}",
                    conta.Valor,
                    dataPagamento,
                    "Contas a Receber",
                    formaPagamento,
                    id,
                    string.IsNullOrWhiteSpace(observacoes) ? $"Conta a receber liquidada: {conta.Cliente}" : observacoes,
                    "ContaReceberLiquidacao",
                    id.ToString(CultureInfo.InvariantCulture));

                transaction.Commit();
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Financeiro",
                    "ContaReceberLiquidada",
                    "ContasReceber",
                    id.ToString(CultureInfo.InvariantCulture),
                    $"Cliente={conta.Cliente}; Valor={conta.Valor:C}; DataPagamento={dataPagamento:dd/MM/yyyy}; Forma={formaPagamento}");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void AdicionarMovimentacao(
            string tipo,
            string descricao,
            decimal valor,
            DateTime data,
            string categoria = "",
            string formaPagamento = "",
            int? referenciaId = null,
            string observacoes = "",
            string origem = "",
            string referenciaExterna = "")
        {
            ValidarMovimentacao(tipo, descricao, valor, data);

            using var connection = GetConnection();
            connection.Open();

            InserirMovimentacao(
                connection,
                transaction: null,
                tipo,
                descricao,
                valor,
                data,
                categoria,
                formaPagamento,
                referenciaId,
                observacoes,
                origem,
                referenciaExterna);

            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Financeiro",
                "MovimentacaoCriada",
                "MovimentacoesFinanceiras",
                referenciaExterna,
                $"Tipo={tipo}; Descricao={descricao}; Valor={valor:C}; Data={data:dd/MM/yyyy}; Origem={origem}");
        }

        public List<dynamic> ObterMovimentacoes(
            DateTime? dataInicio = null,
            DateTime? dataFim = null,
            string tipo = "",
            string categoria = "",
            int limit = 1000)
        {
            return Logger.Measure("Financeiro", $"ObterMovimentacoes(limit={limit})", () =>
            {
                var movimentacoes = new List<dynamic>();
                using var connection = GetConnection();
                connection.Open();

                var filtros = new List<string> { "1 = 1" };
                var command = connection.CreateCommand();

                if (dataInicio.HasValue)
                {
                    filtros.Add("date(Data) >= @dataInicio");
                    command.Parameters.AddWithValue("@dataInicio", dataInicio.Value.ToString("yyyy-MM-dd"));
                }

                if (dataFim.HasValue)
                {
                    filtros.Add("date(Data) <= @dataFim");
                    command.Parameters.AddWithValue("@dataFim", dataFim.Value.ToString("yyyy-MM-dd"));
                }

                if (!string.IsNullOrWhiteSpace(tipo))
                {
                    filtros.Add("COALESCE(Tipo, '') = @tipo");
                    command.Parameters.AddWithValue("@tipo", tipo.Trim());
                }

                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    filtros.Add("COALESCE(Categoria, '') = @categoria");
                    command.Parameters.AddWithValue("@categoria", categoria.Trim());
                }

                command.CommandText = $@"
                    SELECT *
                    FROM MovimentacoesFinanceiras
                    WHERE {string.Join(" AND ", filtros)}
                    ORDER BY Data DESC, Id DESC
                    LIMIT @limit";
                command.Parameters.AddWithValue("@limit", limit <= 0 ? 1000 : Math.Min(limit, 2000));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    movimentacoes.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Tipo = reader.GetString(1),
                        Descricao = reader.GetString(2),
                        Valor = reader.GetDecimal(3),
                        Data = reader.GetString(4),
                        Categoria = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        FormaPagamento = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        ReferenciaId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                        Observacoes = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        DataCriacao = reader.GetString(9),
                        Origem = reader.IsDBNull(10) ? "" : reader.GetString(10),
                        ReferenciaExterna = reader.IsDBNull(11) ? "" : reader.GetString(11)
                    });
                }

                return movimentacoes;
            }, warningThresholdMs: 450);
        }

        public void RegistrarReceitaOrcamento(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento);

            var valorTotal = Math.Max(0m, orcamento.Total);
            if (valorTotal <= 0)
            {
                Logger.LogInfo($"Orcamento '{orcamento.Id}' sem valor financeiro para integrar.");
                return;
            }

            var clienteNome = string.IsNullOrWhiteSpace(orcamento.Cliente?.Nome)
                ? "Cliente nao informado"
                : orcamento.Cliente.Nome.Trim();
            var formaPagamento = string.IsNullOrWhiteSpace(orcamento.CondicoesPagamento)
                ? "A definir"
                : orcamento.CondicoesPagamento.Trim();
            var dataReferencia = orcamento.DataConversaoVenda ?? orcamento.DataAprovacao ?? orcamento.DataCriacao;
            var descricao = CriarDescricaoOrcamento(orcamento);
            var pagamentoImediato = CondicaoPagamentoLiquidaNaHora(formaPagamento);
            var observacoes = BuildObservacoesIntegracao(orcamento, valorTotal, pagamentoImediato);

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                UpsertContaReceberIntegrada(
                    connection,
                    transaction,
                    clienteNome,
                    descricao,
                    valorTotal,
                    dataReferencia.Date,
                    formaPagamento,
                    observacoes,
                    pagamentoImediato ? "Pago" : "Pendente",
                    pagamentoImediato ? dataReferencia : null,
                    "OrcamentoContaReceber",
                    orcamento.Id.ToString());

                if (pagamentoImediato)
                {
                    UpsertMovimentacaoIntegrada(
                        connection,
                        transaction,
                        "Receita",
                        descricao,
                        valorTotal,
                        dataReferencia,
                        "Orcamentos Convertidos",
                        formaPagamento,
                        null,
                        observacoes,
                        "OrcamentoMovimentacao",
                        orcamento.Id.ToString());
                }

                transaction.Commit();
                Logger.LogInfo($"Orcamento '{orcamento.Id}' integrado ao financeiro. Total={valorTotal:C}; PagamentoImediato={pagamentoImediato}.");
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Financeiro",
                    "OrcamentoIntegrado",
                    "Orcamento",
                    orcamento.Id.ToString(),
                    $"Numero={orcamento.Numero}; Total={valorTotal:C}; Forma={formaPagamento}; PagamentoImediato={pagamentoImediato}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.LogError($"Falha ao integrar o orcamento '{orcamento.Id}' ao financeiro.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Financeiro", "FalhaIntegracaoOrcamento", ex, "Orcamento", orcamento.Id.ToString());
                throw;
            }
        }

        public void RegistrarReceitaAgendamento(Agendamento agendamento)
        {
            ArgumentNullException.ThrowIfNull(agendamento);

            var valorServico = DeterminarValorServicoAgendamento(agendamento);
            if (valorServico <= 0)
            {
                Logger.LogInfo($"Agendamento '{agendamento.Id}' finalizado sem valor financeiro para integrar.");
                return;
            }

            var valorRecebido = CalcularValorRecebidoAgendamento(agendamento, valorServico);
            var valorEmAberto = Math.Max(0, valorServico - valorRecebido);
            var dataReferencia = agendamento.DataPagamento ?? agendamento.CheckOut ?? agendamento.DataAgendamento;
            var formaPagamento = string.IsNullOrWhiteSpace(agendamento.FormaPagamento) ? "A definir" : agendamento.FormaPagamento.Trim();
            var descricao = CriarDescricaoAgendamento(agendamento);

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                if (valorEmAberto > 0 &&
                    !ExisteRegistroIntegrado(connection, transaction, "ContasReceber", "AgendamentoContaReceber", agendamento.Id.ToString()))
                {
                    InserirContaReceber(
                        connection,
                        transaction,
                        string.IsNullOrWhiteSpace(agendamento.ClienteNome) ? "Cliente nao informado" : agendamento.ClienteNome,
                        descricao,
                        valorEmAberto,
                        dataReferencia.Date,
                        formaPagamento,
                        BuildObservacoesIntegracao(agendamento, valorServico, valorRecebido, valorEmAberto),
                        "Pendente",
                        null,
                        "AgendamentoContaReceber",
                        agendamento.Id.ToString());
                }

                if (valorRecebido > 0 &&
                    !ExisteRegistroIntegrado(connection, transaction, "MovimentacoesFinanceiras", "AgendamentoMovimentacao", agendamento.Id.ToString()))
                {
                    InserirMovimentacao(
                        connection,
                        transaction,
                        "Entrada",
                        descricao,
                        valorRecebido,
                        dataReferencia,
                        "Servicos Automotivos",
                        formaPagamento,
                        null,
                        BuildObservacoesIntegracao(agendamento, valorServico, valorRecebido, valorEmAberto),
                        "AgendamentoMovimentacao",
                        agendamento.Id.ToString());
                }

                transaction.Commit();
                Logger.LogInfo($"Agendamento '{agendamento.Id}' integrado ao financeiro. Total={valorServico:C}, Recebido={valorRecebido:C}, Aberto={valorEmAberto:C}.");
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Financeiro",
                    "AgendamentoIntegrado",
                    "Agendamento",
                    agendamento.Id.ToString(),
                    $"Total={valorServico:C}; Recebido={valorRecebido:C}; Aberto={valorEmAberto:C}; Cliente={agendamento.ClienteNome}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.LogError($"Falha ao integrar o agendamento '{agendamento.Id}' ao financeiro.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Financeiro", "FalhaIntegracaoAgendamento", ex, "Agendamento", agendamento.Id.ToString());
                throw;
            }
        }

        public void RegistrarReceitaOrdemServico(OrdemServico ordem)
        {
            ArgumentNullException.ThrowIfNull(ordem);

            using var connection = GetConnection();
            connection.Open();
            InicializarTabelas(connection);
            using var transaction = connection.BeginTransaction();

            try
            {
                RegistrarReceitaOrdemServicoIntegrada(connection, transaction, ordem);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        internal static void RegistrarReceitaOrdemServicoIntegrada(
            SqliteConnection connection,
            SqliteTransaction transaction,
            OrdemServico ordem,
            bool registrarAuditoria = true)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(transaction);
            ArgumentNullException.ThrowIfNull(ordem);

            InicializarTabelas(connection);

            var valorTotal = DeterminarValorTotalOrdemServico(ordem);
            if (valorTotal <= 0)
            {
                Logger.LogInfo($"OS '{ordem.Id}' concluida sem valor financeiro para integrar.");
                return;
            }

            var dataReferencia = ordem.DataEntrega ?? ordem.DataConclusao ?? ordem.DataInicio ?? ordem.DataAbertura;
            var descricao = CriarDescricaoOrdemServico(ordem);
            var observacoes = BuildObservacoesIntegracao(ordem, valorTotal);
            var referenciaExterna = ordem.Id.ToString();

            UpsertContaReceberIntegrada(
                connection,
                transaction,
                string.IsNullOrWhiteSpace(ordem.ClienteNomeSnapshot) ? "Cliente nao informado" : ordem.ClienteNomeSnapshot,
                descricao,
                valorTotal,
                dataReferencia.Date,
                "A definir",
                observacoes,
                "Pendente",
                null,
                "OrdemServicoContaReceber",
                referenciaExterna);

            UpsertMovimentacaoIntegrada(
                connection,
                transaction,
                "Receita",
                descricao,
                valorTotal,
                dataReferencia,
                "Ordens de Servico",
                "A definir",
                null,
                observacoes,
                "OrdemServicoMovimentacao",
                referenciaExterna);

            Logger.LogInfo($"OS '{ordem.Id}' integrada ao financeiro. Total={valorTotal:C}.");
            if (registrarAuditoria)
            {
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Financeiro",
                    "OrdemServicoIntegrada",
                    "OrdemServico",
                    ordem.Id.ToString(),
                    $"Numero={ordem.Numero}; Total={valorTotal:C}; Cliente={ordem.ClienteNomeSnapshot}");
            }
        }

        public void AdicionarMeta(string nome, decimal valorMeta, DateTime dataInicio, DateTime dataFim, string descricao = "")
        {
            ValidarMeta(nome, valorMeta, dataInicio, dataFim);

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO MetasFinanceiras (Nome, ValorMeta, ValorAtual, DataInicio, DataFim, Status, Descricao)
                VALUES (@nome, @valorMeta, 0, @dataInicio, @dataFim, 'Em Andamento', @descricao)";

            command.Parameters.AddWithValue("@nome", nome);
            command.Parameters.AddWithValue("@valorMeta", valorMeta);
            command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@descricao", ToDbNullableString(descricao));

            command.ExecuteNonQuery();
        }

        public List<dynamic> ObterMetas()
        {
            var metas = new List<dynamic>();
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM MetasFinanceiras ORDER BY DataFim ASC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                metas.Add(new
                {
                    Id = reader.GetInt32(0),
                    Nome = reader.GetString(1),
                    ValorMeta = reader.GetDecimal(2),
                    ValorAtual = reader.GetDecimal(3),
                    DataInicio = reader.GetString(4),
                    DataFim = reader.GetString(5),
                    Status = reader.GetString(6),
                    Descricao = reader.IsDBNull(7) ? "" : reader.GetString(7)
                });
            }

            return metas;
        }

        public (decimal entradas, decimal saidas, decimal saldo) ObterResumoFinanceiro(DateTime dataInicio, DateTime dataFim)
        {
            using var connection = GetConnection();
            connection.Open();

            decimal entradas = 0;
            decimal saidas = 0;

            if (TryObterResumoFinanceiroSeguro(connection, dataInicio, dataFim, out var resumoSeguro))
            {
                return resumoSeguro;
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COALESCE(SUM(Valor), 0)
                FROM MovimentacoesFinanceiras
                WHERE Tipo IN ('Entrada', 'Receita')
                  AND Data BETWEEN @dataInicio AND @dataFim";
            command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                entradas = reader.GetDecimal(0);
            }

            command.CommandText = @"
                SELECT COALESCE(SUM(Valor), 0)
                FROM MovimentacoesFinanceiras
                WHERE Tipo IN ('Saida', 'Saída', 'Despesa')
                  AND Data BETWEEN @dataInicio AND @dataFim";

            using var reader2 = command.ExecuteReader();
            if (reader2.Read())
            {
                saidas = reader2.GetDecimal(0);
            }

            return (entradas, saidas, entradas - saidas);
        }

        public DemonstrativoResultadoFinanceiro ObterDemonstrativoResultado(DateTime dataInicio, DateTime dataFim)
        {
            using var connection = GetConnection();
            connection.Open();

            var resumo = ObterResumoFinanceiro(dataInicio, dataFim);

            using var contasReceberCommand = connection.CreateCommand();
            contasReceberCommand.CommandText = @"
                SELECT COALESCE(SUM(Valor), 0)
                FROM ContasReceber
                WHERE date(DataVencimento) BETWEEN @dataInicio AND @dataFim
                  AND COALESCE(Status, '') NOT IN ('Pago', 'Recebida', 'Cancelado');";
            contasReceberCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            contasReceberCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
            var contasReceberPendentes = Convert.ToDecimal(contasReceberCommand.ExecuteScalar() ?? 0m, CultureInfo.InvariantCulture);

            using var contasPagarCommand = connection.CreateCommand();
            contasPagarCommand.CommandText = @"
                SELECT COALESCE(SUM(Valor), 0)
                FROM ContasPagar
                WHERE date(DataVencimento) BETWEEN @dataInicio AND @dataFim
                  AND COALESCE(Status, '') <> 'Paga';";
            contasPagarCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            contasPagarCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
            var contasPagarPendentes = Convert.ToDecimal(contasPagarCommand.ExecuteScalar() ?? 0m, CultureInfo.InvariantCulture);

            using var inadimplenciaCommand = connection.CreateCommand();
            inadimplenciaCommand.CommandText = @"
                SELECT COALESCE(SUM(Valor), 0)
                FROM ContasReceber
                WHERE date(DataVencimento) < @hoje
                  AND COALESCE(Status, '') NOT IN ('Pago', 'Recebida', 'Cancelado');";
            inadimplenciaCommand.Parameters.AddWithValue("@hoje", DateTime.Today.ToString("yyyy-MM-dd"));
            var inadimplencia = Convert.ToDecimal(inadimplenciaCommand.ExecuteScalar() ?? 0m, CultureInfo.InvariantCulture);

            return new DemonstrativoResultadoFinanceiro
            {
                DataInicio = dataInicio.Date,
                DataFim = dataFim.Date,
                ReceitasConfirmadas = resumo.entradas,
                DespesasConfirmadas = resumo.saidas,
                ContasReceberPendentes = contasReceberPendentes,
                ContasPagarPendentes = contasPagarPendentes,
                InadimplenciaEmAberto = inadimplencia
            };
        }

        private static bool TryObterResumoFinanceiroSeguro(
            SqliteConnection connection,
            DateTime dataInicio,
            DateTime dataFim,
            out (decimal entradas, decimal saidas, decimal saldo) resumo)
        {
            resumo = (0m, 0m, 0m);

            using var commandEntradas = connection.CreateCommand();
            commandEntradas.CommandText = @"
                SELECT COALESCE(SUM(Valor), 0)
                FROM MovimentacoesFinanceiras
                WHERE Tipo IN ('Entrada', 'Receita')
                  AND Data BETWEEN @dataInicio AND @dataFim";
            commandEntradas.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            commandEntradas.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

            decimal entradas = 0m;
            using (var reader = commandEntradas.ExecuteReader())
            {
                if (reader.Read())
                {
                    entradas = reader.GetDecimal(0);
                }
            }

            using var commandSaidas = connection.CreateCommand();
            commandSaidas.CommandText = @"
                SELECT COALESCE(SUM(Valor), 0)
                FROM MovimentacoesFinanceiras
                WHERE Tipo IN ('Saida', 'SaÃ­da', 'Despesa')
                  AND Data BETWEEN @dataInicio AND @dataFim";
            commandSaidas.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            commandSaidas.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

            decimal saidas = 0m;
            using (var reader = commandSaidas.ExecuteReader())
            {
                if (reader.Read())
                {
                    saidas = reader.GetDecimal(0);
                }
            }

            resumo = (entradas, saidas, entradas - saidas);
            return true;
        }

        private static void ValidarContaPagar(string fornecedor, string descricao, decimal valor, DateTime dataVencimento)
        {
            ComercialValidationHelper.GarantirTextoObrigatorio(fornecedor, "o fornecedor");
            ComercialValidationHelper.GarantirTextoObrigatorio(descricao, "a descricao da conta a pagar");
            ComercialValidationHelper.GarantirValorMaiorQueZero(valor, "o valor da conta a pagar");
        }

        private static void ValidarContaReceber(string cliente, string descricao, decimal valor, DateTime dataVencimento, DateTime? dataPagamento, string status)
        {
            ComercialValidationHelper.GarantirTextoObrigatorio(cliente, "o cliente");
            ComercialValidationHelper.GarantirTextoObrigatorio(descricao, "a descricao da conta a receber");
            ComercialValidationHelper.GarantirValorMaiorQueZero(valor, "o valor da conta a receber");

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusNormalizado = status.Trim();
                if (!string.Equals(statusNormalizado, "Pendente", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statusNormalizado, "Pago", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statusNormalizado, "Parcial", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statusNormalizado, "Cancelado", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Informe um status financeiro valido para a conta a receber.");
                }
            }
        }

        private static void ValidarMovimentacao(string tipo, string descricao, decimal valor, DateTime data)
        {
            ComercialValidationHelper.GarantirTextoObrigatorio(tipo, "o tipo da movimentacao");
            ComercialValidationHelper.GarantirTextoObrigatorio(descricao, "a descricao da movimentacao");
            ComercialValidationHelper.GarantirValorMaiorQueZero(valor, "o valor da movimentacao");
            var erroData = CadastroValidationHelper.ValidarDataNaoFutura(data, "a data da movimentacao", obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroData))
            {
                throw new InvalidOperationException(erroData);
            }
        }

        private static void ValidarMeta(string nome, decimal valorMeta, DateTime dataInicio, DateTime dataFim)
        {
            ComercialValidationHelper.GarantirTextoObrigatorio(nome, "o nome da meta");
            ComercialValidationHelper.GarantirValorMaiorQueZero(valorMeta, "o valor da meta");
            ComercialValidationHelper.GarantirDataFinalNaoAnterior(dataInicio, dataFim, "a data de inicio", "A data final");
        }

        private static void InserirContaReceber(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            string cliente,
            string descricao,
            decimal valor,
            DateTime dataVencimento,
            string formaPagamento,
            string observacoes,
            string status,
            DateTime? dataPagamento,
            string origem,
            string referenciaExterna)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO ContasReceber
                (
                    Cliente,
                    Descricao,
                    Valor,
                    DataVencimento,
                    DataPagamento,
                    Status,
                    FormaPagamento,
                    Observacoes,
                    DataCriacao,
                    Origem,
                    ReferenciaExterna
                )
                VALUES
                (
                    @cliente,
                    @descricao,
                    @valor,
                    @dataVencimento,
                    @dataPagamento,
                    @status,
                    @formaPagamento,
                    @observacoes,
                    @dataCriacao,
                    @origem,
                    @referenciaExterna
                )";

            command.Parameters.AddWithValue("@cliente", cliente);
            command.Parameters.AddWithValue("@descricao", descricao);
            command.Parameters.AddWithValue("@valor", valor);
            command.Parameters.AddWithValue("@dataVencimento", dataVencimento.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@dataPagamento", dataPagamento?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(status) ? "Pendente" : status.Trim());
            command.Parameters.AddWithValue("@formaPagamento", ToDbNullableString(formaPagamento));
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(observacoes));
            command.Parameters.AddWithValue("@dataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@origem", ToDbNullableString(origem));
            command.Parameters.AddWithValue("@referenciaExterna", ToDbNullableString(referenciaExterna));
            command.ExecuteNonQuery();
        }

        private static void AtualizarContaReceberIntegrada(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int id,
            string cliente,
            string descricao,
            decimal valor,
            DateTime dataVencimento,
            string formaPagamento,
            string observacoes,
            string status,
            DateTime? dataPagamento)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE ContasReceber
                SET
                    Cliente = @cliente,
                    Descricao = @descricao,
                    Valor = @valor,
                    DataVencimento = @dataVencimento,
                    DataPagamento = @dataPagamento,
                    Status = @status,
                    FormaPagamento = @formaPagamento,
                    Observacoes = @observacoes
                WHERE Id = @id";

            command.Parameters.AddWithValue("@cliente", cliente);
            command.Parameters.AddWithValue("@descricao", descricao);
            command.Parameters.AddWithValue("@valor", valor);
            command.Parameters.AddWithValue("@dataVencimento", dataVencimento.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@dataPagamento", dataPagamento?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(status) ? "Pendente" : status.Trim());
            command.Parameters.AddWithValue("@formaPagamento", ToDbNullableString(formaPagamento));
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(observacoes));
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        private static void InserirMovimentacao(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            string tipo,
            string descricao,
            decimal valor,
            DateTime data,
            string categoria,
            string formaPagamento,
            int? referenciaId,
            string observacoes,
            string origem,
            string referenciaExterna)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO MovimentacoesFinanceiras
                (
                    Tipo,
                    Descricao,
                    Valor,
                    Data,
                    Categoria,
                    FormaPagamento,
                    ReferenciaId,
                    Observacoes,
                    DataCriacao,
                    Origem,
                    ReferenciaExterna
                )
                VALUES
                (
                    @tipo,
                    @descricao,
                    @valor,
                    @data,
                    @categoria,
                    @formaPagamento,
                    @referenciaId,
                    @observacoes,
                    @dataCriacao,
                    @origem,
                    @referenciaExterna
                )";

            command.Parameters.AddWithValue("@tipo", tipo);
            command.Parameters.AddWithValue("@descricao", descricao);
            command.Parameters.AddWithValue("@valor", valor);
            command.Parameters.AddWithValue("@data", data.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@categoria", ToDbNullableString(categoria));
            command.Parameters.AddWithValue("@formaPagamento", ToDbNullableString(formaPagamento));
            command.Parameters.AddWithValue("@referenciaId", referenciaId.HasValue ? referenciaId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(observacoes));
            command.Parameters.AddWithValue("@dataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@origem", ToDbNullableString(origem));
            command.Parameters.AddWithValue("@referenciaExterna", ToDbNullableString(referenciaExterna));
            command.ExecuteNonQuery();
        }

        private static void AtualizarMovimentacaoIntegrada(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int id,
            string tipo,
            string descricao,
            decimal valor,
            DateTime data,
            string categoria,
            string formaPagamento,
            int? referenciaId,
            string observacoes)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE MovimentacoesFinanceiras
                SET
                    Tipo = @tipo,
                    Descricao = @descricao,
                    Valor = @valor,
                    Data = @data,
                    Categoria = @categoria,
                    FormaPagamento = @formaPagamento,
                    ReferenciaId = @referenciaId,
                    Observacoes = @observacoes
                WHERE Id = @id";

            command.Parameters.AddWithValue("@tipo", tipo);
            command.Parameters.AddWithValue("@descricao", descricao);
            command.Parameters.AddWithValue("@valor", valor);
            command.Parameters.AddWithValue("@data", data.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@categoria", ToDbNullableString(categoria));
            command.Parameters.AddWithValue("@formaPagamento", ToDbNullableString(formaPagamento));
            command.Parameters.AddWithValue("@referenciaId", referenciaId.HasValue ? referenciaId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(observacoes));
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        private static bool ExisteRegistroIntegrado(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            string origem,
            string referenciaExterna)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT 1
                FROM {tableName}
                WHERE Origem = @origem
                  AND ReferenciaExterna = @referenciaExterna
                LIMIT 1;";
            command.Parameters.AddWithValue("@origem", origem);
            command.Parameters.AddWithValue("@referenciaExterna", referenciaExterna);

            return command.ExecuteScalar() != null;
        }

        private static int? ObterIdRegistroIntegrado(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            string origem,
            string referenciaExterna)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT Id
                FROM {tableName}
                WHERE Origem = @origem
                  AND ReferenciaExterna = @referenciaExterna
                LIMIT 1;";
            command.Parameters.AddWithValue("@origem", origem);
            command.Parameters.AddWithValue("@referenciaExterna", referenciaExterna);

            var resultado = command.ExecuteScalar();
            return resultado == null || resultado == DBNull.Value
                ? null
                : Convert.ToInt32(resultado, CultureInfo.InvariantCulture);
        }

        private static void UpsertContaReceberIntegrada(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string cliente,
            string descricao,
            decimal valor,
            DateTime dataVencimento,
            string formaPagamento,
            string observacoes,
            string status,
            DateTime? dataPagamento,
            string origem,
            string referenciaExterna)
        {
            var idExistente = ObterIdRegistroIntegrado(connection, transaction, "ContasReceber", origem, referenciaExterna);
            if (idExistente.HasValue)
            {
                AtualizarContaReceberIntegrada(
                    connection,
                    transaction,
                    idExistente.Value,
                    cliente,
                    descricao,
                    valor,
                    dataVencimento,
                    formaPagamento,
                    observacoes,
                    status,
                    dataPagamento);
                return;
            }

            InserirContaReceber(
                connection,
                transaction,
                cliente,
                descricao,
                valor,
                dataVencimento,
                formaPagamento,
                observacoes,
                status,
                dataPagamento,
                origem,
                referenciaExterna);
        }

        private static void UpsertMovimentacaoIntegrada(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tipo,
            string descricao,
            decimal valor,
            DateTime data,
            string categoria,
            string formaPagamento,
            int? referenciaId,
            string observacoes,
            string origem,
            string referenciaExterna)
        {
            var idExistente = ObterIdRegistroIntegrado(connection, transaction, "MovimentacoesFinanceiras", origem, referenciaExterna);
            if (idExistente.HasValue)
            {
                AtualizarMovimentacaoIntegrada(
                    connection,
                    transaction,
                    idExistente.Value,
                    tipo,
                    descricao,
                    valor,
                    data,
                    categoria,
                    formaPagamento,
                    referenciaId,
                    observacoes);
                return;
            }

            InserirMovimentacao(
                connection,
                transaction,
                tipo,
                descricao,
                valor,
                data,
                categoria,
                formaPagamento,
                referenciaId,
                observacoes,
                origem,
                referenciaExterna);
        }

        private static decimal DeterminarValorServicoAgendamento(Agendamento agendamento)
        {
            if (agendamento.ValorReal > 0)
            {
                return agendamento.ValorReal;
            }

            if (agendamento.ValorServicos > 0 || agendamento.ValorProdutos > 0)
            {
                return agendamento.ValorServicos + agendamento.ValorProdutos;
            }

            return agendamento.ValorEstimado;
        }

        private static decimal DeterminarValorTotalOrdemServico(OrdemServico ordem)
        {
            var totalItens = ordem.Itens?.Sum(i => Math.Max(0m, i.Total)) ?? 0m;
            if (totalItens <= 0 && ordem.ValorMaoObra > 0)
            {
                totalItens = ordem.ValorMaoObra;
            }

            return Math.Max(0m, totalItens - ordem.Desconto);
        }

        private static string CriarDescricaoOrdemServico(OrdemServico ordem)
        {
            var descricao = string.IsNullOrWhiteSpace(ordem.PlacaSnapshot)
                ? ordem.ClienteNomeSnapshot
                : $"{ordem.ClienteNomeSnapshot} - {ordem.PlacaSnapshot}";

            return $"OS {ordem.Numero} - {descricao}".Trim(' ', '-');
        }

        private static string CriarDescricaoOrcamento(Orcamento orcamento)
        {
            var cliente = string.IsNullOrWhiteSpace(orcamento.Cliente?.Nome)
                ? "Cliente nao informado"
                : orcamento.Cliente.Nome.Trim();
            return $"Orcamento {orcamento.Numero} - {cliente}";
        }

        private static string BuildObservacoesIntegracao(OrdemServico ordem, decimal valorTotal)
        {
            return $"OS={ordem.Numero}; Cliente={ordem.ClienteNomeSnapshot}; Total={valorTotal:C}; Origem={ordem.Origem}";
        }

        private static string BuildObservacoesIntegracao(Orcamento orcamento, decimal valorTotal, bool pagamentoImediato)
        {
            return $"Orcamento={orcamento.Numero}; Cliente={orcamento.Cliente?.Nome ?? "Nao informado"}; Total={valorTotal:C}; Status={orcamento.Status}; PagamentoImediato={pagamentoImediato}";
        }

        private static decimal CalcularValorRecebidoAgendamento(Agendamento agendamento, decimal valorServico)
        {
            if (agendamento.ValorPago > 0)
            {
                return Math.Min(valorServico, agendamento.ValorPago);
            }

            return agendamento.Pago ? valorServico : 0m;
        }

        private static bool CondicaoPagamentoLiquidaNaHora(string formaPagamento)
        {
            if (string.IsNullOrWhiteSpace(formaPagamento))
            {
                return false;
            }

            var texto = formaPagamento.Trim().ToLowerInvariant();
            return texto.Contains("pix") ||
                   texto.Contains("dinheiro") ||
                   texto.Contains("debito") ||
                   texto.Contains("débito") ||
                   texto.Contains("credito") ||
                   texto.Contains("crédito") ||
                   texto.Contains("cartao") ||
                   texto.Contains("cartão") ||
                   texto.Contains("avista") ||
                   texto.Contains("a vista");
        }

        private static string CriarDescricaoAgendamento(Agendamento agendamento)
        {
            var numero = string.IsNullOrWhiteSpace(agendamento.Numero) ? agendamento.Id.ToString()[..8] : agendamento.Numero.Trim();
            var servico = string.IsNullOrWhiteSpace(agendamento.TipoServico) ? "Servico automotivo" : agendamento.TipoServico.Trim();
            var placa = string.IsNullOrWhiteSpace(agendamento.VeiculoPlaca) ? string.Empty : $" - {agendamento.VeiculoPlaca.Trim()}";
            return $"Agendamento {numero} - {servico}{placa}";
        }

        private static string BuildObservacoesIntegracao(Agendamento agendamento, decimal valorTotal, decimal valorRecebido, decimal valorEmAberto)
        {
            var observacoes = new List<string>
            {
                $"Integrado automaticamente do agendamento {agendamento.Id}.",
                $"Valor total: {valorTotal.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}."
            };

            if (valorRecebido > 0)
            {
                observacoes.Add($"Valor recebido: {valorRecebido.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}.");
            }

            if (valorEmAberto > 0)
            {
                observacoes.Add($"Saldo em aberto: {valorEmAberto.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}.");
            }

            if (!string.IsNullOrWhiteSpace(agendamento.Observacoes))
            {
                observacoes.Add($"Obs. servico: {agendamento.Observacoes.Trim()}");
            }

            return string.Join(" ", observacoes);
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static bool StatusContaReceberLiquidada(string? status)
        {
            return string.Equals(status, "Pago", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Recebida", StringComparison.OrdinalIgnoreCase);
        }

        private static bool StatusContaPagarLiquidada(string? status)
        {
            return string.Equals(status, "Paga", StringComparison.OrdinalIgnoreCase);
        }

        private static dynamic? ObterContaPagarPorId(SqliteConnection connection, SqliteTransaction transaction, int id)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT Id, Fornecedor, Descricao, Valor, DataVencimento, DataPagamento,
                       COALESCE(Status, ''), COALESCE(Categoria, ''), COALESCE(Observacoes, '')
                FROM ContasPagar
                WHERE Id = @id
                LIMIT 1;";
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new
            {
                Id = reader.GetInt32(0),
                Fornecedor = reader.GetString(1),
                Descricao = reader.GetString(2),
                Valor = reader.GetDecimal(3),
                DataVencimento = reader.GetString(4),
                DataPagamento = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = reader.GetString(6),
                Categoria = reader.GetString(7),
                Observacoes = reader.GetString(8)
            };
        }

        private static dynamic? ObterContaReceberPorId(SqliteConnection connection, SqliteTransaction transaction, int id)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT Id, Cliente, Descricao, Valor, DataVencimento, DataPagamento,
                       COALESCE(Status, ''), COALESCE(FormaPagamento, ''), COALESCE(Observacoes, '')
                FROM ContasReceber
                WHERE Id = @id
                LIMIT 1;";
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new
            {
                Id = reader.GetInt32(0),
                Cliente = reader.GetString(1),
                Descricao = reader.GetString(2),
                Valor = reader.GetDecimal(3),
                DataVencimento = reader.GetString(4),
                DataPagamento = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = reader.GetString(6),
                FormaPagamento = reader.GetString(7),
                Observacoes = reader.GetString(8)
            };
        }
    }
}
