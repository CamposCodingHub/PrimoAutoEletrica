# C2 Architecture — Target Layered Design (Discovery)

**Cycle:** C2.0 Architecture & Discovery  
**Branch:** `cycle-c2/primox-intelligence`  
**Base commit:** `6ed757c` (C1.1.5)  
**Date (America/Sao_Paulo):** 2026-09-26  
**Status:** DESIGN ONLY — no C2.1 indexing/search/UI intelligence implemented in this gate.

## 1. Goal

Define a fail-closed, evidence-grounded Assist stack for PRIMOX Workshop Desktop so that:

- UI never talks to external LLM/OpenAI (or any remote IA) directly.
- Every Assist answer is built from local retrieval + explicit context + a swappable `IAssistantProvider`.
- Financial / personal / cross-client data stay behind permission and minimum-necessary gates.

## 2. Target layers (UI → Assist → Context → Retrieval → Store → Repo → SQLite)

```
┌─────────────────────────────────────────────────────────────┐
│ UI (WPF UserControls / Views / ViewModels)                  │
│  - BaseConhecimentoControl, OS, AutoElétrica, 360 windows   │
│  - Shows Answer / Evidence / Warnings / MissingInformation  │
│  - NEVER imports OpenAI SDK / HTTP LLM clients              │
└───────────────────────────┬─────────────────────────────────┘
                            │ IAssistantService / query DTO
┌───────────────────────────▼─────────────────────────────────┐
│ Assist orchestration                                        │
│  - Permission gate (ASSIST_UTILIZAR / ASSIST_CONFIGURAR)    │
│  - Fail-closed short-circuit (no data / OOD / finance /     │
│    other-client)                                            │
│  - AuditLog of consult attempts                             │
│  - Selects IAssistantProvider                               │
└───────────────┬─────────────────────────────┬───────────────┘
                │                             │
┌───────────────▼──────────────┐  ┌───────────▼───────────────┐
│ Context builder              │  │ Retrieval                 │
│  - Vehicle / OS / measures   │  │  - Knowledge + Cases      │
│  - Minimum-necessary fields  │  │  - Future: index (C2.1+)  │
│  - Classification filter     │  │  - Honest confidence map  │
└───────────────┬──────────────┘  └───────────┬───────────────┘
                │                             │
                └──────────────┬──────────────┘
                               │ AssistantQueryContext
┌──────────────────────────────▼──────────────────────────────┐
│ IAssistantProvider (IAProvider abstraction)                 │
│  - GroundedLocalRuleAssistantProvider (EXISTS, default)     │
│  - Future remote providers MUST sit here ONLY               │
│  - Contract: AskAsync(context) → AssistantResponse          │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│ Store façade (thin) → Repositories → DatabaseService/SQLite │
│  - KnowledgeRepository, OrdemServicoRepository, etc.        │
│  - Operational DB only (never protected baseline write)     │
└─────────────────────────────────────────────────────────────┘
```

### Hard rule: never UI → OpenAI

| Allowed | Forbidden |
|--------|-----------|
| UI → `IAssistantService` → `IAssistantProvider` | UI constructing HttpClient to OpenAI/Azure/Anthropic |
| Provider registered in DI / App services | Secrets or API keys in View/code-behind |
| Local grounded provider with zero network | Silent fallback that invents parts/prices |

**Observed today:** only `GroundedLocalRuleAssistantProvider` implements `IAssistantProvider`. No OpenAI provider class exists in `Services/` or `Models/`. Keep it that way until a later gated cycle explicitly adds a remote provider behind the same interface.

## 3. Mapping to code that already exists (honest)

| Layer | Existing today | Gap for C2.1+ |
|-------|----------------|---------------|
| UI | `UserControls/BaseConhecimentoControl` embeds Assist query box; menu `BaseConhecimento` | No dedicated Assist ViewModel; constructs `AssistantService` in code-behind |
| Assist | `Services/AssistantService` + `IAssistantService` | Token-split retrieval inline; no separate fail-closed policy module |
| Context | Types in `Models/AssistantContext.cs` (`AssistantQueryContext`, vehicle/OS/measurement DTOs) | No standalone ContextBuilder; OS→vehicle voltage heuristic is fragile |
| Retrieval | `IKnowledgeRepository.ObterArtigosAsync` / `ObterCasosAsync` by substring | No inverted index / FTS / ranking store |
| Provider | `IAssistantProvider`, `GroundedLocalRuleAssistantProvider` | No remote provider; response shape ≠ proposed C2 contract fields |
| Store/Repo | `KnowledgeRepository`, `OrdemServicoRepository`, … | Checklist/Diagnóstico live in JSON files, not SQLite |
| SQLite | C1 tables + core ERP tables | No new C2 tables in this gate; migration path documented only |

## 4. IAProvider abstraction

```csharp
// Already in Models/AssistantContext.cs
public interface IAssistantProvider
{
    string ProviderId { get; }
    string DisplayName { get; }
    bool IsConfigured { get; }
    Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken ct = default);
}
```

**Rules for any future provider:**

1. Must receive only an already-filtered `AssistantQueryContext`.
2. Must not open DB connections or bypass repositories.
3. Must set Provider identity on the response (proposed in `C2_ASSIST_CONTRACT.md`; not yet on current `AssistantResponse`).
4. Must degrade to INSUFFICIENT_EVIDENCE / fail-closed rather than hallucinate.

## 5. Fail-closed design notes (design only — not implemented as new code in C2.0)

| Condition | Expected Assist behavior |
|-----------|--------------------------|
| No retrieved knowledge/cases and no trusted domain heuristic | `INSUFFICIENT_EVIDENCE`; list MissingInformation; no invented cause |
| Query out of workshop domain | Explicit out-of-domain warning; empty Evidence |
| Financial fields without FINANCEiro / related permission | Strip financial metrics from Context; Warnings += permission |
| Request scoped to another Cliente without auth/permission | Refuse; audit; no cross-client leakage via 360 joins |
| Provider not configured | Do not call network; return local fail-closed response |

## 6. Schema change policy (if C2.1+ needs tables)

**Path:** DESIGN → SHADOW → REHEARSAL → GO/NO-GO → MIGRATION  

- **DESIGN:** document table/columns/FK in Docs/c2 (this cycle).  
- **SHADOW:** optional write-aside / dual-write on operacional DB only.  
- **REHEARSAL:** migrate copy of operacional; never protected `primoauto.db`.  
- **GO/NO-GO:** human gate + SHA check of protected baseline.  
- **MIGRATION:** only after GO; protected SHA must remain `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`.

**C2.0 action:** NO migration. NO new tables created.

## 7. Protected databases

| DB | Path | Role |
|----|------|------|
| Protected baseline | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` | READ-ONLY for agents; SHA locked |
| Operational | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto_operacional.db` | App runtime (separate SHA) |

C2.0 verified protected SHA match — see root `C2_0_DISCOVERY.md`.

## 8. Out of scope for C2.0 (STOP)

- C2.1 indexing / search ranking / FTS  
- New Assist UI intelligence panels  
- Remote LLM provider  
- Schema migrations  
- Changes to `main`
