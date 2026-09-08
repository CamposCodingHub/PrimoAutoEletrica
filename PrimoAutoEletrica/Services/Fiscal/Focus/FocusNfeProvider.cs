using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Adapter Focus NFe — PREPARADO, HTTP live DESLIGADO nesta fundação.
    /// Não emite documento fiscal real. Não retorna Authorized sem comunicação real.
    /// </summary>
    public sealed class FocusNfeProvider : IFiscalProvider
    {
        private readonly FiscalConfigurationService _configurationService;

        public FocusNfeProvider(FiscalConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public FiscalProviderKind Kind => FiscalProviderKind.FocusNfe;

        public Task<FiscalProviderResult> EmitirAsync(
            FiscalEmissionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var blocked = FiscalProductionGuard.TryDenyProduction(
                request.Environment,
                request.FiscalOperationId,
                request.IdempotencyKey);
            if (blocked != null)
            {
                return Task.FromResult(blocked);
            }

            var config = _configurationService.LoadOrCreate();
            if (!config.LiveHttpEnabled)
            {
                return Task.FromResult(FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotImplemented,
                    FiscalErrorKind.NotImplemented,
                    "Focus NFe adapter preparado; HTTP live desligado. Emissao real = proxima etapa (homologacao).",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-HTTP-OFF"));
            }

            if (string.IsNullOrWhiteSpace(config.HomologationBaseUrl) &&
                request.Environment == FiscalEnvironment.Homologation)
            {
                return Task.FromResult(FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "URL de homologacao Focus nao configurada.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-URL-MISSING"));
            }

            if (!_configurationService.HasHomologationCredential())
            {
                return Task.FromResult(FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Credencial de homologacao Focus ausente no secret store DPAPI.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-TOKEN-MISSING"));
            }

            // Live HTTP ainda não implementado nesta fase — nunca simular Authorized.
            return Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "Chamada HTTP Focus ainda nao implementada. Fundacao apenas.",
                request.FiscalOperationId,
                request.IdempotencyKey,
                internalCode: "FISCAL-FOCUS-HTTP-NOT-IMPLEMENTED"));
        }

        public Task<FiscalProviderResult> ConsultarAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "Consulta Focus preparada no contrato; HTTP live desligado nesta fundacao.",
                operation.Id,
                operation.IdempotencyKey,
                internalCode: "FISCAL-FOCUS-CONSULT-OFF"));
        }

        public Task<FiscalProviderResult> CancelarAsync(
            FiscalCancellationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var blocked = FiscalProductionGuard.TryDenyProduction(
                request.Environment,
                request.FiscalOperationId,
                string.Empty);
            if (blocked != null)
            {
                return Task.FromResult(blocked);
            }

            return Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "Cancelamento Focus: contrato apenas. Nao executar cancelamento real nesta fase.",
                request.FiscalOperationId,
                string.Empty,
                internalCode: "FISCAL-FOCUS-CANCEL-OFF"));
        }

        public Task<FiscalProviderResult> ObterXmlAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "ObterXml Focus: contrato futuro. XML nao gerado nesta fundacao.",
                operation.Id,
                operation.IdempotencyKey,
                internalCode: "FISCAL-FOCUS-XML-OFF"));
        }

        public Task<FiscalProviderResult> ObterDanfeAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "ObterDanfe Focus: contrato futuro. PDF nao gerado nesta fundacao.",
                operation.Id,
                operation.IdempotencyKey,
                internalCode: "FISCAL-FOCUS-DANFE-OFF"));
        }
    }
}
