using System;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3 config surface for external AI. Secrets are NEVER stored here — only the env var name.
    /// Default: Enabled=false (fail-closed).
    /// </summary>
    public sealed class ExternalAssistantOptions
    {
        public const string DefaultApiKeyEnvironmentVariable = "PRIMOX_EXTERNAL_AI_API_KEY";
        public const string KillSwitchEnvironmentVariable = "PRIMOX_EXTERNAL_AI_KILL_SWITCH";
        public const string EnabledEnvironmentVariable = "PRIMOX_EXTERNAL_AI_ENABLED";

        /// <summary>Explicit product enable. Default false. Live remote remains unwired in C3.0 even when true.</summary>
        public bool Enabled { get; init; }

        /// <summary>Name of the environment variable that holds the API key (not the key itself).</summary>
        public string ApiKeyEnvironmentVariable { get; init; } = DefaultApiKeyEnvironmentVariable;

        /// <summary>Optional base URL placeholder for future live provider. Unused in C3.0 stub.</summary>
        public string? BaseUrl { get; init; }

        /// <summary>Stable provider id stamp for responses/audit.</summary>
        public string ProviderId { get; init; } = "PRIMOX_EXTERNAL";

        public string DisplayName { get; init; } = "PRIMOX External AI (disabled by default)";
    }

    public static class ExternalAssistantWarnings
    {
        public const string Disabled = "EXTERNAL_DISABLED";
        public const string NoKey = "EXTERNAL_NO_KEY";
        public const string KillSwitch = "EXTERNAL_KILL_SWITCH";
        public const string LiveNotWired = "EXTERNAL_LIVE_NOT_WIRED";
        public const string Ungrounded = "EXTERNAL_UNGROUNDED";
    }

    public enum ExternalAssistantArmingState
    {
        DisabledByDefault = 0,
        KillSwitch = 1,
        MissingSecret = 2,
        ArmedButLiveNotWired = 3
    }
}
