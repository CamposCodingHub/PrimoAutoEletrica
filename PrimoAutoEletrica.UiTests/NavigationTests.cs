using System;
using Xunit;
using FlaUI.Core.AutomationElements;
using PrimoAutoEletrica.UiTests.Helpers;

namespace PrimoAutoEletrica.UiTests
{
    public class NavigationTests : UiTestBase
    {
        [Fact]
        public void Navegar_entre_todas_as_telas_principais()
        {
            // Lista de AutomationIds dos botões de navegação principal
            var navigationIds = new[]
            {
                NavigationIds.navDashboard,
                NavigationIds.navClientes,
                NavigationIds.navVeiculos,
                NavigationIds.navOrcamento,
                NavigationIds.navOS,
                NavigationIds.navEstoque,
                NavigationIds.navFinanceiro
            };

            foreach (var navId in navigationIds)
            {
                var button = UiElementFinder.FindById(MainWindow, navId).AsButton();
                button.Invoke();

                var expectedTitle = navId.Replace("nav", "");
                var titleElement = UiElementFinder.FindByName(MainWindow, expectedTitle);
                Assert.NotNull(titleElement);
            }
        }
    }
}
