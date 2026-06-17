using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Views
{
    public partial class SelecionarOrcamentoWindow : Window
    {
        public Orcamento? OrcamentoSelecionado { get; private set; }
        private readonly List<Orcamento> _orcamentos;
        private readonly ICollectionView _orcamentosView;

        public SelecionarOrcamentoWindow(List<Orcamento> orcamentos)
        {
            InitializeComponent();
            _orcamentos = orcamentos
                .OrderByDescending(orcamento => orcamento.DataCriacao)
                .ThenByDescending(orcamento => orcamento.Numero)
                .ToList();

            _orcamentosView = CollectionViewSource.GetDefaultView(_orcamentos);
            _orcamentosView.Filter = FiltrarOrcamento;
            OrcamentosDataGrid.ItemsSource = _orcamentosView;

            CarregarStatus();
            AtualizarResumo();
        }

        private void OrcamentosDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelecionarAtual();
        }

        private void Selecionar_Click(object sender, RoutedEventArgs e)
        {
            SelecionarAtual();
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _orcamentosView.Refresh();
            AtualizarResumo();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            _orcamentosView.Refresh();
            AtualizarResumo();
        }

        private void LimparFiltros_Click(object sender, RoutedEventArgs e)
        {
            BuscaTextBox.Text = string.Empty;
            StatusFilterComboBox.SelectedIndex = 0;
            _orcamentosView.Refresh();
            AtualizarResumo();
        }

        private void OrcamentosDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarResumo();
        }

        private void SelecionarAtual()
        {
            if (OrcamentosDataGrid.SelectedItem is Orcamento orcamento)
            {
                OrcamentoSelecionado = orcamento;
                DialogResult = true;
                Close();
                return;
            }

            if (App.IsAutomatedTestMode)
            {
                var primeiroOrcamento = _orcamentosView.Cast<Orcamento>().FirstOrDefault();
                if (primeiroOrcamento != null)
                {
                    OrcamentoSelecionado = primeiroOrcamento;
                    DialogResult = true;
                    Close();
                    return;
                }

                DialogResult = false;
                Close();
                return;
            }

            MessageBox.Show(
                "Selecione um orcamento para abrir.",
                "Selecao",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void CarregarStatus()
        {
            var opcoes = new List<string> { "Todos os status" };
            opcoes.AddRange(_orcamentos
                .Select(orcamento => UiTextSanitizer.SanitizeText(orcamento.Status))
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .OrderBy(status => status));

            StatusFilterComboBox.ItemsSource = opcoes;
            StatusFilterComboBox.SelectedIndex = 0;
        }

        private bool FiltrarOrcamento(object obj)
        {
            if (obj is not Orcamento orcamento)
            {
                return false;
            }

            var busca = BuscaTextBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var encontrou = orcamento.Numero.Contains(busca, System.StringComparison.OrdinalIgnoreCase) ||
                                (orcamento.Cliente?.Nome?.Contains(busca, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                                orcamento.Status.Contains(busca, System.StringComparison.OrdinalIgnoreCase);

                if (!encontrou)
                {
                    return false;
                }
            }

            var statusSelecionado = StatusFilterComboBox.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(statusSelecionado) &&
                !string.Equals(statusSelecionado, "Todos os status", System.StringComparison.OrdinalIgnoreCase) &&
                !UiTextSanitizer.EqualsNormalized(orcamento.Status, statusSelecionado))
            {
                return false;
            }

            return true;
        }

        private void AtualizarResumo()
        {
            var filtrados = _orcamentosView.Cast<Orcamento>().ToList();
            var valorTotal = filtrados.Sum(orcamento => orcamento.Total);
            ResumoTextBlock.Text = $"{filtrados.Count} orcamento(s) filtrado(s) somando {valorTotal:C}.";

            if (OrcamentosDataGrid.SelectedItem is Orcamento selecionado)
            {
                SelecionadoTextBlock.Text = $"Selecionado: {selecionado.Numero}";
            }
            else
            {
                SelecionadoTextBlock.Text = "Nenhum selecionado";
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
