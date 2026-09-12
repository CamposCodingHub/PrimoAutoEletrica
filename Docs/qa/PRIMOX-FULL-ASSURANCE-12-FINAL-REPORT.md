# PRIMOX FULL ASSURANCE-12 — FINAL REPORT

**Date:** 2026-09-11  
**HEAD INITIAL:** `175fac5`  
**HEAD FINAL:** `814ce26`  
**TAG v1.0.0:** `72d85fa` PRESERVED  

## Orchestrator primary run

`TestResults/FullAssurance12/20260911-195635/` (+ postfix retests)

| Gate | Status | Detail |
|------|--------|--------|
| Build Debug/Release | PASS | |
| Unit tests | PASS | 173/173 (incl. path/authz) |
| SecurityRedTeam script | PASS | YELLOW informational heuristics |
| PathTraversalSim | PASS | |
| A12Security | PASS | 3/3 |
| BulkDataQa12 | FAIL then **PASS** after plate fix | 500/500/1000 |
| QaEngine | PASS | 43/43 |
| DeepQa | PASS | 6/6 |
| LoginSessao | PASS | 1/1 |
| LongRun | PASS | 1/1 |
| ExhaustiveUi | PASS | discovered=3282 tested=1882 pass=1882 fail=0 |
| I18n07 / Tema / Sidebar / OvernightQa | PASS | |
| Clientes (orch) | FAIL then **PASS** after SaveChanges name fix | 3/3 |
| Recovery | PASS | 5 cycles |
| Database | PASS | integrity=ok fk=0 migrations=28 |
| Installer E2E | FAIL then **PASS** | fails=0 Final CRUD Exit=0 |
| Installed Clientes 10× | FAIL 9/10 then **PASS 10/10** | exit2=0 (per-cycle AppData) |
| Code signing | BLOCKED | EXTERNAL CERTIFICATE |

## Security decision

**YELLOW** — no Critical/High confirmed open after hardenings; residual Process.Start migration + unsigned package.

## Release decision

**YELLOW** — commercial package ready with unsigned installer; Assurance-12 security/functional gates green after fix loop.

## Commits (planned / created in A12)

See git log after commit phase.
