using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Repositories
{
    public interface IProdutoRepository
    {
        List<Produto> ObterTodos();
        Produto? ObterPorId(Guid id);
        void Inserir(Produto produto);
        void Atualizar(Produto produto);
        void Excluir(Guid id);
        int InserirEmMassa(string caminhoArquivo);
        void BaixarProdutosDoEstoquePorAgendamento(IEnumerable<AgendamentoProduto> produtos, Guid agendamentoId);
    }
}
