# PRIMOX Global UI/UX Visual Audit 3.0 (Script 2)

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
