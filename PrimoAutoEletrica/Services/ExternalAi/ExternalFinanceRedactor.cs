using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3.4 — ensure finance/restricted fields never leave to external provider without permission.
    /// Fail-closed: default strips finance classification and money-like patterns.
    /// </summary>
    public sealed class ExternalFinanceRedactor
    {
        private static readonly string[] FinanceClassTokens =
        {
            "FINANCIAL", "FINANCE", "FINANCEIRO", "COMMERCIAL_PRICE", "PRECO", "COST"
        };

        private static readonly Regex MoneyLike = new(
            @"(\bR\$\s*\d|\bUSD\s*\d|\btotal\s*(gasto|pago|receber)|pre[cç]o\s*de\s*venda|margem\s*%|comiss[aã]o\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public bool ShouldRedact(string? classification, bool includeFinancial)
        {
            if (includeFinancial) return false;
            if (string.IsNullOrWhiteSpace(classification)) return false;
            return FinanceClassTokens.Any(t => classification.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        public string RedactText(string? text, bool includeFinancial)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (includeFinancial) return text;
            if (!MoneyLike.IsMatch(text) && !ContainsFinanceKeyword(text))
                return text;
            return MoneyLike.Replace(text, "[REDACTED_FINANCE]");
        }

        public IReadOnlyList<string> FindFinanceLeaks(string? text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
            var hits = new List<string>();
            if (MoneyLike.IsMatch(text)) hits.Add("money-pattern");
            if (ContainsFinanceKeyword(text)) hits.Add("finance-keyword");
            return hits;
        }

        private static bool ContainsFinanceKeyword(string text)
        {
            string[] keys =
            {
                "contas a pagar", "contas a receber", "salario", "salário", "dre",
                "faturamento", "lucro liquido", "lucro líquido", "pedido de compra aprovado"
            };
            return keys.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
    }
}