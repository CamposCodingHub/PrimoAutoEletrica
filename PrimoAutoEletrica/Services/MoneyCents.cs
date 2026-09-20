using System;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// P0.10 foundation: integer centavos. DB migration to INTEGER cents is NOT DONE.
    /// Use for new math; do not claim SQLite columns are cents yet.
    /// </summary>
    public readonly struct MoneyCents : IEquatable<MoneyCents>, IComparable<MoneyCents>
    {
        public long Cents { get; }

        public MoneyCents(long cents) => Cents = cents;

        public static MoneyCents FromDecimal(decimal amount) =>
            new(decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero)));

        public static MoneyCents FromDouble(double amount) =>
            FromDecimal((decimal)amount);

        public decimal ToDecimal() => Cents / 100m;

        public static MoneyCents operator +(MoneyCents a, MoneyCents b) => new(a.Cents + b.Cents);
        public static MoneyCents operator -(MoneyCents a, MoneyCents b) => new(a.Cents - b.Cents);

        public static MoneyCents ApplyPercentDiscount(MoneyCents gross, decimal percent)
        {
            if (percent < 0m) throw new ArgumentOutOfRangeException(nameof(percent));
            if (percent > 100m) percent = 100m;
            var discount = decimal.ToInt64(decimal.Round(gross.Cents * (percent / 100m), 0, MidpointRounding.AwayFromZero));
            return new MoneyCents(gross.Cents - discount);
        }

        public bool Equals(MoneyCents other) => Cents == other.Cents;
        public override bool Equals(object? obj) => obj is MoneyCents m && Equals(m);
        public override int GetHashCode() => Cents.GetHashCode();
        public int CompareTo(MoneyCents other) => Cents.CompareTo(other.Cents);
        public override string ToString() => ToDecimal().ToString("0.00");

        public static bool operator ==(MoneyCents a, MoneyCents b) => a.Equals(b);
        public static bool operator !=(MoneyCents a, MoneyCents b) => !a.Equals(b);
    }
}
