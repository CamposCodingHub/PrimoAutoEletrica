# PRIMOX — Accessibility Closure Report (P15E-015) — ETAPA 1

**Data:** 2026-09-08  
**HEAD (fecho):** ver commit `fix(a11y)`  
**Tag:** `v1.0.0` → `a4ad6fe` (**intacta**)  
**Decisão:** **VERIFIED**

---

## Resumo

| | Antes | Depois |
|--|-------|--------|
| P15E-015 | PARTIAL (~30 ACCESSIBILITY rows Exhaustive 3.0) | **VERIFIED** (0 ACCESSIBILITY rows) |
| Causa raiz | DataGrid SelectAll (`Command` sem Name/ToolTip) + DatePicker `PART_Button` icon-only | Healer + XAML identity |
| Exhaustive | 1909 PASS / 0 FAIL / BLOCKED 0 | 1909 PASS / 0 FAIL / BLOCKED 0 / **ACCESSIBILITY=0** |

---

## Inventário (Exhaustive pós-correção)

| Métrica | Valor |
|---------|------:|
| Módulos/páginas | 17 |
| Windows | 35 |
| Botões discovered | 3250 |
| Executable (tested+blocked) | 1909 |
| Testados | 1909 |
| PASS | 1909 |
| FAIL | 0 |
| BLOCKED | 0 |
| ACCESSIBILITY ISSUE rows | **0** |
| Rounds | 8 (Light+Dark × 1366/1600/1920/2560) |

---

## Cobertura P15E-015

```text
Descobertos (runtime Exhaustive): 3250
Auditados (fila + chrome heal): todos os ButtonBase classificados como ação
Testáveis / Testados: 1909 / 1909
PASS: 1909
FAIL: 0
BLOCKED: 0
NOT_TESTABLE: native file/print + guards (inalterados)
EXPECTED_DISABLED / NOT_VISIBLE: presentes, esperados
ACCESSIBILITY UNIDENTIFIED: 0
```

Critério cumprido: elementos acionáveis no escopo P15E-015 auditados; testáveis PASS; unidentified icon/chrome = 0.

---

## Problemas encontrados e correções

| ID | Local | Problema | Severidade | Correção | Status |
| -- | ----- | -------- | ---------- | -------- | ------ |
| A11Y-001 | DataGrid SelectAll (AutoEletrica ×2, OS ItensDataGrid, demais grids) | Button com `SelectAllCommand`, sem Name/ToolTip → UNIDENTIFIED | HIGH | `AccessibilityChromeHealer` define Name/ToolTip pt/en/es | FIXED |
| A11Y-002 | `PremiumDatePicker` PART_Button | Ícone sem identidade | HIGH | ToolTip + AutomationProperties.Name no template + healer | FIXED |
| A11Y-003 | OrdemServico ModalClose | Content "X" whitespace | MEDIUM | `Content="X"` + estilo ModalClose já com Fechar | FIXED |
| A11Y-004 | CommandCenter | ToolTip sem AutomationProperties.Name | LOW | Name="Command Center" | FIXED |

---

## Testes

| Teste | Resultado | Evidência |
| ----- | --------- | --------- |
| Build | PASS 0 errors | Debug net6.0-windows |
| QaEngine | PASS 42/42 | `…17-49-02…` |
| CompleteUi | PASS | `…17-50-00…` (FocusVisualStyle) |
| Exhaustive UI | PASS FAIL=0 BLOCKED=0 ACCESSIBILITY=0 | `ui-smoke-2026-09-08-18-11-31-435-p23600` / folder `…175007-23600` |
| Deep QA / Long Run | PASS 6/6 · 5 ciclos / 90 nav / 73,7s | `ui-smoke-2026-09-08-18-14-21-667-p14500` |
| Light | PASS | 4 rounds Light |
| Dark | PASS | 4 rounds Dark |
| 1366 / 1600 / 1920 / 2560 | PASS | Exhaustive 8/8 |

---

## Limitações honestas

- Calendar **day** buttons continuam excluídos por regra de auditoria (não são ações P15E-015).
- Native Print/File dialogs permanecem NOT_TESTABLE.
- HelpControl WIP não foi alterado nesta etapa.
- Labels chrome SelectAll/Abrir calendário usam pt/en/es via `CurrentUICulture` no healer (não novo sistema de i18n).

---

## Arquivos

**Criados:** `Helpers/AccessibilityChromeHealer.cs`, este relatório  
**Alterados:** `Themes/Inputs.xaml`, `Themes/Modal.xaml` (prévio), `OrdemServicoWindow.xaml`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `LoginWindow.xaml.cs`, `App.xaml.cs`, `UiSmokeTestService.ExhaustiveUi.cs`, `UiSmokeTestService.PrimoxQa.CompleteUi.cs`, docs status/melhorias

---

## Decisão final

# VERIFIED

P15E-015 fechado com evidência Exhaustive **0** `ACCESSIBILITY ISSUE`.  
**Não** iniciar ETAPA 2.
