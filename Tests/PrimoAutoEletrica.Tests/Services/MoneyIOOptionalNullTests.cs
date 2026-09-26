using System;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class MoneyIOOptionalNullTests
    {
        [Fact]
        public void LerMoedaOpcionalOuZero_DbNull_ReturnsZero()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE t(Acrescimo INTEGER); INSERT INTO t VALUES (NULL); INSERT INTO t VALUES (199);";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Acrescimo FROM t ORDER BY rowid";
                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(0m, MoneyIO.LerMoedaOpcionalOuZero(reader, 0));
                Assert.True(reader.Read());
                Assert.Equal(1.99m, MoneyIO.LerMoedaOpcionalOuZero(reader, 0));
            }
        }

        [Fact]
        public void LerMoeda_DbNull_Throws()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE t(Total INTEGER); INSERT INTO t VALUES (NULL);";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Total FROM t";
                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Throws<InvalidOperationException>(() => MoneyIO.LerMoeda(reader, 0));
            }
        }
    }
}