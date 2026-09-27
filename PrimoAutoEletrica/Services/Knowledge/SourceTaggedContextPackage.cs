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
    /// C4.1 - explicit source tag for every context/evidence item in a package.
    /// Real sources only; UNKNOWN when origin cannot be proven. Never invent joins.
    /// Soft FKs remain RELATIONSHIP_NOT_PROVEN upstream.
    /// </summary>
    public enum ContextSourceTag
    {
        SYSTEM = 0,
        USER = 1,
        PERMS = 2,
        CLIENTE = 3,
        VEICULO = 4,
        OS = 5,
        BUDGET = 6,
        DIAGNOSTIC = 7,
        SERVICES = 8,
        PARTS = 9,
        HISTORY = 10,
        AGENDA = 11,
        KNOWLEDGE = 12,
        EVIDENCE = 13,
        ASSIST_LOCAL = 14,
        EXTERNAL_EVIDENCE = 15,
        FINANCE = 16,
        UNKNOWN = 99
    }

    /// <summary>Optional actor / RBAC snapshot for the package (suggest-only; never auto-action).</summary>
    public sealed class SourceTaggedActorContext
    {
        public string? UserId { get; init; }
        public string? UserDisplayName { get; init; }
        public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
        public bool CanIncludeFinance { get; init; }
        public Guid? SessionClienteId { get; init; }
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
        public string Section { get; init; } = string.Empty;
        /// <summary>Owning client/vehicle/OS id when known — used for isolation checks.</summary>
        public string IsolationAnchorId { get; init; } = string.Empty;
    }

    public sealed class SourceTaggedContextPackage
    {
        public string PackageId { get; init; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset BuiltAt { get; init; } = DateTimeOffset.Now;
        public string Query { get; init; } = string.Empty;
        public bool IncludeFinancial { get; init; }
        public bool HasAnyProvenAnchor { get; init; }
        public bool IsolationOk { get; init; } = true;
        public string IsolationStatus { get; init; } = "OK";
        public Guid? ScopedClienteId { get; init; }
        public Guid? ScopedVeiculoId { get; init; }
        public Guid? ScopedWorkOrderId { get; init; }
        public SourceTaggedActorContext? Actor { get; init; }
        public IReadOnlyList<SourceTaggedContextItem> Items { get; init; } = Array.Empty<SourceTaggedContextItem>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Conflicts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public ExternalEvidencePackage? EvidencePackage { get; init; }

        public IReadOnlyList<SourceTaggedContextItem> ByTag(ContextSourceTag tag) =>
            Items.Where(i => i.SourceTag == tag).ToList();

        public IReadOnlyList<SourceTaggedContextItem> BySection(string section) =>
            Items.Where(i => string.Equals(i.Section, section, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>Raised when CLIENT/VEHICLE/OS A vs B isolation is violated — CRITICAL STOP.</summary>
    public sealed class ContextIsolationViolationException : Exception
    {
        public string ViolationCode { get; }
        public ContextIsolationViolationException(string code, string message)
            : base(message)
        {
            ViolationCode = code;
        }
    }

    public interface ISourceTaggedContextPackageBuilder
    {
        Task<SourceTaggedContextPackage> BuildAsync(
            ContextCompositionRequest compositionRequest,
            AssistantQueryContext? assistContext = null,
            bool includeFinancial = false,
            SourceTaggedActorContext? actor = null,
            CancellationToken ct = default);
    }

    /// <summary>
    /// C4.1 Context Intelligence (deepened) — reuses ContextComposition + ExternalEvidencePackageBuilder +
    /// FinanceRedactor path + IntelligenceAuditService. Produces a source-tagged package (suggest-only).
    /// Flow: DATA→CONTEXT (this package)→EVIDENCE→RANKING→ASSIST→EXPLAIN→HUMAN DECISION.
    /// </summary>
    public sealed class SourceTaggedContextPackageBuilder : ISourceTaggedContextPackageBuilder
    {
        public const string WarningIsolationLeak = "ISOLATION_LEAK_CRITICAL";
        public const string WarningCrossClientDenied = "CROSS_CLIENT_DENIED";
        public const string WarningFinanceDenied = "FINANCE_PERMISSION_DENIED";
        public const string MissingAgenda = "AGENDA_DATA_ABSENT";
        public const string MissingBudget = "BUDGET_DATA_ABSENT";

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
            SourceTaggedActorContext? actor = null,
            CancellationToken ct = default)
        {
            if (compositionRequest == null) throw new ArgumentNullException(nameof(compositionRequest));
            ct.ThrowIfCancellationRequested();

            var sessionCliente = actor?.SessionClienteId ?? compositionRequest.SessionClienteId;
            var financeAllowed = includeFinancial && (actor == null || actor.CanIncludeFinance);
            if (includeFinancial && actor is { CanIncludeFinance: false })
            {
                financeAllowed = false;
            }

            var request = new ContextCompositionRequest
            {
                Query = compositionRequest.Query,
                VehicleId = compositionRequest.VehicleId,
                ClienteId = compositionRequest.ClienteId,
                WorkOrderId = compositionRequest.WorkOrderId,
                SessionClienteId = sessionCliente,
                IncludeFinancial = financeAllowed
            };

            var composed = await _composition.ComposeAsync(request, ct).ConfigureAwait(false);
            var items = new List<SourceTaggedContextItem>();
            var warnings = new List<string>(composed?.Warnings ?? Array.Empty<string>());
            var conflicts = new List<string>(composed?.Conflicts ?? Array.Empty<string>());
            var missing = new List<string>(composed?.MissingData ?? Array.Empty<string>());

            // USER / PERMS section (real only — UNKNOWN if absent)
            AppendActorItems(items, actor);

            if (composed?.Warnings != null &&
                composed.Warnings.Any(w => string.Equals(w, ContextCompositionService.WarningCrossClientDenied, StringComparison.OrdinalIgnoreCase)))
            {
                warnings.Add(WarningCrossClientDenied);
                var blocked = new SourceTaggedContextPackage
                {
                    Query = composed.Query ?? request.Query ?? string.Empty,
                    IncludeFinancial = false,
                    HasAnyProvenAnchor = false,
                    IsolationOk = false,
                    IsolationStatus = WarningCrossClientDenied,
                    ScopedClienteId = sessionCliente,
                    Actor = actor,
                    Items = items,
                    Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    Conflicts = conflicts,
                    MissingData = missing,
                    BuiltAt = DateTimeOffset.Now
                };
                RecordAudit(blocked, "FAIL_CLOSED_CROSS_CLIENT");
                return blocked;
            }

            if (includeFinancial && !financeAllowed)
            {
                warnings.Add(WarningFinanceDenied);
            }

            // Proven facts from composition
            if (composed?.AllProvenFacts != null)
            {
                foreach (var fact in composed.AllProvenFacts)
                {
                    if (!financeAllowed &&
                        (string.Equals(fact.Classification, "FINANCIAL", StringComparison.OrdinalIgnoreCase) ||
                         InferTagFromFactKey(fact.FactKey, fact.SourceEntity) == ContextSourceTag.FINANCE))
                    {
                        continue;
                    }

                    var tag = InferTagFromFactKey(fact.FactKey, fact.SourceEntity);
                    items.Add(new SourceTaggedContextItem
                    {
                        SourceTag = tag,
                        SourceType = string.IsNullOrWhiteSpace(fact.SourceEntity) ? "FACT" : fact.SourceEntity,
                        SourceId = fact.SourceEntityId ?? fact.FactKey ?? string.Empty,
                        Key = fact.FactKey ?? string.Empty,
                        Value = fact.Value ?? string.Empty,
                        Classification = fact.Classification ?? string.Empty,
                        ProvenanceStatus = fact.Status.ToString(),
                        Section = SectionForTag(tag),
                        IsolationAnchorId = fact.SourceEntityId ?? string.Empty
                    });
                }
            }

            // Nested context sections: services / parts / diagnostic / history / budget / agenda
            AppendWorkOrderSections(items, composed?.WorkOrder, financeAllowed, missing);
            AppendVehicleSections(items, composed?.Vehicle, missing);
            AppendClientSections(items, composed?.Client, missing);

            ExternalEvidencePackage? evidence = null;
            if (assistContext != null)
            {
                evidence = _evidenceBuilder.Build(assistContext, includeFinancial: financeAllowed);
                if (evidence?.Sources != null)
                {
                    foreach (var src in evidence.Sources)
                    {
                        var tag = InferTagFromEvidenceSourceType(src.SourceType);
                        items.Add(new SourceTaggedContextItem
                        {
                            SourceTag = tag,
                            SourceType = src.SourceType ?? string.Empty,
                            SourceId = src.SourceId ?? string.Empty,
                            Key = "evidence",
                            Value = src.Title ?? src.Excerpt ?? string.Empty,
                            Classification = "EVIDENCE",
                            ProvenanceStatus = "PROVEN",
                            EvidenceId = src.EvidenceId ?? string.Empty,
                            Section = "evidence",
                            IsolationAnchorId = src.SourceId ?? string.Empty
                        });
                    }
                }
            }

            var scopedCliente = composed?.Client?.RequestedClienteId is Guid cid && cid != Guid.Empty
                ? cid
                : (composed?.WorkOrder?.ClienteIdProven ?? composed?.Vehicle?.ClienteIdProven ?? request.ClienteId);
            var scopedVeiculo = composed?.Vehicle?.RequestedVehicleId is Guid vid && vid != Guid.Empty
                ? vid
                : (composed?.WorkOrder?.VeiculoIdProven ?? request.VehicleId);
            var scopedOs = composed?.WorkOrder?.RequestedWorkOrderId is Guid oid && oid != Guid.Empty
                ? oid
                : request.WorkOrderId;

            // Isolation CRITICAL — CLIENT/VEHICLE/OS A vs B
            var isolation = ValidateIsolation(
                items,
                scopedCliente,
                scopedVeiculo,
                scopedOs,
                sessionCliente,
                composed);

            if (!isolation.Ok)
            {
                warnings.Add(WarningIsolationLeak);
                warnings.AddRange(isolation.Reasons);
                RecordAudit(new SourceTaggedContextPackage
                {
                    Query = composed?.Query ?? request.Query ?? string.Empty,
                    IsolationOk = false,
                    IsolationStatus = WarningIsolationLeak,
                    Items = items,
                    Warnings = warnings
                }, "CRITICAL_ISOLATION_LEAK");
                throw new ContextIsolationViolationException(
                    WarningIsolationLeak,
                    "CRITICAL STOP: context isolation leak CLIENT/VEHICLE/OS A vs B — " + string.Join("; ", isolation.Reasons));
            }

            var package = new SourceTaggedContextPackage
            {
                Query = composed?.Query ?? request.Query ?? string.Empty,
                IncludeFinancial = financeAllowed,
                HasAnyProvenAnchor = composed?.HasAnyProvenAnchor == true,
                IsolationOk = true,
                IsolationStatus = "OK",
                ScopedClienteId = scopedCliente,
                ScopedVeiculoId = scopedVeiculo,
                ScopedWorkOrderId = scopedOs,
                Actor = actor,
                Items = items,
                Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Conflicts = conflicts.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                MissingData = missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                EvidencePackage = evidence,
                BuiltAt = DateTimeOffset.Now
            };

            RecordAudit(package, package.HasAnyProvenAnchor || package.Items.Count > 0 ? "OK" : "FAIL_CLOSED");
            return package;
        }

        private void RecordAudit(SourceTaggedContextPackage package, string status)
        {
            _audit?.Record(new IntelligenceAuditEntry
            {
                Question = package.Query,
                ContextSummary = $"C4.1 package items={package.Items.Count} anchor={package.HasAnyProvenAnchor} isolation={package.IsolationStatus}",
                AllowedContext = string.Join(",", package.Items.Select(i => i.SourceTag).Distinct().Select(t => t.ToString())),
                EvidenceIds = string.Join(",", package.Items.Select(i => i.EvidenceId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct()),
                EvidenceSummary = $"tagged={package.Items.Count}",
                Provider = "LOCAL/C4.1",
                Result = "SOURCE_TAGGED_PACKAGE",
                Status = status
            });
        }

        private static void AppendActorItems(List<SourceTaggedContextItem> items, SourceTaggedActorContext? actor)
        {
            if (actor == null)
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.UNKNOWN,
                    SourceType = "USER",
                    Key = "user.id",
                    Value = string.Empty,
                    Classification = "ACTOR",
                    ProvenanceStatus = "MISSING",
                    Section = "user"
                });
                return;
            }

            if (!string.IsNullOrWhiteSpace(actor.UserId))
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.USER,
                    SourceType = "USER",
                    SourceId = actor.UserId,
                    Key = "user.id",
                    Value = actor.UserDisplayName ?? actor.UserId,
                    Classification = "ACTOR",
                    ProvenanceStatus = "PROVEN",
                    Section = "user",
                    IsolationAnchorId = actor.SessionClienteId?.ToString("D") ?? string.Empty
                });
            }
            else
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.UNKNOWN,
                    SourceType = "USER",
                    Key = "user.id",
                    ProvenanceStatus = "MISSING",
                    Section = "user"
                });
            }

            var perms = actor.Permissions ?? Array.Empty<string>();
            if (perms.Count == 0)
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.UNKNOWN,
                    SourceType = "PERMS",
                    Key = "perms.list",
                    ProvenanceStatus = "MISSING",
                    Section = "perms"
                });
            }
            else
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.PERMS,
                    SourceType = "PERMS",
                    SourceId = actor.UserId ?? string.Empty,
                    Key = "perms.list",
                    Value = string.Join(",", perms),
                    Classification = "ACTOR",
                    ProvenanceStatus = "PROVEN",
                    Section = "perms"
                });
            }

            items.Add(new SourceTaggedContextItem
            {
                SourceTag = ContextSourceTag.PERMS,
                SourceType = "PERMS",
                Key = "perms.finance",
                Value = actor.CanIncludeFinance ? "ALLOWED" : "DENIED",
                Classification = "ACTOR",
                ProvenanceStatus = "PROVEN",
                Section = "perms"
            });
        }

        private static void AppendWorkOrderSections(
            List<SourceTaggedContextItem> items,
            WorkOrderIntelligenceContext? wo,
            bool financeAllowed,
            List<string> missing)
        {
            if (wo is not { Found: true })
                return;

            var osId = wo.RequestedWorkOrderId.ToString("D");
            var clientAnchor = wo.ClienteIdProven?.ToString("D") ?? string.Empty;

            foreach (var label in wo.ServiceItemLabels ?? Array.Empty<string>())
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.SERVICES,
                    SourceType = "WORKORDER_SERVICE",
                    SourceId = osId,
                    Key = "os.service",
                    Value = label,
                    Classification = "TECHNICAL",
                    ProvenanceStatus = "PROVEN",
                    Section = "services",
                    IsolationAnchorId = clientAnchor
                });
            }

            foreach (var label in wo.PartItemLabels ?? Array.Empty<string>())
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.PARTS,
                    SourceType = "WORKORDER_PART",
                    SourceId = osId,
                    Key = "os.part",
                    Value = label,
                    Classification = "TECHNICAL",
                    ProvenanceStatus = "PROVEN",
                    Section = "parts",
                    IsolationAnchorId = clientAnchor
                });
            }

            foreach (var caseId in wo.ProvenDiagnosticCaseIds ?? Array.Empty<Guid>())
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.DIAGNOSTIC,
                    SourceType = "DIAGNOSTIC_CASE",
                    SourceId = caseId.ToString("D"),
                    Key = "os.diagnostic_case",
                    Value = caseId.ToString("D"),
                    Classification = "TECHNICAL",
                    ProvenanceStatus = "PROVEN",
                    Section = "diagnostic",
                    IsolationAnchorId = clientAnchor
                });
            }

            // Budget — only when finance allowed and a budget-like fact exists; else mark absent (never invent)
            var budgetFact = (wo.Facts ?? Array.Empty<ContextFact>())
                .FirstOrDefault(f =>
                    f.FactKey.Contains("orcamento", StringComparison.OrdinalIgnoreCase) ||
                    f.FactKey.Contains("budget", StringComparison.OrdinalIgnoreCase) ||
                    f.FactKey.Contains("valor", StringComparison.OrdinalIgnoreCase));
            if (budgetFact != null && financeAllowed)
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.BUDGET,
                    SourceType = "WORKORDER_BUDGET",
                    SourceId = osId,
                    Key = budgetFact.FactKey,
                    Value = budgetFact.Value ?? string.Empty,
                    Classification = budgetFact.Classification,
                    ProvenanceStatus = budgetFact.Status.ToString(),
                    Section = "budget",
                    IsolationAnchorId = clientAnchor
                });
            }
            else if (budgetFact == null)
            {
                missing.Add(MissingBudget);
            }

            // Agenda — never invent; mark missing when no agenda fact
            var agendaFact = (wo.Facts ?? Array.Empty<ContextFact>())
                .FirstOrDefault(f =>
                    f.FactKey.Contains("agenda", StringComparison.OrdinalIgnoreCase) ||
                    f.FactKey.Contains("agendamento", StringComparison.OrdinalIgnoreCase));
            if (agendaFact != null)
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.AGENDA,
                    SourceType = "AGENDA",
                    SourceId = osId,
                    Key = agendaFact.FactKey,
                    Value = agendaFact.Value ?? string.Empty,
                    Classification = "TECHNICAL",
                    ProvenanceStatus = agendaFact.Status.ToString(),
                    Section = "agenda",
                    IsolationAnchorId = clientAnchor
                });
            }
            else
            {
                missing.Add(MissingAgenda);
            }
        }

        private static void AppendVehicleSections(
            List<SourceTaggedContextItem> items,
            VehicleIntelligenceContext? vehicle,
            List<string> missing)
        {
            if (vehicle is not { Found: true })
                return;

            var vId = vehicle.RequestedVehicleId.ToString("D");
            var clientAnchor = vehicle.ClienteIdProven?.ToString("D") ?? string.Empty;

            var hist = (vehicle.Facts ?? Array.Empty<ContextFact>())
                .FirstOrDefault(f => f.FactKey.Contains("histor", StringComparison.OrdinalIgnoreCase));
            if (hist != null && !string.IsNullOrWhiteSpace(hist.Value))
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.HISTORY,
                    SourceType = "VEHICLE_HISTORY",
                    SourceId = vId,
                    Key = hist.FactKey,
                    Value = hist.Value,
                    Classification = "TECHNICAL",
                    ProvenanceStatus = hist.Status.ToString(),
                    Section = "history",
                    IsolationAnchorId = clientAnchor
                });
            }

            foreach (var caseId in vehicle.ProvenDiagnosticCaseIds ?? Array.Empty<Guid>())
            {
                if (items.Any(i => i.SourceTag == ContextSourceTag.DIAGNOSTIC && i.SourceId == caseId.ToString("D")))
                    continue;
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.DIAGNOSTIC,
                    SourceType = "DIAGNOSTIC_CASE",
                    SourceId = caseId.ToString("D"),
                    Key = "vehicle.diagnostic_case",
                    Value = caseId.ToString("D"),
                    Classification = "TECHNICAL",
                    ProvenanceStatus = "PROVEN",
                    Section = "diagnostic",
                    IsolationAnchorId = clientAnchor
                });
            }
        }

        private static void AppendClientSections(
            List<SourceTaggedContextItem> items,
            ClientIntelligenceContext? client,
            List<string> missing)
        {
            if (client is not { Found: true })
                return;

            var cId = client.RequestedClienteId.ToString("D");
            var lastOs = (client.Facts ?? Array.Empty<ContextFact>())
                .FirstOrDefault(f => f.FactKey.Contains("last_os", StringComparison.OrdinalIgnoreCase));
            if (lastOs != null && !string.IsNullOrWhiteSpace(lastOs.Value))
            {
                items.Add(new SourceTaggedContextItem
                {
                    SourceTag = ContextSourceTag.HISTORY,
                    SourceType = "CLIENT_HISTORY",
                    SourceId = cId,
                    Key = lastOs.FactKey,
                    Value = lastOs.Value,
                    Classification = "TECHNICAL",
                    ProvenanceStatus = lastOs.Status.ToString(),
                    Section = "history",
                    IsolationAnchorId = cId
                });
            }
        }

        public sealed class IsolationResult
        {
            public bool Ok { get; init; }
            public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
        }

        /// <summary>
        /// CRITICAL isolation: package scoped to Client/Vehicle/OS A must never carry B anchors.
        /// Soft unproven relations are warnings upstream — foreign proven IDs in items = STOP.
        /// </summary>
        public static IsolationResult ValidateIsolation(
            IReadOnlyList<SourceTaggedContextItem> items,
            Guid? scopedClienteId,
            Guid? scopedVeiculoId,
            Guid? scopedWorkOrderId,
            Guid? sessionClienteId,
            ComposedIntelligenceContext? composed)
        {
            var reasons = new List<string>();

            if (sessionClienteId.HasValue &&
                scopedClienteId.HasValue &&
                sessionClienteId.Value != Guid.Empty &&
                scopedClienteId.Value != Guid.Empty &&
                sessionClienteId.Value != scopedClienteId.Value)
            {
                reasons.Add($"CLIENT_SESSION_MISMATCH session={sessionClienteId:D} scoped={scopedClienteId:D}");
            }

            // Nested contexts must agree with scoped anchors when both proven
            if (composed?.Client is { Found: true } c &&
                scopedClienteId.HasValue &&
                scopedClienteId.Value != Guid.Empty &&
                c.RequestedClienteId != Guid.Empty &&
                c.RequestedClienteId != scopedClienteId.Value)
            {
                reasons.Add($"CLIENT_A_VS_B composed={c.RequestedClienteId:D} scoped={scopedClienteId:D}");
            }

            if (composed?.Vehicle is { Found: true } v &&
                scopedVeiculoId.HasValue &&
                scopedVeiculoId.Value != Guid.Empty &&
                v.RequestedVehicleId != Guid.Empty &&
                v.RequestedVehicleId != scopedVeiculoId.Value)
            {
                reasons.Add($"VEHICLE_A_VS_B composed={v.RequestedVehicleId:D} scoped={scopedVeiculoId:D}");
            }

            if (composed?.WorkOrder is { Found: true } w &&
                scopedWorkOrderId.HasValue &&
                scopedWorkOrderId.Value != Guid.Empty &&
                w.RequestedWorkOrderId != Guid.Empty &&
                w.RequestedWorkOrderId != scopedWorkOrderId.Value)
            {
                reasons.Add($"OS_A_VS_B composed={w.RequestedWorkOrderId:D} scoped={scopedWorkOrderId:D}");
            }

            // Cross-client vehicles under client scope
            if (composed?.Vehicle is { Found: true, ClienteIdProven: { } vClient } &&
                scopedClienteId.HasValue &&
                scopedClienteId.Value != Guid.Empty &&
                vClient != Guid.Empty &&
                vClient != scopedClienteId.Value)
            {
                reasons.Add($"VEHICLE_CLIENT_LEAK vehicle.cliente={vClient:D} scoped.cliente={scopedClienteId:D}");
            }

            if (composed?.WorkOrder is { Found: true, ClienteIdProven: { } wClient } &&
                scopedClienteId.HasValue &&
                scopedClienteId.Value != Guid.Empty &&
                wClient != Guid.Empty &&
                wClient != scopedClienteId.Value)
            {
                reasons.Add($"OS_CLIENT_LEAK os.cliente={wClient:D} scoped.cliente={scopedClienteId:D}");
            }

            // Item-level foreign client anchors (when IsolationAnchorId is a Guid differing from scope)
            if (scopedClienteId.HasValue && scopedClienteId.Value != Guid.Empty)
            {
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.IsolationAnchorId)) continue;
                    if (!Guid.TryParse(item.IsolationAnchorId, out var anchor)) continue;
                    if (anchor == Guid.Empty) continue;
                    // Only treat as client-anchor leak for client/history/services/parts/budget sections
                    var section = item.Section ?? string.Empty;
                    var isClientScopedSection =
                        section is "history" or "services" or "parts" or "budget" or "diagnostic" or "agenda" or "user" ||
                        item.SourceTag is ContextSourceTag.CLIENTE or ContextSourceTag.HISTORY
                            or ContextSourceTag.SERVICES or ContextSourceTag.PARTS
                            or ContextSourceTag.BUDGET or ContextSourceTag.DIAGNOSTIC or ContextSourceTag.AGENDA;
                    if (!isClientScopedSection) continue;
                    if (anchor != scopedClienteId.Value &&
                        (!scopedVeiculoId.HasValue || anchor != scopedVeiculoId.Value) &&
                        (!scopedWorkOrderId.HasValue || anchor != scopedWorkOrderId.Value))
                    {
                        // Foreign Guid that is neither scoped vehicle nor OS → treat as client leak when tag is CLIENTE
                        if (item.SourceTag == ContextSourceTag.CLIENTE || section == "history")
                        {
                            if (composed?.Client?.ProvenVehicleIds?.Contains(anchor) == true) continue;
                            if (composed?.Client?.ProvenWorkOrderIds?.Contains(anchor) == true) continue;
                            if (composed?.Vehicle?.ProvenWorkOrderIds?.Contains(anchor) == true) continue;
                            if (anchor != scopedClienteId.Value)
                            {
                                // Allow only if it's the scoped client
                                reasons.Add($"ITEM_CLIENT_LEAK key={item.Key} anchor={anchor:D} scoped={scopedClienteId:D}");
                            }
                        }
                    }
                }
            }

            return new IsolationResult
            {
                Ok = reasons.Count == 0,
                Reasons = reasons
            };
        }

        public static string SectionForTag(ContextSourceTag tag) => tag switch
        {
            ContextSourceTag.USER => "user",
            ContextSourceTag.PERMS => "perms",
            ContextSourceTag.CLIENTE => "client",
            ContextSourceTag.VEICULO => "vehicle",
            ContextSourceTag.OS => "os",
            ContextSourceTag.BUDGET => "budget",
            ContextSourceTag.DIAGNOSTIC => "diagnostic",
            ContextSourceTag.SERVICES => "services",
            ContextSourceTag.PARTS => "parts",
            ContextSourceTag.HISTORY => "history",
            ContextSourceTag.AGENDA => "agenda",
            ContextSourceTag.KNOWLEDGE => "knowledge",
            ContextSourceTag.EVIDENCE => "evidence",
            ContextSourceTag.ASSIST_LOCAL => "assist_local",
            ContextSourceTag.EXTERNAL_EVIDENCE => "evidence",
            ContextSourceTag.FINANCE => "finance",
            ContextSourceTag.SYSTEM => "system",
            _ => "unknown"
        };

        public static ContextSourceTag InferTagFromFactKey(string? key, string? source)
        {
            var hay = $"{key}|{source}".ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(hay) || hay == "|")
                return ContextSourceTag.UNKNOWN;

            if (hay.Contains("financ") || hay.Contains("saldo") || hay.Contains("pagamento") || hay.Contains("titulo"))
                return ContextSourceTag.FINANCE;
            if (hay.Contains("orcamento") || hay.Contains("budget"))
                return ContextSourceTag.BUDGET;
            if (hay.Contains("diagnost") || hay.Contains("diagnostic"))
                return ContextSourceTag.DIAGNOSTIC;
            if (hay.Contains(".service") || hay.Contains("servico") || hay.Contains("serviço") || hay.Contains("os.item") && hay.Contains("serv"))
                return ContextSourceTag.SERVICES;
            if (hay.Contains(".part") || hay.Contains("peca") || hay.Contains("peça") || hay.Contains("pecas"))
                return ContextSourceTag.PARTS;
            if (hay.Contains("histor") || hay.Contains("last_os") || hay.Contains("history"))
                return ContextSourceTag.HISTORY;
            if (hay.Contains("agenda") || hay.Contains("agendamento"))
                return ContextSourceTag.AGENDA;
            if (hay.Contains("workorder") || hay.Contains("ordem") || hay.Contains(".os.") || hay.StartsWith("os.") || hay.Contains("os_") || hay.Contains("|os") || source?.Equals("WORKORDER", StringComparison.OrdinalIgnoreCase) == true)
                return ContextSourceTag.OS;
            if (hay.Contains("vehicle") || hay.Contains("veiculo") || hay.Contains("veículo") || source?.Equals("VEHICLE", StringComparison.OrdinalIgnoreCase) == true)
                return ContextSourceTag.VEICULO;
            if (hay.Contains("client") || hay.Contains("cliente") || source?.Equals("CLIENT", StringComparison.OrdinalIgnoreCase) == true)
                return ContextSourceTag.CLIENTE;
            if (hay.Contains("knowledge") || hay.Contains("artigo") || hay.Contains("caso"))
                return ContextSourceTag.KNOWLEDGE;
            if (hay.Contains("user") || hay.Contains("usuario") || hay.Contains("usuário"))
                return ContextSourceTag.USER;
            if (hay.Contains("perm") || hay.Contains("rbac") || hay.Contains("role"))
                return ContextSourceTag.PERMS;
            if (hay.Contains("system") || hay.Contains("primox") || hay.StartsWith("sys."))
                return ContextSourceTag.SYSTEM;

            // Real sources only — unknown origin stays UNKNOWN (never invent SYSTEM)
            return ContextSourceTag.UNKNOWN;
        }

        public static ContextSourceTag InferTagFromEvidenceSourceType(string? sourceType)
        {
            if (string.IsNullOrWhiteSpace(sourceType)) return ContextSourceTag.UNKNOWN;
            var t = sourceType.Trim();
            if (t.Contains("Knowledge", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Article", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Case", StringComparison.OrdinalIgnoreCase))
                return ContextSourceTag.KNOWLEDGE;
            if (t.Contains("Local", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Assist", StringComparison.OrdinalIgnoreCase))
                return ContextSourceTag.ASSIST_LOCAL;
            if (t.Contains("Evidence", StringComparison.OrdinalIgnoreCase))
                return ContextSourceTag.EVIDENCE;
            if (t.Contains("External", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Http", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Provider", StringComparison.OrdinalIgnoreCase))
                return ContextSourceTag.EXTERNAL_EVIDENCE;
            return ContextSourceTag.UNKNOWN;
        }
    }
}
