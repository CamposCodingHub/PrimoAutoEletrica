using System;
using System.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace PrimoAutoEletrica.Services.Fiscal
{
    public interface IDanfeGenerator
    {
        /// <summary>
        /// Gera PDF informativo a partir de dados da operação/documento.
        /// NÃO afirma conformidade visual oficial DANFE SEFAZ sem layout homologado.
        /// </summary>
        byte[] GenerateInformationalPdf(FiscalOperation operation, FiscalDocumentRecord? document, string? tituloExtra = null);
    }

    /// <summary>PDF fiscal informativo (PdfSharpCore) — não é layout DANFE oficial SEFAZ.</summary>
    public sealed class DanfeInformationalPdfGenerator : IDanfeGenerator
    {
        public byte[] GenerateInformationalPdf(
            FiscalOperation operation,
            FiscalDocumentRecord? document,
            string? tituloExtra = null)
        {
            ArgumentNullException.ThrowIfNull(operation);

            using var pdf = new PdfDocument();
            pdf.Info.Title = "PRIMOX — Documento Fiscal (informativo)";
            pdf.Info.Author = "PRIMOX Workshop";
            var page = pdf.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            using var gfx = XGraphics.FromPdfPage(page);
            var titleFont = new XFont("Arial", 14, XFontStyle.Bold);
            var bodyFont = new XFont("Arial", 10, XFontStyle.Regular);
            var warnFont = new XFont("Arial", 9, XFontStyle.Italic);

            double y = 40;
            gfx.DrawString("PRIMOX Workshop — Resumo Fiscal (NAO e DANFE oficial)", titleFont, XBrushes.Black, 40, y);
            y += 28;
            gfx.DrawString(
                "Este PDF e informativo interno. Layout DANFE SEFAZ oficial depende de XML autorizado/provider.",
                warnFont,
                XBrushes.DarkRed,
                new XRect(40, y, page.Width - 80, 40),
                XStringFormats.TopLeft);
            y += 48;

            void Line(string label, string? value)
            {
                gfx.DrawString($"{label}: {value ?? "-"}", bodyFont, XBrushes.Black, 40, y);
                y += 16;
            }

            Line("Operacao", operation.Id.ToString("N"));
            Line("IdempotencyKey", operation.IdempotencyKey);
            Line("EmpresaId", operation.EmpresaId?.ToString("N"));
            Line("Tipo", operation.DocumentType.ToString());
            Line("Status", operation.Status.ToString());
            Line("Ambiente", operation.Environment.ToString());
            Line("Provider", operation.Provider.ToString());
            Line("ProviderDocumentId", operation.ProviderDocumentId);
            if (document != null)
            {
                Line("Numero", document.Numero);
                Line("Serie", document.Serie);
                Line("Chave", document.ChaveAcesso);
                Line("Protocolo", document.Protocolo);
            }

            if (!string.IsNullOrWhiteSpace(tituloExtra))
            {
                y += 8;
                gfx.DrawString(tituloExtra, bodyFont, XBrushes.Black, 40, y);
            }

            using var stream = new MemoryStream();
            pdf.Save(stream, false);
            return stream.ToArray();
        }
    }
}
