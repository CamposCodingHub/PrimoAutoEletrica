# PRIMOX — Melhorias & Bugs (checklist vivo)

**Fonte oficial de melhorias contínuas.** Não criar arquivo concorrente.  
Itens resolvidos permanecem no histórico.

**Fase ativa:** Exhaustive UI Audit 2.0 (pós-15E)  
**Idiomas suportados:** `pt-BR`, `en-US`, `es-ES`

---

## Checklist Fase 15E

- [x] P15E-001 FocusVisualStyle UnsetValue — CRITICAL — BUG-FOCUS — **FIXED** — VERIFIED (`CompleteUiFocusVisualStyle` + ExhaustiveUi 0 ocorrências)
- [x] P15E-002 Login mover janela → FocusVisualStyle — BUG-FOCUS / BUG-WINDOW — **FIXED** — VERIFIED (causa raiz P15E-001)
- [x] P15E-003 Login botão X cortado/deslocado/contraste — BUG-UI / BUG-LAYOUT — **FIXED** — VERIFIED
- [x] P15E-004 PDV janela Produtos layout/scroll/foco — BUG-LAYOUT / BUG-FOCUS — **FIXED** — VERIFIED
- [x] P15E-005 NF-e Buscar Dark branco — BUG-DARK — **FIXED** — VERIFIED
- [x] P15E-006 NF-e Período Inicial/Final Dark branco — BUG-DARK — **FIXED** — VERIFIED
- [x] P15E-007 Clientes Buscar Dark branco — BUG-DARK — **FIXED** — VERIFIED
- [x] P15E-008 Clientes placeholder pesquisa (3 idiomas) — MISSING-PLACEHOLDER — **FIXED** — VERIFIED
- [x] P15E-009 Veículos placeholder pesquisa (3 idiomas) — MISSING-PLACEHOLDER — **FIXED** — VERIFIED
- [x] P15E-010 Veículos coluna Ações identificação — BUG-ACCESSIBILITY — **FIXED** — VERIFIED
- [x] P15E-011 Auto Elétrica Técnica campos Dark brancos — BUG-DARK — **FIXED** — VERIFIED
- [x] P15E-012 Estoque Novo Produto botões / janelas secundárias — BUG-STYLING — **FIXED** (ModalAccent BasedOn Button) — auditoria recursiva de TODOS os modais: **PARTIAL** (CompleteUi amostra; ExhaustiveUi cobre runtime com depth guard)
- [x] P15E-013 Catálogo Buscar Dark branco — BUG-DARK — **FIXED** — VERIFIED
- [x] P15E-014 Funcionários layout / Ações cortadas — BUG-LAYOUT — **FIXED** — VERIFIED

---

## Checklist Exhaustive Audit 2.0

- [x] P15E-016 Classificador Exhaustive (DataGrid headers / CalendarDay / ScrollBar) — FALSE_POSITIVE → **FIXED** — VERIFIED (FAIL 797→0)
- [x] P15E-017 Popups de regra de negócio tratados como FAIL — FALSE_POSITIVE → **FIXED** (`expected-business`)
- [ ] P15E-015 Botões icon-only sem ToolTip/AutomationName — BUG-ACCESSIBILITY — **OPEN** (~16 findings Exhaustive; não crash)

---

## Registro detalhado

### P15E-001
| Campo | Valor |
|-------|-------|
| ID | P15E-001 |
| Módulo | Global |
| Tela | Login, PDV, Veículos, Auto Elétrica, … |
| Controle | FocusVisualStyle / KeyboardNavigation |
| Categoria | BUG-FOCUS |
| Severidade | CRITICAL |
| Descrição | `InvalidOperationException: '{DependencyProperty.UnsetValue}' não é um valor válido para a propriedade 'FocusVisualStyle'` |
| Light/Dark | Ambos |
| Status | **FIXED** — VERIFIED |
| Correção aplicada | `FocusVisualStyleHealer` + estilos seguros |
| Commit | `48e591d` |
| Reteste | CompleteUi + ExhaustiveUi 2026-09-08 (0 ocorrências) |

### P15E-002
| Campo | Valor |
|-------|-------|
| ID | P15E-002 |
| Status | **FIXED** — VERIFIED (mesma causa P15E-001) |

### P15E-003
| Campo | Valor |
|-------|-------|
| ID | P15E-003 |
| Status | **FIXED** — VERIFIED |

### P15E-004
| Campo | Valor |
|-------|-------|
| ID | P15E-004 |
| Status | **FIXED** — VERIFIED |

### P15E-005 / P15E-006 / P15E-007 / P15E-011 / P15E-013
| Campo | Valor |
|-------|-------|
| IDs | P15E-005,006,007,011,013 |
| Categoria | BUG-DARK |
| Status | **FIXED** — VERIFIED |

### P15E-008 / P15E-009
| Campo | Valor |
|-------|-------|
| IDs | P15E-008, P15E-009 |
| Status | **FIXED** — VERIFIED |

### P15E-010
| Campo | Valor |
|-------|-------|
| ID | P15E-010 |
| Status | **FIXED** — VERIFIED |

### P15E-012
| Campo | Valor |
|-------|-------|
| ID | P15E-012 |
| Módulo | Estoque |
| Tela | NovoProdutoWindow (+ demais modais) |
| Categoria | BUG-STYLING |
| Status | **FIXED** (styling) / cobertura modal **PARTIAL** |
| Reteste | ExhaustiveUi visitou 34 windows; depth max 5 |

### P15E-014
| Campo | Valor |
|-------|-------|
| ID | P15E-014 |
| Status | **FIXED** — VERIFIED |

### P15E-015
| Campo | Valor |
|-------|-------|
| ID | P15E-015 |
| Título | Icon-only buttons sem identidade acessível |
| Módulo | Vários |
| Categoria | BUG-ACCESSIBILITY |
| Severidade | LOW |
| Descrição | ExhaustiveUi marcou ~16 botões com `ACCESSIBILITY ISSUE / UNIDENTIFIED_BUTTON` |
| Evidência | `exhaustive-buttons-latest.csv` Detail |
| Status | **OPEN** |
| Correção | (pendente — não bloqueia Exhaustive PASS) |

### P15E-016
| Campo | Valor |
|-------|-------|
| ID | P15E-016 |
| Título | Falsos positivos do classificador Exhaustive |
| Categoria | TEST_ENGINE_BUG / FALSE_POSITIVE |
| Descrição | Pass 1: 797 FAIL por headers DataGrid, fora da janela, etc. |
| Status | **FIXED** — VERIFIED |
| Commit | `9856f23` |
| Reteste | ExhaustiveUi FAIL=0 (2026-09-08 14:56) |

### P15E-017
| Campo | Valor |
|-------|-------|
| ID | P15E-017 |
| Título | Validação de negócio classificada como FAIL |
| Categoria | FALSE_POSITIVE |
| Descrição | MessageBox “Exclusao bloqueada”, campos obrigatórios, etc. |
| Status | **FIXED** — VERIFIED (`expected-business` / EXPECTED_VALIDATION) |
| Commit | `9856f23` |

---

## Novos itens descobertos na auditoria Exhaustive 2.0

*(nenhum PRODUCT_BUG com FAIL de botão; limitações em relatório Exhaustive)*
