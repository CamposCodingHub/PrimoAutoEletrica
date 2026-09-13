# PRIMOX FULL ASSURANCE-11 — Visual

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

## Method

- ExhaustiveUi FullSimulation **2026-09-11 13:00:25** — 8 rounds Light/Dark × 4 resolutions  
- Tema smoke **2/2**  
- Sidebar **PASS**  
- I18n07 **PASS** (PT/EN/ES)

## Exhaustive (this run)

| Metric | Value |
|--------|-------|
| Discovered | 3278 |
| Executable | 1882 |
| Tested | **1882** |
| PASS | **1882** |
| FAIL | **0** |
| BLOCKED | **0** |

## Themes

| Theme | Result |
|-------|--------|
| Light | **PASS** |
| Dark | **PASS** (Calendar header = known WPF limitation) |

## Resolutions

| Res | Light | Dark |
|-----|-------|------|
| 1366×768 | PASS | PASS |
| 1600×900 | PASS | PASS |
| 1920×1080 | PASS | PASS |
| 2560×1440 | PASS | PASS |

## Languages

| Lang | Result |
|------|--------|
| PT-BR | PASS |
| EN-US | PASS |
| ES-ES | PASS |

## Visual findings

| Id | Sev | Notes |
|----|-----|-------|
| VIS-000 | — | No new P0/P1 visual blockers |
| VIS-CAL-DARK | Known | Native WPF Calendar Dark header |

Gold hover remains Sidebar-restricted (Sidebar PASS).
