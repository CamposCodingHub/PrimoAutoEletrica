# C7.1 AUDIT TRAIL

## START
- run_id c7-ciro-20260928-075130; Model A llama3.1:8b sanity

## ENVIRONMENT
- Ollama up; DB SHA C7420D18…CE0B; catalog DBAEEB07…8100; tip a8ef831…

## COMMANDS
- dotnet test filter C7_Run_Harness_From_Env with C7_SANITY_ONLY=1 C7_PROVIDERS=local-a C7_EXPS=EXP-A

## RESULTS
- ok=6 errors=0; raw C7.1_results.jsonl

## ERRORS
- None blocking

## ARTIFACTS
- docs/intelligence/c7/C7_LOCAL_MODEL_A.md
- data/intelligence/c7/raw/c7-ciro-20260928-075130_C7.1_*

## DECISION
PASS (runtime sanity). Quality HUMAN_REVIEW_REQUIRED.
