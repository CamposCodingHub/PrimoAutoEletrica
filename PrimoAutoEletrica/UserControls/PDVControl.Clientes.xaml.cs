using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Printing;
using System.IO;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrimoAutoEletrica.UserControls
{
    public partial class PDVControl
    {
        // Busca e selecao de clientes no PDV.

        private void BuscarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirJanelaSelecionarCliente();
        }

        private void AbrirSelecionarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirJanelaSelecionarCliente();
        }

        public void AbrirSelecaoClienteParaAutomacao(bool selecionarClienteCadastrado, Guid? clientePreferencialId = null)
        {
            if (!App.IsAutomatedTestMode)
            {
                throw new InvalidOperationException("Selecao automatizada de cliente do PDV disponivel apenas em modo de teste.");
            }

            AbrirJanelaSelecionarCliente(selecionarClienteCadastrado, clientePreferencialId);
        }

        private void AbrirJanelaSelecionarCliente(bool selecionarPrimeiroClienteEmAutomacao = false, Guid? clientePreferencialId = null)
        {
            try
            {
                if (_todosClientes.Count == 0)
                {
                    CarregarDadosIniciais();
                }

                if (_todosClientes.Count == 0)
                {
                    ExibirMensagem(
                        "Nenhum cliente encontrado no cadastro.\n\nA venda pode continuar como Consumidor final.",
                        "Selecionar cliente",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _viewModel.ClienteSelecionado = null;
                    return;
                }

                var janela = new SelecionarClientePDVWindow(_todosClientes, selecionarPrimeiroClienteEmAutomacao, clientePreferencialId);
                ConfigurarOwner(janela);

                if (janela.ShowDialog() == true)
                {
                    _viewModel.ClienteSelecionado = janela.UsarConsumidorFinal
                        ? null
                        : janela.ClienteSelecionado;

                    BuscaClienteTextBox.Clear();

                    if (_viewModel.ClienteSelecionado != null)
                    {
                        ExibirMensagem(
                            $"Cliente selecionado: {_viewModel.ClienteSelecionado.Nome}",
                            "Cliente",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao abrir seleção de clientes:\n{ex.Message}",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BuscaClienteTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RealizarBuscaCliente();
                e.Handled = true;
            }
        }

        private void RealizarBuscaCliente()
        {
            var termo = BuscaClienteTextBox.Text.Trim().ToUpperInvariant();

            _viewModel.Clientes.Clear();

            if (string.IsNullOrWhiteSpace(termo))
            {
                foreach (var cliente in _todosClientes.Take(5))
                {
                    _viewModel.Clientes.Add(cliente);
                }

                return;
            }

            var resultados = _todosClientes
                .Where(c =>
                {
                    var termoDocumento = CadastroValidationHelper.NormalizarDocumento(termo);
                    var termoTelefone = CadastroValidationHelper.NormalizarTelefone(termo);

                    return TextoContem(c.Nome, termo) ||
                           TextoContem(c.Documento, termo) ||
                           (!string.IsNullOrWhiteSpace(termoDocumento) &&
                            CadastroValidationHelper.NormalizarDocumento(c.CPF).Contains(termoDocumento, StringComparison.Ordinal)) ||
                           (!string.IsNullOrWhiteSpace(termoTelefone) &&
                            CadastroValidationHelper.NormalizarTelefone(c.Telefone).Contains(termoTelefone, StringComparison.Ordinal)) ||
                           (!string.IsNullOrWhiteSpace(termoTelefone) &&
                            CadastroValidationHelper.NormalizarTelefone(c.WhatsApp).Contains(termoTelefone, StringComparison.Ordinal)) ||
                           TextoContem(c.Email, termo);
                })
                .Take(10)
                .ToList();

            foreach (var cliente in resultados)
            {
                _viewModel.Clientes.Add(cliente);
            }
        }

        private void ClientesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.ClienteSelecionado != null)
            {
                ExibirMensagem(
                    $"Cliente selecionado: {_viewModel.ClienteSelecionado.Nome}",
                    "Cliente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

    }
}
