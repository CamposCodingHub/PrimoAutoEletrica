using System;

namespace PrimoAutoEletrica.Models
{
    public class LicenseInfo
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public LicenseType Type { get; set; }
        public int MaxUsers { get; set; }
        public int MaxComputers { get; set; }
        public int MaxFiliais { get; set; } = 1;
        public bool IsActive { get; set; }
        public string Features { get; set; } = string.Empty;
        public string? HardwareId { get; set; }
        public DateTime LastValidation { get; set; }
        public string Signature { get; set; } = string.Empty;

        public int RemainingDays => Math.Max(0, (ExpirationDate.Date - DateTime.Today).Days);
        public bool IsExpired => DateTime.Today > ExpirationDate.Date;
    }

    public enum LicenseType
    {
        Trial = 0,
        SingleUser = 1,
        MultiUser = 2,
        Enterprise = 3
    }

    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public LicenseInfo? License { get; set; }
        public DateTime ValidationTime { get; set; }
    }
}
