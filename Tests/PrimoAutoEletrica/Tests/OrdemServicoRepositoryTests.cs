using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class OrdemServicoRepositoryTests
    {
        [Fact]
        public void OrdemServicoRepository_DeveExistir()
        {
            // Verifica se o arquivo OrdemServicoRepository.cs existe
            var ordemServicoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs";
            Assert.True(System.IO.File.Exists(ordemServicoRepositoryPath), "OrdemServicoRepository.cs deve existir");
        }

        [Fact]
        public void OrdemServicoRepository_DeveTerMetodoObterTodos()
        {
            // Verifica se o arquivo contém o método ObterTodos
            var ordemServicoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs";
            var content = System.IO.File.ReadAllText(ordemServicoRepositoryPath);
            Assert.Contains("ObterTodos", content, "OrdemServicoRepository deve ter método ObterTodos");
        }

        [Fact]
        public void OrdemServicoRepository_DeveTerMetodoObterPorId()
        {
            // Verifica se o arquivo contém o método ObterPorId
            var ordemServicoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs";
            var content = System.IO.File.ReadAllText(ordemServicoRepositoryPath);
            Assert.Contains("ObterPorId", content, "OrdemServicoRepository deve ter método ObterPorId");
        }

        [Fact]
        public void OrdemServicoRepository_DeveTerMetodoAdicionar()
        {
            // Verifica se o arquivo contém o método Adicionar
            var ordemServicoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs";
            var content = System.IO.File.ReadAllText(ordemServicoRepositoryPath);
            Assert.Contains("Adicionar", content, "OrdemServicoRepository deve ter método Adicionar");
        }

        [Fact]
        public void OrdemServicoRepository_DeveTerMetodoAtualizar()
        {
            // Verifica se o arquivo contém o método Atualizar
            var ordemServicoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs";
            var content = System.IO.File.ReadAllText(ordemServicoRepositoryPath);
            Assert.Contains("Atualizar", content, "OrdemServicoRepository deve ter método Atualizar");
        }

        [Fact]
        public void OrdemServicoRepository_DeveTerMetodoExcluir()
        {
            // Verifica se o arquivo contém o método Excluir
            var ordemServicoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrdemServicoRepository.cs";
            var content = System.IO.File.ReadAllText(ordemServicoRepositoryPath);
            Assert.Contains("Excluir", content, "OrdemServicoRepository deve ter método Excluir");
        }
    }
}
