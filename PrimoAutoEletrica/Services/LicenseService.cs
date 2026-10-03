using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public class LicenseService
    {
        private readonly string _appDataPath;
        private readonly LoggerService? _logger;
        private static readonly byte[] MasterSecretKey = Encoding.UTF8.GetBytes("PRIMOX-ENTERPRISE-LICENSING-HMAC-2026-KEY!#7842");

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
                sb.Append(Environment.MachineName);
                sb.Append(Environment.UserName);
                sb.Append(Environment.ProcessorCount);
                sb.Append(Environment.OSVersion.Platform.ToString());

                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao gerar hardware ID: {ex.Message}", ex);
                return "DEFAULT-HWID-01";
            }
        }

        public string CalcularAssinaturaLicenca(LicenseInfo license)
        {
            if (license == null) throw new ArgumentNullException(nameof(license));

            var canonical = FormatarDadosParaAssinatura(license);
            using var hmac = new HMACSHA256(MasterSecretKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToBase64String(hash);
        }

        public bool ValidarAssinaturaLicenca(LicenseInfo license)
        {
            if (license == null || string.IsNullOrWhiteSpace(license.Signature))
                return false;

            try
            {
                var expectedSignature = CalcularAssinaturaLicenca(license);
                var actualBytes = Encoding.UTF8.GetBytes(license.Signature);
                var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);

                return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
            }
            catch
            {
                return false;
            }
        }

        public LicenseValidationResult ValidateLicense(string? licenseKey = null)
        {
            var result = new LicenseValidationResult
            {
                ValidationTime = DateTime.Now
            };

            try
            {
                var license = !string.IsNullOrWhiteSpace(licenseKey)
                    ? LoadLicense(licenseKey)
                    : ObterLicencaAtiva();

                if (license == null)
                {
                    result.IsValid = false;
                    result.Message = "Nenhuma licença ativa encontrada no sistema.";
                    return result;
                }

                // 1. Verificar integridade criptográfica (anti-tampering)
                if (!ValidarAssinaturaLicenca(license))
                {
                    result.IsValid = false;
                    result.Message = "Assinatura digital da licença inválida ou corrompida. O arquivo de licença foi adulterado.";
                    _logger?.LogError("[LicenseService] Falha na validação de assinatura HMAC da licença.");
                    return result;
                }

                // 2. Verificar expiração
                if (license.IsExpired)
                {
                    result.IsValid = false;
                    result.Message = $"Licença expirou em {license.ExpirationDate:dd/MM/yyyy}. Entre em contato com o suporte PRIMOX.";
                    return result;
                }

                // 3. Verificar Hardware ID (exceto para Trial genérico onde HardwareId pode ser vazio)
                var currentHardwareId = GenerateHardwareId();
                if (!string.IsNullOrEmpty(license.HardwareId) && !string.Equals(license.HardwareId, currentHardwareId, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Message = $"Licença vinculada a outra máquina (HWID esperado: {license.HardwareId}, atual: {currentHardwareId}).";
                    return result;
                }

                // 4. Verificar se está ativa
                if (!license.IsActive)
                {
                    result.IsValid = false;
                    result.Message = "Licença desativada administrativamente.";
                    return result;
                }

                result.IsValid = true;
                result.License = license;
                result.Message = $"Licença válida ({license.Type}). Expira em {license.ExpirationDate:dd/MM/yyyy} ({license.RemainingDays} dias restantes).";

                license.LastValidation = DateTime.Now;
                SaveLicense(license);

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Erro ao validar licença: {ex.Message}";
                _logger?.LogError($"[LicenseService] Erro ao validar licença: {ex.Message}", ex);
                return result;
            }
        }

        public LicenseInfo ObterLicencaAtiva()
        {
            var configDir = Path.Combine(_appDataPath, "Config");
            if (Directory.Exists(configDir))
            {
                var files = Directory.GetFiles(configDir, "license_*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var lic = JsonSerializer.Deserialize<LicenseInfo>(json);
                        if (lic != null && lic.IsActive && ValidarAssinaturaLicenca(lic) && !lic.IsExpired)
                        {
                            return lic;
                        }
                    }
                    catch
                    {
                        // Prossegue para o próximo
                    }
                }
            }

            // Se nenhuma licença foi encontrada, gerar licença Trial de avaliação (15 dias)
            return GerarOuCarregarTrial();
        }

        private LicenseInfo GerarOuCarregarTrial()
        {
            var trialPath = Path.Combine(_appDataPath, "Config", "license_trial.json");
            if (File.Exists(trialPath))
            {
                try
                {
                    var json = File.ReadAllText(trialPath);
                    var trial = JsonSerializer.Deserialize<LicenseInfo>(json);
                    if (trial != null && ValidarAssinaturaLicenca(trial))
                    {
                        return trial;
                    }
                }
                catch
                {
                    // Recria trial se corrompido
                }
            }

            var hwid = GenerateHardwareId();
            var novoTrial = new LicenseInfo
            {
                LicenseKey = "TRIAL-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(),
                CompanyName = "Período de Avaliação Corporativa PRIMOX",
                Cnpj = "00.000.000/0001-00",
                PurchaseDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddDays(15),
                Type = LicenseType.Trial,
                MaxUsers = 3,
                MaxComputers = 3,
                MaxFiliais = 2,
                IsActive = true,
                HardwareId = hwid,
                LastValidation = DateTime.Now
            };

            novoTrial.Signature = CalcularAssinaturaLicenca(novoTrial);
            SaveLicense(novoTrial);
            _logger?.LogInfo($"[LicenseService] Licença Trial criada para HWID {hwid} válida até {novoTrial.ExpirationDate:dd/MM/yyyy}.");
            return novoTrial;
        }

        public string GerarTokenAtivacaoCompacto(LicenseInfo license)
        {
            if (license == null) throw new ArgumentNullException(nameof(license));
            license.Signature = CalcularAssinaturaLicenca(license);

            var json = JsonSerializer.Serialize(license);
            var bytes = Encoding.UTF8.GetBytes(json);
            return "PRMX-" + Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public LicenseValidationResult AtivarComToken(string token)
        {
            var result = new LicenseValidationResult { ValidationTime = DateTime.Now };

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    result.IsValid = false;
                    result.Message = "Chave de ativação não informada.";
                    return result;
                }

                var cleanToken = token.Trim();
                if (cleanToken.StartsWith("PRMX-", StringComparison.OrdinalIgnoreCase))
                {
                    cleanToken = cleanToken.Substring(5);
                }

                var base64 = cleanToken.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                var jsonBytes = Convert.FromBase64String(base64);
                var json = Encoding.UTF8.GetString(jsonBytes);
                var license = JsonSerializer.Deserialize<LicenseInfo>(json);

                if (license == null)
                {
                    result.IsValid = false;
                    result.Message = "Formato de chave de ativação inválido.";
                    return result;
                }

                // Validar assinatura criptográfica
                if (!ValidarAssinaturaLicenca(license))
                {
                    result.IsValid = false;
                    result.Message = "Chave de ativação inválida ou chave adulterada.";
                    return result;
                }

                // Validar Hardware ID se a licença especificar
                var currentHwid = GenerateHardwareId();
                if (!string.IsNullOrEmpty(license.HardwareId) && !string.Equals(license.HardwareId, currentHwid, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Message = $"Esta licença foi gerada para outra máquina (ID esperado: {license.HardwareId}).";
                    return result;
                }

                if (license.IsExpired)
                {
                    result.IsValid = false;
                    result.Message = "Esta chave de licença já está expirada.";
                    return result;
                }

                license.IsActive = true;
                license.HardwareId = currentHwid; // Vincula à máquina atual
                license.LastValidation = DateTime.Now;
                license.Signature = CalcularAssinaturaLicenca(license); // Reassina com HWID vinculado

                SaveLicense(license);

                result.IsValid = true;
                result.License = license;
                result.Message = $"Licença {license.Type} ativada com sucesso para {license.CompanyName}! Válida até {license.ExpirationDate:dd/MM/yyyy}.";
                _logger?.LogInfo($"[LicenseService] Licença ativada com sucesso: {license.CompanyName} ({license.LicenseKey})");
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Falha ao processar ativação: {ex.Message}";
                _logger?.LogError($"[LicenseService] Erro ao ativar token: {ex.Message}", ex);
                return result;
            }
        }

        public bool ActivateLicense(string licenseKey, string companyName, string cnpj, LicenseType type = LicenseType.SingleUser, int diasValidade = 365, int maxFiliais = 1)
        {
            try
            {
                var license = new LicenseInfo
                {
                    LicenseKey = licenseKey,
                    CompanyName = companyName,
                    Cnpj = cnpj,
                    PurchaseDate = DateTime.Now,
                    ExpirationDate = DateTime.Now.AddDays(diasValidade),
                    Type = type,
                    MaxUsers = type == LicenseType.Enterprise ? 50 : 5,
                    MaxComputers = type == LicenseType.Enterprise ? 50 : 5,
                    MaxFiliais = maxFiliais,
                    IsActive = true,
                    HardwareId = GenerateHardwareId(),
                    LastValidation = DateTime.Now
                };

                license.Signature = CalcularAssinaturaLicenca(license);
                SaveLicense(license);
                _logger?.LogInfo($"Licença ativada: {companyName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao ativar licença: {ex.Message}", ex);
                return false;
            }
        }

        public LicenseInfo? LoadLicense(string licenseKey)
        {
            try
            {
                var licensePath = GetLicensePath(licenseKey);
                if (!File.Exists(licensePath))
                {
                    return null;
                }

                var json = File.ReadAllText(licensePath);
                return JsonSerializer.Deserialize<LicenseInfo>(json);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao carregar licença: {ex.Message}", ex);
                return null;
            }
        }

        public bool SaveLicense(LicenseInfo license)
        {
            try
            {
                var licensePath = GetLicensePath(license.LicenseKey);
                var directory = Path.GetDirectoryName(licensePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(licensePath, json);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao salvar licença: {ex.Message}", ex);
                return false;
            }
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
                license.Signature = CalcularAssinaturaLicenca(license);
                SaveLicense(license);
                _logger?.LogInfo($"Licença desativada: {license.CompanyName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao desativar licença: {ex.Message}", ex);
                return false;
            }
        }

        private string GetLicensePath(string licenseKey)
        {
            var safeKey = string.Join("_", licenseKey.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_appDataPath, "Config", $"license_{safeKey}.json");
        }

        private static string FormatarDadosParaAssinatura(LicenseInfo license)
        {
            var expDate = license.ExpirationDate.ToString("yyyyMMdd");
            var hwid = license.HardwareId ?? "";
            return $"{license.LicenseKey}|{license.CompanyName}|{license.Cnpj}|{expDate}|{(int)license.Type}|{license.MaxUsers}|{license.MaxComputers}|{license.MaxFiliais}|{hwid}|{license.IsActive}";
        }
    }
}
