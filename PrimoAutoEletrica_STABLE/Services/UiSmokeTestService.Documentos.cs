using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks da Fase 11: documentos, PDF, impressao e exportacao.

        private void RunDocumentosPdfImpressaoExportacaoChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Documentos:PdfPadronizadosObrigatorios", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("Fixture do smoke nao inicializada para documentos.");
                var outputDir = Path.Combine(App.RuntimeLogDirectory, "documentos-smoke", DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                Directory.CreateDirectory(outputDir);

                var service = new DocumentoPdfService(new BusinessConfiguration
                {
                    CompanyDisplayName = "Primo Auto Eletrica Smoke",
                    CompanyLegalName = "Primo Auto Eletrica Smoke LTDA",
                    CompanyDocument = "12.345.678/0001-90",
                    CompanyPhone = "(11) 3333-0000",
                    CompanyAddress = "Rua Smoke, 100 - Centro",
                    ReceiptHeader = "Comprovante nao fiscal",
                    ReceiptFooter = "Obrigado pela preferencia."
                });

                var ordem = App.Repositories.OrdensServico.ObterPorId(fixture.OrdemServico.Id)
                    ?? throw new InvalidOperationException("OS sintetica nao encontrada para gerar documentos.");
                PrepararOrdemParaDocumentos(ordem);
                App.Repositories.OrdensServico.Atualizar(ordem);

                var orcamento = new OrcamentoDatabaseService().ObterOrcamentoPorId(fixture.Orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento sintetico nao encontrado para gerar documentos.");
                orcamento.Cliente = fixture.Cliente;
                orcamento.Veiculo = fixture.Veiculo;

                var arquivos = new Dictionary<string, string>
                {
                    ["Orcamento"] = service.GerarOrcamento(orcamento, Path.Combine(outputDir, "01-orcamento.pdf")),
                    ["OS"] = service.GerarOrdemServico(ordem, Path.Combine(outputDir, "02-os.pdf")),
                    ["Checklist"] = service.GerarChecklist(ordem, Path.Combine(outputDir, "03-checklist.pdf")),
                    ["Recibo"] = service.GerarRecibo("REC-SMOKE", fixture.Cliente, 350m, "Servicos eletricos smoke", "PIX", Path.Combine(outputDir, "04-recibo.pdf")),
                    ["ComprovanteVenda"] = service.GerarComprovanteVenda(fixture.Venda, Path.Combine(outputDir, "05-comprovante-venda.pdf")),
                    ["TermoGarantia"] = service.GerarTermoGarantia(ordem, Path.Combine(outputDir, "06-termo-garantia.pdf")),
                    ["TermoAutorizacao"] = service.GerarTermoAutorizacao(ordem, Path.Combine(outputDir, "07-termo-autorizacao.pdf")),
                    ["RelatorioFinanceiro"] = service.GerarRelatorioFinanceiro(CriarDadosFinanceirosDocumento(), Path.Combine(outputDir, "08-relatorio-financeiro.pdf"), "Financeiro smoke")
                };

                foreach (var arquivo in arquivos)
                {
                    ValidarPdfDocumento(arquivo.Key, arquivo.Value);
                }
            });

            RunCheck(result, "Documentos:RoteiroImpressaoManual", () =>
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Docs", "IMPRESSAO_QA.md");
                if (!File.Exists(path))
                {
                    path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Docs", "IMPRESSAO_QA.md"));
                }

                if (!File.Exists(path))
                {
                    throw new InvalidOperationException("Roteiro de QA de impressao nao foi encontrado.");
                }

                var conteudo = File.ReadAllText(path, Encoding.UTF8);
                foreach (var termo in new[] { "Impressora A4", "Impressora termica futura", "margens", "quebra", "fonte", "CSV" })
                {
                    if (!conteudo.Contains(termo, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Roteiro de impressao nao contem o item obrigatorio: {termo}.");
                    }
                }
            });
        }

        private static void ValidarPdfDocumento(string nome, string path)
        {
            EnsureGeneratedFile(path, $"PDF padronizado {nome}");

            var assinatura = File.ReadAllBytes(path).Take(5).ToArray();
            if (assinatura.Length < 5 || Encoding.ASCII.GetString(assinatura) != "%PDF-")
            {
                throw new InvalidOperationException($"PDF padronizado {nome} nao possui assinatura PDF valida.");
            }

            using var pdf = PdfReader.Open(path, PdfDocumentOpenMode.ReadOnly);
            if (pdf.PageCount == 0 ||
                string.IsNullOrWhiteSpace(pdf.Info.Title) ||
                !pdf.Info.Title.Contains("Primo Auto Eletrica", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"PDF padronizado {nome} nao contem metadados/paginas validas.");
            }
        }

        private static List<DadoFinanceiro> CriarDadosFinanceirosDocumento()
        {
            return new List<DadoFinanceiro>
            {
                new()
                {
                    Data = DateTime.Today,
                    Tipo = "Receita",
                    Categoria = "OS",
                    Descricao = "Recebimento de servico eletrico",
                    Valor = 480m,
                    FormaPagamento = "PIX",
                    Usuario = "Smoke"
                },
                new()
                {
                    Data = DateTime.Today,
                    Tipo = "Despesa",
                    Categoria = "Pecas",
                    Descricao = "Compra de regulador alternador",
                    Valor = 120m,
                    FormaPagamento = "Boleto",
                    Usuario = "Smoke"
                }
            };
        }

        private static void PrepararOrdemParaDocumentos(OrdemServico ordem)
        {
            ordem.ClienteNomeSnapshot = string.IsNullOrWhiteSpace(ordem.ClienteNomeSnapshot)
                ? "Cliente Smoke"
                : ordem.ClienteNomeSnapshot;
            ordem.VeiculoDescricaoSnapshot = string.IsNullOrWhiteSpace(ordem.VeiculoDescricaoSnapshot)
                ? "Veiculo Smoke"
                : ordem.VeiculoDescricaoSnapshot;
            ordem.PlacaSnapshot = string.IsNullOrWhiteSpace(ordem.PlacaSnapshot)
                ? "SMK1A23"
                : ordem.PlacaSnapshot;
            ordem.ChecklistEntrada = "Bateria testada; Alternador medido; Fotos registradas";
            ordem.ChecklistSaida = "Carga conferida; Aterramentos reapertados; Cliente orientado";
            ordem.FotosAntes = "foto-entrada.png";
            ordem.FotosDepois = "foto-saida.png";
            ordem.TermoAutorizacao = "Cliente autoriza diagnostico eletrico e execucao dos servicos aprovados.";
            ordem.GarantiaObservacoes = "Garantia de 90 dias para o servico executado.";
            ordem.GarantiaValidaAte = DateTime.Today.AddDays(90);
            ordem.AprovadaCliente = true;
            ordem.MetodoAprovacao = "Assinatura digital/manual";
        }
    }
}
