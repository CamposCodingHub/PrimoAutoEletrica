# PRIMOX FULL ASSURANCE-11 — Inventory

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
