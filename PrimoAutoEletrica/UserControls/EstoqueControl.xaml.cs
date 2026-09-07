using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class EstoqueControl : UserControl
    {
        private readonly EstoqueViewModel _viewModel;
        private readonly PermissionService _permissionService;

        public EstoqueControl()
        {
            InitializeComponent();

            _viewModel = new EstoqueViewModel();
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);

            DataContext = _viewModel;
            Loaded += EstoqueControl_Loaded;
        }

        private void EstoqueControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.CarregarProdutos();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FiltrarProdutos();
        }

        private void NovoProduto_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissionService.TemPermissaoCodigo("ESTOQUE_CRIAR"))
            {
                MessageBox.Show("Você não tem permissão para criar produtos.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var novaProdutoWindow = new NovoProdutoWindow();
            novaProdutoWindow.Owner = Window.GetWindow(this);
            novaProdutoWindow.ShowDialog();

            if (novaProdutoWindow.DialogResult == true)
            {
                _viewModel.CarregarProdutos();
            }
        }

        private void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissionService.TemPermissaoCodigo("ESTOQUE_EDITAR"))
            {
                MessageBox.Show("Você não tem permissão para editar produtos.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Implementação de edição seria adicionada aqui
            MessageBox.Show("Funcionalidade de edição em desenvolvimento.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AjustarEstoque_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissionService.TemPermissaoCodigo("ESTOQUE_AJUSTAR"))
            {
                MessageBox.Show("Você não tem permissão para ajustar estoque.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Implementação de ajuste de estoque seria adicionada aqui
            MessageBox.Show("Funcionalidade de ajuste de estoque em desenvolvimento.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImprimirEtiqueta_Click(object sender, RoutedEventArgs e)
        {
            // Implementação de impressão de etiqueta seria adicionada aqui
            MessageBox.Show("Funcionalidade de impressão de etiqueta em desenvolvimento.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}