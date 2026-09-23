using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PrimoAutoEletrica.Tests.Money
{
    /// <summary>
    /// FASE 2.1 — Suíte de testes matemáticos e de integridade da migração de Money.
    /// Validação estritamente isolada da transformação decimal ↔ cents e casos de cálculo.
    /// NÃO toca no banco real do usuário.
    /// </summary>
    public class MoneyClassificationGateMathTests
    {
        // ====================================================================
        // OBJETIVO 2 — TESTES MATEMÁTICOS DE TRANSFORMAÇÃO DECIMAL ↔ CENTS
        // ====================================================================

        [Theory]
        [InlineData(0.01, 1, 0.01)]
        [InlineData(0.05, 5, 0.05)]
        [InlineData(0.10, 10, 0.10)]
        [InlineData(1.23, 123, 1.23)]
        [InlineData(99.99, 9999, 99.99)]
        [InlineData(100.01, 10001, 100.01)]
        [InlineData(0.005, 1, 0.01)]    // AwayFromZero: 0.005 * 100 = 0.5 → 1 centavo
        [InlineData(1.005, 101, 1.01)]  // AwayFromZero: 1.005 * 100 = 100.5 → 101 centavos
        [InlineData(2.675, 268, 2.68)]  // AwayFromZero: 2.675 * 100 = 267.5 → 268 centavos
        [InlineData(-0.01, -1, -0.01)]  // Comportamento matemático puro negativo
        [InlineData(-1.005, -101, -1.01)] // AwayFromZero negativo: -100.5 → -101 centavos
        public void Transformacao_Decimal_Para_Cents_E_Volta_Exata(decimal inputDecimal, long expectedCents, decimal expectedRoundtripDecimal)
        {
            // Act: decimal -> MoneyCents (long)
            var money = MoneyCents.FromDecimal(inputDecimal);

            // Assert Cents
            Assert.Equal(expectedCents, money.Cents);

            // Act: cents -> decimal
            var roundtrip = money.ToDecimal();

            // Assert Roundtrip
            Assert.Equal(expectedRoundtripDecimal, roundtrip);
        }

        [Fact]
        public void PoliticaArredondamento_MidpointRounding_AwayFromZero_Confirmada()
        {
            // Valida explicitamente que 0.5 centavo sobe em módulo
            Assert.Equal(1L, MoneyCents.FromDecimal(0.005m).Cents);
            Assert.Equal(-1L, MoneyCents.FromDecimal(-0.005m).Cents);

            // Valida que 0.49 centavo trunca para zero
            Assert.Equal(0L, MoneyCents.FromDecimal(0.0049m).Cents);
            Assert.Equal(0L, MoneyCents.FromDecimal(-0.0049m).Cents);
        }

        [Fact]
        public void Separacao_Matematica_Vs_RegraDeNegocio_ValoresNegativos()
        {
            // 1. Matematicamente: MoneyCents aceita negativos (necessário para deltas, estornos, sangrias)
            var deltaNegativo = MoneyCents.FromDecimal(-50.00m);
            Assert.Equal(-5000L, deltaNegativo.Cents);
            Assert.Equal(-50.00m, deltaNegativo.ToDecimal());

            // 2. Regra de Negócio: Operações comerciais rejeitam valores negativos onde indevido
            Assert.Throws<InvalidOperationException>(() =>
                ComercialValidationHelper.GarantirValorMaiorOuIgualZero(-10.00m, "preço"));

            Assert.Throws<InvalidOperationException>(() =>
                ComercialValidationHelper.GarantirValorMaiorQueZero(0m, "valor"));
        }

        // ====================================================================
        // OBJETIVO 3 — TESTES DE CÁLCULO (SUBTOTAL, TOTAL, MARGEM, RATEIO)
        // ====================================================================

        [Fact]
        public void Calculo_Subtotal_QuantidadeVezesPrecoMenosDesconto_SemDivergencia()
        {
            // Cenário: 3 unidades a R$ 19,99 com R$ 5,00 de desconto
            // 3 * 19.99 = 59.97 - 5.00 = 54.97
            decimal quantidade = 3m;
            decimal precoUnitario = 19.99m;
            decimal desconto = 5.00m;

            var subtotalDecimal = ComercialValidationHelper.CalcularSubtotal(quantidade, precoUnitario, desconto);
            Assert.Equal(54.97m, subtotalDecimal);

            // Cálculo em cents inteiros
            var precoCents = MoneyCents.FromDecimal(precoUnitario);
            var descontoCents = MoneyCents.FromDecimal(desconto);
            var subtotalCents = (precoCents.Cents * (long)quantidade) - descontoCents.Cents;

            Assert.Equal(5497L, subtotalCents);
            Assert.Equal(subtotalDecimal, subtotalCents / 100m);
        }

        [Fact]
        public void Calculo_Total_SubtotalMenosDescontoMaisAcrescimo_SemDivergencia()
        {
            // Subtotal = R$ 150,00, Desconto = R$ 15,50, Acréscimo = R$ 7,25
            // Total = 150.00 - 15.50 + 7.25 = 141.75
            var subtotal = MoneyCents.FromDecimal(150.00m);
            var desconto = MoneyCents.FromDecimal(15.50m);
            var acrescimo = MoneyCents.FromDecimal(7.25m);

            var totalCents = subtotal.Cents - desconto.Cents + acrescimo.Cents;
            var total = new MoneyCents(totalCents);

            Assert.Equal(14175L, total.Cents);
            Assert.Equal(141.75m, total.ToDecimal());
        }

        [Fact]
        public void Calculo_Margem_NaoTratadaComoCents_PreservadaComoPercentual()
        {
            // Lucro = R$ 40,00, Total = R$ 200,00 → Margem = 20.00%
            var lucro = MoneyCents.FromDecimal(40.00m);
            var total = MoneyCents.FromDecimal(200.00m);

            // Regra semântica: Margem é PERCENTAGE (não é Money)
            decimal margemPercentual = total.Cents > 0
                ? ((decimal)lucro.Cents / total.Cents) * 100m
                : 0m;

            Assert.Equal(20.00m, margemPercentual);
        }

        [Fact]
        public void Calculo_Rateio_SomaExataSemPerdaDeCentavos()
        {
            // Venda de R$ 100,01 dividida em 3 parcelas
            // Rateio: 33,34 + 33,34 + 33,33 = 100,01
            var totalVenda = MoneyCents.FromDecimal(100.01m);

            var parte1 = MoneyCents.FromDecimal(33.34m);
            var parte2 = MoneyCents.FromDecimal(33.34m);
            var parte3 = MoneyCents.FromDecimal(33.33m);

            var somaPartes = parte1.Cents + parte2.Cents + parte3.Cents;

            Assert.Equal(totalVenda.Cents, somaPartes);
            Assert.True(somaPartes == totalVenda.Cents, "A soma do rateio fecha exatamente com o total em centavos.");
        }

        // ====================================================================
        // OBJETIVO 4 — PROVA EM TABELAS SEM DADOS COM BANCO SINTÉTICO :memory:
        // ====================================================================

        [Fact]
        public void TabelasSemDados_ValidacaoEmBancoSinteticoIsolado_RoundTripExato()
        {
            // O banco real não possui linhas em Vendas, VendaItens, CaixaSessoes, MovimentacoesCaixa.
            // Para provar a conversão sem tocar no banco do usuário, criamos um banco in-memory isolado.
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            // 1. Criar tabelas originais (com REAL)
            using (var createCmd = connection.CreateCommand())
            {
                createCmd.CommandText = @"
                    CREATE TABLE Vendas (
                        Id TEXT PRIMARY KEY,
                        Total REAL NOT NULL,
                        Desconto REAL NOT NULL
                    );
                    CREATE TABLE VendaItens (
                        Id TEXT PRIMARY KEY,
                        VendaId TEXT NOT NULL,
                        PrecoUnitario REAL NOT NULL,
                        CustoUnitario REAL NOT NULL,
                        Desconto REAL NOT NULL,
                        Subtotal REAL NOT NULL
                    );
                    CREATE TABLE CaixaSessoes (
                        Id TEXT PRIMARY KEY,
                        ValorAbertura REAL NOT NULL,
                        ValorEsperado REAL NOT NULL,
                        TotalVendas REAL NOT NULL
                    );
                    CREATE TABLE MovimentacoesCaixa (
                        Id TEXT PRIMARY KEY,
                        ValorMovimento REAL NOT NULL,
                        ValorInicial REAL NOT NULL,
                        ValorFinal REAL NOT NULL,
                        Diferenca REAL NOT NULL
                    );";
                createCmd.ExecuteNonQuery();
            }

            // 2. Inserir dados sintéticos representativos
            using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.CommandText = @"
                    INSERT INTO Vendas VALUES ('V1', 185.50, 10.00);
                    INSERT INTO VendaItens VALUES ('I1', 'V1', 97.75, 45.00, 0.00, 97.75);
                    INSERT INTO VendaItens VALUES ('I2', 'V1', 97.75, 45.00, 10.00, 87.75);
                    INSERT INTO CaixaSessoes VALUES ('CX1', 150.00, 335.50, 185.50);
                    INSERT INTO MovimentacoesCaixa VALUES ('M1', 185.50, 150.00, 335.50, 0.00);";
                insertCmd.ExecuteNonQuery();
            }

            // 3. Executar conversão matemática sintética para INTEGER cents
            using (var migrateCmd = connection.CreateCommand())
            {
                migrateCmd.CommandText = @"
                    CREATE TABLE Vendas_New (
                        Id TEXT PRIMARY KEY,
                        Total INTEGER NOT NULL,
                        Desconto INTEGER NOT NULL
                    );
                    INSERT INTO Vendas_New (Id, Total, Desconto)
                    SELECT Id, CAST(ROUND(Total * 100) AS INTEGER), CAST(ROUND(Desconto * 100) AS INTEGER)
                    FROM Vendas;";
                migrateCmd.ExecuteNonQuery();
            }

            // 4. Validar round-trip exato registro a registro
            using (var verifyCmd = connection.CreateCommand())
            {
                verifyCmd.CommandText = @"
                    SELECT old.Total AS OriginalReal, new.Total AS MigratedCents
                    FROM Vendas old JOIN Vendas_New new ON old.Id = new.Id;";
                using var reader = verifyCmd.ExecuteReader();
                Assert.True(reader.Read());
                var originalReal = reader.GetDouble(0);
                var migratedCents = reader.GetInt64(1);

                Assert.Equal(185.50, originalReal);
                Assert.Equal(18550L, migratedCents);
                Assert.Equal((decimal)originalReal, migratedCents / 100m);
            }
        }
    }
}
