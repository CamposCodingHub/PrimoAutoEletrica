# PRIMOX Help Center Visual Audit 3.0

**Date:** 09/09/2026  
**Decision:** READY WITH LIMITATIONS  
**Tag:** `v1.0.0` → `a4ad6fe` (intact)

## Problem

Overnight Global Audit 2.0 marked Help Light as **CORRIGIDO** after theme bind (`SetResourceReference`). That fixed wrong Light brushes, but **layout/navigation still read as a second dark sidebar** with system-blue TreeView selection.

## Cause

- Left panel treated like primary nav (Surface / heavy dark strip feel).
- Default `TreeViewItem` selection chrome (system highlight).
- Index width/visual weight competing with MainWindow Sidebar.

## UX decision — MODELO B

Compact **secondary index**, not a second Sidebar:

| Item | Choice |
|------|--------|
| Width | 220 (Min 180 / Max 260) |
| Index background | `AppBackgroundBrush` |
| Selection | `BrandSoftBrush` + 3px `PrimaryBrush` left border |
| Header | “ÍNDICE DA AJUDA” / “Tópicos e guias” |
| Badge | “AJUDA INCLUSA” BrandSoft |
| Focus | `SystemParameters.FocusVisualStyleKey` (Primox), not `{x:Null}` |

## Files

- `PrimoAutoEletrica/UserControls/HelpControl.xaml`
- `PrimoAutoEletrica/Services/UiSmokeTestService.DeepQa.cs` (Help in visual modules)
- `PrimoAutoEletrica/Services/UiSmokeTestService.PrimoxQa.CompleteUi.cs` (index ≤260 + topic nav)

## Visual evidence (PNGs read)

Path: `PrimoAutoEletrica/bin/Debug/net6.0-windows/Logs/qa-visual/fase12-a11y/`

- `help-light-1366x768.png` … `help-light-2560x1440.png`
- `help-dark-1366x768.png` … `help-dark-2560x1440.png`

**Observations:** Light index is light/secondary; selected topic shows soft BrandSoft + orange bar (not system blue); Dark coherent with AppBackground; no heavy second dark sidebar chrome.

## QA (Script 1)

| Suite | Folder | Result |
|-------|--------|--------|
| BUILD | — | 0 errors |
| ExhaustiveButtonSimulation | `TestResults/UiSmoke/2026-09-09_07-24-47` | discovered=3266 tested=1933 pass=1933 fail=0 blocked=0 |
| DeepQa | `TestResults/UiSmoke/2026-09-09_07-47-47` | 6/6 PASS (Help PNGs) |
| QaEngine | `TestResults/UiSmoke/2026-09-09_07-51-26` | 43/43 · `CompleteUiHelpCenter` PASS |

## Limitations

1. Overnight “Help Light CORRIGIDO” ≠ layout fix (documented to avoid false confidence).
2. DeepQa frames Help content; full MainWindow+Sidebar juxtaposition may differ slightly.
3. Dark BrandSoft can read as stronger orange fill than Light tint.
4. WIP deploy scripts remain untracked by design.

## Next

Script 2 — Global UI/UX visual audit (after this commit on HEAD).
