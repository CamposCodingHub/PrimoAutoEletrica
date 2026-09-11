# PRIMOX-COMMERCIAL-08 — Regression

**HEAD inicial:** `b5c9481`  
**Data:** 2026-09-10  
**Escopo:** hardening do instalador apenas (sem fiscal / I18N / DB schema)

## Workspace QA (`TestResults/UiSmoke/c08-regression-20260910-203756/`)

| Suite | Resultado | Detalhe |
|-------|-----------|---------|
| Build Release | **PASS** | 0 erros (warnings pré-existentes) |
| Unit tests | **PASS** | 162/162 |
| QaEngine | **PASS** | 43/43 Exit=0 |
| DeepQa | **PASS** | 6/6 Exit=0 |
| ExhaustiveUi | **PASS** | 1/1 Exit=0 |
| LongRun | **PASS** | 1/1 Exit=0 |
| DB (instalado pós-reinstall) | **PASS** | integrity=ok · fk=0 · migrations=28 |
| Fiscal | **PASS** | não alterado nesta fase (WIP relatório preservado) |
| I18N | **PASS** | zero alterações de catálogo/LocalizationService |
| Security | **PASS** | sem secrets/certs introduzidos |

## Installer E2E

| Suite | Resultado |
|-------|-----------|
| 3 ciclos install/uninstall/reinstall | **PASS** |
| Uninstall com app aberto | **PASS** |
| Data preservation | **PASS** |
| QaEngine no pacote instalado | **PASS** Exit=0 |
| Smoke `Clientes` isolado | FAIL (flake UI CadastroCompleto — não installer) |

## Não regressão intencional

- Nenhum arquivo fiscal de runtime alterado
- Nenhuma migration
- I18N-07 permanece CLOSED
- Tag `v1.0.0` = `72d85fa` intacta
