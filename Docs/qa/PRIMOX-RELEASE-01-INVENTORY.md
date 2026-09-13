# PRIMOX RELEASE-01 — INVENTORY

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
**Baseline HEAD:** `58b8e1f`  
**Tag:** `v1.0.0` = `72d85fa` → commit `a4ad6fe`  

Valores lidos do repositório / artefatos (não copiados de relatórios antigos sem verificação).

| Campo | Valor atual | Fonte |
|---|---|---|
| Product Name | PRIMOX Workshop | `AssemblyInfo.cs`, Inno `AppName` |
| Product Version | 1.0.0 | `AssemblyInformationalVersion`, Inno `AppVersion` |
| Assembly Version | 1.0.0.0 | `AssemblyVersion` |
| File Version | 1.0.0.0 | `AssemblyFileVersion` |
| Informational Version | 1.0.0 | `AssemblyInformationalVersion` |
| Company | CamposCodingHub | `AssemblyCompany`, Inno `AppPublisher` |
| Product | PRIMOX Workshop | `AssemblyProduct` |
| Copyright | Copyright (C) 2026 CamposCodingHub | `AssemblyCopyright` |
| Trademark/Brand | PRIMOX | `AssemblyTrademark`, MainWindow title |
| Target Framework | net6.0-windows | `PrimoAutoEletrica.csproj` |
| Runtime / RID | win-x64 (publish) | `Build-PrimoXCommercialRelease.ps1` |
| Publish mode | Self-contained; Trim=false; SingleFile=false | pipeline |
| Installer version | 1.0.0 | `PrimoAutoEletrica.iss` |
| Installer name | `PRIMOX-Workshop-Setup-1.0.0.exe` | pipeline / ISS |
| Executable name | `PrimoAutoEletrica.exe` | ISS `AppExeName` (**INTENTIONAL TECHNICAL NAME**) |
| Database location | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` | App runtime / ISS DataPathHint |
| Config location | `%LOCALAPPDATA%\PrimoAutoEletrica\` (JSON configs) | App |
| Log location | `%LOCALAPPDATA%\PrimoAutoEletrica\Logs\` | App |
| Backup location | `%LOCALAPPDATA%\PrimoAutoEletrica\Backups\` | App |
| Update mechanism | **NOT IMPLEMENTED** (manual reinstall) | product truth |
| Signing status | **BLOCKED** (no commercial cert; env thumbprint absent) | Sign-PRIMOX |
| Fiscal status | Foundation + Import; **LIVE BLOCKED** | product truth |
| Application icon | `PrimoAutoEletrica/icon.ico` (41086 bytes) | csproj `ApplicationIcon` + ISS SetupIconFile |

## Version matrix (pré-correção de branding de janelas)

| SOURCE | VALUE | EXPECTED | STATUS |
|---|---|---|---|
| AssemblyTitle | PRIMOX Workshop | PRIMOX Workshop | OK |
| AssemblyProduct | PRIMOX Workshop | PRIMOX Workshop | OK |
| AssemblyCompany | CamposCodingHub | CamposCodingHub | OK |
| AssemblyVersion | 1.0.0.0 | 1.0.0.0 | OK |
| AssemblyFileVersion | 1.0.0.0 | 1.0.0.0 | OK |
| AssemblyInformationalVersion | 1.0.0 | 1.0.0 | OK |
| Inno AppName | PRIMOX Workshop | PRIMOX Workshop | OK |
| Inno AppVersion | 1.0.0 | 1.0.0 | OK |
| Inno OutputBaseFilename | PRIMOX-Workshop-Setup-1.0.0 | same | OK |
| MainWindow Title | PRIMOX | PRIMOX | OK |
| LoginWindow Title | PRIMOX — Acesso | PRIMOX | OK |
| EXE file name | PrimoAutoEletrica.exe | technical OK | INTENTIONAL TECHNICAL |
| AppData folder | PrimoAutoEletrica | technical OK | INTENTIONAL TECHNICAL |
| Namespace | PrimoAutoEletrica.* | technical OK | INTENTIONAL TECHNICAL |
| Secondary Window Titles | Primo Auto Eletrica (legacy) | PRIMOX | **FIXED** → PRIMOX |

## Brand classification

| Surface | Classification |
|---|---|
| Assembly / Installer / Login / Main | COMMERCIAL SURFACE (PRIMOX) |
| EXE / AppData / namespaces | INTENTIONAL TECHNICAL NAME |
| Secondary window titles (pré-fix) | LEGACY BRAND → corrected |
