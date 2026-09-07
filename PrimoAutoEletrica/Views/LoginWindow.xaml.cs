using Microsoft.Win32;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace PrimoAutoEletrica.Views
{
    public partial class LoginWindow : Window
    {
        private readonly DatabaseService _databaseService = null!;
        private readonly LoggerService _logger;
        private readonly UserSessionService _userSessionService;
        private Funcionario? _funcionarioLogado;
        private bool _mostrarSenha;
        private readonly ViewModels.LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();

            _databaseService = global::PrimoAutoEletrica.App.Database;
            _logger = App.Logger;
            _userSessionService = new UserSessionService(_databaseService, _logger, global::PrimoAutoEletrica.App.Session);
            _viewModel = new ViewModels.LoginViewModel(_databaseService, _logger, _userSessionService);
            DataContext = _viewModel;

            Loaded += LoginWindow_Loaded;

            EmailTextBox.KeyDown += TextBox_KeyDown;
            SenhaPasswordBox.KeyDown += TextBox_KeyDown;
            SenhaTextBox.KeyDown += TextBox_KeyDown;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModels.LoginViewModel.ErrorMessage) || e.PropertyName == nameof(ViewModels.LoginViewModel.HasError))
            {
                Dispatcher.Invoke(() =>
                {
                    ErrorMessageTextBlock.Text = _viewModel.ErrorMessage;
                    ErrorMessageTextBlock.Visibility = _viewModel.HasError ? Visibility.Visible : Visibility.Collapsed;
                });
            }
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadSavedCredentials();

            EmailTextBox.Text = _viewModel.Email;
            LembrarCheckBox.IsChecked = _viewModel.Remember;

            if (!string.IsNullOrWhiteSpace(_viewModel.Email))
            {
                SenhaPasswordBox.Focus();
                return;
            }

            EmailTextBox.Focus();
        }

        private void MostrarSenhaButton_Click(object sender, RoutedEventArgs e)
        {
            _mostrarSenha = !_mostrarSenha;

            if (_mostrarSenha)
            {
                SenhaTextBox.Text = SenhaPasswordBox.Password;
                _viewModel.Password = SenhaPasswordBox.Password;
                SenhaPasswordBox.Visibility = Visibility.Collapsed;
                SenhaTextBox.Visibility = Visibility.Visible;
                ((TextBlock)MostrarSenhaButton.Content).Text = "Ocultar";
                return;
            }

            SenhaPasswordBox.Password = SenhaTextBox.Text;
            _viewModel.Password = SenhaTextBox.Text;
            SenhaPasswordBox.Visibility = Visibility.Visible;
            SenhaTextBox.Visibility = Visibility.Collapsed;
            ((TextBlock)MostrarSenhaButton.Content).Text = "Mostrar";
        }

        private void SenhaPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_mostrarSenha)
            {
                SenhaTextBox.Text = SenhaPasswordBox.Password;
            }

            _viewModel.Password = SenhaPasswordBox.Password;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // delegate to ViewModel
                _viewModel.Email = EmailTextBox.Text?.Trim() ?? string.Empty;
                _viewModel.Remember = LembrarCheckBox.IsChecked == true;
                _viewModel.Password = _mostrarSenha ? SenhaTextBox.Text : SenhaPasswordBox.Password;

                if (!_viewModel.Login())
                {
                    ErrorMessageTextBlock.Text = _viewModel.ErrorMessage;
                    ErrorMessageTextBlock.Visibility = _viewModel.HasError ? Visibility.Visible : Visibility.Collapsed;
                    return;
                }

                if (_viewModel.AuthenticatedFuncionario == null)
                {
                    throw new InvalidOperationException("Funcionario logado nao pode ser nulo apos login bem-sucedido.");
                }

                _funcionarioLogado = _viewModel.AuthenticatedFuncionario;

                // Verificar se há múltiplas filiais disponíveis
                var filialService = App.Services.GetRequiredService<FilialService>();
                await filialService.CarregarFiliaisAsync();
                
                if (filialService.TemFiliaisDisponiveis())
                {
                    var selecaoFilialWindow = new SelecaoFilialWindow();
                    if (selecaoFilialWindow.ShowDialog() == true && selecaoFilialWindow.FilialSelecionada != null)
                    {
                        // Filial selecionada, continuar para MainWindow
                        var mainWindow = new MainWindow(_funcionarioLogado);
                        Application.Current.MainWindow = mainWindow;
                        mainWindow.Show();
                        Close();
                    }
                    else
                    {
                        // Usário cancelou seleção de filial
                        return;
                    }
                }
                else
                {
                    // Nenhuma filial disponível ou apenas uma, usar padrão
                    var filialPadrao = filialService.ObterMatriz();
                    if (filialPadrao != null)
                    {
                        filialService.DefinirFilialAtual(filialPadrao.Id);
                    }
                    
                    var mainWindow = new MainWindow(_funcionarioLogado);
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro inesperado ao processar login.", ex);

                MessageBox.Show(
                    "Nao foi possivel concluir o login. Consulte os logs para mais detalhes.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        

        private void EsqueciSenhaButton_Click(object sender, RoutedEventArgs e)
        {
            App.Audit.RegistrarAcaoCritica("Seguranca", "SolicitarRecuperacaoSenha", "Login", EmailTextBox.Text.Trim());

            MessageBox.Show(
                "Solicite a redefinicao de senha a um administrador do sistema. A tentativa foi registrada para auditoria.",
                "Recuperacao de Senha",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("Encerramento solicitado na tela de login.");
            App.Audit.RegistrarSistema("EncerramentoLogin", "Encerramento solicitado na tela de login.");
            _userSessionService.EndSession();
            global::PrimoAutoEletrica.App.Session.EndSession();
            Application.Current.Shutdown();
        }

    }
}

