# PRIMOX Final Codebase Hardening Audit 3.0 (Script 3)

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

**Date:** 09/09/2026  
**Decision:** READY WITH LIMITATIONS  
**Depends on:** Script 1 `310a825` · Script 2 `0a1ccfd`  
**Tag:** `v1.0.0` → `a4ad6fe` (intact)

## Fiscal protection (explicit)

| Component | Class |
|-----------|-------|
| `NFeEmissaoService` | KEEP-FUTURE |
| `FakeFiscalProvider` | TEST-ONLY (not commercial DI) |
| `IFiscalProvider` / `FocusNfeProvider` / foundation stack | KEEP / KEEP-FUTURE |
| `FiscalProductionGuard` | KEEP — produção BLOQUEADA |

## SAFE-REMOVE executed

1. `Utilities/CodeAuditService.cs` — zero callers  
2. `Services/ScreenshotCaptureService.cs` — zero wiring  
3. `Services/LocalSyncMessageHandler.cs` — zero wiring  
4. 0-byte tests: `PagingHelperTests` / `RBACServiceTests` / `SecurityServiceTests` / `SqlIdentifierGuardTests`  
5. Root leftovers: `fase8-smoke-err.txt` / `fase8-smoke-out.txt`

## KEEP-FUTURE (not removed)

Empty scaffolds: `Dockerfile`, `docker-compose.yml`, Maui shell, deploy/k8s, empty docs placeholders (`DEVELOPER_GUIDE.md`, `USER_MANUAL.md`), orphan ViewModels DI-backed, ContabilExportService.

Legacy themes `Colors.xaml` / `StandardTheme.xaml` — eligible later; not removed this pass.

## Security scan

- No PEM private keys / `sk_live_` / AWS `AKIA*` in source  
- Only smoke password literal `Workflow@123` in ExhaustiveUi (TEST-ONLY automation)  
- WIP deploy scripts remain **untracked** (not committed)

## DB integrity (isolated copy)

- Source: Exhaustive smoke DB copy → `TestResults/db-integrity-script3-20260909-084130/primoauto.db`  
- `PRAGMA integrity_check` = **ok**  
- `PRAGMA foreign_key_check` = **0 violations**  
- Note: this SQLite store has no `__EFMigrationsHistory` table (app uses custom/schema path); Fiscal* tables present

## QA (Script 3)

| Suite | Folder / note | Result |
|-------|---------------|--------|
| BUILD | — | 0 errors |
| Fiscal/NFe/Homolog/Focus units | — | 45 PASS |
| DeepQa (+ LongRun) | `TestResults/UiSmoke/2026-09-09_08-43-02` | 6/6 |
| QaEngine | `TestResults/UiSmoke/2026-09-09_08-45-59` | 43/43 |
| Exhaustive | `TestResults/UiSmoke/2026-09-09_08-53-34` | discovered=3276 tested=1933 pass=1933 fail=0 blocked=0 |
| Restart | kill process between suites | PASS (fresh process each smoke) |
| Visual final | Help Light + PDV Dark PNGs read | PASS WITH LIMITATIONS (Calendar Dark known) |

## WIP preserved

- `Scripts/Atualizar-PrimoAuto.bat`  
- `Scripts/Deploy-ToInstalledApp.ps1`
