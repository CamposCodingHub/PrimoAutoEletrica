using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class OrcamentosView : Window
    {
        public OrcamentosView()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.OrcamentosViewModel();
        }
    }
}
