using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class DashboardControl : UserControl
    {
        private static readonly CultureInfo PtBr = new("pt-BR");

        public ObservableCollection<DashboardMetric> Metrics { get; } = new();
        public ObservableCollection<DashboardRevenueBar> RevenueBars { get; } = new();
        public ObservableCollection<DashboardHighlight> Highlights { get; } = new();

        public DashboardControl()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += (_, _) => CarregarDashboard();
        }

        private void AtualizarDashboardButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarDashboard();
        }

        private void AtalhoModuloButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string modulo })
            {
                return;
            }

            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToModuleForAutomation(modulo);
            }
        }

        private void CarregarDashboard()
        {
            Metrics.Clear();
            RevenueBars.Clear();
            Highlights.Clear();

            try
            {
                using var connection = App.Database.GetConnection();
                connection.Open();

                var hoje = DateTime.Today;
                var mesAtual = hoje.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                var dataHoje = hoje.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var vendasDataColumn = ResolveColumn(connection, "Vendas", "Data", "DataVenda");
                var vendasTotalColumn = ResolveColumn(connection, "Vendas", "Total", "ValorTotal");
                var veiculoIdPreenchido = IsSqlServerConnection(connection)
                    ? "VeiculoId IS NOT NULL"
                    : "VeiculoId IS NOT NULL AND trim(VeiculoId) <> ''";
                var estoqueQuantidadeColumn = ResolveColumn(connection, "Produtos", "QuantidadeEstoque", "EstoqueAtual");
                var estoqueMinimoColumn = ResolveColumn(connection, "Produtos", "QuantidadeMinima", "EstoqueMinimo");

                var faturamentoDia = Sum(connection, "Vendas", vendasTotalColumn, $"{DatePrefix(connection, vendasDataColumn, 10)} = @Data AND COALESCE(Status, 'Concluida') <> 'Cancelada'", Param("@Data", dataHoje));
                var faturamentoMes = Sum(connection, "Vendas", vendasTotalColumn, $"{DatePrefix(connection, vendasDataColumn, 7)} = @Mes AND COALESCE(Status, 'Concluida') <> 'Cancelada'", Param("@Mes", mesAtual));
                var osAbertas = Count(connection, "OrdensServico", "Ativo = 1 AND lower(Status) NOT IN ('entregue', 'cancelada', 'finalizada')");
                var osAtrasadas = Count(connection, "OrdensServico", $"Ativo = 1 AND DataPrevisao IS NOT NULL AND {DatePrefix(connection, "DataPrevisao", 10)} < @Data AND lower(Status) NOT IN ('entregue', 'cancelada', 'finalizada')", Param("@Data", dataHoje));
                var orcamentosPendentes = Count(connection, "Orcamentos", "lower(COALESCE(Status, 'pendente')) NOT IN ('aprovado', 'recusado', 'vencido', 'cancelado', 'convertido')");
                var veiculosNaOficina = CountDistinct(connection, "OrdensServico", "VeiculoId", $"Ativo = 1 AND {veiculoIdPreenchido} AND lower(Status) NOT IN ('entregue', 'cancelada')");
                var contasReceber = Sum(connection, "ContasReceber", "Valor", "lower(COALESCE(Status, 'pendente')) NOT IN ('pago', 'liquidado', 'cancelado')");
                var contasPagar = Sum(connection, "ContasPagar", "Valor", "lower(COALESCE(Status, 'pendente')) NOT IN ('pago', 'liquidado', 'cancelado')");
                var estoqueCritico = Count(connection, "Produtos", $"Ativo = 1 AND {estoqueMinimoColumn} > 0 AND {estoqueQuantidadeColumn} <= {estoqueMinimoColumn}");
                var agendaDia = Count(connection, "Agendamentos", $"{DatePrefix(connection, "DataAgendamento", 10)} = @Data AND lower(COALESCE(Status, 'agendado')) NOT IN ('cancelado', 'cancelada', 'concluido', 'concluida')", Param("@Data", dataHoje));
                var servicosAndamento = Count(connection, "OrdensServico", "Ativo = 1 AND lower(Status) IN ('em diagnostico', 'em diagnóstico', 'aguardando aprovacao', 'aguardando aprovação', 'aguardando peca', 'aguardando peça', 'em execucao', 'em execução', 'em andamento')");

                Metrics.Add(new DashboardMetric("Faturamento do dia", Money(faturamentoDia), "Vendas concluidas hoje", "R$"));
                Metrics.Add(new DashboardMetric("Faturamento do mes", Money(faturamentoMes), "Soma das vendas do mes atual", "MES"));
                Metrics.Add(new DashboardMetric("OS abertas", osAbertas.ToString("N0", PtBr), "Ordens ainda operacionais", "OS"));
                Metrics.Add(new DashboardMetric("OS atrasadas", osAtrasadas.ToString("N0", PtBr), "Previsao anterior a hoje", "!"));
                Metrics.Add(new DashboardMetric("Orcamentos pendentes", orcamentosPendentes.ToString("N0", PtBr), "Aguardando decisao ou envio", "ORC"));
                Metrics.Add(new DashboardMetric("Veiculos na oficina", veiculosNaOficina.ToString("N0", PtBr), "Veiculos vinculados a OS ativas", "VEI"));
                Metrics.Add(new DashboardMetric("Contas a receber", Money(contasReceber), "Titulos em aberto", "REC"));
                Metrics.Add(new DashboardMetric("Contas a pagar", Money(contasPagar), "Compromissos em aberto", "PAG"));
                Metrics.Add(new DashboardMetric("Estoque critico", estoqueCritico.ToString("N0", PtBr), "Produtos abaixo do minimo", "EST"));
                Metrics.Add(new DashboardMetric("Agenda do dia", agendaDia.ToString("N0", PtBr), "Atendimentos de hoje", "AGE"));
                Metrics.Add(new DashboardMetric("Servicos em andamento", servicosAndamento.ToString("N0", PtBr), "OS em fluxo tecnico", "SRV"));

                CarregarGraficoFaturamento(connection, hoje);
                CarregarHighlights(connection, dataHoje, osAtrasadas, estoqueCritico);

                AtualizadoEmTextBlock.Text = $"Atualizado em {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Falha ao carregar dashboard operacional.", ex);
                Highlights.Add(new DashboardHighlight("Dashboard indisponivel", "Nao foi possivel carregar os indicadores. Consulte os logs."));
                AtualizadoEmTextBlock.Text = "Falha ao atualizar indicadores.";
            }
        }

        private void CarregarGraficoFaturamento(DbConnection connection, DateTime hoje)
        {
            var pontos = Enumerable.Range(0, 7)
                .Select(offset => hoje.AddDays(offset - 6))
                .Select(data => new
                {
                    Data = data,
                    Valor = Sum(connection, "Vendas", ResolveColumn(connection, "Vendas", "Total", "ValorTotal"), $"{DatePrefix(connection, ResolveColumn(connection, "Vendas", "Data", "DataVenda"), 10)} = @Data AND COALESCE(Status, 'Concluida') <> 'Cancelada'", Param("@Data", data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
                })
                .ToArray();

            var maximo = pontos.Max(ponto => ponto.Valor);
            foreach (var ponto in pontos)
            {
                var percentual = maximo <= 0 ? 0 : (double)Math.Round((ponto.Valor / maximo) * 100m, 2);
                RevenueBars.Add(new DashboardRevenueBar(
                    ponto.Data.ToString("dd/MM", PtBr),
                    Money(ponto.Valor),
                    percentual));
            }
        }

        private void CarregarHighlights(DbConnection connection, string dataHoje, int osAtrasadas, int estoqueCritico)
        {
            Highlights.Add(new DashboardHighlight("Agenda", $"{Count(connection, "Agendamentos", $"{DatePrefix(connection, "DataAgendamento", 10)} = @Data", Param("@Data", dataHoje)):N0} atendimento(s) cadastrados para hoje."));
            Highlights.Add(new DashboardHighlight("Atrasos", osAtrasadas == 0 ? "Nenhuma OS atrasada." : $"{osAtrasadas:N0} OS com previsao vencida."));
            Highlights.Add(new DashboardHighlight("Estoque", estoqueCritico == 0 ? "Nenhum item abaixo do minimo configurado." : $"{estoqueCritico:N0} item(ns) abaixo do minimo."));

            var produtosSemPreco = Count(connection, "Produtos", "Ativo = 1 AND PrecoVenda <= 0");
            if (produtosSemPreco > 0)
            {
                Highlights.Add(new DashboardHighlight("Preco", $"{produtosSemPreco:N0} produto(s) sem preco de venda."));
            }

            var tabelasAusentes = new[]
            {
                "Vendas", "OrdensServico", "Orcamentos", "Produtos", "Agendamentos", "ContasReceber", "ContasPagar"
            }
            .Where(tabela => !TableExists(connection, tabela))
            .ToArray();

            if (tabelasAusentes.Length > 0)
            {
                Highlights.Add(new DashboardHighlight("Banco incompleto", $"Tabelas ausentes: {string.Join(", ", tabelasAusentes)}."));
            }
        }

        private static decimal Sum(DbConnection connection, string tableName, string columnName, string whereSql, params QueryParameter[] parameters)
        {
            if (!TableExists(connection, tableName) || !ColumnExists(connection, tableName, columnName))
            {
                return 0m;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COALESCE(SUM({columnName}), 0) FROM {tableName} WHERE {whereSql};";
            AddParameters(command, parameters);
            return Convert.ToDecimal(command.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
        }

        private static int Count(DbConnection connection, string tableName, string whereSql = "1 = 1", params QueryParameter[] parameters)
        {
            if (!TableExists(connection, tableName))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(1) FROM {tableName} WHERE {whereSql};";
            AddParameters(command, parameters);
            return Convert.ToInt32(command.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
        }

        private static int CountDistinct(DbConnection connection, string tableName, string columnName, string whereSql, params QueryParameter[] parameters)
        {
            if (!TableExists(connection, tableName) || !ColumnExists(connection, tableName, columnName))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(DISTINCT {columnName}) FROM {tableName} WHERE {whereSql};";
            AddParameters(command, parameters);
            return Convert.ToInt32(command.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
        }

        private static bool TableExists(DbConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT TOP (1) 1
                    FROM sys.tables
                    WHERE name = @Name;"
                : @"
                    SELECT 1
                    FROM sqlite_master
                    WHERE type = 'table'
                      AND name = @Name
                    LIMIT 1;";
            command.Parameters.AddWithValue("@Name", tableName);
            return command.ExecuteScalar() != null;
        }

        private static string DatePrefix(DbConnection connection, string columnName, int length)
        {
            return IsSqlServerConnection(connection)
                ? $"SUBSTRING(CONVERT(varchar(30), {columnName}, 120), 1, {length})"
                : $"substr({columnName}, 1, {length})";
        }

        private static string ResolveColumn(DbConnection connection, string tableName, string preferredColumn, string fallbackColumn)
        {
            if (ColumnExists(connection, tableName, preferredColumn))
            {
                return preferredColumn;
            }

            return ColumnExists(connection, tableName, fallbackColumn)
                ? fallbackColumn
                : preferredColumn;
        }

        private static bool ColumnExists(DbConnection connection, string tableName, string columnName)
        {
            if (!TableExists(connection, tableName))
            {
                return false;
            }

            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT TOP (1) 1
                    FROM sys.columns c
                    INNER JOIN sys.tables t ON t.object_id = c.object_id
                    WHERE t.name = @TableName
                      AND c.name = @ColumnName;"
                : @"
                    SELECT 1
                    FROM pragma_table_info(@TableName)
                    WHERE name = @ColumnName
                    LIMIT 1;";
            command.Parameters.AddWithValue("@TableName", tableName);
            command.Parameters.AddWithValue("@ColumnName", columnName);
            return command.ExecuteScalar() != null;
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            var typeName = connection.GetType().FullName ?? string.Empty;
            return typeName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddParameters(DbCommand command, params QueryParameter[] parameters)
        {
            foreach (var parameter in parameters)
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Name;
                dbParameter.Value = parameter.Value ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }
        }

        private static QueryParameter Param(string name, object value)
        {
            return new QueryParameter(name, value);
        }

        private static string Money(decimal value)
        {
            return value.ToString("C", PtBr);
        }
    }

    public sealed record DashboardMetric(string Titulo, string Valor, string Detalhe, string Icone);

    public sealed record DashboardRevenueBar(string Dia, string ValorFormatado, double Percentual);

    public sealed record DashboardHighlight(string Titulo, string Detalhe);

    internal readonly record struct QueryParameter(string Name, object? Value);
}
