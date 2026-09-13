# PRIMOX NET10 — FINAL REPORT (OVERNIGHT PROGRAM)

**Generated:** 2026-09-13  
**Branch:** `migration/net10`

## Git / TFM

| Item | Valor |
|---|---|
| HEAD inicial (overnight tip pré-01) | `9b8bc7d` (NET10-00 tip) → trabalho 01+ desde `93611a4` |
| HEAD final (pré este relatório commit) | ver commit deste arquivo |
| main | `29b19b1` **intacta** |
| v1.0.0 | `72d85fa` → `a4ad6fe` **intacta** |
| primox-net6-final | `63aeb05` → `29b19b1` **intacta** |
| TFM inicial | `net6.0-windows` |
| TFM final | `net10.0-windows` |

## Fases

| Fase | STATUS | COMMIT (aprox.) | TESTES / SIMULAÇÃO | LIMITAÇÕES |
|---|---|---|---|---|
| NET10-00 | PASS | `7ad65e9`… | baseline git/TFM | — |
| NET10-01 | PASS | `6784d67` | compat probe | — |
| NET10-02 | PASS | `93611a4` | SDK 10 env build net6 | SDK 6 ausente |
| NET10-03 | PASS w/ lim | `dce6d27` | TFM migrate · Unit · QaEngine | NU1701 |
| NET10-04 | PASS | `7a1243b` | PCLRaw 3.0.5 · Login/Dashboard | NU1701 |
| NET10-05 | PASS w/ lim | `f52224e` | DB isolado integrity/backup | net6 EXE cross BLOCKED |
| NET10-06 | PASS | `c5411a1` | Full functional + journey | harness tab fix |
| NET10-07 | PASS w/ lim | `fa927f8` | Tema/I18n/Overnight | screenshot gallery N/E |
| NET10-08 | PASS w/ lim | `509f54a` | A12/A13 + RedTeam YELLOW | REVIEW estático herdado |
| NET10-09 | PASS w/ lim | `3d9d334` | startup 10× / nav 20× | counters limitados |
| NET10-10 | PASS w/ lim | `e006c35` | BulkDataQa13 | PS Add-Type blocked |
| NET10-11 | PASS w/ lim | `7684279` | Fiscal unit 46/46 | LIVE EXTERNAL |
| NET10-12 | PASS | `881b0a5` | publish + smoke EXE | — |
| NET10-13 | PASS w/ lim | `20c87c4` | installer 3 ciclos | unsigned |
| NET10-14 | PASS w/ lim | (este lote) | AppData dual isolation | net6 EXE BLOCKED |
| NET10-15 | PASS | (este lote) | 50 ciclos + recovery | — |
| NET10-16 | PASS w/ lim | (este lote) | master re-run | Exhaustive não re-rodado nesta fase (PASS em 06) |
| NET10-17 | READY WITH LIMITATIONS | (este lote) | comparison | — |
| NET10-18 | SKIPPED | — | RC não criado | regra READY pura |
| NET10-19 | **APPROVED WITH LIMITATIONS** | (este lote) | decision | sem promoção |

## Resumo defeitos novos

| Severidade | Count |
|---|---:|
| P0 | 0 |
| P1 | 0 |
| P2 | 0 |
| P3 | 0 (avisos NuGet conhecidos) |

## Áreas

Security · Database · Functional · Performance · Installer · I18N · Accessibility · Fiscal · Publish — ver fases acima.

## Rollback

main / v1.0.0 / primox-net6-final **não alterados**. Histórico permanece em `migration/net10`.
