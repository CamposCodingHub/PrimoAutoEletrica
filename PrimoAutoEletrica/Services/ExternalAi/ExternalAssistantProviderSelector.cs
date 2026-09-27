using System;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// Selects an external IAssistantProvider using fail-closed arming gates.
    /// C3.1+: when armed, returns HttpExternalAssistantProvider (still requires evidence to call network).
    /// </summary>
    public sealed class ExternalAssistantProviderSelector
    {
        private readonly ExternalAssistantOptions _options;
        private readonly IExternalAssistantSecretSource _secrets;
        private readonly IExternalAssistantHttpTransport? _transport;

        public ExternalAssistantProviderSelector(
            ExternalAssistantOptions? options = null,
            IExternalAssistantSecretSource? secrets = null,
            IExternalAssistantHttpTransport? transport = null)
        {
            _options = options ?? new ExternalAssistantOptions();
            _secrets = secrets ?? new EnvironmentExternalAssistantSecretSource();
            _transport = transport;
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

            return ExternalAssistantArmingState.ArmedReady;
        }

        public ExternalAssistantStatus GetStatus()
        {
            return EvaluateArmingState() switch
            {
                ExternalAssistantArmingState.KillSwitch => ExternalAssistantStatus.KillSwitch,
                ExternalAssistantArmingState.DisabledByDefault => ExternalAssistantStatus.Disabled,
                ExternalAssistantArmingState.MissingSecret => ExternalAssistantStatus.ReadyNoKey,
                ExternalAssistantArmingState.ArmedReady => ExternalAssistantStatus.Live,
                ExternalAssistantArmingState.ArmedButLiveNotWired => ExternalAssistantStatus.NotWired,
                _ => ExternalAssistantStatus.Disabled
            };
        }

        public string GetStatusLabel()
        {
            return GetStatus() switch
            {
                ExternalAssistantStatus.Disabled => "disabled",
                ExternalAssistantStatus.NotWired => "not wired",
                ExternalAssistantStatus.ReadyNoKey => "enabled / no key",
                ExternalAssistantStatus.KillSwitch => "kill-switch",
                ExternalAssistantStatus.Live => "live",
                _ => "disabled"
            };
        }

        /// <summary>Returns Http provider when armed; otherwise disabled fail-closed stub.</summary>
        public IAssistantProvider CreateExternalProvider(bool includeFinancial = false)
        {
            var state = EvaluateArmingState();
            if (state == ExternalAssistantArmingState.ArmedReady)
            {
                return new HttpExternalAssistantProvider(
                    _options,
                    _secrets,
                    IsKillSwitchOn,
                    IsEffectivelyEnabled,
                    _transport,
                    includeFinancial: includeFinancial);
            }

            return new DisabledExternalAssistantProvider(state);
        }

        /// <summary>
        /// Prefer local grounded for production Assist. External is selected only via AssistProviderRouter
        /// when explicitly allowed.
        /// </summary>
        public IAssistantProvider CreateDefaultAssistProvider(IAssistantProvider? localProvider = null)
        {
            return localProvider ?? new GroundedLocalRuleAssistantProvider();
        }

        public bool IsKillSwitchOn()
        {
            if (_options.KillSwitch) return true;
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