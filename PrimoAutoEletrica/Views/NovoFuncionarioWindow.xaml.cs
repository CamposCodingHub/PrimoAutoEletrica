using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class NovoFuncionarioWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly Funcionario _funcionarioLogado;
        private string _caminhoFotoSelecionada = string.Empty;

        public NovoFuncionarioWindow(Funcionario funcionarioLogado)
        {
            InitializeComponent();
            _databaseService = global::PrimoAutoEletrica.App.Database;
            _funcionarioRepository = global::PrimoAutoEletrica.App.Repositories.Funcionarios;
            _funcionarioLogado = funcionarioLogado;

            CarregarPerfisAcesso();
            ConfigurarCampos();
            AtualizarPreviewFoto(null);
        }

        private void CarregarPerfisAcesso()
        {
            try
            {
                PerfilComboBox.Items.Clear();
                var perfis = _databaseService.ObterPerfisAcesso(incluirInativos: false);

                foreach (var perfil in perfis)
                {
                    var item = new ComboBoxItem
                    {
                        Content = perfil.Nome,
                        Tag = perfil.Nome
                    };

                    PerfilComboBox.Items.Add(item);
                }

                if (PerfilComboBox.Items.Count > 0)
                {
                    PerfilComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar perfis de acesso no cadastro de funcionario.", ex);
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao carregar perfis de acesso: {ex.Message}",
                    "Erro",
                    MessageBoxImage.Error,
                    "Funcionarios",
                    ex);
            }
        }

        private void ConfigurarCampos()
        {
            StatusComboBox.SelectedIndex = 0;
            DataAdmissaoDatePicker.SelectedDate = DateTime.Today;
            RemoverFotoButton.IsEnabled = false;

            SalarioTextBox.PreviewTextInput += (sender, e) =>
            {
                if (!char.IsDigit(e.Text[0]) && e.Text[0] != ',' && e.Text[0] != '.')
                {
                    e.Handled = true;
                }
            };
        }

        private void SelecionarFotoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = FuncionarioMediaService.SupportedImageFilter,
                CheckFileExists = true,
                Multiselect = false,
                Title = "Selecionar foto do funcionario"
            };

            if (dialog.ShowDialog(this) == true)
            {
                _caminhoFotoSelecionada = dialog.FileName;
                AtualizarPreviewFoto(_caminhoFotoSelecionada);
            }
        }

        private void RemoverFotoButton_Click(object sender, RoutedEventArgs e)
        {
            _caminhoFotoSelecionada = string.Empty;
            AtualizarPreviewFoto(null);
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarCampos())
                {
                    return;
                }

                var status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Ativo";
                var novoFuncionario = new Funcionario
                {
                    Nome = NomeTextBox.Text.Trim(),
                    CPF = CPFTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(),
                    Senha = PasswordHasherService.HashPassword(SenhaPasswordBox.Password),
                    Funcao = FuncaoTextBox.Text.Trim(),
                    Telefone = TelefoneTextBox.Text.Trim(),
                    DataAdmissao = DataAdmissaoDatePicker.SelectedDate ?? DateTime.Today,
                    Salario = decimal.TryParse(SalarioTextBox.Text, out var salario) ? salario : 0,
                    Status = status,
                    DataCadastro = DateTime.Now,
                    Ativo = !string.Equals(status, "Inativo", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(status, "Bloqueado", StringComparison.OrdinalIgnoreCase)
                };

                if (PerfilComboBox.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag is not string perfil)
                {
                    ShowError("Selecione um perfil de acesso valido.");
                    return;
                }

                novoFuncionario.PerfilAcesso = perfil;
                var novoId = _funcionarioRepository.Salvar(novoFuncionario);
                novoFuncionario.Id = novoId;

                PersistirFotoSeNecessario(novoFuncionario);

                App.Logger.LogInfo($"Funcionario '{novoFuncionario.Email}' criado por '{_funcionarioLogado.Nome}'.");
                WindowInteractionHelper.ShowMessage(
                    "Funcionario cadastrado com sucesso!",
                    "Sucesso",
                    MessageBoxImage.Information,
                    "Funcionarios");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Funcionarios");
            }
            catch (Exception ex)
            {
                ShowError($"Erro ao salvar funcionario: {ex.Message}");
            }
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text))
            {
                ShowError("Por favor, informe o nome do funcionario.");
                NomeTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                ShowError("Por favor, informe o email do funcionario.");
                EmailTextBox.Focus();
                return false;
            }

            var erroCpf = CadastroValidationHelper.ValidarCpf(CPFTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroCpf))
            {
                ShowError(erroCpf);
                CPFTextBox.Focus();
                return false;
            }

            var erroEmail = CadastroValidationHelper.ValidarEmail(EmailTextBox.Text, obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroEmail))
            {
                ShowError(erroEmail);
                EmailTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(SenhaPasswordBox.Password))
            {
                ShowError("Por favor, informe uma senha.");
                SenhaPasswordBox.Focus();
                return false;
            }

            if (SenhaPasswordBox.Password.Length < 6)
            {
                ShowError("A senha deve ter pelo menos 6 caracteres.");
                SenhaPasswordBox.Focus();
                return false;
            }

            if (SenhaPasswordBox.Password != ConfirmarSenhaPasswordBox.Password)
            {
                ShowError("A senha e a confirmacao nao conferem.");
                ConfirmarSenhaPasswordBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(FuncaoTextBox.Text))
            {
                ShowError("Por favor, informe a funcao do funcionario.");
                FuncaoTextBox.Focus();
                return false;
            }

            var erroTelefone = CadastroValidationHelper.ValidarTelefone(TelefoneTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroTelefone))
            {
                ShowError(erroTelefone);
                TelefoneTextBox.Focus();
                return false;
            }

            if (!decimal.TryParse(SalarioTextBox.Text, out var salario) || salario <= 0)
            {
                ShowError("Por favor, informe um salario valido.");
                SalarioTextBox.Focus();
                return false;
            }

            if (DataAdmissaoDatePicker.SelectedDate == null)
            {
                ShowError("Por favor, selecione a data de admissao.");
                DataAdmissaoDatePicker.Focus();
                return false;
            }

            var erroData = CadastroValidationHelper.ValidarDataNaoFutura(DataAdmissaoDatePicker.SelectedDate, "a data de admissao", obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroData))
            {
                ShowError(erroData);
                DataAdmissaoDatePicker.Focus();
                return false;
            }

            if (EmailJaExiste(EmailTextBox.Text.Trim()))
            {
                ShowError("Este email ja esta cadastrado no sistema.");
                EmailTextBox.Focus();
                return false;
            }

            HideError();
            return true;
        }

        private bool EmailJaExiste(string email)
        {
            try
            {
                return _funcionarioRepository.EmailExiste(email);
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao verificar email de funcionario.", ex);
                return false;
            }
        }

        private void PersistirFotoSeNecessario(Funcionario funcionario)
        {
            if (string.IsNullOrWhiteSpace(_caminhoFotoSelecionada))
            {
                return;
            }

            var fotoPersistida = FuncionarioMediaService.PersistSelectedImage(_caminhoFotoSelecionada, funcionario.Id, funcionario.Nome);
            funcionario.Foto = fotoPersistida;
            _funcionarioRepository.Atualizar(funcionario);
        }

        private void AtualizarPreviewFoto(string? caminho)
        {
            var preview = FuncionarioMediaService.TryCreatePreviewSource(caminho);
            FotoPreviewImage.Source = preview;
            FotoPreviewImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            FotoPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;
            RemoverFotoButton.IsEnabled = preview != null || !string.IsNullOrWhiteSpace(_caminhoFotoSelecionada);
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            WindowInteractionHelper.CloseWithDialogResult(this, false, "Funcionarios");
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
