# C7.2 LOCAL MODEL B — Inventory + Sanity

**run_id:** c7-ciro-20260928-075130  
**Phase:** C7.2 / C7.2c (resume completion)  
**Machine:** CIRO  
**Model B:** `qwen2.5-coder:7b` (Ollama)  
**git_tip:** a8ef83137c6e65e196082066f32f046020ee4a1d  
**Protected DB SHA256:** C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B (verified)  
**Catalog SHA256:** DBAEEB07133D86003446ADD42086622DA4C17B6AC49267A0C92578971F0A8100  

## Incomplete trail (preserved)
- `..._C7.2_results.jsonl` — 3/6 cases (BM-01-01, BM-02-01, BM-03-01) before mid-run drop
- `..._C7.2b_results.jsonl` — 1 retry BM-01-01
- Completion file: `..._C7.2c_results.jsonl` — full 6/6 sanity (kept prior raw)

## Measured C7.2c rows

| case_id | domain | harness_status | latency_ms | deterministic_score | evaluator_notes prefix |
|---------|--------|----------------|------------|---------------------|------------------------|
| BM-01-01 | ElectricalFundamentals | PASS | 82347.1 | 0.5 | HUMAN_REVIEW_REQUIRED |
| BM-02-01 | BatterySystems | PASS | 10459.2 | 0.5 | FAIL |
| BM-03-01 | ChargingAlternator | PASS | 15325.6 | 0.5 | FAIL |
| BM-04-01 | StarterStarting | PASS | 69501.0 | null | HUMAN_REVIEW_REQUIRED |
| BM-05-01 | WiringHarness | PASS | 70555.7 | null | HUMAN_REVIEW_REQUIRED |
| BM-06-01 | GroundingMass | PASS | 70563.8 | 0.5 | HUMAN_REVIEW_REQUIRED |

**Summary C7.2c:** ok=6 errors=0  
**Latency (C7.2c measured):** min 10459.2 ms; max 82347.1 ms; mean ≈ 53125.4 ms (n=6).  
**Tokens / cost:** null / PRICE_NOT_VERIFIED  
**Human scores:** all null

## Honesty labels
- Runtime availability: **PASS** (6/6 non-empty)
- Quality: **NOT claimed PASS** — evaluator FAIL or HUMAN_REVIEW_REQUIRED; no human scores
- Full 112 generative matrix for Model B: **NOT_TESTED** in this resume (slow RX 580 inference; local-a matrix prioritized in C7.3 background)

## DECISION
**PASS (sanity / runtime inventory only).** Prior C7.2 incomplete trail retained. No winner language.
