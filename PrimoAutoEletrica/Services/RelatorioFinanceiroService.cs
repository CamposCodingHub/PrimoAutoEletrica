using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.Services
{
    public class RelatorioFinanceiroService
    {
        public void ExportarFluxoCaixaPDF(string caminho, DateTime dataInicio, DateTime dataFim, decimal entradas, decimal saidas, decimal saldo, List<dynamic> movimentacoes)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var graphics = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);
            var fontBold = new XFont("Arial", 12, XFontStyle.Bold);
            var fontTitle = new XFont("Arial", 18, XFontStyle.Bold);

            // Título
            graphics.DrawString("Relatório de Fluxo de Caixa", fontTitle, XBrushes.Black, 50, 50);
            graphics.DrawString($"Período: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}", font, XBrushes.Black, 50, 80);

            // Resumo
            graphics.DrawString("Resumo Financeiro", fontBold, XBrushes.Black, 50, 120);
            graphics.DrawString($"Entradas: R$ {entradas:N2}", font, XBrushes.Green, 50, 140);
            graphics.DrawString($"Saídas: R$ {saidas:N2}", font, XBrushes.Red, 50, 160);
            graphics.DrawString($"Saldo: R$ {saldo:N2}", fontBold, XBrushes.Blue, 50, 180);

            // Cabeçalho da tabela
            var y = 220;
            graphics.DrawString("Data", fontBold, XBrushes.Black, 50, y);
            graphics.DrawString("Tipo", fontBold, XBrushes.Black, 150, y);
            graphics.DrawString("Descrição", fontBold, XBrushes.Black, 250, y);
            graphics.DrawString("Valor", fontBold, XBrushes.Black, 450, y);

            y += 20;
            graphics.DrawLine(new XPen(XColors.Gray, 1), 50, y, 550, y);
            y += 10;

            // Movimentações
            foreach (var mov in movimentacoes)
            {
                graphics.DrawString(mov.Data, font, XBrushes.Black, 50, y);
                graphics.DrawString(mov.Tipo, font, XBrushes.Black, 150, y);
                graphics.DrawString(mov.Descricao, font, XBrushes.Black, 250, y);
                graphics.DrawString($"R$ {mov.Valor:N2}", font, XBrushes.Black, 450, y);
                y += 20;

                if (y > page.Height - 50)
                {
                    page = document.AddPage();
                    graphics = XGraphics.FromPdfPage(page);
                    y = 50;
                }
            }

            document.Save(caminho);
        }

        public void ExportarContasPagarPDF(string caminho, List<dynamic> contas)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var graphics = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);
            var fontBold = new XFont("Arial", 12, XFontStyle.Bold);
            var fontTitle = new XFont("Arial", 18, XFontStyle.Bold);

            // Título
            graphics.DrawString("Relatório de Contas a Pagar", fontTitle, XBrushes.Black, 50, 50);
            graphics.DrawString($"Data: {DateTime.Now:dd/MM/yyyy HH:mm}", font, XBrushes.Black, 50, 80);

            // Cabeçalho da tabela
            var y = 120;
            graphics.DrawString("Vencimento", fontBold, XBrushes.Black, 50, y);
            graphics.DrawString("Fornecedor", fontBold, XBrushes.Black, 150, y);
            graphics.DrawString("Descrição", fontBold, XBrushes.Black, 300, y);
            graphics.DrawString("Valor", fontBold, XBrushes.Black, 450, y);
            graphics.DrawString("Status", fontBold, XBrushes.Black, 530, y);

            y += 20;
            graphics.DrawLine(new XPen(XColors.Gray, 1), 50, y, 600, y);
            y += 10;

            // Contas
            foreach (var conta in contas)
            {
                graphics.DrawString(conta.DataVencimento, font, XBrushes.Black, 50, y);
                graphics.DrawString(conta.Fornecedor, font, XBrushes.Black, 150, y);
                graphics.DrawString(conta.Descricao, font, XBrushes.Black, 300, y);
                graphics.DrawString($"R$ {conta.Valor:N2}", font, XBrushes.Black, 450, y);
                graphics.DrawString(conta.Status, font, XBrushes.Black, 530, y);
                y += 20;

                if (y > page.Height - 50)
                {
                    page = document.AddPage();
                    graphics = XGraphics.FromPdfPage(page);
                    y = 50;
                }
            }

            document.Save(caminho);
        }

        public void ExportarContasReceberPDF(string caminho, List<dynamic> contas)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var graphics = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);
            var fontBold = new XFont("Arial", 12, XFontStyle.Bold);
            var fontTitle = new XFont("Arial", 18, XFontStyle.Bold);

            // Título
            graphics.DrawString("Relatório de Contas a Receber", fontTitle, XBrushes.Black, 50, 50);
            graphics.DrawString($"Data: {DateTime.Now:dd/MM/yyyy HH:mm}", font, XBrushes.Black, 50, 80);

            // Cabeçalho da tabela
            var y = 120;
            graphics.DrawString("Vencimento", fontBold, XBrushes.Black, 50, y);
            graphics.DrawString("Cliente", fontBold, XBrushes.Black, 150, y);
            graphics.DrawString("Descrição", fontBold, XBrushes.Black, 300, y);
            graphics.DrawString("Valor", fontBold, XBrushes.Black, 450, y);
            graphics.DrawString("Status", fontBold, XBrushes.Black, 530, y);

            y += 20;
            graphics.DrawLine(new XPen(XColors.Gray, 1), 50, y, 600, y);
            y += 10;

            // Contas
            foreach (var conta in contas)
            {
                graphics.DrawString(conta.DataVencimento, font, XBrushes.Black, 50, y);
                graphics.DrawString(conta.Cliente, font, XBrushes.Black, 150, y);
                graphics.DrawString(conta.Descricao, font, XBrushes.Black, 300, y);
                graphics.DrawString($"R$ {conta.Valor:N2}", font, XBrushes.Black, 450, y);
                graphics.DrawString(conta.Status, font, XBrushes.Black, 530, y);
                y += 20;

                if (y > page.Height - 50)
                {
                    page = document.AddPage();
                    graphics = XGraphics.FromPdfPage(page);
                    y = 50;
                }
            }

            document.Save(caminho);
        }

        public void ExportarParaCSV(string caminho, List<dynamic> dados, string[] colunas)
        {
            using var writer = new StreamWriter(caminho);
            
            // Cabeçalho
            writer.WriteLine(string.Join(";", colunas));
            
            // Dados
            foreach (var item in dados)
            {
                var valores = new List<string>();
                foreach (var coluna in colunas)
                {
                    var valor = item.GetType().GetProperty(coluna)?.GetValue(item)?.ToString() ?? "";
                    valores.Add($"\"{valor}\"");
                }
                writer.WriteLine(string.Join(";", valores));
            }
        }

        public void ExportarContasPagarCSV(string caminho, List<ContaPagar> contas)
        {
            using var writer = new StreamWriter(caminho);
            
            // Cabeçalho
            writer.WriteLine("Id;Fornecedor;Descricao;Valor;DataVencimento;Status;Categoria;Observacoes");
            
            // Dados
            foreach (var conta in contas)
            {
                writer.WriteLine($"{conta.Id};\"{conta.Fornecedor}\";\"{conta.Descricao}\";{conta.Valor};{conta.DataVencimento:dd/MM/yyyy};\"{conta.Status}\";\"{conta.Categoria}\";\"{conta.Observacoes}\"");
            }
        }

        public void ExportarContasReceberCSV(string caminho, List<ContaReceber> contas)
        {
            using var writer = new StreamWriter(caminho);
            
            // Cabeçalho
            writer.WriteLine("Id;Cliente;Descricao;Valor;DataVencimento;Status;FormaPagamento;Observacoes");
            
            // Dados
            foreach (var conta in contas)
            {
                writer.WriteLine($"{conta.Id};\"{conta.Cliente}\";\"{conta.Descricao}\";{conta.Valor};{conta.DataVencimento:dd/MM/yyyy};\"{conta.Status}\";\"{conta.FormaPagamento}\";\"{conta.Observacoes}\"");
            }
        }

        public void ExportarDashboardPDF(string caminho, Dictionary<string, decimal> cards, Dictionary<string, double> formasPagamento)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var graphics = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);
            var fontBold = new XFont("Arial", 12, XFontStyle.Bold);
            var fontTitle = new XFont("Arial", 18, XFontStyle.Bold);

            // Título
            graphics.DrawString("Dashboard Financeiro", fontTitle, XBrushes.Black, 50, 50);
            graphics.DrawString($"Data: {DateTime.Now:dd/MM/yyyy HH:mm}", font, XBrushes.Black, 50, 80);

            // Cards
            var y = 120;
            graphics.DrawString("Indicadores Financeiros", fontBold, XBrushes.Black, 50, y);
            y += 30;

            foreach (var card in cards)
            {
                graphics.DrawString($"{card.Key}: R$ {card.Value:N2}", font, XBrushes.Black, 50, y);
                y += 20;
            }

            y += 20;
            graphics.DrawString("Formas de Pagamento", fontBold, XBrushes.Black, 50, y);
            y += 30;

            foreach (var forma in formasPagamento)
            {
                graphics.DrawString($"{forma.Key}: R$ {forma.Value:N2}", font, XBrushes.Black, 50, y);
                y += 20;
            }

            document.Save(caminho);
        }

        public void ExportarPanoramaFinanceiroPdf(
            string caminho,
            DateTime dataInicio,
            DateTime dataFim,
            Dictionary<string, decimal> cards,
            DemonstrativoResultadoFinanceiro demonstrativo,
            Dictionary<string, double> formasPagamento,
            List<ContaPagar> contasPagar,
            List<ContaReceber> contasReceber)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var graphics = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 11);
            var fontBold = new XFont("Arial", 11, XFontStyle.Bold);
            var fontTitle = new XFont("Arial", 18, XFontStyle.Bold);
            var y = 40d;
            const double margin = 40d;

            void EnsurePage(double extra = 20d)
            {
                if (y + extra <= page.Height.Point - margin)
                {
                    return;
                }

                page = document.AddPage();
                graphics = XGraphics.FromPdfPage(page);
                y = margin;
            }

            void WriteLine(string text, XFont currentFont, XBrush brush, double spacing = 16d)
            {
                EnsurePage(spacing);
                graphics.DrawString(text, currentFont, brush, margin, y);
                y += spacing;
            }

            void WriteSection(string title)
            {
                y += 8;
                WriteLine(title, fontBold, XBrushes.DarkBlue, 18d);
                graphics.DrawLine(new XPen(XColors.LightGray, 1), margin, y, page.Width.Point - margin, y);
                y += 12;
            }

            WriteLine("Panorama Financeiro Consolidado", fontTitle, XBrushes.Black, 22d);
            WriteLine($"Periodo: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}", font, XBrushes.Black);
            WriteLine($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}", font, XBrushes.DimGray, 18d);

            WriteSection("Indicadores executivos");
            foreach (var card in cards)
            {
                WriteLine($"{card.Key}: R$ {card.Value:N2}", font, XBrushes.Black);
            }

            WriteSection("DRE operacional");
            WriteLine($"Receitas confirmadas: R$ {demonstrativo.ReceitasConfirmadas:N2}", font, XBrushes.DarkGreen);
            WriteLine($"Despesas confirmadas: R$ {demonstrativo.DespesasConfirmadas:N2}", font, XBrushes.DarkRed);
            WriteLine($"Resultado operacional: R$ {demonstrativo.ResultadoOperacional:N2}", fontBold, XBrushes.DarkBlue);
            WriteLine($"Contas a receber pendentes: R$ {demonstrativo.ContasReceberPendentes:N2}", font, XBrushes.Black);
            WriteLine($"Contas a pagar pendentes: R$ {demonstrativo.ContasPagarPendentes:N2}", font, XBrushes.Black);
            WriteLine($"Resultado projetado: R$ {demonstrativo.ResultadoProjetado:N2}", fontBold, XBrushes.DarkViolet);
            WriteLine($"Inadimplencia em aberto: R$ {demonstrativo.InadimplenciaEmAberto:N2}", font, XBrushes.SaddleBrown);

            WriteSection("Formas de pagamento");
            if (formasPagamento.Count == 0)
            {
                WriteLine("Sem recebimentos consolidados no periodo.", font, XBrushes.DimGray);
            }
            else
            {
                foreach (var forma in formasPagamento.OrderByDescending(item => item.Value))
                {
                    WriteLine($"{forma.Key}: R$ {forma.Value:N2}", font, XBrushes.Black);
                }
            }

            WriteSection("Contas a pagar em aberto");
            var contasPagarEmAberto = contasPagar
                .Where(conta => !string.Equals(conta.Status, "Paga", StringComparison.OrdinalIgnoreCase))
                .OrderBy(conta => conta.DataVencimento)
                .Take(12)
                .ToList();
            if (contasPagarEmAberto.Count == 0)
            {
                WriteLine("Nenhuma conta a pagar pendente.", font, XBrushes.DimGray);
            }
            else
            {
                foreach (var conta in contasPagarEmAberto)
                {
                    WriteLine($"{conta.DataVencimento:dd/MM/yyyy} | {conta.Fornecedor} | {conta.Valor:C} | {conta.Status}", font, XBrushes.Black);
                }
            }

            WriteSection("Contas a receber em aberto");
            var contasReceberEmAberto = contasReceber
                .Where(conta =>
                    !string.Equals(conta.Status, "Pago", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(conta.Status, "Recebida", StringComparison.OrdinalIgnoreCase))
                .OrderBy(conta => conta.DataVencimento)
                .Take(12)
                .ToList();
            if (contasReceberEmAberto.Count == 0)
            {
                WriteLine("Nenhuma conta a receber pendente.", font, XBrushes.DimGray);
            }
            else
            {
                foreach (var conta in contasReceberEmAberto)
                {
                    WriteLine($"{conta.DataVencimento:dd/MM/yyyy} | {conta.Cliente} | {conta.Valor:C} | {conta.Status}", font, XBrushes.Black);
                }
            }

            document.Save(caminho);
        }

        public void ExportarPanoramaFinanceiroCsv(
            string caminho,
            List<ContaPagar> contasPagar,
            List<ContaReceber> contasReceber,
            DemonstrativoResultadoFinanceiro demonstrativo)
        {
            using var writer = new StreamWriter(caminho);
            writer.WriteLine("Secao;Campo;Valor");
            writer.WriteLine($"DRE;ReceitasConfirmadas;{demonstrativo.ReceitasConfirmadas}");
            writer.WriteLine($"DRE;DespesasConfirmadas;{demonstrativo.DespesasConfirmadas}");
            writer.WriteLine($"DRE;ResultadoOperacional;{demonstrativo.ResultadoOperacional}");
            writer.WriteLine($"DRE;ContasReceberPendentes;{demonstrativo.ContasReceberPendentes}");
            writer.WriteLine($"DRE;ContasPagarPendentes;{demonstrativo.ContasPagarPendentes}");
            writer.WriteLine($"DRE;ResultadoProjetado;{demonstrativo.ResultadoProjetado}");
            writer.WriteLine($"DRE;InadimplenciaEmAberto;{demonstrativo.InadimplenciaEmAberto}");
            writer.WriteLine();
            writer.WriteLine("ContasPagar;Fornecedor;Descricao;Valor;DataVencimento;Status");
            foreach (var conta in contasPagar)
            {
                writer.WriteLine($"ContasPagar;\"{conta.Fornecedor}\";\"{conta.Descricao}\";{conta.Valor};{conta.DataVencimento:yyyy-MM-dd};\"{conta.Status}\"");
            }

            writer.WriteLine();
            writer.WriteLine("ContasReceber;Cliente;Descricao;Valor;DataVencimento;Status;FormaPagamento");
            foreach (var conta in contasReceber)
            {
                writer.WriteLine($"ContasReceber;\"{conta.Cliente}\";\"{conta.Descricao}\";{conta.Valor};{conta.DataVencimento:yyyy-MM-dd};\"{conta.Status}\";\"{conta.FormaPagamento}\"");
            }
        }
    }
}
