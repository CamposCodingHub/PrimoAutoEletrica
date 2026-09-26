using System;
using System.Data.Common;
using System.Reflection;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>
    /// C1.1.2 BUG-004: Acrescimo/Desconto NULLABLE must not throw when materializing.
    /// Uses reflection to call private MaterializarOrcamento if needed; prefers public list path with temp DB.
    /// </summary>
    public class OrcamentoNullAcrescimoMaterializationTests
    {
        [Fact]
        public void ReadOptionalMoney_NullAcrescimo_DoesNotThrow_ViaMoneyIO()
        {
            using var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE t(Acrescimo INTEGER, Desconto INTEGER, Total INTEGER); INSERT INTO t VALUES (NULL, NULL, 12000);";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Acrescimo, Desconto, Total FROM t";
                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(0m, MoneyIO.LerMoedaOpcionalOuZero(reader, 0));
                Assert.Equal(0m, MoneyIO.LerMoedaOpcionalOuZero(reader, 1));
                Assert.Equal(120.00m, MoneyIO.LerMoeda(reader, 2));
            }
        }
    }
}
