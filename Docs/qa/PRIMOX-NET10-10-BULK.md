# PRIMOX NET10-10 — BULK DATA

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**HEAD:** `3d9d334`

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE |
|---|---:|---|---|
| Invoke-BulkDataGenerator (Add-Type DLL) | 1 | **BLOCKED** ReflectionTypeLoadException (WPF DLL via PowerShell) | `bulk-run.txt` |
| Run-UiSmoke BulkDataQa13 | 0 | **APROVADO** (asserts volumes + orphans + integrity interno) | `BulkDataQa13/` |
| PRAGMA integrity | 0 | **ok** | `counts.txt` |
| Counts (pós-bulk) | — | Clientes **501** · Veiculos **501** · Produtos **1001** · OS **1001** · Orc **501** · ContasRec **501** · ContasPag **501** · Agenda **501** | `counts.txt` |
| backup → restore → MainWindow | 0 | **RESTORE_REOPEN_EXIT=0** | `restore-reopen.txt` |

## SIMULAÇÃO

backup → restore → restart consultas: **PASS**.

## Limitações

- PowerShell Add-Type bulk generator incompatível com WPF net10 assembly → usar smoke in-process.
- Movimentações de estoque validadas pelo assert interno BulkDataQa13 (PASS); coluna SQL específica não recontada nesta sonda.

## Decisão

**PASS WITH LIMITATIONS**. Prosseguir **NET10-11**.
