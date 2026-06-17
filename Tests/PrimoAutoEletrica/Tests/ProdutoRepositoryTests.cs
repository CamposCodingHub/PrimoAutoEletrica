using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class ProdutoRepositoryTests
    {
        [Fact]
        public void ProdutoRepository_DeveExistir()
        {
            // Verifica se o arquivo ProdutoRepository.cs existe
            var produtoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ProdutoRepository.cs";
            Assert.True(System.IO.File.Exists(produtoRepositoryPath), "ProdutoRepository.cs deve existir");
        }

        [Fact]
        public void ProdutoRepository_DeveTerMetodoObterTodos()
        {
            // Verifica se o arquivo contém o método ObterTodos
            var produtoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ProdutoRepository.cs";
            var content = System.IO.File.ReadAllText(produtoRepositoryPath);
            Assert.Contains("ObterTodos", content, "ProdutoRepository deve ter método ObterTodos");
        }

        [Fact]
        public void ProdutoRepository_DeveTerMetodoObterPorId()
        {
            // Verifica se o arquivo contém o método ObterPorId
            var produtoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ProdutoRepository.cs";
            var content = System.IO.File.ReadAllText(produtoRepositoryPath);
            Assert.Contains("ObterPorId", content, "ProdutoRepository deve ter método ObterPorId");
        }

        [Fact]
        public void ProdutoRepository_DeveTerMetodoAdicionar()
        {
            // Verifica se o arquivo contém o método Adicionar
            var produtoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ProdutoRepository.cs";
            var content = System.IO.File.ReadAllText(produtoRepositoryPath);
            Assert.Contains("Adicionar", content, "ProdutoRepository deve ter método Adicionar");
        }

        [Fact]
        public void ProdutoRepository_DeveTerMetodoAtualizar()
        {
            // Verifica se o arquivo contém o método Atualizar
            var produtoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ProdutoRepository.cs";
            var content = System.IO.File.ReadAllText(produtoRepositoryPath);
            Assert.Contains("Atualizar", content, "ProdutoRepository deve ter método Atualizar");
        }

        [Fact]
        public void ProdutoRepository_DeveTerMetodoExcluir()
        {
            // Verifica se o arquivo contém o método Excluir
            var produtoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ProdutoRepository.cs";
            var content = System.IO.File.ReadAllText(produtoRepositoryPath);
            Assert.Contains("Excluir", content, "ProdutoRepository deve ter método Excluir");
        }
    }
}
