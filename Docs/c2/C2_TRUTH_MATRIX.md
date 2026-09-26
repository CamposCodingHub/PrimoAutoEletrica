# C2 Truth Matrix

**Branch:** `cycle-c2/primox-intelligence`  
**Updated:** 2026-09-26 (America/Sao_Paulo) — C2.5

| Capability | Status | Evidence |
|------------|--------|----------|
| Token Retrieval (deterministic index) | **IMPLEMENTED** | `DeterministicKnowledgeIndex` |
| OpenAI / remote LLM | **NOT_IMPLEMENTED** | by design |
| Embedding / Vector DB | **NOT_IMPLEMENTED** | by design |
| Knowledge Search UI ("Buscar no PRIMOX") | **IMPLEMENTED** | `PrimoxKnowledgeSearchControl` + `KnowledgeSearchService` |
| D01–D17 indexed + searchable | **IMPLEMENTED** | C2.1 + C2.2 D01..D17 tests |
| RBAC financial filter on search | **IMPLEMENTED** | `KnowledgeSearchService` + unit tests |
| EvidenceItem on search hits | **IMPLEMENTED** | mapped from real hits only |
| VehicleContext / ClientContext / WorkOrderContext (C2.3) | **IMPLEMENTED** | Context Engine services + ContextEngineTests |
| Auto stock / OS / purchase actions | **NOT_IMPLEMENTED** | STOP |
| Contextual Search (C2.4) | **IMPLEMENTED** | `ContextualSearchService` + tests |
| Grounded Assist (C2.5) | **IMPLEMENTED** | AssistContextBuilder + AssistantService F1 + tests |
| Protected DB migration | **NOT_IMPLEMENTED** | by design |