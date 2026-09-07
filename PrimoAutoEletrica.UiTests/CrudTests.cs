using System;
using Xunit;
using FlaUI.Core.AutomationElements;
using System.Drawing.Imaging;
using PrimoAutoEletrica.UiTests.Helpers;

namespace PrimoAutoEletrica.UiTests
{
    public class CrudTests : UiTestBase
    {
        [Fact]
        public void Cliente_Criar_Editar_Excluir()
        {
            // Navega para a tela de Clientes

            var btnClientes = UiElementFinder.FindById(MainWindow, NavigationIds.navClientes).AsButton();
            btnClientes.Invoke();
            // Thread.Sleep removido - uso de RetryHelper ou espera implícita

            // Clicar em Novo Cliente
            var btnNovo = UiElementFinder.FindById(MainWindow, NavigationIds.btnNovoCliente).AsButton();
            btnNovo.Invoke();
            //Thread.Sleep removido

            // Preencher campos
            var txtNome = UiElementFinder.FindById(MainWindow, NavigationIds.txtNomeCliente).AsTextBox();
            txtNome.Enter("Cliente Teste");
            var txtCpf = UiElementFinder.FindById(MainWindow, NavigationIds.txtCpfCliente).AsTextBox();
            txtCpf.Enter("12345678901");
            // Salvar
            var btnSalvar = UiElementFinder.FindById(MainWindow, NavigationIds.btnSalvarCliente).AsButton();
            btnSalvar.Invoke();
            //Thread.Sleep removido

            // Verificar que o cliente aparece na lista
            var lista = UiElementFinder.FindById(MainWindow, NavigationIds.lstClientes);
            Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste")));

            // Editar cliente
            var item = lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste"));
            item.Click();
            var btnEditar = UiElementFinder.FindById(MainWindow, NavigationIds.btnEditarCliente).AsButton();
            btnEditar.Invoke();
            var txtNomeEdit = UiElementFinder.FindById(MainWindow, "txtNomeCliente").AsTextBox();
            txtNomeEdit.Enter("Cliente Teste Editado");
            var btnSalvarEdit = UiElementFinder.FindById(MainWindow, "btnSalvarCliente").AsButton();
            btnSalvarEdit.Invoke();
            //Thread.Sleep removido
            Assert.NotNull(lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste Editado")));

            // Excluir cliente
            var itemEdit = lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste Editado"));
            itemEdit.Click();
            var btnExcluir = UiElementFinder.FindById(MainWindow, NavigationIds.btnExcluirCliente).AsButton();
            btnExcluir.Invoke();
            // Confirmar caixa de diálogo
            var btnConfirm = UiElementFinder.FindById(MainWindow, NavigationIds.btnConfirmExcluir).AsButton();
            btnConfirm.Invoke();
            RetryHelper.RetryWhile(() => lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste Editado")) == null, 5);
            Assert.Null(lista.FindFirstDescendant(cf => cf.ByName("Cliente Teste Editado")));
        }
    }
}
