using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Acoes comerciais: gerar PDFs (checklist/garantia/laudo) e abrir WhatsApp.
    /// </summary>
    public static class CommercialDocumentActions
    {
        public static string PastaTempDocumentos()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "DocumentosComerciais");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static void AbrirArquivo(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            SecureProcessLauncher.OpenFileOrDirectory(path);
        }

        public static void EnviarWhatsAppArquivo(string? telefone, string mensagem, string? caminhoArquivo = null)
        {
            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(telefone, out var wa))
            {
                MessageBox.Show(
                    "Cliente sem WhatsApp valido cadastrado.",
                    UiText.T("Warning"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var texto = mensagem ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(caminhoArquivo) && File.Exists(caminhoArquivo))
            {
                texto = texto.Trim()
                    + "\n\nDocumento gerado: " + Path.GetFileName(caminhoArquivo)
                    + "\n(Anexe o PDF manualmente no WhatsApp)\n"
                    + caminhoArquivo;
            }

            SecureProcessLauncher.OpenWhatsAppLink($"https://wa.me/{wa}?text={Uri.EscapeDataString(texto)}");
        }


        public static string GerarOrcamentoPdf(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento);
            var path = Path.Combine(PastaTempDocumentos(), $"orcamento-{Sanitize(orcamento.Numero)}-{DateTime.Now:yyyyMMddHHmmss}.pdf");
            new OrcamentoPdfService().GerarPdfOrcamento(orcamento, path);
            return path;
        }
        public static string GerarChecklistOs(OrdemServico ordem)
        {
            var path = Path.Combine(PastaTempDocumentos(), $"checklist-{Sanitize(ordem.Numero)}-{DateTime.Now:yyyyMMddHHmmss}.pdf");
            return new DocumentoPdfService().GerarChecklist(ordem, path);
        }

        public static string GerarTermoGarantiaOs(OrdemServico ordem)
        {
            AplicarGarantiaPadraoSeVazia(ordem);
            var path = Path.Combine(PastaTempDocumentos(), $"garantia-{Sanitize(ordem.Numero)}-{DateTime.Now:yyyyMMddHHmmss}.pdf");
            return new DocumentoPdfService().GerarTermoGarantia(ordem, path);
        }

        public static string GerarLaudo(
            DiagnosticoGuiadoRoteiro? roteiro,
            ProntuarioEletricoVeiculo? prontuario,
            string? tecnico,
            OrdemServico? ordem = null)
        {
            var codigo = roteiro?.Codigo ?? ordem?.Numero ?? "laudo";
            var path = Path.Combine(PastaTempDocumentos(), $"laudo-{Sanitize(codigo)}-{DateTime.Now:yyyyMMddHHmmss}.pdf");
            return new DocumentoPdfService().GerarLaudoEletrico(roteiro, prontuario, tecnico, path, ordem);
        }

        public static void NormalizarGarantiaEmRelacaoAEntrega(OrdemServico ordem, int diasPadrao = 90)
        {
            if (ordem == null) return;

            var dias = diasPadrao > 0 ? diasPadrao : 90;
            try
            {
                var svc = App.Services.GetService(typeof(SystemConfigurationService)) as SystemConfigurationService;
                if (svc != null)
                {
                    var conf = svc.LoadOrCreate(App.RuntimeAppDataPath);
                    if (conf.DefaultWarrantyDays > 0) dias = conf.DefaultWarrantyDays;
                }
            }
            catch (Exception) { /* best-effort: fallback para diasPadrao se config indisponível */ }

            // Usa instante (nao so Date): DatePicker costuma gravar 18:00 e DataEntrega = Now pode ser depois no mesmo dia.
            var referencia = new[] { ordem.DataEntrega, ordem.DataConclusao, (DateTime?)ordem.DataAbertura, DateTime.Now }
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .Max();

            if (!ordem.GarantiaValidaAte.HasValue || ordem.GarantiaValidaAte.Value < referencia)
            {
                var alvo = referencia.Date.AddDays(dias).AddHours(18);
                if (alvo < referencia)
                {
                    alvo = referencia.AddDays(dias);
                }

                ordem.GarantiaValidaAte = alvo;
            }
        }

        public static void AplicarGarantiaPadraoSeVazia(OrdemServico ordem)
        {
            if (ordem == null || ordem.GarantiaValidaAte.HasValue)
            {
                return;
            }

            var dias = 90;
            var tpl = "Servico com garantia padrao de {DiasGarantia} dias.";
            try
            {
                var svc = App.Services.GetService(typeof(SystemConfigurationService)) as SystemConfigurationService;
                if (svc != null)
                {
                    var conf = svc.LoadOrCreate(App.RuntimeAppDataPath);
                    dias = conf.DefaultWarrantyDays;
                    if (!string.IsNullOrWhiteSpace(conf.MessageTemplateGarantia))
                    {
                        tpl = conf.MessageTemplateGarantia;
                    }
                }
            }
            catch
            {
            }

            if (dias <= 0)
            {
                return;
            }

            var baseDate = new[]
                {
                    ordem.DataEntrega,
                    ordem.DataConclusao,
                    (DateTime?)ordem.DataAbertura,
                    DateTime.Now
                }
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .DefaultIfEmpty(DateTime.Now)
                .Max();
            ordem.GarantiaValidaAte = baseDate.Date.AddDays(dias);
            if (string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes))
            {
                ordem.GarantiaObservacoes = tpl.Replace("{DiasGarantia}", dias.ToString());
            }
        }

        private static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "doc";
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '-');
            }

            return value.Trim();
        }
    }
}
