# PRIMOX FULL ASSURANCE-13 — DATABASE

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Data:** 11/09/2026

## Gates

| Check | Resultado | Evidência |
|---|---|---|
| `PRAGMA integrity_check` | **ok** | bulk DB + smoke `A13Database` |
| `PRAGMA foreign_key_check` | **0** | idem |
| Orphan scan | **0** | `orphans.csv` |
| Duplicate scan (CPF/placa/SKU) | **0** suspicious | `duplicates.csv` |
| Consistency (estoque harness) | **PASS** | saldo esperado vs PRIMOX |

## Orphan relations scanned

- Veiculos → Clientes  
- OrdensServico → Clientes / Veiculos  
- Orcamentos → Clientes  
- Agendamentos → Clientes  

## Duplicate policy

| Chave | Classificação se >1 |
|---|---|
| CPF | SUSPICIOUS / INVALID |
| Placa | SUSPICIOUS / INVALID |
| SKU (`Produtos.Codigo`) | SUSPICIOUS / INVALID |

Nenhuma encontrada no banco Bulk QA13.

## Smoke

`A13Database:IntegrityOrphanDuplicate` → PASS (`TestResults/UiSmoke/a13-db/`).
