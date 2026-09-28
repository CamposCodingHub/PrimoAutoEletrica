# C7_FINAL_REPORT — PRIMOX Intelligence Experimental Validation (C7.0–C7.12)

**Machine:** CIRO  
**Timezone:** America/Sao_Paulo (UTC-3)  
**run_id:** c7-ciro-20260928-075130  
**Branch:** `cycle-c7/primox-intelligence`  
**Parent C6 FINAL (FACT):** `4e28e5211a9dab3b049768d635d1460bb5afa518`  
**Master-script baseline discrepancy (FACT):** `71e0088` is C5 tip / C6.0 parent — **not** C7 parent  
**origin/main (READ ONLY):** `bf1eb784a3ed45782487197f38d9ba15d319997e` — never touched  
**Tip at experiment freeze:** `a8ef83137c6e65e196082066f32f046020ee4a1d`  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` — verified OK through resume  
**Catalog SHA256:** `DBAEEB07133D86003446ADD42086622DA4C17B6AC49267A0C92578971F0A8100` (112 cases)  

## Decision Gate
**REQUIRES_MORE_DATA** — see `C7_DECISION_GATE.md`. No winner. No C8 in this report.

## Phase status matrix

| Phase | Status | Artifact |
|-------|--------|----------|
| C7.0 | PASS | `C7_BASELINE.md`, `C7_PROTOCOL.md` |
| C7.1 Local A sanity | PASS (runtime) | `C7_LOCAL_MODEL_A.md`; raw C7.1 6/6 |
| C7.2 Local B sanity | PASS (runtime via C7.2c) | `C7_LOCAL_MODEL_B.md`; prior partial C7.2/C7.2b kept |
| C7.3 grounded 112×A–E | COMPLETE 560/560 | `C7_3_FULL_MATRIX.md` |
| C7.3 local-a generative | PARTIAL 13/560 | `..._C7.3-local-a_results.jsonl` |
| C7.3 local-b generative | NOT_TESTED 0/560 | — |
| C7.4–5 External LIVE | LIVE_NOT_TESTED | `C7_EXTERNAL.md` |
| C7.6 Human review | HUMAN_REVIEW_REQUIRED / NOT_TESTED | worksheet + `C7_6_HUMAN_REVIEW.md` |
| C7.7 Knowledge delta | documented (grounded n=44) | `C7_7_KNOWLEDGE_DELTA.md` |
| C7.8 Context delta | documented (grounded n=44) | `C7_8_CONTEXT_DELTA.md` |
| C7.9 Evidence delta | documented (grounded n=44) | `C7_9_EVIDENCE_DELTA.md` |
| C7.10 Latency/cost | measured latency; cost PRICE_NOT_VERIFIED | `C7_10_LATENCY_COST.md` |
| C7.11 Router | 4/4 probes | `C7_11_ROUTER.md` |
| C7.12 Gate | REQUIRES_MORE_DATA | this + `C7_DECISION_GATE.md` |

## Key measured facts (no invention)
- Grounded evaluator: FAIL 211, HUMAN_REVIEW_REQUIRED 349, clean PASS 0
- Mean det-score deltas (grounded scored): B−A −0.0824; C−A −0.0114; D−A −0.2188; E−A −0.0824
- Local A sanity latency mean ≈64910 ms (n=6); Local B C7.2c mean ≈53125 ms (n=6)
- Local-a PARTIAL latency mean ≈51413 ms (n=13)
- Tokens/cost: null; PRICE_NOT_VERIFIED
- Keys ABSENT → LIVE_NOT_TESTED
- Soft FKs: RELATIONSHIP_NOT_PROVEN when unproven; Assist fail-closed; no autonomous buy/pay/fiscal/stock/OS/client actions in this validation

## Hardware (FACT)
Xeon E5-2697A v4 16c/32t; ~32GB RAM; RX 580 — slow local inference (drives PARTIAL generative).

## Blockers remaining
1. Full generative matrices (local-a remainder + local-b)
2. Human scores
3. External LIVE keys (or accepted permanent LIVE_NOT_TESTED)
4. PRICE_VERIFIED cost path if required

## Push policy
Commit/push **only** `cycle-c7/primox-intelligence`. Never main.
