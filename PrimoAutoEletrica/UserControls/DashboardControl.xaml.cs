using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace PrimoAutoEletrica.UserControls
{
    public partial class DashboardControl : UserControl
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardControl()
        {
            InitializeComponent();
            
            _viewModel = App.Services.GetRequiredService<DashboardViewModel>();
            DataContext = _viewModel;
            
            Loaded += async (_, _) => await _viewModel.CarregarDashboardAsync();
        }

        private void AtualizarDashboardButton_Click(object sender, RoutedEventArgs e)
        {
            _ = _viewModel.CarregarDashboardAsync();
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

        private void BtnNovoOrcamento_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow) mainWindow.NavigateToModuleForAutomation("Orcamentos");
        }

        private void BtnNovaOS_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow) mainWindow.NavigateToModuleForAutomation("OrdensServico");
        }

        private void BtnNovoCliente_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow) mainWindow.NavigateToModuleForAutomation("Clientes");
        }

        private void BtnZerarSistema_Click(object sender, RoutedEventArgs e)
        {
            var dbService = App.Services.GetRequiredService<PrimoAutoEletrica.Services.DatabaseService>();
            var resetWindow = new PrimoAutoEletrica.Views.Configuracoes.ResetSistemaWindow(dbService);
            resetWindow.ShowDialog();

        }

        public ObservableCollection<DashboardMetric> Metrics => ((DashboardViewModel)DataContext).Metrics;
        public ObservableCollection<DashboardRevenueBar> RevenueBars => ((DashboardViewModel)DataContext).RevenueBars;
        public ObservableCollection<DashboardHighlight> Highlights => ((DashboardViewModel)DataContext).Highlights;

    }

    public sealed record DashboardMetric(string Titulo, string Valor, string Detalhe, string Icone);

    public sealed record DashboardRevenueBar(string Dia, string ValorFormatado, double Percentual);

    public sealed record DashboardHighlight(string Titulo, string Detalhe);

    internal readonly record struct QueryParameter(string Name, object? Value);
}
