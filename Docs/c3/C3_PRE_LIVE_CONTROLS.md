# C3 PRE-LIVE CONTROLS (before any real HTTP)

**Date:** 2026-09-27 (America/Sao_Paulo UTC-3)  
**Branch:** `cycle-c3/primox-intelligence`  
**Baseline HEAD:** `04cb4629c1b6555d0d68ac38109466db08b49702`  
**LIVE_EXTERNAL_PROVIDER:** **NOT_TESTED** (API keys ABSENT at start and re-check)

## Controls proven present

| Control | Where | Evidence |
|---------|-------|----------|
| Endpoint | `ExternalAssistantOptions.DefaultBaseUrl` = `https://api.openai.com/v1/chat/completions` | Options.cs; HTTPS only default |
| Timeout | `RequestTimeout` default 30s | Options.cs; CancelAfter in transport |
| Secret source | `IExternalAssistantSecretSource` / `EnvironmentExternalAssistantSecretSource` | env var name only in options (`PRIMOX_EXTERNAL_AI_API_KEY`); never repo file |
| Kill-switch | `PRIMOX_EXTERNAL_AI_KILL_SWITCH` OR options.KillSwitch | Selector.IsKillSwitchOn; provider returns EXTERNAL_KILL_SWITCH with **zero** HTTP |
| Redaction | `ExternalFinanceRedactor` | Finance classification + money patterns stripped by default |
| Grounding validator | `ExternalResponseGroundingValidator` | Rejects empty/hallucinated evidence IDs; strips dangerous actions |
| RBAC / permission gate | `includeFinancial=false` default; enable+secret+kill-switch arming | Finance evidence omitted unless explicit include |
| Logging | IntelligenceAudit + SanitizeException + TruncateSafe | No secrets in audit/exceptions/raw body |
| Error handling | Auth 401/403, timeout 408, malformed, network | Fail-closed AssistantResponse warnings |

## WHAT WILL BE SENT (when live armed)

- RequestId (correlation)
- Query text **after** finance redaction
- Model id (e.g. gpt-4o-mini)
- Evidence package: EvidenceId, SourceType, SourceId, Title, Excerpt (technical), Classification
- System/user instruction: cite evidence IDs only; no invent/finance/cross-client
- Authorization: Bearer header only (never body/logs/audit)

IDs that may appear when present in local retrieval: Knowledge/DiagnosticCase/Evidence SourceIds (e.g. D01–D17), WorkOrder number / vehicle make-model inside excerpts only.

## EXCLUDED (default)

- Financial amounts, prices, margins, DRE, salaries, contas a pagar/receber
- API keys / tokens / passwords
- Unnecessary PII (CPF/CNPJ/phone/address/email) — not in package builder fields
- Unrelated client contexts (isolation = only RetrievedEvidence for the query)
- Autonomous buy/approve/pay/fiscal/stock/price/delete actions

## Key re-check (this run)

```
OPENAI_API_KEY=ABSENT
PRIMOX_EXTERNAL_AI_KEY=ABSENT
PRIMOX_EXTERNAL_AI_API_KEY=ABSENT
```

**Decision:** Do **not** live-call. Complete local/control/MOCK_ONLY tests. Live subset = `LIVE_NOT_TESTED` / phase C3.13 = `PASS_WITH_EXTERNAL_DEPENDENCY`.
