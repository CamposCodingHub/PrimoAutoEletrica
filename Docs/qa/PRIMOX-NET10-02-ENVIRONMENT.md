# PRIMOX NET10-02 — ENVIRONMENT PREPARATION

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
**BEFORE TFM:** `net6.0-windows`  
**AFTER TFM:** `net6.0-windows`

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE | TIMESTAMP |
|---|---:|---|---|---|
| `dotnet --list-sdks` | 0 | **10.0.302 only** | terminal | 2026-09-12 20:44 |
| `dotnet --list-runtimes` | 0 | Desktop 9.0.18 + 10.0.10 | terminal | idem |
| `dotnet build … -c Debug` (produto) | 0 | 0 erros | `…/product-build-debug-pre.txt` | 20:45 |
| `dotnet build … -c Release` | 0 | 0 erros · EXE 192512 | `…/product-build-release-pre.txt` | idem |
| File verify EXE | — | EXISTS sha `E5EE9DF4…` | bin Release | idem |

## Ambiente

| Item | Valor |
|---|---|
| SDK .NET 6 | **AUSENTE** (ENVIRONMENT LIMITATION) |
| SDK .NET 10 | **PRESENT** 10.0.302 |
| SignTool | Windows Kits 10.0.26100.0 |
| Inno | `C:\Program Files\Inno Setup 7\ISCC.exe` |
| global.json | ausente |

## SIMULAÇÃO

Build Debug+Release do produto **sem** mudar TFM: **PASS**.

## Decisão

**GREEN WITH ENVIRONMENT LIMITATION** (sem SDK 6 instalado; TFM ainda net6 via targeting/roll-forward).

Prosseguir NET10-03.
