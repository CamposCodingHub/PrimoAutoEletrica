# PRIMOX-COMMERCIAL-09.5 — Test Matrix

> **DOCUMENTO REESCRITO EM CAMADAS — 2026-09-13**
>
> | Camada | Uso |
> |--------|-----|
> | **Estado atual** | Fonte operacional hoje · ver também `Docs/CURRENT-TRUTH.md` e NET10-26 |
> | **Avanços desta fase (histórico)** | Registro do que esta execução entregou — **não** sobrescrever mentalmente o estado atual |
>
> HEAD de referência pós-NET10-26: `1372e11` · TFM `net10.0-windows` · Branch `migration/net10`
> Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md`

## Estado atual (pós NET10-26 · 2026-09-13)

| Item | Valor |
|------|-------|
| Branch | `migration/net10` |
| HEAD fiscal foundation | `1372e11` |
| TFM | `net10.0-windows` |
| Unit | **194/194** |
| QaEngine | **43/43** |
| DeepQa | **6/6** (baseline NET10-26) |
| Fiscal LIVE / WhatsApp API / Code signing | **BLOCKED_EXTERNAL** |
| Calendar Dark | Mitigado (`CalendarContrastHealer`) — não citar KNOWN LIMITATION antigo como atual |
| NF-e | PARTIAL + TESTED (Focus path + Fake) |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY |
| DANFE | PDF informativo (≠ SEFAZ oficial) |
| Multiempresa fiscal | IMPLEMENTED + TESTED (DB) |

**Claims abaixo sobre net6, “emissão NÃO IMPLEMENTADO”, Unit 173, DANFE/cancel NI, Calendar Dark KNOWN LIMITATION, etc. pertencem ao registro histórico da fase.**

---

## Avanços desta fase (registro histórico — preservar)

## 1. Build

| Config | Result |
|--------|--------|
| Debug | PASS — 0 errors |
| Release | PASS — 0 errors |
| Publish oficial (esta sessão) | não reexecutado como gate; binários Release smoke usados |

## 2. Startup stress

| Ciclos | Sucesso | Crash | Freeze | Residual |
|--------|---------|-------|--------|----------|
| 50 | **50/50** | 0 | 0 | não observado no summary |

## 3. Navigation / language / dialog

| Stress | Target | Result |
|--------|--------|--------|
| NavigationStress | ≥20 ciclos módulos DeepQa Light/Dark | **PASS** (pós-fix) |
| LanguageStress | ≥20 (PT↔EN↔ES) | **PASS** |
| DialogOpenCloseStress | open/close repeat | **PASS** |

## 4. Functional modules (via QaEngine / DeepQa / Exhaustive)

| Módulo | Status |
|--------|--------|
| Dashboard | PASS |
| Clientes / Veículos / OS / Orçamentos | PASS |
| PDV | PASS (sem emissão fiscal real) |
| Estoque / Financeiro / Fornecedores / Funcionários | PASS |
| Agenda | PASS (Calendar Dark = known) |
| Relatórios / Catálogo / Importar NF-e / Ajuda / Config | PASS |
| Login / Logout | PASS pós-fix |

CRUD: dados **QA isolados** (smoke AppData); sem destruição de dados reais.

## 5. Themes × languages × resolutions

| Dimensão | Cobertura | Result |
|----------|-----------|--------|
| Light / Dark | Tema 2/2 + Exhaustive 8 rounds | PASS |
| PT / EN / ES | I18n07 + LanguageStress | PASS operacional |
| 1366 / 1600 / 1920 / 2560 | Exhaustive | PASS |
| 4×2×3 = 24 monolítico | **não** um único job | Cobertura **composta** — documentado |
| Window resize intermediário | Exhaustive multi-res + LongRun | PASS aceitável |
| DPI 100/125/150 | — | **BLOCKED BY ENVIRONMENT** |

## 6. Accessibility / keyboard / controls

| Área | Via | Result |
|------|-----|--------|
| Focus / Tab / Esc / dialogs | Exhaustive + Dialog stress | PASS (0 FAIL buttons) |
| Tooltips / Automation | Exhaustive / Complete UI | PASS |
| DataGrid | módulos CRUD exhaustivos | PASS |
| Empty / Loading / Error | popups esperados dismiss | PASS (business expected) |

## 7. Database / security / installer / memory

| Gate | Result |
|------|--------|
| Integrity / FK / migration check | PASS |
| Secret / credential / artifact scan | PASS (no tracked secrets) |
| Installer install↔uninstall | SKIPPED → **C08** baseline PASS |
| Memory / long session | OvernightQa peak WS logado; LongRun + Exhaustive repeats ≥2h | PASS / sem leak classificado |
| Randomized safe actions | Exhaustive **1880** tested (≥500) | PASS |

## 8. Fiscal / I18N / Commercial-09

| Gate | Result |
|------|--------|
| Fiscal regression (no live emit) | PASS |
| I18N-07 | PASS — **não** abrir I18N-08 |
| Code signing | BLOCKED BY EXTERNAL CERTIFICATE |

## 9. Verdict matrix → decisão

| Critério GREEN | Met? |
|----------------|------|
| Sem P0/P1 produto | SIM |
| Sem crash reproduzível | SIM |
| QA críticos PASS | SIM (pós-fix) |
| Limitações só ambiente/externas | SIM (DPI, installer skip-session, signing, Calendar) |

→ **YELLOW** (closed with non-blocking limitations)
