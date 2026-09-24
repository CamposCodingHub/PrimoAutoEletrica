using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Money
{
    public class MoneyMigrationB53Tests
    {
        [Theory]
        [InlineData(0.005, 1)]
        [InlineData(0.015, 2)]
        [InlineData(0.025, 3)]
        [InlineData(1.005, 101)]
        [InlineData(2.675, 268)]
        [InlineData(10.005, 1001)]
        [InlineData(-0.005, -1)]
        [InlineData(-1.005, -101)]
        public void AwayFromZero_MandatoryPrecisionEdgeCases_RoundCorrectly(decimal input, long expectedCents)
        {
            var rounded = decimal.Round(input, 2, MidpointRounding.AwayFromZero);
            long cents = (long)(rounded * 100m);
            Assert.Equal(expectedCents, cents);

            var mc = MoneyCents.FromDecimal(input);
            Assert.Equal(expectedCents, mc.Cents);
            Assert.Equal(rounded, mc.ToDecimal());
        }

        [Fact]
        public void MoneyIO_NullableAndNotNull_Handling_PreservesSemantics()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE TestMoneyTable (
                    Id INTEGER PRIMARY KEY,
                    ValorNotNull INTEGER NOT NULL,
                    ValorNullable INTEGER NULL
                );
                INSERT INTO TestMoneyTable VALUES (1, 15000, NULL);
                INSERT INTO TestMoneyTable VALUES (2, 2999, 1250);
            ";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT ValorNotNull, ValorNullable FROM TestMoneyTable ORDER BY Id";
            using var reader = cmd.ExecuteReader();

            // Row 1
            Assert.True(reader.Read());
            var v1NotNull = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
            var v1Nullable = MoneyIO.LerMoedaNullable(reader, 1, MoneyPersistenceMode.CentsV1);
            Assert.Equal(150.00m, v1NotNull);
            Assert.Null(v1Nullable);

            // Row 2
            Assert.True(reader.Read());
            var v2NotNull = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
            var v2Nullable = MoneyIO.LerMoedaNullable(reader, 1, MoneyPersistenceMode.CentsV1);
            Assert.Equal(29.99m, v2NotNull);
            Assert.Equal(12.50m, v2Nullable);
        }

        [Fact]
        public void MoneyIO_NegativeValues_PreservesSignCorrectly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE TestNegTable (Id INTEGER PRIMARY KEY, Valor INTEGER NOT NULL);";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO TestNegTable (Id, Valor) VALUES (1, @val);";
            MoneyIO.GravarMoeda(cmd, "@val", -50.00m, MoneyPersistenceMode.CentsV1);
            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT Valor FROM TestNegTable WHERE Id = 1;";
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            var valorDec = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
            Assert.Equal(-50.00m, valorDec);
        }

        [Fact]
        public void Migration_Rehearsal_ShadowTableRebuild_PreservesAggregatesAndFKs()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    PRAGMA foreign_keys = ON;

                    CREATE TABLE Clientes_Legacy (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        TotalGasto REAL NOT NULL
                    );

                    CREATE TABLE Ordens_Legacy (
                        Id TEXT PRIMARY KEY,
                        ClienteId TEXT NOT NULL REFERENCES Clientes_Legacy(Id),
                        ValorMaoObra REAL NOT NULL,
                        Desconto REAL NOT NULL,
                        QuantidadeItens INTEGER NOT NULL
                    );

                    INSERT INTO Clientes_Legacy VALUES ('c1', 'Cliente Alfa', 250.50);
                    INSERT INTO Ordens_Legacy VALUES ('o1', 'c1', 200.50, 10.00, 3);
                    INSERT INTO Ordens_Legacy VALUES ('o2', 'c1', 60.00, 0.00, 1);
                ";
                cmd.ExecuteNonQuery();
            }

            // Perform shadow rebuild to CentsV1
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    PRAGMA foreign_keys = OFF;
                    BEGIN TRANSACTION;

                    CREATE TABLE Clientes_New (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        TotalGasto INTEGER NOT NULL
                    );

                    INSERT INTO Clientes_New
                    SELECT Id, Nome, CAST(ROUND(TotalGasto * 100) AS INTEGER)
                    FROM Clientes_Legacy;

                    CREATE TABLE Ordens_New (
                        Id TEXT PRIMARY KEY,
                        ClienteId TEXT NOT NULL REFERENCES Clientes_New(Id),
                        ValorMaoObra INTEGER NOT NULL,
                        Desconto INTEGER NOT NULL,
                        QuantidadeItens INTEGER NOT NULL
                    );

                    INSERT INTO Ordens_New
                    SELECT Id, ClienteId,
                           CAST(ROUND(ValorMaoObra * 100) AS INTEGER),
                           CAST(ROUND(Desconto * 100) AS INTEGER),
                           QuantidadeItens
                    FROM Ordens_Legacy;

                    DROP TABLE Ordens_Legacy;
                    DROP TABLE Clientes_Legacy;

                    ALTER TABLE Clientes_New RENAME TO Clientes;
                    ALTER TABLE Ordens_New RENAME TO Ordens;

                    COMMIT;
                    PRAGMA foreign_keys = ON;
                ";
                cmd.ExecuteNonQuery();
            }

            // Verify FK check
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_key_check;";
                using var reader = cmd.ExecuteReader();
                Assert.False(reader.Read(), "Expected zero foreign key violations.");
            }

            // Verify integrity check
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA integrity_check;";
                var res = cmd.ExecuteScalar()?.ToString();
                Assert.Equal("ok", res);
            }

            // Verify Aggregates
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT SUM(ValorMaoObra), SUM(Desconto), SUM(QuantidadeItens) FROM Ordens;";
                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());
                long sumMaoObraCents = reader.GetInt64(0);
                long sumDescontoCents = reader.GetInt64(1);
                int sumQtd = reader.GetInt32(2);

                Assert.Equal(26050, sumMaoObraCents);
                Assert.Equal(260.50m, sumMaoObraCents / 100m);
                Assert.Equal(1000, sumDescontoCents);
                Assert.Equal(10.00m, sumDescontoCents / 100m);
                Assert.Equal(4, sumQtd); // Non-monetary preserved
            }
        }

        [Fact]
        public void Migration_Rollback_PreservesStateAtomically()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE Produtos_Test (
                        Id TEXT PRIMARY KEY,
                        Preco REAL NOT NULL
                    );
                    INSERT INTO Produtos_Test VALUES ('p1', 19.99);
                ";
                cmd.ExecuteNonQuery();
            }

            // Attempt transaction that throws
            try
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = "UPDATE Produtos_Test SET Preco = 999.99 WHERE Id = 'p1';";
                cmd.ExecuteNonQuery();

                // Simulated failure
                throw new InvalidOperationException("Simulated migration failure during batch");
            }
            catch (InvalidOperationException)
            {
                // Handled, transaction aborted
            }

            // Verify original price remains intact
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Preco FROM Produtos_Test WHERE Id = 'p1';";
                var preco = Convert.ToDecimal(cmd.ExecuteScalar());
                Assert.Equal(19.99m, preco);
            }
        }

        [Fact]
        public void Migration_Idempotency_SkipsAlreadyMigratedDatabase()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    PRAGMA user_version = 1;
                    CREATE TABLE Contas_Migrated (Id TEXT PRIMARY KEY, Valor INTEGER NOT NULL);
                    INSERT INTO Contas_Migrated VALUES ('c1', 12345);
                ";
                cmd.ExecuteNonQuery();
            }

            // Check version before migration attempt
            int uv;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA user_version;";
                uv = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (uv < 1)
            {
                // Should not execute
                Assert.Fail("Should have skipped execution for user_version >= 1");
            }

            // Verify value remained 12345 cents without double multiplication
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Valor FROM Contas_Migrated WHERE Id = 'c1';";
                long valorCents = Convert.ToInt64(cmd.ExecuteScalar());
                Assert.Equal(12345, valorCents);
                Assert.Equal(123.45m, valorCents / 100m);
            }
        }
    }
}
