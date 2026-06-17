using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class OrcamentoRepositoryTests
    {
        [Fact]
        public void OrcamentoRepository_DeveExistir()
        {
            // Verifica se o arquivo OrcamentoRepository.cs existe
            var orcamentoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrcamentoRepository.cs";
            Assert.True(System.IO.File.Exists(orcamentoRepositoryPath), "OrcamentoRepository.cs deve existir");
        }

        [Fact]
        public void OrcamentoRepository_DeveTerMetodoObterTodos()
        {
            // Verifica se o arquivo contém o método ObterTodos
            var orcamentoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrcamentoRepository.cs";
            var content = System.IO.File.ReadAllText(orcamentoRepositoryPath);
            Assert.Contains("ObterTodos", content, "OrcamentoRepository deve ter método ObterTodos");
        }

        [Fact]
        public void OrcamentoRepository_DeveTerMetodoObterPorId()
        {
            // Verifica se o arquivo contém o método ObterPorId
            var orcamentoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrcamentoRepository.cs";
            var content = System.IO.File.ReadAllText(orcamentoRepositoryPath);
            Assert.Contains("ObterPorId", content, "OrcamentoRepository deve ter método ObterPorId");
        }

        [Fact]
        public void OrcamentoRepository_DeveTerMetodoAdicionar()
        {
            // Verifica se o arquivo contém o método Adicionar
            var orcamentoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrcamentoRepository.cs";
            var content = System.IO.File.ReadAllText(orcamentoRepositoryPath);
            Assert.Contains("Adicionar", content, "OrcamentoRepository deve ter método Adicionar");
        }

        [Fact]
        public void OrcamentoRepository_DeveTerMetodoAtualizar()
        {
            // Verifica se o arquivo contém o método Atualizar
            var orcamentoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrcamentoRepository.cs";
            var content = System.IO.File.ReadAllText(orcamentoRepositoryPath);
            Assert.Contains("Atualizar", content, "OrcamentoRepository deve ter método Atualizar");
        }

        [Fact]
        public void OrcamentoRepository_DeveTerMetodoExcluir()
        {
            // Verifica se o arquivo contém o método Excluir
            var orcamentoRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\OrcamentoRepository.cs";
            var content = System.IO.File.ReadAllText(orcamentoRepositoryPath);
            Assert.Contains("Excluir", content, "OrcamentoRepository deve ter método Excluir");
        }
    }
}
