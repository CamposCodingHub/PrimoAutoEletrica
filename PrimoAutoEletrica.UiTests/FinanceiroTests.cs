using System;
using Xunit;
using FlaUI.Core.AutomationElements;
using PrimoAutoEletrica.UiTests.Helpers;
using FlaUI.Core.Tools;

namespace PrimoAutoEletrica.UiTests
{
    public class FinanceiroTests : UiTestBase
    {
        [Fact]
        public void Fluxo_Financeiro_Criar_Excluir_Entrada()
        {
            try
            {
                // Navegar para Financeiro
                UiElementFinder.FindById(MainWindow, NavigationIds.navFinanceiro).AsButton().Invoke();
                // Definir a lista de entradas
                var lista = UiElementFinder.FindById(MainWindow, NavigationIds.lstEntradas);
                // Navegar para a tela de Financeiro e abrir nova entrada
                UiElementFinder.FindById(MainWindow, NavigationIds.navFinanceiro).AsButton().Invoke();
                UiElementFinder.FindById(MainWindow, NavigationIds.btnNovaEntrada).AsButton().Invoke();
                UiElementFinder.FindById(MainWindow, NavigationIds.txtDescricao).AsTextBox().Enter("Teste Entrada");
                UiElementFinder.FindById(MainWindow, NavigationIds.txtValor).AsTextBox().Enter("500");
                UiElementFinder.FindById(MainWindow, NavigationIds.btnSalvarEntrada).AsButton().Invoke();
                // Verificar na lista
                var navButton = UiElementFinder.FindById(MainWindow, NavigationIds.navOrcamentos).AsButton();
                Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("Teste Entrada")));
                // Excluir entrada
                var item = lista.FindFirstDescendant(cf => cf.ByName("Teste Entrada"));
                item.Click();
                UiElementFinder.FindById(MainWindow, NavigationIds.btnExcluirEntrada).AsButton().Invoke();
                UiElementFinder.FindById(MainWindow, NavigationIds.btnConfirmExcluir).AsButton().Invoke();
                // Aguarda remoção da entrada
                RetryHelper.RetryWhile(() => lista.FindFirstDescendant(cf => cf.ByName("Teste Entrada")) != null, 5);
                Assert.Null(lista.FindFirstDescendant(cf => cf.ByName("Teste Entrada")));
            }
            catch (Exception)
            {
                ScreenshotHelper.Capture(MainWindow, "FinanceiroTests");
                throw;
            }
        }
    }
}
