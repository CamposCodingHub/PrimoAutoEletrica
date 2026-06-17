using System;
using System.IO;
using System.Text.Json;

namespace PrimoAutoEletrica.Services
{
    public sealed class BusinessConfiguration
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
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string EffectiveCompanyName => string.IsNullOrWhiteSpace(CompanyDisplayName)
            ? "Primo Auto Eletrica"
            : CompanyDisplayName.Trim();
    }

    public sealed record LogoValidationResult(bool IsValid, string Message);

    public static class BusinessConfigurationService
    {
        private const string ConfigurationFileName = "business-config.json";
        private static readonly string[] SupportedLogoExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };

        public static string GetConfigurationFilePath(string appDataPath)
        {
            if (string.IsNullOrWhiteSpace(appDataPath))
            {
                throw new ArgumentException("O caminho de dados da aplicacao nao foi informado.", nameof(appDataPath));
            }

            return Path.Combine(appDataPath, ConfigurationFileName);
        }

        public static BusinessConfiguration LoadOrCreateDefault(string appDataPath, LoggerService? logger = null)
        {
            var path = GetConfigurationFilePath(appDataPath);

            if (!File.Exists(path))
            {
                var defaultConfiguration = new BusinessConfiguration();
                Save(appDataPath, defaultConfiguration);
                return defaultConfiguration;
            }

            try
            {
                var json = File.ReadAllText(path);
                var configuration = JsonSerializer.Deserialize<BusinessConfiguration>(json) ?? new BusinessConfiguration();
                Normalize(configuration);
                return configuration;
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Falha ao ler configuracao comercial '{path}': {ex.Message}");
                return new BusinessConfiguration();
            }
        }

        public static void Save(string appDataPath, BusinessConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            Directory.CreateDirectory(appDataPath);
            Normalize(configuration);
            configuration.UpdatedAt = DateTime.Now;

            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(GetConfigurationFilePath(appDataPath), json);
        }

        public static LogoValidationResult ValidateLogoPath(string? logoPath)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                return new LogoValidationResult(true, "Nenhum logo configurado. O comprovante usara apenas texto.");
            }

            var normalizedPath = logoPath.Trim();
            if (!File.Exists(normalizedPath))
            {
                return new LogoValidationResult(false, $"Logo nao encontrado: {normalizedPath}");
            }

            var extension = Path.GetExtension(normalizedPath);
            if (!SupportedLogoExtensions.Any(item => string.Equals(item, extension, StringComparison.OrdinalIgnoreCase)))
            {
                return new LogoValidationResult(false, "Formato de logo nao suportado. Use PNG, JPG, JPEG ou BMP.");
            }

            var fileInfo = new FileInfo(normalizedPath);
            if (fileInfo.Length == 0)
            {
                return new LogoValidationResult(false, "Arquivo de logo esta vazio.");
            }

            return new LogoValidationResult(true, $"Logo valido: {fileInfo.Name} ({FormatFileSize(fileInfo.Length)}).");
        }

        private static void Normalize(BusinessConfiguration configuration)
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
        }

        private static string NormalizeText(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
