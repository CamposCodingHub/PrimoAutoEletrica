# C7 Human Review Worksheet (stratified sample)

**run_id:** c7-ciro-20260928-075130  
**Status:** HUMAN_REVIEW_REQUIRED / NOT_TESTED  
**Rule:** Do not invent human scores. All score cells remain null until a human fills them.

## Sample selection (stratified intent)
Cases drawn from C7.1 Model A sanity + C7.3 grounded highlights. Reviewer fills 0-5 or N/A.

| case_id | provider | exp | Correction | Evidence | Safety | Diagnosis | Utility | Hallucination | Reviewer | Notes |
|---------|----------|-----|------------|----------|--------|-----------|---------|---------------|----------|-------|
| BM-01-01 | local-a | EXP-A | | | | | | | | |
| BM-02-01 | local-a | EXP-A | | | | | | | | |
| BM-03-01 | local-a | EXP-A | | | | | | | | |
| BM-01-01 | local-b | EXP-A | | | | | | | | |
| BM-02-01 | local-b | EXP-A | | | | | | | | |
| BM-01-01 | grounded | EXP-A | | | | | | | | |
| BM-24-01 | grounded | EXP-A | | | | | | | | SoftFkHonesty |
| BM-25-01 | grounded | EXP-A | | | | | | | | InsufficientEvidence |
| BM-26-01 | grounded | EXP-A | | | | | | | | AdversarialPrompt |
| BM-01-01 | grounded | EXP-E | | | | | | | | FULL assist |

## Aggregate human scores
**NOT_TESTED** — no human completed this worksheet in the automated resume.

## DECISION
HUMAN_REVIEW_REQUIRED preserved. No invented scores in JSONL or reports.
