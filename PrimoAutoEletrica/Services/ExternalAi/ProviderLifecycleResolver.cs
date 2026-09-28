using System;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C5.4 — Deterministic provider lifecycle modes for Assist routing/audit.
    /// Priority (highest first):
    /// 1. Kill-switch → DISABLED
    /// 2. Enabled OFF → LOCAL (default)
    /// 3. Enabled ON + no key → LOCAL (ReadyNoKey; never silent EXTERNAL)
    /// 4. Enabled ON + armed + external success → EXTERNAL
    /// 5. Enabled ON + armed + external fail/ungrounded/timeout/4xx/5xx/malformed/network → FALLBACK (or ERROR if no local)
    /// Never silent switch without audit recording (AssistProviderRouter.Record).
    /// </summary>
    public enum AssistProviderMode
    {
        LOCAL = 0,
        EXTERNAL = 1,
        DISABLED = 2,
        FALLBACK = 3,
        ERROR = 4
    }

    public static class ProviderLifecycleResolver
    {
        public static AssistProviderMode ResolveArmingMode(ExternalAssistantProviderSelector selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return selector.EvaluateArmingState() switch
            {
                ExternalAssistantArmingState.KillSwitch => AssistProviderMode.DISABLED,
                ExternalAssistantArmingState.DisabledByDefault => AssistProviderMode.LOCAL,
                ExternalAssistantArmingState.MissingSecret => AssistProviderMode.LOCAL,
                ExternalAssistantArmingState.ArmedButLiveNotWired => AssistProviderMode.LOCAL,
                ExternalAssistantArmingState.ArmedReady => AssistProviderMode.EXTERNAL, // candidate only; runtime may FALLBACK
                _ => AssistProviderMode.LOCAL
            };
        }

        public static AssistProviderMode ResolveOutcomeMode(string result, string status)
        {
            if (string.Equals(result, "LOCAL_FALLBACK", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "FALLBACK", StringComparison.OrdinalIgnoreCase))
                return AssistProviderMode.FALLBACK;
            if (string.Equals(result, "EXTERNAL", StringComparison.OrdinalIgnoreCase))
                return AssistProviderMode.EXTERNAL;
            if (string.Equals(status, "ERROR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(result, "ERROR", StringComparison.OrdinalIgnoreCase))
                return AssistProviderMode.ERROR;
            if (string.Equals(status, "REJECTED", StringComparison.OrdinalIgnoreCase))
                return AssistProviderMode.FALLBACK; // ungrounded/reject path falls back / fail-closed
            return AssistProviderMode.LOCAL;
        }

        public static string ToAuditMode(AssistProviderMode mode) => mode.ToString();
    }
}