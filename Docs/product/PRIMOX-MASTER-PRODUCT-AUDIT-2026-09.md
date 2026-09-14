# PRIMOX Master Product Audit — NET10-31 intensive

**Branch:** `audit/product-discovery-2026-09`  
**Baseline HEAD:** `318f040`  
**Deadline local:** 12:00 (2026-09-14)  
**Status:** IMPLEMENTING → TESTED (parcial visual + unit)

## Inventário visual / UX (onda NET10-31)

| Módulo / superfície | Problema | Melhoria | Prioridade | Status | Teste |
|---|---|---|---|---|---|
| Cliente 360 (`HistoricoClienteWindow`) | White literal; SuccessDark/DangerDark; DataGrid local | Tokens Accent/Success/Danger; BasedOn PremiumDataGrid; Novo veículo | P0/P1 | IMPLEMENTED | Unit + build; UI visual NOT TESTED em runtime manual |
| Auto Elétrica Técnica | DataGrid/ListBox chrome branco no Dark | PremiumDataGrid + OperationalListBox | P0 | IMPLEMENTED | Build; UI theme via QaEngine (em curso) |
| Operações Fiscais | Inputs/grids fora do DS | Implicit TextBox/DataGrid no control + MultiLine detail | P0 | IMPLEMENTED | Build |
| Global DataGrid | Grids sem Style → branco WPF | Implicit `TargetType=DataGrid` → PremiumDataGrid | P0 | IMPLEMENTED | Build; Unit 215 |
| Views com `Foreground=White` | Hardcode | `AccentButtonTextBrush` | P1 | IMPLEMENTED | Build |
| Produto 360 | MISSING | Snapshot + botão Estoque + unit | P1 | IMPLEMENTED | Unit CaseProduto360 PASS |
| ContasReceber.ClienteId | Sem migration segura | — | P0 | BLOCKED | — |
| DVI / Fiscal LIVE / IA fake | — | — | P2/P3 | DEFERRED | — |

## Matriz visual (código + smoke parcial)

| MÓDULO | LIGHT | DARK | INPUT | GRID | MODAL | BUTTON | CARD | STATUS |
|---|---|---|---|---|---|---|---|---|
| Cliente 360 | NOT TESTED | NOT TESTED | PASS* | PASS* | PASS* | PASS* | PASS* | PARTIAL |
| Auto Elétrica | NOT TESTED | PASS* | N/A (sem TextBox) | PASS* | N/A | PASS* | PASS* | PARTIAL |
| Operações Fiscais | NOT TESTED | PASS* | PASS* | PASS* | N/A | PASS* | PASS* | PARTIAL |
| Estoque / Produto 360 | NOT TESTED | NOT TESTED | — | PASS* | MessageBox | PASS* | — | PARTIAL |
| Relatórios / demais | NOT TESTED | PASS* (implicit grid) | — | PASS* | — | — | — | NOT TESTED |

\*PASS* = validado por revisão de XAML + build (+ unit onde aplicável), **não** por inspeção visual manual Light/Dark em runtime.

## BLOCKED / DEFERRED

- **G001** `ContasReceber.ClienteId` — BLOCKED (sem migration / sem TEXT_MATCH)
- DVI entity, portal, QR, aprovação digital remota, TEF, WA Cloud API, 2FA login gate — DEFERRED
- Produto 360 UI ainda MessageBox (hub), não modal DS completo — DEFERRED P2 polish
