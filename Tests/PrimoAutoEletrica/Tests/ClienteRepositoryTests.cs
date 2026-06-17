using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class ClienteRepositoryTests
    {
        [Fact]
        public void ClienteRepository_DeveExistir()
        {
            // Verifica se o arquivo ClienteRepository.cs existe
            var clienteRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ClienteRepository.cs";
            Assert.True(System.IO.File.Exists(clienteRepositoryPath), "ClienteRepository.cs deve existir");
        }

        [Fact]
        public void ClienteRepository_DeveTerMetodoObterTodos()
        {
            // Verifica se o arquivo contém o método ObterTodos
            var clienteRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ClienteRepository.cs";
            var content = System.IO.File.ReadAllText(clienteRepositoryPath);
            Assert.Contains("ObterTodos", content, "ClienteRepository deve ter método ObterTodos");
        }

        [Fact]
        public void ClienteRepository_DeveTerMetodoObterPorId()
        {
            // Verifica se o arquivo contém o método ObterPorId
            var clienteRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ClienteRepository.cs";
            var content = System.IO.File.ReadAllText(clienteRepositoryPath);
            Assert.Contains("ObterPorId", content, "ClienteRepository deve ter método ObterPorId");
        }

        [Fact]
        public void ClienteRepository_DeveTerMetodoAdicionar()
        {
            // Verifica se o arquivo contém o método Adicionar
            var clienteRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ClienteRepository.cs";
            var content = System.IO.File.ReadAllText(clienteRepositoryPath);
            Assert.Contains("Adicionar", content, "ClienteRepository deve ter método Adicionar");
        }

        [Fact]
        public void ClienteRepository_DeveTerMetodoAtualizar()
        {
            // Verifica se o arquivo contém o método Atualizar
            var clienteRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ClienteRepository.cs";
            var content = System.IO.File.ReadAllText(clienteRepositoryPath);
            Assert.Contains("Atualizar", content, "ClienteRepository deve ter método Atualizar");
        }

        [Fact]
        public void ClienteRepository_DeveTerMetodoExcluir()
        {
            // Verifica se o arquivo contém o método Excluir
            var clienteRepositoryPath = @"..\..\..\PrimoAutoEletrica\Repositories\ClienteRepository.cs";
            var content = System.IO.File.ReadAllText(clienteRepositoryPath);
            Assert.Contains("Excluir", content, "ClienteRepository deve ter método Excluir");
        }
    }
}
