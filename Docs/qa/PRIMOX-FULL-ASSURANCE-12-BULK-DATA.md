# PRIMOX FULL ASSURANCE-12 — BULK DATA

## Generator

Smoke filter `BulkDataQa12` (`UiSmokeTestService.Assurance12.cs`), prefix `QA12_`.

## Verified volumes (retest after plate uniqueness fix)

| Entity | Target | Created | Status |
|--------|-------:|--------:|--------|
| Clientes | 500 | 500 | PASS |
| Veículos | 500 | 500 | PASS |
| Produtos | 1000 | 1000 | PASS |
| OS / Orçamentos / Financeiro / Agenda | — | NOT TESTED at full volume in this harness | deferred (relationships via existing QaEngine) |

Elapsed seed: ~5194 ms (workspace Release).  
Post-bulk: `PRAGMA integrity_check=ok`, `foreign_key_check=0`.

## Bug found & fixed

Duplicate plate generation via `GerarPlacaValida` on sequential seeds → collisions. Replaced with `GerarPlacaUnicaQa12(index)`.

Evidence: `TestResults/FullAssurance12/bulk-retest2/`.
