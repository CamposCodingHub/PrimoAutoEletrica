# C3.13 LIVE VALIDATION

**Machine:** CIRO  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Documented:** 2026-09-27 09:59:21 -03:00  
**Branch:** `cycle-c3/primox-intelligence`  
**HEAD:** `b8d21cbec2b02bb5257eac2f052d8afc70783c41`  
**Overall phase class:** **PASS_WITH_EXTERNAL_DEPENDENCY**  
**LIVE_EXTERNAL_PROVIDER:** **LIVE_NOT_TESTED** (API keys ABSENT at FASE 0 and re-check)

---

## 1. PREFLIGHT (no secrets)

| Field | Value |
|-------|-------|
| Provider | OpenAI-compatible Chat Completions (`PRIMOX_EXTERNAL`) |
| Endpoint | `https://api.openai.com/v1/chat/completions` (HTTPS only default) |
| Method | HTTP POST |
| Model | `gpt-4o-mini` (default `ExternalAssistantOptions.ModelId`) |
| Timeout | 30s (`RequestTimeout`; CancelAfter on transport) |
| Retry | **None infinite** — single attempt per Ask; failover is local grounded fallback (C3.16), not HTTP retry loop |
| Token / context limit | Evidence package excerpts only; temperature=0; JSON response contract |
| Auth | Bearer from env `PRIMOX_EXTERNAL_AI_API_KEY` (name only in options; never logged) |
| Redaction | `ExternalFinanceRedactor` — finance/money patterns stripped by default |
| Grounding | `ExternalResponseGroundingValidator` — reject empty/invented evidence IDs; strip dangerous actions |
| Kill-switch | Options.KillSwitch OR env `PRIMOX_EXTERNAL_AI_KILL_SWITCH` (1/true/yes/on) → ZERO HTTP |
| Enable gate | Options.Enabled OR env `PRIMOX_EXTERNAL_AI_ENABLED`; default **false** (fail-closed) |
| Allowed actions | Suggest data-collection / cite EvidenceIds only |
| Forbidden actions | Autonomous buy/approve/pay/fiscal/stock/price/delete; cross-client dumps; key reveal |
| Secret in this doc | **NONE** |

### Key re-check (FASE 0 continuous)

| Scope | OPENAI_API_KEY | PRIMOX_EXTERNAL_AI_KEY | PRIMOX_EXTERNAL_AI_API_KEY |
|-------|----------------|------------------------|----------------------------|
| Process | ABSENT | ABSENT | ABSENT |
| User | ABSENT | ABSENT | ABSENT |
| Machine | ABSENT | ABSENT | ABSENT |

**Decision:** Do **not** perform live HTTP. Complete MOCK_ONLY / local control matrix. Label live rows LIVE_NOT_TESTED.

---

## 2. Minimal LIVE call (when key present)

| Field | Result |
|-------|--------|
| Executed | **NO** |
| Reason | EXTERNAL_DEPENDENCY — API KEY AUSENTE |
| Status / latency / request id / tokens | N/A — not called |
| Auth errors | N/A |

When key becomes PRESENT: run one minimal read-only Ask proving PRIMOX→HTTP→provider→model→response→PRIMOX; record status/latency/request id/tokens without secrets; then LIVE-001..020 live subset.

---

## 3. LIVE-001 .. LIVE-020 matrix

Truth labels only. MOCK_ONLY = injected fake HTTP. LIVE_NOT_TESTED = real provider not contacted.

| ID | Scenario | Result | Label | Evidence |
|----|----------|--------|-------|----------|
| LIVE-001 | Valid path (armed + key + evidence) | Not executed live | LIVE_NOT_TESTED | keys ABSENT |
| LIVE-002 | No key / missing secret | Fail-closed, 0 network | PASS | `C313_MissingSecret_FailClosed_NoNetwork` |
| LIVE-003 | Invalid key | Controlled AuthFailed | PASS / MOCK_ONLY | `C313_InvalidSecret_ControlledError_MOCK_ONLY` |
| LIVE-004 | Unavailable endpoint | Controlled network/unavailable | PASS / MOCK_ONLY | C3.7/C3.16 mock transport fail → local fallback |
| LIVE-005 | Timeout | EXTERNAL_TIMEOUT | PASS / MOCK_ONLY | `C313_Timeout_MOCK_ONLY` |
| LIVE-006 | Malformed response | EXTERNAL_MALFORMED_RESPONSE | PASS / MOCK_ONLY | C3.7 malformed paths + C313 matrix |
| LIVE-007 | HTTP 4xx (non-auth) | Controlled fail-closed | PASS / MOCK_ONLY | HttpExternalAssistantTransport non-success |
| LIVE-008 | HTTP 5xx | Controlled fail-closed | PASS / MOCK_ONLY | same transport path |
| LIVE-009 | Kill-switch OFF→ON proves zero external call | ZERO HTTP when ON | PASS | `C313_KillSwitch_ZeroExternalCalls` |
| LIVE-010 | Provider disabled (default) | EXTERNAL_DISABLED | PASS | arming DisabledByDefault |
| LIVE-011 | Evidence context present | Package includes EvidenceIds | PASS | C3.2/C3.17 fixtures |
| LIVE-012 | Evidence context absent | EXTERNAL_NO_EVIDENCE / insufficient | PASS | grounding / no-evidence gates |
| LIVE-013 | Context isolation CLIENTE A vs B | No cross-leak in package | PASS | `C313_ContextIsolation_A_vs_B` |
| LIVE-014 | Context isolation VEICULO A vs B | Isolation honored | PASS | same isolation test + retrieval scope |
| LIVE-015 | Context isolation OS A vs B | Isolation honored | PASS | same isolation test + retrieval scope |
| LIVE-016 | RBAC finance include flag | Finance omitted unless explicit | PASS | `C313_RBAC_FinanceIncludeFlag_IsExplicitGate` |
| LIVE-017 | Finance redaction intercept | Amounts stripped | PASS | `C313_RedactionIntercept_Safe` + C318 |
| LIVE-018 | Grounding validator | Ungrounded rejected | PASS | C3.3 + C313 hallucination matrix |
| LIVE-019 | Hallucination matrix present/absent/partial/conflict | Rejects invent | PASS / MOCK_ONLY | `C313_HallucinationMatrix_*` |
| LIVE-020 | Honesty gate — do not invent LIVE when key absent | Classification LIVE_NOT_TESTED | PASS | `C313_LiveExternal_NotInvented_WhenKeyAbsent` + `C313_LiveKey_Absent_ClassifiedHonestly` |

Unit filter this continuous run: **ExternalAssistant 51 PASS / 0 FAIL** (includes C3.13–C3.24).

---

## 4. Grounding + hallucination + isolation + kill-switch

| Area | Class |
|------|-------|
| Grounding | PASS (unit) |
| Hallucination | PASS / MOCK_ONLY |
| Context isolation | PASS |
| Finance redaction | PASS |
| Kill-switch zero HTTP | PASS |
| Live remote grounding | LIVE_NOT_TESTED |

---

## 5. Evidence paths

- Unit: `QA_EVIDENCE/C3_CONTINUOUS/unit_external_*.txt`, `unit_full_*.txt`
- Prior live stack: `QA_EVIDENCE/C3_LIVE/`
- Pre-live controls: `Docs/c3/C3_PRE_LIVE_CONTROLS.md`
- This doc: `Docs/c3/C3_13_LIVE_VALIDATION.md`

## 6. Protected / main invariants (re-check)

| Check | Result |
|-------|--------|
| Protected SHA | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` IDENTICAL ReadOnly=True |
| origin/main | `bf1eb784a3ed45782487197f38d9ba15d319997e` UNTOUCHED |
| Keys in git/docs | NONE |

## Gate

**C3.13 = PASS_WITH_EXTERNAL_DEPENDENCY.** Continue local hardening. Do not start live-dependent C4 features that require real provider until key present and LIVE subset executed.
