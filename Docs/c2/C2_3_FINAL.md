# C2.3 FINAL — Context Engine

**Branch:** `cycle-c2/primox-intelligence`  
**Date (America/Sao_Paulo):** 2026-09-26  
**C2.4 started:** NO (default STOP until Night Run continues)

## Decision

**PASS** (service-first; UI impact minimal)

Protected SHA unchanged. `origin/main` untouched. No OpenAI/embeddings. Soft FKs never invent relations.

## Objective

Build grounded Vehicle / Client / WorkOrder intelligence contexts with explicit provenance ("where from?") and compose them without inventing joins.

## Delivered

| Piece | Path |
|-------|------|
| Models | `Models/IntelligenceContextModels.cs` (`ContextFact`, `ContextRelation`, `*IntelligenceContext`, `ComposedIntelligenceContext`) |
| Vehicle | `Services/Knowledge/VehicleContextService.cs` |
| Client | `Services/Knowledge/ClientContextService.cs` |
| WorkOrder | `Services/Knowledge/WorkOrderContextService.cs` |
| Composition | `Services/Knowledge/ContextCompositionService.cs` |
| DI | `DependencyInjection/ServiceExtensions.cs` |
| Tests | `Tests/.../ContextEngineTests.cs` (12) |

## Provenance rules (enforced)

1. **PROVEN** — row exists + FK/Id match (e.g. `OrdensServico.VeiculoId == Veiculos.Id`).
2. **RELATIONSHIP_NOT_PROVEN** — soft FK / plate-only / missing target row (e.g. `PlacaSnapshot` match without `VeiculoId`; `DiagnosticCases.VehicleId` without `Veiculos` row).
3. **MISSING** — empty field or omitted by classification.
4. **CONFLICT** — proven anchors disagree (OS ClienteId vs Veiculo.ClienteId; request id vs OS-proven id).

Every `ContextFact` carries `SourceEntity`, `SourceField`, `OriginDescription`.

## Classification / fail-closed

- Client CPF/contacts: omitted (SENSITIVE / PERSONAL minimized).
- Financial rollups: not in Context Engine (stay in Primox360 + finance permission).
- Cross-client session mismatch at composition → `CROSS_CLIENT_DENIED`, no anchors returned.
- Chassi/Renavam never enter vehicle facts.

## Tests

| Suite | Result |
|-------|--------|
| ContextEngineTests | **12/12 PASS** |
| Full unit Debug (with C2.3 only pre-C2.4) | **549 PASS** |
| ContextEngineTests | **12/12 PASS** |

## Limitations (honest)

1. No dedicated Context UI panel in C2.3 (service-first; Assist wiring deepens in C2.4/C2.5).
2. Checklist / Auto Elétrica JSON measurements still not loaded into context (same C2.1 gap).
3. OS→vehicle plate-only matches are reported as unproven warnings, not silently merged.
4. DiagnosticCase soft FK is proven only when the target vehicle/OS row exists and IDs match.

## Git

Commit message: `C2.3: implement grounded context engine (proven relations only)`

## Protected DB

Must remain `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` after phase.