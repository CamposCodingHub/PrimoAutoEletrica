# C4 CONTINUOUS RUN FINAL — PRIMOX Intelligence

**Machine:** CIRO (de411c5d-4243-4085-bf33-8c3d241d7f2f)  
**Timezone:** America/Sao_Paulo (UTC-3)  
**START:** 2026-09-27 20:12:10 -03  
**END:** 2026-09-27 20:50:45 -03  
**Branch:** `cycle-c4/primox-intelligence`  
**Branch tip (pre-final-doc commit):** `1782e65481a4631df720bfecff1423bc184433a8`  
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` **UNTOUCHED**  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged (ReadOnly)**  

## Principle preserved
DATA→CONTEXT→EVIDENCE→RANKING→ASSIST→EXPLAIN→HUMAN DECISION.  
Never DATA→AI→AUTOMATIC ACTION. Suggestions only.

## Phase table

| Phase | Commit | Status |
|-------|--------|--------|
| FASE0 Baseline | (in dae5f3f) | PASS |
| C4.1 SourceTagged deepen | dae5f3f | PASS |
| C4.2 Evidence Ranking | 258d36b | PASS |
| C4.3 Diagnostic Reasoning | 45ba5e6 | PASS |
| C4.4 Client 360 | 068b2fc | PASS |
| C4.5 Vehicle 360 | 9ab7912 | PASS |
| C4.6 WorkOrder Intelligence | 74dc66b | PASS |
| C4.7 Knowledge Promotion | 5bf35fd | PASS |
| C4.8 Operational Recommendations | 51dd67a | PASS |
| C4.9 Explainability | 3d8c8c8 | PASS |
| C4.10 Evaluation Framework | c5390c7 | PASS |
| C4.11 Adversarial | 1782e65 | PASS |
| C4.12 Final Hardening | 6e91daf | PASS |

## Mandatory status table

| Area | Classification | Evidence / notes |
|------|----------------|------------------|
| Unit | **PASS** 655/655 | includes C4.1–C4.11; ExtAssist 51/51 |
| C4 unit slice | **PASS** 35/35 | filter FullyQualifiedName~C4 |
| Build Release | **PASS** 0 errors | QA_EVIDENCE/C4_CONTINUOUS/build_release.txt |
| UI Smoke Full App | **PASS** 219/219 | TestResults/UiSmoke/c4_cont_fullapp_2026-09-27_20-25-35 |
| Installed EXE (Local App) | **PASS** 13/13 intelligence filter | TestResults/UiSmoke/c4_cont_installed_2026-09-27_20-39-43 |
| Matrix 8 (resolutions×themes) | **NOT_APPLICABLE** / no visual change | Tema covered in installed filter |
| LIVE HTTP | **LIVE_NOT_TESTED** | keys ABSENT; never invent |
| Provider OFF | **PASS** | default |
| Provider ON | **NOT_TESTED** / **LIVE_NOT_TESTED** | keys ABSENT |
| Mock HTTP | **PASS** / **MOCK_ONLY** | ExtAssist unit carry-forward 51/51 |
| Grounding | **PASS** (unit) | EVAL-001 + diagnostic HYPOTHESIS-only |
| Hallucination | **PASS** (unit) | EVAL-008 + C4.11 |
| Context Isolation | **PASS** | CLIENT/VEHICLE/OS A vs B + ranking cross-client block |
| RBAC | **PASS** | finance gate + promotion PUBLISHER |
| Redaction | **PASS** | finance filtered without permission |
| Adversarial / prompt injection | **PASS** | 14/14 attack classes blocked |
| Kill-switch | **PASS** (carry-forward unit) | ExtAssist |
| Audit | **NOT_IMPLEMENTED** durable | in-memory only; no protected DB write |
| Soft FK honesty | **PASS** | RELATIONSHIP_NOT_PROVEN preserved |
| Data integrity | **PASS** | protected DB SHA unchanged |
| BUG-C3-LIVE-001 | no recurrence | Full App ExternalAi paths in 219/219 |
| CentsV1 / Design System | preserved | no money path change |

## Desktop / EXE

| Item | Value |
|------|-------|
| Shortcut | `OneDrive\Desktop\PRIMOX Workshop.lnk` → Local App EXE |
| Install dir | `%LOCALAPPDATA%\PrimoAutoEletrica\App` |
| EXE SHA-256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA-256 | `D2075BBC720F5B500C09E17EAF92A0C0B9C351C3649124A95FC22543C8AE6E55` |

## Bugs
None new (BUG-C4-XXX). BUG-C3-LIVE-001: no recurrence.

## NOT_TESTED / honesty
- LIVE HTTP / Provider ON with real key: **LIVE_NOT_TESTED**
- Durable audit persistence: **NOT_IMPLEMENTED**
- Explicit 8-cell resolution×theme matrix re-run: **NOT_APPLICABLE** (no visual change this cycle)

## Final classification
**PASS_WITH_EXTERNAL_DEPENDENCY**

Rationale: all local/unit/FullApp/EXE gates PASS; external LIVE remains LIVE_NOT_TESTED (keys ABSENT); durable audit NOT_IMPLEMENTED. Not production-ready for LIVE external Assist. origin/main untouched. Suggest-only intelligence stack C4.1–C4.12 delivered on `cycle-c4/primox-intelligence`.

