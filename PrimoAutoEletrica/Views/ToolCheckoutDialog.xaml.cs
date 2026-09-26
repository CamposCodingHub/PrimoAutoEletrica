using System;
using System.Windows;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class ToolCheckoutDialog : Window
    {
        private readonly Tool _tool;
        private readonly IToolService _toolService;

        public bool Sucesso { get; private set; }

        public ToolCheckoutDialog(Tool tool, IToolService? toolService = null)
        {
            InitializeComponent();
            _tool = tool ?? throw new ArgumentNullException(nameof(tool));
            _toolService = toolService ?? new ToolService();

            ToolInfoTextBlock.Text = $"{_tool.Code} — {_tool.Name} (Local: {_tool.LocationName})";
            FuncionarioTextBox.Text = App.Session?.CurrentUser?.Nome ?? "Técnico Oficina";
            PrevisaoDevolucaoDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        }

        private async void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;

            var responsavel = FuncionarioTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(responsavel))
            {
                ValidationMessageTextBlock.Text = "Informe o nome do técnico responsável pela retirada.";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                FuncionarioTextBox.Focus();
                return;
            }

            var userId = App.Session?.UserId ?? 1;
            var osNumero = string.IsNullOrWhiteSpace(NumeroOsTextBox.Text) ? null : NumeroOsTextBox.Text.Trim();
            var placa = string.IsNullOrWhiteSpace(PlacaTextBox.Text) ? null : PlacaTextBox.Text.Trim().ToUpperInvariant();
            var previsao = PrevisaoDevolucaoDatePicker.SelectedDate;
            var obs = string.IsNullOrWhiteSpace(ObservacoesTextBox.Text) ? null : ObservacoesTextBox.Text.Trim();

            try
            {
                ConfirmarButton.IsEnabled = false;

                var ok = await _toolService.RetirarFerramentaAsync(
                    _tool.ToolId,
                    userId,
                    responsavel,
                    osId: null,
                    osNumero: osNumero,
                    placa: placa,
                    previsaoDevolucao: previsao,
                    observacao: obs);

                if (ok)
                {
                    Sucesso = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ValidationMessageTextBlock.Text = "Não foi possível registrar a retirada da ferramenta.";
                    ValidationMessageTextBlock.Visibility = Visibility.Visible;
                    ConfirmarButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ValidationMessageTextBlock.Text = ex.Message;
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                ConfirmarButton.IsEnabled = true;
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
