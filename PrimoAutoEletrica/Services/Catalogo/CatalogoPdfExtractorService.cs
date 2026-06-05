using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

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

            using var document = PdfDocument.Open(caminhoArquivo);
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

                    IndexarCategorias(lines, perfil.CodigoRegex, categoriasPorPagina);
                    ProcessarLinhasPagina(
                        lines,
                        page.Number,
                        caminhoArquivo,
                        fonteCatalogo,
                        marca,
                        perfil.CodigoRegex,
                        categoriasPorPagina,
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
                    ? "Nome extraido do PDF; descricao pendente de revisao."
                    : "Nome e descricao extraidos do PDF; revisar antes de converter ao estoque.";
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

            return (itens.Values.OrderBy(item => item.CodigoNormalizado, StringComparer.OrdinalIgnoreCase).ToList(), erros);
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
                    var codigoOriginal = CatalogoCodeNormalizer.FormatManufacturerCode(match.Groups["codigo"].Value, marca);
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

                        MesclarTextoExtraido(item, nomeExtraido, descricaoExtraida);
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
            if (string.Equals(marca, "UETA", StringComparison.OrdinalIgnoreCase))
            {
                return codigoOriginal.Contains("U-", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(marca, "DNI", StringComparison.OrdinalIgnoreCase))
            {
                return codigoOriginal.Contains("DNI", StringComparison.OrdinalIgnoreCase);
            }

            return true;
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
    }
}
