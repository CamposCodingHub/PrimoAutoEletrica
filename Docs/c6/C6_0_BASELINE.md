# C6.0 BASELINE - PRIMOX Intelligence Feasibility Kickoff

**Machine:** CIRO (`de411c5d-4243-4085-bf33-8c3d241d7f2f`)  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Date:** 2026-09-28 06:56:29 -03  
**Branch:** `cycle-c6/primox-intelligence`  
**Created FROM:** `71e008810cdaf29f47eae76b63b478ee72d763c1` (cycle-c5 tip) — ancestry verified (`merge-base --is-ancestor` exit 0)  
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` **UNTOUCHED** (NEVER push main)  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged (ReadOnly)**  
**Operational DB:** `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto_operacional.db` **PRESENT**  
**Keys:** `PRIMOX_EXTERNAL_AI_API_KEY` / `ENABLED` / `KILL_SWITCH` / `OPENAI_API_KEY` = **ABSENT** → EXTERNAL LIVE = **LIVE_NOT_TESTED**

**Goal (this cycle):** empirical answer — *"best intelligence architecture for PRIMOX years ahead"* — **NOT** production AI. Decision must be data-based, vendor-neutral.  
**Rule:** AI may **suggest only**. Never autonomous buy / pay / stock / fiscal / OS / client mutations.  
**Statuses allowed:** NOT_TESTED | PASS | FAIL | BLOCKED | EXTERNAL_DEPENDENCY | MOCK_ONLY | STUB_ONLY | OBSERVATION_ONLY | PASS_WITH_EXTERNAL_DEPENDENCY | LIVE_NOT_TESTED | HUMAN_REVIEW_REQUIRED | PRICE_NOT_VERIFIED | ENVIRONMENT_DEPENDENCY.  
**Honesty:** Never NOT_TESTED→PASS, MOCK→LIVE. Never invent LIVE results, prices, latency, or quality scores.

---

## 1. Tip / ancestry verification

| Check | Result |
|-------|--------|
| Source tip (cycle-c5) | `71e008810cdaf29f47eae76b63b478ee72d763c1` — `C5.12: set continuous run final tip hashes` (2026-09-28 06:47:37 -03) |
| New branch tip at create | `71e008810cdaf29f47eae76b63b478ee72d763c1` (identical to source) |
| `merge-base --is-ancestor 71e0088 HEAD` | exit 0 — **PASS** |
| `origin/main` | `bf1eb784a3ed45782487197f38d9ba15d319997e` — **not ancestor of cycle-c5 tip** (224 commits ahead of main on cycle path); main **untouched** |
| Protected DB rehash | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` — **matches kickoff** |

**C6.0 status:** **PASS** (tip correct; not BLOCKED).

---

## 2. C5 carry-forward (mandatory status snapshot)

From `Docs/c5/C5_CONTINUOUS_RUN_FINAL.md` (tip before final-doc hash commit was `0585511`; continuous-run final tip = `71e0088`):

| Area | Classification | Notes |
|------|----------------|-------|
| Unit | **PASS** 695/695 | C5 final |
| ExtAssist | **PASS** 51/51 | carry-forward |
| Build Release | **PASS** | Deploy-ToInstalledApp |
| UI Smoke Full App | **PASS** 219/219 | c5_cont_fullapp |
| Installed EXE intelligence | **PASS** 13/13 | Local App shortcut |
| Matrix 8 | **NOT_APPLICABLE** | no visual change in C5 |
| LIVE HTTP | **LIVE_NOT_TESTED** | keys ABSENT |
| Durable intelligence audit | **PASS** | PersistentIntelligenceAuditService + SQLite |
| Soft FK honesty | **PASS** | RELATIONSHIP_NOT_PROVEN |
| Protected DB integrity | **PASS** | SHA unchanged |
| Final C5 class | **PASS_WITH_EXTERNAL_DEPENDENCY** | |

Principle preserved: DATA→CONTEXT→EVIDENCE→RANKING→ASSIST→EXPLAIN→HUMAN DECISION. Never DATA→AI→AUTOMATIC ACTION.

---

## 3. Architecture inventory (reuse; no duplicate abstractions)

### 3.1 Provider contract (keep `IAssistantProvider`)

```csharp
public interface IAssistantProvider
{
    string ProviderId { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default);
}
```

Path: `PrimoAutoEletrica/Models/AssistantContext.cs`

**Allowed future modes (C6):** GroundedLocalRule | LocalModel | ExternalModel | Disabled | Future.  
**Do not** rigid-couple to OpenAI. Adapters OK; contract stays vendor-neutral.

### 3.2 Existing providers / routing

| Component | Path | Role | C6 relevance |
|-----------|------|------|--------------|
| GroundedLocalRuleAssistantProvider | `Services/GroundedLocalRuleAssistantProvider.cs` | Default local grounded rules | Scenario A baseline |
| AssistProviderRouter | `ExternalAi/AssistProviderRouter.cs` | Prefer local; optional external; fallback + audit Record | Router precursor for C6.11 |
| ExternalAssistantProviderSelector | `ExternalAi/ExternalAssistantProviderSelector.cs` | Fail-closed arming | C6.7 LIVE gates |
| HttpExternalAssistantProvider + Transport | `ExternalAi/Http*.cs` | LIVE HTTP when armed | C6.7 experimental OpenAI adapter |
| DisabledExternalAssistantProvider | stub | Fail-closed | Disabled mode |
| ProviderLifecycleResolver | `ExternalAi/ProviderLifecycleResolver.cs` | LOCAL/EXTERNAL/DISABLED/FALLBACK/ERROR | C5.4 carry-forward |
| AssistantService | `Services/AssistantService.cs` | App entry; RBAC finance gate | Keep |

### 3.3 Knowledge / evidence stack (C3–C4)

| Capability | Path |
|------------|------|
| SourceTagged context | `Services/Knowledge/SourceTaggedContextPackage.cs` |
| Evidence ranking | `EvidenceRankingService.cs` |
| Diagnostic reasoning (HYPOTHESIS-only) | `DiagnosticReasoningService.cs` |
| Client / Vehicle / WO 360 | `C4OperationalIntelligence.cs` |
| Knowledge promotion | `KnowledgePromotionService.cs` |
| Evaluation framework EVAL-001..012 | `EvaluationFramework.cs` |
| Adversarial guard 14 classes | `AdversarialGuard.cs` |
| Context builders | `*Context*.cs` / `AssistContextBuilder.cs` |
| Grounding validator | `ExternalResponseGroundingValidator.cs` |
| Finance redaction | `ExternalFinanceRedactor.cs` |
| Persistent audit | `PersistentIntelligenceAuditService.cs` |

### 3.4 ABSENT at C6.0 (to be built)

| Capability | Status | Target phase |
|------------|--------|--------------|
| Benchmark provider metrics contract (tokens, latency, TTFT, cost, grounding, EvidenceCount, CorrelationId, …) | **NOT_TESTED** / absent | **C6.1** |
| Model registry `ModelDefinition` + PriceSource/PriceCheckedAt | **NOT_TESTED** / absent | **C6.2** |
| Official ≥100-case PRIMOX benchmark (28 domains) | **NOT_TESTED** / absent | **C6.3** |
| Golden answers + deterministic evaluator | **NOT_TESTED** / absent | **C6.4** |
| A–G knowledge vs model comparison harness | **NOT_TESTED** / absent | **C6.5** |
| `LocalModelAssistantProvider` (Endpoint/ModelId/Timeout/…) | **NOT_TESTED** / absent (no LocalModel intel provider) | **C6.6** |
| External adapter experimental (OpenAI first) contract/mock without key | partial HTTP exists; benchmarking contract **NOT_TESTED** | **C6.7** |
| Independent cost engine + usage scenarios | **NOT_TESTED** / absent | **C6.8** |
| Local infra economics (GPU/CPU/RAM) | **NOT_TESTED** / absent | **C6.9** |
| Quality/Latency/Cost matrix | **NOT_TESTED** | **C6.10** |
| Intelligence Router prototype (deterministic→local→external→safe fallback) | Router exists; C6 policy router **NOT_TESTED** | **C6.11** |
| Final architecture decision gate | **NOT_TESTED** | **C6.12** |

---

## 4. Keys / LIVE policy (reconfirmed)

| Variable | State |
|----------|-------|
| `PRIMOX_EXTERNAL_AI_API_KEY` | **ABSENT** |
| `PRIMOX_EXTERNAL_AI_ENABLED` | **ABSENT** |
| `PRIMOX_EXTERNAL_AI_KILL_SWITCH` | **ABSENT** |
| `OPENAI_API_KEY` | **ABSENT** |

→ All EXTERNAL LIVE work = **LIVE_NOT_TESTED**. Continue with **MOCK_ONLY** / **STUB_ONLY** / **NOT_TESTED**. Never invent LIVE, prices, latency, or quality. Never print secrets. Never ask to paste keys in chat.

---

## 5. Protected data integrity gate

| Asset | Path | SHA256 | Policy |
|-------|------|--------|--------|
| Protected DB | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` | ReadOnly; rehash every phase; change = **STOP CRITICAL-DATA-INTEGRITY-001** |

---

## 6. C6 phase plan (commit naming)

| Phase | Commit subject | Deliverable |
|-------|----------------|-------------|
| C6.0 | Baseline | `Docs/c6/C6_0_BASELINE.md` (this file) |
| C6.1 | Model Provider Contract | Benchmark metrics on provider results — no secrets/full prompts by default |
| C6.2 | Model Registry | `ModelDefinition` + PriceSource/PriceCheckedAt or PRICE_NOT_VERIFIED |
| C6.3 | Official Benchmark | ≥100 structured cases / 28 domains |
| C6.4 | Golden + Evaluator | Deterministic where possible; else HUMAN_REVIEW_REQUIRED |
| C6.5 | Knowledge vs Model | Scenarios A–G → `C6_5_KNOWLEDGE_VS_MODEL.md` |
| C6.6 | LocalModel provider | Generic Endpoint/ModelId/…; BLOCKED if no runtime |
| C6.7 | External adapter | OpenAI experimental; contract/mock/routing/token/cost without key |
| C6.8 | Cost engine | Independent; LOW/MEDIUM/HIGH/EXTREME → `C6_8_COST_MODEL.md` |
| C6.9 | Infra economics | GPU/CPU/RAM; THEORETICAL vs BENCHMARKED vs PRODUCTION_OBSERVED |
| C6.10 | Q/L/C matrix | Honest NOT_TESTED / HUMAN_REVIEW_REQUIRED |
| C6.11 | Intelligence Router | Deterministic→local→external→safe fallback; assistive only |
| C6.12 | Final Hardening | `C6_FINAL_DECISION.md` + `C6_CONTINUOUS_RUN_FINAL.md` |

Per phase: Implement → Debug+Release build → unit + phase tests → regression → security → main+protected SHA check → `Docs/c6/C6_N_*.md` → commit → push **cycle-c6 only** → next.

---

## 7. Decision options (deferred to C6.12 — do not pre-sell)

Candidates to evaluate with evidence only:

- `LOCAL_FIRST`
- `EXTERNAL_FIRST`
- `HYBRID`
- `KNOWLEDGE_FIRST_WITH_EXTERNAL_ESCALATION`
- `NOT_ENOUGH_EVIDENCE`

At C6.0: **NOT_TESTED** (no decision yet).

---

## 8. Push / branch policy

- Push **ONLY** `cycle-c6/primox-intelligence`
- **NEVER** push `main` / `origin/main`
- Untracked QA_EVIDENCE / TestData / patches from prior cycles: leave untracked unless a phase explicitly requires them

---

## 9. C6.0 conclusion

| Gate | Status |
|------|--------|
| Tip = 71e0088 (cycle-c5) | **PASS** |
| Ancestry verified | **PASS** |
| Branch created | **PASS** `cycle-c6/primox-intelligence` |
| Protected DB SHA | **PASS** unchanged |
| Keys / LIVE honesty | **PASS** (ABSENT → LIVE_NOT_TESTED) |
| Docs/c6 scaffold | **PASS** (this file) |
| Architecture decision | **NOT_TESTED** (deferred C6.12) |

**C6.0 overall:** **PASS** — ready for C6.1 Model Provider Contract.