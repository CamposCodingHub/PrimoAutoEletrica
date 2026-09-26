using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.UserControls
{
    public partial class NecessidadesCompraControl : UserControl
    {
        private readonly IPurchaseService _purchaseService;
        private List<PurchaseRequest> _todasRequisicoes = new();
        private List<Produto> _todasSugestoes = new();
        private bool _isInitialized = false;

        public NecessidadesCompraControl()
        {
            InitializeComponent();
            _purchaseService = new PurchaseService();
            _isInitialized = true;

            Loaded += async (s, e) => await CarregarDadosAsync();
        }

        public async Task CarregarDadosAsync()
        {
            try
            {
                var reqs = await _purchaseService.ListarRequisicoesAsync();
                _todasRequisicoes = reqs.ToList();

                var sugestoes = await _purchaseService.ObterSugestoesEstoqueMinimoAsync();
                _todasSugestoes = sugestoes.ToList();

                AtualizarIndicadores();
                AplicarFiltrosRequisicoes();
                SugestoesDataGrid.ItemsSource = _todasSugestoes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados de compras: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarIndicadores()
        {
            PendentesAprovacaoText.Text = _todasRequisicoes.Count(r => r.Status == PurchaseRequestStatus.REQUESTED || r.Status == PurchaseRequestStatus.DRAFT).ToString();
            SugestoesEstoqueMinimoText.Text = _todasSugestoes.Count.ToString();
            PedidosEmTransitoText.Text = _todasRequisicoes.Count(r => r.Status == PurchaseRequestStatus.ORDERED).ToString();
            RecebidasText.Text = _todasRequisicoes.Count(r => r.Status == PurchaseRequestStatus.RECEIVED).ToString();
        }

        private void AplicarFiltrosRequisicoes()
        {
            if (!_isInitialized || RequisicoesDataGrid == null)
            {
                return;
            }

            var filtradas = _todasRequisicoes.AsEnumerable();

            var busca = BuscaRequisicaoTextBox?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                filtradas = filtradas.Where(r =>
                    r.Number.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    r.RequestedByUserName.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (r.SupplierName != null && r.SupplierName.Contains(busca, StringComparison.OrdinalIgnoreCase)) ||
                    (r.FiscalDocumentNumber != null && r.FiscalDocumentNumber.Contains(busca, StringComparison.OrdinalIgnoreCase)));
            }

            if (FiltroPrioridadeCombo?.SelectedItem is ComboBoxItem prioItem && prioItem.Content?.ToString() is string prioStr && !prioStr.StartsWith("Todas", StringComparison.OrdinalIgnoreCase))
            {
                var targetPrio = prioStr switch
                {
                    "Baixa" => PurchasePriority.LOW,
                    "Normal" => PurchasePriority.NORMAL,
                    "Alta" => PurchasePriority.HIGH,
                    "Urgente / Crítica" => PurchasePriority.URGENT,
                    _ => (PurchasePriority?)null
                };

                if (targetPrio.HasValue)
                {
                    filtradas = filtradas.Where(r => r.Priority == targetPrio.Value);
                }
            }

            if (FiltroStatusCombo?.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() is string statusStr && !statusStr.StartsWith("Todas", StringComparison.OrdinalIgnoreCase))
            {
                var targetStatus = statusStr switch
                {
                    "Solicitada" => PurchaseRequestStatus.REQUESTED,
                    "Aprovada" => PurchaseRequestStatus.APPROVED,
                    "Pedido Realizado" => PurchaseRequestStatus.ORDERED,
                    "Recebida" => PurchaseRequestStatus.RECEIVED,
                    "Cancelada" => PurchaseRequestStatus.CANCELLED,
                    _ => (PurchaseRequestStatus?)null
                };

                if (targetStatus.HasValue)
                {
                    filtradas = filtradas.Where(r => r.Status == targetStatus.Value);
                }
            }

            RequisicoesDataGrid.ItemsSource = filtradas.ToList();
        }

        private async void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            await CarregarDadosAsync();
        }

        private async void NovaRequisicaoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NovaRequisicaoCompraDialog(_purchaseService)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                await CarregarDadosAsync();
            }
        }

        private async void AprovarButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequisicoesDataGrid.SelectedItem is not PurchaseRequest selecionada)
            {
                MessageBox.Show("Selecione uma requisição para aprovação.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selecionada.Status != PurchaseRequestStatus.REQUESTED && selecionada.Status != PurchaseRequestStatus.DRAFT)
            {
                MessageBox.Show($"A requisição '{selecionada.Number}' já se encontra na situação '{selecionada.StatusDisplay}'.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show(
                $"Deseja aprovar a requisição '{selecionada.Number}' no valor estimado de {selecionada.TotalEstimatedCost:C2}?",
                "Aprovar Requisição de Compra",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var userId = App.Session?.UserId ?? 1;
                    var userName = App.Session?.CurrentUser?.Nome ?? "Gestor Responsável";
                    var ok = await _purchaseService.AprovarRequisicaoAsync(selecionada.PurchaseRequestId, userId, userName);
                    if (ok)
                    {
                        MessageBox.Show("Requisição de compra aprovada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        await CarregarDadosAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro na Aprovação", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void FormalizarPedidoButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequisicoesDataGrid.SelectedItem is not PurchaseRequest selecionada)
            {
                MessageBox.Show("Selecione uma requisição para formalizar pedido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selecionada.Status != PurchaseRequestStatus.APPROVED)
            {
                MessageBox.Show($"Apenas requisições previamente aprovadas podem ser formalizadas como pedido de compra. (Situação atual: {selecionada.StatusDisplay})", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show(
                $"Confirmar formalização de pedido para a requisição '{selecionada.Number}'?",
                "Formalizar Pedido",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var fornecedor = selecionada.SupplierName ?? "Fornecedor Parceiro";
                    var ok = await _purchaseService.FormalizarPedidoAsync(selecionada.PurchaseRequestId, selecionada.SupplierId, fornecedor, selecionada.TotalEstimatedCost);
                    if (ok)
                    {
                        MessageBox.Show("Pedido formalizado com sucesso! Mercadoria em trânsito.", "Pedido Formalizado", MessageBoxButton.OK, MessageBoxImage.Information);
                        await CarregarDadosAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro ao Formalizar Pedido", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ReceberMercadoriaButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequisicoesDataGrid.SelectedItem is not PurchaseRequest selecionada)
            {
                MessageBox.Show("Selecione a requisição de compra a ser recebida fisicamente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selecionada.Status != PurchaseRequestStatus.ORDERED && selecionada.Status != PurchaseRequestStatus.APPROVED)
            {
                MessageBox.Show($"A requisição precisa estar em trânsito/aprovada para ser recebida (Situação: {selecionada.StatusDisplay}).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show(
                $"Confirmar o recebimento físico e a entrada dos itens no estoque para a requisição '{selecionada.Number}'?",
                "Recebimento de Mercadoria",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var userId = App.Session?.UserId ?? 1;
                    var notaFiscal = $"NF-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
                    var ok = await _purchaseService.ReceberMercadoriaAsync(selecionada.PurchaseRequestId, userId, notaFiscal);
                    if (ok)
                    {
                        MessageBox.Show($"Mercadoria recebida com sucesso! Gerada movimentação de estoque e registro no contas a pagar (CentsV1).", "Recebimento Concluído", MessageBoxButton.OK, MessageBoxImage.Information);
                        await CarregarDadosAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro no Recebimento", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequisicoesDataGrid.SelectedItem is not PurchaseRequest selecionada)
            {
                MessageBox.Show("Selecione uma requisição para cancelar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selecionada.Status == PurchaseRequestStatus.RECEIVED)
            {
                MessageBox.Show("Não é permitido cancelar uma requisição já recebida no estoque.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show(
                $"Deseja realmente cancelar a requisição '{selecionada.Number}'?",
                "Cancelar Requisição",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var userId = App.Session?.UserId ?? 1;
                    var ok = await _purchaseService.CancelarRequisicaoAsync(selecionada.PurchaseRequestId, userId, "Cancelada pelo usuário no painel de compras.");
                    if (ok)
                    {
                        MessageBox.Show("Requisição cancelada.", "Cancelamento", MessageBoxButton.OK, MessageBoxImage.Information);
                        await CarregarDadosAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro ao Cancelar", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void GerarRequisicaoSugestoes_Click(object sender, RoutedEventArgs e)
        {
            if (SugestoesDataGrid.SelectedItem is not Produto produto)
            {
                MessageBox.Show("Selecione um produto da lista de sugestões para abrir a requisição de compra.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var qtdSugerida = Math.Max(1, produto.QuantidadeMinima - produto.QuantidadeEstoque);
            var res = MessageBox.Show(
                $"Gerar solicitação de compra para o item '{produto.Codigo} - {produto.Nome}'?\n\nQuantidade sugerida: {qtdSugerida} un.\nEstoque Atual: {produto.QuantidadeEstoque} | Mínimo: {produto.QuantidadeMinima}",
                "Gerar Solicitação de Reposição",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    var userId = App.Session?.UserId ?? 1;
                    var userName = App.Session?.CurrentUser?.Nome ?? "Gestor de Compras";

                    var req = await _purchaseService.CriarRequisicaoAsync(
                        userId,
                        userName,
                        PurchasePriority.HIGH,
                        PurchaseReason.LOW_STOCK,
                        $"Reposição emergencial de estoque mínimo: {produto.Codigo} - {produto.Nome} (Qtd: {qtdSugerida}).");

                    if (req != null)
                    {
                        await _purchaseService.AdicionarItemAsync(req.PurchaseRequestId, produto.Id, qtdSugerida, PurchasePriority.HIGH, "Reposição automática de estoque mínimo");
                        MessageBox.Show($"Requisição '{req.Number}' gerada com sucesso!", "Solicitação Criada", MessageBoxButton.OK, MessageBoxImage.Information);
                        await CarregarDadosAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro ao Gerar Requisição", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BuscaRequisicaoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            AplicarFiltrosRequisicoes();
        }

        private void FiltroRequisicaoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            AplicarFiltrosRequisicoes();
        }

        private void LimparFiltrosRequisicao_Click(object sender, RoutedEventArgs e)
        {
            BuscaRequisicaoTextBox.Text = string.Empty;
            FiltroPrioridadeCombo.SelectedIndex = 0;
            FiltroStatusCombo.SelectedIndex = 0;
            AplicarFiltrosRequisicoes();
        }
    }
}
