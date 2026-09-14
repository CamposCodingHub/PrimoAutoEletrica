# PRIMOX — Fonte de verdade atual (CURRENT TRUTH)

**Atualizado:** 2026-09-14 (NET10-31 intensive)  
**Branch:** `audit/product-discovery-2026-09`  
**HEAD tip:** `20144dd` (ver `git log -1`)  
**Push desta sessão:** **NO**  
**TFM:** `net10.0-windows` / `net10.0`  
**Tag `v1.0.0` / main / primox-net6-final:** protegidos  

## Snapshot CURRENT

| Área | Estado |
|------|--------|
| Unit | **215/215** PASS |
| Build App Release | PASS |
| QaEngine | **43/43** (`TestResults/UiSmoke/net10-31-qaengine`) |
| DeepQa | **6/6** (`net10-31-deepqa`) |
| ExhaustiveUi | APROVADO (`net10-31-exhaustive`) |
| Tema Light/Dark smoke | **1/1** (`net10-31-tema`) |
| Cliente/Veículo 360 + context | IMPROVED (tokens + Novo veículo + Abrir 360) |
| Produto 360 | IMPLEMENTED (hub MessageBox + unit) |
| OS 360 | PARTIAL (Fiscal/Pós-venda MISSING) |
| Dark DataGrid/Fiscal/Auto Elétrica | FIXED (implicit PremiumDataGrid + estilos locais) |
| Financeiro Origem+Ref | DONE (coluna Ref.) |
| G001 ContasReceber.ClienteId | **BLOCKED** |
| API RateLimiter 120/min | **DONE** |
| Dashboard período 7/30/90 | **DONE** |
| G011 Orcamento status | **DONE** |
| 2FA login gate | **PARTIAL** (sem colunas Totp / sem challenge login) |
| Fiscal LIVE / Signing / WA Cloud / TEF / DVI | BLOCKED / MISSING |

## Honestidade 100%

`PROJECT_STATUS.md` checklist histórico **não** pode chegar a 100% puro: itens externos permanecem abertos.  
Matriz visual Light/Dark manual completa de todos os módulos = **NOT TESTED** (parcial via smoke Tema + revisão XAML).

## Desktop

`PRIMOX Workshop.lnk` → `PrimoAutoEletrica\bin\Release\net10.0-windows\PrimoAutoEletrica.exe`
