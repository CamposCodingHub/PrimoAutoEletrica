# PRIMOX NET10-03 — TARGET MIGRATION

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
**BEFORE TFM:** `net6.0-windows`  
**AFTER TFM:** `net10.0-windows`  
**Commit (fase):** a ser registrado após `git commit`

## Objetivo

Migrar TargetFramework do produto e testes para `net10.0-windows` na branch `migration/net10`, preservando win-x64 / self-contained / Trim=false / SingleFile=false, com build + unit + smoke reais.

## Estado inicial (ANTES)

| Check | Valor |
|---|---|
| branch | `migration/net10` |
| HEAD | `93611a4` (NET10-02) |
| main | `29b19b1` |
| v1.0.0 | `72d85fa` → `a4ad6fe` |
| primox-net6-final | `63aeb05` → `29b19b1` |
| TFM | `net6.0-windows` |

## Mudanças

- `PrimoAutoEletrica.csproj` TFM → `net10.0-windows`
- Sqlite / Sqlite.Core **9.0.9** · Extensions DI/Logging **9.0.9** · SQLitePCLRaw.bundle **2.1.11**
- `Tests/PrimoAutoEletrica.Tests` TFM → `net10.0-windows`
- `Scripts/Run-UiSmoke.ps1` lê `<TargetFramework>` do csproj (evita EXE net6 stale)

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE | TIMESTAMP |
|---|---:|---|---|---|
| `dotnet restore` | 0 | OK (NU1903 residual SQLitePCLRaw) | `TestResults/Net10-Overnight/20260912/net10-restore.txt` | 20:47 |
| `dotnet build -c Debug` | 0 | 0 erros / avisos presentes | `…/net10-build-debug.txt` | 20:47 |
| `dotnet build -c Release` | 0 | 0 erros · EXE 203776 bytes | `…/net10-build-release.txt` | 20:47 |
| File verify EXE net10 | — | EXISTS SHA256 `05D103E9…775B` | `PrimoAutoEletrica/bin/Release/net10.0-windows/` | 20:47 |
| `dotnet test` | 0 | **173/173 PASS** | `…/unit-net10.txt` | 20:48 |
| Run-UiSmoke MainWindow (net10 EXE) | 0 | **APROVADO** 1/1 | `…/MainWindow-net10/` | 20:48:56 |
| Run-UiSmoke QaEngine (net10 EXE) | 0 | **APROVADO** 43/43 | `…/QaEngine-net10/` | 20:54:17 |
| Run-UiSmoke (comma filter) | 1 | FAIL harness filter (não regressão produto) | documentado | 20:48 |
| First MainWindow via path net6 stale | 0 | **INVALID for net10** (descartado) | `startup-net10/MainWindow` | 20:48 |

## SIMULAÇÃO OBRIGATÓRIA

| Cenário | EXE | RESULT |
|---|---|---|
| Startup MainWindow | `bin\Release\net10.0-windows\PrimoAutoEletrica.exe` | **PASS** |
| QaEngine (login/dashboard/janelas) | mesmo EXE net10 | **PASS** 43/43 |

COMMAND (exemplo QaEngine):

```text
Scripts\Run-UiSmoke.ps1 -Configuration Release -SmokeFilter QaEngine -SkipBuild
  -OutputDirectory TestResults\Net10-Overnight\20260912\QaEngine-net10
```

EXIT CODE: **0**  
RESULT: Status APROVADO · FailedChecks 0

## Problemas / correções

1. Script forçava net6 → smoke lia EXE antigo → **corrigido** (TFM do csproj).
2. NU1903 SQLitePCLRaw permanece em 2.1.11 → **limitação** (tratar em NET10-04).
3. NU1701 LiveCharts/OpenTK → conhecido / não bloqueante.

## Proteção Git (DEPOIS pré-commit)

| Ref | Valor |
|---|---|
| branch | `migration/net10` |
| main | `29b19b1` **intacta** |
| v1.0.0 | `72d85fa` → `a4ad6fe` **intacta** |
| primox-net6-final | `63aeb05` → `29b19b1` **intacta** |

## Decisão

**PASS WITH LIMITATIONS** — Build Debug/Release, Unit 173/173, MainWindow e QaEngine no EXE **net10** comprovados.  
Prosseguir **NET10-04** (compatibility fix loop + NU1903 / scripts TFM).
