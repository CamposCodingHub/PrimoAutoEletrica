using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class EstoqueWorkflowTests
    {
        [Fact]
        public void EstoqueViewModel_DeveExistir()
        {
            // Verifica se o arquivo EstoqueViewModel.cs existe
            var estoqueViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\EstoqueViewModel.cs";
            Assert.True(System.IO.File.Exists(estoqueViewModelPath), "EstoqueViewModel.cs deve existir");
        }

        [Fact]
        public void EstoqueViewModel_DeveTerMetodoAdicionarProduto()
        {
            // Verifica se o arquivo contém o método AdicionarProduto
            var estoqueViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\EstoqueViewModel.cs";
            var content = System.IO.File.ReadAllText(estoqueViewModelPath);
            Assert.Contains("AdicionarProduto", content, "EstoqueViewModel deve ter método AdicionarProduto");
        }

        [Fact]
        public void EstoqueViewModel_DeveTerMetodoRemoverProduto()
        {
            // Verifica se o arquivo contém o método RemoverProduto
            var estoqueViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\EstoqueViewModel.cs";
            var content = System.IO.File.ReadAllText(estoqueViewModelPath);
            Assert.Contains("RemoverProduto", content, "EstoqueViewModel deve ter método RemoverProduto");
        }

        [Fact]
        public void EstoqueViewModel_DeveTerMetodoEntradaEstoque()
        {
            // Verifica se o arquivo contém o método EntradaEstoque
            var estoqueViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\EstoqueViewModel.cs";
            var content = System.IO.File.ReadAllText(estoqueViewModelPath);
            Assert.Contains("EntradaEstoque", content, "EstoqueViewModel deve ter método EntradaEstoque");
        }

        [Fact]
        public void EstoqueViewModel_DeveTerMetodoSaidaEstoque()
        {
            // Verifica se o arquivo contém o método SaidaEstoque
            var estoqueViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\EstoqueViewModel.cs";
            var content = System.IO.File.ReadAllText(estoqueViewModelPath);
            Assert.Contains("SaidaEstoque", content, "EstoqueViewModel deve ter método SaidaEstoque");
        }
    }
}
