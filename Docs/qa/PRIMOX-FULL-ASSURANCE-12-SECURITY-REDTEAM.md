# PRIMOX FULL ASSURANCE-12 — SECURITY RED TEAM

**Date:** 2026-09-11  
**HEAD baseline:** `175fac5`  
**Scope:** isolated QA only — no third-party / SEFAZ / production attacks.

## Harnesses

| Harness | Result | Evidence |
|---------|--------|----------|
| `Invoke-SecurityRedTeam.ps1` | PASS (decision YELLOW informational) | `TestResults/FullAssurance12/20260911-195635/Security/` |
| Smoke `A12Security` | **PASS 3/3** | authz backup gate, path canary reject, URI scheme gate |
| Unit `PathSecurityHelperTests` | PASS | |
| Unit `BackupAuthorizationTests` | PASS | |
| `Invoke-PathTraversalSimulation.ps1` | PASS | sandbox + junction created |

## Findings closed vs A11 REVIEW

| Topic | A11 | A12 |
|-------|-----|-----|
| AUTHENTICATION / plaintext verify | FIXED in A11 | Still verified via LoginSessao |
| AUTHORIZATION service boundary | REVIEW | **Hardened** for backup/restore (`*Authorized` + SISTEMA_CONFIGURAR) |
| PATH TRAVERSAL restore | REVIEW | **Hardened** (`PathSecurityHelper` jail on restore/create destino) |
| PROCESS START | REVIEW | **Hardened** critical open paths via `SecureProcessLauncher` |
| Secrets scan | SAFE | SAFE (tracked sources) |

## Residual / honest REVIEW

| Item | Why still REVIEW | Exploitability | Next step |
|------|------------------|----------------|-----------|
| Repository-level authz | Repositories remain session-agnostic by design | Requires caller bypass of UI+service | Continue expanding `*Authorized` on finance/employees if product policy requires defense-in-depth |
| Remaining `Process.Start` call sites | Not all migrated to SecureProcessLauncher | Mostly WhatsApp URL / PDF under appdata | Migrate remaining Ordens/Orcamentos/ImportarNFe opens |
| ZIP traversal | N/A (backup is raw SQLite `.db` copy) | — | Keep N/A until zip import exists |
| SecurityRedTeam script decision YELLOW | Heuristic SQL interpolation REVIEW count | Informational | Manual review samples already logged |

## Severity tally (A12 verified)

- CRITICAL: 0  
- HIGH: 0 newly confirmed exploitable  
- MEDIUM: 0 open product vulns after hardenings  
- LOW / INFORMATIONAL: residual defense-in-depth notes above  

**Security decision hint:** YELLOW (signing/env + residual Process.Start migration), not RED.
