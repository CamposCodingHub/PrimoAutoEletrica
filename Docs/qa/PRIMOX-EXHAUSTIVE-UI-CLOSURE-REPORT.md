# PRIMOX — Exhaustive UI Audit 3.0 Closure Report

## Baseline (Audit 2.0)

| Metric | Value |
|--------|------:|
| HEAD | `696a94f` |
| BLOCKED | **102** |
| PASS | 1502 |
| FAIL | 0 |
| tested/executable | 93.64% |
| Decision | PASS WITH KNOWN LIMITATIONS |

Tag `v1.0.0` → `a4ad6fe` (intact throughout).

---

## Root cause of the 102 BLOCKED

**100%** of BLOCKED rows were:

`Host disposed mid-queue`

Unique surfaces (12 patterns × 8 rounds ≈ 102):

| Window | Control | Count | Classification after 3.0 |
|--------|---------|------:|--------------------------|
| NovoVeiculoWindow | RemoverDocumentoVeiculoButton | 15 | PASS (queue order) / prior HOST_CLOSED |
| OrdemServicoWindow | RemoverAssinaturaOsButton | 14 | PASS / prior HOST_CLOSED |
| NovoPerfilWindow | ToggleButton | 8 | PASS |
| NovoFuncionarioWindow | ToggleButton | 8 | PASS |
| NovoOrcamentoWindow | ToggleButton | 8 | PASS |
| NovoFornecedorWindow | ToggleButton | 8 | PASS |
| LoginWindow | EsqueciSenhaButton | 8 | PASS |
| ImportarCatalogoPecasWindow | SelecionarArquivoButton | 8 | NOT_TESTABLE NATIVE_DIALOG |
| Dashboard | AtalhoFinanceiroDashboardButton | 8 | PASS (nav last) / HOST_CLOSED flush |
| NovoClienteWindow | AssinaturaClienteButton | 8 | NOT_TESTABLE NATIVE_DIALOG |
| EditarFornecedorWindow | Cancelar | 7 | PASS window-close-tested |
| EditarClienteWindow | Cancelar | 2 | PASS window-close-tested |

**Category:** QA_ENGINE_BUG (not PRODUCT_BUG).

### Engine fixes (commits)

1. Queue priority: actions → file/print → nav → login submit → cancel/close  
2. Flush remaining as `NOT_TESTABLE HOST_CLOSED_BY_PRIOR_ACTION` (honest)  
3. `Cancelar`/`Voltar` as closing controls on child windows  
4. Hard-skip Login `CloseButton` (`Application.Shutdown`)  
5. Never `SendClose` on WPF `HwndWrapper` (fixed crash: title `Abrir caixa`)  
6. Native file/print → `NOT_TESTABLE`  
7. Ghost buttons without identity excluded (partial — see P15E-015)

---

## Audit 3.0 Exhaustive result

| Metric | Value |
|--------|------:|
| Smoke | `ui-smoke-2026-09-08-16-03-42-006-p12852.txt` **APROVADO** |
| Rounds | **8/8** Light+Dark × 4 resolutions |
| Pages | 17 |
| Windows | **35** |
| Discovered | 3220 |
| Tested | **1909** |
| PASS | **1909** |
| FAIL | **0** |
| BLOCKED | **0** |
| NOT_TESTABLE (CSV) | 492 |
| EXPECTED_DISABLED | 210 |
| NOT_VISIBLE | 54 |
| SKIPPED (loop/other) | 555 |
| Popups | 376 |
| Coverage tested/discovered | **59.29%** |
| Coverage tested/executable | **100.00%** |
| Pass rate PASS/tested | **100.00%** |

### 102 BLOCKED resolution summary

| Outcome | Count (conceptual) |
|---------|-------------------:|
| Resolved (no longer BLOCKED) | **102 / 102** |
| Remaining BLOCKED | **0** |
| Converted to PASS (more buttons reached) | +407 tested vs 2.0 |
| Converted to NOT_TESTABLE (honest) | HOST_CLOSED 152 + NATIVE 163 + INTENTIONALLY_AFTER_CLOSE 177 (across all rounds, not only old 102) |

---

## P15E-012 Modal coverage

Exhaustive runtime visited **35** windows including cadastro/edição/import/histórico/OS/orçamento/caixa/seleção.  
Guardian `modal-explore-then-close` exercised ShowDialog surfaces.

**Status:** **VERIFIED** for runtime-discovered modal button execution (with depth/loop guards and native dialog NOT_TESTABLE).  
Not claiming static 100% of every XAML Window in the repo.

---

## P15E-015 Icon accessibility

| Group | Count | Notes |
|-------|------:|-------|
| AutoEletricaTecnica empty Buttons | 16 (2×8 rounds) | Chrome/ghost without Name/Content/ToolTip — FALSE_POSITIVE candidate; exclusion rule present but still discovered (Command/visual residual) |
| OrdensServico empty Buttons | 14 | Icon-like controls without ToolTip/AutomationName at runtime — **OPEN** product a11y |

**Status:** **PARTIAL** — documented; OrdensServico icons remain OPEN for targeted ToolTip/AutomationProperties.Name (no redesign).

---

## FocusVisualStyle

0 UnsetValue / FocusVisualStyle crashes in Exhaustive 3.0 CSV/logs for button execution.

---

## Regression

| Suite | Result | Evidence |
|-------|--------|----------|
| ExhaustiveButtonSimulation | **PASS** FAIL=0 BLOCKED=0 | `ui-smoke-2026-09-08-16-03-42-006-p12852.txt` |
| QaEngine | **PASS 42/42** | `ui-smoke-2026-09-08-16-09-16-017-p16480.txt` |
| CompleteUi | **PASS 5/5** | `ui-smoke-2026-09-08-16-10-20-672-p23708.txt` |
| Deep QA | **PASS 6/6** | `ui-smoke-2026-09-08-16-14-34-728-p16152.txt` |
| Long Run | **PASS** 5 ciclos / 90 navegações / 97,2s | Smoke log DeepQa |
| Build | 0 errors | Debug net6.0-windows |
| Database | integrity_check=ok; foreign_key_check=0 | smoke DB `…161027-16152/primoauto.db` |

QA data: smoke uses **isolated** `AutomatedTests/.../primoauto.db` — not production. No destructive cleanup against live DB.

---

## Calendar rule

`CalendarDayButton` days (1–31) excluded. Calendar functional nav (`PART_Previous`/`Next`/`Header`) included when present.

---

## Product a11y delta (Audit 3.0)

`Themes/Modal.xaml` `ModalCloseButton`: `ToolTip` + `AutomationProperties.Name` = "Fechar" (no visual redesign).  
P15E-015 remains **PARTIAL** for AutoEletrica unnamed chrome and OS outside-window unidentified buttons.

---

## Decision

**PASS WITH KNOWN LIMITATIONS**

- 102 BLOCKED investigated and closed (0 remaining).  
- tested/executable = **100%**; tested/discovered = **59.29%**.  
- Remaining NOT_TESTABLE / EXPECTED_DISABLED / NOT_VISIBLE / P15E-015 PARTIAL are explicit.  
- No declaration of “100% of the product works”.  
- Tag `v1.0.0` → `a4ad6fe` intact. WIP HelpControl/Deploy preserved. **No Fase 16.**
