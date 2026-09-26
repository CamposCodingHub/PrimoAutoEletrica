using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    /// <summary>
    /// C2.3 Context Engine — provenance of every fact. Soft FKs that cannot be proven
    /// become RELATIONSHIP_NOT_PROVEN (never invent client/vehicle/OS/case links).
    /// </summary>
    public enum ContextProvenanceStatus
    {
        PROVEN = 0,
        RELATIONSHIP_NOT_PROVEN = 1,
        MISSING = 2,
        CONFLICT = 3
    }

    /// <summary>Single grounded fact with explicit origin ("where from?").</summary>
    public sealed class ContextFact
    {
        public string FactKey { get; init; } = string.Empty;
        public string DisplayLabel { get; init; } = string.Empty;
        public string? Value { get; init; }
        public ContextProvenanceStatus Status { get; init; } = ContextProvenanceStatus.MISSING;
        public string SourceEntity { get; init; } = string.Empty;
        public string? SourceEntityId { get; init; }
        public string SourceField { get; init; } = string.Empty;
        public string OriginDescription { get; init; } = string.Empty;
        public string Classification { get; init; } = "TECHNICAL";
    }

    public sealed class ContextRelation
    {
        public string RelationType { get; init; } = string.Empty;
        public Guid? FromId { get; init; }
        public Guid? ToId { get; init; }
        public ContextProvenanceStatus Status { get; init; } = ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN;
        public string Reason { get; init; } = string.Empty;
        public string SourceEntity { get; init; } = string.Empty;
        public string SourceField { get; init; } = string.Empty;
    }

    public sealed class VehicleIntelligenceContext
    {
        public Guid RequestedVehicleId { get; init; }
        public bool Found { get; init; }
        public Guid? ClienteIdProven { get; init; }
        public IReadOnlyList<ContextFact> Facts { get; init; } = Array.Empty<ContextFact>();
        public IReadOnlyList<ContextRelation> Relations { get; init; } = Array.Empty<ContextRelation>();
        public IReadOnlyList<Guid> ProvenWorkOrderIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<Guid> ProvenDiagnosticCaseIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Conflicts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public DateTimeOffset BuiltAt { get; init; } = DateTimeOffset.Now;
        public string Provider { get; init; } = "PRIMOX_VEHICLE_CONTEXT";
    }

    public sealed class ClientIntelligenceContext
    {
        public Guid RequestedClienteId { get; init; }
        public bool Found { get; init; }
        public IReadOnlyList<ContextFact> Facts { get; init; } = Array.Empty<ContextFact>();
        public IReadOnlyList<ContextRelation> Relations { get; init; } = Array.Empty<ContextRelation>();
        public IReadOnlyList<Guid> ProvenVehicleIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<Guid> ProvenWorkOrderIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Conflicts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public DateTimeOffset BuiltAt { get; init; } = DateTimeOffset.Now;
        public string Provider { get; init; } = "PRIMOX_CLIENT_CONTEXT";
    }

    public sealed class WorkOrderIntelligenceContext
    {
        public Guid RequestedWorkOrderId { get; init; }
        public bool Found { get; init; }
        public Guid? ClienteIdProven { get; init; }
        public Guid? VeiculoIdProven { get; init; }
        public IReadOnlyList<ContextFact> Facts { get; init; } = Array.Empty<ContextFact>();
        public IReadOnlyList<ContextRelation> Relations { get; init; } = Array.Empty<ContextRelation>();
        public IReadOnlyList<Guid> ProvenDiagnosticCaseIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<string> ServiceItemLabels { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> PartItemLabels { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Conflicts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public DateTimeOffset BuiltAt { get; init; } = DateTimeOffset.Now;
        public string Provider { get; init; } = "PRIMOX_WORKORDER_CONTEXT";
    }

    /// <summary>Composed intelligence context for Assist/Search — min-necessary, no financial by default.</summary>
    public sealed class ComposedIntelligenceContext
    {
        public string Query { get; init; } = string.Empty;
        public VehicleIntelligenceContext? Vehicle { get; init; }
        public ClientIntelligenceContext? Client { get; init; }
        public WorkOrderIntelligenceContext? WorkOrder { get; init; }
        public IReadOnlyList<ContextFact> AllProvenFacts { get; init; } = Array.Empty<ContextFact>();
        public IReadOnlyList<ContextRelation> UnprovenRelations { get; init; } = Array.Empty<ContextRelation>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Conflicts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public bool HasAnyProvenAnchor { get; init; }
        public DateTimeOffset BuiltAt { get; init; } = DateTimeOffset.Now;
        public string Provider { get; init; } = "PRIMOX_CONTEXT_COMPOSITION";
    }

    public sealed class ContextCompositionRequest
    {
        public string? Query { get; init; }
        public Guid? VehicleId { get; init; }
        public Guid? ClienteId { get; init; }
        public Guid? WorkOrderId { get; init; }
        /// <summary>When true, include FINANCIAL-class facts (still never invent). Default false.</summary>
        public bool IncludeFinancial { get; init; }
        /// <summary>Session cliente scope for cross-client fail-closed (optional).</summary>
        public Guid? SessionClienteId { get; init; }
    }
}