# PRIMOX — Melhorias & Bugs (checklist vivo)

**Fonte oficial de melhorias contínuas.** Não criar arquivo concorrente.  
Itens resolvidos permanecem no histórico.

**Fase ativa:** Fiscal Provider Decision 1.0 — **FISCAL ARCHITECTURE DECISION READY** (sem implementação)  
**Idiomas suportados:** `pt-BR`, `en-US`, `es-ES` (Ajuda operacional longa = **pt-BR**)

> Verdade comercial: `Docs/qa/PRIMOX-PRODUCT-TRUTH-AUDIT-1.0.md` — **PRODUCT TRUTH VERIFIED WITH LIMITATIONS**.  
> Auditoria total: `Docs/qa/PRIMOX-TOTAL-CODEBASE-INTEGRATION-AUDIT-1.0.md` — **COMMERCIAL READY WITH LIMITATIONS**.  
> Sanitização: `Docs/qa/PRIMOX-CODEBASE-SANITIZATION-1.0-REPORT.md` — **SANITIZED WITH LIMITATIONS**.  
> Fiscal: `Docs/qa/PRIMOX-FISCAL-PROVIDER-AUDIT-1.0.md` · `PRIMOX-FISCAL-DECISION.md` — **Focus NFe recomendado**; **NÃO implementar emissão ainda**.  
> Ajuda: `Docs/qa/PRIMOX-HELP-AUDIT-1.0.md` · `PRIMOX-HELP-COVERAGE-MATRIX.md`.  
> Roadmap: `Docs/qa/PRIMOX-100-PERCENT-ROADMAP.md` — **REQUIRES PRODUCT DECISION**.  
> A11y: `Docs/qa/PRIMOX-ACCESSIBILITY-CLOSURE-REPORT.md` — **P15E-015 VERIFIED**.

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
- [x] P15E-015 Botões icon-only / sem identidade acessível — BUG-ACCESSIBILITY — **VERIFIED** (Exhaustive ACCESSIBILITY=0; AccessibilityChromeHealer + DatePicker PART_Button; 2026-09-08)

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
| Severidade | HIGH (fechado) |
| Descrição | Exhaustive 3.0: ~30 linhas ACCESSIBILITY (DataGrid SelectAll + DatePicker PART_Button) |
| Status | **VERIFIED** |
| Correção | `AccessibilityChromeHealer`; PremiumDatePicker Name/ToolTip; ModalClose; Command Center Name |
| Evidência | Exhaustive `…18-11-31…` ACCESSIBILITY=0; DeepQa AcessibilidadeFocoEIdentidade PASS |
| Relatório | `Docs/qa/PRIMOX-ACCESSIBILITY-CLOSURE-REPORT.md` |
| Data | 2026-09-08 |

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

---

## AUDITORIA TOTAL — INTEGRAÇÕES + CODEBASE (2026-09-08)

**Relatório:** `Docs/qa/PRIMOX-TOTAL-CODEBASE-INTEGRATION-AUDIT-1.0.md`  
**Decisão:** COMMERCIAL READY WITH LIMITATIONS  
**Remoções de código:** **0** (conservador)

### Achados

| Área | Classificação |
|------|---------------|
| WhatsApp `wa.me` | REAL |
| NotificationService SMS/WA | PLACEHOLDER (Delay + “Enviado”) |
| Twilio HTTP | NÃO IMPLEMENTADO |
| SMTP | NÃO IMPLEMENTADO |
| PIX interno | REAL; gateway NÃO IMPLEMENTADO |
| NF-e import | REAL; emissão NÃO IMPLEMENTADO (`NFeEmissaoService` 0 bytes) |
| NFC-e / NFS-e / cert A1 | NÃO IMPLEMENTADO |
| API | PARCIAL (sem JWT wired) |
| Filial / sync remoto / SaaS | SCAFFOLD / NÃO IMPLEMENTADO |
| Migrations código | 27 CURRENT vs ~32 histórico DB |

### Correções nesta auditoria

- Somente documentação + matrizes + arquitetura. **Sem** features.

### Arquivos removidos

- Nenhum.

### Arquivos / infra preservados

- QA engines (Exhaustive, QaEngine, DeepQa, CompleteUi, Long Run)  
- WIP Help + Deploy scripts  
- Tag `v1.0.0` → `a4ad6fe`  
- Empty shells / orphans listados como candidatos (não apagados)

### Integrações / gaps / decisões pendentes

- Matrizes: `PRIMOX-INTEGRATION-MATRIX.md`, `PRIMOX-INTEGRATION-GAPS.md`, `PRIMOX-CODEBASE-CLEANUP-MATRIX.md`  
- Arch: `PRIMOX-INTEGRATION-ARCHITECTURE.md` + updates fiscal/sync/cloud  
- Pendente dono: A/B fiscal; prioridade API vs filial vs fiscal; GO limpeza 0-byte; horizonte SaaS

### Itens abertos derivados (não bugs Exhaustive)

| ID | Tema | Status |
|----|------|--------|
| INT-001 | Neutralizar NotificationService fake success | **DONE** — Sanitization 1.0 (return false / NaoConfigurado) |
| INT-002 | NF-e emissão via provider (B) | OPEN — fora escopo até GO |
| INT-003 | FilialService persistência | OPEN (honesto: multi-filial off; sem SP/RJ fake) |
| INT-004 | API JWT + policies | OPEN |
| CLN-001 | Remover 6 services 0-byte + Run-Keycloak.ps1 | **DONE** — Sanitization 1.0 |

---

## CODEBASE SANITIZATION 1.0 (2026-09-08)

**Relatório:** `Docs/qa/PRIMOX-CODEBASE-SANITIZATION-1.0-REPORT.md`  
**Decisão:** SANITIZED WITH LIMITATIONS  
**Removidos:** 7 arquivos 0-byte (6 Services + Run-Keycloak.ps1)  
**Preservados:** NFeEmissaoService (FUTURE), QA, WIP Deploy, UNKNOWN nested, VMs LEGACY  
**Neutralização:** NotificationService; FilialService + Login (sem diálogo multi falso)  
**Regressão:** QaEngine 43/43 · DeepQa 6/6 · Exhaustive 1909/0/0 · DB integrity ok (27 migrations)

### HELP CENTER 1.0

**Relatório:** `Docs/qa/PRIMOX-HELP-AUDIT-1.0.md`  
**Correções:** tema Design System; conteúdo operacional; limites honestos; smoke HelpCenter  
**Limitações:** artigos longos pt-BR; sem vídeos/screenshots versionados; sem overlay tutorial  
**Futuro:** i18n completa da Ajuda; mídia; modo passo a passo na UI

---

## FISCAL PROVIDER DECISION & HOMOLOGATION AUDIT 1.0 (2026-09-08)

**Relatórios:**  
`Docs/qa/PRIMOX-FISCAL-PROVIDER-AUDIT-1.0.md` · `PRIMOX-FISCAL-PROVIDER-COMPARISON.md` · `PRIMOX-FISCAL-DECISION.md` · `PRIMOX-FISCAL-HOMOLOGATION-PLAN.md` · `Docs/architecture/PRIMOX-FISCAL-PROVIDER-ARCHITECTURE.md`

| Item | Resultado |
|------|-----------|
| Verdade atual | Import NF-e REAL+TESTADA; emissão NF-e/NFC-e/NFS-e NÃO IMPLEMENTADA |
| Caminho | Opção B — provedor (não SEFAZ direta) |
| Recomendado | **Focus NFe** |
| Alternativa | TecnoSpeed PlugNotas |
| Evitar agora | Nuvem Fiscal (site 500 + risco continuidade) |
| Certificado | A1 |
| Onda 1 futura | NF-e homologação apenas |
| Código/banco/NF emitida nesta etapa | **NÃO** |
| Decisão implementação | **GO CONDICIONAL** (após aceite + trial + A1 homolog) |
| Status | **FISCAL ARCHITECTURE DECISION READY** |

### Itens abertos fiscais

| ID | Tema | Status |
|----|------|--------|
| FIS-001 | Aceite humano Focus vs PlugNotas | OPEN |
| FIS-002 | Obter A1 homologação | OPEN |
| FIS-003 | Trial Focus + primeira emissão homolog (código futuro) | BLOCKED até FIS-001/002 |
| INT-002 | NF-e emissão via provider | OPEN — alinhado a Focus |
