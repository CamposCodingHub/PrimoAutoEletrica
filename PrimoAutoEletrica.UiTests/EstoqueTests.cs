using System;
using Xunit;
using FlaUI.Core.AutomationElements;
using PrimoAutoEletrica.UiTests.Helpers;
using FlaUI.Core.Tools;

namespace PrimoAutoEletrica.UiTests
{
    public class EstoqueTests : UiTestBase
    {
        [Fact]
        public void Peça_Criar_Editar_Excluir()
        {
            try
            {
                // Navegar para a tela de Estoque
                UiElementFinder.FindById(MainWindow, NavigationIds.navEstoque).AsButton().Invoke();
                // Novo item
                UiElementFinder.FindById(MainWindow, NavigationIds.btnNovoEstoque).AsButton().Invoke();
                // Preencher campos
                UiElementFinder.FindById(MainWindow, NavigationIds.txtCodigo).AsTextBox().Enter("ABC123");
                UiElementFinder.FindById(MainWindow, NavigationIds.txtDescricao).AsTextBox().Enter("Peça Teste");
                UiElementFinder.FindById(MainWindow, NavigationIds.txtQuantidade).AsTextBox().Enter("10");
                // Salvar
                UiElementFinder.FindById(MainWindow, NavigationIds.btnSalvarEstoque).AsButton().Invoke();
                // Verificar presença na lista
                var lista = UiElementFinder.FindById(MainWindow, NavigationIds.lstEstoque);
                Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("ABC123")));
                // Editar item
                var item = lista.FindFirstDescendant(cf => cf.ByName("ABC123"));
                item.Click();
                UiElementFinder.FindById(MainWindow, NavigationIds.btnEditarEstoque).AsButton().Invoke();
                UiElementFinder.FindById(MainWindow, NavigationIds.txtDescricao).AsTextBox().Enter("Peça Teste Editada");
                UiElementFinder.FindById(MainWindow, "btnSalvarEstoque").AsButton().Invoke();
                Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("Peça Teste Editada")));
                // Excluir item
                var itemEdit = lista.FindFirstDescendant(cf => cf.ByName("Peça Teste Editada"));
                itemEdit.Click();
                UiElementFinder.FindById(MainWindow, NavigationIds.btnExcluirEstoque).AsButton().Invoke();
                UiElementFinder.FindById(MainWindow, NavigationIds.btnConfirmExcluir).AsButton().Invoke();
                Assert.Null(lista.FindFirstDescendant(cf => cf.ByName("Peça Teste Editada")));
            }
            catch (Exception)
            {
                ScreenshotHelper.Capture(MainWindow, "EstoqueTests");
                throw;
            }
        }
    }
}
