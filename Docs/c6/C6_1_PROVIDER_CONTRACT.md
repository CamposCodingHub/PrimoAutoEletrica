# C6.1 Model Provider Contract — Benchmarking Metrics

**Machine:** CIRO (`de411c5d-4243-4085-bf33-8c3d241d7f2f`)  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Branch:** `cycle-c6/primox-intelligence`  
**Parent tip:** `5b690d0466d7e8983a191a4b8de73e7d5d023963` (C6.0)  
**origin/main:** `bf1eb784a3ed45782487197f38d9ba15d319997e` **UNTOUCHED**  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged**  
**Keys:** ABSENT → LIVE_NOT_TESTED

## Goal
Vendor-neutral provider benchmarking contract reused across LocalModel / ExternalModel / GroundedLocalRule — **no OpenAI lock-in**, no secrets, no full prompts by default.

## Deliverables
| Item | Path |
|------|------|
| `ProviderBenchmarkRecord` | `Services/Intelligence/ProviderBenchmarkRecord.cs` |
| `BenchmarkingAssistantProvider` decorator | `Services/Intelligence/BenchmarkingAssistantProvider.cs` |
| Unit tests | `Tests/.../C61ProviderBenchmarkContractTests.cs` |

## Contract fields
ProviderId, ModelId, ModelVersion, ExecutionMode, PromptTokens, CompletionTokens, TotalTokens, LatencyMs, TtftMs, Outcome (Success/Failure/…), FailureReason, GroundingStatus, EvidenceCount, QualityScore/Label, Metadata, EstimatedCost, CostStatus, Timestamp, CorrelationId, PromptFingerprint (SHA256 truncated — **not** full prompt), IncludesFullPrompt=false, IncludesSecrets=false.

## Honesty rules encoded
- `QualityScore` null + `QualityLabel=NOT_TESTED` until C6.4 evaluator runs
- `EstimatedCost` null + `CostStatus=PRICE_NOT_VERIFIED` until C6.2/C6.8 verify prices
- `TtftMs` null + metadata `TtftStatus=NOT_MEASURED` for non-streaming local path
- Never elevates NOT_TESTED→PASS or MOCK→LIVE

## Architecture note
Keeps existing `IAssistantProvider`. Decorator wraps any provider (GroundedLocalRule / LocalModel / External / Disabled / Future).

## Status
**PASS** (unit). LIVE external metrics: **LIVE_NOT_TESTED**.