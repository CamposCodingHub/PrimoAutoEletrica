using System;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Empresa/estabelecimento fiscal lógico (multi-tenant preparado).
    /// Dados reais de CNPJ/IE ficam vazios até configuração — nunca inventados.
    /// </summary>
    public sealed class FiscalEmpresaRecord
    {
        public Guid Id { get; set; }
        public string CodigoInterno { get; set; } = string.Empty;
        public string NomeExibicao { get; set; } = string.Empty;
        public bool Ativa { get; set; } = true;
        public FiscalIssuerProfile Emitente { get; set; } = new();
        public FiscalProviderKind ProviderPreferido { get; set; } = FiscalProviderKind.FocusNfe;
        public FiscalEnvironment AmbientePadrao { get; set; } = FiscalEnvironment.Homologation;
        public string? SerieNFe { get; set; }
        public string? SerieNFCe { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Totais determinísticos de itens fiscais (sem inventar tributos SEFAZ).</summary>
    public static class FiscalItemTotaller
    {
        public static decimal RoundMoney(decimal value)
            => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static decimal LineTotal(decimal quantidade, decimal valorUnitario, decimal desconto = 0m)
            => RoundMoney((quantidade * valorUnitario) - desconto);

        public static (decimal Produtos, decimal Descontos, decimal Frete, decimal Seguro, decimal Despesas, decimal Total)
            Summarize(
                decimal produtos,
                decimal descontos = 0m,
                decimal frete = 0m,
                decimal seguro = 0m,
                decimal despesas = 0m)
        {
            var p = RoundMoney(produtos);
            var d = RoundMoney(descontos);
            var f = RoundMoney(frete);
            var s = RoundMoney(seguro);
            var e = RoundMoney(despesas);
            var total = RoundMoney(p - d + f + s + e);
            return (p, d, f, s, e, total);
        }
    }
}
