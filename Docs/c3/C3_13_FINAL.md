# C3.13 FINAL — Controlled live

**Decision:** **PASS_WITH_EXTERNAL_DEPENDENCY** (live subset = **LIVE_NOT_TESTED**)  
**Date:** 2026-09-27 America/Sao_Paulo  
**LIVE call:** **NO** — API keys absent (re-checked). Did not invent live HTTP.

## Live subset

| Field | Value |
|-------|-------|
| LIVE_EXTERNAL_PROVIDER | NOT_TESTED |
| EXTERNAL_DEPENDENCY | API KEY AUSENTE |
| Endpoint called | NONE |
| HTTP status / latency / provider / model (live) | N/A — not called |

## Local / control / MOCK_ONLY completed

- Pre-live controls inventory
- Missing secret fail-closed (0 network)
- Kill-switch → ZERO external calls
- Invalid secret → controlled AuthFailed (**MOCK_ONLY**)
- Timeout / malformed paths (**MOCK_ONLY**)
- Hallucination matrix present/absent/partial/conflict (**MOCK_ONLY**)
- Context isolation A vs B
- RBAC finance include flag
- Redaction intercept
- Live classification honesty gate (`ExternalLiveCallGate`)

## Gates

| Check | Result |
|-------|--------|
| ExternalAssistant unit filter | 51 PASS (C3.0–C3.24 filter) |
| Protected SHA | IDENTICAL `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` |
| origin/main | untouched `bf1eb784…` |
| Secrets in repo | NONE |
