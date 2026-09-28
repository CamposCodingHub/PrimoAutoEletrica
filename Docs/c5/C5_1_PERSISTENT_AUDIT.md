# C5.1 — Persistent Intelligence Audit

**Machine:** CIRO  
**Timezone:** America/Sao_Paulo (UTC-3)  
**Date:** 2026-09-28 06:23:33 -03  
**Branch:** `cycle-c5/primox-intelligence`  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged**  
**Keys:** ABSENT → LIVE_NOT_TESTED  

## Goal
Make intelligence decisions **durably auditable** following existing PRIMOX SQLite persistence (`AuditLogService` / `DatabaseService.Audit` / Microsoft.Data.Sqlite), not a random new DB layer. In-memory alone = NOT_IMPLEMENTED.

## Implementation

| Item | Detail |
|------|--------|
| Table | `IntelligenceAuditLogs` |
| Schema init | `PersistentIntelligenceAuditService.EnsureSchema` + hooked from `DatabaseService.InitializeAuditSchema` |
| Default file | `%LOCALAPPDATA%\PrimoAutoEletrica\intelligence-audit.db` (also creatable inside operational SQLite via EnsureSchema) |
| Service | `PersistentIntelligenceAuditService` (`IsDurable=true`) |
| In-memory retained | `IntelligenceAuditService` (`IsDurable=false`) for unit/compat |
| Interface | `IIntelligenceAuditService` + `FilterForReader` RBAC + `IsDurable` |
| Router | `AssistProviderRouter.Record` populates C5 justified fields + CorrelationId |

### Justified fields stored
AuditId, Timestamp, UserId, SessionId, Action, Provider, ProviderMode, ContextType, ContextId, CorrelationId, EvidenceCount, EvidenceIds, GroundingStatus, ResultStatus, FailureReason, DurationMs, FallbackUsed, SecurityDecision (+ minimized legacy summaries).

### Minimization (NOT stored raw)
API keys/tokens/raw auth, full prompts/responses, finance dumps. Secret-like strings → `[REDACTED]`.

### RBAC on read
- `Administrador` → all rows  
- Other non-empty profiles → own `UserId` only  
- Null/empty profile → empty result  

### Append-oriented
Public API is INSERT (`Record`) + read filters. No update/delete API for production use (`ClearForTests` test-only). Integrity tamper matrix → C5.2.

## CRITICAL persistence proof
**write → dispose/reopen new service instance on same SQLite file → read back**  
Test: `C51_CRITICAL_Write_DisposeReopen_ReadBack_FromSqlite` **PASS**

## Tests (Release)
| Suite | Result |
|-------|--------|
| C51PersistentIntelligenceAudit | **6/6 PASS** |
| ExtAssist + C51 + IntelligenceC26 filter | **67/67 PASS** |
| Build Release | **0 errors** |

## Truth labels
| Area | Label |
|------|-------|
| Durable audit persistence | **PASS** |
| In-memory-only durable claim | N/A (explicit `IsDurable=false`) |
| RBAC audit read | **PASS** (unit) |
| LIVE | **LIVE_NOT_TESTED** |
| Protected DB | **PASS** (SHA unchanged) |
| Audit integrity tamper | **NOT_TESTED** → C5.2 |

## Status
**C5.1 = PASS**