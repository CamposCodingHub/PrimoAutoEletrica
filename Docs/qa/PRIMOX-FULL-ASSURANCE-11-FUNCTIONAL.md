# PRIMOX FULL ASSURANCE-11 — Functional

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Evidence root:** `TestResults/FullAssurance11/20260911-123215/`  
**Method:** QaEngine CompleteUi + DeepQa + Exhaustive (1882) + OvernightQa + LoginSessao — **VERIFIED** numbers from this run.

## Module matrix (summary)

| Module | Coverage | Result |
|--------|----------|--------|
| Clientes | QaEngine + Exhaustive CRUD/dialogs | **PASS** |
| Veículos | same | **PASS** |
| OS | DeepQa + Exhaustive state/actions | **PASS** |
| Orçamentos | Exhaustive | **PASS** |
| Agenda | Exhaustive + calendar known Dark limit | **PASS** |
| Estoque | Exhaustive | **PASS** |
| PDV / Caixa | Exhaustive (no fiscal emit) | **PASS** |
| Financeiro | Exhaustive | **PASS** |
| Relatórios | Exhaustive open/generate | **PASS** |
| Login | LoginSessao | **PASS** |
| Config | Exhaustive | **PASS** |
| Fornecedores / Funcionários | Exhaustive | **PASS** |
| Catálogo / Ajuda / NF-e import | Exhaustive | **PASS** |
| Backup/Restore | file-level hash on PackagingE2E DB | **PASS** |

## Workflow consistency

End-to-end paths exercised via DeepQa modules + Exhaustive cross-dialog operations. No P0/P1 financial divergence found in this run.

## Double-action / race

Exhaustive + Overnight nav stress (20 cycles) — no verified duplicate-record P0. Installer Clientes smoke Exit=2 remains **QA HARNESS / ENVIRONMENT** flake (C10 known).

## Findings

| Id | Sev | Notes |
|----|-----|-------|
| FUNC-000 | — | No product P0/P1 opened this phase |
| FUNC-HARNESS-01 | P3 harness | Installed-package Clientes smoke Exit=2 |
