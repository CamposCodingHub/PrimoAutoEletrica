# C2 Truth Matrix

**Branch:** `cycle-c2/primox-intelligence`  
**Updated:** 2026-09-26 (America/Sao_Paulo) — C2.11

| Capability | Status | Evidence |
|------------|--------|----------|
| Token Retrieval (deterministic index) | **IMPLEMENTED** | `DeterministicKnowledgeIndex` |
| OpenAI / remote LLM | **NOT_IMPLEMENTED** | by design |
| Embedding / Vector DB | **NOT_IMPLEMENTED** | by design |
| Knowledge Search UI | **IMPLEMENTED** | C2.2 |
| D01–D17 indexed + searchable | **IMPLEMENTED** | C2.1/C2.2 |
| RBAC financial filter on search | **IMPLEMENTED** | C2.2 |
| EvidenceItem on search hits | **IMPLEMENTED** | C2.1/C2.2 |
| Vehicle/Client/WorkOrder Context (C2.3) | **IMPLEMENTED** | Context Engine |
| Contextual Search (C2.4) | **IMPLEMENTED** | `ContextualSearchService` |
| Grounded Assist (C2.5) | **IMPLEMENTED** | Assist + F1 |
| 360 Intelligence overlay (C2.6) | **IMPLEMENTED** | `Intelligence360Enricher` |
| Diagnostic Intelligence (C2.7) | **IMPLEMENTED** | `DiagnosticIntelligenceService` |
| Knowledge Promotion (C2.8) | **IMPLEMENTED** | `KnowledgePromotionService` |
| Intelligence Audit (C2.9) | **IMPLEMENTED** | `IntelligenceAuditService` |
| Adversarial fail-closed (C2.10) | **IMPLEMENTED** | IntelligenceC26toC210Tests |
| UI polish intelligence (C2.11) | **IMPLEMENTED** | Search empty contextual message |
| Auto stock / OS / purchase actions | **NOT_IMPLEMENTED** | STOP |
| Protected DB migration | **NOT_IMPLEMENTED** | by design |