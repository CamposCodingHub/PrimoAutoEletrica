using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Modelo NFC-e (65) — infraestrutura preparada; emissão live BLOCKED_EXTERNAL.
    /// </summary>
    public sealed class FiscalNfceDocument
    {
        public Guid EmpresaId { get; set; }
        public FiscalEnvironment Environment { get; set; } = FiscalEnvironment.Homologation;
        public string? Serie { get; set; }
        public string? Numero { get; set; }
        public string? CscId { get; set; }
        public string? CscTokenMaskedHint { get; set; }
        public bool Contingencia { get; set; }
        public FiscalIssuerProfile Emitente { get; set; } = new();
        public FiscalNFeDestinatario? Consumidor { get; set; }
        public IList<FiscalNFeItem> Itens { get; set; } = new List<FiscalNFeItem>();
        public decimal ValorTotal { get; set; }
    }

    /// <summary>
    /// Modelo NFS-e — abstração multi-município; padrão municipal não assumido.
    /// </summary>
    public sealed class FiscalNfseDocument
    {
        public Guid EmpresaId { get; set; }
        public FiscalEnvironment Environment { get; set; } = FiscalEnvironment.Homologation;
        public string? MunicipioIbge { get; set; }
        public string? NumeroRps { get; set; }
        public string? SerieRps { get; set; }
        public FiscalIssuerProfile Prestador { get; set; } = new();
        public FiscalNFeDestinatario? Tomador { get; set; }
        public string DescricaoServico { get; set; } = string.Empty;
        public string? CodigoServicoMunicipal { get; set; }
        public decimal ValorServicos { get; set; }
        public decimal? AliquotaIss { get; set; }
        public bool IssRetido { get; set; }
    }

    public interface INfseProvider
    {
        FiscalProviderKind Kind { get; }
        System.Threading.Tasks.Task<FiscalProviderResult> EmitirRpsAsync(
            FiscalNfseDocument document,
            FiscalEmissionRequest request,
            System.Threading.CancellationToken cancellationToken = default);
    }

    /// <summary>Scaffold NFS-e — sem inventar endpoint municipal.</summary>
    public sealed class ScaffoldNfseProvider : INfseProvider
    {
        public FiscalProviderKind Kind => FiscalProviderKind.None;

        public System.Threading.Tasks.Task<FiscalProviderResult> EmitirRpsAsync(
            FiscalNfseDocument document,
            FiscalEmissionRequest request,
            System.Threading.CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "NFS-e municipal: contrato/padrão do municipio ausente (BLOCKED_EXTERNAL).",
                request.FiscalOperationId,
                request.IdempotencyKey,
                internalCode: "FISCAL-NFSE-SCAFFOLD"));
    }
}
