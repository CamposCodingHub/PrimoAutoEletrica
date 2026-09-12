# PRIMOX NET10-01 — COMPATIBILITY ASSESSMENT

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**BEFORE TFM:** `net6.0-windows`  
**AFTER TFM:** `net6.0-windows` (sem alteração nesta fase)  
**Decisão:** **YELLOW — PROCEED** (riscos conhecidos; sem blocker estrutural)

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE | TIMESTAMP |
|---|---:|---|---|---|
| `dotnet list PrimoAutoEletrica.csproj package` | 0 | packages listados | `TestResults/Net10-Overnight/20260912/packages-list.txt` | 2026-09-12 20:45 |
| Obsolete/API scan `Select-String` | 0 | 1 hit SqlConnectionStringBuilder (não Obsolete) | `…/obsolete-scan.txt` | idem |
| `dotnet build Tools/Net10CompatProbe` net10.0-windows | 0 | PASS | `…/probe-build.txt` | idem |
| Run `Net10CompatProbe.exe` | 0 | `PROBE_OK sqlite=1 wpf=10.0.0.0` | `…/probe-run.txt` size=162304 | idem |

## Matrix

| COMPONENTE | .NET 6 | .NET 10 | STATUS | RISCO | AÇÃO |
|---|---|---|---|---|---|
| TFM produto | net6.0-windows | net10.0-windows (alvo) | YELLOW | EOL TFM 6 | migrar em NET10-03 |
| SDK máquina | ausente | 10.0.302 | YELLOW | build 6 via roll-forward | OK p/ migração |
| WPF | UseWPF | probe WPF 10.0.0.0 | GREEN | baixo | — |
| Microsoft.Data.Sqlite | 7.0.20 | probe 9.0.0 OK | YELLOW | NU1903 SQLitePCLRaw 2.1.10 | bump na migração |
| Microsoft.Extensions.* | 7.0.0 | probe 9.0.0 OK | YELLOW | alinhar | bump |
| LiveCharts SkiaSharp WPF | rc5.4 | NU1701 netfx | YELLOW | compat WPF | validar pós-TFM |
| OpenTK transitivo | NU1701 | idem | YELLOW | charts | validar |
| EPPlus / PdfSharp / Dapper / Otp | atuais | esperado OK | GREEN | baixo | validar build |
| DPAPI / Registry / Process | Windows | Windows | GREEN | CA1416 | manter |
| Installer Inno 7 | ISCC presente | — | GREEN | — | artefato experimental depois |
| Fiscal | foundation | — | YELLOW | não LIVE | NET10-11 |

## SIMULAÇÃO

Probe `net10.0-windows` + Sqlite in-memory + WPF type load: **PASS** (produto TFM intacto).

## STOP → CONTINUE

Sem blocker. Prosseguir NET10-02.
