from pathlib import Path
import re

# --- 1) Add GerarLaudoEletrico to DocumentoPdfService ---
p = Path('PrimoAutoEletrica/Services/DocumentoPdfService.cs')
t = p.read_text(encoding='utf-8')
if 'GerarLaudoEletrico' in t:
    print('Laudo already exists')
else:
    # ensure using for AutoEletrica models - Models already imported via OrdemServico
    if 'using PrimoAutoEletrica.Models;' not in t:
        t = 'using PrimoAutoEletrica.Models;\n' + t
    method = r'''
        public string GerarLaudoEletrico(
            DiagnosticoGuiadoRoteiro? roteiro,
            ProntuarioEletricoVeiculo? prontuario,
            string? tecnico,
            string caminhoArquivo,
            OrdemServico? ordem = null)
        {
            roteiro ??= new DiagnosticoGuiadoRoteiro
            {
                Titulo = "Diagnostico eletrico",
                Sintoma = "Nao informado",
                Resultado = "Sem resultado registrado",
                Conclusao = "Sem conclusao registrada"
            };

            var veiculo = prontuario?.Veiculo
                ?? ordem?.VeiculoDescricaoSnapshot
                ?? "Veiculo nao informado";
            var placa = prontuario?.Placa
                ?? ordem?.PlacaSnapshot
                ?? "-";

            var testes = (roteiro.SequenciaTestes ?? new System.Collections.Generic.List<string>())
                .Select((x, i) => $"{i + 1}. {x}")
                .DefaultIfEmpty("Sem sequencia de testes registrada.")
                .ToArray();
            var causas = (roteiro.PossiveisCausas ?? new System.Collections.Generic.List<string>())
                .Select(x => $"- {x}")
                .DefaultIfEmpty("- Nao informado")
                .ToArray();
            var valores = (roteiro.ValoresEsperados ?? new System.Collections.Generic.List<string>())
                .Select(x => $"- {x}")
                .DefaultIfEmpty("- Nao informado")
                .ToArray();
            var pecas = (roteiro.PecasSugeridas ?? new System.Collections.Generic.List<string>())
                .Select(x => $"- {x}")
                .DefaultIfEmpty("- Nenhuma peca sugerida")
                .ToArray();
            var servicos = (roteiro.ServicosSugeridos ?? new System.Collections.Generic.List<string>())
                .Select(x => $"- {x}")
                .DefaultIfEmpty("- Nenhum servico sugerido")
                .ToArray();

            return GerarDocumento(
                "LAUDO TECNICO - AUTO ELETRICA",
                caminhoArquivo,
                new[]
                {
                    Secao("Identificacao", new[]
                    {
                        Linha("Data", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                        Linha("Tecnico", string.IsNullOrWhiteSpace(tecnico) ? "Nao informado" : tecnico),
                        Linha("OS", ordem?.Numero ?? "-"),
                        Linha("Cliente", ordem?.ClienteNomeSnapshot ?? "-"),
                        Linha("Veiculo", veiculo),
                        Linha("Placa", placa),
                        Linha("Sistema", prontuario?.SistemaEletrico ?? "-")
                    }),
                    Secao("Roteiro / sintoma", new[]
                    {
                        Linha("Codigo", roteiro.Codigo),
                        Linha("Titulo", roteiro.Titulo),
                        Linha("Sintoma", roteiro.Sintoma)
                    }),
                    Secao("Possiveis causas", causas),
                    Secao("Sequencia de testes", testes),
                    Secao("Valores esperados", valores),
                    Secao("Resultado e conclusao", new[]
                    {
                        Linha("Resultado", string.IsNullOrWhiteSpace(roteiro.Resultado) ? "Nao informado" : roteiro.Resultado),
                        Linha("Conclusao", string.IsNullOrWhiteSpace(roteiro.Conclusao) ? "Nao informado" : roteiro.Conclusao)
                    }),
                    Secao("Servicos sugeridos", servicos),
                    Secao("Pecas sugeridas", pecas),
                    Secao("Observacoes", new[]
                    {
                        "Laudo gerado pelo modulo Auto Eletrica Tecnica do PrimoAutoEletrica.",
                        "Documento informativo para o cliente; nao substitui laudo pericial."
                    })
                });
        }

'''
    # insert before GerarTermoGarantia or after GerarChecklist
    anchor = 'public string GerarTermoGarantia'
    if anchor not in t:
        raise SystemExit('anchor GerarTermoGarantia missing')
    t = t.replace(anchor, method + '        ' + anchor, 1)
    # need System.Linq if not present
    if 'using System.Linq;' not in t:
        t = t.replace('using System;', 'using System;\nusing System.Linq;', 1)
    p.write_text(t, encoding='utf-8')
    print('ADDED GerarLaudoEletrico')

# --- 2) CommercialDocumentActions helper ---
helper = Path('PrimoAutoEletrica/Services/CommercialDocumentActions.cs')
helper.write_text(r'''using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Acoes comerciais reutilizaveis: gerar PDFs (checklist/garantia/laudo) e abrir WhatsApp.
    /// </summary>
    public static class CommercialDocumentActions
    {
        public static string AskSavePdf(string nomeSugerido)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = nomeSugerido,
                AddExtension = true,
                DefaultExt = ".pdf"
            };
            return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
        }

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
                texto = texto.Trim() + $"\n\nDocumento gerado: {Path.GetFileName(caminhoArquivo)}\n(Anexe o PDF manualmente no WhatsApp: {caminhoArquivo})";
            }

            SecureProcessLauncher.OpenWhatsAppLink($"https://wa.me/{wa}?text={Uri.EscapeDataString(texto)}");
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

        public static void AplicarGarantiaPadraoSeVazia(OrdemServico ordem)
        {
            if (ordem == null || ordem.GarantiaValidaAte.HasValue)
            {
                return;
            }

            var cfg = App.Services.GetService(typeof(SystemConfigurationService)) as SystemConfigurationService;
            var dias = cfg?.GetConfiguration()?.DefaultWarrantyDays
                ?? new SystemConfiguration().DefaultWarrantyDays;
            if (dias <= 0)
            {
                return;
            }

            var baseDate = ordem.DataEntrega ?? ordem.DataConclusao ?? DateTime.Now;
            ordem.GarantiaValidaAte = baseDate.Date.AddDays(dias);
            if (string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes))
            {
                var tpl = cfg?.GetConfiguration()?.MessageTemplateGarantia
                    ?? "Servico com garantia padrao de {DiasGarantia} dias.";
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
'''.replace('App.Services.GetService', 'Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService(App.Services,') 
# fix the GetService hack - write clean version
helper.write_text('''using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Acoes comerciais reutilizaveis: gerar PDFs (checklist/garantia/laudo) e abrir WhatsApp.
    /// </summary>
    public static class CommercialDocumentActions
    {
        public static string AskSavePdf(string nomeSugerido)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = nomeSugerido,
                AddExtension = true,
                DefaultExt = ".pdf"
            };
            return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
        }

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
                    + "\\n\\nDocumento gerado: " + Path.GetFileName(caminhoArquivo)
                    + "\\n(Anexe o PDF manualmente no WhatsApp)\\n" + caminhoArquivo;
            }

            SecureProcessLauncher.OpenWhatsAppLink($"https://wa.me/{wa}?text={Uri.EscapeDataString(texto)}");
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

        public static void AplicarGarantiaPadraoSeVazia(OrdemServico ordem)
        {
            if (ordem == null || ordem.GarantiaValidaAte.HasValue)
            {
                return;
            }

            var cfg = App.Services.GetService<SystemConfigurationService>();
            var conf = cfg?.GetConfiguration() ?? new SystemConfiguration();
            var dias = conf.DefaultWarrantyDays;
            if (dias <= 0)
            {
                return;
            }

            var baseDate = ordem.DataEntrega ?? ordem.DataConclusao ?? DateTime.Now;
            ordem.GarantiaValidaAte = baseDate.Date.AddDays(dias);
            if (string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes))
            {
                var tpl = string.IsNullOrWhiteSpace(conf.MessageTemplateGarantia)
                    ? "Servico com garantia padrao de {DiasGarantia} dias."
                    : conf.MessageTemplateGarantia;
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
''', encoding='utf-8')
print('WROTE CommercialDocumentActions')
