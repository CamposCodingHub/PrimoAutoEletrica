# C6 FINAL DECISION — PRIMOX Intelligence Feasibility

**Machine:** CIRO (de411c5d-4243-4085-bf33-8c3d241d7f2f)  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Date:** 2026-09-28 07:07:30 -03:00  
**Branch:** `cycle-c6/primox-intelligence`  
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` **UNTOUCHED**  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged**  
**Keys:** ABSENT → EXTERNAL LIVE = **LIVE_NOT_TESTED**

## Formal decision

### `NOT_ENOUGH_EVIDENCE`

**Rationale (data, not vendor sales pitch):**
- GroundedLocalRule / knowledge paths are implemented and unit-exercised (**PASS**).
- Local model runtime on CIRO = **ENVIRONMENT_DEPENDENCY** (no verified local LLM runtime) — cannot claim LOCAL_FIRST quality/latency/cost.
- External OpenAI-compatible adapter exists (experimental) but keys ABSENT → **LIVE_NOT_TESTED** / **MOCK_ONLY** only — cannot claim EXTERNAL_FIRST or HYBRID quality.
- Cost engine + infra models correctly return **PRICE_NOT_VERIFIED** for default registry entries — no honest USD/BRL production cost ranking.
- Q/L/C matrix cells for model scenarios remain **NOT_TESTED** / **HUMAN_REVIEW_REQUIRED** where unmeasured.
- Therefore: **insufficient empirical evidence** to declare the best multi-year production intelligence architecture or any vendor winner.

## Provisional operating posture (not a vendor decision)

While evidence is incomplete, the **implemented router posture** matches:

**Knowledge / deterministic first → optional local model → optional external → safe fallback**  
(= engineering sketch of `KNOWLEDGE_FIRST_WITH_EXTERNAL_ESCALATION`)

This is an **assistive-only** prototype. It does **not** authorize EXTERNAL_FIRST, does **not** sell OpenAI, and does **not** promote unmeasured local GPUs.

## Evidences collected
| Area | Status |
|------|--------|
| C6.1 Provider benchmark contract | PASS |
| C6.2 Model registry (PRICE_NOT_VERIFIED default) | PASS |
| C6.3 Benchmark catalog 112 / 28 domains | PASS |
| C6.4 Evaluator + HUMAN_REVIEW_REQUIRED path | PASS |
| C6.5 A–G harness; D–G STUB/MOCK without runtime | PASS (honesty) |
| C6.6 LocalModel provider | PASS contract; ENVIRONMENT_DEPENDENCY live |
| C6.7 External adapter | PASS mock/contract; LIVE_NOT_TESTED |
| C6.8 Cost engine | PASS; prices PRICE_NOT_VERIFIED |
| C6.9 Infra economics | PASS; THEORETICAL / PRICE_NOT_VERIFIED |
| C6.10 Q/L/C matrix | PASS builder; scores NOT_TESTED where due |
| C6.11 Intelligence Router | PASS unit; assistive + unsafe-intent block |

## Tests not run / blocked
- LIVE external HTTP quality/latency/cost
- Local GPU/CPU model BENCHMARKED latency/quality
- Human review of full 112-case golden set
- PRODUCTION_OBSERVED infra costs
- Full 500-case corpus (path defined; not built)

## Risks
- Premature EXTERNAL_FIRST → cost/privacy/grounding risk without evidence
- Premature LOCAL_FIRST → infra Capex without BENCHMARKED quality
- Treating MOCK_ONLY as LIVE quality = honesty violation

## Dependencies for a future decisive gate
1. Secure key present (never paste in chat) + LIVE preflight suite
2. Local runtime installed and RuntimeAvailable=true with BENCHMARKED runs on ≥100 cases
3. Verified PriceSource + PriceCheckedAt for candidate models
4. Human review sample of non-deterministic cases
5. Expand benchmark toward 500 cases

## Next experiments
1. Arm LIVE external on isolated CIRO session; score subset of deterministic cases; record tokens/latency without storing prompts/secrets
2. Stand up one local OpenAI-compatible runtime; repeat A–G with BENCHMARKED labels
3. Operator-enter verified prices into ModelRegistry with PriceCheckedAt
4. Human-review 20 advanced cases; calibrate evaluator thresholds
5. Re-run C6.12 decision gate — only then consider upgrading from NOT_ENOUGH_EVIDENCE

## Explicit non-decisions
- No vendor lock-in
- No production AI go-live
- No autonomous buy/stock/finance/OS actions