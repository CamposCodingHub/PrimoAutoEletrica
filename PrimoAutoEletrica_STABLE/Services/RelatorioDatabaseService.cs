using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PrimoAutoEletrica.Services
{
    public class RelatorioDatabaseService
    {
        private readonly DatabaseService _databaseService;
        private readonly FinanceiroDatabaseService _financeiroDatabaseService;
        private readonly LoggerService _logger;

        public RelatorioDatabaseService()
        {
            _databaseService = global::PrimoAutoEletrica.App.Database;
            _financeiroDatabaseService = new FinanceiroDatabaseService();
            _logger = global::PrimoAutoEletrica.App.Logger;

            _ = new OrcamentoDatabaseService();

            InicializarTabelas();
        }

        private DbConnection GetConnection()
        {
            return _databaseService.GetConnection();
        }

        private void InicializarTabelas()
        {
            using var connection = GetConnection();
            connection.Open();

            if (IsSqlServerConnection(connection))
            {
                // O SQL Server e provisionado pelo schema mestre.
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS Relatorios (
                    Id TEXT PRIMARY KEY,
                    Tipo TEXT NOT NULL,
                    Nome TEXT NOT NULL,
                    DataGeracao TEXT NOT NULL,
                    DataInicio TEXT NOT NULL,
                    DataFim TEXT NOT NULL,
                    UsuarioGerou TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    FiltrosAplicados TEXT,
                    ValorTotal REAL,
                    TotalRegistros INTEGER,
                    CaminhoArquivo TEXT,
                    Observacoes TEXT
                )";
            command.ExecuteNonQuery();

            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS Auditoria (
                    Id TEXT PRIMARY KEY,
                    DataHora TEXT NOT NULL,
                    Usuario TEXT NOT NULL,
                    Acao TEXT NOT NULL,
                    Tabela TEXT NOT NULL,
                    RegistroId TEXT NOT NULL,
                    ValorAnterior TEXT,
                    ValorNovo TEXT,
                    IP TEXT
                )";
            command.ExecuteNonQuery();

            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS Metas (
                    Id TEXT PRIMARY KEY,
                    Tipo TEXT NOT NULL,
                    Periodo TEXT NOT NULL,
                    MetaValor REAL NOT NULL,
                    ValorAtual REAL,
                    PercentualAtingido REAL,
                    Responsavel TEXT,
                    DataInicio TEXT NOT NULL,
                    DataFim TEXT NOT NULL,
                    Status TEXT NOT NULL
                )";
            command.ExecuteNonQuery();

            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS Alertas (
                    Id TEXT PRIMARY KEY,
                    Tipo TEXT NOT NULL,
                    Mensagem TEXT NOT NULL,
                    Severidade TEXT NOT NULL,
                    DataGeracao TEXT NOT NULL,
                    Lido INTEGER DEFAULT 0,
                    Origem TEXT
                )";
            command.ExecuteNonQuery();

            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS Timeline (
                    Id TEXT PRIMARY KEY,
                    DataHora TEXT NOT NULL,
                    TipoEvento TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    Usuario TEXT NOT NULL,
                    Valor REAL,
                    Categoria TEXT
                )";
            command.ExecuteNonQuery();

            MigrarDadosRelatorioLegados(connection);
        }

        private static void MigrarDadosRelatorioLegados(DbConnection connection)
        {
            var legacyPath = ObterCaminhoBancoRelatorioLegado();
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
                    "Relatorios",
                    "Auditoria",
                    "Metas",
                    "Alertas",
                    "Timeline"
                })
                {
                    if (!TabelaExiste(connection, "legado", tableName) ||
                        !TabelaTemDados(connection, "legado", tableName) ||
                        TabelaTemDados(connection, "main", tableName))
                    {
                        continue;
                    }

                    var copyCommand = connection.CreateCommand();
                    copyCommand.CommandText = $@"
                        INSERT INTO main.{tableName}
                        SELECT *
                        FROM legado.{tableName};";
                    copyCommand.ExecuteNonQuery();
                }
            }
            finally
            {
                var detachCommand = connection.CreateCommand();
                detachCommand.CommandText = "DETACH DATABASE legado;";
                detachCommand.ExecuteNonQuery();
            }
        }

        private static string? ObterCaminhoBancoRelatorioLegado()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "primocautoeletrica.db"),
                Path.Combine(Environment.CurrentDirectory, "Data", "primocautoeletrica.db")
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

        public List<DadoFinanceiro> ObterDadosFinanceiros(
            DateTime dataInicio,
            DateTime dataFim,
            string categoria = "",
            string formaPagamento = "",
            int limit = 250)
        {
            return _logger.Measure("Relatorios", $"ObterDadosFinanceiros(limit={limit})", () =>
            {
                var dados = new List<DadoFinanceiro>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "MovimentacoesFinanceiras"))
                {
                    return dados;
                }

                var filtros = new List<string>
                {
                    "Data BETWEEN @dataInicio AND @dataFim"
                };

                var command = connection.CreateCommand();
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    filtros.Add("COALESCE(Categoria, '') = @categoria");
                    command.Parameters.AddWithValue("@categoria", categoria.Trim());
                }

                if (!string.IsNullOrWhiteSpace(formaPagamento))
                {
                    filtros.Add("COALESCE(FormaPagamento, '') = @formaPagamento");
                    command.Parameters.AddWithValue("@formaPagamento", formaPagamento.Trim());
                }

                command.CommandText = $@"
                    SELECT Id, Data, Tipo, Categoria, Descricao, Valor, FormaPagamento
                    FROM MovimentacoesFinanceiras
                    WHERE {string.Join(" AND ", filtros)}
                    ORDER BY Data DESC, Id DESC
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 250));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dados.Add(new DadoFinanceiro
                    {
                        Id = LerGuid(reader, 0, "financeiro"),
                        Data = LerData(reader, 1),
                        Tipo = LerTexto(reader, 2),
                        Categoria = LerTexto(reader, 3),
                        Descricao = LerTexto(reader, 4),
                        Valor = LerDecimal(reader, 5),
                        FormaPagamento = LerTexto(reader, 6),
                        Usuario = string.Empty
                    });
                }

                return dados;
            }, warningThresholdMs: 500);
        }

        public decimal ObterFaturamentoTotal(DateTime dataInicio, DateTime dataFim)
        {
            using var connection = GetConnection();
            connection.Open();

            if (TabelaExiste(connection, "main", "Vendas"))
            {
                var totalColumn = ColunaExiste(connection, "Vendas", "ValorTotal")
                    ? "ValorTotal"
                    : "Total";
                var statusFilter = ColunaExiste(connection, "Vendas", "Status")
                    ? "AND Status IN ('Concluida', 'Concluída', 'Concluída')"
                    : string.Empty;

                var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT COALESCE(SUM({totalColumn}), 0)
                    FROM Vendas
                    WHERE {DateOnlyExpression(connection, "Data")} BETWEEN @dataInicio AND @dataFim
                      {statusFilter}";
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                return ConverterParaDecimal(command.ExecuteScalar());
            }

            if (!TabelaExiste(connection, "main", "MovimentacoesFinanceiras"))
            {
                return 0;
            }

            var fallback = connection.CreateCommand();
            fallback.CommandText = $@"
                SELECT COALESCE(SUM(Valor), 0)
                FROM MovimentacoesFinanceiras
                WHERE Data BETWEEN @dataInicio AND @dataFim
                  AND Tipo IN ('Entrada', 'Receita')";
            fallback.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            fallback.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

            return ConverterParaDecimal(fallback.ExecuteScalar());
        }

        public decimal ObterLucroLiquido(DateTime dataInicio, DateTime dataFim)
        {
            using var connection = GetConnection();
            connection.Open();

            if (TabelaExiste(connection, "main", "Vendas"))
            {
                var lucroExpression = ColunaExiste(connection, "Vendas", "Lucro")
                    ? "Lucro"
                    : ColunaExiste(connection, "Vendas", "ValorTotal")
                        ? "ValorTotal"
                        : "Total";
                var statusFilter = ColunaExiste(connection, "Vendas", "Status")
                    ? "AND Status IN ('Concluida', 'Concluída', 'Concluída')"
                    : string.Empty;

                var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT COALESCE(SUM({lucroExpression}), 0)
                    FROM Vendas
                    WHERE {DateOnlyExpression(connection, "Data")} BETWEEN @dataInicio AND @dataFim
                      {statusFilter}";
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                return ConverterParaDecimal(command.ExecuteScalar());
            }

            if (!TabelaExiste(connection, "main", "MovimentacoesFinanceiras"))
            {
                return 0;
            }

            var fallback = connection.CreateCommand();
            fallback.CommandText = $@"
                SELECT COALESCE(SUM(
                    CASE
                        WHEN Tipo IN ('Entrada', 'Receita') THEN Valor
                        WHEN Tipo IN ('Saída', 'Saída', 'Despesa') THEN -Valor
                        ELSE 0
                    END
                ), 0)
                FROM MovimentacoesFinanceiras
                WHERE Data BETWEEN @dataInicio AND @dataFim";
            fallback.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            fallback.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

            return ConverterParaDecimal(fallback.ExecuteScalar());
        }

        public DemonstrativoResultadoFinanceiro ObterDemonstrativoResultado(DateTime dataInicio, DateTime dataFim)
        {
            return _logger.Measure("Relatorios", "ObterDemonstrativoResultado", () =>
                _financeiroDatabaseService.ObterDemonstrativoResultado(dataInicio, dataFim), warningThresholdMs: 500);
        }

        public List<DadoConciliacaoFinanceira> ObterConciliacaoFinanceira(DateTime dataInicio, DateTime dataFim)
        {
            return _logger.Measure("Relatorios", "ObterConciliacaoFinanceira", () =>
            {
                var dados = new Dictionary<string, DadoConciliacaoFinanceira>(StringComparer.OrdinalIgnoreCase);
                using var connection = GetConnection();
                connection.Open();

                if (TabelaExiste(connection, "main", "Vendas"))
                {
                    var totalExpression = ColunaExiste(connection, "Vendas", "ValorTotal")
                        ? "COALESCE(ValorTotal, 0)"
                        : "COALESCE(Total, 0)";
                    var filtros = new List<string>
                    {
                        $"{DateOnlyExpression(connection, "Data")} BETWEEN @dataInicio AND @dataFim"
                    };

                    if (ColunaExiste(connection, "Vendas", "Status"))
                    {
                        filtros.Add("COALESCE(Status, 'Concluida') NOT IN ('Cancelada', 'Cancelado')");
                    }

                    using var vendasCommand = connection.CreateCommand();
                    vendasCommand.CommandText = $@"
                        SELECT COALESCE(NULLIF(FormaPagamento, ''), 'Nao informado'),
                               COUNT(*),
                               COALESCE(SUM({totalExpression}), 0)
                        FROM Vendas
                        WHERE {string.Join(" AND ", filtros)}
                        GROUP BY COALESCE(NULLIF(FormaPagamento, ''), 'Nao informado')";
                    vendasCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                    vendasCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                    using var vendasReader = vendasCommand.ExecuteReader();
                    while (vendasReader.Read())
                    {
                        var item = ObterOuCriarConciliacao(dados, LerTexto(vendasReader, 0));
                        item.QuantidadeVendas = LerInteiro(vendasReader, 1);
                        item.TotalVendas = LerDecimal(vendasReader, 2);
                    }
                }

                if (TabelaExiste(connection, "main", "MovimentacoesFinanceiras"))
                {
                    using var financeiroCommand = connection.CreateCommand();
                    financeiroCommand.CommandText = $@"
                        SELECT COALESCE(NULLIF(FormaPagamento, ''), 'Nao informado'),
                               COUNT(*),
                               COALESCE(SUM(Valor), 0)
                        FROM MovimentacoesFinanceiras
                        WHERE {DateOnlyExpression(connection, "Data")} BETWEEN @dataInicio AND @dataFim
                          AND COALESCE(Tipo, '') IN ('Entrada', 'Receita')
                        GROUP BY COALESCE(NULLIF(FormaPagamento, ''), 'Nao informado')";
                    financeiroCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                    financeiroCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                    using var financeiroReader = financeiroCommand.ExecuteReader();
                    while (financeiroReader.Read())
                    {
                        var item = ObterOuCriarConciliacao(dados, LerTexto(financeiroReader, 0));
                        item.QuantidadeMovimentacoes = LerInteiro(financeiroReader, 1);
                        item.EntradasFinanceiras = LerDecimal(financeiroReader, 2);
                    }
                }

                return dados.Values
                    .OrderByDescending(item => Math.Abs(item.Diferenca))
                    .ThenBy(item => item.FormaPagamento, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }, warningThresholdMs: 500);
        }

        public List<DadoOrdemServicoRelatorio> ObterOrdensServicoRelatorio(
            DateTime dataInicio,
            DateTime dataFim,
            bool finalizadas,
            int limit = 100)
        {
            return _logger.Measure("Relatorios", finalizadas ? "ObterOsFinalizadas" : "ObterOsAbertas", () =>
            {
                var dados = new List<DadoOrdemServicoRelatorio>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "OrdensServico"))
                {
                    return dados;
                }

                var possuiItens = TabelaExiste(connection, "main", "OrdemServicoItens");
                var possuiFuncionarios = TabelaExiste(connection, "main", "Funcionarios");
                var tecnicoJoin = possuiFuncionarios
                    ? "LEFT JOIN Funcionarios f ON f.Id = os.TecnicoId"
                    : string.Empty;
                var tecnicoExpression = possuiFuncionarios
                    ? "COALESCE(NULLIF(f.Nome, ''), 'Sem tecnico')"
                    : "'Sem tecnico'";
                var itemValorExpression = possuiItens
                    ? "COALESCE((SELECT SUM(COALESCE(i.Quantidade, 0) * COALESCE(i.ValorUnitario, 0)) FROM OrdemServicoItens i WHERE i.OrdemServicoId = os.Id), 0)"
                    : "0";
                var itemCustoExpression = possuiItens
                    ? "COALESCE((SELECT SUM(COALESCE(i.Quantidade, 0) * COALESCE(i.CustoUnitario, 0)) FROM OrdemServicoItens i WHERE i.OrdemServicoId = os.Id), 0)"
                    : "0";
                var itemCountExpression = possuiItens
                    ? "COALESCE((SELECT COUNT(*) FROM OrdemServicoItens i WHERE i.OrdemServicoId = os.Id), 0)"
                    : "0";
                var dataReferencia = finalizadas
                    ? $"{DateOnlyExpression(connection, "COALESCE(os.DataEntrega, os.DataConclusao, os.DataAbertura)")}"
                    : $"{DateOnlyExpression(connection, "os.DataAbertura")}";
                var statusFiltro = finalizadas
                    ? "LOWER(COALESCE(os.Status, '')) IN ('entregue', 'finalizada', 'finalizado')"
                    : "LOWER(COALESCE(os.Status, '')) NOT IN ('entregue', 'finalizada', 'finalizado', 'cancelada', 'cancelado')";

                using var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT os.Id,
                           os.Numero,
                           COALESCE(NULLIF(os.ClienteNomeSnapshot, ''), 'Cliente nao informado'),
                           {tecnicoExpression},
                           COALESCE(os.Status, ''),
                           os.DataAbertura,
                           COALESCE(os.DataEntrega, os.DataConclusao),
                           (COALESCE(os.ValorMaoObra, 0) + {itemValorExpression} - COALESCE(os.Desconto, 0)) AS ValorTotal,
                           (COALESCE(os.ValorMaoObra, 0) + {itemValorExpression} - {itemCustoExpression} - COALESCE(os.Desconto, 0)) AS LucroBruto,
                           {itemCountExpression} AS TotalItens,
                           COALESCE(os.TempoRealMinutos, 0)
                    FROM OrdensServico os
                    {tecnicoJoin}
                    WHERE COALESCE(os.Ativo, 1) = 1
                      AND {dataReferencia} BETWEEN @dataInicio AND @dataFim
                      AND {statusFiltro}
                    ORDER BY {dataReferencia} DESC, os.Numero DESC
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 100));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dados.Add(new DadoOrdemServicoRelatorio
                    {
                        Id = LerGuid(reader, 0, "ordem-servico"),
                        Numero = LerTexto(reader, 1),
                        ClienteNome = LerTexto(reader, 2),
                        TecnicoNome = LerTexto(reader, 3),
                        Status = LerTexto(reader, 4),
                        DataAbertura = LerData(reader, 5),
                        DataConclusao = LerDataOpcional(reader, 6),
                        ValorTotal = LerDecimal(reader, 7),
                        LucroBruto = LerDecimal(reader, 8),
                        TotalItens = LerInteiro(reader, 9),
                        TempoRealMinutos = LerInteiro(reader, 10)
                    });
                }

                return dados;
            }, warningThresholdMs: 600);
        }

        public List<DadoOrdemServicoTecnico> ObterOrdensServicoPorTecnico(DateTime dataInicio, DateTime dataFim, int limit = 100)
        {
            return _logger.Measure("Relatorios", "ObterOrdensServicoPorTecnico", () =>
            {
                var dados = new List<DadoOrdemServicoTecnico>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "OrdensServico"))
                {
                    return dados;
                }

                var possuiItens = TabelaExiste(connection, "main", "OrdemServicoItens");
                var possuiFuncionarios = TabelaExiste(connection, "main", "Funcionarios");
                var tecnicoJoin = possuiFuncionarios
                    ? "LEFT JOIN Funcionarios f ON f.Id = os.TecnicoId"
                    : string.Empty;
                var tecnicoExpression = possuiFuncionarios
                    ? "COALESCE(NULLIF(f.Nome, ''), 'Sem tecnico')"
                    : "'Sem tecnico'";
                var itemValorExpression = possuiItens
                    ? "COALESCE((SELECT SUM(COALESCE(i.Quantidade, 0) * COALESCE(i.ValorUnitario, 0)) FROM OrdemServicoItens i WHERE i.OrdemServicoId = os.Id), 0)"
                    : "0";
                var itemCustoExpression = possuiItens
                    ? "COALESCE((SELECT SUM(COALESCE(i.Quantidade, 0) * COALESCE(i.CustoUnitario, 0)) FROM OrdemServicoItens i WHERE i.OrdemServicoId = os.Id), 0)"
                    : "0";

                using var command = connection.CreateCommand();
                command.CommandText = $@"
                    WITH os_totais AS
                    (
                        SELECT os.Id,
                               {tecnicoExpression} AS TecnicoNome,
                               LOWER(COALESCE(os.Status, '')) AS StatusNormalizado,
                               (COALESCE(os.ValorMaoObra, 0) + {itemValorExpression} - COALESCE(os.Desconto, 0)) AS ValorTotal,
                               (COALESCE(os.ValorMaoObra, 0) + {itemValorExpression} - {itemCustoExpression} - COALESCE(os.Desconto, 0)) AS LucroBruto,
                               COALESCE(os.TempoRealMinutos, 0) AS TempoRealMinutos
                        FROM OrdensServico os
                        {tecnicoJoin}
                        WHERE COALESCE(os.Ativo, 1) = 1
                          AND {DateOnlyExpression(connection, "COALESCE(os.DataEntrega, os.DataConclusao, os.DataAbertura)")} BETWEEN @dataInicio AND @dataFim
                          AND LOWER(COALESCE(os.Status, '')) NOT IN ('cancelada', 'cancelado')
                    )
                    SELECT TecnicoNome,
                           COALESCE(SUM(CASE WHEN StatusNormalizado NOT IN ('entregue', 'finalizada', 'finalizado') THEN 1 ELSE 0 END), 0) AS OrdensAbertas,
                           COALESCE(SUM(CASE WHEN StatusNormalizado IN ('entregue', 'finalizada', 'finalizado') THEN 1 ELSE 0 END), 0) AS OrdensFinalizadas,
                           COALESCE(SUM(ValorTotal), 0) AS ValorTotal,
                           COALESCE(SUM(LucroBruto), 0) AS LucroBruto,
                           COALESCE(SUM(TempoRealMinutos), 0) AS TempoRealMinutos
                    FROM os_totais
                    GROUP BY TecnicoNome
                    ORDER BY ValorTotal DESC, OrdensFinalizadas DESC, TecnicoNome
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 100));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dados.Add(new DadoOrdemServicoTecnico
                    {
                        TecnicoNome = LerTexto(reader, 0),
                        OrdensAbertas = LerInteiro(reader, 1),
                        OrdensFinalizadas = LerInteiro(reader, 2),
                        ValorTotal = LerDecimal(reader, 3),
                        LucroBruto = LerDecimal(reader, 4),
                        TempoRealMinutos = LerInteiro(reader, 5)
                    });
                }

                return dados;
            }, warningThresholdMs: 600);
        }

        public List<DadoServicoRelatorio> ObterServicosRelatorio(DateTime dataInicio, DateTime dataFim, int limit = 100)
        {
            return _logger.Measure("Relatorios", "ObterServicosRelatorio", () =>
            {
                var dados = new Dictionary<string, DadoServicoRelatorio>(StringComparer.OrdinalIgnoreCase);
                using var connection = GetConnection();
                connection.Open();

                if (TabelaExiste(connection, "main", "OrdensServico") &&
                    TabelaExiste(connection, "main", "OrdemServicoItens"))
                {
                    using var osCommand = connection.CreateCommand();
                    osCommand.CommandText = $@"
                        SELECT COALESCE(NULLIF(i.Descricao, ''), 'Servico sem descricao') AS Servico,
                               COALESCE(SUM(COALESCE(i.Quantidade, 0)), 0) AS Quantidade,
                               COALESCE(SUM(COALESCE(i.Quantidade, 0) * COALESCE(i.ValorUnitario, 0)), 0) AS ReceitaTotal,
                               COALESCE(SUM(COALESCE(i.Quantidade, 0) * COALESCE(i.CustoUnitario, 0)), 0) AS CustoTotal,
                               MAX(COALESCE(os.DataEntrega, os.DataConclusao, os.DataAbertura)) AS UltimaExecucao
                        FROM OrdemServicoItens i
                        INNER JOIN OrdensServico os ON os.Id = i.OrdemServicoId
                        WHERE LOWER(COALESCE(i.Tipo, '')) = 'servico'
                          AND COALESCE(os.Ativo, 1) = 1
                          AND LOWER(COALESCE(os.Status, '')) NOT IN ('cancelada', 'cancelado')
                          AND {DateOnlyExpression(connection, "COALESCE(os.DataEntrega, os.DataConclusao, os.DataAbertura)")} BETWEEN @dataInicio AND @dataFim
                        GROUP BY COALESCE(NULLIF(i.Descricao, ''), 'Servico sem descricao')";
                    osCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                    osCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                    using var osReader = osCommand.ExecuteReader();
                    while (osReader.Read())
                    {
                        AcumularServico(
                            dados,
                            LerTexto(osReader, 0),
                            "OS",
                            LerDecimal(osReader, 1),
                            LerDecimal(osReader, 2),
                            LerDecimal(osReader, 3),
                            LerData(osReader, 4));
                    }
                }

                if (TabelaExiste(connection, "main", "Vendas") &&
                    TabelaExiste(connection, "main", "VendaItens"))
                {
                    var receitaExpression = ColunaExiste(connection, "VendaItens", "Subtotal")
                        ? "COALESCE(vi.Subtotal, 0)"
                        : "(COALESCE(vi.PrecoUnitario, 0) * COALESCE(vi.Quantidade, 0) - COALESCE(vi.Desconto, 0))";
                    var custoExpression = ColunaExiste(connection, "VendaItens", "CustoUnitario")
                        ? "COALESCE(vi.CustoUnitario, 0) * COALESCE(vi.Quantidade, 0)"
                        : "0";
                    var filtros = CriarFiltrosPeriodoVendas(connection);

                    using var vendasCommand = connection.CreateCommand();
                    vendasCommand.CommandText = $@"
                        SELECT COALESCE(NULLIF(vi.DescricaoItem, ''), NULLIF(vi.ProdutoNome, ''), 'Servico sem descricao') AS Servico,
                               COALESCE(SUM(COALESCE(vi.Quantidade, 0)), 0) AS Quantidade,
                               COALESCE(SUM({receitaExpression}), 0) AS ReceitaTotal,
                               COALESCE(SUM({custoExpression}), 0) AS CustoTotal,
                               MAX(v.Data) AS UltimaExecucao
                        FROM VendaItens vi
                        INNER JOIN Vendas v ON v.Id = vi.VendaId
                        WHERE LOWER(COALESCE(vi.Tipo, '')) = 'servico'
                          AND {string.Join(" AND ", filtros)}
                        GROUP BY COALESCE(NULLIF(vi.DescricaoItem, ''), NULLIF(vi.ProdutoNome, ''), 'Servico sem descricao')";
                    vendasCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                    vendasCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                    using var vendasReader = vendasCommand.ExecuteReader();
                    while (vendasReader.Read())
                    {
                        AcumularServico(
                            dados,
                            LerTexto(vendasReader, 0),
                            "PDV",
                            LerDecimal(vendasReader, 1),
                            LerDecimal(vendasReader, 2),
                            LerDecimal(vendasReader, 3),
                            LerData(vendasReader, 4));
                    }
                }

                return dados.Values
                    .OrderByDescending(item => item.Quantidade)
                    .ThenByDescending(item => item.ReceitaTotal)
                    .ThenBy(item => item.Servico, StringComparer.OrdinalIgnoreCase)
                    .Take(LimitarQuantidade(limit, 100))
                    .ToList();
            }, warningThresholdMs: 700);
        }

        public List<DadoVenda> ObterDadosVendas(
            DateTime dataInicio,
            DateTime dataFim,
            string vendedor = "",
            string cliente = "",
            string formaPagamento = "",
            string status = "",
            int limit = 250)
        {
            return _logger.Measure("Relatorios", $"ObterDadosVendas(limit={limit})", () =>
            {
                var dados = new List<DadoVenda>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Vendas"))
                {
                    return dados;
                }

                var totalExpression = ColunaExiste(connection, "Vendas", "ValorTotal")
                    ? "v.ValorTotal"
                    : "v.Total";
                var lucroExpression = ColunaExiste(connection, "Vendas", "Lucro")
                    ? "v.Lucro"
                    : totalExpression;
                var statusExpression = ColunaExiste(connection, "Vendas", "Status")
                    ? "COALESCE(v.Status, '')"
                    : "'Concluida'";
                var itensExpression = ColunaExiste(connection, "Vendas", "ItensQuantidade")
                    ? "COALESCE(v.ItensQuantidade, 0)"
                    : "COALESCE(v.QuantidadeItens, 0)";
                var hasVendedorId = ColunaExiste(connection, "Vendas", "VendedorId");
                var vendedorIdExpression = hasVendedorId ? "v.VendedorId" : "v.Usuario";
                var vendedorNomeExpression = hasVendedorId ? "COALESCE(f.Nome, '')" : "COALESCE(v.Usuario, '')";
                var vendedorJoin = hasVendedorId
                    ? $"LEFT JOIN Funcionarios f ON {TextCastExpression(connection, "v.VendedorId")} = {TextCastExpression(connection, "f.Id")}"
                    : string.Empty;

                var filtros = new List<string>
                {
                    $"{DateOnlyExpression(connection, "v.Data")} BETWEEN @dataInicio AND @dataFim"
                };

                var command = connection.CreateCommand();
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                if (!string.IsNullOrWhiteSpace(vendedor))
                {
                    filtros.Add($"{vendedorNomeExpression} LIKE @vendedor");
                    command.Parameters.AddWithValue("@vendedor", CriarFiltroLike(vendedor));
                }

                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    filtros.Add("COALESCE(c.Nome, '') LIKE @cliente");
                    command.Parameters.AddWithValue("@cliente", CriarFiltroLike(cliente));
                }

                if (!string.IsNullOrWhiteSpace(formaPagamento))
                {
                    filtros.Add("COALESCE(v.FormaPagamento, '') = @formaPagamento");
                    command.Parameters.AddWithValue("@formaPagamento", formaPagamento.Trim());
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    filtros.Add($"{statusExpression} = @status");
                    command.Parameters.AddWithValue("@status", status.Trim());
                }

                command.CommandText = $@"
                    SELECT v.Id, v.Data, v.ClienteId, COALESCE(c.Nome, ''), {vendedorIdExpression},
                           {vendedorNomeExpression}, {totalExpression}, v.Desconto, {lucroExpression},
                           COALESCE(v.FormaPagamento, ''), {statusExpression}, {itensExpression}
                    FROM Vendas v
                    LEFT JOIN Clientes c ON v.ClienteId = c.Id
                    {vendedorJoin}
                    WHERE {string.Join(" AND ", filtros)}
                    ORDER BY v.Data DESC, v.Id DESC
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 250));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dados.Add(new DadoVenda
                    {
                        Id = LerGuid(reader, 0, "venda"),
                        Data = LerData(reader, 1),
                        ClienteId = LerGuid(reader, 2, "cliente"),
                        ClienteNome = LerTexto(reader, 3),
                        VendedorId = LerGuid(reader, 4, "vendedor"),
                        VendedorNome = LerTexto(reader, 5),
                        ValorTotal = LerDecimal(reader, 6),
                        Desconto = LerDecimal(reader, 7),
                        Lucro = LerDecimal(reader, 8),
                        FormaPagamento = LerTexto(reader, 9),
                        Status = LerTexto(reader, 10),
                        ItensQuantidade = LerInteiro(reader, 11)
                    });
                }

                return dados;
            }, warningThresholdMs: 500);
        }

        public decimal ObterTicketMedio(DateTime dataInicio, DateTime dataFim)
        {
            return _logger.Measure("Relatorios", "ObterTicketMedio", () =>
            {
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Vendas"))
                {
                    return 0m;
                }

                var totalExpression = ColunaExiste(connection, "Vendas", "ValorTotal")
                    ? "ValorTotal"
                    : "Total";
                var statusFilter = ColunaExiste(connection, "Vendas", "Status")
                    ? "AND Status IN ('Concluida', 'Concluída', 'Concluida em Venda', 'Concluída em Venda')"
                    : string.Empty;

                var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT COALESCE(AVG({totalExpression}), 0)
                    FROM Vendas
                    WHERE {DateOnlyExpression(connection, "Data")} BETWEEN @dataInicio AND @dataFim
                      {statusFilter}";
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                return ConverterParaDecimal(command.ExecuteScalar());
            }, warningThresholdMs: 400);
        }

        public List<DadoMargemProduto> ObterMargemPorProduto(
            DateTime dataInicio,
            DateTime dataFim,
            string categoria = "",
            int limit = 50)
        {
            return _logger.Measure("Relatorios", $"ObterMargemPorProduto(limit={limit})", () =>
            {
                var dados = new List<DadoMargemProduto>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Vendas") ||
                    !TabelaExiste(connection, "main", "VendaItens"))
                {
                    return dados;
                }

                var produtosDisponiveis = TabelaExiste(connection, "main", "Produtos");
                var categoriaDisponivel = produtosDisponiveis && ColunaExiste(connection, "Produtos", "Categoria");
                var produtoJoin = produtosDisponiveis
                    ? $"LEFT JOIN Produtos p ON {TextCastExpression(connection, "vi.ProdutoId")} = {TextCastExpression(connection, "p.Id")}"
                    : string.Empty;
                var produtoIdExpression = $"COALESCE(NULLIF({TextCastExpression(connection, "vi.ProdutoId")}, ''), '00000000-0000-0000-0000-000000000000')";
                var produtoNomeExpression = produtosDisponiveis
                    ? "COALESCE(NULLIF(p.Nome, ''), NULLIF(vi.ProdutoNome, ''), NULLIF(vi.DescricaoItem, ''), 'Item sem nome')"
                    : "COALESCE(NULLIF(vi.ProdutoNome, ''), NULLIF(vi.DescricaoItem, ''), 'Item sem nome')";
                var categoriaExpression = categoriaDisponivel
                    ? "COALESCE(NULLIF(p.Categoria, ''), 'Sem categoria')"
                    : "'Sem categoria'";
                var receitaExpression = ColunaExiste(connection, "VendaItens", "Subtotal")
                    ? "COALESCE(vi.Subtotal, 0)"
                    : "(COALESCE(vi.PrecoUnitario, 0) * COALESCE(vi.Quantidade, 0) - COALESCE(vi.Desconto, 0))";
                var custoExpression = ColunaExiste(connection, "VendaItens", "CustoUnitario")
                    ? "COALESCE(vi.CustoUnitario, 0) * COALESCE(vi.Quantidade, 0)"
                    : "0";

                var filtros = new List<string>
                {
                    $"{DateOnlyExpression(connection, "v.Data")} BETWEEN @dataInicio AND @dataFim"
                };

                if (ColunaExiste(connection, "Vendas", "Status"))
                {
                    filtros.Add("COALESCE(v.Status, 'Concluida') NOT IN ('Cancelada', 'Cancelado')");
                }

                var command = connection.CreateCommand();
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                if (!string.IsNullOrWhiteSpace(categoria) && categoriaDisponivel)
                {
                    filtros.Add($"{categoriaExpression} = @categoria");
                    command.Parameters.AddWithValue("@categoria", categoria.Trim());
                }

                command.CommandText = $@"
                    SELECT {produtoIdExpression},
                           {produtoNomeExpression},
                           {categoriaExpression},
                           COALESCE(SUM(COALESCE(vi.Quantidade, 0)), 0) AS QuantidadeVendida,
                           COALESCE(SUM({receitaExpression}), 0) AS ReceitaTotal,
                           COALESCE(SUM({custoExpression}), 0) AS CustoTotal
                    FROM VendaItens vi
                    INNER JOIN Vendas v ON {TextCastExpression(connection, "vi.VendaId")} = {TextCastExpression(connection, "v.Id")}
                    {produtoJoin}
                    WHERE {string.Join(" AND ", filtros)}
                    GROUP BY {produtoIdExpression}, {produtoNomeExpression}, {categoriaExpression}
                    HAVING COALESCE(SUM({receitaExpression}), 0) > 0
                       OR COALESCE(SUM(COALESCE(vi.Quantidade, 0)), 0) > 0
                    ORDER BY (COALESCE(SUM({receitaExpression}), 0) - COALESCE(SUM({custoExpression}), 0)) DESC,
                             COALESCE(SUM({receitaExpression}), 0) DESC
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 50));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var receita = LerDecimal(reader, 4);
                    var custo = LerDecimal(reader, 5);
                    var lucro = receita - custo;

                    dados.Add(new DadoMargemProduto
                    {
                        ProdutoId = LerGuid(reader, 0, "produto-margem"),
                        ProdutoNome = LerTexto(reader, 1),
                        Categoria = LerTexto(reader, 2),
                        QuantidadeVendida = LerInteiro(reader, 3),
                        ReceitaTotal = receita,
                        CustoTotal = custo,
                        LucroBruto = lucro,
                        MargemPercentual = receita > 0 ? lucro / receita * 100m : 0m
                    });
                }

                return dados;
            }, warningThresholdMs: 600);
        }

        public List<DadoVendaPeriodo> ObterVendasPorHora(DateTime dataInicio, DateTime dataFim)
        {
            return _logger.Measure("Relatorios", "ObterVendasPorHora", () =>
            {
                var dados = new List<DadoVendaPeriodo>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Vendas"))
                {
                    return dados;
                }

                var totalExpression = ColunaExiste(connection, "Vendas", "ValorTotal")
                    ? "COALESCE(v.ValorTotal, 0)"
                    : "COALESCE(v.Total, 0)";
                var itensExpression = ColunaExiste(connection, "Vendas", "ItensQuantidade")
                    ? "COALESCE(v.ItensQuantidade, 0)"
                    : "COALESCE(v.QuantidadeItens, 0)";
                var filtros = CriarFiltrosPeriodoVendas(connection);

                var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT {HourExpression(connection, "v.Data")} AS HoraVenda,
                           COUNT(*) AS QuantidadeVendas,
                           COALESCE(SUM({itensExpression}), 0) AS ItensVendidos,
                           COALESCE(SUM({totalExpression}), 0) AS Faturamento,
                           COALESCE(AVG({totalExpression}), 0) AS TicketMedio
                    FROM Vendas v
                    WHERE {string.Join(" AND ", filtros)}
                    GROUP BY {HourExpression(connection, "v.Data")}
                    ORDER BY HoraVenda";
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var hora = LerInteiro(reader, 0);
                    dados.Add(new DadoVendaPeriodo
                    {
                        Periodo = $"{hora:00}:00",
                        Hora = hora,
                        QuantidadeVendas = LerInteiro(reader, 1),
                        ItensVendidos = LerInteiro(reader, 2),
                        Faturamento = LerDecimal(reader, 3),
                        TicketMedio = LerDecimal(reader, 4)
                    });
                }

                return dados;
            }, warningThresholdMs: 500);
        }

        public List<DadoVendaPeriodo> ObterVendasPorDia(DateTime dataInicio, DateTime dataFim, int limit = 31)
        {
            return _logger.Measure("Relatorios", $"ObterVendasPorDia(limit={limit})", () =>
            {
                var dados = new List<DadoVendaPeriodo>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Vendas"))
                {
                    return dados;
                }

                var totalExpression = ColunaExiste(connection, "Vendas", "ValorTotal")
                    ? "COALESCE(v.ValorTotal, 0)"
                    : "COALESCE(v.Total, 0)";
                var itensExpression = ColunaExiste(connection, "Vendas", "ItensQuantidade")
                    ? "COALESCE(v.ItensQuantidade, 0)"
                    : "COALESCE(v.QuantidadeItens, 0)";
                var filtros = CriarFiltrosPeriodoVendas(connection);

                var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT {DateOnlyExpression(connection, "v.Data")} AS DiaVenda,
                           COUNT(*) AS QuantidadeVendas,
                           COALESCE(SUM({itensExpression}), 0) AS ItensVendidos,
                           COALESCE(SUM({totalExpression}), 0) AS Faturamento,
                           COALESCE(AVG({totalExpression}), 0) AS TicketMedio
                    FROM Vendas v
                    WHERE {string.Join(" AND ", filtros)}
                    GROUP BY {DateOnlyExpression(connection, "v.Data")}
                    ORDER BY DiaVenda DESC
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 31));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var data = LerData(reader, 0);
                    dados.Add(new DadoVendaPeriodo
                    {
                        Periodo = data == DateTime.MinValue ? LerTexto(reader, 0) : data.ToString("dd/MM/yyyy"),
                        Data = data == DateTime.MinValue ? null : data,
                        QuantidadeVendas = LerInteiro(reader, 1),
                        ItensVendidos = LerInteiro(reader, 2),
                        Faturamento = LerDecimal(reader, 3),
                        TicketMedio = LerDecimal(reader, 4)
                    });
                }

                return dados;
            }, warningThresholdMs: 500);
        }

        public List<DadoInadimplencia> ObterInadimplenciaDetalhada(
            DateTime dataReferencia,
            string cliente = "",
            int limit = 100)
        {
            return _logger.Measure("Relatorios", $"ObterInadimplenciaDetalhada(limit={limit})", () =>
            {
                var dados = new List<DadoInadimplencia>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "ContasReceber"))
                {
                    return dados;
                }

                var filtros = new List<string>
                {
                    $"{DateOnlyExpression(connection, "DataVencimento")} < @dataReferencia",
                    "COALESCE(Status, '') NOT IN ('Pago', 'Recebida', 'Cancelado', 'Cancelada')"
                };

                var command = connection.CreateCommand();
                command.Parameters.AddWithValue("@dataReferencia", dataReferencia.Date.ToString("yyyy-MM-dd"));

                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    filtros.Add("COALESCE(Cliente, '') LIKE @cliente");
                    command.Parameters.AddWithValue("@cliente", CriarFiltroLike(cliente));
                }

                command.CommandText = $@"
                    SELECT Id, Cliente, Descricao, Valor, DataVencimento,
                           COALESCE(Status, ''), COALESCE(FormaPagamento, ''),
                           COALESCE(Origem, ''), COALESCE(ReferenciaExterna, '')
                    FROM ContasReceber
                    WHERE {string.Join(" AND ", filtros)}
                    ORDER BY {DateOnlyExpression(connection, "DataVencimento")} ASC, Valor DESC
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 100));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var vencimento = LerData(reader, 4);
                    dados.Add(new DadoInadimplencia
                    {
                        Id = LerInteiro(reader, 0),
                        Cliente = LerTexto(reader, 1),
                        Descricao = LerTexto(reader, 2),
                        Valor = LerDecimal(reader, 3),
                        DataVencimento = vencimento,
                        DiasAtraso = vencimento == DateTime.MinValue
                            ? 0
                            : Math.Max(0, (dataReferencia.Date - vencimento.Date).Days),
                        Status = LerTexto(reader, 5),
                        FormaPagamento = LerTexto(reader, 6),
                        Origem = LerTexto(reader, 7),
                        ReferenciaExterna = LerTexto(reader, 8)
                    });
                }

                return dados;
            }, warningThresholdMs: 500);
        }

        public List<DadoEstoque> ObterDadosEstoque(string categoria = "", string marca = "", int limit = 250)
        {
            return _logger.Measure("Relatorios", $"ObterDadosEstoque(limit={limit})", () =>
            {
                var dados = new List<DadoEstoque>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Produtos"))
                {
                    return dados;
                }

                var filtros = new List<string> { "1 = 1" };
                var command = connection.CreateCommand();

                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    filtros.Add("COALESCE(Categoria, '') = @categoria");
                    command.Parameters.AddWithValue("@categoria", categoria.Trim());
                }

                if (!string.IsNullOrWhiteSpace(marca))
                {
                    filtros.Add("COALESCE(Marca, '') = @marca");
                    command.Parameters.AddWithValue("@marca", marca.Trim());
                }

                command.CommandText = $@"
                    SELECT Id, Nome, Codigo, Categoria, Marca, QuantidadeEstoque,
                           QuantidadeMinima, PrecoVenda, DataUltimaVenda, DataUltimaAtualizacao, VendasUltimoMes
                    FROM Produtos
                    WHERE {string.Join(" AND ", filtros)}
                    ORDER BY (COALESCE(QuantidadeEstoque, 0) * COALESCE(PrecoVenda, 0)) DESC, Nome
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 250));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var quantidade = LerInteiro(reader, 5);
                    var preco = LerDecimal(reader, 7);
                    var ultimaMovimentacao = LerDataOpcional(reader, 8)
                        ?? LerDataOpcional(reader, 9)
                        ?? DateTime.MinValue;

                    dados.Add(new DadoEstoque
                    {
                        Id = LerGuid(reader, 0, "produto"),
                        ProdutoId = LerGuid(reader, 0, "produto"),
                        ProdutoNome = LerTexto(reader, 1),
                        ProdutoCodigo = LerTexto(reader, 2),
                        Categoria = LerTexto(reader, 3),
                        Marca = LerTexto(reader, 4),
                        QuantidadeAtual = quantidade,
                        QuantidadeMinima = LerInteiro(reader, 6),
                        ValorUnitario = preco,
                        ValorTotal = quantidade * preco,
                        GiroMensal = LerInteiro(reader, 10),
                        DiasSemMovimentacao = ultimaMovimentacao == DateTime.MinValue
                            ? 0
                            : (int)Math.Max(0, (DateTime.Today - ultimaMovimentacao.Date).TotalDays),
                        UltimaMovimentacao = ultimaMovimentacao
                    });
                }

                AplicarCurvaAbc(dados);
                return dados;
            }, warningThresholdMs: 450);
        }

        private static void AplicarCurvaAbc(IList<DadoEstoque> dados)
        {
            var valorTotalEstoque = dados.Sum(item => Math.Max(0m, item.ValorTotal));
            if (valorTotalEstoque <= 0)
            {
                foreach (var item in dados)
                {
                    item.CurvaAbc = "Sem valor";
                    item.ParticipacaoEstoquePercentual = 0m;
                    item.ParticipacaoAcumuladaPercentual = 0m;
                }

                return;
            }

            decimal acumulado = 0m;
            foreach (var item in dados
                         .OrderByDescending(item => item.ValorTotal)
                         .ThenBy(item => item.ProdutoNome, StringComparer.OrdinalIgnoreCase))
            {
                var valorItem = Math.Max(0m, item.ValorTotal);
                var acumuladoAntes = acumulado / valorTotalEstoque * 100m;
                acumulado += valorItem;

                item.ParticipacaoEstoquePercentual = valorItem / valorTotalEstoque * 100m;
                item.ParticipacaoAcumuladaPercentual = acumulado / valorTotalEstoque * 100m;
                item.CurvaAbc = acumuladoAntes < 80m
                    ? "A"
                    : acumuladoAntes < 95m
                        ? "B"
                        : "C";
            }
        }

        public List<DadoEstoque> ObterProdutosSemEstoque()
        {
            return ObterDadosEstoque().Where(p => p.QuantidadeAtual <= 0).ToList();
        }

        public List<DadoEstoque> ObterProdutosEstoqueMinimo()
        {
            return ObterDadosEstoque().Where(p => p.QuantidadeAtual <= p.QuantidadeMinima).ToList();
        }

        public List<DadoCliente> ObterDadosClientes(string cliente = "", int limit = 250)
        {
            return _logger.Measure("Relatorios", $"ObterDadosClientes(limit={limit})", () =>
            {
                var dados = new List<DadoCliente>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Clientes"))
                {
                    return dados;
                }

                var filtros = new List<string> { "1 = 1" };
                var command = connection.CreateCommand();

                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    filtros.Add("(Nome LIKE @cliente OR COALESCE(Telefone, WhatsApp, '') LIKE @cliente OR COALESCE(Email, '') LIKE @cliente)");
                    command.Parameters.AddWithValue("@cliente", CriarFiltroLike(cliente));
                }

                command.CommandText = $@"
                    SELECT Id, Nome, COALESCE(Telefone, WhatsApp, ''), COALESCE(Email, ''),
                           Ativo, ClienteVip, TotalGasto, TotalServicos, UltimaVisita
                    FROM Clientes
                    WHERE {string.Join(" AND ", filtros)}
                    ORDER BY Nome
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 250));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var ativo = LerInteiro(reader, 4) == 1;
                    var vip = LerInteiro(reader, 5) == 1;
                    var totalCompras = LerDecimal(reader, 6);
                    var numeroCompras = LerInteiro(reader, 7);

                    dados.Add(new DadoCliente
                    {
                        Id = LerGuid(reader, 0, "cliente"),
                        Nome = LerTexto(reader, 1),
                        Telefone = LerTexto(reader, 2),
                        Email = LerTexto(reader, 3),
                        Status = !ativo ? "Inativo" : vip ? "VIP" : "Ativo",
                        TotalCompras = totalCompras,
                        NumeroCompras = numeroCompras,
                        TicketMedio = numeroCompras > 0 ? totalCompras / numeroCompras : 0,
                        UltimaCompra = LerDataOpcional(reader, 8) ?? DateTime.MinValue,
                        LimiteCredito = 0,
                        Inadimplencia = 0
                    });
                }

                return dados;
            }, warningThresholdMs: 450);
        }

        public List<DadoCliente> ObterClientesVIP()
        {
            return ObterDadosClientes().Where(c => c.Status == "VIP").ToList();
        }

        public List<DadoCliente> ObterClientesInadimplentes()
        {
            return ObterDadosClientes().Where(c => c.Status == "Inadimplente").ToList();
        }

        public int ObterTotalClientes(string cliente = "")
        {
            return _logger.Measure("Relatorios", "ObterTotalClientes", () =>
            {
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Clientes"))
                {
                    return 0;
                }

                var filtros = new List<string> { "1 = 1" };
                var command = connection.CreateCommand();

                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    filtros.Add("(Nome LIKE @cliente OR COALESCE(Telefone, WhatsApp, '') LIKE @cliente OR COALESCE(Email, '') LIKE @cliente)");
                    command.Parameters.AddWithValue("@cliente", CriarFiltroLike(cliente));
                }

                command.CommandText = $@"
                    SELECT COUNT(*)
                    FROM Clientes
                    WHERE {string.Join(" AND ", filtros)}";

                return Convert.ToInt32(command.ExecuteScalar());
            }, warningThresholdMs: 250);
        }

        public int ObterProdutosSemGiro(string categoria = "", string marca = "")
        {
            return _logger.Measure("Relatorios", "ObterProdutosSemGiro", () =>
            {
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Produtos"))
                {
                    return 0;
                }

                var filtros = new List<string> { "COALESCE(VendasUltimoMes, 0) <= 0" };
                var command = connection.CreateCommand();

                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    filtros.Add("COALESCE(Categoria, '') = @categoria");
                    command.Parameters.AddWithValue("@categoria", categoria.Trim());
                }

                if (!string.IsNullOrWhiteSpace(marca))
                {
                    filtros.Add("COALESCE(Marca, '') = @marca");
                    command.Parameters.AddWithValue("@marca", marca.Trim());
                }

                command.CommandText = $@"
                    SELECT COUNT(*)
                    FROM Produtos
                    WHERE {string.Join(" AND ", filtros)}";

                return Convert.ToInt32(command.ExecuteScalar());
            }, warningThresholdMs: 250);
        }

        public List<DadoOrcamento> ObterDadosOrcamentos(
            DateTime dataInicio,
            DateTime dataFim,
            string cliente = "",
            string status = "",
            int limit = 250)
        {
            return _logger.Measure("Relatorios", $"ObterDadosOrcamentos(limit={limit})", () =>
            {
                var dados = new List<DadoOrcamento>();
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Orcamentos"))
                {
                    return dados;
                }

                var filtros = new List<string>
                {
                    $"{DateOnlyExpression(connection, "o.DataCriacao")} BETWEEN @dataInicio AND @dataFim"
                };
                var command = connection.CreateCommand();
                command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    filtros.Add("COALESCE(c.Nome, '') LIKE @cliente");
                    command.Parameters.AddWithValue("@cliente", CriarFiltroLike(cliente));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    filtros.Add("COALESCE(o.Status, '') = @status");
                    command.Parameters.AddWithValue("@status", status.Trim());
                }

                command.CommandText = $@"
                    SELECT o.Id, o.Numero, o.DataCriacao, o.DataValidade, o.ClienteId,
                           COALESCE(c.Nome, ''), o.VendedorId, o.Total, o.Status,
                           o.DataAprovacao, o.DataConversaoVenda,
                           COALESCE((
                               SELECT SUM(COALESCE(oi.Quantidade, 0))
                               FROM OrcamentoItens oi
                               WHERE oi.OrcamentoId = o.Id
                           ), 0)
                    FROM Orcamentos o
                    LEFT JOIN Clientes c ON o.ClienteId = c.Id
                    WHERE {string.Join(" AND ", filtros)}
                    ORDER BY o.DataCriacao DESC, o.Id DESC
                    {LimitClause(connection)}";
                command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 250));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dados.Add(new DadoOrcamento
                    {
                        Id = LerGuid(reader, 0, "orcamento"),
                        Numero = LerTexto(reader, 1),
                        DataCriacao = LerData(reader, 2),
                        DataValidade = LerDataOpcional(reader, 3) ?? DateTime.MinValue,
                        ClienteId = LerGuid(reader, 4, "cliente"),
                        ClienteNome = LerTexto(reader, 5),
                        VendedorId = LerGuid(reader, 6, "vendedor"),
                        VendedorNome = string.Empty,
                        ValorTotal = LerDecimal(reader, 7),
                        Status = LerTexto(reader, 8),
                        DataAprovacao = LerDataOpcional(reader, 9),
                        DataConversaoVenda = LerDataOpcional(reader, 10),
                        ItensQuantidade = LerInteiro(reader, 11)
                    });
                }

                return dados;
            }, warningThresholdMs: 500);
        }

        public decimal ObterConversaoOrcamentos(DateTime dataInicio, DateTime dataFim)
        {
            return _logger.Measure("Relatorios", "ObterConversaoOrcamentos", () =>
            {
                using var connection = GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "main", "Orcamentos"))
                {
                    return 0m;
                }

                using var totalCommand = connection.CreateCommand();
                totalCommand.CommandText = $@"
                    SELECT COUNT(*)
                    FROM Orcamentos
                    WHERE {DateOnlyExpression(connection, "DataCriacao")} BETWEEN @dataInicio AND @dataFim";
                totalCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                totalCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                var total = Convert.ToInt32(totalCommand.ExecuteScalar());
                if (total == 0)
                {
                    return 0m;
                }

                using var convertidosCommand = connection.CreateCommand();
                convertidosCommand.CommandText = $@"
                    SELECT COUNT(*)
                    FROM Orcamentos
                    WHERE {DateOnlyExpression(connection, "DataCriacao")} BETWEEN @dataInicio AND @dataFim
                      AND COALESCE(Status, '') IN ('Convertido em Venda', 'Aprovado')";
                convertidosCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                convertidosCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

                var convertidos = Convert.ToInt32(convertidosCommand.ExecuteScalar());
                return decimal.Round((decimal)convertidos * 100m / total, 2);
            }, warningThresholdMs: 350);
        }

        public List<DadoCaixa> ObterDadosCaixa(DateTime dataInicio, DateTime dataFim, int limit = 250)
        {
            return _logger.Measure("Relatorios", $"ObterDadosCaixa(limit={limit})", () =>
            {
                using var connection = GetConnection();
                connection.Open();

                if (TabelaExiste(connection, "main", "MovimentacoesCaixa"))
                {
                    return ObterDadosCaixaDaTabela(connection, dataInicio, dataFim, limit);
                }

                if (TabelaExiste(connection, "main", "MovimentacoesFinanceiras"))
                {
                    return ObterDadosCaixaDoFinanceiro(connection, dataInicio, dataFim, limit);
                }

                return new List<DadoCaixa>();
            }, warningThresholdMs: 450);
        }

        public string ObterResumoConsistenciaOperacional(DateTime dataInicio, DateTime dataFim)
        {
            return _logger.Measure("Relatorios", "ObterResumoConsistenciaOperacional", () =>
            {
                using var connection = GetConnection();
                connection.Open();

                var apontamentos = new List<string>();

                if (TabelaExiste(connection, "main", "Orcamentos"))
                {
                    using var comandoVenda = connection.CreateCommand();
                    comandoVenda.CommandText = $@"
                        SELECT COUNT(*)
                        FROM Orcamentos
                        WHERE {DateOnlyExpression(connection, "DataCriacao")} BETWEEN @dataInicio AND @dataFim
                          AND COALESCE(Status, '') = 'Convertido em Venda'
                          AND DataConversaoVenda IS NULL;";
                    comandoVenda.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                    comandoVenda.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
                    var convertidosSemData = Convert.ToInt32(comandoVenda.ExecuteScalar());
                    if (convertidosSemData > 0)
                    {
                        apontamentos.Add($"{convertidosSemData} orcamento(s) convertidos em venda sem data de conversao.");
                    }

                    using var comandoOs = connection.CreateCommand();
                    comandoOs.CommandText = $@"
                        SELECT COUNT(*)
                        FROM Orcamentos
                        WHERE {DateOnlyExpression(connection, "DataCriacao")} BETWEEN @dataInicio AND @dataFim
                          AND COALESCE(Status, '') = 'Convertido em OS'
                          AND OrdemServicoId IS NULL;";
                    comandoOs.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                    comandoOs.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
                    var convertidosSemOs = Convert.ToInt32(comandoOs.ExecuteScalar());
                    if (convertidosSemOs > 0)
                    {
                        apontamentos.Add($"{convertidosSemOs} orcamento(s) convertidos em OS sem vinculo persistido.");
                    }
                }

                if (TabelaExiste(connection, "main", "OrdensServico") && TabelaExiste(connection, "main", "ContasReceber"))
                {
                    using var comandoOs = connection.CreateCommand();
                    comandoOs.CommandText = $@"
                        SELECT COUNT(*)
                        FROM OrdensServico os
                        WHERE {DateOnlyExpression(connection, "COALESCE(os.DataEntrega, os.DataConclusao, os.DataAbertura)")} BETWEEN @dataInicio AND @dataFim
                          AND COALESCE(os.Status, '') = 'Entregue'
                          AND NOT EXISTS (
                              SELECT 1
                              FROM ContasReceber cr
                              WHERE cr.Origem = 'OrdemServicoContaReceber'
                                AND cr.ReferenciaExterna = os.Id
                          );";
                    comandoOs.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                    comandoOs.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
                    var osSemFinanceiro = Convert.ToInt32(comandoOs.ExecuteScalar());
                    if (osSemFinanceiro > 0)
                    {
                        apontamentos.Add($"{osSemFinanceiro} OS entregue(s) sem conta a receber integrada.");
                    }
                }

                if (TabelaExiste(connection, "main", "Agendamentos"))
                {
                    using var comandoAgenda = connection.CreateCommand();
                    comandoAgenda.CommandText = $@"
                        SELECT COUNT(*)
                        FROM Agendamentos
                        WHERE {DateOnlyExpression(connection, "DataAgendamento")} BETWEEN @dataInicio AND @dataFim
                          AND COALESCE(Status, '') = 'Finalizado'
                          AND CheckOut IS NULL;";
                    comandoAgenda.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
                    comandoAgenda.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
                    var finalizadosSemCheckout = Convert.ToInt32(comandoAgenda.ExecuteScalar());
                    if (finalizadosSemCheckout > 0)
                    {
                        apontamentos.Add($"{finalizadosSemCheckout} agendamento(s) finalizados sem registro de check-out.");
                    }
                }

                if (TabelaExiste(connection, "main", "Produtos") &&
                    ColunaExiste(connection, "Produtos", "QuantidadeReservada") &&
                    ColunaExiste(connection, "Produtos", "QuantidadeEstoque"))
                {
                    using var comandoEstoque = connection.CreateCommand();
                    comandoEstoque.CommandText = $@"
                        SELECT COUNT(*)
                        FROM Produtos
                        WHERE COALESCE(QuantidadeReservada, 0) > COALESCE(QuantidadeEstoque, 0);";
                    var reservasInvalidas = Convert.ToInt32(comandoEstoque.ExecuteScalar());
                    if (reservasInvalidas > 0)
                    {
                        apontamentos.Add($"{reservasInvalidas} produto(s) com reserva acima do saldo fisico.");
                    }
                }

                return apontamentos.Count == 0
                    ? "Nenhuma inconsistencia operacional critica encontrada entre os modulos consultados."
                    : string.Join(" ", apontamentos);
            }, warningThresholdMs: 400);
        }

        private static List<DadoCaixa> ObterDadosCaixaDaTabela(DbConnection connection, DateTime dataInicio, DateTime dataFim, int limit)
        {
            var dados = new List<DadoCaixa>();
            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Id, Data, Tipo, ValorInicial, ValorFinal, Sangrias, Suprimentos, Diferenca,
                       COALESCE(Operador, ''), COALESCE(Observacoes, '')
                FROM MovimentacoesCaixa
                WHERE {DateOnlyExpression(connection, "Data")} BETWEEN @dataInicio AND @dataFim
                ORDER BY Data DESC
                {LimitClause(connection)}";
            command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 250));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                dados.Add(new DadoCaixa
                {
                    Id = LerGuid(reader, 0, "caixa"),
                    Data = LerData(reader, 1),
                    Tipo = LerTexto(reader, 2),
                    ValorInicial = LerDecimal(reader, 3),
                    ValorFinal = LerDecimal(reader, 4),
                    Sangrias = LerDecimal(reader, 5),
                    Suprimentos = LerDecimal(reader, 6),
                    Diferenca = LerDecimal(reader, 7),
                    Operador = LerTexto(reader, 8),
                    Observacoes = LerTexto(reader, 9)
                });
            }

            return dados;
        }

        private static List<DadoCaixa> ObterDadosCaixaDoFinanceiro(DbConnection connection, DateTime dataInicio, DateTime dataFim, int limit)
        {
            var dados = new List<DadoCaixa>();
            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Data,
                       COALESCE(SUM(CASE WHEN Tipo IN ('Entrada', 'Receita') THEN Valor ELSE 0 END), 0) AS Entradas,
                       COALESCE(SUM(CASE WHEN Tipo IN ('Saída', 'Saída', 'Despesa') THEN Valor ELSE 0 END), 0) AS Saidas
                FROM MovimentacoesFinanceiras
                WHERE Data BETWEEN @dataInicio AND @dataFim
                GROUP BY Data
                ORDER BY Data DESC
                {LimitClause(connection)}";
            command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@limit", LimitarQuantidade(limit, 250));

            decimal saldoAnterior = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var data = LerData(reader, 0);
                var entradas = LerDecimal(reader, 1);
                var saidas = LerDecimal(reader, 2);
                var saldoAtual = saldoAnterior + entradas - saidas;

                dados.Add(new DadoCaixa
                {
                    Id = CriarGuidEstavel($"caixa:{data:yyyy-MM-dd}"),
                    Data = data,
                    Tipo = "Resumo Diário",
                    ValorInicial = saldoAnterior,
                    ValorFinal = saldoAtual,
                    Sangrias = saidas,
                    Suprimentos = entradas,
                    Diferenca = entradas - saidas,
                    Operador = string.Empty,
                    Observacoes = "Gerado a partir das movimentações financeiras."
                });

                saldoAnterior = saldoAtual;
            }

            dados.Reverse();
            return dados;
        }

        public void AdicionarMeta(DadoMeta meta)
        {
            using var connection = GetConnection();
            connection.Open();

            if (IsSqlServerConnection(connection))
            {
                // O SQL Server e provisionado pelo schema mestre.
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Metas (Id, Tipo, Periodo, MetaValor, ValorAtual, PercentualAtingido,
                                   Responsavel, DataInicio, DataFim, Status)
                VALUES (@Id, @Tipo, @Periodo, @MetaValor, @ValorAtual, @PercentualAtingido,
                        @Responsavel, @DataInicio, @DataFim, @Status)";
            command.Parameters.AddWithValue("@Id", meta.Id.ToString());
            command.Parameters.AddWithValue("@Tipo", meta.Tipo);
            command.Parameters.AddWithValue("@Periodo", meta.Periodo);
            command.Parameters.AddWithValue("@MetaValor", meta.MetaValor);
            command.Parameters.AddWithValue("@ValorAtual", meta.ValorAtual);
            command.Parameters.AddWithValue("@PercentualAtingido", meta.PercentualAtingido);
            command.Parameters.AddWithValue("@Responsavel", meta.Responsavel);
            command.Parameters.AddWithValue("@DataInicio", meta.DataInicio.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@DataFim", meta.DataFim.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Status", meta.Status);
            command.ExecuteNonQuery();
        }

        public List<DadoMeta> ObterMetas()
        {
            var dados = new List<DadoMeta>();
            using var connection = GetConnection();
            connection.Open();

            if (TabelaExiste(connection, "main", "Metas") && TabelaTemDados(connection, "main", "Metas"))
            {
                var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT Id, Tipo, Periodo, MetaValor, ValorAtual, PercentualAtingido,
                           COALESCE(Responsavel, ''), DataInicio, DataFim, Status
                    FROM Metas
                    ORDER BY DataFim DESC";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    dados.Add(new DadoMeta
                    {
                        Id = LerGuid(reader, 0, "meta"),
                        Tipo = LerTexto(reader, 1),
                        Periodo = LerTexto(reader, 2),
                        MetaValor = LerDecimal(reader, 3),
                        ValorAtual = LerDecimal(reader, 4),
                        PercentualAtingido = LerDecimal(reader, 5),
                        Responsavel = LerTexto(reader, 6),
                        DataInicio = LerData(reader, 7),
                        DataFim = LerData(reader, 8),
                        Status = LerTexto(reader, 9)
                    });
                }

                return dados;
            }

            if (!TabelaExiste(connection, "main", "MetasFinanceiras"))
            {
                return dados;
            }

            var fallback = connection.CreateCommand();
            fallback.CommandText = $@"
                SELECT Id, Nome, ValorMeta, ValorAtual, DataInicio, DataFim, Status
                FROM MetasFinanceiras
                ORDER BY DataFim DESC";

            using var fallbackReader = fallback.ExecuteReader();
            while (fallbackReader.Read())
            {
                var valorMeta = LerDecimal(fallbackReader, 2);
                var valorAtual = LerDecimal(fallbackReader, 3);

                dados.Add(new DadoMeta
                {
                    Id = LerGuid(fallbackReader, 0, "meta-financeira"),
                    Tipo = LerTexto(fallbackReader, 1),
                    Periodo = "Personalizado",
                    MetaValor = valorMeta,
                    ValorAtual = valorAtual,
                    PercentualAtingido = valorMeta > 0 ? (valorAtual / valorMeta) * 100 : 0,
                    Responsavel = string.Empty,
                    DataInicio = LerData(fallbackReader, 4),
                    DataFim = LerData(fallbackReader, 5),
                    Status = LerTexto(fallbackReader, 6)
                });
            }

            return dados;
        }

        public void AdicionarAlerta(DadoAlerta alerta)
        {
            using var connection = GetConnection();
            connection.Open();

            if (IsSqlServerConnection(connection))
            {
                // O SQL Server e provisionado pelo schema mestre.
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Alertas (Id, Tipo, Mensagem, Severidade, DataGeracao, Lido, Origem)
                VALUES (@Id, @Tipo, @Mensagem, @Severidade, @DataGeracao, @Lido, @Origem)";
            command.Parameters.AddWithValue("@Id", alerta.Id.ToString());
            command.Parameters.AddWithValue("@Tipo", alerta.Tipo);
            command.Parameters.AddWithValue("@Mensagem", alerta.Mensagem);
            command.Parameters.AddWithValue("@Severidade", alerta.Severidade);
            command.Parameters.AddWithValue("@DataGeracao", alerta.DataGeracao.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Lido", alerta.Lido ? 1 : 0);
            command.Parameters.AddWithValue("@Origem", alerta.Origem);
            command.ExecuteNonQuery();
        }

        public List<DadoAlerta> ObterAlertas()
        {
            var dados = new List<DadoAlerta>();
            using var connection = GetConnection();
            connection.Open();

            if (!TabelaExiste(connection, "main", "Alertas"))
            {
                return dados;
            }

            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Id, Tipo, Mensagem, Severidade, DataGeracao, Lido, COALESCE(Origem, '')
                FROM Alertas
                ORDER BY DataGeracao DESC
                {LimitLiteralClause(connection, 50)}";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                dados.Add(new DadoAlerta
                {
                    Id = LerGuid(reader, 0, "alerta"),
                    Tipo = LerTexto(reader, 1),
                    Mensagem = LerTexto(reader, 2),
                    Severidade = LerTexto(reader, 3),
                    DataGeracao = LerData(reader, 4),
                    Lido = LerInteiro(reader, 5) == 1,
                    Origem = LerTexto(reader, 6)
                });
            }

            return dados;
        }

        public void RegistrarAuditoria(DadoAuditoria auditoria)
        {
            using var connection = GetConnection();
            connection.Open();

            if (IsSqlServerConnection(connection))
            {
                // O SQL Server e provisionado pelo schema mestre.
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Auditoria (Id, DataHora, Usuario, Acao, Tabela, RegistroId,
                                      ValorAnterior, ValorNovo, IP)
                VALUES (@Id, @DataHora, @Usuario, @Acao, @Tabela, @RegistroId,
                        @ValorAnterior, @ValorNovo, @IP)";
            command.Parameters.AddWithValue("@Id", auditoria.Id.ToString());
            command.Parameters.AddWithValue("@DataHora", auditoria.DataHora.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Usuario", auditoria.Usuario);
            command.Parameters.AddWithValue("@Acao", auditoria.Acao);
            command.Parameters.AddWithValue("@Tabela", auditoria.Tabela);
            command.Parameters.AddWithValue("@RegistroId", auditoria.RegistroId.ToString());
            command.Parameters.AddWithValue("@ValorAnterior", auditoria.ValorAnterior);
            command.Parameters.AddWithValue("@ValorNovo", auditoria.ValorNovo);
            command.Parameters.AddWithValue("@IP", auditoria.IP);
            command.ExecuteNonQuery();
        }

        public List<DadoAuditoria> ObterAuditoria(DateTime dataInicio, DateTime dataFim)
        {
            return ObterAuditoriaPaginada(new AuditoriaOperacionalFiltro
            {
                DataInicio = dataInicio,
                DataFim = dataFim,
                PaginaAtual = 1,
                ItensPorPagina = 100
            }).Itens;
        }

        public QueryPageResult<DadoAuditoria> ObterAuditoriaPaginada(AuditoriaOperacionalFiltro filtro)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            return _logger.Measure(
                "Auditoria",
                $"ObterAuditoriaPaginada(pagina={filtro.PaginaAtual}, tamanho={filtro.ItensPorPagina})",
                () =>
                {
                    using var connection = GetConnection();
                    connection.Open();

                    if (TabelaExiste(connection, "main", "AuditLogs"))
                    {
                        var auditLogs = ObterAuditoriaAuditLogsPaginada(connection, filtro);
                        if (auditLogs.TotalItens > 0 || !TabelaExiste(connection, "main", "Auditoria"))
                        {
                            return auditLogs;
                        }
                    }

                    if (!TabelaExiste(connection, "main", "Auditoria"))
                    {
                        return CriarPaginacaoVazia<DadoAuditoria>(filtro);
                    }

                    return ObterAuditoriaLegadaPaginada(connection, filtro);
                },
                warningThresholdMs: 450);
        }

        public List<string> ObterCategoriasAuditoriaOperacional(DateTime dataInicio, DateTime dataFim)
        {
            return _logger.Measure("Auditoria", "ObterCategoriasAuditoriaOperacional", () =>
            {
                using var connection = GetConnection();
                connection.Open();

                if (TabelaExiste(connection, "main", "AuditLogs"))
                {
                    var categorias = new List<string>();
                    using var command = connection.CreateCommand();
                    command.CommandText = $@"
                        SELECT DISTINCT COALESCE(Categoria, '') AS Categoria
                        FROM AuditLogs
                        WHERE {DateTimeExpression(connection, "DataHora")} BETWEEN @dataInicio AND @dataFim
                          AND trim(COALESCE(Categoria, '')) <> ''
                        ORDER BY Categoria";
                    command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd 00:00:00"));
                    command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd 23:59:59"));

                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var categoria = LerTexto(reader, 0);
                        if (!string.IsNullOrWhiteSpace(categoria))
                        {
                            categorias.Add(categoria);
                        }
                    }

                    return categorias;
                }

                if (!TabelaExiste(connection, "main", "Auditoria"))
                {
                    return new List<string>();
                }

                var categoriasLegadas = new List<string>();
                using var legacyCommand = connection.CreateCommand();
                legacyCommand.CommandText = $@"
                    SELECT DISTINCT COALESCE(Tabela, '') AS Tabela
                    FROM Auditoria
                    WHERE {DateTimeExpression(connection, "DataHora")} BETWEEN @dataInicio AND @dataFim
                      AND trim(COALESCE(Tabela, '')) <> ''
                    ORDER BY Tabela";
                legacyCommand.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd 00:00:00"));
                legacyCommand.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd 23:59:59"));

                using var legacyReader = legacyCommand.ExecuteReader();
                while (legacyReader.Read())
                {
                    var categoria = LerTexto(legacyReader, 0);
                    if (!string.IsNullOrWhiteSpace(categoria))
                    {
                        categoriasLegadas.Add(categoria);
                    }
                }

                return categoriasLegadas;
            }, warningThresholdMs: 250);
        }

        public void AdicionarTimeline(DadoTimeline timeline)
        {
            using var connection = GetConnection();
            connection.Open();

            if (IsSqlServerConnection(connection))
            {
                // O SQL Server e provisionado pelo schema mestre.
                return;
            }

            var command = connection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Timeline (Id, DataHora, TipoEvento, Descricao, Usuario, Valor, Categoria)
                VALUES (@Id, @DataHora, @TipoEvento, @Descricao, @Usuario, @Valor, @Categoria)";
            command.Parameters.AddWithValue("@Id", timeline.Id.ToString());
            command.Parameters.AddWithValue("@DataHora", timeline.DataHora.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@TipoEvento", timeline.TipoEvento);
            command.Parameters.AddWithValue("@Descricao", timeline.Descricao);
            command.Parameters.AddWithValue("@Usuario", timeline.Usuario);
            command.Parameters.AddWithValue("@Valor", timeline.Valor);
            command.Parameters.AddWithValue("@Categoria", timeline.Categoria);
            command.ExecuteNonQuery();
        }

        public List<DadoTimeline> ObterTimeline(DateTime dataInicio, DateTime dataFim)
        {
            var dados = new List<DadoTimeline>();
            using var connection = GetConnection();
            connection.Open();

            if (TabelaExiste(connection, "main", "AuditLogs"))
            {
                dados.AddRange(ObterTimelineAuditLogs(connection, dataInicio, dataFim));
            }

            if (!TabelaExiste(connection, "main", "Timeline"))
            {
                return dados
                    .OrderByDescending(d => d.DataHora)
                    .Take(80)
                    .ToList();
            }

            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Id, DataHora, TipoEvento, Descricao, Usuario, Valor, COALESCE(Categoria, '')
                FROM Timeline
                WHERE {DateTimeExpression(connection, "DataHora")} BETWEEN @dataInicio AND @dataFim
                ORDER BY DataHora DESC
                {LimitLiteralClause(connection, 50)}";
            command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd 00:00:00"));
            command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd 23:59:59"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                dados.Add(new DadoTimeline
                {
                    Id = LerGuid(reader, 0, "timeline"),
                    DataHora = LerData(reader, 1),
                    TipoEvento = LerTexto(reader, 2),
                    Descricao = LerTexto(reader, 3),
                    Usuario = LerTexto(reader, 4),
                    Valor = reader.IsDBNull(5) ? null : LerDecimal(reader, 5),
                    Categoria = LerTexto(reader, 6)
                });
            }

            return dados
                .OrderByDescending(d => d.DataHora)
                .Take(80)
                .ToList();
        }

        private static QueryPageResult<DadoAuditoria> ObterAuditoriaLegadaPaginada(DbConnection connection, AuditoriaOperacionalFiltro filtro)
        {
            var pagina = Math.Max(1, filtro.PaginaAtual);
            var itensPorPagina = LimitarQuantidade(filtro.ItensPorPagina, 100, 200);
            var offset = (pagina - 1) * itensPorPagina;
            var filtros = new List<string>
            {
                $"{DateTimeExpression(connection, "DataHora")} BETWEEN @dataInicio AND @dataFim"
            };

            using var countCommand = connection.CreateCommand();
            countCommand.Parameters.AddWithValue("@dataInicio", filtro.DataInicio.ToString("yyyy-MM-dd 00:00:00"));
            countCommand.Parameters.AddWithValue("@dataFim", filtro.DataFim.ToString("yyyy-MM-dd 23:59:59"));

            if (!string.IsNullOrWhiteSpace(filtro.Categoria))
            {
                filtros.Add("COALESCE(Tabela, '') = @categoria");
                countCommand.Parameters.AddWithValue("@categoria", filtro.Categoria.Trim());
            }

            if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            {
                filtros.Add("COALESCE(Usuario, '') LIKE @usuario");
                countCommand.Parameters.AddWithValue("@usuario", CriarFiltroLike(filtro.Usuario));
            }

            if (!string.IsNullOrWhiteSpace(filtro.TermoLivre))
            {
                filtros.Add("(COALESCE(Acao, '') LIKE @termo OR COALESCE(ValorAnterior, '') LIKE @termo OR COALESCE(ValorNovo, '') LIKE @termo OR COALESCE(RegistroId, '') LIKE @termo)");
                countCommand.Parameters.AddWithValue("@termo", CriarFiltroLike(filtro.TermoLivre));
            }

            countCommand.CommandText = $@"
                SELECT COUNT(*)
                FROM Auditoria
                WHERE {string.Join(" AND ", filtros)}";
            var totalItens = Convert.ToInt32(countCommand.ExecuteScalar());

            using var command = connection.CreateCommand();
            command.Parameters.AddWithValue("@dataInicio", filtro.DataInicio.ToString("yyyy-MM-dd 00:00:00"));
            command.Parameters.AddWithValue("@dataFim", filtro.DataFim.ToString("yyyy-MM-dd 23:59:59"));

            if (!string.IsNullOrWhiteSpace(filtro.Categoria))
            {
                command.Parameters.AddWithValue("@categoria", filtro.Categoria.Trim());
            }

            if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            {
                command.Parameters.AddWithValue("@usuario", CriarFiltroLike(filtro.Usuario));
            }

            if (!string.IsNullOrWhiteSpace(filtro.TermoLivre))
            {
                command.Parameters.AddWithValue("@termo", CriarFiltroLike(filtro.TermoLivre));
            }

            command.CommandText = $@"
                SELECT Id, DataHora, Usuario, Acao, Tabela, RegistroId,
                       COALESCE(ValorAnterior, ''), COALESCE(ValorNovo, ''), COALESCE(IP, '')
                FROM Auditoria
                WHERE {string.Join(" AND ", filtros)}
                ORDER BY DataHora DESC
                {PaginationClause(connection)}";
            command.Parameters.AddWithValue("@limit", itensPorPagina);
            command.Parameters.AddWithValue("@offset", offset);

            var itens = new List<DadoAuditoria>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(new DadoAuditoria
                {
                    Id = LerGuid(reader, 0, "auditoria"),
                    DataHora = LerData(reader, 1),
                    Usuario = LerTexto(reader, 2),
                    Acao = LerTexto(reader, 3),
                    Tabela = LerTexto(reader, 4),
                    Categoria = LerTexto(reader, 4),
                    RegistroId = LerGuid(reader, 5, "registro"),
                    ValorAnterior = ResumirDetalhes(LerTexto(reader, 6)),
                    ValorNovo = ResumirDetalhes(LerTexto(reader, 7)),
                    IP = LerTexto(reader, 8),
                    Maquina = LerTexto(reader, 8),
                    Sucesso = true,
                    Severidade = "Info"
                });
            }

            return CriarResultadoPaginado(itens, pagina, itensPorPagina, totalItens);
        }

        private static QueryPageResult<DadoAuditoria> ObterAuditoriaAuditLogsPaginada(DbConnection connection, AuditoriaOperacionalFiltro filtro)
        {
            var pagina = Math.Max(1, filtro.PaginaAtual);
            var itensPorPagina = LimitarQuantidade(filtro.ItensPorPagina, 100, 200);
            var offset = (pagina - 1) * itensPorPagina;
            var filtros = new List<string>
            {
                $"{DateTimeExpression(connection, "DataHora")} BETWEEN @dataInicio AND @dataFim"
            };

            using var countCommand = connection.CreateCommand();
            countCommand.Parameters.AddWithValue("@dataInicio", filtro.DataInicio.ToString("yyyy-MM-dd 00:00:00"));
            countCommand.Parameters.AddWithValue("@dataFim", filtro.DataFim.ToString("yyyy-MM-dd 23:59:59"));
            AplicarFiltrosAuditoriaAuditLogs(countCommand, filtros, filtro);

            countCommand.CommandText = $@"
                SELECT COUNT(*)
                FROM AuditLogs
                WHERE {string.Join(" AND ", filtros)}";
            var totalItens = Convert.ToInt32(countCommand.ExecuteScalar());

            using var command = connection.CreateCommand();
            command.Parameters.AddWithValue("@dataInicio", filtro.DataInicio.ToString("yyyy-MM-dd 00:00:00"));
            command.Parameters.AddWithValue("@dataFim", filtro.DataFim.ToString("yyyy-MM-dd 23:59:59"));
            AplicarFiltrosAuditoriaAuditLogs(command, null, filtro);
            command.CommandText = $@"
                SELECT Id,
                       DataHora,
                       COALESCE(UsuarioNome, 'Sistema'),
                       Acao,
                       COALESCE(NULLIF(Entidade, ''), Categoria),
                       COALESCE(NULLIF(EntidadeId, ''), {TextCastExpression(connection, "Id")}),
                       Categoria,
                       Severidade,
                       Sucesso,
                       COALESCE(Detalhes, ''),
                       COALESCE(ValorAnterior, ''),
                       COALESCE(ValorNovo, ''),
                       COALESCE(Perfil, ''),
                       COALESCE(Maquina, ''),
                       COALESCE(CorrelationId, '')
                FROM AuditLogs
                WHERE {string.Join(" AND ", filtros)}
                ORDER BY DataHora DESC
                {PaginationClause(connection)}";
            command.Parameters.AddWithValue("@limit", itensPorPagina);
            command.Parameters.AddWithValue("@offset", offset);

            var itens = new List<DadoAuditoria>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var categoria = LerTexto(reader, 6);
                var severidade = LerTexto(reader, 7);
                var sucesso = LerInteiro(reader, 8) == 1;
                var valorAnterior = LerTexto(reader, 10);
                var valorNovo = LerTexto(reader, 11);
                var perfil = LerTexto(reader, 12);
                var maquina = LerTexto(reader, 13);
                var correlationId = LerTexto(reader, 14);
                var contexto = string.Join(" | ", new[]
                    {
                        categoria,
                        severidade,
                        sucesso ? "Sucesso" : "Falha",
                        perfil,
                        maquina,
                        correlationId
                    }
                    .Where(valor => !string.IsNullOrWhiteSpace(valor)));

                itens.Add(new DadoAuditoria
                {
                    Id = LerGuid(reader, 0, "audit-log"),
                    DataHora = LerData(reader, 1),
                    Usuario = LerTexto(reader, 2),
                    Acao = LerTexto(reader, 3),
                    Tabela = LerTexto(reader, 4),
                    RegistroId = LerGuid(reader, 5, "audit-registro"),
                    Categoria = categoria,
                    Severidade = severidade,
                    Sucesso = sucesso,
                    Perfil = perfil,
                    CorrelationId = correlationId,
                    Maquina = maquina,
                    ValorAnterior = string.IsNullOrWhiteSpace(valorAnterior) ? contexto : ResumirDetalhes(valorAnterior),
                    ValorNovo = string.IsNullOrWhiteSpace(valorNovo) ? ResumirDetalhes(LerTexto(reader, 9)) : ResumirDetalhes(valorNovo),
                    IP = maquina
                });
            }

            return CriarResultadoPaginado(itens, pagina, itensPorPagina, totalItens);
        }

        private static void AplicarFiltrosAuditoriaAuditLogs(DbCommand command, List<string>? filtros, AuditoriaOperacionalFiltro filtro)
        {
            if (!string.IsNullOrWhiteSpace(filtro.Categoria))
            {
                filtros?.Add("COALESCE(Categoria, '') = @categoria");
                command.Parameters.AddWithValue("@categoria", filtro.Categoria.Trim());
            }

            if (!string.IsNullOrWhiteSpace(filtro.Severidade))
            {
                filtros?.Add("COALESCE(Severidade, '') = @severidade");
                command.Parameters.AddWithValue("@severidade", filtro.Severidade.Trim());
            }

            if (string.Equals(filtro.Status, "Sucesso", StringComparison.OrdinalIgnoreCase))
            {
                filtros?.Add("Sucesso = 1");
            }
            else if (string.Equals(filtro.Status, "Falha", StringComparison.OrdinalIgnoreCase))
            {
                filtros?.Add("Sucesso = 0");
            }

            if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            {
                filtros?.Add("COALESCE(UsuarioNome, '') LIKE @usuario");
                command.Parameters.AddWithValue("@usuario", CriarFiltroLike(filtro.Usuario));
            }

            if (!string.IsNullOrWhiteSpace(filtro.TermoLivre))
            {
                filtros?.Add(@"
                    (
                        COALESCE(Acao, '') LIKE @termo
                        OR COALESCE(Entidade, '') LIKE @termo
                        OR COALESCE(EntidadeId, '') LIKE @termo
                        OR COALESCE(Detalhes, '') LIKE @termo
                        OR COALESCE(ValorAnterior, '') LIKE @termo
                        OR COALESCE(ValorNovo, '') LIKE @termo
                        OR COALESCE(CorrelationId, '') LIKE @termo
                    )");
                command.Parameters.AddWithValue("@termo", CriarFiltroLike(filtro.TermoLivre));
            }
        }

        private static List<DadoTimeline> ObterTimelineAuditLogs(DbConnection connection, DateTime dataInicio, DateTime dataFim)
        {
            var dados = new List<DadoTimeline>();
            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Id,
                       DataHora,
                       Categoria,
                       Acao,
                       COALESCE(Detalhes, ''),
                       COALESCE(UsuarioNome, 'Sistema'),
                       COALESCE(NULLIF(Entidade, ''), ''),
                       COALESCE(NULLIF(EntidadeId, ''), '')
                FROM AuditLogs
                WHERE {DateTimeExpression(connection, "DataHora")} BETWEEN @dataInicio AND @dataFim
                ORDER BY DataHora DESC
                {LimitLiteralClause(connection, 80)}";
            command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd 00:00:00"));
            command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd 23:59:59"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var categoria = LerTexto(reader, 2);
                var acao = LerTexto(reader, 3);
                var detalhes = ResumirDetalhes(LerTexto(reader, 4));
                var entidade = LerTexto(reader, 6);
                var entidadeId = LerTexto(reader, 7);

                if (string.IsNullOrWhiteSpace(detalhes))
                {
                    detalhes = string.Join(" ", new[] { entidade, entidadeId }
                        .Where(valor => !string.IsNullOrWhiteSpace(valor)));
                }

                dados.Add(new DadoTimeline
                {
                    Id = LerGuid(reader, 0, "audit-timeline"),
                    DataHora = LerData(reader, 1),
                    TipoEvento = $"{categoria} / {acao}",
                    Descricao = detalhes,
                    Usuario = LerTexto(reader, 5),
                    Valor = null,
                    Categoria = categoria
                });
            }

            return dados;
        }

        private static string ResumirDetalhes(string detalhes)
        {
            if (string.IsNullOrWhiteSpace(detalhes))
            {
                return string.Empty;
            }

            detalhes = detalhes.Replace("\r", " ").Replace("\n", " ").Trim();
            return detalhes.Length <= 500
                ? detalhes
                : detalhes[..497] + "...";
        }

        private static string CriarFiltroLike(string valor)
        {
            return $"%{valor.Trim()}%";
        }

        private static List<string> CriarFiltrosPeriodoVendas(DbConnection connection)
        {
            var filtros = new List<string>
            {
                $"{DateOnlyExpression(connection, "v.Data")} BETWEEN @dataInicio AND @dataFim"
            };

            if (ColunaExiste(connection, "Vendas", "Status"))
            {
                filtros.Add("COALESCE(v.Status, 'Concluida') NOT IN ('Cancelada', 'Cancelado')");
            }

            return filtros;
        }

        private static DadoConciliacaoFinanceira ObterOuCriarConciliacao(
            Dictionary<string, DadoConciliacaoFinanceira> dados,
            string formaPagamento)
        {
            var formaNormalizada = string.IsNullOrWhiteSpace(formaPagamento)
                ? "Nao informado"
                : formaPagamento.Trim();

            if (!dados.TryGetValue(formaNormalizada, out var item))
            {
                item = new DadoConciliacaoFinanceira
                {
                    FormaPagamento = formaNormalizada
                };
                dados[formaNormalizada] = item;
            }

            return item;
        }

        private static void AcumularServico(
            Dictionary<string, DadoServicoRelatorio> dados,
            string servico,
            string origem,
            decimal quantidade,
            decimal receita,
            decimal custo,
            DateTime ultimaExecucao)
        {
            var nomeServico = string.IsNullOrWhiteSpace(servico)
                ? "Servico sem descricao"
                : servico.Trim();

            if (!dados.TryGetValue(nomeServico, out var item))
            {
                item = new DadoServicoRelatorio
                {
                    Servico = nomeServico,
                    Origem = origem,
                    UltimaExecucao = ultimaExecucao
                };
                dados[nomeServico] = item;
            }
            else if (!item.Origem.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Contains(origem, StringComparer.OrdinalIgnoreCase))
            {
                item.Origem = $"{item.Origem}/{origem}";
            }

            item.Quantidade += quantidade;
            item.ReceitaTotal += receita;
            item.CustoTotal += custo;

            if (ultimaExecucao > item.UltimaExecucao)
            {
                item.UltimaExecucao = ultimaExecucao;
            }
        }

        private static int LimitarQuantidade(int quantidadeSolicitada, int quantidadePadrao, int maximo = 500)
        {
            if (quantidadeSolicitada <= 0)
            {
                return quantidadePadrao;
            }

            return Math.Min(quantidadeSolicitada, maximo);
        }

        private static QueryPageResult<T> CriarPaginacaoVazia<T>(AuditoriaOperacionalFiltro filtro)
        {
            var itensPorPagina = LimitarQuantidade(filtro.ItensPorPagina, 100, 200);
            return new QueryPageResult<T>
            {
                Itens = new List<T>(),
                PaginaAtual = Math.Max(1, filtro.PaginaAtual),
                ItensPorPagina = itensPorPagina,
                TotalItens = 0,
                TotalPaginas = 0
            };
        }

        private static QueryPageResult<T> CriarResultadoPaginado<T>(List<T> itens, int paginaAtual, int itensPorPagina, int totalItens)
        {
            return new QueryPageResult<T>
            {
                Itens = itens,
                PaginaAtual = paginaAtual,
                ItensPorPagina = itensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = totalItens == 0 ? 0 : (int)Math.Ceiling((double)totalItens / itensPorPagina)
            };
        }

        public DadoComparativo ObterComparativoMensal(int ano, int mes)
        {
            var dataInicioAtual = new DateTime(ano, mes, 1);
            var dataFimAtual = dataInicioAtual.AddMonths(1).AddDays(-1);

            var dataInicioAnterior = dataInicioAtual.AddMonths(-1);
            var dataFimAnterior = dataInicioAtual.AddDays(-1);

            var valorAtual = ObterFaturamentoTotal(dataInicioAtual, dataFimAtual);
            var valorAnterior = ObterFaturamentoTotal(dataInicioAnterior, dataFimAnterior);

            var variacao = valorAtual - valorAnterior;
            var percentualVariacao = valorAnterior > 0 ? (variacao / valorAnterior) * 100 : 0;
            var tendencia = variacao > 0 ? "Crescimento" : variacao < 0 ? "Queda" : "Estável";

            return new DadoComparativo
            {
                Id = Guid.NewGuid(),
                PeriodoAtual = $"{mes}/{ano}",
                PeriodoAnterior = $"{dataInicioAnterior.Month}/{dataInicioAnterior.Year}",
                ValorAtual = valorAtual,
                ValorAnterior = valorAnterior,
                Variacao = variacao,
                PercentualVariacao = percentualVariacao,
                Tendencia = tendencia
            };
        }

        private static bool TabelaExiste(DbConnection connection, string schema, string tableName)
        {
            var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT TOP (1) 1
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                    WHERE t.name = @name
                      AND s.name = @schema;"
                : $@"
                    SELECT 1
                    FROM {schema}.sqlite_master
                    WHERE type = 'table'
                      AND name = @name
                    LIMIT 1;";
            command.Parameters.AddWithValue("@name", tableName);
            if (IsSqlServerConnection(connection))
            {
                command.Parameters.AddWithValue("@schema", NormalizarSchemaSqlServer(schema));
            }

            return command.ExecuteScalar() != null;
        }

        private static bool TabelaTemDados(DbConnection connection, string schema, string tableName)
        {
            if (!TabelaExiste(connection, schema, tableName))
            {
                return false;
            }

            var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? $"SELECT TOP (1) 1 FROM {DelimitarIdentificador(NormalizarSchemaSqlServer(schema))}.{DelimitarIdentificador(tableName)};"
                : $"SELECT 1 FROM {schema}.{tableName} LIMIT 1;";
            return command.ExecuteScalar() != null;
        }

        private static bool ColunaExiste(DbConnection connection, string tableName, string columnName)
        {
            var command = connection.CreateCommand();
            if (IsSqlServerConnection(connection))
            {
                command.CommandText = $@"
                    SELECT TOP (1) 1
                    FROM sys.columns c
                    INNER JOIN sys.tables t ON t.object_id = c.object_id
                    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                    WHERE t.name = @tableName
                      AND c.name = @columnName
                      AND s.name = 'dbo';";
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@columnName", columnName);
                return command.ExecuteScalar() != null;
            }

            command.CommandText = $"PRAGMA table_info({tableName});";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(LerTexto(reader, 1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            return connection.GetType().FullName?.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static string LimitClause(DbConnection connection)
        {
            return IsSqlServerConnection(connection)
                ? "OFFSET 0 ROWS FETCH NEXT @limit ROWS ONLY"
                : "LIMIT @limit";
        }

        private static string LimitLiteralClause(DbConnection connection, int quantidade)
        {
            return IsSqlServerConnection(connection)
                ? $"OFFSET 0 ROWS FETCH NEXT {quantidade} ROWS ONLY"
                : $"LIMIT {quantidade}";
        }

        private static string PaginationClause(DbConnection connection)
        {
            return IsSqlServerConnection(connection)
                ? "OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY"
                : "LIMIT @limit OFFSET @offset";
        }

        private static string DateOnlyExpression(DbConnection connection, string expression)
        {
            return IsSqlServerConnection(connection)
                ? $"CONVERT(date, TRY_CONVERT(datetime, {expression}))"
                : $"date({expression})";
        }

        private static string DateTimeExpression(DbConnection connection, string expression)
        {
            return IsSqlServerConnection(connection)
                ? $"TRY_CONVERT(datetime, {expression})"
                : $"datetime({expression})";
        }

        private static string HourExpression(DbConnection connection, string expression)
        {
            return IsSqlServerConnection(connection)
                ? $"DATEPART(HOUR, TRY_CONVERT(datetime, {expression}))"
                : $"CAST(strftime('%H', {expression}) AS INTEGER)";
        }

        private static string TextCastExpression(DbConnection connection, string expression)
        {
            return IsSqlServerConnection(connection)
                ? $"CONVERT(NVARCHAR(80), {expression})"
                : $"CAST({expression} AS TEXT)";
        }

        private static string NormalizarSchemaSqlServer(string schema)
        {
            return string.Equals(schema, "main", StringComparison.OrdinalIgnoreCase) ? "dbo" : schema;
        }

        private static string DelimitarIdentificador(string identifier)
        {
            return $"[{identifier.Replace("]", "]]")}]";
        }

        private static string LerTexto(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index)
                ? string.Empty
                : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static decimal LerDecimal(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index)
                ? 0
                : Convert.ToDecimal(reader.GetValue(index));
        }

        private static int LerInteiro(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index)
                ? 0
                : Convert.ToInt32(reader.GetValue(index));
        }

        private static DateTime LerData(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return DateTime.MinValue;
            }

            var raw = Convert.ToString(reader.GetValue(index));
            return DateTime.TryParse(raw, out var value) ? value : DateTime.MinValue;
        }

        private static DateTime? LerDataOpcional(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            var raw = Convert.ToString(reader.GetValue(index));
            return DateTime.TryParse(raw, out var value) ? value : null;
        }

        private static Guid LerGuid(DbDataReader reader, int index, string prefix)
        {
            if (reader.IsDBNull(index))
            {
                return CriarGuidEstavel($"{prefix}:null");
            }

            var raw = Convert.ToString(reader.GetValue(index)) ?? string.Empty;
            return Guid.TryParse(raw, out var guid)
                ? guid
                : CriarGuidEstavel($"{prefix}:{raw}");
        }

        private static decimal ConverterParaDecimal(object? value)
        {
            return value == null || value == DBNull.Value
                ? 0
                : Convert.ToDecimal(value);
        }

        private static Guid CriarGuidEstavel(string key)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(key));
            return new Guid(hash);
        }
    }
}


