# PRIMOX-I18N-05-BASELINE — Pre-flight

**Missão:** PRIMOX-I18N-05 — Core Content Localization  
**Data/hora:** 2026-09-10 12:29 (America/Sao_Paulo)  
**Escopo desta fase:** registro de baseline — código ainda não alterado neste documento

---

## 1. Identificação Git

| Item | Valor |
|------|-------|
| Branch | `main` |
| HEAD | `2a0d685` (`docs(i18n): record I18N-04 final HEAD in manual UX audit`) |
| HEAD esperado I18N-04 | `2a0d685` — **CONFIRMADO** |
| Tag `v1.0.0` | `72d85fa` — **INTACTA** |
| Remote | `main...origin/main` **ahead 15** |

### Worktree

| Estado | Detalhe |
|--------|---------|
| WIP preservado | `Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` (M) — **NÃO tocar** |
| Scripts deploy | `Scripts/Atualizar-PrimoAuto.bat` · `Scripts/Deploy-ToInstalledApp.ps1` — **preservar** |

---

## 2. Cadeia I18N

| Fase | Conteúdo | HEAD |
|------|----------|------|
| I18N-01 | Localization Core | (audit) |
| I18N-02 | Module chrome | |
| I18N-03 | Interaction / dialogs | `de6b72e` |
| I18N-04 | Manual UX + user-visible | `2a0d685` |
| I18N-05 | Core content | **INÍCIO** (este baseline) |

---

## 3. Build / unit baseline

| Item | Resultado |
|------|-----------|
| Build Release | **0 errors** (73 warnings pré-existentes) |
| Localization + Fiscal lote | **66/66 PASS** |
| QaEngine | em execução / ver `PRIMOX-I18N-05-REGRESSION.md` |

---

## 4. Static source coverage (NÃO = user-visible)

Fonte: `Scripts/Audit-I18nCoverage.ps1`

| Métrica | Valor |
|---------|------:|
| XAML files | 104 |
| Literal UI attrs | **2219** |
| Bound LocalizationHelper | **540** |
| Coverage | **~19.6%** |
| UiText.T | 269 |

---

## 5. Residuals / catalog baseline

| Métrica | Valor |
|---------|------:|
| TRANSLATION_REQUIRED (scanner) | **507** |
| BRAND | 5 |
| FISCAL_TERM | 19 |
| UNKNOWN | 1572 |
| Catalog keys PT/EN/ES | **576** cada |
| USED / UNUSED | 260 / 316 |
| MISSING_EN / MISSING_ES | **0 / 0** |

Artefatos: `TestResults/I18n/i18n-*-20260910-1229*.json`

---

## 6. Arquitetura (confirmada)

`LocalizationService` + `LocalizationHelper` + catálogos Pt/En/Es + Modules + Interaction  
`CurrentCulture` negócio = **pt-BR** · UICulture muda com idioma  
**NÃO** criar segundo serviço/catálogo.

---

## 7. GO / NO-GO

| Check | Status |
|-------|--------|
| HEAD compatível | GO |
| Tag intacta | GO |
| Build | GO |
| Loc+Fiscal | GO |
| WIP isolado | GO |

**Decisão pre-flight:** **GO** para AUDIT → CLASSIFICATION → TRANSLATION.
