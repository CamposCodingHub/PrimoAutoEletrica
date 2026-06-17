using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Repositories
{
    public interface IOrdemServicoRepository
    {
        string GerarProximoNumero();
        List<OrdemServico> ObterTodos(bool incluirInativas = false);
        List<OrdemServico> ObterPorClienteId(Guid clienteId, bool incluirInativas = false);
        OrdemServico? ObterPorId(Guid id);
        void Inserir(OrdemServico ordem);
        void Atualizar(OrdemServico ordem);
        void Excluir(Guid id);
    }
}
