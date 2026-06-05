using System;
using System.IO;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class AuditoriaRepositoryTests
    {
        [Fact]
        public void RegistrarEObterEvento_Basic_Works()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "PrimoAutoEletrica_Test", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var logger = new LoggerService();
            var db = new DatabaseService(tempDir, logger: logger);

            using var conn = db.GetConnection();
            conn.Open();

            var repo = new AuditoriaRepository(conn);

            var evento = new { Categoria = "Teste", Acao = "Inserir", Detalhes = "Evento de teste" };

            repo.RegistrarEvento(evento);

            var inicio = DateTime.UtcNow.AddMinutes(-5);
            var fim = DateTime.UtcNow.AddMinutes(5);

            var resultados = repo.ObterEventos(inicio, fim);

            Assert.NotNull(resultados);
            Assert.True(System.Linq.Enumerable.Any(resultados), "Nenhum evento de auditoria retornado.");
        }
    }
}
