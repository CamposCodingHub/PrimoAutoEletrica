using System;
using System.Collections.Generic;
using System.Data;

namespace PrimoAutoEletrica.Repositories
{
    public class OrcamentoRepository
    {
        private readonly IDbConnection _connection;

        public OrcamentoRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public void InserirOrcamento(object orcamento)
        {
            // TODO: implementar persistencia de orcamento
            throw new NotImplementedException();
        }

        public IEnumerable<object> ObterOrcamentosCliente(int clienteId)
        {
            return Array.Empty<object>();
        }
    }
}
