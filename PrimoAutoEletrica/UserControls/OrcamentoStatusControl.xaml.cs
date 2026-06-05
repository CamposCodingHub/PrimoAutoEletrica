using System.Windows.Controls;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoStatusControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoStatusControl()
        {
            InitializeComponent();
        }

        public void SetViewModel(OrcamentosViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel.OrcamentoAtual;
        }
    }
}
