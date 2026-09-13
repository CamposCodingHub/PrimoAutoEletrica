# PRIMOX Global UI/UX Visual Audit 3.0 (Script 2)

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

**Date:** 09/09/2026  
**Decision:** READY WITH LIMITATIONS  
**Depends on:** Script 1 commit Help Center Modelo B  
**Tag:** `v1.0.0` → `a4ad6fe` (intact) · Fiscal = regression only (produção BLOQUEADA)

## Scope

Full inventário visual/a11y/responsive of main modules + convert high-ROI light-only hex to DynamicResource theme brushes. No fiscal production enable.

## Color classification

| Class | Rule |
|-------|------|
| Brand | `PrimaryBrush` / `BrandSoftBrush` / `SidebarBackgroundBrush` |
| Surfaces | `AppBackgroundBrush` / `SurfaceBrush` / `SurfaceAltBrush` / cards |
| Status | `Success*` / `Warning*` / `Danger*` / `Info*` + `*CardBackgroundBrush` |
| Tables | `TableHoverBrush` / `TableSelectedBrush` / `CardHoverBrush` |
| KEEP light-only | Print/preview paper surfaces; Login overlay `#33FFFFFF` |

## P0/P1 fixes applied

| Priority | Fix |
|----------|-----|
| P0 | `Themes/ListView.xaml` hover/selected → theme brushes (was `#F1F5F9` / indigo) |
| P0 | `VeiculosControl` alert chips → `ThemeBrush(Danger/Warning/Info/Success*)` |
| P0 | `ConfigurarPermissoesWindow` DataGrid + delete/stats → DynamicResource |
| P0 | `NovoAgendamentoPremiumWindow` tech/resumo cards → Info/Success theme |
| P1 | `ResetSistemaWindow` full theme bind |
| P1 | `HistoricoClienteWindow` débitos + timeline icon → Danger/Primary |
| P1 | `HistoricoEstoqueWindow`, `OperacaoCaixaWindow`, `EditarClienteWindow` status text |

## Deferred / KEEP LIMITATION

- **Calendar Dark header nativo** — no CalendarItem rewrite (known).
- FinanceiroViewModel chart hex, TendenciaToColorConverter — residual P2.
- AssinaturaDigital `#6B7280`, ConfirmacaoCritica `#334155` — low traffic residual.
- OS print/preview hex — intentional paper surfaces.

## Visual evidence (PNGs read)

Path: `Logs/qa-visual/fase12-a11y/`

- `dashboard-light/dark-1366x768.png` — coherent Light/Dark cards, orange primary CTA
- `veiculos-dark-1366x768.png` — “Monitorado” success chip themed (not light pastel on dark)
- `agendamentos-dark-1366x768.png` — Agenda Dark coherent; Calendar remaining LIMITATION
- Help PNGs remain Modelo B (Script 1)

## QA

| Suite | Folder | Result |
|-------|--------|--------|
| BUILD | — | 0 errors |
| DeepQa | `TestResults/UiSmoke/2026-09-09_08-05-00` | 6/6 |
| QaEngine | `TestResults/UiSmoke/2026-09-09_08-08-05` | 43/43 |
| Exhaustive | `TestResults/UiSmoke/2026-09-09_08-15-40` | discovered=3276 tested=1933 pass=1933 fail=0 blocked=0 |

## WIP preserved untracked

- `Scripts/Atualizar-PrimoAuto.bat`
- `Scripts/Deploy-ToInstalledApp.ps1`
