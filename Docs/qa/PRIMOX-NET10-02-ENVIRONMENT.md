# PRIMOX NET10-02 — ENVIRONMENT PREPARATION

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**BEFORE TFM:** `net6.0-windows`  
**AFTER TFM:** `net6.0-windows`

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE | TIMESTAMP |
|---|---:|---|---|---|
| `dotnet --list-sdks` | 0 | **10.0.302 only** | terminal | 2026-09-12 20:44 |
| `dotnet --list-runtimes` | 0 | Desktop 9.0.18 + 10.0.10 | terminal | idem |
| `dotnet build … -c Debug` (produto) | 0 | 0 erros | `…/product-build-debug-pre.txt` | 20:45 |
| `dotnet build … -c Release` | 0 | 0 erros · EXE 192512 | `…/product-build-release-pre.txt` | idem |
| File verify EXE | — | EXISTS sha `E5EE9DF4…` | bin Release | idem |

## Ambiente

| Item | Valor |
|---|---|
| SDK .NET 6 | **AUSENTE** (ENVIRONMENT LIMITATION) |
| SDK .NET 10 | **PRESENT** 10.0.302 |
| SignTool | Windows Kits 10.0.26100.0 |
| Inno | `C:\Program Files\Inno Setup 7\ISCC.exe` |
| global.json | ausente |

## SIMULAÇÃO

Build Debug+Release do produto **sem** mudar TFM: **PASS**.

## Decisão

**GREEN WITH ENVIRONMENT LIMITATION** (sem SDK 6 instalado; TFM ainda net6 via targeting/roll-forward).

Prosseguir NET10-03.
