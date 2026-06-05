using System;
using System.Collections.Generic;
using System.Data;

namespace PrimoAutoEletrica.Repositories
{
    public class AuditoriaRepository
    {
        private readonly IDbConnection _connection;

        public AuditoriaRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public void RegistrarEvento(object evento)
        {
            // TODO: implementar registro de auditoria
            throw new NotImplementedException();
        }

        public IEnumerable<object> ObterEventos(DateTime inicio, DateTime fim)
        {
            return Array.Empty<object>();
        }
    }
}
