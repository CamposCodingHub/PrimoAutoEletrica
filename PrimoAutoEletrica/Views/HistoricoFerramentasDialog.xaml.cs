using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Services;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class HistoricoFerramentasDialog : Window
    {
        private readonly IFerramentaService _ferramentaService;

        public HistoricoFerramentasDialog(Guid? ferramentaId = null)
        {
            InitializeComponent();

            _ferramentaService = App.Services.GetService<IFerramentaService>()
                ?? new FerramentaService(App.Database, App.Logger);

            Loaded += async (s, e) =>
            {
                try
                {
                    var lista = await _ferramentaService.ObterHistoricoMovimentacoesAsync(ferramentaId, 100);
                    HistoricoDataGrid.ItemsSource = lista;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao carregar histórico de movimentações:\n{ex.Message}", "Histórico", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
