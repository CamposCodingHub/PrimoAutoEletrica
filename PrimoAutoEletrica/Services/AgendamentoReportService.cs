using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public class AgendamentoReportService
    {
        private readonly AgendamentoDatabaseService _databaseService;

        public AgendamentoReportService()
        {
            _databaseService = new AgendamentoDatabaseService();
        }

        public ReportData GerarRelatorioDiario(DateTime data)
        {
            var agendamentos = _databaseService.ObterAgendamentosPorData(data);
            
            return new ReportData
            {
                Titulo = $"Relatório Diário - {data:dd/MM/yyyy}",
                DataGeracao = DateTime.Now,
                Periodo = $"{data:dd/MM/yyyy}",
                TotalAgendamentos = agendamentos.Count,
                AgendamentosConcluidos = agendamentos.Count(a => a.Status == "Finalizado" || a.Status == "Entregue"),
                AgendamentosPendentes = agendamentos.Count(a => a.Status == "Agendado" || a.Status == "Confirmado"),
                AgendamentosEmAndamento = agendamentos.Count(a => a.Status == "Em Andamento"),
                ValorTotalEstimado = agendamentos.Sum(a => a.ValorEstimado),
                ValorTotalRealizado = agendamentos.Where(a => a.Status == "Finalizado" || a.Status == "Entregue").Sum(a => a.ValorReal),
                Agendamentos = agendamentos.ToList()
            };
        }

        public ReportData GerarRelatorioSemanal(DateTime dataInicio, DateTime dataFim)
        {
            var agendamentos = _databaseService.ObterAgendamentosPorPeriodo(dataInicio, dataFim);
            
            return new ReportData
            {
                Titulo = "Relatório Semanal",
                DataGeracao = DateTime.Now,
                Periodo = $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}",
                TotalAgendamentos = agendamentos.Count,
                AgendamentosConcluidos = agendamentos.Count(a => a.Status == "Finalizado" || a.Status == "Entregue"),
                AgendamentosPendentes = agendamentos.Count(a => a.Status == "Agendado" || a.Status == "Confirmado"),
                AgendamentosEmAndamento = agendamentos.Count(a => a.Status == "Em Andamento"),
                ValorTotalEstimado = agendamentos.Sum(a => a.ValorEstimado),
                ValorTotalRealizado = agendamentos.Where(a => a.Status == "Finalizado" || a.Status == "Entregue").Sum(a => a.ValorReal),
                Agendamentos = agendamentos.ToList()
            };
        }

        public ReportData GerarRelatorioMensal(int ano, int mes)
        {
            var dataInicio = new DateTime(ano, mes, 1);
            var dataFim = new DateTime(ano, mes, DateTime.DaysInMonth(ano, mes));
            var agendamentos = _databaseService.ObterAgendamentosPorPeriodo(dataInicio, dataFim);
            
            return new ReportData
            {
                Titulo = $"Relatório Mensal - {new DateTime(ano, mes, 1):MMMM/yyyy}",
                DataGeracao = DateTime.Now,
                Periodo = $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}",
                TotalAgendamentos = agendamentos.Count,
                AgendamentosConcluidos = agendamentos.Count(a => a.Status == "Finalizado" || a.Status == "Entregue"),
                AgendamentosPendentes = agendamentos.Count(a => a.Status == "Agendado" || a.Status == "Confirmado"),
                AgendamentosEmAndamento = agendamentos.Count(a => a.Status == "Em Andamento"),
                ValorTotalEstimado = agendamentos.Sum(a => a.ValorEstimado),
                ValorTotalRealizado = agendamentos.Where(a => a.Status == "Finalizado" || a.Status == "Entregue").Sum(a => a.ValorReal),
                Agendamentos = agendamentos.ToList()
            };
        }

        public ReportData GerarRelatorioPorTecnico(string tecnicoNome, DateTime dataInicio, DateTime dataFim)
        {
            var agendamentos = _databaseService.ObterAgendamentosPorPeriodo(dataInicio, dataFim)
                .Where(a => a.TecnicoNome == tecnicoNome).ToList();
            
            return new ReportData
            {
                Titulo = $"Relatório por Técnico - {tecnicoNome}",
                DataGeracao = DateTime.Now,
                Periodo = $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}",
                TotalAgendamentos = agendamentos.Count,
                AgendamentosConcluidos = agendamentos.Count(a => a.Status == "Finalizado" || a.Status == "Entregue"),
                AgendamentosPendentes = agendamentos.Count(a => a.Status == "Agendado" || a.Status == "Confirmado"),
                AgendamentosEmAndamento = agendamentos.Count(a => a.Status == "Em Andamento"),
                ValorTotalEstimado = agendamentos.Sum(a => a.ValorEstimado),
                ValorTotalRealizado = agendamentos.Where(a => a.Status == "Finalizado" || a.Status == "Entregue").Sum(a => a.ValorReal),
                Agendamentos = agendamentos
            };
        }

        public ReportData GerarRelatorioPorCliente(string clienteNome, DateTime dataInicio, DateTime dataFim)
        {
            var agendamentos = _databaseService.ObterAgendamentosPorPeriodo(dataInicio, dataFim)
                .Where(a => a.ClienteNome.Contains(clienteNome, StringComparison.OrdinalIgnoreCase)).ToList();
            
            return new ReportData
            {
                Titulo = $"Relatório por Cliente - {clienteNome}",
                DataGeracao = DateTime.Now,
                Periodo = $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}",
                TotalAgendamentos = agendamentos.Count,
                AgendamentosConcluidos = agendamentos.Count(a => a.Status == "Finalizado" || a.Status == "Entregue"),
                AgendamentosPendentes = agendamentos.Count(a => a.Status == "Agendado" || a.Status == "Confirmado"),
                AgendamentosEmAndamento = agendamentos.Count(a => a.Status == "Em Andamento"),
                ValorTotalEstimado = agendamentos.Sum(a => a.ValorEstimado),
                ValorTotalRealizado = agendamentos.Where(a => a.Status == "Finalizado" || a.Status == "Entregue").Sum(a => a.ValorReal),
                Agendamentos = agendamentos
            };
        }

        public ReportData GerarRelatorioPorStatus(string status, DateTime dataInicio, DateTime dataFim)
        {
            var agendamentos = _databaseService.ObterAgendamentosPorPeriodo(dataInicio, dataFim)
                .Where(a => a.Status == status).ToList();
            
            return new ReportData
            {
                Titulo = $"Relatório por Status - {status}",
                DataGeracao = DateTime.Now,
                Periodo = $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}",
                TotalAgendamentos = agendamentos.Count,
                AgendamentosConcluidos = agendamentos.Count(a => a.Status == "Finalizado" || a.Status == "Entregue"),
                AgendamentosPendentes = agendamentos.Count(a => a.Status == "Agendado" || a.Status == "Confirmado"),
                AgendamentosEmAndamento = agendamentos.Count(a => a.Status == "Em Andamento"),
                ValorTotalEstimado = agendamentos.Sum(a => a.ValorEstimado),
                ValorTotalRealizado = agendamentos.Where(a => a.Status == "Finalizado" || a.Status == "Entregue").Sum(a => a.ValorReal),
                Agendamentos = agendamentos
            };
        }

        public ReportData GerarRelatorioProdutividade(DateTime dataInicio, DateTime dataFim)
        {
            var agendamentos = _databaseService.ObterAgendamentosPorPeriodo(dataInicio, dataFim);
            
            var produtividadePorTecnico = agendamentos
                .GroupBy(a => a.TecnicoNome)
                .Select(g => new ProdutividadeTecnico
                {
                    TecnicoNome = g.Key,
                    TotalAgendamentos = g.Count(),
                    AgendamentosConcluidos = g.Count(a => a.Status == "Finalizado" || a.Status == "Entregue"),
                    ValorTotal = g.Sum(a => a.ValorReal > 0 ? a.ValorReal : a.ValorEstimado),
                    TempoMedio = g.Where(a => a.HoraInicio.HasValue && a.HoraTermino.HasValue)
                        .Select(a => (a.HoraTermino!.Value - a.HoraInicio!.Value).TotalHours)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .ToList();
            
            return new ReportData
            {
                Titulo = "Relatório de Produtividade",
                DataGeracao = DateTime.Now,
                Periodo = $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}",
                TotalAgendamentos = agendamentos.Count,
                AgendamentosConcluidos = agendamentos.Count(a => a.Status == "Finalizado" || a.Status == "Entregue"),
                AgendamentosPendentes = agendamentos.Count(a => a.Status == "Agendado" || a.Status == "Confirmado"),
                AgendamentosEmAndamento = agendamentos.Count(a => a.Status == "Em Andamento"),
                ValorTotalEstimado = agendamentos.Sum(a => a.ValorEstimado),
                ValorTotalRealizado = agendamentos.Where(a => a.Status == "Finalizado" || a.Status == "Entregue").Sum(a => a.ValorReal),
                Agendamentos = agendamentos.ToList(),
                ProdutividadeTecnicos = produtividadePorTecnico
            };
        }

        public string ExportarParaCSV(ReportData reportData)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine($"Relatório: {reportData.Titulo}");
            csv.AppendLine($"Período: {reportData.Periodo}");
            csv.AppendLine($"Data de Geração: {reportData.DataGeracao:dd/MM/yyyy HH:mm}");
            csv.AppendLine();
            csv.AppendLine("Resumo");
            csv.AppendLine($"Total de Agendamentos;{reportData.TotalAgendamentos}");
            csv.AppendLine($"Concluídos;{reportData.AgendamentosConcluidos}");
            csv.AppendLine($"Pendentes;{reportData.AgendamentosPendentes}");
            csv.AppendLine($"Em Andamento;{reportData.AgendamentosEmAndamento}");
            csv.AppendLine($"Valor Total Estimado;{reportData.ValorTotalEstimado:F2}");
            csv.AppendLine($"Valor Total Realizado;{reportData.ValorTotalRealizado:F2}");
            csv.AppendLine();
            csv.AppendLine("Detalhes dos Agendamentos");
            csv.AppendLine("Número;Cliente;Veículo;Placa;Serviço;Técnico;Data;Horário;Status;Prioridade;Valor Estimado;Valor Realizado");
            
            foreach (var agendamento in reportData.Agendamentos)
            {
                csv.AppendLine($"{agendamento.Numero};{agendamento.ClienteNome};{agendamento.VeiculoModelo};{agendamento.VeiculoPlaca};" +
                              $"{agendamento.TipoServico};{agendamento.TecnicoNome};{agendamento.DataAgendamento:dd/MM/yyyy};" +
                              $"{agendamento.HoraInicio:HH:mm} - {agendamento.HoraTermino:HH:mm};{agendamento.Status};{agendamento.Prioridade};" +
                              $"{agendamento.ValorEstimado:F2};{agendamento.ValorReal:F2}");
            }
            
            return csv.ToString();
        }

        public string ExportarParaHTML(ReportData reportData)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>" + reportData.Titulo + "</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { color: #2C3E50; }");
            html.AppendLine(".header { background: #2C3E50; color: white; padding: 20px; border-radius: 10px; margin-bottom: 20px; }");
            html.AppendLine(".summary { background: #ECF0F1; padding: 15px; border-radius: 10px; margin-bottom: 20px; }");
            html.AppendLine(".summary-item { margin: 5px 0; }");
            html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            html.AppendLine("th { background: #3498DB; color: white; padding: 12px; text-align: left; }");
            html.AppendLine("td { border: 1px solid #ddd; padding: 10px; }");
            html.AppendLine("tr:nth-child(even) { background: #f2f2f2; }");
            html.AppendLine(".status-agendado { background: #95A5A6; color: white; padding: 4px 8px; border-radius: 4px; }");
            html.AppendLine(".status-confirmado { background: #3498DB; color: white; padding: 4px 8px; border-radius: 4px; }");
            html.AppendLine(".status-em-andamento { background: #F39C12; color: white; padding: 4px 8px; border-radius: 4px; }");
            html.AppendLine(".status-finalizado { background: #27AE60; color: white; padding: 4px 8px; border-radius: 4px; }");
            html.AppendLine(".status-cancelado { background: #E74C3C; color: white; padding: 4px 8px; border-radius: 4px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>" + reportData.Titulo + "</h1>");
            html.AppendLine("<p>Período: " + reportData.Periodo + "</p>");
            html.AppendLine("<p>Data de Geração: " + reportData.DataGeracao.ToString("dd/MM/yyyy HH:mm") + "</p>");
            html.AppendLine("</div>");
            
            // Summary
            html.AppendLine("<div class='summary'>");
            html.AppendLine("<h2>Resumo</h2>");
            html.AppendLine("<div class='summary-item'><strong>Total de Agendamentos:</strong> " + reportData.TotalAgendamentos + "</div>");
            html.AppendLine("<div class='summary-item'><strong>Concluídos:</strong> " + reportData.AgendamentosConcluidos + "</div>");
            html.AppendLine("<div class='summary-item'><strong>Pendentes:</strong> " + reportData.AgendamentosPendentes + "</div>");
            html.AppendLine("<div class='summary-item'><strong>Em Andamento:</strong> " + reportData.AgendamentosEmAndamento + "</div>");
            html.AppendLine("<div class='summary-item'><strong>Valor Total Estimado:</strong> R$ " + reportData.ValorTotalEstimado.ToString("F2") + "</div>");
            html.AppendLine("<div class='summary-item'><strong>Valor Total Realizado:</strong> R$ " + reportData.ValorTotalRealizado.ToString("F2") + "</div>");
            html.AppendLine("</div>");
            
            // Table
            html.AppendLine("<h2>Detalhes dos Agendamentos</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Número</th>");
            html.AppendLine("<th>Cliente</th>");
            html.AppendLine("<th>Veículo</th>");
            html.AppendLine("<th>Placa</th>");
            html.AppendLine("<th>Serviço</th>");
            html.AppendLine("<th>Técnico</th>");
            html.AppendLine("<th>Data</th>");
            html.AppendLine("<th>Horário</th>");
            html.AppendLine("<th>Status</th>");
            html.AppendLine("<th>Prioridade</th>");
            html.AppendLine("<th>Valor Estimado</th>");
            html.AppendLine("<th>Valor Realizado</th>");
            html.AppendLine("</tr>");
            
            foreach (var agendamento in reportData.Agendamentos)
            {
                var statusClass = agendamento.Status.ToLower().Replace(" ", "-").Replace("ã", "a").Replace("ç", "c");
                html.AppendLine("<tr>");
                html.AppendLine("<td>" + agendamento.Numero + "</td>");
                html.AppendLine("<td>" + agendamento.ClienteNome + "</td>");
                html.AppendLine("<td>" + agendamento.VeiculoModelo + "</td>");
                html.AppendLine("<td>" + agendamento.VeiculoPlaca + "</td>");
                html.AppendLine("<td>" + agendamento.TipoServico + "</td>");
                html.AppendLine("<td>" + agendamento.TecnicoNome + "</td>");
                html.AppendLine("<td>" + agendamento.DataAgendamento.ToString("dd/MM/yyyy") + "</td>");
                html.AppendLine("<td>" + (agendamento.HoraInicio?.ToString("HH:mm") ?? "") + " - " + (agendamento.HoraTermino?.ToString("HH:mm") ?? "") + "</td>");
                html.AppendLine("<td><span class='status-" + statusClass + "'>" + agendamento.Status + "</span></td>");
                html.AppendLine("<td>" + agendamento.Prioridade + "</td>");
                html.AppendLine("<td>R$ " + agendamento.ValorEstimado.ToString("F2") + "</td>");
                html.AppendLine("<td>R$ " + agendamento.ValorReal.ToString("F2") + "</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</table>");
            
            // Footer
            html.AppendLine("<p style='margin-top: 30px; color: #7F8C8D; font-size: 12px;'>");
            html.AppendLine("Gerado pelo Sistema Primo Auto Elétrica - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            html.AppendLine("</p>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        public void ExportarParaPDF(ReportData reportData, string caminhoArquivo)
        {
            var documento = new PdfDocument();
            documento.Info.Title = reportData.Titulo;
            documento.Info.Author = "Primo Auto Eletrica";
            documento.Info.Subject = "Relatorio operacional de agendamentos";

            var pagina = documento.AddPage();
            pagina.Size = PdfSharpCore.PageSize.A4;
            pagina.Orientation = PdfSharpCore.PageOrientation.Portrait;

            var grafico = XGraphics.FromPdfPage(pagina);
            var fonteTitulo = new XFont("Arial", 18, XFontStyle.Bold);
            var fonteSecao = new XFont("Arial", 12, XFontStyle.Bold);
            var fonteTexto = new XFont("Arial", 9, XFontStyle.Regular);

            var y = 30d;
            grafico.DrawString(reportData.Titulo, fonteTitulo, XBrushes.DarkBlue, new XRect(30, y, pagina.Width - 60, 28), XStringFormats.TopLeft);
            y += 28;
            grafico.DrawString($"Periodo: {reportData.Periodo}", fonteTexto, XBrushes.DimGray, 30, y);
            y += 14;
            grafico.DrawString($"Gerado em: {reportData.DataGeracao:dd/MM/yyyy HH:mm}", fonteTexto, XBrushes.DimGray, 30, y);
            y += 22;

            grafico.DrawString("Resumo", fonteSecao, XBrushes.Black, 30, y);
            y += 18;
            grafico.DrawString($"Total: {reportData.TotalAgendamentos}", fonteTexto, XBrushes.Black, 30, y);
            y += 14;
            grafico.DrawString($"Concluidos: {reportData.AgendamentosConcluidos}", fonteTexto, XBrushes.Black, 30, y);
            y += 14;
            grafico.DrawString($"Pendentes: {reportData.AgendamentosPendentes}", fonteTexto, XBrushes.Black, 30, y);
            y += 14;
            grafico.DrawString($"Em andamento: {reportData.AgendamentosEmAndamento}", fonteTexto, XBrushes.Black, 30, y);
            y += 14;
            grafico.DrawString($"Valor estimado: {reportData.ValorTotalEstimado:C}", fonteTexto, XBrushes.Black, 30, y);
            y += 14;
            grafico.DrawString($"Valor realizado: {reportData.ValorTotalRealizado:C}", fonteTexto, XBrushes.Black, 30, y);
            y += 24;

            grafico.DrawString("Agendamentos", fonteSecao, XBrushes.Black, 30, y);
            y += 18;

            foreach (var agendamento in reportData.Agendamentos.Take(18))
            {
                var linha = $"{agendamento.DataAgendamento:dd/MM}  {agendamento.ClienteNome}  |  {agendamento.TipoServico}  |  {agendamento.Status}  |  {agendamento.ValorEstimado:C}";
                grafico.DrawString(linha, fonteTexto, XBrushes.Black, 30, y);
                y += 13;
            }

            if (reportData.Agendamentos.Count > 18)
            {
                y += 8;
                grafico.DrawString($"... {reportData.Agendamentos.Count - 18} registro(s) adicional(is) omitido(s) no PDF resumido.", fonteTexto, XBrushes.DimGray, 30, y);
            }

            documento.Save(caminhoArquivo);
        }

        public void SalvarArquivo(string conteudo, string caminhoArquivo)
        {
            try
            {
                File.WriteAllText(caminhoArquivo, conteudo, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar arquivo: {ex.Message}", ex);
            }
        }

        public void ImprimirRelatorio(ReportData reportData)
        {
            // Implementação para impressão direta
            // Pode ser integrado com PrintDialog do WPF
        }
    }

    public class ReportData
    {
        public string Titulo { get; set; } = string.Empty;
        public DateTime DataGeracao { get; set; }
        public string Periodo { get; set; } = string.Empty;
        public int TotalAgendamentos { get; set; }
        public int AgendamentosConcluidos { get; set; }
        public int AgendamentosPendentes { get; set; }
        public int AgendamentosEmAndamento { get; set; }
        public decimal ValorTotalEstimado { get; set; }
        public decimal ValorTotalRealizado { get; set; }
        public List<Agendamento> Agendamentos { get; set; } = new();
        public List<ProdutividadeTecnico> ProdutividadeTecnicos { get; set; } = new();
    }

    public class ProdutividadeTecnico
    {
        public string TecnicoNome { get; set; } = string.Empty;
        public int TotalAgendamentos { get; set; }
        public int AgendamentosConcluidos { get; set; }
        public decimal ValorTotal { get; set; }
        public double TempoMedio { get; set; }
    }
}
