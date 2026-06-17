using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class FornecedorRepositoryTests
    {
        [Fact]
        public void FornecedorRepository_DeveExistir()
        {
            // Verifica se o arquivo FornecedorRepository.cs existe
            var fornecedorRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FornecedorRepository.cs";
            Assert.True(System.IO.File.Exists(fornecedorRepositoryPath), "FornecedorRepository.cs deve existir");
        }

        [Fact]
        public void FornecedorRepository_DeveTerMetodoObterTodos()
        {
            // Verifica se o arquivo contém o método ObterTodos
            var fornecedorRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FornecedorRepository.cs";
            var content = System.IO.File.ReadAllText(fornecedorRepositoryPath);
            Assert.Contains("ObterTodos", content, "FornecedorRepository deve ter método ObterTodos");
        }

        [Fact]
        public void FornecedorRepository_DeveTerMetodoObterPorId()
        {
            // Verifica se o arquivo contém o método ObterPorId
            var fornecedorRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FornecedorRepository.cs";
            var content = System.IO.File.ReadAllText(fornecedorRepositoryPath);
            Assert.Contains("ObterPorId", content, "FornecedorRepository deve ter método ObterPorId");
        }

        [Fact]
        public void FornecedorRepository_DeveTerMetodoAdicionar()
        {
            // Verifica se o arquivo contém o método Adicionar
            var fornecedorRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FornecedorRepository.cs";
            var content = System.IO.File.ReadAllText(fornecedorRepositoryPath);
            Assert.Contains("Adicionar", content, "FornecedorRepository deve ter método Adicionar");
        }

        [Fact]
        public void FornecedorRepository_DeveTerMetodoAtualizar()
        {
            // Verifica se o arquivo contém o método Atualizar
            var fornecedorRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FornecedorRepository.cs";
            var content = System.IO.File.ReadAllText(fornecedorRepositoryPath);
            Assert.Contains("Atualizar", content, "FornecedorRepository deve ter método Atualizar");
        }

        [Fact]
        public void FornecedorRepository_DeveTerMetodoExcluir()
        {
            // Verifica se o arquivo contém o método Excluir
            var fornecedorRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FornecedorRepository.cs";
            var content = System.IO.File.ReadAllText(fornecedorRepositoryPath);
            Assert.Contains("Excluir", content, "FornecedorRepository deve ter método Excluir");
        }
    }
}
