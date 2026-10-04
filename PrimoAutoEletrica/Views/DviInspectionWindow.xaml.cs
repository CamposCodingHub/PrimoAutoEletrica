using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using PrimoAutoEletrica.Models.Dvi;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrimoAutoEletrica.Views
{
    public partial class DviInspectionWindow : Window
    {
        private readonly IDviInspectionService _dviService;
        private InspecaoDvi _inspecao;
        private InspecaoDviItem? _itemSelecionado;
        private InspecaoDviFoto? _fotoSelecionada;
        private string _categoriaFiltro = "Todas";
        private bool _isUpdatingUi;

        public InspecaoDvi Inspecao => _inspecao;

        public class DviItemViewRow
        {
            public InspecaoDviItem Item { get; set; } = null!;
            public Brush SeveridadeBrush { get; set; } = Brushes.Gray;
            public string StatusDescricao { get; set; } = "OK";
            public string FotosBadge => Item.Fotos?.Count > 0 ? $"📷 {Item.Fotos.Count} foto(s)" : string.Empty;
        }

        public DviInspectionWindow(
            InspecaoDvi? inspecaoExistente = null,
            string? ordemServicoId = null,
            string? placa = null,
            string? modelo = null,
            string? clienteNome = null,
            string? clienteTelefone = null,
            IDviInspectionService? dviServiceOverride = null)
        {
            InitializeComponent();

            _dviService = dviServiceOverride ??
                          App.Services?.GetService<IDviInspectionService>() ??
                          new DviInspectionService(App.Services?.GetService<DatabaseService>() ?? new DatabaseService());

            if (inspecaoExistente != null)
            {
                _inspecao = inspecaoExistente;
            }
            else
            {
                _inspecao = new InspecaoDvi
                {
                    OrdemServicoId = ordemServicoId,
                    PlacaVeiculo = placa ?? string.Empty,
                    ModeloVeiculo = modelo ?? string.Empty,
                    ClienteNome = clienteNome ?? string.Empty,
                    ClienteTelefone = clienteTelefone ?? string.Empty,
                    ResponsavelTecnico = "Eletricista Técnico",
                    DataInspecao = DateTime.Now
                };
            }

            Loaded += DviInspectionWindow_Loaded;
        }

        private async void DviInspectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_inspecao.Id > 0)
            {
                var carregada = await _dviService.ObterPorIdAsync(_inspecao.Id);
                if (carregada != null) _inspecao = carregada;
            }
            else if (!string.IsNullOrWhiteSpace(_inspecao.OrdemServicoId))
            {
                var existente = await _dviService.ObterPorOrdemServicoIdAsync(_inspecao.OrdemServicoId);
                if (existente != null) _inspecao = existente;
            }

            // Se ainda não tiver itens, gera checklist padrão de auto elétrica
            if (_inspecao.Itens == null || _inspecao.Itens.Count == 0)
            {
                _inspecao.Itens = _dviService.GerarChecklistPadraoAutoEletrica(_inspecao.Id);
            }

            PreencherCabecalho();
            AtualizarListaItens();
            AtualizarContadores();
        }

        private void PreencherCabecalho()
        {
            _isUpdatingUi = true;
            try
            {
                PlacaTextBox.Text = _inspecao.PlacaVeiculo;
                ModeloTextBox.Text = _inspecao.ModeloVeiculo;
                ClienteTextBox.Text = _inspecao.ClienteNome;
                TelefoneTextBox.Text = _inspecao.ClienteTelefone;
                TecnicoTextBox.Text = _inspecao.ResponsavelTecnico;

                if (!string.IsNullOrWhiteSpace(_inspecao.OrdemServicoId))
                {
                    OsVinculadaText.Text = $"OS #{_inspecao.OrdemServicoId}";
                    OsVinculadaBadge.Visibility = Visibility.Visible;
                    InserirNaOsBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    OsVinculadaBadge.Visibility = Visibility.Collapsed;
                    InserirNaOsBtn.Visibility = Visibility.Collapsed;
                }

                StatusBadgeText.Text = _inspecao.StatusAprovacao switch
                {
                    DviStatusAprovacao.Pendente => "Pendente",
                    DviStatusAprovacao.EnviadoWhatsApp => "Enviado WhatsApp",
                    DviStatusAprovacao.AprovadoCliente => "Aprovado pelo Cliente",
                    DviStatusAprovacao.RecusadoCliente => "Recusado",
                    _ => "Pendente"
                };
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void AtualizarListaItens()
        {
            if (_inspecao.Itens == null) return;

            var filtrados = _categoriaFiltro == "Todas"
                ? _inspecao.Itens
                : _inspecao.Itens.Where(i => i.Categoria.Contains(_categoriaFiltro, StringComparison.OrdinalIgnoreCase)).ToList();

            var rows = filtrados.Select(item =>
            {
                Brush brush = item.Severidade switch
                {
                    DviStatusSeveridade.Critico => (Brush)FindResource("DangerBrush"),
                    DviStatusSeveridade.Atencao => (Brush)FindResource("WarningBrush"),
                    _ => (Brush)FindResource("SuccessBrush")
                };

                string statusDesc = item.Severidade switch
                {
                    DviStatusSeveridade.Critico => "🔴 Crítico",
                    DviStatusSeveridade.Atencao => "🟡 Atenção",
                    _ => "🟢 Conforme"
                };

                return new DviItemViewRow
                {
                    Item = item,
                    SeveridadeBrush = brush,
                    StatusDescricao = statusDesc
                };
            }).ToList();

            ItensListView.ItemsSource = rows;

            if (_itemSelecionado != null)
            {
                var row = rows.FirstOrDefault(r => r.Item == _itemSelecionado);
                if (row != null) ItensListView.SelectedItem = row;
            }
            else if (rows.Count > 0)
            {
                ItensListView.SelectedIndex = 0;
            }
        }

        private void AtualizarContadores()
        {
            if (_inspecao.Itens == null) return;

            int criticos = _inspecao.TotalItensCriticos;
            int atencao = _inspecao.TotalItensAtencao;
            int ok = _inspecao.TotalItensOk;

            ContadorCriticoText.Text = $"🔴 {criticos} Críticos";
            ContadorAtencaoText.Text = $"🟡 {atencao} Preventivos";
            ContadorOkText.Text = $"🟢 {ok} Conformes";

            decimal total = 0;
            foreach (var item in _inspecao.Itens)
            {
                if (item.ValorEstimadoReparo.HasValue) total += item.ValorEstimadoReparo.Value;
            }
            _inspecao.ValorTotalEstimado = total;
            TotalEstimadoText.Text = $"R$ {total:N2}";
        }

        private void ItensListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItensListView.SelectedItem is DviItemViewRow row)
            {
                CarregarDetalheItem(row.Item);
            }
        }

        private void CarregarDetalheItem(InspecaoDviItem item)
        {
            _itemSelecionado = item;
            _isUpdatingUi = true;
            try
            {
                NomeItemSelecionadoText.Text = $"{item.Categoria} › {item.NomeItem}";
                ObservacaoItemTextBox.Text = item.ObservacaoTecnica ?? string.Empty;
                ValorItemTextBox.Text = item.ValorEstimadoReparo.HasValue ? item.ValorEstimadoReparo.Value.ToString("N2", CultureInfo.CurrentCulture) : string.Empty;

                RadioOk.IsChecked = item.Severidade == DviStatusSeveridade.Ok;
                RadioAtencao.IsChecked = item.Severidade == DviStatusSeveridade.Atencao;
                RadioCritico.IsChecked = item.Severidade == DviStatusSeveridade.Critico;

                AtualizarListaFotos();
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void AtualizarListaFotos()
        {
            if (_itemSelecionado == null)
            {
                FotosListBox.ItemsSource = null;
                FotoPreviewImage.Source = null;
                ExcluirFotoBtn.Visibility = Visibility.Collapsed;
                return;
            }

            FotosListBox.ItemsSource = null;
            FotosListBox.ItemsSource = _itemSelecionado.Fotos;

            if (_itemSelecionado.Fotos != null && _itemSelecionado.Fotos.Count > 0)
            {
                FotosListBox.SelectedIndex = 0;
            }
            else
            {
                FotoPreviewImage.Source = null;
                ExcluirFotoBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void FotosListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FotosListBox.SelectedItem is InspecaoDviFoto foto && File.Exists(foto.CaminhoArquivo))
            {
                _fotoSelecionada = foto;
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(foto.CaminhoArquivo);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    FotoPreviewImage.Source = bitmap;
                    ExcluirFotoBtn.Visibility = Visibility.Visible;
                }
                catch
                {
                    FotoPreviewImage.Source = null;
                    ExcluirFotoBtn.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _fotoSelecionada = null;
                FotoPreviewImage.Source = null;
                ExcluirFotoBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void RadioSeveridade_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi || _itemSelecionado == null) return;

            if (RadioCritico.IsChecked == true) _itemSelecionado.Severidade = DviStatusSeveridade.Critico;
            else if (RadioAtencao.IsChecked == true) _itemSelecionado.Severidade = DviStatusSeveridade.Atencao;
            else _itemSelecionado.Severidade = DviStatusSeveridade.Ok;

            AtualizarListaItens();
            AtualizarContadores();
        }

        private void ObservacaoItemTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUi || _itemSelecionado == null) return;
            _itemSelecionado.ObservacaoTecnica = ObservacaoItemTextBox.Text;
        }

        private void ValorItemTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUi || _itemSelecionado == null) return;

            var txt = ValorItemTextBox.Text.Trim().Replace("R$", "").Trim();
            if (decimal.TryParse(txt, NumberStyles.Any, CultureInfo.CurrentCulture, out var valor) ||
                decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out valor))
            {
                _itemSelecionado.ValorEstimadoReparo = valor;
            }
            else if (string.IsNullOrWhiteSpace(txt))
            {
                _itemSelecionado.ValorEstimadoReparo = null;
            }

            AtualizarContadores();
        }

        private void ItemMarcarOk_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InspecaoDviItem item)
            {
                item.Severidade = DviStatusSeveridade.Ok;
                AtualizarListaItens();
                AtualizarContadores();
                if (_itemSelecionado == item) CarregarDetalheItem(item);
            }
        }

        private void ItemMarcarAtencao_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InspecaoDviItem item)
            {
                item.Severidade = DviStatusSeveridade.Atencao;
                AtualizarListaItens();
                AtualizarContadores();
                if (_itemSelecionado == item) CarregarDetalheItem(item);
            }
        }

        private void ItemMarcarCritico_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InspecaoDviItem item)
            {
                item.Severidade = DviStatusSeveridade.Critico;
                AtualizarListaItens();
                AtualizarContadores();
                if (_itemSelecionado == item) CarregarDetalheItem(item);
            }
        }

        private void FiltrarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string cat)
            {
                _categoriaFiltro = cat;
                AtualizarListaItens();
            }
        }

        private async void AdicionarFotoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_itemSelecionado == null)
            {
                MessageBox.Show("Selecione um item da lista para anexar fotos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new OpenFileDialog
            {
                Title = "Selecionar Foto de Evidência Técnica",
                Filter = "Imagens (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp|Todos os arquivos (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true && File.Exists(dlg.FileName))
            {
                try
                {
                    var bytes = await File.ReadAllBytesAsync(dlg.FileName);
                    var foto = await _dviService.SalvarFotoAsync(_itemSelecionado.Id, bytes, Path.GetFileName(dlg.FileName), _itemSelecionado.NomeItem);

                    _itemSelecionado.Fotos.Add(foto);
                    AtualizarListaFotos();
                    AtualizarListaItens();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao anexar imagem: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExcluirFotoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_fotoSelecionada == null || _itemSelecionado == null) return;

            var confirm = MessageBox.Show("Deseja realmente remover esta foto da inspeção?", "Excluir Foto", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _dviService.ExcluirFotoAsync(_fotoSelecionada.Id);
                    _itemSelecionado.Fotos.Remove(_fotoSelecionada);
                    _fotoSelecionada = null;
                    AtualizarListaFotos();
                    AtualizarListaItens();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao excluir foto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GerarChecklistAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Deseja recarregar o checklist padrão de Auto Elétrica? Os itens atuais serão complementados.", "Checklist Auto Elétrica", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                _inspecao.Itens = _dviService.GerarChecklistPadraoAutoEletrica(_inspecao.Id);
                AtualizarListaItens();
                AtualizarContadores();
            }
        }

        private async void SalvarInspecaoBtn_Click(object sender, RoutedEventArgs e)
        {
            await SalvarAsync(mostrarMensagem: true);
        }

        private async Task<bool> SalvarAsync(bool mostrarMensagem = false)
        {
            if (string.IsNullOrWhiteSpace(PlacaTextBox.Text))
            {
                MessageBox.Show("Informe a placa do veículo antes de salvar.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                PlacaTextBox.Focus();
                return false;
            }

            _inspecao.PlacaVeiculo = PlacaTextBox.Text.Trim().ToUpperInvariant();
            _inspecao.ModeloVeiculo = ModeloTextBox.Text.Trim();
            _inspecao.ClienteNome = ClienteTextBox.Text.Trim();
            _inspecao.ClienteTelefone = TelefoneTextBox.Text.Trim();
            _inspecao.ResponsavelTecnico = TecnicoTextBox.Text.Trim();

            try
            {
                await _dviService.SalvarInspecaoAsync(_inspecao);
                if (mostrarMensagem)
                {
                    MessageBox.Show($"Inspeção DVI #{_inspecao.Id} para o veículo {_inspecao.PlacaVeiculo} salva com sucesso!", "DVI 2.0 Salvo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar inspeção DVI: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async void WhatsAppBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TelefoneTextBox.Text))
            {
                MessageBox.Show("Informe o telefone/WhatsApp do cliente no cabeçalho da inspeção.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                TelefoneTextBox.Focus();
                return;
            }

            var salvou = await SalvarAsync(mostrarMensagem: false);
            if (!salvou) return;

            var url = _dviService.GerarLinkWhatsApp(_inspecao);
            _inspecao.StatusAprovacao = DviStatusAprovacao.EnviadoWhatsApp;
            await _dviService.SalvarInspecaoAsync(_inspecao);
            StatusBadgeText.Text = "Enviado WhatsApp";

            if (App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo($"Envio DVI via WhatsApp validado em automação para {_inspecao.PlacaVeiculo}. Link: {url}");
                MessageBox.Show("Link WhatsApp gerado e validado em automação!", "WhatsApp", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
                MessageBox.Show($"Não foi possível abrir o navegador: {ex.Message}\n\nLink:\n{url}", "WhatsApp", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void InserirNaOsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_inspecao.OrdemServicoId))
            {
                MessageBox.Show("Esta inspeção não está vinculada a nenhuma Ordem de Serviço.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var itensDefeituosos = _inspecao.Itens?.FindAll(i => i.Severidade != DviStatusSeveridade.Ok) ?? new();
            if (itensDefeituosos.Count == 0)
            {
                MessageBox.Show("Nenhum item com severidade Atenção ou Crítico foi encontrado para inserção na OS.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"Deseja converter {itensDefeituosos.Count} itens identificados na inspeção DVI diretamente em linhas de serviço na OS #{_inspecao.OrdemServicoId}?", "Converter para OS", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    var salvou = await SalvarAsync(mostrarMensagem: false);
                    if (!salvou) return;

                    var inseridos = await _dviService.ConverterItensParaOrdemServicoAsync(_inspecao.Id, _inspecao.OrdemServicoId);
                    MessageBox.Show($"{inseridos} itens da inspeção DVI foram inseridos com sucesso na OS #{_inspecao.OrdemServicoId}!", "Itens Inseridos na OS", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao converter itens para a OS: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FecharBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
