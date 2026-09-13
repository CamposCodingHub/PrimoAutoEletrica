# PRIMOX FULL ASSURANCE-12 — INVENTORY

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Date:** 2026-09-11  
**HEAD baseline:** `175fac5`  
**Tag v1.0.0:** `72d85fa` (PROTECTED)  
**Branch:** `main`

## Method

Fresh filesystem inventory of `PrimoAutoEletrica/` (excluding `bin/` / `obj/`). Counts are current at Assurance-12 start, not copied from Assurance-11.

## Counts

| Asset | Count | Notes |
|------|------:|-------|
| Windows (`*Window.xaml`) | 47 | Views + dialogs |
| UserControls (`UserControls/*.xaml`) | 28 | Module shells |
| ViewModels (`*.cs`) | 22 | |
| Services (`Services/**/*.cs`) | 174 | Includes QA/smoke partials + new A12 helpers |
| Repositories | 14 | `Repositories` + `Data/Repositories` |
| Permission check sites (`TemPermissao*`) | 82 | UI + service call sites |
| Migration-related source files | 2+ | Runtime migration catalog also embedded (28 applied historically) |

## Security-relevant surfaces (A12 focus)

| Surface | Location / notes |
|---------|------------------|
| Authentication | `DatabaseService.Autenticar*`, `PasswordHasherService`, `LoginWindow` / `LoginViewModel` |
| Authorization | `PermissionService`, UI gates, `SystemConfigurationService.SaveAuthorized`, **new** `DatabaseBackupService.*Authorized` |
| Path / file | `DatabaseBackupService` restore/create, media, imports/exports — **new** `PathSecurityHelper` jail |
| Process start | WhatsApp/PDF/folder open — **new** `SecureProcessLauncher` on critical paths |
| Backup / restore | Configurações UI + service; smoke Configuracoes |
| SQL | Parameterized Dapper/ADO; static red-team heuristic in `Invoke-SecurityRedTeam.ps1` |

## QA / Scripts added in A12

- `Scripts/Run-FullAssurance12.ps1`
- `Scripts/QA/Invoke-PathTraversalSimulation.ps1`
- `Scripts/QA/Invoke-BulkDataGenerator.ps1` (legacy helper; primary bulk = smoke `BulkDataQa12`)
- `Scripts/QA/Invoke-InstalledClientesSmoke.ps1`
- `UiSmokeTestService.Assurance12.cs` filters: `A12Security`, `BulkDataQa12`
- Unit: `PathSecurityHelperTests`, `BackupAuthorizationTests`

## Installer / packaging

- Commercial setup + PackagingE2E AppId isolation
- Installed Clientes smoke: evidence capture + Minimized window + PersistReport → `RuntimeLogDirectory`

## WIP preserved (not part of A12 commits)

- Fiscal live homologation WIP must not be forced PASS / must not enter security commits unless explicitly requested
