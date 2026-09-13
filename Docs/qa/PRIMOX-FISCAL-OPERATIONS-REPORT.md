# PRIMOX — FISCAL OPERATIONS 2.0 — REPORT

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

**Decision:** `READY WITH LIMITATIONS`  
**Classification:** `NF-e HOMOLOGATION IMPLEMENTED — LIVE HOMOLOGATION PENDING`

```text
HEAD (pré-commit): 13af145 + worktree
TAG: v1.0.0 → a4ad6fe INTACTA
BRANCH: main
WIP Scripts: PRESERVADO
```

## Evidence

| Área | Resultado |
|------|-----------|
| Build | PASS (0 errors) |
| Fiscal + Ops unit | PASS (45) |
| Production Guard | PASS (HTTP=0 em produção) |
| Fake scenarios | PASS |
| State machine inválidas | PASS |
| Cancel negativos / Fake cancel | PASS |
| Preview | PASS |
| HealthCheck | PASS |
| QaEngine | PASS 43/43 |
| DeepQa | PASS 6/6 |
| Exhaustive | PASS (FullSimulation OK) |
| Long Run | PASS (via DeepQa LongRunNavegacaoTema + QaEngine LongRun) |
| DB integrity | PASS (`ok`) |
| FK check | PASS (`[]`) |
| Live Focus | **NOT EXECUTED** — `PRIMOX_FOCUS_HOMOLOG_TOKEN` ausente |
| Cancel live | **NOT EXECUTED** |

## UI

- Menu **Operações Fiscais** → `FiscalOperationsControl`
- PDV: pré-visualização fiscal antes do envio homolog

## Não feito / fora de escopo

- Emissão produção
- NFC-e / NFS-e / SaaS / cloud
- DANFE completo
- Live homolog sem credencial

## STOP

SIM — não iniciar NFC-e/produção automaticamente.
