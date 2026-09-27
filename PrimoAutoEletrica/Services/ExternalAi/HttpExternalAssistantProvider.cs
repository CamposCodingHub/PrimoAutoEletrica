using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3.1+ HTTP external assistant provider. Fail-closed:
    /// - No network unless Enabled + secret + kill-switch off
    /// - No network without local evidence package
    /// - Never logs API keys
    /// </summary>
    public sealed class HttpExternalAssistantProvider : IAssistantProvider
    {
        private readonly ExternalAssistantOptions _options;
        private readonly IExternalAssistantSecretSource _secrets;
        private readonly Func<bool> _isKillSwitchOn;
        private readonly Func<bool> _isEnabled;
        private readonly IExternalAssistantHttpTransport _transport;
        private readonly ExternalEvidencePackageBuilder _packageBuilder;
        private readonly ExternalResponseGroundingValidator _validator;
        private readonly bool _includeFinancial;

        public HttpExternalAssistantProvider(
            ExternalAssistantOptions options,
            IExternalAssistantSecretSource secrets,
            Func<bool> isKillSwitchOn,
            Func<bool> isEnabled,
            IExternalAssistantHttpTransport? transport = null,
            ExternalEvidencePackageBuilder? packageBuilder = null,
            ExternalResponseGroundingValidator? validator = null,
            bool includeFinancial = false)
        {
            _options = options ?? new ExternalAssistantOptions();
            _secrets = secrets ?? new EnvironmentExternalAssistantSecretSource();
            _isKillSwitchOn = isKillSwitchOn ?? (() => false);
            _isEnabled = isEnabled ?? (() => false);
            _transport = transport ?? new HttpExternalAssistantTransport();
            _packageBuilder = packageBuilder ?? new ExternalEvidencePackageBuilder();
            _validator = validator ?? new ExternalResponseGroundingValidator();
            _includeFinancial = includeFinancial;
        }

        public string ProviderId => string.IsNullOrWhiteSpace(_options.ProviderId) ? "PRIMOX_EXTERNAL" : _options.ProviderId;
        public string DisplayName => string.IsNullOrWhiteSpace(_options.DisplayName) ? "PRIMOX External AI" : _options.DisplayName;

        public bool IsConfigured =>
            !_isKillSwitchOn() &&
            _isEnabled() &&
            !string.IsNullOrWhiteSpace(_secrets.TryGetApiKey(_options.ApiKeyEnvironmentVariable));

        public async Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(context);

            if (_isKillSwitchOn())
            {
                return DisabledLike(ExternalAssistantWarnings.KillSwitch,
                    "O provedor externo de IA está bloqueado pelo kill-switch. Nenhuma chamada remota foi feita.");
            }

            if (!_isEnabled())
            {
                return DisabledLike(ExternalAssistantWarnings.Disabled,
                    "Provedor externo de IA está desabilitado por padrão. Nenhuma chamada remota foi feita.");
            }

            var key = _secrets.TryGetApiKey(_options.ApiKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(key))
            {
                return DisabledLike(ExternalAssistantWarnings.NoKey,
                    "Provedor externo habilitado, mas nenhuma chave foi encontrada no ambiente/user store. Nenhuma chamada remota foi feita.");
            }

            var package = _packageBuilder.Build(context, includeFinancial: _includeFinancial);
            if (!package.HasEvidence)
            {
                return new AssistantResponse
                {
                    AnswerMarkdown = "Consulta externa recusada: pacote de evidência local vazio (fail-closed). Nenhuma chamada remota foi feita.",
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Warnings = new[] { ExternalAssistantWarnings.NoEvidence, AssistFailClosedPolicy.WarningProviderUnavailable },
                    MissingInformation = new[] { "Evidência local (Knowledge / DiagnosticCase / Measurement) antes de chamar provedor externo." },
                    RecommendedActions = new[]
                    {
                        "Usar Assist local grounded após coletar medições.",
                        "Ampliar base de conhecimento ou vincular OS/veículo à consulta."
                    },
                    Evidence = Array.Empty<EvidenceItem>(),
                    CitedSources = Array.Empty<AssistantSourceCitation>(),
                    Provider = ProviderId,
                    Timestamp = DateTimeOffset.Now
                };
            }

            var payload = new ExternalAssistantRequestPayload
            {
                RequestId = package.RequestId,
                Query = package.Query,
                Model = _options.ModelId ?? "gpt-4o-mini",
                Evidence = package.Sources
            };

            var endpoint = string.IsNullOrWhiteSpace(_options.BaseUrl)
                ? ExternalAssistantOptions.DefaultBaseUrl
                : _options.BaseUrl!;

            ExternalAssistantRawResponse raw;
            try
            {
                raw = await _transport.SendAsync(
                    payload,
                    key,
                    endpoint,
                    _options.RequestTimeout,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never include secret in message
                var safe = SanitizeException(ex);
                return new AssistantResponse
                {
                    AnswerMarkdown = "Falha de rede ao consultar provedor externo (fail-closed). Nenhuma ação autônoma foi tomada. Detalhe: " + safe,
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Warnings = new[] { ExternalAssistantWarnings.NetworkError },
                    MissingInformation = new[] { "Provedor externo disponível / rede estável." },
                    RecommendedActions = new[] { "Repetir com Assist local grounded." },
                    Evidence = Array.Empty<EvidenceItem>(),
                    CitedSources = Array.Empty<AssistantSourceCitation>(),
                    Provider = ProviderId,
                    Timestamp = DateTimeOffset.Now
                };
            }

            if (raw.HttpStatusCode == 401 || raw.HttpStatusCode == 403)
            {
                return new AssistantResponse
                {
                    AnswerMarkdown = "Autenticação no provedor externo falhou (401/403). Verifique a chave no user store/env — valor nunca é logado. Nenhuma ação foi tomada.",
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Warnings = new[] { ExternalAssistantWarnings.AuthFailed },
                    Evidence = Array.Empty<EvidenceItem>(),
                    CitedSources = Array.Empty<AssistantSourceCitation>(),
                    Provider = ProviderId,
                    Timestamp = DateTimeOffset.Now
                };
            }

            if (raw.HttpStatusCode == 408)
            {
                return new AssistantResponse
                {
                    AnswerMarkdown = "Timeout ao consultar provedor externo. Fail-closed; use Assist local.",
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Warnings = new[] { ExternalAssistantWarnings.Timeout },
                    Evidence = Array.Empty<EvidenceItem>(),
                    CitedSources = Array.Empty<AssistantSourceCitation>(),
                    Provider = ProviderId,
                    Timestamp = DateTimeOffset.Now
                };
            }

            if (string.IsNullOrWhiteSpace(raw.AnswerMarkdown) ||
                (raw.HttpStatusCode.HasValue && raw.HttpStatusCode.Value >= 400))
            {
                return new AssistantResponse
                {
                    AnswerMarkdown = "Resposta do provedor externo malformada, vazia ou com erro HTTP (fail-closed).",
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Warnings = new[] { ExternalAssistantWarnings.MalformedResponse },
                    Evidence = Array.Empty<EvidenceItem>(),
                    CitedSources = Array.Empty<AssistantSourceCitation>(),
                    Provider = ProviderId,
                    Timestamp = DateTimeOffset.Now
                };
            }

            return _validator.Validate(raw, package, ProviderId);
        }

        private AssistantResponse DisabledLike(string warning, string answer)
        {
            return new AssistantResponse
            {
                AnswerMarkdown = answer,
                ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                Warnings = new[] { warning, AssistFailClosedPolicy.WarningProviderUnavailable },
                MissingInformation = new[]
                {
                    "Para usar IA externa: enable explícito + secret em env/user store + kill-switch off + evidência local."
                },
                RecommendedActions = new[]
                {
                    "Continuar com o provedor local grounded (PRIMOX_LOCAL_GROUNDED).",
                    "Coletar medições e evidências locais antes de qualquer consulta."
                },
                Evidence = Array.Empty<EvidenceItem>(),
                CitedSources = Array.Empty<AssistantSourceCitation>(),
                Provider = "PRIMOX_EXTERNAL_DISABLED",
                Timestamp = DateTimeOffset.Now
            };
        }

        public static string SanitizeException(Exception ex)
        {
            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("sk-", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Bearer", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("api-key", StringComparison.OrdinalIgnoreCase))
            {
                return ex.GetType().Name + " [REDACTED]";
            }
            return ex.GetType().Name + ": " + (msg.Length > 200 ? msg.Substring(0, 200) : msg);
        }
    }
}