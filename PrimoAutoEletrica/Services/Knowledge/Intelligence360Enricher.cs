using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IIntelligence360Enricher
    {
        Task<Intelligence360Overlay> ForVehicleAsync(Guid vehicleId, CancellationToken ct = default);
        Task<Intelligence360Overlay> ForClientAsync(Guid clienteId, CancellationToken ct = default);
        Task<Intelligence360Overlay> ForWorkOrderAsync(Guid workOrderId, CancellationToken ct = default);
    }

    /// <summary>C2.6 — overlay for existing 360 screens: alerts/history/knowledge from proven context only.</summary>
    public sealed class Intelligence360Overlay
    {
        public Guid AnchorId { get; init; }
        public string AnchorType { get; init; } = string.Empty;
        public bool Found { get; init; }
        public IReadOnlyList<string> Alerts { get; init; } = Array.Empty<string>();
        public IReadOnlyList<ContextFact> ProvenFacts { get; init; } = Array.Empty<ContextFact>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> UnprovenNotes { get; init; } = Array.Empty<string>();
        public string Provider { get; init; } = "PRIMOX_360_INTELLIGENCE";
    }

    public sealed class Intelligence360Enricher : IIntelligence360Enricher
    {
        private readonly IContextCompositionService _composition;

        public Intelligence360Enricher(IContextCompositionService composition)
        {
            _composition = composition ?? throw new ArgumentNullException(nameof(composition));
        }

        public Task<Intelligence360Overlay> ForVehicleAsync(Guid vehicleId, CancellationToken ct = default) =>
            BuildAsync("Vehicle", vehicleId, new ContextCompositionRequest { VehicleId = vehicleId }, ct);

        public Task<Intelligence360Overlay> ForClientAsync(Guid clienteId, CancellationToken ct = default) =>
            BuildAsync("Client", clienteId, new ContextCompositionRequest { ClienteId = clienteId }, ct);

        public Task<Intelligence360Overlay> ForWorkOrderAsync(Guid workOrderId, CancellationToken ct = default) =>
            BuildAsync("WorkOrder", workOrderId, new ContextCompositionRequest { WorkOrderId = workOrderId }, ct);

        private async Task<Intelligence360Overlay> BuildAsync(string type, Guid id, ContextCompositionRequest req, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                return new Intelligence360Overlay
                {
                    AnchorId = id,
                    AnchorType = type,
                    Found = false,
                    Alerts = new[] { "ANCHOR_ID_EMPTY" },
                    MissingData = new[] { $"{type}Id válido" }
                };
            }

            var composed = await _composition.ComposeAsync(req, ct).ConfigureAwait(false);
            var alerts = new List<string>();
            if (composed.Conflicts.Count > 0) alerts.Add("CONTEXT_CONFLICTS_PRESENT");
            if (composed.UnprovenRelations.Count > 0) alerts.Add("UNPROVEN_RELATIONS_PRESENT");
            var recurring = composed.AllProvenFacts.FirstOrDefault(f => f.FactKey == "vehicle.problema_recorrente");
            if (recurring != null) alerts.Add("RECURRING_PROBLEM_ON_RECORD");

            return new Intelligence360Overlay
            {
                AnchorId = id,
                AnchorType = type,
                Found = composed.HasAnyProvenAnchor,
                Alerts = alerts,
                ProvenFacts = composed.AllProvenFacts.Take(12).ToList(),
                MissingData = composed.MissingData,
                UnprovenNotes = composed.UnprovenRelations.Select(r => $"{r.RelationType}:{r.Status}").ToList()
            };
        }
    }
}