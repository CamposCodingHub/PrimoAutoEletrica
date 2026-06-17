using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class SystemConfiguration
    {
        public string CompanyDisplayName { get; set; } = "Primo Auto Eletrica";
        public string CompanyLegalName { get; set; } = "Primo Auto Eletrica";
        public string CompanyDocument { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;
        public string CompanyWhatsApp { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string LogoPath { get; set; } = string.Empty;
        public string ReceiptHeader { get; set; } = "Comprovante nao fiscal";
        public string ReceiptFooter { get; set; } = "Obrigado pela preferencia.";
        public string PreferredTheme { get; set; } = nameof(AppTheme.Light);
        public bool AutoBackupEnabled { get; set; } = true;
        public int AutoBackupRetentionCopies { get; set; } = 10;
        public string DefaultPrinterName { get; set; } = string.Empty;
        public string OsNumberPrefix { get; set; } = "OS-{yyyy}-";
        public int OsNextNumber { get; set; } = 1;
        public string OrcamentoNumberPrefix { get; set; } = "ORC-{yyyyMMdd}-";
        public int OrcamentoNextNumber { get; set; } = 1;
        public string PermissionPolicySummary { get; set; } = "Controle por perfis e permissoes persistidas.";
        public string MessageTemplateOrcamento { get; set; } = "Ola {Cliente}, seu orcamento {Numero} esta pronto para aprovacao. Total: {Total}.";
        public string MessageTemplateOrdemPronta { get; set; } = "Ola {Cliente}, seu veiculo {Veiculo} esta pronto para retirada. OS {Numero}.";
        public string MessageTemplateGarantia { get; set; } = "Servico com garantia padrao de {DiasGarantia} dias mediante apresentacao da OS.";
        public int DefaultWarrantyDays { get; set; } = 90;
        public decimal DefaultProductMarginPercent { get; set; } = 40m;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = "Sistema";

        public BusinessConfiguration ToBusinessConfiguration()
        {
            return new BusinessConfiguration
            {
                CompanyDisplayName = CompanyDisplayName,
                CompanyLegalName = CompanyLegalName,
                CompanyDocument = CompanyDocument,
                CompanyPhone = CompanyPhone,
                CompanyWhatsApp = CompanyWhatsApp,
                CompanyAddress = CompanyAddress,
                LogoPath = LogoPath,
                ReceiptHeader = ReceiptHeader,
                ReceiptFooter = ReceiptFooter
            };
        }
    }

    public sealed record NumberingPolicy(string Prefix, int NextNumber);

    public sealed class SystemConfigurationService
    {
        private const string TableName = "ConfiguracoesSistema";
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;

        private static readonly IReadOnlyDictionary<string, string> Groups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(SystemConfiguration.CompanyDisplayName)] = "Oficina",
            [nameof(SystemConfiguration.CompanyLegalName)] = "Oficina",
            [nameof(SystemConfiguration.CompanyDocument)] = "Oficina",
            [nameof(SystemConfiguration.CompanyPhone)] = "Oficina",
            [nameof(SystemConfiguration.CompanyWhatsApp)] = "Oficina",
            [nameof(SystemConfiguration.CompanyAddress)] = "Oficina",
            [nameof(SystemConfiguration.LogoPath)] = "Oficina",
            [nameof(SystemConfiguration.ReceiptHeader)] = "Oficina",
            [nameof(SystemConfiguration.ReceiptFooter)] = "Oficina",
            [nameof(SystemConfiguration.PreferredTheme)] = "Aparencia",
            [nameof(SystemConfiguration.AutoBackupEnabled)] = "Backup",
            [nameof(SystemConfiguration.AutoBackupRetentionCopies)] = "Backup",
            [nameof(SystemConfiguration.DefaultPrinterName)] = "Impressao",
            [nameof(SystemConfiguration.OsNumberPrefix)] = "Numeracao",
            [nameof(SystemConfiguration.OsNextNumber)] = "Numeracao",
            [nameof(SystemConfiguration.OrcamentoNumberPrefix)] = "Numeracao",
            [nameof(SystemConfiguration.OrcamentoNextNumber)] = "Numeracao",
            [nameof(SystemConfiguration.PermissionPolicySummary)] = "Permissoes",
            [nameof(SystemConfiguration.MessageTemplateOrcamento)] = "Mensagens",
            [nameof(SystemConfiguration.MessageTemplateOrdemPronta)] = "Mensagens",
            [nameof(SystemConfiguration.MessageTemplateGarantia)] = "Mensagens",
            [nameof(SystemConfiguration.DefaultWarrantyDays)] = "Operacao",
            [nameof(SystemConfiguration.DefaultProductMarginPercent)] = "Operacao"
        };

        public SystemConfigurationService(DatabaseService databaseService, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger;
        }

        public SystemConfiguration LoadOrCreate(string appDataPath)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();
            EnsureSchema(connection);

            var values = ReadValues(connection);
            var configuration = values.Count == 0
                ? CreateDefault(appDataPath)
                : FromValues(values, CreateDefault(appDataPath));

            Normalize(configuration);

            if (values.Count == 0)
            {
                SaveTrusted(configuration, configuration.UpdatedBy);
            }

            return configuration;
        }

        public void SaveAuthorized(SystemConfiguration configuration, Funcionario operador, PermissionService permissionService)
        {
            ArgumentNullException.ThrowIfNull(operador);
            ArgumentNullException.ThrowIfNull(permissionService);

            if (!permissionService.TemPermissaoCodigo("SISTEMA_CONFIGURAR"))
            {
                throw new UnauthorizedAccessException("O perfil atual nao possui permissao SISTEMA_CONFIGURAR para alterar configuracoes criticas.");
            }

            SaveTrusted(configuration, operador.Nome);
        }

        public void SaveTrusted(SystemConfiguration configuration, string? updatedBy)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            Normalize(configuration);
            configuration.UpdatedAt = DateTime.Now;
            configuration.UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "Sistema" : updatedBy.Trim();

            using var connection = _databaseService.GetConnection();
            connection.Open();
            EnsureSchema(connection);

            using var transaction = connection.BeginTransaction();
            var isSqlServer = IsSqlServerConnection(connection);
            foreach (var (key, value) in ToValues(configuration))
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = isSqlServer
                    ? $@"
                    MERGE {TableName} WITH (HOLDLOCK) AS Target
                    USING (SELECT @Chave AS Chave) AS Source
                    ON Target.Chave = Source.Chave
                    WHEN MATCHED THEN
                        UPDATE SET Valor = @Valor,
                                   Grupo = @Grupo,
                                   AtualizadoEm = @AtualizadoEm,
                                   AtualizadoPor = @AtualizadoPor
                    WHEN NOT MATCHED THEN
                        INSERT
                        (
                            Chave,
                            Valor,
                            Grupo,
                            AtualizadoEm,
                            AtualizadoPor
                        )
                        VALUES
                        (
                            @Chave,
                            @Valor,
                            @Grupo,
                            @AtualizadoEm,
                            @AtualizadoPor
                        );"
                    : $@"
                    INSERT INTO {TableName}
                    (
                        Chave,
                        Valor,
                        Grupo,
                        AtualizadoEm,
                        AtualizadoPor
                    )
                    VALUES
                    (
                        @Chave,
                        @Valor,
                        @Grupo,
                        @AtualizadoEm,
                        @AtualizadoPor
                    )
                    ON CONFLICT(Chave) DO UPDATE SET
                        Valor = excluded.Valor,
                        Grupo = excluded.Grupo,
                        AtualizadoEm = excluded.AtualizadoEm,
                        AtualizadoPor = excluded.AtualizadoPor;";
                command.Parameters.AddWithValue("@Chave", key);
                command.Parameters.AddWithValue("@Valor", value);
                command.Parameters.AddWithValue("@Grupo", Groups.TryGetValue(key, out var group) ? group : "Sistema");
                command.Parameters.AddWithValue("@AtualizadoEm", isSqlServer
                    ? configuration.UpdatedAt
                    : configuration.UpdatedAt.ToString("o", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@AtualizadoPor", configuration.UpdatedBy);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public IReadOnlyDictionary<string, string> ReadPersistedValues()
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();
            EnsureSchema(connection);
            return ReadValues(connection);
        }

        public static NumberingPolicy ResolveOrdemServicoNumberingPolicy(DbConnection connection, DateTime referenceDate)
        {
            var values = TryReadValues(connection);
            var prefix = ReadString(values, nameof(SystemConfiguration.OsNumberPrefix), "OS-{yyyy}-");
            var nextNumber = ReadInt(values, nameof(SystemConfiguration.OsNextNumber), 1);
            return new NumberingPolicy(BuildNumberPrefix(prefix, referenceDate), Math.Max(1, nextNumber));
        }

        public static NumberingPolicy ResolveOrcamentoNumberingPolicy(DbConnection connection, DateTime referenceDate)
        {
            var values = TryReadValues(connection);
            var prefix = ReadString(values, nameof(SystemConfiguration.OrcamentoNumberPrefix), "ORC-{yyyyMMdd}-");
            var nextNumber = ReadInt(values, nameof(SystemConfiguration.OrcamentoNextNumber), 1);
            return new NumberingPolicy(BuildNumberPrefix(prefix, referenceDate), Math.Max(1, nextNumber));
        }

        public static string BuildNumberPrefix(string? prefixTemplate, DateTime referenceDate)
        {
            var template = string.IsNullOrWhiteSpace(prefixTemplate) ? "DOC-{yyyy}-" : prefixTemplate.Trim();
            return template
                .Replace("{yyyyMMdd}", referenceDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{yyyy}", referenceDate.ToString("yyyy", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{yy}", referenceDate.ToString("yy", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{MM}", referenceDate.ToString("MM", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{dd}", referenceDate.ToString("dd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }

        private SystemConfiguration CreateDefault(string appDataPath)
        {
            var business = BusinessConfigurationService.LoadOrCreateDefault(appDataPath, _logger);
            var station = StationService.GetConfiguration(appDataPath);
            var theme = new ThemeService().GetCurrentTheme();

            return new SystemConfiguration
            {
                CompanyDisplayName = business.CompanyDisplayName,
                CompanyLegalName = business.CompanyLegalName,
                CompanyDocument = business.CompanyDocument,
                CompanyPhone = business.CompanyPhone,
                CompanyWhatsApp = business.CompanyWhatsApp,
                CompanyAddress = business.CompanyAddress,
                LogoPath = business.LogoPath,
                ReceiptHeader = business.ReceiptHeader,
                ReceiptFooter = business.ReceiptFooter,
                PreferredTheme = theme.ToString(),
                DefaultPrinterName = station.PreferredPdvPrinterName ?? string.Empty,
                UpdatedBy = "Sistema"
            };
        }

        private static void EnsureSchema(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? $@"
                IF OBJECT_ID(N'dbo.{TableName}', N'U') IS NULL
                BEGIN
                    CREATE TABLE {TableName}
                    (
                        Chave NVARCHAR(120) NOT NULL PRIMARY KEY,
                        Valor NVARCHAR(MAX) NOT NULL,
                        Grupo NVARCHAR(80) NOT NULL,
                        AtualizadoEm NVARCHAR(80) NOT NULL,
                        AtualizadoPor NVARCHAR(200)
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ConfiguracoesSistema_Grupo')
                BEGIN
                    CREATE INDEX IX_ConfiguracoesSistema_Grupo
                    ON {TableName} (Grupo);
                END;"
                : $@"
                CREATE TABLE IF NOT EXISTS {TableName}
                (
                    Chave TEXT PRIMARY KEY,
                    Valor TEXT NOT NULL,
                    Grupo TEXT NOT NULL,
                    AtualizadoEm TEXT NOT NULL,
                    AtualizadoPor TEXT
                );

                CREATE INDEX IF NOT EXISTS IX_ConfiguracoesSistema_Grupo
                ON {TableName} (Grupo);";
            command.ExecuteNonQuery();
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            var typeName = connection.GetType().FullName ?? string.Empty;
            return typeName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<string, string> TryReadValues(DbConnection connection)
        {
            try
            {
                EnsureSchema(connection);
                return ReadValues(connection);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static Dictionary<string, string> ReadValues(DbConnection connection)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Chave, Valor
                FROM {TableName};";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                values[reader.GetString(0)] = reader.GetString(1);
            }

            return values;
        }

        private static SystemConfiguration FromValues(IReadOnlyDictionary<string, string> values, SystemConfiguration fallback)
        {
            return new SystemConfiguration
            {
                CompanyDisplayName = ReadString(values, nameof(SystemConfiguration.CompanyDisplayName), fallback.CompanyDisplayName),
                CompanyLegalName = ReadString(values, nameof(SystemConfiguration.CompanyLegalName), fallback.CompanyLegalName),
                CompanyDocument = ReadString(values, nameof(SystemConfiguration.CompanyDocument), fallback.CompanyDocument),
                CompanyPhone = ReadString(values, nameof(SystemConfiguration.CompanyPhone), fallback.CompanyPhone),
                CompanyWhatsApp = ReadString(values, nameof(SystemConfiguration.CompanyWhatsApp), fallback.CompanyWhatsApp),
                CompanyAddress = ReadString(values, nameof(SystemConfiguration.CompanyAddress), fallback.CompanyAddress),
                LogoPath = ReadString(values, nameof(SystemConfiguration.LogoPath), fallback.LogoPath),
                ReceiptHeader = ReadString(values, nameof(SystemConfiguration.ReceiptHeader), fallback.ReceiptHeader),
                ReceiptFooter = ReadString(values, nameof(SystemConfiguration.ReceiptFooter), fallback.ReceiptFooter),
                PreferredTheme = ReadString(values, nameof(SystemConfiguration.PreferredTheme), fallback.PreferredTheme),
                AutoBackupEnabled = ReadBool(values, nameof(SystemConfiguration.AutoBackupEnabled), fallback.AutoBackupEnabled),
                AutoBackupRetentionCopies = ReadInt(values, nameof(SystemConfiguration.AutoBackupRetentionCopies), fallback.AutoBackupRetentionCopies),
                DefaultPrinterName = ReadString(values, nameof(SystemConfiguration.DefaultPrinterName), fallback.DefaultPrinterName),
                OsNumberPrefix = ReadString(values, nameof(SystemConfiguration.OsNumberPrefix), fallback.OsNumberPrefix),
                OsNextNumber = ReadInt(values, nameof(SystemConfiguration.OsNextNumber), fallback.OsNextNumber),
                OrcamentoNumberPrefix = ReadString(values, nameof(SystemConfiguration.OrcamentoNumberPrefix), fallback.OrcamentoNumberPrefix),
                OrcamentoNextNumber = ReadInt(values, nameof(SystemConfiguration.OrcamentoNextNumber), fallback.OrcamentoNextNumber),
                PermissionPolicySummary = ReadString(values, nameof(SystemConfiguration.PermissionPolicySummary), fallback.PermissionPolicySummary),
                MessageTemplateOrcamento = ReadString(values, nameof(SystemConfiguration.MessageTemplateOrcamento), fallback.MessageTemplateOrcamento),
                MessageTemplateOrdemPronta = ReadString(values, nameof(SystemConfiguration.MessageTemplateOrdemPronta), fallback.MessageTemplateOrdemPronta),
                MessageTemplateGarantia = ReadString(values, nameof(SystemConfiguration.MessageTemplateGarantia), fallback.MessageTemplateGarantia),
                DefaultWarrantyDays = ReadInt(values, nameof(SystemConfiguration.DefaultWarrantyDays), fallback.DefaultWarrantyDays),
                DefaultProductMarginPercent = ReadDecimal(values, nameof(SystemConfiguration.DefaultProductMarginPercent), fallback.DefaultProductMarginPercent),
                UpdatedAt = ReadDateTime(values, nameof(SystemConfiguration.UpdatedAt), fallback.UpdatedAt),
                UpdatedBy = ReadString(values, nameof(SystemConfiguration.UpdatedBy), fallback.UpdatedBy)
            };
        }

        private static IEnumerable<(string Key, string Value)> ToValues(SystemConfiguration configuration)
        {
            return new[]
            {
                Pair(nameof(SystemConfiguration.CompanyDisplayName), configuration.CompanyDisplayName),
                Pair(nameof(SystemConfiguration.CompanyLegalName), configuration.CompanyLegalName),
                Pair(nameof(SystemConfiguration.CompanyDocument), configuration.CompanyDocument),
                Pair(nameof(SystemConfiguration.CompanyPhone), configuration.CompanyPhone),
                Pair(nameof(SystemConfiguration.CompanyWhatsApp), configuration.CompanyWhatsApp),
                Pair(nameof(SystemConfiguration.CompanyAddress), configuration.CompanyAddress),
                Pair(nameof(SystemConfiguration.LogoPath), configuration.LogoPath),
                Pair(nameof(SystemConfiguration.ReceiptHeader), configuration.ReceiptHeader),
                Pair(nameof(SystemConfiguration.ReceiptFooter), configuration.ReceiptFooter),
                Pair(nameof(SystemConfiguration.PreferredTheme), configuration.PreferredTheme),
                Pair(nameof(SystemConfiguration.AutoBackupEnabled), configuration.AutoBackupEnabled),
                Pair(nameof(SystemConfiguration.AutoBackupRetentionCopies), configuration.AutoBackupRetentionCopies),
                Pair(nameof(SystemConfiguration.DefaultPrinterName), configuration.DefaultPrinterName),
                Pair(nameof(SystemConfiguration.OsNumberPrefix), configuration.OsNumberPrefix),
                Pair(nameof(SystemConfiguration.OsNextNumber), configuration.OsNextNumber),
                Pair(nameof(SystemConfiguration.OrcamentoNumberPrefix), configuration.OrcamentoNumberPrefix),
                Pair(nameof(SystemConfiguration.OrcamentoNextNumber), configuration.OrcamentoNextNumber),
                Pair(nameof(SystemConfiguration.PermissionPolicySummary), configuration.PermissionPolicySummary),
                Pair(nameof(SystemConfiguration.MessageTemplateOrcamento), configuration.MessageTemplateOrcamento),
                Pair(nameof(SystemConfiguration.MessageTemplateOrdemPronta), configuration.MessageTemplateOrdemPronta),
                Pair(nameof(SystemConfiguration.MessageTemplateGarantia), configuration.MessageTemplateGarantia),
                Pair(nameof(SystemConfiguration.DefaultWarrantyDays), configuration.DefaultWarrantyDays),
                Pair(nameof(SystemConfiguration.DefaultProductMarginPercent), configuration.DefaultProductMarginPercent),
                Pair(nameof(SystemConfiguration.UpdatedAt), configuration.UpdatedAt.ToString("o", CultureInfo.InvariantCulture)),
                Pair(nameof(SystemConfiguration.UpdatedBy), configuration.UpdatedBy)
            };
        }

        private static (string Key, string Value) Pair(string key, string? value)
        {
            return (key, value ?? string.Empty);
        }

        private static (string Key, string Value) Pair(string key, bool value)
        {
            return (key, value ? "true" : "false");
        }

        private static (string Key, string Value) Pair(string key, int value)
        {
            return (key, value.ToString(CultureInfo.InvariantCulture));
        }

        private static (string Key, string Value) Pair(string key, decimal value)
        {
            return (key, value.ToString(CultureInfo.InvariantCulture));
        }

        private static string ReadString(IReadOnlyDictionary<string, string> values, string key, string fallback)
        {
            return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : fallback;
        }

        private static bool ReadBool(IReadOnlyDictionary<string, string> values, string key, bool fallback)
        {
            return values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
                ? parsed
                : fallback;
        }

        private static int ReadInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
        {
            return values.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static decimal ReadDecimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback)
        {
            return values.TryGetValue(key, out var value) && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static DateTime ReadDateTime(IReadOnlyDictionary<string, string> values, string key, DateTime fallback)
        {
            return values.TryGetValue(key, out var value) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : fallback;
        }

        private static void Normalize(SystemConfiguration configuration)
        {
            configuration.CompanyDisplayName = NormalizeText(configuration.CompanyDisplayName, "Primo Auto Eletrica");
            configuration.CompanyLegalName = NormalizeText(configuration.CompanyLegalName, configuration.CompanyDisplayName);
            configuration.CompanyDocument = configuration.CompanyDocument?.Trim() ?? string.Empty;
            configuration.CompanyPhone = configuration.CompanyPhone?.Trim() ?? string.Empty;
            configuration.CompanyWhatsApp = configuration.CompanyWhatsApp?.Trim() ?? string.Empty;
            configuration.CompanyAddress = configuration.CompanyAddress?.Trim() ?? string.Empty;
            configuration.LogoPath = configuration.LogoPath?.Trim() ?? string.Empty;
            configuration.ReceiptHeader = NormalizeText(configuration.ReceiptHeader, "Comprovante nao fiscal");
            configuration.ReceiptFooter = NormalizeText(configuration.ReceiptFooter, "Obrigado pela preferencia.");
            configuration.PreferredTheme = string.Equals(configuration.PreferredTheme, nameof(AppTheme.Dark), StringComparison.OrdinalIgnoreCase)
                ? nameof(AppTheme.Dark)
                : nameof(AppTheme.Light);
            configuration.AutoBackupRetentionCopies = Math.Clamp(configuration.AutoBackupRetentionCopies, 1, 365);
            configuration.DefaultPrinterName = configuration.DefaultPrinterName?.Trim() ?? string.Empty;
            configuration.OsNumberPrefix = NormalizeText(configuration.OsNumberPrefix, "OS-{yyyy}-");
            configuration.OsNextNumber = Math.Max(1, configuration.OsNextNumber);
            configuration.OrcamentoNumberPrefix = NormalizeText(configuration.OrcamentoNumberPrefix, "ORC-{yyyyMMdd}-");
            configuration.OrcamentoNextNumber = Math.Max(1, configuration.OrcamentoNextNumber);
            configuration.PermissionPolicySummary = NormalizeText(configuration.PermissionPolicySummary, "Controle por perfis e permissoes persistidas.");
            configuration.MessageTemplateOrcamento = NormalizeText(configuration.MessageTemplateOrcamento, "Ola {Cliente}, seu orcamento {Numero} esta pronto para aprovacao. Total: {Total}.");
            configuration.MessageTemplateOrdemPronta = NormalizeText(configuration.MessageTemplateOrdemPronta, "Ola {Cliente}, seu veiculo {Veiculo} esta pronto para retirada. OS {Numero}.");
            configuration.MessageTemplateGarantia = NormalizeText(configuration.MessageTemplateGarantia, "Servico com garantia padrao de {DiasGarantia} dias mediante apresentacao da OS.");
            configuration.DefaultWarrantyDays = Math.Clamp(configuration.DefaultWarrantyDays, 0, 3650);
            configuration.DefaultProductMarginPercent = Math.Clamp(configuration.DefaultProductMarginPercent, 0m, 1000m);
            configuration.UpdatedBy = NormalizeText(configuration.UpdatedBy, "Sistema");
        }

        private static string NormalizeText(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
