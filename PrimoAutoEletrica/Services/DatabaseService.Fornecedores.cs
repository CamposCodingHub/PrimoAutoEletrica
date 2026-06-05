using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        public void InserirFornecedor(Fornecedor fornecedor)
        {
            global::PrimoAutoEletrica.App.Repositories.Fornecedores.Inserir(fornecedor);
        }

        public List<Fornecedor> ObterTodosFornecedores()
        {
            return global::PrimoAutoEletrica.App.Repositories.Fornecedores.ObterTodos();
        }

        public Fornecedor? ObterFornecedorPorId(Guid id)
        {
            return global::PrimoAutoEletrica.App.Repositories.Fornecedores.ObterPorId(id);
        }

        public Fornecedor? ObterFornecedorPorCNPJ(string cnpj)
        {
            return global::PrimoAutoEletrica.App.Repositories.Fornecedores.ObterPorCnpj(cnpj);
        }

        public Fornecedor? ObterFornecedorPorNomeFantasia(string nomeFantasia)
        {
            return global::PrimoAutoEletrica.App.Repositories.Fornecedores.ObterPorNomeFantasia(nomeFantasia);
        }

        public void AtualizarFornecedor(Fornecedor fornecedor)
        {
            global::PrimoAutoEletrica.App.Repositories.Fornecedores.Atualizar(fornecedor);
        }

        public void ExcluirFornecedor(Guid id)
        {
            global::PrimoAutoEletrica.App.Repositories.Fornecedores.Excluir(id);
        }
    }
}
