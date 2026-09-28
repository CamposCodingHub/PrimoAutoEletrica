# C5.3 — CorrelationId End-to-End

**Date:** 2026-09-28 -03  
**Branch:** `cycle-c5/primox-intelligence`  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged**

## Chain
User/Session → Assist (Router) → Context (`AssistantQueryContext.CorrelationId`) → Evidence (`ExternalEvidencePackage.RequestId`) → Provider payload RequestId → Grounding (same request) → Response (`AssistantResponse.CorrelationId`) → Audit (`IntelligenceAuditEntry.CorrelationId`)

## Implementation
- `AssistantQueryContext.CorrelationId` / `AssistantResponse.CorrelationId`
- Router bootstraps CorrelationId (context → Parameters → generate)
- `ExternalEvidencePackageBuilder` uses context CorrelationId as RequestId
- Durable audit stores CorrelationId; Filter by correlationId
- REQUEST-A vs REQUEST-B isolation proven in unit tests

## Tests
C53: 4/4 PASS (+ C51/C52 regression → 16/16)

## Status
**C5.3 = PASS**