using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks de login, sessao, lockout e seguranca.

        private void RunLoginSessaoSegurancaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "LoginSessao:MensagensLockoutLogoutPermissoes", () =>
            {
                var funcionarioLogin = EnsureFuncionario("smoke-login-seguranca@primoauto.com", "Smoke Login Seguranca", "Vendedor");
                var vendedor = EnsureFuncionario("smoke-vendedor@primoauto.com", "Smoke Vendedor", "Vendedor");
                var startedAt = DateTime.Now.AddSeconds(-2);
                MainWindow? mainWindow = null;

                try
                {
                    ResetLoginAttempts(funcionarioLogin.Id);
                    ValidarMensagensLogin();
                    ValidarBloqueioTemporarioLogin(funcionarioLogin);

                    ResetLoginAttempts(funcionarioLogin.Id);
                    var loginOk = App.Database.AutenticarFuncionarioDetalhado(funcionarioLogin.Email, "Workflow@123");
                    if (!loginOk.IsSuccess || loginOk.Funcionario == null)
                    {
                        throw new InvalidOperationException("Login valido nao foi aceito apos resetar tentativas de seguranca.");
                    }

                    App.Session.StartSession(funcionarioLogin);
                    var userSessionService = new UserSessionService(App.Database, _logger, App.Session);
                    userSessionService.CreateSession();
                    var sessionId = App.Session.SessionId;

                    if (GetUserSessionActiveFlag(sessionId) != 1)
                    {
                        throw new InvalidOperationException("Sessao de usuario nao foi registrada como ativa no banco.");
                    }

                    mainWindow = new MainWindow(funcionarioLogin);
                    ShowWindowForInteraction(mainWindow);
                    InvokeButtonHandler(mainWindow, "MenuSair_Click", null);

                    if (GetUserSessionActiveFlag(sessionId) != 0)
                    {
                        throw new InvalidOperationException("Logout pelo shell nao encerrou a sessao ativa no banco.");
                    }

                    FecharJanelasLoginAbertas();

                    App.Session.StartSession(funcionarioLogin);
                    userSessionService = new UserSessionService(App.Database, _logger, App.Session);
                    userSessionService.CreateSession();
                    var inactivitySessionId = App.Session.SessionId;
                    DefinirUltimaAtividadeSessaoBanco(inactivitySessionId, DateTime.Now.AddMinutes(-10));
                    userSessionService.CleanExpiredSessions(timeoutMinutes: 1);

                    if (GetUserSessionActiveFlag(inactivitySessionId) != 0)
                    {
                        throw new InvalidOperationException("Sessao inativa nao foi expirada no banco pelo timeout configurado.");
                    }

                    SessionInactivityEventArgs? inactivityEvent = null;
                    using (var inactivityMonitor = new SessionInactivityService(
                               App.Session,
                               _logger,
                               timeout: TimeSpan.FromMinutes(1),
                               pollingInterval: TimeSpan.FromHours(1)))
                    {
                        inactivityMonitor.SessionExpired += (_, args) => inactivityEvent = args;
                        inactivityMonitor.Start();
                        DefinirUltimaAtividadeApp(DateTime.Now.AddMinutes(-2));
                        DispararVerificacaoInatividade(inactivityMonitor);
                    }

                    if (inactivityEvent == null || inactivityEvent.IdleFor < inactivityEvent.Timeout)
                    {
                        throw new InvalidOperationException("Monitor de inatividade nao disparou a expiracao da sessao.");
                    }

                    var adminPermissionService = new PermissionService(syntheticUser, _logger, App.Database);
                    if (!adminPermissionService.TemPermissao("Financeiro") ||
                        !adminPermissionService.TemPermissaoCodigo("FUNCIONARIOS_EXCLUIR"))
                    {
                        throw new InvalidOperationException("Perfil Administrador nao recebeu permissoes sensiveis esperadas.");
                    }

                    App.Session.StartSession(vendedor);
                    var permissionService = new PermissionService(vendedor, _logger, App.Database);
                    if (permissionService.TemPermissao("Financeiro"))
                    {
                        throw new InvalidOperationException("Perfil Vendedor recebeu acesso indevido ao modulo Financeiro.");
                    }

                    if (!ExisteAuditoriaPermissaoNegadaDesde(startedAt, "Financeiro", "Vendedor"))
                    {
                        throw new InvalidOperationException("Permissao negada por perfil nao foi registrada na auditoria.");
                    }
                }
                finally
                {
                    ResetLoginAttempts(funcionarioLogin.Id);

                    if (mainWindow?.IsVisible == true)
                    {
                        mainWindow.Close();
                    }

                    FecharJanelasLoginAbertas();
                    App.Session.StartSession(syntheticUser);
                }
            });
        }

        private static void ValidarMensagensLogin()
        {
            var window = new LoginWindow();

            try
            {
                ShowWindowForInteraction(window);
                SetTextBoxValue(window, "EmailTextBox", string.Empty);
                SetPasswordBoxValue(window, "SenhaPasswordBox", string.Empty);
                SetTextBoxValue(window, "SenhaTextBox", string.Empty);

                ClickButton(window, "LoginButton");
                AssertLoginError(window, "Digite o e-mail.");

                SetTextBoxValue(window, "EmailTextBox", "smoke-login-seguranca@primoauto.com");
                SetPasswordBoxValue(window, "SenhaPasswordBox", string.Empty);
                SetTextBoxValue(window, "SenhaTextBox", string.Empty);

                ClickButton(window, "LoginButton");
                AssertLoginError(window, "Digite a senha.");
            }
            finally
            {
                if (window.IsVisible)
                {
                    window.Close();
                }
            }
        }

        private void ValidarBloqueioTemporarioLogin(Funcionario funcionario)
        {
            LoginAuthenticationResult ultimoResultado = LoginAuthenticationResult.Invalid("Nao executado.");

            for (var tentativa = 1; tentativa <= 5; tentativa++)
            {
                ultimoResultado = App.Database.AutenticarFuncionarioDetalhado(funcionario.Email, $"SenhaErrada{tentativa}");
            }

            if (!ultimoResultado.IsLocked ||
                !ultimoResultado.BloqueadoAte.HasValue ||
                !ultimoResultado.MensagemUsuario.Contains("temporariamente bloqueada", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Autenticacao nao bloqueou a conta apos tentativas invalidas.");
            }

            var loginDuranteBloqueio = App.Database.AutenticarFuncionarioDetalhado(funcionario.Email, "Workflow@123");
            if (!loginDuranteBloqueio.IsLocked)
            {
                throw new InvalidOperationException("Login correto foi aceito enquanto a conta estava temporariamente bloqueada.");
            }
        }

        private static void AssertLoginError(LoginWindow window, string expectedMessage)
        {
            var errorTextBlock = FindElementByName<TextBlock>(window, "ErrorMessageTextBlock")
                ?? throw new InvalidOperationException("Mensagem de erro do login nao foi localizada.");

            if (errorTextBlock.Visibility != Visibility.Visible ||
                !string.Equals(errorTextBlock.Text, expectedMessage, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Mensagem de login esperada '{expectedMessage}', obtida '{errorTextBlock.Text}'.");
            }

            if (errorTextBlock.Foreground is not SolidColorBrush brush)
            {
                throw new InvalidOperationException("Contraste/cor da mensagem de erro do login nao corresponde ao padrao de alerta.");
            }

            // Fonte de verdade: DangerBrush do tema ativo (Light #DC2626 / Dark #EF4444).
            // Nao hardcodar #B91C1C legado — divergia do design system e gerava falso FAIL.
            var expectedBrush = window.TryFindResource("DangerBrush") as SolidColorBrush;
            var expected = expectedBrush?.Color ?? Color.FromRgb(0xDC, 0x26, 0x26);
            if (brush.Color != expected)
            {
                throw new InvalidOperationException(
                    $"Contraste/cor da mensagem de erro do login nao corresponde ao DangerBrush do tema " +
                    $"(obtido=#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}, " +
                    $"esperado=#{expected.R:X2}{expected.G:X2}{expected.B:X2}).");
            }
        }

        private static void SetPasswordBoxValue(DependencyObject root, string name, string value)
        {
            var passwordBox = FindElementByName<PasswordBox>(root, name)
                ?? throw new InvalidOperationException($"PasswordBox '{name}' nao foi localizado.");
            passwordBox.Password = value;
            PumpDispatcher();
        }

        private static void FecharJanelasLoginAbertas()
        {
            var loginWindows = Application.Current?.Windows
                .OfType<LoginWindow>()
                .ToList()
                ?? new List<LoginWindow>();

            foreach (var loginWindow in loginWindows)
            {
                if (loginWindow.IsVisible)
                {
                    loginWindow.Close();
                }
            }
        }

        private static void ResetLoginAttempts(int funcionarioId)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM LoginTentativasSeguranca
                WHERE FuncionarioId = @FuncionarioId;";
            command.Parameters.AddWithValue("@FuncionarioId", funcionarioId);
            command.ExecuteNonQuery();
        }

        private static int? GetUserSessionActiveFlag(Guid sessionId)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = string.Equals(App.Database.RuntimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase)
                ? @"
                SELECT TOP (1) IsActive
                FROM UserSessions
                WHERE SessionId = @SessionId
                ORDER BY LoginAt DESC;"
                : @"
                SELECT IsActive
                FROM UserSessions
                WHERE SessionId = @SessionId
                ORDER BY LoginAt DESC
                LIMIT 1;";
            command.Parameters.AddWithValue("@SessionId", sessionId.ToString());

            var result = command.ExecuteScalar();
            return result == null || result == DBNull.Value
                ? null
                : Convert.ToInt32(result);
        }

        private static void DefinirUltimaAtividadeSessaoBanco(Guid sessionId, DateTime lastSeenAt)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE UserSessions
                SET LastSeenAt = @LastSeenAt
                WHERE SessionId = @SessionId;";
            command.Parameters.AddWithValue("@LastSeenAt", lastSeenAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@SessionId", sessionId.ToString());

            if (command.ExecuteNonQuery() != 1)
            {
                throw new InvalidOperationException("Sessao de inatividade nao foi localizada para preparar o smoke.");
            }
        }

        private static void DefinirUltimaAtividadeApp(DateTime lastActivityAt)
        {
            var setter = typeof(AppSessionService)
                .GetProperty(nameof(AppSessionService.LastActivityAt), BindingFlags.Instance | BindingFlags.Public)
                ?.GetSetMethod(nonPublic: true)
                ?? throw new MissingMethodException(typeof(AppSessionService).FullName, $"set_{nameof(AppSessionService.LastActivityAt)}");

            setter.Invoke(App.Session, new object[] { lastActivityAt });
        }

        private static void DispararVerificacaoInatividade(SessionInactivityService service)
        {
            var method = typeof(SessionInactivityService)
                .GetMethod("OnTimerTick", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(SessionInactivityService).FullName, "OnTimerTick");

            method.Invoke(service, new object?[] { null, EventArgs.Empty });
        }

        private static bool ExisteAuditoriaPermissaoNegadaDesde(DateTime startedAt, string modulo, string perfil)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(1)
                FROM AuditLogs
                WHERE Categoria = 'Seguranca'
                  AND Acao = 'PermissaoNegada'
                  AND Sucesso = 0
                  AND DataHora >= @StartedAt
                  AND COALESCE(Detalhes, '') LIKE @Modulo
                  AND COALESCE(Detalhes, '') LIKE @Perfil;";
            command.Parameters.AddWithValue("@StartedAt", startedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@Modulo", $"%Alvo={modulo}%");
            command.Parameters.AddWithValue("@Perfil", $"%Perfil={perfil}%");

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

    }
}
