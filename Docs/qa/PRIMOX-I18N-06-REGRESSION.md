# PRIMOX-I18N-06-REGRESSION

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Data:** 2026-09-10  
**Baseline HEAD:** `42cce65`  
**Tag `v1.0.0`:** `72d85fa` intacta

| Suite | Resultado | Artefato |
|-------|-----------|----------|
| Build Release | **0 errors** | local |
| Loc+Fiscal lote | **68/68 PASS** (I18n06_ClosureKeys) | `dotnet test` |
| I18n06 MultilingualClosure | **PASS** | `TestResults/UiSmoke/2026-09-10_18-23-16` · `Logs/qa-visual/i18n-06/` |
| QaEngine | **43/43** | `…/2026-09-10_18-24-10` |
| DeepQa (+LongRun) | **6/6** | `…/2026-09-10_18-29-47` |
| ExhaustiveUi | **PASS** | `…/2026-09-10_18-32-39` |
| DB | integrity **ok** · FK **0** | AutomatedTests `…-183245-15432` |
| Security | sem secrets | review |

### Nota baseline smoke

Primeira tentativa de QaEngine baseline (`18-16-12`) foi interrompida ao liberar lock do `PrimoAutoEletrica.exe` (PRIMOX Workshop).  
Validação completa reexecutada **após** as alterações (tabela acima). Baseline unitário Loc+Fiscal e métricas estáticas foram capturados antes da implementação.
