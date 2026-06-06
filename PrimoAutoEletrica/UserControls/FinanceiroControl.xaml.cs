using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrimoAutoEletrica.UserControls
{
    public partial class FinanceiroControl : UserControl
    {
        private readonly FinanceiroViewModel _viewModel;
        private readonly PermissionService _permissionService;
        private DispatcherTimer? _timer;
        private readonly RelatorioFinanceiroService _relatorioService;

        public FinanceiroViewModel ViewModel => _viewModel;

        public FinanceiroControl()
        {
            InitializeComponent();
            _viewModel = new FinanceiroViewModel();
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger);
            _relatorioService = new RelatorioFinanceiroService();

            DataContext = _viewModel;
            Loaded += FinanceiroControl_Loaded;
            Unloaded += FinanceiroControl_Unloaded;
        }

        private void FinanceiroControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
        }

        private void FinanceiroControl_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigurarTimer();
        }

        private void ConfigurarTimer()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(60)
                };
                _timer.Tick += (s, args) => _viewModel.AtualizarHora();
            }

            _timer.Start();
        }

        private void AtualizarDadosButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AtualizarDashboard();
            ExibirMensagem("Dados atualizados com sucesso!", "Atualizacao", MessageBoxImage.Information);
        }

        private void ExportarRelatorioButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("FINANCEIRO_EXPORTAR", "Voce nao possui permissao para exportar relatorios financeiros."))
            {
                return;
            }

            if (App.IsAutomatedTestMode)
            {
                ExportarRelatorioEmAutomacao();
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Arquivo CSV (*.csv)|*.csv|Arquivo PDF (*.pdf)|*.pdf",
                DefaultExt = "csv",
                FileName = $"RelatorioFinanceiro_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    _relatorioService.ExportarPanoramaFinanceiroCsv(
                        saveDialog.FileName,
                        _viewModel.ContasPagar.ToList(),
                        _viewModel.ContasReceber.ToList(),
                        _viewModel.DemonstrativoResultado);
                }
                else if (saveDialog.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    var (inicio, fim) = _viewModel.ObterPeriodoFinanceiroAtual();
                    _relatorioService.ExportarPanoramaFinanceiroPdf(
                        saveDialog.FileName,
                        inicio,
                        fim,
                        new Dictionary<string, decimal>
                        {
                            { "Saldo Atual", _viewModel.SaldoAtual },
                            { "Entradas", _viewModel.TotalEntradas },
                            { "Saidas", _viewModel.TotalSaidas },
                            { "Saldo Projetado", _viewModel.SaldoPrevisto }
                        },
                        _viewModel.DemonstrativoResultado,
                        _viewModel.ObterResumoFormasPagamentoAtual(),
                        _viewModel.ContasPagar.ToList(),
                        _viewModel.ContasReceber.ToList());
                }

                ExibirMensagem("Relatorio exportado com sucesso!", "Exportacao", MessageBoxImage.Information);
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Financeiro",
                    "RelatorioExportado",
                    "RelatorioFinanceiro",
                    DateTime.Now.ToString("yyyyMMddHHmmss"),
                    saveDialog.FileName);
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao exportar relatorio: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void GerarPDFButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("FINANCEIRO_EXPORTAR", "Voce nao possui permissao para gerar PDF financeiro."))
            {
                return;
            }

            if (App.IsAutomatedTestMode)
            {
                GerarFluxoCaixaPdfEmAutomacao();
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Arquivo PDF (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = $"FluxoCaixa_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var financeiroDbService = new FinanceiroDatabaseService();
                var movimentacoes = financeiroDbService.ObterMovimentacoes();
                var (entradas, saidas, saldo) = financeiroDbService.ObterResumoFinanceiro(
                    DateTime.Now.AddMonths(-1), DateTime.Now);

                _relatorioService.ExportarFluxoCaixaPDF(
                    saveDialog.FileName,
                    DateTime.Now.AddMonths(-1),
                    DateTime.Now,
                    entradas,
                    saidas,
                    saldo,
                    movimentacoes);

                ExibirMensagem("PDF gerado com sucesso!", "PDF", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao gerar PDF: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void ImprimirButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("FINANCEIRO_IMPRIMIR", "Voce nao possui permissao para imprimir relatorios financeiros."))
            {
                return;
            }

            try
            {
                _viewModel.AtualizarDashboard();

                if (App.IsAutomatedTestMode)
                {
                    var documentoAutomacao = CriarDocumentoFinanceiro();
                    documentoAutomacao.PageWidth = 794;
                    documentoAutomacao.PageHeight = 1123;
                    documentoAutomacao.PagePadding = new Thickness(48);
                    documentoAutomacao.ColumnWidth = 698;
                    App.Logger.LogInfo("Impressao do relatorio financeiro validada em automacao sem abrir dialogo de impressora.", "Financeiro");
                    return;
                }

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() != true)
                {
                    return;
                }

                var documento = CriarDocumentoFinanceiro();
                documento.PageWidth = printDialog.PrintableAreaWidth;
                documento.PageHeight = printDialog.PrintableAreaHeight;
                documento.PagePadding = new Thickness(48);
                documento.ColumnWidth = printDialog.PrintableAreaWidth;

                var paginator = ((IDocumentPaginatorSource)documento).DocumentPaginator;
                paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                printDialog.PrintDocument(paginator, "Relatorio financeiro Primo Auto Eletrica");

                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Financeiro",
                    "RelatorioImpresso",
                    "RelatorioFinanceiro",
                    DateTime.Now.ToString("yyyyMMddHHmmss"),
                    "Impressao do relatorio financeiro mensal");
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Erro ao imprimir relatorio financeiro.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Financeiro", "FalhaImpressaoRelatorio", ex, "RelatorioFinanceiro");
                ExibirMensagem($"Erro ao imprimir relatorio financeiro:\n\n{ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            ExibirMensagem(mensagem, "Acesso negado", MessageBoxImage.Warning);
            return false;
        }

        private FlowDocument CriarDocumentoFinanceiro()
        {
            var hoje = DateTime.Today;
            var inicio = new DateTime(hoje.Year, hoje.Month, 1);
            var fim = inicio.AddMonths(1).AddDays(-1);

            var financeiroDbService = new FinanceiroDatabaseService();
            var (entradas, saidas, saldo) = financeiroDbService.ObterResumoFinanceiro(inicio, fim);

            var documento = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Foreground = Brushes.Black
            };

            documento.Blocks.Add(new Paragraph(new Run("Relatorio Financeiro"))
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            documento.Blocks.Add(new Paragraph(new Run(
                $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy} | Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} | Usuario: {App.Session.UserName}"))
            {
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 0, 0, 18)
            });

            AdicionarResumoFinanceiro(documento, entradas, saidas, saldo);
            AdicionarTabelaContasPagar(documento, _viewModel.ContasPagarFiltradas.Take(20).ToList());
            AdicionarTabelaContasReceber(documento, _viewModel.ContasReceberFiltradas.Take(20).ToList());

            return documento;
        }

        private static void AdicionarResumoFinanceiro(FlowDocument documento, decimal entradas, decimal saidas, decimal saldo)
        {
            documento.Blocks.Add(new Paragraph(new Run("Resumo do mes"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var tabela = CriarTabela(3);
            AdicionarLinha(tabela, true, "Entradas", "Saidas", "Saldo");
            AdicionarLinha(tabela, false, entradas.ToString("C"), saidas.ToString("C"), saldo.ToString("C"));
            documento.Blocks.Add(tabela);
        }

        private static void AdicionarTabelaContasPagar(FlowDocument documento, List<ContaPagar> contas)
        {
            documento.Blocks.Add(new Paragraph(new Run("Contas a pagar"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 18, 0, 8)
            });

            if (contas.Count == 0)
            {
                documento.Blocks.Add(new Paragraph(new Run("Nenhuma conta a pagar no filtro atual.")));
                return;
            }

            var tabela = CriarTabela(4);
            AdicionarLinha(tabela, true, "Fornecedor", "Vencimento", "Status", "Valor");
            foreach (var conta in contas)
            {
                AdicionarLinha(
                    tabela,
                    false,
                    conta.Fornecedor,
                    conta.DataVencimento.ToString("dd/MM/yyyy"),
                    conta.Status,
                    conta.Valor.ToString("C"));
            }

            documento.Blocks.Add(tabela);
        }

        private static void AdicionarTabelaContasReceber(FlowDocument documento, List<ContaReceber> contas)
        {
            documento.Blocks.Add(new Paragraph(new Run("Contas a receber"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 18, 0, 8)
            });

            if (contas.Count == 0)
            {
                documento.Blocks.Add(new Paragraph(new Run("Nenhuma conta a receber no filtro atual.")));
                return;
            }

            var tabela = CriarTabela(4);
            AdicionarLinha(tabela, true, "Cliente", "Vencimento", "Status", "Valor");
            foreach (var conta in contas)
            {
                AdicionarLinha(
                    tabela,
                    false,
                    conta.Cliente,
                    conta.DataVencimento.ToString("dd/MM/yyyy"),
                    conta.Status,
                    conta.Valor.ToString("C"));
            }

            documento.Blocks.Add(tabela);
        }

        private static Table CriarTabela(int colunas)
        {
            var tabela = new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            for (var i = 0; i < colunas; i++)
            {
                tabela.Columns.Add(new TableColumn());
            }

            tabela.RowGroups.Add(new TableRowGroup());
            return tabela;
        }

        private static void AdicionarLinha(Table tabela, bool cabecalho, params string[] valores)
        {
            var linha = new TableRow();
            foreach (var valor in valores)
            {
                var paragrafo = new Paragraph(new Run(valor))
                {
                    Margin = new Thickness(0),
                    FontWeight = cabecalho ? FontWeights.Bold : FontWeights.Normal
                };

                linha.Cells.Add(new TableCell(paragrafo)
                {
                    Padding = new Thickness(6),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Background = cabecalho ? Brushes.Gainsboro : Brushes.Transparent
                });
            }

            tabela.RowGroups[0].Rows.Add(linha);
        }

        private void FiltrosAvancadosButton_Click(object sender, RoutedEventArgs e)
        {
            ExibirMensagem("Filtros avancados disponiveis nos botoes de filtro das secoes de Contas a Pagar e Contas a Receber.", "Filtros", MessageBoxImage.Information);
        }

        private void FiltroContasPagarTodas_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.FiltrarContasPagar("todas");
        }

        private void FiltroContasPagarVencidas_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.FiltrarContasPagar("vencidas");
        }

        private void FiltroContasPagarHoje_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.FiltrarContasPagar("hoje");
        }

        private void FiltroContasPagarSemana_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.FiltrarContasPagar("semana");
        }

        private void FiltroContasReceberTodas_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.FiltrarContasReceber("todas");
        }

        private void FiltroContasReceberVencidas_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.FiltrarContasReceber("vencidas");
        }

        private void FiltroContasReceberHoje_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.FiltrarContasReceber("hoje");
        }

        private void FiltroContasReceberSemana_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.FiltrarContasReceber("semana");
        }

        private void BaixarContaPagarSelecionada_Click(object sender, RoutedEventArgs e)
        {
            var conta = _viewModel.ContaPagarSelecionada;
            if (conta == null)
            {
                ExibirMensagem("Selecione uma conta a pagar para registrar a baixa.", "Financeiro", MessageBoxImage.Information);
                return;
            }

            if (UiTextSanitizer.EqualsNormalized(conta.Status, "Paga"))
            {
                ExibirMensagem("A conta selecionada ja esta liquidada.", "Financeiro", MessageBoxImage.Information);
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Baixar conta a pagar",
                    Header = "Liquidacao financeira de despesa",
                    Summary = $"Voce esta prestes a registrar a baixa da conta '{conta.Descricao}'.",
                    Details = $"Fornecedor: {conta.Fornecedor}\nVencimento: {conta.DataVencimento:dd/MM/yyyy}\nValor: {conta.Valor:C}",
                    Impact = "O sistema vai liquidar a conta, registrar a despesa e atualizar o DRE operacional.",
                    Keyword = "BAIXAR",
                    ConfirmButtonText = "Baixar conta"
                });

            if (!confirmado)
            {
                return;
            }

            try
            {
                _viewModel.RegistrarPagamentoContaPagar(conta);
                ExibirMensagem("Conta a pagar liquidada com sucesso!", "Financeiro", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao liquidar conta a pagar selecionada.", ex, "Financeiro");
                ExibirMensagem($"Erro ao baixar conta a pagar:\n\n{ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void BaixarContaReceberSelecionada_Click(object sender, RoutedEventArgs e)
        {
            var conta = _viewModel.ContaReceberSelecionada;
            if (conta == null)
            {
                ExibirMensagem("Selecione uma conta a receber para registrar o recebimento.", "Financeiro", MessageBoxImage.Information);
                return;
            }

            if (UiTextSanitizer.EqualsNormalized(conta.Status, "Pago") ||
                UiTextSanitizer.EqualsNormalized(conta.Status, "Recebida"))
            {
                ExibirMensagem("A conta selecionada ja esta liquidada.", "Financeiro", MessageBoxImage.Information);
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Receber conta a receber",
                    Header = "Liquidacao financeira de receita",
                    Summary = $"Voce esta prestes a registrar o recebimento da conta '{conta.Descricao}'.",
                    Details = $"Cliente: {conta.Cliente}\nVencimento: {conta.DataVencimento:dd/MM/yyyy}\nValor: {conta.Valor:C}",
                    Impact = "O sistema vai liquidar a conta, registrar a receita e atualizar o DRE operacional.",
                    Keyword = "RECEBER",
                    ConfirmButtonText = "Registrar recebimento"
                });

            if (!confirmado)
            {
                return;
            }

            try
            {
                _viewModel.RegistrarRecebimentoContaReceber(conta);
                ExibirMensagem("Conta a receber liquidada com sucesso!", "Financeiro", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao liquidar conta a receber selecionada.", ex, "Financeiro");
                ExibirMensagem($"Erro ao registrar recebimento:\n\n{ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void ExportarRelatorioEmAutomacao()
        {
            try
            {
                var diretorio = ObterDiretorioAutomacao();
                var csvPath = Path.Combine(diretorio, $"RelatorioFinanceiro_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                _relatorioService.ExportarPanoramaFinanceiroCsv(
                    csvPath,
                    _viewModel.ContasPagar.ToList(),
                    _viewModel.ContasReceber.ToList(),
                    _viewModel.DemonstrativoResultado);

                App.Logger.LogInfo($"Relatorio financeiro CSV validado em automacao em '{csvPath}'.", "Financeiro");
                App.Audit.RegistrarAcaoCritica(
                    "Financeiro",
                    "RelatorioExportadoAutomacao",
                    "RelatorioFinanceiro",
                    DateTime.Now.ToString("yyyyMMddHHmmss"),
                    csvPath);
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao exportar relatorio: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void GerarFluxoCaixaPdfEmAutomacao()
        {
            try
            {
                var diretorio = ObterDiretorioAutomacao();
                var pdfPath = Path.Combine(diretorio, $"FluxoCaixa_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                var financeiroDbService = new FinanceiroDatabaseService();
                var movimentacoes = financeiroDbService.ObterMovimentacoes();
                var (entradas, saidas, saldo) = financeiroDbService.ObterResumoFinanceiro(
                    DateTime.Now.AddMonths(-1), DateTime.Now);

                _relatorioService.ExportarFluxoCaixaPDF(
                    pdfPath,
                    DateTime.Now.AddMonths(-1),
                    DateTime.Now,
                    entradas,
                    saidas,
                    saldo,
                    movimentacoes);

                App.Logger.LogInfo($"PDF financeiro validado em automacao em '{pdfPath}'.", "Financeiro");
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao gerar PDF: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private static string ObterDiretorioAutomacao()
        {
            var diretorio = Path.Combine(App.RuntimeLogDirectory, "financeiro-smoke");
            Directory.CreateDirectory(diretorio);
            return diretorio;
        }

        private static void ExibirMensagem(string mensagem, string titulo, MessageBoxImage imagem, Exception? ex = null)
        {
            if (App.IsAutomatedTestMode)
            {
                var texto = $"{titulo}: {mensagem}";
                if (imagem == MessageBoxImage.Error)
                {
                    App.Logger.LogError(texto, ex, "Financeiro");
                }
                else if (imagem == MessageBoxImage.Warning)
                {
                    App.Logger.LogWarning(texto, "Financeiro");
                }
                else
                {
                    App.Logger.LogInfo(texto, "Financeiro");
                }

                return;
            }

            MessageBox.Show(mensagem, titulo, MessageBoxButton.OK, imagem);
        }
    }
}
