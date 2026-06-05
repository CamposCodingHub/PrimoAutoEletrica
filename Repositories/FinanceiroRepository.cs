using System;
using System.Collections.Generic;
using System.Data;

namespace PrimoAutoEletrica.Repositories
{
    public class FinanceiroRepository
    {
        private readonly IDbConnection _connection;

        public FinanceiroRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public void RegistrarLancamento(object lancamento)
        {
            // TODO: implementar persistencia de lancamentos financeiros
            throw new NotImplementedException();
        }

        public IEnumerable<object> ObterResumoFinanceiro(DateTime inicio, DateTime fim)
        {
            // TODO: retornar resumo financeiro
            return Array.Empty<object>();
        }
    }
}
