using System;
using System.Security.Cryptography;

namespace PrimoAutoEletrica.Services
{
    public static class CryptoService
    {
        public static string ProtectString(string plain)
        {
            if (plain == null) return string.Empty;
            var bytes = System.Text.Encoding.UTF8.GetBytes(plain);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        public static string UnprotectString(string protectedBase64)
        {
            if (string.IsNullOrEmpty(protectedBase64)) return string.Empty;
            try
            {
                var bytes = Convert.FromBase64String(protectedBase64);
                var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(unprotected);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
