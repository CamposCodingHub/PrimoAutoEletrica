using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class PagamentoMistoWindow : Window
    {
        private readonly decimal _totalVenda;

        public PagamentoMistoWindow(decimal totalVenda)
        {
            if (totalVenda <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalVenda), "O total da venda precisa ser maior que zero.");
            }

            _totalVenda = decimal.Round(totalVenda, 2);
            InitializeComponent();
            Loaded += PagamentoMistoWindow_Loaded;
        }

        public string ResumoPagamento { get; private set; } = string.Empty;

        private void PagamentoMistoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ResumoTotalTextBlock.Text = $"Total da venda: {_totalVenda:C}. Informe os valores recebidos por forma.";
            DinheiroTextBox.Text = _totalVenda.ToString("F2", CultureInfo.CurrentCulture);
            PixTextBox.Text = "0,00";
            CartaoTextBox.Text = "0,00";
            AtualizarConferencia();
            DinheiroTextBox.Focus();
            DinheiroTextBox.SelectAll();
        }

        private void ValorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AtualizarConferencia();
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            var partes = ObterPartesPagamento();
            var totalInformado = partes.Sum(parte => parte.Valor);
            var diferenca = decimal.Round(totalInformado - _totalVenda, 2);

            if (diferenca != 0)
            {
                MessageBox.Show(
                    $"A soma informada precisa fechar com o total da venda.\n\nTotal: {_totalVenda:C}\nInformado: {totalInformado:C}\nDiferenca: {diferenca:C}",
                    "Pagamento misto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (partes.Count(parte => parte.Valor > 0) < 2)
            {
                MessageBox.Show(
                    "Informe pelo menos duas formas de pagamento, ou selecione uma forma simples no PDV.",
                    "Pagamento misto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ResumoPagamento = CriarResumo(partes);
            DialogResult = true;
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void AtualizarConferencia()
        {
            if (ConferenciaTextBlock == null)
            {
                return;
            }

            var partes = ObterPartesPagamento();
            var totalInformado = partes.Sum(parte => parte.Valor);
            var diferenca = decimal.Round(totalInformado - _totalVenda, 2);
            ConferenciaTextBlock.Text = $"Informado: {totalInformado:C} | Total: {_totalVenda:C} | Diferenca: {diferenca:C}";
        }

        private List<PagamentoMistoParte> ObterPartesPagamento()
        {
            return new List<PagamentoMistoParte>
            {
                new("Dinheiro", LerValor(DinheiroTextBox)),
                new("PIX", LerValor(PixTextBox)),
                new("Cartao", LerValor(CartaoTextBox))
            };
        }

        private static decimal LerValor(TextBox? textBox)
        {
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
            {
                return 0m;
            }

            if (decimal.TryParse(textBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var atual) ||
                decimal.TryParse(textBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out atual))
            {
                return Math.Max(0m, decimal.Round(atual, 2));
            }

            return 0m;
        }

        private static string CriarResumo(IEnumerable<PagamentoMistoParte> partes)
        {
            return "Misto: " + string.Join(
                " | ",
                partes
                    .Where(parte => parte.Valor > 0)
                    .Select(parte => $"{parte.Forma} {parte.Valor:C}"));
        }

        private sealed record PagamentoMistoParte(string Forma, decimal Valor);
    }
}
