using System;
using System.Collections.Generic;
using System.Data;

namespace PrimoAutoEletrica.Repositories
{
    public class AgendamentoRepository
    {
        private readonly IDbConnection _connection;

        public AgendamentoRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public void InserirAgendamento(object agendamento)
        {
            // TODO: implementar insercao de agendamento
            throw new NotImplementedException();
        }

        public IEnumerable<object> ObterAgendamentosProximos(DateTime data)
        {
            return Array.Empty<object>();
        }
    }
}
