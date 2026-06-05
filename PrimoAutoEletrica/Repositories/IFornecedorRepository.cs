using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Repositories
{
    public interface IFornecedorRepository
    {
        List<Fornecedor> ObterTodos();
        Fornecedor? ObterPorId(Guid id);
        Fornecedor? ObterPorCnpj(string cnpj);
        Fornecedor? ObterPorNomeFantasia(string nomeFantasia);
        void Inserir(Fornecedor fornecedor);
        void Atualizar(Fornecedor fornecedor);
        void Excluir(Guid id);
    }
}
