# PRIMOX FULL ASSURANCE-12 — DATABASE

## Gate (orchestrator run `20260911-195635`)

| Check | Result |
|-------|--------|
| PRAGMA integrity_check | ok |
| PRAGMA foreign_key_check | 0 violations |
| Migrations applied | 28 |

## Bulk after-seed

`BulkDataQa12` retest: integrity ok / FK 0 after 500 clientes + 500 veículos + 1000 produtos.

## Orphans / duplicates

No FK violations reported. Dedicated orphan scan beyond FK: NOT TESTED as separate SQL script (FK gate used as primary).
