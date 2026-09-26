# C2 Data Lineage — Intelligence-Relevant Fields

**Cycle:** C2.0 Discovery  
**Honesty rule:** only paths verified in source on branch `cycle-c2/primox-intelligence` @ `6ed757c`.  
**Notation:** ORIGEM → DB/FILE → Repo/Service → Domain → Context → Assist

## Legend

| Token | Meaning |
|-------|---------|
| SQLite | Table in operacional/app SQLite via DatabaseService |
| JSON-FILE | File store under AppData (not a table) |
| SNAPSHOT | Denormalized column on parent row |
| SOFT-FK | Id stored as TEXT/Guid without enforced FOREIGN KEY |
| GAP | Desired C2 path not fully wired today |

---

## 1. Technical knowledge (boletins)

| Field examples | Lineage |
|----------------|---------|
| Code, Title, System, Voltage, Symptom, PossibleCauses, DiagnosticProcedure, Solution, Warnings, Tags, Status | UI `BaseConhecimentoControl` / `CasoTecnicoDialog` → `KnowledgeService` → `KnowledgeRepository` → SQLite `TechnicalKnowledgeEntries` (PK `KnowledgeId`, UNIQUE `Code`, FK `CreatedByUserId`→`Funcionarios`) → retrieved into `AssistantQueryContext.RetrievedKnowledge` → `IAssistantProvider` |
| SourceType enum | Domain `KnowledgeSourceType` persisted as TEXT |

**Assist use:** primary Evidence for grounded answers.

## 2. Diagnostic cases (casos reais)

| Field examples | Lineage |
|----------------|---------|
| Code, Title, Symptom, ConfirmedCause, Solution, PartsUsed, Measurements, DtcCodes, FinalResult | UI / `KnowledgeService.CriarCasoAPartirDeOSAsync` → `KnowledgeRepository` → SQLite `DiagnosticCases` (PK `CaseId`) → `RetrievedCases` → Provider |
| VehicleId, WorkOrderId | SOFT-FK TEXT columns (indexes exist; **no** FK to `Veiculos`/`OrdensServico`) |
| KnowledgeEntryId | FK → `TechnicalKnowledgeEntries(KnowledgeId)` ON DELETE SET NULL |
| TechnicianId | FK → `Funcionarios(Id)` ON DELETE SET NULL |

**Assist use:** empirical Evidence; confidence should stay conservative when only one case matches.

## 3. Vehicle

| Field examples | Lineage |
|----------------|---------|
| Id, ClienteId, Marca, Modelo, Ano, Placa, SistemaEletrico, Quilometragem, electrical test fields, ProblemaRecorrente | UI `VeiculosControl` / windows → `ClienteRepository` (vehicle CRUD lives with client repo) / `VeiculoProfileService` → SQLite `Veiculos` (PK `Id`, FK `ClienteId`→`Clientes`) |
| Into Assist | `AssistantService` builds `AssistantVehicleContext` from OS snapshot / heuristics today — **not** a full Veiculo load in all paths (GAP) |

**360:** `Primox360Service.ObterVeiculo360` → `Veiculo360Snapshot` (domain name is Portuguese `Veiculo360Snapshot`, not `Vehicle360`).

## 4. Client

| Field examples | Lineage |
|----------------|---------|
| Id, Nome, CPF, contacts, address, LGPD flags, TotalGasto | UI Clientes* → `ClienteRepository` → SQLite `Clientes` |
| Into Assist | **Not** currently injected into `AssistantQueryContext` (GAP — and PERSONAL/SENSITIVE; should stay out unless minimum-necessary + permission) |

**360:** `Cliente360Snapshot` via `IPrimox360Service.ObterCliente360` — includes financial rollups; **must not** feed Assist without finance permission (see classification).

## 5. Work order / OS

| Field examples | Lineage |
|----------------|---------|
| Id, Numero, ClienteId, VeiculoId?, ProblemaRelatado, Diagnostico*, Status, Itens, Checklist* strings, money fields | UI OS windows → `OrdemServicoRepository` / `DatabaseService.OrdensServico` → SQLite `OrdensServico`, `OrdemServicoItens`, `OrdemServicoEventos` |
| Into Assist | `AssistantService.ConsultarAsync(osId:)` loads OS → `AssistantWorkOrderContext` (Number, Symptom, Status, CurrentItems) + weak vehicle snapshot from `VeiculoDescricaoSnapshot` |

## 6. Budget / Orçamento

| Field examples | Lineage |
|----------------|---------|
| Id, ClienteId, VeiculoId, Numero, Status, totals, margins, Diagnostico | UI Orcamentos* → `OrcamentoDatabaseService` (+ approval/PDF services) → SQLite `Orcamentos` / items |
| Into Assist | **Not wired** today (GAP). Financial — fail-closed without permission. |

## 7. Checklist

| Field examples | Lineage |
|----------------|---------|
| ChecklistTecnicoOS items by OS/Veiculo | `ChecklistTecnicoService` → **JSON-FILE** `checklist-os-{ordemServicoId}.json` (not SQLite) |
| OS string fields ChecklistEntrada/Entrega/Saida | SNAPSHOT on `OrdensServico` |
| DVI | `DviChecklistService` (separate path) |

**Assist:** not in Retrieval today (GAP). Prefer structured JSON checklist over free-text OS strings when indexing later.

## 8. Auto Elétrica / Diagnóstico técnico

| Field examples | Lineage |
|----------------|---------|
| Roteiros, prontuário, medições, `DiagnosticoTecnico` | UI `AutoEletricaTecnicaControl` → `AutoEletricaTecnicaService` / `DiagnosticoTecnicoService` → **JSON-FILE** store |
| Measurements into Assist | Types exist (`AssistantMeasurementContext`) but population from AutoElétrica JSON is **GAP** |

## 9. Stock / Product

| Field examples | Lineage |
|----------------|---------|
| Produto Id, Codigo, Nome, estoque qtys, PrecoCompra/Venda | UI Estoque/Produtos → `ProdutoRepository` / `EstoqueOperationalService` → SQLite `Produtos` (+ stock movements elsewhere) |
| Into Assist | Not direct. Parts names may appear via DiagnosticCase.PartsUsed or OS items. Unit costs are FINANCIAL. |

**360:** `Produto360Snapshot` / `Produto360Window`.

## 10. PurchaseRequest

| Field examples | Lineage |
|----------------|---------|
| PurchaseRequestId, Number, Priority, Status, cost cents, Items | UI `NecessidadesCompraControl` / dialogs → `PurchaseService` → `PurchaseRepository` → SQLite `PurchaseRequests`, `PurchaseRequestItems` (FK ProductId→`Produtos`, SupplierId→`Fornecedores`) |
| Into Assist | Out of Assist scope for C2.1 unless explicitly requested; costs = FINANCIAL. |

## 11. Tool / Ferramentas

| Field examples | Lineage |
|----------------|---------|
| ToolId, Code, Name, Status, PurchaseValueCents, checkouts, maintenance | UI `FerramentasControl` / `Tool360Window` → `ToolService` → `ToolRepository` → SQLite `Tools`, `ToolCheckouts`, `ToolMaintenances` |
| Into Assist | Optional future (“which tool for D01”); purchase value = FINANCIAL. |

## 12. Finance

| Field examples | Lineage |
|----------------|---------|
| Contas a receber / caixa / OS totals | `FinanceiroDatabaseService`, `CaixaService`, OS/Orçamento money columns; 360 joins ContasReceber by ClienteId / Origem+ReferenciaExterna (**no name-only match** — enforced in Primox360 design comments) |
| Into Assist | **Fail-closed** without finance permission. Never default-include in Context. |

## 13. AuditLog

| Field examples | Lineage |
|----------------|---------|
| Categoria, Acao, Entidade, Detalhes, Usuario*, Severidade | `AuditLogService` / `AuditoriaRepository` → SQLite `AuditLogs` |
| Assist | `AssistantService` / `KnowledgeService` already call audit on mutations/consults (verify per call sites). Audit is OPERATIONAL; do not send raw audit rows to LLM context. |

## 14. Client360 / Vehicle360 / OS360

| Aggregate | Service | Domain type | Persistence |
|-----------|---------|-------------|-------------|
| Client 360 | `Primox360Service.ObterCliente360` | `Cliente360Snapshot` | Read-model over Clientes/Veiculos/OS/Orcamentos/ContasReceber |
| Vehicle 360 | `ObterVeiculo360` | `Veiculo360Snapshot` | Read-model; may flag plate fallback |
| OS 360 | `ObterOrdemServico360` | `OrdemServico360Snapshot` | Hub links CONNECTED/MISSING |

**Naming note:** codebase uses Portuguese `Cliente360` / `Veiculo360`, not English `Client360` / `Vehicle360`.

## 15. Assist foundation types

| Type | File | Role |
|------|------|------|
| `IAssistantProvider` | `Models/AssistantContext.cs` | Provider port |
| `AssistantQueryContext` | same | Input bag |
| `AssistantResponse` | same | Output (current shape) |
| `AssistantConfidenceLevel` | same | INSUFFICIENT_EVIDENCE/LOW/MEDIUM/HIGH |
| `IAssistantService` / `AssistantService` | `Services/AssistantService.cs` | Orchestration + permission |
| `GroundedLocalRuleAssistantProvider` | `Services/GroundedLocalRuleAssistantProvider.cs` | Default deterministic provider |

See `C2_ASSIST_CONTRACT.md` for proposed adaptations (`EvidenceItem`, Warnings, MissingInformation, SuggestedNextSteps, Provider, Timestamp).

## 16. End-to-end Assist path (as implemented today)

```
User query (BaseConhecimentoControl)
  → AssistantService.ConsultarAsync (ASSIST_UTILIZAR)
  → token split → KnowledgeRepository artigos/casos
  → optional OS load → AssistantQueryContext
  → GroundedLocalRuleAssistantProvider.AskAsync
  → AssistantResponse (AnswerMarkdown, Hypotheses, RecommendedActions, CitedSources, ConfidenceLevel)
  → UI TextBlocks
```

No layer currently named Retrieval/Store separately; logic is collocated in `AssistantService`.
