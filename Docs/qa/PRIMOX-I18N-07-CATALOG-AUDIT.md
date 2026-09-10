# PRIMOX-I18N-07 — CATALOG AUDIT

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
