# C6.5 Knowledge vs Model (A–G)

## Scenarios
| Id | Description |
|----|-------------|
| A | Rules (GroundedLocalRule) |
| B | Knowledge |
| C | Knowledge+Context |
| D | Model no PRIMOX ctx |
| E | Model+ctx |
| F | Model+evidence |
| G | Model+ctx+evidence+rules |

## Metrics (not opinion)
Runner returns Status + ProviderBenchmarkRecord + Evaluation per scenario.

## Environment honesty (this machine)
- A–C: executable via GroundedLocalRule → **PASS** / OBSERVATION_ONLY when measured
- D–G: no local runtime + keys ABSENT → **STUB_ONLY** / **MOCK_ONLY** — **no invented LIVE quality/latency**

## Status
**PASS** (harness). Model arms: **STUB_ONLY** / **LIVE_NOT_TESTED**.