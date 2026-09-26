using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IContextCompositionService
    {
        Task<ComposedIntelligenceContext> ComposeAsync(ContextCompositionRequest request, CancellationToken ct = default);
    }

    /// <summary>
    /// C2.3 — composes Vehicle / Client / WorkOrder contexts without inventing joins.
    /// Cross-anchor consistency: if OS proves ClienteId/VeiculoId, prefer those over unrelated request ids
    /// and flag CONFLICT / RELATIONSHIP_NOT_PROVEN when caller ids disagree with proven FKs.
    /// Financial facts stay out unless IncludeFinancial=true (still never invented).
    /// </summary>
    public sealed class ContextCompositionService : IContextCompositionService
    {
        public const string WarningCrossClientDenied = "CROSS_CLIENT_DENIED";
        public const string WarningNoAnchor = "NO_PROVEN_ANCHOR";
        public const string WarningIdConflict = "REQUEST_ID_CONFLICT";

        private readonly IVehicleContextService _vehicles;
        private readonly IClientContextService _clients;
        private readonly IWorkOrderContextService _workOrders;
        private readonly LoggerService? _logger;

        public ContextCompositionService(
            IVehicleContextService vehicles,
            IClientContextService clients,
            IWorkOrderContextService workOrders,
            LoggerService? logger = null)
        {
            _vehicles = vehicles ?? throw new ArgumentNullException(nameof(vehicles));
            _clients = clients ?? throw new ArgumentNullException(nameof(clients));
            _workOrders = workOrders ?? throw new ArgumentNullException(nameof(workOrders));
            _logger = logger;
        }

        public async Task<ComposedIntelligenceContext> ComposeAsync(ContextCompositionRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ct.ThrowIfCancellationRequested();

            var warnings = new List<string>();
            var conflicts = new List<string>();
            var missing = new List<string>();

            WorkOrderIntelligenceContext? woCtx = null;
            if (request.WorkOrderId.HasValue && request.WorkOrderId.Value != Guid.Empty)
            {
                woCtx = await _workOrders.BuildAsync(request.WorkOrderId.Value, ct).ConfigureAwait(false);
            }

            // Resolve preferred anchors from proven OS links first (never invent).
            Guid? vehicleId = request.VehicleId;
            Guid? clienteId = request.ClienteId;

            if (woCtx is { Found: true })
            {
                if (woCtx.VeiculoIdProven.HasValue)
                {
                    if (vehicleId.HasValue && vehicleId.Value != Guid.Empty && vehicleId.Value != woCtx.VeiculoIdProven.Value)
                    {
                        conflicts.Add($"Request.VehicleId={vehicleId:D} diverge de OS.VeiculoId comprovado={woCtx.VeiculoIdProven:D}");
                        warnings.Add(WarningIdConflict);
                        // Do not silently swap — keep request id but mark conflict; still load both? Prefer proven OS vehicle for composition.
                    }
                    vehicleId = woCtx.VeiculoIdProven;
                }

                if (woCtx.ClienteIdProven.HasValue)
                {
                    if (clienteId.HasValue && clienteId.Value != Guid.Empty && clienteId.Value != woCtx.ClienteIdProven.Value)
                    {
                        conflicts.Add($"Request.ClienteId={clienteId:D} diverge de OS.ClienteId comprovado={woCtx.ClienteIdProven:D}");
                        warnings.Add(WarningIdConflict);
                    }
                    clienteId = woCtx.ClienteIdProven;
                }
            }

            // Cross-client fail-closed at composition boundary (session vs requested/proven client).
            if (request.SessionClienteId.HasValue &&
                clienteId.HasValue &&
                clienteId.Value != Guid.Empty &&
                request.SessionClienteId.Value != clienteId.Value)
            {
                warnings.Add(WarningCrossClientDenied);
                return new ComposedIntelligenceContext
                {
                    Query = request.Query ?? string.Empty,
                    Warnings = warnings,
                    MissingData = new[] { "Autorização / escopo de cliente compatível" },
                    Conflicts = conflicts,
                    HasAnyProvenAnchor = false,
                    BuiltAt = DateTimeOffset.Now
                };
            }

            VehicleIntelligenceContext? vehicleCtx = null;
            if (vehicleId.HasValue && vehicleId.Value != Guid.Empty)
            {
                vehicleCtx = await _vehicles.BuildAsync(vehicleId.Value, ct).ConfigureAwait(false);
            }

            ClientIntelligenceContext? clientCtx = null;
            if (clienteId.HasValue && clienteId.Value != Guid.Empty)
            {
                clientCtx = await _clients.BuildAsync(clienteId.Value, ct).ConfigureAwait(false);
            }

            // If vehicle proves a different client than requested (without OS), flag — do not invent merge.
            if (vehicleCtx is { Found: true, ClienteIdProven: { } vClient } &&
                request.ClienteId.HasValue &&
                request.ClienteId.Value != Guid.Empty &&
                request.ClienteId.Value != vClient &&
                woCtx == null)
            {
                conflicts.Add($"Request.ClienteId={request.ClienteId:D} diverge de Veiculos.ClienteId={vClient:D}");
                warnings.Add(WarningIdConflict);
            }

            bool includeFinancial = request.IncludeFinancial;
            IEnumerable<ContextFact> FilterFacts(IEnumerable<ContextFact> facts) =>
                facts.Where(f =>
                    includeFinancial ||
                    !string.Equals(f.Classification, "FINANCIAL", StringComparison.OrdinalIgnoreCase));

            var allProven = new List<ContextFact>();
            var unproven = new List<ContextRelation>();

            void Absorb(IReadOnlyList<ContextFact>? facts, IReadOnlyList<ContextRelation>? rels, IReadOnlyList<string>? miss, IReadOnlyList<string>? conf, IReadOnlyList<string>? warn)
            {
                if (facts != null)
                {
                    allProven.AddRange(FilterFacts(facts).Where(f => f.Status == ContextProvenanceStatus.PROVEN));
                }
                if (rels != null)
                {
                    unproven.AddRange(rels.Where(r =>
                        r.Status == ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN ||
                        r.Status == ContextProvenanceStatus.CONFLICT));
                }
                if (miss != null) missing.AddRange(miss);
                if (conf != null) conflicts.AddRange(conf);
                if (warn != null) warnings.AddRange(warn);
            }

            if (woCtx != null) Absorb(woCtx.Facts, woCtx.Relations, woCtx.MissingData, woCtx.Conflicts, woCtx.Warnings);
            if (vehicleCtx != null) Absorb(vehicleCtx.Facts, vehicleCtx.Relations, vehicleCtx.MissingData, vehicleCtx.Conflicts, vehicleCtx.Warnings);
            if (clientCtx != null) Absorb(clientCtx.Facts, clientCtx.Relations, clientCtx.MissingData, clientCtx.Conflicts, clientCtx.Warnings);

            var hasAnchor =
                (woCtx?.Found == true) ||
                (vehicleCtx?.Found == true) ||
                (clientCtx?.Found == true);

            if (!hasAnchor)
            {
                warnings.Add(WarningNoAnchor);
                if (string.IsNullOrWhiteSpace(request.Query) &&
                    !request.VehicleId.HasValue &&
                    !request.ClienteId.HasValue &&
                    !request.WorkOrderId.HasValue)
                {
                    missing.Add("Informe VehicleId, ClienteId ou WorkOrderId com relação comprovável");
                }
            }

            _logger?.LogInfo(
                $"Compose query='{(request.Query ?? string.Empty).Trim()}' anchor={hasAnchor} provenFacts={allProven.Count} unprovenRels={unproven.Count}",
                "ContextComposition");

            return new ComposedIntelligenceContext
            {
                Query = request.Query ?? string.Empty,
                Vehicle = vehicleCtx,
                Client = clientCtx,
                WorkOrder = woCtx,
                AllProvenFacts = allProven
                    .GroupBy(f => f.FactKey, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList(),
                UnprovenRelations = unproven,
                MissingData = missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Conflicts = conflicts.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                HasAnyProvenAnchor = hasAnchor,
                BuiltAt = DateTimeOffset.Now
            };
        }
    }
}