using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class Tool360Window : Window
    {
        private Tool _tool;
        private readonly IToolService _toolService;
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        public Tool360Window(Tool tool, IToolService? toolService = null)
        {
            InitializeComponent();
            _tool = tool ?? throw new ArgumentNullException(nameof(tool));
            _toolService = toolService ?? new ToolService();

            Loaded += async (s, e) => await CarregarDadosAsync();
        }

        private async Task CarregarDadosAsync()
        {
            // Se tiver serviço, busca versão mais recente
            var atualizada = await _toolService.ObterFerramentaPorIdAsync(_tool.ToolId);
            if (atualizada != null)
            {
                _tool = atualizada;
            }

            AtualizarVisualizacao();
            await CarregarHistoricosAsync();
        }

        private void AtualizarVisualizacao()
        {
            CodeTextBlock.Text = _tool.Code;
            NameTextBlock.Text = _tool.Name;
            CategoryLocationTextBlock.Text = $"{_tool.Category} | Local: {_tool.LocationName}";

            // Status Badge
            StatusBadgeTextBlock.Text = _tool.StatusDisplay;
            switch (_tool.Status)
            {
                case ToolStatus.AVAILABLE:
                    StatusBadgeBorder.Style = (Style)FindResource("StatusBadgeSuccess");
                    StatusBadgeTextBlock.Style = (Style)FindResource("StatusBadgeTextSuccess");
                    break;
                case ToolStatus.IN_USE:
                    StatusBadgeBorder.Style = (Style)FindResource("StatusBadgeWarning");
                    StatusBadgeTextBlock.Style = (Style)FindResource("StatusBadgeTextWarning");
                    break;
                case ToolStatus.MAINTENANCE:
                    StatusBadgeBorder.Style = (Style)FindResource("StatusBadgeInfo");
                    StatusBadgeTextBlock.Style = (Style)FindResource("StatusBadgeTextInfo");
                    break;
                case ToolStatus.DAMAGED:
                case ToolStatus.LOST:
                    StatusBadgeBorder.Style = (Style)FindResource("StatusBadgeDanger");
                    StatusBadgeTextBlock.Style = (Style)FindResource("StatusBadgeTextDanger");
                    break;
                default:
                    StatusBadgeBorder.Style = (Style)FindResource("StatusBadgeNeutral");
                    StatusBadgeTextBlock.Style = (Style)FindResource("StatusBadgeTextNeutral");
                    break;
            }

            // Cards de Métricas
            if (_tool.IsInUse && !string.IsNullOrWhiteSpace(_tool.CurrentResponsibleUserName))
            {
                ResponsavelTextBlock.Text = _tool.CurrentResponsibleUserName;
                ResponsavelDetalheTextBlock.Text = "Em posse do técnico operacional";
            }
            else
            {
                ResponsavelTextBlock.Text = "Livre na Oficina";
                ResponsavelDetalheTextBlock.Text = "Disponível para qualquer técnico";
            }

            ValorPatrimonialTextBlock.Text = _tool.PurchaseValue.ToString("C2", PtBr);
            DataCompraTextBlock.Text = _tool.PurchaseDate.HasValue
                ? $"Adquirida em {_tool.PurchaseDate.Value:dd/MM/yyyy}"
                : "Data não informada";

            QrCodeUriTextBlock.Text = _tool.QrCodeUri;

            // Especificações e Rastreabilidade
            MarcaTextBlock.Text = string.IsNullOrWhiteSpace(_tool.Brand) ? "—" : _tool.Brand;
            ModeloTextBlock.Text = string.IsNullOrWhiteSpace(_tool.Model) ? "—" : _tool.Model;
            SerialTextBlock.Text = string.IsNullOrWhiteSpace(_tool.SerialNumber) ? "—" : _tool.SerialNumber;
            PatrimonioTextBlock.Text = string.IsNullOrWhiteSpace(_tool.PatrimonyNumber) ? "—" : _tool.PatrimonyNumber;

            UltimaManutencaoTextBlock.Text = _tool.LastMaintenanceDate.HasValue
                ? _tool.LastMaintenanceDate.Value.ToString("dd/MM/yyyy")
                : "—";

            ProximaManutencaoTextBlock.Text = _tool.NextMaintenanceDate.HasValue
                ? _tool.NextMaintenanceDate.Value.ToString("dd/MM/yyyy")
                : "—";

            ObservacoesTextBlock.Text = string.IsNullOrWhiteSpace(_tool.Notes)
                ? "Nenhuma observação técnica registrada."
                : _tool.Notes;

            // Habilitação dos Botões
            RetirarButton.IsEnabled = _tool.IsAvailable;
            DevolverButton.IsEnabled = _tool.IsInUse;
            ManutencaoButton.IsEnabled = true;
        }

        private async Task CarregarHistoricosAsync()
        {
            try
            {
                var historico = await _toolService.ObterHistoricoMovimentacoesAsync(_tool.ToolId);
                HistoricoDataGrid.ItemsSource = historico;

                var manutencoes = await _toolService.ObterManutencoesAsync(_tool.ToolId);
                ManutencoesDataGrid.ItemsSource = manutencoes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar históricos da ferramenta: {ex.Message}");
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void RetirarButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ToolCheckoutDialog(_tool, _toolService)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                await CarregarDadosAsync();
            }
        }

        private async void DevolverButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Deseja confirmar a devolução da ferramenta '{_tool.Code} - {_tool.Name}'?\n\nCondição: Em perfeito estado (OK).",
                "Confirmar Devolução",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var userId = App.Session?.UserId ?? 1;
                var sucesso = await _toolService.DevolverFerramentaAsync(_tool.ToolId, userId, ToolCondition.OK, "Devolução padrão registrada via Tool360.");
                if (sucesso)
                {
                    MessageBox.Show("Ferramenta devolvida com sucesso e liberada para uso.", "Devolução Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
                    await CarregarDadosAsync();
                }
                else
                {
                    MessageBox.Show("Não foi possível registrar a devolução.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro na Devolução", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ManutencaoButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Registrar calibração / aferição técnica para a ferramenta '{_tool.Code} - {_tool.Name}'?",
                "Registrar Calibração",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var sucesso = await _toolService.RegistrarManutencaoAsync(
                    _tool.ToolId,
                    ToolMaintenanceType.CALIBRATION,
                    "Aferição e calibração metrológica de rotina",
                    custo: 0m,
                    prestador: "Interno / Laboratório Credenciado",
                    observacoes: "Calibração periódica registrada via Tool360.");

                if (sucesso)
                {
                    MessageBox.Show("Calibração registrada com sucesso!", "Calibração Registrada", MessageBoxButton.OK, MessageBoxImage.Information);
                    await CarregarDadosAsync();
                }
                else
                {
                    MessageBox.Show("Não foi possível registrar a calibração.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro na Manutenção", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
