# PRIMOX NET10-04 — COMPATIBILITY FIX LOOP

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

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**HEAD inicial:** `dce6d27` (NET10-03)

## Objetivo

Corrigir incompatibilidades reais pós-migração TFM sem suppressions injustificadas; garantir smoke Login/Dashboard no EXE net10.

## Correções

1. **SQLitePCLRaw.bundle_e_sqlite3** `2.1.11` → **`3.0.5`** — elimina **NU1903** (build 0 erros).
2. **Run-UiSmoke.ps1** — TFM sempre do csproj (salvo `-ForceFramework`).
3. **Invoke-BulkDataGenerator.ps1** / **Invoke-RecoverySimulation.ps1** / **Invoke-Assurance13PerformanceProfile.ps1** — paths TFM dinâmicos.
4. **Deploy-ToInstalledApp.ps1** — fallback TFM `net10.0-windows`.

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE | TIMESTAMP |
|---|---:|---|---|---|
| `dotnet add … SQLitePCLRaw 3.0.5` + restore/build Release | 0 | 0 erros · **NU1903 ausente** | `pclraw3-*.txt` | 20:55 |
| `dotnet build -c Release` | 0 | 0 erros | `net10-04-build-release.txt` | 20:57 |
| `dotnet test` Release | 0 | **173/173 PASS** | `unit-net10-04b.txt` | 20:57 |
| Run-UiSmoke LoginSessao | 0 | **APROVADO** | `LoginSessao-net10/` | 20:57:04 |
| Run-UiSmoke Dashboard | 0 | **APROVADO** | `Dashboard-net10/` | 20:57:09 |
| QaEngine (NET10-03, ainda válido pós-fix) | 0 | **43/43** | `QaEngine-net10/` | 20:54 |

## SIMULAÇÃO

startup + login + Dashboard + navegação básica via filtros **LoginSessao** e **Dashboard** no EXE `net10.0-windows`: **PASS**.

## Limitações restantes

- NU1701 LiveCharts/OpenTK/SkiaSharp.Views.WPF (pacotes netfx) — não bloqueante.
- Scripts Assurance/Commercial ainda aceitam `-Framework net6.0-windows` por parâmetro default em alguns orquestradores; Run-UiSmoke agora ignora e usa csproj.

## Proteção Git

main / v1.0.0 / primox-net6-final **intactos** (verificados no commit).

## Decisão

**PASS** — loop de correção sem P0/P1. Prosseguir **NET10-05**.
