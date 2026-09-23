using System;
using System.Data;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Money
{
    public class MoneyRepositoryIoTests
    {
        [Theory]
        [InlineData(0.01, 1)]
        [InlineData(1.23, 123)]
        [InlineData(99.99, 9999)]
        [InlineData(100.01, 10001)]
        [InlineData(1234.56, 123456)]
        [InlineData(10000.00, 1000000)]
        public void MoneyIO_Write_And_Read_RoundTrip_InMemory_SQLite(decimal amount, long expectedCents)
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE TesteValores (Id TEXT PRIMARY KEY, Valor INTEGER NOT NULL, ValorOpcional INTEGER);";
                cmd.ExecuteNonQuery();
            }

            // Write using MoneyIO
            using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.CommandText = "INSERT INTO TesteValores (Id, Valor, ValorOpcional) VALUES (@id, @valor, @valorOpcional)";
                var pId = insertCmd.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = "rec1";
                insertCmd.Parameters.Add(pId);

                MoneyIO.GravarMoeda(insertCmd, "@valor", amount, MoneyPersistenceMode.CentsV1);
                MoneyIO.GravarMoedaNullable(insertCmd, "@valorOpcional", amount, MoneyPersistenceMode.CentsV1);
                insertCmd.ExecuteNonQuery();
            }

            // Verify raw DB storage is in cents
            using (var verifyCmd = connection.CreateCommand())
            {
                verifyCmd.CommandText = "SELECT Valor, ValorOpcional FROM TesteValores WHERE Id = 'rec1'";
                using var reader = verifyCmd.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(expectedCents, reader.GetInt64(0));
                Assert.Equal(expectedCents, reader.GetInt64(1));
            }

            // Read using MoneyIO
            using (var readCmd = connection.CreateCommand())
            {
                readCmd.CommandText = "SELECT Valor, ValorOpcional FROM TesteValores WHERE Id = 'rec1'";
                using var reader = readCmd.ExecuteReader();
                Assert.True(reader.Read());
                decimal lidoObrigatorio = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
                decimal? lidoOpcional = MoneyIO.LerMoedaNullable(reader, 1, MoneyPersistenceMode.CentsV1);

                Assert.Equal(amount, lidoObrigatorio);
                Assert.Equal(amount, lidoOpcional.Value);
            }
        }

        [Fact]
        public void MoneyIO_NullRules_Controlled_Behavior()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE TesteNull (Id TEXT PRIMARY KEY, Obrigatorio INTEGER, Opcional INTEGER);";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO TesteNull (Id, Obrigatorio, Opcional) VALUES ('n1', NULL, NULL);";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Obrigatorio, Opcional FROM TesteNull WHERE Id = 'n1'";
                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());

                // Nullable reader must return null without turning into 0
                decimal? opcional = MoneyIO.LerMoedaNullable(reader, 1, MoneyPersistenceMode.CentsV1);
                Assert.Null(opcional);

                // Non-null reader must throw InvalidOperationException when encounter DBNull
                var ex = Assert.Throws<InvalidOperationException>(() =>
                {
                    MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
                });
                Assert.Contains("DBNull em campo NOT NULL", ex.Message);
            }
        }

        [Fact]
        public void MoneyIO_NegativeValues_Preserved()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE TesteNegativo (Id TEXT PRIMARY KEY, Diferenca INTEGER NOT NULL);";
                cmd.ExecuteNonQuery();
            }

            decimal sangria = -50.00m;
            using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.CommandText = "INSERT INTO TesteNegativo (Id, Diferenca) VALUES ('s1', @dif)";
                var pId = insertCmd.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = "s1";
                insertCmd.Parameters.Add(pId);

                MoneyIO.GravarMoeda(insertCmd, "@dif", sangria, MoneyPersistenceMode.CentsV1);
                insertCmd.ExecuteNonQuery();
            }

            using (var readCmd = connection.CreateCommand())
            {
                readCmd.CommandText = "SELECT Diferenca FROM TesteNegativo WHERE Id = 's1'";
                using var reader = readCmd.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(-5000L, reader.GetInt64(0)); // -5000 cents in DB

                decimal lido = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
                Assert.Equal(-50.00m, lido);
            }
        }

        [Theory]
        [InlineData(0.005, 1)]
        [InlineData(1.005, 101)]
        [InlineData(2.675, 268)]
        [InlineData(-0.005, -1)]
        [InlineData(-1.005, -101)]
        public void MoneyIO_Rounding_AwayFromZero_EdgeCases(decimal input, long expectedCents)
        {
            var mc = MoneyCents.FromDecimal(input);
            Assert.Equal(expectedCents, mc.Cents);
            Assert.Equal(decimal.Round(input, 2, MidpointRounding.AwayFromZero), mc.ToDecimal());
        }

        [Fact]
        public void MoneyCents_Rateio_100_01_Dividido_Por_3_Zero_Loss()
        {
            // R$ 100,01 dividido em 3 parcelas
            var total = MoneyCents.FromDecimal(100.01m);
            var parcelas = total.Split(3);

            Assert.Equal(3, parcelas.Length);
            Assert.Equal(33.34m, parcelas[0].ToDecimal());
            Assert.Equal(33.34m, parcelas[1].ToDecimal());
            Assert.Equal(33.33m, parcelas[2].ToDecimal());

            // Soma exata
            var soma = parcelas[0] + parcelas[1] + parcelas[2];
            Assert.Equal(100.01m, soma.ToDecimal());
            Assert.Equal(10001L, soma.Cents);
        }

        [Fact]
        public void MoneyIO_Query_Operations_WHERE_ORDER_MIN_MAX()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE Produtos (Id TEXT PRIMARY KEY, PrecoVenda INTEGER NOT NULL);
                    INSERT INTO Produtos VALUES ('p1', 123);      -- R$ 1,23
                    INSERT INTO Produtos VALUES ('p2', 10001);    -- R$ 100,01
                    INSERT INTO Produtos VALUES ('p3', 123456);   -- R$ 1.234,56
                    INSERT INTO Produtos VALUES ('p4', 9999);     -- R$ 99,99
                ";
                cmd.ExecuteNonQuery();
            }

            // WHERE filter using MoneyIO.PrepararFiltro
            using (var queryCmd = connection.CreateCommand())
            {
                queryCmd.CommandText = "SELECT COUNT(*) FROM Produtos WHERE PrecoVenda > @minPreco";
                var p = queryCmd.CreateParameter();
                p.ParameterName = "@minPreco";
                p.Value = MoneyIO.PrepararFiltro(100.00m, MoneyPersistenceMode.CentsV1); // 10000 cents
                queryCmd.Parameters.Add(p);

                var count = Convert.ToInt32(queryCmd.ExecuteScalar());
                Assert.Equal(2, count); // p2 (100.01) and p3 (1234.56)
            }

            // MIN / MAX
            using (var aggCmd = connection.CreateCommand())
            {
                aggCmd.CommandText = "SELECT MIN(PrecoVenda), MAX(PrecoVenda) FROM Produtos";
                using var reader = aggCmd.ExecuteReader();
                Assert.True(reader.Read());
                decimal min = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
                decimal max = MoneyIO.LerMoeda(reader, 1, MoneyPersistenceMode.CentsV1);
                Assert.Equal(1.23m, min);
                Assert.Equal(1234.56m, max);
            }

            // ORDER BY
            using (var orderCmd = connection.CreateCommand())
            {
                orderCmd.CommandText = "SELECT PrecoVenda FROM Produtos ORDER BY PrecoVenda DESC LIMIT 1";
                var topCents = Convert.ToInt64(orderCmd.ExecuteScalar());
                Assert.Equal(123456L, topCents);
                Assert.Equal(1234.56m, topCents / 100m);
            }
        }

        [Fact]
        public void Test_All_15_Aggregates_Explicit_Coverage()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            // Set up all tables needed for the 15 queries
            using (var setupCmd = connection.CreateCommand())
            {
                setupCmd.CommandText = @"
                    CREATE TABLE MovimentacoesFinanceiras (Id TEXT PRIMARY KEY, Valor INTEGER NOT NULL, Data TEXT NOT NULL, Tipo TEXT NOT NULL);
                    CREATE TABLE ContasReceber (Id TEXT PRIMARY KEY, Valor INTEGER NOT NULL, Status TEXT NOT NULL, DataVencimento TEXT NOT NULL);
                    CREATE TABLE ContasPagar (Id TEXT PRIMARY KEY, Valor INTEGER NOT NULL, Status TEXT NOT NULL);
                    CREATE TABLE Vendas (Id TEXT PRIMARY KEY, Total INTEGER NOT NULL, Usuario TEXT NOT NULL, Status TEXT NOT NULL);
                    CREATE TABLE CaixaSessoes (Id TEXT PRIMARY KEY, TotalVendas INTEGER NOT NULL, OperadorId INTEGER NOT NULL);

                    -- Inserts com valores conhecidos (em centavos):
                    -- R$ 1.234,56 (123456) e R$ 99,99 (9999) = R$ 1.334,55 (133455 cents)
                    INSERT INTO MovimentacoesFinanceiras VALUES ('mf1', 123456, '2026-09-23', 'Receita');
                    INSERT INTO MovimentacoesFinanceiras VALUES ('mf2', 9999, '2026-09-23', 'Receita');
                    INSERT INTO MovimentacoesFinanceiras VALUES ('mf3', 50000, '2026-09-23', 'Despesa');
                    INSERT INTO MovimentacoesFinanceiras VALUES ('mf4', 10001, '2026-09-23', 'Entrada');
                    INSERT INTO MovimentacoesFinanceiras VALUES ('mf5', 2000, '2026-09-23', 'Saida');

                    INSERT INTO ContasReceber VALUES ('cr1', 100567, 'Pendente', '2026-09-20');
                    INSERT INTO ContasReceber VALUES ('cr2', 123, 'Pendente', '2026-09-23');

                    INSERT INTO ContasPagar VALUES ('cp1', 50000, 'Pendente');

                    INSERT INTO Vendas VALUES ('v1', 100567, 'operador1', 'Concluida');

                    INSERT INTO CaixaSessoes VALUES ('cx1', 200000, 10);
                ";
                setupCmd.ExecuteNonQuery();
            }

            // 1. FinanceiroDatabaseService (Receita período)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN '2026-09-01' AND '2026-09-30' AND Tipo = 'Receita'",
                133455L, 1334.55m);

            // 2. FinanceiroDatabaseService (Despesa período)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN '2026-09-01' AND '2026-09-30' AND Tipo = 'Despesa'",
                50000L, 500.00m);

            // 3. FinanceiroDatabaseService (Contas a Receber Pendentes)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber WHERE Status = 'Pendente'",
                100690L, 1006.90m);

            // 4. FinanceiroDatabaseService (Contas a Pagar Pendentes)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM ContasPagar WHERE Status = 'Pendente'",
                50000L, 500.00m);

            // 5. FinanceiroDatabaseService (Inadimplência vencida)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber WHERE DataVencimento < '2026-09-22' AND Status = 'Pendente'",
                100567L, 1005.67m);

            // 6. FinanceiroDatabaseService (Total Movimentações no período)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN '2026-09-01' AND '2026-09-30'",
                195456L, 1954.56m);

            // 7. FinanceiroDatabaseService (Fluxo diário Entradas)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Tipo = 'Entrada' AND Data = '2026-09-23'",
                10001L, 100.01m);

            // 8. FinanceiroDatabaseService (Fluxo diário Saídas)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Tipo = 'Saida' AND Data = '2026-09-23'",
                2000L, 20.00m);

            // 9. FuncionarioOperationalService (Vendas por usuário)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Total), 0) FROM Vendas WHERE Usuario = 'operador1' AND Status = 'Concluida'",
                100567L, 1005.67m);

            // 10. FuncionarioOperationalService (Desempenho operador caixa)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(TotalVendas), 0) FROM CaixaSessoes WHERE OperadorId = 10",
                200000L, 2000.00m);

            // 11. RelatorioDatabaseService (Receita Total relatório)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN '2026-09-01' AND '2026-09-30' AND Tipo IN ('Entrada', 'Receita')",
                143456L, 1434.56m);

            // 12. RelatorioDatabaseService (Despesa Total relatório)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data BETWEEN '2026-09-01' AND '2026-09-30' AND Tipo IN ('Saida', 'Despesa')",
                52000L, 520.00m);

            // 13. DashboardViewModel (Contas a Receber hoje)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber WHERE DataVencimento = '2026-09-23' AND Status = 'Pendente'",
                123L, 1.23m);

            // 14. DashboardViewModel (Faturamento mensal)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data >= '2026-09-01' AND Tipo = 'Receita'",
                133455L, 1334.55m);

            // 15. DashboardViewModel (Despesas mensais)
            TestAggregate(connection,
                "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras WHERE Data >= '2026-09-01' AND Tipo = 'Despesa'",
                50000L, 500.00m);
        }

        private static void TestAggregate(SqliteConnection connection, string sql, long expectedCents, decimal expectedDecimal)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            var scalar = cmd.ExecuteScalar();

            // 1. Raw result must be cents (long)
            long rawCents = Convert.ToInt64(scalar);
            Assert.Equal(expectedCents, rawCents);

            // 2. Converted using MoneyIO must match exact decimal expected
            decimal converted = MoneyIO.ConverterAgregacao(scalar, MoneyPersistenceMode.CentsV1);
            Assert.Equal(expectedDecimal, converted);
        }

        [Fact]
        public void MoneyIO_DetectMode_UserVersion_Detection()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            // Default user_version is 0 -> LegacyReal
            Assert.Equal(MoneyPersistenceMode.LegacyReal, MoneyIO.DetectMode(connection));

            // Set user_version = 1 -> CentsV1
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA user_version = 1;";
                cmd.ExecuteNonQuery();
            }
            Assert.Equal(MoneyPersistenceMode.CentsV1, MoneyIO.DetectMode(connection));

            // Set user_version = 2 -> CentsV1 (future schema versions)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA user_version = 2;";
                cmd.ExecuteNonQuery();
            }
            Assert.Equal(MoneyPersistenceMode.CentsV1, MoneyIO.DetectMode(connection));
        }

        [Fact]
        public void MoneyIO_StaticAnalysisGate_NoUnclassifiedOldMoneyAccess()
        {
            // Locates the audit file and verifies that all occurrences are classified (0 Class D)
            var baseDir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            string? auditPath = null;
            while (baseDir != null)
            {
                var candidate = System.IO.Path.Combine(baseDir.FullName, "Docs", "audit", "2026-09-20", "P2_3_OLD_MONEY_ACCESS_AUDIT.json");
                if (System.IO.File.Exists(candidate))
                {
                    auditPath = candidate;
                    break;
                }
                baseDir = baseDir.Parent;
            }

            Assert.NotNull(auditPath);
            string json = System.IO.File.ReadAllText(auditPath!);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.True(root.GetArrayLength() > 0);

            int countClassA = 0;
            int countClassB = 0;
            int countClassC = 0;
            int countClassD = 0;

            foreach (var item in root.EnumerateArray())
            {
                string cls = item.GetProperty("classification").GetString() ?? "";
                if (cls == "A") countClassA++;
                else if (cls == "B") countClassB++;
                else if (cls == "C") countClassC++;
                else countClassD++;
            }

            Assert.Equal(0, countClassD);
            Assert.Equal(38, countClassA);
            Assert.Equal(2, countClassB);
            Assert.Equal(42, countClassC);
        }
    }
}
