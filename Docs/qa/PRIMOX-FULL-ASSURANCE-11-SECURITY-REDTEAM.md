# PRIMOX FULL ASSURANCE-11 — Security Red Team

**Date:** 2026-09-11  
**HEAD baseline:** `3aa03b8`  
**Scope:** Local/defensive only — no third-party, Focus, SEFAZ, or production attacks.

## Decision

**SECURITY DECISION: YELLOW**

- Critical verified exploits: **0**
- High product residual fixed this phase: legacy plaintext password verify → **rejected**
- Architectural REVIEW items remain (UI-centric authz, restore path privilege of admin user)
- Code signing commercial cert: **BLOCKED EXTERNAL** (not a product vuln)

## Findings

### SEC-001 — Secrets in Git

| Field | Value |
|-------|-------|
| Severity | Informational |
| Result | **PASS / SAFE** |
| Detail | No PFX / private keys / Focus tokens in tracked sources (refined scan) |

### SEC-002 — SQL injection

| Field | Value |
|-------|-------|
| Severity | Informational |
| Result | **PASS (static REVIEW complete)** |
| Detail | Parameterized queries dominate. `$""` usages are dialect/allowlist/identifier — no verified user-text concatenation into SQL. Isolated probe proved parameterized safety. |

### SEC-003 — Command injection (`Process.Start`)

| Field | Value |
|-------|-------|
| Severity | Low / REVIEW |
| Result | **SAFE for WhatsApp/wa.me + generated PDFs**; **REVIEW** for shell-open of stored document paths |
| Detail | `UseShellExecute` on DB-stored attachment paths — requires already-authorized local user |

### SEC-004 — Legacy plaintext password verification — **FIXED**

| Field | Value |
|-------|-------|
| Id | SEC-004 |
| Severity | **HIGH** (pre-fix) → mitigated |
| Category | AUTHENTICATION |
| File | `Services/PasswordHasherService.cs` |
| Precondition | DB row with non-`PBKDF2$` `Senha` |
| Before | `VerifyPassword` accepted `string.Equals` plaintext |
| After | Non-hashed storage **always returns false**; admin must reset |
| Test | `Seguranca.RecusarSenhaPlanaArmazenada` + LoginSessao / units |

### SEC-005 — Authorization depth

| Field | Value |
|-------|-------|
| Severity | Medium (defense-in-depth) |
| Classification | REVIEW / architectural |
| Detail | Most checks in UI/`NavigationService`; repos generally lack re-check. In-process bypass possible for a local attacker with code injection — **not** a remote unauthenticated exploit. Not changed this phase (scope: no large authz rewrite). |

### SEC-006 — Backup restore path

| Field | Value |
|-------|-------|
| Severity | Low / REVIEW |
| Detail | Restore accepts path after `GetFullPath` without jail to backup folder; gated by `SISTEMA_CONFIGURAR`. Invalid/corrupt restore tested via harness expectations — app should reject bad files. |

## Dynamic auth (VERIFIED via harness)

| Test | Result |
|------|--------|
| LoginSessao (invalid/valid/lockout messaging) | **PASS** |
| QaEngine (includes session flows) | **PASS 43/43** |

## Counts

| Sev | Count |
|-----|-------|
| Critical | 0 |
| High (open) | 0 (1 fixed) |
| Medium | 1 REVIEW (authz depth) |
| Low | 2 REVIEW |
| Informational | several SAFE |

## Agents

- `Scripts/QA/Invoke-SecurityRedTeam.ps1`
- Explore static audit (auth/SQL/Process)
