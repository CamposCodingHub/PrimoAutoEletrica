using System;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class NovaRequisicaoCompraDialog : Window
    {
        private readonly IPurchaseService _purchaseService;
        public PurchaseRequest? RequisicaoCriada { get; private set; }

        public NovaRequisicaoCompraDialog(IPurchaseService? purchaseService = null)
        {
            InitializeComponent();
            _purchaseService = purchaseService ?? new PurchaseService();

            SolicitanteTextBox.Text = App.Session?.CurrentUser?.Nome ?? "Técnico Solicitante";
        }

        private async void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;

            var prioridadeStr = (PrioridadeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Normal";
            var prioridade = prioridadeStr switch
            {
                "Baixa" => PurchasePriority.LOW,
                "Alta" => PurchasePriority.HIGH,
                "Urgente / Crítica" => PurchasePriority.URGENT,
                _ => PurchasePriority.NORMAL
            };

            var motivoStr = (MotivoComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Reposição de Estoque";
            var motivo = motivoStr switch
            {
                "Ordem de Serviço Específica" => PurchaseReason.WORK_ORDER,
                "Aquisição de Patrimônio / Ferramenta" => PurchaseReason.MAINTENANCE,
                "Insumo Operacional" => PurchaseReason.NEW_SERVICE,
                "Outro" => PurchaseReason.OTHER,
                _ => PurchaseReason.LOW_STOCK
            };

            var obs = ObservacoesTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(obs))
            {
                ValidationMessageTextBlock.Text = "Descreva a justificativa ou os itens solicitados.";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                ObservacoesTextBox.Focus();
                return;
            }

            try
            {
                SalvarButton.IsEnabled = false;
                var userId = App.Session?.UserId ?? 1;
                var userName = SolicitanteTextBox.Text;

                var req = await _purchaseService.CriarRequisicaoAsync(userId, userName, prioridade, motivo, obs);
                if (req != null)
                {
                    RequisicaoCriada = req;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ValidationMessageTextBlock.Text = "Não foi possível registrar a requisição.";
                    ValidationMessageTextBlock.Visibility = Visibility.Visible;
                    SalvarButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ValidationMessageTextBlock.Text = ex.Message;
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                SalvarButton.IsEnabled = true;
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
