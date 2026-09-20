# Cross-check auditoria comercial × código (2026-09-19/20 BRT)

Branch: `audit/product-discovery-2026-09`

## Legenda
- **FEITO** = evidência no código/tests desta branch
- **PARCIAL** = mitigação honesta, escopo incompleto
- **NÃO FEITO** = ainda aberto

| # | Item auditoria | Status | Evidência / nota |
|---|----------------|--------|------------------|
| 1 | API JWT efetiva | **FEITO (esta sessão)** | `AddJwtBearer` + `UseAuthentication` + `RequireAuthorization` + policies ORCAMENTO_*/ESTOQUE_*/FINANCEIRO_*; `/api/health` e `/api/auth/token` anônimos; `SafeProblem` sem `ex.Message` |
| 2 | RBAC fail-closed | **FEITO** | `PermissionCheckResult` + `GarantirPermissaoCritica` |
| 3 | Histórico por nome | **FEITO** | `HistoricoClienteWindow` — matching por nome removido; Primox360 por ClienteId |
| 4 | License server + RSA | **NÃO FEITO** | Ainda scaffold JSON+SHA256; banner `SCAFFOLD_ONLY` no `LicenseService` |
| 5 | .NET 6 → 10 | **FEITO** | Desktop/API/Tests = `net10.0-windows` |
| 6 | Suite testes unificada | **PARCIAL** | Oficial: `Tests/PrimoAutoEletrica.Tests` na sln; pasta raiz `PrimoAutoEletrica.Tests` ainda existe fora da sln |
| 7 | CI false-green | **PARCIAL** | GATEs de vuln/security sem continue-on-error; INFO (StyleCop/FxCop/complexity) ainda soft |
| 8 | Testes segurança reais | **PARCIAL** | Gates + fail-closed + 360 ID; falta WebApplicationFactory 401 live e isolamento multi-tenant HTTP |
| 9 | Dinheiro INTEGER cents | **NÃO FEITO** | SQLite REAL permanece |
| 10 | DatabaseService monolito | **NÃO FEITO** | Dívida arquitetural |
| 11 | App.Database service locator | **NÃO FEITO** | Dívida |
| 12 | NotificationService honesto | **FEITO** | NaoConfigurado |
| 13–15 | TECH moat / Heavy / mercado | **NÃO FEITO** (produto) | Roadmap |
| 16 | DVI de verdade | **PARCIAL** | DVI local + vínculo OS/orçamento; sem portal aprovação/vídeo cloud |
| 17 | Portal cliente | **NÃO FEITO** | |
| 18 | Mobile técnico | **NÃO FEITO** | |
| 19 | WhatsApp Cloud | **NÃO FEITO** | wa.me / local only |
| 20 | Motor pós-venda | **PARCIAL** | Lembretes locais; sem disparo automático Cloud |
| 21 | Frota | **NÃO FEITO** | |
| 22 | Financeiro empresarial deep | **PARCIAL** | Base existe; DRE/BI avançado não |
| 23 | PIX/pagamento real | **NÃO FEITO** | |
| 24 | Fiscal produção | **PARCIAL** | Foundation; produção SEFAZ incompleta |
| 25 | Multi-filial real | **NÃO FEITO** | Unidade local honesta |
| 26 | Cloud/SaaS | **NÃO FEITO** | |
| 27 | Intelligence | **NÃO FEITO** | |
| 28 | PBKDF2 600k | **FEITO** | `Iterations = 600_000` + NeedsRehash |

## Top 15 PROJECT_STATUS
| Prioridade | Item | Status agora |
|------------|------|--------------|
| P0 | Histórico → IDs | FEITO |
| P0 | API auth | FEITO (JWT local) |
| P0 | License server | NÃO FEITO |
| P0 | RBAC fail-closed | FEITO |
| P0 | Testes segurança reais | PARCIAL |
| P0 | CI gate | PARCIAL |
| P1 | .NET 10 | FEITO |
| P1 | Consolidar tests | PARCIAL |
| P1 | Money cents | NÃO FEITO |
| P1 | DVI | PARCIAL |
| P1 | Portal | NÃO FEITO |
| P1 | WhatsApp Cloud | NÃO FEITO |
| P1 | Pós-venda | PARCIAL |
| P1 | Mobile | NÃO FEITO |
| P1 | TECH/HEAVY | NÃO FEITO |

Não afirmar Phases 2–6 / SaaS / “100% sem bugs”.

## Evidência de testes (2026-09-20)
- Unit: 234 PASS
- ExhaustiveUi: 2424 PASS / 0 FAIL (53,93% discovered coverage; 100% of executable)
- QaEngine: 43/43
- DeepQa: APROVADO
