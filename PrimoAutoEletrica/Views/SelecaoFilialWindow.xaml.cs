using PrimoAutoEletrica.Models;
using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Services;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class SelecaoFilialWindow : Window
    {
        private readonly FilialService _filialService;
        public Filial? FilialSelecionada { get; private set; }

        public SelecaoFilialWindow()
        {
            InitializeComponent();
            _filialService = App.Services.GetRequiredService<FilialService>();
            Loaded += SelecaoFilialWindow_Loaded;
        }

        private async void SelecaoFilialWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var filiais = await _filialService.CarregarFiliaisAsync();
                FiliaisListView.ItemsSource = filiais;
                
                if (filiais.Count > 0)
                {
                    FiliaisListView.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erro ao carregar filiais: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            if (FiliaisListView.SelectedItem is Filial filial)
            {
                FilialSelecionada = filial;
                _filialService.DefinirFilialAtual(filial.Id);
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Selecione uma filial antes de confirmar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}