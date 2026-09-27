using System;
using System.IO;
using System.Text.Json;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3.5 — safe settings store. Persists enable/provider/model/kill-switch only.
    /// NEVER stores API key plaintext. File is under LocalAppData (not repo).
    /// </summary>
    public sealed class ExternalAssistantSettingsSnapshot
    {
        public bool Enabled { get; set; }
        public bool KillSwitch { get; set; }
        public string ProviderName { get; set; } = "PRIMOX External AI";
        public string? ModelId { get; set; } = "gpt-4o-mini";
        public string ApiKeyEnvironmentVariable { get; set; } = ExternalAssistantOptions.DefaultApiKeyEnvironmentVariable;
        public string? BaseUrl { get; set; } = ExternalAssistantOptions.DefaultBaseUrl;
        /// <summary>Display-only status label at last save (disabled/not wired/live).</summary>
        public string StatusLabel { get; set; } = "disabled";
    }

    public sealed class ExternalAssistantSettingsStore
    {
        public const string FileName = "external-ai-settings.json";

        private readonly string _path;

        public ExternalAssistantSettingsStore(string? appDataPath = null)
        {
            var root = string.IsNullOrWhiteSpace(appDataPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica")
                : appDataPath;
            Directory.CreateDirectory(root);
            _path = Path.Combine(root, FileName);
        }

        public string PathUsed => _path;

        public ExternalAssistantSettingsSnapshot Load()
        {
            if (!File.Exists(_path))
                return new ExternalAssistantSettingsSnapshot();

            try
            {
                var json = File.ReadAllText(_path);
                var snap = JsonSerializer.Deserialize<ExternalAssistantSettingsSnapshot>(json)
                           ?? new ExternalAssistantSettingsSnapshot();
                // Belt-and-suspenders: strip any accidental secret-like fields if someone hand-edited
                RejectIfLooksLikeSecret(snap.ApiKeyEnvironmentVariable, nameof(snap.ApiKeyEnvironmentVariable));
                RejectIfLooksLikeSecret(snap.ModelId, nameof(snap.ModelId));
                RejectIfLooksLikeSecret(snap.BaseUrl, nameof(snap.BaseUrl));
                return snap;
            }
            catch
            {
                return new ExternalAssistantSettingsSnapshot();
            }
        }

        public void Save(ExternalAssistantSettingsSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            RejectIfLooksLikeSecret(snapshot.ApiKeyEnvironmentVariable, nameof(snapshot.ApiKeyEnvironmentVariable));
            if (!string.IsNullOrWhiteSpace(snapshot.ApiKeyEnvironmentVariable) &&
                snapshot.ApiKeyEnvironmentVariable.Length > 120)
            {
                throw new InvalidOperationException("ApiKeyEnvironmentVariable looks like a secret value — refuse save.");
            }

            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            if (json.Contains("sk-", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("\"apiKey\"", StringComparison.OrdinalIgnoreCase) ||
                json.Contains("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Refusing to persist settings that appear to contain secrets.");
            }

            File.WriteAllText(_path, json);
        }

        public ExternalAssistantOptions ToOptions(ExternalAssistantSettingsSnapshot? snap = null)
        {
            snap ??= Load();
            return new ExternalAssistantOptions
            {
                Enabled = snap.Enabled,
                KillSwitch = snap.KillSwitch,
                ProviderId = "PRIMOX_EXTERNAL",
                DisplayName = string.IsNullOrWhiteSpace(snap.ProviderName) ? "PRIMOX External AI" : snap.ProviderName,
                ModelId = snap.ModelId,
                BaseUrl = snap.BaseUrl,
                ApiKeyEnvironmentVariable = string.IsNullOrWhiteSpace(snap.ApiKeyEnvironmentVariable)
                    ? ExternalAssistantOptions.DefaultApiKeyEnvironmentVariable
                    : snap.ApiKeyEnvironmentVariable
            };
        }

        private static void RejectIfLooksLikeSecret(string? value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (value.StartsWith("sk-", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                (value.Length > 40 && value.Contains("api", StringComparison.OrdinalIgnoreCase) && !value.Contains("PRIMOX_", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Field '{field}' looks like a secret — refuse.");
            }
        }
    }
}