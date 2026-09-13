# PRIMOX — Fonte de verdade atual (CURRENT TRUTH)

**Atualizado:** 2026-09-13 (pós NET10-29)  
**Branch de trabalho:** `audit/product-discovery-2026-09`  
**HEAD CURRENT:** `5d7c374` — `feat(product): implement customer vehicle os 360 net10-29`  
**NET10-28 (docs):** `fa6d1d5` — master product discovery  
**migration/net10 tip:** `68076a6` (NET10-27 TFM)  
**TFM ativo:** **`net10.0-windows` / `net10.0`**  
**Tag comercial protegida:** `v1.0.0` → `72d85fa` (**não mover**)  
**main / primox-net6-final:** protegidos — sem merge/push/tag desta fase sem ordem explícita

**Crônica:** [`Docs/PRIMOX-ADVANCES-CHRONICLE.md`](PRIMOX-ADVANCES-CHRONICLE.md)  
**360 evidence:** [`Docs/product/PRIMOX-CUSTOMER-VEHICLE-OS-360-EVIDENCE-NET10-29-2026-09.md`](product/PRIMOX-CUSTOMER-VEHICLE-OS-360-EVIDENCE-NET10-29-2026-09.md)  
**Discovery NET10-28:** [`Docs/product/PRIMOX-PRODUCT-DISCOVERY-EVIDENCE-NET10-28-2026-09.md`](product/PRIMOX-PRODUCT-DISCOVERY-EVIDENCE-NET10-28-2026-09.md)

---

## O que usar como verdade

| Tema | Documento canônico |
|------|--------------------|
| Status vivo | Este arquivo + tracker |
| Cliente / Veículo / OS 360 | Evidence NET10-29 + `Primox360Service` |
| Product gaps / mercado | Master Gap + Market Benchmark (NET10-28) |
| Fiscal | NET10-26 docs — LIVE = BLOCKED_EXTERNAL |
| TFM | NET10-27 migration doc |

---

## Snapshot técnico (2026-09-13 — CURRENT)

| Área | Estado atual |
|------|--------------|
| Desktop core | REAL + TESTADO |
| TFM | 100% net10 |
| Unit | **203/203** PASS |
| Build Release | PASS |
| QaEngine | **43/43** APROVADO (`net10-29-qaengine`) |
| DeepQa | **6/6** APROVADO (`net10-29-deepqa`) |
| Cliente 360 | **PASS** (KPIs por ID; dívida total = N/A) |
| Veículo 360 | **PASS** (VeiculoId) |
| OS 360 | **PARTIAL** (hub; Fiscal/pós-venda MISSING) |
| ContasReceber.ClienteId | **BLOCKED** (sem migration por nome) |
| Dívida via Origem+ReferenciaExterna | REAL no Cliente 360 |
| NF-e Focus LIVE | BLOCKED_EXTERNAL |
| WA Business / TEF / DVI / approval remoto | NOT_IMPLEMENTED / BLOCKED |
| 2FA no login | Setup existe; **não** wired no login (HEAD) |
| Code signing | BLOCKED_EXTERNAL |
| Multi-filial | `MultiFilialDisponivel=false` |

---

## Etapas recentes

| Etapa | HEAD | Resultado |
|-------|------|-----------|
| NET10-27 | `68076a6` | TFM net10 active surface |
| NET10-28 | `fa6d1d5` | Product master discovery (docs) |
| NET10-29 | `5d7c374` | Cliente/Veículo/OS 360 **PARTIAL** |

**Próximo recomendado:** NET10-30 — Workflow + Automation Engine (+ design migration ContasReceber.ClienteId)

---

## Proteções

- Não mover `main`, `v1.0.0`, `primox-net6-final` sem ordem.
- Não tratar marketing de concorrente como prova técnica.
- Unit CURRENT ≠ Unit histórico (194/197 em docs antigos).
