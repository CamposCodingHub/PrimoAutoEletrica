# PRIMOX FULL ASSURANCE-13 — BULK DATA

**Data:** 11/09/2026  
**Filtro:** `BulkDataQa13` (`UiSmokeTestService.Assurance13.cs`)  
**Prefixo:** `QA13_`  
**Banco:** isolado (`--app-data=TestResults/UiSmoke/a13-bulk/appdata`)

## Evidência

| Módulo | Target | Actual | Resultado |
|---|---:|---:|---|
| CLIENTES | ≥500 | 500 (`QA13_`/`QA12_`) | **PASS** |
| VEÍCULOS | ≥500 | 500 (`Observacoes` QA13) | **PASS** |
| PRODUTOS | ≥1000 | 1000 | **PASS** |
| OS | ≥1000 | 1000 criadas (DB `OrdensServico`=1001 c/ fixture) | **PASS** |
| ORÇAMENTOS | ≥500 | 500 (DB=501 c/ fixture) | **PASS** |
| MOVIMENTAÇÕES ESTOQUE | ≥1000 | 500 Entrada + 500 Saída (audit `*EstoqueDedicada`) | **PASS** |
| FINANCEIRO | ≥1000 | 500 ContasReceber + 500 ContasPagar | **PASS** |
| AGENDA | ≥500 | 500 (`Numero` QA13%) | **PASS** |

Smoke: `TestResults/UiSmoke/a13-bulk/` — **1/1 PASS** (~9479 ms)

## Validações

- Saldo estoque: calculado independentemente no harness e comparado item a item → sem divergência (senão FAIL).
- `PRAGMA integrity_check` / `foreign_key_check` após bulk → ok / 0.
- Orphan scan pós-bulk: **0** (`TestResults/UiSmoke/a13-bulk/db-scans/`).
- Duplicate scan (CPF/placa/SKU): **0** grupos suspeitos.

## Cross-module

Seed liga Cliente → Veículo → OS/Orçamento/Agenda/Estoque/Financeiro no mesmo AppData isolado.

## Tipos de estoque

Somente `Entrada` / `Saida` (API real `EstoqueOperationalService`). Ajuste/consumo/estorno como labels de UI **não** inventados se não existirem na API manual.
