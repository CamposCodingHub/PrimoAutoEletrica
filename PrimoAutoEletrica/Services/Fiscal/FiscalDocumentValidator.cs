using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Services.Fiscal
{
    public sealed class FiscalDocumentValidator
    {
        private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);

        public FiscalValidationResult ValidateForHomologEmission(
            FiscalNFeDocument document,
            FiscalConfiguration configuration,
            bool requireProviderCredential)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(configuration);

            var result = new FiscalValidationResult();

            if (document.Environment == FiscalEnvironment.Production)
            {
                result.Add("FISCAL-PROD-BLOCKED", "Emissao em PRODUCAO esta bloqueada nesta fase.");
                return result;
            }

            if (document.Environment != FiscalEnvironment.Homologation &&
                document.Environment != FiscalEnvironment.Development)
            {
                result.Add("FISCAL-ENV-INVALID", "Ambiente fiscal invalido para homologacao.");
            }

            if (document.VendaId is null || document.VendaId == Guid.Empty)
            {
                result.Add("FISCAL-ORIGIN-MISSING", "Origem VendaId ausente.", "VendaId");
            }

            ValidateEmitente(document.Emitente, result);
            ValidateDestinatario(document.Destinatario, result);
            ValidateItens(document, result);
            ValidateTotais(document, result);

            if (requireProviderCredential)
            {
                if (!configuration.LiveHttpEnabled)
                {
                    result.Add(
                        "FISCAL-HTTP-OFF",
                        "HTTP live Focus desligado. Habilite LiveHttpEnabled apenas para Homologacao no fiscal-foundation.json.",
                        "LiveHttpEnabled");
                }

                if (string.IsNullOrWhiteSpace(configuration.HomologationBaseUrl))
                {
                    result.Add("FISCAL-FOCUS-URL-MISSING", "URL de homologacao Focus nao configurada.", "HomologationBaseUrl");
                }
                else if (IsProductionFocusUrl(configuration.HomologationBaseUrl))
                {
                    result.Add("FISCAL-FOCUS-URL-PROD", "URL de PRODUCAO Focus nao e permitida nesta fase.", "HomologationBaseUrl");
                }
            }

            return result;
        }

        private static void ValidateEmitente(FiscalIssuerProfile emitente, FiscalValidationResult result)
        {
            if (emitente == null)
            {
                result.Add("FISCAL-EMITENTE-MISSING", "Emitente fiscal nao configurado.");
                return;
            }

            RequireDigits(result, OnlyDigits(emitente.Cnpj), 14, "CNPJ emitente", "Emitente.Cnpj", "FISCAL-EMITENTE-CNPJ");
            RequireText(result, emitente.RazaoSocial, "Razao social do emitente", "Emitente.RazaoSocial", "FISCAL-EMITENTE-RAZAO");
            RequireText(result, emitente.InscricaoEstadual, "Inscricao estadual do emitente", "Emitente.InscricaoEstadual", "FISCAL-EMITENTE-IE");
            RequireText(result, emitente.RegimeTributario, "Regime tributario do emitente", "Emitente.RegimeTributario", "FISCAL-EMITENTE-CRT");
            RequireText(result, emitente.Logradouro, "Logradouro do emitente", "Emitente.Logradouro", "FISCAL-EMITENTE-END");
            RequireText(result, emitente.Numero, "Numero do emitente", "Emitente.Numero", "FISCAL-EMITENTE-NUM");
            RequireText(result, emitente.Bairro, "Bairro do emitente", "Emitente.Bairro", "FISCAL-EMITENTE-BAIRRO");
            RequireText(result, emitente.Municipio, "Municipio do emitente", "Emitente.Municipio", "FISCAL-EMITENTE-MUN");
            RequireDigits(result, OnlyDigits(emitente.CodigoMunicipioIbge), 7, "Codigo IBGE do municipio emitente", "Emitente.CodigoMunicipioIbge", "FISCAL-EMITENTE-IBGE");
            RequireText(result, emitente.Uf, "UF do emitente", "Emitente.Uf", "FISCAL-EMITENTE-UF");
            RequireDigits(result, OnlyDigits(emitente.Cep), 8, "CEP do emitente", "Emitente.Cep", "FISCAL-EMITENTE-CEP");
            RequireText(result, emitente.DefaultIcmsSituacaoTributaria, "CSOSN/CST padrao (configuracao fiscal obrigatoria)", "Emitente.DefaultIcmsSituacaoTributaria", "FISCAL-EMITENTE-CSOSN");
            RequireText(result, emitente.DefaultIcmsOrigem, "Origem ICMS padrao (configuracao fiscal obrigatoria)", "Emitente.DefaultIcmsOrigem", "FISCAL-EMITENTE-ORIGEM");
            RequireText(result, emitente.SerieNFe, "Serie NF-e do estabelecimento", "Emitente.SerieNFe", "FISCAL-EMITENTE-SERIE");
        }

        private static void ValidateDestinatario(FiscalNFeDestinatario dest, FiscalValidationResult result)
        {
            RequireText(result, dest.Nome, "Nome do destinatario", "Destinatario.Nome", "FISCAL-DEST-NOME");
            var doc = OnlyDigits(dest.Documento);
            if (dest.IsCnpj)
            {
                RequireDigits(result, doc, 14, "CNPJ do destinatario", "Destinatario.Documento", "FISCAL-DEST-CNPJ");
            }
            else
            {
                RequireDigits(result, doc, 11, "CPF do destinatario", "Destinatario.Documento", "FISCAL-DEST-CPF");
            }

            RequireText(result, dest.Logradouro, "Logradouro do destinatario", "Destinatario.Logradouro", "FISCAL-DEST-END");
            RequireText(result, dest.Numero, "Numero do destinatario", "Destinatario.Numero", "FISCAL-DEST-NUM");
            RequireText(result, dest.Bairro, "Bairro do destinatario", "Destinatario.Bairro", "FISCAL-DEST-BAIRRO");
            RequireText(result, dest.Municipio, "Municipio do destinatario", "Destinatario.Municipio", "FISCAL-DEST-MUN");
            RequireText(result, dest.Uf, "UF do destinatario", "Destinatario.Uf", "FISCAL-DEST-UF");
            RequireDigits(result, OnlyDigits(dest.Cep), 8, "CEP do destinatario", "Destinatario.Cep", "FISCAL-DEST-CEP");
        }

        private static void ValidateItens(FiscalNFeDocument document, FiscalValidationResult result)
        {
            if (document.Itens == null || document.Itens.Count == 0)
            {
                result.Add("FISCAL-ITEMS-EMPTY", "NF-e exige ao menos um item de produto.");
                return;
            }

            foreach (var item in document.Itens)
            {
                var prefix = $"Item {item.NumeroItem}";
                RequireText(result, item.Codigo, $"{prefix}: codigo", $"Item[{item.NumeroItem}].Codigo", "FISCAL-ITEM-CODIGO");
                RequireText(result, item.Descricao, $"{prefix}: descricao", $"Item[{item.NumeroItem}].Descricao", "FISCAL-ITEM-DESC");

                var ncm = OnlyDigits(item.Ncm);
                if (string.IsNullOrWhiteSpace(ncm))
                {
                    result.Add("FISCAL-ITEM-NCM", $"NF-e bloqueada: produto \"{item.Descricao}\" nao possui NCM fiscal configurado.", $"Item[{item.NumeroItem}].Ncm");
                }
                else if (ncm.Length != 8 || ncm == "00000000" || ncm == "99999999")
                {
                    result.Add("FISCAL-ITEM-NCM-INVALID", $"{prefix}: NCM invalido ou ficticio.", $"Item[{item.NumeroItem}].Ncm");
                }

                var cfop = OnlyDigits(item.Cfop);
                if (string.IsNullOrWhiteSpace(cfop) || cfop.Length != 4)
                {
                    result.Add("FISCAL-ITEM-CFOP", $"NF-e bloqueada: produto \"{item.Descricao}\" nao possui CFOP fiscal configurado.", $"Item[{item.NumeroItem}].Cfop");
                }

                if (string.IsNullOrWhiteSpace(item.Unidade))
                {
                    result.Add("FISCAL-ITEM-UNIDADE", $"{prefix}: unidade de medida ausente.", $"Item[{item.NumeroItem}].Unidade");
                }

                if (item.Quantidade <= 0)
                {
                    result.Add("FISCAL-ITEM-QTD", $"{prefix}: quantidade deve ser > 0.", $"Item[{item.NumeroItem}].Quantidade");
                }

                if (item.ValorUnitario < 0)
                {
                    result.Add("FISCAL-ITEM-PRECO", $"{prefix}: preco unitario invalido.", $"Item[{item.NumeroItem}].ValorUnitario");
                }

                if (string.IsNullOrWhiteSpace(item.IcmsSituacaoTributaria))
                {
                    result.Add("FISCAL-ITEM-CST", $"{prefix}: CST/CSOSN ausente (nao inventado).", $"Item[{item.NumeroItem}].IcmsSituacaoTributaria");
                }

                if (string.IsNullOrWhiteSpace(item.IcmsOrigem))
                {
                    result.Add("FISCAL-ITEM-ORIGEM", $"{prefix}: origem ICMS ausente.", $"Item[{item.NumeroItem}].IcmsOrigem");
                }
            }
        }

        private static void ValidateTotais(FiscalNFeDocument document, FiscalValidationResult result)
        {
            var somaItens = document.Itens.Sum(i => i.ValorTotal);
            if (Math.Abs(somaItens - document.ValorProdutos) > 0.01m)
            {
                result.Add("FISCAL-TOTAL-ITEMS", "Soma dos itens diverge do total de produtos.");
            }

            var esperado = document.ValorProdutos - document.ValorDesconto;
            if (esperado < 0)
            {
                result.Add("FISCAL-TOTAL-NEG", "Total da NF-e nao pode ser negativo.");
            }

            if (Math.Abs(esperado - document.ValorTotal) > 0.01m)
            {
                result.Add("FISCAL-TOTAL-MISMATCH", "Total da NF-e inconsistente (produtos - desconto).");
            }
        }

        private static void RequireText(FiscalValidationResult result, string? value, string label, string field, string code)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Add(code, $"{label} ausente (MissingConfiguration).", field);
            }
        }

        private static void RequireDigits(FiscalValidationResult result, string digits, int length, string label, string field, string code)
        {
            if (string.IsNullOrWhiteSpace(digits) || digits.Length != length || !DigitsOnly.IsMatch(digits))
            {
                result.Add(code, $"{label} ausente ou invalido (esperado {length} digitos).", field);
            }
        }

        private static string OnlyDigits(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());

        public static bool IsProductionFocusUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var u = url.Trim().ToLowerInvariant();
            return u.Contains("api.focusnfe.com.br", StringComparison.Ordinal) &&
                   !u.Contains("homologacao", StringComparison.Ordinal);
        }
    }
}
