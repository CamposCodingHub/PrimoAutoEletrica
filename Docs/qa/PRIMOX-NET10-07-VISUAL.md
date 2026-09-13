# PRIMOX NET10-07 — VISUAL / WPF

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**HEAD:** `c5411a1`

## CURRENTLY EXECUTED

| Filter | EXIT | RESULT | Pass/Total | EVIDENCE |
|---|---:|---|---:|---|
| Tema (Light/Dark) | 0 | APROVADO | 2/2 | `NET10-07-Visual/Tema/` |
| I18n07 (PT/EN/ES gate) | 0 | APROVADO | 1/1 | `…/I18n07/` |
| I18n06 | 0 | APROVADO | 1/1 | `…/I18n06/` |
| Sidebar | 0 | APROVADO | 1/1 | `…/Sidebar/` |
| CommandCenter | 0 | APROVADO | 1/1 | `…/CommandCenter/` |
| Components | 0 | APROVADO | 1/1 | `…/Components/` |
| Calendar/DatePicker | 0 | APROVADO | 4/4 | `…/Calendar/` |
| OvernightQa (nav/lang/dialog) | 0 | APROVADO | 3/3 | `…/OvernightQa/` |
| ExhaustiveUi (NET10-06) | 0 | APROVADO | FullSimulation | covers multi-resolution harness |

## SIMULAÇÃO

Abrir/navegar módulos em Light+Dark e PT+EN+ES via Tema + I18n07 + OvernightQa: **PASS**.

## Limitações

- Comparação pixel-perfect .NET 6 × .NET 10 screenshots: **NOT EXECUTED** como suite de imagens dedicada (não exigido pixel-perfect; Exhaustive/Overnight cobrem layout funcional).
- Calendar Dark: limitação WPF conhecida (baseline).

## Decisão

**PASS WITH LIMITATIONS** (sem gallery screenshot baseline diff). Prosseguir **NET10-08**.
