# PRIMOX-I18N-07 — REGRESSION

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

| Suite | Resultado | Evidência |
|-------|-----------|-----------|
| Build Release | **0 errors** | local |
| Localization+Fiscal | **69/69 PASS** (+I18n07 Gate test) | `dotnet test` filter Loc/Fiscal/Nfe/Focus |
| I18n07 Final Gate | **PASS** | `TestResults/UiSmoke/2026-09-10_19-27-09` · `Logs/qa-visual/i18n-07/` |
| QaEngine | **43/43** | `TestResults/UiSmoke/2026-09-10_19-30-44` |
| DeepQa (+LongRun) | **6/6** | `TestResults/UiSmoke/2026-09-10_19-38-13` |
| ExhaustiveUi | **PASS** | `TestResults/UiSmoke/2026-09-10_19-41-06` (FullSimulation) |
| DB integrity | **ok** | smoke `primoauto.db` |
| FK check | **0** | |
| Migrations | **nenhuma** criada | |
| Security (secrets) | **limpo** em artefatos I18N-07 | |
| Tag v1.0.0 | **72d85fa** intacta | |
| WIP fiscal | preservado | `PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` |

## Runtime language

| Check | Resultado |
|-------|-----------|
| Switch PT↔EN↔ES | PASS (LIVE_UPDATE) |
| Persistence | PASS |
| Invalid fallback | PASS → pt-BR |
| 10-cycle (DeepQa LongRun + multi-lang audit) | PASS sem crash |

## Módulos regressão funcional

Sem regressão reportada em Login/Shell/Dashboard/Clientes/Veículos/OS/Orçamentos/PDV/Estoque/Financeiro/Agenda/Relatórios/Catálogo/Config/Help Core após Gate.

Fiscal: **não alterado** · testes fiscais no lote Loc+Fiscal PASS.
