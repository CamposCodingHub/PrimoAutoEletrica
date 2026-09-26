# C2.2 Search — Deterministic PRIMOX Intelligent Search

**Branch:** `cycle-c2/primox-intelligence`  
**Gate:** C2.2 Intelligent Search UI  
**C2.3 started:** **NO**  
**Date (America/Sao_Paulo):** 2026-09-26

## Objective

"Buscar no PRIMOX" using C2.1 Knowledge Foundation: **normalize → retrieve → deterministic relevance → group → evidence → open source**. Never invent results. Not "IA" — deterministic retrieval only.

## Architecture

```
UI (PrimoxKnowledgeSearchControl / BaseConhecimento tab)
  → KnowledgeSearchViewModel
    → KnowledgeSearchService
      → KnowledgeRetrievalService
        → DeterministicKnowledgeIndex + KnowledgeItemAdapters / Repositories
```

No SQL in XAML/code-behind. Services registered in `DependencyInjection/ServiceExtensions.cs` (no scattered `new KnowledgeSearchService()` in feature UI beyond DI-fallback for early init).

## Contracts

| Type | Role |
|------|------|
| `KnowledgeSearchQuery` | Text, Filters, Limit, Offset, UserContext, HasFinancePermission (+ C2.1 filters) |
| `KnowledgeSearchMatch` | Id, Title, Summary, Type, SourceType, SourceId, Relevance, Evidence, Metadata |
| `KnowledgeSearchResponse` | State (NoQuery/Searching/NoResults/Found/Error), Groups, Results, timings |
| `KnowledgeSearchUserContext` | VehicleId/WorkOrderId stubs only — **no C2.3 VehicleContext** |

**Relevance** = retrieval ranking score. Never called "confidence" / fault probability.

## Normalization / tokenization

- Trim, case-insensitive, collapse spaces, strip diacritics (PT)
- Stop words (documented): de, da, do, das, dos, em, no, na, um, uma, os, as, ao, para, com, sem, por
- Technical tokens preserved: D01–D17, 12V, 24V, CAN, ABS, ECU (alphanumeric len≥2)

## Deterministic ranking (actual order)

Implemented in `DeterministicKnowledgeIndex` field weights + `KnowledgeQueryNormalizer.ExactPhraseBonus`:

1. Exact code match bonus (+6)
2. Field weight **code** (5)
3. Exact phrase in **title** (+4) / **tags** (+3) / **symptom** (+2.5) / **body** (+2)
4. Field weights: title/symptom (3) > tags (2.5) > system/diagnosis (2) > solution/model/vehicle (1.5) > text (1)
5. Tie-break: Code ascending, then Id ascending

## Grouping

Only non-empty groups: **CONHECIMENTO** / **DIAGNÓSTICOS** / **PROCEDIMENTOS** / **OUTROS**.

## RBAC

Service-layer filter: `KnowledgeType.PURCHASE`, `Classification=FINANCIAL`, SourceEntity Purchase/Financeiro denied without finance permission. Also `GetBySourceId` enforces the same. Unit tests cover both.

## UI

- Tab **Buscar no PRIMOX** inside `BaseConhecimentoControl`
- Textbox, Pesquisar, Enter, Limpar, loading/status, grouped result cards (title/type/summary/origin/relevance/open-source)
- States: no query / searching / no results / found / error
- `WindowOwnerHelper` for open-source dialogs

## Out of scope (STOP)

- C2.3 VehicleContext/ClientContext / auto stock/OS/purchase
- OpenAI / embeddings / vector DB / API keys
- Protected DB writes/migrations