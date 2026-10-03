using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class ComprasNecessidadeControl : UserControl
    {
        private readonly IGestaoComprasService _gestaoComprasService;
        private readonly IFornecedorRepository _fornecedorRepository;
        private readonly LoggerService? _logger;
        private List<ItemFaltaEstoque> _todosItens = new();
        private bool _isCarregando = false;

        public ComprasNecessidadeControl()
        {
            InitializeComponent();

            _gestaoComprasService = App.Services.GetService<IGestaoComprasService>()
                ?? new GestaoComprasService(App.Database, App.Repositories.Produtos, App.Repositories.Fornecedores, new EstoqueOperationalService(App.Database, App.Logger), App.Logger);
            _fornecedorRepository = App.Repositories.Fornecedores;
            _logger = App.Logger;

            Loaded += ComprasNecessidadeControl_Loaded;
        }

        private async void ComprasNecessidadeControl_Loaded(object sender, RoutedEventArgs e)
        {
            InicializarCombos();
            await CarregarDadosAsync();
        }

        private void InicializarCombos()
        {
            // Urgencias
            UrgenciaComboBox.Items.Clear();
            UrgenciaComboBox.Items.Add(new ComboBoxItem { Content = "Todas as Urgências", Tag = null });
            UrgenciaComboBox.Items.Add(new ComboBoxItem { Content = "Crítica (Ruptura)", Tag = NivelUrgenciaFalta.Critica });
            UrgenciaComboBox.Items.Add(new ComboBoxItem { Content = "Alta (Abaixo Mínimo)", Tag = NivelUrgenciaFalta.Alta });
            UrgenciaComboBox.Items.Add(new ComboBoxItem { Content = "Média (Ponto de Pedido)", Tag = NivelUrgenciaFalta.Media });
            UrgenciaComboBox.Items.Add(new ComboBoxItem { Content = "Preventiva (Giro)", Tag = NivelUrgenciaFalta.Preventiva });
            UrgenciaComboBox.SelectedIndex = 0;

            // Curva ABC
            CurvaAbcComboBox.Items.Clear();
            CurvaAbcComboBox.Items.Add(new ComboBoxItem { Content = "Todas as Curvas", Tag = "Todas" });
            CurvaAbcComboBox.Items.Add(new ComboBoxItem { Content = "Curva A (Alto Giro)", Tag = "A" });
            CurvaAbcComboBox.Items.Add(new ComboBoxItem { Content = "Curva B (Médio Giro)", Tag = "B" });
            CurvaAbcComboBox.Items.Add(new ComboBoxItem { Content = "Curva C (Baixo Giro)", Tag = "C" });
            CurvaAbcComboBox.SelectedIndex = 0;

            // Fornecedores
            AtualizarComboFornecedores();
        }

        private void AtualizarComboFornecedores()
        {
            try
            {
                FornecedorComboBox.Items.Clear();
                FornecedorComboBox.Items.Add(new ComboBoxItem { Content = "Todos os Fornecedores", Tag = Guid.Empty });

                var fornecedores = _fornecedorRepository.ObterTodos().Where(f => f.Ativo).OrderBy(f => f.NomeFantasia);
                foreach (var f in fornecedores)
                {
                    FornecedorComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = !string.IsNullOrWhiteSpace(f.NomeFantasia) ? f.NomeFantasia : f.RazaoSocial,
                        Tag = f.Id
                    });
                }
                FornecedorComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError("Erro ao carregar fornecedores no combo de compras.", ex);
            }
        }

        private async Task CarregarDadosAsync()
        {
            if (_isCarregando) return;
            _isCarregando = true;

            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ContentGrid.Visibility = Visibility.Collapsed;
                EmptyPanel.Visibility = Visibility.Collapsed;

                var termo = BuscaTextBox.Text?.Trim();
                var urgencia = (UrgenciaComboBox.SelectedItem as ComboBoxItem)?.Tag as NivelUrgenciaFalta?;
                var fornecedorId = (FornecedorComboBox.SelectedItem as ComboBoxItem)?.Tag is Guid id && id != Guid.Empty ? id : (Guid?)null;
                var curva = (CurvaAbcComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                _todosItens = await _gestaoComprasService.ObterNecessidadesReposicaoAsync(termo, urgencia, fornecedorId, curva);

                // Atualizar metricas
                var resumo = await _gestaoComprasService.ObterResumoNecessidadesAsync();
                TotalFaltaText.Text = $"{resumo.TotalItensEmFalta} itens";
                RupturaCriticaText.Text = resumo.RupturasCriticas.ToString();
                AbaixoMinimoText.Text = resumo.ItensAbaixoMinimo.ToString();
                CustoEstimadoText.Text = resumo.CustoTotalEstimado.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
                FornecedoresImpactadosText.Text = $"{resumo.FornecedoresImpactados} distribuidoras";

                NecessidadesDataGrid.ItemsSource = null;
                NecessidadesDataGrid.ItemsSource = _todosItens;

                if (_todosItens.Count == 0)
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                    ContentGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyPanel.Visibility = Visibility.Collapsed;
                    ContentGrid.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Erro ao carregar necessidades de reposicao.", ex);
                MessageBox.Show($"Erro ao consultar necessidades de compras: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                _isCarregando = false;
            }
        }

        private async void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await CarregarDadosAsync();
        }

        private async void Filtros_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isCarregando)
            {
                await CarregarDadosAsync();
            }
        }

        private async void LimparFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            BuscaTextBox.Text = string.Empty;
            UrgenciaComboBox.SelectedIndex = 0;
            FornecedorComboBox.SelectedIndex = 0;
            CurvaAbcComboBox.SelectedIndex = 0;
            SelecionarTodosCheckBox.IsChecked = false;
            await CarregarDadosAsync();
        }

        private void SelecionarTodosCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_todosItens != null)
            {
                foreach (var item in _todosItens)
                {
                    item.Selecionado = true;
                }
                NecessidadesDataGrid.Items.Refresh();
            }
        }

        private void SelecionarTodosCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_todosItens != null)
            {
                foreach (var item in _todosItens)
                {
                    item.Selecionado = false;
                }
                NecessidadesDataGrid.Items.Refresh();
            }
        }

        private async void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            await CarregarDadosAsync();
        }

        private async void CotarWhatsAppButton_Click(object sender, RoutedEventArgs e)
        {
            var selecionados = _todosItens.Where(i => i.Selecionado).ToList();
            if (selecionados.Count == 0)
            {
                var resultado = MessageBox.Show(
                    "Nenhum item foi selecionado. Deseja cotar todos os itens exibidos na lista?",
                    "Cotar Itens",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    selecionados = _todosItens.ToList();
                }
                else
                {
                    return;
                }
            }

            // Agrupar por fornecedor
            var porFornecedor = selecionados
                .GroupBy(i => i.FornecedorPreferencialId ?? Guid.Empty)
                .ToList();

            foreach (var grupo in porFornecedor)
            {
                var fId = grupo.Key;
                var itens = grupo.ToList();
                var pedido = await _gestaoComprasService.GerarRascunhoPedidoAsync(fId, itens);

                var mensagem = _gestaoComprasService.FormatarMensagemCotacaoWhatsApp(pedido);

                Clipboard.SetText(mensagem);

                var fornecedor = fId != Guid.Empty ? _fornecedorRepository.ObterPorId(fId) : null;
                var telefone = fornecedor?.WhatsAppVendedor ?? fornecedor?.Celular ?? fornecedor?.Telefone ?? string.Empty;
                var numeroLimpo = new string(telefone.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrWhiteSpace(numeroLimpo))
                {
                    if (!numeroLimpo.StartsWith("55") && numeroLimpo.Length >= 10)
                    {
                        numeroLimpo = "55" + numeroLimpo;
                    }

                    var url = $"https://wa.me/{numeroLimpo}?text={Uri.EscapeDataString(mensagem)}";
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"Não foi possível abrir o navegador diretamente: {ex.Message}");
                    }
                }

                MessageBox.Show(
                    $"Texto da cotação para '{pedido.FornecedorNome}' copiado para a Área de Transferência!\n\nVocê já pode colar (Ctrl+V) no WhatsApp do distribuidor.",
                    "Cotação Gerada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                break; // Se houver múltiplos, abre o primeiro para conveniência
            }
        }

        private async void GerarPedidoButton_Click(object sender, RoutedEventArgs e)
        {
            var selecionados = _todosItens.Where(i => i.Selecionado).ToList();
            if (selecionados.Count == 0)
            {
                MessageBox.Show("Selecione pelo menos um item para gerar a ordem de compra.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var porFornecedor = selecionados
                .GroupBy(i => i.FornecedorPreferencialId ?? Guid.Empty)
                .ToList();

            int pedidosCriados = 0;
            string? ultimoPdfGerado = null;
            var pdfService = new DocumentoPdfService();

            foreach (var grupo in porFornecedor)
            {
                var fId = grupo.Key;
                var itens = grupo.ToList();
                var pedido = await _gestaoComprasService.GerarRascunhoPedidoAsync(fId, itens);
                pedido.Status = StatusPedidoCompra.AprovadoAguardandoEntrega;

                await _gestaoComprasService.SalvarPedidoCompraAsync(pedido);
                pedidosCriados++;

                try
                {
                    var pastaDestino = Path.Combine(App.RuntimeAppDataPath, "PedidosCompra");
                    Directory.CreateDirectory(pastaDestino);
                    var caminhoPdf = Path.Combine(pastaDestino, $"{pedido.Numero}.pdf");
                    pdfService.GerarPedidoCompra(pedido, caminhoPdf);
                    ultimoPdfGerado = caminhoPdf;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Não foi possível gerar o PDF da ordem de compra: {ex.Message}");
                }
            }

            var msg = $"{pedidosCriados} Pedido(s) de Compra gerado(s) com sucesso com status 'Aguardando Entrega'!";
            if (!string.IsNullOrEmpty(ultimoPdfGerado) && File.Exists(ultimoPdfGerado))
            {
                var res = MessageBox.Show(
                    $"{msg}\n\nDeseja abrir o arquivo PDF da Ordem de Compra agora?",
                    "Pedido de Compra Gerado",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = ultimoPdfGerado, UseShellExecute = true });
                    }
                    catch { }
                }
            }
            else
            {
                MessageBox.Show(msg, "Pedido de Compra", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await CarregarDadosAsync();
        }

        private async void VerPedidosButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pedidos = await _gestaoComprasService.ListarPedidosCompraAsync();
                if (pedidos.Count == 0)
                {
                    MessageBox.Show("Nenhum pedido de compra foi emitido até o momento.", "Pedidos de Compra", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var texto = string.Join("\n\n", pedidos.Select(p =>
                    $"• [{p.Numero}] {p.FornecedorNome}\n   Status: {p.StatusDescricao} | Itens: {p.Itens.Count} | Total: {p.ValorTotal:C2} | Data: {p.DataCriacao:dd/MM/yyyy}"));

                MessageBox.Show(
                    $"PEDIDOS DE COMPRA EMITIDOS ({pedidos.Count}):\n\n{texto}",
                    "Histórico de Pedidos de Compra",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Erro ao listar pedidos de compra.", ex);
            }
        }

        private void ItemWhatsAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemFaltaEstoque item)
            {
                var texto = $"Olá! Poderia cotar o item *[{item.Codigo}] {item.Nome}*? Precisamos de *{item.QuantidadeSugeridaCompra} un*. Obrigado!";
                Clipboard.SetText(texto);

                var tel = new string((item.FornecedorWhatsApp ?? string.Empty).Where(char.IsDigit).ToArray());
                if (!string.IsNullOrWhiteSpace(tel))
                {
                    if (!tel.StartsWith("55") && tel.Length >= 10) tel = "55" + tel;
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = $"https://wa.me/{tel}?text={Uri.EscapeDataString(texto)}",
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }

                MessageBox.Show($"Mensagem de cotação do item '{item.Nome}' copiada para a Área de Transferência!", "Cotação Rápida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
