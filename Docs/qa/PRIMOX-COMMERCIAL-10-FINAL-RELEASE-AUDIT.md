# PRIMOX-COMMERCIAL-10 — Final Release Audit

> **DOCUMENTO REESCRITO EM CAMADAS — 2026-09-13**
>
> | Camada | Uso |
> |--------|-----|
> | **Estado atual** | Fonte operacional hoje · ver também `Docs/CURRENT-TRUTH.md` e NET10-26 |
> | **Avanços desta fase (histórico)** | Registro do que esta execução entregou — **não** sobrescrever mentalmente o estado atual |
>
> HEAD de referência pós-NET10-26: `1372e11` · TFM `net10.0-windows` · Branch `migration/net10`
> Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md`

## Estado atual (pós NET10-26 · 2026-09-13)

| Item | Valor |
|------|-------|
| Branch | `migration/net10` |
| HEAD fiscal foundation | `1372e11` |
| TFM | `net10.0-windows` |
| Unit | **194/194** |
| QaEngine | **43/43** |
| DeepQa | **6/6** (baseline NET10-26) |
| Fiscal LIVE / WhatsApp API / Code signing | **BLOCKED_EXTERNAL** |
| Calendar Dark | Mitigado (`CalendarContrastHealer`) — não citar KNOWN LIMITATION antigo como atual |
| NF-e | PARTIAL + TESTED (Focus path + Fake) |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY |
| DANFE | PDF informativo (≠ SEFAZ oficial) |
| Multiempresa fiscal | IMPLEMENTED + TESTED (DB) |

**Claims abaixo sobre net6, “emissão NÃO IMPLEMENTADO”, Unit 173, DANFE/cancel NI, Calendar Dark KNOWN LIMITATION, etc. pertencem ao registro histórico da fase.**

---

## Avanços desta fase (registro histórico — preservar)

**Date:** 2026-09-11  
**Phase:** PRIMOX WORKSHOP — COMMERCIAL-10  
**Decision:** **YELLOW — READY WITH EXTERNAL SIGNING BLOCKER**

```text
Commercial release package validated; Authenticode commercial
signing remains externally blocked pending acquisition/configuration
of a commercial Code Signing certificate.
```

## 1. Executive Summary

COMMERCIAL-10 validated the **1.0.0** commercial packaging pipeline end-to-end: build, publish, installer, install/uninstall/reinstall (3 cycles), data preservation, backup/restore, workspace QA, I18N regression, Light/Dark, security (refined), and code-signing **readiness**.

**No** commercial Authenticode certificate is present → signing = **BLOCKED** (honest YELLOW).  
**No** Fiscal LIVE, NFC-e, NFS-e, SaaS, or auto-update work was started.

| Area | Verdict |
|------|---------|
| Desktop package | READY |
| Installer lifecycle | READY (3/3 + app-open uninstall) |
| Workspace QA | PASS |
| Code Signing | BLOCKED EXTERNAL |
| SmartScreen | NOT VERIFIED |
| Fiscal LIVE | BLOCKED |

## 2. Repository Baseline

| Item | Value |
|------|-------|
| HEAD inicial | `bf1eb78` |
| Branch | `main` |
| Tag `v1.0.0` OLD | `72d85fa` |
| Tag `v1.0.0` FINAL | `72d85fa` — **PRESERVED** |
| Worktree start | clean (fiscal report present in tree, not dirty) |
| Evidence | `TestResults/Commercial10/20260911-064617` (baseline) · `…/20260911-064927` (gate) · `…/InstallerE2E-rerun` |

## 3. Version Integrity

| Surface | Value | Status |
|---------|-------|--------|
| AssemblyVersion | 1.0.0.0 | PASS |
| FileVersion | 1.0.0.0 | PASS |
| InformationalVersion | 1.0.0 | PASS |
| Publish ProductVersion | 1.0.0 | PASS |
| Installer AppVersion | 1.0.0 | PASS |
| About / UI strings | 1.0.0 | PASS |

## 4. Build

| Config | Result |
|--------|--------|
| Debug | PASS — 0 errors |
| Release | PASS — 0 errors |
| Units | **162/162 PASS** |

Warnings: NETSDK1138 (net6 EOL), NU1701 (OpenTK/Skia), CA1416 Windows-only APIs — pré-existentes; documentados; não mascarados.

## 5. Publish

| Item | Value |
|------|-------|
| Command | `Build-PrimoXCommercialRelease.ps1` |
| RID | win-x64 self-contained |
| EXE | `artifacts/publish/win-x64/PrimoAutoEletrica.exe` |
| Metadata | Product=`PRIMOX Workshop` · Company=`CamposCodingHub` |
| DB in package | **excluded** |

## 6–14. Installer / Install / Uninstall / Reinstall / Data

PackagingE2E AppId (`PRIMOX.Workshop.PackagingE2E`) — does **not** overwrite commercial `PRIMOX.Workshop.1`.

| Gate | Result |
|------|--------|
| Generate Setup | PASS |
| Clean install | PASS ×3 Exit=0 |
| Startup | PASS ×3 |
| Silent uninstall | PASS ×3 |
| Uninstall with app open | **PASS** |
| Reinstall | PASS |
| Data preservation | PASS (DB + markers) |
| Backup/restore file-level | PASS |
| Installed Clientes smoke | **FAIL Exit=2** (harness flake) |

**Harness fix this phase:** remove residual install dir after successful uninstall (Logs leftovers → next install Exit=2). Documented in `Test-CommercialInstallerHardening.ps1`.

## 15–16. Database / Security

| Gate | Result |
|------|--------|
| integrity_check (seed path) | PASS |
| FK check | PASS (0) |
| Secret scan (refined) | **PASS** — no private keys / Focus tokens / PFX in tracked sources |
| Naive password regex | false positives in tests/smoke/docs — **not** release blockers |

## 17–20. Code Signing / Timestamp / Verify / SmartScreen

| Gate | Result |
|------|--------|
| `Sign-PRIMOX.ps1 -Mode Readiness` | **BLOCKED** exit 2 |
| SignTool | READY |
| Timestamp DigiCert RFC3161 | READY (config) |
| Commercial cert | NOT AVAILABLE |
| Localhost cert | **rejected** |
| Sign / Verify | **NOT EXECUTED** |
| SmartScreen | **NOT VERIFIED** |

## 21. QA

| Suite | Result |
|-------|--------|
| QaEngine | **43/43** |
| DeepQa | **6/6** |
| Exhaustive | **1882/0/0** (8 rounds Light/Dark × 4 resolutions) |
| LongRun | PASS |
| Sidebar | PASS |

## 22–24. I18N / Visual / Accessibility

| Gate | Result |
|------|--------|
| I18n07 | PASS |
| Tema Light/Dark | 2/2 PASS |
| Resolutions Exhaustive | 1366 / 1600 / 1920 / 2560 PASS |
| Accessibility | covered by Exhaustive/CompleteUi — PASS |
| Calendar Dark header | KNOWN LIMITATION |

## 25. Fiscal Regression

Automated fiscal/unit path via test suite **PASS**.  
LIVE homologation **BLOCKED** (token/config). Production **BLOCKED**.  
WIP `PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` **preserved** (not altered to PASS).

## 26. Known Limitations

1. Code Signing commercial certificate absent  
2. SmartScreen not verified  
3. Calendar Dark WPF header  
4. Fiscal LIVE / NFC-e / NFS-e / auto-update / SaaS not in 1.0  
5. Installed-package Clientes smoke Exit=2 flake (workspace QA green)

## 27. Release Decision

**YELLOW**

Desktop Commercial Package = READY  
Installer = READY  
QA = PASS  
Code Signing = BLOCKED EXTERNAL  
Fiscal LIVE = BLOCKED EXTERNAL / CONFIGURATION  

## 28–29. Artifacts / SHA-256

| Artifact | SHA256 | Size |
|----------|--------|------|
| `PRIMOX-Workshop-Setup-1.0.0.exe` | `C7E33C2A9BDC9482DBF0D8BD95D69A55042836D02B80BC68C25BDAE557C635AB` | 62269512 |

Local paths: `artifacts/installer/`, `artifacts/checksums/` (gitignored).  
Evidence: `TestResults/Commercial10/` (gitignored).

## 30. Checklist

- [x] v1.0.0 preservada  
- [x] WIP fiscal preservado  
- [x] Build Debug/Release PASS  
- [x] Publish PASS  
- [x] Version / Branding / Icon PASS  
- [x] Secret scan PASS (refined)  
- [x] Installer / Startup / Uninstall / Silent / Reinstall / Data PASS  
- [x] Backup/Restore PASS  
- [x] Database PASS  
- [x] QaEngine / Deep / Exhaustive / LongRun PASS  
- [x] I18N / Light / Dark / Accessibility PASS  
- [x] Fiscal regression PASS (LIVE BLOCKED)  
- [x] Code Signing BLOCKED (expected)  
- [x] SHA256 recorded  
- [x] Release Manifest + Audit + PROJECT_STATUS  
- [ ] Signature / Timestamp on package — N/A until cert  
- [ ] SmartScreen — NOT VERIFIED  

## 31. Git commits (this phase)

See `git log` after commit — expected:

1. `chore(release): prepare PRIMOX 1.0.0 commercial package`  
2. `test(release): validate commercial release gate`  
3. `docs(release): record commercial release audit`  

## 32. STOP

**Do not** start Commercial-11, Fiscal LIVE, NFC-e, NFS-e, SaaS, mobile, auto-update, or IA without a new explicit instruction.
