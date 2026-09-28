# C6.7 External Model Adapter (experimental)

OpenAI-compatible first — **not** a vendor lock-in of the architecture. Reuses secret-source pattern; never logs keys.

## Tests without key
ForceMock → **MOCK_ONLY**; no key → **LIVE_NOT_TESTED**.

## Status
**PASS** (mock/contract). LIVE: **LIVE_NOT_TESTED** (keys ABSENT).