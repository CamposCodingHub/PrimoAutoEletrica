using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrdensServicoControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly PermissionService _permissionService;
        private readonly FinanceiroDatabaseService _financeiroDatabaseService;
        private readonly OrdensServicoViewModel _viewModel;
        private List<OrdemServicoPainelItemViewModel> _todasOrdens = new();
        private Dictionary<int, Funcionario> _funcionarios = new();
        private Dictionary<Guid, Produto> _produtos = new();

        public OrdensServicoControl()
        {
            InitializeComponent();
            _viewModel = App.Services.GetRequiredService<OrdensServicoViewModel>();
            DataContext = _viewModel;

            _databaseService = global::PrimoAutoEletrica.App.Database;
            _ordemServicoRepository = global::PrimoAutoEletrica.App.Repositories.OrdensServico;
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);
            _financeiroDatabaseService = new FinanceiroDatabaseService();
            FiltroStatusComboBox.SelectedIndex = 0;
            FiltroPrioridadeComboBox.SelectedIndex = 0;

            Focusable = true;
            PreviewKeyDown += OrdensServicoControl_PreviewKeyDown;
            CarregarOrdens();
            Loaded += (_, _) =>
                Dispatcher.BeginInvoke(new Action(() => BuscaTextBox.Focus()), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void OrdensServicoControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NovaOsButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                BuscaTextBox.Focus();
                BuscaTextBox.SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter
                && Keyboard.Modifiers == ModifierKeys.None
                && OrdensListBox.IsKeyboardFocusWithin
                && ObterOrdemSelecionada() != null)
            {
                EditarOsButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void CarregarOrdens(Guid? ordemPreferida = null)
        {
            DefinirEstadoPainel(OsPainelEstado.Loading);

            try
            {
                _funcionarios = App.Repositories.Funcionarios.ObterTodos(false)
                    .ToDictionary(f => f.Id, f => f);

                _produtos = App.Repositories.Produtos.ObterTodos()
                    .ToDictionary(p => p.Id, p => p);

                _todasOrdens = _ordemServicoRepository.ObterTodos()
                    .OrderByDescending(o => o.DataAbertura)
                    .Select(o => new OrdemServicoPainelItemViewModel(o, _funcionarios, _produtos))
                    .ToList();

                AplicarFiltros(ordemPreferida);
                AtualizarIndicadores();
                DefinirEstadoPainel(_todasOrdens.Count == 0 ? OsPainelEstado.Empty : OsPainelEstado.Loaded);
            }
            catch (Exception ex)
            {
                OsErrorDescriptionText.Text = ex.Message;
                DefinirEstadoPainel(OsPainelEstado.Error);
                MessageBox.Show(
                    $"Falha ao carregar ordens de servico:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private enum OsPainelEstado
        {
            Loading,
            Loaded,
            Empty,
            Error
        }

        private void DefinirEstadoPainel(OsPainelEstado estado)
        {
            OsLoadingPanel.Visibility = estado == OsPainelEstado.Loading ? Visibility.Visible : Visibility.Collapsed;
            OsErrorPanel.Visibility = estado == OsPainelEstado.Error ? Visibility.Visible : Visibility.Collapsed;
            OsEmptyPanel.Visibility = estado == OsPainelEstado.Empty ? Visibility.Visible : Visibility.Collapsed;
            OsContentGrid.Visibility = estado == OsPainelEstado.Loaded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AplicarFiltros(Guid? ordemPreferida = null)
        {
            var busca = BuscaTextBox.Text?.Trim() ?? string.Empty;
            var status = ObterTextoComboBox(FiltroStatusComboBox);
            var prioridade = ObterTextoComboBox(FiltroPrioridadeComboBox);

            IEnumerable<OrdemServicoPainelItemViewModel> consulta = _todasOrdens;

            if (!string.IsNullOrWhiteSpace(busca))
            {
                consulta = consulta.Where(o =>
                    o.Numero.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    o.ClienteNome.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    o.VeiculoResumo.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    o.Placa.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    o.ProblemaRelatado.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    o.TecnicoResumo.Contains(busca, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(status, "Todos os status", StringComparison.OrdinalIgnoreCase))
                consulta = consulta.Where(o => UiTextSanitizer.EqualsNormalized(o.Status, status));

            if (!string.Equals(prioridade, "Todas", StringComparison.OrdinalIgnoreCase))
                consulta = consulta.Where(o => string.Equals(o.Prioridade, prioridade, StringComparison.OrdinalIgnoreCase));

            var lista = consulta
                .OrderByDescending(o => o.PrioridadePeso)
                .ThenBy(o => o.PrazoOrdenacao)
                .ThenByDescending(o => o.DataAbertura)
                .ToList();

            OrdensListBox.ItemsSource = lista;

            if (lista.Count == 0)
            {
                OrdensListBox.SelectedItem = null;
                return;
            }

            var itemSelecionado = ordemPreferida.HasValue
                ? lista.FirstOrDefault(o => o.Id == ordemPreferida.Value)
                : OrdensListBox.SelectedItem as OrdemServicoPainelItemViewModel;

            if (itemSelecionado == null || !lista.Any(o => o.Id == itemSelecionado.Id))
                itemSelecionado = lista[0];

            OrdensListBox.SelectedItem = itemSelecionado;
        }

        private void AtualizarIndicadores()
        {
            var abertas = _todasOrdens.Where(o => !o.EhConcluida).ToList();

            TotalAbertasText.Text = abertas.Count.ToString();
            AguardandoAprovacaoText.Text = abertas.Count(o => UiTextSanitizer.EqualsNormalized(o.Status, "Aguardando aprovacao")).ToString();
            AguardandoPecaText.Text = abertas.Count(o => UiTextSanitizer.EqualsNormalized(o.Status, "Aguardando peca")).ToString();
            ProntasText.Text = abertas.Count(o =>
                UiTextSanitizer.EqualsNormalized(o.Status, "Finalizada") ||
                UiTextSanitizer.EqualsNormalized(o.Status, "Aguardando pagamento") ||
                UiTextSanitizer.EqualsNormalized(o.Status, "Pronta para entrega")).ToString();
            FaturamentoPrevistoText.Text = abertas.Sum(o => o.Total).ToString("C");
        }

        private void NovaOsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORDENS_SERVICO_EDITAR", "Voce nao possui permissao para criar ordens de servico."))
                return;

            var janela = new OrdemServicoWindow(_databaseService);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (janela.ShowDialog() == true && janela.OrdemSalva != null)
                CarregarOrdens(janela.OrdemSalva.Id);
        }

        private void EditarOsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORDENS_SERVICO_EDITAR", "Voce nao possui permissao para editar ordens de servico."))
                return;

            var item = ObterOrdemSelecionada();
            if (item == null)
                return;

            var ordem = _ordemServicoRepository.ObterPorId(item.Id);
            if (ordem == null)
                return;

            var janela = new OrdemServicoWindow(_databaseService, ordem);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (janela.ShowDialog() == true && janela.OrdemSalva != null)
                CarregarOrdens(janela.OrdemSalva.Id);
        }

        private void AprovarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORDENS_SERVICO_APROVAR", "Voce nao possui permissao para aprovar ordens de servico."))
                return;

            AlterarOrdemSelecionada("Aprovar cliente", ordem =>
            {
                ordem.AprovadaCliente = true;

                if (string.IsNullOrWhiteSpace(ordem.MetodoAprovacao))
                    ordem.MetodoAprovacao = "WhatsApp";

                if (ordem.Status == "Rascunho" || ordem.Status == "Aguardando aprovacao")
                    ordem.Status = "Aprovada";

                ordem.Eventos.Insert(0, CriarEvento(
                    ordem.Id,
                    "Cliente aprovou",
                    $"OS aprovada pelo cliente via {ordem.MetodoAprovacao}.",
                    "Aprovacao"));
            });
        }

        private void AguardarPecaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORDENS_SERVICO_AVANCAR_STATUS", "Voce nao possui permissao para alterar o status da ordem de servico."))
                return;

            AlterarOrdemSelecionada("Aguardar peca", ordem =>
            {
                if (ordem.Status == "Entregue" || ordem.Status == "Cancelada")
                    throw new InvalidOperationException("Nao e possivel mover uma OS encerrada para aguardando peca.");

                ordem.Status = "Aguardando peca";
                ordem.Eventos.Insert(0, CriarEvento(
                    ordem.Id,
                    "Aguardando peca",
                    "OS pausada aguardando componente do estoque ou fornecedor.",
                    "Pecas"));
            });
        }

        private void AvancarStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORDENS_SERVICO_AVANCAR_STATUS", "Voce nao possui permissao para avancar o status da ordem de servico."))
                return;

            AlterarOrdemSelecionada("Avancar status", ordem =>
            {
                var proximoStatus = ObterProximoStatus(ordem);
                if (proximoStatus == null)
                    throw new InvalidOperationException("Esta OS nao possui proximo status automatico.");

                ordem.Status = proximoStatus;

                ordem.Eventos.Insert(0, CriarEvento(
                    ordem.Id,
                    "Status avancado",
                    $"OS avancada para '{proximoStatus}'.",
                    "Fluxo"));
            });
        }

        private void EnviarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ObterOrdemSelecionada();
            if (item == null)
                return;

            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(item.Telefone, out var telefone))
            {
                if (App.IsAutomatedTestMode)
                {
                    App.Logger.LogInfo($"Envio ao cliente ignorado em automacao para OS {item.Numero}: telefone/WhatsApp invalido.");
                    return;
                }

                MessageBox.Show(
                    "A OS selecionada nao possui telefone/WhatsApp valido para envio.",
                    UiText.T("ContactMissing"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Salvar PDF
            var pdfPath = SalvarPdf(item);
            
            if (pdfPath != null)
            {
                if (App.IsAutomatedTestMode)
                {
                    App.Logger.LogInfo($"Envio ao cliente validado em automacao para OS {item.Numero}. PDF gerado em '{pdfPath}' sem abrir apps externos.");
                    return;
                }

                var mensagem = $"Olá! Segue em anexo a Ordem de Serviço {item.Numero}.\n\n{MontarResumoCompartilhavel(item)}";
                var url = $"https://wa.me/{telefone}?text={Uri.EscapeDataString(mensagem)}";

                MessageBox.Show(
                    $"PDF salvo em:\n{pdfPath}\n\nO WhatsApp será aberto para você anexar o arquivo manualmente.",
                    "PDF Salvo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }

        private string? SalvarPdf(OrdemServicoPainelItemViewModel item)
        {
            try
            {
                var pdfDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PrimoAutoEletrica", "PDFs");
                Directory.CreateDirectory(pdfDir);

                var fileName = $"OS_{item.Numero.Replace("/", "-")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var pdfPath = Path.Combine(pdfDir, fileName);

                CriarPdfOrdemServico(item, pdfPath);

                return pdfPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao salvar PDF: {ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }

        private static void CriarPdfOrdemServico(OrdemServicoPainelItemViewModel item, string pdfPath)
        {
            var pdfDoc = new PdfSharpCore.Pdf.PdfDocument();
            pdfDoc.Info.Title = $"Ordem de Servico {item.Numero}";
            pdfDoc.Info.Subject = "Documento operacional da ordem de servico";

            PdfSharpCore.Pdf.PdfPage page = null!;
            PdfSharpCore.Drawing.XGraphics gfx = null!;
            var y = 50d;
            const double margin = 50d;

            var font = new PdfSharpCore.Drawing.XFont("Arial", 10);
            var smallFont = new PdfSharpCore.Drawing.XFont("Arial", 8);
            var boldFont = new PdfSharpCore.Drawing.XFont("Arial", 12, PdfSharpCore.Drawing.XFontStyle.Bold);
            var titleFont = new PdfSharpCore.Drawing.XFont("Arial", 16, PdfSharpCore.Drawing.XFontStyle.Bold);

            void NovaPagina()
            {
                gfx?.Dispose();
                page = pdfDoc.AddPage();
                gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
                y = margin;
            }

            double LarguraUtil()
            {
                return page.Width.Point - (margin * 2);
            }

            void GarantirEspaco(double altura)
            {
                if (y + altura > page.Height.Point - margin)
                {
                    NovaPagina();
                }
            }

            void Linha(string texto, PdfSharpCore.Drawing.XFont fonte, PdfSharpCore.Drawing.XBrush brush, double espacamento = 15)
            {
                foreach (var linha in QuebrarTexto(gfx, texto, fonte, LarguraUtil()))
                {
                    GarantirEspaco(espacamento);
                    gfx.DrawString(linha, fonte, brush, margin, y);
                    y += espacamento;
                }
            }

            void Secao(string titulo)
            {
                GarantirEspaco(28);
                y += 8;
                gfx.DrawString(titulo, boldFont, PdfSharpCore.Drawing.XBrushes.DarkOrange, margin, y);
                y += 16;
                gfx.DrawLine(PdfSharpCore.Drawing.XPens.DarkOrange, margin, y, page.Width.Point - margin, y);
                y += 14;
            }

            NovaPagina();

            gfx.DrawString("PRIMO AUTO ELETRICA", titleFont, PdfSharpCore.Drawing.XBrushes.DarkOrange, margin, y);
            y += 22;
            Linha("Servicos Automotivos de Qualidade", smallFont, PdfSharpCore.Drawing.XBrushes.DimGray, 12);
            y += 8;
            Linha($"ORDEM DE SERVICO No {item.Numero}", boldFont, PdfSharpCore.Drawing.XBrushes.Black, 18);

            Secao("Cliente");
            Linha($"Nome: {item.ClienteNome}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha($"Telefone: {item.Telefone ?? "-"}", font, PdfSharpCore.Drawing.XBrushes.Black);

            Secao("Veiculo");
            Linha($"Resumo: {item.VeiculoResumo}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha($"Placa: {item.Placa}", font, PdfSharpCore.Drawing.XBrushes.Black);

            Secao("Operacao");
            Linha($"Data: {item.DataAbertura:dd/MM/yyyy HH:mm}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha($"Status: {item.Status}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha($"Tecnico: {item.TecnicoResumo}", font, PdfSharpCore.Drawing.XBrushes.Black);

            Secao("Problema Relatado");
            Linha(string.IsNullOrWhiteSpace(item.ProblemaRelatado) ? "-" : item.ProblemaRelatado, font, PdfSharpCore.Drawing.XBrushes.Black);

            Secao("Diagnostico Inicial");
            Linha(string.IsNullOrWhiteSpace(item.DiagnosticoInicial) ? "-" : item.DiagnosticoInicial, font, PdfSharpCore.Drawing.XBrushes.Black);

            Secao("Diagnostico Final");
            Linha(string.IsNullOrWhiteSpace(item.DiagnosticoFinal) ? "-" : item.DiagnosticoFinal, font, PdfSharpCore.Drawing.XBrushes.Black);

            Secao("Itens da Ordem");
            if (item.Itens.Count == 0)
            {
                Linha("Nenhum item lancado.", font, PdfSharpCore.Drawing.XBrushes.DimGray);
            }
            else
            {
                foreach (var linha in item.Itens)
                {
                    Linha($"{linha.Descricao} | {linha.Resumo} | {linha.TotalFormatado}", font, PdfSharpCore.Drawing.XBrushes.Black);
                }
            }

            Secao("Resumo Financeiro");
            Linha($"Pecas: {item.TotalPecasFormatado}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha($"Servicos: {item.TotalServicosFormatado}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha($"Desconto: {item.DescontoFormatado}", font, PdfSharpCore.Drawing.XBrushes.Firebrick);
            Linha($"Total da OS: {item.TotalFormatado}", boldFont, PdfSharpCore.Drawing.XBrushes.DarkOrange, 18);

            Secao("Checklist e Garantia");
            Linha(string.IsNullOrWhiteSpace(item.ChecklistEntrada) ? "Checklist de entrada: nao informado." : $"Checklist de entrada: {item.ChecklistEntrada}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha(string.IsNullOrWhiteSpace(item.ChecklistSaida) ? "Checklist de saida: nao informado." : $"Checklist de saida: {item.ChecklistSaida}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha(string.IsNullOrWhiteSpace(item.GarantiaObservacoes) ? "Garantia: sem observacoes." : $"Garantia: {item.GarantiaObservacoes}", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha(item.GarantiaResumo, font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha(item.EvidenciasResumo, font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha(item.TempoResumo, font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha(item.OrcamentoResumo, font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha(item.AssinaturaResumo, font, PdfSharpCore.Drawing.XBrushes.Black);

            Secao("Aprovacao");
            Linha(item.AprovacaoResumo, font, PdfSharpCore.Drawing.XBrushes.Black);
            y += 30;
            Linha("Assinatura do cliente: __________________________________________", font, PdfSharpCore.Drawing.XBrushes.Black);
            Linha("Obrigado pela preferencia.", boldFont, PdfSharpCore.Drawing.XBrushes.DarkOrange, 18);

            gfx.Dispose();
            pdfDoc.Save(pdfPath);
        }

        private static List<string> QuebrarTexto(
            PdfSharpCore.Drawing.XGraphics gfx,
            string texto,
            PdfSharpCore.Drawing.XFont fonte,
            double larguraMaxima)
        {
            var linhas = new List<string>();
            var palavras = (texto ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var atual = string.Empty;

            foreach (var palavra in palavras)
            {
                var candidata = string.IsNullOrWhiteSpace(atual) ? palavra : $"{atual} {palavra}";
                if (gfx.MeasureString(candidata, fonte).Width <= larguraMaxima)
                {
                    atual = candidata;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(atual))
                {
                    linhas.Add(atual);
                }
                atual = palavra;
            }

            if (!string.IsNullOrWhiteSpace(atual))
            {
                linhas.Add(atual);
            }

            if (linhas.Count == 0)
            {
                linhas.Add("-");
            }

            return linhas;
        }

        private void ImprimirOsButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ObterOrdemSelecionada();
            if (item == null)
                return;

            if (App.IsAutomatedTestMode)
            {
                _ = CriarDocumentoImpressao(item);
                App.Logger.LogInfo($"Impressao da OS {item.Numero} validada em automacao sem abrir dialogo de impressora.");
                return;
            }

            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
                return;

            var documento = CriarDocumentoImpressao(item);
            dialog.PrintDocument(((IDocumentPaginatorSource)documento).DocumentPaginator, item.Numero);
        }

        private void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarOrdens((OrdensListBox.SelectedItem as OrdemServicoPainelItemViewModel)?.Id);
        }

        private void GerarFinanceiroButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissaoModulo("Financeiro", "Voce nao possui permissao para integrar a OS ao financeiro."))
                return;

            AlterarOrdemSelecionada("Gerar financeiro", ordem =>
            {
                _financeiroDatabaseService.RegistrarReceitaOrdemServico(ordem);
                ordem.Eventos.Insert(0, CriarEvento(
                    ordem.Id,
                    "Financeiro integrado",
                    $"Conta a receber atualizada para a OS {ordem.Numero}.",
                    "Financeiro"));
            });
        }

        private void CopiarResumoButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ObterOrdemSelecionada();
            if (item == null)
                return;

            Clipboard.SetText(MontarResumoCompartilhavel(item));
            MessageBox.Show(
                "Resumo da OS copiado para a area de transferencia.",
                "Resumo copiado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void HistoricoClienteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ObterOrdemSelecionada();
            if (item == null)
                return;

            var cliente = App.Repositories.Clientes.ObterPorId(item.ClienteId);
            if (cliente == null)
                return;

            var janela = new HistoricoClienteWindow(cliente);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            janela.ShowDialog();
        }

        private void ExcluirOsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORDENS_SERVICO_EXCLUIR", "Voce nao possui permissao para excluir ordens de servico."))
                return;

            var item = ObterOrdemSelecionada();
            if (item == null)
            {
                MessageBox.Show(UiText.T("SelectOsToDelete"), UiText.T("NoOsSelected"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!CriticalActionDialogService.ConfirmarExclusao(
                Window.GetWindow(this),
                "ordem de servico",
                item.Numero,
                $"Cliente: {item.ClienteNome}\nVeiculo: {item.VeiculoResumo}\nTotal previsto: {item.TotalFormatado}",
                "A ordem, os itens, os eventos e os vinculos operacionais desse registro serao removidos do banco local sem desfazer automatico."))
                return;

            _ordemServicoRepository.Excluir(item.Id);
            App.Audit.RegistrarAcaoCritica(
                "OrdensServico",
                "ExcluirOS",
                "OrdemServico",
                item.Id.ToString(),
                $"Numero={item.Numero}; Cliente={item.ClienteNome}; Total={item.TotalFormatado}");
            MessageBox.Show($"OS {item.Numero} excluida com sucesso.", UiText.T("Deleted"), MessageBoxButton.OK, MessageBoxImage.Information);
            CarregarOrdens();
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
                return true;

            MessageBox.Show(mensagem, UiText.T("AccessDenied"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private bool ValidarPermissaoModulo(string modulo, string mensagem)
        {
            if (_permissionService.TemPermissao(modulo))
                return true;

            MessageBox.Show(mensagem, UiText.T("AccessDenied"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros((OrdensListBox.SelectedItem as OrdemServicoPainelItemViewModel)?.Id);
        }

        private void FiltroStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            AplicarFiltros((OrdensListBox.SelectedItem as OrdemServicoPainelItemViewModel)?.Id);
        }

        private void FiltroPrioridadeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            AplicarFiltros((OrdensListBox.SelectedItem as OrdemServicoPainelItemViewModel)?.Id);
        }

        private void OrdensListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private OrdemServicoPainelItemViewModel? ObterOrdemSelecionada()
        {
            if (OrdensListBox.SelectedItem is OrdemServicoPainelItemViewModel item)
                return item;

            MessageBox.Show(
                "Selecione uma ordem de servico primeiro.",
                "OS nao selecionada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return null;
        }

        private void AlterarOrdemSelecionada(string acao, Action<OrdemServico> alteracao)
        {
            var item = ObterOrdemSelecionada();
            if (item == null)
                return;

            try
            {
                var ordem = _ordemServicoRepository.ObterPorId(item.Id);
                if (ordem == null)
                    throw new InvalidOperationException("Nao foi possivel localizar a OS selecionada.");

                alteracao(ordem);
                _ordemServicoRepository.Atualizar(ordem);
                CarregarOrdens(ordem.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Falha ao {acao.ToLower()}:\n{ex.Message}",
                    "Operacao nao concluida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static string? ObterProximoStatus(OrdemServico ordem)
        {
            return ordem.Status switch
            {
                "Rascunho" => "Aberta",
                "Aberta" => "Em diagnostico",
                "Aguardando aprovacao" => ordem.AprovadaCliente ? "Em execucao" : null,
                "Aprovada" => "Em diagnostico",
                "Em diagnostico" => ordem.AprovadaCliente ? "Em execucao" : "Aguardando aprovacao",
                "Aguardando peca" => "Em execucao",
                "Em execucao" => "Finalizada",
                "Finalizada" => "Aguardando pagamento",
                "Aguardando pagamento" => "Entregue",
                "Pronta para entrega" => "Entregue",
                _ => null
            };
        }

        private static OrdemServicoEvento CriarEvento(Guid ordemId, string titulo, string descricao, string tipo)
        {
            return new OrdemServicoEvento
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = ordemId,
                DataEvento = DateTime.Now,
                Titulo = titulo,
                Descricao = descricao,
                Tipo = tipo,
                Usuario = "Sistema"
            };
        }

        private static string ObterTextoComboBox(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                // Preferir Tag (valor interno estavel) quando existir; Content pode estar localizado.
                if (item.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
                    return tag;

                if (item.Content is string content)
                    return content;

                return item.Content?.ToString() ?? string.Empty;
            }

            return comboBox.Text ?? string.Empty;
        }

        private static string MontarResumoCompartilhavel(OrdemServicoPainelItemViewModel item)
        {
            return
                $"OS {item.Numero}\n" +
                $"Cliente: {item.ClienteNome}\n" +
                $"Veiculo: {item.VeiculoResumo} {item.Placa}\n" +
                $"Status: {item.Status}\n" +
                $"Tecnico: {item.TecnicoResumo}\n" +
                $"Previsao: {item.PrazoResumo}\n" +
                $"Total previsto: {item.TotalFormatado}\n" +
                $"Problema: {item.ProblemaRelatado}\n" +
                $"Diagnostico inicial: {item.DiagnosticoInicial}\n" +
                $"Diagnostico final: {item.DiagnosticoFinal}\n" +
                $"Termo: {(string.IsNullOrWhiteSpace(item.TermoAutorizacao) ? "Nao informado" : item.TermoAutorizacao)}\n" +
                $"{item.EvidenciasResumo}\n" +
                $"{item.TempoResumo}\n" +
                $"{item.OrcamentoResumo}\n" +
                $"Garantia: {item.GarantiaResumo}";
        }

        private static FlowDocument CriarDocumentoImpressao(OrdemServicoPainelItemViewModel item)
        {
            var documento = new FlowDocument
            {
                PagePadding = new Thickness(42, 36, 42, 44),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                LineHeight = 16
            };

            documento.Blocks.Add(CriarCabecalhoImpressao(item));
            documento.Blocks.Add(CriarBlocoResumoImpressao(item));
            documento.Blocks.Add(CriarSecaoTextoImpressao("Problema relatado", item.ProblemaRelatado));
            documento.Blocks.Add(CriarSecaoTextoImpressao("Diagnostico inicial", item.DiagnosticoInicial));
            documento.Blocks.Add(CriarSecaoTextoImpressao("Diagnostico final", item.DiagnosticoFinal));
            documento.Blocks.Add(CriarTabelaItensImpressao(item));
            documento.Blocks.Add(CriarBlocoFinanceiroImpressao(item));
            documento.Blocks.Add(CriarBlocoObservacoesImpressao(item));
            documento.Blocks.Add(CriarSecaoListaImpressao(
                "Checklist, garantia e evidencias",
                new[]
                {
                    string.IsNullOrWhiteSpace(item.ChecklistEntrada) ? "Checklist de entrada: nao informado." : $"Checklist de entrada: {item.ChecklistEntrada}",
                    string.IsNullOrWhiteSpace(item.TermoAutorizacao) ? "Termo de autorizacao: nao informado." : $"Termo de autorizacao: {item.TermoAutorizacao}",
                    string.IsNullOrWhiteSpace(item.ChecklistSaida) ? "Checklist de saida: nao informado." : $"Checklist de saida: {item.ChecklistSaida}",
                    string.IsNullOrWhiteSpace(item.GarantiaObservacoes) ? "Garantia: sem observacoes adicionais." : $"Garantia: {item.GarantiaObservacoes}",
                    item.GarantiaResumo,
                    item.EvidenciasResumo
                }));
            documento.Blocks.Add(CriarSecaoListaImpressao(
                "Contexto operacional",
                new[]
                {
                    $"Aprovacao: {item.AprovacaoResumo}",
                    $"Metodo de aprovacao: {TextoOuPadrao(item.MetodoAprovacao, "Nao definido")}",
                    item.TempoResumo,
                    item.OrcamentoResumo,
                    item.AssinaturaResumo
                }));
            documento.Blocks.Add(CriarRodapeImpressao());

            return documento;
        }

        private static BlockUIContainer CriarCabecalhoImpressao(OrdemServicoPainelItemViewModel item)
        {
            var brandDark = CriarBrush("#182538");
            var brandOrange = CriarBrush("#E46C0A");
            var primaryText = CriarBrush("#F8FAFC");
            var secondaryText = CriarBrush("#D8E1EA");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = "Primo Auto Eletrica",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = primaryText
            });
            left.Children.Add(new TextBlock
            {
                Text = "Ordem de servico tecnica com historico, pecas, servicos e garantia",
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 11,
                Foreground = secondaryText,
                TextWrapping = TextWrapping.Wrap
            });
            left.Children.Add(new TextBlock
            {
                Text = $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}",
                Margin = new Thickness(0, 10, 0, 0),
                FontSize = 10,
                Foreground = secondaryText
            });

            var badge = new Border
            {
                Background = Brushes.White,
                BorderBrush = CriarBrush("#D6DDE6"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(20, 0, 0, 0),
                MinWidth = 200
            };

            var badgeStack = new StackPanel();
            badgeStack.Children.Add(new TextBlock
            {
                Text = "ORDEM DE SERVICO",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = brandOrange
            });
            badgeStack.Children.Add(new TextBlock
            {
                Text = item.Numero,
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = CriarBrush("#1F2937")
            });
            badgeStack.Children.Add(new TextBlock
            {
                Text = $"{item.Status}  |  {item.Prioridade}",
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 10,
                Foreground = CriarBrush("#6B7280")
            });
            badge.Child = badgeStack;

            Grid.SetColumn(left, 0);
            Grid.SetColumn(badge, 1);
            grid.Children.Add(left);
            grid.Children.Add(badge);

            var border = new Border
            {
                Background = brandDark,
                BorderBrush = brandOrange,
                BorderThickness = new Thickness(0, 0, 0, 4),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24, 22, 24, 20),
                Child = grid
            };

            return new BlockUIContainer(border)
            {
                Margin = new Thickness(0, 0, 0, 18)
            };
        }

        private static BlockUIContainer CriarBlocoResumoImpressao(OrdemServicoPainelItemViewModel item)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AdicionarResumoCelula(grid, 0, 0, "Cliente", item.ClienteNome);
            AdicionarResumoCelula(grid, 0, 1, "Telefone", TextoOuPadrao(item.Telefone, "-"));
            AdicionarResumoCelula(grid, 0, 2, "Abertura", item.DataAbertura.ToString("dd/MM/yyyy HH:mm"));
            AdicionarResumoCelula(grid, 1, 0, "Veiculo", TextoOuPadrao(item.VeiculoResumo, "-"));
            AdicionarResumoCelula(grid, 1, 1, "Placa", TextoOuPadrao(item.Placa, "-"));
            AdicionarResumoCelula(grid, 1, 2, "Tecnico", item.TecnicoResumo);
            AdicionarResumoCelula(grid, 2, 0, "Previsao", item.PrazoResumo);
            AdicionarResumoCelula(grid, 2, 1, "Aprovacao", item.AprovacaoResumo);
            AdicionarResumoCelula(grid, 2, 2, "Progresso", item.ProgressoResumo);

            var border = new Border
            {
                Background = CriarBrush("#F8FAFC"),
                BorderBrush = CriarBrush("#D9E0E8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Child = grid
            };

            return new BlockUIContainer(border)
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
        }

        private static Section CriarSecaoTextoImpressao(string titulo, string conteudo)
        {
            var secao = new Section
            {
                Margin = new Thickness(0, 0, 0, 14)
            };
            secao.Blocks.Add(CriarTituloSecaoImpressao(titulo));
            secao.Blocks.Add(CriarParagrafoCorpoImpressao(TextoOuPadrao(conteudo, "-")));
            return secao;
        }

        private static Section CriarSecaoListaImpressao(string titulo, IEnumerable<string> linhas)
        {
            var secao = new Section
            {
                Margin = new Thickness(0, 0, 0, 14)
            };
            secao.Blocks.Add(CriarTituloSecaoImpressao(titulo));

            foreach (var linha in linhas.Where(linha => !string.IsNullOrWhiteSpace(linha)))
            {
                secao.Blocks.Add(new Paragraph(new Run($"- {linha}"))
                {
                    Margin = new Thickness(0, 0, 0, 5),
                    FontSize = 11,
                    Foreground = CriarBrush("#374151")
                });
            }

            return secao;
        }

        private static Section CriarTabelaItensImpressao(OrdemServicoPainelItemViewModel item)
        {
            var tabela = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0)
            };

            tabela.Columns.Add(new TableColumn { Width = new GridLength(250) });
            tabela.Columns.Add(new TableColumn { Width = new GridLength(170) });
            tabela.Columns.Add(new TableColumn { Width = new GridLength(110) });
            tabela.Columns.Add(new TableColumn { Width = new GridLength(90) });

            var grupo = new TableRowGroup();
            tabela.RowGroups.Add(grupo);

            var cabecalho = new TableRow
            {
                Background = CriarBrush("#E46C0A")
            };
            cabecalho.Cells.Add(CriarCelulaTabelaImpressao("Descricao", true));
            cabecalho.Cells.Add(CriarCelulaTabelaImpressao("Resumo", true));
            cabecalho.Cells.Add(CriarCelulaTabelaImpressao("Estoque", true));
            cabecalho.Cells.Add(CriarCelulaTabelaImpressao("Total", true, TextAlignment.Right));
            grupo.Rows.Add(cabecalho);

            if (item.Itens.Count == 0)
            {
                var vazio = new TableRow
                {
                    Background = Brushes.White
                };
                vazio.Cells.Add(CriarCelulaTabelaImpressao("Nenhum item lancado nesta ordem."));
                vazio.Cells.Add(CriarCelulaTabelaImpressao("-"));
                vazio.Cells.Add(CriarCelulaTabelaImpressao("-"));
                vazio.Cells.Add(CriarCelulaTabelaImpressao(item.TotalFormatado, false, TextAlignment.Right));
                grupo.Rows.Add(vazio);
            }

            var indice = 0;
            foreach (var linha in item.Itens)
            {
                var row = new TableRow
                {
                    Background = (indice++ % 2) == 0 ? Brushes.White : CriarBrush("#F8FAFC")
                };
                row.Cells.Add(CriarCelulaTabelaImpressao(linha.Descricao));
                row.Cells.Add(CriarCelulaTabelaImpressao(linha.Resumo));
                row.Cells.Add(CriarCelulaTabelaImpressao(linha.EstoqueResumo));
                row.Cells.Add(CriarCelulaTabelaImpressao(linha.TotalFormatado, false, TextAlignment.Right));
                grupo.Rows.Add(row);
            }

            var secao = new Section
            {
                Margin = new Thickness(0, 0, 0, 14)
            };
            secao.Blocks.Add(CriarTituloSecaoImpressao("Itens da ordem"));
            secao.Blocks.Add(tabela);
            return secao;
        }

        private static BlockUIContainer CriarBlocoFinanceiroImpressao(OrdemServicoPainelItemViewModel item)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            grid.Children.Add(CriarCardFinanceiro("Pecas", item.TotalPecasFormatado, "#F8FAFC", "#1F2937", 0));
            grid.Children.Add(CriarCardFinanceiro("Servicos", item.TotalServicosFormatado, "#F8FAFC", "#1F2937", 1));
            grid.Children.Add(CriarCardFinanceiro("Desconto", item.DescontoFormatado, "#FFF1F2", "#B91C1C", 2));
            grid.Children.Add(CriarCardFinanceiro("Total", item.TotalFormatado, "#FFF7ED", "#C2410C", 3, destaque: true));

            var container = new StackPanel();
            container.Children.Add(new TextBlock
            {
                Text = "Resumo financeiro",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = CriarBrush("#C2410C"),
                Margin = new Thickness(0, 0, 0, 10)
            });
            container.Children.Add(grid);

            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = CriarBrush("#D9E0E8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Child = container
            };

            return new BlockUIContainer(border)
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
        }

        private static BlockUIContainer CriarBlocoObservacoesImpressao(OrdemServicoPainelItemViewModel item)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var clienteCard = CriarCardTextoImpressao("Observacoes do cliente", item.ObservacoesCliente, new Thickness(0, 0, 8, 0));
            var internaCard = CriarCardTextoImpressao("Observacoes internas", item.ObservacoesInternas, new Thickness(8, 0, 0, 0));

            Grid.SetColumn(clienteCard, 0);
            Grid.SetColumn(internaCard, 1);
            grid.Children.Add(clienteCard);
            grid.Children.Add(internaCard);

            var container = new StackPanel();
            container.Children.Add(new TextBlock
            {
                Text = "Observacoes complementares",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = CriarBrush("#C2410C"),
                Margin = new Thickness(0, 0, 0, 10)
            });
            container.Children.Add(grid);

            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = CriarBrush("#D9E0E8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Child = container
            };

            return new BlockUIContainer(border)
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
        }

        private static BlockUIContainer CriarRodapeImpressao()
        {
            var stack = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            stack.Children.Add(new Border
            {
                Height = 1,
                Background = CriarBrush("#D9E0E8"),
                Margin = new Thickness(0, 0, 0, 12)
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Obrigado pela preferencia!",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = CriarBrush("#C2410C"),
                TextAlignment = TextAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Primo Auto Eletrica  |  Atendimento tecnico, pecas e servicos automotivos",
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 10,
                Foreground = CriarBrush("#6B7280"),
                TextAlignment = TextAlignment.Center
            });

            return new BlockUIContainer(stack);
        }

        private static Paragraph CriarTituloSecaoImpressao(string titulo)
        {
            return new Paragraph(new Run(titulo.ToUpperInvariant()))
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 6, 12, 6),
                Background = CriarBrush("#FFF7ED"),
                BorderBrush = CriarBrush("#F2D3B0"),
                BorderThickness = new Thickness(1),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = CriarBrush("#C2410C")
            };
        }

        private static Paragraph CriarParagrafoCorpoImpressao(string conteudo)
        {
            return new Paragraph(new Run(conteudo))
            {
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(12, 10, 12, 10),
                Background = Brushes.White,
                BorderBrush = CriarBrush("#D9E0E8"),
                BorderThickness = new Thickness(1),
                FontSize = 11,
                Foreground = CriarBrush("#374151")
            };
        }

        private static TableCell CriarCelulaTabelaImpressao(string texto, bool header = false, TextAlignment alinhamento = TextAlignment.Left)
        {
            var paragraph = new Paragraph(new Run(TextoOuPadrao(texto, "-")))
            {
                Margin = new Thickness(0),
                TextAlignment = alinhamento,
                FontSize = header ? 10 : 10.5,
                Foreground = header ? Brushes.White : CriarBrush("#374151")
            };

            return new TableCell(paragraph)
            {
                Padding = new Thickness(8, 7, 8, 7),
                BorderBrush = header ? CriarBrush("#D86A11") : CriarBrush("#D9E0E8"),
                BorderThickness = new Thickness(0.8),
                FontWeight = header ? FontWeights.Bold : FontWeights.Normal
            };
        }

        private static Border CriarCardFinanceiro(string titulo, string valor, string fundoHex, string valorHex, int column, bool destaque = false)
        {
            var border = new Border
            {
                Background = CriarBrush(fundoHex),
                BorderBrush = CriarBrush("#D9E0E8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(column == 0 ? 0 : 8, 0, column == 3 ? 0 : 0, 0)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = titulo.ToUpperInvariant(),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = CriarBrush("#6B7280")
            });
            stack.Children.Add(new TextBlock
            {
                Text = valor,
                Margin = new Thickness(0, 8, 0, 0),
                FontSize = destaque ? 17 : 13,
                FontWeight = FontWeights.Bold,
                Foreground = CriarBrush(valorHex)
            });

            border.Child = stack;
            Grid.SetColumn(border, column);
            return border;
        }

        private static Border CriarCardTextoImpressao(string titulo, string conteudo, Thickness margin)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = CriarBrush("#D9E0E8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14),
                Margin = margin
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = titulo,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = CriarBrush("#C2410C")
            });
            stack.Children.Add(new TextBlock
            {
                Text = TextoOuPadrao(conteudo, "-"),
                Margin = new Thickness(0, 8, 0, 0),
                FontSize = 11,
                Foreground = CriarBrush("#374151"),
                TextWrapping = TextWrapping.Wrap
            });

            border.Child = stack;
            return border;
        }

        private static void AdicionarResumoCelula(Grid grid, int row, int column, string titulo, string valor)
        {
            var stack = new StackPanel
            {
                Margin = new Thickness(column == 0 ? 0 : 10, row == 0 ? 0 : 8, 0, 0)
            };
            stack.Children.Add(new TextBlock
            {
                Text = titulo.ToUpperInvariant(),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = CriarBrush("#6B7280")
            });
            stack.Children.Add(new TextBlock
            {
                Text = TextoOuPadrao(valor, "-"),
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = CriarBrush("#1F2937"),
                TextWrapping = TextWrapping.Wrap
            });

            Grid.SetRow(stack, row);
            Grid.SetColumn(stack, column);
            grid.Children.Add(stack);
        }

        private static SolidColorBrush CriarBrush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }

        private static string TextoOuPadrao(string? texto, string fallback)
        {
            return string.IsNullOrWhiteSpace(texto) ? fallback : texto.Trim();
        }
    }

    public sealed class OrdemServicoPainelItemViewModel
    {
        public OrdemServicoPainelItemViewModel(
            OrdemServico ordem,
            IReadOnlyDictionary<int, Funcionario> funcionarios,
            IReadOnlyDictionary<Guid, Produto> produtos)
        {
            Id = ordem.Id;
            ClienteId = ordem.ClienteId;
            Numero = ordem.Numero;
            ClienteNome = ordem.ClienteNomeSnapshot;
            Telefone = ordem.TelefoneClienteSnapshot;
            VeiculoResumo = ordem.VeiculoDescricaoSnapshot;
            Placa = ordem.PlacaSnapshot;
            Status = string.IsNullOrWhiteSpace(ordem.Status) ? "Rascunho" : ordem.Status;
            Prioridade = string.IsNullOrWhiteSpace(ordem.Prioridade) ? "Normal" : ordem.Prioridade;
            ProblemaRelatado = string.IsNullOrWhiteSpace(ordem.ProblemaRelatado) ? "-" : ordem.ProblemaRelatado;
            Diagnostico = string.IsNullOrWhiteSpace(ordem.Diagnostico) ? "-" : ordem.Diagnostico;
            DiagnosticoInicial = string.IsNullOrWhiteSpace(ordem.DiagnosticoInicial) ? Diagnostico : ordem.DiagnosticoInicial;
            DiagnosticoFinal = string.IsNullOrWhiteSpace(ordem.DiagnosticoFinal) ? Diagnostico : ordem.DiagnosticoFinal;
            ObservacoesCliente = string.IsNullOrWhiteSpace(ordem.ObservacoesCliente) ? "-" : ordem.ObservacoesCliente;
            ObservacoesInternas = string.IsNullOrWhiteSpace(ordem.ObservacoesInternas) ? "-" : ordem.ObservacoesInternas;
            ChecklistEntrada = string.IsNullOrWhiteSpace(ordem.ChecklistEntrada) ? string.Empty : ordem.ChecklistEntrada;
            TermoAutorizacao = string.IsNullOrWhiteSpace(ordem.TermoAutorizacao) ? string.Empty : ordem.TermoAutorizacao;
            ChecklistEntrega = string.IsNullOrWhiteSpace(ordem.ChecklistEntrega) ? string.Empty : ordem.ChecklistEntrega;
            ChecklistSaida = string.IsNullOrWhiteSpace(ordem.ChecklistSaida) ? ChecklistEntrega : ordem.ChecklistSaida;
            GarantiaObservacoes = string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes) ? string.Empty : ordem.GarantiaObservacoes;
            QuantidadeFotosAntes = OrdemServicoMediaService.DeserializePaths(ordem.FotosAntes).Count;
            QuantidadeFotosDepois = OrdemServicoMediaService.DeserializePaths(ordem.FotosDepois).Count;
            AssinaturaClienteUrl = ordem.AssinaturaClienteUrl ?? string.Empty;
            TempoPrevistoMinutos = ordem.TempoPrevistoMinutos;
            TempoRealMinutos = ordem.TempoRealMinutos;
            OrcamentoId = ordem.OrcamentoId;
            DataAbertura = ordem.DataAbertura;
            DataPrevisao = ordem.DataPrevisao;
            GarantiaValidaAte = ordem.GarantiaValidaAte;
            AprovadaCliente = ordem.AprovadaCliente;
            MetodoAprovacao = ordem.MetodoAprovacao;
            Desconto = ordem.Desconto;
            TecnicoResumo = ordem.TecnicoId.HasValue && funcionarios.TryGetValue(ordem.TecnicoId.Value, out var tecnico)
                ? tecnico.Nome
                : "Nao atribuido";

            TotalPecas = ordem.Itens
                .Where(i => string.Equals(i.Tipo, "Peca", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Total);

            TotalServicos = ordem.Itens
                .Where(i => string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Total);

            CustoTotal = ordem.Itens.Sum(i => i.CustoTotal);
            Total = Math.Max(0, TotalPecas + TotalServicos - Desconto);
            Lucro = Total - CustoTotal;
            EhConcluida = Status == "Entregue" || Status == "Cancelada";
            PrazoOrdenacao = DataPrevisao ?? DateTime.MaxValue;
            Subtitulo = $"Abertura em {DataAbertura:dd/MM/yyyy HH:mm}";
            Progresso = Status switch
            {
                "Rascunho" => 8,
                "Aberta" => 14,
                "Aguardando aprovacao" => 18,
                "Aprovada" => 32,
                "Em diagnostico" => 48,
                "Aguardando peca" => 58,
                "Em execucao" => 72,
                "Finalizada" => 86,
                "Aguardando pagamento" => 94,
                "Pronta para entrega" => 92,
                "Entregue" => 100,
                _ => 0
            };
            ProgressoResumo = $"{Progresso}% do fluxo concluido";

            (StatusBackground, StatusForeground) = Status switch
            {
                "Rascunho" => (ThemeBrush("SurfaceAltBrush", "#E5E7EB"), ThemeBrush("SecondaryTextBrush", "#374151")),
                "Aberta" => (ThemeBrush("InfoCardBackgroundBrush", "#E0F2FE"), ThemeBrush("InfoBrush", "#0369A1")),
                "Aguardando aprovacao" => (ThemeBrush("WarningCardBackgroundBrush", "#FEF3C7"), ThemeBrush("WarningBrush", "#B45309")),
                "Aprovada" => (ThemeBrush("InfoCardBackgroundBrush", "#DBEAFE"), ThemeBrush("InfoBrush", "#1D4ED8")),
                "Em diagnostico" => (ThemeBrush("InfoCardBackgroundBrush", "#E0F2FE"), ThemeBrush("InfoBrush", "#0369A1")),
                "Em execucao" => (ThemeBrush("BrandSoftBrush", "#FFEDD5"), ThemeBrush("PrimaryBrush", "#C2410C")),
                "Aguardando peca" => (ThemeBrush("WarningCardBackgroundBrush", "#FEF3C7"), ThemeBrush("WarningBrush", "#B45309")),
                "Finalizada" => (ThemeBrush("SuccessCardBackgroundBrush", "#DCFCE7"), ThemeBrush("SuccessBrush", "#047857")),
                "Aguardando pagamento" => (ThemeBrush("WarningCardBackgroundBrush", "#FDE68A"), ThemeBrush("WarningBrush", "#92400E")),
                "Pronta para entrega" => (ThemeBrush("SuccessCardBackgroundBrush", "#DCFCE7"), ThemeBrush("SuccessBrush", "#047857")),
                "Entregue" => (ThemeBrush("SuccessCardBackgroundBrush", "#D1FAE5"), ThemeBrush("SuccessBrush", "#065F46")),
                _ => (ThemeBrush("DangerCardBackgroundBrush", "#FEE2E2"), ThemeBrush("DangerBrush", "#B91C1C"))
            };

            (PrioridadeBackground, PrioridadeForeground) = Prioridade switch
            {
                "Alta" => (ThemeBrush("DangerCardBackgroundBrush", "#FEE2E2"), ThemeBrush("DangerBrush", "#B91C1C")),
                "Baixa" => (ThemeBrush("InfoCardBackgroundBrush", "#E0F2FE"), ThemeBrush("InfoBrush", "#0369A1")),
                _ => (ThemeBrush("SurfaceAltBrush", "#E5E7EB"), ThemeBrush("SecondaryTextBrush", "#374151"))
            };

            Itens = new ObservableCollection<OrdemServicoLinhaResumoViewModel>(
                ordem.Itens.Select(i => new OrdemServicoLinhaResumoViewModel(i, produtos)));

            Eventos = new ObservableCollection<OrdemServicoEventoResumoViewModel>(
                ordem.Eventos
                    .OrderByDescending(e => e.DataEvento)
                    .Select(e => new OrdemServicoEventoResumoViewModel(e)));
        }

        public Guid Id { get; }
        public Guid ClienteId { get; }
        public string Numero { get; }
        public string ClienteNome { get; }
        public string Telefone { get; }
        public string VeiculoResumo { get; }
        public string Placa { get; }
        public string Status { get; }
        public string Prioridade { get; }
        public string ProblemaRelatado { get; }
        public string Diagnostico { get; }
        public string DiagnosticoInicial { get; }
        public string DiagnosticoFinal { get; }
        public string ObservacoesCliente { get; }
        public string ObservacoesInternas { get; }
        public string ChecklistEntrada { get; }
        public string TermoAutorizacao { get; }
        public string ChecklistEntrega { get; }
        public string ChecklistSaida { get; }
        public string GarantiaObservacoes { get; }
        public int QuantidadeFotosAntes { get; }
        public int QuantidadeFotosDepois { get; }
        public string AssinaturaClienteUrl { get; }
        public int TempoPrevistoMinutos { get; }
        public int TempoRealMinutos { get; }
        public Guid? OrcamentoId { get; }
        public string TecnicoResumo { get; }
        public string MetodoAprovacao { get; }
        public bool AprovadaCliente { get; }
        public DateTime DataAbertura { get; }
        public DateTime? DataPrevisao { get; }
        public DateTime? GarantiaValidaAte { get; }
        public DateTime PrazoOrdenacao { get; }
        public decimal TotalPecas { get; }
        public decimal TotalServicos { get; }
        public decimal Desconto { get; }
        public decimal Total { get; }
        public decimal CustoTotal { get; }
        public decimal Lucro { get; }
        public bool EhConcluida { get; }
        public int Progresso { get; }
        public string ProgressoResumo { get; }
        public string Subtitulo { get; }
        public Brush StatusBackground { get; }
        public Brush StatusForeground { get; }
        public Brush PrioridadeBackground { get; }
        public Brush PrioridadeForeground { get; }
        public ObservableCollection<OrdemServicoLinhaResumoViewModel> Itens { get; }
        public ObservableCollection<OrdemServicoEventoResumoViewModel> Eventos { get; }
        public bool SemEventos => Eventos.Count == 0;
        public bool SemItens => Itens.Count == 0;

        public string TotalPecasFormatado => TotalPecas.ToString("C");
        public string TotalServicosFormatado => TotalServicos.ToString("C");
        public string DescontoFormatado => Desconto.ToString("C");
        public string TotalFormatado => Total.ToString("C");
        public string LucroFormatado => Lucro.ToString("C");

        public int PrioridadePeso => Prioridade switch
        {
            "Alta" => 3,
            "Normal" => 2,
            _ => 1
        };

        public string PrazoResumo
        {
            get
            {
                if (!DataPrevisao.HasValue)
                    return "Sem previsao";

                if (DataPrevisao.Value.Date == DateTime.Today)
                    return $"Hoje {DataPrevisao.Value:HH:mm}";

                return DataPrevisao.Value.ToString("dd/MM/yyyy");
            }
        }

        public string AprovacaoResumo
        {
            get
            {
                if (AprovadaCliente)
                    return $"Aprovada via {(string.IsNullOrWhiteSpace(MetodoAprovacao) ? "atendimento" : MetodoAprovacao)}";

                if (Status == "Aguardando aprovacao")
                    return "Pendente de retorno do cliente";

                return "Ainda nao aprovada";
            }
        }

        public string GarantiaResumo =>
            GarantiaValidaAte.HasValue
                ? $"Garantia valida ate {GarantiaValidaAte.Value:dd/MM/yyyy}"
                : "Garantia sem data definida";

        public string EvidenciasResumo =>
            $"{QuantidadeFotosAntes} foto(s) antes | {QuantidadeFotosDepois} depois | Assinatura {(string.IsNullOrWhiteSpace(AssinaturaClienteUrl) ? "pendente" : "anexada")}";

        public string TempoResumo =>
            $"Previsto {FormatarMinutos(TempoPrevistoMinutos)} | Real {FormatarMinutos(TempoRealMinutos)}";

        public string OrcamentoResumo =>
            OrcamentoId.HasValue
                ? $"Orcamento vinculado: {OrcamentoId.Value.ToString()[..8]}"
                : "Sem orcamento vinculado";

        public string AssinaturaResumo =>
            string.IsNullOrWhiteSpace(AssinaturaClienteUrl)
                ? "Assinatura do cliente pendente"
                : "Assinatura do cliente anexada";

        private static string FormatarMinutos(int minutos)
        {
            if (minutos <= 0)
            {
                return "nao informado";
            }

            var horas = minutos / 60;
            var resto = minutos % 60;
            return horas > 0 ? $"{horas}h {resto}min" : $"{resto}min";
        }

        private static Brush ThemeBrush(string key, string fallbackHex)
        {
            if (Application.Current?.TryFindResource(key) is Brush brush)
                return brush;

            return CriarBrush(fallbackHex);
        }

        private static SolidColorBrush CriarBrush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }
    }

    public sealed class OrdemServicoLinhaResumoViewModel
    {
        public OrdemServicoLinhaResumoViewModel(OrdemServicoItem item, IReadOnlyDictionary<Guid, Produto> produtos)
        {
            Descricao = item.Descricao;
            Resumo = $"{item.Tipo} - Qtd {item.Quantidade:N2} - Unit. {item.ValorUnitario:C}";
            TotalFormatado = item.Total.ToString("C");

            if (string.Equals(item.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))
            {
                EstoqueResumo = "Servico";
                EstoqueBackground = ThemeBrush("SurfaceAltBrush", "#E5E7EB");
                EstoqueForeground = ThemeBrush("SecondaryTextBrush", "#374151");
                return;
            }

            if (item.EstoqueMovimentado)
            {
                EstoqueResumo = "Baixado do estoque";
                EstoqueBackground = ThemeBrush("SuccessCardBackgroundBrush", "#DCFCE7");
                EstoqueForeground = ThemeBrush("SuccessBrush", "#047857");
                return;
            }

            if (item.ProdutoId.HasValue && produtos.TryGetValue(item.ProdutoId.Value, out var produto))
            {
                var quantidadeNecessaria = Convert.ToInt32(item.Quantidade);
                var estoqueSuficiente = produto.QuantidadeEstoque >= quantidadeNecessaria;

                EstoqueResumo = estoqueSuficiente
                    ? $"{produto.QuantidadeEstoque} disponivel"
                    : $"So {produto.QuantidadeEstoque} no estoque";

                EstoqueBackground = estoqueSuficiente
                    ? ThemeBrush("InfoCardBackgroundBrush", "#DBEAFE")
                    : ThemeBrush("DangerCardBackgroundBrush", "#FEE2E2");

                EstoqueForeground = estoqueSuficiente
                    ? ThemeBrush("InfoBrush", "#1D4ED8")
                    : ThemeBrush("DangerBrush", "#B91C1C");

                return;
            }

            EstoqueResumo = "Sem vinculo de estoque";
            EstoqueBackground = ThemeBrush("WarningCardBackgroundBrush", "#FEF3C7");
            EstoqueForeground = ThemeBrush("WarningBrush", "#92400E");
        }

        public string Descricao { get; }
        public string Resumo { get; }
        public string TotalFormatado { get; }
        public string EstoqueResumo { get; }
        public Brush EstoqueBackground { get; }
        public Brush EstoqueForeground { get; }

        private static Brush ThemeBrush(string key, string fallbackHex)
        {
            if (Application.Current?.TryFindResource(key) is Brush brush)
                return brush;

            return CriarBrush(fallbackHex);
        }

        private static SolidColorBrush CriarBrush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }
    }

    public sealed class OrdemServicoEventoResumoViewModel
    {
        public OrdemServicoEventoResumoViewModel(OrdemServicoEvento evento)
        {
            Titulo = evento.Titulo;
            Descricao = string.IsNullOrWhiteSpace(evento.Descricao) ? "-" : evento.Descricao;
            DataResumo = evento.DataEvento.ToString("dd/MM/yyyy HH:mm");
            TipoResumo = string.IsNullOrWhiteSpace(evento.Tipo) ? "Evento" : evento.Tipo;
            UsuarioResumo = string.IsNullOrWhiteSpace(evento.Usuario) ? "Nao informado" : evento.Usuario;
            MetaResumo = $"{DataResumo} · {UsuarioResumo}";

            IndicadorBackground = evento.Tipo switch
            {
                "Aprovacao" => ThemeBrush("InfoBrush", "#1D4ED8"),
                "Pecas" => ThemeBrush("WarningBrush", "#B45309"),
                "Fluxo" => ThemeBrush("PrimaryBrush", "#C2410C"),
                _ => ThemeBrush("MutedTextBrush", "#6B7280")
            };
        }

        public string Titulo { get; }
        public string Descricao { get; }
        public string DataResumo { get; }
        public string TipoResumo { get; }
        public string UsuarioResumo { get; }
        public string MetaResumo { get; }
        public Brush IndicadorBackground { get; }

        private static Brush ThemeBrush(string key, string fallbackHex)
        {
            if (Application.Current?.TryFindResource(key) is Brush brush)
                return brush;

            return CriarBrush(fallbackHex);
        }

        private static SolidColorBrush CriarBrush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }
    }
}


