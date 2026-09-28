# C7.0 BASELINE — PRIMOX Intelligence Experimental Validation Kickoff

**Machine:** CIRO (`de411c5d-4243-4085-bf33-8c3d241d7f2f`)  
**Timezone:** America/Sao_Paulo (UTC-3)  
**START (C7.0):** 2026-09-28 07:50:24 -03  
**Branch:** `cycle-c7/primox-intelligence`  
**Created FROM (actual C6 FINAL tip):** `4e28e5211a9dab3b049768d635d1460bb5afa518` (`C6.12: set continuous run final tip hashes`, 2026-09-28 07:27:34 -03)  
**Ancestry:** `merge-base --is-ancestor 4e28e52 HEAD` → exit 0  
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` **READ ONLY — NEVER push main**  
**Protected DB path:** `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto_pristine_official.db` (also matches `primoauto.db` at C7.0)  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **ReadOnly — rehash every phase**  
**Operational DB:** `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto_operacional.db` PRESENT (SHA differs; not the protected gate)  
**run_id:** c7-ciro-20260928-075130  
**Keys:** `PRIMOX_EXTERNAL_AI_API_KEY` / `ENABLED` / `KILL_SWITCH` / `OPENAI_API_KEY` / `ANTHROPIC_API_KEY` / `AZURE_OPENAI_API_KEY` = **ABSENT** → EXTERNAL LIVE = **LIVE_NOT_TESTED**

**Goal:** empirical experimental validation C7.0→C7.12 of PRIMOX intelligence stack on frozen 112-case catalog.  
**Not goals:** production architecture decision; marketing; declaring winner/best model/provider; inventing latency/tokens/cost/RAM/CPU/GPU/prices/human evals.

**Allowed statuses:** NOT_TESTED | PASS | FAIL | BLOCKED | EXTERNAL_DEPENDENCY | MOCK_ONLY | STUB_ONLY | OBSERVATION_ONLY | PASS_WITH_EXTERNAL_DEPENDENCY | LIVE_NOT_TESTED | HUMAN_REVIEW_REQUIRED | PRICE_NOT_VERIFIED | ENVIRONMENT_DEPENDENCY | INSUFFICIENT_EVIDENCE | REQUIRES_MORE_DATA | EXPERIMENT_BLOCKED | EVIDENCE_SUFFICIENT_FOR_NEXT_STEP | READY_FOR_ARCHITECTURAL_REVIEW | NOT_ENOUGH_EVIDENCE

---

## FACT — Critical baseline discrepancy (master script vs actual C6 tip)

| Claim | Value | Classification |
|-------|-------|----------------|
| Master script baseline SHA | `71e0088` (`71e008810cdaf29f47eae76b63b478ee72d763c1`) | **FACT** — that SHA is **C5 tip / C6.0 parent** (`C5.12: set continuous run final tip hashes`, 2026-09-28 06:47:37 -03). Documented in `Docs/c6/C6_0_BASELINE.md` as C6 create-from tip. |
| Actual C6 FINAL tip | `4e28e5211a9dab3b049768d635d1460bb5afa518` on `cycle-c6/primox-intelligence` | **FACT** — `C6.12: set continuous run final tip hashes` (2026-09-28 07:27:34 -03). Confirmed via `git log -1` and prior C6 continuous-run artifacts under `Docs/c6/`. |
| Commits between master baseline and C6 final | **14** (`git rev-list --count 71e0088..4e28e52`) | **FACT** — C6.0→C6.12 work sits between them. |
| C7 branch parent chosen | `4e28e52` (**actual C6 FINAL tip**) | **FACT / DECISION for this cycle** — C7 must start from completed C6, not from C6.0 parent. |
| If C7 had used `71e0088` | Would omit C6.1–C6.12 artifacts (provider contract, registry, 112 catalog, evaluator, knowledge-vs-model, local/external adapters, cost engine, infra economics, QLC matrix, router, hardening) | **INTERPRETATION** of discrepancy impact |

**Correction applied:** `cycle-c7/primox-intelligence` created with `git checkout -B cycle-c7/primox-intelligence 4e28e5211a9dab3b049768d635d1460bb5afa518`. Master-script `71e0088` retained only as historical C6.0-create reference, **not** as C7 parent.

---

## FACT — Tip / main / protected integrity (C7.0 checkpoint)

| Check | Result |
|-------|--------|
| HEAD | `4e28e5211a9dab3b049768d635d1460bb5afa518` |
| Branch | `cycle-c7/primox-intelligence` |
| `origin/main` | `bf1eb784a3ed45782487197f38d9ba15d319997e` — untouched this phase |
| Protected DB rehash | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` — **matches** protected gate |
| Push policy | Push **only** `cycle-c7/*`. **NEVER** `main`. |

---

## FACT — C6 carry-forward artifacts confirmed present

| Artifact | Path / evidence | Status |
|----------|-----------------|--------|
| Docs C6 suite | `Docs/c6/C6_0_BASELINE.md` … `C6_12_FINAL_HARDENING.md`, `C6_CONTINUOUS_RUN_FINAL.md`, `C6_FINAL_DECISION.md` | PRESENT |
| Benchmark catalog 112 | `PrimoAutoEletrica/Services/Intelligence/PrimoxBenchmarkCatalog.cs` — `BuildAll`: 28 domains × 4 variants | PRESENT |
| Catalog file SHA256 | `DBAEEB07133D86003446ADD42086622DA4C17B6AC49267A0C92578971F0A8100` | FROZEN for C7 dataset version |
| Knowledge services | `Services/Knowledge/*`, `KnowledgeVsModelRunner.cs` | PRESENT |
| Deterministic / grounded rules | `GroundedLocalRuleAssistantProvider.cs`, deterministic evaluable flags on catalog | PRESENT |
| Intelligence Router | `Services/Intelligence/IntelligenceRouter.cs` | PRESENT |
| Cost Engine | `Services/Intelligence/IntelligenceCostEngine.cs` | PRESENT |
| Model Registry | `Services/Intelligence/ModelRegistry.cs` | PRESENT |
| Evaluator | `Services/Intelligence/BenchmarkEvaluator.cs` | PRESENT |
| Local model provider | `LocalModelAssistantProvider.cs` — RuntimeAvailable default false → ENVIRONMENT_DEPENDENCY when no runtime | PRESENT |
| External adapter | `ExternalModelAssistantProvider.cs` / ExternalAi — keys ABSENT → LIVE_NOT_TESTED | PRESENT |

---

## MEASUREMENT — Environment snapshot (C7.0, no API/model install yet)

| Item | Value | Notes |
|------|-------|-------|
| CPU | Intel Xeon E5-2697A v4 @ 2.60GHz | 16 cores / 32 logical |
| RAM | 31.84 GB | WMI TotalPhysicalMemory |
| GPU | AMD Radeon RX 580 2048SP | AdapterRAM reported ~4.0e9 bytes |
| OS | Microsoft Windows 11 Pro | |
| dotnet | net10 Desktop/AspNetCore 10.0.10/10.0.12; net9 also present | |
| ollama CLI | 0.33.3 present | daemon **not** listening on 127.0.0.1:11434 at C7.0 |
| ollama models manifests | none listed under `.ollama/models/manifests` at C7.0 probe | generative local = ENVIRONMENT_DEPENDENCY unless later phase proves otherwise |
| External API keys | ABSENT | LIVE_NOT_TESTED |

---

## LIMITATION

- C7.0 does **not** install models or call APIs (protocol first-checkpoint rule).
- Generative local / external results deferred; honest statuses only.
- Human evals not invented.

## C7.0 DECISION

**PASS** — branch correct parent (`4e28e52`), discrepancy documented, protected SHA verified, C6 artifacts confirmed, protocol freeze next (`C7_PROTOCOL.md`).

**Alias note:** master prefers `docs/intelligence/c7/`; project also has `Docs/c7/` (same volume on Windows case-insensitive FS). Master path is authoritative for C7 reports; `Docs/c7/` may hold short pointers if needed.
