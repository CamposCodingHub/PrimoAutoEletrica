using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public class OrcamentoPdfService
    {
        private const double PageMargin = 38d;
        private const double BottomMargin = 36d;
        private const double HeaderHeight = 106d;
        private const double BodyLineHeight = 13d;
        private const double SmallLineHeight = 11d;

        public void GerarPdfOrcamento(Orcamento orcamento, string caminhoArquivo)
        {
            ArgumentNullException.ThrowIfNull(orcamento);

            var cliente = ResolveCliente(orcamento);

            using var document = new PdfDocument();
            document.Info.Title = $"Orcamento {orcamento.Numero} - Primo Auto Eletrica";
            document.Info.Author = "Primo Auto Eletrica";
            document.Info.Subject = "Orcamento comercial";

            var fontHero = new XFont("Arial", 22, XFontStyle.Bold);
            var fontTitle = new XFont("Arial", 16, XFontStyle.Bold);
            var fontSection = new XFont("Arial", 11, XFontStyle.Bold);
            var fontLabel = new XFont("Arial", 8, XFontStyle.Bold);
            var fontBody = new XFont("Arial", 10, XFontStyle.Regular);
            var fontBodyBold = new XFont("Arial", 10, XFontStyle.Bold);
            var fontSmall = new XFont("Arial", 8, XFontStyle.Regular);
            var fontTotal = new XFont("Arial", 18, XFontStyle.Bold);

            var brandDark = XColor.FromArgb(24, 37, 56);
            var brandOrange = XColor.FromArgb(228, 108, 10);
            var warmSurface = XColor.FromArgb(255, 247, 237);
            var cardSurface = XColor.FromArgb(248, 250, 252);
            var lightBorder = XColor.FromArgb(217, 224, 232);
            var primaryText = XColor.FromArgb(31, 41, 55);
            var mutedText = XColor.FromArgb(107, 114, 128);
            var stripedRow = XColor.FromArgb(248, 250, 252);
            var totalSurface = XColor.FromArgb(255, 247, 237);
            var dangerTint = XColor.FromArgb(255, 241, 242);

            PdfPage page = null!;
            XGraphics graphics = null!;
            double pageWidth = 0d;
            double pageHeight = 0d;
            double contentWidth = 0d;
            double y = 0d;
            var pageNumber = 0;

            BeginPage(false);
            DrawHero();
            DrawOverview();
            DrawItemsSection();
            DrawFinancialSummary();
            DrawTextSection("Condicoes comerciais", Safe(orcamento.CondicoesPagamento, "Nao informado."));
            DrawTextSection("Prazo e entrega", Safe(orcamento.PrazoEntrega, "Nao informado."));
            DrawTextSection("Observacoes", Safe(orcamento.Observacoes, "Sem observacoes adicionais."));
            DrawFooter();
            graphics.Dispose();

            document.Save(caminhoArquivo);

            void BeginPage(bool continuation)
            {
                graphics?.Dispose();
                page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                graphics = XGraphics.FromPdfPage(page);
                pageWidth = page.Width.Point;
                pageHeight = page.Height.Point;
                contentWidth = pageWidth - (PageMargin * 2d);
                y = PageMargin;
                pageNumber++;

                DrawPageHeader(continuation);
            }

            void EnsureSpace(double requiredHeight, bool continuationPage = false)
            {
                if (y + requiredHeight <= pageHeight - BottomMargin)
                {
                    return;
                }

                DrawFooter();
                BeginPage(continuationPage);
            }

            void DrawPageHeader(bool continuation)
            {
                var darkBrush = new XSolidBrush(brandDark);
                var orangeBrush = new XSolidBrush(brandOrange);
                var whiteBrush = XBrushes.White;
                var secondaryBrush = new XSolidBrush(XColor.FromArgb(216, 225, 234));
                var borderPen = new XPen(lightBorder, 1);

                graphics.DrawRectangle(darkBrush, PageMargin, y, contentWidth, HeaderHeight);
                graphics.DrawRectangle(orangeBrush, PageMargin, y, 12, HeaderHeight);

                graphics.DrawString(
                    "Primo Auto Eletrica",
                    fontHero,
                    whiteBrush,
                    new XRect(PageMargin + 24, y + 16, contentWidth - 220, 24),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    "Orcamento comercial com pecas, servicos e apresentacao pronta para impressao",
                    fontBody,
                    secondaryBrush,
                    new XRect(PageMargin + 24, y + 48, contentWidth - 220, 16),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    continuation ? "Continuidade do documento" : $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}",
                    fontSmall,
                    secondaryBrush,
                    new XRect(PageMargin + 24, y + 73, contentWidth - 220, 12),
                    XStringFormats.TopLeft);

                var badgeWidth = 184d;
                var badgeX = PageMargin + contentWidth - badgeWidth - 18d;
                var badgeY = y + 14d;
                graphics.DrawRectangle(XBrushes.White, badgeX, badgeY, badgeWidth, 74d);
                graphics.DrawRectangle(borderPen, badgeX, badgeY, badgeWidth, 74d);

                graphics.DrawString(
                    "ORCAMENTO",
                    fontSection,
                    orangeBrush,
                    new XRect(badgeX + 14, badgeY + 12, badgeWidth - 28, 12),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    Safe(orcamento.Numero, "Sem numero"),
                    fontTitle,
                    new XSolidBrush(primaryText),
                    new XRect(badgeX + 14, badgeY + 30, badgeWidth - 28, 18),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    $"{Safe(orcamento.Status, "Em aberto")}  |  Emissao {orcamento.DataCriacao:dd/MM/yyyy}",
                    fontSmall,
                    new XSolidBrush(mutedText),
                    new XRect(badgeX + 14, badgeY + 55, badgeWidth - 28, 12),
                    XStringFormats.TopLeft);

                y += HeaderHeight + 18d;
            }

            void DrawHero()
            {
                EnsureSpace(56d);

                graphics.DrawString(
                    "Proposta comercial",
                    fontTitle,
                    new XSolidBrush(primaryText),
                    new XRect(PageMargin, y, contentWidth, 18),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    "Documento comercial com dados do cliente, itens orcados e resumo financeiro",
                    fontBody,
                    new XSolidBrush(mutedText),
                    new XRect(PageMargin, y + 24, contentWidth, 14),
                    XStringFormats.TopLeft);

                y += 48d;
            }

            void DrawOverview()
            {
                var leftFields = new (string Label, string Value)[]
                {
                    ("Cliente", Safe(cliente?.Nome, "Nao informado")),
                    ("Documento", Safe(cliente?.Documento, "Nao informado")),
                    ("Contato", BuildContato(cliente)),
                    ("Email", Safe(cliente?.Email, "Nao informado")),
                    ("Endereco", BuildEndereco(cliente))
                };

                var rightFields = new (string Label, string Value)[]
                {
                    ("Numero", Safe(orcamento.Numero, "Nao informado")),
                    ("Status", Safe(orcamento.Status, "Em aberto")),
                    ("Criacao", orcamento.DataCriacao.ToString("dd/MM/yyyy HH:mm")),
                    ("Validade", orcamento.DataValidade?.ToString("dd/MM/yyyy") ?? "Nao definida"),
                    ("Pagamento", Safe(orcamento.CondicoesPagamento, "Nao informado")),
                    ("Entrega", Safe(orcamento.PrazoEntrega, "Nao informado"))
                };

                var gap = 14d;
                var cardWidth = (contentWidth - gap) / 2d;
                var leftHeight = CalculateCardHeight(leftFields, cardWidth);
                var rightHeight = CalculateCardHeight(rightFields, cardWidth);
                var cardHeight = Math.Max(leftHeight, rightHeight);

                EnsureSpace(cardHeight + 18d);

                DrawInfoCard(PageMargin, y, cardWidth, cardHeight, "Dados do cliente", leftFields);
                DrawInfoCard(PageMargin + cardWidth + gap, y, cardWidth, cardHeight, "Resumo comercial", rightFields);

                y += cardHeight + 18d;
            }

            void DrawItemsSection()
            {
                DrawSectionLabel("Itens do orcamento");

                var columns = new[]
                {
                    Math.Round(contentWidth * 0.46d, 2),
                    Math.Round(contentWidth * 0.14d, 2),
                    Math.Round(contentWidth * 0.08d, 2),
                    Math.Round(contentWidth * 0.12d, 2),
                    Math.Round(contentWidth * 0.10d, 2),
                    0d
                };
                columns[5] = Math.Round(contentWidth - columns.Take(5).Sum(), 2);
                var tableX = PageMargin;

                DrawItemsHeader(tableX, columns);

                if (orcamento.Itens.Count == 0)
                {
                    const double emptyRowHeight = 32d;
                    EnsureSpace(emptyRowHeight + 8d, continuationPage: true);
                    DrawEmptyItemsRow(tableX, columns.Sum(), emptyRowHeight);
                    y += emptyRowHeight + 16d;
                    return;
                }

                for (var index = 0; index < orcamento.Itens.Count; index++)
                {
                    var item = orcamento.Itens[index];
                    var descriptionLines = WrapText(Safe(item.ProdutoNome, "Item sem descricao"), fontBodyBold, columns[0] - 18d);
                    var detailLines = WrapText(BuildItemResumo(item), fontSmall, columns[0] - 18d);
                    var descriptionHeight = descriptionLines.Count * BodyLineHeight;
                    var detailHeight = detailLines.Count * SmallLineHeight;
                    var rowHeight = Math.Max(34d, descriptionHeight + detailHeight + 14d);
                    var pageBefore = pageNumber;

                    EnsureSpace(rowHeight + 8d, continuationPage: true);
                    if (pageBefore != pageNumber)
                    {
                        DrawSectionLabel("Itens do orcamento");
                        DrawItemsHeader(tableX, columns);
                    }

                    DrawItemRow(index, item, tableX, columns, rowHeight, descriptionLines, detailLines);
                    y += rowHeight;
                }

                y += 16d;
            }

            void DrawFinancialSummary()
            {
                const double cardHeight = 62d;
                const double gap = 8d;
                var cardWidth = (contentWidth - (gap * 3d)) / 4d;

                EnsureSpace(cardHeight + 30d, continuationPage: true);

                DrawSectionLabel("Resumo financeiro");

                DrawMetricCard(PageMargin, y, cardWidth, cardHeight, "Subtotal", orcamento.Subtotal.ToString("C"), cardSurface, primaryText);
                DrawMetricCard(PageMargin + cardWidth + gap, y, cardWidth, cardHeight, "Desconto", orcamento.Desconto.ToString("C"), dangerTint, XColor.FromArgb(185, 28, 28));
                DrawMetricCard(PageMargin + (cardWidth + gap) * 2d, y, cardWidth, cardHeight, "Acrescimo", orcamento.Acrescimo.ToString("C"), cardSurface, primaryText);
                DrawMetricCard(PageMargin + (cardWidth + gap) * 3d, y, cardWidth, cardHeight, "Total", orcamento.Total.ToString("C"), totalSurface, brandOrange, destaque: true);

                y += cardHeight + 18d;
            }

            void DrawTextSection(string title, string content)
            {
                var lines = WrapText(content, fontBody, contentWidth - 28d);
                var sectionHeight = 18d + (lines.Count * BodyLineHeight) + 18d;
                var pageBefore = pageNumber;

                EnsureSpace(sectionHeight + 30d, continuationPage: true);
                if (pageBefore != pageNumber)
                {
                    DrawHero();
                }

                DrawSectionLabel(title);

                var borderPen = new XPen(lightBorder, 1);
                graphics.DrawRectangle(XBrushes.White, PageMargin, y, contentWidth, 14d + (lines.Count * BodyLineHeight));
                graphics.DrawRectangle(borderPen, PageMargin, y, contentWidth, 14d + (lines.Count * BodyLineHeight));

                var currentY = y + 8d;
                foreach (var line in lines)
                {
                    graphics.DrawString(
                        line,
                        fontBody,
                        new XSolidBrush(primaryText),
                        new XRect(PageMargin + 12d, currentY, contentWidth - 24d, 12d),
                        XStringFormats.TopLeft);
                    currentY += BodyLineHeight;
                }

                y += 14d + (lines.Count * BodyLineHeight) + 16d;
            }

            void DrawInfoCard(double x, double top, double width, double height, string title, IReadOnlyList<(string Label, string Value)> fields)
            {
                var borderPen = new XPen(lightBorder, 1);
                graphics.DrawRectangle(XBrushes.White, x, top, width, height);
                graphics.DrawRectangle(borderPen, x, top, width, height);
                graphics.DrawRectangle(new XSolidBrush(warmSurface), x, top, width, 24d);
                graphics.DrawRectangle(new XSolidBrush(brandOrange), x, top, 8d, 24d);

                graphics.DrawString(
                    title.ToUpperInvariant(),
                    fontSection,
                    new XSolidBrush(primaryText),
                    new XRect(x + 16d, top + 7d, width - 24d, 12d),
                    XStringFormats.TopLeft);

                var currentY = top + 36d;
                foreach (var (label, value) in fields)
                {
                    graphics.DrawString(
                        label.ToUpperInvariant(),
                        fontLabel,
                        new XSolidBrush(mutedText),
                        new XRect(x + 14d, currentY, width - 28d, 10d),
                        XStringFormats.TopLeft);
                    currentY += 10d;

                    var valueLines = WrapText(value, fontBody, width - 28d);
                    foreach (var line in valueLines)
                    {
                        graphics.DrawString(
                            line,
                            fontBody,
                            new XSolidBrush(primaryText),
                            new XRect(x + 14d, currentY, width - 28d, 12d),
                            XStringFormats.TopLeft);
                        currentY += BodyLineHeight;
                    }

                    currentY += 4d;
                }
            }

            void DrawSectionLabel(string title)
            {
                EnsureSpace(30d, continuationPage: true);

                graphics.DrawRectangle(new XSolidBrush(warmSurface), PageMargin, y, contentWidth, 20d);
                graphics.DrawRectangle(new XPen(lightBorder, 1), PageMargin, y, contentWidth, 20d);
                graphics.DrawRectangle(new XSolidBrush(brandOrange), PageMargin, y, 8d, 20d);
                graphics.DrawString(
                    title.ToUpperInvariant(),
                    fontSection,
                    new XSolidBrush(primaryText),
                    new XRect(PageMargin + 16d, y + 4d, contentWidth - 20d, 12d),
                    XStringFormats.TopLeft);

                y += 28d;
            }

            void DrawItemsHeader(double tableX, IReadOnlyList<double> columns)
            {
                EnsureSpace(24d, continuationPage: true);

                var headers = new[] { "Descricao", "Tipo", "Qtd", "Unit.", "Desc.", "Total" };
                var tableWidth = columns.Sum();
                var borderPen = new XPen(lightBorder, 0.9);

                graphics.DrawRectangle(new XSolidBrush(brandDark), tableX, y, tableWidth, 22d);
                graphics.DrawRectangle(borderPen, tableX, y, tableWidth, 22d);

                double currentX = tableX;
                for (var index = 0; index < columns.Count; index++)
                {
                    var width = columns[index];
                    graphics.DrawString(
                        headers[index],
                        fontLabel,
                        XBrushes.White,
                        new XRect(currentX + 5d, y + 6d, width - 10d, 10d),
                        index >= 2 ? XStringFormats.TopRight : XStringFormats.TopLeft);

                    currentX += width;
                    if (index < columns.Count - 1)
                    {
                        graphics.DrawLine(borderPen, currentX, y, currentX, y + 22d);
                    }
                }

                y += 22d;
            }

            void DrawEmptyItemsRow(double tableX, double tableWidth, double rowHeight)
            {
                var borderPen = new XPen(lightBorder, 0.8);
                graphics.DrawRectangle(XBrushes.White, tableX, y, tableWidth, rowHeight);
                graphics.DrawRectangle(borderPen, tableX, y, tableWidth, rowHeight);
                graphics.DrawString(
                    "Nenhum item adicionado neste orcamento.",
                    fontBody,
                    new XSolidBrush(mutedText),
                    new XRect(tableX + 10d, y + 10d, tableWidth - 20d, 12d),
                    XStringFormats.TopLeft);
            }

            void DrawItemRow(
                int rowIndex,
                OrcamentoItem item,
                double tableX,
                IReadOnlyList<double> columns,
                double rowHeight,
                IReadOnlyList<string> descriptionLines,
                IReadOnlyList<string> detailLines)
            {
                var tableWidth = columns.Sum();
                var borderPen = new XPen(lightBorder, 0.8);
                var rowBrush = new XSolidBrush((rowIndex % 2) == 0 ? XColors.White : stripedRow);

                graphics.DrawRectangle(rowBrush, tableX, y, tableWidth, rowHeight);
                graphics.DrawRectangle(borderPen, tableX, y, tableWidth, rowHeight);

                double currentX = tableX;
                for (var index = 0; index < columns.Count - 1; index++)
                {
                    currentX += columns[index];
                    graphics.DrawLine(borderPen, currentX, y, currentX, y + rowHeight);
                }

                var descriptionX = tableX + 8d;
                var currentY = y + 7d;
                foreach (var line in descriptionLines)
                {
                    graphics.DrawString(
                        line,
                        fontBodyBold,
                        new XSolidBrush(primaryText),
                        new XRect(descriptionX, currentY, columns[0] - 16d, 12d),
                        XStringFormats.TopLeft);
                    currentY += BodyLineHeight;
                }

                foreach (var line in detailLines)
                {
                    graphics.DrawString(
                        line,
                        fontSmall,
                        new XSolidBrush(mutedText),
                        new XRect(descriptionX, currentY, columns[0] - 16d, 10d),
                        XStringFormats.TopLeft);
                    currentY += SmallLineHeight;
                }

                var typeX = tableX + columns[0];
                var qtyX = typeX + columns[1];
                var unitX = qtyX + columns[2];
                var discountX = unitX + columns[3];
                var totalX = discountX + columns[4];

                graphics.DrawString(
                    item.TipoDescricao,
                    fontBody,
                    new XSolidBrush(primaryText),
                    new XRect(typeX + 6d, y + 7d, columns[1] - 12d, 12d),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    item.Quantidade.ToString(),
                    fontBody,
                    new XSolidBrush(primaryText),
                    new XRect(qtyX + 6d, y + 7d, columns[2] - 12d, 12d),
                    XStringFormats.TopRight);

                graphics.DrawString(
                    item.PrecoUnitario.ToString("C"),
                    fontBody,
                    new XSolidBrush(primaryText),
                    new XRect(unitX + 6d, y + 7d, columns[3] - 12d, 12d),
                    XStringFormats.TopRight);

                graphics.DrawString(
                    item.Desconto.ToString("C"),
                    fontBody,
                    new XSolidBrush(XColor.FromArgb(185, 28, 28)),
                    new XRect(discountX + 6d, y + 7d, columns[4] - 12d, 12d),
                    XStringFormats.TopRight);

                graphics.DrawString(
                    item.Subtotal.ToString("C"),
                    fontBodyBold,
                    new XSolidBrush(primaryText),
                    new XRect(totalX + 6d, y + 7d, columns[5] - 12d, 12d),
                    XStringFormats.TopRight);
            }

            void DrawMetricCard(double x, double top, double width, double height, string label, string value, XColor background, XColor valueColor, bool destaque = false)
            {
                var borderPen = new XPen(lightBorder, 1);
                graphics.DrawRectangle(new XSolidBrush(background), x, top, width, height);
                graphics.DrawRectangle(borderPen, x, top, width, height);

                graphics.DrawString(
                    label.ToUpperInvariant(),
                    fontLabel,
                    new XSolidBrush(mutedText),
                    new XRect(x + 12d, top + 12d, width - 24d, 10d),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    value,
                    destaque ? fontTotal : fontTitle,
                    new XSolidBrush(valueColor),
                    new XRect(x + 12d, top + 28d, width - 24d, 18d),
                    XStringFormats.TopLeft);
            }

            void DrawFooter()
            {
                var lineY = pageHeight - 26d;
                graphics.DrawLine(new XPen(lightBorder, 1), PageMargin, lineY, pageWidth - PageMargin, lineY);

                graphics.DrawString(
                    "Primo Auto Eletrica  |  Proposta comercial pronta para impressao",
                    fontSmall,
                    new XSolidBrush(mutedText),
                    new XRect(PageMargin, lineY + 6d, contentWidth / 2d, 10d),
                    XStringFormats.TopLeft);

                graphics.DrawString(
                    $"Pagina {pageNumber}",
                    fontSmall,
                    new XSolidBrush(mutedText),
                    new XRect(PageMargin + (contentWidth / 2d), lineY + 6d, contentWidth / 2d, 10d),
                    XStringFormats.TopRight);
            }

            double CalculateCardHeight(IEnumerable<(string Label, string Value)> fields, double width)
            {
                var availableWidth = width - 28d;
                var totalHeight = 36d;

                foreach (var (_, value) in fields)
                {
                    totalHeight += 10d;
                    totalHeight += WrapText(value, fontBody, availableWidth).Count * BodyLineHeight;
                    totalHeight += 4d;
                }

                return Math.Max(132d, totalHeight + 10d);
            }
        }

        public string SalvarPdfDialog(Orcamento orcamento)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Orcamento_{orcamento.Numero}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                GerarPdfOrcamento(orcamento, saveFileDialog.FileName);
                return saveFileDialog.FileName;
            }

            return string.Empty;
        }

        private static Cliente? ResolveCliente(Orcamento orcamento)
        {
            if (orcamento.Cliente != null)
            {
                return orcamento.Cliente;
            }

            if (!orcamento.ClienteId.HasValue)
            {
                return null;
            }

            try
            {
                return global::PrimoAutoEletrica.App.Repositories.Clientes.ObterPorId(orcamento.ClienteId.Value);
            }
            catch
            {
                return null;
            }
        }

        private static string BuildContato(Cliente? cliente)
        {
            if (cliente == null)
            {
                return "Nao informado";
            }

            var telefone = Prefer(cliente.Telefone, cliente.WhatsApp);
            return string.IsNullOrWhiteSpace(telefone) ? "Nao informado" : telefone;
        }

        private static string BuildEndereco(Cliente? cliente)
        {
            if (cliente == null)
            {
                return "Nao informado";
            }

            var partes = new List<string>();
            var linhaPrincipal = string.Join(", ", new[] { Safe(cliente.Rua, string.Empty), Safe(cliente.Numero, string.Empty) }
                .Where(valor => !string.IsNullOrWhiteSpace(valor)));

            if (!string.IsNullOrWhiteSpace(linhaPrincipal))
            {
                partes.Add(linhaPrincipal);
            }

            var linhaSecundaria = string.Join(" - ", new[] { Safe(cliente.Bairro, string.Empty), Safe(cliente.Cidade, string.Empty), Safe(cliente.Estado, string.Empty) }
                .Where(valor => !string.IsNullOrWhiteSpace(valor)));

            if (!string.IsNullOrWhiteSpace(linhaSecundaria))
            {
                partes.Add(linhaSecundaria);
            }

            if (!string.IsNullOrWhiteSpace(cliente.CEP))
            {
                partes.Add($"CEP {cliente.CEP}");
            }

            return partes.Count == 0 ? "Nao informado" : string.Join(" | ", partes);
        }

        private static string BuildItemResumo(OrcamentoItem item)
        {
            var detalhes = new List<string>();

            if (!string.IsNullOrWhiteSpace(item.ProdutoCodigo) && !string.Equals(item.ProdutoCodigo, "SERVICO", StringComparison.OrdinalIgnoreCase))
            {
                detalhes.Add($"Codigo {item.ProdutoCodigo}");
            }

            if (!string.IsNullOrWhiteSpace(item.ProdutoCategoria))
            {
                detalhes.Add(item.ProdutoCategoria);
            }

            if (!string.IsNullOrWhiteSpace(item.ProdutoMarca))
            {
                detalhes.Add(item.ProdutoMarca);
            }

            if (!string.IsNullOrWhiteSpace(item.ProdutoAplicacao))
            {
                detalhes.Add(item.ProdutoAplicacao);
            }

            if (!string.IsNullOrWhiteSpace(item.Observacoes))
            {
                detalhes.Add(item.Observacoes);
            }

            return detalhes.Count == 0
                ? "Item comercial pronto para aprovacao"
                : string.Join(" | ", detalhes);
        }

        private static List<string> WrapText(string? text, XFont font, double maxWidth)
        {
            var value = Safe(text, "-");
            var words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return new List<string> { "-" };
            }

            using var document = new PdfDocument();
            var page = document.AddPage();
            using var graphics = XGraphics.FromPdfPage(page);

            var lines = new List<string>();
            var current = string.Empty;

            foreach (var word in words)
            {
                var candidate = string.IsNullOrWhiteSpace(current) ? word : $"{current} {word}";
                if (graphics.MeasureString(candidate, font).Width <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(current))
                {
                    lines.Add(current);
                }

                if (graphics.MeasureString(word, font).Width <= maxWidth)
                {
                    current = word;
                    continue;
                }

                current = string.Empty;
                var partial = string.Empty;
                foreach (var character in word)
                {
                    var partialCandidate = partial + character;
                    if (graphics.MeasureString(partialCandidate, font).Width <= maxWidth)
                    {
                        partial = partialCandidate;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(partial))
                    {
                        lines.Add(partial);
                    }

                    partial = character.ToString();
                }

                current = partial;
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                lines.Add(current);
            }

            return lines.Count == 0 ? new List<string> { "-" } : lines;
        }

        private static string Prefer(string? primary, string? secondary)
        {
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return primary.Trim();
            }

            return Safe(secondary, string.Empty);
        }

        private static string Safe(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
