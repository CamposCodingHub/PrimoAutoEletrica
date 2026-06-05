using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class AdicionarFornecedorDialog : Window
    {
        public bool AdicionarFornecedor { get; private set; }

        public AdicionarFornecedorDialog(string nomeFornecedor)
        {
            InitializeComponent();
            FornecedorNomeText.Text = nomeFornecedor;
        }

        private void SimButton_Click(object sender, RoutedEventArgs e)
        {
            AdicionarFornecedor = true;
            DialogResult = true;
            Close();
        }

        private void NaoButton_Click(object sender, RoutedEventArgs e)
        {
            AdicionarFornecedor = false;
            DialogResult = false;
            Close();
        }
    }
}
