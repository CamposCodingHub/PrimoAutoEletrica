using System;
using Xunit;
using FlaUI.Core.AutomationElements;
using PrimoAutoEletrica.UiTests.Helpers;

namespace PrimoAutoEletrica.UiTests
{
    public class LoginTests : UiTestBase
    {
        [Fact]
        public void Login_Com_Credenciais_Correta_Deve_Abrir_Dashboard()
        {
            // Arrange - localizar campos e botão
            var usernameBox = UiElementFinder.FindById(MainWindow, NavigationIds.txtUsername).AsTextBox();
            var passwordBox = UiElementFinder.FindById(MainWindow, NavigationIds.txtPassword).AsTextBox();
            var loginButton = UiElementFinder.FindById(MainWindow, NavigationIds.btnLogin).AsButton();

            // Act
            usernameBox.Enter(NavigationIds.admin);
            passwordBox.Enter(NavigationIds.admin123);
            loginButton.Invoke();

            // Assert - a janela principal deve conter o controle do Dashboard
            var dashboardLabel = UiElementFinder.FindByName(MainWindow, NavigationIds.Dashboard);
            Assert.NotNull(dashboardLabel);
        }
    }
}
