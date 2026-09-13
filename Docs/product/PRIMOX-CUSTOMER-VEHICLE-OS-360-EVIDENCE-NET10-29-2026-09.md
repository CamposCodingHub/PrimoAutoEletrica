# PRIMOX NET10-29 — Customer / Vehicle / OS 360 Evidence Pack

**Date/Time baseline:** 2026-09-13T19:48:12-03:00  
**Branch:** `audit/product-discovery-2026-09`  
**Final HEAD:** (set at commit) — see `git log -1` after `feat(product): implement customer vehicle os 360 net10-29`  
**QaEngine:** 43/43 APROVADO (`net10-29-qaengine`) · **DeepQa:** 6/6 APROVADO (`net10-29-deepqa`)  
**Unit CURRENT:** 203/203  

**Baseline HEAD:** `fa6d1d5a14a235f2f07af382b5992739c150d6c4` (NET10-28 intact)  
**Working tree at start:** clean except `?? Docs/product/_net10-28-runlog.txt` (PRE-EXISTING / audit residual — not committed)

---

## 1. Status final (honest)

| Surface | EXISTE | FUNCIONA | INTEGRADA | VALOR COMPROVADO | Overall |
|---------|--------|----------|-----------|------------------|---------|
| CUSTOMER 360 | PASS | PASS | PASS | PASS (IDs) | **PASS** (dívida total = N/A) |
| VEHICLE 360 | PASS | PASS | PASS | PASS (VeiculoId) | **PASS** |
| OS 360 | PASS | PASS | PARTIAL | PASS (hub links) | **PARTIAL** |

**NET10-29 overall: PARTIAL** — intentional: ContasReceber.ClienteId migration **BLOCKED**; Fiscal/pós-venda still MISSING on hub.

---

## 2. Relationship matrix (verified)

| ENTIDADE | CAMPO | DESTINO | TIPO | EVIDÊNCIA | STATUS |
|----------|-------|---------|------|-----------|--------|
| ContasReceber | Cliente TEXT | Cliente nome | D / FRÁGIL | FinanceiroDatabaseService.cs CREATE | UNCHANGED schema |
| ContasReceber | Origem+ReferenciaExterna | OS/Orç Id | C SAFE | Upsert integração | USED by Primox360 |
| OrdemServico | ClienteId | Cliente | A | model/repo | USED |
| OrdemServico | VeiculoId | Veiculo | B | model/repo | USED (no placa in KPI) |
| Orcamento | ClienteId / VeiculoId | Cliente/Veiculo | B | model | USED |
| Venda | Cliente?.Id | Cliente | B | VendaRepository | USED |

---

## 3. Text-match audit (actions)

| Location | Was | Action |
|----------|-----|--------|
| HistoricoClienteWindow ContasReceber nome Contains | CRITICAL | **Replaced** by Origem+ReferenciaExterna via Primox360Service |
| Historico orçamentos/vendas nome fallback | FRAGILE | **Removed** — ClienteId only |
| VisualizarVeiculo OS by placa | FRAGILE | **Removed** from primary list — VeiculoId only |
| VisualizarVeiculo orç by ClienteId | WEAK | **Replaced** by VeiculoId (+ OS link) |
| ContasReceber.ClienteId column | — | **NOT added** (HIGH RISK / BLOCKED) |

---

## 4. GAP matrix (NET10-29)

| ID | ENTIDADE | PROBLEMA | ATUAL | ALVO | RISCO | AÇÃO | STATUS |
|----|----------|----------|-------|------|-------|------|--------|
| G001 | ContasReceber→Cliente | TEXT only | Cliente TEXT | ClienteId | HIGH | migration design | **BLOCKED** |
| G002 | Dívida total KPI | N/A | N/A display | FK | HIGH | depends G001 | **BLOCKED** |
| G003 | Receita 12m UI | missing | KPI cards | show | LOW | implement | **DONE** |
| G007 | TotalGasto UI | backend | shown | show | LOW | implement | **DONE** |
| G008 | Dias desde serviço | missing | shown | show | LOW | implement | **DONE** |
| G009 | Orç VeiculoId filter | wrong | VeiculoId | filter | LOW | implement | **DONE** |
| G010 | OS→Fiscal | MISSING | MISSING | emit | HIGH | out of scope | **BLOCKED** |
| G013 | Pós-venda | MISSING | MISSING | jobs | MED | NET10-30 | **DEFERRED** |

---

## 5. Implemented changes

**ADDED**
- `Models/Primox360Models.cs`
- `Services/Primox360Service.cs` (+ `IPrimox360Service`)
- `Tests/.../Primox360ServiceTests.cs` (9 cases)
- this evidence pack

**MODIFIED**
- `DependencyInjection/ServiceExtensions.cs` — register 360
- `HistoricoClienteWindow.xaml(.cs)` — KPI 360 + ID finance
- `VisualizarVeiculoWindow.xaml.cs` — KPIs, VeiculoId filters, open Cliente 360
- `OrdensServicoControl.xaml(.cs)` — OS hub text + Veículo 360 button
- `FinanceiroDatabaseService.cs` / `OrcamentoDatabaseService.cs` — optional `DatabaseService` ctor (testability)

**DELETED:** none  
**NOT done:** ContasReceber ClienteId migration, DVI, approval, WA API, Focus live

---

## 6. Tests CURRENT

| Suite | Result | Evidence |
|-------|--------|----------|
| Build Release | 0 errors | Run-UiSmoke / dotnet build |
| Unit | **203/203** PASS | was 194 baseline; +9 Primox360 |
| Primox360 | 9/9 PASS | Primox360ServiceTests |
| QaEngine | **43/43** APROVADO | `TestResults/UiSmoke/net10-29-qaengine/ui-smoke-summary.json` |
| DeepQa | **6/6** APROVADO | `TestResults/UiSmoke/net10-29-deepqa/ui-smoke-summary.json` |

---

## 7. Known limitations

- Dívida total do cliente permanece **N/A** sem ClienteId.
- OS Fiscal / Pós-venda = MISSING on hub (honest).
- Timeline = consolidation of existing events (PARTIAL).
- Auto elétrica aprendida / DVI = FUTURE / out of scope.
- Agendamentos no veículo ainda podem usar placa (ACCEPTABLE filter) — not used for revenue KPIs.

## 8. Next

NET10-30 — Workflow + Automation Engine  
+ ContasReceber ClienteId migration design (explicit).
