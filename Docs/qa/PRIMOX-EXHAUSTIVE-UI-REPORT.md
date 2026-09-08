# PRIMOX — Exhaustive UI Button Audit 2.0

## 1. Identificação

| Campo | Valor |
|-------|-------|
| Produto | PRIMOX Workshop **1.0.0** |
| HEAD (motor) | `9856f23` (`test(qa): add exhaustive UI button and window simulation`) |
| Branch | `main` |
| Tag protegida | `v1.0.0` → `a4ad6fe810a96914275891d294e9e585c7867e9e` (**intacta**) |
| Data reteste | 2026-09-08 |
| Filtro | `ExhaustiveButtonSimulation` / `ExhaustiveUi` |
| Build | Debug `net6.0-windows` — 0 erros |
| Smoke | `ui-smoke-2026-09-08-14-56-30-519-p10740.txt` — **APROVADO** |
| Pasta isolada | `AutomatedTests/ui-smoke-test-20260908-143717-10740` |

Complementar a CompleteUi / QaEngine / DeepQa — **não** substitui.

---

## 2. Inventário

| Item | Quantidade |
|------|----------:|
| Módulos/páginas runtime (Exhaustive) | 17 |
| Windows visitadas (runtime) | 34 |
| Views `*.xaml` no repo | 45 |
| UserControls `*.xaml` no repo | 27 |
| Botões runtime descobertos (soma 8 rounds) | **3216** |
| Linhas de resultado CSV | 2215 |
| Botões estáticos XAML (inventário Completo prévio) | ver Fase 15E |

**Exclusões por regra de auditoria:** `CalendarDayButton` (dias), `DataGridColumnHeader` / `DataGridRowHeader`, `CheckBox` / `RadioButton`, `RepeatButton` dentro de `ScrollBar`.

---

## 3. Execução (8 rounds)

| Round | Tema | Resolução | Descobertos | Testados | PASS | FAIL | BLOCKED\* |
| ----- | ---- | --------: | ----------: | -------: | ---: | ---: | --------: |
| ROUND-L-1366 | Light | 1366×768 | 391 | 184 | 184 | 0 | (agregado) |
| ROUND-D-1366 | Dark | 1366×768 | 399 | 187 | 187 | 0 | |
| ROUND-L-1600 | Light | 1600×900 | 406 | 191 | 191 | 0 | |
| ROUND-D-1600 | Dark | 1600×900 | 400 | 186 | 186 | 0 | |
| ROUND-L-1920 | Light | 1920×1080 | 408 | 191 | 191 | 0 | |
| ROUND-D-1920 | Dark | 1920×1080 | 402 | 186 | 186 | 0 | |
| ROUND-L-2560 | Light | 2560×1440 | 410 | 191 | 191 | 0 | |
| ROUND-D-2560 | Dark | 2560×1440 | 400 | 186 | 186 | 0 | |
| **Total** | | | **3216** | **1502** | **1502** | **0** | **102** |

\* BLOCKED / SKIPPED agregados no summary global (não por round no log de linha).

Duração: ~19,2 min (`ExhaustiveUi:FullSimulation` 1152023 ms).

---

## 4. Cobertura (métricas separadas)

| Métrica | Valor | Fórmula |
|---------|------:|---------|
| Discovered | 3216 | soma filas SCAN |
| CSV results | 2215 | PASS+SKIPPED+DISABLED+BLOCKED+NOT_VISIBLE |
| PASS | 1502 | |
| FAIL | 0 | |
| EXPECTED_DISABLED | 194 | |
| NOT_VISIBLE | 54 | |
| SKIPPED (ex.: file/print, loop guard) | 363 | |
| BLOCKED | 102 | |
| **Executable (tested+blocked)** | **1604** | 1502+102 |
| **Coverage tested/discovered** | **46,70%** | 1502/3216 |
| **Coverage tested/executable** | **93,64%** | 1502/1604 |
| **Pass rate PASS/tested** | **100,00%** | 1502/1502 |

**Não** se declara 100% de cobertura geral: há disabled, hidden, native dialogs e BLOCKED.

Static inventory coverage ≠ runtime-discovered executable coverage.

---

## 5. FAILs reais (produto)

**Nenhum** FAIL de botão no reteste 2.0 (classificador endurecido).

`FocusVisualStyle` / `DependencyProperty.UnsetValue`: **0** ocorrências no CSV deste run.

---

## 6. Falsos positivos eliminados

| Pass | Descobertos | Testados | PASS | FAIL | Notas |
|------|------------:|---------:|-----:|-----:|-------|
| Pass 1 (classificador ruidoso) | 5800 | 2839 | 2042 | **797** | headers DataGrid, outside-window, popups de negócio |
| Pass 2 / Audit 2.0 | 3216 | 1502 | 1502 | **0** | exclusões + expected-business ≠ FAIL |

Falsos positivos em massa (~797) tratados no motor (`IsPrimaryActionButton`, classificação de popup).

---

## 7. Bugs corrigidos nesta auditoria

| Tipo | Commit | Descrição |
|------|--------|-----------|
| Motor QA | `9856f23` | Motor ExhaustiveUi + NativeMethods `ReadDialogText` / `TryDismissDialogSafe` |
| Motor QA | (docs/commit seguinte) | Long Run DeepQa → **5 ciclos** |

**Bugs de produto novos comprovados pelo Exhaustive:** nenhum (FAIL=0).

Correções de produto da Fase 15E (FocusVisualStyle, Dark inputs, etc.) permanecem VERIFIED.

---

## 8. Bugs conhecidos / limitações

| ID | Classificação | Descrição |
|----|---------------|-----------|
| P15E-012 | PARTIAL | Estilo ModalAccent OK; recursão completa de *todos* os modais ainda amostral no CompleteUi |
| P15E-015 | OPEN | ~16 botões com `ACCESSIBILITY ISSUE / UNIDENTIFIED_BUTTON` (ícone sem ToolTip/AutomationName) — finding, não FAIL de crash |
| NATIVE_DIALOG | NOT_TESTABLE | File picker / print (~64 SKIPPED) |
| DEPTH | ENVIRONMENT_LIMITATION | `ExhaustiveMaxWindowDepth = 5` + assinatura max 2 (anti-loop) |
| STATE | DISABLED_BY_DESIGN | 194 EXPECTED_DISABLED sem transição de estado forçada em todos os casos |
| BLOCKED | BLOCKED | 102 (host disposed / referência inválida mid-queue) |
| Fixture OS→venda | EXPECTED_VALIDATION / data | Popup “Produto do orcamento nao encontrado” em smoke — mensagem tratada; fixture pode deixar item órfão |

---

## 9. Janelas secundárias

34 windows runtime (ex.: `NovoClienteWindow`, `ImportarCatalogoPecasWindow`, `OrdemServicoWindow`, `OperacaoCaixaWindow`, `SelecaoFilialWindow`, …).  
Fluxo: SCAN → FREEZE → EXEC → child SCAN → close → resume pai.

---

## 10. Pop-ups

352 dismissals capturados (`exhaustive-popups-latest.txt`).

Classificação observada: `info`, `confirm`, `expected-business`, `modal-explore-then-close`, `selecao-*`, `native-file-dialog-close`.

Popups de validação (ex.: “Selecione um cliente”, “Exclusao bloqueada”) **não** elevam FAIL.

---

## 11. Navegação

Árvore: `TestResults/UiSmoke/ExhaustiveUi/exhaustive-tree-latest.md`  
CSV: `exhaustive-buttons-latest.csv`

---

## 12. Light / Dark

4 rounds Light + 4 rounds Dark — todos PASS nos botões executados.

---

## 13. Resoluções

1366×768, 1600×900, 1920×1080, 2560×1440 — todos executados.  
Findings `outside-window` (~148) registrados como detalhe, **não** FAIL automático.

---

## 14. QA existente (pós-Exhaustive)

| Suite | Resultado | Evidência |
|-------|-----------|-----------|
| ExhaustiveUi | **PASS** 1/1 | `…14-56-30-519-p10740.txt` |
| QaEngine (+ CompleteUi via filtro) | **PASS** 42/42 | `…15-02-35-701-p16156.txt` |
| DeepQa | **PASS** 6/6 | `…15-05-24-112-p8936.txt` |
| Long Run | **PASS** 5 ciclos, 90 navegações, 75,2 s | log Smoke DeepQa |

CompleteUi **preservado** (não removido).

---

## 15. Dados

- Isolamento: pasta `AutomatedTests/ui-smoke-test-*` (não produção).
- Prefixos smoke / QA; limpeza via DB isolado do smoke.
- Base de produção / install comercial: **não** usada neste reteste.

---

## 16. Conclusão

**PASS WITH KNOWN LIMITATIONS**

- Motor confiável após endurecimento do classificador.
- 8/8 rounds Light/Dark × resoluções.
- FAIL produto = 0 neste reteste; cobertura executável ≠ 100% geral.
- Limitações: native dialogs, disabled, depth/loop guards, a11y icon-only, BLOCKED mid-queue.

**Decisão:** não declarar 100% funcional / produção-perfeita; produto permanece **PRIMOX Workshop 1.0.0** (sem Fase 16).
