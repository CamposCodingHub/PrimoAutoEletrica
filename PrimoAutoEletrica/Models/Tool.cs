using System;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Models
{
    public enum ToolStatus
    {
        AVAILABLE = 0,
        IN_USE = 1,
        BORROWED = 2,
        MAINTENANCE = 3,
        DAMAGED = 4,
        LOST = 5,
        RETIRED = 6
    }

    public enum ToolCondition
    {
        OK = 0,
        DAMAGED = 1,
        NEEDS_CALIBRATION = 2,
        DIRTY = 3,
        MISSING_ACCESSORIES = 4
    }

    public enum ToolCheckoutStatus
    {
        OPEN = 0,
        RETURNED = 1,
        OVERDUE = 2
    }

    public enum ToolMaintenanceType
    {
        PREVENTIVE = 0,
        CORRECTIVE = 1,
        CALIBRATION = 2,
        INSPECTION = 3,
        DAMAGE_REPAIR = 4,
        DISPOSAL = 5
    }

    public enum MaintenanceStatus
    {
        SCHEDULED = 0,
        IN_PROGRESS = 1,
        COMPLETED = 2,
        CANCELLED = 3
    }

    /// <summary>
    /// Representa uma ferramenta física e o patrimônio operacional técnico da oficina.
    /// Custos monetários utilizam estritamente o padrão CentsV1 (long cents).
    /// </summary>
    public sealed class Tool
    {
        public Guid ToolId { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Geral";
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? PatrimonyNumber { get; set; }
        public string? Description { get; set; }
        public string? PhotoPath { get; set; }
        public string LocationName { get; set; } = "Bancada Geral";
        public int? CurrentResponsibleUserId { get; set; }
        public string? CurrentResponsibleUserName { get; set; }
        public ToolStatus Status { get; set; } = ToolStatus.AVAILABLE;
        public DateTime? PurchaseDate { get; set; }
        public long PurchaseValueCents { get; set; }
        public DateTime? WarrantyExpiration { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string? Notes { get; set; }
        public int RowVersion { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Helpers de visualização e compatibilidade CentsV1
        public decimal PurchaseValue
        {
            get => PurchaseValueCents / 100m;
            set => PurchaseValueCents = MoneyCents.FromDecimal(value).Cents;
        }

        public bool IsAvailable => Status == ToolStatus.AVAILABLE;
        public bool IsInUse => Status == ToolStatus.IN_USE;
        public bool IsMaintenance => Status == ToolStatus.MAINTENANCE;
        public bool IsDamaged => Status == ToolStatus.DAMAGED;

        public string StatusDisplay => Status switch
        {
            ToolStatus.AVAILABLE => "Disponível",
            ToolStatus.IN_USE => "Em Uso",
            ToolStatus.BORROWED => "Emprestada",
            ToolStatus.MAINTENANCE => "Em Manutenção",
            ToolStatus.DAMAGED => "Avariada",
            ToolStatus.LOST => "Extraviada",
            ToolStatus.RETIRED => "Baixada",
            _ => Status.ToString()
        };

        public string QrCodeUri => $"PRIMOX://TOOL/{Code}";
    }

    /// <summary>
    /// Registro de custódia e histórico de retirada / devolução da ferramenta.
    /// </summary>
    public sealed class ToolCheckout
    {
        public Guid CheckoutId { get; set; } = Guid.NewGuid();
        public Guid ToolId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid? WorkOrderId { get; set; }
        public string? WorkOrderNumber { get; set; }
        public string? VehiclePlate { get; set; }
        public DateTime CheckoutDate { get; set; } = DateTime.Now;
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int? ReturnedByUserId { get; set; }
        public ToolCondition? ReturnCondition { get; set; }
        public string? CheckoutNotes { get; set; }
        public string? ReturnNotes { get; set; }
        public ToolCheckoutStatus Status { get; set; } = ToolCheckoutStatus.OPEN;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Registro de manutenção preventiva, corretiva ou calibração metrológica.
    /// </summary>
    public sealed class ToolMaintenance
    {
        public Guid MaintenanceId { get; set; } = Guid.NewGuid();
        public Guid ToolId { get; set; }
        public ToolMaintenanceType MaintenanceType { get; set; } = ToolMaintenanceType.PREVENTIVE;
        public string Description { get; set; } = string.Empty;
        public long CostCents { get; set; }
        public string? Provider { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime? CompletionDate { get; set; }
        public string? PerformedBy { get; set; }
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.COMPLETED;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public decimal Cost
        {
            get => CostCents / 100m;
            set => CostCents = MoneyCents.FromDecimal(value).Cents;
        }
    }
}
