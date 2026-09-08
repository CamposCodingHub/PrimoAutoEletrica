# PRIMOX — Fiscal Foundation Test Matrix

**Phase:** Fiscal Foundation 1.0  
**Date:** 2026-09-08  
**Rule:** Never mark Focus API as PASS when not called.

| ID | Scenario | Expected | Result |
|----|----------|----------|--------|
| FF-01 | Production Guard default | `ProductionBlocked` / `FISCAL-PROD-BLOCKED` | PASS (unit) |
| FF-02 | Homologation ≠ Production | Homolog allowed by guard; Production denied | PASS (unit) |
| FF-03 | Config defaults | LiveHttp=false, ProductionUnlocked=false, Env=Homologation | PASS (unit) |
| FF-04 | Focus Emit with HTTP off | NotImplemented, **not** Authorized | PASS (unit) |
| FF-05 | Focus Emit Production | ProductionBlocked | PASS (unit) |
| FF-06 | Idempotent retry after timeout | Same OperationId; consult can resolve | PASS (unit) |
| FF-07 | FakeAuthorized / FakeRejected | Explicit statuses + provider codes | PASS (unit) |
| FF-08 | FakeNetwork + invalid cancel | NetworkError + ValidationError + user message | PASS (unit) |
| FF-09 | Migration tables | FiscalOperations/Documents/Events + id `202609080001` | PASS (unit + probe) |
| FF-10 | JSON enum round-trip | Status/Environment serialize with string converter | PASS (unit) |
| FF-11 | Isolated DB integrity | integrity=ok, fk_issues=0 | PASS (probe) |
| FF-12 | Build | 0 errors | PASS |
| FF-13 | QaEngine (+ CompleteUi) | 43/43 | PASS |
| FF-14 | DeepQa + Long Run 5 | 6/6 · LongRunNavegacaoTema PASS | PASS |
| FF-15 | Exhaustive Light/Dark × 4 res | 1909 PASS / 0 FAIL / 0 BLOCKED | PASS |
| FF-16 | Focus live API | Safe health/sandbox without fiscal write | **NOT EXECUTED** |
| FF-17 | Real NF emission | Forbidden this phase | **NOT EXECUTED** |
| FF-18 | Production credential use | Forbidden | **NOT EXECUTED** |

## Negative cases covered (unit / fake)

token absent (Focus NotConfigured path when HTTP forced on conceptually), URL missing, network error, timeout, fiscal rejection, invalid cancel, production blocked, HTTP off.

## Themes / resolutions (Exhaustive)

Rounds executed: Light/Dark × 1366×768, 1600×900, 1920×1080, 2560×1440 — all in `exhaustive-summary-latest.md`.
