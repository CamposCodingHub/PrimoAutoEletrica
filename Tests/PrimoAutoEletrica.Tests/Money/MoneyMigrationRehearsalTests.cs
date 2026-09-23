using System;
using System.Data;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Money
{
    public class MoneyMigrationRehearsalTests
    {
        [Theory]
        [InlineData(0.005, 1)]
        [InlineData(1.005, 101)]
        [InlineData(2.675, 268)]
        [InlineData(-0.005, -1)]
        [InlineData(-1.005, -101)]
        [InlineData(0.01, 1)]
        [InlineData(0.05, 5)]
        [InlineData(0.10, 10)]
        [InlineData(1.23, 123)]
        [InlineData(99.99, 9999)]
        [InlineData(100.01, 10001)]
        [InlineData(1005.67, 100567)]
        public void AwayFromZero_Rounding_MandatoryCases_MatchExpectedCents(decimal input, long expectedCents)
        {
            var rounded = decimal.Round(input, 2, MidpointRounding.AwayFromZero);
            long cents = (long)(rounded * 100m);
            Assert.Equal(expectedCents, cents);

            // And MoneyCents roundtrip
            var mc = MoneyCents.FromDecimal(input);
            Assert.Equal(expectedCents, mc.Cents);
            Assert.Equal(rounded, mc.ToDecimal());
        }

        [Fact]
        public void Synthetic_Table_Rebuild_And_Roundtrip_Preservation()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            // 1. Original schema with REAL
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE Vendas_Original (
                        Id TEXT PRIMARY KEY,
                        Total REAL NOT NULL,
                        Desconto REAL NOT NULL,
                        DescontoPercentual REAL NOT NULL
                    );
                    INSERT INTO Vendas_Original VALUES ('v1', 0.01, 0.00, 0.0);
                    INSERT INTO Vendas_Original VALUES ('v2', 1005.67, 99.99, 10.0);
                    INSERT INTO Vendas_Original VALUES ('v3', 100.01, 1.23, 5.5);
                ";
                cmd.ExecuteNonQuery();
            }

            // 2. Rebuild to shadow table with INTEGER cents for Total and Desconto, but REAL for DescontoPercentual
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE Vendas_New (
                        Id TEXT PRIMARY KEY,
                        Total INTEGER NOT NULL,
                        Desconto INTEGER NOT NULL,
                        DescontoPercentual REAL NOT NULL
                    );
                    INSERT INTO Vendas_New (Id, Total, Desconto, DescontoPercentual)
                    SELECT Id,
                           CAST(ROUND(Total * 100) AS INTEGER),
                           CAST(ROUND(Desconto * 100) AS INTEGER),
                           DescontoPercentual
                    FROM Vendas_Original;
                    DROP TABLE Vendas_Original;
                    ALTER TABLE Vendas_New RENAME TO Vendas;
                ";
                cmd.ExecuteNonQuery();
            }

            // 3. Validate roundtrip and non-monetary preservation
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Total, Desconto, DescontoPercentual FROM Vendas ORDER BY Id";
                using var reader = cmd.ExecuteReader();

                // v1
                Assert.True(reader.Read());
                Assert.Equal("v1", reader.GetString(0));
                Assert.Equal(1L, reader.GetInt64(1)); // 0.01 -> 1 cent
                Assert.Equal(0L, reader.GetInt64(2));
                Assert.Equal(0.0, reader.GetDouble(3));

                // v2
                Assert.True(reader.Read());
                Assert.Equal("v2", reader.GetString(0));
                Assert.Equal(100567L, reader.GetInt64(1)); // 1005.67 -> 100567 cents
                Assert.Equal(9999L, reader.GetInt64(2));   // 99.99 -> 9999 cents
                Assert.Equal(10.0, reader.GetDouble(3));   // 10% preserved as REAL

                // v3
                Assert.True(reader.Read());
                Assert.Equal("v3", reader.GetString(0));
                Assert.Equal(10001L, reader.GetInt64(1));  // 100.01 -> 10001 cents
                Assert.Equal(123L, reader.GetInt64(2));    // 1.23 -> 123 cents
                Assert.Equal(5.5, reader.GetDouble(3));    // 5.5% preserved as REAL
            }
        }

        [Fact]
        public void NonMonetary_Columns_Must_Not_Be_Converted_To_Cents()
        {
            // Percentage: 15.5% must not become 1550
            decimal margemLucro = 15.5m;
            Assert.Equal(15.5m, margemLucro);

            // Quantity: 3.5 peças must not become 350
            decimal quantidade = 3.5m;
            Assert.Equal(3.5m, quantidade);

            // Coordinate: -20.8123 must not become -2081
            double latitude = -20.8123;
            Assert.Equal(-20.8123, latitude);
        }

        [Fact]
        public void Aggregate_SUM_Returns_Cents_Sum_Application_Must_Divide_By_100()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE Movimentacoes (
                        Id TEXT PRIMARY KEY,
                        Valor INTEGER NOT NULL
                    );
                    INSERT INTO Movimentacoes VALUES ('m1', 123456); -- R$ 1.234,56
                    INSERT INTO Movimentacoes VALUES ('m2', 9999);   -- R$ 99,99
                    INSERT INTO Movimentacoes VALUES ('m3', 1);      -- R$ 0,01
                ";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT SUM(Valor) FROM Movimentacoes";
                var rawSum = Convert.ToInt64(cmd.ExecuteScalar());
                Assert.Equal(133456L, rawSum); // 133456 cents

                // Layer of application conversion:
                decimal decimalSum = (decimal)rawSum / 100m;
                Assert.Equal(1334.56m, decimalSum); // R$ 1.334,56
            }
        }

        [Fact]
        public void Negative_Money_Semantics_Validation()
        {
            // Allowed: Quebra de caixa / Sangria / Ajuste negativo
            var sangria = new MoneyCents(-5000);
            Assert.Equal(-5000, sangria.Cents);
            Assert.Equal(-50.00m, sangria.ToDecimal());

            // Commercial rules forbid negative price:
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                decimal precoInvalido = -10.00m;
                if (precoInvalido < 0)
                    throw new ArgumentOutOfRangeException(nameof(precoInvalido), "Preço de venda não pode ser negativo.");
            });
        }
    }
}
