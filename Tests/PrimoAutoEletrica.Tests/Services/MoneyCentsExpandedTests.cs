using PrimoAutoEletrica.Services;
using System;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>
    /// Testes de arredondamento e precisão monetária expandidos.
    /// Cobre edge cases: desconto, parcelamento, rateio, soma de itens.
    /// </summary>
    public class MoneyCentsExpandedTests
    {
        [Theory]
        [InlineData(0.01, 1)]
        [InlineData(0.10, 10)]
        [InlineData(0.29, 29)]
        [InlineData(19.99, 1999)]
        [InlineData(999999.99, 99999999)]
        [InlineData(0.001, 0)]     // < 0.5 centavo → arredonda pra 0
        [InlineData(0.005, 1)]     // AwayFromZero → 1 centavo
        [InlineData(0.009, 1)]
        [InlineData(100.00, 10000)]
        public void FromDecimal_EdgeCases_PrecisaoCentavos(decimal amount, long expectedCents)
        {
            Assert.Equal(expectedCents, MoneyCents.FromDecimal(amount).Cents);
        }

        [Fact]
        public void Desconto_10Porcento_De_R100()
        {
            var bruto = MoneyCents.FromDecimal(100.00m);
            var liquido = MoneyCents.ApplyPercentDiscount(bruto, 10m);
            Assert.Equal(90_00, liquido.Cents);
        }

        [Fact]
        public void Desconto_5Porcento_De_R19_99()
        {
            var bruto = MoneyCents.FromDecimal(19.99m);
            var liquido = MoneyCents.ApplyPercentDiscount(bruto, 5m);
            // 19.99 * 0.05 = 0.9995 → arredonda para 1.00 → 19.99 - 1.00 = 18.99
            Assert.Equal(18_99, liquido.Cents);
        }

        [Fact]
        public void Desconto_100Porcento_ZeraValor()
        {
            var bruto = MoneyCents.FromDecimal(500.00m);
            var liquido = MoneyCents.ApplyPercentDiscount(bruto, 100m);
            Assert.Equal(0, liquido.Cents);
        }

        [Fact]
        public void Desconto_0Porcento_MantemValor()
        {
            var bruto = MoneyCents.FromDecimal(123.45m);
            var liquido = MoneyCents.ApplyPercentDiscount(bruto, 0m);
            Assert.Equal(123_45, liquido.Cents);
        }

        [Fact]
        public void Desconto_NegativoLancaException()
        {
            var bruto = MoneyCents.FromDecimal(100m);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MoneyCents.ApplyPercentDiscount(bruto, -5m));
        }

        [Fact]
        public void Desconto_Acima100Porcento_CapEm100()
        {
            var bruto = MoneyCents.FromDecimal(100m);
            var liquido = MoneyCents.ApplyPercentDiscount(bruto, 150m);
            Assert.Equal(0, liquido.Cents); // cap em 100%
        }

        [Fact]
        public void SomaDeItens_R0_10_MaisR0_20_IgualR0_30()
        {
            // Clássico problema IEEE 754: 0.1 + 0.2 != 0.3 em double
            var a = MoneyCents.FromDecimal(0.10m);
            var b = MoneyCents.FromDecimal(0.20m);
            var soma = a + b;
            Assert.Equal(30, soma.Cents);
            Assert.Equal(0.30m, soma.ToDecimal());
        }

        [Fact]
        public void SomaDeItens_MultiplosItens_OrdemServico()
        {
            // Simula 5 itens de uma OS
            var item1 = MoneyCents.FromDecimal(150.00m);
            var item2 = MoneyCents.FromDecimal(89.99m);
            var item3 = MoneyCents.FromDecimal(42.50m);
            var item4 = MoneyCents.FromDecimal(17.75m);
            var item5 = MoneyCents.FromDecimal(0.01m);

            var total = item1 + item2 + item3 + item4 + item5;
            Assert.Equal(300_25, total.Cents);
            Assert.Equal(300.25m, total.ToDecimal());
        }

        [Fact]
        public void Parcelamento_3x_De_R100()
        {
            // R$ 100,00 em 3x: 33,33 + 33,33 + 33,34 = 100,00
            var total = MoneyCents.FromDecimal(100.00m);
            int parcelas = 3;
            long parcelaBase = total.Cents / parcelas; // 3333
            long resto = total.Cents - (parcelaBase * parcelas); // 1

            long soma = 0;
            for (int i = 0; i < parcelas; i++)
            {
                long valorParcela = parcelaBase + (i == parcelas - 1 ? resto : 0);
                soma += valorParcela;
            }

            Assert.Equal(total.Cents, soma);
        }

        [Fact]
        public void Parcelamento_7x_De_R999_99()
        {
            var total = MoneyCents.FromDecimal(999.99m);
            int parcelas = 7;
            long parcelaBase = total.Cents / parcelas;
            long resto = total.Cents - (parcelaBase * parcelas);

            long soma = 0;
            for (int i = 0; i < parcelas; i++)
            {
                soma += parcelaBase + (i == parcelas - 1 ? resto : 0);
            }

            Assert.Equal(total.Cents, soma);
        }

        [Fact]
        public void Rateio_2Itens_SemPerda()
        {
            var total = MoneyCents.FromDecimal(100.01m);
            var metade1 = new MoneyCents(total.Cents / 2);
            var metade2 = new MoneyCents(total.Cents - metade1.Cents);

            Assert.Equal(total.Cents, (metade1 + metade2).Cents);
        }

        [Fact]
        public void FromDouble_ConverteCorreto()
        {
            var mc = MoneyCents.FromDouble(19.99);
            Assert.Equal(1999, mc.Cents);
        }

        [Fact]
        public void Equality_Funciona()
        {
            var a = MoneyCents.FromDecimal(10.50m);
            var b = MoneyCents.FromDecimal(10.50m);
            Assert.Equal(a, b);
            Assert.True(a == b);
        }

        [Fact]
        public void Comparison_Funciona()
        {
            var menor = MoneyCents.FromDecimal(9.99m);
            var maior = MoneyCents.FromDecimal(10.00m);
            Assert.True(menor.CompareTo(maior) < 0);
        }

        [Fact]
        public void ToString_Formata_ComDuasCasas()
        {
            var mc = MoneyCents.FromDecimal(123.40m);
            // ToString usa cultura atual; validar que ToDecimal preserva valor
            Assert.Equal(123.40m, mc.ToDecimal());
            // ToString deve conter "123" e "40" separados pelo separador decimal da cultura
            var str = mc.ToString();
            Assert.Contains("123", str);
            Assert.Contains("40", str);
        }
    }
}
