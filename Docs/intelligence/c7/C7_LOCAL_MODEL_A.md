# C7.1 LOCAL MODEL A — Inventory + Sanity

**run_id:** c7-ciro-20260928-075130  
**Phase:** C7.1  
**Machine:** CIRO  
**Model A:** `llama3.1:8b` (Ollama)  
**git_tip:** a8ef83137c6e65e196082066f32f046020ee4a1d  
**Protected DB SHA256:** C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B (verified)  
**Catalog SHA256:** DBAEEB07133D86003446ADD42086622DA4C17B6AC49267A0C92578971F0A8100  

## Environment
- Ollama runtime: proven listening on 127.0.0.1:11434 during C7.1
- Endpoint used: `http://127.0.0.1:11434/v1/chat/completions`
- Provider: `PRIMOX_LOCAL_MODEL` / provider_key `local-a`
- Execution mode: LocalModel
- External keys: ABSENT → not used

## Sanity design
- C7_SANITY_ONLY=1 → 6 Basic cases (first Basic per domain, Take 6)
- EXP condition: EXP-A (MODEL_ONLY) only
- Raw: `data/intelligence/c7/raw/c7-ciro-20260928-075130_C7.1_results.jsonl`
- Summary: `data/intelligence/c7/raw/c7-ciro-20260928-075130_C7.1_summary.json` → ok=6 errors=0

## Measured rows (harness status = non-empty model output; evaluator overall is separate)

| case_id | domain | harness_status | latency_ms | deterministic_score | evaluator_notes prefix |
|---------|--------|----------------|------------|---------------------|------------------------|
| BM-01-01 | ElectricalFundamentals | PASS | 85366.2 | 0.5 | HUMAN_REVIEW_REQUIRED |
| BM-02-01 | BatterySystems | PASS | 70734.5 | 0.5 | HUMAN_REVIEW_REQUIRED |
| BM-03-01 | ChargingAlternator | PASS | 39582.6 | 0.5 | FAIL |
| BM-04-01 | StarterStarting | PASS | 73141.3 | null | HUMAN_REVIEW_REQUIRED |
| BM-05-01 | WiringHarness | PASS | 63000.3 | null | HUMAN_REVIEW_REQUIRED |
| BM-06-01 | GroundingMass | PASS | 57634.8 | 0.5 | HUMAN_REVIEW_REQUIRED |

**Latency (measured):** min 39582.6 ms; max 85366.2 ms; mean ≈ 64910.0 ms (n=6).  
**Tokens / cost:** null / PRICE_NOT_VERIFIED (not instrumented in this harness path).  
**Human scores:** all null → HUMAN_REVIEW_REQUIRED (not invented).

## Honesty labels
- Runtime availability: **PASS** (6/6 non-empty responses)
- Quality / golden match: **NOT claimed PASS** — evaluator HUMAN_REVIEW_REQUIRED or FAIL on scored cases; human scores absent
- Full 112×EXP matrix for Model A: deferred to C7.3-local-a (see C7.3 docs) — do not treat sanity as full validation

## DECISION
**PASS (sanity / runtime inventory only).** Continue C7.2. No winner language.
