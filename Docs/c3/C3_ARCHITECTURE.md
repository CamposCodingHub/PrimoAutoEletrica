# C3 Architecture — External AI Provider (Grounded, Fail-Closed)

**Cycle:** C3  
**Branch:** `cycle-c3/primox-intelligence`  
**Status:** Foundation — stub + contracts; live remote **OFF by default**

## Layers (unchanged from C2)

UI → Assist orchestration → Context → Retrieval → Store/Repo/SQLite  
**New:** Assist may select an `IAssistantProvider` that is either local grounded or external (disabled stub until explicitly armed).

## Components (C3.0)

| Type | Responsibility |
|------|----------------|
| `ExternalAssistantOptions` | Explicit `Enabled` (default false), `ApiKeyEnvironmentVariable`, optional base URL placeholder, provider display id |
| `IExternalAssistantSecretSource` | Resolve secret from env/user store; never from repo files |
| `EnvironmentExternalAssistantSecretSource` | Reads named env var only |
| `ExternalAssistantProviderSelector` | Applies kill-switch + enable + secret → returns `IAssistantProvider` |
| `DisabledExternalAssistantProvider` | `IsConfigured=false`; `AskAsync` returns fail-closed response (no network) |
| `ExternalAssistantWarnings` | Stable warning codes for UI/tests |

## Selection algorithm

1. If kill-switch → Disabled (`EXTERNAL_KILL_SWITCH`)
2. Else if not `Enabled` → Disabled (`EXTERNAL_DISABLED`)
3. Else if secret missing/blank → Disabled (`EXTERNAL_NO_KEY`)
4. Else → **still return Disabled in C3.0** with Warning `EXTERNAL_LIVE_NOT_WIRED` (live client deferred; proves arming gates without shipping network)
5. Future C3.x: step 4 returns live provider implementing `IAssistantProvider`

Step 4 is intentional honesty: C3.0 proves the **path and fail-closed gates** without enabling live OpenAI.

## Config surface

- Prefer options object / future settings JSON **without secret values**.
- Env: `PRIMOX_EXTERNAL_AI_API_KEY`, `PRIMOX_EXTERNAL_AI_KILL_SWITCH`, optional `PRIMOX_EXTERNAL_AI_ENABLED=1` for night-run style labs only.
- Default process config: Enabled=false even if key exists (must set Enabled explicitly OR env enable for labs).

## Safety invariants

1. No OpenAI package reference in csproj for C3.0.
2. No hardcoded sk- keys.
3. CentsV1 / Design System / protected DB untouched.
4. External never auto-executes OS/fiscal/money mutations.
