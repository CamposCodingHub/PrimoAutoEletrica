# C3 CONTINUOUS HARDENING NOTES

**Date:** 2026-09-27 10:01:23 -03:00  
**Branch:** `cycle-c3/primox-intelligence`  
**HEAD (start):** `b8d21cbec2b02bb5257eac2f052d8afc70783c41`

## Persistent audit

| Claim | Truth |
|-------|-------|
| Create/Read/Filter/Auth/Retention | PASS (unit C3.14) |
| Secrets redacted | PASS (`[REDACTED]`) |
| Protected DB write | **NONE** |
| Durable/persistent across process | **NO** — in-memory ring MaxEntries=500 |

**Honest label:** Audit = process-lifetime memory. Do **not** claim persistent durable store.

## Data minimization / redaction

| Control | Status |
|---------|--------|
| ExternalFinanceRedactor | PASS (unit) |
| ExternalPayloadMinimizationAuditor | PASS (C3.15) |
| Evidence package excludes finance by default | PASS |
| Live payload minimization under real provider | LIVE_NOT_TESTED |

## Observability metrics separation

| Channel | How labeled | Measured this run |
|---------|-------------|-------------------|
| LOCAL | Assist origin local / grounded | Unit + UiSmoke |
| MOCK | MOCK_ONLY tests / injected transport | Unit C3.13/16/18/19/23 |
| LIVE | Would require armed key | LIVE_NOT_TESTED — **no invented latency/tokens/cost** |

## Timeout / retry / circuit

| Control | Behavior |
|---------|----------|
| HTTP timeout | 30s CancelAfter — EXTERNAL_TIMEOUT |
| Infinite retry | **Absent** (no Polly loop) |
| Failure path | AssistProviderRouter → local grounded fallback (EXTERNAL_FALLBACK_LOCAL) |
| Circuit breaker product | **Not implemented as named circuit** — fail-closed + single-attempt + local fallback |

## UI provider states (installed EXE)

| State | Status |
|-------|--------|
| Provider OFF / disabled | PASS (default + UiSmoke ExternalAiSettings history) |
| Provider ON without key | ReadyNoKey / fail-closed — unit PASS; live Assist NOT_TESTED |
| Unavailable / loading / success / error / timeout / refusal (live ON) | NOT_TESTED (key ABSENT) |

## Resolutions × Light/Dark

| Resolution | Light | Dark |
|------------|-------|------|
| 1280x720 | NOT_TESTED (this continuous) | NOT_TESTED |
| 1366x768 | NOT_TESTED (this continuous) | NOT_TESTED |
| 1600x900 | NOT_TESTED (this continuous) | NOT_TESTED |
| 1920x1080 | NOT_TESTED (this continuous) | NOT_TESTED |

Note: Tema Light/Dark modules covered in intelligence UiSmoke (Tema:ClaroEscuroModulosPrincipais). Explicit 4-resolution matrix = NOT_TESTED this cycle (prior preflight had QaEngine resolutions on 2026-09-26 — not reclaimed as this continuous PASS).

## Bugs

| ID | Status |
|----|--------|
| BUG-C3-LIVE-001 ExternalAiSettingsWindow DialogResult | FOUND → FIXED → RETESTED (Full App 219/219 prior); history retained |
