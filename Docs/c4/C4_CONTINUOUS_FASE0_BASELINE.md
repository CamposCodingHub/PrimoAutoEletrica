# C4 CONTINUOUS FASE0 BASELINE

**Machine:** CIRO (de411c5d-4243-4085-bf33-8c3d241d7f2f)
**Timezone:** America/Sao_Paulo (UTC-3)
**START:** 2026-09-27 20:12:10 -03
**Branch:** `cycle-c4/primox-intelligence`
**Tip HEAD:** `0d56cefbf36a3f17c784400639a8e9289a45daab`
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` (UNTOUCHED — never commit/push/merge/checkout main for work)
**Protected DB SHA256:** `c7420d1811d4cfea16ce833326c6a331f360bebf025ea7f3a7ee785192a7ce0b` (ReadOnly; rehash every phase)

## Kickoff verification

| Check | Expected | Actual | Status |
|-------|----------|--------|--------|
| Repo | C:\Projetos\PrimoAutoEletrica | OK | PASS |
| Prior tip | 0d56cefbf36a3f17c784400639a8e9289a45daab | 0d56cefbf36a3f17c784400639a8e9289a45daab | PASS |
| origin/main | bf1eb784a3ed45782487197f38d9ba15d319997e | bf1eb784a3ed45782487197f38d9ba15d319997e | PASS |
| Protected DB | C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B | c7420d1811d4cfea16ce833326c6a331f360bebf025ea7f3a7ee785192a7ce0b | PASS |
| Branch | cycle-c4/primox-intelligence from tip | cycle-c4/primox-intelligence | PASS |
| LIVE keys | ABSENT | ABSENT | LIVE_NOT_TESTED |
| Audit durable | NOT_IMPLEMENTED | NOT_IMPLEMENTED | NOT_IMPLEMENTED |

## Known C3 carry-forward (re-verify per impacted phase)

| Suite | C3 status | C4 note |
|-------|-----------|---------|
| Full App | 219/219 | Re-run on C4.12 hardening |
| Unit | 624/624 | Re-run after each phase BUILD |
| ExtAssist | 51/51 | Re-verify when External path touched |
| Matrix | 8/8 | Re-run if visual change |
| Provider OFF | PASS | Keep default |
| Provider ON | NOT_TESTED | LIVE_NOT_TESTED keys ABSENT |
| Audit | NOT_IMPLEMENTED | In-memory only; no protected DB write |
| BUG-C3-LIVE-001 | no recurrence | Watch External LIVE path |

## Principle

DATA→CONTEXT→EVIDENCE→RANKING→ASSIST→EXPLAIN→HUMAN DECISION.
Never DATA→AI→AUTOMATIC ACTION. Suggestions only.

## Soft FK honesty

Unproven soft FKs → `RELATIONSHIP_NOT_PROVEN`. Never invent client/vehicle/OS/case joins.

## Phase order (no skip)

C4.1 SourceTagged deepen → C4.2 Evidence Ranking → C4.3 Diagnostic Reasoning → C4.4 Client 360 → C4.5 Vehicle 360 → C4.6 WorkOrder Intelligence → C4.7 Knowledge Promotion → C4.8 Operational Recommendations → C4.9 Explainability → C4.10 Evaluation Framework → C4.11 Adversarial → C4.12 Final Hardening + `Docs/c4/C4_CONTINUOUS_RUN_FINAL.md`.

## Decision

**FASE0 BASELINE = PASS.** Proceed to deepen C4.1.
