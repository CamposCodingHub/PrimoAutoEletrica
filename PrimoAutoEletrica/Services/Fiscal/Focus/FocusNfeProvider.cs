using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Adapter Focus NFe — HTTP live apenas em Homologação quando habilitado.
    /// Produção sempre bloqueada. Nunca retorna Authorized sem resposta real do provider.
    /// </summary>
    public sealed class FocusNfeProvider : IFiscalProvider
    {
        private readonly FiscalConfigurationService _configurationService;
        private readonly FocusNfeHttpClient _http;
        private FiscalNFeDocument? _pendingDocument;

        public FocusNfeProvider(
            FiscalConfigurationService configurationService,
            FocusNfeHttpClient? httpClient = null)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _http = httpClient ?? new FocusNfeHttpClient();
        }

        public FiscalProviderKind Kind => FiscalProviderKind.FocusNfe;

        /// <summary>Anexa o documento NF-e completo para a próxima emissão HTTP (mesmo processo).</summary>
        public void SetPendingDocument(FiscalNFeDocument document)
        {
            _pendingDocument = document ?? throw new ArgumentNullException(nameof(document));
        }

        public async Task<FiscalProviderResult> EmitirAsync(
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
                return blocked;
            }

            if (request.Environment == FiscalEnvironment.Production)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.ProductionBlocked,
                    FiscalErrorKind.ProductionBlocked,
                    "Producao fiscal bloqueada no FocusNfeProvider.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-PROD-BLOCKED");
            }

            var config = _configurationService.LoadOrCreate();
            if (!config.LiveHttpEnabled)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Focus HTTP live desligado. Configure LiveHttpEnabled=true apenas para Homologacao.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-HTTP-OFF");
            }

            var baseUrl = string.IsNullOrWhiteSpace(config.HomologationBaseUrl)
                ? "https://homologacao.focusnfe.com.br"
                : config.HomologationBaseUrl.Trim();

            if (FiscalDocumentValidator.IsProductionFocusUrl(baseUrl))
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.ProductionBlocked,
                    FiscalErrorKind.ProductionBlocked,
                    "URL de producao Focus bloqueada.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-PROD-BLOCKED");
            }

            var token = _configurationService.TryGetHomologationToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Credencial de homologacao Focus ausente no secret store DPAPI.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-TOKEN-MISSING");
            }

            if (_pendingDocument == null)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.ValidationError,
                    "Documento NF-e nao anexado ao provider (payload incompleto).",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-PAYLOAD-MISSING");
            }

            var document = _pendingDocument;
            _pendingDocument = null;

            if (document.Environment == FiscalEnvironment.Production)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.ProductionBlocked,
                    FiscalErrorKind.ProductionBlocked,
                    "Documento marcado para producao — bloqueado.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-PROD-BLOCKED");
            }

            var payload = FocusNfePayloadBuilder.Build(document);
            var reference = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? request.FiscalOperationId.ToString("N")
                : request.IdempotencyKey;

            var http = await _http.PostNfeAsync(baseUrl, token, reference, payload, cancellationToken)
                .ConfigureAwait(false);
            return FocusNfeResponseMapper.MapEmissionResponse(http, request.FiscalOperationId, request.IdempotencyKey);
        }

        public async Task<FiscalProviderResult> ConsultarAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();

            if (operation.Environment == FiscalEnvironment.Production)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.ProductionBlocked,
                    FiscalErrorKind.ProductionBlocked,
                    "Consulta em producao bloqueada.",
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-PROD-BLOCKED");
            }

            var config = _configurationService.LoadOrCreate();
            if (!config.LiveHttpEnabled)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Focus HTTP live desligado para consulta.",
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-HTTP-OFF");
            }

            var token = _configurationService.TryGetHomologationToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Credencial homologacao ausente para consulta.",
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-TOKEN-MISSING");
            }

            var baseUrl = string.IsNullOrWhiteSpace(config.HomologationBaseUrl)
                ? "https://homologacao.focusnfe.com.br"
                : config.HomologationBaseUrl.Trim();

            var reference = string.IsNullOrWhiteSpace(operation.IdempotencyKey)
                ? operation.Id.ToString("N")
                : operation.IdempotencyKey;

            var http = await _http.GetNfeAsync(baseUrl, token, reference, cancellationToken).ConfigureAwait(false);
            return FocusNfeResponseMapper.MapConsultResponse(http, operation.Id, operation.IdempotencyKey);
        }

        public async Task<FiscalProviderResult> CancelarAsync(
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
                return blocked;
            }

            if (request.Environment == FiscalEnvironment.Production)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.ProductionBlocked,
                    FiscalErrorKind.ProductionBlocked,
                    "Cancelamento Focus em producao bloqueado.",
                    request.FiscalOperationId,
                    string.Empty,
                    internalCode: "FISCAL-PROD-BLOCKED");
            }

            var justificativa = (request.Justificativa ?? string.Empty).Trim();
            if (justificativa.Length < 15 || justificativa.Length > 255)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.ValidationError,
                    "Justificativa Focus invalida (15-255 caracteres).",
                    request.FiscalOperationId,
                    string.Empty,
                    internalCode: "FISCAL-FOCUS-CANCEL-JUSTIFICATIVA");
            }

            var config = _configurationService.LoadOrCreate();
            if (!config.LiveHttpEnabled)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Focus HTTP live desligado para cancelamento.",
                    request.FiscalOperationId,
                    string.Empty,
                    internalCode: "FISCAL-FOCUS-HTTP-OFF");
            }

            var token = _configurationService.TryGetHomologationToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Credencial homologacao ausente para cancelamento (BLOCKED_EXTERNAL).",
                    request.FiscalOperationId,
                    string.Empty,
                    internalCode: "FISCAL-FOCUS-TOKEN-MISSING");
            }

            var baseUrl = string.IsNullOrWhiteSpace(config.HomologationBaseUrl)
                ? "https://homologacao.focusnfe.com.br"
                : config.HomologationBaseUrl.Trim();

            var reference = string.IsNullOrWhiteSpace(request.ProviderReference)
                ? request.FiscalOperationId.ToString("N")
                : request.ProviderReference.Trim();
            var http = await _http.DeleteNfeAsync(baseUrl, token, reference, justificativa, cancellationToken)
                .ConfigureAwait(false);
            return FocusNfeResponseMapper.MapConsultResponse(http, request.FiscalOperationId, reference);
        }

        public async Task<FiscalProviderResult> ObterXmlAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();

            if (operation.Environment == FiscalEnvironment.Production)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.ProductionBlocked,
                    FiscalErrorKind.ProductionBlocked,
                    "ObterXml em producao bloqueado.",
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-PROD-BLOCKED");
            }

            var config = _configurationService.LoadOrCreate();
            if (!config.LiveHttpEnabled)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Focus HTTP live desligado para XML.",
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-HTTP-OFF");
            }

            var token = _configurationService.TryGetHomologationToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.NotConfigured,
                    FiscalErrorKind.ConfigurationError,
                    "Credencial homologacao ausente para XML (BLOCKED_EXTERNAL).",
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-TOKEN-MISSING");
            }

            var baseUrl = string.IsNullOrWhiteSpace(config.HomologationBaseUrl)
                ? "https://homologacao.focusnfe.com.br"
                : config.HomologationBaseUrl.Trim();

            var reference = string.IsNullOrWhiteSpace(operation.IdempotencyKey)
                ? operation.Id.ToString("N")
                : operation.IdempotencyKey;

            var consult = await _http.GetNfeAsync(baseUrl, token, reference, cancellationToken).ConfigureAwait(false);
            var mapped = FocusNfeResponseMapper.MapConsultResponse(consult, operation.Id, operation.IdempotencyKey);
            if (string.IsNullOrWhiteSpace(mapped.ArtifactRelativePath))
            {
                return FiscalProviderResult.Fail(
                    mapped.Status,
                    FiscalErrorKind.ProviderError,
                    "Focus nao retornou caminho_xml_nota_fiscal. XML SEFAZ nao gerado localmente.",
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-XML-PATH-MISSING");
            }

            FocusNfeHttpResponse download;
            try
            {
                download = await _http.DownloadHomologArtifactAsync(
                    baseUrl, token, mapped.ArtifactRelativePath, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.ProviderError,
                    ex.Message,
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-XML-SSRF-BLOCK");
            }

            if (download.IsTimeout || download.IsNetworkError || !download.IsSuccessStatusCode ||
                string.IsNullOrWhiteSpace(download.Body))
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.ProviderError,
                    "Falha ao baixar XML Focus (homolog).",
                    operation.Id,
                    operation.IdempotencyKey,
                    internalCode: "FISCAL-FOCUS-XML-DOWNLOAD");
            }

            return FiscalProviderResult.Ok(
                mapped.Status == FiscalDocumentStatus.Authorized ? FiscalDocumentStatus.Authorized : mapped.Status,
                operation.Id,
                operation.IdempotencyKey,
                "XML obtido do Focus (homolog).",
                providerDocumentId: mapped.ProviderDocumentId,
                chave: mapped.ChaveAcesso,
                protocolo: mapped.Protocolo,
                xmlContent: download.Body,
                artifactRelativePath: mapped.ArtifactRelativePath);
        }

        public Task<FiscalProviderResult> ObterDanfeAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
            => Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "DANFE oficial Focus: use IDanfeGenerator informativo localmente ou download Focus quando caminho disponivel (BLOCKED_EXTERNAL sem token).",
                operation.Id,
                operation.IdempotencyKey,
                internalCode: "FISCAL-FOCUS-DANFE-PROVIDER"));
    }
}
