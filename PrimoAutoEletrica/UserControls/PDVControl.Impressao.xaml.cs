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
        // Reimpressao e diagnostico de impressora.

        private void ReimprimirUltimaVendaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("PDV_REIMPRIMIR", "Voce nao possui permissao para reimprimir comprovantes do PDV."))
            {
                return;
            }

            var venda = SelecionarVendaOperacional(
                titulo: "Reimprimir comprovante",
                subtitulo: "Escolha uma venda recente para reimpressao do comprovante operacional.",
                textoConfirmacao: "Reimprimir venda",
                incluirCanceladas: false,
                filtro: item => !string.Equals(item.Status, "Cancelada", StringComparison.OrdinalIgnoreCase));

            if (venda == null)
            {
                return;
            }

            try
            {
                var snapshot = _printerDiagnosticsService.CaptureSnapshot();

                if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
                {
                    var documentoAutomacao = _comprovanteService.CriarDocumento(venda);
                    _ = documentoAutomacao.Blocks.Count;

                    global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                        "PDV",
                        "VendaReimpressa",
                        "Venda",
                        venda.Id.ToString(),
                        $"FormaPagamento={venda.FormaPagamento}; Total={venda.Total:C}; Modo=AutomatedTest; Impressoras={snapshot.BuildSummary()}");

                    return;
                }

                if (!snapshot.HasInstalledPrinters)
                {
                    // Fallback automatizado: salvar comprovante em arquivo XPS para auditoria quando nao houver impressora
                    try
                    {
                        var documentoFallback = _comprovanteService.CriarDocumento(venda);
                        var fallbackDir = Path.Combine(App.RuntimeAppDataPath, "Reimpressao");
                        Directory.CreateDirectory(fallbackDir);
                        var fileName = $"reimpressao-{venda.Id}-{DateTime.Now:yyyyMMddHHmmss}.xaml";
                        var filePath = Path.Combine(fallbackDir, fileName);

                        var xaml = XamlWriter.Save(documentoFallback);
                        File.WriteAllText(filePath, xaml);

                        App.Logger.LogInfo($"Comprovante salvo em XAML devido a ausencia de impressoras: {filePath}", "PDV");
                        App.Audit.RegistrarAcaoCritica("PDV", "VendaReimpressaFallback", "Venda", venda.Id.ToString(), $"FallbackXaml={filePath}; Total={venda.Total:C}");

                        ExibirMensagem(
                            $"Nenhuma impressora instalada. Comprovante salvo em: {filePath}",
                            "PDV",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        return;
                    }
                    catch (Exception ex)
                    {
                        ExibirMensagem(
                            string.IsNullOrWhiteSpace(snapshot.CaptureError)
                                ? $"Nenhuma impressora instalada foi detectada neste computador.\nFalha ao gerar fallback: {ex.Message}"
                                : $"Nenhuma impressora instalada foi detectada.\n\nDetalhes: {snapshot.CaptureError}\nFalha ao gerar fallback: {ex.Message}",
                            "PDV",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }
                }

                var printDialog = new PrintDialog();
                var impressoraConfigurada = ResolverImpressoraConfigurada(printDialog);

                if (impressoraConfigurada == null && printDialog.ShowDialog() != true)
                {
                    return;
                }

                var documento = _comprovanteService.CriarDocumento(venda);
                var areaLargura = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 302d;
                var areaAltura = printDialog.PrintableAreaHeight > 0 ? printDialog.PrintableAreaHeight : 840d;

                documento.PageWidth = areaLargura;
                documento.PageHeight = areaAltura;
                documento.PagePadding = new Thickness(32);
                documento.ColumnWidth = areaLargura;

                var paginator = ((IDocumentPaginatorSource)documento).DocumentPaginator;
                paginator.PageSize = new Size(areaLargura, areaAltura);

                printDialog.PrintDocument(paginator, $"Comprovante venda {venda.Id}");

                var printerSelecionada = impressoraConfigurada ?? _printerDiagnosticsService.DescribeSelectedPrinter(printDialog.PrintQueue);

                global::PrimoAutoEletrica.App.Logger.LogInfo(
                    $"Reimpressao do PDV enviada para '{printerSelecionada.Name}' (virtual={printerSelecionada.IsVirtual}, driver='{printerSelecionada.DriverName}', porta='{printerSelecionada.PortName}').",
                    "PDV");

                if (printerSelecionada.IsVirtual)
                {
                    global::PrimoAutoEletrica.App.Logger.LogWarning(
                        "A reimpressao do PDV foi enviada para uma impressora virtual. A homologacao final continua dependendo de uma impressora fisica real.",
                        "PDV");
                }

                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "PDV",
                    "VendaReimpressa",
                    "Venda",
                    venda.Id.ToString(),
                    $"FormaPagamento={venda.FormaPagamento}; Total={venda.Total:C}; Impressora={printerSelecionada.Name}; Driver={printerSelecionada.DriverName}; Porta={printerSelecionada.PortName}; Virtual={printerSelecionada.IsVirtual}");
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Nao foi possivel reimprimir a venda: {ex.Message}",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RegistrarDiagnosticoImpressao()
        {
            if (_diagnosticoImpressaoRegistrado)
            {
                return;
            }

            _diagnosticoImpressaoRegistrado = true;

            var snapshot = _printerDiagnosticsService.CaptureSnapshot();

            global::PrimoAutoEletrica.App.Logger.LogInfo(
                $"Diagnostico de impressao do PDV: {snapshot.BuildSummary()}",
                "PDV");

            if (!snapshot.HasPhysicalPrinter)
            {
                global::PrimoAutoEletrica.App.Logger.LogWarning(
                    "Nenhuma impressora fisica foi detectada para homologacao final do PDV. O ambiente atual depende apenas de fila virtual.",
                    "PDV");
            }
        }

        private PrinterDiagnosticInfo? ResolverImpressoraConfigurada(PrintDialog printDialog)
        {
            var stationConfig = StationService.GetConfiguration(App.RuntimeAppDataPath);

            if (!stationConfig.UseConfiguredPdvPrinter)
            {
                return null;
            }

            if (_printerDiagnosticsService.TryResolvePrinterQueue(
                    stationConfig.PreferredPdvPrinterName,
                    out var printQueue,
                    out var printerInfo,
                    out var message) &&
                printQueue != null &&
                printerInfo != null)
            {
                printDialog.PrintQueue = printQueue;

                App.Logger.LogInfo(
                    $"Reimpressao do PDV usara impressora configurada da estacao: {printerInfo.BuildSummary()}",
                    "PDV");

                return printerInfo;
            }

            App.Logger.LogWarning(
                $"Impressora preferencial do PDV indisponivel; usando seletor manual. {message}",
                "PDV");

            return null;
        }

    }
}
