# PRIMOX FULL ASSURANCE-11 — Inventory

**Date:** 2026-09-11  
**HEAD:** `3aa03b8`  
**Tag `v1.0.0`:** `72d85fa` **PRESERVED**  
**Branch:** `main` (ahead of origin by C10 commits; clean worktree at start)

## 1. Source of truth

Filesystem/repo inspected — not assumed from older docs.

| Item | Count / Value |
|------|----------------|
| Projects (*.csproj, excl. bin/obj) | **14** |
| Solution | `PrimoAutoEletrica.sln` |
| TFM desktop | `net6.0-windows` |
| XAML files | **104** |
| Window XAML (`*Window.xaml`) | **47** |
| UserControl XAML (`*Control.xaml`) | **29** |
| ViewModels (`*ViewModel.cs`) | **21** |
| Services (`Services/*Service.cs`) | **86** |
| Repository files | **14** |
| Schema migration IDs (code) | **28** |
| Product version | **1.0.0** / **1.0.0.0** |

## 2. Navigation / modules (shell)

Typical DeepQa / CompleteUi modules:

Dashboard · Clientes · Veículos · OS · Orçamentos · PDV · Estoque · Financeiro · Fornecedores · Funcionários · Agenda · Relatórios · Catálogo · Importar NF-e · Ajuda · Configurações · (Fiscal Ops when present)

## 3. Data surface

| Item | Location |
|------|----------|
| SQLite DB | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` |
| Installer commercial | `PRIMOX.Workshop.1` → Program Files |
| Packaging E2E | isolated AppId / LocalAppData test dirs |
| Backups | App + `DatabaseBackupService` / `ExternalBackupService` |

## 4. Auth / security touchpoints

| Area | Notes |
|------|-------|
| Login | `LoginWindow` / `LoginViewModel` |
| Permissions | profiles / `ConfigurarPermissoes` / service guards |
| Password storage | hash path (verify in red-team) |
| Process.Start | WhatsApp / file open / shell — review UseShellExecute |
| SQL | Dapper + parameterized; some `$""` for **identifiers** (PRAGMA/table) — review |

## 5. Existing QA harnesses (reuse)

| Asset | Role |
|-------|------|
| `Run-UiSmoke.ps1` | QaEngine / DeepQa / Exhaustive / LongRun / I18n / Tema |
| `UiSmokeTestService.*` | in-process UI automation |
| `Run-CommercialOvernightQa.ps1` | overnight stress |
| `Run-Commercial10ReleaseGate.ps1` | release gate |
| `Test-CommercialInstallerHardening.ps1` | install lifecycle |
| `Sign-PRIMOX.ps1` | signing readiness |

## 6. Assurance-11 agents (new)

Under `Scripts/QA/`:

| Agent | File |
|-------|------|
| Orchestrator | `../Run-FullAssurance11.ps1` |
| Security static + secrets | `Invoke-SecurityRedTeam.ps1` |
| Database integrity | `Invoke-DatabaseIntegrity.ps1` |
| Recovery / kill-restart | `Invoke-RecoverySimulation.ps1` |
| Visual / theme matrix notes | covered via Exhaustive + Tema + I18n07 |

## 7. Out of scope (explicit)

- Fiscal LIVE / Focus SEFAZ real  
- NFC-e / NFS-e  
- Commercial certificate purchase  
- Attacks on third parties / production data  

## 8. WIP preservation

`Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` present in tree; **not** modified by this phase unless dirty (was clean at start).
