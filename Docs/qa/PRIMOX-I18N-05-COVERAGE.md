# PRIMOX-I18N-05-COVERAGE

**Data:** 2026-09-10  
**Baseline HEAD:** `2a0d685`  
**Evidência runtime:** `Logs/qa-visual/i18n-05/` · smoke `I18n05`

---

## 1) STATIC SOURCE COVERAGE

| Momento | Literais | Bound | % |
|---------|---------:|------:|--:|
| Antes (I18N-04 fim) | 2219 | 540 | ~19.6% |
| Depois (I18N-05) | **1844** | **915** | **~33.2%** |

UiText.T (code-behind): 269 (inalterado nesta fase).

## 2) CONTENT / RESIDUALS

| Métrica | Antes | Depois |
|---------|------:|-------:|
| TRANSLATION_REQUIRED | 507 | **357** |
| Bindings aplicados (ocorrências) | — | **~382** |
| Arquivos XAML tocados | — | **52** |

## 3) USER-VISIBLE (runtime smoke I18n05)

| Idioma | PASS | PARTIAL | FAIL | Strict PASS% | Navegáveis% |
|--------|-----:|--------:|-----:|-------------:|------------:|
| pt-BR | 15 | 0 | 0 | **100%** | 100% |
| en-US | 2 | 13 | 0 | **13.3%** | **100%** |
| es-ES | 1 | 14 | 0 | **6.7%** | **100%** |

Notável: **OrdensServico** e **Financeiro** (EN) / **Financeiro** (ES) atingiram strict PASS.

## 4) CATÁLOGO

| Item | Valor |
|------|------:|
| Keys PT/EN/ES | **724** |
| USED | 430 |
| UNUSED | 294 |
| MISSING_EN / MISSING_ES | **0 / 0** |
| ORPHAN_USED | **0** |

## 5) Interpretação

Melhoria real de conteúdo operacional P0/P1, sem declarar English/Spanish complete.  
Help Extended e muitos empty states / code-behind strings permanecem PARTIAL.
