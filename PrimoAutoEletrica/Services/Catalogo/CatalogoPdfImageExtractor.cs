using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Rendering.Skia.Helpers;

namespace PrimoAutoEletrica.Services.Catalogo
{
    /// <summary>
    /// Extrai imagens embutidas de paginas PDF e grava PNG quando possivel.
    /// Usa Skia para decodificar Flate/RGB tipico de catalogos (DNI etc.).
    /// </summary>
    internal static class CatalogoPdfImageExtractor
    {
        public sealed record ImagemExtraida(
            double CentroY,
            double CentroX,
            string CaminhoLocal,
            int Largura,
            int Altura,
            double BoxBottom,
            double BoxTop);

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
                    var box = image.BoundingBox;
                    var w = image.WidthInSamples;
                    var h = image.HeightInSamples;

                    // Icones/logos minimos do catalogo DNI ~80px; abaixo de 48 ignora.
                    if (w < 48 || h < 48 || box.Width < 30 || box.Height < 30)
                    {
                        continue;
                    }

                    if (!TryObterPngBytes(image, out var bytes) || bytes == null || bytes.Length < 400)
                    {
                        continue;
                    }

                    var nome = $"{prefixoArquivo}_p{page.Number}_{index:D3}.png";
                    var caminho = Path.Combine(pastaDestino, nome);
                    File.WriteAllBytes(caminho, bytes);

                    var centroY = box.Bottom + (box.Height / 2.0);
                    var centroX = box.Left + (box.Width / 2.0);
                    imagens.Add(new ImagemExtraida(
                        centroY,
                        centroX,
                        caminho,
                        w,
                        h,
                        box.Bottom,
                        box.Top));
                    index++;
                }
                catch
                {
                    // Segue a importacao mesmo se uma imagem falhar.
                }
            }

            return imagens
                .OrderByDescending(img => img.CentroY)
                .ThenBy(img => img.CentroX)
                .ToList();
        }

        public static string? AssociarImagemMaisProxima(
            IReadOnlyList<ImagemExtraida> imagens,
            double? codigoCentroY,
            double? codigoCentroX = null)
        {
            if (imagens == null || imagens.Count == 0)
            {
                return null;
            }

            if (!codigoCentroY.HasValue)
            {
                return imagens
                    .OrderByDescending(img => img.Largura * img.Altura)
                    .First()
                    .CaminhoLocal;
            }

            return imagens
                .OrderBy(img =>
                {
                    var dy = Math.Abs(img.CentroY - codigoCentroY.Value);
                    var dx = codigoCentroX.HasValue
                        ? Math.Abs(img.CentroX - codigoCentroX.Value)
                        : 0;
                    // Peso maior no eixo Y (catalogos DNI empilham produtos na vertical).
                    return dy * 3.0 + dx;
                })
                .ThenByDescending(img => img.Largura * img.Altura)
                .First()
                .CaminhoLocal;
        }

        private static bool TryObterPngBytes(IPdfImage image, out byte[]? bytes)
        {
            bytes = null;

            try
            {
                if (image.TryGetPng(out var png) && png is { Length: > 0 })
                {
                    bytes = png;
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                using var skBitmap = image.GetSKBitmap();
                if (skBitmap != null)
                {
                    using var skImage = SKImage.FromBitmap(skBitmap);
                    using var data = skImage.Encode(SKEncodedImageFormat.Png, 90);
                    if (data != null && data.Size > 0)
                    {
                        bytes = data.ToArray();
                        return bytes.Length > 0;
                    }
                }
            }
            catch
            {
            }

            try
            {
                var raw = image.RawBytes.ToArray();
                if (PareceJpeg(raw))
                {
                    bytes = raw;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool PareceJpeg(byte[] data)
        {
            return data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8;
        }
    }
}
