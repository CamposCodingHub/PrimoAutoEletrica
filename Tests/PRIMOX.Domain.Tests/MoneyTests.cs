using System;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.ValueObjects;
using Xunit;

namespace PRIMOX.Domain.Tests
{
    public class MoneyTests
    {
        [Fact]
        public void Zero_Deve_Retornar_ValorZero()
        {
            var money = Money.Zero();
            Assert.True(money.IsZero);
            Assert.Equal(0m, money.Amount);
            Assert.Equal("BRL", money.Currency.SafeCode);
        }

        [Fact]
        public void Criacao_Com_Valores_Positivos_E_Negativos()
        {
            var positivo = Money.FromBRL(150.50m);
            var negativo = Money.FromBRL(-45.20m);

            Assert.True(positivo.IsPositive);
            Assert.False(positivo.IsNegative);
            Assert.Equal(150.50m, positivo.Amount);

            Assert.True(negativo.IsNegative);
            Assert.False(negativo.IsPositive);
            Assert.Equal(-45.20m, negativo.Amount);
        }

        [Fact]
        public void Arredondamento_Deve_Seguir_MidpointRounding_AwayFromZero()
        {
            var money = new Money(100.555m, Currency.BRL);
            Assert.Equal(100.56m, money.Amount);

            var moneyBaixo = new Money(100.554m, Currency.BRL);
            Assert.Equal(100.55m, moneyBaixo.Amount);
        }

        [Fact]
        public void Soma_Com_MesmaMoeda_Deve_Calcular_Corretamente()
        {
            var m1 = Money.FromBRL(100.25m);
            var m2 = Money.FromBRL(50.75m);

            var resultado = m1 + m2;

            Assert.Equal(151.00m, resultado.Amount);
            Assert.Equal("BRL", resultado.Currency.SafeCode);
        }

        [Fact]
        public void Subtracao_Com_MesmaMoeda_Deve_Calcular_Corretamente()
        {
            var m1 = Money.FromBRL(200.00m);
            var m2 = Money.FromBRL(80.50m);

            var resultado = m1 - m2;

            Assert.Equal(119.50m, resultado.Amount);
        }

        [Fact]
        public void Multiplicacao_Por_Escalar_Deve_Funcionar_Comutativa()
        {
            var m = Money.FromBRL(50.00m);

            var res1 = m * 3m;
            var res2 = 3m * m;

            Assert.Equal(150.00m, res1.Amount);
            Assert.Equal(150.00m, res2.Amount);
        }

        [Fact]
        public void Divisao_Por_Escalar_Deve_Calcular_Corretamente()
        {
            var m = Money.FromBRL(100.00m);
            var resultado = m / 4m;

            Assert.Equal(25.00m, resultado.Amount);
        }

        [Fact]
        public void Divisao_Por_Zero_Deve_Lancar_DivideByZeroException()
        {
            var m = Money.FromBRL(100.00m);
            Assert.Throws<DivideByZeroException>(() => m / 0m);
        }

        [Fact]
        public void Operacao_Entre_Moedas_Diferentes_Deve_Lancar_MoedasDivergentesException()
        {
            var brl = Money.FromBRL(100m);
            var usd = Money.FromUSD(100m);

            var ex = Assert.Throws<MoedasDivergentesException>(() => brl + usd);
            Assert.Equal("BRL", ex.MoedaOrigem);
            Assert.Equal("USD", ex.MoedaDestino);
        }

        [Fact]
        public void Comparacoes_De_Igualdade_E_Ordem()
        {
            var menor = Money.FromBRL(50m);
            var maior = Money.FromBRL(100m);
            var igual = Money.FromBRL(50m);

            Assert.True(menor < maior);
            Assert.True(menor <= igual);
            Assert.True(maior > menor);
            Assert.True(menor == igual);
            Assert.False(menor != igual);
        }

        [Fact]
        public void Abs_Deve_Retornar_Valor_Absoluto()
        {
            var negativo = Money.FromBRL(-99.90m);
            var absoluto = negativo.Abs();

            Assert.Equal(99.90m, absoluto.Amount);
            Assert.True(absoluto.IsPositive);
        }
    }
}
