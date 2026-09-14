using System.Globalization;
using System.Windows;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Views
{
    public partial class Produto360Window : Window
    {
        public Produto360Window(Produto360Snapshot snapshot)
        {
            InitializeComponent();
            Carregar(snapshot);
        }

        private void Carregar(Produto360Snapshot snap)
        {
            TituloTextBlock.Text = snap.Nome;
            CodigoTextBlock.Text = string.IsNullOrWhiteSpace(snap.Codigo) ? "Sem código" : snap.Codigo;
            HubTextBlock.Text = snap.HubResumo;
            EstoqueTextBlock.Text =
                $"{snap.EstoqueAtual} / mín {snap.EstoqueMinimo} · disp {snap.EstoqueDisponivel}" +
                (snap.EstoqueCritico ? " · CRÍTICO" : string.Empty);
            if (snap.EstoqueCritico)
            {
                EstoqueTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
            }

            MargemTextBlock.Text = $"{snap.MargemPercentual.ToString("0.##", CultureInfo.CurrentCulture)}%";
            UsoOsTextBlock.Text =
                $"{snap.OsComUsoCount} OS · qtd {snap.QuantidadeUsadaEmOs.ToString("0.##", CultureInfo.CurrentCulture)} · {snap.ValorUsadoEmOs.ToString("C", CultureInfo.CurrentCulture)}";
            ComercialTextBlock.Text =
                $"Fornecedor: {snap.Fornecedor}\n" +
                $"Custo: {snap.Custo.ToString("C", CultureInfo.CurrentCulture)} · Preço: {snap.Preco.ToString("C", CultureInfo.CurrentCulture)}";
            OsIdsTextBlock.Text = snap.OrdemServicoResumos.Count == 0
                ? "Nenhuma OS com este ProdutoId."
                : string.Join("\n", snap.OrdemServicoResumos);
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
