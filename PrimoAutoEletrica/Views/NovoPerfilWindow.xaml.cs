using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class NovoPerfilWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly Funcionario _funcionarioLogado;
        private List<Permissao> _permissoes = new();

        public NovoPerfilWindow(Funcionario funcionarioLogado)
        {
            InitializeComponent();
            _databaseService = global::PrimoAutoEletrica.App.Database;
            _funcionarioLogado = funcionarioLogado;
            
            Loaded += NovoPerfilWindow_Loaded;
        }

        private void NovoPerfilWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarPermissoes();
            NivelComboBox.SelectedIndex = 0; // Executivo
        }

        private void CarregarPermissoes()
        {
            try
            {
                PermissoesIniciaisStackPanel.Children.Clear();
                _permissoes = _databaseService.ObterPermissoes(incluirInativas: false);

                // Adicionar checkboxes de permissoes essenciais
                var permissoesEssenciais = _permissoes.Where(p => p.Codigo.Contains("DASHBOARD")).ToList();
                foreach (var permissao in permissoesEssenciais)
                {
                    var checkBox = new CheckBox
                    {
                        Content = permissao.Nome,
                        Tag = permissao.Id,
                        IsChecked = true,
                        IsEnabled = false, // Permissoes essenciais nao podem ser desmarcadas
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    PermissoesIniciaisStackPanel.Children.Add(checkBox);
                }

                // Adicionar outras permissoes comuns
                var permissoesComuns = _permissoes.Where(p => !p.Codigo.Contains("DASHBOARD")).Take(10).ToList();
                foreach (var permissao in permissoesComuns)
                {
                    var checkBox = new CheckBox
                    {
                        Content = permissao.Nome,
                        Tag = permissao.Id,
                        IsChecked = false,
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    PermissoesIniciaisStackPanel.Children.Add(checkBox);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar permissoes: {ex.Message}", "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarCampos())
                {
                    return;
                }

                var permissaoIds = PermissoesIniciaisStackPanel.Children
                    .OfType<CheckBox>()
                    .Where(checkBox => checkBox.IsChecked == true && checkBox.Tag is int)
                    .Select(checkBox => (int)checkBox.Tag)
                    .ToList();

                var perfil = new PerfilAcesso
                {
                    Nome = NomePerfilTextBox.Text.Trim(),
                    Descricao = DescricaoTextBox.Text.Trim(),
                    NivelHierarquico = (NivelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Operacional",
                    Ativo = true,
                    DataCriacao = DateTime.Now,
                    CriadoPor = _funcionarioLogado.Nome,
                    PodeDeletar = true,
                    OrdemExibicao = 999
                };

                if (!CriticalActionDialogService.ConfirmarAcao(
                    this,
                    new CriticalActionRequest
                    {
                        WindowTitle = "Criar perfil",
                        Header = "Criacao de perfil de acesso",
                        Summary = $"Voce esta prestes a criar o perfil '{perfil.Nome}'.",
                        Details = $"Nivel: {perfil.NivelHierarquico}\nPermissoes iniciais: {permissaoIds.Count}\nCriado por: {_funcionarioLogado.Nome}",
                        Impact = "O novo perfil podera ser atribuido a usuarios e liberar acessos operacionais imediatamente apos o cadastro.",
                        Keyword = "CRIAR",
                        ConfirmButtonText = "Criar perfil"
                    }))
                {
                    return;
                }

                _databaseService.CriarPerfilAcesso(perfil, permissaoIds, _funcionarioLogado.Nome);

                MessageBox.Show("Perfil criado com sucesso!", "Sucesso", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"Erro ao criar perfil: {ex.Message}");
            }
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NomePerfilTextBox.Text))
            {
                ShowError("Por favor, informe o nome do perfil.");
                NomePerfilTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(DescricaoTextBox.Text))
            {
                ShowError("Por favor, informe a descricao do perfil.");
                DescricaoTextBox.Focus();
                return false;
            }

            if (NivelComboBox.SelectedItem == null)
            {
                ShowError("Por favor, selecione o nivel hierarquico.");
                NivelComboBox.Focus();
                return false;
            }

            // Verificar se nome ja existe
            if (NomeJaExiste(NomePerfilTextBox.Text.Trim()))
            {
                ShowError("Este nome de perfil ja esta cadastrado no sistema.");
                NomePerfilTextBox.Focus();
                return false;
            }

            HideError();
            return true;
        }

        private bool NomeJaExiste(string nome)
        {
            try
            {
                return _databaseService.NomePerfilExiste(nome);
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao verificar nome de perfil.", ex);
                return false;
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}


