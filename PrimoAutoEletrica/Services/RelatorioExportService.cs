using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public class RelatorioExportService
    {
        private readonly BusinessConfiguration _businessConfiguration;

        public RelatorioExportService()
            : this(BusinessConfigurationService.LoadOrCreateDefault(
                global::PrimoAutoEletrica.App.RuntimeAppDataPath,
                global::PrimoAutoEletrica.App.Logger))
        {
        }

        public RelatorioExportService(BusinessConfiguration businessConfiguration)
        {
            _businessConfiguration = businessConfiguration ?? new BusinessConfiguration();
        }

        public void ExportarParaPDF(List<DadoFinanceiro> dadosFinanceiros, List<DadoVenda> dadosVendas, string caminhoArquivo)
        {
            EnsureDirectory(caminhoArquivo);

            var documento = new PdfDocument();
            var empresa = _businessConfiguration.EffectiveCompanyName;
            documento.Info.Title = $"Relatorio Empresarial - {empresa}";
            documento.Info.Author = empresa;
            documento.Info.Subject = "Relatorio Analitico";

            var pagina = documento.AddPage();
            pagina.Size = PdfSharpCore.PageSize.A4;
            pagina.Orientation = PdfSharpCore.PageOrientation.Portrait;

            var grafico = XGraphics.FromPdfPage(pagina);
            var fonteTitulo = new XFont("Arial", 24, XFontStyle.Bold);
            var fonteSubtitulo = new XFont("Arial", 14, XFontStyle.Regular);
            var fonteTexto = new XFont("Arial", 10, XFontStyle.Regular);
            var fonteNegrito = new XFont("Arial", 10, XFontStyle.Bold);

            // Cabeçalho
            TryDrawLogo(grafico, 50, 18, 88, 44);
            grafico.DrawString("CENTRAL DE INTELIGENCIA EMPRESARIAL", fonteTitulo, XBrushes.DarkBlue, new XRect(0, 20, pagina.Width, 40), XStringFormats.Center);
            grafico.DrawString(empresa, fonteSubtitulo, XBrushes.Gray, new XRect(0, 60, pagina.Width, 20), XStringFormats.Center);
            grafico.DrawString($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}", fonteTexto, XBrushes.Gray, new XRect(0, 80, pagina.Width, 20), XStringFormats.Center);

            // Linha separadora
            var caneta = new XPen(XColors.DarkBlue, 2);
            grafico.DrawLine(caneta, 50, 100, pagina.Width - 50, 100);

            // Resumo Financeiro
            var y = 120;
            grafico.DrawString("RESUMO FINANCEIRO", fonteNegrito, XBrushes.DarkBlue, 50, y);
            y += 20;

            var faturamentoTotal = dadosFinanceiros.Where(d => d.Tipo == "Receita").Sum(d => d.Valor);
            var despesasTotal = dadosFinanceiros.Where(d => d.Tipo == "Despesa").Sum(d => d.Valor);
            var lucroLiquido = faturamentoTotal - despesasTotal;

            grafico.DrawString($"Faturamento Total: {faturamentoTotal:C}", fonteTexto, XBrushes.Black, 50, y); y += 15;
            grafico.DrawString($"Despesas Totais: {despesasTotal:C}", fonteTexto, XBrushes.Red, 50, y); y += 15;
            grafico.DrawString($"Lucro Líquido: {lucroLiquido:C}", fonteNegrito, XBrushes.Green, 50, y); y += 30;

            // Vendas
            grafico.DrawString("RESUMO DE VENDAS", fonteNegrito, XBrushes.DarkBlue, 50, y);
            y += 20;

            var totalVendas = dadosVendas.Count;
            var valorTotalVendas = dadosVendas.Sum(v => v.ValorTotal);
            var ticketMedio = totalVendas > 0 ? valorTotalVendas / totalVendas : 0;

            grafico.DrawString($"Total de Vendas: {totalVendas}", fonteTexto, XBrushes.Black, 50, y); y += 15;
            grafico.DrawString($"Valor Total: {valorTotalVendas:C}", fonteTexto, XBrushes.Black, 50, y); y += 15;
            grafico.DrawString($"Ticket Médio: {ticketMedio:C}", fonteTexto, XBrushes.Black, 50, y); y += 30;

            // Detalhamento Financeiro
            grafico.DrawString("DETALHAMENTO FINANCEIRO", fonteNegrito, XBrushes.DarkBlue, 50, y);
            y += 20;

            foreach (var dado in dadosFinanceiros.Take(20))
            {
                var cor = dado.Tipo == "Receita" ? XBrushes.Green : XBrushes.Red;
                grafico.DrawString($"{dado.Data:dd/MM/yyyy} - {dado.Tipo}: {dado.Valor:C} - {dado.Descricao}", fonteTexto, cor, 50, y);
                y += 15;
            }

            // Rodapé
            grafico.DrawLine(caneta, 50, pagina.Height - 50, pagina.Width - 50, pagina.Height - 50);
            grafico.DrawString($"Relatorio gerado automaticamente pelo Sistema ERP {empresa}", fonteTexto, XBrushes.Gray, new XRect(0, pagina.Height - 40, pagina.Width, 20), XStringFormats.Center);

            documento.Save(caminhoArquivo);
        }

        public void ExportarParaExcel(List<DadoFinanceiro> dadosFinanceiros, List<DadoVenda> dadosVendas, string caminhoArquivo)
        {
            EnsureDirectory(caminhoArquivo);

            using var writer = new StreamWriter(caminhoArquivo);
            
            // Cabeçalho
            writer.WriteLine($"CENTRAL DE INTELIGENCIA EMPRESARIAL - {_businessConfiguration.EffectiveCompanyName}");
            writer.WriteLine($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}");
            writer.WriteLine();

            // Resumo Financeiro
            writer.WriteLine("RESUMO FINANCEIRO");
            writer.WriteLine("Tipo;Data;Categoria;Descrição;Valor;Forma Pagamento;Usuário");
            
            foreach (var dado in dadosFinanceiros)
            {
                writer.WriteLine($"{dado.Tipo};{dado.Data:dd/MM/yyyy};{dado.Categoria};{dado.Descricao};{dado.Valor};{dado.FormaPagamento};{dado.Usuario}");
            }

            writer.WriteLine();

            // Resumo de Vendas
            writer.WriteLine("RESUMO DE VENDAS");
            writer.WriteLine("Data;Cliente;Vendedor;Valor Total;Desconto;Lucro;Forma Pagamento;Status;Itens");
            
            foreach (var venda in dadosVendas)
            {
                writer.WriteLine($"{venda.Data:dd/MM/yyyy};{venda.ClienteNome};{venda.VendedorNome};{venda.ValorTotal};{venda.Desconto};{venda.Lucro};{venda.FormaPagamento};{venda.Status};{venda.ItensQuantidade}");
            }
        }

        public void ExportarParaCSV(List<DadoFinanceiro> dadosFinanceiros, string caminhoArquivo)
        {
            EnsureDirectory(caminhoArquivo);

            using var writer = new StreamWriter(caminhoArquivo);
            
            writer.WriteLine("Tipo;Data;Categoria;Descrição;Valor;Forma Pagamento;Usuário");
            
            foreach (var dado in dadosFinanceiros)
            {
                writer.WriteLine($"{dado.Tipo};{dado.Data:dd/MM/yyyy};{dado.Categoria};{dado.Descricao};{dado.Valor};{dado.FormaPagamento};{dado.Usuario}");
            }
        }

        private static void EnsureDirectory(string caminhoArquivo)
        {
            var directory = Path.GetDirectoryName(caminhoArquivo);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void TryDrawLogo(XGraphics graphics, double x, double y, double maxWidth, double maxHeight)
        {
            if (string.IsNullOrWhiteSpace(_businessConfiguration.LogoPath) ||
                !BusinessConfigurationService.ValidateLogoPath(_businessConfiguration.LogoPath).IsValid)
            {
                return;
            }

            try
            {
                using var image = XImage.FromFile(Path.GetFullPath(_businessConfiguration.LogoPath));
                var scale = Math.Min(maxWidth / image.PixelWidth, maxHeight / image.PixelHeight);
                var width = image.PixelWidth * scale;
                var height = image.PixelHeight * scale;
                graphics.DrawImage(image, x, y, width, height);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogWarning(
                    $"Falha ao inserir logo no relatorio PDF: {ex.Message}",
                    "Relatorios");
            }
        }
    }
}
