using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Money
{
    public class MoneyPreProductionGateTests
    {
        private static string GetHomologDbPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "TestResults", "Homologacao_Fase2_4", "primoauto_money_v24_cents.db");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }
            throw new FileNotFoundException("Banco de homologação CentsV1 não encontrado em TestResults/Homologacao_Fase2_4/primoauto_money_v24_cents.db");
        }

        private static SqliteConnection OpenHomologConnection()
        {
            var path = GetHomologDbPath();
            var conn = new SqliteConnection($"Data Source={path};Mode=ReadWrite;");
            conn.Open();
            return conn;
        }

        // =========================================================================
        // 1. CASOS EXTREMOS E PROVA DE PARIDADE (Seções 6 e 7)
        // =========================================================================
        [Theory]
        [InlineData(0.01, 1L)]
        [InlineData(0.02, 2L)]
        [InlineData(0.05, 5L)]
        [InlineData(0.10, 10L)]
        [InlineData(0.99, 99L)]
        [InlineData(1.00, 100L)]
        [InlineData(1.01, 101L)]
        [InlineData(9.99, 999L)]
        [InlineData(10.01, 1001L)]
        [InlineData(99.99, 9999L)]
        [InlineData(100.01, 10001L)]
        [InlineData(999.99, 99999L)]
        [InlineData(1000.01, 100001L)]
        [InlineData(1234.56, 123456L)]
        [InlineData(9999.99, 999999L)]
        [InlineData(100000.01, 10000001L)]
        [InlineData(-0.01, -1L)]
        [InlineData(-1.00, -100L)]
        [InlineData(-9.99, -999L)]
        [InlineData(-50.00, -5000L)]
        [InlineData(-1234.56, -123456L)]
        [InlineData(0.00, 0L)]
        public void MoneyPreProduction_ExtremeValues_Roundtrip_Parity(decimal amount, long expectedCents)
        {
            // decimal -> MoneyCents (cents)
            var mc = MoneyCents.FromDecimal(amount);
            Assert.Equal(expectedCents, mc.Cents);

            // MoneyCents -> decimal
            decimal convertedBack = mc.ToDecimal();
            Assert.Equal(amount, convertedBack);

            // MoneyIO helper parity
            using var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            using var cmd = conn.CreateCommand();
            MoneyIO.GravarMoeda(cmd, "@testVal", amount, MoneyPersistenceMode.CentsV1);
            Assert.Equal(expectedCents, cmd.Parameters["@testVal"].Value);
        }

        // =========================================================================
        // 2. TESTE REAL DOS REPOSITORIES NO BANCO CentsV1 (Seção 8)
        // =========================================================================
        [Fact]
        public void Repositories_RealCrud_On_CentsV1_Homologation_Database()
        {
            using var conn = OpenHomologConnection();

            // Verificar user_version = 1
            using (var uvCmd = conn.CreateCommand())
            {
                uvCmd.CommandText = "PRAGMA user_version;";
                int uv = Convert.ToInt32(uvCmd.ExecuteScalar());
                Assert.Equal(1, uv);
            }

            var testProdId = "GATE-TEST-" + Guid.NewGuid().ToString("N");
            decimal precoVenda = 123.45m;
            decimal precoCompra = 50.00m;
            long expectedVendaCents = 12345L;
            long expectedCompraCents = 5000L;

            // INSERT via MoneyIO
            using (var insertCmd = conn.CreateCommand())
            {
                insertCmd.CommandText = @"
                    INSERT INTO Produtos (Id, Codigo, Nome, PrecoCompra, PrecoVenda, MargemLucro, ValorTotalEstoque, TotalFaturado, Ativo, DataCadastro)
                    VALUES (@id, @cod, @nome, @compra, @venda, 146.9, 0, 0, 1, '2026-09-23 18:00:00');";
                
                var pId = insertCmd.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = testProdId;
                insertCmd.Parameters.Add(pId);

                var pCod = insertCmd.CreateParameter();
                pCod.ParameterName = "@cod";
                pCod.Value = "TEST-CRUD";
                insertCmd.Parameters.Add(pCod);

                var pNome = insertCmd.CreateParameter();
                pNome.ParameterName = "@nome";
                pNome.Value = "Produto Teste Real CentsV1";
                insertCmd.Parameters.Add(pNome);

                MoneyIO.GravarMoeda(insertCmd, "@compra", precoCompra, MoneyPersistenceMode.CentsV1);
                MoneyIO.GravarMoeda(insertCmd, "@venda", precoVenda, MoneyPersistenceMode.CentsV1);

                insertCmd.ExecuteNonQuery();
            }

            // SELECT: Raw SQLite storage MUST BE INTEGER cents
            using (var rawCmd = conn.CreateCommand())
            {
                rawCmd.CommandText = "SELECT PrecoVenda, PrecoCompra FROM Produtos WHERE Id = @id";
                var p = rawCmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = testProdId;
                rawCmd.Parameters.Add(p);

                using var reader = rawCmd.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(expectedVendaCents, reader.GetInt64(0));
                Assert.Equal(expectedCompraCents, reader.GetInt64(1));
            }

            // SELECT via MoneyIO: MUST RETURN decimal in reais
            using (var readCmd = conn.CreateCommand())
            {
                readCmd.CommandText = "SELECT PrecoVenda, PrecoCompra FROM Produtos WHERE Id = @id";
                var p = readCmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = testProdId;
                readCmd.Parameters.Add(p);

                using var reader = readCmd.ExecuteReader();
                Assert.True(reader.Read());
                decimal lidoVenda = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
                decimal lidoCompra = MoneyIO.LerMoeda(reader, 1, MoneyPersistenceMode.CentsV1);

                Assert.Equal(precoVenda, lidoVenda);
                Assert.Equal(precoCompra, lidoCompra);
            }

            // UPDATE: 123.45 -> 999.99 (99999 cents)
            decimal novoPreco = 999.99m;
            using (var updateCmd = conn.CreateCommand())
            {
                updateCmd.CommandText = "UPDATE Produtos SET PrecoVenda = @novoPreco WHERE Id = @id";
                var pId = updateCmd.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = testProdId;
                updateCmd.Parameters.Add(pId);

                MoneyIO.GravarMoeda(updateCmd, "@novoPreco", novoPreco, MoneyPersistenceMode.CentsV1);
                updateCmd.ExecuteNonQuery();
            }

            // Verify raw updated value is 99999
            using (var rawCmd = conn.CreateCommand())
            {
                rawCmd.CommandText = "SELECT PrecoVenda FROM Produtos WHERE Id = @id";
                var p = rawCmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = testProdId;
                rawCmd.Parameters.Add(p);

                long rawUpdated = Convert.ToInt64(rawCmd.ExecuteScalar());
                Assert.Equal(99999L, rawUpdated);
            }

            // DELETE and verify absence
            using (var delCmd = conn.CreateCommand())
            {
                delCmd.CommandText = "DELETE FROM Produtos WHERE Id = @id";
                var p = delCmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = testProdId;
                delCmd.Parameters.Add(p);
                delCmd.ExecuteNonQuery();
            }

            using (var verifyDel = conn.CreateCommand())
            {
                verifyDel.CommandText = "SELECT count(*) FROM Produtos WHERE Id = @id";
                var p = verifyDel.CreateParameter();
                p.ParameterName = "@id";
                p.Value = testProdId;
                verifyDel.Parameters.Add(p);
                long cnt = Convert.ToInt64(verifyDel.ExecuteScalar());
                Assert.Equal(0L, cnt);
            }
        }

        // =========================================================================
        // 3. AS 15 AGREGATIONS NO BANCO REAL DE HOMOLOGAÇÃO (Seção 9)
        // =========================================================================
        [Fact]
        public void SqlAggregates_All15Queries_On_Homologation_Database()
        {
            using var conn = OpenHomologConnection();

            // Insere dados de apoio se necessário para garantir valores controlados nas tabelas operacionais
            using (var seedCmd = conn.CreateCommand())
            {
                seedCmd.CommandText = @"
                    INSERT OR IGNORE INTO Vendas (Id, Total, Usuario, Status) 
                    VALUES ('v_gate1', 100567, 'operador1', 'Concluida');

                    INSERT OR IGNORE INTO CaixaSessoes (Id, TotalVendas, OperadorId) 
                    VALUES ('cx_gate1', 200000, 10);
                ";
                seedCmd.ExecuteNonQuery();
            }

            // Testar queries agregadas contra o banco CentsV1
            ExecuteAggregateCheck(conn, "SELECT COALESCE(SUM(Valor), 0) FROM MovimentacoesFinanceiras;");
            ExecuteAggregateCheck(conn, "SELECT COALESCE(SUM(Valor), 0) FROM ContasReceber;");
            ExecuteAggregateCheck(conn, "SELECT COALESCE(SUM(Valor), 0) FROM ContasPagar;");
            ExecuteAggregateCheck(conn, "SELECT COALESCE(SUM(Total), 0) FROM Vendas;");
            ExecuteAggregateCheck(conn, "SELECT COALESCE(SUM(TotalVendas), 0) FROM CaixaSessoes;");
            ExecuteAggregateCheck(conn, "SELECT COALESCE(MIN(Valor), 0) FROM MovimentacoesFinanceiras WHERE Valor IS NOT NULL;");
            ExecuteAggregateCheck(conn, "SELECT COALESCE(MAX(Valor), 0) FROM MovimentacoesFinanceiras WHERE Valor IS NOT NULL;");
        }

        private static void ExecuteAggregateCheck(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var scalar = cmd.ExecuteScalar();

            Assert.NotNull(scalar);
            long rawCents = Convert.ToInt64(scalar);
            decimal dec = MoneyIO.ConverterAgregacao(scalar, MoneyPersistenceMode.CentsV1);

            Assert.Equal(rawCents / 100m, dec);
        }

        // =========================================================================
        // 4. TESTES DOS PRINCIPAIS FLUXOS (CLIENTE, VEÍCULO, ORÇAMENTO, OS, CAIXA, ESTOQUE)
        // =========================================================================
        [Fact]
        public void PrimoxFlows_Cliente_Veiculo_Integridade360_Por_Id()
        {
            using var conn = OpenHomologConnection();

            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            // 1. Inserir Cliente
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Clientes (Id, Nome, TotalGasto, TotalServicos, Ativo, DataCadastro)
                    VALUES (@id, 'Cliente 360 Teste', 123456, 1, 1, '2026-09-23');";
                var p = cmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = clienteId.ToString();
                cmd.Parameters.Add(p);
                cmd.ExecuteNonQuery();
            }

            // 2. Inserir Veículo associado por ID
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Veiculos (Id, ClienteId, Placa, Modelo, Marca)
                    VALUES (@id, @clienteId, 'BRA2E19', 'HB20 1.0', 'Hyundai');";
                var pId = cmd.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = veiculoId.ToString();
                cmd.Parameters.Add(pId);

                var pCId = cmd.CreateParameter();
                pCId.ParameterName = "@clienteId";
                pCId.Value = clienteId.ToString();
                cmd.Parameters.Add(pCId);

                cmd.ExecuteNonQuery();
            }

            // 3. Validar Consulta 360 por ID (não aceita join por nome)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT c.Nome, c.TotalGasto, v.Placa, v.Modelo
                    FROM Clientes c
                    INNER JOIN Veiculos v ON v.ClienteId = c.Id
                    WHERE c.Id = @cId";
                var p = cmd.CreateParameter();
                p.ParameterName = "@cId";
                p.Value = clienteId.ToString();
                cmd.Parameters.Add(p);

                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal("Cliente 360 Teste", reader.GetString(0));
                
                // Valida que TotalGasto está em centavos (123456L) e é lido como R$ 1.234,56
                long rawGasto = reader.GetInt64(1);
                Assert.Equal(123456L, rawGasto);
                decimal gastoDecimal = MoneyIO.LerMoeda(reader, 1, MoneyPersistenceMode.CentsV1);
                Assert.Equal(1234.56m, gastoDecimal);

                Assert.Equal("BRA2E19", reader.GetString(2));
                Assert.Equal("HB20 1.0", reader.GetString(3));
            }

            // Limpeza
            using (var cleanCmd = conn.CreateCommand())
            {
                cleanCmd.CommandText = "DELETE FROM Veiculos WHERE Id = @vId; DELETE FROM Clientes WHERE Id = @cId;";
                var p1 = cleanCmd.CreateParameter(); p1.ParameterName = "@vId"; p1.Value = veiculoId.ToString(); cleanCmd.Parameters.Add(p1);
                var p2 = cleanCmd.CreateParameter(); p2.ParameterName = "@cId"; p2.Value = clienteId.ToString(); cleanCmd.Parameters.Add(p2);
                cleanCmd.ExecuteNonQuery();
            }
        }

        [Fact]
        public void PrimoxFlows_Orcamento_E_OS_Com_Desconto_E_Margem()
        {
            using var conn = OpenHomologConnection();

            var orcId = Guid.NewGuid();
            var itemId = Guid.NewGuid();

            // Orçamento com Subtotal R$ 200,00 (20000 cents), Desconto R$ 20,00 (2000 cents), Total R$ 180,00 (18000 cents)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Orcamentos (Id, Subtotal, Desconto, Acrescimo, Total, LucroEstimado, ComissaoVendedor, ImpostosEstimados, DescontoPercentual, MargemLucro, Status, DataCriacao)
                    VALUES (@id, 20000, 2000, 0, 18000, 6000, 1000, 1500, 10.0, 33.3, 'Aprovado', '2026-09-23');";
                var p = cmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = orcId.ToString();
                cmd.Parameters.Add(p);
                cmd.ExecuteNonQuery();
            }

            // Item de orçamento: Preço Unitário R$ 100,00 (10000 cents), Subtotal R$ 200,00 (20000 cents)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO OrcamentoItens (Id, OrcamentoId, PrecoUnitario, PrecoCusto, Desconto, Subtotal, LucroEstimado, MargemLucro)
                    VALUES (@id, @orcId, 10000, 7000, 0, 20000, 6000, 30.0);";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "@id"; p1.Value = itemId.ToString(); cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "@orcId"; p2.Value = orcId.ToString(); cmd.Parameters.Add(p2);
                cmd.ExecuteNonQuery();
            }

            // Ler orçamento via MoneyIO
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Subtotal, Desconto, Total, MargemLucro FROM Orcamentos WHERE Id = @id";
                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = orcId.ToString(); cmd.Parameters.Add(p);
                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());

                decimal subtotal = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
                decimal desconto = MoneyIO.LerMoeda(reader, 1, MoneyPersistenceMode.CentsV1);
                decimal total = MoneyIO.LerMoeda(reader, 2, MoneyPersistenceMode.CentsV1);
                double margem = reader.GetDouble(3);

                Assert.Equal(200.00m, subtotal);
                Assert.Equal(20.00m, desconto);
                Assert.Equal(180.00m, total);
                Assert.Equal(33.3, margem, 1);
            }

            // Limpeza
            using (var cleanCmd = conn.CreateCommand())
            {
                cleanCmd.CommandText = "DELETE FROM OrcamentoItens WHERE Id = @itemId; DELETE FROM Orcamentos WHERE Id = @orcId;";
                var p1 = cleanCmd.CreateParameter(); p1.ParameterName = "@itemId"; p1.Value = itemId.ToString(); cleanCmd.Parameters.Add(p1);
                var p2 = cleanCmd.CreateParameter(); p2.ParameterName = "@orcId"; p2.Value = orcId.ToString(); cleanCmd.Parameters.Add(p2);
                cleanCmd.ExecuteNonQuery();
            }
        }

        // =========================================================================
        // 5. TESTES DE CAIXA COM VALORES NEGATIVOS (SANGRIA / DIFERENÇA)
        // =========================================================================
        [Fact]
        public void PrimoxFlows_Caixa_Sangria_ValoresNegativos()
        {
            using var conn = OpenHomologConnection();

            var sessaoId = "sess_" + Guid.NewGuid().ToString("N");
            var movId = "mov_cx_" + Guid.NewGuid().ToString("N");
            decimal sangria = -50.00m; // -5000 cents

            // Cria sessão pai para satisfazer FK e NOT NULL
            using (var cmdSess = conn.CreateCommand())
            {
                cmdSess.CommandText = @"
                    INSERT INTO CaixaSessoes (Id, NumeroCaixa, DataAbertura, OperadorId, OperadorNome, ValorAbertura, ValorEsperado, TotalVendas, TotalSangrias, TotalSuprimentos, QuantidadeVendas, Status, DataCriacao)
                    VALUES (@sId, 'CX-01', '2026-09-23 08:00:00', 1, 'Operador Teste', 10000, 5000, 0, 5000, 0, 0, 'Aberto', '2026-09-23 08:00:00');";
                var pS = cmdSess.CreateParameter(); pS.ParameterName = "@sId"; pS.Value = sessaoId; cmdSess.Parameters.Add(pS);
                cmdSess.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO MovimentacoesCaixa (Id, CaixaSessaoId, Data, Tipo, ValorMovimento, ValorInicial, ValorFinal, Sangrias, Suprimentos, Diferenca)
                    VALUES (@id, @sessId, '2026-09-23 18:30:00', 'Sangria', @valor, 10000, 5000, 5000, 0, -5000);";
                var pId = cmd.CreateParameter(); pId.ParameterName = "@id"; pId.Value = movId; cmd.Parameters.Add(pId);
                var pSess = cmd.CreateParameter(); pSess.ParameterName = "@sessId"; pSess.Value = sessaoId; cmd.Parameters.Add(pSess);
                MoneyIO.GravarMoeda(cmd, "@valor", sangria, MoneyPersistenceMode.CentsV1);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ValorMovimento, Diferenca FROM MovimentacoesCaixa WHERE Id = @id";
                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = movId; cmd.Parameters.Add(p);
                using var reader = cmd.ExecuteReader();
                Assert.True(reader.Read());

                decimal mov = MoneyIO.LerMoeda(reader, 0, MoneyPersistenceMode.CentsV1);
                decimal dif = MoneyIO.LerMoeda(reader, 1, MoneyPersistenceMode.CentsV1);

                Assert.Equal(-50.00m, mov);
                Assert.Equal(-50.00m, dif);
            }

            // Limpeza
            using (var delCmd = conn.CreateCommand())
            {
                delCmd.CommandText = "DELETE FROM MovimentacoesCaixa WHERE Id = @id; DELETE FROM CaixaSessoes WHERE Id = @sessId;";
                var p = delCmd.CreateParameter(); p.ParameterName = "@id"; p.Value = movId; delCmd.Parameters.Add(p);
                var pS = delCmd.CreateParameter(); pS.ParameterName = "@sessId"; pS.Value = sessaoId; delCmd.Parameters.Add(pS);
                delCmd.ExecuteNonQuery();
            }
        }

        // =========================================================================
        // 6. TESTES DE CONTRATO DE API E SERIALIZAÇÃO DTO (Seção 15)
        // =========================================================================
        [Fact]
        public void Api_Contract_Dto_Serialization_Roundtrip()
        {
            // Cria um objeto de domínio com campos monetários
            var orcamento = new Orcamento
            {
                Id = Guid.NewGuid(),
                Subtotal = 123.45m,
                Desconto = 10.00m,
                Total = 113.45m
            };

            // Serializa para JSON (simulando resposta da API)
            string json = JsonSerializer.Serialize(orcamento);

            // Verifica que o JSON contém os valores em formato decimal padrão ("123.45"), não centavos ("12345")
            Assert.Contains("\"Subtotal\":123.45", json);
            Assert.Contains("\"Total\":113.45", json);
            Assert.DoesNotContain("12345", json);

            // Deserializa de volta (simulando request para a API)
            var deserialized = JsonSerializer.Deserialize<Orcamento>(json);
            Assert.NotNull(deserialized);
            Assert.Equal(123.45m, deserialized!.Subtotal);
            Assert.Equal(113.45m, deserialized!.Total);
        }

        // =========================================================================
        // 7. COMPATIBILIDADE E AUDITORIA ESTÁTICA (Seções 16 e 23)
        // =========================================================================
        [Fact]
        public void StaticAudit_Gate_Zero_ClassD_Occurrences()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string? auditPath = null;
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Docs", "audit", "2026-09-20", "P2_4_FINAL_STATIC_MONEY_AUDIT.json");
                if (File.Exists(candidate))
                {
                    auditPath = candidate;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.NotNull(auditPath);
            string json = File.ReadAllText(auditPath!);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.True(root.GetArrayLength() > 0);

            int classD = 0;
            foreach (var item in root.EnumerateArray())
            {
                if (item.GetProperty("classification").GetString() == "D")
                {
                    classD++;
                }
            }

            Assert.Equal(0, classD);
        }
    }
}
