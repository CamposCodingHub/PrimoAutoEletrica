using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class PdvWorkflowTests
    {
        [Fact]
        public void PdvViewModel_DeveExistir()
        {
            // Verifica se o arquivo PdvViewModel.cs existe
            var pdvViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\PdvViewModel.cs";
            Assert.True(System.IO.File.Exists(pdvViewModelPath), "PdvViewModel.cs deve existir");
        }

        [Fact]
        public void PdvViewModel_DeveTerMetodoAdicionarItem()
        {
            // Verifica se o arquivo contém o método AdicionarItem
            var pdvViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\PdvViewModel.cs";
            var content = System.IO.File.ReadAllText(pdvViewModelPath);
            Assert.Contains("AdicionarItem", content, "PdvViewModel deve ter método AdicionarItem");
        }

        [Fact]
        public void PdvViewModel_DeveTerMetodoRemoverItem()
        {
            // Verifica se o arquivo contém o método RemoverItem
            var pdvViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\PdvViewModel.cs";
            var content = System.IO.File.ReadAllText(pdvViewModelPath);
            Assert.Contains("RemoverItem", content, "PdvViewModel deve ter método RemoverItem");
        }

        [Fact]
        public void PdvViewModel_DeveTerMetodoFinalizarVenda()
        {
            // Verifica se o arquivo contém o método FinalizarVenda
            var pdvViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\PdvViewModel.cs";
            var content = System.IO.File.ReadAllText(pdvViewModelPath);
            Assert.Contains("FinalizarVenda", content, "PdvViewModel deve ter método FinalizarVenda");
        }

        [Fact]
        public void PdvViewModel_DeveTerMetodoCancelarVenda()
        {
            // Verifica se o arquivo contém o método CancelarVenda
            var pdvViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\PdvViewModel.cs";
            var content = System.IO.File.ReadAllText(pdvViewModelPath);
            Assert.Contains("CancelarVenda", content, "PdvViewModel deve ter método CancelarVenda");
        }
    }
}
