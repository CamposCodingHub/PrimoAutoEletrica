using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public sealed class CatalogoMarcaPerfil
    {
        public string Marca { get; init; } = string.Empty;
        public string FonteCatalogoPadrao { get; init; } = string.Empty;
        public Regex CodigoRegex { get; init; } = null!;
    }

    public static class CatalogoMarcaDetector
    {
        private static readonly CatalogoMarcaPerfil PerfilDni = new()
        {
            Marca = "DNI",
            FonteCatalogoPadrao = "Catalogo DNI Automotive 2025/2026",
            // Codigos DNI sao tipicamente 3-4 digitos. Indices colam pagina (DNI 081438 = 0814 + pag 38).
            // Blocos grudados (DNI DNI 82178218... / DNI DNI DNI 210321052106) sao expandidos no extrator.
            CodigoRegex = new Regex(
                @"(?<codigo>DNI(?:\s*DNI){1,8}\s*\d{8,40})|(?<codigo>DNI[\s-]*\d{3,4}(?:-[A-Z]{1,4})?)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        };

        private static readonly CatalogoMarcaPerfil PerfilUeta = new()
        {
            Marca = "UETA",
            FonteCatalogoPadrao = "Catalogo Ueta Industria",
            CodigoRegex = new Regex(
                @"\b(?<codigo>U-\d{3,4}[A-Z]?)\b",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        };

        /// <summary>
        /// Perfil generico: letras+digito OU codigo numerico tipico de pecas (rolamentos 6205-2RS, etc.).
        /// </summary>
        private static readonly CatalogoMarcaPerfil PerfilGenerico = new()
        {
            Marca = "GERAL",
            FonteCatalogoPadrao = "Catalogo importado",
            CodigoRegex = new Regex(
                @"\b(?<codigo>(?:[A-Z]{1,6}[\s-]*)?\d{3,5}(?:[.\-/][A-Z0-9]{1,6}){0,3}|[A-Z]{2,6}[\s-]*\d[A-Z0-9.\-]{2,14})\b",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        };

        private static readonly IReadOnlyList<CatalogoMarcaPerfil> PerfisOrdenados = new[]
        {
            PerfilDni,
            PerfilUeta
        };

        private static readonly HashSet<string> MarcasConhecidasArquivo = new(StringComparer.OrdinalIgnoreCase)
        {
            "DNI", "UETA", "GF", "BOSCH", "NGK", "HELLA", "VALEO", "PHILIPS", "OSRAM", "MAGNETI", "MARELLI", "DELPHI", "FACET",
            "IKRO", "SKF", "NSK", "FAG", "INA", "NTN", "TIMKEN"
        };

        private static readonly HashSet<string> TokensArquivoIgnorados = new(StringComparer.OrdinalIgnoreCase)
        {
            "CATALOGO", "CATÃLOGO", "CATALOG", "PDF", "LINHA", "GERAL", "AMOSTRA", "MINI", "AUTO", "AUTOS", "MOTOS"
        };

        public static IReadOnlyList<CatalogoMarcaPerfil> ObterPerfis() =>
            PerfisOrdenados.Concat(new[] { PerfilGenerico }).ToList();

        public static CatalogoMarcaPerfil ObterPerfil(string? marca)
        {
            if (string.IsNullOrWhiteSpace(marca))
            {
                return PerfilGenerico;
            }

            var normalized = marca.Trim().ToUpperInvariant();
            var known = PerfisOrdenados.FirstOrDefault(perfil =>
                string.Equals(perfil.Marca, normalized, StringComparison.OrdinalIgnoreCase));
            if (known != null)
            {
                return known;
            }

            if (string.Equals(normalized, "GERAL", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "AUTO", StringComparison.OrdinalIgnoreCase))
            {
                return PerfilGenerico;
            }

            // Preserva a marca informada (ex.: GF, ROLAMENTOS) em vez de sobrescrever para GERAL.
            return CriarPerfilMarca(normalized);
        }

        public static CatalogoMarcaPerfil CriarPerfilMarca(string marca)
        {
            var brand = marca.Trim().ToUpperInvariant();
            var escaped = Regex.Escape(brand);

            // NGK/NTK: codigos de vela (BKR6E, LFR6AIX) raramente trazem o prefixo da marca.
            if (brand is "NGK" or "NTK")
            {
                return new CatalogoMarcaPerfil
                {
                    Marca = brand,
                    FonteCatalogoPadrao = $"Catalogo {brand}",
                    CodigoRegex = new Regex(
                        @"\b(?<codigo>(?:NGK|NTK)[\s-]*[A-Z0-9]{2,14}|[A-Z]{1,4}\d{1,2}[A-Z]{1,8}\d{0,2})\b",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                };
            }

            return new CatalogoMarcaPerfil
            {
                Marca = brand,
                FonteCatalogoPadrao = $"Catalogo {brand}",
                // Aceita "GF 7208", "SKF 6205-2RS", "6205-2RS" ou codigos numericos da marca.
                CodigoRegex = new Regex(
                    $@"\b(?<codigo>{escaped}[\s-]*[A-Z0-9.\-]{{2,18}}|\d{{3,5}}(?:[.\-/][A-Z0-9]{{1,6}}){{0,3}}|[A-Z]{{1,4}}\d{{3,5}}[A-Z0-9.\-]{{0,8}})\b",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            };
        }

        public static string DetectarMarcaPorArquivo(string caminhoArquivo, string? marcaInformada = null)
        {
            if (!string.IsNullOrWhiteSpace(marcaInformada) &&
                !string.Equals(marcaInformada.Trim(), "GERAL", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(marcaInformada.Trim(), "AUTO", StringComparison.OrdinalIgnoreCase))
            {
                return marcaInformada.Trim().ToUpperInvariant();
            }

            var fileName = Path.GetFileNameWithoutExtension(caminhoArquivo) ?? string.Empty;
            foreach (var marca in MarcasConhecidasArquivo)
            {
                if (ContemMarca(fileName, marca))
                {
                    return marca.ToUpperInvariant();
                }
            }

            // Ex.: CATÃLOGO_ROLAMENTOS / CATÃLOGO_PORTA_ESCOVAS â†’ marca do token significativo.
            var token = ExtrairTokenMarcaDoArquivo(fileName);
            return token;
        }

        public static string DetectarMarcaPorConteudoPdf(string caminhoArquivo, int paginasAmostra = 6)
        {
            try
            {
                using var document = PdfDocument.Open(caminhoArquivo);
                var amostra = string.Join(
                    " ",
                    document.GetPages()
                        .Take(paginasAmostra)
                        .Select(page => page.Text ?? string.Empty));

                if (amostra.Contains("uetaind.com.br", StringComparison.OrdinalIgnoreCase) ||
                    amostra.Contains("codigo ueta", StringComparison.OrdinalIgnoreCase) ||
                    PerfilUeta.CodigoRegex.Matches(amostra).Count >= 8)
                {
                    return "UETA";
                }

                if (amostra.Contains("dni.com.br", StringComparison.OrdinalIgnoreCase) ||
                    PerfilDni.CodigoRegex.Matches(amostra).Count >= 8)
                {
                    return "DNI";
                }

                foreach (var marca in MarcasConhecidasArquivo)
                {
                    if (marca is "DNI" or "UETA")
                    {
                        continue;
                    }

                    if (amostra.Contains(marca, StringComparison.OrdinalIgnoreCase))
                    {
                        var hits = CriarPerfilMarca(marca).CodigoRegex.Matches(amostra).Count;
                        if (hits >= 5)
                        {
                            return marca.ToUpperInvariant();
                        }
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        public static (string Marca, string Fonte) ResolverMarcaEFonte(
            string caminhoArquivo,
            string? marcaInformada,
            string? fonteInformada)
        {
            var marcaPorArquivo = DetectarMarcaPorArquivo(caminhoArquivo, marcaInformada);
            var marcaPorPdf = Path.GetExtension(caminhoArquivo).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? DetectarMarcaPorConteudoPdf(caminhoArquivo)
                : string.Empty;

            var marca = !string.IsNullOrWhiteSpace(marcaPorArquivo)
                ? marcaPorArquivo
                : !string.IsNullOrWhiteSpace(marcaPorPdf)
                    ? marcaPorPdf
                    : string.IsNullOrWhiteSpace(marcaInformada) ||
                      string.Equals(marcaInformada, "GERAL", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(marcaInformada, "AUTO", StringComparison.OrdinalIgnoreCase)
                        ? "GERAL"
                        : marcaInformada.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(fonteInformada))
            {
                return (marca, fonteInformada.Trim());
            }

            var perfil = ObterPerfil(marca);
            var fileName = Path.GetFileNameWithoutExtension(caminhoArquivo) ?? "catalogo";
            return (marca, string.Equals(perfil.FonteCatalogoPadrao, "Catalogo importado", StringComparison.OrdinalIgnoreCase)
                ? $"Catalogo importado de {fileName}"
                : perfil.FonteCatalogoPadrao);
        }

        public static CatalogoMarcaPerfil ResolverPerfilExtracao(string caminhoArquivo, string marca)
        {
            var marcaFinal = marca.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(marcaFinal) ||
                string.Equals(marcaFinal, "GERAL", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(marcaFinal, "AUTO", StringComparison.OrdinalIgnoreCase))
            {
                var detectada = DetectarMarcaPorArquivo(caminhoArquivo);
                if (string.IsNullOrWhiteSpace(detectada))
                {
                    detectada = DetectarMarcaPorConteudoPdf(caminhoArquivo);
                }

                if (!string.IsNullOrWhiteSpace(detectada))
                {
                    marcaFinal = detectada;
                }
                else
                {
                    marcaFinal = "GERAL";
                }
            }

            var perfil = ObterPerfil(marcaFinal);
            if (string.Equals(perfil.Marca, "GERAL", StringComparison.OrdinalIgnoreCase) &&
                Path.GetExtension(caminhoArquivo).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var dniCount = ContarCodigosNoPdf(caminhoArquivo, PerfilDni.CodigoRegex);
                var uetaCount = ContarCodigosNoPdf(caminhoArquivo, PerfilUeta.CodigoRegex);
                if (uetaCount > dniCount && uetaCount >= 5)
                {
                    return PerfilUeta;
                }

                if (dniCount >= 5)
                {
                    return PerfilDni;
                }
            }

            return perfil;
        }

        private static string ExtrairTokenMarcaDoArquivo(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            var normalized = fileName
                .Replace('\u00C1', 'A').Replace('\u00C0', 'A').Replace('\u00C3', 'A')
                .Replace('\u00C9', 'E').Replace('\u00CA', 'E')
                .Replace('\u00CD', 'I')
                .Replace('\u00D3', 'O').Replace('\u00D4', 'O').Replace('\u00D5', 'O')
                .Replace('\u00DA', 'U')
                .Replace('\u00C7', 'C')
                .ToUpperInvariant();

            var parts = Regex.Split(normalized, @"[^A-Z0-9]+")
                .Where(p => p.Length >= 3)
                .Where(p => !TokensArquivoIgnorados.Contains(p))
                .Where(p => !Regex.IsMatch(p, @"^\d+$"))
                .Where(p => !Regex.IsMatch(p, @"^\d{4}$")) // anos
                .ToList();

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            // Prefere token composto significativo (PORTAESCOVAS, ROLAMENTOS, REGULADORES...).
            var melhor = parts
                .OrderByDescending(p => p.Length)
                .First();

            return melhor.Length > 24 ? melhor[..24] : melhor;
        }

        private static int ContarCodigosNoPdf(string caminhoArquivo, Regex regex)
        {
            try
            {
                using var document = PdfDocument.Open(caminhoArquivo);
                var texto = string.Join(" ", document.GetPages().Take(12).Select(page => page.Text ?? string.Empty));
                return regex.Matches(texto).Count;
            }
            catch
            {
                return 0;
            }
        }

        private static bool ContemMarca(string value, string marca)
        {
            return value.Contains(marca, StringComparison.OrdinalIgnoreCase);
        }
    }
}

