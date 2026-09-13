# PRIMOX-I18N-05-REGRESSION

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Data:** 2026-09-10  
**Baseline HEAD:** `2a0d685`  
**Tag `v1.0.0`:** `72d85fa` intacta

| Suite | Resultado | Artefato |
|-------|-----------|----------|
| Build Release | **0 errors** | local |
| Localization + Fiscal lote | **67/67 PASS** | `dotnet test` (incl. `I18n05_ContentKeys`) |
| I18n05 CoreContent smoke | **PASS** | `TestResults/UiSmoke/2026-09-10_12-44-33` · `Logs/qa-visual/i18n-05/` |
| QaEngine (+ CompleteUi) | **43/43 PASS** | `…/2026-09-10_12-59-26` (retry após fix coluna Ações) |
| DeepQa (+ LongRun) | **6/6 PASS** | `…/2026-09-10_13-04-46` |
| ExhaustiveUi | **PASS** FullSimulation | `…/2026-09-10_13-07-34` |
| DB integrity | **ok** · FK **0** | AutomatedTests isolation `…-130736-11800` |
| Security | sem secrets introduzidos | review |

### Falha transitória

QaEngine primeira rodada **42/43**: `CompleteUiLayoutActions` — header `Ações` (ç) não batia em `Contains("Aco")`.  
Correção: aceitar Ações / Actions / Acciones. Re-run **43/43**.

### Baseline QaEngine (pré-código)

Também **APROVADO** em `TestResults/UiSmoke/2026-09-10_12-30-13` (pre-flight).
