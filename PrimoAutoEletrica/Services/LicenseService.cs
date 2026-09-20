// SCAFFOLD_ONLY: licenca local JSON + SHA256 — SEM assinatura RSA/ECDSA e SEM license server.
// Nao tratar ActivateLicense como protecao comercial. Ver Docs/audit/2026-09-19.
using System;
using System.Security.Cryptography;
using System.Text;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public class LicenseService
    {
        /// <summary>P0.05: JSON local + SHA256 apenas — NAO e license server comercial (RSA/ECDSA).</summary>
        public const bool IsCommercialScaffoldOnly = true;
        private readonly string _appDataPath;
        private readonly LoggerService? _logger;

        public LicenseService(string appDataPath, LoggerService? logger = null)
        {
            _appDataPath = appDataPath;
            _logger = logger;
        }

        public string GenerateHardwareId()
        {
            try
            {
                var sb = new StringBuilder();
                
                // Usar MachineName e UserName como identificador simples
                sb.Append(Environment.MachineName);
                sb.Append(Environment.UserName);
                sb.Append(Environment.ProcessorCount);
                sb.Append(Environment.OSVersion.VersionString);

                // Hash do hardware ID
                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao gerar hardware ID: {ex.Message}", ex);
                return "UNKNOWN";
            }
        }

        public LicenseValidationResult ValidateLicense(string licenseKey)
        {
            var result = new LicenseValidationResult
            {
                ValidationTime = DateTime.Now
            };

            try
            {
                // Carregar licenÃ§a do arquivo
                var license = LoadLicense(licenseKey);
                if (license == null)
                {
                    result.IsValid = false;
                    result.Message = "LicenÃ§a nÃ£o encontrada";
                    return result;
                }

                // Verificar expiraÃ§Ã£o
                if (DateTime.Now > license.ExpirationDate)
                {
                    result.IsValid = false;
                    result.Message = "LicenÃ§a expirada";
                    return result;
                }

                // Verificar hardware ID
                var currentHardwareId = GenerateHardwareId();
                if (!string.IsNullOrEmpty(license.HardwareId) && license.HardwareId != currentHardwareId)
                {
                    result.IsValid = false;
                    result.Message = "LicenÃ§a nÃ£o vÃ¡lida para este computador";
                    return result;
                }

                // Verificar ativaÃ§Ã£o
                if (!license.IsActive)
                {
                    result.IsValid = false;
                    result.Message = "LicenÃ§a nÃ£o ativada";
                    return result;
                }

                result.IsValid = true;
                result.Message = $"LicenÃ§a vÃ¡lida. Expira em {license.ExpirationDate:dd/MM/yyyy}";
                result.License = license;

                // Atualizar Ãºltima validaÃ§Ã£o
                license.LastValidation = DateTime.Now;
                SaveLicense(license);

                _logger?.LogInfo($"LicenÃ§a validada com sucesso: {license.CompanyName}");
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Erro ao validar licenÃ§a: {ex.Message}";
                _logger?.LogError($"Erro ao validar licenÃ§a: {ex.Message}", ex);
                return result;
            }
        }

        public bool ActivateLicense(string licenseKey, string companyName, string cnpj)
        {
            try
            {
                var license = new LicenseInfo
                {
                    LicenseKey = licenseKey,
                    CompanyName = companyName,
                    Cnpj = cnpj,
                    PurchaseDate = DateTime.Now,
                    ExpirationDate = DateTime.Now.AddYears(1),
                    Type = LicenseType.SingleUser,
                    MaxUsers = 1,
                    MaxComputers = 1,
                    IsActive = true,
                    HardwareId = GenerateHardwareId(),
                    LastValidation = DateTime.Now
                };

                SaveLicense(license);
                _logger?.LogInfo($"LicenÃ§a ativada: {companyName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao ativar licenÃ§a: {ex.Message}", ex);
                return false;
            }
        }

        public LicenseInfo? LoadLicense(string licenseKey)
        {
            try
            {
                var licensePath = GetLicensePath(licenseKey);
                if (!System.IO.File.Exists(licensePath))
                {
                    return null;
                }

                var json = System.IO.File.ReadAllText(licensePath);
                return System.Text.Json.JsonSerializer.Deserialize<LicenseInfo>(json);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao carregar licenÃ§a: {ex.Message}", ex);
                return null;
            }
        }

        public bool SaveLicense(LicenseInfo license)
        {
            try
            {
                var licensePath = GetLicensePath(license.LicenseKey);
                var directory = System.IO.Path.GetDirectoryName(licensePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                var json = System.Text.Json.JsonSerializer.Serialize(license, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(licensePath, json);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao salvar licenÃ§a: {ex.Message}", ex);
                return false;
            }
        }

        private string GetLicensePath(string licenseKey)
        {
            return System.IO.Path.Combine(_appDataPath, "Config", $"license_{licenseKey}.json");
        }

        public bool DeactivateLicense(string licenseKey)
        {
            try
            {
                var license = LoadLicense(licenseKey);
                if (license == null)
                {
                    return false;
                }

                license.IsActive = false;
                SaveLicense(license);
                _logger?.LogInfo($"LicenÃ§a desativada: {license.CompanyName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao desativar licenÃ§a: {ex.Message}", ex);
                return false;
            }
        }
    }
}

