using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.Fiscal
{
    public interface ICertificateProvider
    {
        Task<FiscalCertificateInfo?> TryGetCertificateAsync(Guid empresaId, CancellationToken cancellationToken = default);
    }

    public sealed class FiscalCertificateInfo
    {
        public Guid EmpresaId { get; init; }
        public string ThumbprintMasked { get; init; } = string.Empty;
        public DateTime? NotAfterUtc { get; init; }
        public bool IsExpired { get; init; }
        public string Source { get; init; } = string.Empty;
    }

    /// <summary>
    /// Sem certificado A1/PFX real no ambiente — sempre retorna null (BLOCKED_EXTERNAL).
    /// Fake separado apenas para testes unitários.
    /// </summary>
    public sealed class NullCertificateProvider : ICertificateProvider
    {
        public Task<FiscalCertificateInfo?> TryGetCertificateAsync(Guid empresaId, CancellationToken cancellationToken = default)
            => Task.FromResult<FiscalCertificateInfo?>(null);
    }

    public sealed class FakeCertificateProvider : ICertificateProvider
    {
        public Task<FiscalCertificateInfo?> TryGetCertificateAsync(Guid empresaId, CancellationToken cancellationToken = default)
            => Task.FromResult<FiscalCertificateInfo?>(new FiscalCertificateInfo
            {
                EmpresaId = empresaId,
                ThumbprintMasked = "FAKE****TEST",
                NotAfterUtc = DateTime.UtcNow.AddYears(1),
                IsExpired = false,
                Source = "FakeCertificateProvider"
            });
    }

    public interface IXmlSigner
    {
        Task<FiscalXmlSignResult> SignAsync(string xml, Guid empresaId, CancellationToken cancellationToken = default);
    }

    public sealed class FiscalXmlSignResult
    {
        public bool Success { get; init; }
        public string? SignedXml { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? InternalCode { get; init; }
    }

    /// <summary>Assinatura real exige certificado — bloqueado externamente.</summary>
    public sealed class BlockedXmlSigner : IXmlSigner
    {
        public Task<FiscalXmlSignResult> SignAsync(string xml, Guid empresaId, CancellationToken cancellationToken = default)
            => Task.FromResult(new FiscalXmlSignResult
            {
                Success = false,
                Message = "Assinatura digital fiscal bloqueada: certificado A1/PFX ausente (BLOCKED_EXTERNAL).",
                InternalCode = "FISCAL-CERT-BLOCKED-EXTERNAL"
            });
    }

    public interface IWhatsAppProvider
    {
        Task<WhatsAppSendResult> SendAsync(WhatsAppMessageRequest request, CancellationToken cancellationToken = default);
    }

    public sealed class WhatsAppMessageRequest
    {
        public Guid? EmpresaId { get; init; }
        public string DestinationE164Synthetic { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public string? AttachmentPath { get; init; }
        public string Kind { get; init; } = "text";
    }

    public sealed class WhatsAppSendResult
    {
        public bool Success { get; init; }
        public string Status { get; init; } = "pending";
        public string Message { get; init; } = string.Empty;
        public string? ManualDeepLink { get; init; }
        public string? InternalCode { get; init; }
    }

    /// <summary>Fallback manual wa.me — nunca envia API real.</summary>
    public sealed class ManualWhatsAppProvider : IWhatsAppProvider
    {
        public Task<WhatsAppSendResult> SendAsync(WhatsAppMessageRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            // Nunca usar números reais; deep-link apenas se destino sintetico informado.
            var digits = new string((request.DestinationE164Synthetic ?? string.Empty).Where(char.IsDigit).ToArray());
            string? link = null;
            if (digits.Length >= 10)
            {
                var text = Uri.EscapeDataString(request.Body ?? string.Empty);
                link = $"https://wa.me/{digits}?text={text}";
            }

            return Task.FromResult(new WhatsAppSendResult
            {
                Success = false,
                Status = "manual_fallback",
                Message = "WhatsApp Business API ausente (BLOCKED_EXTERNAL). Use deep-link manual se disponivel.",
                ManualDeepLink = link,
                InternalCode = "WHATSAPP-BLOCKED-EXTERNAL"
            });
        }
    }

    public interface IFiscalWebhookProcessor
    {
        Task<FiscalWebhookProcessResult> ProcessAsync(FiscalWebhookEnvelope envelope, CancellationToken cancellationToken = default);
    }

    public sealed class FiscalWebhookEnvelope
    {
        public string Provider { get; init; } = string.Empty;
        public string EventId { get; init; } = string.Empty;
        public string SignatureHeader { get; init; } = string.Empty;
        public string PayloadJson { get; init; } = string.Empty;
        public string? SharedSecretMaskedHint { get; init; }
    }

    public sealed class FiscalWebhookProcessResult
    {
        public bool Accepted { get; init; }
        public bool Duplicate { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? InternalCode { get; init; }
    }

    /// <summary>
    /// Processador de webhook preparado — sem host HTTP local inseguro no WPF.
    /// Idempotência por EventId em memória/persistência futura.
    /// </summary>
    public sealed class FiscalWebhookProcessor : IFiscalWebhookProcessor
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _seen = new(StringComparer.Ordinal);

        public Task<FiscalWebhookProcessResult> ProcessAsync(FiscalWebhookEnvelope envelope, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (string.IsNullOrWhiteSpace(envelope.EventId))
            {
                return Task.FromResult(new FiscalWebhookProcessResult
                {
                    Accepted = false,
                    Message = "EventId obrigatorio.",
                    InternalCode = "FISCAL-WEBHOOK-EVENT-ID"
                });
            }

            if (string.IsNullOrWhiteSpace(envelope.PayloadJson))
            {
                return Task.FromResult(new FiscalWebhookProcessResult
                {
                    Accepted = false,
                    Message = "Payload invalido.",
                    InternalCode = "FISCAL-WEBHOOK-PAYLOAD"
                });
            }

            // Sem secret real: rejeita se assinatura vazia quando provider exige (scaffold).
            if (string.IsNullOrWhiteSpace(envelope.SignatureHeader))
            {
                return Task.FromResult(new FiscalWebhookProcessResult
                {
                    Accepted = false,
                    Message = "Assinatura de webhook ausente.",
                    InternalCode = "FISCAL-WEBHOOK-SIGNATURE"
                });
            }

            var key = $"{envelope.Provider}|{envelope.EventId}";
            if (!_seen.TryAdd(key, 0))
            {
                return Task.FromResult(new FiscalWebhookProcessResult
                {
                    Accepted = true,
                    Duplicate = true,
                    Message = "Webhook duplicado ignorado (idempotente).",
                    InternalCode = "FISCAL-WEBHOOK-DUPLICATE"
                });
            }

            return Task.FromResult(new FiscalWebhookProcessResult
            {
                Accepted = true,
                Duplicate = false,
                Message = "Webhook aceito para processamento (hospedagem HTTP externa requerida).",
                InternalCode = "FISCAL-WEBHOOK-ACCEPTED"
            });
        }
    }
}
