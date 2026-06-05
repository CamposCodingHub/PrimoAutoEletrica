using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Helpers
{
    public static class UiTextSanitizer
    {
        private static readonly IReadOnlyDictionary<string, string> Replacements = new Dictionary<string, string>
        {
            ["Ã¡"] = "á",
            ["Ã "] = "à",
            ["Ã¢"] = "â",
            ["Ã£"] = "ã",
            ["Ã©"] = "é",
            ["Ãª"] = "ê",
            ["Ã­"] = "í",
            ["Ã³"] = "ó",
            ["Ã´"] = "ô",
            ["Ãµ"] = "õ",
            ["Ãº"] = "ú",
            ["Ã§"] = "ç",
            ["Ã"] = "Á",
            ["Ã€"] = "À",
            ["Ã‚"] = "Â",
            ["Ãƒ"] = "Ã",
            ["Ã‰"] = "É",
            ["ÃŠ"] = "Ê",
            ["Ã"] = "Í",
            ["Ã“"] = "Ó",
            ["Ã”"] = "Ô",
            ["Ã•"] = "Õ",
            ["Ãš"] = "Ú",
            ["Ã‡"] = "Ç",
            ["ï¿½Â¡"] = "á",
            ["ï¿½Â "] = "à",
            ["ï¿½Â¢"] = "â",
            ["ï¿½Â£"] = "ã",
            ["ï¿½Â©"] = "é",
            ["ï¿½Âª"] = "ê",
            ["ï¿½Â­"] = "í",
            ["ï¿½Â³"] = "ó",
            ["ï¿½Â´"] = "ô",
            ["ï¿½Âµ"] = "õ",
            ["ï¿½Âº"] = "ú",
            ["ï¿½Â§"] = "ç",
            ["ï¿½Â"] = "Á",
            ["ï¿½Â‰"] = "É",
            ["ï¿½Â"] = "Í",
            ["ï¿½Â“"] = "Ó",
            ["ï¿½Âš"] = "Ú",
            ["ï¿½Â‡"] = "Ç",
            ["â€¢"] = " - ",
            ["â€“"] = "-",
            ["â€”"] = "-",
            ["Âº"] = "º",
            ["Âª"] = "ª",
            ["Â°"] = "°"
        };

        public static string SanitizeText(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var text = value;
            foreach (var replacement in Replacements)
            {
                text = text.Replace(replacement.Key, replacement.Value);
            }

            return text.Replace("  ", " ").Trim();
        }

        public static string SanitizeIcon(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var text = SanitizeText(value);
            return LooksCorrupted(text) || text.Any(character => character > 126)
                ? "*"
                : text.Trim();
        }

        public static object SanitizeValue(object? value)
        {
            return value is string text ? SanitizeText(text) : value ?? string.Empty;
        }

        public static string NormalizeKey(string? value)
        {
            var text = SanitizeText(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
        }

        public static bool EqualsNormalized(string? value, string expected)
        {
            return string.Equals(NormalizeKey(value), NormalizeKey(expected), StringComparison.Ordinal);
        }

        private static bool LooksCorrupted(string value)
        {
            return value.Contains('�')
                || value.Contains("ï¿½", StringComparison.Ordinal)
                || value.Contains("â€", StringComparison.Ordinal)
                || value.Contains("Â", StringComparison.Ordinal);
        }
    }
}
