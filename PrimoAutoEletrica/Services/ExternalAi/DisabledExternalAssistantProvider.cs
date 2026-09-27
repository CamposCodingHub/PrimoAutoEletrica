using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// Fail-closed external provider stub. Never calls the network.
    /// IsConfigured is always false so orchestrators treat it as unavailable.
    /// </summary>
    public sealed class DisabledExternalAssistantProvider : IAssistantProvider
    {
        private readonly ExternalAssistantArmingState _state;
        private readonly string _warningCode;

        public DisabledExternalAssistantProvider(ExternalAssistantArmingState state = ExternalAssistantArmingState.DisabledByDefault)
        {
            _state = state;
            _warningCode = state switch
            {
                ExternalAssistantArmingState.KillSwitch => ExternalAssistantWarnings.KillSwitch,
                ExternalAssistantArmingState.MissingSecret => ExternalAssistantWarnings.NoKey,
                ExternalAssistantArmingState.ArmedButLiveNotWired => ExternalAssistantWarnings.LiveNotWired,
                _ => ExternalAssistantWarnings.Disabled
            };
        }

        public string ProviderId => "PRIMOX_EXTERNAL_DISABLED";
        public string DisplayName => "PRIMOX External AI (disabled / fail-closed)";
        public bool IsConfigured => false;
        public ExternalAssistantArmingState ArmingState => _state;
        public string WarningCode => _warningCode;

        public Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(context);

            var answer = _state switch
            {
                ExternalAssistantArmingState.KillSwitch =>
                    "O provedor externo de IA está bloqueado pelo kill-switch (PRIMOX_EXTERNAL_AI_KILL_SWITCH). Nenhuma chamada remota foi feita.",
                ExternalAssistantArmingState.MissingSecret =>
                    "Provedor externo habilitado, mas nenhuma chave foi encontrada no ambiente/user store. Nenhuma chamada remota foi feita.",
                ExternalAssistantArmingState.ArmedButLiveNotWired =>
                    "Portões de armamento passaram (enable + secret), porém o cliente HTTP live ainda não está ligado neste release (C3.0). Nenhuma chamada remota foi feita.",
                _ =>
                    "Provedor externo de IA está desabilitado por padrão. O Assist local grounded permanece disponível. Nenhuma chamada remota foi feita."
            };

            var response = new AssistantResponse
            {
                AnswerMarkdown = answer,
                ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                Warnings = new[] { _warningCode, AssistFailClosedPolicy.WarningProviderUnavailable },
                MissingInformation = new[]
                {
                    "Para usar IA externa no futuro: enable explícito + secret em env/user store + kill-switch off + evidência local."
                },
                RecommendedActions = new[]
                {
                    "Continuar com o provedor local grounded (PRIMOX_LOCAL_GROUNDED).",
                    "Coletar medições e evidências locais antes de qualquer consulta."
                },
                Evidence = Array.Empty<EvidenceItem>(),
                CitedSources = Array.Empty<AssistantSourceCitation>(),
                Hypotheses = Array.Empty<AssistantHypothesis>(),
                Provider = ProviderId,
                Timestamp = DateTimeOffset.Now
            };

            return Task.FromResult(response);
        }
    }
}
