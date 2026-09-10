# PRIMOX-I18N-04-MANUAL-UX-AUDIT

**Missão:** Manual Multilingual UX Audit + User-Visible Coverage + Controlled Fixes  
**Baseline HEAD:** `6036a40`  
**Tag `v1.0.0`:** `72d85fa` intacta  
**WIP preservado:** `PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` (não tocado) · Deploy scripts preservados

---

## Fase 0 — Pre-flight

Ver `Docs/qa/PRIMOX-I18N-04-BASELINE.md`  
Build 0 · Localization 20/20 · static ~19% · GO

## Fase 1–2 — Inventário + matriz

- `Docs/qa/PRIMOX-I18N-USER-VISIBLE-SURFACE-MAP.md`
- `Docs/qa/PRIMOX-I18N-04-UX-MATRIX.md` (preenchida com runtime PARTIAL EN/ES)

## Fase 3–4 — Execução real + evidência

Smoke `I18n04:MultilingualUserVisibleAudit`  
Evidência: `Logs/qa-visual/i18n-04/{pt,en,es}/*.png` (48 PNGs)  
Artefato: `i18n04-audit-*.json/md`

## Fase 5–6 — Residuais + glossário

- `Scripts/Audit-I18nRuntimeResiduals.ps1`
- `Docs/qa/PRIMOX-I18N-RESIDUAL-TEXT-MATRIX.md`
- `Docs/qa/PRIMOX-I18N-TRANSLATION-GLOSSARY.md`

## Fase 7 — Layout

Exhaustive/DeepQa cobrem Light/Dark × resoluções.  
Correções P0 focaram bindings (não redução global de fonte).  
Clipping ES: **NOT FULLY SWEPT** além do Exhaustive (documentar LIMITATION).

## Fase 8 — Ajuda

HELP CORE = navegação parcial localizada; corpo **pt-BR** (CONTENT PARTIAL).  
HELP EXTENDED = pendência explícita. **Não** declarar Help multilíngue completo.

## Fase 9 — Correções controladas (P0)

CollapseMenu · SaveDraft · SaveChanges · SaveEmitente · SaveSignature · SaveNewPassword · PDV cancel shortcuts · NewEmployee · ManageProfiles · ConfigurePermissions · SearchClientShortcut  
QaEngine ajustado para localizar botão SaveChanges.

## Fase 10 — Catálogo

`Scripts/Audit-I18nCatalog.ps1` → PT/EN/ES **572** keys · USED 256 · UNUSED 316 · MISSING_EN/ES **0** · DUPLICATE_CANDIDATE groups 30

## Fase 11–12 — Persistência / runtime

Persistência PASS · fallback inválido → pt-BR PASS · LIVE_UPDATE PASS (smoke)

## Fase 13 — Testes

Novo unit: `I18n04_CriticalActionKeys_ExistInAllLanguages`  
Smoke filtro `I18n04` / `MultilingualUx`

## Fase 14 — Role flows

Cobertos indiretamente pelos 15 módulos I18n04 + QaEngine (caixa/OS/estoque/financeiro/admin).  
Walk manual humano completo: **NOT EXECUTED**

## Fase 15 — Métricas

Ver `Docs/qa/PRIMOX-I18N-USER-VISIBLE-COVERAGE.md`

## Fase 16+ — Regressão

Ver `Docs/qa/PRIMOX-I18N-04-REGRESSION.md`

---

## Decisão

**READY WITH LIMITATIONS / YELLOW**

Motivo: navegação EN/ES funciona e evidência visual existe, mas strict user-visible PASS EN/ES = 0% (corpos/empty/Help ainda PT).
