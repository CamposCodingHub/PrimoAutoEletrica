# PRIMOX NET10-17 — FINAL COMPARISON

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

| Área | .NET 6 (baseline/histórico) | .NET 10 (esta branch) | Notas |
|---|---|---|---|
| Framework | net6.0-windows | **net10.0-windows** | migrado |
| Build | PASS (baseline) | **PASS** 0 erros | CURRENT |
| Startup | ~2063 ms hist. | **~1910 ms** (10×) | CURRENT vs HISTORICAL |
| Functional | QaEngine 43 etc. | **43/43 · Deep 6/6 · Exhaustive PASS** | CURRENT |
| Database | migrations 28 | **28 · integrity ok** | CURRENT |
| Security | YELLOW herdado | **A12/A13 PASS · RedTeam YELLOW** | sem Critical novo |
| Visual/I18N | PASS | **Tema/I18n07/Overnight PASS** | screenshots gallery N/E |
| Installer | 1.0.0 comercial | **1.1.0-net10-preview** experimental | 1.0.0 intact |
| Publish | self-contained | **self-contained win-x64** ~186 MB folder | CURRENT |
| Side-by-side | — | AppData isolation PASS | net6 EXE BLOCKED EXTERNAL |
| Fiscal LIVE | BLOCKED | **BLOCKED EXTERNAL** | Fake 46/46 PASS |
| Signing | BLOCKED | **BLOCKED** | externo |

## Classificação

**NET10 READY WITH LIMITATIONS**

Não promover para main / não substituir comercial 1.0.0.
