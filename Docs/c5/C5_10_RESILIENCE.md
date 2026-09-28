# C5.10 — Failure/Fallback Resilience
External fail → local FALLBACK. RequestTimeout finite (default 30s, ≤2min). No crash; no infinite retry in router (≤3 transport attempts observed); no false EXTERNAL success; no autonomous action.
**Status: PASS** (unit)