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
        // Cancelamento da venda atual, suspensao e retomada.

        private void CancelarVendaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Carrinho.Count == 0)
            {
                return;
            }

            if (!ValidarPermissao("PDV_CANCELAR_VENDA", "Voce nao possui permissao para cancelar a venda atual."))
            {
                return;
            }

            if (CriticalActionDialogService.ConfirmarCancelamento(
                Window.GetWindow(this),
                "venda",
                "venda atual",
                $"Itens no carrinho: {_viewModel.Carrinho.Count}\nTotal atual: {_viewModel.Total:C}\nCliente selecionado: {_viewModel.ClienteSelecionado?.Nome ?? "Nao informado"}",
                "O carrinho, o desconto aplicado e o cliente selecionado serao descartados desta sessao do PDV."))
            {
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "PDV",
                    "VendaCanceladaTela",
                    "Carrinho",
                    Guid.NewGuid().ToString(),
                    $"Itens={_viewModel.Carrinho.Count}; Total={_viewModel.Total:C}");

                LimparCarrinho();
            }
        }

        private void SuspenderVendaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Carrinho.Count == 0)
            {
                ExibirMensagem(
                    "Nao ha itens no carrinho para suspender.",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (!ValidarPermissao("PDV_SUSPENDER_VENDA", "Voce nao possui permissao para suspender vendas no PDV."))
            {
                return;
            }

            var suspensa = new VendaSuspensaPdv(
                Guid.NewGuid(),
                DateTime.Now,
                _viewModel.ClienteSelecionado,
                _viewModel.Carrinho.Select(CloneItemVenda).ToList(),
                _viewModel.DescontoGeral,
                _viewModel.FormaPagamentoSelecionada,
                _viewModel.PagamentoMistoResumo,
                App.Session.UserName);

            VendasSuspensas.Add(suspensa);

            App.Audit.RegistrarAcaoCritica(
                "PDV",
                "VendaSuspensa",
                "Carrinho",
                suspensa.Id.ToString(),
                $"Itens={suspensa.Itens.Count}; Total={suspensa.Total:C}; Cliente={suspensa.Cliente?.Nome ?? "Consumidor final"}; Operador={suspensa.Operador}");

            LimparCarrinho();
            AtualizarResumoVendasSuspensas();

            ExibirMensagem(
                $"Venda suspensa com sucesso.\nTotal: {suspensa.Total:C}",
                "PDV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RetomarVendaSuspensaButton_Click(object sender, RoutedEventArgs e)
        {
            if (VendasSuspensas.Count == 0)
            {
                ExibirMensagem(
                    "Nao ha vendas suspensas para retomar.",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (!ValidarPermissao("PDV_RETOMAR_VENDA", "Voce nao possui permissao para retomar vendas suspensas no PDV."))
            {
                return;
            }

            if (_viewModel.Carrinho.Count > 0 &&
                !CriticalActionDialogService.ConfirmarAcao(
                    Window.GetWindow(this),
                    new CriticalActionRequest
                    {
                        WindowTitle = "Retomar venda suspensa",
                        Header = "Substituir carrinho atual",
                        Summary = "Existe uma venda em andamento no PDV.",
                        Details = $"Itens atuais: {_viewModel.Carrinho.Count}\nTotal atual: {_viewModel.Total:C}",
                        Impact = "O carrinho atual sera descartado para retomar a venda suspensa selecionada.",
                        Keyword = "RETOMAR",
                        ConfirmButtonText = "Retomar suspensa"
                    }))
            {
                return;
            }

            var suspensa = VendasSuspensas
                .OrderByDescending(venda => venda.DataSuspensao)
                .First();

            VendasSuspensas.Remove(suspensa);

            _viewModel.Carrinho.Clear();

            foreach (var item in suspensa.Itens.Select(CloneItemVenda))
            {
                _viewModel.Carrinho.Add(item);
            }

            _viewModel.ClienteSelecionado = suspensa.Cliente;
            _viewModel.DescontoGeral = suspensa.DescontoGeral;
            _viewModel.FormaPagamentoSelecionada = suspensa.FormaPagamento;
            _viewModel.PagamentoMistoResumo = suspensa.PagamentoMistoResumo;

            AtualizarGridCarrinho();
            AtualizarFormaPagamentoSelecionada();
            AtualizarResumoVendasSuspensas();

            App.Audit.RegistrarAcaoCritica(
                "PDV",
                "VendaSuspensaRetomada",
                "Carrinho",
                suspensa.Id.ToString(),
                $"Itens={suspensa.Itens.Count}; Total={suspensa.Total:C}; Cliente={suspensa.Cliente?.Nome ?? "Consumidor final"}; Operador={App.Session.UserName}");

            ExibirMensagem(
                $"Venda suspensa retomada.\nTotal: {_viewModel.Total:C}",
                "PDV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

    }
}
