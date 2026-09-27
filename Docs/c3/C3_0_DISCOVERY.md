# C3.0 Discovery — External AI Provider Path (Grounded)

**Branch:** `cycle-c3/primox-intelligence`  
**Base:** `ee39fad` (C2 Night Run final)  
**Gate:** C3.0 Architecture & Discovery + fail-closed stub foundation  
**Live OpenAI default:** **OFF**  
**API keys in repo:** **FORBIDDEN**  
**Autonomous dangerous actions:** **FORBIDDEN**  
**Invented evidence:** **FORBIDDEN**

## Decision target

Prepare a **GROUNDED** external provider path that plugs into existing `IAssistantProvider` **without** enabling live remote calls by default. C3.0 delivers docs + contracts + disabled/null provider + config surface + tests proving fail-closed.

## Inventory (current, real)

| Piece | Location | Status |
|-------|----------|--------|
| `IAssistantProvider` | `Models/AssistantContext.cs` | EXISTS (`ProviderId`, `DisplayName`, `IsConfigured`, `AskAsync`) |
| Local grounded provider | `Services/GroundedLocalRuleAssistantProvider.cs` | EXISTS — default |
| Orchestrator | `Services/AssistantService.cs` / `IAssistantService` | EXISTS — fails closed via `AssistFailClosedPolicy` |
| Fail-closed F1–F5 | `Services/Knowledge/AssistFailClosedPolicy.cs` | EXISTS (F5 = PROVIDER_UNAVAILABLE) |
| Evidence / Warnings / MissingInformation | `AssistantResponse` | EXISTS (C2.1+) |
| Intelligence audit | `Services/Knowledge/IntelligenceAuditService.cs` | EXISTS (C2.9, in-memory, redacts secrets) |
| OpenAI / HTTP LLM client | — | **ABSENT** |
| External provider | — | **ABSENT** (this cycle) |
| API key storage | — | must be env / user secret store only |

## How ExternalProvider plugs in

```
UI ──► IAssistantService (AssistantService)
          │  RBAC ASSIST_UTILIZAR / ASSIST_CONFIGURAR
          │  Context + Retrieval (local only)
          │  AssistFailClosedPolicy F1–F6
          │  IntelligenceAuditService.Record (redacted)
          ▼
     IAssistantProvider  ◄── selected by ExternalAssistantProviderSelector
          │
          ├── GroundedLocalRuleAssistantProvider (default, always available)
          ├── DisabledExternalAssistantProvider (IsConfigured=false; fail-closed)
          └── (FUTURE) LiveExternalAssistantProvider — ONLY if:
                explicit enable + secret present + kill-switch OFF + evidence required
```

**Rule:** UI never imports OpenAI SDK / HTTP LLM clients. Only a provider behind `IAssistantProvider` may talk to the network, and only after gates pass.

## Evidence requirements

1. External answers MUST carry non-empty `Evidence` / `CitedSources` mapped from **local** retrieval candidates passed in `AssistantQueryContext` — remote model may **rephrase/rank**, never invent sources.
2. If local retrieval is empty → F1 INSUFFICIENT_EVIDENCE (do not call remote).
3. If remote returns claims without mappable evidence → strip to fail-closed / LOW with Warnings `EXTERNAL_UNGROUNDED`.
4. Never present remote confidence as fault probability.

## RBAC

| Permission | Role |
|------------|------|
| `ASSIST_UTILIZAR` | Consult (local or external if enabled) |
| `ASSIST_CONFIGURAR` | Toggle enable / inspect provider status (no raw key display) |
| Finance / cross-client | Existing F3/F4 remain mandatory before any provider |

## Audit (C2.9)

- Every external consult attempt records Provider id, Result, Failure, MissingEvidence.
- Redaction bans `api_key`, `token`, `bearer`, `password`, `secret`.
- No protected DB write for audit in C3.

## Kill-switch

Env `PRIMOX_EXTERNAL_AI_KILL_SWITCH=1` (or `true`/`yes`) forces Disabled provider regardless of config enable + secret.

## No-key-in-repo

- Config stores **env var name** only (default `PRIMOX_EXTERNAL_AI_API_KEY`).
- `.gitignore` blocks `.env`, `**/secrets/`, `*openai*key*`, local override JSON with secrets.
- Unit tests assert Disabled/fail-closed when env unset; assert selector never embeds key literals.

## Fail-closed matrix (C3)

| Condition | Behavior |
|-----------|----------|
| `Enabled=false` (default) | Disabled provider; Warning `EXTERNAL_DISABLED` |
| Enabled but no secret | Disabled; Warning `EXTERNAL_NO_KEY` |
| Kill-switch on | Disabled; Warning `EXTERNAL_KILL_SWITCH` |
| Enabled + key but no local evidence | Do not call remote; F1 |
| Exception / network | F6 user-safe; audit Failure |
| Hallucinated action proposals without evidence | Refuse / strip RecommendedActions to safe collect-data steps |

## Out of scope for C3.0 foundation

- Live OpenAI HTTP calls
- Embedding / vector DB
- Autonomous OS mutations / fiscal / money actions from AI
- Committing API keys or enabling external by default
