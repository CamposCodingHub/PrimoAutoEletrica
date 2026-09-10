using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.Views
{
    public partial class GerenciarPerfisWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly Funcionario _funcionarioLogado;
        private List<PerfilAcesso> _perfis = new();
        private List<Permissao> _permissoes = new();
        private List<PermissaoPerfilItem> _permissoesPerfilAtual = new();
        private PerfilAcesso? _perfilSelecionado;

        public GerenciarPerfisWindow(Funcionario funcionarioLogado)
        {
            InitializeComponent();
            _databaseService = App.Database;
            _funcionarioLogado = funcionarioLogado;

            Loaded += GerenciarPerfisWindow_Loaded;
        }

        private void GerenciarPerfisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarPermissoes();
            CarregarPerfis();
        }

        private void CarregarPerfis()
        {
            try
            {
                _perfis = _databaseService.ObterPerfisAcesso();

                PerfisDataGrid.ItemsSource = _perfis
                    .OrderBy(p => p.OrdemExibicao)
                    .ThenBy(p => p.Nome)
                    .Select(p => new PerfilGridItem(
                        p.Id,
                        p.Nome,
                        p.Descricao,
                        p.NivelHierarquico,
                        p.Ativo ? "Ativo" : "Inativo"))
                    .ToList();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar perfis de acesso.", ex);
                MessageBox.Show($"Erro ao carregar perfis: {ex.Message}", UiText.T("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CarregarPermissoes()
        {
            try
            {
                _permissoes = _databaseService.ObterPermissoes(incluirInativas: false);
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar permissoes dos perfis.", ex);
                MessageBox.Show($"Erro ao carregar permissoes: {ex.Message}", UiText.T("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PerfisDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PerfisDataGrid.SelectedItem is not PerfilGridItem selectedItem)
            {
                DetalhesPanel.Visibility = Visibility.Collapsed;
                NenhumPerfilSelecionadoTextBlock.Visibility = Visibility.Visible;
                return;
            }

            _perfilSelecionado = _perfis.FirstOrDefault(p => p.Id == selectedItem.Id);

            if (_perfilSelecionado != null)
            {
                CarregarDetalhesPerfil();
                DetalhesPanel.Visibility = Visibility.Visible;
                NenhumPerfilSelecionadoTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void CarregarDetalhesPerfil()
        {
            if (_perfilSelecionado == null)
            {
                return;
            }

            NomePerfilTextBox.Text = _perfilSelecionado.Nome;
            DescricaoPerfilTextBox.Text = _perfilSelecionado.Descricao;

            var permissoesDoPerfil = _databaseService.ObterPermissoesDoPerfil(_perfilSelecionado.Id);
            _permissoesPerfilAtual = _permissoes
                .OrderBy(p => p.Modulo)
                .ThenBy(p => p.Nome)
                .Select(p => new PermissaoPerfilItem
                {
                    Id = p.Id,
                    Nome = $"{p.Nome} ({p.Modulo})",
                    Modulo = p.Modulo,
                    IsChecked = permissoesDoPerfil.Contains(p.Id)
                })
                .ToList();

            PermissoesItemsControl.ItemsSource = _permissoesPerfilAtual;
            ConfigurarEstadoEdicaoPerfil(false);
        }

        private void NovoPerfilButton_Click(object sender, RoutedEventArgs e)
        {
            var novoPerfilWindow = new NovoPerfilWindow(_funcionarioLogado);
            if (novoPerfilWindow.ShowDialog() == true)
            {
                CarregarPermissoes();
                CarregarPerfis();
            }
        }

        private void EditarPerfilButton_Click(object sender, RoutedEventArgs e)
        {
            TrySelecionarPerfilPeloContexto(sender);

            if (_perfilSelecionado == null)
            {
                return;
            }

            ConfigurarEstadoEdicaoPerfil(true);
            NomePerfilTextBox.Focus();
        }

        private void ExcluirPerfilButton_Click(object sender, RoutedEventArgs e)
        {
            TrySelecionarPerfilPeloContexto(sender);

            if (_perfilSelecionado == null)
            {
                return;
            }

            if (!_perfilSelecionado.PodeDeletar)
            {
                MessageBox.Show("Este perfil nao pode ser excluido pois e um perfil essencial do sistema.", UiText.T("Warning"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!CriticalActionDialogService.ConfirmarExclusao(
                this,
                "perfil",
                _perfilSelecionado.Nome,
                $"Descricao: {_perfilSelecionado.Descricao}\nNivel: {_perfilSelecionado.NivelHierarquico}",
                "O perfil sera removido do cadastro de acessos. Revise atribuicoes e usuarios vinculados antes de prosseguir."))
            {
                return;
            }

            try
            {
                _databaseService.ExcluirPerfilAcesso(_perfilSelecionado.Id, _funcionarioLogado.Nome);
                App.Logger.LogInfo($"Perfil '{_perfilSelecionado.Nome}' excluido por '{_funcionarioLogado.Nome}'.");

                MessageBox.Show("Perfil excluido com sucesso!", UiText.T("Success"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CarregarPerfis();
                _perfilSelecionado = null;
                PerfisDataGrid.SelectedItem = null;
                DetalhesPanel.Visibility = Visibility.Collapsed;
                NenhumPerfilSelecionadoTextBlock.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao excluir perfil de acesso.", ex);
                MessageBox.Show($"Erro ao excluir perfil: {ex.Message}", UiText.T("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SalvarPerfilButton_Click(object sender, RoutedEventArgs e)
        {
            if (_perfilSelecionado == null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(NomePerfilTextBox.Text))
                {
                    MessageBox.Show("O nome do perfil e obrigatorio.", UiText.T("Validation"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var permissoesMarcadas = _permissoesPerfilAtual
                    .Where(p => p.IsChecked)
                    .Select(p => p.Id)
                    .ToList();

                var nomeAtualizado = NomePerfilTextBox.Text.Trim();
                var descricaoAtualizada = DescricaoPerfilTextBox.Text.Trim();

                if (!CriticalActionDialogService.ConfirmarAcao(
                    this,
                    new CriticalActionRequest
                    {
                        WindowTitle = "Atualizar perfil",
                        Header = "Revisao de perfil de acesso",
                        Summary = $"Voce esta prestes a atualizar o perfil '{_perfilSelecionado.Nome}'.",
                        Details = $"Novo nome: {nomeAtualizado}\nNivel: {_perfilSelecionado.NivelHierarquico}\nPermissoes marcadas: {permissoesMarcadas.Count}",
                        Impact = "As permissoes salvas aqui passam a valer para todos os usuarios vinculados a este perfil.",
                        Keyword = "SALVAR",
                        ConfirmButtonText = "Salvar perfil"
                    }))
                {
                    return;
                }

                _perfilSelecionado.Nome = nomeAtualizado;
                _perfilSelecionado.Descricao = descricaoAtualizada;
                _databaseService.AtualizarPerfil(_perfilSelecionado, permissoesMarcadas, _funcionarioLogado.Nome);
                App.Logger.LogInfo($"Perfil '{_perfilSelecionado.Nome}' atualizado por '{_funcionarioLogado.Nome}'.");

                MessageBox.Show("Perfil atualizado com sucesso!", UiText.T("Success"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CarregarPerfis();
                ConfigurarEstadoEdicaoPerfil(false);
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao salvar perfil de acesso.", ex);
                MessageBox.Show($"Erro ao salvar perfil: {ex.Message}", UiText.T("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelarPerfilButton_Click(object sender, RoutedEventArgs e)
        {
            if (_perfilSelecionado != null)
            {
                CarregarDetalhesPerfil();
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfigurarEstadoEdicaoPerfil(bool emEdicao)
        {
            if (_perfilSelecionado == null)
            {
                NomePerfilTextBox.IsReadOnly = true;
                DescricaoPerfilTextBox.IsReadOnly = true;
                PermissoesItemsControl.IsEnabled = false;
                SalvarPerfilButton.IsEnabled = false;
                CancelarPerfilButton.IsEnabled = false;
                return;
            }

            NomePerfilTextBox.IsReadOnly = !emEdicao || !_perfilSelecionado.PodeDeletar;
            DescricaoPerfilTextBox.IsReadOnly = !emEdicao;
            PermissoesItemsControl.IsEnabled = emEdicao;
            SalvarPerfilButton.IsEnabled = emEdicao;
            CancelarPerfilButton.IsEnabled = emEdicao;
        }

        private void TrySelecionarPerfilPeloContexto(object sender)
        {
            if (sender is not FrameworkElement { DataContext: PerfilGridItem perfilGridItem })
            {
                return;
            }

            PerfisDataGrid.SelectedItem = perfilGridItem;
            _perfilSelecionado = _perfis.FirstOrDefault(p => p.Id == perfilGridItem.Id);

            if (_perfilSelecionado == null)
            {
                return;
            }

            CarregarDetalhesPerfil();
            DetalhesPanel.Visibility = Visibility.Visible;
            NenhumPerfilSelecionadoTextBlock.Visibility = Visibility.Collapsed;
        }

        private sealed record PerfilGridItem(
            int Id,
            string Nome,
            string Descricao,
            string NivelHierarquico,
            string Status);

        private sealed class PermissaoPerfilItem
        {
            public int Id { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string Modulo { get; set; } = string.Empty;
            public bool IsChecked { get; set; }
        }
    }
}
