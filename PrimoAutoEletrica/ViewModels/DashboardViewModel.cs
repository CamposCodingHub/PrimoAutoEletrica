using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimoAutoEletrica.Utilities;
using Dapper;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.UserControls; // For the Records (Metrics, Highlights, RevenueBars)

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
                // Início da atualização com retry resiliente
                AtualizadoEmTexto = "Atualizando...";
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                // Consultas Dapper diretas
                int osAbertas = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM OrdensServico WHERE Ativo = 1 AND Status NOT IN ('entregue', 'cancelada', 'finalizada')");

                int orcamentosPendentes = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Orcamentos WHERE Status NOT IN ('aprovado', 'recusado', 'vencido', 'cancelado')");

                decimal faturamentoMes = await connection.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(Valor),0) FROM Vendas WHERE MONTH(Data) = MONTH(GETDATE()) AND YEAR(Data) = YEAR(GETDATE())");

                // Populate collections
                Metrics.Add(new DashboardMetric("Faturamento do mes", faturamentoMes.ToString("C2", PtBr), "Soma das vendas do mes atual", "MES"));
                Metrics.Add(new DashboardMetric("OS abertas", osAbertas.ToString("N0", PtBr), "Ordens ainda operacionais", "OS"));
                Metrics.Add(new DashboardMetric("Orcamentos pendentes", orcamentosPendentes.ToString("N0", PtBr), "Aguardando decisao ou envio", "ORC"));

                Highlights.Add(new DashboardHighlight("MVVM Ativado", "O Dashboard agora utiliza arquitetura MVVM, DI e Dapper!"));

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
