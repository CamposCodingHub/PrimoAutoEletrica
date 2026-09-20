using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

        private static readonly CatalogoMarcaPerfil PerfilGenerico = new()
        {
            Marca = "GERAL",
            FonteCatalogoPadrao = "Catalogo importado",
            CodigoRegex = new Regex(
                @"\b(?<codigo>(?:[A-Z]{2,6})[\s-]*[A-Z0-9]{3,12}(?:-[A-Z0-9]{1,6})?)\b",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        };

        private static readonly IReadOnlyList<CatalogoMarcaPerfil> PerfisOrdenados = new[]
        {
            PerfilDni,
            PerfilUeta,
            PerfilGenerico
        };

        public static IReadOnlyList<CatalogoMarcaPerfil> ObterPerfis() => PerfisOrdenados;

        public static CatalogoMarcaPerfil ObterPerfil(string? marca)
        {
            if (string.IsNullOrWhiteSpace(marca))
            {
                return PerfilDni;
            }

            var normalized = marca.Trim().ToUpperInvariant();
            return PerfisOrdenados.FirstOrDefault(perfil =>
                       string.Equals(perfil.Marca, normalized, StringComparison.OrdinalIgnoreCase))
                   ?? PerfilGenerico;
        }

        public static string DetectarMarcaPorArquivo(string caminhoArquivo, string? marcaInformada = null)
        {
            var fileName = Path.GetFileNameWithoutExtension(caminhoArquivo) ?? string.Empty;
            var marcaDoNome = DetectarMarcaNoTexto(fileName);
            if (!string.IsNullOrWhiteSpace(marcaDoNome))
            {
                return marcaDoNome;
            }

            return string.Empty;
        }

        public static string DetectarMarcaPorConteudoPdf(string caminhoArquivo, int paginasAmostra = 6)
        {
            try
            {
                using var document = CatalogoPdfDocumentOpener.Open(caminhoArquivo);
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

                var marcaNoTexto = DetectarMarcaNoTexto(amostra);
                if (!string.IsNullOrWhiteSpace(marcaNoTexto))
                {
                    return marcaNoTexto;
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
                    : MarcaInformadaDeveSerRespeitada(marcaInformada)
                        ? marcaInformada!.Trim().ToUpperInvariant()
                        : "GERAL";

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
                else if (string.Equals(marcaFinal, "AUTO", StringComparison.OrdinalIgnoreCase))
                {
                    marcaFinal = "GERAL";
                }
            }

            var perfil = ObterPerfil(marcaFinal);
            if (perfil.Marca == "GERAL" &&
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
                using var document = CatalogoPdfDocumentOpener.Open(caminhoArquivo);
                var texto = string.Join(" ", document.GetPages().Take(12).Select(page => page.Text ?? string.Empty));
                return regex.Matches(texto).Count;
            }
            catch
            {
                return 0;
            }
        }

        private static bool MarcaInformadaDeveSerRespeitada(string? marcaInformada)
        {
            if (string.IsNullOrWhiteSpace(marcaInformada))
            {
                return false;
            }

            var normalized = marcaInformada.Trim();
            return !string.Equals(normalized, "GERAL", StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(normalized, "AUTO", StringComparison.OrdinalIgnoreCase);
        }

        private static string DetectarMarcaNoTexto(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (Regex.IsMatch(value, @"\bUETA\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return "UETA";
            }

            if (Regex.IsMatch(value, @"\bDNI\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return "DNI";
            }

            if (Regex.IsMatch(value, @"\bSKF\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return "SKF";
            }

            if (Regex.IsMatch(value, @"\bBOSCH\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return "BOSCH";
            }

            if (Regex.IsMatch(value, @"\bGF\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return "GF";
            }

            return string.Empty;
        }
    }
}
