using System;
using PRIMOX.Domain.Exceptions;

namespace PRIMOX.Domain.ValueObjects
{
    /// <summary>
    /// Value Object representando valores monetários no PRIMOX.
    /// Garante precisão decimal (2 casas), moeda explícita, imutabilidade e proteção contra operações acidentais multi-moeda.
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

        public Currency SafeCurrency =>
            string.IsNullOrWhiteSpace(Currency.Code) ? Currency.BRL : Currency;

        public static Money Zero(Currency? currency = null) => new(0m, currency ?? Currency.BRL);

        public static Money FromBRL(decimal amount) => new(amount, Currency.BRL);
        public static Money FromUSD(decimal amount) => new(amount, Currency.USD);
        public static Money FromEUR(decimal amount) => new(amount, Currency.EUR);

        public bool IsZero => Amount == 0m;
        public bool IsPositive => Amount > 0m;
        public bool IsNegative => Amount < 0m;

        public Money Abs() => new(Math.Abs(Amount), SafeCurrency);

        public static Money operator +(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return new Money(left.Amount + right.Amount, left.SafeCurrency);
        }

        public static Money operator -(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return new Money(left.Amount - right.Amount, left.SafeCurrency);
        }

        public static Money operator *(Money left, decimal multiplier) =>
            new(left.Amount * multiplier, left.SafeCurrency);

        public static Money operator *(decimal multiplier, Money right) =>
            new(right.Amount * multiplier, right.SafeCurrency);

        public static Money operator /(Money left, decimal divisor)
        {
            if (divisor == 0m)
                throw new DivideByZeroException("Divisor monetário não pode ser zero.");

            return new Money(left.Amount / divisor, left.SafeCurrency);
        }

        public static bool operator ==(Money left, Money right) => left.Equals(right);
        public static bool operator !=(Money left, Money right) => !left.Equals(right);
        public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
        public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
        public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
        public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

        public bool Equals(Money other) =>
            Amount == other.Amount && SafeCurrency.SafeCode == other.SafeCurrency.SafeCode;

        public override bool Equals(object? obj) => obj is Money other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Amount, SafeCurrency.SafeCode);

        public int CompareTo(Money other)
        {
            EnsureSameCurrency(this, other);
            return Amount.CompareTo(other.Amount);
        }

        public override string ToString() =>
            string.Format(SafeCurrency.Culture, "{0:C}", Amount);

        private static void EnsureSameCurrency(Money left, Money right)
        {
            var codeA = left.SafeCurrency.SafeCode;
            var codeB = right.SafeCurrency.SafeCode;

            if (!string.Equals(codeA, codeB, StringComparison.OrdinalIgnoreCase))
            {
                throw new MoedasDivergentesException(codeA, codeB);
            }
        }
    }
}
