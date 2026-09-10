# PRIMOX-I18N-USER-VISIBLE-COVERAGE — I18N-04

**Data:** 2026-09-10  
**HEAD baseline:** `6036a40`  
**Evidência runtime:** `Logs/qa-visual/i18n-04/` · smoke `TestResults/UiSmoke/2026-09-10_07-37-50` (filtro `I18n04`)

---

## Duas métricas (não misturar)

### 1) STATIC SOURCE COVERAGE

Fonte: `Scripts/Audit-I18nCoverage.ps1`

| Momento | Literais | Bound LocHelper | % |
|---------|---------:|----------------:|--:|
| Baseline I18N-04 | 2231 | 525 | ~19.0% |
| Após correções P0 I18N-04 | **2219** | **540** | **~19.6%** |

Complementar: `UiText.T` = 269 (code-behind).

### 2) USER-VISIBLE COVERAGE (runtime)

Fonte: smoke `I18n04:MultilingualUserVisibleAudit` (15 módulos críticos × 3 idiomas + screenshots).

| Idioma | Módulos | PASS | PARTIAL | FAIL | Strict PASS% | Navegáveis% |
|--------|--------:|-----:|--------:|-----:|-------------:|------------:|
| pt-BR | 15 | 15 | 0 | 0 | **100%** | 100% |
| en-US | 15 | 0 | 15 | 0 | **0%** | **100%** |
| es-ES | 15 | 0 | 15 | 0 | **0%** | **100%** |

**Definições:**

- **Strict PASS:** nenhum token PT classificado como residual de UI no visual tree do módulo.
- **PARTIAL:** módulo abre e é utilizável, mas ainda há português residual em labels/empty/Help/corpo.
- **Navegáveis:** FAIL=0 (abre sem crash).

### Critical vs secondary

| Camada | EN | ES | Notas |
|--------|----|----|-------|
| Critical navigation (Sidebar/Header/switch) | LIVE_UPDATE | LIVE_UPDATE | Persistência + fallback OK |
| Critical module pages (15) | PARTIAL | PARTIAL | Corpos ainda PT-heavy |
| Help body | PARTIAL / CONTENT | PARTIAL | HELP CORE ainda pt-BR |
| Dialogs/Views forms | MAJORITARIAMENTE PT | MAJORITARIAMENTE PT | inventário SURF-DLG-* |

**Critical flows (caixa/técnico/estoque/financeiro/admin) em EN/ES:**  
navegáveis = **PASS**; zero-residual = **FAIL/PARTIAL** — **não** declarar “English complete”.

---

## Persistência / Runtime

| Check | Resultado |
|-------|-----------|
| Persistência language_settings.json | PASS |
| Idioma inválido → pt-BR | PASS |
| Runtime PT→EN→ES | LIVE_UPDATE (bindings) |
| Elementos REQUIRES_RESTART | não observado no smoke de páginas bound; code-behind strings capturam idioma no momento da renderização |

---

## Interpretação honesta

Um usuário só-inglês/só-espanhol **consegue abrir** Dashboard, PDV, OS, Estoque, etc.  
Ele **ainda encontra português** em empty states, textos descritivos, Help, vários headers e relatórios.

Resposta à pergunta de qualidade I18N-04:

> Consegue usar funções principais? **Parcialmente.**  
> Sem português indevido / UI quebrada? **Não — ainda há português residual relevante.**

Decisão sugerida: **READY WITH LIMITATIONS / YELLOW**
