# PRIMOX MASTER AUDIT-01 — INVENTORY

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

**Data:** 12/09/2026  
**Baseline HEAD:** `b706370`  
**Tag:** `v1.0.0` = `72d85fa` → `a4ad6fe`  
**Fonte:** inspeção direta do repositório (não copiado de RELEASE-01 / A13).

## Solution

| Projeto | TFM | Classe |
|---|---|---|
| `PrimoAutoEletrica` (WPF) | net6.0-windows | **PRODUCT** |
| `Tests/PrimoAutoEletrica.Tests` | net9.0-windows | Tests |
| `PrimoAutoEletrica.UiTests` | net6.0 | Tests |
| `PrimoAutoEletrica.Api` | net9.0-windows | Out of scope / secondary |
| `PrimoAutoEletrica.Maui` | stub | Out of scope |
| Tools (`LocalSyncSimulator`, `DbConfigurator`) | net9.0 | Tools |

## Product identity

| Campo | Valor |
|---|---|
| Product | PRIMOX Workshop |
| Company | CamposCodingHub |
| Version | 1.0.0 / 1.0.0.0 |
| Trademark | PRIMOX |
| EXE | `PrimoAutoEletrica.exe` (technical) |
| Icon | `PrimoAutoEletrica/icon.ico` |
| AppData | `%LOCALAPPDATA%\PrimoAutoEletrica` |
| DB file | `primoauto.db` |

## Architecture (real)

- **UI:** WPF · MainWindow shell · `NavigationService` · UserControls por módulo · janelas dialogais
- **Data:** SQLite (default) via `DatabaseService` · 28 migrations embutidas · opcional SQL Server
- **Domain services:** Venda, Caixa, Estoque, Oficina, Orçamento, NFe import, Fiscal foundation
- **Security:** `SecureProcessLauncher`, `PathSecurityHelper`, `PasswordHasherService`, `PermissionService`
- **I18N:** `LocalizationService` PT/EN/ES
- **Themes:** Light/Dark via `ThemeService`
- **QA harness:** `UiSmokeTestService` + `PrimoxQaEngine` (não distribuído no package comercial)

## Navigable modules (sidebar)

| Tag | Surface |
|---|---|
| Dashboard | `DashboardControl` |
| Clientes | `ClientesControl` |
| Veiculos | `VeiculosControl` |
| OrdensServico | `OrdensServicoControl` |
| OficinaKanban | `OficinaKanbanControl` |
| Orcamentos | `OrcamentosControl` |
| Agendamentos | `AgendamentosControl` |
| PDV | `PDVControl` |
| Estoque | `EstoqueControl` |
| CatalogoPecas | `CatalogoPecasControl` |
| Fornecedores | `FornecedoresControl` |
| Funcionarios | `FuncionariosControl` |
| Financeiro | `FinanceiroControl` |
| Relatorios | `RelatoriosControl` |
| ImportarNFe | `ImportarNFeControl` |
| FiscalOperacoes | `FiscalOperationsControl` |
| AutoEletricaTecnica | `AutoEletricaTecnicaControl` |
| Help | `HelpControl` |
| Configuracoes | `ConfiguracoesSistemaWindow` (window) |

## Repositories

`Cliente`, `Produto`, `Fornecedor`, `Funcionario`, `OrdemServico`, `Venda`, `Auditoria`, `Importacao` (+ Registry).

## Installer

`Installer/PrimoAutoEletrica.iss` — AppName PRIMOX Workshop · AppVersion 1.0.0 · Publisher CamposCodingHub · `{commonpf}\PRIMOX\Workshop` · AppData preservado no uninstall.

## Publish

`Scripts/Build-PrimoXCommercialRelease.ps1` — self-contained win-x64 · Trim/SingleFile false · PDB stripped.

## Legacy notes (inventory)

- `primoeletrica.db` aparece apenas em `ObterCaminhoBancoLegado()` (Financeiro/Orçamento) — **não** é DB ativo; runtime usa `DatabaseService` → `primoauto.db`.
- Superfície comercial residual encontrada: `AtualizacaoWindow.xaml` texto “Primo Auto Elétrica” (corrigido nesta auditoria).
