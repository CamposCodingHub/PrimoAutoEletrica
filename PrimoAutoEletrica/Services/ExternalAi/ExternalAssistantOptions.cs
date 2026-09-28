using System;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3 config surface for external AI. Secrets are NEVER stored here - only the env var name.
    /// Default: Enabled=false (fail-closed).
    /// </summary>
    public sealed class ExternalAssistantOptions
    {
        public const string DefaultApiKeyEnvironmentVariable = "PRIMOX_EXTERNAL_AI_API_KEY";
        public const string KillSwitchEnvironmentVariable = "PRIMOX_EXTERNAL_AI_KILL_SWITCH";
        public const string EnabledEnvironmentVariable = "PRIMOX_EXTERNAL_AI_ENABLED";
        public const string DefaultBaseUrl = "https://api.openai.com/v1/chat/completions";

        /// <summary>Explicit product enable. Default false.</summary>
        public bool Enabled { get; init; }

        /// <summary>Name of the environment variable that holds the API key (not the key itself).</summary>
        public string ApiKeyEnvironmentVariable { get; init; } = DefaultApiKeyEnvironmentVariable;

        /// <summary>Optional chat-completions endpoint. No secret.</summary>
        public string? BaseUrl { get; init; } = DefaultBaseUrl;

        /// <summary>Optional model id (e.g. gpt-4o-mini). No secret.</summary>
        public string? ModelId { get; init; } = "gpt-4o-mini";

        /// <summary>Stable provider id stamp for responses/audit.</summary>
        public string ProviderId { get; init; } = "PRIMOX_EXTERNAL";

        public string DisplayName { get; init; } = "PRIMOX External AI (disabled by default)";

        /// <summary>Soft kill-switch persisted in local settings (OR'd with env kill-switch).</summary>
        public bool KillSwitch { get; init; }

        /// <summary>Timeout for live HTTP. Default 30s.</summary>
        public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
    }

    public static class ExternalAssistantWarnings
    {
        public const string Disabled = "EXTERNAL_DISABLED";
        public const string NoKey = "EXTERNAL_NO_KEY";
        public const string KillSwitch = "EXTERNAL_KILL_SWITCH";
        public const string LiveNotWired = "EXTERNAL_LIVE_NOT_WIRED";
        public const string Ungrounded = "EXTERNAL_UNGROUNDED";
        public const string NoEvidence = "EXTERNAL_NO_EVIDENCE";
        public const string NetworkError = "EXTERNAL_NETWORK_ERROR";
        public const string AuthFailed = "EXTERNAL_AUTH_FAILED";
        public const string MalformedResponse = "EXTERNAL_MALFORMED_RESPONSE";
        public const string Timeout = "EXTERNAL_TIMEOUT";
        public const string FinanceRedacted = "EXTERNAL_FINANCE_REDACTED";
        public const string FallbackLocal = "EXTERNAL_FALLBACK_LOCAL";
        public const string Conflict = "EXTERNAL_CONFLICT";
        public const string InventedOs = "EXTERNAL_INVENTED_OS";
    }

    public enum ExternalAssistantArmingState
    {
        DisabledByDefault = 0,
        KillSwitch = 1,
        MissingSecret = 2,
        /// <summary>Gates passed; live HTTP provider may be selected (still requires evidence to call).</summary>
        ArmedReady = 3,
        /// <summary>Legacy C3.0 honesty stamp - retained for docs/tests of the stub era.</summary>
        ArmedButLiveNotWired = 4
    }

    /// <summary>UI/status surface values (no secrets).</summary>
    public enum ExternalAssistantStatus
    {
        Disabled = 0,
        NotWired = 1,
        ReadyNoKey = 2,
        KillSwitch = 3,
        Live = 4
    }
}