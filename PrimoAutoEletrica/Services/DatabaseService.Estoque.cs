using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        public void BaixarProdutosDoEstoquePorAgendamento(IEnumerable<AgendamentoProduto> produtos, Guid agendamentoId)
        {
            global::PrimoAutoEletrica.App.Repositories.Produtos.BaixarProdutosDoEstoquePorAgendamento(produtos, agendamentoId);
        }
    }
}
