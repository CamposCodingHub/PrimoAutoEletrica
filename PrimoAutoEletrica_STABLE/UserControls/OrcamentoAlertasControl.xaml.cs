using System.Windows.Controls;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoAlertasControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoAlertasControl()
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
