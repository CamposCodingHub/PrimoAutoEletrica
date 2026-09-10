# PRIMOX-I18N-07 — Baseline

**Data:** 2026-09-10  
**Fase:** FINAL MULTILINGUAL GATE  
**HEAD inicial:** `6927756` (`docs(i18n): record multilingual closure audit`)  
**Branch:** `main` (ahead 21)  
**Tag protegida:** `v1.0.0` = `72d85fa` ✅  
**Worktree:** limpo para I18N-07; WIP fiscal preservado (`Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md`)

## Pre-flight

| Item | Valor |
|------|-------|
| HEAD esperado I18N-06 | `6927756` |
| HEAD real | `6927756` (match) |
| Tag v1.0.0 | `72d85fa` |
| WIP fiscal | preservado (não incluído) |

## QA baseline (pré-alteração)

| Suite | Resultado |
|-------|-----------|
| Build Release | **0 errors** (warnings only) |
| Localization + Fiscal filter | **68/68 PASS** |
| Static coverage | literals **1824** · bindings **935** · **33,9%** |
| UiText.T | **303** |
| MessageBox.Show | **237** |
| Residuals TR | **337** |
| UNKNOWN | **1348** |
| Catalog | Used **476** · Unused **319** · Missing EN/ES **0** · Dup groups **45** |

## User-visible strict (I18N-06 evidence, pré-I18N-07)

| Idioma | Strict PASS |
|--------|-------------|
| PT | **100%** |
| EN | **26,7%** (PASS: Dashboard, OS, PDV, Financeiro) |
| ES | **13,3%** (PASS: Dashboard, Financeiro) |

## Residuais críticos conhecidos (alvo desta fase)

- Orçamentos: painel, `Rascunho`, MessageBox hardcoded
- Estoque: empty states + `Editar produto` / tooltips
- Clientes: `Veiculo principal`
- Veículos: contagem / histórico
- Agenda: subtítulo / `Veículo`
- Fornecedores: labels P1
- Help Extended: PT-BR (P3 / exception)
- Relatórios: event labels TECHNICAL
- Detector ES: cognatos (`Editar`/`Cancelar`/`Buscar`) — classificar honestamente

## Escopo

Fechar experiência multilíngue USER-VISIBLE (P0=0, P1 critical flows=0).  
Não usar static % como critério exclusivo. Não criar I18N-08.
