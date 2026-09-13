# PRIMOX NET10-09 — PERFORMANCE

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**HEAD:** `509f54a`

## CURRENTLY EXECUTED

| Métrica | RESULT | EVIDENCE |
|---|---|---|
| Startup MainWindow ×10 | avg **1910.1** ms · min 1879 · max 1981 · fails **0** | `startup-profile.json` |
| Nav Dashboard ×20 | avg **4607.9** ms · fails **0** | `nav-20.json` |
| Baseline .NET 6 (Assurance-13 histórico) | startup avg ~2063 ms | DOCUMENTAÇÃO BASELINE (não reexecutado net6 nesta máquina — SDK 6 ausente) |

## SIMULAÇÃO

Long-run curto: 20 ciclos Dashboard smoke com AppData isolado por ciclo: **PASS**.

## Comparação

Não inventar melhoria absoluta vs net6 re-medido nesta host. Observação: startup net10 medido **≈1910 ms** vs baseline documentado net6 **≈2063 ms** (categorias distintas: CURRENT vs HISTORICAL).

## Decisão

**PASS WITH LIMITATIONS** (RAM/CPU/handles detalhados não amostrados via PerformanceCounter nesta fase — startup+nav reais OK). Prosseguir **NET10-10**.
