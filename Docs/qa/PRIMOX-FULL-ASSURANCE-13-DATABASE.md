# PRIMOX FULL ASSURANCE-13 — DATABASE

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
