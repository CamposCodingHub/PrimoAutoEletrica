using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services.Fiscal
{
    public sealed class FiscalEmissionRequest
    {
        public Guid FiscalOperationId { get; init; } = Guid.NewGuid();
        public string IdempotencyKey { get; init; } = string.Empty;
        public FiscalDocumentType DocumentType { get; init; } = FiscalDocumentType.NFe;
        public FiscalEnvironment Environment { get; init; } = FiscalEnvironment.Homologation;
        public FiscalProviderKind Provider { get; init; } = FiscalProviderKind.FocusNfe;
        public string OriginModule { get; init; } = string.Empty;
        public Guid? OrdemServicoId { get; init; }
        public Guid? VendaId { get; init; }
        public Guid? OrcamentoId { get; init; }
        public string? ClienteDocumento { get; init; }
        public string? ClienteNome { get; init; }
        public IReadOnlyList<FiscalDocumentItemDto> Items { get; init; } = Array.Empty<FiscalDocumentItemDto>();
        public decimal Total { get; init; }
        public string? Observacoes { get; init; }
    }

    public sealed class FiscalDocumentItemDto
    {
        public string Codigo { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public string? Ncm { get; init; }
        public string? Cfop { get; init; }
        public decimal Quantidade { get; init; }
        public decimal ValorUnitario { get; init; }
    }

    public sealed class FiscalCancellationRequest
    {
        public Guid FiscalOperationId { get; init; }
        public string Justificativa { get; init; } = string.Empty;
        public FiscalEnvironment Environment { get; init; } = FiscalEnvironment.Homologation;
    }

    public sealed record FiscalProviderResult
    {
        public bool Success { get; init; }
        public FiscalDocumentStatus Status { get; init; } = FiscalDocumentStatus.Unknown;
        public FiscalErrorKind ErrorKind { get; init; } = FiscalErrorKind.None;
        public string Message { get; init; } = string.Empty;
        public string? InternalCode { get; init; }
        public string? ProviderCode { get; init; }
        public string? ProviderMessage { get; init; }
        public string? ProviderDocumentId { get; init; }
        public string? ChaveAcesso { get; init; }
        public string? Protocolo { get; init; }
        public Guid FiscalOperationId { get; init; }
        public string IdempotencyKey { get; init; } = string.Empty;

        public static FiscalProviderResult Fail(
            FiscalDocumentStatus status,
            FiscalErrorKind kind,
            string message,
            Guid operationId,
            string idempotencyKey,
            string? internalCode = null,
            string? providerCode = null,
            string? providerMessage = null) => new()
        {
            Success = false,
            Status = status,
            ErrorKind = kind,
            Message = message,
            InternalCode = internalCode,
            ProviderCode = providerCode,
            ProviderMessage = providerMessage,
            FiscalOperationId = operationId,
            IdempotencyKey = idempotencyKey
        };

        public static FiscalProviderResult Ok(
            FiscalDocumentStatus status,
            Guid operationId,
            string idempotencyKey,
            string message,
            string? providerDocumentId = null,
            string? chave = null,
            string? protocolo = null) => new()
        {
            Success = status is FiscalDocumentStatus.Authorized or FiscalDocumentStatus.Processing or FiscalDocumentStatus.Pending,
            Status = status,
            Message = message,
            FiscalOperationId = operationId,
            IdempotencyKey = idempotencyKey,
            ProviderDocumentId = providerDocumentId,
            ChaveAcesso = chave,
            Protocolo = protocolo
        };
    }

    public sealed class FiscalOperation
    {
        public Guid Id { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public FiscalDocumentType DocumentType { get; set; }
        public FiscalDocumentStatus Status { get; set; }
        public FiscalEnvironment Environment { get; set; }
        public FiscalProviderKind Provider { get; set; }
        public string OriginModule { get; set; } = string.Empty;
        public Guid? OrdemServicoId { get; set; }
        public Guid? VendaId { get; set; }
        public Guid? OrcamentoId { get; set; }
        public string? ProviderDocumentId { get; set; }
        public string? LastErrorKind { get; set; }
        public string? LastErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class FiscalDocumentRecord
    {
        public Guid Id { get; set; }
        public Guid OperationId { get; set; }
        public FiscalDocumentType DocumentType { get; set; }
        public string? Numero { get; set; }
        public string? Serie { get; set; }
        public string? ChaveAcesso { get; set; }
        public FiscalDocumentStatus Status { get; set; }
        public FiscalEnvironment Environment { get; set; }
        public FiscalProviderKind Provider { get; set; }
        public string? Protocolo { get; set; }
        public string? Reason { get; set; }
        public string? XmlEnviadoPath { get; set; }
        public string? XmlAutorizadoPath { get; set; }
        public Guid? OrdemServicoId { get; set; }
        public Guid? VendaId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
