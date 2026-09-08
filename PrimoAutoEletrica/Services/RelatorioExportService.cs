using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using OfficeOpenXml;
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

        static RelatorioExportService()
        {
            // EPPlus 8+: LicenseContext obsoleto; usar License API nao comercial.
            ExcelPackage.License.SetNonCommercialOrganization("Primo Auto Eletrica");
        }

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

        public void ExportarParaPDF(
            List<DadoFinanceiro> dadosFinanceiros,
            List<DadoVenda> dadosVendas,
            string caminhoArquivo,
            List<DadoOrdemServicoRelatorio>? ordensAbertas = null,
            List<DadoOrdemServicoRelatorio>? ordensFinalizadas = null,
            List<DadoOrdemServicoTecnico>? ordensPorTecnico = null,
            List<DadoServicoRelatorio>? servicosMaisRealizados = null,
            List<DadoServicoRelatorio>? lucroPorServico = null)
        {
            EnsureDirectory(caminhoArquivo);
            ordensAbertas ??= new List<DadoOrdemServicoRelatorio>();
            ordensFinalizadas ??= new List<DadoOrdemServicoRelatorio>();
            ordensPorTecnico ??= new List<DadoOrdemServicoTecnico>();
            servicosMaisRealizados ??= new List<DadoServicoRelatorio>();
            lucroPorServico ??= new List<DadoServicoRelatorio>();

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

            grafico.DrawString("RESUMO DE OS E SERVICOS", fonteNegrito, XBrushes.DarkBlue, 50, y);
            y += 20;
            grafico.DrawString($"OS abertas: {ordensAbertas.Count} | OS finalizadas: {ordensFinalizadas.Count}", fonteTexto, XBrushes.Black, 50, y); y += 15;
            grafico.DrawString($"Tecnicos com OS: {ordensPorTecnico.Count} | Servicos realizados: {servicosMaisRealizados.Count}", fonteTexto, XBrushes.Black, 50, y); y += 15;
            var servicoDestaque = lucroPorServico.OrderByDescending(item => item.LucroBruto).FirstOrDefault();
            if (servicoDestaque != null)
            {
                grafico.DrawString($"Maior lucro por servico: {servicoDestaque.Servico} ({servicoDestaque.LucroBruto:C})", fonteTexto, XBrushes.Green, 50, y);
                y += 15;
            }
            y += 15;

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

        public void ExportarParaExcel(
            List<DadoFinanceiro> dadosFinanceiros,
            List<DadoVenda> dadosVendas,
            string caminhoArquivo,
            List<DadoOrdemServicoRelatorio>? ordensAbertas = null,
            List<DadoOrdemServicoRelatorio>? ordensFinalizadas = null,
            List<DadoOrdemServicoTecnico>? ordensPorTecnico = null,
            List<DadoServicoRelatorio>? servicosMaisRealizados = null,
            List<DadoServicoRelatorio>? lucroPorServico = null)
        {
            EnsureDirectory(caminhoArquivo);
            ordensAbertas ??= new List<DadoOrdemServicoRelatorio>();
            ordensFinalizadas ??= new List<DadoOrdemServicoRelatorio>();
            ordensPorTecnico ??= new List<DadoOrdemServicoTecnico>();
            servicosMaisRealizados ??= new List<DadoServicoRelatorio>();
            lucroPorServico ??= new List<DadoServicoRelatorio>();

            using var package = new ExcelPackage();
            var empresa = _businessConfiguration.EffectiveCompanyName;

            // Cabeçalho geral
            var workbook = package.Workbook;
            workbook.Properties.Title = $"Relatório Empresarial - {empresa}";
            workbook.Properties.Author = empresa;
            workbook.Properties.Subject = "Relatório Analítico";
            workbook.Properties.Comments = $"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // Aba de Resumo Financeiro
            var worksheetFinanceiro = workbook.Worksheets.Add("Resumo Financeiro");
            worksheetFinanceiro.Cells["A1"].Value = $"CENTRAL DE INTELIGÊNCIA EMPRESARIAL - {empresa}";
            worksheetFinanceiro.Cells["A2"].Value = $"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheetFinanceiro.Cells["A4"].Value = "RESUMO FINANCEIRO";
            worksheetFinanceiro.Cells["A4"].Style.Font.Bold = true;
            worksheetFinanceiro.Cells["A4"].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

            // Cabeçalho da tabela financeira
            worksheetFinanceiro.Cells["A5"].Value = "Tipo";
            worksheetFinanceiro.Cells["B5"].Value = "Data";
            worksheetFinanceiro.Cells["C5"].Value = "Categoria";
            worksheetFinanceiro.Cells["D5"].Value = "Descrição";
            worksheetFinanceiro.Cells["E5"].Value = "Valor";
            worksheetFinanceiro.Cells["F5"].Value = "Forma Pagamento";
            worksheetFinanceiro.Cells["G5"].Value = "Usuário";

            // Estilizar cabeçalho
            var headerRange = worksheetFinanceiro.Cells["A5:G5"];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            // Preencher dados financeiros
            int row = 6;
            foreach (var dado in dadosFinanceiros)
            {
                worksheetFinanceiro.Cells[row, 1].Value = dado.Tipo;
                worksheetFinanceiro.Cells[row, 2].Value = dado.Data.ToString("dd/MM/yyyy");
                worksheetFinanceiro.Cells[row, 3].Value = dado.Categoria;
                worksheetFinanceiro.Cells[row, 4].Value = dado.Descricao;
                worksheetFinanceiro.Cells[row, 5].Value = dado.Valor;
                worksheetFinanceiro.Cells[row, 5].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetFinanceiro.Cells[row, 6].Value = dado.FormaPagamento;
                worksheetFinanceiro.Cells[row, 7].Value = dado.Usuario;

                // Cor baseada no tipo
                if (dado.Tipo == "Receita")
                {
                    worksheetFinanceiro.Cells[row, 5].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                }
                else
                {
                    worksheetFinanceiro.Cells[row, 5].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                }

                row++;
            }

            // Autoajustar colunas
            worksheetFinanceiro.Cells[worksheetFinanceiro.Dimension.Address].AutoFitColumns();

            // Aba de Vendas
            var worksheetVendas = workbook.Worksheets.Add("Vendas");
            worksheetVendas.Cells["A1"].Value = "RESUMO DE VENDAS";
            worksheetVendas.Cells["A1"].Style.Font.Bold = true;
            worksheetVendas.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

            worksheetVendas.Cells["A3"].Value = "Data";
            worksheetVendas.Cells["B3"].Value = "Cliente";
            worksheetVendas.Cells["C3"].Value = "Vendedor";
            worksheetVendas.Cells["D3"].Value = "Valor Total";
            worksheetVendas.Cells["E3"].Value = "Desconto";
            worksheetVendas.Cells["F3"].Value = "Lucro";
            worksheetVendas.Cells["G3"].Value = "Forma Pagamento";
            worksheetVendas.Cells["H3"].Value = "Status";
            worksheetVendas.Cells["I3"].Value = "Itens";

            var headerVendas = worksheetVendas.Cells["A3:I3"];
            headerVendas.Style.Font.Bold = true;
            headerVendas.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerVendas.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            row = 4;
            foreach (var venda in dadosVendas)
            {
                worksheetVendas.Cells[row, 1].Value = venda.Data.ToString("dd/MM/yyyy");
                worksheetVendas.Cells[row, 2].Value = venda.ClienteNome;
                worksheetVendas.Cells[row, 3].Value = venda.VendedorNome;
                worksheetVendas.Cells[row, 4].Value = venda.ValorTotal;
                worksheetVendas.Cells[row, 4].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetVendas.Cells[row, 5].Value = venda.Desconto;
                worksheetVendas.Cells[row, 5].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetVendas.Cells[row, 6].Value = venda.Lucro;
                worksheetVendas.Cells[row, 6].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetVendas.Cells[row, 7].Value = venda.FormaPagamento;
                worksheetVendas.Cells[row, 8].Value = venda.Status;
                worksheetVendas.Cells[row, 9].Value = venda.ItensQuantidade;
                row++;
            }

            worksheetVendas.Cells[worksheetVendas.Dimension.Address].AutoFitColumns();

            // Aba de Ordens de Serviço
            var worksheetOS = workbook.Worksheets.Add("Ordens de Serviço");
            worksheetOS.Cells["A1"].Value = "ORDENS DE SERVIÇO";
            worksheetOS.Cells["A1"].Style.Font.Bold = true;
            worksheetOS.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

            // OS Abertas
            worksheetOS.Cells["A3"].Value = "OS ABERTAS";
            worksheetOS.Cells["A3"].Style.Font.Bold = true;
            worksheetOS.Cells["A4"].Value = "Número";
            worksheetOS.Cells["B4"].Value = "Cliente";
            worksheetOS.Cells["C4"].Value = "Técnico";
            worksheetOS.Cells["D4"].Value = "Status";
            worksheetOS.Cells["E4"].Value = "Abertura";
            worksheetOS.Cells["F4"].Value = "Valor";
            worksheetOS.Cells["G4"].Value = "Lucro";
            worksheetOS.Cells["H4"].Value = "Itens";

            var headerOS = worksheetOS.Cells["A4:H4"];
            headerOS.Style.Font.Bold = true;
            headerOS.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerOS.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            row = 5;
            foreach (var ordem in ordensAbertas)
            {
                worksheetOS.Cells[row, 1].Value = ordem.Numero;
                worksheetOS.Cells[row, 2].Value = ordem.ClienteNome;
                worksheetOS.Cells[row, 3].Value = ordem.TecnicoNome;
                worksheetOS.Cells[row, 4].Value = ordem.Status;
                worksheetOS.Cells[row, 5].Value = ordem.DataAbertura.ToString("dd/MM/yyyy");
                worksheetOS.Cells[row, 6].Value = ordem.ValorTotal;
                worksheetOS.Cells[row, 6].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetOS.Cells[row, 7].Value = ordem.LucroBruto;
                worksheetOS.Cells[row, 7].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetOS.Cells[row, 8].Value = ordem.TotalItens;
                row++;
            }

            // OS Finalizadas
            row += 2;
            worksheetOS.Cells[row, 1].Value = "OS FINALIZADAS";
            worksheetOS.Cells[row, 1].Style.Font.Bold = true;
            row++;
            worksheetOS.Cells[row, 1].Value = "Número";
            worksheetOS.Cells[row, 2].Value = "Cliente";
            worksheetOS.Cells[row, 3].Value = "Técnico";
            worksheetOS.Cells[row, 4].Value = "Status";
            worksheetOS.Cells[row, 5].Value = "Conclusão";
            worksheetOS.Cells[row, 6].Value = "Valor";
            worksheetOS.Cells[row, 7].Value = "Lucro";
            worksheetOS.Cells[row, 8].Value = "Itens";

            var headerOSFinalizadas = worksheetOS.Cells[row, 1, row, 8];
            headerOSFinalizadas.Style.Font.Bold = true;
            headerOSFinalizadas.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerOSFinalizadas.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            row++;

            foreach (var ordem in ordensFinalizadas)
            {
                worksheetOS.Cells[row, 1].Value = ordem.Numero;
                worksheetOS.Cells[row, 2].Value = ordem.ClienteNome;
                worksheetOS.Cells[row, 3].Value = ordem.TecnicoNome;
                worksheetOS.Cells[row, 4].Value = ordem.Status;
                worksheetOS.Cells[row, 5].Value = ordem.DataConclusao.HasValue ? ordem.DataConclusao.Value.ToString("dd/MM/yyyy") : "";
                worksheetOS.Cells[row, 6].Value = ordem.ValorTotal;
                worksheetOS.Cells[row, 6].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetOS.Cells[row, 7].Value = ordem.LucroBruto;
                worksheetOS.Cells[row, 7].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetOS.Cells[row, 8].Value = ordem.TotalItens;
                row++;
            }

            worksheetOS.Cells[worksheetOS.Dimension.Address].AutoFitColumns();

            // Aba de Serviços
            var worksheetServicos = workbook.Worksheets.Add("Serviços");
            worksheetServicos.Cells["A1"].Value = "SERVIÇOS";
            worksheetServicos.Cells["A1"].Style.Font.Bold = true;
            worksheetServicos.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);

            // Serviços Mais Realizados
            worksheetServicos.Cells["A3"].Value = "SERVIÇOS MAIS REALIZADOS";
            worksheetServicos.Cells["A3"].Style.Font.Bold = true;
            worksheetServicos.Cells["A4"].Value = "Serviço";
            worksheetServicos.Cells["B4"].Value = "Origem";
            worksheetServicos.Cells["C4"].Value = "Quantidade";
            worksheetServicos.Cells["D4"].Value = "Receita";
            worksheetServicos.Cells["E4"].Value = "Custo";
            worksheetServicos.Cells["F4"].Value = "Lucro";
            worksheetServicos.Cells["G4"].Value = "Margem";
            worksheetServicos.Cells["H4"].Value = "Última Execução";

            var headerServicos = worksheetServicos.Cells["A4:H4"];
            headerServicos.Style.Font.Bold = true;
            headerServicos.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerServicos.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            row = 5;
            foreach (var servico in servicosMaisRealizados)
            {
                worksheetServicos.Cells[row, 1].Value = servico.Servico;
                worksheetServicos.Cells[row, 2].Value = servico.Origem;
                worksheetServicos.Cells[row, 3].Value = servico.Quantidade;
                worksheetServicos.Cells[row, 4].Value = servico.ReceitaTotal;
                worksheetServicos.Cells[row, 4].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetServicos.Cells[row, 5].Value = servico.CustoTotal;
                worksheetServicos.Cells[row, 5].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetServicos.Cells[row, 6].Value = servico.LucroBruto;
                worksheetServicos.Cells[row, 6].Style.Numberformat.Format = "\"R$\" #,##0.00";
                worksheetServicos.Cells[row, 7].Value = servico.MargemPercentual;
                worksheetServicos.Cells[row, 7].Style.Numberformat.Format = "0.00%";
                worksheetServicos.Cells[row, 8].Value = servico.UltimaExecucao.ToString("dd/MM/yyyy");
                row++;
            }

            worksheetServicos.Cells[worksheetServicos.Dimension.Address].AutoFitColumns();

            // Salvar arquivo
            var fileInfo = new FileInfo(caminhoArquivo);
            package.SaveAs(fileInfo);
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
