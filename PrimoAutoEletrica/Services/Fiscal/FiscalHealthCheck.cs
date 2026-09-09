using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services.Fiscal
{
    public enum FiscalHealthStatus
    {
        Ready = 0,
        Incomplete = 1,
        Invalid = 2,
        ProviderUnavailable = 3,
        CredentialMissing = 4,
        ProductionBlocked = 5
    }

    public sealed class FiscalHealthIssue
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? Field { get; init; }
        public bool Blocking { get; init; } = true;
    }

    public sealed class FiscalProductPendingItem
    {
        public Guid ProdutoId { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public string Ncm { get; init; } = string.Empty;
        public string Cfop { get; init; } = string.Empty;
        public string Problema { get; init; } = string.Empty;
    }

    public sealed class FiscalHealthReport
    {
        public FiscalHealthStatus Status { get; init; }
        public bool ReadyForHomologEmission => Status == FiscalHealthStatus.Ready;
        public string Summary { get; init; } = string.Empty;
        public IReadOnlyList<FiscalHealthIssue> Issues { get; init; } = Array.Empty<FiscalHealthIssue>();
        public IReadOnlyList<FiscalProductPendingItem> PendingProducts { get; init; } = Array.Empty<FiscalProductPendingItem>();
        public bool EmitenteOk { get; init; }
        public bool AmbienteHomologacaoOk { get; init; }
        public bool ProviderOk { get; init; }
        public bool CredentialOk { get; init; }
        public bool LiveHttpEnabled { get; init; }
        public bool ProductionBlocked { get; init; } = true;
    }

    /// <summary>
    /// Saúde fiscal operacional — nunca declara READY se houver requisito obrigatório faltando.
    /// </summary>
    public sealed class FiscalHealthCheck
    {
        private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);
        private readonly FiscalConfigurationService _configurationService;
        private readonly RepositoryRegistry? _repositories;

        public FiscalHealthCheck(
            FiscalConfigurationService configurationService,
            RepositoryRegistry? repositories = null)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _repositories = repositories;
        }

        public FiscalHealthReport Evaluate(bool requireLiveCredential = true)
        {
            var config = _configurationService.LoadOrCreate();
            FiscalConfigurationService.Normalize(config);
            var issues = new List<FiscalHealthIssue>();
            var pending = EvaluatePendingProducts(config, issues);

            var emitenteOk = ValidateEmitente(config.Issuer, issues);
            var ambienteOk = config.Environment == FiscalEnvironment.Homologation;
            if (!ambienteOk)
            {
                issues.Add(new FiscalHealthIssue
                {
                    Code = "FISCAL-HEALTH-ENV",
                    Message = "Ambiente deve ser Homologacao nesta fase.",
                    Field = "Environment"
                });
            }

            var providerOk = config.Provider == FiscalProviderKind.FocusNfe;
            if (!providerOk)
            {
                issues.Add(new FiscalHealthIssue
                {
                    Code = "FISCAL-HEALTH-PROVIDER",
                    Message = "Provider Focus NFe nao configurado.",
                    Field = "Provider"
                });
            }

            var credentialOk = _configurationService.HasHomologationCredential();
            if (requireLiveCredential && !credentialOk)
            {
                issues.Add(new FiscalHealthIssue
                {
                    Code = "FISCAL-HEALTH-CREDENTIAL",
                    Message = "Credencial de homologacao ausente (DPAPI ou PRIMOX_FOCUS_HOMOLOG_TOKEN).",
                    Field = "Credential"
                });
            }

            if (requireLiveCredential && !config.LiveHttpEnabled)
            {
                issues.Add(new FiscalHealthIssue
                {
                    Code = "FISCAL-HEALTH-HTTP-OFF",
                    Message = "LiveHttpEnabled=false — HTTP Focus desligado.",
                    Field = "LiveHttpEnabled"
                });
            }

            if (config.ProductionUnlocked || config.Environment == FiscalEnvironment.Production)
            {
                issues.Add(new FiscalHealthIssue
                {
                    Code = "FISCAL-HEALTH-PROD",
                    Message = "Producao permanece bloqueada.",
                    Field = "Production"
                });
            }

            var status = ResolveStatus(issues, credentialOk, requireLiveCredential);
            var ready = status == FiscalHealthStatus.Ready;
            return new FiscalHealthReport
            {
                Status = status,
                Summary = ready
                    ? "Pronto para emissao em HOMOLOGACAO (configuracao completa)."
                    : "Nao pronto para emissao — pendencias obrigatórias.",
                Issues = issues,
                PendingProducts = pending,
                EmitenteOk = emitenteOk,
                AmbienteHomologacaoOk = ambienteOk,
                ProviderOk = providerOk,
                CredentialOk = credentialOk,
                LiveHttpEnabled = config.LiveHttpEnabled,
                ProductionBlocked = true
            };
        }

        private IReadOnlyList<FiscalProductPendingItem> EvaluatePendingProducts(
            FiscalConfiguration config,
            List<FiscalHealthIssue> issues)
        {
            if (_repositories?.Produtos == null)
            {
                return Array.Empty<FiscalProductPendingItem>();
            }

            var list = new List<FiscalProductPendingItem>();
            IEnumerable<Produto> produtos;
            try
            {
                produtos = _repositories.Produtos.ObterTodos().Where(p => p.Ativo);
            }
            catch
            {
                return Array.Empty<FiscalProductPendingItem>();
            }

            foreach (var p in produtos)
            {
                var problemas = new List<string>();
                var ncm = Digits(p.NCMS);
                if (string.IsNullOrWhiteSpace(ncm) || ncm.Length != 8 || ncm is "00000000" or "99999999")
                {
                    problemas.Add("NCM ausente/invalido");
                }

                var cfop = Digits(p.CFOP);
                if (string.IsNullOrWhiteSpace(cfop) || cfop.Length != 4)
                {
                    problemas.Add("CFOP ausente/invalido");
                }

                if (string.IsNullOrWhiteSpace(p.UnidadeMedida))
                {
                    problemas.Add("Unidade ausente");
                }

                // CST/CSOSN e origem: issuer default cobre itens sem campo proprio no Produto.
                if (string.IsNullOrWhiteSpace(config.Issuer.DefaultIcmsSituacaoTributaria))
                {
                    // Já reportado no emitente; não duplicar por produto.
                }

                if (problemas.Count == 0)
                {
                    continue;
                }

                list.Add(new FiscalProductPendingItem
                {
                    ProdutoId = p.Id,
                    Codigo = p.Codigo ?? string.Empty,
                    Nome = p.Nome ?? string.Empty,
                    Ncm = p.NCMS ?? string.Empty,
                    Cfop = p.CFOP ?? string.Empty,
                    Problema = string.Join("; ", problemas)
                });
            }

            if (list.Count > 0)
            {
                issues.Add(new FiscalHealthIssue
                {
                    Code = "FISCAL-HEALTH-PRODUCTS",
                    Message = $"{list.Count} produto(s) ativo(s) com pendencia fiscal (NCM/CFOP/unidade). Venda comercial ok; emissao NF-e bloqueada para esses itens.",
                    Field = "Produtos",
                    Blocking = false
                });
            }

            return list;
        }

        private static bool ValidateEmitente(FiscalIssuerProfile issuer, List<FiscalHealthIssue> issues)
        {
            var ok = true;
            void Req(string code, string msg, string field)
            {
                ok = false;
                issues.Add(new FiscalHealthIssue { Code = code, Message = msg, Field = field });
            }

            if (Digits(issuer.Cnpj).Length != 14) Req("FISCAL-HEALTH-CNPJ", "CNPJ emitente ausente/invalido.", "Emitente.Cnpj");
            if (string.IsNullOrWhiteSpace(issuer.RazaoSocial)) Req("FISCAL-HEALTH-RAZAO", "Razao social ausente.", "Emitente.RazaoSocial");
            if (string.IsNullOrWhiteSpace(issuer.InscricaoEstadual)) Req("FISCAL-HEALTH-IE", "IE ausente.", "Emitente.InscricaoEstadual");
            if (string.IsNullOrWhiteSpace(issuer.RegimeTributario)) Req("FISCAL-HEALTH-CRT", "Regime tributario ausente.", "Emitente.RegimeTributario");
            if (string.IsNullOrWhiteSpace(issuer.Logradouro)) Req("FISCAL-HEALTH-END", "Logradouro ausente.", "Emitente.Logradouro");
            if (string.IsNullOrWhiteSpace(issuer.Numero)) Req("FISCAL-HEALTH-NUM", "Numero ausente.", "Emitente.Numero");
            if (string.IsNullOrWhiteSpace(issuer.Bairro)) Req("FISCAL-HEALTH-BAIRRO", "Bairro ausente.", "Emitente.Bairro");
            if (string.IsNullOrWhiteSpace(issuer.Municipio)) Req("FISCAL-HEALTH-MUN", "Municipio ausente.", "Emitente.Municipio");
            if (Digits(issuer.CodigoMunicipioIbge).Length != 7) Req("FISCAL-HEALTH-IBGE", "IBGE municipio ausente/invalido.", "Emitente.CodigoMunicipioIbge");
            if (string.IsNullOrWhiteSpace(issuer.Uf) || issuer.Uf.Trim().Length != 2) Req("FISCAL-HEALTH-UF", "UF ausente/invalida.", "Emitente.Uf");
            if (Digits(issuer.Cep).Length != 8) Req("FISCAL-HEALTH-CEP", "CEP ausente/invalido.", "Emitente.Cep");
            if (string.IsNullOrWhiteSpace(issuer.SerieNFe)) Req("FISCAL-HEALTH-SERIE", "Serie NF-e ausente.", "Emitente.SerieNFe");
            if (string.IsNullOrWhiteSpace(issuer.DefaultIcmsSituacaoTributaria))
                Req("FISCAL-HEALTH-CSOSN", "CSOSN/CST padrao ausente.", "Emitente.DefaultIcmsSituacaoTributaria");
            if (string.IsNullOrWhiteSpace(issuer.DefaultIcmsOrigem))
                Req("FISCAL-HEALTH-ORIGEM", "Origem ICMS padrao ausente.", "Emitente.DefaultIcmsOrigem");

            return ok;
        }

        private static FiscalHealthStatus ResolveStatus(
            List<FiscalHealthIssue> issues,
            bool credentialOk,
            bool requireLiveCredential)
        {
            var blocking = issues.Where(i => i.Blocking).ToList();
            if (blocking.Any(i => i.Code.Contains("PROD", StringComparison.OrdinalIgnoreCase)))
            {
                return FiscalHealthStatus.ProductionBlocked;
            }

            if (requireLiveCredential && !credentialOk)
            {
                return FiscalHealthStatus.CredentialMissing;
            }

            if (blocking.Count == 0)
            {
                return FiscalHealthStatus.Ready;
            }

            if (blocking.Any(i => i.Code.Contains("INVALID", StringComparison.OrdinalIgnoreCase)
                                  || i.Code.Contains("CNPJ", StringComparison.OrdinalIgnoreCase)
                                  || i.Code.Contains("IBGE", StringComparison.OrdinalIgnoreCase)))
            {
                return FiscalHealthStatus.Invalid;
            }

            return FiscalHealthStatus.Incomplete;
        }

        private static string Digits(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : new string(value.Where(char.IsDigit).ToArray());
    }
}
