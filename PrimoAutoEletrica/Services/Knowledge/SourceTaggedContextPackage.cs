using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.ExternalAi;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// C4.1 — explicit source tag for every context/evidence item in a package.
    /// Never invent joins; soft FKs remain RELATIONSHIP_NOT_PROVEN upstream.
    /// </summary>
    public enum ContextSourceTag
    {
        SYSTEM = 0,
        CLIENTE = 1,
        VEICULO = 2,
        OS = 3,
        KNOWLEDGE = 4,
        ASSIST_LOCAL = 5,
        EXTERNAL_EVIDENCE = 6,
        UNKNOWN = 99
    }

    public sealed class SourceTaggedContextItem
    {
        public ContextSourceTag SourceTag { get; init; } = ContextSourceTag.UNKNOWN;
        public string SourceType { get; init; } = string.Empty;
        public string SourceId { get; init; } = string.Empty;
        public string Key { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string Classification { get; init; } = string.Empty;
        public string ProvenanceStatus { get; init; } = string.Empty;
        public string EvidenceId { get; init; } = string.Empty;
    }

    public sealed class SourceTaggedContextPackage
    {
        public string PackageId { get; init; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset BuiltAt { get; init; } = DateTimeOffset.Now;
        public string Query { get; init; } = string.Empty;
        public bool IncludeFinancial { get; init; }
        public bool HasAnyProvenAnchor { get; init; }
        public IReadOnlyList<SourceTaggedContextItem> Items { get; init; } = Array.Empty<SourceTaggedContextItem>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Conflicts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public ExternalEvidencePackage? EvidencePackage { get; init; }

        public IReadOnlyList<SourceTaggedContextItem> ByTag(ContextSourceTag tag) =>
            Items.Where(i => i.SourceTag == tag).ToList();
    }

    public interface ISourceTaggedContextPackageBuilder
    {
        Task<SourceTaggedContextPackage> BuildAsync(
            ContextCompositionRequest compositionRequest,
            AssistantQueryContext? assistContext = null,
            bool includeFinancial = false,
            CancellationToken ct = default);
    }

    /// <summary>
    /// C4.1 Context Intelligence — reuses ContextComposition + ExternalEvidencePackageBuilder +
    /// FinanceRedactor + IntelligenceAuditService. Produces a source-tagged package (suggest-only).
    /// </summary>
    public sealed class SourceTaggedContextPackageBuilder : ISourceTaggedContextPackageBuilder
    {
        private readonly IContextCompositionService _composition;
        private readonly ExternalEvidencePackageBuilder _evidenceBuilder;
        private readonly IIntelligenceAuditService? _audit;

        public SourceTaggedContextPackageBuilder(
            IContextCompositionService composition,
            ExternalEvidencePackageBuilder? evidenceBuilder = null,
            IIntelligenceAuditService? audit = null)
        {
            _composition = composition ?? throw new ArgumentNullException(nameof(composition));
            _evidenceBuilder = evidenceBuilder ?? new ExternalEvidencePackageBuilder();
            _audit = audit;
        }

        public async Task<SourceTaggedContextPackage> BuildAsync(
            ContextCompositionRequest compositionRequest,
            AssistantQueryContext? assistContext = null,
            bool includeFinancial = false,
            CancellationToken ct = default)
        {
            if (compositionRequest == null) throw new ArgumentNullException(nameof(compositionRequest));
            ct.ThrowIfCancellationRequested();

            var request = new ContextCompositionRequest
            {
                Query = compositionRequest.Query,
                VehicleId = compositionRequest.VehicleId,
                ClienteId = compositionRequest.ClienteId,
                WorkOrderId = compositionRequest.WorkOrderId,
                SessionClienteId = compositionRequest.SessionClienteId,
                IncludeFinancial = includeFinancial
            };

            var composed = await _composition.ComposeAsync(request, ct).ConfigureAwait(false);
            var items = new List<SourceTaggedContextItem>();

            if (composed?.AllProvenFacts != null)
            {
                foreach (var fact in composed.AllProvenFacts)
                {
                    if (!includeFinancial &&
                        string.Equals(fact.Classification, "FINANCIAL", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    items.Add(new SourceTaggedContextItem
                    {
                        SourceTag = InferTagFromFactKey(fact.FactKey, fact.SourceEntity),
                        SourceType = string.IsNullOrWhiteSpace(fact.SourceEntity) ? "FACT" : fact.SourceEntity,
                        SourceId = fact.SourceEntityId ?? fact.FactKey ?? string.Empty,
                        Key = fact.FactKey ?? string.Empty,
                        Value = fact.Value ?? string.Empty,
                        Classification = fact.Classification ?? string.Empty,
                        ProvenanceStatus = fact.Status.ToString()
                    });
                }
            }

            ExternalEvidencePackage? evidence = null;
            if (assistContext != null)
            {
                evidence = _evidenceBuilder.Build(assistContext, includeFinancial: includeFinancial);
                if (evidence?.Sources != null)
                {
                    foreach (var src in evidence.Sources)
                    {
                        items.Add(new SourceTaggedContextItem
                        {
                            SourceTag = InferTagFromEvidenceSourceType(src.SourceType),
                            SourceType = src.SourceType ?? string.Empty,
                            SourceId = src.SourceId ?? string.Empty,
                            Key = "evidence",
                            Value = src.Title ?? src.Excerpt ?? string.Empty,
                            Classification = "EVIDENCE",
                            ProvenanceStatus = "PROVEN",
                            EvidenceId = src.EvidenceId ?? string.Empty
                        });
                    }
                }
            }

            var package = new SourceTaggedContextPackage
            {
                Query = composed?.Query ?? request.Query ?? string.Empty,
                IncludeFinancial = includeFinancial,
                HasAnyProvenAnchor = composed?.HasAnyProvenAnchor == true,
                Items = items,
                Warnings = composed?.Warnings ?? Array.Empty<string>(),
                Conflicts = composed?.Conflicts ?? Array.Empty<string>(),
                MissingData = composed?.MissingData ?? Array.Empty<string>(),
                EvidencePackage = evidence,
                BuiltAt = DateTimeOffset.Now
            };

            _audit?.Record(new IntelligenceAuditEntry
            {
                Question = package.Query,
                ContextSummary = $"C4.1 package items={package.Items.Count} anchor={package.HasAnyProvenAnchor}",
                AllowedContext = string.Join(",", package.Items.Select(i => i.SourceTag).Distinct().Select(t => t.ToString())),
                EvidenceIds = string.Join(",", package.Items.Select(i => i.EvidenceId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct()),
                EvidenceSummary = $"tagged={package.Items.Count}",
                Provider = "LOCAL/C4.1",
                Result = "SOURCE_TAGGED_PACKAGE",
                Status = package.HasAnyProvenAnchor || package.Items.Count > 0 ? "OK" : "FAIL_CLOSED"
            });

            return package;
        }

        public static ContextSourceTag InferTagFromFactKey(string? key, string? source)
        {
            var hay = $"{key}|{source}".ToLowerInvariant();
            if (hay.Contains("workorder") || hay.Contains("ordem") || hay.Contains(".os.") || hay.StartsWith("os.") || hay.Contains("os_"))
                return ContextSourceTag.OS;
            if (hay.Contains("vehicle") || hay.Contains("veiculo") || hay.Contains("veículo"))
                return ContextSourceTag.VEICULO;
            if (hay.Contains("client") || hay.Contains("cliente"))
                return ContextSourceTag.CLIENTE;
            if (hay.Contains("knowledge") || hay.Contains("artigo") || hay.Contains("caso"))
                return ContextSourceTag.KNOWLEDGE;
            return ContextSourceTag.SYSTEM;
        }

        public static ContextSourceTag InferTagFromEvidenceSourceType(string? sourceType)
        {
            if (string.IsNullOrWhiteSpace(sourceType)) return ContextSourceTag.EXTERNAL_EVIDENCE;
            var t = sourceType.Trim();
            if (t.Contains("Knowledge", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Article", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Case", StringComparison.OrdinalIgnoreCase))
                return ContextSourceTag.KNOWLEDGE;
            if (t.Contains("Local", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Assist", StringComparison.OrdinalIgnoreCase))
                return ContextSourceTag.ASSIST_LOCAL;
            return ContextSourceTag.EXTERNAL_EVIDENCE;
        }
    }
}