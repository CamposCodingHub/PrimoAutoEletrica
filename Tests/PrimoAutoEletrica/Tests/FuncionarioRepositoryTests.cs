using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class FuncionarioRepositoryTests
    {
        [Fact]
        public void FuncionarioRepository_DeveExistir()
        {
            // Verifica se o arquivo FuncionarioRepository.cs existe
            var funcionarioRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FuncionarioRepository.cs";
            Assert.True(System.IO.File.Exists(funcionarioRepositoryPath), "FuncionarioRepository.cs deve existir");
        }

        [Fact]
        public void FuncionarioRepository_DeveTerMetodoObterTodos()
        {
            // Verifica se o arquivo contém o método ObterTodos
            var funcionarioRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FuncionarioRepository.cs";
            var content = System.IO.File.ReadAllText(funcionarioRepositoryPath);
            Assert.Contains("ObterTodos", content, "FuncionarioRepository deve ter método ObterTodos");
        }

        [Fact]
        public void FuncionarioRepository_DeveTerMetodoObterPorId()
        {
            // Verifica se o arquivo contém o método ObterPorId
            var funcionarioRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FuncionarioRepository.cs";
            var content = System.IO.File.ReadAllText(funcionarioRepositoryPath);
            Assert.Contains("ObterPorId", content, "FuncionarioRepository deve ter método ObterPorId");
        }

        [Fact]
        public void FuncionarioRepository_DeveTerMetodoAdicionar()
        {
            // Verifica se o arquivo contém o método Adicionar
            var funcionarioRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FuncionarioRepository.cs";
            var content = System.IO.File.ReadAllText(funcionarioRepositoryPath);
            Assert.Contains("Adicionar", content, "FuncionarioRepository deve ter método Adicionar");
        }

        [Fact]
        public void FuncionarioRepository_DeveTerMetodoAtualizar()
        {
            // Verifica se o arquivo contém o método Atualizar
            var funcionarioRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FuncionarioRepository.cs";
            var content = System.IO.File.ReadAllText(funcionarioRepositoryPath);
            Assert.Contains("Atualizar", content, "FuncionarioRepository deve ter método Atualizar");
        }

        [Fact]
        public void FuncionarioRepository_DeveTerMetodoExcluir()
        {
            // Verifica se o arquivo contém o método Excluir
            var funcionarioRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\FuncionarioRepository.cs";
            var content = System.IO.File.ReadAllText(funcionarioRepositoryPath);
            Assert.Contains("Excluir", content, "FuncionarioRepository deve ter método Excluir");
        }
    }
}
