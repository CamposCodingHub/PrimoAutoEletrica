using System.Windows.Controls;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoTimelineControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoTimelineControl()
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
