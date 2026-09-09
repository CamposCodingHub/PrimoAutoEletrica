using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Pré-visualização técnica (NÃO é DANFE).
    /// </summary>
    public sealed class FiscalNFePreview
    {
        public string Title { get; init; } = "NF-e — Homologação";
        public string Ambiente { get; init; } = "Homologacao";
        public string EmitenteResumo { get; init; } = string.Empty;
        public string DestinatarioResumo { get; init; } = string.Empty;
        public IReadOnlyList<string> LinhasItens { get; init; } = Array.Empty<string>();
        public string TotaisResumo { get; init; } = string.Empty;
        public string TextoCompleto { get; init; } = string.Empty;
        public FiscalValidationResult Validation { get; init; } = new();
    }

    public sealed class FiscalNFePreviewBuilder
    {
        private readonly FiscalDocumentValidator _validator;

        public FiscalNFePreviewBuilder(FiscalDocumentValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public FiscalNFePreview Build(
            FiscalNFeDocument document,
            FiscalConfiguration configuration,
            bool requireProviderCredential = false)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(configuration);

            var validation = _validator.ValidateForHomologEmission(
                document, configuration, requireProviderCredential);

            var culture = CultureInfo.GetCultureInfo("pt-BR");
            var emitente = document.Emitente;
            var dest = document.Destinatario;

            var emitenteResumo =
                $"{emitente.RazaoSocial} | CNPJ {emitente.Cnpj} | IE {emitente.InscricaoEstadual}\n" +
                $"{emitente.Logradouro}, {emitente.Numero} — {emitente.Bairro}\n" +
                $"{emitente.Municipio}/{emitente.Uf} CEP {emitente.Cep} | Série {emitente.SerieNFe}";

            var destResumo =
                $"{dest.Nome} | {(dest.IsCnpj ? "CNPJ" : "CPF")} {dest.Documento}\n" +
                $"{dest.Logradouro}, {dest.Numero} — {dest.Bairro}\n" +
                $"{dest.Municipio}/{dest.Uf} CEP {dest.Cep}";

            var itens = document.Itens.Select(i =>
                $"{i.NumeroItem}. {i.Descricao} | NCM {i.Ncm} | CFOP {i.Cfop} | " +
                $"Qtd {i.Quantidade.ToString("N3", culture)} {i.Unidade} | " +
                $"Unit {i.ValorUnitario.ToString("C", culture)} | Total {i.ValorTotal.ToString("C", culture)} | " +
                $"CST/CSOSN {i.IcmsSituacaoTributaria} Orig {i.IcmsOrigem}"
            ).ToList();

            var totais =
                $"Produtos: {document.ValorProdutos.ToString("C", culture)}\n" +
                $"Descontos: {document.ValorDesconto.ToString("C", culture)}\n" +
                $"Total: {document.ValorTotal.ToString("C", culture)}";

            var sb = new StringBuilder();
            sb.AppendLine("NF-e — Homologação (pré-visualização técnica — NÃO é DANFE)");
            sb.AppendLine($"Ambiente: {document.Environment}");
            sb.AppendLine();
            sb.AppendLine("Emitente");
            sb.AppendLine(emitenteResumo);
            sb.AppendLine();
            sb.AppendLine("Destinatário");
            sb.AppendLine(destResumo);
            sb.AppendLine();
            sb.AppendLine("Itens");
            sb.AppendLine(new string('-', 48));
            foreach (var line in itens)
            {
                sb.AppendLine(line);
            }

            sb.AppendLine();
            sb.AppendLine("Totais");
            sb.AppendLine(new string('-', 48));
            sb.AppendLine(totais);
            sb.AppendLine();
            sb.AppendLine(validation.IsValid
                ? "Validação: OK (sem envio ao provider nesta prévia)."
                : "Validação: INVÁLIDA — emissão bloqueada.\n" + validation.Summarize());

            return new FiscalNFePreview
            {
                Ambiente = document.Environment.ToString(),
                EmitenteResumo = emitenteResumo,
                DestinatarioResumo = destResumo,
                LinhasItens = itens,
                TotaisResumo = totais,
                TextoCompleto = sb.ToString(),
                Validation = validation
            };
        }
    }
}
