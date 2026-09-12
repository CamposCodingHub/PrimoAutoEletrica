# PRIMOX FULL ASSURANCE-13 — REGRESSION

**Data:** 11/09/2026  
**Orquestrador:** `Scripts/Run-FullAssurance13.ps1`  
**Out:** `TestResults/FullAssurance13/20260911-223751/` (+ retests pontuais)

## Build / Unit

| Gate | Resultado |
|---|---|
| Debug | PASS |
| Release | PASS |
| Unit | PASS 173/173 |

## Security

| Gate | Resultado |
|---|---|
| SecurityRedTeam | PASS (DecisionHint YELLOW = Informational only) |
| PathTraversalSim | PASS |
| A13Security | PASS 1/1 |
| A12Security | PASS 3/3 |
| A13Concurrency | PASS 1/1 |

## Bulk / DB / Perf

| Gate | Resultado |
|---|---|
| BulkDataQa13 | PASS |
| A13Database | PASS |
| A13Performance | PASS |
| StartupProfile 10× | PASS |
| DB integrity/FK/orphan/dup (bulk) | ok / 0 / 0 / 0 |
| Backup/restore file copy on bulk DB | PASS |
| Recovery 3× | PASS |

## Functional smoke

| Módulo | Resultado |
|---|---|
| QaEngine | 43/43 PASS |
| DeepQa | 6/6 PASS |
| LoginSessao | PASS |
| LongRun | PASS |
| ExhaustiveUi | PASS (discovered=3292 tested=1882 pass=1882 fail=0) |
| I18n07 | PASS |
| Tema | 2/2 PASS |
| Sidebar | PASS |
| OvernightQa | 3/3 PASS |
| Clientes | 3/3 PASS |
| OrdensServico | **FAIL→PASS** após Tag prioridade i18n |
| Orcamentos | **FAIL→PASS** após assert `QuoteNearExpiry` |
| Agendamentos | 2/2 PASS |
| Estoque | 2/2 PASS |
| PDV | 2/2 PASS |
| Financeiro | 3/3 PASS |
| Relatorios | 6/6 PASS |
| Configuracoes | 2/2 PASS |
| Fornecedores | 4/4 PASS |
| Funcionarios | 3/3 PASS |

## Installer

3 ciclos + uninstall app aberto + reinstall + Final CRUD Clientes: **fails=0**, Exit=0 (Clientes Exit=2 = 0).

## Fiscal

LIVE **BLOCKED** (fora de escopo). Unit/FakeFiscal cobertos via suíte existente (QaEngine/units) — não reexecutado LIVE.
