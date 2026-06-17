using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public static class CatalogoCodeNormalizer
    {
        private static readonly Regex SpacesRegex = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex CodeRegex = new(
            @"^(?<brand>[A-Z]{2,6})[\s-]*(?<body>[A-Z0-9]+(?:-[A-Z0-9]+)*)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static string NormalizeCode(string? codigo, string? marca = null)
        {
            var pretty = FormatManufacturerCode(codigo, marca);
            if (string.IsNullOrWhiteSpace(pretty))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(pretty.Length);
            foreach (var character in pretty.ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        public static string FormatManufacturerCode(string? codigo, string? marca = null)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return string.Empty;
            }

            var value = SpacesRegex.Replace(codigo.Trim().ToUpperInvariant(), " ");
            if (!string.IsNullOrWhiteSpace(marca))
            {
                var brand = SanitizeFreeText(marca).ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(brand) &&
                    !value.StartsWith(brand, StringComparison.OrdinalIgnoreCase))
                {
                    value = $"{brand} {value}";
                }
            }

            var match = CodeRegex.Match(value.Replace(" -", "-").Replace("- ", "-"));
            if (!match.Success)
            {
                return value;
            }

            var detectedBrand = match.Groups["brand"].Value.ToUpperInvariant();
            var body = match.Groups["body"].Value.ToUpperInvariant();
            return $"{detectedBrand} {body}";
        }

        public static string NormalizeStatusForPersistence(string? status)
        {
            return status?.Trim() switch
            {
                "Revisado" => "Revisado",
                "Incompleto" => "Incompleto",
                "DuplicadoProvavel" => "DuplicadoProvavel",
                "ConvertidoEstoque" => "ConvertidoEstoque",
                "Ignorado" => "Ignorado",
                _ => "Pendente"
            };
        }

        public static string NormalizePreviewStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "Novo";
            }

            var normalized = status.Trim();
            return normalized switch
            {
                "Novo" => "Novo",
                "DuplicadoProvavel" => "DuplicadoProvavel",
                "Ja existente" => "Ja existente",
                "Incompleto" => "Incompleto",
                "Pendente" => "Pendente",
                "Pendente de revisao" => "Pendente de revisao",
                "Revisado" => "Revisado",
                _ => normalized
            };
        }

        public static string SanitizeFreeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return SpacesRegex.Replace(value.Trim(), " ");
        }

        public static string NormalizeHeader(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
