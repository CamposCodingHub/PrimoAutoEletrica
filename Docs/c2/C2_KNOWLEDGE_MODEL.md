# C2 Knowledge Model — Operational Knowledge Foundation (C2.1)

**Cycle:** C2.1  
**Branch:** `cycle-c2/primox-intelligence`  
**Date (America/Sao_Paulo):** 2026-09-26  
**Status:** IMPLEMENTED (retrieval layer; no protected DB migration; no OpenAI)

## 1. Purpose

Provide a unified **retrieval** model over existing workshop knowledge sources without replacing domain entities (`TechnicalKnowledgeEntry`, `DiagnosticCase`, `OrdemServico`, roteiros Auto Elétrica).

## 2. KnowledgeType

| Type | Meaning | Primary source (C2.1) |
|------|---------|------------------------|
| TECHNICAL_CASE | Artigo/boletim técnico | `TechnicalKnowledgeEntries` |
| DIAGNOSTIC_CASE | Caso real | `DiagnosticCases` |
| VEHICLE_HISTORY | Histórico veículo | reserved (adapter-ready) |
| WORK_ORDER | Snapshot OS | `OrdensServico` (optional, top 50) |
| CHECKLIST | Checklist | reserved (JSON files — not invented) |
| PROCEDURE | Roteiro/procedimento | D01–D17 + biblioteca técnica |
| SYMPTOM / CAUSE / SOLUTION | Facetas | reserved for future split |
| MEASUREMENT | Medição | reserved |
| PART | Peça | reserved |
| PURCHASE | Compra | reserved (FINANCIAL class) |
| OTHER | Fallback | — |

## 3. KnowledgeItem

Retrieval DTO fields: `ItemId`, `Type`, `Code`, `Title`, `System`, `Symptom`, `Diagnosis`, `Solution`, `VehicleModel`, `VehiclePlate`, `WorkOrderNumber`, `Tags`, `BodyText`, `SourceEntity`, `SourceEntityId`, `UpdatedAt`, `DedupKey`.

**Rule:** adapters map → index; domain entities remain source of truth.

## 4. Deterministic indexing (NO embeddings)

`DeterministicKnowledgeIndex`:

- Tokenize + strip diacritics (PT)
- Postings by field: `code`, `title`, `symptom`, `tags`, `system`, `diagnosis`, `solution`, `model`, `vehicle`, `text`
- Weighted score (code > symptom/title > tags > … > text)
- Upsert by `DedupKey` → no improper duplicates
- Provider id: `PRIMOX_DETERMINISTIC_INDEX`

## 5. KnowledgeSearchResult

`Query`, `Hits[]` (`Item`, `Score`, `MatchLabel=token-match`, `MatchedFields`, `Excerpt`), `IndexedItemCount`, `Timestamp`, `Provider`, `Warnings`.

## 6. D01–D17

Indexed from `AutoEletricaTecnicaService.CreateRoteirosDiagnostico()` as `PROCEDURE`.  
**Invariant:** 17/17 distinct codes D01…D17; none lost; upsert prevents duplicates.

## 7. Assist contract additives

- `EvidenceItem` (RelevanceScore = retrieval only; RelevanceLabel ≠ fault probability)
- `AssistantResponse`: `Evidence`, `Warnings`, `MissingInformation`, `Provider`, `Timestamp`, aliases `Answer` / `SuggestedNextSteps`
- Mapper: `AssistantContractMapper.Enrich`
- Fail-closed stubs: F1–F4 in `AssistFailClosedPolicy`

## 8. Schema policy

**No new table migrated on protected DB.**  
If a dedicated shadow index table is needed later: DESIGN → SHADOW → REHEARSAL → GO/NO-GO → MIGRATION.

## 9. Code map

| Piece | Path |
|-------|------|
| Models | `Models/KnowledgeItem.cs`, `Models/AssistantContext.cs` |
| Index | `Services/Knowledge/DeterministicKnowledgeIndex.cs` |
| Adapters | `Services/Knowledge/KnowledgeItemAdapters.cs` |
| Retrieval | `Services/Knowledge/KnowledgeRetrievalService.cs` |
| Fail-closed | `Services/Knowledge/AssistFailClosedPolicy.cs` |
| Context | `Services/Knowledge/AssistContextBuilder.cs` |
| Mapper | `Services/Knowledge/AssistantContractMapper.cs` |
| Tests | `Tests/.../KnowledgeRetrievalTests.cs` |
