using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services.Intelligence
{
    public enum InfraEvidenceClass
    {
        THEORETICAL = 0,
        BENCHMARKED = 1,
        PRODUCTION_OBSERVED = 2
    }

    public sealed class LocalInfraCostModel
    {
        public string ProfileId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public decimal? GpuCapex { get; init; }
        public decimal? CpuCapex { get; init; }
        public decimal? RamCapex { get; init; }
        public decimal? StorageCapex { get; init; }
        public decimal? PowerKw { get; init; }
        public decimal? EnergyPricePerKwh { get; init; }
        public decimal? HoursPerMonth { get; init; }
        public int? AmortizationMonths { get; init; }
        public string Currency { get; init; } = "BRL";
        public PriceSourceKind PriceSource { get; init; } = PriceSourceKind.PRICE_NOT_VERIFIED;
        public DateTimeOffset? PriceCheckedAt { get; init; }
        public InfraEvidenceClass EvidenceClass { get; init; } = InfraEvidenceClass.THEORETICAL;
        public string Notes { get; init; } = string.Empty;
    }

    public sealed class InfraCostBreakdown
    {
        public string ProfileId { get; init; } = string.Empty;
        public decimal? CapexTotal { get; init; }
        public decimal? MonthlyAmortizedCapex { get; init; }
        public decimal? MonthlyEnergy { get; init; }
        public decimal? MonthlyTotal { get; init; }
        public decimal? CostPerWorkshop { get; init; }
        public decimal? CostPerQuery { get; init; }
        public string Status { get; init; } = "PRICE_NOT_VERIFIED";
        public InfraEvidenceClass EvidenceClass { get; init; } = InfraEvidenceClass.THEORETICAL;
        public string Notes { get; init; } = string.Empty;
    }

    /// <summary>C6.9 — local infra economics. Default PRICE_NOT_VERIFIED; no invented HW quotes.</summary>
    public sealed class LocalInfraEconomics
    {
        public IReadOnlyList<LocalInfraCostModel> DefaultProfiles { get; } = new[]
        {
            new LocalInfraCostModel
            {
                ProfileId = "cpu-only-workshop-pc",
                DisplayName = "Existing workshop PC (CPU-only local model)",
                EvidenceClass = InfraEvidenceClass.THEORETICAL,
                PriceSource = PriceSourceKind.PRICE_NOT_VERIFIED,
                Notes = "No Capex measured — reuse existing PC hypothesis; PRICE_NOT_VERIFIED"
            },
            new LocalInfraCostModel
            {
                ProfileId = "gpu-workstation-theoretical",
                DisplayName = "Dedicated GPU workstation (theoretical)",
                EvidenceClass = InfraEvidenceClass.THEORETICAL,
                PriceSource = PriceSourceKind.PRICE_NOT_VERIFIED,
                Notes = "GPU/CPU/RAM prices not verified in this environment — do not invent BRL/USD quotes"
            }
        };

        public InfraCostBreakdown Evaluate(
            LocalInfraCostModel model,
            int workshops = 1,
            long queriesPerMonth = 0)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (model.PriceSource == PriceSourceKind.PRICE_NOT_VERIFIED ||
                model.GpuCapex is null && model.CpuCapex is null)
            {
                return new InfraCostBreakdown
                {
                    ProfileId = model.ProfileId,
                    Status = "PRICE_NOT_VERIFIED",
                    EvidenceClass = model.EvidenceClass,
                    Notes = model.Notes + " | Capex/energy not verified — CostPerWorkshop/Query NOT_TESTED numerically."
                };
            }

            decimal capex = (model.GpuCapex ?? 0) + (model.CpuCapex ?? 0) + (model.RamCapex ?? 0) + (model.StorageCapex ?? 0);
            int months = model.AmortizationMonths ?? 36;
            decimal monthlyCapex = months > 0 ? capex / months : capex;
            decimal monthlyEnergy = 0;
            if (model.PowerKw is not null && model.EnergyPricePerKwh is not null && model.HoursPerMonth is not null)
                monthlyEnergy = model.PowerKw.Value * model.HoursPerMonth.Value * model.EnergyPricePerKwh.Value;
            decimal monthly = monthlyCapex + monthlyEnergy;
            decimal? perWorkshop = workshops > 0 ? monthly / workshops : null;
            decimal? perQuery = queriesPerMonth > 0 ? monthly / queriesPerMonth : null;

            return new InfraCostBreakdown
            {
                ProfileId = model.ProfileId,
                CapexTotal = capex,
                MonthlyAmortizedCapex = decimal.Round(monthlyCapex, 2),
                MonthlyEnergy = decimal.Round(monthlyEnergy, 2),
                MonthlyTotal = decimal.Round(monthly, 2),
                CostPerWorkshop = perWorkshop is null ? null : decimal.Round(perWorkshop.Value, 4),
                CostPerQuery = perQuery is null ? null : decimal.Round(perQuery.Value, 8),
                Status = "PRICE_VERIFIED_CALC",
                EvidenceClass = model.EvidenceClass,
                Notes = model.Notes
            };
        }
    }
}