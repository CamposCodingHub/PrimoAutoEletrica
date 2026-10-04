using System;
using System.Globalization;

namespace PRIMOX.Domain.ValueObjects
{
    /// <summary>
    /// Moeda suportada no ecossistema PRIMOX.
    /// Imutável, segura para internacionalização.
    /// </summary>
    public readonly record struct Currency(string Code, string Symbol, string CultureName)
    {
        public static readonly Currency BRL = new("BRL", "R$", "pt-BR");
        public static readonly Currency USD = new("USD", "$", "en-US");
        public static readonly Currency EUR = new("EUR", "€", "de-DE");

        public CultureInfo Culture
        {
            get
            {
                try
                {
                    return CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(CultureName) ? "pt-BR" : CultureName);
                }
                catch
                {
                    return CultureInfo.InvariantCulture;
                }
            }
        }

        public string SafeCode => string.IsNullOrWhiteSpace(Code) ? "BRL" : Code;

        public override string ToString() => SafeCode;
    }
}
