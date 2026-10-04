using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.AI;
using PrimoAutoEletrica.Services.AI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class SelecionarOrdemServicoDialog : Window
    {
        private readonly IWorkOrderAiBridgeService _bridgeService;
        private readonly ObservableCollection<AIPartOrServiceProposal> _itens;
        private List<OrdemServico> _todasOrdens = new();
        private OrdemServico? _osSelecionada;

        public int ItensInseridosComSucesso { get; private set; }
        public OrdemServico? OrdemServicoDestino => _osSelecionada;

        public SelecionarOrdemServicoDialog(
            IWorkOrderAiBridgeService bridgeService,
            IEnumerable<AIPartOrServiceProposal> itensPropostos,
            Guid? osPreSelecionadaId = null)
        {
            InitializeComponent();
            _bridgeService = bridgeService ?? throw new ArgumentNullException(nameof(bridgeService));

            _itens = new ObservableCollection<AIPartOrServiceProposal>(
                itensPropostos != null ? itensPropostos.Select(i => new AIPartOrServiceProposal
                {
                    Id = i.Id,
                    Tipo = i.Tipo,
                    Descricao = i.Descricao,
                    CodigoFabricante = i.CodigoFabricante,
                    Quantidade = i.Quantidade <= 0 ? 1 : i.Quantidade,
                    PrecoSugerido = i.PrecoSugerido,
                    SaldoEstoque = i.SaldoEstoque,
                    ProdutoId = i.ProdutoId,
                    TempoEstimadoMinutos = i.TempoEstimadoMinutos,
                    Selecionado = true
                }) : Enumerable.Empty<AIPartOrServiceProposal>()
            );

            ItensListView.ItemsSource = _itens;

            Loaded += async (s, e) =>
            {
                AtualizarResumo();
                await CarregarOrdensServicoAsync(null, osPreSelecionadaId);
            };
        }

        private async Task CarregarOrdensServicoAsync(string? filtro = null, Guid? osPreSelecionadaId = null)
        {
            try
            {
                var ordens = await _bridgeService.ListarOrdensServicoAbertasAsync(filtro);
                _todasOrdens = ordens;
                OrdensServicoDataGrid.ItemsSource = _todasOrdens;

                if (osPreSelecionadaId.HasValue)
                {
                    var match = _todasOrdens.FirstOrDefault(o => o.Id == osPreSelecionadaId.Value);
                    if (match != null)
                    {
                        OrdensServicoDataGrid.SelectedItem = match;
                    }
                }
                else if (_todasOrdens.Count > 0 && OrdensServicoDataGrid.SelectedItem == null)
                {
                    OrdensServicoDataGrid.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar Ordens de Serviço abertas:\n{ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarResumo()
        {
            var selecionados = _itens.Where(i => i.Selecionado).ToList();
            var total = selecionados.Sum(i => i.ValorTotal);
            var itensSemEstoque = selecionados.Where(i => i.Tipo == "Peca" && (!i.SaldoEstoque.HasValue || i.SaldoEstoque.Value <= 0)).ToList();

            QtdItensSelecionadosTextBlock.Text = $"{selecionados.Count} item(ns) selecionado(s)";
            TotalItensTextBlock.Text = total.ToString("C", CultureInfo.CurrentCulture);

            GerarRequisicaoCompraButton.Visibility = itensSemEstoque.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (itensSemEstoque.Count > 0)
            {
                GerarRequisicaoCompraButton.Content = $"📦 Requisitar {itensSemEstoque.Count} Peça(s) em Falta";
            }

            ConfirmarInsercaoButton.IsEnabled = _osSelecionada != null && selecionados.Count > 0;
        }

        private void ItemCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            AtualizarResumo();
        }

        private void OrdensServicoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _osSelecionada = OrdensServicoDataGrid.SelectedItem as OrdemServico;

            if (_osSelecionada != null)
            {
                OsSelecionadaTexto.Text = $"OS #{_osSelecionada.Numero} - {_osSelecionada.ClienteNomeSnapshot} | {_osSelecionada.VeiculoDescricaoSnapshot} ({_osSelecionada.PlacaSnapshot})";
                OsStatusText.Text = _osSelecionada.Status.ToUpperInvariant();
                OsStatusBadge.Visibility = Visibility.Visible;
            }
            else
            {
                OsSelecionadaTexto.Text = "⚠️ Nenhuma Ordem de Serviço selecionada na tabela acima.";
                OsStatusBadge.Visibility = Visibility.Collapsed;
            }

            AtualizarResumo();
        }

        private async void BuscaOSTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var f = BuscaOSTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(f))
            {
                OrdensServicoDataGrid.ItemsSource = _todasOrdens;
            }
            else
            {
                await CarregarOrdensServicoAsync(f);
            }
        }

        private async void RecarregarOS_Click(object sender, RoutedEventArgs e)
        {
            BuscaOSTextBox.Text = string.Empty;
            await CarregarOrdensServicoAsync(null);
        }

        private async void GerarRequisicaoCompraButton_Click(object sender, RoutedEventArgs e)
        {
            var pecasEmFalta = _itens
                .Where(i => i.Selecionado && i.Tipo == "Peca" && (!i.SaldoEstoque.HasValue || i.SaldoEstoque.Value <= 0))
                .ToList();

            if (pecasEmFalta.Count == 0)
            {
                MessageBox.Show("Todas as peças selecionadas possuem estoque disponível!", "Gestão de Compras", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int requisitadas = 0;
            foreach (var p in pecasEmFalta)
            {
                var reqId = await _bridgeService.GerarRequisicaoCompraAsync(p, $"Solicitação gerada via Copilot IA para OS #{_osSelecionada?.Numero ?? "Pendente"}");
                if (reqId.HasValue)
                {
                    requisitadas++;
                }
            }

            MessageBox.Show(
                $"✅ {requisitadas} requisição(ões) de compra foram geradas no módulo de Compras com sucesso!\n\nAs peças foram sinalizadas para cotação e pedido aos distribuidores.",
                "Gestão de Compras & Reposição",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            AtualizarResumo();
        }

        private async void ConfirmarInsercaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_osSelecionada == null)
            {
                MessageBox.Show("Selecione uma Ordem de Serviço de destino na tabela.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selecionados = _itens.Where(i => i.Selecionado).ToList();
            if (selecionados.Count == 0)
            {
                MessageBox.Show("Selecione pelo menos um item para adicionar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ConfirmarInsercaoButton.IsEnabled = false;

                var inseridos = await _bridgeService.InserirItensEmOrdemServicoAsync(_osSelecionada.Id, selecionados);
                ItensInseridosComSucesso = inseridos;

                MessageBox.Show(
                    $"🎉 Sucesso!\n\n{inseridos} item(ns) foram inseridos com sucesso na Ordem de Serviço #{_osSelecionada.Numero} ({_osSelecionada.ClienteNomeSnapshot}).\n\nOs valores e o registro de auditoria foram sincronizados.",
                    "PRIMOX Copilot - Ponte OS Concluída",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inserir itens na OS #{_osSelecionada.Numero}:\n{ex.Message}", "Erro de Gravação", MessageBoxButton.OK, MessageBoxImage.Error);
                ConfirmarInsercaoButton.IsEnabled = true;
            }
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
