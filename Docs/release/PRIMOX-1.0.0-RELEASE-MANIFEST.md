# PRIMOX Workshop 1.0.0 — Release Manifest

**Generated:** 2026-09-11  
**Phase:** COMMERCIAL-10 — Final Commercial Release Gate  
**Decision:** **YELLOW** — Desktop commercial package READY; Authenticode commercial signing **BLOCKED BY EXTERNAL CERTIFICATE**

| Field | Value |
|-------|-------|
| Version | **1.0.0** |
| Commit (gate HEAD) | `bf1eb78` (+ C10 commits after docs) |
| Tag `v1.0.0` | `72d85fa` **PRESERVED** (not moved) |
| Build | Release |
| Runtime / TFM | `net6.0-windows` |
| RID | `win-x64` |
| Architecture | x64 |
| Distribution | Self-contained publish + Inno Setup 6 |
| Data path | `%LOCALAPPDATA%\PrimoAutoEletrica` (`primoauto.db`) |

## Installer

| Field | Value |
|-------|-------|
| File | `PRIMOX-Workshop-Setup-1.0.0.exe` |
| Path | `artifacts/installer/` (local; gitignored) |
| Size | **62 269 512** bytes (~59.4 MB) |
| SHA256 | `C7E33C2A9BDC9482DBF0D8BD95D69A55042836D02B80BC68C25BDAE557C635AB` |
| AppId | `PRIMOX.Workshop.1` |
| ProductName | PRIMOX Workshop |
| Company | CamposCodingHub |

## Signing

| Field | Value |
|-------|-------|
| Status | **BLOCKED** |
| Certificate | **NOT AVAILABLE** (store has localhost only — rejected) |
| Thumbprint env | `PRIMOX_CODESIGN_THUMBPRINT` **ABSENT** |
| SignTool | READY (Windows Kit x64) |
| Timestamp URL | READY — `http://timestamp.digicert.com` (RFC3161) |
| Verification | **NOT EXECUTED** (no commercial sign) |
| SmartScreen | **NOT VERIFIED** |

When a commercial OV/EV Code Signing cert is installed:

```powershell
$env:PRIMOX_CODESIGN_THUMBPRINT = '<thumbprint>'
.\Scripts\Sign-PRIMOX.ps1 -Mode Readiness
.\Scripts\Build-PrimoXCommercialRelease.ps1   # signs EXE then Setup if thumbprint set
.\Scripts\Sign-PRIMOX.ps1 -Mode Verify -Path .\artifacts\installer\PRIMOX-Workshop-Setup-1.0.0.exe
# Then recalculate SHA256 AFTER signing
```

## QA (workspace Release)

| Suite | Result |
|-------|--------|
| QaEngine | **43/43 PASS** |
| DeepQa | **6/6 PASS** |
| ExhaustiveUi | **1882 PASS / 0 FAIL / 0 BLOCKED** |
| LongRun | **PASS** |
| I18n07 | **PASS** |
| Tema Light/Dark | **2/2 PASS** |
| Units | **162/162 PASS** |

## Installer E2E (PackagingE2E AppId — isolated)

| Gate | Result |
|------|--------|
| Install ×3 | **PASS** Exit=0 |
| Startup ×3 | **PASS** |
| Uninstall ×3 | **PASS** |
| Silent uninstall | **PASS** |
| Uninstall with app open | **PASS** |
| Reinstall | **PASS** |
| Data preservation | **PASS** |
| Backup/restore (file-level) | **PASS** |
| Installed UI smoke (Clientes) | **FAIL Exit=2** — harness flake on installed package; workspace QaEngine **PASS** |

## Database

| Gate | Result |
|------|--------|
| Integrity | **PASS** (seed/verify + QaEngine) |
| Foreign keys | **PASS** (0 violations in seed path) |
| Migrations | present / applied in runtime (schema via product migrations) |

## Fiscal / I18N

| Gate | Result |
|------|--------|
| Fiscal foundation / regression | **PASS** automated; **LIVE BLOCKED** (no token / config) |
| Production fiscal | **BLOCKED** |
| I18N PT/EN/ES | **PASS** (I18N-07 closed) |

## Known Limitations

1. Code Signing commercial certificate absent  
2. SmartScreen reputation **NOT VERIFIED**  
3. Calendar Dark header — native WPF known limitation  
4. Fiscal LIVE / NF-e production not validated  
5. NFC-e / NFS-e / auto-update / SaaS **NOT IMPLEMENTED**  
6. Installed-package Clientes UI smoke intermittent Exit=2 (non-blocking vs workspace QA)

## Release Decision

**YELLOW — READY WITH EXTERNAL SIGNING BLOCKER**

Desktop Commercial Package = READY  
Installer lifecycle = READY  
QA = PASS  
Code Signing = BLOCKED EXTERNAL  
Fiscal LIVE = BLOCKED EXTERNAL / CONFIGURATION  
