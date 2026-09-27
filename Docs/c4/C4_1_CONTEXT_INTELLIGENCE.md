# C4.1 CONTEXT INTELLIGENCE (DEEPENED)

**Machine:** CIRO  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Date:** 2026-09-27 20:14:31 -03  
**Branch:** `cycle-c4/primox-intelligence`  
**Parent tip:** `0d56cefbf36a3f17c784400639a8e9289a45daab`  
**Protected DB SHA256:** `c7420d1811d4cfea16ce833326c6a331f360bebf025ea7f3a7ee785192a7ce0b` (unchanged)

## Scope

Deepen `SourceTaggedContextPackage` — DATA→CONTEXT package with real source tags only:

| Section | Tag | Behavior |
|---------|-----|----------|
| user | USER / UNKNOWN | Actor UserId; UNKNOWN if absent |
| perms | PERMS / UNKNOWN | Permissions + finance flag |
| client | CLIENTE | From composition proven facts |
| vehicle | VEICULO | From composition |
| OS | OS | From composition |
| budget | BUDGET | Only if finance allowed + fact exists; else MissingData |
| diagnostic | DIAGNOSTIC | Proven diagnostic case ids |
| services | SERVICES | WorkOrder ServiceItemLabels |
| parts | PARTS | WorkOrder PartItemLabels |
| history | HISTORY | Vehicle historico / client last_os (never invent) |
| agenda | AGENDA | Real only; else MissingData AGENDA_DATA_ABSENT |
| knowledge / evidence | KNOWLEDGE / EVIDENCE / ASSIST_LOCAL / EXTERNAL_EVIDENCE | From evidence builder |
| finance | FINANCE | Only if IncludeFinancial AND actor.CanIncludeFinance |

Unknown origin → `ContextSourceTag.UNKNOWN` (never invent SYSTEM).

## Isolation (CRITICAL STOP)

`ValidateIsolation` detects CLIENT/VEHICLE/OS A vs B mismatches and vehicle/OS client leaks.  
On leak: `ContextIsolationViolationException` (`ISOLATION_LEAK_CRITICAL`).  
Cross-client session: fail-closed package (`CROSS_CLIENT_DENIED`), no foreign client facts.

## Reuse (no duplicate stacks)

- `ContextCompositionService`
- `ExternalEvidencePackageBuilder` (+ finance gate)
- `IntelligenceAuditService` (in-memory; AUDIT_PERSISTENCE=NOT_IMPLEMENTED)

## Tests

| Suite | Result |
|-------|--------|
| C41SourceTaggedContextPackageTests | **11/11 PASS** |
| Isolation CLIENT A vs B | PASS |
| Isolation VEHICLE A vs B | PASS |
| Isolation OS A vs B | PASS |
| Leak throws CRITICAL | PASS |
| Finance RBAC gate | PASS |

## Honesty

- LIVE HTTP: **LIVE_NOT_TESTED** (keys ABSENT)
- Durable audit: **NOT_IMPLEMENTED**
- Suggest-only; no autonomous mutations
- Soft FKs: RELATIONSHIP_NOT_PROVEN upstream

## Decision

**C4.1 Context Intelligence (deepened) = PASS** (local unit + isolation).  
No main merge. No protected DB write.
