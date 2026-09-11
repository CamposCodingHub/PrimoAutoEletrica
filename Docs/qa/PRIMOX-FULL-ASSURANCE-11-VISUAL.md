# PRIMOX FULL ASSURANCE-11 — Visual

## Method

- ExhaustiveUi FullSimulation **2026-09-11 13:00:25** — 8 rounds Light/Dark × 4 resolutions  
- Tema smoke **2/2**  
- Sidebar **PASS**  
- I18n07 **PASS** (PT/EN/ES)

## Exhaustive (this run)

| Metric | Value |
|--------|-------|
| Discovered | 3278 |
| Executable | 1882 |
| Tested | **1882** |
| PASS | **1882** |
| FAIL | **0** |
| BLOCKED | **0** |

## Themes

| Theme | Result |
|-------|--------|
| Light | **PASS** |
| Dark | **PASS** (Calendar header = known WPF limitation) |

## Resolutions

| Res | Light | Dark |
|-----|-------|------|
| 1366×768 | PASS | PASS |
| 1600×900 | PASS | PASS |
| 1920×1080 | PASS | PASS |
| 2560×1440 | PASS | PASS |

## Languages

| Lang | Result |
|------|--------|
| PT-BR | PASS |
| EN-US | PASS |
| ES-ES | PASS |

## Visual findings

| Id | Sev | Notes |
|----|-----|-------|
| VIS-000 | — | No new P0/P1 visual blockers |
| VIS-CAL-DARK | Known | Native WPF Calendar Dark header |

Gold hover remains Sidebar-restricted (Sidebar PASS).
