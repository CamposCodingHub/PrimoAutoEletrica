using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>Dados do emitente — configuração explícita; nunca inventados.</summary>
    public sealed class FiscalIssuerProfile
    {
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public string InscricaoEstadual { get; set; } = string.Empty;
        /// <summary>1=Simples Nacional, 2=Simples excesso, 3=Regime Normal — deve ser informado.</summary>
        public string RegimeTributario { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string CodigoMunicipioIbge { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string Cep { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        /// <summary>Série NF-e do estabelecimento (obrigatória para emissão).</summary>
        public string SerieNFe { get; set; } = string.Empty;
        /// <summary>Número inicial informado pelo estabelecimento — não inventar.</summary>
        public string? NumeroInicialNFe { get; set; }
        /// <summary>CSOSN/CST padrão obrigatório quando produto não tiver classificação própria.</summary>
        public string DefaultIcmsSituacaoTributaria { get; set; } = string.Empty;
        public string DefaultIcmsOrigem { get; set; } = string.Empty;
    }

    public sealed class FiscalValidationIssue
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? Field { get; init; }
    }

    public sealed class FiscalValidationResult
    {
        public bool IsValid => Issues.Count == 0;
        public IList<FiscalValidationIssue> Issues { get; } = new List<FiscalValidationIssue>();

        public void Add(string code, string message, string? field = null)
            => Issues.Add(new FiscalValidationIssue { Code = code, Message = message, Field = field });

        public string Summarize()
            => string.Join(Environment.NewLine, Issues.Select(i => $"[{i.Code}] {i.Message}"));
    }

    /// <summary>Documento NF-e interno (independente do Focus).</summary>
    public sealed class FiscalNFeDocument
    {
        public Guid FiscalOperationId { get; init; }
        public string IdempotencyKey { get; init; } = string.Empty;
        public FiscalEnvironment Environment { get; init; } = FiscalEnvironment.Homologation;
        public Guid? VendaId { get; init; }
        public string NaturezaOperacao { get; init; } = "Venda de mercadoria";
        public FiscalIssuerProfile Emitente { get; init; } = new();
        public FiscalNFeDestinatario Destinatario { get; init; } = new();
        public IReadOnlyList<FiscalNFeItem> Itens { get; init; } = Array.Empty<FiscalNFeItem>();
        public decimal ValorProdutos { get; init; }
        public decimal ValorDesconto { get; init; }
        public decimal ValorTotal { get; init; }
        public string? FormaPagamentoCodigo { get; init; }
    }

    public sealed class FiscalNFeDestinatario
    {
        public string Nome { get; init; } = string.Empty;
        public string Documento { get; init; } = string.Empty;
        public bool IsCnpj { get; init; }
        public string Logradouro { get; init; } = string.Empty;
        public string Numero { get; init; } = string.Empty;
        public string Bairro { get; init; } = string.Empty;
        public string Municipio { get; init; } = string.Empty;
        public string Uf { get; init; } = string.Empty;
        public string Cep { get; init; } = string.Empty;
        public int IndicadorIeDestinatario { get; init; } = 9;
    }

    public sealed class FiscalNFeItem
    {
        public int NumeroItem { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public string Ncm { get; init; } = string.Empty;
        public string Cfop { get; init; } = string.Empty;
        public string? Cest { get; init; }
        public string Unidade { get; init; } = string.Empty;
        public decimal Quantidade { get; init; }
        public decimal ValorUnitario { get; init; }
        public decimal ValorTotal { get; init; }
        public string IcmsOrigem { get; init; } = string.Empty;
        public string IcmsSituacaoTributaria { get; init; } = string.Empty;
    }
}
