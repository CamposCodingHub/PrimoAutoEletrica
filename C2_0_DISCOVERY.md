# C2.0 Discovery Summary — PRIMOX Workshop Intelligence

**Branch:** `cycle-c2/primox-intelligence`  
**Base:** `6ed757c` (C1.1.5 Desktop certified)  
**Gate:** C2.0 Architecture & Discovery ONLY  
**C2.1 started:** **NO**  
**Decision:** **C2.0 PASS** (docs + inventory; no product feature code; protected DB intact; `main` untouched)

## Documents produced

| Path | Content |
|------|---------|
| `Docs/c2/C2_ARCHITECTURE.md` | Target layers UI→Assist→Context→Retrieval→Store→Repo→SQLite; IAProvider; never UI→OpenAI |
| `Docs/c2/C2_DATA_LINEAGE.md` | ORIGEM→DB/FILE→Repo→Service→Domain→Context→Assist |
| `C2_DATA_LINEAGE.md` (repo root copy) | Same lineage for quick access |
| `Docs/c2/C2_DATA_CLASSIFICATION.md` | PUBLIC/INTERNAL/OPERATIONAL/FINANCIAL/TECHNICAL/PERSONAL/SENSITIVE + min necessary |
| `Docs/c2/C2_ASSIST_CONTRACT.md` | Current vs proposed contracts; EvidenceItem; fail-closed |
| `C2_0_DISCOVERY.md` (this file) | Inventory + GO/NO-GO |

## Protected DB SHA check

| Item | Value |
|------|-------|
| Path | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` |
| Expected SHA-256 | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` |
| Observed SHA-256 | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` |
| Result | **MATCH / INTACT** (mtime local 2026-09-23 18:24:05 America/Sao_Paulo) |
| Migrations in C2.0 | **NONE** |

## Git / main

| Check | Result |
|-------|--------|
| Current branch | `cycle-c2/primox-intelligence` |
| `main` HEAD | `29b19b16d0e6e3413bdba20c505e20c992596c24` (untouched by this work) |
| Product feature code in C2.0 commit | None (docs only) |

---

## Inventory map (real names)

### BaseConhecimento

| Layer | Location |
|-------|----------|
| UI | `UserControls/BaseConhecimentoControl.xaml(.cs)` — also hosts Assist query UI |
| Nav | `NavigationService` keys `BaseConhecimento` / `Conhecimento`; `MainWindow` menu |
| Service | `KnowledgeService` / `IKnowledgeService` |
| Repo | `KnowledgeRepository` / `IKnowledgeRepository` |
| Tables | `TechnicalKnowledgeEntries`, `DiagnosticCases` (init in `DatabaseService.C1.cs`) |
| ViewModel | None dedicated (code-behind) |
| Tests | `Services/KnowledgeServiceTests.cs`, `BaseConhecimentoOwnerHarnessRegressionTests.cs`, C1 UI tests |

### DiagnosticCase

| Layer | Location |
|-------|----------|
| Domain | `Models/TechnicalKnowledge.cs` → `DiagnosticCase` |
| PK | `CaseId` (TEXT/Guid) |
| FK | `KnowledgeEntryId`→`TechnicalKnowledgeEntries`; `TechnicianId`→`Funcionarios`; **VehicleId/WorkOrderId soft (no FK)** |
| Service/Repo/UI/Tests | via Knowledge* + `Views/CasoTecnicoDialog` + AssistFoundationTests |

### TechnicalKnowledgeEntry

| Layer | Location |
|-------|----------|
| Domain | `Models/TechnicalKnowledge.cs` |
| PK | `KnowledgeId`; UNIQUE `Code` |
| FK | `CreatedByUserId`→`Funcionarios` |
| Status | DRAFT/PUBLISHED/ARCHIVED |

### Vehicle (`Veiculo`)

| Layer | Location |
|-------|----------|
| Domain | `Models/Veiculo.cs` |
| Table | `Veiculos` |
| Repo | CRUD via `ClienteRepository` (vehicles nested with clients) |
| Services | `VeiculoProfileService`, `VeiculoMediaService`, catalog services |
| UI / VM | `VeiculosControl`, `VeiculosViewModel`, Novo/Visualizar windows |
| 360 | `Veiculo360Snapshot` via `Primox360Service` |

### Client (`Cliente`)

| Layer | Location |
|-------|----------|
| Domain | `Models/Cliente.cs` |
| Table | `Clientes` |
| Repo | `ClienteRepository` / `IClienteRepository` |
| UI / VM | `ClientesControl`, `ClientesViewModel`, Clientes/* windows |
| 360 | `Cliente360Snapshot` (**not** English `Client360` type name) |

### WorkOrder / OS (`OrdemServico`)

| Layer | Location |
|-------|----------|
| Domain | `OrdemServico`, `OrdemServicoItem`, `OrdemServicoEvento` |
| Tables | `OrdensServico`, `OrdemServicoItens`, `OrdemServicoEventos` |
| Repo | `OrdemServicoRepository` |
| DB partial | `DatabaseService.OrdensServico.cs` |
| UI / VM | `OrdemServicoWindow`, `OrdensServicoViewModel` |
| 360 | `OrdemServico360Snapshot` |

### Budget / Orçamento

| Layer | Location |
|-------|----------|
| Domain | `Orcamento`, `OrcamentoItem` |
| Table | `Orcamentos` (+ items) |
| Services | `OrcamentoDatabaseService`, `OrcamentoAprovacaoService`, `OrcamentoPdfService` |
| Repo folder | No `IOrcamentoRepository` in `Repositories/` (service-centric) |
| UI / VM | Rich `Orcamento*Control` set + `OrcamentosViewModel` |

### Checklist

| Layer | Location |
|-------|----------|
| Service | `ChecklistTecnicoService`, `DviChecklistService` |
| Persistence | **JSON files**, not SQLite; OS also has Checklist* string snapshots |
| UI | `ChecklistTecnicoWindow` |

### Auto Elétrica

| Layer | Location |
|-------|----------|
| Domain | `Models/AutoEletricaTecnica.cs` (roteiros, prontuário, `DiagnosticoTecnico`, …) |
| Services | `AutoEletricaTecnicaService`, `AutoEletricaRoteiroPersistService`, `DiagnosticoTecnicoService` (JSON) |
| UI / VM | `AutoEletricaTecnicaControl`, `AutoEletricaTecnicaViewModel` |
| Tests | `UiSmokeTestService.AutoEletrica`, C1 suites |

### Stock / Product

| Layer | Location |
|-------|----------|
| Domain | `Produto` (+ fornecedor/import models) |
| Table | `Produtos` |
| Repo | `ProdutoRepository` |
| Services | `EstoqueOperationalService`, `DatabaseService.Produtos/Estoque` |
| UI / VM | `EstoqueControl`, `EstoqueViewModel`, produto windows, `Produto360Window` |

### PurchaseRequest

| Layer | Location |
|-------|----------|
| Domain | `Models/PurchaseRequest.cs` (+ items, enums) |
| Tables | `PurchaseRequests`, `PurchaseRequestItems` |
| Service/Repo | `PurchaseService` / `PurchaseRepository` |
| UI | `NecessidadesCompraControl`, `NovaRequisicaoCompraDialog` |
| Tests | `PurchaseServiceTests.cs` |

### Tool / Ferramentas

| Layer | Location |
|-------|----------|
| Domain | `Models/Tool.cs` (Tool, ToolCheckout, ToolMaintenance) |
| Tables | `Tools`, `ToolCheckouts`, `ToolMaintenances` |
| Service/Repo | `ToolService` / `ToolRepository` |
| UI | `FerramentasControl`, `Tool360Window`, dialogs |
| Tests | `ToolServiceTests.cs` |

### Finance

| Layer | Location |
|-------|----------|
| Services | `FinanceiroDatabaseService`, `CaixaService`, `RelatorioFinanceiroService` |
| UI / VM | `FinanceiroControl`, `FinanceiroViewModel`, caixa windows |
| 360 finance | ContasReceber joins by Id/Origem only (documented in Primox360Models) |

### AuditLog

| Layer | Location |
|-------|----------|
| Table | `AuditLogs` (`DatabaseService.Audit.cs`) |
| Services | `AuditLogService`, `AuditTrailService` |
| Repo | `AuditoriaRepository` (also EventoJson variant inserts observed) |

### Client360 / Vehicle360

| Aggregate | Real type / API |
|-----------|-----------------|
| Client 360 | `Cliente360Snapshot` / `IPrimox360Service.ObterCliente360` |
| Vehicle 360 | `Veiculo360Snapshot` / `ObterVeiculo360` |
| Service file | `Services/Primox360Service.cs` |
| Tests | `Primox360ServiceTests`, `Primox360IdFinancialJoinTests`, `Flow360FullLifecycleE2ETests` |
| UI | Cliente/Veiculo visualize flows + `Produto360Window` / `Tool360Window` (no separate Client360Window filename found) |

### Assistant Foundation

| Piece | Status |
|-------|--------|
| `IAssistantProvider` | EXISTS |
| `AssistantQueryContext` / `AssistantResponse` / confidence enum | EXISTS |
| `AssistantService` + `ASSIST_UTILIZAR` | EXISTS |
| `GroundedLocalRuleAssistantProvider` | EXISTS (only provider) |
| OpenAI / remote provider | **ABSENT** |
| `EvidenceItem`, Warnings, MissingInformation, Provider, Timestamp fields | **ABSENT** (proposed in contract doc) |
| Retrieval index / FTS | **ABSENT** (token split only) |
| ContextBuilder isolation | **ABSENT** (logic inside AssistantService) |
| Assist DI singleton | **WEAK** (UI constructs `new AssistantService()`) |
| Tests | `AssistFoundationTests.cs` (insufficient evidence + structured response paths) |

---

## Existing vs missing (Assist / C2.1 readiness)

| Capability | Existing | Missing / gap |
|------------|----------|---------------|
| Local grounded provider | Yes | Remote provider (intentionally deferred) |
| Permission seed Assist/Knowledge/Tools/Compras | Yes (C1 schema) | Finance-specific Assist strip |
| KB + Cases SQLite | Yes | FTS/index; stronger FKs Vehicle/OS on cases |
| OS→Assist context | Partial | Full Veiculo load; measurements from AutoElétrica JSON |
| Checklist/Diagnóstico in Assist | No | File→Context adapters |
| Fail-closed cross-client | Design only | Implementation + tests |
| C2 response contract fields | Partial | EvidenceItem + Warnings + MissingInformation + Provider + Timestamp |
| Schema migrations for C2 | N/A | Follow DESIGN→SHADOW→REHEARSAL→GO/NO-GO→MIGRATION; **not started** |

## Fail-closed notes (design only)

1. **No data** → INSUFFICIENT_EVIDENCE + MissingInformation.  
2. **Out of domain** → Warning; no enrichment from finance/PII.  
3. **Financial without permission** → strip + Warning.  
4. **Other client without auth** → refuse + AuditLog.  

No new runtime stubs added in C2.0 (docs-first).

## Schema policy reminder

Any future table: **DESIGN → SHADOW → REHEARSAL → GO/NO-GO → MIGRATION**.  
Never write protected `primoauto.db`. Rehearse on operacional copies only.

## GO / NO-GO for C2.1

| Criterion | Result |
|-----------|--------|
| Architecture documented | GO |
| Lineage + classification + contract documented | GO |
| Inventory of real types (incl. Assist foundation) | GO |
| Protected SHA intact | GO |
| `main` intact | GO |
| No premature C2.1 implementation | GO |
| **C2.1 entry** | **GO with gate** — start indexing/search only after accepting contract adaptations and fail-closed tests plan |

### Proposed next gate (C2.1)

1. Additive DTOs (`EvidenceItem` + response extensions) + mappers; keep local provider.  
2. Retrieval MVP over `TechnicalKnowledgeEntries` + `DiagnosticCases` (ranked token/FTS on operacional).  
3. ContextBuilder with classification filter + unit tests for F1–F4 fail-closed.  
4. Register `IAssistantService` in DI; stop `new AssistantService()` in UI.  
5. Still **no** OpenAI; **no** protected DB migration; **no** `main` merge until cycle certification.

---

**C2.0 PASS** · **C2.1 started: NO**
