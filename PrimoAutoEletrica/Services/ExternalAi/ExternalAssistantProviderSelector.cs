using System;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// Selects an external IAssistantProvider using fail-closed arming gates.
    /// C3.0 never returns a live HTTP provider — at best ArmedButLiveNotWired disabled stub.
    /// </summary>
    public sealed class ExternalAssistantProviderSelector
    {
        private readonly ExternalAssistantOptions _options;
        private readonly IExternalAssistantSecretSource _secrets;

        public ExternalAssistantProviderSelector(
            ExternalAssistantOptions? options = null,
            IExternalAssistantSecretSource? secrets = null)
        {
            _options = options ?? new ExternalAssistantOptions();
            _secrets = secrets ?? new EnvironmentExternalAssistantSecretSource();
        }

        public ExternalAssistantOptions Options => _options;

        /// <summary>True only when kill-switch is off, Enabled is true, and a non-empty secret resolves.</summary>
        public bool WouldArmLiveGates()
        {
            if (IsKillSwitchOn()) return false;
            if (!IsEffectivelyEnabled()) return false;
            var key = _secrets.TryGetApiKey(_options.ApiKeyEnvironmentVariable);
            return !string.IsNullOrWhiteSpace(key);
        }

        public ExternalAssistantArmingState EvaluateArmingState()
        {
            if (IsKillSwitchOn())
                return ExternalAssistantArmingState.KillSwitch;

            if (!IsEffectivelyEnabled())
                return ExternalAssistantArmingState.DisabledByDefault;

            var key = _secrets.TryGetApiKey(_options.ApiKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(key))
                return ExternalAssistantArmingState.MissingSecret;

            // C3.0 honesty: gates can pass, but live client is not wired.
            return ExternalAssistantArmingState.ArmedButLiveNotWired;
        }

        /// <summary>Always returns a disabled/fail-closed provider in C3.0 (no network).</summary>
        public IAssistantProvider CreateExternalProvider()
        {
            var state = EvaluateArmingState();
            return new DisabledExternalAssistantProvider(state);
        }

        /// <summary>
        /// Prefer local grounded for production Assist. External stub is available for status/tests only
        /// unless a future orchestrator explicitly routes to CreateExternalProvider.
        /// </summary>
        public IAssistantProvider CreateDefaultAssistProvider(IAssistantProvider? localProvider = null)
        {
            return localProvider ?? new GroundedLocalRuleAssistantProvider();
        }

        public bool IsKillSwitchOn()
        {
            var raw = Environment.GetEnvironmentVariable(ExternalAssistantOptions.KillSwitchEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(raw)) return false;
            raw = raw.Trim();
            return raw == "1"
                || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEffectivelyEnabled()
        {
            if (_options.Enabled) return true;
            var raw = Environment.GetEnvironmentVariable(ExternalAssistantOptions.EnabledEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(raw)) return false;
            raw = raw.Trim();
            return raw == "1"
                || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("on", StringComparison.OrdinalIgnoreCase);
        }
    }
}
