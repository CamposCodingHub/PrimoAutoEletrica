# PRIMOX-I18N-06-RESIDUALS

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Antes (I18N-05):** TRANSLATION_REQUIRED **357** · UNKNOWN **1348**  
**Depois:** TRANSLATION_REQUIRED **337** (−20) · UNKNOWN **1348**

## Classificação desta fase

| Categoria | Tratamento |
|-----------|------------|
| P0_CRITICAL code-behind (Dashboard attention, PDV suspended, Clientes empty) | **Traduzido** via `LocalizationService.Closure` + `UiText` |
| P1 short XAML labels | **Parcial** — + bindings adicionais |
| P2 long descriptions (Config/Backup/Help body) | **Pendente** |
| P3 Help Extended | **Pendente** (CONTENT PARTIAL) |
| Relatórios event labels (`FuncionarioCriado`, etc.) | TECHNICAL/INTERNAL-ish · **não** traduzidos cegamente |
| Status DB `Rascunho` | INTERNAL status · UI localizer parcial |
| USER_DATA / mock KPI names | **não** traduzir |

## Remanescentes relevantes (EN smoke)

- Orçamentos: insights/status `Rascunho` / textos de painel
- Estoque: empty states + tooltips longos
- Relatórios: rótulos de eventos/auditoria
- Help Extended: corpo pt-BR
- Agenda: subtítulo com “Veículo”

## UNUSED / DUPLICATES

UNUSED ~319 · DUPLICATE_CANDIDATE ~45 — **KEEP** (não remoção em massa).
