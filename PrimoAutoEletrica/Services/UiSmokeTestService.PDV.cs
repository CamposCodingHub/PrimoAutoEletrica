using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks operacionais dedicados ao PDV.

        private void RunPdvOperationalInteractionChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "PDV:SelecaoClienteConsumidorFinalPelaTela", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var hostWindow = new Window
                {
                    Content = new PDVControl(),
                    Title = "Smoke PDV Cliente Host"
                };

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    var control = hostWindow.Content as PDVControl
                        ?? throw new InvalidOperationException("Host do PDV nao conseguiu carregar o controle de selecao de cliente.");

                    WaitForCondition(
                        () => control.ViewModel.Clientes.Count > 0,
                        TimeSpan.FromSeconds(10),
                        "O PDV nao carregou clientes sinteticos para validar a selecao.");

                    control.AbrirSelecaoClienteParaAutomacao(selecionarClienteCadastrado: true, clientePreferencialId: fixture.Cliente.Id);
                    WaitForCondition(
                        () => control.ViewModel.ClienteSelecionado?.Id == fixture.Cliente.Id,
                        TimeSpan.FromSeconds(5),
                        "A janela de selecao do PDV nao vinculou o cliente esperado.");
                    AssertWindowStillOperational(hostWindow, "selecao de cliente cadastrado no PDV");

                    control.AbrirSelecaoClienteParaAutomacao(selecionarClienteCadastrado: false);
                    WaitForCondition(
                        () => control.ViewModel.ClienteSelecionado == null,
                        TimeSpan.FromSeconds(5),
                        "A janela de selecao do PDV nao retornou para consumidor final.");
                    AssertWindowStillOperational(hostWindow, "selecao de consumidor final no PDV");
                }
                finally
                {
                    CloseTransientWindows(hostWindow);
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });

            RunCheck(result, "PDV:InteracaoCompletaTela", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var caixaService = new CaixaService(App.Database);
                var vendaService = new VendaService(App.Database);
                FecharCaixaAbertoAntesDoBloqueioPdv(caixaService);

                var hostWindow = new Window
                {
                    Content = new PDVControl(),
                    Title = "Smoke PDV Host"
                };
                AutomatedDialogSupervisor? supervisor = null;

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    var control = hostWindow.Content as PDVControl
                        ?? throw new InvalidOperationException("Host do PDV nao conseguiu carregar o controle principal.");
                    supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                    supervisor.Start();

                    WaitForCondition(
                        () => control.ViewModel.Produtos.Count > 0 && control.ViewModel.Clientes.Count > 0,
                        TimeSpan.FromSeconds(10),
                        "O PDV nao carregou produtos e clientes sinteticos para a simulacao.");

                    AdicionarProdutoAoCarrinhoParaAutomacao(control, fixture.Produto);
                    WaitForCondition(
                        () => control.ViewModel.Carrinho.Count > 0,
                        TimeSpan.FromSeconds(5),
                        "O PDV nao conseguiu montar um carrinho minimo antes de validar o bloqueio por caixa fechado.");

                    ClickButton(control, "PagamentoButton");
                    WaitForCondition(
                        () => !control.ViewModel.CaixaAberto && control.ViewModel.Carrinho.Count > 0,
                        TimeSpan.FromSeconds(5),
                        "O PDV nao preservou o carrinho ao bloquear o pagamento sem caixa aberto.");
                    RestoreWindowForInteraction(hostWindow);

                    ClickButton(control, "SuspenderVendaButton");
                    RestoreWindowForInteraction(hostWindow);
                    WaitForCondition(
                        () => control.ViewModel.Carrinho.Count == 0 &&
                              !control.ViewModel.VendasSuspensasResumo.StartsWith("Nenhuma", StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "O PDV nao suspendeu a venda atual corretamente.");
                    AssertWindowStillOperational(hostWindow, "suspensao de venda");

                    ClickButton(control, "RetomarVendaSuspensaButton");
                    RestoreWindowForInteraction(hostWindow);
                    WaitForCondition(
                        () => control.ViewModel.Carrinho.Count > 0 &&
                              control.ViewModel.VendasSuspensasResumo.StartsWith("Nenhuma", StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "O PDV nao retomou a venda suspensa corretamente.");
                    AssertWindowStillOperational(hostWindow, "retomada de venda suspensa");

                    ClickButton(control, "Abrir caixa");
                    WaitForCondition(
                        () => control.ViewModel.CaixaAberto,
                        TimeSpan.FromSeconds(10),
                        "O PDV nao abriu a sessao de caixa durante a simulacao.");
                    AssertWindowStillOperational(hostWindow, "abertura de caixa");

                    ClickButton(control, "Suprimento");
                    WaitForCondition(
                        () => control.ViewModel.SaldoCaixaAtual > 0,
                        TimeSpan.FromSeconds(10),
                        "O suprimento de caixa nao refletiu no saldo operacional.");
                    AssertWindowStillOperational(hostWindow, "suprimento");

                    ClickButton(control, "Sangria");
                    WaitForCondition(
                        () => control.ViewModel.CaixaAberto,
                        TimeSpan.FromSeconds(10),
                        "A sessao de caixa ficou inconsistente apos a sangria.");
                    AssertWindowStillOperational(hostWindow, "sangria");

                    control.ViewModel.ClienteSelecionado = fixture.Cliente;
                    SetTextBoxValue(control, "DescontoTextBox", "1,00");
                    ClickButton(control, "AplicarDescontoButton");
                    WaitForCondition(
                        () => control.ViewModel.Total > 0 && control.ViewModel.Carrinho.Count > 0,
                        TimeSpan.FromSeconds(5),
                        "O PDV nao montou o carrinho antes do pagamento.");

                    ClickButton(control, "MistoButton");
                    WaitForCondition(
                        () => string.Equals(control.ViewModel.FormaPagamentoSelecionada, "Misto", StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "O PDV nao selecionou pagamento misto antes da finalizacao.");

                    var sessaoAntesPagamento = caixaService.ObterSessaoAbertaAtual();
                    var estoqueAntesPagamento = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)?.QuantidadeEstoque
                        ?? throw new InvalidOperationException("Produto sintetico nao foi localizado antes do pagamento.");
                    var historicoAntesPagamento = vendaService.ObterHistoricoOperacional(
                        limite: 20,
                        caixaSessaoId: sessaoAntesPagamento?.Id,
                        inicio: DateTime.Now.AddMinutes(-10),
                        incluirCanceladas: true).Count;

                    ClickButton(control, "PagamentoButton");
                    WaitForCondition(
                        () =>
                        {
                            if (control.ViewModel.Carrinho.Count == 0 && control.ViewModel.UltimaVendaFinalizadaId.HasValue)
                            {
                                return true;
                            }

                            var historicoAtual = vendaService.ObterHistoricoOperacional(
                                limite: 20,
                                caixaSessaoId: sessaoAntesPagamento?.Id,
                                inicio: DateTime.Now.AddMinutes(-10),
                                incluirCanceladas: true).Count;

                            return historicoAtual > historicoAntesPagamento;
                        },
                        TimeSpan.FromSeconds(10),
                        $"O pagamento do PDV nao concluiu a venda esperada. " +
                        $"Carrinho={control.ViewModel.Carrinho.Count}; " +
                        $"UltimaVenda={control.ViewModel.UltimaVendaFinalizadaId?.ToString() ?? "null"}; " +
                        $"Subtotal={control.ViewModel.Subtotal:F2}; Total={control.ViewModel.Total:F2}; " +
                        $"CaixaAberto={control.ViewModel.CaixaAberto}; SaldoCaixa={control.ViewModel.SaldoCaixaAtual:F2}; " +
                        $"SessaoAntesPagamento={sessaoAntesPagamento?.Id.ToString() ?? "null"}; " +
                        $"HistoricoAntes={historicoAntesPagamento}; " +
                        $"HistoricoDepois={vendaService.ObterHistoricoOperacional(limite: 20, caixaSessaoId: sessaoAntesPagamento?.Id, inicio: DateTime.Now.AddMinutes(-10), incluirCanceladas: true).Count}.");
                    AssertWindowStillOperational(hostWindow, "finalizacao da venda");

                    var vendaFinalizadaId = control.ViewModel.UltimaVendaFinalizadaId
                        ?? vendaService.ObterHistoricoOperacional(
                                limite: 1,
                                caixaSessaoId: sessaoAntesPagamento?.Id,
                                inicio: DateTime.Now.AddMinutes(-10),
                                incluirCanceladas: true)
                            .FirstOrDefault()?.Id
                        ?? throw new InvalidOperationException("Venda finalizada pelo PDV nao foi localizada para validar estoque/financeiro.");

                    var estoqueAposVenda = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)?.QuantidadeEstoque
                        ?? throw new InvalidOperationException("Produto sintetico nao foi localizado apos a venda.");
                    if (estoqueAposVenda >= estoqueAntesPagamento)
                    {
                        throw new InvalidOperationException("Venda concluida nao baixou estoque do produto vendido.");
                    }

                    var financeiro = new FinanceiroDatabaseService();
                    var movimentacoesAposVenda = financeiro.ObterMovimentacoes(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), limit: 200);
                    if (!movimentacoesAposVenda.Any(movimentacao =>
                            string.Equals((string)movimentacao.Origem, "PDVMovimentacao", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals((string)movimentacao.ReferenciaExterna, vendaFinalizadaId.ToString(), StringComparison.OrdinalIgnoreCase) &&
                            (decimal)movimentacao.Valor > 0))
                    {
                        throw new InvalidOperationException("Venda concluida nao gerou lancamento financeiro de entrada do PDV.");
                    }

                    ClickButton(control, "ReimprimirUltimaVendaButton");
                    AssertWindowStillOperational(hostWindow, "reimpressao");

                    ClickButton(control, "CancelarUltimaVendaConcluidaButton");
                    AssertWindowStillOperational(hostWindow, "cancelamento de venda concluida");

                    var vendaCancelada = vendaService.ObterVendaPorId(vendaFinalizadaId)
                        ?? throw new InvalidOperationException("Venda cancelada pelo PDV nao foi recarregada.");
                    if (!string.Equals(vendaCancelada.Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Cancelamento do PDV nao marcou a venda como cancelada.");
                    }

                    var estoqueAposCancelamento = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)?.QuantidadeEstoque
                        ?? throw new InvalidOperationException("Produto sintetico nao foi localizado apos cancelamento.");
                    if (estoqueAposCancelamento != estoqueAntesPagamento)
                    {
                        throw new InvalidOperationException("Cancelamento do PDV nao estornou o estoque ao saldo anterior.");
                    }

                    var movimentacoesAposCancelamento = financeiro.ObterMovimentacoes(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), limit: 200);
                    if (!movimentacoesAposCancelamento.Any(movimentacao =>
                            string.Equals((string)movimentacao.Origem, "PDVCancelamento", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals((string)movimentacao.ReferenciaExterna, vendaFinalizadaId.ToString(), StringComparison.OrdinalIgnoreCase) &&
                            (decimal)movimentacao.Valor > 0))
                    {
                        throw new InvalidOperationException("Cancelamento do PDV nao gerou lancamento financeiro de estorno.");
                    }

                    ClickButton(control, "Fechar caixa");
                    WaitForCondition(
                        () => !control.ViewModel.CaixaAberto,
                        TimeSpan.FromSeconds(10),
                        "O fechamento do caixa nao foi refletido no PDV.");
                    AssertWindowStillOperational(hostWindow, "fechamento de caixa");
                }
                finally
                {
                    supervisor?.Dispose();
                    CloseTransientWindows(hostWindow);
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });
        }

        private static void FecharCaixaAbertoAntesDoBloqueioPdv(CaixaService caixaService)
        {
            var sessaoAberta = caixaService.ObterSessaoAbertaAtual();
            if (sessaoAberta == null)
            {
                return;
            }

            caixaService.FecharCaixa(
                sessaoAberta.ValorEsperado,
                "Fechamento preparatorio do smoke test para validar bloqueio de pagamento sem caixa aberto.");
        }

    }
}
