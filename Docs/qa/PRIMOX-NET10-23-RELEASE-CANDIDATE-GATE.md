# PRIMOX NET10-23 — RELEASE CANDIDATE GATE

**Timestamp:** 2026-09-13  
**Branch:** `migration/net10`  
**HEAD:** `cccbb43` (sem alteração de código nesta fase)  
**Evidence:** `TestResults/Net10-Overnight/20260913/NET10-23-RC-Gate/`  
**Regra:** zero-trust · sem merge · sem push · sem mover tags · comercial 1.0.0 intacto

---

## F00 — Git

| Ref | Valor | Status |
|---|---|---|
| Branch | `migration/net10` | OK |
| HEAD | `cccbb43` | OK |
| `main` | `29b19b1` | **PASS** |
| `v1.0.0` peeled | `a4ad6fe` | **PASS** |
| `primox-net6-final` peeled | `29b19b1` | **PASS** |
| Working tree início | limpo | OK |

---

## F01 — Estado atual (CURRENT)

| Gate | Comando | Exit | Resultado | Evidência |
|---|---|---:|---|---|
| Build Debug | `dotnet build -c Debug` | 0 | **0 erros** | `01-build-debug.txt` · 11:11 |
| Build Release | `dotnet build -c Release` | 0 | **0 erros** | `01-build-release.txt` |
| Unit | `dotnet test` Release | 0 | **173/173** | `01-unit.txt` |
| QaEngine | Run-UiSmoke QaEngine | 0 | **43/43 APROVADO** | `01-QaEngine/` · 11:17 |
| DeepQa | Run-UiSmoke DeepQa | 0 | **6/6 APROVADO** | `01-DeepQa/` · 11:19 |

---

## F02 — TFM

| Check | Resultado |
|---|---|
| Produto ACTIVE | `PrimoAutoEletrica.csproj` → **`net10.0-windows`** |
| Unit ACTIVE | `Tests/.../PrimoAutoEletrica.Tests.csproj` → **`net10.0-windows`** |
| `net6.0-windows` em csproj/props/targets/pubxml | **0** |
| Outros TFMs (Api/Simulation/Tools net9) | TOOL / HISTORICAL · não produto Workshop |

---

## F03 — Publish limpo

| Item | Valor |
|---|---|
| Output | `artifacts/publish/net10-23-rc-gate-win-x64/` |
| Exit | **0** |
| Files | **472** · ~177.0 MB |
| PDB | **0** · DB **0** |
| OpenTK / LiveCharts / SkiaSharp.Views.WPF | **0 / 0 / 0** |
| EXE SHA256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| Published Clientes | exit **0** |

---

## F04 — Artefato installer NET10

| Item | Valor |
|---|---|
| Nome | `PRIMOX-Workshop-Setup-1.1.0-PackagingE2E.exe` |
| Geração | `Build-PrimoXCommercialRelease.ps1` via `Test-CommercialInstallerHardening` (AppId PackagingE2E) |
| Size | **54195339** bytes |
| SHA256 | `FADEB2BA05A6739DC28E8B22B0262886806991E98A5EA314AECA2D1511A119EE` |
| Timestamp | 2026-09-13 11:22:49 |
| Comercial 1.0.0 BEFORE | `9A08494D…A9C5` · 61686497 |
| Comercial 1.0.0 AFTER | `9A08494D…A9C5` · 61686497 · **INTACTO** |

Nota: tentativa com Version `1.1.0-net10-rc23` falhou no ISCC (`VersionInfoVersion` inválido). Reexecução com Version numérica `1.1.0` + suffix PackagingE2E (script oficial).

---

## F05–F09 — Installer E2E 3 ciclos

Script: `Scripts/Test-CommercialInstallerHardening.ps1 -Version 1.1.0 -Cycles 3`  
Exit: **0** · Report: `05-commercial08-report.md`

| Ciclo | Install | Startup | Uninstall | Data | Notes |
|---|---|---|---|---|---|
| 1 | PASS Exit=0 | PASS | PASS Exit=0 | PASS integrity=ok | Installed smoke Clientes Exit=0 |
| 2 | PASS Exit=0 | PASS | PASS Exit=0 | PASS | — |
| 3 | PASS Exit=0 | PASS | PASS Exit=0 | PASS | — |

Extras do script: Uninstall with app open PASS · Final reinstall PASS · Final CRUD Clientes PASS · Start Menu e2e=True commercial=False

**INSTALLER E2E = PASS (3/3)**

### Journeys no EXE instalado (ciclo dedicado RC23)

| Módulo | Exit | Status |
|---|---:|---|
| LoginSessao | 0 | PASS |
| Dashboard | 0 | PASS |
| Agendamentos | 0 | PASS |
| Calendar | 0 | PASS |
| Clientes | 0 | PASS |
| OrdensServico | 0 | PASS |
| Estoque | 0 | PASS |
| Financeiro | 0 | PASS |
| Relatorios | 0 | PASS |
| Configuracoes | 0 | PASS |

**journeyFails=0** · Install Exit=0 · Uninstall Exit=0

---

## F06 — Isolamento AppData

| Item | Resultado |
|---|---|
| Install dir E2E | `%LOCALAPPDATA%\PRIMOX-Workshop-InstallTest-C08*` |
| Data dir E2E | `%LOCALAPPDATA%\PRIMOX-Workshop-DataTest-C08*` |
| Prod data | `%LOCALAPPDATA%\PrimoAutoEletrica` |
| Marker prod hash before/after | **match=True** (não sobrescrito) |

---

## F07 / F10 — Uninstall / Reinstall / DB

| Check | Resultado |
|---|---|
| Uninstall | Exit **0** · exeGone=True |
| User data após uninstall | dataDir **preservado** · dbExists=True (política intencional) |
| Reinstall (script final) | PASS · data readable · integrity=ok |
| Controlled seed integrity | **ok** (todos os ciclos) |

---

## F11 — Published vs Installed

| EXE | Login smoke | Exit |
|---|---|---:|
| Published `net10-23-rc-gate-win-x64` | LoginSessao | **0** |
| Installed PackagingE2E | LoginSessao | **0** |

---

## F12 / F18 — Visual / Calendar

| Smoke | Status |
|---|---|
| Calendar | APROVADO |
| Tema | APROVADO |
| Login / Dashboard / Agenda / Financeiro / Relatorios / Config | APROVADO |
| Calendar Dark PNG | `18-CalendarPng/agendamentos-calendar-dark.png` (12031 bytes) |

**Calendar Dark: PASS** (legível nesta sessão)

---

## F13 — Security

| Smoke | Status |
|---|---|
| A12Security | **APROVADO** |
| LoginSessao | **APROVADO** |
| Critical | **0** |

---

## F14–F16 — Externos

| Item | Status |
|---|---|
| Fiscal LIVE (`PRIMOX_FOCUS_HOMOLOG_TOKEN`) | **BLOCKED_EXTERNAL** |
| Code Signing (SignTool OK · thumbprint ausente) | **BLOCKED_EXTERNAL** |
| .NET6 SxS (SDK6 False) | **BLOCKED_EXTERNAL** |

---

## F17 — NU1701

| Check | Resultado |
|---|---|
| restore NU1701_COUNT | **0** |
| Publish OpenTK/LiveCharts | **0** |

---

## F19 — Critical regression (pós E2E)

| Gate | Resultado |
|---|---|
| Build Release | **0 erros** |
| Unit | **173/173** |
| QaEngine | **43/43 APROVADO** · 11:36 |
| DeepQa | **6/6 APROVADO** · 11:39 |

---

## F20 — Git final

| Ref | Valor |
|---|---|
| HEAD | `cccbb43` (+ commit docs desta fase) |
| main | `29b19b1` |
| v1.0.0 | `a4ad6fe` |
| primox-net6-final | `29b19b1` |

---

## Decisão

Todas as gates técnicas locais desta sessão: **PASS**, incluindo **Installer E2E 3/3**.

Dependências externas permanecem:

- Fiscal LIVE  
- Code Signing comercial  
- .NET6 Side-by-Side  

**FINAL VERDICT: APPROVED WITH EXTERNAL LIMITATIONS**

O produto NET10 está tecnicamente pronto segundo os testes executados nesta sessão, porém promoção comercial completa continua dependente de recursos externos.
