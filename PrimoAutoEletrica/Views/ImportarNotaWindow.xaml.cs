using Microsoft.Win32;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.Views
{
    public partial class ImportarNotaWindow : Window
    {
        private readonly NFeService _nfeService;
        private readonly XmlProdutoParser _xmlParser;
        private readonly DatabaseService _databaseService;
        private readonly IFornecedorRepository _fornecedorRepository;
        private readonly ImportacaoRepository _importacaoRepository;
        private readonly AppSessionService _session;
        private NotaFiscalImportada? _notaAtual;
        private string _caminhoArquivo = string.Empty;

        public ImportarNotaWindow()
        {
            InitializeComponent();

            _databaseService = global::PrimoAutoEletrica.App.Database;
            _fornecedorRepository = global::PrimoAutoEletrica.App.Repositories.Fornecedores;
            _nfeService = new NFeService(_databaseService);
            _xmlParser = new XmlProdutoParser();
            _importacaoRepository = new ImportacaoRepository(_databaseService);
            _session = global::PrimoAutoEletrica.App.Session;

            CarregarHistoricoImportacoes();
        }

        public ImportarNotaWindow(Guid usuarioId, string usuarioNome)
            : this()
        {
        }

        private void SelecionarXmlButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                AdicionarLog("Selecao de XML validada em automacao sem abrir seletor nativo.");
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Arquivos XML (*.xml)|*.xml|Todos os arquivos (*.*)|*.*",
                Title = "Selecione o arquivo XML da NF-e"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _caminhoArquivo = openFileDialog.FileName;
                CarregarXml(_caminhoArquivo);
            }
        }

        private void CarregarXml(string caminhoArquivo)
        {
            try
            {
                AdicionarLog($"Carregando arquivo: {Path.GetFileName(caminhoArquivo)}");

                if (!_xmlParser.IsValidNFeXml(caminhoArquivo))
                {
                    MessageBox.Show(
                        "O arquivo selecionado nao e uma NF-e valida.",
                        UiText.T("InvalidFile"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _notaAtual = _nfeService.PrepararImportacao(caminhoArquivo);

                if (_notaAtual.Status == StatusImportacaoNota.Erro)
                {
                    MessageBox.Show(
                        $"Erro ao ler XML: {_notaAtual.Erro}",
                        "Erro de leitura",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                AtualizarInterface(_notaAtual);
                AdicionarLog("XML carregado e conferencia preparada.");
                AdicionarLog($"{_notaAtual.Produtos.Count} item(ns) encontrado(s).");
                AdicionarLog($"Valor total: {_notaAtual.ValorTotal:C}.");

                VerificarFornecedorCadastrado(_notaAtual.Fornecedor);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao carregar XML: {ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                AdicionarLog($"Erro: {ex.Message}");
            }
        }

        private void AtualizarInterface(NotaFiscalImportada nota)
        {
            FornecedorText.Text = nota.Fornecedor.Nome;
            ChaveAcessoText.Text = $"Chave: {ValorOuPadrao(nota.ChaveAcesso)}";
            DataEmissaoText.Text = nota.DataEmissao.ToString("dd/MM/yyyy");
            NumeroNotaText.Text = $"Numero/Série: {ValorOuPadrao(nota.Numero)}/{ValorOuPadrao(nota.Serie)}";
            ValorTotalText.Text = nota.ValorTotal.ToString("C");

            ProdutosDataGrid.ItemsSource = nota.Produtos;
            VincularEventosProdutos(nota.Produtos);
            AtualizarStatusConferencia();
            CarregarHistoricoImportacoes();
        }

        private void AtualizarStatusConferencia()
        {
            if (_notaAtual == null)
            {
                ImportarButton.IsEnabled = false;
                return;
            }

            foreach (var produto in _notaAtual.Produtos)
            {
                AplicarPrevisaoAoProduto(produto);
            }

            ProdutosDataGrid.Items.Refresh();

            var selecionados = _notaAtual.Produtos.Count(produto => produto.SelecionadoParaImportacao && !string.Equals(produto.AcaoPlanejada, AcoesPlanejadas.Ignorar, StringComparison.OrdinalIgnoreCase));
            var pendencias = _notaAtual.Produtos.Count(produto => produto.Status == StatusImportacao.Erro);
            var novos = _notaAtual.Produtos.Count(produto => produto.Status == StatusImportacao.Novo);
            var atualizados = _notaAtual.Produtos.Count(produto => produto.Status == StatusImportacao.Atualizado);
            var ignorados = _notaAtual.Produtos.Count(produto => produto.Status == StatusImportacao.Ignorado);

            TotalProdutosText.Text = _notaAtual.Produtos.Count.ToString(CultureInfo.InvariantCulture);
            SelecionadosText.Text = selecionados.ToString(CultureInfo.InvariantCulture);
            PendenciasText.Text = pendencias.ToString(CultureInfo.InvariantCulture);
            ResultadoPrevistoText.Text = pendencias > 0
                ? "Conferencia com pendencias"
                : "Pronta para importar";
            ResultadoDetalheText.Text = $"{novos} novo(s), {atualizados} atualizacao(oes), {ignorados} ignorado(s).";

            ImportarButton.IsEnabled = selecionados > 0 && pendencias == 0;
        }

        private void AplicarPrevisaoAoProduto(ProdutoImportado produto)
        {
            if (!produto.SelecionadoParaImportacao || string.Equals(produto.AcaoPlanejada, AcoesPlanejadas.Ignorar, StringComparison.OrdinalIgnoreCase))
            {
                produto.Status = StatusImportacao.Ignorado;
                produto.MotivoIgnorado = string.IsNullOrWhiteSpace(produto.MotivoIgnorado)
                    ? "Item ignorado na conferencia."
                    : produto.MotivoIgnorado;
                return;
            }

            var erroBase = ValidarBase(produto);
            if (!string.IsNullOrWhiteSpace(erroBase))
            {
                produto.Status = StatusImportacao.Erro;
                produto.MotivoIgnorado = erroBase;
                return;
            }

            if (string.Equals(produto.AcaoPlanejada, AcoesPlanejadas.AtualizarExistente, StringComparison.OrdinalIgnoreCase))
            {
                if (!produto.ProdutoExistenteId.HasValue && string.IsNullOrWhiteSpace(produto.ProdutoVinculadoReferencia))
                {
                    produto.Status = StatusImportacao.Erro;
                    produto.MotivoIgnorado = "Informe o produto existente para atualizar.";
                    return;
                }

                produto.Status = StatusImportacao.Atualizado;
                produto.MotivoIgnorado = string.Empty;
                return;
            }

            if (string.Equals(produto.AcaoPlanejada, AcoesPlanejadas.CriarNovo, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(produto.CategoriaSugerida))
                {
                    produto.Status = StatusImportacao.Erro;
                    produto.MotivoIgnorado = "Defina a categoria do novo produto.";
                    return;
                }

                if (produto.PrecoVendaSugerido <= 0)
                {
                    produto.Status = StatusImportacao.Erro;
                    produto.MotivoIgnorado = "Defina um preco de venda valido.";
                    return;
                }

                produto.Status = StatusImportacao.Novo;
                produto.MotivoIgnorado = string.Empty;
                return;
            }

            produto.Status = StatusImportacao.Erro;
            produto.MotivoIgnorado = "Acao planejada invalida.";
        }

        private static string ValidarBase(ProdutoImportado produto)
        {
            if (string.IsNullOrWhiteSpace(produto.Nome))
            {
                return "Produto sem descricao valida.";
            }

            if (produto.Quantidade <= 0)
            {
                return "Quantidade invalida.";
            }

            if (produto.ValorUnitario <= 0)
            {
                return "Valor unitario invalido.";
            }

            return string.Empty;
        }

        private void ImportarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_notaAtual == null)
            {
                MessageBox.Show(
                    "Selecione um arquivo XML primeiro.",
                    UiText.T("Warning"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            AtualizarStatusConferencia();
            if (!ImportarButton.IsEnabled)
            {
                MessageBox.Show(
                    "Existem pendencias na conferencia. Revise os itens marcados antes de importar.",
                    "Conferencia pendente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                AdicionarLog("Iniciando importacao confirmada...");
                ImportarButton.IsEnabled = false;
                SelecionarXmlButton.IsEnabled = false;

                var usuarioId = _session.SessionId != Guid.Empty ? _session.SessionId : Guid.NewGuid();
                var usuarioNome = _session.IsAuthenticated ? _session.UserName : "Sistema";

                _notaAtual = _nfeService.ImportarNotaPreparada(_notaAtual, usuarioId, usuarioNome);
                AtualizarInterface(_notaAtual);

                if (_notaAtual.Status == StatusImportacaoNota.Concluida)
                {
                    AdicionarLog("Importacao concluida com sucesso.");

                    MessageBox.Show(
                        $"Importacao concluida com sucesso.\n\n" +
                        $"Novos: {_notaAtual.Produtos.Count(produto => produto.Status == StatusImportacao.Novo)}\n" +
                        $"Atualizados: {_notaAtual.Produtos.Count(produto => produto.Status == StatusImportacao.Atualizado)}\n" +
                        $"Ignorados: {_notaAtual.Produtos.Count(produto => produto.Status == StatusImportacao.Ignorado)}",
                        UiText.T("Success"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                    return;
                }

                if (_notaAtual.Status == StatusImportacaoNota.Parcial)
                {
                    AdicionarLog($"Importacao parcial: {_notaAtual.Erro}");
                    MessageBox.Show(
                        $"Importacao parcial: {_notaAtual.Erro}",
                        UiText.T("Warning"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    AdicionarLog($"Erro na importacao: {_notaAtual.Erro}");
                    MessageBox.Show(
                        $"Erro na importacao: {_notaAtual.Erro}",
                        UiText.T("Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao importar: {ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                AdicionarLog($"Erro: {ex.Message}");
            }
            finally
            {
                SelecionarXmlButton.IsEnabled = true;
                AtualizarStatusConferencia();
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_notaAtual != null && _notaAtual.Status == StatusImportacaoNota.EmProcessamento)
            {
                var identificador = string.IsNullOrWhiteSpace(_notaAtual.Numero) ? _notaAtual.ChaveAcesso : _notaAtual.Numero;
                if (CriticalActionDialogService.ConfirmarCancelamento(
                    this,
                    "importacao NF-e",
                    identificador,
                    $"Fornecedor: {_notaAtual.Fornecedor.Nome}\nProdutos: {_notaAtual.Produtos.Count}\nValor total: {_notaAtual.ValorTotal:C}",
                    "A importacao sera interrompida e a nota ficara marcada como cancelada para esta tentativa."))
                {
                    AdicionarLog("Cancelamento da importacao solicitado pelo usuario.");
                    _nfeService.CancelarImportacao(_notaAtual);
                }
            }

            DialogResult = false;
            Close();
        }

        private void DragDropArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                DragDropArea.Background = new SolidColorBrush(Color.FromRgb(219, 234, 254));
                DragDropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(96, 165, 250));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void DragDropArea_DragLeave(object sender, DragEventArgs e)
        {
            DragDropArea.Background = Application.Current.TryFindResource("SurfaceAltBrush") as Brush ?? Brushes.Transparent;
            DragDropArea.BorderBrush = Application.Current.TryFindResource("BorderBrush") as Brush ?? Brushes.Gray;
        }

        private void DragDropArea_Drop(object sender, DragEventArgs e)
        {
            DragDropArea_DragLeave(sender, e);

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0)
            {
                return;
            }

            var arquivo = files[0];
            if (!arquivo.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Por favor, selecione apenas arquivos XML.",
                    UiText.T("InvalidFile"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _caminhoArquivo = arquivo;
            CarregarXml(_caminhoArquivo);
        }

        private void AplicarSugestoesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_notaAtual == null)
            {
                return;
            }

            var alvo = ObterLinhasSelecionadas();
            if (alvo.Count == 0)
            {
                alvo = _notaAtual.Produtos.ToList();
            }

            foreach (var produto in alvo)
            {
                if (produto.MargemAplicada <= 0)
                {
                    produto.MargemAplicada = 200m;
                }

                produto.RecalcularPrecoVenda();

                if (string.IsNullOrWhiteSpace(produto.AcaoPlanejada))
                {
                    produto.AcaoPlanejada = produto.ProdutoExistenteId.HasValue
                        ? AcoesPlanejadas.AtualizarExistente
                        : AcoesPlanejadas.CriarNovo;
                }

                if (!produto.SelecionadoParaImportacao)
                {
                    produto.SelecionadoParaImportacao = true;
                }

                if (string.IsNullOrWhiteSpace(produto.CategoriaSugerida))
                {
                    produto.CategoriaSugerida = "Pecas Diversas";
                }
            }

            AtualizarStatusConferencia();
            AdicionarLog("Sugestoes reaplicadas aos itens selecionados.");
        }

        private void SelecionarTodosButton_Click(object sender, RoutedEventArgs e)
        {
            if (_notaAtual == null)
            {
                return;
            }

            foreach (var produto in _notaAtual.Produtos)
            {
                produto.SelecionadoParaImportacao = true;
                if (string.Equals(produto.AcaoPlanejada, AcoesPlanejadas.Ignorar, StringComparison.OrdinalIgnoreCase))
                {
                    produto.AcaoPlanejada = produto.ProdutoExistenteId.HasValue
                        ? AcoesPlanejadas.AtualizarExistente
                        : AcoesPlanejadas.CriarNovo;
                }
            }

            AtualizarStatusConferencia();
        }

        private void IgnorarSelecionadosButton_Click(object sender, RoutedEventArgs e)
        {
            if (_notaAtual == null)
            {
                return;
            }

            var selecionados = ObterLinhasSelecionadas();
            if (selecionados.Count == 0)
            {
                return;
            }

            foreach (var produto in selecionados)
            {
                produto.SelecionadoParaImportacao = false;
                produto.AcaoPlanejada = AcoesPlanejadas.Ignorar;
                if (string.IsNullOrWhiteSpace(produto.MotivoIgnorado))
                {
                    produto.MotivoIgnorado = "Item ignorado na conferencia.";
                }
            }

            AtualizarStatusConferencia();
        }

        private void ProdutosDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (e.Row.Item is ProdutoImportado produto)
                {
                    produto.RecalcularPrecoVenda();
                }

                AtualizarStatusConferencia();
            }));
        }

        private void HistoricoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoricoListBox.SelectedItem is not NotaFiscalImportada nota)
            {
                HistoricoDetalhesText.Text = "Selecione um historico para detalhes.";
                return;
            }

            HistoricoDetalhesText.Text =
                $"Nota {ValorOuPadrao(nota.Numero)} / Serie {ValorOuPadrao(nota.Serie)}\n" +
                $"Fornecedor: {ValorOuPadrao(nota.Fornecedor.Nome)}\n" +
                $"Status: {nota.Status}\n" +
                $"Valor: {nota.ValorTotal:C}\n" +
                $"Importada em: {nota.DataImportacao:dd/MM/yyyy HH:mm}\n" +
                $"{(string.IsNullOrWhiteSpace(nota.Erro) ? "Sem observacoes adicionais." : nota.Erro)}";
        }

        private void AdicionarLog(string mensagem)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogsText.Text += $"[{timestamp}] {mensagem}\n";

            if (LogsText.Parent is ScrollViewer parent)
            {
                parent.ScrollToEnd();
            }
        }

        private void VerificarFornecedorCadastrado(FornecedorNota fornecedorNota)
        {
            try
            {
                var fornecedorExistente = !string.IsNullOrWhiteSpace(fornecedorNota.CNPJ)
                    ? _fornecedorRepository.ObterPorCnpj(fornecedorNota.CNPJ)
                    : _fornecedorRepository.ObterPorNomeFantasia(fornecedorNota.Nome);

                if (fornecedorExistente != null)
                {
                    AdicionarLog($"Fornecedor '{fornecedorNota.Nome}' ja esta cadastrado.");
                    return;
                }

                AdicionarLog($"Fornecedor '{fornecedorNota.Nome}' nao esta cadastrado.");

                if (App.IsAutomatedTestMode)
                {
                    AdicionarLog("Cadastro de fornecedor apenas sinalizado em automacao.");
                    return;
                }

                var dialog = new AdicionarFornecedorDialog(fornecedorNota.Nome)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() != true || !dialog.AdicionarFornecedor)
                {
                    AdicionarLog("Fornecedor nao foi adicionado a lista.");
                    return;
                }

                var novoFornecedor = new Fornecedor
                {
                    RazaoSocial = fornecedorNota.Nome,
                    NomeFantasia = fornecedorNota.NomeFantasia,
                    CNPJ = fornecedorNota.CNPJ ?? string.Empty,
                    InscricaoEstadual = fornecedorNota.IE ?? string.Empty,
                    Telefone = fornecedorNota.Telefone ?? string.Empty,
                    CEP = fornecedorNota.CEP ?? string.Empty,
                    Rua = fornecedorNota.Logradouro ?? string.Empty,
                    Numero = fornecedorNota.Numero ?? string.Empty,
                    Complemento = fornecedorNota.Complemento ?? string.Empty,
                    Bairro = fornecedorNota.Bairro ?? string.Empty,
                    Cidade = fornecedorNota.Municipio ?? string.Empty,
                    Estado = fornecedorNota.UF ?? string.Empty,
                    Categoria = "Pecas",
                    Ativo = true,
                    Nota = 5,
                    DataCadastro = DateTime.Now
                };

                _fornecedorRepository.Inserir(novoFornecedor);
                AdicionarLog($"Fornecedor '{fornecedorNota.Nome}' adicionado automaticamente.");
            }
            catch (Exception ex)
            {
                AdicionarLog($"Erro ao verificar fornecedor: {ex.Message}");
            }
        }

        private void CarregarHistoricoImportacoes()
        {
            try
            {
                HistoricoListBox.ItemsSource = _importacaoRepository.ObterHistoricoImportacoes(8);
            }
            catch (Exception ex)
            {
                AdicionarLog($"Falha ao carregar historico de importacoes: {ex.Message}");
            }
        }

        private void VincularEventosProdutos(IEnumerable<ProdutoImportado> produtos)
        {
            foreach (var produto in produtos)
            {
                produto.PropertyChanged -= Produto_PropertyChanged;
                produto.PropertyChanged += Produto_PropertyChanged;
            }
        }

        private void Produto_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ProdutoImportado produto)
            {
                return;
            }

            if (e.PropertyName == nameof(ProdutoImportado.MargemAplicada))
            {
                produto.RecalcularPrecoVenda();
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(AtualizarStatusConferencia));
        }

        private List<ProdutoImportado> ObterLinhasSelecionadas()
        {
            return ProdutosDataGrid.SelectedItems
                .OfType<ProdutoImportado>()
                .ToList();
        }

        private static string ValorOuPadrao(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();
        }
    }
}
