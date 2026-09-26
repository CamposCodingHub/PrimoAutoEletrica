using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.UserControls
{
    public partial class FerramentasControl : UserControl
    {
        private readonly IToolService _toolService;
        private List<Tool> _todasFerramentas = new();
        private bool _isInitialized = false;

        public FerramentasControl()
        {
            InitializeComponent();
            _toolService = new ToolService();
            _isInitialized = true;

            Loaded += async (s, e) => await CarregarFerramentasAsync();
        }

        public async Task CarregarFerramentasAsync()
        {
            try
            {
                var ferramentas = await _toolService.ListarFerramentasAsync();
                _todasFerramentas = ferramentas.ToList();

                AtualizarIndicadores(_todasFerramentas);
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar inventário de ferramentas: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarIndicadores(List<Tool> itens)
        {
            TotalFerramentasText.Text = itens.Count.ToString();
            DisponiveisText.Text = itens.Count(t => t.Status == ToolStatus.AVAILABLE).ToString();
            EmUsoText.Text = itens.Count(t => t.Status == ToolStatus.IN_USE).ToString();
            ManutencaoText.Text = itens.Count(t => t.Status == ToolStatus.MAINTENANCE || t.Status == ToolStatus.DAMAGED).ToString();
        }

        private void AplicarFiltros()
        {
            if (!_isInitialized || FerramentasDataGrid == null)
            {
                return;
            }

            var filtrados = _todasFerramentas.AsEnumerable();

            var busca = BuscaTextBox?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                filtrados = filtrados.Where(t =>
                    t.Code.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (t.Brand != null && t.Brand.Contains(busca, StringComparison.OrdinalIgnoreCase)) ||
                    t.LocationName.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (t.CurrentResponsibleUserName != null && t.CurrentResponsibleUserName.Contains(busca, StringComparison.OrdinalIgnoreCase)));
            }

            if (FiltroCategoriaCombo?.SelectedItem is ComboBoxItem catItem && catItem.Content?.ToString() is string cat && !cat.StartsWith("Todas", StringComparison.OrdinalIgnoreCase))
            {
                filtrados = filtrados.Where(t => string.Equals(t.Category, cat, StringComparison.OrdinalIgnoreCase));
            }

            if (FiltroStatusCombo?.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() is string statusStr && !statusStr.StartsWith("Todas", StringComparison.OrdinalIgnoreCase))
            {
                var targetStatus = statusStr switch
                {
                    "Disponível" => ToolStatus.AVAILABLE,
                    "Em Uso" => ToolStatus.IN_USE,
                    "Em Manutenção" => ToolStatus.MAINTENANCE,
                    "Avariada" => ToolStatus.DAMAGED,
                    _ => (ToolStatus?)null
                };

                if (targetStatus.HasValue)
                {
                    filtrados = filtrados.Where(t => t.Status == targetStatus.Value);
                }
            }

            FerramentasDataGrid.ItemsSource = filtrados.ToList();
        }

        private async void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            await CarregarFerramentasAsync();
        }

        private void Ver360Button_Click(object sender, RoutedEventArgs e)
        {
            AbrirTool360();
        }

        private void FerramentasDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AbrirTool360();
        }

        private async void AbrirTool360()
        {
            if (FerramentasDataGrid.SelectedItem is not Tool selecionada)
            {
                MessageBox.Show("Selecione uma ferramenta na lista para abrir a visão 360°.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new Tool360Window(selecionada, _toolService)
            {
                Owner = Window.GetWindow(this)
            };

            win.ShowDialog();
            await CarregarFerramentasAsync();
        }

        private async void RegistrarRetiradaButton_Click(object sender, RoutedEventArgs e)
        {
            if (FerramentasDataGrid.SelectedItem is not Tool selecionada)
            {
                MessageBox.Show("Selecione uma ferramenta disponível para retirada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selecionada.Status != ToolStatus.AVAILABLE)
            {
                MessageBox.Show($"A ferramenta '{selecionada.Code} - {selecionada.Name}' não está disponível para retirada (Situação: {selecionada.StatusDisplay}).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new ToolCheckoutDialog(selecionada, _toolService)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                await CarregarFerramentasAsync();
            }
        }

        private async void RegistrarDevolucaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (FerramentasDataGrid.SelectedItem is not Tool selecionada)
            {
                MessageBox.Show("Selecione uma ferramenta que está em uso para registrar devolução.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selecionada.Status != ToolStatus.IN_USE)
            {
                MessageBox.Show($"A ferramenta '{selecionada.Code} - {selecionada.Name}' não está com retirada em aberto (Situação: {selecionada.StatusDisplay}).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show(
                $"Confirmar a devolução da ferramenta '{selecionada.Code} - {selecionada.Name}'?\n\nCondição: Em perfeito estado (OK).",
                "Confirmar Devolução",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var userId = App.Session?.UserId ?? 1;
                    var ok = await _toolService.DevolverFerramentaAsync(selecionada.ToolId, userId, ToolCondition.OK, "Devolução registrada via painel de ferramentas.");
                    if (ok)
                    {
                        MessageBox.Show("Devolução registrada com sucesso!", "Devolução Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
                        await CarregarFerramentasAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro na Devolução", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void NovaFerramentaButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NovaFerramentaDialog(_toolService)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                await CarregarFerramentasAsync();
            }
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            AplicarFiltros();
        }

        private void FiltroCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            AplicarFiltros();
        }

        private void LimparFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            BuscaTextBox.Text = string.Empty;
            FiltroCategoriaCombo.SelectedIndex = 0;
            FiltroStatusCombo.SelectedIndex = 0;
            AplicarFiltros();
        }
    }
}
