# PRIMOX-I18N-MODULE-COVERAGE — PRIMOX-I18N-02-2026-09

**Data:** 10/09/2026  
**HEAD inicial:** `6742a58`  
**Tag `v1.0.0`:** preservada (`72d85fa`)

---

## 1. Baseline (fase anterior)

| Métrica | Valor |
|---------|------:|
| Literal UI attrs (Text/Content/Header/ToolTip) | **~2641** |
| Bindings LocalizationHelper | **~55–87 refs** |
| Decisão anterior | Localization Core COMPLETE / READY WITH LIMITATIONS |

Ferramenta: `Scripts/Audit-I18nCoverage.ps1`

---

## 2. Arquitetura

Reuso do core I18N-01:

- `LocalizationService` (+ partial `LocalizationService.Modules.cs`)
- `LocalizationHelper` com **indexer** `Path=[Key]` (runtime refresh)
- `LocalizeExtension` reforçada (Binding → indexer)
- Persistência / fallback / UICulture inalterados
- **CurrentCulture de negócio = pt-BR** (mantido)

Novos:

- Catálogo de módulos (~300 chaves × pt/en/es)
- `WorkOrderStatusLocalizer` + converter (apresentação; IDs internos intactos)
- Auditoria `Scripts/Audit-I18nCoverage.ps1`

---

## 3. Módulos migrados (honestidade)

| Módulo | Status | Notas |
|--------|--------|-------|
| Shell | PASS | fase 01 |
| Dashboard | PASS (chrome + métricas VM) | títulos/estados/ações + Workshop Pulse via `GetString` |
| Clientes | PARTIAL→FORTE | título, ações, KPIs, botões comuns |
| Veículos | PARTIAL→FORTE | títulos / commons |
| OS | PARTIAL→FORTE | chrome + filtro status (Tag interno) + display localizado |
| Orçamentos | PARTIAL | commons / títulos |
| PDV | PARTIAL | commons / títulos / botões |
| Estoque | PARTIAL | commons / títulos |
| Financeiro | PARTIAL | commons / títulos / contas |
| Fornecedores | PARTIAL | commons / títulos |
| Funcionários | PARTIAL | commons / títulos |
| Agenda | PARTIAL | commons / títulos |
| Relatórios | PARTIAL | commons / títulos / filtros |
| Catálogo | PARTIAL | commons / títulos |
| Importar NF-e | PARTIAL | UI only |
| Configurações | PARTIAL | commons / abas chave |
| Ajuda | CONTENT PARTIAL | nav módulos + HelpTitle/Included; **corpo técnico permanece pt-BR** |

“FORTE” = chrome principal (título, subtítulo, action bar, botões comuns, vários headers) localizado.  
Não significa 100% de MessageBox/validações/diálogos secundários.

---

## 4. Métricas depois (auditoria real)

Gerado em `TestResults/I18n/i18n-coverage-20260910-013140.json`:

| Métrica | Depois |
|---------|-------:|
| XAML files | 104 |
| Literal UI attrs | **2231** |
| Bound LocalizationHelper attrs | **525** |
| LocalizationHelper refs | **559** |
| Cobertura bound/(literal+bound) | **19%** |

**Antes → Depois (attrs UI):**

- Literais: 2641 → 2231 (Δ −410)
- Bound: ~55 → 525 (Δ +470)
- Cobertura attrs: ~2% → **19%**

---

## 5. Strings restantes (classificação aproximada)

Dos ~2231 literais restantes (não inventar precisão absoluta):

| Classe | Estimativa | Exemplos |
|--------|------------|----------|
| A — UI ainda localizável | **maioria** (~1200–1600) | diálogos Novo/Editar, MessageBox, labels longos, empty states secundários |
| B — dados / binding dinâmico | presente | nomes, valores de VM |
| C/H — técnico / símbolos | ~300+ | `0,00%`, `12V`, `|`, atalhos mistos |
| D — fiscal / acrônimos | ~40 | CNPJ, NCM, NF-e (muitos mantidos de propósito) |
| E/F/G | fora do XAML UI | logs, testes, docs |

MessageBox/Toast em code-behind: ainda majoritariamente pt-BR (pendência I18N-03).

---

## 6. Testes

| Suite | Resultado |
|-------|-----------|
| Build Release | **0 errors** |
| Localization unit | **20/20 PASS** |
| Fiscal units (filtro) | **34 PASS** no lote |
| QaEngine / DeepQa / Exhaustive | **PASS** — QaEngine 43/43 (`2026-09-10_01-31-39`) · DeepQa 6/6 (`2026-09-10_01-37-10`) · ExhaustiveUi PASS (`2026-09-10_01-39-46`) |

---

## 7. Visual / cultura

- UICulture muda; números/moeda **pt-BR**
- Sidebar Gold Hover / Design System: não alterados
- Calendar Dark: limitação pré-existente

---

## 8. Limitações

1. Cobertura XAML ~**19%** dos attrs UI — **não** 100%.
2. Diálogos Novo/Editar e MessageBoxes: parcialmente não migrados.
3. Help **conteúdo** = CONTENT PARTIAL (pt-BR).
4. Converter de status OS: refresh visual completo ao trocar idioma pode exigir rebind/navegação em alguns itens.
5. Subtítulos dinâmicos do Dashboard (string interpolada) ainda pt-BR.

---

## 9. Decisão

**YELLOW — Module Coverage IMPROVED / READY WITH LIMITATIONS**

Justificativa: módulos críticos passaram a usar o catálogo oficial (títulos, ações, commons, OS status display, Dashboard metrics). Cobertura objetiva subiu de ~2% para **19%** dos attrs UI; restante documentado.
