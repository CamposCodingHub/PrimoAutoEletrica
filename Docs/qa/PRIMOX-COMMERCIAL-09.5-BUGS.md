# PRIMOX-COMMERCIAL-09.5 — Bugs

**Regra:** Scanner ≠ bug. Corrigir só defeito real ou harness que mascarava/falsificava PASS/FAIL.

## Resumo

| Sev | Found | Fixed | Product? |
|-----|-------|-------|----------|
| P0 | 0 | 0 | — |
| P1 | 0 produto | 0 | — |
| P2 harness | 2 | 2 | Não (QA assert / timeout) |
| P3 | 0 novos | 0 | — |

## C095-01 — Login error color assertion vs DangerBrush

| Campo | Valor |
|-------|-------|
| ID | C095-01 |
| Categoria | ACCESSIBILITY / QA (assert) |
| Tela | Login |
| Idioma / Tema | n/a (assert de cor) |
| Reprodução | `SmokeFilter=LoginSessao` → mensagem inválida |
| Severidade | **P2 harness** (falso FAIL; UI já usava design system) |
| Causa | Assert hardcodava `#B91C1C`; tema usa `DangerBrush` (`#DC2626` Light / `#EF4444` Dark) |
| Correção | `UiSmokeTestService.Login.cs` — comparar `Foreground` com `DangerBrush` resolvido |
| Regressão | LoginSessao **PASS** (`post-fix-20260911-001117`) |

**Não** é bug visual de produto: a cor de erro do login já seguia o tema.

## C095-02 — OvernightQa NavigationStress timeout 120s

| Campo | Valor |
|-------|-------|
| ID | C095-02 |
| Categoria | ENVIRONMENT / QA (harness) |
| Tela | Multi-módulo (NavigationStress) |
| Reprodução | `SmokeFilter=OvernightQa` com 20 ciclos |
| Severidade | **P2 harness** |
| Causa | Check legítimo ~317 s; `RunCheck` default 120 s |
| Correção | Threshold `OvernightQa:` → **900000 ms** (15 min) |
| Regressão | OvernightQa **3/3 PASS**; NavigationStress ~317 s PASS |

## Não tratados / não bugs

| Item | Classificação |
|------|---------------|
| Calendar Dark header | KNOWN WPF — não alterar automaticamente |
| NETSDK1138 net6.0 EOL warning | Ambiente/SDK — não bloqueante desta fase |
| Temporary password popups no Exhaustive | Comportamento esperado de QA; evidência local — **não** versionar segredos |
| Termos fiscais / NCM / IDs | Legítimos — não TRANSLATION bugs |

## Loop de correção

1. Overnight FAIL LoginSessao + OvernightQa  
2. Reprodução isolada  
3. Classificação harness  
4. Fix assert + timeout  
5. Rebuild Release  
6. Retest LoginSessao → OvernightQa → QaEngine — **todos PASS**
