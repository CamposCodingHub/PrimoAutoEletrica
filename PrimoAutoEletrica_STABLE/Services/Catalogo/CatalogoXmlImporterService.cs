using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public sealed class CatalogoXmlImporterService
    {
        public (List<CatalogoImportacaoPreviewItem> Itens, List<CatalogoImportacaoErro> Erros) Importar(
            string caminhoArquivo,
            string fonteCatalogo,
            string marcaPadrao)
        {
            var itens = new List<CatalogoImportacaoPreviewItem>();
            var erros = new List<CatalogoImportacaoErro>();
            var document = SecureXmlLoader.Load(caminhoArquivo, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var nodes = document
                .Descendants()
                .Where(element => element.Elements().Any())
                .Where(element =>
                {
                    var childNames = element.Elements()
                        .Select(child => CatalogoCodeNormalizer.NormalizeHeader(child.Name.LocalName))
                        .ToList();

                    return childNames.Contains("codigo")
                        || childNames.Contains("codigofabricante")
                        || childNames.Contains("partnumber")
                        || childNames.Contains("sku");
                })
                .ToList();

            if (nodes.Count == 0)
            {
                erros.Add(new CatalogoImportacaoErro
                {
                    LinhaOrigem = "XML",
                    MensagemErro = "Nao foi encontrada uma estrutura de itens reconhecivel no XML.",
                    ConteudoOriginal = Path.GetFileName(caminhoArquivo)
                });

                return (itens, erros);
            }

            foreach (var node in nodes)
            {
                try
                {
                    var map = node.Elements()
                        .GroupBy(element => CatalogoCodeNormalizer.NormalizeHeader(element.Name.LocalName))
                        .ToDictionary(group => group.Key, group => string.Join(" | ", group.Select(element => element.Value.Trim())), StringComparer.OrdinalIgnoreCase);

                    var codigo = FirstNonEmpty(map, "codigofabricante", "codigo", "partnumber", "sku");
                    var marca = FirstNonEmpty(map, "marca", "fabricante");
                    var nome = FirstNonEmpty(map, "nome", "produto", "item");
                    var descricao = FirstNonEmpty(map, "descricao", "detalhes");
                    var categoria = FirstNonEmpty(map, "categoria", "grupo");
                    var subcategoria = FirstNonEmpty(map, "subcategoria", "subgrupo");
                    var aplicacao = FirstNonEmpty(map, "aplicacao", "veiculo", "veiculoaplicacao");

                    var codigoFormatado = CatalogoCodeNormalizer.FormatManufacturerCode(codigo, string.IsNullOrWhiteSpace(marca) ? marcaPadrao : marca);
                    var marcaFinal = string.IsNullOrWhiteSpace(marca) ? marcaPadrao : marca;

                    itens.Add(new CatalogoImportacaoPreviewItem
                    {
                        CodigoFabricante = codigoFormatado,
                        CodigoNormalizado = CatalogoCodeNormalizer.NormalizeCode(codigoFormatado, marcaFinal),
                        Marca = CatalogoCodeNormalizer.SanitizeFreeText(marcaFinal).ToUpperInvariant(),
                        Nome = CatalogoCodeNormalizer.SanitizeFreeText(nome),
                        Descricao = CatalogoCodeNormalizer.SanitizeFreeText(descricao),
                        Categoria = CatalogoCodeNormalizer.SanitizeFreeText(categoria),
                        Subcategoria = CatalogoCodeNormalizer.SanitizeFreeText(subcategoria),
                        Aplicacao = CatalogoCodeNormalizer.SanitizeFreeText(aplicacao),
                        FonteCatalogo = CatalogoCodeNormalizer.SanitizeFreeText(fonteCatalogo),
                        ArquivoOrigem = Path.GetFileName(caminhoArquivo),
                        Voltagem = FirstNonEmpty(map, "voltagem", "tensao"),
                        Amperagem = FirstNonEmpty(map, "amperagem", "corrente"),
                        QuantidadeTerminais = FirstNonEmpty(map, "terminais", "quantidadeterminais"),
                        PaginaCatalogo = FirstNonEmpty(map, "pagina", "paginacatalogo"),
                        ObservacoesTecnicas = FirstNonEmpty(map, "observacoes", "obs", "notas"),
                        StatusRevisao = "Novo",
                        ConteudoOriginal = node.ToString(SaveOptions.DisableFormatting),
                        Linha = (node as IXmlLineInfo)?.LineNumber.ToString() ?? string.Empty
                    });
                }
                catch (Exception ex)
                {
                    erros.Add(new CatalogoImportacaoErro
                    {
                        LinhaOrigem = (node as IXmlLineInfo)?.LineNumber.ToString() ?? "XML",
                        MensagemErro = $"Falha ao interpretar item XML: {ex.Message}",
                        ConteudoOriginal = node.ToString(SaveOptions.DisableFormatting)
                    });
                }
            }

            return (itens, erros);
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
    }
}
