# PRIMOX NET10-12 — PUBLISH

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**HEAD:** `7684279`

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE |
|---|---:|---|---|
| `dotnet publish -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false` | 0 | output `artifacts/publish/net10-preview-win-x64` | `publish-log.txt` |
| Audit | — | files **484** · publishBytes **195701494** (~186.6 MB) · PDB **0** · DB **0** · EXE SHA256 `05D103E9…775B` | `publish-audit.txt` |
| Published EXE Clientes smoke | 0 | **PASS** | `published-smoke-exit.txt` |
| Published EXE MainWindow | 0 | **PASS** | same |

Preservado: Trim=false · SingleFile=false · win-x64 self-contained · TFM net10.0-windows.

## Comparação tamanho

| | Valor |
|---|---|
| NET10 publish folder | ~186.6 MB (484 files) |
| NET6 commercial setup histórico | ~59 MB setup (comprimido) — categorias distintas |

## SIMULAÇÃO

EXE publicado fora da pasta de build/obj: startup+CRUD Clientes+shutdown: **PASS**.

## Decisão

**PASS**. Prosseguir **NET10-13** (installer experimental separado).
