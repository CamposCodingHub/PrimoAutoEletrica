using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoProdutosPanelControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoProdutosPanelControl()
        {
            InitializeComponent();
        }

        public void SetViewModel(OrcamentosViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void Produto_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null ||
                sender is not FrameworkElement element ||
                element.Tag is not Produto produto)
            {
                return;
            }

            if (_viewModel.OrcamentoAtual == null)
            {
                _viewModel.NovoOrcamento();
            }

            var quantidadeWindow = new QuantidadeProdutoWindow(produto);
            WindowOwnerHelper.ConfigureOwner(quantidadeWindow, this);

            if (quantidadeWindow.ShowDialog() == true && quantidadeWindow.Quantidade > 0)
            {
                _viewModel.AdicionarItemAoCarrinho(produto, quantidadeWindow.Quantidade);
            }
        }
    }
}
