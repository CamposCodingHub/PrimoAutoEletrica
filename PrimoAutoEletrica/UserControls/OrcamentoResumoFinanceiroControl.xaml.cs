using System.Windows.Controls;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoResumoFinanceiroControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoResumoFinanceiroControl()
        {
            InitializeComponent();
        }

        public void SetViewModel(OrcamentosViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }
    }
}
