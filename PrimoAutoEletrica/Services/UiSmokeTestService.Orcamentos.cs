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
        // Checks de orcamentos, conversoes, PDF, WhatsApp e alertas.

        private void RunOrcamentosConversoesPdfWhatsAppAlertasChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Orcamentos:ConversoesPdfWhatsAppAlertas", () =>
            {
                GarantirBancoIsoladoDoSmoke("conversoes e integracoes de orcamentos");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var service = new OrcamentoDatabaseService();
                var orcamento = service.ObterOrcamentoPorId(fixture.Orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento sintetico nao encontrado para validacao operacional.");

                if (!OrcamentosControl.TryBuildWhatsAppShareUrl(orcamento, out var whatsappUrl, out var erroWhatsApp) ||
                    !whatsappUrl.StartsWith("https://wa.me/", StringComparison.OrdinalIgnoreCase) ||
                    !whatsappUrl.Contains(Uri.EscapeDataString(orcamento.Numero), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"URL de WhatsApp do orcamento nao foi montada corretamente: {erroWhatsApp}");
                }

                var hostWindow = CreateHostWindow(new OrcamentosControl(), nameof(OrcamentosControl));
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not OrcamentosControl control)
                    {
                        throw new InvalidOperationException("Host de OrcamentosControl nao conseguiu carregar o orcamento sintetico.");
                    }

                    var carteira = FindElementByName<ListBox>(control, "CarteiraOrcamentosListBox")
                        ?? throw new InvalidOperationException("CarteiraOrcamentosListBox nao foi localizada para validar PDF e WhatsApp.");
                    WaitForCondition(
                        () => SelecionarOrcamentoNaLista(carteira, orcamento.Id),
                        TimeSpan.FromSeconds(5),
                        "O orcamento sintetico nao apareceu na carteira operacional.");

                    var diretorioPdf = Path.Combine(App.RuntimeLogDirectory, "orcamentos-smoke");
                    Directory.CreateDirectory(diretorioPdf);
                    var inicio = DateTime.Now.AddSeconds(-1);

                    ClickButton(control, "ExportarPdfOrcamentoButton");
                    ClickButton(control, "WhatsAppOrcamentoButton");

                    WaitForCondition(
                        () => new DirectoryInfo(diretorioPdf)
                            .GetFiles("Orcamento_*.pdf")
                            .Any(file => file.LastWriteTime >= inicio && file.Length > 0),
                        TimeSpan.FromSeconds(5),
                        "O botao Exportar PDF nao gerou o arquivo esperado.");
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }

                var pastaPdf = Path.Combine(
                    App.RuntimeAppDataPath,
                    "AutomatedTests",
                    "Orcamentos");
                Directory.CreateDirectory(pastaPdf);
                var caminhoPdf = Path.Combine(pastaPdf, $"orcamento-{orcamento.Numero}-{Guid.NewGuid():N}.pdf");
                new OrcamentoPdfService().GerarPdfOrcamento(orcamento, caminhoPdf);
                if (!File.Exists(caminhoPdf) || new FileInfo(caminhoPdf).Length == 0)
                {
                    throw new InvalidOperationException("PDF sintetico do orcamento nao foi gerado corretamente.");
                }

                orcamento.Status = "Rascunho";
                orcamento.DataValidade = DateTime.Today.AddDays(3);
                service.AtualizarOrcamento(orcamento);

                var alertaVm = new OrcamentosViewModel();
                var orcamentoComAlerta = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento com alerta nao foi recarregado.");
                alertaVm.SelecionarOrcamento(orcamentoComAlerta);
                if (!alertaVm.Alertas.Any(alerta => alerta.Contains("proximo do vencimento", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Alerta de vencimento do orcamento nao foi emitido no ViewModel.");
                }

                var conversaoOsVm = new OrcamentosViewModel();
                conversaoOsVm.AprovarOrcamento(orcamentoComAlerta);
                var aprovado = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento aprovado nao foi recarregado.");
                var ordem = conversaoOsVm.ConverterEmOrdemServico(aprovado);
                var convertidoEmOs = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento convertido em OS nao foi recarregado.");

                if (!string.Equals(convertidoEmOs.Status, "Convertido em OS", StringComparison.OrdinalIgnoreCase) ||
                    !convertidoEmOs.OrdemServicoId.HasValue ||
                    !convertidoEmOs.DataConversaoOrdemServico.HasValue)
                {
                    throw new InvalidOperationException("Conversao do orcamento em OS nao persistiu status, data e vinculo.");
                }

                var ordemServicoId = convertidoEmOs.OrdemServicoId.GetValueOrDefault();
                var ordemPersistida = App.Repositories.OrdensServico.ObterPorId(ordemServicoId)
                    ?? throw new InvalidOperationException("OS gerada pelo orcamento nao foi localizada.");
                if (ordemPersistida.OrcamentoId != convertidoEmOs.Id ||
                    ordemPersistida.ClienteId != convertidoEmOs.ClienteId ||
                    ordemPersistida.VeiculoId != convertidoEmOs.VeiculoId ||
                    ordemPersistida.Desconto != convertidoEmOs.Desconto ||
                    ordemPersistida.Itens.Count != convertidoEmOs.Itens.Count ||
                    !ordemPersistida.Itens.Any(item => string.Equals(item.Descricao, convertidoEmOs.Itens.First().ProdutoNome, StringComparison.OrdinalIgnoreCase)) ||
                    !ordemPersistida.Diagnostico.Contains(convertidoEmOs.Diagnostico, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(ordemPersistida.Origem, "Orcamento", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("OS gerada pelo orcamento nao manteve cliente, veiculo, diagnostico, valores, origem ou itens esperados.");
                }

                var ordemReutilizada = conversaoOsVm.ConverterEmOrdemServico(convertidoEmOs);
                if (ordemReutilizada.Id != ordem.Id)
                {
                    throw new InvalidOperationException("Segunda conversao do mesmo orcamento criou uma OS duplicada.");
                }

                var orcamentoVenda = CreatePersistedOrcamento(fixture.Cliente, fixture.Produto);
                orcamentoVenda.CondicoesPagamento = "PIX";
                service.AtualizarOrcamento(orcamentoVenda);

                var conversaoVendaVm = new OrcamentosViewModel();
                var vendaAntes = service.ObterOrcamentoPorId(orcamentoVenda.Id)
                    ?? throw new InvalidOperationException("Orcamento para venda nao foi recarregado.");
                conversaoVendaVm.ConverterParaPDV(vendaAntes);

                var convertidoEmVenda = service.ObterOrcamentoPorId(orcamentoVenda.Id)
                    ?? throw new InvalidOperationException("Orcamento convertido em venda nao foi recarregado.");
                if (!string.Equals(convertidoEmVenda.Status, "Convertido em Venda", StringComparison.OrdinalIgnoreCase) ||
                    !convertidoEmVenda.DataConversaoVenda.HasValue)
                {
                    throw new InvalidOperationException("Conversao do orcamento em venda/PDV nao persistiu status e data.");
                }

                var contasReceber = new FinanceiroDatabaseService().ObterContasReceber();
                var contaIntegrada = contasReceber.Any(conta =>
                    string.Equals((string)conta.Origem, "OrcamentoContaReceber", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string)conta.ReferenciaExterna, convertidoEmVenda.Id.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    (decimal)conta.Valor > 0);
                if (!contaIntegrada)
                {
                    throw new InvalidOperationException("Conversao do orcamento em venda nao integrou conta a receber ao financeiro.");
                }
            });
        }

        private static bool SelecionarOrcamentoNaLista(ListBox listBox, Guid orcamentoId)
        {
            var item = listBox.Items
                .OfType<Orcamento>()
                .FirstOrDefault(orcamento => orcamento.Id == orcamentoId);
            if (item == null)
            {
                return false;
            }

            listBox.SelectedItem = item;
            listBox.ScrollIntoView(item);
            WaitForUiIdle();
            return true;
        }

    }
}
