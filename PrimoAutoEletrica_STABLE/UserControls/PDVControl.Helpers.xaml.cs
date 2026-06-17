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
        // Cancelamento registrado, permissoes, atualizacoes visuais e helpers.

        private void CancelarUltimaVendaConcluidaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("PDV_CANCELAR_VENDA_REGISTRADA", "Voce nao possui permissao para cancelar uma venda ja concluida."))
            {
                return;
            }

            var venda = SelecionarVendaOperacional(
                titulo: "Cancelar venda concluida",
                subtitulo: "Escolha uma venda recente para executar o estorno completo do PDV.",
                textoConfirmacao: "Cancelar venda",
                incluirCanceladas: true,
                filtro: item => !string.Equals(item.Status, "Cancelada", StringComparison.OrdinalIgnoreCase));

            if (venda == null)
            {
                return;
            }

            var motivoDialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Cancelar venda concluida",
                Header = "Cancelamento completo da venda",
                Subheader = $"Venda {venda.Id.ToString()[..8]} | Total {venda.Total:C}\nInforme o motivo do cancelamento para estornar estoque, caixa e financeiro.",
                ObservacoesLabel = "Motivo do cancelamento",
                ConfirmButtonText = "Avancar para cancelamento",
                SolicitarValor = false,
                ObservacoesObrigatorias = true
            });

            ConfigurarOwner(motivoDialog);

            if (motivoDialog.ShowDialog() != true)
            {
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Cancelar venda concluida",
                    Header = "Estorno completo de venda",
                    Summary = $"Voce esta prestes a cancelar a venda {venda.Id.ToString()[..8]}.",
                    Details = $"Cliente: {venda.Cliente?.Nome ?? "Consumidor final"}\nTotal: {venda.Total:C}\nForma de pagamento: {venda.FormaPagamento}\nMotivo: {motivoDialog.ObservacoesInformadas}",
                    Impact = "O sistema vai reverter estoque, lancamento financeiro e movimento de caixa desta venda.",
                    Keyword = "CANCELAR",
                    ConfirmButtonText = "Cancelar venda concluida"
                });

            if (!confirmado)
            {
                return;
            }

            try
            {
                _vendaService.CancelarVenda(venda.Id, motivoDialog.ObservacoesInformadas);

                if (_viewModel.UltimaVendaFinalizadaId == venda.Id)
                {
                    _viewModel.UltimaVendaFinalizadaId = null;
                }

                CarregarDadosIniciais();
                AtualizarEstadoCaixa();

                ExibirMensagem(
                    "Venda cancelada com estorno completo do fluxo operacional.",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            ExibirMensagem(
                mensagem,
                "Acesso negado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }

        private void LimparCarrinho()
        {
            _viewModel.Carrinho.Clear();
            _viewModel.ClienteSelecionado = null;
            _viewModel.DescontoGeral = 0;
            _viewModel.FormaPagamentoSelecionada = "Dinheiro";
            _viewModel.PagamentoMistoResumo = string.Empty;

            CarrinhoListView.ItemsSource = null;
            CarrinhoListView.ItemsSource = _viewModel.Carrinho;

            DescontoTextBox.Clear();
            BuscaClienteTextBox.Clear();

            _viewModel.Clientes.Clear();

            foreach (var cliente in _todosClientes.Take(5))
            {
                _viewModel.Clientes.Add(cliente);
            }

            _viewModel.AtualizarTotal();
            AtualizarFormaPagamentoSelecionada();
        }

        private void AtualizarGridCarrinho()
        {
            CarrinhoListView.ItemsSource = null;
            CarrinhoListView.ItemsSource = _viewModel.Carrinho;
            _viewModel.AtualizarTotal();
        }

        private void AtualizarResumoVendasSuspensas()
        {
            if (VendasSuspensas.Count == 0)
            {
                _viewModel.VendasSuspensasResumo = "Nenhuma venda suspensa.";
                return;
            }

            var totalSuspenso = VendasSuspensas.Sum(venda => venda.Total);

            var ultima = VendasSuspensas
                .OrderByDescending(venda => venda.DataSuspensao)
                .First();

            _viewModel.VendasSuspensasResumo =
                $"{VendasSuspensas.Count} venda(s) suspensa(s) somando {totalSuspenso:C}. Ultima: {ultima.DataSuspensao:HH:mm} | {ultima.Total:C}.";
        }

        private void AtualizarFormaPagamentoSelecionada()
        {
            AtualizarEstadoBotaoFormaPagamento(DinheiroButton, "Dinheiro");
            AtualizarEstadoBotaoFormaPagamento(PixButton, "PIX");
            AtualizarEstadoBotaoFormaPagamento(DebitoButton, "Debito");
            AtualizarEstadoBotaoFormaPagamento(CreditoButton, "Credito");
            AtualizarEstadoBotaoFormaPagamento(MistoButton, "Misto");
        }

        private void AtualizarEstadoBotaoFormaPagamento(Button? button, string formaPagamento)
        {
            if (button == null)
            {
                return;
            }

            var selecionado = string.Equals(
                _viewModel.FormaPagamentoSelecionada,
                formaPagamento,
                StringComparison.OrdinalIgnoreCase);

            button.Background = selecionado
                ? (Brush)FindResource("PrimaryBrush")
                : (Brush)FindResource("SurfaceAltBrush");

            button.BorderBrush = selecionado
                ? (Brush)FindResource("PrimaryBrush")
                : (Brush)FindResource("BorderBrush");

            button.Foreground = selecionado
                ? (Brush)FindResource("AccentButtonTextBrush")
                : (Brush)FindResource("PrimaryTextBrush");

            button.FontWeight = selecionado
                ? FontWeights.Bold
                : FontWeights.SemiBold;
        }

        private void AtualizarEstadoCaixa()
        {
            try
            {
                var sessao = _caixaService.ObterSessaoAbertaAtual();
                _viewModel.AtualizarCaixa(sessao);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogWarning(
                    $"Falha ao atualizar estado do caixa no PDV: {ex.Message}");
            }
        }

        private CaixaSessaoOperacional? GarantirCaixaAbertoParaVenda()
        {
            var sessao = _caixaService.ObterSessaoAbertaAtual();

            if (sessao != null)
            {
                return sessao;
            }

            ExibirMensagem(
                "Abra o caixa antes de finalizar uma venda. O PDV agora bloqueia pagamentos sem sessao operacional ativa.",
                "Caixa fechado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            AtualizarEstadoCaixa();

            return null;
        }

        private bool GarantirSessaoCaixaAberta()
        {
            if (_caixaService.ObterSessaoAbertaAtual() != null)
            {
                return true;
            }

            ExibirMensagem(
                "Abra o caixa antes de executar esta operacao.",
                "Caixa",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            AtualizarEstadoCaixa();

            return false;
        }

        private void AtualizarProdutosVisiveis(IEnumerable<Produto> produtos)
        {
            _viewModel.Produtos.Clear();

            foreach (var produto in produtos)
            {
                _viewModel.Produtos.Add(produto);
            }
        }

        private int ObterDisponibilidadeOperacional(Produto produto)
        {
            return Math.Max(0, produto.QuantidadeDisponivel);
        }

        private Venda? SelecionarVendaOperacional(
            string titulo,
            string subtitulo,
            string textoConfirmacao,
            bool incluirCanceladas,
            Func<Venda, bool>? filtro = null)
        {
            var sessaoAberta = _caixaService.ObterSessaoAbertaAtual();

            var vendas = _vendaService.ObterHistoricoOperacional(
                limite: 60,
                caixaSessaoId: sessaoAberta?.Id,
                inicio: DateTime.Today.AddDays(-30),
                incluirCanceladas: incluirCanceladas);

            if (vendas.Count == 0 && sessaoAberta?.Id != null)
            {
                vendas = _vendaService.ObterHistoricoOperacional(
                    limite: 60,
                    caixaSessaoId: null,
                    inicio: DateTime.Today.AddDays(-30),
                    incluirCanceladas: incluirCanceladas);
            }

            if (filtro != null)
            {
                vendas = vendas
                    .Where(filtro)
                    .ToList();
            }

            if (vendas.Count == 0)
            {
                ExibirMensagem(
                    "Nenhuma venda recente foi encontrada para esta operacao.",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return null;
            }

            var dialog = new SelecionarVendaWindow(
                vendas,
                titulo,
                subtitulo,
                textoConfirmacao);

            ConfigurarOwner(dialog);

            return dialog.ShowDialog() == true
                ? dialog.VendaSelecionada
                : null;
        }

        private static void ConfigurarOwner(Window dialog)
        {
            WindowOwnerHelper.ConfigureOwner(dialog);
        }

        private static ItemVenda CloneItemVenda(ItemVenda item)
        {
            return new ItemVenda
            {
                Produto = item.Produto,
                ProdutoId = item.ProdutoId,
                Tipo = item.Tipo,
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                CustoUnitario = item.CustoUnitario,
                Desconto = item.Desconto
            };
        }

        private sealed record VendaSuspensaPdv(
            Guid Id,
            DateTime DataSuspensao,
            Cliente? Cliente,
            List<ItemVenda> Itens,
            decimal DescontoGeral,
            string FormaPagamento,
            string PagamentoMistoResumo,
            string Operador)
        {
            public decimal Total => Math.Max(0m, Itens.Sum(item => item.Subtotal) - DescontoGeral);
        }

        private MessageBoxResult ExibirMensagem(
            string mensagem,
            string titulo,
            MessageBoxButton botoes,
            MessageBoxImage icone)
        {
            if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
            {
                var texto = $"{titulo}: {mensagem}";

                if (icone == MessageBoxImage.Error)
                {
                    global::PrimoAutoEletrica.App.Logger.LogError(texto, null, "PDV");
                }
                else if (icone == MessageBoxImage.Warning)
                {
                    global::PrimoAutoEletrica.App.Logger.LogWarning(texto, "PDV");
                }
                else
                {
                    global::PrimoAutoEletrica.App.Logger.LogInfo(texto, "PDV");
                }

                return botoes switch
                {
                    MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel => MessageBoxResult.Yes,
                    MessageBoxButton.OKCancel => MessageBoxResult.OK,
                    _ => MessageBoxResult.OK
                };
            }

            var owner = Window.GetWindow(this);

            return owner != null
                ? MessageBox.Show(owner, mensagem, titulo, botoes, icone)
                : MessageBox.Show(mensagem, titulo, botoes, icone);
        }
    }
}
