using OtpNet;
using System;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço de autenticação de dois fatores (2FA) usando TOTP (Time-based One-Time Password).
    /// Compatível com Google Authenticator, Microsoft Authenticator e Authy.
    /// </summary>
    public interface ITwoFactorService
    {
        /// <summary>
        /// Gera uma chave secreta para configuração de 2FA de um usuário.
        /// </summary>
        /// <param name="userIdentifier">Email ou nome de usuário para exibição no app authenticator</param>
        /// <returns>Tupla com chave secreta (Base32) e URL otpauth para QR Code</returns>
        (string SecretKey, string OtpAuthUrl) GenerateSecret(string userIdentifier);

        /// <summary>
        /// Verifica se o código TOTP de 6 dígitos é válido para a chave secreta informada.
        /// Permite margem de ±1 time step (30 segundos) para compensar diferenças de relógio.
        /// </summary>
        /// <param name="secretKey">Chave secreta Base32 do usuário</param>
        /// <param name="code">Código TOTP de 6 dígitos informado pelo usuário</param>
        /// <returns>true se o código for válido</returns>
        bool VerifyCode(string secretKey, string code);

        /// <summary>
        /// Gera o código TOTP atual para uma chave secreta (útil para testes).
        /// </summary>
        /// <param name="secretKey">Chave secreta Base32 do usuário</param>
        /// <returns>Código TOTP de 6 dígitos</returns>
        string GenerateCurrentCode(string secretKey);
    }

    /// <summary>
    /// Implementação do serviço 2FA usando a biblioteca Otp.NET (TOTP - RFC 6238).
    /// </summary>
    public class TwoFactorService : ITwoFactorService
    {
        private const string Issuer = "PrimoAutoEletrica";
        private const int SecretKeyLength = 20; // 160 bits, padrão RFC
        private const int TimeStepSeconds = 30;
        private const int CodeDigits = 6;

        private readonly LoggerService _logger;

        public TwoFactorService(LoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public (string SecretKey, string OtpAuthUrl) GenerateSecret(string userIdentifier)
        {
            if (string.IsNullOrWhiteSpace(userIdentifier))
            {
                throw new ArgumentException("Identificador do usuario nao pode ser vazio.", nameof(userIdentifier));
            }

            try
            {
                // Gerar chave secreta aleatória
                var key = KeyGeneration.GenerateRandomKey(SecretKeyLength);
                var secretKey = Base32Encoding.ToString(key);

                // Gerar URL otpauth:// para geração de QR Code
                // Formato: otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&digits={digits}&period={period}
                var encodedIssuer = Uri.EscapeDataString(Issuer);
                var encodedUser = Uri.EscapeDataString(userIdentifier);
                var otpAuthUrl = $"otpauth://totp/{encodedIssuer}:{encodedUser}?secret={secretKey}&issuer={encodedIssuer}&digits={CodeDigits}&period={TimeStepSeconds}";

                _logger.LogInfo($"Chave 2FA gerada para usuario: {userIdentifier}");
                return (secretKey, otpAuthUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao gerar chave 2FA.", ex);
                throw;
            }
        }

        public bool VerifyCode(string secretKey, string code)
        {
            if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            // Remover espaços que o usuário possa ter digitado
            code = code.Replace(" ", "").Trim();

            if (code.Length != CodeDigits)
            {
                return false;
            }

            try
            {
                var secretBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(secretBytes, step: TimeStepSeconds, totpSize: CodeDigits);

                // VerificationWindow.RfcSpecifiedNetworkDelay permite ±1 time step
                var isValid = totp.VerifyTotp(code, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);

                if (!isValid)
                {
                    _logger.LogWarning("Codigo 2FA invalido informado.", "Seguranca");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao verificar codigo 2FA.", ex);
                return false;
            }
        }

        public string GenerateCurrentCode(string secretKey)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentException("Chave secreta nao pode ser vazia.", nameof(secretKey));
            }

            try
            {
                var secretBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(secretBytes, step: TimeStepSeconds, totpSize: CodeDigits);
                return totp.ComputeTotp();
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao gerar codigo 2FA.", ex);
                throw;
            }
        }
    }
}
