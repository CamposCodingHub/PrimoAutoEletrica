# C7.7 Knowledge vs Model — observed deltas

**run_id:** c7-ciro-20260928-075130  
**Source:** grounded C7.3 JSONL only (112×EXP). Generative local full-matrix deltas: NOT_TESTED / PARTIAL until rows exist.

## Method
Paired `deterministic_score` where both EXP-A and EXP-B present and non-null (DeterministicEvaluable subset).

## Measured (grounded, n=44 scored pairs)
| Metric | Value |
|--------|-------|
| mean (EXP-B − EXP-A) | −0.0824 |
| improved (Δ>0) | 15 |
| same (Δ=0) | 5 |
| worse (Δ<0) | 24 |

## Evidence categories (all 560 grounded rows)
SUPPORTED 30 | PARTIALLY_SUPPORTED 330 | UNSUPPORTED 58 | INSUFFICIENT_EVIDENCE 142

## Labels
- Observed delta: **documented**
- Causal claim / production routing change: **NOT justified by this alone**
- Soft FK / relationship claims: RELATIONSHIP_NOT_PROVEN where applicable in domain SoftFkHonesty (see category counts; no invented FK proofs)

## DECISION
Knowledge-assist injection did **not** improve mean deterministic_score on the scored grounded subset. Continue honesty: no winner.
