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
        public void ExportarParaPDF(List<DadoFinanceiro> dadosFinanceiros, List<DadoVenda> dadosVendas, string caminhoArquivo)
        {
            EnsureDirectory(caminhoArquivo);

            var documento = new PdfDocument();
            documento.Info.Title = "Relatório Empresarial - Primo Auto Elétrica";
            documento.Info.Author = "Primo Auto Elétrica";
            documento.Info.Subject = "Relatório Analítico";

            var pagina = documento.AddPage();
            pagina.Size = PdfSharpCore.PageSize.A4;
            pagina.Orientation = PdfSharpCore.PageOrientation.Portrait;

            var grafico = XGraphics.FromPdfPage(pagina);
            var fonteTitulo = new XFont("Arial", 24, XFontStyle.Bold);
            var fonteSubtitulo = new XFont("Arial", 14, XFontStyle.Regular);
            var fonteTexto = new XFont("Arial", 10, XFontStyle.Regular);
            var fonteNegrito = new XFont("Arial", 10, XFontStyle.Bold);

            // Cabeçalho
            grafico.DrawString("CENTRAL DE INTELIGÊNCIA EMPRESARIAL", fonteTitulo, XBrushes.DarkBlue, new XRect(0, 20, pagina.Width, 40), XStringFormats.Center);
            grafico.DrawString("Primo Auto Elétrica", fonteSubtitulo, XBrushes.Gray, new XRect(0, 60, pagina.Width, 20), XStringFormats.Center);
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
            grafico.DrawString("Relatório gerado automaticamente pelo Sistema ERP Primo Auto Elétrica", fonteTexto, XBrushes.Gray, new XRect(0, pagina.Height - 40, pagina.Width, 20), XStringFormats.Center);

            documento.Save(caminhoArquivo);
        }

        public void ExportarParaExcel(List<DadoFinanceiro> dadosFinanceiros, List<DadoVenda> dadosVendas, string caminhoArquivo)
        {
            EnsureDirectory(caminhoArquivo);

            using var writer = new StreamWriter(caminhoArquivo);
            
            // Cabeçalho
            writer.WriteLine("CENTRAL DE INTELIGÊNCIA EMPRESARIAL - PRIMO AUTO ELÉTRICA");
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
    }
}
