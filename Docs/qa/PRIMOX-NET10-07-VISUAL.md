# PRIMOX NET10-07 — VISUAL / WPF

> **DOCUMENTO REESCRITO EM CAMADAS — 2026-09-13**
>
> | Camada | Uso |
> |--------|-----|
> | **Estado atual** | Fonte operacional hoje · ver também `Docs/CURRENT-TRUTH.md` e NET10-26 |
> | **Avanços desta fase (histórico)** | Registro do que esta execução entregou — **não** sobrescrever mentalmente o estado atual |
>
> HEAD de referência pós-NET10-26: `1372e11` · TFM `net10.0-windows` · Branch `migration/net10`
> Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md`

## Estado atual (pós NET10-26 · 2026-09-13)

| Item | Valor |
|------|-------|
| Branch | `migration/net10` |
| HEAD fiscal foundation | `1372e11` |
| TFM | `net10.0-windows` |
| Unit | **194/194** |
| QaEngine | **43/43** |
| DeepQa | **6/6** (baseline NET10-26) |
| Fiscal LIVE / WhatsApp API / Code signing | **BLOCKED_EXTERNAL** |
| Calendar Dark | Mitigado (`CalendarContrastHealer`) — não citar KNOWN LIMITATION antigo como atual |
| NF-e | PARTIAL + TESTED (Focus path + Fake) |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY |
| DANFE | PDF informativo (≠ SEFAZ oficial) |
| Multiempresa fiscal | IMPLEMENTED + TESTED (DB) |

**Claims abaixo sobre net6, “emissão NÃO IMPLEMENTADO”, Unit 173, DANFE/cancel NI, Calendar Dark KNOWN LIMITATION, etc. pertencem ao registro histórico da fase.**

---

## Avanços desta fase (registro histórico — preservar)

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
