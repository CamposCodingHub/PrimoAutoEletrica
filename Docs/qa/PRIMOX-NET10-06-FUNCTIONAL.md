# PRIMOX NET10-06 — FULL FUNCTIONAL REGRESSION

**Data:** 2026-09-12  
**Branch:** `migration/net10`  
**HEAD inicial:** `f52224e`

## Objetivo

Regressão funcional real em net10: Unit, QaEngine, DeepQa, Exhaustive, LongRun, módulos e user journey.

## Correção (ciclo 1/3)

**Problema:** `Produtos:CadastroCompletoPelaTela` falhou — aba `Foto e Identificacao` (sem acento) ≠ header i18n `Foto e identificação`.  
**Correção:** `SelectTabByHeader` com normalização diacrítica + chaves LocalizationService no smoke Produtos.  
**Reteste:** Produtos **3/3 APROVADO**.

## CURRENTLY EXECUTED

| COMMAND | EXIT | RESULT | EVIDENCE |
|---|---:|---|---|
| `dotnet test` Release | 0 | **173/173** | `unit.txt` |
| DeepQa | 0 | **6/6** | `DeepQa/` |
| LongRun | 0 | **1/1** | `LongRun/` |
| ExhaustiveUi | 0 | **PASS** FullSimulation ~1397s | `ExhaustiveUi/` |
| QaEngine | 0 | **43/43** | `fix-QaEngine/` |
| Módulos (Veiculos…Configuracoes + fixes) | 0 | ver matriz | `module-results*.json` |
| Produtos pós-fix | 0 | **3/3** | `fix2-Produtos/` |
| Documentacao (Ajuda) | 0 | **1/1** | `fix-Documentacao/` |

### Matriz módulos (EXE net10)

| Filter | Status | Pass/Total |
|---|---|---:|
| DeepQa | APROVADO | 6/6 |
| Veiculos | APROVADO | 3/3 |
| OrdensServico | APROVADO | 2/2 |
| Orcamentos | APROVADO | 1/1 |
| PDV | APROVADO | 2/2 |
| Estoque | APROVADO | 2/2 |
| Financeiro | APROVADO | 3/3 |
| Fornecedores | APROVADO | 4/4 |
| Funcionarios | APROVADO | 3/3 |
| Agenda (Agendamentos) | APROVADO | 2/2 |
| Relatorios | APROVADO | 6/6 |
| Produtos (catálogo) | APROVADO pós-fix | 3/3 |
| ImportarNFe | APROVADO | 3/3 |
| Documentacao | APROVADO | 1/1 |
| Configuracoes | APROVADO | 2/2 |
| LongRun | APROVADO | 1/1 |
| ExhaustiveUi | APROVADO | 1/1 |
| Catalogo/Ajuda (nomes inválidos) | FALHOU harness | N/A — corrigido com Produtos/Documentacao |

## SIMULAÇÃO — Full User Journey

| Step | Status | Pass |
|---|---|---:|
| LoginSessao | APROVADO | 1 |
| Clientes | APROVADO | 3 |
| Veiculos | APROVADO | 3 |
| OrdensServico | APROVADO | 2 |
| Orcamentos | APROVADO | 1 |
| PDV | APROVADO | 2 |
| Estoque | APROVADO | 2 |
| Financeiro | APROVADO | 3 |
| Relatorios | APROVADO | 6 |

**JOURNEY_FAILS=0** · evidence `full-user-journey.json`

## Decisão

**PASS** (após 1 ciclo de correção harness i18n tabs). Prosseguir **NET10-07**.
