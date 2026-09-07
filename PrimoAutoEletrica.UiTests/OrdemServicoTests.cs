using System;
using Xunit;
using FlaUI.Core.AutomationElements;
using PrimoAutoEletrica.UiTests.Helpers;

namespace PrimoAutoEletrica.UiTests
{
    /// <summary>
    /// Testes UI para o módulo de Ordem de Serviço.
    /// </summary>
    public class OrdemServicoTests : UiTestBase
    {
        [Fact]
        public void Criar_Editar_Excluir_OrdemServico()
        {
            // Navega para a tela de Ordem de Serviço
            var navButton = UiElementFinder.FindById(MainWindow, NavigationIds.navOS).AsButton();
            navButton.Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep

            // Cria nova OS
            UiElementFinder.FindById(MainWindow, NavigationIds.btnNovoOS).AsButton().Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep
            UiElementFinder.FindById(MainWindow, NavigationIds.txtClienteOS).AsTextBox().Enter("Cliente Teste");
            UiElementFinder.FindById(MainWindow, NavigationIds.txtDescricaoOS).AsTextBox().Enter("Serviço de teste");
            UiElementFinder.FindById(MainWindow, NavigationIds.btnSalvarOS).AsButton().Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep

            var lista = UiElementFinder.FindById(MainWindow, NavigationIds.lstOS);
            Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste")));

            // Edita a OS criada
            var item = lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste"));
            item.Click();
            UiElementFinder.FindById(MainWindow, NavigationIds.btnEditarOS).AsButton().Invoke();
            UiElementFinder.FindById(MainWindow, NavigationIds.txtDescricaoOS).AsTextBox().Enter("Serviço atualizado");
            UiElementFinder.FindById(MainWindow, NavigationIds.btnSalvarOS).AsButton().Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep
            Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("Serviço atualizado")));

            // Exclui a OS
            var itemEdit = lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste"));
            itemEdit.Click();
            UiElementFinder.FindById(MainWindow, NavigationIds.btnExcluirOS).AsButton().Invoke();
            UiElementFinder.FindById(MainWindow, NavigationIds.btnConfirmExcluirOS).AsButton().Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep
            Assert.Null(lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste")));
        }
    }
}
