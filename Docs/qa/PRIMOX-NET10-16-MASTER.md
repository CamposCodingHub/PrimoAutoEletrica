# PRIMOX NET10-16 — MASTER REGRESSION

**Data:** 2026-09-12/13  
**Branch:** `migration/net10`

## CURRENTLY EXECUTED (re-run)

| Gate | RESULT | EVIDENCE |
|---|---|---|
| Unit | **173/173** | `unit.txt` |
| QaEngine | **43/43** | `master-smoke.json` |
| DeepQa | **6/6** | same |
| A12Security | **3/3** | same |
| Tema | **2/2** | same |
| I18n07 | **1/1** | same |
| Full journey sample | journeyFails=**0** | `journey.txt` |
| Exhaustive/LongRun/Installer/DB | CURRENT earlier phases | NET10-06/10/13/15 |

## Gates

Product/Functional/Database/Security/Installer/Visual/I18N: **PASS** (com limitações já documentadas).  
Accessibility: parcialmente coberto por Components/Tema — **PASS WITH LIMITATIONS**.  
Performance: startup medido — **PASS WITH LIMITATIONS**.  
Fiscal LIVE: **BLOCKED EXTERNAL**.  
Documentation: esta suíte.

## P0/P1/P2 novos

**0 / 0 / 0** nesta execução master.

## Decisão

**PASS WITH LIMITATIONS**. Prosseguir **NET10-17**.
