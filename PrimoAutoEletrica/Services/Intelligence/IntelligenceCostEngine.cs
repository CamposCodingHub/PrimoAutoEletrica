using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services.Intelligence
{
    public enum UsageScenarioKind
    {
        LOW = 0,
        MEDIUM = 1,
        HIGH = 2,
        EXTREME = 3
    }

    public sealed class UsageScenario
    {
        public UsageScenarioKind Kind { get; init; }
        public int QueriesPerUserPerDay { get; init; }
        public int WorkingDaysPerMonth { get; init; } = 22;
        public string Hypothesis { get; init; } = string.Empty;
    }

    public sealed class CostEstimate
    {
        public string ModelId { get; init; } = string.Empty;
        public UsageScenarioKind Scenario { get; init; }
        public int Workshops { get; init; }
        public int UsersPerWorkshop { get; init; }
        public decimal? MonthlyCost { get; init; }
        public decimal? CostPerQuery { get; init; }
        public string Status { get; init; } = "PRICE_NOT_VERIFIED";
        public string Notes { get; init; } = string.Empty;
        public string EvidenceClass { get; init; } = "THEORETICAL";
    }

    /// <summary>
    /// C6.8 — cost engine independent of provider. Never invents prices:
    /// if ModelDefinition is PRICE_NOT_VERIFIED → estimate Status=PRICE_NOT_VERIFIED.
    /// </summary>
    public sealed class IntelligenceCostEngine
    {
        public static IReadOnlyList<UsageScenario> DefaultScenarios { get; } = new[]
        {
            new UsageScenario { Kind = UsageScenarioKind.LOW, QueriesPerUserPerDay = 5, Hypothesis = "Light assistive use — diagnostic lookup few times/day" },
            new UsageScenario { Kind = UsageScenarioKind.MEDIUM, QueriesPerUserPerDay = 25, Hypothesis = "Regular tech consult across OS flow" },
            new UsageScenario { Kind = UsageScenarioKind.HIGH, QueriesPerUserPerDay = 80, Hypothesis = "Heavy multi-bay continuous assist" },
            new UsageScenario { Kind = UsageScenarioKind.EXTREME, QueriesPerUserPerDay = 250, Hypothesis = "Stress upper bound — not a production forecast" }
        };

        public CostEstimate EstimateMonthly(
            ModelDefinition model,
            UsageScenario scenario,
            int workshops,
            int usersPerWorkshop,
            int avgPromptTokens = 800,
            int avgCompletionTokens = 400)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(scenario);
            if (workshops < 1) throw new ArgumentOutOfRangeException(nameof(workshops));
            if (usersPerWorkshop < 1) throw new ArgumentOutOfRangeException(nameof(usersPerWorkshop));

            long queriesMonth = (long)scenario.QueriesPerUserPerDay * scenario.WorkingDaysPerMonth * workshops * usersPerWorkshop;

            if (model.PriceStatus == "PRICE_NOT_VERIFIED" ||
                model.InputPricePer1M is null ||
                model.OutputPricePer1M is null)
            {
                return new CostEstimate
                {
                    ModelId = model.ModelId,
                    Scenario = scenario.Kind,
                    Workshops = workshops,
                    UsersPerWorkshop = usersPerWorkshop,
                    MonthlyCost = null,
                    CostPerQuery = null,
                    Status = "PRICE_NOT_VERIFIED",
                    EvidenceClass = "THEORETICAL",
                    Notes = $"Queries/month hypothetical={queriesMonth}. Prices unverified — not inventing USD. Hypothesis: {scenario.Hypothesis}"
                };
            }

            decimal costPerQuery =
                (avgPromptTokens / 1_000_000m) * model.InputPricePer1M.Value +
                (avgCompletionTokens / 1_000_000m) * model.OutputPricePer1M.Value;
            decimal monthly = costPerQuery * queriesMonth;

            return new CostEstimate
            {
                ModelId = model.ModelId,
                Scenario = scenario.Kind,
                Workshops = workshops,
                UsersPerWorkshop = usersPerWorkshop,
                MonthlyCost = decimal.Round(monthly, 4),
                CostPerQuery = decimal.Round(costPerQuery, 8),
                Status = "PRICE_VERIFIED_CALC",
                EvidenceClass = "THEORETICAL",
                Notes = $"Based on PriceSource={model.PriceSource} checked {model.PriceCheckedAt:o}. Hypothesis: {scenario.Hypothesis}"
            };
        }
    }
}