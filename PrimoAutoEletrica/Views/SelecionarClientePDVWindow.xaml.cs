using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.Views
{
    public partial class SelecionarClientePDVWindow : Window
    {
        private readonly List<Cliente> _todosClientes;
        private readonly bool _selecionarPrimeiroClienteEmAutomacao;
        private List<Cliente> _clientesFiltrados = new();

        public Cliente? ClienteSelecionado { get; private set; }

        public bool UsarConsumidorFinal { get; private set; }

        public SelecionarClientePDVWindow(
            IEnumerable<Cliente> clientes,
            bool selecionarPrimeiroClienteEmAutomacao = false)
        {
            InitializeComponent();

            _todosClientes = clientes?
                .Where(cliente => cliente != null)
                .OrderBy(cliente => cliente.Nome)
                .ToList() ?? new List<Cliente>();
            _selecionarPrimeiroClienteEmAutomacao = selecionarPrimeiroClienteEmAutomacao;

            Loaded += SelecionarClientePDVWindow_Loaded;
        }

        private void SelecionarClientePDVWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AplicarFiltros();
                    if (_selecionarPrimeiroClienteEmAutomacao && _clientesFiltrados.Count > 0)
                    {
                        ClientesDataGrid.SelectedItem = _clientesFiltrados[0];
                        SelecionarClienteAtual();
                        return;
                    }

                    ClienteSelecionado = null;
                    UsarConsumidorFinal = true;
                    WindowInteractionHelper.CloseWithDialogResult(this, true, "PDV");
                }));
                return;
            }

            AplicarFiltros();

            BuscaTextBox.Focus();
            BuscaTextBox.SelectAll();
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void BuscaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelecionarClienteAtual();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                WindowInteractionHelper.CloseWithDialogResult(this, false, "PDV");
                e.Handled = true;
            }
        }

        private void ClientesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelecionarClienteAtual();
        }

        private void SelecionarLinhaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Cliente cliente })
            {
                ClientesDataGrid.SelectedItem = cliente;
            }

            SelecionarClienteAtual();
        }

        private void SelecionarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            SelecionarClienteAtual();
        }

        private void LimparBuscaButton_Click(object sender, RoutedEventArgs e)
        {
            BuscaTextBox.Clear();
            BuscaTextBox.Focus();
        }

        private void ConsumidorFinalButton_Click(object sender, RoutedEventArgs e)
        {
            ClienteSelecionado = null;
            UsarConsumidorFinal = true;
            WindowInteractionHelper.CloseWithDialogResult(this, true, "PDV");
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            WindowInteractionHelper.CloseWithDialogResult(this, false, "PDV");
        }

        private void SelecionarClienteAtual()
        {
            if (ClientesDataGrid.SelectedItem is not Cliente cliente)
            {
                WindowInteractionHelper.ShowMessage(
                    "Selecione um cliente na tabela ou use a opção Consumidor final.",
                    "Selecionar cliente",
                    MessageBoxImage.Information,
                    "PDV");

                return;
            }

            ClienteSelecionado = cliente;
            UsarConsumidorFinal = false;
            WindowInteractionHelper.CloseWithDialogResult(this, true, "PDV");
        }

        private void AplicarFiltros()
        {
            if (ClientesDataGrid == null)
            {
                return;
            }

            IEnumerable<Cliente> consulta = _todosClientes;

            var termo = BuscaTextBox?.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                var termoDocumento = CadastroValidationHelper.NormalizarDocumento(termo);
                var termoTelefone = CadastroValidationHelper.NormalizarTelefone(termo);

                consulta = consulta.Where(cliente =>
                    Contem(cliente.Nome, termo)
                    || Contem(cliente.Documento, termo)
                    || Contem(cliente.CPF, termo)
                    || Contem(cliente.Telefone, termo)
                    || Contem(cliente.WhatsApp, termo)
                    || Contem(cliente.Email, termo)
                    || (!string.IsNullOrWhiteSpace(termoDocumento) &&
                        CadastroValidationHelper.NormalizarDocumento(cliente.CPF).Contains(termoDocumento, StringComparison.Ordinal))
                    || (!string.IsNullOrWhiteSpace(termoDocumento) &&
                        CadastroValidationHelper.NormalizarDocumento(cliente.Documento).Contains(termoDocumento, StringComparison.Ordinal))
                    || (!string.IsNullOrWhiteSpace(termoTelefone) &&
                        CadastroValidationHelper.NormalizarTelefone(cliente.Telefone).Contains(termoTelefone, StringComparison.Ordinal))
                    || (!string.IsNullOrWhiteSpace(termoTelefone) &&
                        CadastroValidationHelper.NormalizarTelefone(cliente.WhatsApp).Contains(termoTelefone, StringComparison.Ordinal)));
            }

            _clientesFiltrados = consulta
                .OrderBy(cliente => cliente.Nome)
                .Take(500)
                .ToList();

            ClientesDataGrid.ItemsSource = _clientesFiltrados;
            ResumoTextBlock.Text = $"Clientes encontrados: {_clientesFiltrados.Count}";
        }

        private static bool Contem(string? origem, string termo)
        {
            return !string.IsNullOrWhiteSpace(origem)
                && origem.Contains(termo, StringComparison.OrdinalIgnoreCase);
        }
    }
}
