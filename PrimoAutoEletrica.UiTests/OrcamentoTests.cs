using System;
using Xunit;
using FlaUI.Core.AutomationElements;
using PrimoAutoEletrica.UiTests.Helpers;

namespace PrimoAutoEletrica.UiTests
{
    /// <summary>
    /// Testes UI para o módulo de Orçamentos.
    /// </summary>
    public class OrcamentoTests : UiTestBase
    {
        [Fact]
        public void Criar_Editar_Excluir_Orcamento()
        {
            // Navega para a tela de Orçamentos
            var navButton = UiElementFinder.FindById(MainWindow, NavigationIds.navOrcamentos).AsButton();
            navButton.Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep

            // Cria novo orçamento
            var btnNovo = UiElementFinder.FindById(MainWindow, NavigationIds.btnNovoOrcamento).AsButton();
            btnNovo.Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep
            var txtCliente = UiElementFinder.FindById(MainWindow, NavigationIds.txtClienteOrcamento).AsTextBox();
            txtCliente.Enter("Cliente Teste");
            var txtValor = UiElementFinder.FindById(MainWindow, NavigationIds.txtValorOrcamento).AsTextBox();
            txtValor.Enter("1000");
            var btnSalvar = UiElementFinder.FindById(MainWindow, NavigationIds.btnSalvarOrcamento).AsButton();
            btnSalvar.Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep

            // Verifica que o orçamento aparece na lista
            var lista = UiElementFinder.FindById(MainWindow, NavigationIds.lstOrcamentos);
            Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste")));

            // Edita orçamento
            var item = lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste"));
            item.Click();
            var btnEditar = UiElementFinder.FindById(MainWindow, NavigationIds.btnEditarOrcamento).AsButton();
            btnEditar.Invoke();
            var txtValorEdit = UiElementFinder.FindById(MainWindow, NavigationIds.txtValorOrcamento).AsTextBox();
            txtValorEdit.Enter("1500");
            var btnSalvarEdit = UiElementFinder.FindById(MainWindow, NavigationIds.btnSalvarOrcamento).AsButton();
            btnSalvarEdit.Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep
            Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("1500")));

            // Exclui orçamento
            var itemEdit = lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste"));
            itemEdit.Click();
            var btnExcluir = UiElementFinder.FindById(MainWindow, NavigationIds.btnExcluirOrcamento).AsButton();
            btnExcluir.Invoke();
            var btnConfirm = UiElementFinder.FindById(MainWindow, NavigationIds.btnConfirmExcluirOrcamento).AsButton();
            btnConfirm.Invoke();
            RetryHelper.RetryWhile(() => MainWindow != null, 5); // substitui Thread.Sleep
            Assert.Null(lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste")));
        }
    }
}
