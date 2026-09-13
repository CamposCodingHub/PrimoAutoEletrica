# PRIMOX NET10-05 — DATABASE

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**HEAD inicial:** `7a1243b`  
**AppData:** isolado sob `TestResults/Net10-Overnight/20260912/NET10-05-Database/`  
**NUNCA** usou banco real de produção/usuário.

## Objetivo

Validar create / migrations / integrity / FK / CRUD / transactions / backup / restore / reopen no runtime **net10**.

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE | TIMESTAMP |
|---|---:|---|---|---|
| Run-UiSmoke MainWindow (create DB) | 0 | APROVADO · `primoauto.db` criado | `…/create-db/` | 20:58:22 |
| Run-UiSmoke Clientes (CRUD) | 0 | APROVADO · 2 clientes | `…/crud-clientes/` | 20:58:45 |
| DbCheckNet10 `check` (CRUD DB) | 0 | integrity=**ok** · FK=1 · fk_viol=**0** · migrations=**28** · Clientes=2 | `integrity-before-backup.txt` | 21:00 |
| DbCheckNet10 `tx-rollback` | 0 | temp insert ok · after_rollback=**0** | `tx-rollback.txt` | 21:00 |
| File backup SHA256 | — | match=**True** | `backup-hash.txt` | 21:00 |
| DbCheck after restore | 0 | integrity=**ok** · Clientes=2 | `integrity-after-restore.txt` | 21:00 |
| MainWindow on seeded restored AppData | 0 | **REOPEN_SEEDED_EXIT=0** | `reopen-seeded-exit.txt` | 21:00 |
| Run-UiSmoke A13Database | 0 | **APROVADO** 1/1 | `…/A13Database/` | 20:59:44 |

## SIMULAÇÃO

criar banco isolado → CRUD Clientes → integrity → tx rollback → backup → restore → reopen MainWindow → A13Database: **PASS**.

## Compatibilidade cruzada

| Direção | Status |
|---|---|
| DB criado por net10 aberto por net10 | **PASS** |
| Schema migrations=28 (linhagem compartilhada com net6) | **PASS** |
| DB net10 → EXE net6 | **BLOCKED EXTERNAL** (SDK .NET 6 ausente neste host) |

## Proteção Git

main / v1.0.0 / primox-net6-final **intactos**.

## Decisão

**PASS WITH LIMITATIONS** (cross-runtime net6 EXE não executável neste ambiente). Prosseguir **NET10-06**.
