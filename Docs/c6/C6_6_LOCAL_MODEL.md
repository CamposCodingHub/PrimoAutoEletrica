# C6.6 LocalModelAssistantProvider

## Options
Endpoint, ModelId, Timeout, MaxTokens, Temperature, ContextLimit, Concurrency, RuntimeAvailable.

## Honesty
`RuntimeAvailable=false` (default) → IsConfigured=false → **ENVIRONMENT_DEPENDENCY** — no invented results.

## Status
**PASS** (unit contract). Live local runtime: **ENVIRONMENT_DEPENDENCY** / **NOT_TESTED**.