using System;
using System.IO;
using System.Text.Json;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Configuração fiscal não secreta + ponteiros para segredos DPAPI.
    /// Tokens/certificados NUNCA vão para appsettings nem Git.
    /// </summary>
    public sealed class FiscalConfiguration
    {
        public FiscalProviderKind Provider { get; set; } = FiscalProviderKind.FocusNfe;
        public FiscalEnvironment Environment { get; set; } = FiscalEnvironment.Homologation;
        public string HomologationBaseUrl { get; set; } = "https://homologacao.focusnfe.com.br";
        public string ProductionBaseUrl { get; set; } = string.Empty;

        /// <summary>HTTP live — permitido somente com Environment=Homologation.</summary>
        public bool LiveHttpEnabled { get; set; }

        /// <summary>Produção permanece bloqueada até fase futura explícita.</summary>
        public bool ProductionUnlocked { get; set; }

        public string CredentialStorePath { get; set; } = string.Empty;
        public FiscalIssuerProfile Issuer { get; set; } = new();
    }

    public sealed class FiscalSecretMaterial
    {
        public string HomologationTokenProtected { get; set; } = string.Empty;
        public string ProductionTokenProtected { get; set; } = string.Empty;
    }

    public sealed class FiscalConfigurationService
    {
        public const string ConfigFileName = "fiscal-foundation.json";
        public const string SecretsFileName = "fiscal-secrets.dpapi";
        public const string DefaultHomologationBaseUrl = "https://homologacao.focusnfe.com.br";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string _appDataPath;
        private readonly LoggerService? _logger;

        public FiscalConfigurationService(string appDataPath, LoggerService? logger = null)
        {
            _appDataPath = appDataPath ?? throw new ArgumentNullException(nameof(appDataPath));
            _logger = logger;
        }

        public string ConfigPath => Path.Combine(_appDataPath, "Config", ConfigFileName);
        public string SecretsPath => Path.Combine(_appDataPath, "Config", SecretsFileName);

        public FiscalConfiguration LoadOrCreate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

            if (!File.Exists(ConfigPath))
            {
                var created = CreateDefault();
                Save(created);
                return created;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<FiscalConfiguration>(json, JsonOptions) ?? CreateDefault();
                Normalize(config);
                return config;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[Fiscal] Falha ao ler configuracao: {ex.Message}. Usando defaults seguros.");
                return CreateDefault();
            }
        }

        public void Save(FiscalConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            Normalize(configuration);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(configuration, JsonOptions));
        }

        public void SaveHomologationToken(string plainToken)
        {
            if (string.IsNullOrWhiteSpace(plainToken))
            {
                throw new ArgumentException("Token homologacao vazio.", nameof(plainToken));
            }

            var secrets = LoadSecrets();
            secrets.HomologationTokenProtected = CryptoService.ProtectString(plainToken);
            SaveSecrets(secrets);
        }

        public string? TryGetHomologationToken()
        {
            // Preferência: secret store DPAPI; opcionalmente variável de ambiente local (nunca logada).
            var secrets = LoadSecrets();
            if (!string.IsNullOrWhiteSpace(secrets.HomologationTokenProtected))
            {
                var token = CryptoService.UnprotectString(secrets.HomologationTokenProtected);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    return token;
                }
            }

            var env = Environment.GetEnvironmentVariable("PRIMOX_FOCUS_HOMOLOG_TOKEN");
            return string.IsNullOrWhiteSpace(env) ? null : env.Trim();
        }

        public bool HasHomologationCredential() => !string.IsNullOrWhiteSpace(TryGetHomologationToken());

        private FiscalSecretMaterial LoadSecrets()
        {
            if (!File.Exists(SecretsPath))
            {
                return new FiscalSecretMaterial();
            }

            try
            {
                var json = File.ReadAllText(SecretsPath);
                return JsonSerializer.Deserialize<FiscalSecretMaterial>(json, JsonOptions) ?? new FiscalSecretMaterial();
            }
            catch
            {
                return new FiscalSecretMaterial();
            }
        }

        private void SaveSecrets(FiscalSecretMaterial secrets)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SecretsPath)!);
            File.WriteAllText(SecretsPath, JsonSerializer.Serialize(secrets, JsonOptions));
        }

        private FiscalConfiguration CreateDefault()
        {
            var config = new FiscalConfiguration
            {
                Provider = FiscalProviderKind.FocusNfe,
                Environment = FiscalEnvironment.Homologation,
                HomologationBaseUrl = DefaultHomologationBaseUrl,
                ProductionBaseUrl = string.Empty,
                LiveHttpEnabled = false,
                ProductionUnlocked = false,
                CredentialStorePath = SecretsPath,
                Issuer = new FiscalIssuerProfile()
            };
            Normalize(config);
            return config;
        }

        public static void Normalize(FiscalConfiguration configuration)
        {
            // Produção nunca permanece ativa nesta fase.
            configuration.ProductionUnlocked = false;
            if (configuration.Environment == FiscalEnvironment.Production)
            {
                configuration.Environment = FiscalEnvironment.Homologation;
            }

            // Live HTTP só faz sentido em Homologação; default permanece desligado até opt-in explícito.
            if (configuration.Environment != FiscalEnvironment.Homologation)
            {
                configuration.LiveHttpEnabled = false;
            }

            if (string.IsNullOrWhiteSpace(configuration.HomologationBaseUrl))
            {
                configuration.HomologationBaseUrl = DefaultHomologationBaseUrl;
            }

            if (FiscalDocumentValidator.IsProductionFocusUrl(configuration.HomologationBaseUrl))
            {
                configuration.HomologationBaseUrl = DefaultHomologationBaseUrl;
                configuration.LiveHttpEnabled = false;
            }

            // Nunca gravar URL de produção utilizável.
            configuration.ProductionBaseUrl = string.Empty;

            if (configuration.Provider == FiscalProviderKind.FakeTestOnly)
            {
                configuration.Provider = FiscalProviderKind.FocusNfe;
            }

            configuration.Issuer ??= new FiscalIssuerProfile();
        }
    }
}
