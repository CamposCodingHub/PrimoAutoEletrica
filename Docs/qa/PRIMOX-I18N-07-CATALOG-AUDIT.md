# PRIMOX-I18N-07 — CATALOG AUDIT

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Fonte:** `Scripts/Audit-I18nCatalog.ps1` · `TestResults/I18n/i18n-catalog-audit-20260910-192721.json`

| Métrica | Valor |
|---------|------:|
| Used | 478 |
| Unused | 317 |
| Missing EN | **0** |
| Missing ES | **0** |
| Duplicate candidate groups | 45 |
| Orphan used (scanner) | 38 |

## Qualidade

- Sem traduções vazias críticas detectadas no Gate/Closure merge
- Gate keys (I18N-07) mescladas em `BuildCatalog` após Closure
- Placeholders `{0}`/`{1}` validados em testes Gate (`VehiclesFoundFormat`, `HistoryWithOsAppointmentsFormat`, `StatusLabelFormat`)
- UNUSED / DUPLICATES: **não removidos** (política de fase)

## Fallback

- Chave inválida → fallback `pt-BR` (probe I18n07 PASS)
- CurrentCulture de formatação permanece **pt-BR**
