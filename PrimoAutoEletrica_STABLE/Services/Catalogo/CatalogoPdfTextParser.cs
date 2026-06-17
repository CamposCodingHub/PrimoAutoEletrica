using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Services.Catalogo
{
    internal static class CatalogoPdfTextParser
    {
        private static readonly Regex UrlRegex = new(
            @"www\.[a-z0-9.-]+\.[a-z]{2,}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex PaginaSolturaRegex = new(
            @"^\d{1,4}(?=[A-Za-z])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex TituloPortuguesRegex = new(
            @"(?<titulo>(?:\b[A-ZÁÉÍÓÚÂÊÔÃÇ][A-Za-zÁÉÍÓÚÂÊÔÃÇçáéíóúâêôãç0-9/-]{2,}\b\s*){2,8})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex EspecificacaoRegex = new(
            @"(?<spec>(?:Especifica[cç][oõ]es?|Tens[aã]o|Pot[eê]ncia|Corrente|Terminais?|Med\.?|Aplica[cç][aã]o|Uso\s+Geral|Auxiliar)[:\s][^|]{4,120})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex InglesDuplicadoRegex = new(
            @"\b(Portable|Universal|Warning|Light|Switches|Relay|Specifications)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly string[] Ruídos =
        {
            "especificações",
            "especificacoes",
            "original code",
            "codigos originais",
            "código ueta",
            "codigo ueta",
            "indice por produto",
            "index by product"
        };

        public static (string Nome, string Descricao) ExtrairNomeDescricao(
            string linha,
            int indiceCodigo,
            int comprimentoCodigo,
            Regex codigoRegex)
        {
            if (string.IsNullOrWhiteSpace(linha))
            {
                return (string.Empty, string.Empty);
            }

            var trecho = ExtrairTrechoProximoAoCodigo(linha, indiceCodigo, comprimentoCodigo, codigoRegex);
            trecho = LimparRuído(trecho);
            if (string.IsNullOrWhiteSpace(trecho))
            {
                trecho = LimparRuído(RemoverCodigos(linha, codigoRegex));
            }

            if (string.IsNullOrWhiteSpace(trecho))
            {
                return (string.Empty, string.Empty);
            }

            var nome = ExtrairTitulo(trecho);
            var descricao = ExtrairDescricaoTecnica(trecho, nome);
            return (nome, descricao);
        }

        public static string GerarNomeFallback(string marca, string codigoFabricante)
        {
            var corpo = ExtrairCorpoCodigo(codigoFabricante);
            return $"Produto {marca.Trim().ToUpperInvariant()} {corpo}".Trim();
        }

        public static bool EhNomeFallback(string? nome, string marca, string codigoFabricante)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return true;
            }

            var fallback = GerarNomeFallback(marca, codigoFabricante);
            return string.Equals(nome.Trim(), fallback, StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtrairTrechoProximoAoCodigo(
            string linha,
            int indiceCodigo,
            int comprimentoCodigo,
            Regex codigoRegex)
        {
            var inicioDepois = indiceCodigo + comprimentoCodigo;
            var depois = inicioDepois < linha.Length
                ? linha[inicioDepois..]
                : string.Empty;
            var antes = indiceCodigo > 0
                ? linha[..indiceCodigo]
                : string.Empty;

            var depoisLimpo = CortarNoProximoCodigo(depois, codigoRegex);
            var antesLimpo = CortarNoProximoCodigo(antes, codigoRegex, reverso: true);

            var codigoNoFinal = indiceCodigo + comprimentoCodigo >= linha.Length * 0.55;
            var candidatos = codigoNoFinal
                ? new[] { antesLimpo, depoisLimpo }
                : new[] { depoisLimpo, antesLimpo };

            return candidatos
                .Select(DesaglutinarTexto)
                .Select(CatalogoCodeNormalizer.SanitizeFreeText)
                .Where(value => value.Length >= 8)
                .OrderByDescending(value => PontuarTrecho(value))
                .FirstOrDefault() ?? string.Empty;
        }

        private static string CortarNoProximoCodigo(string trecho, Regex codigoRegex, bool reverso = false)
        {
            if (string.IsNullOrWhiteSpace(trecho))
            {
                return string.Empty;
            }

            var match = codigoRegex.Match(trecho);
            if (!match.Success)
            {
                return trecho;
            }

            return reverso
                ? trecho[(match.Index + match.Length)..]
                : trecho[..match.Index];
        }

        private static string RemoverCodigos(string linha, Regex codigoRegex)
        {
            return CatalogoCodeNormalizer.SanitizeFreeText(codigoRegex.Replace(linha, " "));
        }

        private static string LimparRuído(string trecho)
        {
            if (string.IsNullOrWhiteSpace(trecho))
            {
                return string.Empty;
            }

            var value = trecho;
            value = UrlRegex.Replace(value, " ");
            value = PaginaSolturaRegex.Replace(value, string.Empty);
            value = Regex.Replace(value, @"\.{2,}", " ", RegexOptions.CultureInvariant);
            value = Regex.Replace(value, @"\s{2,}", " ", RegexOptions.CultureInvariant);

            foreach (var ruido in Ruídos)
            {
                value = Regex.Replace(
                    value,
                    Regex.Escape(ruido),
                    " ",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            value = InglesDuplicadoRegex.Replace(value, " ");
            return CatalogoCodeNormalizer.SanitizeFreeText(value);
        }

        private static string ExtrairTitulo(string trecho)
        {
            var matches = TituloPortuguesRegex.Matches(trecho);
            string? melhor = null;
            var melhorPontuacao = 0;

            foreach (Match match in matches)
            {
                var titulo = CatalogoCodeNormalizer.SanitizeFreeText(match.Groups["titulo"].Value);
                if (titulo.Length < 10 || titulo.Length > 120)
                {
                    continue;
                }

                if (Regex.IsMatch(titulo, @"\d{4,}", RegexOptions.CultureInvariant))
                {
                    continue;
                }

                var pontuacao = PontuarTrecho(titulo);
                if (pontuacao > melhorPontuacao)
                {
                    melhorPontuacao = pontuacao;
                    melhor = titulo;
                }
            }

            if (!string.IsNullOrWhiteSpace(melhor) && NomePareceValido(melhor))
            {
                return ToTitleCase(melhor);
            }

            var tokens = trecho
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Length >= 3 && !Regex.IsMatch(token, @"^\d+$"))
                .Take(8)
                .ToArray();

            if (tokens.Length < 2)
            {
                return string.Empty;
            }

            var fallback = string.Join(' ', tokens);
            return fallback.Length >= 10 && NomePareceValido(fallback) ? ToTitleCase(fallback) : string.Empty;
        }

        private static string DesaglutinarTexto(string trecho)
        {
            if (string.IsNullOrWhiteSpace(trecho))
            {
                return string.Empty;
            }

            var value = trecho;
            value = Regex.Replace(value, @"(?<=[a-záéíóúâêôãç])(?=[A-ZÁÉÍÓÚÂÊÔÃÇ])", " ", RegexOptions.CultureInvariant);
            value = Regex.Replace(value, @"(?<=[A-Za-zÁ-ú])(?=\d)", " ", RegexOptions.CultureInvariant);
            value = Regex.Replace(value, @"(?<=\d)(?=[A-Za-zÁ-ú])", " ", RegexOptions.CultureInvariant);
            return CatalogoCodeNormalizer.SanitizeFreeText(value);
        }

        private static bool NomePareceValido(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome) || nome.Length < 8 || nome.Length > 100)
            {
                return false;
            }

            if (nome.Contains("codigo", StringComparison.OrdinalIgnoreCase) ||
                nome.Contains("pagina", StringComparison.OrdinalIgnoreCase) ||
                nome.Contains("www.", StringComparison.OrdinalIgnoreCase) ||
                nome.Contains("indice", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (nome.Count(char.IsWhiteSpace) == 0 && nome.Length > 24)
            {
                return false;
            }

            var letras = nome.Count(char.IsLetter);
            return letras >= 8;
        }

        private static string ExtrairDescricaoTecnica(string trecho, string nome)
        {
            var specs = new List<string>();
            foreach (Match match in EspecificacaoRegex.Matches(trecho))
            {
                var spec = CatalogoCodeNormalizer.SanitizeFreeText(match.Groups["spec"].Value);
                if (spec.Length >= 8 && !spec.Contains(nome, StringComparison.OrdinalIgnoreCase))
                {
                    specs.Add(spec);
                }
            }

            if (specs.Count == 0)
            {
                var restante = trecho;
                if (!string.IsNullOrWhiteSpace(nome))
                {
                    restante = restante.Replace(nome, string.Empty, StringComparison.OrdinalIgnoreCase);
                }

                restante = CatalogoCodeNormalizer.SanitizeFreeText(restante);
                if (restante.Length >= 20 && restante.Length <= 400)
                {
                    return restante;
                }

                return string.Empty;
            }

            return string.Join(" | ", specs.Distinct(StringComparer.OrdinalIgnoreCase).Take(4));
        }

        private static int PontuarTrecho(string trecho)
        {
            if (string.IsNullOrWhiteSpace(trecho))
            {
                return 0;
            }

            var letras = trecho.Count(char.IsLetter);
            var espacos = trecho.Count(char.IsWhiteSpace);
            var digitos = trecho.Count(char.IsDigit);
            return letras + (espacos * 3) - (digitos * 2);
        }

        private static string ExtrairCorpoCodigo(string codigoFabricante)
        {
            if (string.IsNullOrWhiteSpace(codigoFabricante))
            {
                return string.Empty;
            }

            var parts = codigoFabricante.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 ? parts[1] : codigoFabricante;
        }

        private static string ToTitleCase(string value)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
        }
    }
}
