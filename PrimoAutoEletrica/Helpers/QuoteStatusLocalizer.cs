using System.Collections.Generic;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Apresentacao localizada de status de orçamento.
    /// IDs internos permanecem em portugues tecnico (maquina de estados).
    /// </summary>
    public static class QuoteStatusLocalizer
    {
        private static readonly Dictionary<string, string> KeyByStatus = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Rascunho"] = "Draft",
            ["Em Aberto"] = "QuoteStatusOpen",
            ["Em aberto"] = "QuoteStatusOpen",
            ["Enviado"] = "QuoteStatusSent",
            ["Aprovado"] = "QuoteStatusApproved",
            ["Recusado"] = "QuoteStatusRejected",
            ["Vencido"] = "QuoteStatusExpired",
            ["Convertido em Venda"] = "QuoteStatusConvertedSale",
            ["Convertido em OS"] = "QuoteStatusConvertedOs"
        };

        public static string Display(string? internalStatus)
        {
            if (string.IsNullOrWhiteSpace(internalStatus))
            {
                return string.Empty;
            }

            var trimmed = internalStatus.Trim();
            if (KeyByStatus.TryGetValue(trimmed, out var key))
            {
                return LocalizationService.Instance.GetString(key);
            }

            return trimmed;
        }

        public static string DisplayWithLabel(string? internalStatus)
        {
            var display = Display(internalStatus);
            if (string.IsNullOrEmpty(display))
            {
                return string.Empty;
            }

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                LocalizationService.Instance.GetString("StatusLabelFormat"),
                display);
        }
    }
}
