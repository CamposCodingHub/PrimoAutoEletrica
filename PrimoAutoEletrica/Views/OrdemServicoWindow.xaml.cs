using Microsoft.Win32;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrimoAutoEletrica.Views
{
    public partial class OrdemServicoWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly OrcamentoDatabaseService _orcamentoDatabaseService;
        private readonly RecordLockService _recordLockService;
        private readonly OrdemServico? _ordemEmEdicao;
        private readonly List<Cliente> _clientes;
        private readonly List<Funcionario> _funcionarios;
        private readonly List<Produto> _produtos;
        private readonly ObservableCollection<OrdemServicoItemEditor> _itens = new();
        private bool _lockObtido = false;
        private readonly List<string> _fotosAntesTela = new();
        private readonly List<string> _fotosDepoisTela = new();
        private readonly HashSet<string> _fotosAntesOriginais = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _fotosDepoisOriginais = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _fotosAntesRemovidas = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _fotosDepoisRemovidas = new(StringComparer.OrdinalIgnoreCase);
        private Orcamento? _orcamentoSelecionado;
        private string _assinaturaAtual = string.Empty;
        private string _assinaturaOriginal = string.Empty;
        private bool _removerAssinaturaSolicitada;

        public OrdemServico? OrdemSalva { get; private set; }

        public OrdemServicoWindow(
            DatabaseService databaseService,
            OrdemServico? ordem = null,
            Cliente? clientePreSelecionado = null)
        {
            InitializeComponent();

            _databaseService = databaseService;
            _ordemServicoRepository = App.Repositories.OrdensServico;
            _orcamentoDatabaseService = new OrcamentoDatabaseService();
            _recordLockService = new RecordLockService(_databaseService, App.Logger, App.Session, App.Audit);
            _ordemEmEdicao = ordem == null ? null : ClonarOrdem(ordem);
            _clientes = App.Repositories.Clientes.ObterTodos().OrderBy(c => c.Nome).ToList();
            _funcionarios = App.Repositories.Funcionarios.ObterTodos().OrderBy(f => f.Nome).ToList();
            _produtos = App.Repositories.Produtos.ObterTodos()
                .Where(p => p.Ativo)
                .OrderBy(p => p.Nome)
                .ToList();

            ClienteComboBox.ItemsSource = _clientes;
            TecnicoComboBox.ItemsSource = _funcionarios;
            ProdutoComboBox.ItemsSource = _produtos;
            ItensDataGrid.ItemsSource = _itens;

            PrioridadeComboBox.SelectedIndex = 1;
            OrigemComboBox.SelectedIndex = 0;
            StatusComboBox.SelectedIndex = 0;
            MetodoAprovacaoComboBox.SelectedIndex = 0;
            DataPrevisaoDatePicker.SelectedDate = DateTime.Today.AddDays(1);
            TempoPrevistoTextBox.Text = "0";
            TempoRealTextBox.Text = "0";

            Loaded += OrdemServicoWindow_Loaded;
            Closed += OrdemServicoWindow_Closed;

            if (_ordemEmEdicao == null)
            {
                NumeroTextBlock.Text = _ordemServicoRepository.GerarProximoNumero();

                if (clientePreSelecionado != null)
                    SelecionarCliente(clientePreSelecionado.Id);
            }
            else
            {
                CarregarOrdem(_ordemEmEdicao);
            }

            AtualizarBotoes();
            AtualizarTotais();
            AtualizarResumoOrcamento();
            AtualizarGalerias();
            AtualizarPreviewAssinatura();
        }

        private void OrdemServicoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_ordemEmEdicao != null)
            {
                var lockResult = _recordLockService.TryLock("OrdemServico", _ordemEmEdicao.Id.ToString(), _ordemEmEdicao.Numero);
                if (!lockResult.Success)
                {
                    MessageBox.Show(
                        lockResult.Message,
                        UiText.T("RecordBlocked"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Close();
                    return;
                }
                _lockObtido = true;
            }
        }

        private void OrdemServicoWindow_Closed(object? sender, EventArgs e)
        {
            if (_lockObtido && _ordemEmEdicao != null)
            {
                _recordLockService.ReleaseLock("OrdemServico", _ordemEmEdicao.Id.ToString());
            }
        }

        private void ClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClienteComboBox.SelectedItem is not Cliente cliente)
            {
                VeiculoComboBox.ItemsSource = null;
                _orcamentoSelecionado = null;
                AtualizarResumoOrcamento();
                return;
            }

            VeiculoComboBox.ItemsSource = cliente.Veiculos;
            TelefoneClienteTextBox.Text = string.IsNullOrWhiteSpace(cliente.Telefone)
                ? cliente.WhatsApp
                : cliente.Telefone;
            CarregarOrcamentosDisponiveis(cliente.Id);

            if (cliente.Veiculos.Count > 0)
            {
                VeiculoComboBox.SelectedIndex = 0;
                AplicarVeiculo(cliente.Veiculos[0]);
            }
        }

        private void VeiculoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VeiculoComboBox.SelectedItem is Veiculo veiculo)
                AplicarVeiculo(veiculo);
        }

        private void AdicionarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProdutoComboBox.SelectedItem is not Produto produto)
            {
                WindowInteractionHelper.ShowMessage(
                    "Selecione um produto do estoque antes de adicionar a peca.",
                    "Produto nao selecionado",
                    MessageBoxImage.Information,
                    "OrdensServico");
                return;
            }

            if (!int.TryParse(QuantidadeProdutoTextBox.Text, out var quantidade) || quantidade <= 0)
            {
                WindowInteractionHelper.ShowMessage(
                    "Informe uma quantidade inteira valida para a peca.",
                    "Quantidade invalida",
                    MessageBoxImage.Warning,
                    "OrdensServico");
                QuantidadeProdutoTextBox.Focus();
                return;
            }

            var item = new OrdemServicoItemEditor
            {
                ProdutoId = produto.Id,
                Tipo = "Peca",
                Descricao = produto.Nome,
                Quantidade = quantidade,
                ValorUnitario = produto.PrecoVenda,
                CustoUnitario = produto.PrecoCompra,
                Observacoes = string.IsNullOrWhiteSpace(produto.Categoria)
                    ? string.Empty
                    : $"Categoria: {produto.Categoria}"
            };

            AdicionarItem(item);
            QuantidadeProdutoTextBox.Text = "1";
        }

        private void AdicionarServicoButton_Click(object sender, RoutedEventArgs e)
        {
            AdicionarItem(new OrdemServicoItemEditor
            {
                Tipo = "Servico",
                Descricao = "Servico tecnico",
                Quantidade = 1,
                ValorUnitario = 0,
                CustoUnitario = 0
            });
        }

        private void RemoverItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (ItensDataGrid.SelectedItem is not OrdemServicoItemEditor item)
            {
                WindowInteractionHelper.ShowMessage(
                    "Selecione um item para remover.",
                    "Item nao selecionado",
                    MessageBoxImage.Information,
                    "OrdensServico");
                return;
            }

            item.PropertyChanged -= Item_PropertyChanged;
            _itens.Remove(item);
            AtualizarTotais();
        }

        private void ItensDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(AtualizarTotais), DispatcherPriority.Background);
        }

        private void DescontoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AtualizarTotais();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            SalvarOrdem(emitir: false);
        }

        private void EmitirButton_Click(object sender, RoutedEventArgs e)
        {
            SalvarOrdem(emitir: true);
        }

        private void SalvarOrdem(bool emitir)
        {
            if (ClienteComboBox.SelectedItem is not Cliente cliente)
            {
                WindowInteractionHelper.ShowMessage(
                    "Selecione um cliente para a ordem de servico.",
                    UiText.T("ClientRequired"),
                    MessageBoxImage.Warning,
                    "OrdensServico");
                return;
            }

            if (string.IsNullOrWhiteSpace(VeiculoDescricaoTextBox.Text))
            {
                WindowInteractionHelper.ShowMessage(
                    "Informe o veiculo da ordem de servico.",
                    "Veiculo obrigatorio",
                    MessageBoxImage.Warning,
                    "OrdensServico");
                VeiculoDescricaoTextBox.Focus();
                return;
            }

            try
            {
                ValidarDadosComerciaisDaTela();
            }
            catch (InvalidOperationException ex)
            {
                WindowInteractionHelper.ShowMessage(
                    ex.Message,
                    "Validacao da ordem",
                    MessageBoxImage.Warning,
                    "OrdensServico");
                return;
            }

            var ordem = _ordemEmEdicao ?? new OrdemServico();
            var statusAnterior = _ordemEmEdicao?.Status ?? "Rascunho";

            ordem.Numero = NumeroTextBlock.Text.Trim();
            ordem.ClienteId = cliente.Id;
            ordem.VeiculoId = (VeiculoComboBox.SelectedItem as Veiculo)?.Id;
            ordem.TecnicoId = (TecnicoComboBox.SelectedItem as Funcionario)?.Id;
            ordem.ClienteNomeSnapshot = cliente.Nome;
            ordem.TelefoneClienteSnapshot = TelefoneClienteTextBox.Text.Trim();
            ordem.VeiculoDescricaoSnapshot = VeiculoDescricaoTextBox.Text.Trim();
            ordem.PlacaSnapshot = PlacaTextBox.Text.Trim();
            ordem.Prioridade = ObterTextoSelecionado(PrioridadeComboBox, "Normal");
            ordem.Origem = ObterTextoSelecionado(OrigemComboBox, "Balcao");
            ordem.ProblemaRelatado = ProblemaTextBox.Text.Trim();
            ordem.DiagnosticoInicial = DiagnosticoInicialTextBox.Text.Trim();
            ordem.DiagnosticoFinal = DiagnosticoTextBox.Text.Trim();
            ordem.Diagnostico = ordem.DiagnosticoFinal;
            ordem.ObservacoesCliente = ObservacoesClienteTextBox.Text.Trim();
            ordem.ObservacoesInternas = ObservacoesInternasTextBox.Text.Trim();
            ordem.ChecklistEntrada = ChecklistEntradaTextBox.Text.Trim();
            ordem.TermoAutorizacao = string.IsNullOrWhiteSpace(TermoAutorizacaoTextBox.Text)
                ? CriarTermoAutorizacaoPadrao(cliente, ordem)
                : TermoAutorizacaoTextBox.Text.Trim();
            ordem.ChecklistEntrega = ChecklistEntregaTextBox.Text.Trim();
            ordem.ChecklistSaida = ChecklistSaidaTextBox.Text.Trim();
            ordem.GarantiaObservacoes = GarantiaObservacoesTextBox.Text.Trim();
            ordem.AprovadaCliente = ClienteAprovouCheckBox.IsChecked == true;
            ordem.MetodoAprovacao = ObterTextoSelecionado(MetodoAprovacaoComboBox, "Nao definido");
            ordem.DataPrevisao = DataPrevisaoDatePicker.SelectedDate?.Date.AddHours(18);
            ordem.GarantiaValidaAte = GarantiaValidaAteDatePicker.SelectedDate?.Date.AddHours(18);
            ordem.TempoPrevistoMinutos = LerInteiro(TempoPrevistoTextBox.Text);
            ordem.TempoRealMinutos = LerInteiro(TempoRealTextBox.Text);
            ordem.OrcamentoId = _orcamentoSelecionado?.Id;
            ordem.Desconto = LerDecimal(DescontoTextBox.Text);
            ordem.Itens = CriarItensDaTela();
            PersistirMidias(ordem);

            var statusSelecionado = ObterTextoSelecionado(StatusComboBox, "Rascunho");

            if (_ordemEmEdicao == null && !emitir)
            {
                ordem.Status = "Rascunho";
            }
            else if (_ordemEmEdicao == null && emitir)
            {
                ordem.Status = ordem.AprovadaCliente ? "Aprovada" : "Aguardando aprovacao";
            }
            else if (emitir && string.Equals(statusSelecionado, "Rascunho", StringComparison.OrdinalIgnoreCase))
            {
                ordem.Status = ordem.AprovadaCliente ? "Aprovada" : "Aguardando aprovacao";
            }
            else
            {
                ordem.Status = statusSelecionado;
            }

            ordem.Eventos = (_ordemEmEdicao?.Eventos ?? new List<OrdemServicoEvento>())
                .Select(CloneEvento)
                .ToList();

            RegistrarEventoDeSalvar(ordem, emitir, statusAnterior);

            try
            {
                if (_ordemEmEdicao == null)
                    _ordemServicoRepository.Inserir(ordem);
                else
                    _ordemServicoRepository.Atualizar(ordem);

                OrdemSalva = _ordemServicoRepository.ObterPorId(ordem.Id) ?? ordem;
                WindowInteractionHelper.CloseWithDialogResult(this, true, "OrdensServico");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao salvar a ordem de servico:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxImage.Error,
                    "OrdensServico",
                    ex);
            }
        }

        private void SelecionarCliente(Guid clienteId)
        {
            var cliente = _clientes.FirstOrDefault(c => c.Id == clienteId);
            if (cliente != null)
                ClienteComboBox.SelectedItem = cliente;
        }

        private void CarregarOrdem(OrdemServico ordem)
        {
            TituloTextBlock.Text = "Editar Dossiê Técnico";
            NumeroTextBlock.Text = ordem.Numero;
            SelecionarCliente(ordem.ClienteId);

            if (ordem.TecnicoId.HasValue)
            {
                TecnicoComboBox.SelectedItem = _funcionarios
                    .FirstOrDefault(f => f.Id == ordem.TecnicoId.Value);
            }

            SelecionarComboItem(PrioridadeComboBox, ordem.Prioridade);
            SelecionarComboItem(OrigemComboBox, ordem.Origem);
            SelecionarComboItem(StatusComboBox, ordem.Status);
            SelecionarComboItem(MetodoAprovacaoComboBox, string.IsNullOrWhiteSpace(ordem.MetodoAprovacao) ? "Nao definido" : ordem.MetodoAprovacao);

            ProblemaTextBox.Text = ordem.ProblemaRelatado;
            DiagnosticoInicialTextBox.Text = string.IsNullOrWhiteSpace(ordem.DiagnosticoInicial) ? ordem.Diagnostico : ordem.DiagnosticoInicial;
            DiagnosticoTextBox.Text = string.IsNullOrWhiteSpace(ordem.DiagnosticoFinal) ? ordem.Diagnostico : ordem.DiagnosticoFinal;
            ObservacoesClienteTextBox.Text = ordem.ObservacoesCliente;
            ObservacoesInternasTextBox.Text = ordem.ObservacoesInternas;
            ChecklistEntradaTextBox.Text = ordem.ChecklistEntrada;
            TermoAutorizacaoTextBox.Text = ordem.TermoAutorizacao;
            ChecklistEntregaTextBox.Text = ordem.ChecklistEntrega;
            ChecklistSaidaTextBox.Text = string.IsNullOrWhiteSpace(ordem.ChecklistSaida)
                ? ordem.ChecklistEntrega
                : ordem.ChecklistSaida;
            GarantiaObservacoesTextBox.Text = ordem.GarantiaObservacoes;
            ClienteAprovouCheckBox.IsChecked = ordem.AprovadaCliente;
            DataPrevisaoDatePicker.SelectedDate = ordem.DataPrevisao?.Date;
            GarantiaValidaAteDatePicker.SelectedDate = ordem.GarantiaValidaAte?.Date;
            TelefoneClienteTextBox.Text = ordem.TelefoneClienteSnapshot;
            VeiculoDescricaoTextBox.Text = ordem.VeiculoDescricaoSnapshot;
            PlacaTextBox.Text = ordem.PlacaSnapshot;
            DescontoTextBox.Text = ordem.Desconto.ToString("N2");
            TempoPrevistoTextBox.Text = ordem.TempoPrevistoMinutos.ToString(CultureInfo.InvariantCulture);
            TempoRealTextBox.Text = ordem.TempoRealMinutos.ToString(CultureInfo.InvariantCulture);

            _fotosAntesTela.Clear();
            _fotosAntesTela.AddRange(OrdemServicoMediaService.DeserializePaths(ordem.FotosAntes));
            _fotosDepoisTela.Clear();
            _fotosDepoisTela.AddRange(OrdemServicoMediaService.DeserializePaths(ordem.FotosDepois));
            _fotosAntesOriginais.Clear();
            _fotosDepoisOriginais.Clear();
            foreach (var foto in _fotosAntesTela)
                _fotosAntesOriginais.Add(foto);
            foreach (var foto in _fotosDepoisTela)
                _fotosDepoisOriginais.Add(foto);
            _fotosAntesRemovidas.Clear();
            _fotosDepoisRemovidas.Clear();

            _assinaturaOriginal = ordem.AssinaturaClienteUrl ?? string.Empty;
            _assinaturaAtual = _assinaturaOriginal;
            _removerAssinaturaSolicitada = false;

            if (ClienteComboBox.SelectedItem is Cliente cliente && ordem.VeiculoId.HasValue)
            {
                var veiculo = cliente.Veiculos.FirstOrDefault(v => v.Id == ordem.VeiculoId.Value);
                if (veiculo != null)
                    VeiculoComboBox.SelectedItem = veiculo;
            }

            if (ordem.OrcamentoId.HasValue)
            {
                _orcamentoSelecionado = _orcamentoDatabaseService.ObterTodosOrcamentos()
                    .FirstOrDefault(o => o.Id == ordem.OrcamentoId.Value);
            }

            AtualizarResumoOrcamento();
            AtualizarGalerias();
            AtualizarPreviewAssinatura();

            foreach (var item in ordem.Itens)
            {
                AdicionarItem(new OrdemServicoItemEditor
                {
                    Id = item.Id,
                    ProdutoId = item.ProdutoId,
                    Tipo = item.Tipo,
                    Descricao = item.Descricao,
                    Quantidade = item.Quantidade,
                    ValorUnitario = item.ValorUnitario,
                    CustoUnitario = item.CustoUnitario,
                    Observacoes = item.Observacoes,
                    EstoqueMovimentado = item.EstoqueMovimentado
                });
            }
        }

        private void AplicarVeiculo(Veiculo veiculo)
        {
            VeiculoDescricaoTextBox.Text = $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano}".Trim();
            PlacaTextBox.Text = veiculo.Placa;
        }

        private void CarregarOrcamentosDisponiveis(Guid clienteId)
        {
            var orcamentos = _orcamentoDatabaseService.ObterTodosOrcamentos()
                .Where(o => o.ClienteId == clienteId)
                .OrderByDescending(o => o.DataCriacao)
                .ToList();

            if (_orcamentoSelecionado != null && _orcamentoSelecionado.ClienteId != clienteId)
            {
                _orcamentoSelecionado = null;
            }

            if (_orcamentoSelecionado == null)
            {
                _orcamentoSelecionado = orcamentos.FirstOrDefault(o =>
                    string.Equals(o.Status, "Aprovado", StringComparison.OrdinalIgnoreCase));
            }

            AtualizarResumoOrcamento();
        }

        private void SelecionarOrcamentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClienteComboBox.SelectedItem is not Cliente cliente)
            {
                WindowInteractionHelper.ShowMessage(
                    "Selecione o cliente antes de vincular um orcamento.",
                    UiText.T("ClientRequired"),
                    MessageBoxImage.Information,
                    "OrdensServico");
                return;
            }

            var orcamentos = _orcamentoDatabaseService.ObterTodosOrcamentos()
                .Where(o => o.ClienteId == cliente.Id)
                .OrderByDescending(o => o.DataCriacao)
                .ToList();

            if (orcamentos.Count == 0)
            {
                WindowInteractionHelper.ShowMessage(
                    "Nao existem orcamentos para este cliente.",
                    "Sem orcamentos",
                    MessageBoxImage.Information,
                    "OrdensServico");
                return;
            }

            var janela = new SelecionarOrcamentoWindow(orcamentos);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (janela.ShowDialog() == true && janela.OrcamentoSelecionado != null)
            {
                _orcamentoSelecionado = janela.OrcamentoSelecionado;
                AtualizarResumoOrcamento();
            }
        }

        private void LimparOrcamentoButton_Click(object sender, RoutedEventArgs e)
        {
            _orcamentoSelecionado = null;
            AtualizarResumoOrcamento();
        }

        private void AtualizarResumoOrcamento()
        {
            if (OrcamentoResumoTextBlock == null)
            {
                return;
            }

            OrcamentoResumoTextBlock.Text = _orcamentoSelecionado == null
                ? "Nenhum orcamento vinculado."
                : $"{_orcamentoSelecionado.Numero} | {_orcamentoSelecionado.Status} | {_orcamentoSelecionado.Total:C}";
        }

        private void AdicionarFotoAntesButton_Click(object sender, RoutedEventArgs e)
        {
            AdicionarFotosNaGaleria(_fotosAntesTela, _fotosAntesRemovidas);
            AtualizarGalerias();
        }

        private void LimparFotosAntesButton_Click(object sender, RoutedEventArgs e)
        {
            MarcarFotosComoRemovidas(_fotosAntesTela, _fotosAntesOriginais, _fotosAntesRemovidas);
            _fotosAntesTela.Clear();
            AtualizarGalerias();
        }

        private void AdicionarFotoDepoisButton_Click(object sender, RoutedEventArgs e)
        {
            AdicionarFotosNaGaleria(_fotosDepoisTela, _fotosDepoisRemovidas);
            AtualizarGalerias();
        }

        private void LimparFotosDepoisButton_Click(object sender, RoutedEventArgs e)
        {
            MarcarFotosComoRemovidas(_fotosDepoisTela, _fotosDepoisOriginais, _fotosDepoisRemovidas);
            _fotosDepoisTela.Clear();
            AtualizarGalerias();
        }

        private void AdicionarFotosNaGaleria(List<string> destino, HashSet<string> removidas)
        {
            if (App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo("Selecao de fotos da OS ignorada em automacao.");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = OrdemServicoMediaService.SupportedImageFilter,
                CheckFileExists = true,
                Multiselect = true,
                Title = "Selecionar imagens da OS"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            foreach (var arquivo in dialog.FileNames.Where(OrdemServicoMediaService.IsSupportedImageFile))
            {
                if (!destino.Any(path => string.Equals(path, arquivo, StringComparison.OrdinalIgnoreCase)))
                {
                    destino.Add(arquivo);
                }

                removidas.Remove(arquivo);
            }
        }

        private static void MarcarFotosComoRemovidas(IEnumerable<string> fotosAtuais, HashSet<string> originais, HashSet<string> removidas)
        {
            foreach (var foto in fotosAtuais.Where(f => originais.Contains(f)))
            {
                removidas.Add(foto);
            }
        }

        private void AtualizarGalerias()
        {
            AtualizarGaleria(FotosAntesPanel, _fotosAntesTela, _fotosAntesOriginais, _fotosAntesRemovidas);
            AtualizarGaleria(FotosDepoisPanel, _fotosDepoisTela, _fotosDepoisOriginais, _fotosDepoisRemovidas);
        }

        private void AtualizarGaleria(WrapPanel panel, List<string> fotos, HashSet<string> originais, HashSet<string> removidas)
        {
            panel.Children.Clear();

            if (fotos.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Nenhuma imagem carregada.",
                    Foreground = (Brush)FindResource("MutedTextBrush"),
                    Margin = new Thickness(0, 6, 0, 0)
                });
                return;
            }

            foreach (var foto in fotos.ToList())
            {
                var preview = OrdemServicoMediaService.TryCreatePreviewSource(foto);
                var card = new Border
                {
                    Width = 128,
                    Margin = new Thickness(0, 0, 10, 10),
                    Padding = new Thickness(8),
                    Background = (Brush)FindResource("CardBackgroundBrush"),
                    BorderBrush = (Brush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12)
                };

                var stack = new StackPanel();
                stack.Children.Add(new Border
                {
                    Height = 86,
                    CornerRadius = new CornerRadius(10),
                    Background = (Brush)FindResource("SurfaceAltBrush"),
                    Child = preview == null
                        ? new TextBlock
                        {
                            Text = "Sem preview",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = (Brush)FindResource("MutedTextBrush")
                        }
                        : new Image
                        {
                            Source = preview,
                            Stretch = Stretch.UniformToFill
                        }
                });
                stack.Children.Add(new TextBlock
                {
                    Text = Path.GetFileName(foto),
                    Margin = new Thickness(0, 8, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)FindResource("PrimaryTextBrush"),
                    FontSize = 11,
                    MaxHeight = 32
                });

                var remover = new Button
                {
                    Content = "Remover",
                    Margin = new Thickness(0, 8, 0, 0),
                    Style = (Style)FindResource("ModalSecondaryButton"),
                    Tag = foto
                };
                remover.Click += (_, _) =>
                {
                    if (originais.Contains(foto))
                    {
                        removidas.Add(foto);
                    }

                    fotos.Remove(foto);
                    AtualizarGalerias();
                };
                stack.Children.Add(remover);
                card.Child = stack;
                panel.Children.Add(card);
            }
        }

        private void SelecionarAssinaturaButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo("Selecao de assinatura da OS ignorada em automacao.");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = OrdemServicoMediaService.SupportedImageFilter,
                CheckFileExists = true,
                Multiselect = false,
                Title = "Selecionar assinatura do cliente"
            };

            if (dialog.ShowDialog(this) == true)
            {
                _assinaturaAtual = dialog.FileName;
                _removerAssinaturaSolicitada = false;
                AtualizarPreviewAssinatura();
            }
        }

        private void CapturarAssinaturaButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contexto = string.IsNullOrWhiteSpace(NumeroTextBlock.Text)
                    ? "OS"
                    : $"OS-{NumeroTextBlock.Text.Replace("/", "-")}";
                var janela = new AssinaturaDigitalWindow(contexto);
                WindowOwnerHelper.ConfigureOwner(janela, this);

                if (janela.ShowDialog() == true && !string.IsNullOrWhiteSpace(janela.SignaturePath))
                {
                    _assinaturaAtual = janela.SignaturePath;
                    _removerAssinaturaSolicitada = false;
                    AtualizarPreviewAssinatura();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nao foi possivel capturar a assinatura: {ex.Message}",
                    UiText.T("DigitalSignature"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public void CarregarMidiasParaAutomacao(
            IEnumerable<string>? fotosAntes,
            IEnumerable<string>? fotosDepois,
            string? assinatura)
        {
            if (!App.IsAutomatedTestMode)
            {
                throw new InvalidOperationException("Carga automatizada de midias de OS disponivel apenas em modo de teste.");
            }

            CarregarFotosAutomacao(_fotosAntesTela, fotosAntes, "fotos antes");
            CarregarFotosAutomacao(_fotosDepoisTela, fotosDepois, "fotos depois");

            if (!string.IsNullOrWhiteSpace(assinatura))
            {
                if (!File.Exists(assinatura) || !OrdemServicoMediaService.IsSupportedImageFile(assinatura))
                {
                    throw new InvalidOperationException("Assinatura automatizada da OS nao existe ou usa extensao nao suportada.");
                }

                _assinaturaAtual = assinatura;
                _removerAssinaturaSolicitada = false;
            }

            AtualizarGalerias();
            AtualizarPreviewAssinatura();
        }

        private static void CarregarFotosAutomacao(List<string> destino, IEnumerable<string>? caminhos, string descricao)
        {
            if (caminhos == null)
            {
                return;
            }

            foreach (var caminho in caminhos.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                if (!File.Exists(caminho) || !OrdemServicoMediaService.IsSupportedImageFile(caminho))
                {
                    throw new InvalidOperationException($"Uma das {descricao} da OS nao existe ou usa extensao nao suportada.");
                }

                if (!destino.Any(path => string.Equals(path, caminho, StringComparison.OrdinalIgnoreCase)))
                {
                    destino.Add(caminho);
                }
            }
        }

        private void RemoverAssinaturaButton_Click(object sender, RoutedEventArgs e)
        {
            _assinaturaAtual = string.Empty;
            _removerAssinaturaSolicitada = true;
            AtualizarPreviewAssinatura();
        }

        private void AtualizarPreviewAssinatura()
        {
            var preview = OrdemServicoMediaService.TryCreatePreviewSource(_assinaturaAtual);
            AssinaturaPreviewImage.Source = preview;
            AssinaturaPreviewImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            AssinaturaPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;
            AssinaturaStatusTextBlock.Text = preview == null
                ? "Anexe a assinatura ou aprovacao visual do cliente quando disponivel."
                : Path.GetFileName(_assinaturaAtual);
        }

        private void PersistirMidias(OrdemServico ordem)
        {
            ordem.FotosAntes = OrdemServicoMediaService.SerializePaths(
                PersistirGaleriaFotos(_fotosAntesTela, _fotosAntesOriginais, _fotosAntesRemovidas, ordem.Id, ordem.Numero, "antes"));
            ordem.FotosDepois = OrdemServicoMediaService.SerializePaths(
                PersistirGaleriaFotos(_fotosDepoisTela, _fotosDepoisOriginais, _fotosDepoisRemovidas, ordem.Id, ordem.Numero, "depois"));

            if (_removerAssinaturaSolicitada)
            {
                OrdemServicoMediaService.DeleteManagedImageIfOwned(_assinaturaOriginal);
                ordem.AssinaturaClienteUrl = string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(_assinaturaAtual))
            {
                if (string.Equals(_assinaturaAtual, _assinaturaOriginal, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(OrdemServicoMediaService.ResolveExistingPath(_assinaturaAtual)))
                {
                    ordem.AssinaturaClienteUrl = _assinaturaAtual;
                }
                else
                {
                    var caminho = OrdemServicoMediaService.PersistSelectedImage(_assinaturaAtual, ordem.Id, ordem.Numero, "assinatura");
                    if (!string.IsNullOrWhiteSpace(_assinaturaOriginal))
                    {
                        OrdemServicoMediaService.DeleteManagedImageIfOwned(_assinaturaOriginal);
                    }

                    ordem.AssinaturaClienteUrl = caminho;
                }
            }
            else
            {
                ordem.AssinaturaClienteUrl = _assinaturaOriginal;
            }
        }

        private static List<string> PersistirGaleriaFotos(
            List<string> fotosTela,
            HashSet<string> originais,
            HashSet<string> removidas,
            Guid ordemId,
            string numeroOrdem,
            string categoria)
        {
            var resultado = new List<string>();

            foreach (var foto in fotosTela)
            {
                if (originais.Contains(foto) && !string.IsNullOrWhiteSpace(OrdemServicoMediaService.ResolveExistingPath(foto)))
                {
                    resultado.Add(foto);
                    continue;
                }

                resultado.Add(OrdemServicoMediaService.PersistSelectedImage(foto, ordemId, numeroOrdem, categoria));
            }

            foreach (var fotoRemovida in originais.Where(foto => !resultado.Contains(foto, StringComparer.OrdinalIgnoreCase)).Concat(removidas).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                OrdemServicoMediaService.DeleteManagedImageIfOwned(fotoRemovida);
            }

            return resultado;
        }

        private void AdicionarItem(OrdemServicoItemEditor item)
        {
            item.PropertyChanged += Item_PropertyChanged;
            _itens.Add(item);
            AtualizarTotais();
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            AtualizarTotais();
        }

        private void AtualizarTotais()
        {
            if (PecasTotalTextBlock == null ||
                ServicosTotalTextBlock == null ||
                CustoTotalTextBlock == null ||
                TotalGeralTextBlock == null)
            {
                return;
            }

            var pecas = _itens
                .Where(i => string.Equals(i.Tipo, "Peca", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Total);

            var servicos = _itens
                .Where(i => string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Total);

            var custo = _itens.Sum(i => i.CustoTotal);
            var desconto = LerDecimal(DescontoTextBox.Text);
            var total = Math.Max(0, pecas + servicos - desconto);

            PecasTotalTextBlock.Text = pecas.ToString("C");
            ServicosTotalTextBlock.Text = servicos.ToString("C");
            CustoTotalTextBlock.Text = custo.ToString("C");
            TotalGeralTextBlock.Text = total.ToString("C");
        }

        private void ValidarDadosComerciaisDaTela()
        {
            var subtotal = _itens.Sum(i => i.Total);
            var desconto = LerDecimal(DescontoTextBox.Text);
            var referenciaAbertura = _ordemEmEdicao?.DataAbertura ?? DateTime.Now;

            ComercialValidationHelper.GarantirDescontoValido(desconto, subtotal, "O desconto da ordem de servico");
            ComercialValidationHelper.GarantirDataFinalNaoAnterior(referenciaAbertura, DataPrevisaoDatePicker.SelectedDate, "a data de abertura", "A data de previsao");

            foreach (var item in _itens)
            {
                ComercialValidationHelper.GarantirTextoObrigatorio(item.Descricao, "a descricao do item da ordem de servico");
                ComercialValidationHelper.GarantirQuantidadePositiva(item.Quantidade, $"a quantidade do item '{item.Descricao}'");
                ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.ValorUnitario, $"o valor unitario do item '{item.Descricao}'");
                ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.CustoUnitario, $"o custo do item '{item.Descricao}'");
            }

            if (LerInteiro(TempoPrevistoTextBox.Text) < 0 || LerInteiro(TempoRealTextBox.Text) < 0)
            {
                throw new InvalidOperationException("Informe tempos validos em minutos.");
            }
        }

        private List<OrdemServicoItem> CriarItensDaTela()
        {
            var itens = new List<OrdemServicoItem>();

            for (var index = 0; index < _itens.Count; index++)
            {
                var editor = _itens[index];

                if (string.IsNullOrWhiteSpace(editor.Descricao))
                    throw new InvalidOperationException("Existe item sem descricao na ordem de servico.");

                if (editor.Quantidade <= 0)
                    throw new InvalidOperationException($"O item '{editor.Descricao}' precisa ter quantidade maior que zero.");

                if (editor.ProdutoId.HasValue &&
                    string.Equals(editor.Tipo, "Peca", StringComparison.OrdinalIgnoreCase) &&
                    decimal.Truncate(editor.Quantidade) != editor.Quantidade)
                {
                    throw new InvalidOperationException(
                        $"O item '{editor.Descricao}' usa estoque e precisa de quantidade inteira.");
                }

                itens.Add(new OrdemServicoItem
                {
                    Id = editor.Id == Guid.Empty ? Guid.NewGuid() : editor.Id,
                    ProdutoId = editor.ProdutoId,
                    Tipo = editor.Tipo,
                    Descricao = editor.Descricao.Trim(),
                    Quantidade = editor.Quantidade,
                    ValorUnitario = editor.ValorUnitario,
                    CustoUnitario = editor.CustoUnitario,
                    Observacoes = editor.Observacoes?.Trim() ?? string.Empty,
                    OrdemExibicao = index + 1,
                    EstoqueMovimentado = editor.EstoqueMovimentado
                });
            }

            return itens;
        }

        private void AtualizarBotoes()
        {
            if (_ordemEmEdicao == null)
                return;

            SalvarButton.Content = "Salvar alteracoes";

            if (!string.Equals(_ordemEmEdicao.Status, "Rascunho", StringComparison.OrdinalIgnoreCase))
                EmitirButton.Visibility = Visibility.Collapsed;
        }

        private void RegistrarEventoDeSalvar(OrdemServico ordem, bool emitir, string statusAnterior)
        {
            string titulo;
            string descricao;

            if (_ordemEmEdicao == null && emitir)
            {
                titulo = "OS emitida";
                descricao = $"OS emitida com status inicial '{ordem.Status}'.";
            }
            else if (_ordemEmEdicao == null)
            {
                titulo = "OS salva em rascunho";
                descricao = "OS registrada como rascunho para revisao posterior.";
            }
            else if (!string.Equals(statusAnterior, ordem.Status, StringComparison.OrdinalIgnoreCase))
            {
                titulo = "Status alterado";
                descricao = $"Status alterado de '{statusAnterior}' para '{ordem.Status}'.";
            }
            else
            {
                titulo = "OS atualizada";
                descricao = "Dados da ordem de servico foram atualizados.";
            }

            ordem.Eventos.Insert(0, new OrdemServicoEvento
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = ordem.Id,
                DataEvento = DateTime.Now,
                Titulo = titulo,
                Descricao = descricao,
                Tipo = "Operacao",
                Usuario = "Sistema"
            });
        }

        private static string ObterTextoSelecionado(ComboBox comboBox, string fallback)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? fallback;

            return comboBox.Text ?? fallback;
        }

        private static string CriarTermoAutorizacaoPadrao(Cliente cliente, OrdemServico ordem)
        {
            var veiculo = string.IsNullOrWhiteSpace(ordem.VeiculoDescricaoSnapshot)
                ? "veiculo informado na ordem de servico"
                : ordem.VeiculoDescricaoSnapshot;

            return $"Autorizo a Primo Auto Eletrica a diagnosticar e executar os servicos descritos nesta OS para {veiculo}, incluindo testes eletricos, aplicacao de pecas aprovadas e registro de evidencias. Cliente: {cliente.Nome}. OS: {ordem.Numero}.";
        }

        private static void SelecionarComboItem(ComboBox comboBox, string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return;

            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Content?.ToString(), texto, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static decimal LerDecimal(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return 0m;

            return decimal.TryParse(
                texto,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.CurrentCulture,
                out var valor)
                ? Math.Max(0, valor)
                : 0m;
        }

        private static int LerInteiro(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return 0;

            return int.TryParse(texto, NumberStyles.Integer, CultureInfo.CurrentCulture, out var valor)
                ? Math.Max(0, valor)
                : 0;
        }

        private static OrdemServico ClonarOrdem(OrdemServico origem)
        {
            return new OrdemServico
            {
                Id = origem.Id,
                Numero = origem.Numero,
                ClienteId = origem.ClienteId,
                VeiculoId = origem.VeiculoId,
                TecnicoId = origem.TecnicoId,
                ClienteNomeSnapshot = origem.ClienteNomeSnapshot,
                TelefoneClienteSnapshot = origem.TelefoneClienteSnapshot,
                VeiculoDescricaoSnapshot = origem.VeiculoDescricaoSnapshot,
                PlacaSnapshot = origem.PlacaSnapshot,
                Status = origem.Status,
                Prioridade = origem.Prioridade,
                Origem = origem.Origem,
                ProblemaRelatado = origem.ProblemaRelatado,
                Diagnostico = origem.Diagnostico,
                DiagnosticoInicial = origem.DiagnosticoInicial,
                DiagnosticoFinal = origem.DiagnosticoFinal,
                ChecklistEntrada = origem.ChecklistEntrada,
                TermoAutorizacao = origem.TermoAutorizacao,
                ChecklistEntrega = origem.ChecklistEntrega,
                ChecklistSaida = origem.ChecklistSaida,
                FotosAntes = origem.FotosAntes,
                FotosDepois = origem.FotosDepois,
                GarantiaObservacoes = origem.GarantiaObservacoes,
                AssinaturaClienteUrl = origem.AssinaturaClienteUrl,
                ObservacoesInternas = origem.ObservacoesInternas,
                ObservacoesCliente = origem.ObservacoesCliente,
                AprovadaCliente = origem.AprovadaCliente,
                MetodoAprovacao = origem.MetodoAprovacao,
                DataAbertura = origem.DataAbertura,
                DataPrevisao = origem.DataPrevisao,
                DataAprovacao = origem.DataAprovacao,
                DataInicio = origem.DataInicio,
                DataConclusao = origem.DataConclusao,
                DataEntrega = origem.DataEntrega,
                GarantiaValidaAte = origem.GarantiaValidaAte,
                TempoPrevistoMinutos = origem.TempoPrevistoMinutos,
                TempoRealMinutos = origem.TempoRealMinutos,
                OrcamentoId = origem.OrcamentoId,
                ValorMaoObra = origem.ValorMaoObra,
                Desconto = origem.Desconto,
                Ativo = origem.Ativo,
                Itens = origem.Itens.Select(i => new OrdemServicoItem
                {
                    Id = i.Id,
                    OrdemServicoId = i.OrdemServicoId,
                    ProdutoId = i.ProdutoId,
                    Tipo = i.Tipo,
                    Descricao = i.Descricao,
                    Quantidade = i.Quantidade,
                    ValorUnitario = i.ValorUnitario,
                    CustoUnitario = i.CustoUnitario,
                    Observacoes = i.Observacoes,
                    OrdemExibicao = i.OrdemExibicao,
                    EstoqueMovimentado = i.EstoqueMovimentado
                }).ToList(),
                Eventos = origem.Eventos.Select(CloneEvento).ToList()
            };
        }

        private static OrdemServicoEvento CloneEvento(OrdemServicoEvento evento)
        {
            return new OrdemServicoEvento
            {
                Id = evento.Id,
                OrdemServicoId = evento.OrdemServicoId,
                DataEvento = evento.DataEvento,
                Titulo = evento.Titulo,
                Descricao = evento.Descricao,
                Tipo = evento.Tipo,
                Usuario = evento.Usuario
            };
        }
    }

    public sealed class OrdemServicoItemEditor : INotifyPropertyChanged
    {
        private string _tipo = "Servico";
        private string _descricao = string.Empty;
        private decimal _quantidade = 1m;
        private decimal _valorUnitario;
        private decimal _custoUnitario;
        private string _observacoes = string.Empty;

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ProdutoId { get; set; }
        public bool EstoqueMovimentado { get; set; }

        public string Tipo
        {
            get => _tipo;
            set
            {
                if (_tipo == value)
                    return;

                _tipo = value;
                OnPropertyChanged();
            }
        }

        public string Descricao
        {
            get => _descricao;
            set
            {
                if (_descricao == value)
                    return;

                _descricao = value;
                OnPropertyChanged();
            }
        }

        public decimal Quantidade
        {
            get => _quantidade;
            set
            {
                if (_quantidade == value)
                    return;

                _quantidade = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(CustoTotal));
                OnPropertyChanged(nameof(TotalFormatado));
            }
        }

        public decimal ValorUnitario
        {
            get => _valorUnitario;
            set
            {
                if (_valorUnitario == value)
                    return;

                _valorUnitario = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(TotalFormatado));
            }
        }

        public decimal CustoUnitario
        {
            get => _custoUnitario;
            set
            {
                if (_custoUnitario == value)
                    return;

                _custoUnitario = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustoTotal));
            }
        }

        public string Observacoes
        {
            get => _observacoes;
            set
            {
                if (_observacoes == value)
                    return;

                _observacoes = value;
                OnPropertyChanged();
            }
        }

        public decimal Total => Quantidade * ValorUnitario;
        public decimal CustoTotal => Quantidade * CustoUnitario;
        public string TotalFormatado => Total.ToString("C");

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
