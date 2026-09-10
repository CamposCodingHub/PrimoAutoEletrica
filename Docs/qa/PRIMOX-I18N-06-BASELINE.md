# PRIMOX-I18N-06-BASELINE — Pre-flight

**Missão:** PRIMOX-I18N-06 — Multilingual Closure & Final UX  
**Data/hora:** 2026-09-10 18:16 (America/Sao_Paulo)  
**Escopo:** registro de baseline — implementação ainda não iniciada neste documento

---

## 1. Identificação Git

| Item | Valor |
|------|-------|
| Branch | `main` |
| HEAD | `42cce65` (`docs(i18n): record core content localization audit`) |
| HEAD esperado I18N-05 | `42cce65` — **CONFIRMADO** |
| Tag `v1.0.0` | `72d85fa` — **INTACTA** |
| Remote | `main...origin/main` **ahead 18** |

### Worktree

| Estado | Detalhe |
|--------|---------|
| WIP preservado | `Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` (M) — **NÃO tocar** |
| Deploy scripts | preservar |

---

## 2. Cadeia I18N

| Fase | Conteúdo | HEAD |
|------|----------|------|
| I18N-01…04 | Core → UX audit | … → `2a0d685` |
| I18N-05 | Core content | `42cce65` |
| I18N-06 | Closure | **INÍCIO** |

---

## 3. Build / unit baseline

| Item | Resultado |
|------|-----------|
| Build Release | **0 errors** (após liberar lock `PrimoAutoEletrica.exe` / PRIMOX Workshop PID 5452) |
| Loc+Fiscal+Nfe+Focus | **67/67 PASS** |
| QaEngine / DeepQa / Exhaustive | em execução baseline — ver `PRIMOX-I18N-06-REGRESSION.md` |

---

## 4. Métricas baseline (independentes)

### A) STATIC SOURCE COVERAGE

| Métrica | Valor |
|---------|------:|
| Literais | **1844** |
| Bound | **915** |
| Coverage | **~33.2%** |
| UiText.T | 269 |
| MessageBox.Show | 237 |

### B) TRANSLATABLE CONTENT

| Classe | Count |
|--------|------:|
| TRANSLATION_REQUIRED | **357** |
| BRAND | 5 |
| FISCAL_TERM | 18 |
| UNKNOWN | **1348** |

### C/D) USER-VISIBLE (última evidência I18N-05)

| Idioma | Strict PASS% | Navegáveis |
|--------|-------------:|----------:|
| pt-BR | 100% | 100% |
| en-US | 13.3% | 100% |
| es-ES | 6.7% | 100% |

### Catálogo

| Item | Valor |
|------|------:|
| Keys ×3 | 724 |
| USED / UNUSED | 430 / 294 |
| MISSING_EN/ES | 0 / 0 |
| DUPLICATE_CANDIDATE | 37 |

---

## 5. Arquitetura

`LocalizationService` + Modules + Interaction + Content + `LocalizationHelper` + `UiText`  
`CurrentCulture` = **pt-BR** · **NÃO** criar arquitetura paralela.

---

## 6. GO

| Check | Status |
|-------|--------|
| HEAD / tag | GO |
| WIP isolado | GO |
| Build / Loc+Fiscal | GO |

**Decisão pre-flight:** **GO** para AUDIT → CLASSIFICATION → P0/P1 → CODE-BEHIND → HELP CORE.
