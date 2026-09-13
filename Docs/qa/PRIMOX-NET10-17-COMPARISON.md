# PRIMOX NET10-17 — FINAL COMPARISON

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
