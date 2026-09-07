using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.ViewModels;

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

            Navegar(modulo);
        }

        private void AttentionItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: string modulo } || string.IsNullOrWhiteSpace(modulo))
            {
                return;
            }

            Navegar(modulo);
        }

        private void Navegar(string modulo)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToModuleForAutomation(modulo);
            }
        }

        public ObservableCollection<DashboardMetric> Metrics => _viewModel.Metrics;
        public ObservableCollection<DashboardRevenueBar> RevenueBars => _viewModel.RevenueBars;
        public ObservableCollection<DashboardHighlight> Highlights => _viewModel.Highlights;
        public ObservableCollection<DashboardAttentionItem> AttentionItems => _viewModel.AttentionItems;
        public ObservableCollection<DashboardFlowStage> FlowStages => _viewModel.FlowStages;
        public ObservableCollection<DashboardActivityItem> RecentActivities => _viewModel.RecentActivities;
        public bool IsAttentionEmpty => _viewModel.IsAttentionEmpty;
    }

    public sealed record DashboardMetric(string Titulo, string Valor, string Detalhe, string Icone);

    public sealed record DashboardRevenueBar(string Dia, string ValorFormatado, double Percentual);

    public sealed record DashboardHighlight(string Titulo, string Detalhe);

    public sealed record DashboardAttentionItem(string Titulo, string Detalhe, string Severidade, string ModuloDestino);

    public sealed record DashboardFlowStage(string Titulo, int Quantidade);

    public sealed record DashboardActivityItem(string Tipo, string Titulo, string Detalhe, string Quando, string Usuario);
}
