using Microsoft.Extensions.DependencyInjection;
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
            _viewModel = App.Services.GetRequiredService<FuncionariosViewModel>();
            _permissionService = new PermissionService(funcionarioLogado, App.Logger, App.Database);

            DataContext = _viewModel;
            Loaded += FuncionariosControl_Loaded;
        }

        private Funcionario ObterFuncionarioLogado()
        {
            if (App.Session.CurrentUser is Funcionario funcionario)
                return funcionario;

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
            FuncionariosDataGrid.ItemsSource = _viewModel.FuncionariosFiltrados;
            _viewModel.CarregarFuncionarios();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                _viewModel.TextoBusca = tb.Text;
            _viewModel.FiltrarFuncionarios();
        }

        private void NovoFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.TemPermissaoCriar())
            {
                MessageBox.Show("Você não tem permissão para criar funcionários.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new NovoFuncionarioWindow(ObterFuncionarioLogado()) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                _viewModel.CarregarFuncionarios();
        }

        private void EditarFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.TemPermissaoEditar())
            {
                MessageBox.Show("Você não tem permissão para editar funcionários.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = ObterItemSelecionado(sender);
            if (item == null)
            {
                MessageBox.Show("Selecione um funcionario para editar.", "Funcionarios", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var funcionario = _viewModel.ObterFuncionarioPorId(item.Id);
            if (funcionario == null)
            {
                MessageBox.Show("Funcionario nao encontrado.", "Funcionarios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new EditarFuncionarioWindow(ObterFuncionarioLogado(), funcionario) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                _viewModel.CarregarFuncionarios();
        }

        private void ExcluirFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.TemPermissaoExcluir())
            {
                MessageBox.Show("Você não tem permissão para excluir funcionários.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = ObterItemSelecionado(sender);
            if (item == null)
            {
                MessageBox.Show("Selecione um funcionario para excluir.", "Funcionarios", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Deseja inativar o funcionario '{item.Nome}'?\n(A exclusao e logica e pode ser revertida.)",
                "Confirmar exclusao",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _viewModel.ExcluirFuncionario(item.Id);
                MessageBox.Show("Funcionario inativado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Falha ao excluir funcionario.", ex);
                MessageBox.Show($"Nao foi possivel excluir.\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FuncionarioListItem? ObterItemSelecionado(object? sender)
        {
            if (sender is FrameworkElement { Tag: FuncionarioListItem tagged })
                return tagged;
            if (sender is FrameworkElement { DataContext: FuncionarioListItem fromContext })
                return fromContext;
            return FuncionariosDataGrid.SelectedItem as FuncionarioListItem;
        }
    }
}
