using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoBuscaInteligenteControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoBuscaInteligenteControl()
        {
            InitializeComponent();
            BuscaBox.TextChanged += BuscaBox_TextChanged;
        }

        public void SetViewModel(OrcamentosViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void BuscaBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                var termoBusca = BuscaBox.Text.ToLower();
                // Filtrar orçamentos baseado no termo de busca
                // Isso pode ser expandido para buscar por nome do cliente, número, status, etc.
            }
        }

        private void BuscaBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(BuscaBox.Text))
            {
                // Executar busca ao pressionar Enter
            }
        }
    }
}
