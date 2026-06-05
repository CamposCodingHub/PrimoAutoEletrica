using PrimoAutoEletrica.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class HistoricoEstoqueWindow : Window
    {
        public HistoricoEstoqueWindow(Produto produto, IEnumerable<DadoAuditoria> historico)
        {
            InitializeComponent();

            HeaderTextBlock.Text = $"Historico do produto {produto.Nome}";
            SubheaderTextBlock.Text = $"Codigo {produto.Codigo} | Localizacao {produto.Localizacao} | Ultimas movimentacoes operacionais registradas.";
            EstoqueAtualTextBlock.Text = produto.QuantidadeEstoque.ToString();
            ReservadoTextBlock.Text = produto.QuantidadeReservada.ToString();
            DisponivelTextBlock.Text = produto.QuantidadeDisponivel.ToString();
            HistoricoDataGrid.ItemsSource = historico?.ToList() ?? new List<DadoAuditoria>();
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
