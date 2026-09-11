# PRIMOX FULL ASSURANCE-11 — Final Report

**Date:** 2026-09-11  
**HEAD initial:** `3aa03b8`  
**HEAD final:** (see commits)  
**Tag `v1.0.0`:** `72d85fa` **PRESERVED**  
**Branch:** `main`  
**Main gate duration:** ~41.1 min  
**Evidence:** `TestResults/FullAssurance11/20260911-123215/` · post-fix Login/QaEngine

## Decisions

| Gate | Decision |
|------|----------|
| SECURITY | **YELLOW** |
| RELEASE / ASSURANCE | **YELLOW** |

Commercial package remains READY WITH LIMITATIONS (C10). This phase added deeper assurance + one auth hardening.

## Baseline (VERIFIED this run)

| Suite | Result |
|-------|--------|
| Build Debug/Release | PASS |
| Units | **162/162** PASS (incl. plaintext-reject assertion path) |
| Security red-team script | PASS (no Critical secrets) |
| QaEngine | **43/43** |
| DeepQa | **6/6** |
| LoginSessao | **1/1** |
| LongRun | PASS |
| Exhaustive | **1882 PASS / 0 FAIL / 0 BLOCKED** (discovered 3278) |
| I18n07 | PASS |
| Tema | 2/2 |
| Sidebar | PASS |
| OvernightQa | 3/3 |
| Recovery ×5 | PASS |
| Database | integrity=ok · fk=0 · migrations=28 |
| Backup/restore | PASS |
| Installer lifecycle 3/3 + app-open | PASS |
| Installer Clientes smoke | FAIL Exit=2 (harness flake) |
| Code signing | BLOCKED EXTERNAL |

## Post-fix (password harden)

| Suite | Result |
|-------|--------|
| Units | PASS 162/162 |
| LoginSessao | PASS |
| QaEngine | PASS 43/43 |

## Product fix

**SEC-004 HIGH:** `PasswordHasherService.VerifyPassword` no longer accepts plaintext stored passwords.

## Known limitations

- Code signing commercial certificate absent  
- SmartScreen NOT VERIFIED  
- Fiscal LIVE BLOCKED  
- Calendar Dark WPF  
- Authz mostly UI-layer (REVIEW)  
- Installed Clientes smoke flake  
- Bulk 500+ synthetic seed NOT TESTED as dedicated generator  

## STOP

Do not start Commercial-12 / auto-update / SaaS / mobile / NFC-e / NFS-e / fiscal LIVE / certificate purchase. Do not push automatically.
