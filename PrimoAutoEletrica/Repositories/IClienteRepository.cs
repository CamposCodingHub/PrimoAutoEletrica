using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Repositories
{
    public interface IClienteRepository
    {
        List<Cliente> ObterTodos();
        Cliente? ObterPorId(Guid id);
        void Inserir(Cliente cliente);
        void Atualizar(Cliente cliente);
        void Excluir(Guid id);
        bool Restaurar(Guid id);
        List<Veiculo> ObterTodosVeiculos();
        List<Veiculo> ObterVeiculosPorClienteId(Guid clienteId);
        void SalvarVeiculo(Veiculo veiculo);
        void ExcluirVeiculo(Guid veiculoId);
        void SalvarVeiculosDoCliente(Guid clienteId, IEnumerable<Veiculo> veiculos);
    }
}
