using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Money
{
    public sealed class B4MoneyRuntimePrecisionTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public B4MoneyRuntimePrecisionTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE TestMoneyLedger (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Descricao TEXT NOT NULL,
                    ValorCentavos INTEGER NOT NULL,
                    ValorLegacy REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _conn.Dispose();
        }

        [Theory]
        [InlineData(0.01, 1)]
        [InlineData(0.05, 5)]
        [InlineData(0.10, 10)]
        [InlineData(1.23, 123)]
        [InlineData(99.99, 9999)]
        [InlineData(100.01, 10001)]
        [InlineData(1005.67, 100567)]
        [InlineData(-0.01, -1)]
        [InlineData(-50.00, -5000)]
        [InlineData(-100.01, -10001)]
        public void MoneyCents_ConversaoExata_SemPerdaDePrecisao(decimal valorDecimal, long centavosEsperados)
        {
            var cents = MoneyCents.FromDecimal(valorDecimal);
            Assert.Equal(centavosEsperados, cents.Cents);

            var roundtrip = cents.ToDecimal();
            Assert.Equal(valorDecimal, roundtrip);
        }

        [Theory]
        [InlineData(0.005, 0.01)]
        [InlineData(1.005, 1.01)]
        [InlineData(2.675, 2.68)]
        [InlineData(-0.005, -0.01)]
        [InlineData(-1.005, -1.01)]
        [InlineData(-2.675, -2.68)]
        public void MoneyRounding_AwayFromZero_GaranteArredondamentoComercial(decimal input, decimal expected)
        {
            var rounded = Math.Round(input, 2, MidpointRounding.AwayFromZero);
            Assert.Equal(expected, rounded);

            var cents = MoneyCents.FromDecimal(input);
            var expectedCents = (long)Math.Round(expected * 100m, 0, MidpointRounding.AwayFromZero);
            Assert.Equal(expectedCents, cents.Cents);
        }

        [Fact]
        public void Sqlite_OperacoesMonetariasEmCentavos_InsertUpdateSelectAgregacoes()
        {
            var valores = new (string Desc, decimal Valor)[]
            {
                ("Item A", 0.01m),
                ("Item B", 0.05m),
                ("Item C", 0.10m),
                ("Item D", 1.23m),
                ("Item E", 99.99m),
                ("Item F", 100.01m),
                ("Item G", 1005.67m),
                ("Estorno A", -0.01m),
                ("Desconto B", -50.00m),
                ("Estorno C", -100.01m)
            };

            // 1. INSERT
            foreach (var item in valores)
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "INSERT INTO TestMoneyLedger (Descricao, ValorCentavos, ValorLegacy) VALUES (@d, @c, @l);";
                cmd.Parameters.AddWithValue("@d", item.Desc);
                cmd.Parameters.AddWithValue("@c", MoneyCents.FromDecimal(item.Valor).Cents);
                cmd.Parameters.AddWithValue("@l", (double)item.Valor);
                cmd.ExecuteNonQuery();
            }

            // 2. SELECT & WHERE (busca exata por centavos)
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Descricao, ValorCentavos FROM TestMoneyLedger WHERE ValorCentavos = @c;";
                cmd.Parameters.AddWithValue("@c", 100567L);
                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal("Item G", reader.GetString(0));
                Assert.Equal(100567L, reader.GetInt64(1));
            }

            // 3. UPDATE
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE TestMoneyLedger SET ValorCentavos = @novo WHERE Descricao = 'Item A';";
                cmd.Parameters.AddWithValue("@novo", MoneyCents.FromDecimal(0.02m).Cents);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ValorCentavos FROM TestMoneyLedger WHERE Descricao = 'Item A';";
                var updatedVal = (long)cmd.ExecuteScalar()!;
                Assert.Equal(2L, updatedVal);
            }

            // 4. AGGREGAÇÕES: SUM, MIN, MAX, AVG
            // Valores atuais no banco:
            // 0.02 + 0.05 + 0.10 + 1.23 + 99.99 + 100.01 + 1005.67 - 0.01 - 50.00 - 100.01 = 1057.05m
            // Em centavos: 105705
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT 
                        SUM(ValorCentavos) AS TotalCentavos,
                        MIN(ValorCentavos) AS MinCentavos,
                        MAX(ValorCentavos) AS MaxCentavos,
                        AVG(ValorCentavos) AS MediaCentavos
                    FROM TestMoneyLedger;";

                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());

                var sumCents = reader.GetInt64(0);
                var minCents = reader.GetInt64(1);
                var maxCents = reader.GetInt64(2);
                var avgCents = reader.GetDouble(3);

                Assert.Equal(105705L, sumCents);
                Assert.Equal(1057.05m, MoneyCents.FromCents(sumCents).ToDecimal());

                Assert.Equal(-10001L, minCents);
                Assert.Equal(-100.01m, MoneyCents.FromCents(minCents).ToDecimal());

                Assert.Equal(100567L, maxCents);
                Assert.Equal(1005.67m, MoneyCents.FromCents(maxCents).ToDecimal());

                // Média: 105705 / 10 = 10570.5 centavos = 105.705
                Assert.Equal(10570.5, avgCents, precision: 2);
            }

            // 5. ORDER BY (Ordenação estrita por inteiros sem jitter float)
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ValorCentavos FROM TestMoneyLedger ORDER BY ValorCentavos ASC;";
                using var reader = cmd.ExecuteReader();
                var ordenados = new List<long>();
                while (reader.Read())
                {
                    ordenados.Add(reader.GetInt64(0));
                }

                Assert.Equal(10, ordenados.Count);
                Assert.Equal(-10001L, ordenados.First());
                Assert.Equal(100567L, ordenados.Last());
                Assert.True(ordenados.SequenceEqual(ordenados.OrderBy(x => x)));
            }
        }
    }
}
