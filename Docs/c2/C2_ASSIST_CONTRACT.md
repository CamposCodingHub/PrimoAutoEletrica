# C2 Assist Contract — Proposed / Adapted

**Cycle:** C2.0 Discovery  
**Honesty:** Documents **current** types vs **proposed** C2 contract. No breaking rewrite in C2.0 (docs-first). Implementation deferred to C2.1+ with tests.

## 1. Current foundation (exists)

Source: `PrimoAutoEletrica/Models/AssistantContext.cs`

### AssistantQueryContext (current)

| Member | Type | Notes |
|--------|------|-------|
| Query | string | Required |
| Vehicle | AssistantVehicleContext? | Plate/Make/Model/Year/Category/Voltage |
| WorkOrder | AssistantWorkOrderContext? | Number/Symptom/Status/CurrentItems |
| Measurements | IReadOnlyList\<AssistantMeasurementContext\> | Parameter/value/unit/range/evaluation |
| RetrievedKnowledge | IReadOnlyList\<TechnicalKnowledgeEntry\> | Full domain entities today |
| RetrievedCases | IReadOnlyList\<DiagnosticCase\> | Full domain entities today |
| Parameters | Dictionary\<string, object\> | Escape hatch |

### AssistantResponse (current)

| Member | Type | Notes |
|--------|------|-------|
| AnswerMarkdown | string | Primary answer |
| Hypotheses | IReadOnlyList\<AssistantHypothesis\> | Title/ProbabilityRating/Rationale/tests |
| RecommendedActions | IReadOnlyList\<string\> | Closest to SuggestedNextSteps |
| CitedSources | IReadOnlyList\<AssistantSourceCitation\> | Closest to Evidence (partial) |
| ConfidenceLevel | AssistantConfidenceLevel | INSUFFICIENT_EVIDENCE/LOW/MEDIUM/HIGH |
| HasSufficientEvidence | bool | Derived |
| Disclaimers | string | Fixed consultive disclaimer |

### IAssistantProvider (current)

`ProviderId`, `DisplayName`, `IsConfigured`, `AskAsync(AssistantQueryContext)`.

Default implementation: `GroundedLocalRuleAssistantProvider` (`ProviderId = PRIMOX_LOCAL_GROUNDED`).

## 2. Proposed C2 contract (adapt, do not pretend it already exists)

### EvidenceItem (NEW — proposed)

Honest confidence naming — never call retrieval score “probability of fault” without measurement.

```text
EvidenceItem
  - EvidenceId: string/Guid
  - Kind: Knowledge | DiagnosticCase | Measurement | WorkOrderNote | Other
  - SourceCode: string          # e.g. KB-ELET-001 / CASE-...
  - Title: string
  - Excerpt: string            # minimum necessary quote
  - RelevanceScore: double?    # retrieval ranking only; nullable if unknown
  - RelevanceLabel: string     # e.g. "token-match", "manual-link" — NOT "fault probability"
  - ConfidenceContribution: AssistantConfidenceLevel  # how much this item supports the answer
  - Classification: TECHNICAL | OPERATIONAL | ...
```

Map from current `AssistantSourceCitation` + retrieved entities.

### AssistantResponse (ADAPTED — proposed fields)

Keep backward-compatible aliases where cheap; prefer explicit names below for new UI:

| Proposed field | Maps from current | Rule |
|----------------|-------------------|------|
| Answer | AnswerMarkdown | Required; empty only if fail-closed message |
| Evidence | CitedSources + structured EvidenceItem list | Empty ⇒ confidence cannot be HIGH |
| Warnings | (new; Disclaimers may seed) | Permission strips, OOD, stale data |
| MissingInformation | (new) | What to measure/collect next |
| SuggestedNextSteps | RecommendedActions | Checklist-like actions |
| Provider | (new) from IAssistantProvider.ProviderId/DisplayName | Always set |
| Timestamp | (new) UTC or local ISO | Always set by orchestrator |
| ConfidenceLevel | existing enum | Keep INSUFFICIENT_EVIDENCE naming |

Hypotheses may remain as an optional extension for technical copiloting; not required by the minimal C2 UI contract.

### AssistantQueryContext (ADAPTED — proposed)

- Keep current structural fields.  
- Add optional: `ClienteId` for authZ only (not for prompt dumping).  
- Add: `AllowedClasses` / permission snapshot.  
- Prefer passing **EvidenceItem candidates** from Retrieval instead of full mutable domain entities (reduces over-sharing).  

## 3. Fail-closed rules (contract-level)

| # | Condition | Response requirements |
|---|-----------|----------------------|
| F1 | No Evidence and no trusted domain rule | Confidence=INSUFFICIENT_EVIDENCE; Answer states insufficient evidence; MissingInformation non-empty; Evidence empty |
| F2 | Out of domain | Warning `OUT_OF_DOMAIN`; no FINANCIAL/PERSONAL enrichment |
| F3 | Financial asked w/o permission | Warning `FINANCIAL_PERMISSION_DENIED`; Answer refuses numbers; no leakage from 360 |
| F4 | Other client / missing authZ | Warning `CROSS_CLIENT_DENIED`; refuse; AuditLog |
| F5 | Provider not configured | Do not call network; same as F1 with Warning `PROVIDER_UNAVAILABLE` |
| F6 | Exception in provider | Catch at Assist layer; user-safe Answer; AuditLog error; no stack to UI beyond message |

## 4. Orchestrator responsibilities (`IAssistantService`)

1. Enforce `ASSIST_UTILIZAR`.  
2. Build Context via classification filter.  
3. Run Retrieval (C2.1+).  
4. Apply F1–F6 before/after provider.  
5. Stamp Provider + Timestamp.  
6. Audit consult (query hash / OS id / confidence — avoid logging raw PII).  

## 5. Gaps inventory (existing vs C2.1 needs)

| Item | Status |
|------|--------|
| IAssistantProvider | EXISTS |
| Grounded local provider | EXISTS + tests (`AssistFoundationTests`) |
| AssistantQueryContext / Response | EXISTS (partial vs proposed) |
| EvidenceItem | MISSING |
| Warnings / MissingInformation / Provider / Timestamp on response | MISSING |
| Dedicated ContextBuilder + Retrieval index | MISSING |
| DI registration of IAssistantService as single composition root | WEAK (UI `new AssistantService()`) |
| Cross-client / finance fail-closed in Assist | DESIGN ONLY (permission exists for Assist use; finance strip not Assist-specific) |
| OpenAI / remote provider | ABSENT (desired) |
| Dedicated Assist ViewModel | MISSING (code-behind in BaseConhecimento) |

## 6. Compatibility stance for C2.1

Prefer additive DTOs + mapper from current `AssistantResponse` → C2 view model over a big-bang rename. Update `AssistFoundationTests` when fields land. **Do not implement in C2.0.**
