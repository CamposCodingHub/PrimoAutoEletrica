# PRIMOX NET10-00 — BASELINE + PROTEÇÃO

**Data:** 12/09/2026  
**Fase:** NET10-00  
**Decisão:** **BASELINE ESTABLISHED / PROTECTED**  
**Migração .NET 10:** ainda **NÃO iniciada** (TargetFramework intacto)

---

## Proteção Git

| Item | Valor |
|---|---|
| Branch estável | `main` |
| HEAD baseline (main) | `29b19b1` (`29b19b16d0e6e3413bdba20c505e20c992596c24`) |
| Tag comercial `v1.0.0` | `72d85fa` → commit `a4ad6fe` **PRESERVADA** |
| Tag proteção `primox-net6-final` | aponta para `29b19b1` **NÃO MOVER** |
| Branch experimental | `migration/net10` (nascida do baseline) |
| Remote | `origin` = `https://github.com/CamposCodingHub/PrimoAutoEletrica.git` |
| Push nesta fase | **NÃO** |

---

## Produto (inspecionado)

| Campo | Valor real |
|---|---|
| Product | PRIMOX Workshop |
| Version | 1.0.0 |
| TFM | **`net6.0-windows`** (`PrimoAutoEletrica.csproj`) |
| Output | WinExe WPF · `PrimoAutoEletrica.exe` |
| Nullable / ImplicitUsings | enable / enable |
| RID publish | `win-x64` |
| Publish | self-contained · Trim=false · SingleFile=false |
| DB | SQLite `primoauto.db` · `Microsoft.Data.Sqlite` 7.0.20 |
| Migrations embutidas | **28** (`202605210001` … `202609080001`) |
| Installer | `Installer/PrimoAutoEletrica.iss` · AppId `PRIMOX.Workshop.1` · Setup `PRIMOX-Workshop-Setup-1.0.0` |

### Packages (principais)

EPPlus 8.7.0 · LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc5.4 · Microsoft.Data.SqlClient 5.2.2 · Microsoft.Data.Sqlite 7.0.20 · Otp.NET · PdfSharpCore · Dapper · CommunityToolkit.Mvvm · Microsoft.Extensions.* 7.0.0 · SQLitePCLRaw.bundle_e_sqlite3 · System.Security.Cryptography.Xml 9.0.18

---

## Ambiente de build (máquina de auditoria)

| Item | Valor |
|---|---|
| SDK instalado | **10.0.302** (único SDK listado) |
| Host | 10.0.10 |
| Runtimes Desktop | 9.0.18 · 10.0.10 |
| SDK .NET 6 | **AUSENTE** nesta máquina |
| Build do TFM net6 | via targeting + `$env:DOTNET_ROLL_FORWARD='LatestMajor'` |
| Packs observados | `Microsoft.NETCore.App.Ref` / `Microsoft.WindowsDesktop.App.Ref` **10.0.10** |
| SignTool | `Windows Kits 10.0.26100.0\x64\signtool.exe` |
| Inno Setup | `C:\Program Files\Inno Setup 7\ISCC.exe` |
| MSBuild (SDK) | 18.6.11+35b593beb |
| global.json | ausente |

**Nota de proteção:** a linha de produto permanece TFM `net6.0-windows`. A ausência do SDK 6 nesta estação é fato de ambiente (documentado); não implica promoção a .NET 10.

---

## Baseline QA (execução NET10-00)

| Gate | Resultado | Evidência |
|---|---|---|
| Build Debug | **PASS** 0 erros | `TestResults/Net10-00/20260912/build-debug.txt` |
| Build Release | **PASS** 0 erros (rebuild após lock) | sessão |
| Unit | **173/173 PASS** | sessão |
| QaEngine | **43/43 PASS** | `…/QaEngine` |
| DeepQa | **6/6 PASS** | `…/DeepQa` |
| LongRun | **PASS** | `…/LongRun` |
| ExhaustiveUi | **PASS** 1942/1942 (disc 3289) | `…/ExhaustiveUi` |
| I18n07 | **PASS** | `…/I18n07` |
| Tema | **PASS** | `…/Tema` |
| A13Database | **PASS** | `…/A13Database` |
| A12Security | **PASS** | `…/A12Security` |
| A13Performance | **PASS** | `…/A13Performance` |
| Startup sample 3× | avg **1936.7 ms** (MainWindow smoke) | `…/startup-sample` |

Release build inicial falhou por lock do EXE (smoke paralelo) — **não** é regressão de produto; rebuild limpo PASS.

---

## Artefatos comerciais existentes (não regenerados nesta fase)

| Artefato | Valor |
|---|---|
| Setup | `artifacts/installer/PRIMOX-Workshop-Setup-1.0.0.exe` |
| Size | 61 686 497 bytes (~58.83 MB) |
| SHA256 | `9A08494D48208304A10913EF3C288214EDD5C10A9B10C416642AEFA99373A9C5` |
| Publish files | 590 · PDB=0 |
| Publish EXE SHA256 | `E5EE9DF4E3EE40E9F152AB1EC66061F47217DA0FD31EDCD6C90318BD1CDA9995` |
| Publish DLL SHA256 | `E330BF91558E0BDDE542559D1EC11C1292A19065E5DF84CE32D735FD2EE1706B` |
| Signing | UNSIGNED / BLOCKED (externo) |

Nenhum overwrite do installer 1.0.0 nesta fase.

---

## Validação de proteção

| Check | Status |
|---|---|
| `main` intacta no baseline | YES (`29b19b1`) |
| `v1.0.0` intacta | YES (`72d85fa` → `a4ad6fe`) |
| `primox-net6-final` = baseline | YES |
| `migration/net10` nascida do baseline | YES |
| TFM ainda `net6.0-windows` | YES |
| Push | NÃO |

---

## STOP

Fase NET10-00 concluída.  
**Não** iniciar NET10-01 automaticamente.  
**Não** alterar TargetFramework nesta fase.
