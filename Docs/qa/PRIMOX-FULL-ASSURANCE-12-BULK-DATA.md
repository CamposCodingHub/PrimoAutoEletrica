# PRIMOX FULL ASSURANCE-12 — BULK DATA

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

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
