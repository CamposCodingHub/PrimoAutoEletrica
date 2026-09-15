using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Rendering.Skia;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public sealed class CatalogoPdfExtractorService
    {
        private static readonly Regex CategoryIndexRegex = new(
            @"^(?<categoria>[A-Z0-9 /-]{4,80}?)\s*(?:\.{2,}|\s{2,})\s*(?<pagina>\d{1,4})\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex PageListRegex = new(
            @"(?<paginas>\d{1,4}(?:\s*[,;]\s*\d{1,4}){0,8})\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex DniDigitsRegex = new(@"\d+", RegexOptions.Compiled);

        private static readonly HashSet<string> CodigosProibidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "HTTPS", "HTTP", "WWW", "PAGE", "PAGINA", "INDEX", "INDICE", "TOTAL", "GERAL",
            "CORES", "MATERIAL", "MEDIDAS", "VERSOES", "VERSOES", "LENTE", "ABAS"
        };

        private readonly LoggerService _logger;

        public CatalogoPdfExtractorService(LoggerService? logger = null)
        {
            _logger = logger ?? global::PrimoAutoEletrica.App.Logger;
        }

        public (List<CatalogoImportacaoPreviewItem> Itens, List<CatalogoImportacaoErro> Erros) Extrair(
            string caminhoArquivo,
            string fonteCatalogo,
            string marcaInformada)
        {
            var (marca, _) = CatalogoMarcaDetector.ResolverMarcaEFonte(caminhoArquivo, marcaInformada, fonteCatalogo);
            var perfil = CatalogoMarcaDetector.ResolverPerfilExtracao(caminhoArquivo, marca);
            marca = perfil.Marca;

            var itens = new Dictionary<string, CatalogoImportacaoPreviewItem>(StringComparer.OrdinalIgnoreCase);
            var erros = new List<CatalogoImportacaoErro>();
            var categoriasPorPagina = new SortedDictionary<int, string>();
            var pastaImagens = CatalogoWorkspacePaths.GetImagesDirectory(
                marca,
                Path.GetFileNameWithoutExtension(caminhoArquivo) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            using var document = PdfDocument.Open(caminhoArquivo, SkiaRenderingParsingOptions.Instance);
            foreach (var page in document.GetPages())
            {
                try
                {
                    var pageText = page.Text ?? string.Empty;
                    var lines = pageText
                        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToList();

                    if (lines.Count <= 3 && pageText.Length > 200)
                    {
                        lines = FatiarTextoPorCodigo(pageText, perfil.CodigoRegex);
                    }

                    IndexarCategorias(lines, perfil.CodigoRegex, categoriasPorPagina);

                    var imagensPagina = CatalogoPdfImageExtractor.ExtrairImagensDaPagina(
                        page,
                        pastaImagens,
                        SanitizeFileToken(marca));

                    var codigosComPosicao = LocalizarCodigosComPosicao(page, perfil.CodigoRegex, marca);

                    ProcessarLinhasPagina(
                        lines,
                        page.Number,
                        caminhoArquivo,
                        fonteCatalogo,
                        marca,
                        perfil.CodigoRegex,
                        categoriasPorPagina,
                        imagensPagina,
                        codigosComPosicao,
                        itens);
                }
                catch (Exception ex)
                {
                    erros.Add(new CatalogoImportacaoErro
                    {
                        LinhaOrigem = $"Pagina {page.Number}",
                        MensagemErro = $"Falha ao processar a pagina {page.Number}: {ex.Message}",
                        ConteudoOriginal = Path.GetFileName(caminhoArquivo),
                        DataErro = DateTime.Now
                    });

                    _logger.LogWarning($"Falha ao processar a pagina {page.Number} do catalogo PDF '{caminhoArquivo}': {ex.Message}");
                }
            }

            foreach (var item in itens.Values)
            {
                if (string.IsNullOrWhiteSpace(item.Categoria))
                {
                    item.Categoria = InferirCategoriaPorPagina(item.PaginaCatalogo, categoriasPorPagina);
                }

                AplicarFallbackNome(item, marca);
                item.Descricao = CatalogoCodeNormalizer.SanitizeFreeText(item.Descricao);
                item.StatusRevisao = "Pendente de revisao";
                item.MensagemValidacao = string.IsNullOrWhiteSpace(item.Descricao)
                    ? (string.IsNullOrWhiteSpace(item.ImagemLocal)
                        ? "Nome extraido do PDF; descricao pendente de revisao."
                        : "Nome e foto extraidos do PDF; descricao pendente de revisao.")
                    : (string.IsNullOrWhiteSpace(item.ImagemLocal)
                        ? "Nome e descricao extraidos do PDF; revisar antes de converter ao estoque."
                        : "Nome, descricao e foto extraidos do PDF; revisar antes de converter ao estoque.");
            }

            if (itens.Count == 0)
            {
                erros.Add(new CatalogoImportacaoErro
                {
                    LinhaOrigem = "PDF",
                    MensagemErro = $"Nenhum codigo {marca} foi detectado no PDF. Verifique a marca selecionada ou use CSV/Excel.",
                    ConteudoOriginal = Path.GetFileName(caminhoArquivo),
                    DataErro = DateTime.Now
                });
            }
            else
            {
                var comFoto = itens.Values.Count(i => !string.IsNullOrWhiteSpace(i.ImagemLocal));
                _logger.LogInfo($"Catalogo PDF: {itens.Count} item(ns); {comFoto} com imagem local em '{pastaImagens}'.");
            }

            return (itens.Values.OrderBy(item => item.CodigoNormalizado, StringComparer.OrdinalIgnoreCase).ToList(), erros);
        }

        private static List<string> FatiarTextoPorCodigo(string pageText, Regex codigoRegex)
        {
            var parts = new List<string>();
            var matches = codigoRegex.Matches(pageText);
            if (matches.Count == 0)
            {
                return new List<string> { pageText };
            }

            for (var i = 0; i < matches.Count; i++)
            {
                var start = matches[i].Index;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : pageText.Length;
                var slice = pageText[start..end].Trim();
                if (!string.IsNullOrWhiteSpace(slice))
                {
                    parts.Add(slice);
                }
            }

            return parts;
        }

        private static Dictionary<string, (double Y, double X)> LocalizarCodigosComPosicao(
            Page page,
            Regex codigoRegex,
            string marca)
        {
            var mapa = new Dictionary<string, (double Y, double X)>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var word in page.GetWords())
                {
                    var texto = word.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(texto))
                    {
                        continue;
                    }

                    var match = codigoRegex.Match(texto);
                    if (!match.Success)
                    {
                        continue;
                    }

                    var y = word.BoundingBox.Bottom + (word.BoundingBox.Height / 2.0);
                    var x = word.BoundingBox.Left + (word.BoundingBox.Width / 2.0);
                    foreach (var codigo in ExpandirCodigosDetectados(match.Groups["codigo"].Value, marca))
                    {
                        if (!mapa.ContainsKey(codigo))
                        {
                            mapa[codigo] = (y, x);
                        }
                    }
                }
            }
            catch
            {
            }

            return mapa;
        }

        private static IEnumerable<string> ExpandirCodigosDetectados(string raw, string marca)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                yield break;
            }

            if (string.Equals(marca, "DNI", StringComparison.OrdinalIgnoreCase))
            {
                var digitsOnly = string.Concat(DniDigitsRegex.Matches(raw).Select(m => m.Value));
                if (Regex.IsMatch(raw, @"DNI(?:\s*DNI)+", RegexOptions.IgnoreCase) && digitsOnly.Length >= 8)
                {
                    for (var i = 0; i + 4 <= digitsOnly.Length; i += 4)
                    {
                        yield return $"DNI {digitsOnly.Substring(i, 4)}";
                    }

                    yield break;
                }

                // Indices: "DNI 081438" = codigo 0814 + pagina 38 colada (prefere 4 digitos).
                var single = Regex.Match(raw, @"DNI[\s-]*(?<n>\d{4}|\d{3})(?:-[A-Z]{1,4})?", RegexOptions.IgnoreCase);
                if (single.Success)
                {
                    yield return $"DNI {single.Groups["n"].Value}";
                    yield break;
                }
            }

            yield return raw;
        }

        private static void IndexarCategorias(
            IEnumerable<string> lines,
            Regex codigoRegex,
            IDictionary<int, string> categoriasPorPagina)
        {
            foreach (var line in lines)
            {
                var lineWithoutCode = codigoRegex.Replace(line, string.Empty);
                if (lineWithoutCode.Length != line.Length)
                {
                    continue;
                }

                var match = CategoryIndexRegex.Match(lineWithoutCode.ToUpperInvariant());
                if (!match.Success || !int.TryParse(match.Groups["pagina"].Value, out var pagina))
                {
                    continue;
                }

                var categoria = match.Groups["categoria"].Value.Trim(' ', '.', '-', ':');
                if (categoria.Length < 4 || categoria.Length > 80)
                {
                    continue;
                }

                categoriasPorPagina[pagina] = ToTitleCase(categoria);
            }
        }

        private static void ProcessarLinhasPagina(
            IEnumerable<string> lines,
            int paginaAtual,
            string caminhoArquivo,
            string fonteCatalogo,
            string marca,
            Regex codigoRegex,
            IReadOnlyDictionary<int, string> categoriasPorPagina,
            IReadOnlyList<CatalogoPdfImageExtractor.ImagemExtraida> imagensPagina,
            IReadOnlyDictionary<string, (double Y, double X)> codigosComPosicao,
            IDictionary<string, CatalogoImportacaoPreviewItem> itens)
        {
            foreach (var line in lines)
            {
                var matches = codigoRegex.Matches(line);
                if (matches.Count == 0)
                {
                    continue;
                }

                var paginasAssociadas = ExtrairPaginasDaLinha(line, paginaAtual);
                foreach (Match match in matches)
                {
                    foreach (var codigoBruto in ExpandirCodigosDetectados(match.Groups["codigo"].Value, marca))
                    {
                        var codigoOriginal = CatalogoCodeNormalizer.FormatManufacturerCode(codigoBruto, marca);
                        var codigoNormalizado = CatalogoCodeNormalizer.NormalizeCode(codigoOriginal, marca);
                        if (string.IsNullOrWhiteSpace(codigoNormalizado) || !CodigoPareceValido(codigoOriginal, marca))
                        {
                            continue;
                        }

                        var (nomeExtraido, descricaoExtraida) = CatalogoPdfTextParser.ExtrairNomeDescricao(
                            line,
                            match.Index,
                            match.Length,
                            codigoRegex);

                        double? yCodigo = null;
                        double? xCodigo = null;
                        if (codigosComPosicao.TryGetValue(codigoBruto, out var pos) ||
                            codigosComPosicao.TryGetValue(codigoOriginal, out pos))
                        {
                            yCodigo = pos.Y;
                            xCodigo = pos.X;
                        }

                        var imagemLocal = CatalogoPdfImageExtractor.AssociarImagemMaisProxima(
                            imagensPagina,
                            yCodigo,
                            xCodigo);

                        var chave = $"{codigoNormalizado}|{marca}|{fonteCatalogo}";
                        if (!itens.TryGetValue(chave, out var item))
                        {
                            item = new CatalogoImportacaoPreviewItem
                            {
                                CodigoFabricante = codigoOriginal,
                                CodigoNormalizado = codigoNormalizado,
                                Marca = marca,
                                FonteCatalogo = fonteCatalogo,
                                ArquivoOrigem = Path.GetFileName(caminhoArquivo),
                                Categoria = InferirCategoria(paginasAssociadas, categoriasPorPagina),
                                PaginaCatalogo = string.Join(", ", paginasAssociadas),
                                ConteudoOriginal = line,
                                Nome = nomeExtraido,
                                Descricao = descricaoExtraida,
                                ImagemLocal = imagemLocal ?? string.Empty,
                                Linha = $"Pagina {paginaAtual}",
                                StatusRevisao = "Pendente de revisao"
                            };
                            itens[chave] = item;
                        }
                        else
                        {
                            item.PaginaCatalogo = MesclarPaginas(item.PaginaCatalogo, paginasAssociadas);
                            if (string.IsNullOrWhiteSpace(item.Categoria))
                            {
                                item.Categoria = InferirCategoria(paginasAssociadas, categoriasPorPagina);
                            }

                            if (string.IsNullOrWhiteSpace(item.ImagemLocal) && !string.IsNullOrWhiteSpace(imagemLocal))
                            {
                                item.ImagemLocal = imagemLocal;
                            }

                            MesclarTextoExtraido(item, nomeExtraido, descricaoExtraida);
                        }
                    }
                }
            }
        }

        private static void MesclarTextoExtraido(
            CatalogoImportacaoPreviewItem item,
            string nomeExtraido,
            string descricaoExtraida)
        {
            if (string.IsNullOrWhiteSpace(item.Nome) && !string.IsNullOrWhiteSpace(nomeExtraido))
            {
                item.Nome = nomeExtraido;
            }
            else if (!string.IsNullOrWhiteSpace(nomeExtraido) &&
                     CatalogoPdfTextParser.EhNomeFallback(item.Nome, item.Marca, item.CodigoFabricante) &&
                     !CatalogoPdfTextParser.EhNomeFallback(nomeExtraido, item.Marca, item.CodigoFabricante))
            {
                item.Nome = nomeExtraido;
            }

            if (string.IsNullOrWhiteSpace(item.Descricao))
            {
                item.Descricao = descricaoExtraida;
            }
            else if (!string.IsNullOrWhiteSpace(descricaoExtraida) &&
                     !item.Descricao.Contains(descricaoExtraida, StringComparison.OrdinalIgnoreCase))
            {
                item.Descricao = $"{item.Descricao} | {descricaoExtraida}";
            }
        }

        private static void AplicarFallbackNome(CatalogoImportacaoPreviewItem item, string marca)
        {
            if (string.IsNullOrWhiteSpace(item.Nome))
            {
                item.Nome = CatalogoPdfTextParser.GerarNomeFallback(marca, item.CodigoFabricante);
            }
        }

        private static bool CodigoPareceValido(string codigoOriginal, string marca)
        {
            if (string.IsNullOrWhiteSpace(codigoOriginal))
            {
                return false;
            }

            if (string.Equals(marca, "UETA", StringComparison.OrdinalIgnoreCase))
            {
                return codigoOriginal.Contains("U-", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(marca, "DNI", StringComparison.OrdinalIgnoreCase))
            {
                if (!codigoOriginal.Contains("DNI", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var digits = string.Concat(codigoOriginal.Where(char.IsDigit));
                return digits.Length is 3 or 4;
            }

            if (!codigoOriginal.Any(char.IsDigit))
            {
                return false;
            }

            var tokens = codigoOriginal.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Any(token => CodigosProibidos.Contains(token)))
            {
                return false;
            }

            var corpo = tokens.LastOrDefault() ?? codigoOriginal;
            return corpo.Length >= 2;
        }

        private static List<int> ExtrairPaginasDaLinha(string line, int paginaAtual)
        {
            var pages = new List<int>();
            var match = PageListRegex.Match(line);
            if (match.Success)
            {
                pages.AddRange(match.Groups["paginas"].Value
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Trim())
                    .Where(value => int.TryParse(value, out _))
                    .Select(int.Parse));
            }

            if (pages.Count == 0)
            {
                pages.Add(paginaAtual);
            }

            return pages.Distinct().OrderBy(value => value).ToList();
        }

        private static string MesclarPaginas(string paginaAtual, IEnumerable<int> novasPaginas)
        {
            var paginas = new HashSet<int>();
            foreach (var token in (paginaAtual ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token.Trim(), out var value))
                {
                    paginas.Add(value);
                }
            }

            foreach (var page in novasPaginas)
            {
                paginas.Add(page);
            }

            return string.Join(", ", paginas.OrderBy(value => value));
        }

        private static string InferirCategoria(IEnumerable<int> paginas, IReadOnlyDictionary<int, string> categoriasPorPagina)
        {
            foreach (var page in paginas.OrderBy(value => value))
            {
                var categoria = InferirCategoriaPorPagina(page.ToString(), categoriasPorPagina);
                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    return categoria;
                }
            }

            return string.Empty;
        }

        private static string InferirCategoriaPorPagina(string paginaCatalogo, IReadOnlyDictionary<int, string> categoriasPorPagina)
        {
            if (categoriasPorPagina.Count == 0)
            {
                return string.Empty;
            }

            var page = paginaCatalogo
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim())
                .Select(token => int.TryParse(token, out var value) ? value : 0)
                .FirstOrDefault(value => value > 0);

            if (page <= 0)
            {
                return string.Empty;
            }

            var candidate = categoriasPorPagina
                .Where(entry => entry.Key <= page)
                .OrderBy(entry => entry.Key)
                .LastOrDefault();

            return candidate.Equals(default(KeyValuePair<int, string>)) ? string.Empty : candidate.Value;
        }

        private static string ToTitleCase(string value)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
        }

        private static string SanitizeFileToken(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "cat" : value.Trim();
        }
    }
}
