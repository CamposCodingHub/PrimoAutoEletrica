# C7.3 FULL MATRIX — GroundedLocalRule (COMPLETE) + Local generative (PARTIAL)

**run_id:** c7-ciro-20260928-075130  
**git_tip (at run):** a8ef83137c6e65e196082066f32f046020ee4a1d  
**Protected DB SHA256:** C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B  
**Catalog SHA256:** DBAEEB07133D86003446ADD42086622DA4C17B6AC49267A0C92578971F0A8100  
**Cases:** 112 frozen  

## A) Provider grounded — COMPLETE

| Metric | Value |
|--------|-------|
| Raw | `data/intelligence/c7/raw/c7-ciro-20260928-075130_C7.3_results.jsonl` |
| Summary | ok=560 errors=0 |
| Matrix | 112 × EXP-A..E = **560** |
| harness status PASS | 560/560 |
| evaluator FAIL / HUMAN_REVIEW_REQUIRED / clean PASS | 211 / 349 / **0** |
| deterministic_score present | 44 cases paired |
| tokens / cost | null / PRICE_NOT_VERIFIED |

### Observed deterministic_score deltas vs EXP-A (n=44)
| Contrast | mean Δ | improved | same | worse |
|----------|--------:|---------:|-----:|------:|
| EXP-B − A | −0.0824 | 15 | 5 | 24 |
| EXP-C − A | −0.0114 | 0 | 42 | 2 |
| EXP-D − A | −0.2188 | 0 | 14 | 30 |
| EXP-E − A | −0.0824 | 15 | 5 | 24 |

## B) Local generative — PARTIAL

| Provider | Sanity | Full 112×EXP-A..E |
|----------|--------|-------------------|
| local-a `llama3.1:8b` | C7.1 6/6 runtime | **PARTIAL 13/560** — `..._C7.3-local-a_results.jsonl` (EXP-A BM-01-01..BM-04-01 prefix; stopped 2026-09-28 12:03:52 -03 to free GPU for C7.11). latency_ms measured n=13: min 5370.9 max 77328.1 mean ≈51413 |
| local-b `qwen2.5-coder:7b` | C7.2c 6/6 runtime | **NOT_TESTED** (0/560) |

## DECISION
Grounded COMPLETE. Generative PARTIAL/NOT_TESTED — exact counts only; no invented rows.
