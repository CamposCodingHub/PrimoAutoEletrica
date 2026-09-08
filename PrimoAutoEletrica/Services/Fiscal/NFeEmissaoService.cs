using System;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Services.Fiscal;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// KEEP — FUTURE bridge para emissão NF-e.
    /// Anteriormente arquivo 0 bytes. Agora facade documentada sobre a fundação fiscal.
    /// NÃO emite NF-e real; NÃO autoriza produção.
    /// Importação de XML continua em <see cref="NFeService"/>.
    /// </summary>
    public sealed class NFeEmissaoService
    {
        public const string FoundationStatus = "FiscalFoundation";
        public const string EmissionStatus = "NotImplemented";

        private readonly FiscalApplicationService _fiscal;

        public NFeEmissaoService(FiscalApplicationService fiscal)
        {
            _fiscal = fiscal ?? throw new ArgumentNullException(nameof(fiscal));
        }

        /// <summary>
        /// Solicita emissão via fundação. Com Focus HTTP off, retorna NotImplemented / NotConfigured —
        /// nunca Authorized sem provedor real.
        /// </summary>
        public Task<FiscalProviderResult> SolicitarEmissaoNFeAsync(
            FiscalEmissionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var nfeRequest = new FiscalEmissionRequest
            {
                FiscalOperationId = request.FiscalOperationId,
                IdempotencyKey = request.IdempotencyKey,
                DocumentType = FiscalDocumentType.NFe,
                Environment = request.Environment,
                Provider = request.Provider == FiscalProviderKind.None
                    ? FiscalProviderKind.FocusNfe
                    : request.Provider,
                OriginModule = string.IsNullOrWhiteSpace(request.OriginModule) ? "NFeEmissaoService" : request.OriginModule,
                OrdemServicoId = request.OrdemServicoId,
                VendaId = request.VendaId,
                OrcamentoId = request.OrcamentoId,
                ClienteDocumento = request.ClienteDocumento,
                ClienteNome = request.ClienteNome,
                Items = request.Items,
                Total = request.Total,
                Observacoes = request.Observacoes
            };

            return _fiscal.EmitirAsync(nfeRequest, cancellationToken);
        }

        public Task<FiscalProviderResult> ConsultarOperacaoAsync(
            Guid fiscalOperationId,
            CancellationToken cancellationToken = default)
            => _fiscal.ConsultarAsync(fiscalOperationId, cancellationToken);
    }
}
