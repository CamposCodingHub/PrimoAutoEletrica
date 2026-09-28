using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services.Intelligence
{
    /// <summary>C6.2 — where a listed price came from. Never invent prices.</summary>
    public enum PriceSourceKind
    {
        PRICE_NOT_VERIFIED = 0,
        VendorPublicPage = 1,
        VendorApi = 2,
        ManualOperatorEntry = 3,
        BenchmarkDerived = 4,
        TheoreticalEstimate = 5
    }

    public sealed class ModelDefinition
    {
        public string ModelId { get; init; } = string.Empty;
        public string? ModelVersion { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string ProviderFamily { get; init; } = string.Empty; // e.g. LocalGGUF, OpenAI-compatible, Rules
        public IntelligenceExecutionMode ExecutionMode { get; init; }
        public int? ContextLimitTokens { get; init; }
        public int? MaxOutputTokens { get; init; }
        /// <summary>Input price per 1M tokens. Null unless verified.</summary>
        public decimal? InputPricePer1M { get; init; }
        /// <summary>Output price per 1M tokens. Null unless verified.</summary>
        public decimal? OutputPricePer1M { get; init; }
        public string Currency { get; init; } = "USD";
        public PriceSourceKind PriceSource { get; init; } = PriceSourceKind.PRICE_NOT_VERIFIED;
        public DateTimeOffset? PriceCheckedAt { get; init; }
        public string? PriceNotes { get; init; }
        public bool IsLocal { get; init; }
        public bool RequiresApiKey { get; init; }
        public IReadOnlyDictionary<string, string> Metadata { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string PriceStatus =>
            PriceSource == PriceSourceKind.PRICE_NOT_VERIFIED || InputPricePer1M is null
                ? "PRICE_NOT_VERIFIED"
                : "PRICE_VERIFIED";
    }

    /// <summary>C6.2 in-memory model registry — vendor-neutral; prices default PRICE_NOT_VERIFIED.</summary>
    public sealed class ModelRegistry
    {
        private readonly Dictionary<string, ModelDefinition> _models =
            new(StringComparer.OrdinalIgnoreCase);

        public ModelRegistry()
        {
            Register(new ModelDefinition
            {
                ModelId = "primox-grounded-local-rules",
                ModelVersion = "c5-carry",
                DisplayName = "PRIMOX Grounded Local Rules",
                ProviderFamily = "Rules",
                ExecutionMode = IntelligenceExecutionMode.GroundedLocalRule,
                IsLocal = true,
                RequiresApiKey = false,
                PriceSource = PriceSourceKind.PRICE_NOT_VERIFIED,
                PriceNotes = "Rules engine — no token price; infra cost deferred to C6.9"
            });

            Register(new ModelDefinition
            {
                ModelId = "local-model-generic",
                ModelVersion = null,
                DisplayName = "Generic Local Model (endpoint-configurable)",
                ProviderFamily = "LocalOpenAICompatible",
                ExecutionMode = IntelligenceExecutionMode.LocalModel,
                ContextLimitTokens = null,
                IsLocal = true,
                RequiresApiKey = false,
                PriceSource = PriceSourceKind.PRICE_NOT_VERIFIED,
                PriceNotes = "No local runtime verified at C6.2 — PRICE_NOT_VERIFIED"
            });

            Register(new ModelDefinition
            {
                ModelId = "openai-compatible-external",
                ModelVersion = null,
                DisplayName = "External OpenAI-compatible (experimental)",
                ProviderFamily = "OpenAICompatible",
                ExecutionMode = IntelligenceExecutionMode.ExternalModel,
                IsLocal = false,
                RequiresApiKey = true,
                InputPricePer1M = null,
                OutputPricePer1M = null,
                PriceSource = PriceSourceKind.PRICE_NOT_VERIFIED,
                PriceCheckedAt = null,
                PriceNotes = "Never invent vendor prices; LIVE pricing LIVE_NOT_TESTED without key"
            });
        }

        public void Register(ModelDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            if (string.IsNullOrWhiteSpace(definition.ModelId))
                throw new ArgumentException("ModelId required", nameof(definition));
            _models[definition.ModelId] = definition;
        }

        public bool TryGet(string modelId, out ModelDefinition? definition) =>
            _models.TryGetValue(modelId, out definition);

        public ModelDefinition GetRequired(string modelId) =>
            _models.TryGetValue(modelId, out var d)
                ? d
                : throw new KeyNotFoundException("Model not registered: " + modelId);

        public IReadOnlyList<ModelDefinition> ListAll() =>
            _models.Values.OrderBy(m => m.ModelId, StringComparer.OrdinalIgnoreCase).ToList();

        public IReadOnlyList<ModelDefinition> ListUnverifiedPrices() =>
            ListAll().Where(m => m.PriceStatus == "PRICE_NOT_VERIFIED").ToList();
    }
}