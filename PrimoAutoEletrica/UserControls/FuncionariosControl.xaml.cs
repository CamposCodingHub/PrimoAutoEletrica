using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class FuncionariosControl : UserControl
    {
        private readonly FuncionariosViewModel _viewModel;
        private readonly PermissionService _permissionService;

        public FuncionarioPainelOperacional? UltimoPainelOperacional { get; private set; }

        public FuncionariosControl()
        {
            InitializeComponent();

            var funcionarioLogado = ObterFuncionarioLogado();
            _viewModel = new FuncionariosViewModel(App.Repositories.Funcionarios, funcionarioLogado);
            _permissionService = new PermissionService(funcionarioLogado, App.Logger, App.Database);

            DataContext = _viewModel;
            Loaded += FuncionariosControl_Loaded;
        }

        private Funcionario ObterFuncionarioLogado()
        {
            if (global::PrimoAutoEletrica.App.Session.CurrentUser is Funcionario funcionario)
            {
                return funcionario;
            }

            return new Funcionario
            {
                Id = 1,
                Nome = "Administrador",
                Email = "admin@primoauto.com",
                PerfilAcesso = "Administrador",
                Ativo = true
            };
        }

        private void FuncionariosControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.CarregarFuncionarios();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FiltrarFuncionarios();
        }

        private void NovoFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.TemPermissaoCriar())
            {
                MessageBox.Show("Você não tem permissão para criar funcionários.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var funcionarioLogado = ObterFuncionarioLogado();
            var novoFuncionarioWindow = new NovoFuncionarioWindow(funcionarioLogado);
            novoFuncionarioWindow.Owner = Window.GetWindow(this);
            novoFuncionarioWindow.ShowDialog();

            if (novoFuncionarioWindow.DialogResult == true)
            {
                _viewModel.CarregarFuncionarios();
            }
        }

        private void EditarFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.TemPermissaoEditar())
            {
                MessageBox.Show("Você não tem permissão para editar funcionários.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Implementação de edição seria adicionada aqui
            MessageBox.Show("Funcionalidade de edição em desenvolvimento.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExcluirFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.TemPermissaoExcluir())
            {
                MessageBox.Show("Você não tem permissão para excluir funcionários.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Implementação de exclusão seria adicionada aqui
            MessageBox.Show("Funcionalidade de exclusão em desenvolvimento.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}