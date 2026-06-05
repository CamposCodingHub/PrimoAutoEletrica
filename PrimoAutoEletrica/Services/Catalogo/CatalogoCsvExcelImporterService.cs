using ExcelDataReader;
using Microsoft.VisualBasic.FileIO;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public sealed class CatalogoCsvExcelImporterService
    {
        private readonly LoggerService _logger;

        public CatalogoCsvExcelImporterService(LoggerService? logger = null)
        {
            _logger = logger ?? global::PrimoAutoEletrica.App.Logger;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public (List<CatalogoImportacaoPreviewItem> Itens, List<CatalogoImportacaoErro> Erros) ImportarCsv(
            string caminhoArquivo,
            string fonteCatalogo,
            string marca)
        {
            var itens = new List<CatalogoImportacaoPreviewItem>();
            var erros = new List<CatalogoImportacaoErro>();

            using var parser = new TextFieldParser(caminhoArquivo, Encoding.UTF8)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true
            };

            parser.SetDelimiters(";", ",", "\t");
            string[]? headers = null;
            var linhaAtual = 0;

            while (!parser.EndOfData)
            {
                linhaAtual++;
                string[]? fields = null;

                try
                {
                    fields = parser.ReadFields();
                }
                catch (Exception ex)
                {
                    erros.Add(CriarErro(linhaAtual.ToString(CultureInfo.InvariantCulture), string.Empty, $"Falha ao ler a linha CSV: {ex.Message}", string.Empty));
                    continue;
                }

                if (fields == null || fields.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (headers == null)
                {
                    headers = fields;
                    continue;
                }

                try
                {
                    var item = MapearLinha(headers, fields, fonteCatalogo, marca, caminhoArquivo, linhaAtual);
                    itens.Add(item);
                }
                catch (Exception ex)
                {
                    erros.Add(CriarErro(linhaAtual.ToString(CultureInfo.InvariantCulture), string.Empty, ex.Message, string.Join(" | ", fields)));
                }
            }

            return (itens, erros);
        }

        public (List<CatalogoImportacaoPreviewItem> Itens, List<CatalogoImportacaoErro> Erros) ImportarExcel(
            string caminhoArquivo,
            string fonteCatalogo,
            string marca)
        {
            var itens = new List<CatalogoImportacaoPreviewItem>();
            var erros = new List<CatalogoImportacaoErro>();

            using var stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            string[]? headers = null;
            var linhaAtual = 0;

            do
            {
                while (reader.Read())
                {
                    linhaAtual++;
                    var fields = Enumerable.Range(0, reader.FieldCount)
                        .Select(index => Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty)
                        .ToArray();

                    if (fields.All(string.IsNullOrWhiteSpace))
                    {
                        continue;
                    }

                    if (headers == null)
                    {
                        headers = fields;
                        continue;
                    }

                    try
                    {
                        var item = MapearLinha(headers, fields, fonteCatalogo, marca, caminhoArquivo, linhaAtual);
                        itens.Add(item);
                    }
                    catch (Exception ex)
                    {
                        erros.Add(CriarErro(linhaAtual.ToString(CultureInfo.InvariantCulture), string.Empty, ex.Message, string.Join(" | ", fields)));
                    }
                }
            }
            while (reader.NextResult());

            return (itens, erros);
        }

        private static CatalogoImportacaoPreviewItem MapearLinha(
            IReadOnlyList<string> headers,
            IReadOnlyList<string> values,
            string fonteCatalogo,
            string marcaPadrao,
            string caminhoArquivo,
            int linhaAtual)
        {
            var row = ConstruirMapaLinha(headers, values);
            var codigo = FirstNonEmpty(row,
                "codigofabricante",
                "codigo",
                "codigodoproduto",
                "partnumber",
                "sku");

            var marca = FirstNonEmpty(row, "marca", "fabricante");
            var nome = FirstNonEmpty(row, "nome", "produto", "item");
            var descricao = FirstNonEmpty(row, "descricao", "descricaodetalhada", "detalhes");
            var categoria = FirstNonEmpty(row, "categoria", "grupo");
            var subcategoria = FirstNonEmpty(row, "subcategoria", "subgrupo");
            var aplicacao = FirstNonEmpty(row, "aplicacao", "aplicacoes");
            var pagina = FirstNonEmpty(row, "pagina", "paginacatalogo");
            var voltagem = FirstNonEmpty(row, "voltagem", "tensao");
            var amperagem = FirstNonEmpty(row, "amperagem", "corrente");
            var observacoes = FirstNonEmpty(row, "observacoes", "obs", "notas");
            var precoReferencia = FirstNonEmpty(row, "precoreferencia", "preco", "valor");

            if (!string.IsNullOrWhiteSpace(precoReferencia))
            {
                observacoes = string.IsNullOrWhiteSpace(observacoes)
                    ? $"Preco referencia: {precoReferencia}"
                    : $"{observacoes} | Preco referencia: {precoReferencia}";
            }

            var marcaFinal = string.IsNullOrWhiteSpace(marca) ? marcaPadrao : marca;
            var codigoFormatado = CatalogoCodeNormalizer.FormatManufacturerCode(codigo, marcaFinal);
            var codigoNormalizado = CatalogoCodeNormalizer.NormalizeCode(codigoFormatado, marcaFinal);

            return new CatalogoImportacaoPreviewItem
            {
                CodigoFabricante = codigoFormatado,
                CodigoNormalizado = codigoNormalizado,
                Marca = CatalogoCodeNormalizer.SanitizeFreeText(marcaFinal).ToUpperInvariant(),
                Nome = CatalogoCodeNormalizer.SanitizeFreeText(nome),
                Descricao = CatalogoCodeNormalizer.SanitizeFreeText(descricao),
                Categoria = CatalogoCodeNormalizer.SanitizeFreeText(categoria),
                Subcategoria = CatalogoCodeNormalizer.SanitizeFreeText(subcategoria),
                Aplicacao = CatalogoCodeNormalizer.SanitizeFreeText(aplicacao),
                PaginaCatalogo = CatalogoCodeNormalizer.SanitizeFreeText(pagina),
                FonteCatalogo = CatalogoCodeNormalizer.SanitizeFreeText(fonteCatalogo),
                ArquivoOrigem = Path.GetFileName(caminhoArquivo),
                Voltagem = CatalogoCodeNormalizer.SanitizeFreeText(voltagem),
                Amperagem = CatalogoCodeNormalizer.SanitizeFreeText(amperagem),
                ObservacoesTecnicas = CatalogoCodeNormalizer.SanitizeFreeText(observacoes),
                StatusRevisao = "Novo",
                ConteudoOriginal = string.Join(" | ", values),
                Linha = linhaAtual.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static Dictionary<string, string> ConstruirMapaLinha(IReadOnlyList<string> headers, IReadOnlyList<string> values)
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Count; index++)
            {
                var key = CatalogoCodeNormalizer.NormalizeHeader(headers[index]);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var value = index < values.Count ? values[index] : string.Empty;
                row[key] = value ?? string.Empty;
            }

            return row;
        }

        private static string FirstNonEmpty(IReadOnlyDictionary<string, string> row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static CatalogoImportacaoErro CriarErro(string linhaOrigem, string codigoDetectado, string mensagemErro, string conteudoOriginal)
        {
            return new CatalogoImportacaoErro
            {
                LinhaOrigem = linhaOrigem,
                CodigoDetectado = codigoDetectado,
                MensagemErro = mensagemErro,
                ConteudoOriginal = conteudoOriginal,
                DataErro = DateTime.Now
            };
        }
    }
}
