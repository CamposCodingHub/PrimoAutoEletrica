using System;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class AgendamentoNullDurationMaterializationTests
    {
        [Fact]
        public void ObterTodosAgendamentos_FromOperacionalCopy_NullDuracaoEstimada_MapsToZero()
        {
            var source = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "primoauto_operacional.db");
            Assert.True(File.Exists(source), "primoauto_operacional.db must exist for C1.1.3 proof");

            var tempRoot = Path.Combine(Path.GetTempPath(), "primox-c113-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                var dest = Path.Combine(tempRoot, "primoauto_operacional.db");
                File.Copy(source, dest, overwrite: true);

                var settings = new DatabaseConnectionSettings
                {
                    Provider = "SQLite",
                    SQLitePath = "primoauto_operacional.db"
                };

                var db = new DatabaseService(tempRoot, settings, new LoggerService());
                var svc = new AgendamentoDatabaseService(db);
                var list = svc.ObterTodosAgendamentos();

                Assert.True(list.Count >= 1, "expected agendamentos in operacional copy");
                Assert.Contains(list, a => a.DuracaoEstimada == TimeSpan.Zero);
                Assert.All(list, a =>
                {
                    Assert.NotEqual(Guid.Empty, a.Id);
                    Assert.NotEqual(Guid.Empty, a.ClienteId);
                    Assert.NotEqual(Guid.Empty, a.VeiculoId);
                    Assert.False(string.IsNullOrWhiteSpace(a.Status));
                    Assert.False(string.IsNullOrWhiteSpace(a.Prioridade));
                    Assert.True(a.DuracaoEstimada >= TimeSpan.Zero);
                    Assert.True(a.DuracaoReal >= TimeSpan.Zero);
                    Assert.True(a.ValorEstimado >= 0m);
                });
            }
            finally
            {
                try { Directory.Delete(tempRoot, recursive: true); } catch { }
            }
        }

        [Fact]
        public void ObterTodosAgendamentos_InMemory_NullOrdinal6_AndFilledDuration()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "primox-c113-mem-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                var settings = new DatabaseConnectionSettings
                {
                    Provider = "SQLite",
                    SQLitePath = "primoauto_c113.db"
                };
                var db = new DatabaseService(tempRoot, settings, new LoggerService());
                var svc = new AgendamentoDatabaseService(db);

                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
DELETE FROM Agendamentos;
INSERT INTO Agendamentos (
  Id, Numero, DataCriacao, DataAgendamento, HoraInicio, HoraTermino, DuracaoEstimada, DuracaoReal,
  Status, Prioridade, TipoServico, CategoriaServico, DescricaoServico, Observacoes,
  ClienteId, ClienteNome, ClienteTelefone, ClienteEmail, ClienteDocumento,
  ClienteVip, ClienteTotalGasto, ClienteAtendimentos, ClienteUltimaVisita,
  VeiculoId, VeiculoPlaca, VeiculoModelo, VeiculoMarca, VeiculoAno, VeiculoCor, VeiculoCombustivel,
  VeiculoQuilometragem, VeiculoObservacoes,
  TecnicoId, TecnicoNome, TecnicoEspecialidade, TecnicoAtivo
) VALUES
(
  'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1', 'AG-NULL', '2026-09-26 10:00:00', '2026-09-27',
  '09:00', '10:00', NULL, NULL,
  'Agendado', NULL, 'Eletrica', NULL, 'Teste null duration', NULL,
  'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'Cliente Teste', NULL, NULL, NULL,
  NULL, NULL, NULL, NULL,
  'cccccccc-cccc-cccc-cccc-ccccccccccc1', 'ABC1D23', 'Gol', NULL, NULL, NULL, NULL,
  NULL, NULL,
  NULL, NULL, NULL, NULL
),
(
  'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2', 'AG-FILL', '2026-09-26 11:00:00', '2026-09-28',
  '14:00', '16:00', '02:00:00', '01:45:00',
  'Agendado', 'Alta', 'Eletrica', 'Alternador', 'Com duracao', 'obs',
  'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2', 'Cliente 2', '11999999999', 'a@b.c', '123',
  1, 150050, 3, '2026-09-01',
  'cccccccc-cccc-cccc-cccc-ccccccccccc2', 'XYZ9Z99', 'Uno', 'Fiat', '2012', 'Prata', 'Flex',
  120000, 'ok',
  'dddddddd-dddd-dddd-dddd-ddddddddddd1', 'Tecnico', 'Eletrica', 1
);";
                    cmd.ExecuteNonQuery();
                }

                var list = svc.ObterTodosAgendamentos()
                    .Where(a => a.Numero == "AG-NULL" || a.Numero == "AG-FILL")
                    .OrderBy(a => a.Numero)
                    .ToList();

                Assert.Equal(2, list.Count);

                var nullRow = list.Single(a => a.Numero == "AG-NULL");
                Assert.Equal(TimeSpan.Zero, nullRow.DuracaoEstimada);
                Assert.Equal(TimeSpan.Zero, nullRow.DuracaoReal);
                Assert.Equal("Normal", nullRow.Prioridade);
                Assert.Equal(Guid.Empty, nullRow.TecnicoId);
                Assert.Equal(0m, nullRow.ValorEstimado);
                Assert.Equal(0, nullRow.VeiculoQuilometragem);
                Assert.False(nullRow.ClienteVip);

                var filled = list.Single(a => a.Numero == "AG-FILL");
                Assert.Equal(TimeSpan.FromHours(2), filled.DuracaoEstimada);
                Assert.Equal(TimeSpan.FromMinutes(105), filled.DuracaoReal);
                Assert.Equal("Alta", filled.Prioridade);
                Assert.Equal(Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"), filled.TecnicoId);
                Assert.Equal(1500.50m, filled.ClienteTotalGasto);
                Assert.Equal(3, filled.ClienteAtendimentos);
                Assert.True(filled.ClienteVip);
                Assert.Equal(120000, filled.VeiculoQuilometragem);
            }
            finally
            {
                try { Directory.Delete(tempRoot, recursive: true); } catch { }
            }
        }
    }
}
