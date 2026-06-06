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
    public partial class EditarFuncionarioWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly Funcionario _funcionarioLogado;
        private readonly Funcionario _funcionarioParaEditar;
        private string _caminhoFotoSelecionada = string.Empty;
        private bool _removerFotoAtual;

        public EditarFuncionarioWindow(Funcionario funcionarioLogado, Funcionario funcionarioParaEditar)
        {
            InitializeComponent();
            _databaseService = App.Database;
            _funcionarioRepository = App.Repositories.Funcionarios;
            _funcionarioLogado = funcionarioLogado;
            _funcionarioParaEditar = funcionarioParaEditar;

            CarregarDadosFuncionario();
        }

        private void CarregarDadosFuncionario()
        {
            try
            {
                NomeTextBox.Text = _funcionarioParaEditar.Nome;
                EmailTextBox.Text = _funcionarioParaEditar.Email;
                TelefoneTextBox.Text = _funcionarioParaEditar.Telefone;
                CPFTextBox.Text = _funcionarioParaEditar.CPF;
                FuncaoTextBox.Text = _funcionarioParaEditar.Funcao;
                SalarioTextBox.Text = _funcionarioParaEditar.Salario.ToString();
                DataAdmissaoDatePicker.SelectedDate = _funcionarioParaEditar.DataAdmissao;
                UltimoAcessoTextBlock.Text = _funcionarioParaEditar.DataUltimoLogin?.ToString("dd/MM/yyyy HH:mm") ?? "Sem acesso registrado";
                DataCadastroTextBlock.Text = _funcionarioParaEditar.DataCadastro.ToString("dd/MM/yyyy HH:mm");

                AtualizarPreviewFoto(_funcionarioParaEditar.Foto);
                CarregarPerfisAcesso();
                SelecionarStatusAtual();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar dados do funcionario para edicao.", ex);
                ShowError($"Erro ao carregar dados: {ex.Message}");
            }
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

                foreach (ComboBoxItem item in PerfilComboBox.Items)
                {
                    if (item.Tag is string perfil && perfil == _funcionarioParaEditar.PerfilAcesso)
                    {
                        PerfilComboBox.SelectedItem = item;
                        break;
                    }
                }

                if (PerfilComboBox.SelectedItem == null && PerfilComboBox.Items.Count > 0)
                {
                    PerfilComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar perfis de acesso na edicao de funcionario.", ex);
                ShowError($"Erro ao carregar perfis: {ex.Message}");
            }
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
                _removerFotoAtual = false;
                AtualizarPreviewFoto(_caminhoFotoSelecionada);
            }
        }

        private void RemoverFotoButton_Click(object sender, RoutedEventArgs e)
        {
            _caminhoFotoSelecionada = string.Empty;
            _removerFotoAtual = true;
            AtualizarPreviewFoto(null);
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideError();

                if (!ValidarCampos(out var salario))
                {
                    return;
                }

                _funcionarioParaEditar.Nome = NomeTextBox.Text.Trim();
                _funcionarioParaEditar.CPF = CPFTextBox.Text.Trim();
                _funcionarioParaEditar.Email = EmailTextBox.Text.Trim();
                _funcionarioParaEditar.Telefone = TelefoneTextBox.Text.Trim();
                _funcionarioParaEditar.Funcao = FuncaoTextBox.Text.Trim();
                _funcionarioParaEditar.Salario = salario;
                _funcionarioParaEditar.DataAdmissao = DataAdmissaoDatePicker.SelectedDate!.Value;

                if (StatusComboBox.SelectedItem is ComboBoxItem statusSelecionado)
                {
                    _funcionarioParaEditar.Status = statusSelecionado.Content?.ToString() ?? _funcionarioParaEditar.Status;
                }

                _funcionarioParaEditar.Ativo =
                    !string.Equals(_funcionarioParaEditar.Status, "Inativo", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(_funcionarioParaEditar.Status, "Bloqueado", StringComparison.OrdinalIgnoreCase);

                if (PerfilComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string perfil)
                {
                    _funcionarioParaEditar.PerfilAcesso = perfil;
                }

                if (!string.IsNullOrEmpty(SenhaPasswordBox.Password))
                {
                    _funcionarioParaEditar.Senha = PasswordHasherService.HashPassword(SenhaPasswordBox.Password);
                }

                PersistirFotoSeNecessario();
                _funcionarioRepository.Atualizar(_funcionarioParaEditar);
                App.Logger.LogInfo($"Funcionario '{_funcionarioParaEditar.Email}' atualizado por '{_funcionarioLogado.Nome}'.");

                WindowInteractionHelper.ShowMessage(
                    "Funcionario atualizado com sucesso!",
                    "Sucesso",
                    MessageBoxImage.Information,
                    "Funcionarios");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Funcionarios");
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao salvar edicao de funcionario.", ex);
                ShowError($"Erro ao salvar: {ex.Message}");
            }
        }

        private bool ValidarCampos(out decimal salario)
        {
            salario = 0;

            if (string.IsNullOrWhiteSpace(NomeTextBox.Text))
            {
                ShowError("O nome e obrigatorio.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                ShowError("O email e obrigatorio.");
                return false;
            }

            var erroCpf = CadastroValidationHelper.ValidarCpf(CPFTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroCpf))
            {
                ShowError(erroCpf);
                return false;
            }

            var erroEmail = CadastroValidationHelper.ValidarEmail(EmailTextBox.Text, obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroEmail))
            {
                ShowError(erroEmail);
                return false;
            }

            if (string.IsNullOrWhiteSpace(FuncaoTextBox.Text))
            {
                ShowError("A funcao e obrigatoria.");
                return false;
            }

            var erroTelefone = CadastroValidationHelper.ValidarTelefone(TelefoneTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroTelefone))
            {
                ShowError(erroTelefone);
                return false;
            }

            if (!decimal.TryParse(SalarioTextBox.Text, out salario))
            {
                ShowError("O salario deve ser um valor numerico valido.");
                return false;
            }

            var erroSalario = CadastroValidationHelper.ValidarDecimal(salario, "um salario", permitirZero: false);
            if (!string.IsNullOrWhiteSpace(erroSalario))
            {
                ShowError(erroSalario);
                return false;
            }

            if (DataAdmissaoDatePicker.SelectedDate == null)
            {
                ShowError("A data de admissao e obrigatoria.");
                return false;
            }

            var erroData = CadastroValidationHelper.ValidarDataNaoFutura(DataAdmissaoDatePicker.SelectedDate, "a data de admissao", obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroData))
            {
                ShowError(erroData);
                return false;
            }

            if (StatusComboBox.SelectedItem == null)
            {
                ShowError("O status e obrigatorio.");
                return false;
            }

            if (PerfilComboBox.SelectedItem == null)
            {
                ShowError("O perfil de acesso e obrigatorio.");
                return false;
            }

            if (!string.IsNullOrEmpty(SenhaPasswordBox.Password))
            {
                if (SenhaPasswordBox.Password != ConfirmarSenhaPasswordBox.Password)
                {
                    ShowError("As senhas nao coincidem.");
                    return false;
                }

                if (SenhaPasswordBox.Password.Length < 6)
                {
                    ShowError("A senha deve ter pelo menos 6 caracteres.");
                    return false;
                }
            }

            return true;
        }

        private void PersistirFotoSeNecessario()
        {
            var fotoAtual = _funcionarioParaEditar.Foto;

            if (_removerFotoAtual)
            {
                FuncionarioMediaService.DeleteManagedImageIfOwned(fotoAtual);
                _funcionarioParaEditar.Foto = string.Empty;
                return;
            }

            if (string.IsNullOrWhiteSpace(_caminhoFotoSelecionada))
            {
                return;
            }

            var fotoPersistida = FuncionarioMediaService.PersistSelectedImage(_caminhoFotoSelecionada, _funcionarioParaEditar.Id, _funcionarioParaEditar.Nome);
            if (!string.IsNullOrWhiteSpace(fotoAtual) && !string.Equals(fotoAtual, fotoPersistida, StringComparison.OrdinalIgnoreCase))
            {
                FuncionarioMediaService.DeleteManagedImageIfOwned(fotoAtual);
            }

            _funcionarioParaEditar.Foto = fotoPersistida;
        }

        private void AtualizarPreviewFoto(string? caminho)
        {
            var preview = FuncionarioMediaService.TryCreatePreviewSource(caminho);
            FotoPreviewImage.Source = preview;
            FotoPreviewImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            FotoPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;
            RemoverFotoButton.IsEnabled = preview != null || !string.IsNullOrWhiteSpace(_caminhoFotoSelecionada) || !string.IsNullOrWhiteSpace(_funcionarioParaEditar.Foto);
        }

        private void SelecionarStatusAtual()
        {
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (string.Equals(item.Content?.ToString(), _funcionarioParaEditar.Status, StringComparison.OrdinalIgnoreCase))
                {
                    StatusComboBox.SelectedItem = item;
                    return;
                }
            }

            if (StatusComboBox.Items.Count > 0)
            {
                StatusComboBox.SelectedIndex = 0;
            }
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
