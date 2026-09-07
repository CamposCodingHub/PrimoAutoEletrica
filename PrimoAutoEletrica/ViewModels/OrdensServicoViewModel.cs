using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace PrimoAutoEletrica.ViewModels
{
    public partial class OrdensServicoViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _title = "Ordens de Serviço";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedStatus = "Todas";

        public OrdensServicoViewModel()
        {
        }

        [RelayCommand]
        private void Search()
        {
            // Implementação de busca de ordens de serviço
        }

        [RelayCommand]
        private void CreateNew()
        {
            // Abrir diálogo para criar nova ordem de serviço
        }

        [RelayCommand]
        private void EditSelected()
        {
            // Editar ordem de serviço selecionada
        }

        [RelayCommand]
        private void UpdateStatus()
        {
            // Atualizar status da ordem de serviço
        }

        [RelayCommand]
        private void PrintOrder()
        {
            // Imprimir ordem de serviço
        }

        [RelayCommand]
        private void ExportToPDF()
        {
            // Exportar ordem de serviço para PDF
        }
    }
}