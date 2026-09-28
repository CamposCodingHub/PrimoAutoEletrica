# C7_PROTOCOL — Experimental Validation Protocol (frozen)

**run_id:** c7-ciro-20260928-075130  
**Branch:** cycle-c7/primox-intelligence @ parent `4e28e5211a9dab3b049768d635d1460bb5afa518`  
**Protected DB SHA256 (must rehash each phase):** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`  
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` READ ONLY  
**START freeze:** 2026-09-28 07:51:30 -03

---

## 1. FACT — Frozen dataset (exactly 112 cases)

| Field | Value |
|-------|-------|
| Source | `PrimoAutoEletrica/Services/Intelligence/PrimoxBenchmarkCatalog.cs` |
| Construction | `BuildAll`: `Domains.Length` (28) × 4 variants = **112** |
| CaseId pattern | `BM-{domainIndex+1:D2}-{variant+1:D2}` → BM-01-01 … BM-28-04 |
| File SHA256 | `DBAEEB07133D86003446ADD42086622DA4C17B6AC49267A0C92578971F0A8100` |
| Dataset version id | `primox-benchmark-c6.3-frozen-c7.0` |
| Mutation policy | **FORBIDDEN** during C7. Re-hash catalog file each phase; if hash changes → EXPERIMENT_BLOCKED |

Domains (28): ElectricalFundamentals, BatterySystems, ChargingAlternator, StarterStarting, WiringHarness, GroundingMass, SensorsActuators, CANNetwork, Lighting, Ignition, FuelInjectionElectrical, HVACElectrical, SafetyRestraint, BodyElectronics, HybridEVBasics, DiagnosticProcedure, MeasurementInterpretation, DTCAnalysis, ParasiticDrain, PowerDistribution, RelaysFuses, AftermarketIntegration, WorkshopSafety, SoftFkHonesty, InsufficientEvidence, AdversarialPrompt, PartsIdentification, WorkOrderContext.

---

## 2. Experiment conditions EXP-A … EXP-E

| ExpId | Name | Model generative | Knowledge retrieval | Work-order / session context | Evidence / grounding payload | Notes |
|-------|------|------------------|---------------------|------------------------------|------------------------------|-------|
| EXP-A | MODEL_ONLY | ON (if runtime) | OFF | OFF | OFF | Pure model/rules response to question text only |
| EXP-B | +KNOWLEDGE | ON | ON | OFF | OFF | Inject retrieved Knowledge hits (or deterministic index) |
| EXP-C | +CONTEXT | ON | OFF | ON | OFF | Inject WO/session context fields when present in case metadata; else empty context recorded |
| EXP-D | +EVIDENCE | ON | OFF | OFF | ON | Inject RequiredEvidence / grounding stubs from case |
| EXP-E | FULL | ON | ON | ON | ON | All assists enabled |

**Applicable providers per honesty rules:**

| Provider class | When runnable | If unavailable |
|----------------|---------------|----------------|
| GroundedLocalRule / deterministic catalog golden path | Always (local, no key) | N/A |
| Local generative model A/B | Only if runtime+model proven | ENVIRONMENT_DEPENDENCY / NOT_TESTED — **do not simulate answers** |
| External LIVE | Only if secure key present and kill-switch not blocking | LIVE_NOT_TESTED |

Harness may still execute EXP-A…E for **deterministic / GroundedLocalRule** paths to validate schema and deltas that do not require generative models.

---

## 3. Result schema (individual results — not averages only)

Each trial writes one JSON object (and optional JSONL append). Unmeasured fields = `null`. Never invent numbers.

```json
{
  "schema_version": "c7.0.1",
  "experiment_id": "string — UNIQUE per trial",
  "run_id": "string",
  "phase": "C7.x",
  "exp_condition": "EXP-A|EXP-B|EXP-C|EXP-D|EXP-E",
  "case_id": "BM-XX-YY",
  "domain": "string",
  "difficulty": "Basic|Intermediate|Advanced|Adversarial",
  "provider_id": "string",
  "model_id": "string|null",
  "model_version": "string|null",
  "execution_mode": "GroundedLocalRule|LocalModel|ExternalModel|DeterministicGolden|Router",
  "status": "PASS|FAIL|NOT_TESTED|LIVE_NOT_TESTED|ENVIRONMENT_DEPENDENCY|HUMAN_REVIEW_REQUIRED|ERROR|SKIPPED",
  "started_at": "ISO-8601 with offset",
  "ended_at": "ISO-8601 with offset|null",
  "latency_ms": null,
  "prompt_tokens": null,
  "completion_tokens": null,
  "total_tokens": null,
  "cost_usd": null,
  "price_status": "PRICE_NOT_VERIFIED|PRICE_VERIFIED|null",
  "cpu_percent": null,
  "ram_mb": null,
  "gpu_util_percent": null,
  "knowledge_injected": false,
  "context_injected": false,
  "evidence_injected": false,
  "input_question": "string",
  "output_text": "string|null",
  "deterministic_score": null,
  "evaluator_notes": "string|null",
  "error_code": "string|null",
  "error_message": "string|null",
  "local_error": null,
  "external_success": null,
  "observed_delta_ref_experiment_id": "string|null",
  "evidence_category": "SUPPORTED|PARTIALLY_SUPPORTED|UNSUPPORTED|CONTRADICTED|INSUFFICIENT_EVIDENCE|null",
  "human_scores": {
    "Correction": null,
    "Evidence": null,
    "Safety": null,
    "Diagnosis": null,
    "Utility": null,
    "Hallucination": null
  },
  "git_tip": "string",
  "catalog_sha256": "DBAEEB07133D86003446ADD42086622DA4C17B6AC49267A0C92578971F0A8100",
  "protected_db_sha256": "rehashed value",
  "secrets_present": false
}
```

**experiment_id uniqueness:** `{run_id}|{phase}|{exp_condition}|{provider_id}|{case_id}|{attempt}`  
Example: `c7-ciro-20260928-075130|C7.3|EXP-A|primox-grounded-local-rules|BM-01-01|1`

---

## 4. Phase plan (continuous after C7.0 checkpoint)

| Phase | Deliverable | Honesty gate |
|-------|-------------|--------------|
| C7.0 | Baseline + Protocol (this) | No API/model install first |
| C7.1 | Local model A — inventory + sanity 6 cases → `C7_LOCAL_MODEL_A.md` | No runtime → ENVIRONMENT_DEPENDENCY |
| C7.2 | Local model B or NOT_TESTED + reason → `C7_LOCAL_MODEL_B.md` | Verifiable reason required |
| C7.3 | 112 × applicable EXP for each available local provider; individual results under `data/intelligence/c7/` | Preserve individuals |
| C7.4 | External LIVE preflight → `C7_EXTERNAL.md` | Keys absent → LIVE_NOT_TESTED |
| C7.5 | External 112 if LIVE else skip | |
| C7.6 | Human review stratified sample; worksheets if human unavailable | Do not invent scores |
| C7.7 | Knowledge vs Model observed_delta only | Categories SUPPORTED… |
| C7.8 | Context vs no-context observed_delta | |
| C7.9 | Evidence/grounding observed_delta | |
| C7.10 | Latency+cost; Cost Engine only verified prices | PRICE_NOT_VERIFIED default |
| C7.11 | Router behavior; never hide LOCAL ERROR when EXTERNAL SUCCESS | Record both |
| C7.12 | Decision Gate Q1–13 + `C7_DECISION_GATE.md` + `C7_FINAL_REPORT.md` | NO WINNER language |

---

## 5. Audit trail (per phase)

Each phase appends `docs/intelligence/c7/audit/C7_x_AUDIT.md` with sections:  
START / ENVIRONMENT / COMMANDS / INPUTS / RESULTS / ERRORS / ARTIFACTS / DECISION  

New `run_id` if re-run. Checkpoint commit between phases when practical.

---

## 6. Decision Gate allowed outcomes (C7.12)

- EVIDENCE_SUFFICIENT_FOR_NEXT_STEP  
- NOT_ENOUGH_EVIDENCE  
- EXPERIMENT_BLOCKED  
- REQUIRES_MORE_DATA  
- READY_FOR_ARCHITECTURAL_REVIEW  

**Forbidden:** winner / best model / best provider / production architecture decision / marketing claims.

---

## 7. Paths

| Kind | Path |
|------|------|
| Reports (master) | `docs/intelligence/c7/` |
| Raw structured | `data/intelligence/c7/` (`raw/`, `worksheets/`) |
| Alias pointer | `Docs/c7/README.md` |

---

## 8. C7.0 PROTOCOL DECISION

**PASS** — dataset frozen at 112; catalog SHA recorded; EXP-A…E defined; result schema with null unmeasured fields; experiment_id uniqueness rule set; honesty gates explicit.
