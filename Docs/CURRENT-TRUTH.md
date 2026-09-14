# PRIMOX — Fonte de verdade atual (CURRENT TRUTH)

**Atualizado:** 2026-09-13 (pós NET10-30 + reconciliación PROJECT_STATUS)  
**Branch:** `audit/product-discovery-2026-09`  
**HEAD tip:** ver `git log -1` (commits NET10-30 + status reconcile)  
**Push desta sessão:** **NO**  
**TFM:** `net10.0-windows` / `net10.0`  
**Tag `v1.0.0` / main / primox-net6-final:** protegidos  

## Snapshot CURRENT

| Área | Estado |
|------|--------|
| Unit | **214/214** PASS |
| Build App + API | PASS |
| QaEngine | **43/43** (r2 confirmado; r3 em execução/evidence) |
| DeepQa | **6/6** |
| ExhaustiveUi | APROVADO r1 |
| Cliente/Veículo 360 + context | PASS |
| OS 360 | PARTIAL |
| G001 ContasReceber.ClienteId | **BLOCKED** |
| API RateLimiter 120/min | **DONE** |
| Dashboard período 7/30/90 | **DONE** |
| G011 Orcamento status | **DONE** |
| 2FA login gate | **PARTIAL** (sem colunas Totp / sem challenge login) |
| Fiscal LIVE / Signing / WA Cloud / TEF / DVI | BLOCKED / MISSING |

## Honestidade 100%

`PROJECT_STATUS.md` checklist histórico **não** pode chegar a 100% puro: itens externos permanecem abertos.  
Internos stale foram reconciliados; implementáveis seguros desta sessão fechados.

## Desktop

`PRIMOX Workshop.lnk` → `PrimoAutoEletrica\bin\Release\net10.0-windows\PrimoAutoEletrica.exe`
