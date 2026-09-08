# PRIMOX — Melhorias & Bugs (checklist vivo)

**Fonte oficial de melhorias contínuas.** Não criar arquivo concorrente.  
Itens resolvidos permanecem no histórico.

**Fase ativa:** Exhaustive UI Audit **3.0** (fecho; pós-15E)  
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
- [x] P15E-012 Estoque Novo Produto botões / janelas secundárias — BUG-STYLING — **FIXED** (ModalAccent BasedOn Button) — cobertura modal runtime Exhaustive 3.0: **VERIFIED** (35 windows; não 100% estático XAML)
- [x] P15E-013 Catálogo Buscar Dark branco — BUG-DARK — **FIXED** — VERIFIED
- [x] P15E-014 Funcionários layout / Ações cortadas — BUG-LAYOUT — **FIXED** — VERIFIED

---

## Checklist Exhaustive Audit 2.0 / 3.0

- [x] P15E-016 Classificador Exhaustive (DataGrid headers / CalendarDay / ScrollBar) — FALSE_POSITIVE → **FIXED** — VERIFIED (FAIL 797→0)
- [x] P15E-017 Popups de regra de negócio tratados como FAIL — FALSE_POSITIVE → **FIXED** (`expected-business`)
- [x] P15E-018 Host disposed mid-queue (102 BLOCKED) — QA_ENGINE_BUG → **FIXED** — VERIFIED (Exhaustive 3.0 BLOCKED=0)
- [ ] P15E-015 Botões icon-only / sem identidade acessível — BUG-ACCESSIBILITY — **PARTIAL** (~30 findings Exhaustive 3.0; ModalCloseButton agora com Name/ToolTip; AutoEletrica chrome + OS outside-window ainda OPEN)

---

## Registro detalhado

### P15E-001
| Campo | Valor |
|-------|-------|
| ID | P15E-001 |
| Status | **FIXED** — VERIFIED |
| Reteste | CompleteUi + ExhaustiveUi 3.0 (0 FocusVisualStyle UnsetValue) |

### P15E-002 … P15E-011 / P15E-013 / P15E-014
| Campo | Valor |
|-------|-------|
| Status | **FIXED** — VERIFIED (Audit 3.0 regressão) |

### P15E-012
| Campo | Valor |
|-------|-------|
| ID | P15E-012 |
| Módulo | Estoque (+ demais modais) |
| Categoria | BUG-STYLING / modal coverage |
| Status styling | **FIXED** |
| Status cobertura modal | **VERIFIED** (runtime Exhaustive 3.0: 35 windows, guardian modal-explore-then-close) |
| Nota | Não declara inventário estático 100% de todo Window XAML do repo |
| Reteste | Exhaustive 3.0 `ui-smoke-2026-09-08-16-03-42-006-p12852.txt` |

### P15E-015
| Campo | Valor |
|-------|-------|
| ID | P15E-015 |
| Título | Icon-only / UNIDENTIFIED_BUTTON sem ToolTip/AutomationName |
| Categoria | BUG-ACCESSIBILITY |
| Severidade | LOW |
| Descrição | Exhaustive 3.0: ~30 linhas `ACCESSIBILITY ISSUE / UNIDENTIFIED_BUTTON` (16 AutoEletrica chrome sem Name/Content; 14 OrdensServico/OrdemServicoWindow outside-window) |
| Status | **PARTIAL** |
| Correção parcial | `ModalCloseButton`: ToolTip + AutomationProperties.Name="Fechar" |
| Restante | AutoEletrica residual chrome; botões OS outside-window sem identidade |
| Bloqueia Exhaustive? | Não (PASS funcional; finding de a11y) |

### P15E-016
| Campo | Valor |
|-------|-------|
| Status | **FIXED** — VERIFIED |
| Commit | `9856f23` |

### P15E-017
| Campo | Valor |
|-------|-------|
| Status | **FIXED** — VERIFIED (`expected-business`) |
| Commit | `9856f23` |

### P15E-018
| Campo | Valor |
|-------|-------|
| ID | P15E-018 |
| Título | 102 BLOCKED Host disposed mid-queue |
| Categoria | QA_ENGINE_BUG |
| Descrição | Cancel/close mid-queue; Login Close→Shutdown; native dismiss tratava WPF “Abrir caixa” como file dialog |
| Status | **FIXED** — VERIFIED |
| Commits | `d033c2b`, `292ec11`, `80237e5`, `e31e5e8` |
| Reteste | Exhaustive 3.0 BLOCKED=0 · tested/executable 100% |

---

## Novos itens Audit 3.0

*(nenhum PRODUCT_BUG com FAIL de botão; P15E-015 PARTIAL a11y; limitações NOT_TESTABLE documentadas no fecho)*
