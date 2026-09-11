# PRIMOX-COMMERCIAL-08 — Baseline

**Data:** 2026-09-10  
**Fase:** INSTALLER E2E HARDENING  
**HEAD inicial:** `b5c9481` (`docs: add project tracker for future follow-up`)  
**Branch:** `main` (synced with origin)  
**Tag `v1.0.0`:** `72d85fa` ✅ intacta  
**Worktree:** WIP fiscal preservado (`Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md`)

## Inventário confirmado

| Item | Valor real |
|------|------------|
| Installer | `Installer/PrimoAutoEletrica.iss` (Inno Setup 6) |
| Pipeline | `Scripts/Build-PrimoXCommercialRelease.ps1` |
| E2E packaging | `Scripts/Test-InstalledPackageE2E.ps1` |
| Deploy dev | `Scripts/Deploy-ToInstalledApp.ps1` |
| AppId comercial | `PRIMOX.Workshop.1` |
| AppId E2E | `PRIMOX.Workshop.PackagingE2E` |
| Install path comercial | `C:\Program Files\PRIMOX\Workshop` (presente) |
| Uninstaller | `unins000.exe` |
| AppData | `%LOCALAPPDATA%\PrimoAutoEletrica` |
| Banco | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` |
| Self-contained | win-x64 |
| PrivilegesRequired | `admin` |
| Version | 1.0.0 / 1.0.0.0 |
| TFM | net6.0-windows |

## Problemas históricos a confirmar

1. Silent uninstall `unins000` hang / UAC cancel  
2. Reinstall Setup Exit=2 após force-clean  
3. Data preservation PASS (política AppData)  
4. Code signing BLOCKED (fora de escopo desta fase)

## Gap no .iss (pré-correção)

- Sem `CloseApplications=force`  
- Sem código Pascal explícito para encerrar `PrimoAutoEletrica.exe` no uninstall/install  
- Silent uninstall depende de `/FORCECLOSEAPPLICATIONS` no caller; se o EXE ainda segura arquivos, Inno pode aguardar indefinidamente
