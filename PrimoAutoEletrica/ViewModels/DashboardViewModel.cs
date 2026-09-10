using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.ViewModels
{
    public enum DashboardLoadState
    {
        Idle,
        Loading,
        Loaded,
        Error
    }

    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private static readonly CultureInfo PtBr = new("pt-BR");

        /// <summary>Fluxo real do Kanban da oficina (OficinaProfissionalService.StatusKanban).</summary>
        private static readonly string[] FluxoOperacional =
        {
            "Agendado",
            "Recebido",
            "Em diagnostico",
            "Aguardando aprovacao",
            "Aguardando peca",
            "Em execucao",
            "Finalizado",
            "Aguardando pagamento",
            "Entregue",
            "Cancelado"
        };

        public ObservableCollection<DashboardMetric> Metrics { get; } = new();
        public ObservableCollection<DashboardRevenueBar> RevenueBars { get; } = new();
        public ObservableCollection<DashboardHighlight> Highlights { get; } = new();
        public ObservableCollection<DashboardAttentionItem> AttentionItems { get; } = new();
        public ObservableCollection<DashboardFlowStage> FlowStages { get; } = new();
        public ObservableCollection<DashboardActivityItem> RecentActivities { get; } = new();

        [ObservableProperty]
        private string _atualizadoEmTexto = string.Empty;

        [ObservableProperty]
        private string _subtitulo = "Visão operacional com dados reais do banco.";

        [ObservableProperty]
        private DashboardLoadState _loadState = DashboardLoadState.Idle;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isAttentionEmpty = true;

        [ObservableProperty]
        private bool _hasRecentActivity;

        [ObservableProperty]
        private bool _hasRevenueBars;

        public bool IsLoading => LoadState == DashboardLoadState.Loading;
        public bool HasError => LoadState == DashboardLoadState.Error;
        public bool IsLoaded => LoadState == DashboardLoadState.Loaded;

        public DashboardViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        partial void OnLoadStateChanged(DashboardLoadState value)
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsLoaded));
        }

        [RelayCommand]
        public async Task CarregarDashboardAsync()
        {
            Metrics.Clear();
            RevenueBars.Clear();
            Highlights.Clear();
            AttentionItems.Clear();
            FlowStages.Clear();
            RecentActivities.Clear();
            IsAttentionEmpty = true;
            HasRecentActivity = false;
            HasRevenueBars = false;
            ErrorMessage = string.Empty;
            LoadState = DashboardLoadState.Loading;
            AtualizadoEmTexto = "Atualizando...";

            try
            {
                using var connection = _databaseService.GetConnection();
                await connection.OpenAsync();

                var osAbertas = await CountSafeAsync(connection,
                    "SELECT COUNT(1) FROM OrdensServico WHERE Ativo = 1 AND lower(COALESCE(Status,'')) NOT IN ('entregue', 'cancelada', 'cancelado', 'finalizada', 'finalizado')");

                var osEmAndamento = await CountSafeAsync(connection,
                    @"SELECT COUNT(1) FROM OrdensServico
                      WHERE Ativo = 1 AND lower(COALESCE(Status,'')) IN ('em execucao', 'em andamento', 'em diagnostico')");

                var osAguardando = await CountSafeAsync(connection,
                    @"SELECT COUNT(1) FROM OrdensServico
                      WHERE Ativo = 1 AND lower(COALESCE(Status,'')) LIKE 'aguardando%'");

                var orcamentosPendentes = await CountSafeAsync(connection,
                    @"SELECT COUNT(1) FROM Orcamentos
                      WHERE lower(COALESCE(Status,'')) NOT IN ('aprovado', 'recusado', 'vencido', 'cancelado', 'convertido em os', 'convertido em venda')");

                var faturamentoMes = await SumVendasMesAsync(connection);
                var totalClientes = await CountSafeAsync(connection, "SELECT COUNT(1) FROM Clientes");
                var totalProdutos = await CountSafeAsync(connection, "SELECT COUNT(1) FROM Produtos WHERE Ativo = 1");
                var estoqueBaixo = await CountSafeAsync(connection,
                    "SELECT COUNT(1) FROM Produtos WHERE Ativo = 1 AND QuantidadeEstoque <= EstoqueMinimo AND EstoqueMinimo > 0");
                var agendamentosHoje = await CountSafeAsync(connection,
                    @"SELECT COUNT(1) FROM Agendamentos
                      WHERE date(DataAgendamento) = date('now')
                        AND lower(COALESCE(Status,'')) NOT IN ('cancelado', 'concluido', 'concluído', 'entregue')");

                // Workshop Pulse — labels via LocalizationService (cultura UI); valores formatados em pt-BR
                var L = LocalizationService.Instance.GetString;
                Metrics.Add(new DashboardMetric(L("MetricBillingMonth"), faturamentoMes.ToString("C2", PtBr), L("MetricBillingMonthDetail"), "R$"));
                Metrics.Add(new DashboardMetric(L("MetricOpenOs"), osAbertas.ToString("N0", PtBr), L("MetricOpenOsDetail"), "OS"));
                Metrics.Add(new DashboardMetric(L("MetricPendingQuotes"), orcamentosPendentes.ToString("N0", PtBr), L("MetricPendingQuotesDetail"), "OR"));
                Metrics.Add(new DashboardMetric(L("MetricClients"), totalClientes.ToString("N0", PtBr), L("MetricClientsDetail"), "CL"));
                Metrics.Add(new DashboardMetric(L("MetricStockProducts"), totalProdutos.ToString("N0", PtBr), L("MetricStockProductsDetail"), "PR"));
                Metrics.Add(new DashboardMetric(L("MetricInProgress"), osEmAndamento.ToString("N0", PtBr), L("MetricInProgressDetail"), "EX"));
                Metrics.Add(new DashboardMetric(L("MetricWaiting"), osAguardando.ToString("N0", PtBr), L("MetricWaitingDetail"), "AG"));
                Metrics.Add(new DashboardMetric(L("MetricAgendaToday"), agendamentosHoje.ToString("N0", PtBr), L("MetricAgendaTodayDetail"), "HO"));

                await CarregarAttentionAsync(connection, orcamentosPendentes, estoqueBaixo);
                await CarregarFluxoAsync(connection);
                await CarregarAtividadeRecenteAsync(connection);
                await CarregarRevenueBarsAsync(connection);

                Subtitulo = $"Oficina: {osAbertas} OS abertas · {osEmAndamento} em andamento · {orcamentosPendentes} orçamentos pendentes.";
                AtualizadoEmTexto = $"Atualizado em {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                LoadState = DashboardLoadState.Loaded;
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Falha ao carregar Centro de Operacoes.", ex);
                ErrorMessage = "Nao foi possivel carregar os indicadores. Consulte os logs.";
                AtualizadoEmTexto = "Falha ao atualizar.";
                LoadState = DashboardLoadState.Error;
                Highlights.Add(new DashboardHighlight("Centro de Operacoes indisponivel", ErrorMessage));
            }
        }

        private async Task CarregarAttentionAsync(IDbConnection connection, int orcamentosPendentes, int estoqueBaixo)
        {
            if (orcamentosPendentes > 0)
            {
                AttentionItems.Add(new DashboardAttentionItem(
                    "Orçamentos",
                    $"{orcamentosPendentes} orçamento(s) aguardando decisão",
                    "Warning",
                    "Orcamentos"));
            }

            if (estoqueBaixo > 0)
            {
                AttentionItems.Add(new DashboardAttentionItem(
                    "Estoque crítico",
                    $"{estoqueBaixo} produto(s) no ou abaixo do mínimo",
                    "Danger",
                    "Estoque"));
            }

            try
            {
                var osAtrasadas = await connection.QueryAsync<(string Numero, string Status, string? Cliente)>(
                    @"SELECT Numero, Status, ClienteNomeSnapshot
                      FROM OrdensServico
                      WHERE Ativo = 1
                        AND DataPrevisao IS NOT NULL
                        AND datetime(DataPrevisao) < datetime('now')
                        AND lower(COALESCE(Status,'')) NOT IN ('entregue', 'cancelada', 'cancelado', 'finalizada', 'finalizado')
                      ORDER BY DataPrevisao ASC
                      LIMIT 8");

                foreach (var os in osAtrasadas)
                {
                    AttentionItems.Add(new DashboardAttentionItem(
                        $"OS {os.Numero}",
                        $"Prazo vencido · {os.Status} · {os.Cliente ?? "Cliente n/d"}",
                        "Danger",
                        "OrdensServico"));
                }
            }
            catch
            {
                // DataPrevisao pode estar nulo ou formato inconsistente.
            }

            try
            {
                var aguardandoAprovacao = await CountSafeAsync(connection,
                    @"SELECT COUNT(1) FROM OrdensServico
                      WHERE Ativo = 1 AND lower(COALESCE(Status,'')) IN ('aguardando aprovacao', 'aguardando aprovação')");
                if (aguardandoAprovacao > 0)
                {
                    AttentionItems.Add(new DashboardAttentionItem(
                        "Aprovação de OS",
                        $"{aguardandoAprovacao} OS aguardando aprovação",
                        "Warning",
                        "OficinaKanban"));
                }
            }
            catch { /* ignore */ }

            try
            {
                var agendaAtrasada = await CountSafeAsync(connection,
                    @"SELECT COUNT(1) FROM Agendamentos
                      WHERE date(DataAgendamento) < date('now')
                        AND lower(COALESCE(Status,'')) IN ('agendado', 'confirmado', 'pendente', 'em andamento')");
                if (agendaAtrasada > 0)
                {
                    AttentionItems.Add(new DashboardAttentionItem(
                        "Agenda atrasada",
                        $"{agendaAtrasada} agendamento(s) com data anterior a hoje",
                        "Warning",
                        "Agendamentos"));
                }
            }
            catch { /* ignore */ }

            IsAttentionEmpty = AttentionItems.Count == 0;

            // Highlights mantidos como espelho resumido (compat / resumo lateral)
            if (IsAttentionEmpty)
            {
                Highlights.Add(new DashboardHighlight(
                    "Operação estável",
                    "Nenhuma ocorrência requer atenção no momento."));
            }
            else
            {
                foreach (var item in AttentionItems.Take(5))
                {
                    Highlights.Add(new DashboardHighlight(item.Titulo, item.Detalhe));
                }
            }
        }

        private async Task CarregarFluxoAsync(IDbConnection connection)
        {
            var contagens = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var rows = await connection.QueryAsync<(string Status, int Total)>(
                    @"SELECT COALESCE(Status, 'Rascunho') as Status, COUNT(1) as Total
                      FROM OrdensServico
                      WHERE Ativo = 1
                      GROUP BY COALESCE(Status, 'Rascunho')");

                foreach (var row in rows)
                {
                    contagens[row.Status] = row.Total;
                }
            }
            catch
            {
                // Sem tabela: fluxo vazio.
            }

            foreach (var stage in FluxoOperacional)
            {
                var total = SomarStatusEquivalente(contagens, stage);
                FlowStages.Add(new DashboardFlowStage(stage, total));
            }

            // Status reais fora do Kanban (ex.: Rascunho, Aberta, Aprovada, Pronta para entrega)
            var extras = contagens
                .Where(kv => !FluxoOperacional.Any(s => string.Equals(s, kv.Key, StringComparison.OrdinalIgnoreCase))
                             && !EquivalenteKanban(kv.Key)
                             && kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .Take(4);

            foreach (var extra in extras)
            {
                FlowStages.Add(new DashboardFlowStage(extra.Key, extra.Value));
            }
        }

        private async Task CarregarAtividadeRecenteAsync(IDbConnection connection)
        {
            try
            {
                var eventos = await connection.QueryAsync<(string Titulo, string Descricao, string Tipo, string Usuario, string DataEvento, string Numero)>(
                    @"SELECT e.Titulo, e.Descricao, e.Tipo, e.Usuario, e.DataEvento, os.Numero
                      FROM OrdemServicoEventos e
                      INNER JOIN OrdensServico os ON os.Id = e.OrdemServicoId
                      ORDER BY datetime(e.DataEvento) DESC
                      LIMIT 12");

                foreach (var ev in eventos)
                {
                    var quando = DateTime.TryParse(ev.DataEvento, out var dt)
                        ? dt.ToString("dd/MM HH:mm", PtBr)
                        : ev.DataEvento;
                    RecentActivities.Add(new DashboardActivityItem(
                        string.IsNullOrWhiteSpace(ev.Tipo) ? "OS" : ev.Tipo,
                        string.IsNullOrWhiteSpace(ev.Titulo) ? "Evento de OS" : ev.Titulo,
                        $"OS {ev.Numero} · {ev.Descricao}",
                        quando,
                        string.IsNullOrWhiteSpace(ev.Usuario) ? "—" : ev.Usuario));
                }
            }
            catch
            {
                // Sem eventos: empty state na UI.
            }

            HasRecentActivity = RecentActivities.Count > 0;
        }

        private async Task CarregarRevenueBarsAsync(IDbConnection connection)
        {
            try
            {
                var revenueData = (await connection.QueryAsync<(string Dia, decimal Total)>(
                    @"SELECT strftime('%d/%m', Data) as Dia, COALESCE(SUM(Valor), 0) as Total
                      FROM Vendas
                      WHERE date(Data) >= date('now', '-7 day')
                      GROUP BY date(Data)
                      ORDER BY date(Data)")).ToList();

                if (revenueData.Count == 0)
                {
                    HasRevenueBars = false;
                    return;
                }

                var max = revenueData.Max(r => (double)r.Total);
                if (max <= 0) max = 1;

                foreach (var item in revenueData)
                {
                    var percentual = Math.Max(2, ((double)item.Total / max) * 100);
                    RevenueBars.Add(new DashboardRevenueBar(item.Dia, item.Total.ToString("C0", PtBr), percentual));
                }

                HasRevenueBars = RevenueBars.Count > 0;
            }
            catch
            {
                HasRevenueBars = false;
            }
        }

        private static int SomarStatusEquivalente(Dictionary<string, int> contagens, string stage)
        {
            var total = 0;
            foreach (var kv in contagens)
            {
                if (StatusPertenceAoEstagio(kv.Key, stage))
                {
                    total += kv.Value;
                }
            }

            return total;
        }

        private static bool EquivalenteKanban(string status)
        {
            return FluxoOperacional.Any(s => StatusPertenceAoEstagio(status, s));
        }

        private static bool StatusPertenceAoEstagio(string status, string stage)
        {
            if (string.Equals(status, stage, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Alinhado a OficinaProfissionalService.PertenceAColuna (somente mapeamentos reais).
            if (string.Equals(stage, "Agendado", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(status, "Agendado", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(stage, "Recebido", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(status, "Recebido", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(status, "Aberta", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(status, "Rascunho", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(stage, "Finalizado", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(status, "Finalizado", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(status, "Finalizada", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(status, "Pronta para entrega", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(stage, "Cancelado", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(status, "Cancelado", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(status, "Cancelada", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static async Task<int> CountSafeAsync(IDbConnection connection, string sql)
        {
            try
            {
                return await connection.ExecuteScalarAsync<int>(sql);
            }
            catch
            {
                return 0;
            }
        }

        private static async Task<decimal> SumVendasMesAsync(IDbConnection connection)
        {
            try
            {
                return await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT COALESCE(SUM(Valor),0) FROM Vendas
                      WHERE strftime('%m', Data) = strftime('%m', 'now')
                        AND strftime('%Y', Data) = strftime('%Y', 'now')");
            }
            catch
            {
                try
                {
                    return await connection.ExecuteScalarAsync<decimal>(
                        @"SELECT COALESCE(SUM(Valor),0) FROM Vendas
                          WHERE MONTH(Data) = MONTH(GETDATE()) AND YEAR(Data) = YEAR(GETDATE())");
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}
