# C3.21 FINAL — Full app regression

**Decision:** **PASS** (after BUG-C3-LIVE-001 FOUND→FIXED→RETEST)  
**Date:** 2026-09-27 America/Sao_Paulo

## Inventory

Modules/areas from UiSmoke partials: Agendamentos, AutoEletrica, Calendar, CatalogoImport, Clientes, Components, Configuracoes, Dashboard, DeepQa, Discovery, Documentacao, Documentos, Dvi, Estoque, ExhaustiveUi, Financeiro, Fornecedores, Funcionarios, Login, Modals, Navigation, NFe, OficinaKanban, Orcamentos, OrdensServico, PDV, Produtos, Relatorios, Robustez, Search, Shell, Theme, Veiculos, PrimoxQa/CompleteUi, Assurance12/13, I18n04, OvernightQa.

Prior empty-filter page groups: see `QA_EVIDENCE/C3_LIVE/page_inventory_from_preflight217.txt`.

## Runs

| Run | Result | Path |
|-----|--------|------|
| Full App first | **218/219 FAIL** ExternalAiSettingsWindow | `TestResults/UiSmoke/c3_live_fullapp_2026-09-27` |
| Targeted retest | **1/1 PASS** | `TestResults/UiSmoke/c3_live_retest_extai2_2026-09-27` |
| Full App retest | **219/219 PASS** | `TestResults/UiSmoke/c3_live_fullapp_retest_2026-09-27` |

No silent omit. BUG history retained in ERRORS FOUND / `QA_EVIDENCE/C3_LIVE/BUG-C3-LIVE-001.md`.
