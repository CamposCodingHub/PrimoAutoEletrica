using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace PrimoAutoEletrica.ViewModels
{
    public partial class FornecedoresViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _title = "Fornecedores";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public FornecedoresViewModel()
        {
        }

        [RelayCommand]
        private void Search()
        {
            // Implementação de busca de fornecedores
        }

        [RelayCommand]
        private void AddNew()
        {
            // Abrir diálogo para adicionar novo fornecedor
        }

        [RelayCommand]
        private void EditSelected()
        {
            // Editar fornecedor selecionado
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            // Excluir fornecedor selecionado
        }

        [RelayCommand]
        private void ExportToExcel()
        {
            // Exportar lista de fornecedores para Excel
        }
    }
}