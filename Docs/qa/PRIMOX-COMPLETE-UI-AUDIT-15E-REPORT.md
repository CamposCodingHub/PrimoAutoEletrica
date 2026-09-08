# PRIMOX COMPLETE UI AUDIT — Fase 15E

**Produto:** PRIMOX Workshop  
**Versão base:** 1.0.0  
**Tag:** `v1.0.0` → `a4ad6fe` (**intacta**)  
**Branch:** `main`  
**Data:** 08/09/2026  

**Decisão:** **GO WITH KNOWN LIMITATIONS**

---

## 1. Estado inicial

- Packaging 15D GO; uso manual posterior revelou gaps de QA visual/interativo.
- Exceção crítica: `FocusVisualStyle` / `DependencyProperty.UnsetValue` (códigos 20260908111133470…).
- Dark: TextBoxes sem estilo implícito → chrome WPF branco.
- Login X com contraste fraco; PDV produtos oversized; Funcionários Ações estreitas.

WIP preservado: HelpControl, Deploy scripts. Tag não movida.

---

## 2. Inventário (runtime / estático)

| Métrica | Valor (aprox.) |
|---------|----------------|
| Módulos canônicos | ≥ 16 (DeepQa / QaEngine) |
| Windows | ≥ 40 |
| UserControls | ≥ 20 |
| Botões runtime (discovery) | ≥ 50 |
| Idiomas | **pt-BR**, **en-US**, **es-ES** |

Checklist vivo: `Docs/qa/PRIMOX-MELHORIAS.md`

---

## 3. Motor 15E

Arquivo: `Services/UiSmokeTestService.PrimoxQa.CompleteUi.cs`  
Filtro: `CompleteUi` / `Fase15E` (também executado com `QaEngine`)

| Check | Escopo |
|-------|--------|
| CompleteUiFocusVisualStyle | Login + todos módulos; Tab/focus; heal |
| CompleteUiDarkInputSurfaces | Dark TextBox/DatePicker near-white detection + PDV size |
| CompleteUiButtonByButton | até 10 botões/módulo (foco + clique seguro); continua após falha |
| CompleteUiPlaceholdersI18n | SearchPlaceholder pt/en/es |
| CompleteUiLayoutActions | Funcionários Ações width; Login CloseButton |

---

## 4. Correções (causa raiz)

### P15E-001 / 002 — FocusVisualStyle
1. Override `SystemParameters.FocusVisualStyleKey` com Style **autocontido** (sem BasedOn frágil).  
2. Setters de foco → `DynamicResource {x:Static SystemParameters.FocusVisualStyleKey}`.  
3. `FocusVisualStyleHealer` em Login/MainWindow Loaded + interceptação no Dispatcher (sem dialog assustador).  
4. Template de foco sem `CornerRadius` DynamicResource.

### Dark inputs (005–007, 011, 013)
- `Style TargetType=TextBox` / `PasswordBox` implícitos.  
- `DatePickerTextBox` template transparente.  
- SearchBox com placeholder via `Tag`.

### Layout / UX
- Login Close: Path + contraste.  
- PDV produtos: 960×640 (min 720×480).  
- Funcionários Ações: Width 250.  
- Veículos: ToolTips.  
- `SearchPlaceholder` i18n.  
- ModalAccentButton BasedOn Button.

---

## 5. Evidências de teste

| Suíte | Resultado | Evidência |
|-------|-----------|-----------|
| CompleteUi | **5/5 PASS** | `TestResults/UiSmoke/2026-09-08_12-12-12` |
| FocusVisualStyle | **PASS** | `2026-09-08_12-11-29` |
| QaEngine completo | **42/42 PASS** (37 + 5 CompleteUi) | `2026-09-08_12-13-36` |
| Deep QA | **6/6 PASS** | `2026-09-08_12-19-11` |
| Long Run 5 | **PASS** | `2026-09-08_12-21-00` |

---

## 6. Cobertura (honesta)

| Dimensão | Estimativa | Notas |
|----------|------------|-------|
| Funcional (QaEngine legado + CompleteUi) | alta nos módulos canônicos | não 100% de todos handlers |
| Visual Dark inputs | alta nos módulos alvo 15E | janelas secundárias profundas: amostral |
| Light | parcial nesta fase | herdado DeepQa / Tema |
| Foco | alta (CompleteUi + healer) | |
| Layout | parcial (Funcionários, PDV, Login) | 4 resoluções: herdado ResponsividadeResolucoes |
| Tradução | placeholders Search 3 idiomas PASS | strings hardcoded restantes: backlog |
| Janelas secundárias | amostral (1 clique seguro/módulo + close) | não clique exaustivo em todos “Novo/Editar” |

**Não declarar 100%.**

---

## 7. Limitações conhecidas

- Clique exaustivo em todos os botões “Novo/Editar/Import” omitido de propósito (diálogos nativos / risco).  
- Strings hardcoded em várias telas (não inventar traduções).  
- AssemblyCompany legado / signing: fora de escopo.  
- Screenshots automáticos por botão: evidência textual priorizada; DeepQa visual permanece.

---

## 8. Decisão

**GO WITH KNOWN LIMITATIONS** — críticos P15E-001…014 tratados; CompleteUi verde; cobertura ampliada sem falso 100%.

**PARAR** — não iniciar Fase 16 automaticamente.
