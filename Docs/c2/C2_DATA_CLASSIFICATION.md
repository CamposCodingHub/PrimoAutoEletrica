# C2 Data Classification — Assist Context

**Cycle:** C2.0 Discovery  
**Purpose:** classify fields before they may enter Assist Context. Minimum-necessary by default. Fail-closed on FINANCIAL / PERSONAL / SENSITIVE / cross-client.

## Classes

| Class | Definition | Default Assist policy |
|-------|------------|----------------------|
| PUBLIC | Non-sensitive workshop-facing technical labels (e.g. published KB title/code once PUBLISHED) | Allow |
| INTERNAL | Operational codes, statuses, module names | Allow if authenticated session |
| OPERATIONAL | OS numbers, tool codes, stock quantities (non-cost), checklist pass/fail | Allow for users with module permission |
| TECHNICAL | Symptoms, procedures, measurements, DTC, causes, solutions from KB/Cases/AutoElétrica | Allow with `CONHECIMENTO_VER` / `ASSIST_UTILIZAR` |
| FINANCIAL | Prices, costs (incl. CentsV1), margins, debt, revenue, purchase totals | **Deny** unless finance permission; never default in Context |
| PERSONAL | Client name + contact + address | Minimum necessary (e.g. first name only when OS-scoped); prefer omit |
| SENSITIVE | CPF/CNPJ, RG, LGPD artifacts, signatures, document paths | **Deny** in Assist Context always in C2.x design |

## Entity matrix (high level)

| Entity | Typical classes | Minimum necessary for Assist |
|--------|-----------------|------------------------------|
| TechnicalKnowledgeEntry | TECHNICAL, PUBLIC (if PUBLISHED), INTERNAL (DRAFT) | Code, Title, System, Voltage, Symptom, procedures, warnings — only PUBLISHED unless elevated |
| DiagnosticCase | TECHNICAL, OPERATIONAL; PartsUsed may imply cost elsewhere | Symptom, cause, solution, measurements, DTC, vehicle model — strip client identifiers if present |
| Veiculo | OPERATIONAL + TECHNICAL; plate OPERATIONAL; chassi/renavam SENSITIVE-adjacent | Make/Model/Year/Voltage/SistemaEletrico; plate only if needed for OS scope |
| Cliente | PERSONAL + SENSITIVE + FINANCIAL (TotalGasto) | Prefer ClienteId only for authZ checks; do not send CPF/contacts to provider |
| OrdemServico | OPERATIONAL + TECHNICAL + FINANCIAL (totals) | Numero, Symptom, Status, item descriptions; strip money |
| Orcamento | FINANCIAL + OPERATIONAL | Exclude by default |
| Checklist (JSON) | OPERATIONAL + TECHNICAL | Item results / failures only |
| DiagnosticoTecnico (JSON) | TECHNICAL | Measurements + roteiro conclusions |
| Produto / Estoque | OPERATIONAL (+ FINANCIAL for prices) | Name/code for parts suggestions; no PrecoCompra/Venda without permission |
| PurchaseRequest | FINANCIAL + OPERATIONAL | Exclude by default |
| Tool | OPERATIONAL (+ FINANCIAL PurchaseValueCents) | Name/category/status; no purchase value |
| ContasReceber / Caixa / 360 revenue | FINANCIAL | Exclude; 360 finance KPIs stay in UI 360, not Assist |
| AuditLogs | INTERNAL / OPERATIONAL | Never as model context |

## Permission anchors already in code

| Code | Use |
|------|-----|
| `ASSIST_UTILIZAR` | Required by `AssistantService.ConsultarAsync` |
| `ASSIST_CONFIGURAR` | Seeded; for future provider config |
| `CONHECIMENTO_VER/CRIAR/EDITAR/PUBLICAR/ARQUIVAR` | `KnowledgeService` |
| Finance module perms (existing Financeiro UI) | Must gate FINANCIAL context (design for C2.1 — not fully Assist-wired yet) |

## Fail-closed classification rules (design)

1. **No data / empty retrieval:** respond INSUFFICIENT_EVIDENCE; do not widen class by guessing.  
2. **Out of domain:** do not pull FINANCIAL or PERSONAL “to be helpful”.  
3. **Financial without permission:** drop fields; add Warning.  
4. **Other client without auth:** refuse entire consult; audit; do not return other Cliente360.  
5. **Draft knowledge:** treat as INTERNAL; exclude from default retrieval unless `CONHECIMENTO_EDITAR`/`PUBLICAR` path.

## Minimum-necessary Context template (proposed)

```
Query (user text)
Vehicle: make, model, year, voltage, category   # no chassi/CPF
WorkOrder: number, symptom, status, item labels # no money
Measurements: parameter, value, unit, expected  # technical only
RetrievedKnowledge: published entries only
RetrievedCases: sanitized cases (no client PII)
Parameters: flags (permissions, locale) — not secrets
```
