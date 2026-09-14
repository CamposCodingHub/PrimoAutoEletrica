# PRIMOX — Fonte de verdade atual (CURRENT TRUTH)

**Atualizado:** 2026-09-13 (pós NET10-30 Master Audit / UX P0)  
**Branch de trabalho:** `audit/product-discovery-2026-09`  
**HEAD tip:** `506de39`  
**Push desta sessão:** **NO**  
**TFM ativo:** **`net10.0-windows` / `net10.0`**  
**Tag comercial protegida:** `v1.0.0` → `72d85fa` (**não mover**)  
**main / primox-net6-final:** protegidos

**Master Audit FINAL:** [`Docs/product/PRIMOX-MASTER-PRODUCT-AUDIT-FINAL-2026-09.md`](product/PRIMOX-MASTER-PRODUCT-AUDIT-FINAL-2026-09.md)  
**Evidence FINAL:** [`Docs/product/PRIMOX-MASTER-PRODUCT-AUDIT-EVIDENCE-FINAL-2026-09.md`](product/PRIMOX-MASTER-PRODUCT-AUDIT-EVIDENCE-FINAL-2026-09.md)  
**360 evidence NET10-29:** [`Docs/product/PRIMOX-CUSTOMER-VEHICLE-OS-360-EVIDENCE-NET10-29-2026-09.md`](product/PRIMOX-CUSTOMER-VEHICLE-OS-360-EVIDENCE-NET10-29-2026-09.md)

---

## Snapshot técnico CURRENT

| Área | Estado |
|------|--------|
| Unit | **206/206** PASS |
| Build Release | PASS |
| QaEngine | **43/43** (`net10-30-qaengine-r2`) |
| DeepQa | **6/6** (`net10-30-deepqa-r2`) |
| ExhaustiveUi | **APROVADO** (`net10-30-exhaustive-r1`) |
| Cliente 360 | PASS + context actions |
| Veículo 360 | PASS + context actions |
| OS 360 | PARTIAL (Fiscal/pós MISSING) |
| ContasReceber.ClienteId | **BLOCKED** |
| Agenda Prefill ClienteId/VeiculoId | REAL |
| Fiscal LIVE / Signing / WA API / DVI | BLOCKED / MISSING |
| Desktop shortcut | Release `PrimoAutoEletrica.exe` |

---

## Etapas

| Etapa | HEAD | Resultado |
|-------|------|-----------|
| NET10-28 | `fa6d1d5` | Discovery docs |
| NET10-29 | `5d7c374` | 360 KPIs PARTIAL |
| NET10-30 | `506de39` | Master Audit + UX P0 **PARTIAL** (gates PASS) |

**Próximo:** fechar itens internos restantes do `PROJECT_STATUS.md` sem inventar externos.

---

## Proteções

- Sem merge/push/tag em main/v1.0.0/primox-net6-final sem ordem.
- Unit CURRENT = **206** (não usar 173/194/203 históricos).
