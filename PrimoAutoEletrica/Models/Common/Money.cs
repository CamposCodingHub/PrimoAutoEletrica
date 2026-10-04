using System;
using System.Globalization;

namespace PrimoAutoEletrica.Models.Common
{
    /// <summary>
    /// Moeda suportada no ecossistema PRIMOX.
    /// </summary>
    public readonly record struct Currency(string Code, string Symbol, string CultureName)
    {
        public static readonly Currency BRL = new("BRL", "R$", "pt-BR");
        public static readonly Currency USD = new("USD", "$", "en-US");
        public static readonly Currency EUR = new("EUR", "€", "es-ES");

        public CultureInfo Culture => CultureInfo.GetCultureInfo(CultureName);

        public override string ToString() => Code;
    }

    /// <summary>
    /// Value Object representando valores monetários no PRIMOX.
    /// Encapsula precisão decimal, moeda e regras de apresentação.
    /// </summary>
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public decimal Amount { get; }
        public Currency Currency { get; }

        public Money(decimal amount, Currency? currency = null)
        {
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            Currency = currency ?? Currency.BRL;
        }

        public static Money Zero(Currency? currency = null) => new(0m, currency);

        public static Money FromBRL(decimal amount) => new(amount, Currency.BRL);
        public static Money FromUSD(decimal amount) => new(amount, Currency.USD);

        public static Money operator +(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return new Money(left.Amount + right.Amount, left.Currency);
        }

        public static Money operator -(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return new Money(left.Amount - right.Amount, left.Currency);
        }

        public static Money operator *(Money left, decimal multiplier) =>
            new(left.Amount * multiplier, left.Currency);

        public static Money operator /(Money left, decimal divisor)
        {
            if (divisor == 0) throw new DivideByZeroException("Divisor monetário não pode ser zero.");
            return new Money(left.Amount / divisor, left.Currency);
        }

        public static bool operator ==(Money left, Money right) => left.Equals(right);
        public static bool operator !=(Money left, Money right) => !left.Equals(right);
        public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
        public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
        public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
        public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

        public bool Equals(Money other) => Amount == other.Amount && Currency.Code == other.Currency.Code;
        public override bool Equals(object? obj) => obj is Money other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Amount, Currency.Code);

        public int CompareTo(Money other)
        {
            EnsureSameCurrency(this, other);
            return Amount.CompareTo(other.Amount);
        }

        public override string ToString() =>
            string.Format(Currency.Culture, "{0:C}", Amount);

        private static void EnsureSameCurrency(Money left, Money right)
        {
            if (left.Currency.Code != right.Currency.Code)
            {
                throw new InvalidOperationException($"Não é permitido operar moedas diferentes: '{left.Currency.Code}' e '{right.Currency.Code}'.");
            }
        }
    }
}
