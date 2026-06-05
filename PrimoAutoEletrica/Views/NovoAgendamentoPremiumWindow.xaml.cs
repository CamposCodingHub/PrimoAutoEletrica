using System;
using System.Windows;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.Views
{
    public partial class NovoAgendamentoPremiumWindow : Window
    {
        public NovoAgendamentoPremiumWindow()
            : this(new NovoAgendamentoPremiumViewModel())
        {
        }

        public NovoAgendamentoPremiumWindow(NovoAgendamentoPremiumViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
