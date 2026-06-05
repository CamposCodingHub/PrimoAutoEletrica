using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoClientePanelControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoClientePanelControl()
        {
            InitializeComponent();
        }

        public void SetViewModel(OrcamentosViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void CadastroRapido_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            var clientesAntes = App.Repositories.Clientes.ObterTodos().Select(cliente => cliente.Id).ToHashSet();
            var janela = new NovoClienteWindow();
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (janela.ShowDialog() != true)
            {
                return;
            }

            var clienteCriado = App.Repositories.Clientes
                .ObterTodos()
                .Where(cliente => !clientesAntes.Contains(cliente.Id))
                .OrderByDescending(cliente => cliente.DataCadastro)
                .FirstOrDefault();

            if (clienteCriado != null)
            {
                _viewModel.DefinirClienteAtual(clienteCriado);
                MessageBox.Show(
                    $"Cliente {clienteCriado.Nome} vinculado ao orcamento atual.",
                    "Orcamentos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BuscarCliente_Click(object sender, RoutedEventArgs e)
        {
            var cliente = _viewModel?.ClienteAtual;
            if (cliente == null)
            {
                MessageBox.Show(
                    "Cadastre ou vincule um cliente ao orcamento para continuar.",
                    "Orcamentos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var historicoWindow = new HistoricoClienteWindow(cliente);
            WindowOwnerHelper.ConfigureOwner(historicoWindow, this);
            historicoWindow.ShowDialog();
        }

        private void HistoricoCompleto_Click(object sender, RoutedEventArgs e)
        {
            var cliente = _viewModel?.ClienteAtual;
            if (cliente == null)
            {
                MessageBox.Show(
                    "Nenhum cliente vinculado ao orcamento atual.",
                    "Orcamentos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var historicoWindow = new HistoricoClienteWindow(cliente);
            WindowOwnerHelper.ConfigureOwner(historicoWindow, this);
            historicoWindow.ShowDialog();
        }
    }
}
