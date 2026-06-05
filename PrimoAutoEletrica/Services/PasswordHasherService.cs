using System;
using System.Security.Cryptography;

namespace PrimoAutoEletrica.Services
{
    public static class PasswordHasherService
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;
        private const string Prefix = "PBKDF2";

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Senha nao pode ser vazia.", nameof(password));
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedPassword))
            {
                return false;
            }

            if (!IsHashed(storedPassword))
            {
                return string.Equals(password, storedPassword, StringComparison.Ordinal);
            }

            var parts = storedPassword.Split('$');
            if (parts.Length != 4 ||
                !int.TryParse(parts[1], out var iterations))
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);
                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static bool NeedsRehash(string storedPassword)
        {
            if (!IsHashed(storedPassword))
            {
                return true;
            }

            var parts = storedPassword.Split('$');
            return parts.Length != 4 ||
                   !int.TryParse(parts[1], out var iterations) ||
                   iterations < Iterations;
        }

        private static bool IsHashed(string value)
        {
            return value.StartsWith($"{Prefix}$", StringComparison.Ordinal);
        }
    }
}
