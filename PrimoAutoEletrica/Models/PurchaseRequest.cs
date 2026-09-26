using System;
using System.Collections.Generic;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Models
{
    public enum PurchasePriority
    {
        LOW = 0,
        NORMAL = 1,
        HIGH = 2,
        URGENT = 3
    }

    public enum PurchaseReason
    {
        LOW_STOCK = 0,
        OUT_OF_STOCK = 1,
        DAMAGED = 2,
        NEW_SERVICE = 3,
        MAINTENANCE = 4,
        EMPLOYEE_REQUEST = 5,
        PREVENTIVE = 6,
        WORK_ORDER = 7,
        OTHER = 8
    }

    public enum PurchaseRequestStatus
    {
        DRAFT = 0,
        REQUESTED = 1,
        APPROVED = 2,
        QUOTING = 3,
        ORDERED = 4,
        PARTIALLY_RECEIVED = 5,
        RECEIVED = 6,
        CANCELLED = 7
    }

    public enum PurchaseItemStatus
    {
        PENDING = 0,
        ORDERED = 1,
        RECEIVED = 2,
        CANCELLED = 3
    }

    /// <summary>
    /// Representa uma requisição ou necessidade de compra formal na oficina.
    /// Custos monetários utilizam estritamente o padrão CentsV1 (long cents).
    /// </summary>
    public sealed class PurchaseRequest
    {
        public Guid PurchaseRequestId { get; set; } = Guid.NewGuid();
        public string Number { get; set; } = string.Empty;
        public int RequestedByUserId { get; set; }
        public string RequestedByUserName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public PurchasePriority Priority { get; set; } = PurchasePriority.NORMAL;
        public PurchaseReason Reason { get; set; } = PurchaseReason.LOW_STOCK;
        public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.REQUESTED;
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? CancelledByUserId { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public long TotalEstimatedCostCents { get; set; }
        public long TotalActualCostCents { get; set; }
        public string? FiscalDocumentNumber { get; set; }
        public string? Notes { get; set; }
        public int RowVersion { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<PurchaseRequestItem> Items { get; set; } = new();

        public decimal TotalEstimatedCost
        {
            get => TotalEstimatedCostCents / 100m;
            set => TotalEstimatedCostCents = MoneyCents.FromDecimal(value).Cents;
        }

        public decimal TotalActualCost
        {
            get => TotalActualCostCents / 100m;
            set => TotalActualCostCents = MoneyCents.FromDecimal(value).Cents;
        }

        public bool IsUrgent => Priority == PurchasePriority.URGENT;
        public bool IsApproved => Status == PurchaseRequestStatus.APPROVED;
        public bool CanApprove => Status == PurchaseRequestStatus.REQUESTED || Status == PurchaseRequestStatus.QUOTING;
        public bool CanReceive => Status == PurchaseRequestStatus.ORDERED || Status == PurchaseRequestStatus.PARTIALLY_RECEIVED;

        public string PriorityDisplay => Priority switch
        {
            PurchasePriority.URGENT => "🔴 Urgente",
            PurchasePriority.HIGH => "🟠 Alta",
            PurchasePriority.NORMAL => "Normal",
            PurchasePriority.LOW => "Baixa",
            _ => Priority.ToString()
        };

        public string StatusDisplay => Status switch
        {
            PurchaseRequestStatus.DRAFT => "Rascunho",
            PurchaseRequestStatus.REQUESTED => "Aguardando Aprovação",
            PurchaseRequestStatus.APPROVED => "Aprovada",
            PurchaseRequestStatus.QUOTING => "Em Cotação",
            PurchaseRequestStatus.ORDERED => "Pedido Realizado",
            PurchaseRequestStatus.PARTIALLY_RECEIVED => "Recebida Parcialmente",
            PurchaseRequestStatus.RECEIVED => "Recebida",
            PurchaseRequestStatus.CANCELLED => "Cancelada",
            _ => Status.ToString()
        };
    }

    /// <summary>
    /// Item específico demandado em uma necessidade de compra.
    /// </summary>
    public sealed class PurchaseRequestItem
    {
        public Guid ItemId { get; set; } = Guid.NewGuid();
        public Guid PurchaseRequestId { get; set; }
        public Guid? ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; } = 1;
        public decimal SuggestedQuantity { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal IdealStock { get; set; }
        public long EstimatedUnitCostCents { get; set; }
        public long ActualUnitCostCents { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public PurchasePriority Priority { get; set; } = PurchasePriority.NORMAL;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public PurchaseItemStatus Status { get; set; } = PurchaseItemStatus.PENDING;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public decimal EstimatedUnitCost
        {
            get => EstimatedUnitCostCents / 100m;
            set => EstimatedUnitCostCents = MoneyCents.FromDecimal(value).Cents;
        }

        public decimal ActualUnitCost
        {
            get => ActualUnitCostCents / 100m;
            set => ActualUnitCostCents = MoneyCents.FromDecimal(value).Cents;
        }

        public decimal EstimatedTotalCost => EstimatedUnitCost * RequestedQuantity;
    }
}
