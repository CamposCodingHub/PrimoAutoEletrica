# PRIMOX-I18N-04-REGRESSION

**Data:** 2026-09-10

| Suite | Resultado | Artefato |
|-------|-----------|----------|
| Build Release | **0 errors** | local |
| Localization (incl. `I18n04_CriticalActionKeys`) | **20/20 PASS** | `dotnet test --filter Localization` |
| Loc+Fiscal+Nfe+Focus lote | **66/66 PASS** | `dotnet test` |
| Fiscal lote | **PASS** (incluído no lote 66) | idem |
| I18n04 MultilingualUx | **PASS** | `TestResults/UiSmoke/2026-09-10_07-37-50` |
| QaEngine (+ CompleteUi) | **43/43 PASS** | `…/2026-09-10_07-48-13` (retry após fix SaveChanges) |
| DeepQa (+ LongRun) | **6/6 PASS** | `…/2026-09-10_07-53-47` |
| ExhaustiveUi | **PASS** 1941/1941 FAIL=0 (TestResults/UiSmoke/2026-09-10_07-56-42) | `TestResults/I18n/i18n04-exhaustive.log` |
| DB integrity | integrity **ok** · FK **0** | AutomatedTests isolation |
| Security | sem secrets introduzidos | review |

### Falha transitória

QaEngine primeira rodada **42/43**: `ClientePersistenciaRoundTrip` — botão passou a exibir `Salvar alterações` (chave `SaveChanges`).  
Correção: smoke aceita `SaveChanges` / acentos / `Save`. Re-run **43/43**.

### Tag

`v1.0.0` → `72d85fa` intacta.
