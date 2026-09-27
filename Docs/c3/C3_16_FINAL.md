# C3.16 FINAL — Failover

**Decision:** **PASS**  
**Date:** 2026-09-27 America/Sao_Paulo

## Behavior

`AssistProviderRouter`: on external network/timeout/auth/malformed fail-closed → **local grounded** + warning `EXTERNAL_FALLBACK_LOCAL`.

## Test

`C316_ExternalFail_FallsBackToLocalGrounded` — MOCK_ONLY HTTP 500 → fallback local — PASS

Failover **exists** and was tested with mock transport (not live outage).
