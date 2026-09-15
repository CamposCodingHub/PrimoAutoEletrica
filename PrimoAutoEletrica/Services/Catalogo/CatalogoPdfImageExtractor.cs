using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.XObjects;

namespace PrimoAutoEletrica.Services.Catalogo
{
    /// <summary>
    /// Extrai imagens embutidas de paginas PDF (quando o filtro PdfPig consegue decodificar).
    /// </summary>
    internal static class CatalogoPdfImageExtractor
    {
        public sealed record ImagemExtraida(double CentroY, string CaminhoLocal, int Largura, int Altura);

        public static List<ImagemExtraida> ExtrairImagensDaPagina(
            Page page,
            string pastaDestino,
            string prefixoArquivo)
        {
            var imagens = new List<ImagemExtraida>();
            Directory.CreateDirectory(pastaDestino);

            var index = 0;
            foreach (var image in page.GetImages())
            {
                try
                {
                    if (image.WidthInSamples < 48 || image.HeightInSamples < 48)
                    {
                        continue;
                    }

                    if (!TryObterBytes(image, out var bytes, out var extensao) ||
                        bytes == null ||
                        bytes.Length < 800)
                    {
                        continue;
                    }

                    var nome = $"{prefixoArquivo}_p{page.Number}_{index:D3}.{extensao}";
                    var caminho = Path.Combine(pastaDestino, nome);
                    File.WriteAllBytes(caminho, bytes);

                    var centroY = image.Bounds.Bottom + (image.Bounds.Height / 2.0);
                    imagens.Add(new ImagemExtraida(centroY, caminho, image.WidthInSamples, image.HeightInSamples));
                    index++;
                }
                catch
                {
                    // Alguns filtros (JPX/JBIG2/DCT) podem falhar — segue sem travar a importacao.
                }
            }

            return imagens
                .OrderByDescending(img => img.CentroY)
                .ThenByDescending(img => img.Largura * img.Altura)
                .ToList();
        }

        public static string? AssociarImagemMaisProxima(
            IReadOnlyList<ImagemExtraida> imagens,
            double? codigoCentroY)
        {
            if (imagens == null || imagens.Count == 0)
            {
                return null;
            }

            if (!codigoCentroY.HasValue)
            {
                // Sem posicao do codigo: usa a maior imagem da pagina.
                return imagens
                    .OrderByDescending(img => img.Largura * img.Altura)
                    .First()
                    .CaminhoLocal;
            }

            return imagens
                .OrderBy(img => Math.Abs(img.CentroY - codigoCentroY.Value))
                .ThenByDescending(img => img.Largura * img.Altura)
                .First()
                .CaminhoLocal;
        }

        private static bool TryObterBytes(IPdfImage image, out byte[]? bytes, out string extensao)
        {
            bytes = null;
            extensao = "png";

            try
            {
                if (image.TryGetPng(out var png) && png is { Length: > 0 })
                {
                    bytes = png;
                    extensao = "png";
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                // PdfPig 0.1.x: RawBytes pode ser JPEG (DCT) pronto para gravar.
                var rawSpan = image.RawBytes;
                if (rawSpan.Length > 0)
                {
                    var raw = rawSpan.ToArray();
                    if (PareceJpeg(raw))
                    {
                        bytes = raw;
                        extensao = "jpg";
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool PareceJpeg(byte[] bytes)
        {
            return bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8;
        }
    }
}
