# PRIMOX NET10-14 — SIDE-BY-SIDE

**Data:** 2026-09-12  
**Branch:** `migration/net10`

## CURRENTLY EXECUTED

| FEATURE | NET6 | NET10 | RESULT |
|---|---|---|---|
| Isolated AppData A vs B (two EXEs) | N/A (EXE ausente) | bin Release + publish preview | **PASS** exit 0/0 · DBs independentes (hashes distintos) |
| Commercial Setup 1.0.0 hash | intact | — | **PASS** `9A08494D…` |
| Preview Setup 1.1.0-net10 | — | gerado | **PASS** (AppId/path isoláveis via PackagingE2E) |
| Abrir net6 + net10 juntos | **BLOCKED EXTERNAL** | — | SDK/EXE net6 ausente neste host |

Evidence: `TestResults/.../NET10-14-SideBySide/side-by-side.txt`

## SIMULAÇÃO

Duas instâncias net10 com AppData distintos sem destruir uma à outra: **PASS**.

## Decisão

**PASS WITH LIMITATIONS**. Prosseguir **NET10-15**.
