# PRIMOX-COMMERCIAL-09.5 — Overnight Full Product QA

**Fase:** PRIMOX-COMMERCIAL-09.5-2026-09  
**Data:** 2026-09-10 → 2026-09-11  
**Decisão:** **YELLOW — COMMERCIAL OVERNIGHT QA CLOSED WITH NON-BLOCKING LIMITATIONS**

```text
Detectar → reproduzir → classificar → corrigir só bug real → retest → documentar
NÃO mascarar FAIL · NÃO iniciar Commercial-10 / Fiscal Live / I18N-08
```

## 1. Baseline Git

| Item | Valor |
|------|-------|
| HEAD inicial | `1966d2b` (`docs(release): record code signing readiness audit`) |
| Branch | `main` |
| Tag `v1.0.0` | `72d85fa` **intacta** (não aponta para HEAD desta fase) |
| Worktree | WIP fiscal preservado: `Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` |
| Escopo | QA/hardening — **sem** features, redesign, migration, fiscal, I18N arch, installer arch, signing |

## 2. Orquestração

| Artefato | Papel |
|----------|-------|
| `Scripts/Run-CommercialOvernightQa.ps1` | Build + suites + duração mínima + summary |
| `UiSmokeTestService.OvernightQa.cs` | Nav / language / dialog stress |
| Evidência | `TestResults/Commercial095/20260910-220835/` |
| Pós-fix | `TestResults/Commercial095/post-fix-20260911-001117/` |

## 3. Duração e execução overnight

| Métrica | Valor |
|---------|-------|
| Duration | **122.4 min** (≥ 120 min meta) |
| FailCount (overnight bruto) | **2** (harness — ver BUGS) |
| DecisionHint overnight | YELLOW |
| Pós-fix LoginSessao | **PASS 1/1** |
| Pós-fix OvernightQa | **PASS 3/3** |
| Pós-fix QaEngine | **PASS 43/43** |

## 4. Resultados por suite (overnight + pós-fix)

| Suite | Overnight | Pós-fix | Notas |
|-------|-----------|----------|-------|
| Build Debug | PASS (0 errors) | — | |
| Build Release | PASS (0 errors) | — | |
| Unit tests | PASS **162/162** | — | |
| Security scan | PASS | — | sem secrets tracked |
| StartupStress | **50/50** | — | START→Dashboard→CLOSE |
| QaEngine | **43/43** | **43/43** | Complete UI incluso |
| DeepQa | **6/6** (+ R1–R4 repeats) | — | |
| ExhaustiveUi | **PASS** (+ R1–R4) | — | ver VISUAL / MATRIX |
| LongRun | PASS | — | |
| I18n07 | PASS | — | |
| Tema | **2/2** | — | Light/Dark |
| Sidebar | PASS | — | |
| LoginSessao | FAIL → | **PASS** | C095-01 |
| OvernightQa | FAIL 2/3 → | **PASS 3/3** | C095-02 |
| Database | PASS | — | integrity/FK/migration check |
| InstallerRegression | SKIPPED | — | baseline **COMMERCIAL-08** |

## 5. Exhaustive (última rodada overnight filler)

Fonte: `TestResults/UiSmoke/ExhaustiveUi/exhaustive-summary-latest.md` (2026-09-11 00:10:52)

| Item | Valor |
|------|-------|
| Rounds | Light/Dark × 1366 / 1600 / 1920 / 2560 (**8**) |
| Pages | 18 |
| Buttons discovered | 3288 |
| Tested | **1880** |
| PASS / FAIL / BLOCKED | **1880 / 0 / 0** |
| Popups dismissed | 405 |

## 6. OvernightQa (pós-fix)

| Check | Resultado | Tempo approx. |
|-------|-----------|---------------|
| NavigationStress (20 ciclos) | PASS | ~317 s |
| LanguageStress (20 ciclos) | PASS | ~20 s |
| DialogOpenCloseStress | PASS | ~14 s |

## 7. Correções desta fase

Somente harness/assert (não produto UI):

1. **C095-01** — assert login erro alinhado a `DangerBrush` do tema  
2. **C095-02** — timeout `OvernightQa:*` elevado a 15 min (nav legítima ~5+ min)

Nenhuma alteração de regra de negócio, fiscal, I18N, installer ou signing.

## 8. Limitações (não bloqueadores de produto)

| Limitação | Status |
|-----------|--------|
| DPI 100/125/150 automático | **BLOCKED BY ENVIRONMENT** |
| Installer E2E nesta sessão | SKIPPED — C08 baseline PASS |
| Code signing comercial | BLOCKED (COMMERCIAL-09) |
| Calendar WPF Dark header | KNOWN — sem alteração automática |
| Matriz 24 (4×2×3) monolítica | Coberta por composição Exhaustive + I18n07 + Tema (não um único job 24-way) |
| Emissão fiscal real | NÃO executada (regressão apenas) |

## 9. Decisão

**YELLOW — COMMERCIAL OVERNIGHT QA CLOSED WITH NON-BLOCKING LIMITATIONS**

- P0 produto: **0**  
- P1 produto crítico: **0** (falhas overnight = harness)  
- Crash reproduzível: **0**  
- QA críticos pós-fix: **PASS**  

**STOP.** Não iniciar Commercial-10 / compra de certificado / Fiscal Live / I18N-08.
