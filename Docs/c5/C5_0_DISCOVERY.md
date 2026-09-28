# C5.0 DISCOVERY — PRIMOX Intelligence Auditability Continuity Map

**Machine:** CIRO (`de411c5d-4243-4085-bf33-8c3d241d7f2f`)  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Date:** 2026-09-28 06:18:56 -03  
**Branch:** `cycle-c5/primox-intelligence`  
**Created FROM:** `b6fc22d7b6d1a894267fabf28acec5c515018b6c` (cycle-c4 tip) — ancestry verified (`merge-base --is-ancestor` exit 0)  
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` **UNTOUCHED** (NEVER push main)  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged (ReadOnly)**  
**Operational DB:** `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto_operacional.db` **PRESENT** (audit-test target)  
**Keys:** `PRIMOX_EXTERNAL_AI_API_KEY` / `ENABLED` / `KILL_SWITCH` = **ABSENT** → LIVE = **LIVE_NOT_TESTED**

**Rule:** AI may **suggest only**. Never autonomous buy / pay / stock / fiscal / OS / client mutations.  
**Priority (this cycle):** persistent audit → integrity → correlation → provider lifecycle → LIVE if key → grounding → isolation → RBAC/redaction → resilience → perf → final.

---

## 1. C4 stack capabilities (verified inventory)

### 1.1 Local intelligence (C4.1–C4.12)

| Capability | Path | Status |
|------------|------|--------|
| SourceTagged context package | `Services/Knowledge/SourceTaggedContextPackage.cs` + C4.1 tests | PASS (C4) |
| Evidence ranking | `EvidenceRankingService.cs` | PASS (C4) |
| Diagnostic reasoning (HYPOTHESIS-only) | `DiagnosticReasoningService.cs` | PASS (C4) |
| Client / Vehicle / WO 360 | `C4OperationalIntelligence.cs` | PASS (C4) |
| Knowledge promotion (PUBLISHER gate) | `KnowledgePromotionService.cs` | PASS (C4) |
| Operational recommendations | `C4OperationalIntelligence.cs` | PASS (C4) |
| Explainability | C4.9 surfaces | PASS (C4) |
| Evaluation framework EVAL-001..012 | `EvaluationFramework.cs` | PASS (C4) / LIVE_NOT_TESTED honesty |
| Adversarial guard 14 classes | `AdversarialGuard.cs` | PASS 14/14 (C4) |
| Context builders (Client/Vehicle/WO/Compose) | `Services/Knowledge/*Context*.cs` | PASS (C3/C4) |
| Grounded local Assist default | `GroundedLocalRuleAssistantProvider.cs` | PASS |
| Soft FK honesty | `ContextProvenanceStatus.RELATIONSHIP_NOT_PROVEN` | PASS |

### 1.2 External Assist / provider (C3 carry-forward)

| Component | Path | Role |
|-----------|------|------|
| AssistProviderRouter | `ExternalAi/AssistProviderRouter.cs` | Prefer local; optional external when armed; fallback + in-memory audit Record |
| ExternalAssistantProviderSelector | `ExternalAi/ExternalAssistantProviderSelector.cs` | Fail-closed arming: KillSwitch / Disabled / MissingSecret / ArmedReady |
| HttpExternalAssistantProvider + Transport | `ExternalAi/Http*.cs` | LIVE HTTP when armed + evidence |
| DisabledExternalAssistantProvider | stub | Fail-closed when not armed |
| ExternalEvidencePackage(+Builder) | evidence package | Min-necessary for external |
| ExternalResponseGroundingValidator | grounding / anti-hallucination | Reject EXTERNAL_UNGROUNDED |
| ExternalFinanceRedactor | finance redaction | Before provider when no RBAC |
| ExternalPayloadMinimizationAuditor | minimization check | |
| ExternalAssistantSettingsStore | JSON LocalAppData | enable/model/kill-switch only — **never** API key |
| ExternalAssistantOptions | env var **name** only | Default Enabled=false |
| IExternalAssistantSecretSource | env resolution | Key never in settings file |

### 1.3 Audit surfaces (critical gap for C5)

| Surface | Persistence | Notes |
|---------|-------------|-------|
| `IntelligenceAuditService` | **In-memory ring (MaxEntries=500)** | Used by AssistProviderRouter.Record; survives process? **NO** → C4 final: **NOT_IMPLEMENTED** durable |
| `AuditLogService` → table `AuditLogs` | **SQLite durable** via `DatabaseService` | Product audit pattern; schema already has `CorrelationId`, `UsuarioId`, `SessaoId`, `Perfil` |
| `AssistantService` | Calls `_auditLogService.Registrar(categoria:"Assist", ...)` | Durable product audit lines; **does not** pass CorrelationId today; details are free-text summaries |
| Protected `primoauto.db` | ReadOnly | Runtime auto-redirects to `primoauto_operacional.db` when ReadOnly (B2.1) |

**Honesty:** In-memory alone = **NOT_IMPLEMENTED** for C5.1 durable audit. Must follow existing PRIMOX persistence (`DatabaseService` / SQLite / operational DB), not invent a random new DB layer.

---

## 2. Dataflow (intelligence path)

```
User/Session (AppSessionService: UserId, SessionId, AccessProfile)
  → AssistantService (RBAC finance gate: Administrador / permission)
    → AssistFailClosedPolicy (pre-provider)
    → AssistContextBuilder / ContextComposition / Evidence retrieval
    → AdversarialGuard (query / stored content)
    → AssistProviderRouter
         ├─ LOCAL grounded (default)
         └─ EXTERNAL (only if preferExternal && WouldArmLiveGates)
              → AUTH gates (kill-switch / enabled / secret)
              → FILTER (allowed context / evidence package)
              → REDACTION (ExternalFinanceRedactor)
              → PROVIDER (HttpExternalAssistantProvider)
              → GROUNDING (ExternalResponseGroundingValidator)
              → FALLBACK local on fail / ungrounded
    → IntelligenceAuditService.Record  [IN-MEMORY ONLY today]
    → AuditLogService.Registrar(Assist/…) [DURABLE product audit, incomplete intel fields]
```

**Desired C5 correlation chain:**  
User → Assist → Context → Evidence → Provider → Grounding → Response → Audit  
with a single **CorrelationId** end-to-end; REQUEST-A vs REQUEST-B must not mix.

---

## 3. Gaps / risks (C5 candidates)

| ID | Gap | Risk | C5 phase |
|----|-----|------|----------|
| G-01 | Intelligence audit not durable across process | Cannot forensically reconstruct Assist decisions | **C5.1** |
| G-02 | No append-integrity proof on intel audit (create/read/update/delete/tamper/cross-user) | Tamperable audit = false trust | **C5.2** |
| G-03 | CorrelationId not threaded on Assist/Router/Grounding path (schema exists on AuditLogs only) | Cannot correlate A vs B; mixing risk | **C5.3** |
| G-04 | Provider lifecycle modes not first-class as LOCAL/EXTERNAL/DISABLED/FALLBACK/ERROR matrix with mandatory recording | Silent switch possible without durable record | **C5.4** |
| G-05 | LIVE HTTP never validated (keys ABSENT) | External path unproven in this environment | **C5.5** / LIVE_NOT_TESTED |
| G-06 | LIVE negative matrix (invalid/timeout/4xx/5xx/malformed/network) partial / MOCK_ONLY | Resilience honesty incomplete for LIVE | **C5.6** |
| G-07 | Grounding: UNGROUNDED reject exists; explicit CONFLICT vs CONFIRMED when remote contradicts local evidence is weak/undocumented | False CONFIRMED risk | **C5.7** |
| G-08 | Context isolation C3/C4 PASS unit; must re-verify A/B + LIVE if available; STOP on leak | Cross-client / OS leak = CRITICAL | **C5.8** |
| G-09 | RBAC+redaction order AUTH→FILTER→REDACTION→PROVIDER exists in design; need explicit pipeline enforcement + roles per real PRIMOX PerfilAcesso | Finance/PII leak to provider | **C5.9** |
| G-10 | Retry/timeout: RequestTimeout=30s; finite retry not fully documented; must prove no crash/infinite/false success/auto-action | Resilience | **C5.10** |
| G-11 | Perf LOCAL/MOCK/LIVE separation; no invent p95 | Observability honesty | **C5.11** |
| G-12 | Full suites + Full App (actual count) + EXE republish + final matrix | Regression | **C5.12** |

---

## 4. Audit field design candidates (C5.1) — justified only

Follow existing `AuditLogService` + SQLite patterns on **operational/homolog** DB (never protected `primoauto.db`).

| Field | Justification | Store? |
|-------|---------------|--------|
| AuditId | Primary key / forensics | YES |
| Timestamp | When | YES |
| UserId | Who | YES |
| SessionId | Session scope | YES |
| Action | Assist action (Consulta/FailClosed/Fallback/…) | YES |
| Provider | LOCAL / EXTERNAL provider id | YES |
| ProviderMode | LOCAL/EXTERNAL/DISABLED/FALLBACK/ERROR | YES |
| ContextType | CLIENT/VEHICLE/OS/COMPOSITE/NONE | YES |
| ContextId | Scoped id (no PII dump) | YES |
| CorrelationId | End-to-end request id | YES |
| EvidenceCount | Volume without payloads | YES |
| EvidenceIds | Citeable ids only | YES |
| GroundingStatus | CONFIRMED/CONFLICT/UNGROUNDED/REJECT/N_A | YES |
| ResultStatus | OK/REJECTED/FAIL_CLOSED/FALLBACK/ERROR | YES |
| FailureReason | Short reason (redacted) | YES |
| Duration | Observability | YES |
| FallbackUsed | bool | YES |
| SecurityDecision | ALLOW/DENY/REDACT/KILL_SWITCH | YES |

**Minimize — do NOT store by default:** API keys/tokens/raw auth, full prompts, full provider responses, finance amounts/PII dumps. If ever justified later → separate documented exception + redaction.

**RBAC on audit read:** only profiles with appropriate access (e.g. Administrador / explicit audit permission) may list intelligence audit; cross-user reads denied unless admin.

**CRITICAL persistence test:** write → dispose/reopen connection (or new service instance against same SQLite file) → read back. In-memory alone = **NOT_IMPLEMENTED**.

---

## 5. Provider lifecycle (current → C5.4 target)

### Current arming states (`ExternalAssistantArmingState`)
KillSwitch | DisabledByDefault | MissingSecret | ArmedReady | ArmedButLiveNotWired(legacy)

### Current status labels
disabled | kill-switch | enabled/no key | live | not wired

### Target deterministic modes for audit/recording
**LOCAL / EXTERNAL / DISABLED / FALLBACK / ERROR**

**Priority (documented intent for C5.4):**
1. Kill-switch ON → DISABLED (no external call)
2. Enabled OFF → LOCAL (default)
3. Enabled ON + no key → LOCAL + ReadyNoKey (no silent EXTERNAL)
4. Enabled ON + invalid/timeout/4xx/5xx/malformed/network → FALLBACK or ERROR with record (never silent success)
5. Enabled ON + valid key + evidence + grounding OK → EXTERNAL
6. External fail / ungrounded → FALLBACK to LOCAL with FallbackUsed=true recorded

**Matrix cells (C5.4):** OFF / ON-no-key / invalid / valid / timeout / 4xx / 5xx / malformed / network — never silent switch without recording.

---

## 6. Security boundaries

| Boundary | Mechanism | C5 note |
|----------|-----------|---------|
| Protected DB | ReadOnly + SHA gate | Rehash EVERY phase; change = STOP CRITICAL-DATA-INTEGRITY-001 |
| Operational DB | Writable runtime | Intelligence durable audit + homolog tests ONLY here |
| Secrets | Env var name in options; value via secret source | Never print; never ask paste in chat; never invent LIVE |
| Settings file | enable/model/kill-switch only | Reject secret-like saves |
| Finance | Redactor + RBAC Administrador/permission | Before provider |
| Context isolation | SessionClienteId / CROSS_*_DENIED | STOP on leak |
| Adversarial | 14 attack classes | Stored content cannot override system |
| Suggest-only | No auto buy/pay/stock/fiscal/OS/client | Preserve |
| CentsV1 / Design System | Money path untouched | Preserve |
| Soft FK | RELATIONSHIP_NOT_PROVEN | Preserve |
| origin/main | Untouched | Push ONLY `cycle-c5/primox-intelligence` |

---

## 7. Baseline gates to re-verify (do not assume counts)

| Gate | C4 recorded | C5 rule |
|------|-------------|---------|
| Unit | 655/655 | Re-run; count may change with new tests |
| ExtAssist | 51/51 | Re-verify what each phase impacts |
| Full App UI Smoke | 219/219 | **Do not assume stays 219** — record actual |
| Installed EXE intel filter | 13/13 | Republish + SHA in C5.12 |
| Adversarial | 14/14 | Regression |
| Audit durable | NOT_IMPLEMENTED | C5.1 target |
| LIVE | LIVE_NOT_TESTED | Continue unless secure key present |
| Matrix 8 | NOT_APPLICABLE (no UI change C4) | NOT_APPLICABLE unless UI changed |

---

## 8. Persistence pattern to reuse (no duplicate random layer)

1. **Schema:** `CREATE TABLE IF NOT EXISTS` via `DatabaseService` partial / dedicated initializer (same style as `DatabaseService.Audit.cs`).
2. **Write path:** service analogous to `AuditLogService.Registrar` — INSERT only, parameterized, session fields from `AppSessionService`.
3. **Read path:** filtered queries + **RBAC gate** before return.
4. **Test DB:** temp SQLite file or operational/homolog — **never** protected `primoauto.db`.
5. **Optional bridge:** keep `IIntelligenceAuditService` API; add durable implementation behind it (or decorator: memory+SQLite) so Router continues to Record without dual call sites — inspect before duplicating.

Existing product `AuditLogs` already has CorrelationId — C5 may either:
- **A)** Dedicated `IntelligenceAuditLogs` table (cleaner justified fields), or  
- **B)** Extend Assist Registrar payloads into AuditLogs with structured Detalhes + CorrelationId  

**Preferred (C5.1):** dedicated table using same SQLite/`DatabaseService` patterns, keeping product AuditLogs unchanged for non-intel events; avoid dumping full prompts into Detalhes.

---

## 9. Hard stops carried into C5

- Protected DB SHA change → STOP CRITICAL-DATA-INTEGRITY-001  
- Key leak / secret print → STOP  
- Context leak A↔B → STOP  
- Kill-switch fail → STOP  
- Push/merge to main → FORBIDDEN  
- Fake LIVE / invent metrics / mock→live promotion → FORBIDDEN  
- Empty catches → FORBIDDEN  
- In-memory-only claiming durable audit → NOT_IMPLEMENTED  

---

## 10. Decision

**C5.0 Discovery = PASS** (map complete).  

**Next:** C5.1 Persistent Intelligence Audit on operational/homolog SQLite following `AuditLogService`/`DatabaseService.Audit` patterns; justified fields only; RBAC on read; CRITICAL write→reopen→readback proof; docs `Docs/c5/C5_1_*.md`; separate commit; push `cycle-c5` only.

**Final classification target (cycle):** `PASS_WITH_EXTERNAL_DEPENDENCY` if only key absent and all local gates PASS.

