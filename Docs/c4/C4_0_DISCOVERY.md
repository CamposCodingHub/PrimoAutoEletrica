# C4.0 DISCOVERY - PRIMOX Intelligence Continuity Map

**Machine:** CIRO  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Date:** 2026-09-27 10:00:02 -03:00  
**Branch:** `cycle-c3/primox-intelligence`  
**Baseline HEAD at discovery start:** `b8d21cbec2b02bb5257eac2f052d8afc70783c41`  
**Gate entry:** C3.13–C3.24 closed as **PASS_WITH_EXTERNAL_DEPENDENCY** (LIVE HTTP = LIVE_NOT_TESTED; keys ABSENT). No critical FAIL on security/integrity/grounding/isolation/kill-switch/redaction/regression from prior stack + continuous re-verify.

**Rule:** AI may **suggest only**. Never autonomous buy / pay / stock / fiscal / OS / client mutations.

---

## 1. Existing surfaces (inventory)

### Knowledge / Context / Assist (local grounded default)

| Component | Path | Role |
|-----------|------|------|
| AssistContextBuilder | `Services/Knowledge/AssistContextBuilder.cs` | Builds Assist context from local retrieval |
| AssistFailClosedPolicy | `Services/Knowledge/AssistFailClosedPolicy.cs` | Fail-closed Assist policy |
| ClientContextService | `Services/Knowledge/ClientContextService.cs` | CLIENTE context scope |
| VehicleContextService | `Services/Knowledge/VehicleContextService.cs` | VEICULO context scope |
| WorkOrderContextService | `Services/Knowledge/WorkOrderContextService.cs` | OS / WorkOrder context |
| ContextCompositionService | `Services/Knowledge/ContextCompositionService.cs` | Composes multi-source context |
| ContextualSearchService | `Services/Knowledge/ContextualSearchService.cs` | Contextual search |
| KnowledgeRetrievalService / Search / Index / Promotion | `Services/Knowledge/*` | Knowledge corpus retrieval + promotion |
| DiagnosticIntelligenceService | `Services/Knowledge/DiagnosticIntelligenceService.cs` | Diagnostic intelligence |
| Intelligence360Enricher | `Services/Knowledge/Intelligence360Enricher.cs` | 360 enrichment bridge |
| GroundedLocalRuleAssistantProvider | `Services/GroundedLocalRuleAssistantProvider.cs` | Default local Assist |
| AssistantService | `Services/AssistantService.cs` | Assist orchestration entry |

### Evidence / External provider / Audit / Redaction

| Component | Path | Role |
|-----------|------|------|
| ExternalEvidencePackage(+Builder) | `Services/ExternalAi/` | Evidence package for external path |
| ExternalResponseGroundingValidator | `Services/ExternalAi/` | Grounding / anti-hallucination |
| ExternalFinanceRedactor | `Services/ExternalAi/` | Finance redaction |
| ExternalPayloadMinimizationAuditor | `Services/ExternalAi/` | Data minimization check |
| HttpExternalAssistantProvider / Transport | `Services/ExternalAi/` | Live HTTP (armed only) |
| ExternalAssistantProviderSelector | `Services/ExternalAi/` | Fail-closed arming gates |
| AssistProviderRouter | `Services/ExternalAi/AssistProviderRouter.cs` | Prefer local; fallback on external fail; audit |
| DisabledExternalAssistantProvider | `Services/ExternalAi/` | Stub when not armed |
| IntelligenceAuditService | `Services/Knowledge/IntelligenceAuditService.cs` | **In-memory** ring (MaxEntries=500); **not** durable; **no** protected DB write |

### RBAC / Finance / 360 / WO / Diagnostic (product)

| Area | Known entry points | C4 note |
|------|--------------------|---------|
| Finance | `FinanceiroDatabaseService`, `RelatorioFinanceiroService`, UiSmoke.Financeiro | External path must keep finance redacted unless explicit RBAC include |
| 360 | `Primox360Service`, Produto360/Tool360 windows, Intelligence360Enricher | Suggest-only overlays; no auto mutations |
| Work Order / Oficina | `WorkOrderContextService`, UiSmoke.OficinaKanban | Context isolation OS A vs B already C3.13 PASS |
| Diagnostic | `DiagnosticIntelligenceService`, `DiagnosticoTecnicoService` | Local grounded + evidence IDs |
| RBAC | Finance include flag on external package; Assist fail-closed | No separate named RbacService file — gate is flag + role checks in Assist path |

### Data stores (integrity)

| Store | Path | Rule |
|-------|------|------|
| Protected DB | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` | SHA `C7420D18…A7CE0B` ReadOnly — **never mutate** |
| Operational DB | `primoauto_operacional.db` | Operational OK; not for intelligence durable audit yet |
| Audit | In-memory only | Durable non-protected audit = future authorized phase |

---

## 2. Observability separation (required honesty)

| Channel | Label | Status at discovery |
|---------|-------|---------------------|
| LOCAL grounded Assist | LOCAL | Implemented + unit/UiSmoke covered |
| MOCK HTTP external | MOCK_ONLY | Implemented + unit covered |
| LIVE HTTP external | LIVE / LIVE_NOT_TESTED | Wired but **LIVE_NOT_TESTED** (keys ABSENT) |

Never mix metrics across channels without labels.

---

## 3. C4.1+ candidate phases (not started here)

1. **C4.1** — Durable audit store on **non-protected** path (opt-in; retention; redaction) — designs only until authorized
2. **C4.2** — Observability metrics dashboard LOCAL vs MOCK vs LIVE (no fake live numbers)
3. **C4.3** — 360 Assist suggest-only overlays (Produto/Tool/WO) with fail-closed
4. **C4.4** — Context pack v2 (CLIENTE/VEICULO/OS) with RELATIONSHIP_NOT_PROVEN for soft FKs
5. **C4.5** — LIVE matrix completion when API key PRESENT (C3.13 LIVE-001..020 live subset)
6. **C4.6** — Explicit resolution matrix 1280/1366/1600/1920 × Light/Dark if still NOT_TESTED

Each phase: intermediate tests + own commit on `cycle-c3/primox-intelligence` only. No main merge.

---

## 4. Hard stops / soft rules carried into C4

- Preserve CentsV1 + Design System
- No empty catches
- Soft FKs unproven → `RELATIONSHIP_NOT_PROVEN`
- Assist fail-closed
- No fake PASS / no mock→live promotion
- AI suggest only — never autonomous buy/pay/stock/fiscal/OS/client changes
- STOP on protected DB SHA change, key leak, context leak, kill-switch fail, main change

---

## 5. Decision

**C4.0 Discovery = PASS** (map complete).  
**C4.1+ implementation:** deferred to subsequent continuous commits after this discovery doc is committed; first implementation candidate = observability labeling + durable-audit design note (no protected DB write).
