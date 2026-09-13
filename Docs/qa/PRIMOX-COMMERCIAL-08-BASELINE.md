# PRIMOX-COMMERCIAL-08 — Baseline

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
