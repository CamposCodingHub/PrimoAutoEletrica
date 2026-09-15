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
            CodigoRegex = new Regex(
                @"\b(?<codigo>DNI[\s-]*\d{3,6}(?:-[A-Z0-9]{1,4})?)\b",
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
        /// Perfil generico: exige digito no codigo para evitar capturar palavras do PDF (ABAS, LENTE...).
        /// </summary>
        private static readonly CatalogoMarcaPerfil PerfilGenerico = new()
        {
            Marca = "GERAL",
            FonteCatalogoPadrao = "Catalogo importado",
            CodigoRegex = new Regex(
                @"\b(?<codigo>(?:[A-Z]{2,6})[\s-]*\d[A-Z0-9]{2,11}(?:-[A-Z0-9]{1,6})?)\b",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        };

        private static readonly IReadOnlyList<CatalogoMarcaPerfil> PerfisOrdenados = new[]
        {
            PerfilDni,
            PerfilUeta
        };

        private static readonly HashSet<string> MarcasConhecidasArquivo = new(StringComparer.OrdinalIgnoreCase)
        {
            "DNI", "UETA", "GF", "BOSCH", "NGK", "HELLA", "VALEO", "PHILIPS", "OSRAM", "MAGNETI", "MARELLI", "DELPHI", "FACET"
        };

        public static IReadOnlyList<CatalogoMarcaPerfil> ObterPerfis() =>
            PerfisOrdenados.Concat(new[] { PerfilGenerico }).ToList();

        public static CatalogoMarcaPerfil ObterPerfil(string? marca)
        {
            if (string.IsNullOrWhiteSpace(marca))
            {
                return PerfilDni;
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

            // Preserva a marca informada (ex.: GF) em vez de sobrescrever para GERAL.
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
                // Aceita "GF 7208", "GF-7208" ou codigos numericos de produto na mesma marca.
                CodigoRegex = new Regex(
                    $@"\b(?<codigo>{escaped}[\s-]*\d[A-Z0-9.]{{1,14}}(?:-[A-Z0-9]{{1,6}})?|\d{{3,6}}(?:\.\d{{1,4}}){{0,3}})\b",
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

            return string.Empty;
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
                    : string.IsNullOrWhiteSpace(marcaInformada) || string.Equals(marcaInformada, "GERAL", StringComparison.OrdinalIgnoreCase)
                        ? "DNI"
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
                else if (string.Equals(marcaFinal, "GERAL", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(marcaFinal, "AUTO", StringComparison.OrdinalIgnoreCase))
                {
                    marcaFinal = "DNI";
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
