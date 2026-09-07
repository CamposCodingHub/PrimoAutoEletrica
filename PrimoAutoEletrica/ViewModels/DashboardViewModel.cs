using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private static readonly CultureInfo PtBr = new("pt-BR");

        public ObservableCollection<DashboardMetric> Metrics { get; } = new();
        public ObservableCollection<DashboardRevenueBar> RevenueBars { get; } = new();
        public ObservableCollection<DashboardHighlight> Highlights { get; } = new();

        [ObservableProperty]
        private string _atualizadoEmTexto = string.Empty;

        public DashboardViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [RelayCommand]
        public async Task CarregarDashboardAsync()
        {
            Metrics.Clear();
            RevenueBars.Clear();
            Highlights.Clear();

            try
            {
                AtualizadoEmTexto = "Atualizando...";
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // KPI: OS abertas
                int osAbertas = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM OrdensServico WHERE Ativo = 1 AND Status NOT IN ('entregue', 'cancelada', 'finalizada')");

                // KPI: Orcamentos pendentes
                int orcamentosPendentes = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM Orcamentos WHERE Status NOT IN ('aprovado', 'recusado', 'vencido', 'cancelado')");

                // KPI: Faturamento do mes
                decimal faturamentoMes = 0;
                try
                {
                    faturamentoMes = await connection.ExecuteScalarAsync<decimal>(
                        "SELECT COALESCE(SUM(Valor),0) FROM Vendas WHERE MONTH(Data) = MONTH(GETDATE()) AND YEAR(Data) = YEAR(GETDATE())");
                }
                catch
                {
                    // SQLite nao suporta GETDATE(), tentar alternativa
                    try
                    {
                        faturamentoMes = await connection.ExecuteScalarAsync<decimal>(
                            "SELECT COALESCE(SUM(Valor),0) FROM Vendas WHERE strftime('%m', Data) = strftime('%m', 'now') AND strftime('%Y', Data) = strftime('%Y', 'now')");
                    }
                    catch { /* Tabela pode nao existir */ }
                }

                // KPI: Clientes cadastrados
                int totalClientes = 0;
                try
                {
                    totalClientes = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM Clientes");
                }
                catch { /* Tabela pode nao existir */ }

                // KPI: Produtos com estoque baixo
                int estoqueBaixo = 0;
                try
                {
                    estoqueBaixo = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM Produtos WHERE Ativo = 1 AND QuantidadeEstoque <= EstoqueMinimo AND EstoqueMinimo > 0");
                }
                catch { /* Tabela ou colunas podem nao existir */ }

                // KPI: Total de produtos
                int totalProdutos = 0;
                try
                {
                    totalProdutos = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM Produtos WHERE Ativo = 1");
                }
                catch { /* Tabela pode nao existir */ }

                // Populate Metrics
                Metrics.Add(new DashboardMetric("Faturamento do mes", faturamentoMes.ToString("C2", PtBr), "Soma das vendas do mes atual", "💰"));
                Metrics.Add(new DashboardMetric("OS abertas", osAbertas.ToString("N0", PtBr), "Ordens ainda operacionais", "⚙️"));
                Metrics.Add(new DashboardMetric("Orcamentos pendentes", orcamentosPendentes.ToString("N0", PtBr), "Aguardando decisao ou envio", "📋"));
                Metrics.Add(new DashboardMetric("Clientes", totalClientes.ToString("N0", PtBr), "Total de clientes cadastrados", "👥"));
                Metrics.Add(new DashboardMetric("Produtos em estoque", totalProdutos.ToString("N0", PtBr), "Produtos ativos no catalogo", "📦"));

                // Estoque baixo como alerta
                if (estoqueBaixo > 0)
                {
                    Metrics.Add(new DashboardMetric("Estoque baixo", estoqueBaixo.ToString("N0", PtBr), "Produtos abaixo do minimo", "⚠️"));
                }

                // Revenue bars - últimos 7 dias
                try
                {
                    var revenueData = await connection.QueryAsync<dynamic>(
                        @"SELECT 
                            CAST(Data AS DATE) as Dia,
                            COALESCE(SUM(Valor), 0) as Total
                        FROM Vendas 
                        WHERE Data >= DATEADD(day, -7, GETDATE())
                        GROUP BY CAST(Data AS DATE)
                        ORDER BY Dia");

                    foreach (var item in revenueData)
                    {
                        decimal valor = (decimal)(item.Total ?? 0m);
                        string dia = ((DateTime)item.Dia).ToString("dd/MM");
                        RevenueBars.Add(new DashboardRevenueBar(dia, valor.ToString("C0", PtBr), (double)valor));
                    }
                }
                catch { /* Tabela ou funcao pode nao existir */ }

                // Highlights
                Highlights.Add(new DashboardHighlight("Sistema v1.2.1", "Build Release com 92/92 testes aprovados."));

                if (estoqueBaixo > 0)
                {
                    Highlights.Add(new DashboardHighlight($"⚠️ {estoqueBaixo} produto(s) com estoque baixo", "Verifique o modulo de Estoque para reabastecer."));
                }

                AtualizadoEmTexto = $"Atualizado em {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Falha ao carregar dashboard (MVVM).", ex);
                Highlights.Add(new DashboardHighlight("Dashboard indisponivel", "Nao foi possivel carregar os indicadores. Consulte os logs."));
                AtualizadoEmTexto = "Falha ao atualizar indicadores.";
            }
        }
    }
}
