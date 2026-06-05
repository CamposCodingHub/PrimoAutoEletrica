using PrimoAutoEletrica.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PrimoAutoEletrica.Views
{
    public partial class SelecionarVendaWindow : Window
    {
        public Venda? VendaSelecionada { get; private set; }

        public SelecionarVendaWindow(
            IEnumerable<Venda> vendas,
            string titulo = "Selecionar venda",
            string subtitulo = "Escolha uma venda recente para continuar a operacao no PDV.",
            string textoConfirmacao = "Selecionar venda")
        {
            InitializeComponent();

            HeaderTextBlock.Text = titulo;
            SubheaderTextBlock.Text = subtitulo;
            SelecionarButton.Content = textoConfirmacao;
            VendasDataGrid.ItemsSource = vendas?
                .OrderByDescending(venda => venda.Data)
                .ToList()
                ?? new List<Venda>();
        }

        private void Selecionar_Click(object sender, RoutedEventArgs e)
        {
            ConfirmarSelecao();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void VendasDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmarSelecao();
        }

        private void ConfirmarSelecao()
        {
            if (VendasDataGrid.SelectedItem is not Venda venda)
            {
                MessageBox.Show("Selecione uma venda para continuar.", "Venda", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            VendaSelecionada = venda;
            DialogResult = true;
        }
    }
}
