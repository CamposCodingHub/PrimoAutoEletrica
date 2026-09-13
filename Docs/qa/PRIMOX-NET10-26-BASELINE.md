# PRIMOX NET10-26 — BASELINE (BLOCO 00)

**Timestamp:** 2026-09-13T14:32:00-03:00 (approx start) → 2026-09-13T14:40:50-03:00 (DeepQa end)  
**Branch:** `migration/net10`  
**Evidence root:** `TestResults/Net10-Overnight/20260913/NET10-26-Fiscal/`

## Git protect

| Ref | SHA |
|-----|-----|
| CURRENT HEAD | `114c39df403161893af67c74adc55aa832315ed7` |
| main | `29b19b16d0e6e3413bdba20c505e20c992596c24` |
| v1.0.0 | `72d85fa20f6102f694534e4e37b4ff03c8223529` |
| primox-net6-final | `63aeb05ec31064cc732e86c959e82a91d541cd5e` |

HEAD message: `114c39d audit: fiscal completeness NET10-25`

## Runtime / TFM

| Item | Value |
|------|-------|
| TFM | `net10.0-windows` |
| Assembly version | `1.0.0.0` |
| SDK | `10.0.302` |

## Baseline gates (pré-implementação)

| Gate | Result | Evidence |
|------|--------|----------|
| Build Debug | PASS (exit 0) | `bloco00-baseline.log` BUILD_D_EXIT=0 |
| Build Release | PASS (exit 0) | BUILD_R_EXIT=0 |
| Unit | **173/173** PASS | UNIT_EXIT=0 |
| Fiscal Fake filter | **46/46** PASS | FISCAL_EXIT=0 |
| QaEngine | **43/43** PASS | `smoke-QaEngine/ui-smoke-summary.json` |
| DeepQa | **6/6** PASS | `smoke-DeepQa/ui-smoke-summary.json` |

## Consistency

Baseline **PASS** — consistente com NET10-25 audit. Implementação NET10-26 pode prosseguir.

## Nota

Valores NET10-25 revalidados no repositório real; documentação anterior usada apenas como referência.
