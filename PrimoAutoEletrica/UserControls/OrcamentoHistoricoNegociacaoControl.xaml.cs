using System.Windows.Controls;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoHistoricoNegociacaoControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoHistoricoNegociacaoControl()
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
