# C3 External Provider Contract

## IAssistantProvider (reuse)

Existing interface — external implementations MUST honor:

- `IsConfigured` false ⇒ orchestrator must not treat provider as live; AskAsync still safe (fail-closed).
- Never throw secrets in exceptions.
- Prefer returning `AssistantResponse` with `Provider`, `Warnings`, `MissingInformation`, `Evidence`.

## Warning codes

| Code | Meaning |
|------|---------|
| `EXTERNAL_DISABLED` | Feature flag off (default) |
| `EXTERNAL_NO_KEY` | Enabled but secret unresolved |
| `EXTERNAL_KILL_SWITCH` | Kill-switch engaged |
| `EXTERNAL_LIVE_NOT_WIRED` | Gates passed but live HTTP client not shipped yet |
| `EXTERNAL_UNGROUNDED` | (future) remote output lacked mappable evidence |
| `PROVIDER_UNAVAILABLE` | Legacy F5 alias |

## AskAsync contract for DisabledExternalAssistantProvider

- Confidence = `INSUFFICIENT_EVIDENCE`
- Answer explains external path unavailable / not armed
- RecommendedActions = safe data-collection only (no invented repair steps)
- Evidence empty
- Provider = `PRIMOX_EXTERNAL_DISABLED`
