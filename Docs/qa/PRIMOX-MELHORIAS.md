# PRIMOX — Melhorias & Bugs (checklist vivo)

**Fonte oficial de melhorias contínuas.** Não criar arquivo concorrente.  
Itens resolvidos permanecem no histórico.

**Fase ativa:** 15E — Complete UI Interaction, Visual QA & Button-by-Button Audit  
**Idiomas suportados:** `pt-BR`, `en-US`, `es-ES`

---

## Checklist Fase 15E

- [x] P15E-001 FocusVisualStyle UnsetValue — CRITICAL — BUG-FOCUS — **FIXED** — VERIFIED (`CompleteUiFocusVisualStyle`)
- [x] P15E-002 Login mover janela → FocusVisualStyle — BUG-FOCUS / BUG-WINDOW — **FIXED** (mesma causa raiz P15E-001)
- [x] P15E-003 Login botão X cortado/deslocado/contraste — BUG-UI / BUG-LAYOUT — **FIXED** (Path + contraste Accent)
- [x] P15E-004 PDV janela Produtos layout/scroll/foco — BUG-LAYOUT / BUG-FOCUS — **FIXED** (960×640, denser)
- [x] P15E-005 NF-e Buscar Dark branco — BUG-DARK — **FIXED** (TextBox implícito + SearchBox)
- [x] P15E-006 NF-e Período Inicial/Final Dark branco — BUG-DARK — **FIXED** (DatePickerTextBox style)
- [x] P15E-007 Clientes Buscar Dark branco — BUG-DARK — **FIXED**
- [x] P15E-008 Clientes placeholder pesquisa (3 idiomas) — MISSING-PLACEHOLDER — **FIXED** (`SearchPlaceholder`)
- [x] P15E-009 Veículos placeholder pesquisa (3 idiomas) — MISSING-PLACEHOLDER — **FIXED**
- [x] P15E-010 Veículos coluna Ações identificação — BUG-ACCESSIBILITY — **FIXED** (ToolTips + AutomationName)
- [x] P15E-011 Auto Elétrica Técnica campos Dark brancos — BUG-DARK — **FIXED** (TextBox implícito)
- [x] P15E-012 Estoque Novo Produto botões / janelas secundárias — BUG-STYLING — **FIXED** (ModalAccent BasedOn Button) — auditoria recursiva de TODOS os modais: **PARTIAL** (CompleteUi cobre amostra)
- [x] P15E-013 Catálogo Buscar Dark branco — BUG-DARK — **FIXED**
- [x] P15E-014 Funcionários layout / Ações cortadas — BUG-LAYOUT — **FIXED** (coluna 250px)

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
| Como reproduzir | Foco teclado / mover janela / Tab; códigos 20260908111133470, 20260908111511965, 20260908112008769, 20260908112029685 |
| Light/Dark | Ambos |
| Resolução | Várias |
| Status | IN_PROGRESS |
| Correção aplicada | (em andamento) |
| Commit | |
| Teste de regressão | |

### P15E-002
| Campo | Valor |
|-------|-------|
| ID | P15E-002 |
| Módulo | Auth |
| Tela | LoginWindow |
| Categoria | BUG-FOCUS / BUG-WINDOW |
| Severidade | HIGH |
| Descrição | Mover a janela de Login dispara FocusVisualStyle |
| Status | IN_PROGRESS |

### P15E-003
| Campo | Valor |
|-------|-------|
| ID | P15E-003 |
| Módulo | Auth |
| Tela | LoginWindow |
| Controle | CloseButton |
| Categoria | BUG-UI / BUG-LAYOUT |
| Severidade | MEDIUM |
| Descrição | X de fechar parece fora da área, mal dimensionado, contraste fraco (Icon.Close usa PrimaryTextBrush sobre Navy) |
| Status | IN_PROGRESS |

### P15E-004
| Campo | Valor |
|-------|-------|
| ID | P15E-004 |
| Módulo | PDV |
| Tela | SelecionarProdutoPDVWindow |
| Categoria | BUG-LAYOUT / BUG-FOCUS |
| Severidade | HIGH |
| Descrição | Janela excessivamente grande (MinWidth 1100 / MinHeight 720), espaçamento, scroll horizontal; FocusVisualStyle ao interagir |
| Status | IN_PROGRESS |

### P15E-005 / P15E-006 / P15E-007 / P15E-011 / P15E-013
| Campo | Valor |
|-------|-------|
| IDs | P15E-005,006,007,011,013 |
| Categoria | BUG-DARK |
| Severidade | HIGH |
| Descrição | TextBox sem estilo implícito usa chrome WPF padrão (branco) no Dark; DatePickerTextBox pode manter superfície clara |
| Causa raiz candidata | Ausência de `Style TargetType=TextBox` / `PasswordBox`; DatePickerTextBox template nativo |
| Status | IN_PROGRESS |

### P15E-008 / P15E-009
| Campo | Valor |
|-------|-------|
| IDs | P15E-008, P15E-009 |
| Categoria | MISSING-PLACEHOLDER |
| Severidade | LOW |
| Descrição | Campos de pesquisa sem placeholder localizado |
| Status | IN_PROGRESS |

### P15E-010
| Campo | Valor |
|-------|-------|
| ID | P15E-010 |
| Módulo | Veículos |
| Controle | Coluna Ações |
| Categoria | BUG-ACCESSIBILITY / MISSING-TOOLTIP |
| Severidade | MEDIUM |
| Descrição | Botões Ver/Edit/Del sem ToolTip claro |
| Status | IN_PROGRESS |

### P15E-012
| Campo | Valor |
|-------|-------|
| ID | P15E-012 |
| Módulo | Estoque |
| Tela | NovoProdutoWindow (+ demais modais) |
| Categoria | BUG-STYLING |
| Severidade | MEDIUM |
| Descrição | Botões de cadastro aparentam fora do Design System |
| Status | OPEN |

### P15E-014
| Campo | Valor |
|-------|-------|
| ID | P15E-014 |
| Módulo | Funcionários |
| Controle | DataGrid coluna Ações Width=190 |
| Categoria | BUG-LAYOUT |
| Severidade | HIGH |
| Descrição | Botões Ver/Editar/Inativar comprimidos/cortados |
| Status | IN_PROGRESS |

---

## Novos itens descobertos na auditoria

*(preenchido durante execução)*
