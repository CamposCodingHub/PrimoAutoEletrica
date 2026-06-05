using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class ProdutoEtiquetaService
    {
        private const int CopiesPerProduct = 8;
        private static readonly IReadOnlyDictionary<char, string> Code39Patterns = new Dictionary<char, string>
        {
            ['0'] = "nnnwwnwnn",
            ['1'] = "wnnwnnnnw",
            ['2'] = "nnwwnnnnw",
            ['3'] = "wnwwnnnnn",
            ['4'] = "nnnwwnnnw",
            ['5'] = "wnnwwnnnn",
            ['6'] = "nnwwwnnnn",
            ['7'] = "nnnwnnwnw",
            ['8'] = "wnnwnnwnn",
            ['9'] = "nnwwnnwnn",
            ['A'] = "wnnnnwnnw",
            ['B'] = "nnwnnwnnw",
            ['C'] = "wnwnnwnnn",
            ['D'] = "nnnnwwnnw",
            ['E'] = "wnnnwwnnn",
            ['F'] = "nnwnwwnnn",
            ['G'] = "nnnnnwwnw",
            ['H'] = "wnnnnwwnn",
            ['I'] = "nnwnnwwnn",
            ['J'] = "nnnnwwwnn",
            ['K'] = "wnnnnnnww",
            ['L'] = "nnwnnnnww",
            ['M'] = "wnwnnnnwn",
            ['N'] = "nnnnwnnww",
            ['O'] = "wnnnwnnwn",
            ['P'] = "nnwnwnnwn",
            ['Q'] = "nnnnnnwww",
            ['R'] = "wnnnnnwwn",
            ['S'] = "nnwnnnwwn",
            ['T'] = "nnnnwnwwn",
            ['U'] = "wwnnnnnnw",
            ['V'] = "nwwnnnnnw",
            ['W'] = "wwwnnnnnn",
            ['X'] = "nwnnwnnnw",
            ['Y'] = "wwnnwnnnn",
            ['Z'] = "nwwnwnnnn",
            ['-'] = "nwnnnnwnw",
            ['.'] = "wwnnnnwnn",
            [' '] = "nwwnnnwnn",
            ['$'] = "nwnwnwnnn",
            ['/'] = "nwnwnnnwn",
            ['+'] = "nwnnnwnwn",
            ['%'] = "nnnwnwnwn",
            ['*'] = "nwnnwnwnn"
        };

        public ProdutoEtiquetaResultado GerarEtiquetas(Produto produto, string? pastaDestino = null)
        {
            ArgumentNullException.ThrowIfNull(produto);

            var destino = string.IsNullOrWhiteSpace(pastaDestino)
                ? Path.Combine(AppContext.BaseDirectory, "Etiquetas")
                : pastaDestino;

            Directory.CreateDirectory(destino);

            var referencia = PrimeiroValor(produto.CodigoBarras, produto.SKU, produto.Codigo, produto.Nome);
            var arquivo = Path.Combine(
                destino,
                $"Etiqueta_{SanitizeFileName(referencia)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using var document = new PdfDocument();
            document.Info.Title = $"Etiquetas - {produto.Nome}";
            document.Info.Author = "Primo Auto Eletrica";
            document.Info.Subject = "Etiqueta operacional de produto";

            var page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            page.Orientation = PdfSharpCore.PageOrientation.Portrait;

            using var graphics = XGraphics.FromPdfPage(page);
            DrawSheet(graphics, page, produto);

            document.Save(arquivo);

            return new ProdutoEtiquetaResultado(arquivo, CopiesPerProduct);
        }

        private static void DrawSheet(XGraphics graphics, PdfPage page, Produto produto)
        {
            var margin = 34d;
            var gap = 8d;
            var columns = 2;
            var rows = 4;
            var labelWidth = (page.Width.Point - (margin * 2d) - gap) / columns;
            var labelHeight = (page.Height.Point - (margin * 2d) - (gap * (rows - 1))) / rows;

            var fontBrand = new XFont("Arial", 8, XFontStyle.Bold);
            var fontName = new XFont("Arial", 10, XFontStyle.Bold);
            var fontText = new XFont("Arial", 7, XFontStyle.Regular);
            var fontTextBold = new XFont("Arial", 7, XFontStyle.Bold);
            var fontPrice = new XFont("Arial", 12, XFontStyle.Bold);
            var fontCode = new XFont("Consolas", 8, XFontStyle.Regular);
            var borderPen = new XPen(XColor.FromArgb(190, 199, 208), 0.8);
            var mutedBrush = new XSolidBrush(XColor.FromArgb(86, 99, 114));
            var brandBrush = new XSolidBrush(XColor.FromArgb(228, 108, 10));
            var textBrush = new XSolidBrush(XColor.FromArgb(31, 41, 55));
            var headerBrush = new XSolidBrush(XColor.FromArgb(24, 37, 56));
            var lightBrush = new XSolidBrush(XColor.FromArgb(248, 250, 252));

            for (var index = 0; index < CopiesPerProduct; index++)
            {
                var row = index / columns;
                var column = index % columns;
                var x = margin + column * (labelWidth + gap);
                var y = margin + row * (labelHeight + gap);

                graphics.DrawRectangle(lightBrush, x, y, labelWidth, labelHeight);
                graphics.DrawRectangle(borderPen, x, y, labelWidth, labelHeight);
                graphics.DrawRectangle(headerBrush, x, y, labelWidth, 24);
                graphics.DrawRectangle(brandBrush, x, y, 6, 24);

                graphics.DrawString(
                    "PRIMO AUTO ELETRICA",
                    fontBrand,
                    XBrushes.White,
                    new XRect(x + 12, y + 7, labelWidth - 24, 10),
                    XStringFormats.TopLeft);

                var contentX = x + 12;
                var contentWidth = labelWidth - 24;
                var currentY = y + 34;

                graphics.DrawString(
                    Trim(produto.Nome, 42),
                    fontName,
                    textBrush,
                    new XRect(contentX, currentY, contentWidth, 14),
                    XStringFormats.TopLeft);
                currentY += 18;

                DrawLine(graphics, "Codigo", PrimeiroValor(produto.Codigo, "-"), fontTextBold, fontText, textBrush, mutedBrush, contentX, currentY, contentWidth / 2d);
                DrawLine(graphics, "SKU", PrimeiroValor(produto.SKU, "-"), fontTextBold, fontText, textBrush, mutedBrush, contentX + contentWidth / 2d, currentY, contentWidth / 2d);
                currentY += 13;

                DrawLine(graphics, "Local", PrimeiroValor(produto.Localizacao, produto.Prateleira, "-"), fontTextBold, fontText, textBrush, mutedBrush, contentX, currentY, contentWidth / 2d);
                DrawLine(graphics, "Unidade", PrimeiroValor(produto.UnidadeMedida, "-"), fontTextBold, fontText, textBrush, mutedBrush, contentX + contentWidth / 2d, currentY, contentWidth / 2d);
                currentY += 16;

                var barcodeValue = PrimeiroValor(produto.CodigoBarras, produto.SKU, produto.Codigo);
                var barcodeDrawn = TryDrawCode39(graphics, barcodeValue, contentX, currentY, contentWidth, 34);
                currentY += 38;

                graphics.DrawString(
                    barcodeDrawn ? NormalizeCode39(barcodeValue) : $"COD: {PrimeiroValor(barcodeValue, "-")}",
                    fontCode,
                    textBrush,
                    new XRect(contentX, currentY, contentWidth, 10),
                    XStringFormats.TopCenter);

                graphics.DrawString(
                    produto.PrecoVenda > 0 ? produto.PrecoVenda.ToString("C2") : "Preco a conferir",
                    fontPrice,
                    brandBrush,
                    new XRect(contentX, y + labelHeight - 26, contentWidth, 16),
                    XStringFormats.TopRight);
            }
        }

        private static void DrawLine(
            XGraphics graphics,
            string label,
            string value,
            XFont labelFont,
            XFont valueFont,
            XBrush textBrush,
            XBrush mutedBrush,
            double x,
            double y,
            double width)
        {
            graphics.DrawString($"{label}: ", labelFont, mutedBrush, new XRect(x, y, width, 10), XStringFormats.TopLeft);
            graphics.DrawString(Trim(value, 24), valueFont, textBrush, new XRect(x + 40, y, width - 40, 10), XStringFormats.TopLeft);
        }

        private static bool TryDrawCode39(XGraphics graphics, string value, double x, double y, double width, double height)
        {
            var normalized = NormalizeCode39(value);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            var encoded = $"*{normalized}*";
            var totalUnits = encoded.Sum(character => Code39Patterns[character].Sum(part => part == 'w' ? 3 : 1) + 1);
            var unit = width / totalUnits;

            if (unit < 0.65d)
            {
                return false;
            }

            var cursor = x;
            foreach (var character in encoded)
            {
                var pattern = Code39Patterns[character];
                for (var index = 0; index < pattern.Length; index++)
                {
                    var module = pattern[index] == 'w' ? unit * 3d : unit;
                    if (index % 2 == 0)
                    {
                        graphics.DrawRectangle(XBrushes.Black, cursor, y, module, height);
                    }

                    cursor += module;
                }

                cursor += unit;
            }

            return true;
        }

        private static string NormalizeCode39(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = new string(value
                .Trim()
                .ToUpperInvariant()
                .Select(character => char.IsWhiteSpace(character) ? ' ' : character)
                .Where(character => character != '*' && Code39Patterns.ContainsKey(character))
                .Take(24)
                .ToArray());

            return normalized.Trim();
        }

        private static string PrimeiroValor(params string?[] valores)
        {
            return valores.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor))?.Trim() ?? string.Empty;
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength
                ? trimmed
                : $"{trimmed[..Math.Max(0, maxLength - 3)]}...";
        }

        private static string SanitizeFileName(string value)
        {
            var safe = string.IsNullOrWhiteSpace(value)
                ? "produto"
                : value.Trim();

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(invalid, '_');
            }

            return Trim(safe, 36).Replace(' ', '_');
        }
    }

    public sealed record ProdutoEtiquetaResultado(string CaminhoArquivo, int QuantidadeEtiquetas);
}
