# PRIMOX — UI Page-by-Page Audit 2.0

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

Gerado na auditoria overnight 2.0. Classificação baseada em QaEngine (navegação, tema, responsividade, a11y formal) + Exhaustive quando executado + correção Help.

| Página | Light | Dark | Funções | A11y | Responsive | Estado |
|--------|-------|------|---------|------|------------|--------|
| Dashboard | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Clientes | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Veículos | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| OS | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Orçamentos | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Agenda | PASS | PASS WITH NOTES | PASS | PASS WITH NOTES | PASS | PASS (Calendar Dark LIMITATION) |
| Estoque | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Financeiro | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| PDV | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Relatórios | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Importar NF-e | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Operações Fiscais | PASS WITH NOTES | PASS WITH NOTES | PASS | PASS | PASS WITH NOTES | FIXED (novo) |
| Ajuda | FIXED | PASS | PASS | PASS | PASS | FIXED |
| Configurações | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Login | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Command Center | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Kanban | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Fornecedores | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Funcionários | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |
| Catálogo | PASS | PASS | PASS | PASS WITH NOTES | PASS | PASS |

### Contagens (esta rodada)

- PASS: maioria dos módulos via QaEngine
- FIXED: Ajuda (theme bind); Operações Fiscais (novo módulo)
- LIMITATION: Calendar Dark header nativo
- BLOCKED: live Focus homolog UI end-to-end sem token
