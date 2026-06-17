using System;
using System.Security.Cryptography;
using System.Text;

namespace PrimoAutoEletrica.Services.DatabaseProviders
{
    /// <summary>
    /// Serviço de criptografia para senhas de banco de dados usando DPAPI.
    /// </summary>
    public static class DatabaseEncryptionService
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PrimoAutoEletrica.DatabaseEncryption");

        /// <summary>
        /// Criptografa uma senha usando DPAPI.
        /// </summary>
        /// <param name="password">Senha a ser criptografada.</param>
        /// <returns>Senha criptografada em base64.</returns>
        public static string EncryptPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return string.Empty;

            try
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var encryptedBytes = ProtectedData.Protect(passwordBytes, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao criptografar senha.", ex);
            }
        }

        /// <summary>
        /// Descriptografa uma senha usando DPAPI.
        /// </summary>
        /// <param name="encryptedPassword">Senha criptografada em base64.</param>
        /// <returns>Senha descriptografada.</returns>
        public static string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrWhiteSpace(encryptedPassword))
                return string.Empty;

            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedPassword);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao descriptografar senha.", ex);
            }
        }

        /// <summary>
        /// Verifica se uma senha está criptografada.
        /// </summary>
        /// <param name="password">Senha a ser verificada.</param>
        /// <returns>True se a senha está criptografada, false caso contrário.</returns>
        public static bool IsEncrypted(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                var bytes = Convert.FromBase64String(password);
                return bytes.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
