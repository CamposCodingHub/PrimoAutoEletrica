# PRIMOX NET10-13 — INSTALLER (EXPERIMENTAL)

**Data:** 2026-09-12  
**Branch:** `migration/net10`

## Artefatos

| Arquivo | SHA256 | Nota |
|---|---|---|
| `PRIMOX-Workshop-Setup-1.1.0-net10-preview.exe` | `3E361DB2…8729` | experimental · ~62.8 MB |
| `PRIMOX-Workshop-Setup-1.1.0-PackagingE2E-net10.exe` | (E2E AppId) | AppId isolado |
| `PRIMOX-Workshop-Setup-1.0.0.exe` | `9A08494D…A9C5` | **INTACTO** (antes=depois) |

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT |
|---|---:|---|
| Build-PrimoXCommercialRelease 1.1.0 net10-preview | 0 | Setup gerado · signing BLOCKED_NO_THUMBPRINT |
| 3× install→MainWindow smoke→uninstall | 0 | **all InstallExit/SmokeExit/UninstallExit=0** · DB retained · EXE gone |
| Reinstall + data preserved | 0 | **True** |

Evidence: `TestResults/Net10-Overnight/20260912/NET10-13-Installer/manual-3cycles.json`

## SIMULAÇÃO

instalar → usar → fechar → desinstalar → AppData retido → reinstalar → dados OK: **PASS** (3 ciclos).

## Decisão

**PASS WITH LIMITATIONS** (unsigned). Prosseguir **NET10-14**.
