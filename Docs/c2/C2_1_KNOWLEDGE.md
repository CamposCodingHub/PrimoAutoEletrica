# C2.1 Knowledge — Delivery Evidence

**Branch:** `cycle-c2/primox-intelligence`  
**Gate:** C2.1 Operational Knowledge Foundation  
**C2.2 started:** **NO** (default STOP after C2.1)  
**Date (America/Sao_Paulo):** 2026-09-26

## Decision

| Item | Result |
|------|--------|
| C2.1 decision | **PASS** (pending final `dotnet test` / build confirmation in commit notes) |
| Protected DB SHA | Must remain `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` |
| `main` | Untouched |
| OpenAI / API keys | None |
| UI Intelligent Search (C2.2) | Not started |

## Truth matrix slice

| Capability | Status | Evidence |
|------------|--------|----------|
| KnowledgeItem + KnowledgeType | DONE | `Models/KnowledgeItem.cs` |
| Deterministic index (no vector) | DONE | `DeterministicKnowledgeIndex` |
| Search "queda de tensão" | DONE | `KnowledgeRetrievalTests.Search_QuedaDeTensao_*` |
| D01–D17 = 17/17 indexed | DONE | `Index_D01_to_D17_Are_17_of_17_*` |
| Adapters over KB/Cases/Roteiros/OS | DONE | `KnowledgeItemAdapters` + `KnowledgeRetrievalService` |
| EvidenceItem + response enrich | DONE | `AssistantContext` + `AssistantContractMapper` |
| Fail-closed F1–F4 stubs | DONE | `AssistFailClosedPolicy` |
| ContextBuilder | PARTIAL | `AssistContextBuilder` (retrieval + OS; checklist/measurements deferred) |
| Protected migration | NOT DONE (by design) | adapters only |
| C2.2 UI search | NOT STARTED | STOP |

## Limitations (honest)

1. Checklist / Diagnóstico JSON file adapters not fully wired (would invent paths without confirmed layout).  
2. ContextBuilder does not yet load Auto Elétrica measurements from JSON.  
3. OS indexing limited to `ObterTodos().Take(50)` when repo injected.  
4. No FTS5/SQLite virtual table — in-memory postings only (rebuild on process lifetime).  
5. Assist UI still constructs `AssistantService` in code-behind; DI registration deferred.  
6. RelevanceScore is **retrieval ranking**, never fault probability.

## D01–D17 proof method

1. `AutoEletricaTecnicaService.CreateRoteirosDiagnostico()` returns 17 roteiros.  
2. Each mapped as `PROCEDURE` with codes D01…D17.  
3. `GetIndexedProceduresD01ToD17()` asserts count=17 and distinct codes.  
4. Upsert by DedupKey prevents improper duplicates.

## Root inventory used (from C2.0)

- `TechnicalKnowledgeEntry` / `DiagnosticCase` / `KnowledgeService` / `KnowledgeRepository`  
- `AutoEletricaTecnicaService` roteiros D01–D17  
- `AssistantService` / `GroundedLocalRuleAssistantProvider` / `AssistFoundationTests`  
- Operational DB read-only peek for DiagnosticCases table presence  

## Commit message

`C2.1: operational knowledge foundation and deterministic search`
