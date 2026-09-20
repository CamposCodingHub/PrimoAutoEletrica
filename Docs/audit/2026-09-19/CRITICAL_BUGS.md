# A — CRITICAL_BUGS (Phase 0 — 2026-09-19 BRT)

**Branch:** `audit/product-discovery-2026-09`  
**Baseline commit (pré-fix):** `aa744ac`  
**Hierarquia de evidência:** RUNTIME > CODE > DB > TESTS > DOCS  
**Regra:** nenhum PASS/DONE inventado. API/SaaS/fiscal/sync **não** estão prontos.

## P0-01 — API sem autenticação (CONFIRMADO)

| Campo | Valor |
|-------|-------|
| ID | P0-01 |
| PRIORIDADE | P0 / Crítico |
| CATEGORIA | Segurança / API |
| ARQUIVO | `PrimoAutoEletrica.Api/Program.cs` |
| LINHA | endpoints ~59–177; `UseAuthorization` ~56 sem `AddAuthentication` |
| PROBLEMA | Minimal APIs de negócio (`/api/orcamentos`, `/api/estoque/produtos`, `/api/financeiro/*`) expostas sem JWT/auth. `UseAuthorization()` sem schema é efetivamente no-op. `Results.Problem(..., ex.Message)` vaza detalhe interno. |
| EVIDÊNCIA | CODE: ausência de `AddAuthentication`/`AddJwtBearer`/`RequireAuthorization`; presença de MapGet/MapPost de negócio. TEST: `ApiAuthenticationGateTests.Program_Cs_EstadoAtual_SemJwt_DocumentadoComoRiscoCritico`. |
| RISCO | Qualquer cliente na rede (ou túnel) lê/altera orçamentos, estoque e financeiro. |
| COMO REPRODUZIR | Subir API e `curl` GET/POST nos endpoints sem header Authorization — esperado 200/201 hoje. |
| CAUSA RAIZ | API scaffolding sem auth bound ao desktop-only threat model. |
| CORREÇÃO | Ligar JWT (ou Windows Auth intranet) + `RequireAuthorization` em grupo de endpoints de negócio; health público; ProblemDetails sem `ex.Message` em produção. Gate skipado em `Program_Cs_DeveTerJwtERequireAuthorization_AntesDeProducao` deve ser reativado. |
| TESTE NECESSÁRIO | Integração: 401 sem token; 200 com token válido; health anônimo. |
| STATUS | ABERTO — **API NÃO está production-ready**. Gate honesto documentado (sem fake de licensing server). |
| COMMIT | (este PR) |
| REGRESSÃO | Não regressar para endpoints anônimos de negócio. |

## P0-02 — PermissionService fail-open (CONFIRMADO → MITIGADO neste PR)

| Campo | Valor |
|-------|-------|
| ID | P0-02 |
| PRIORIDADE | P0 / Crítico |
| CATEGORIA | Segurança / Autorização desktop |
| ARQUIVO | `PrimoAutoEletrica/Services/PermissionService.cs`, `PermissionCheckResult.cs` |
| LINHA | `VerificarPermissaoCodigo`, `ConsultarPermissaoPersistida`, `GarantirPermissaoCritica` |
| PROBLEMA | Em erro de DB, catch retornava null e o fluxo caía em fallback hardcoded (fail-open). |
| EVIDÊNCIA | CODE pré-fix: catch → null → `ObterCodigosPermitidosFallback`. Pós-fix: Unavailable ≠ fallback; críticos usam `GarantirPermissaoCritica`. |
| RISCO | Operador não privilegiado executa exclusão/ajuste/financeiro quando SQLite falha. |
| COMO REPRODUZIR | Simular exceção no lookup (teste unitário `PermissionServiceFailClosedTests`). |
| CAUSA RAIZ | Null ambiguava “sem linha” e “erro de infra”. |
| CORREÇÃO | `PermissionCheckResult` { Allowed, Denied, Unavailable }; fail-closed em Unavailable; call sites críticos via `ValidarPermissao` → `GarantirPermissaoCritica`. |
| TESTE NECESSÁRIO | `PermissionServiceFailClosedTests` (obrigatório). |
| STATUS | MITIGADO em código neste PR — validar build/test no Windows. |
| COMMIT | (este PR) |
| REGRESSÃO | Não reintroduzir fallback em catch de infra. |

## P0-03 — Histórico financeiro por nome Contains (CONFIRMADO → MITIGADO)

| Campo | Valor |
|-------|-------|
| ID | P0-03 |
| PRIORIDADE | P0 / Crítico |
| CATEGORIA | Integridade de dados / Privacidade |
| ARQUIVO | `PrimoAutoEletrica/Views/HistoricoClienteWindow.xaml.cs` |
| LINHA | load financeiro ~295 (`ObterContasReceberVinculadasAoCliente(_cliente.Id)`); método `ClienteCorresponde` removido |
| PROBLEMA | Anti-padrão `Contains` entre nomes misturava contas de “JOÃO SILVA” e “JOÃO SILVA JUNIOR”. |
| EVIDÊNCIA | CODE: load já usava ClienteId via Primox360; método nome-based removido neste PR. Primox360 declara não usar TEXT_MATCH para KPIs financeiros. |
| RISCO | Vazamento de dívida/recebíveis entre clientes homônimos. |
| COMO REPRODUZIR | Dois clientes nomes similares + contas distintas — ver teste de isolamento. |
| CAUSA RAIZ | Matching textual legado. |
| CORREÇÃO | Somente ClienteId / Origem+ReferenciaExterna; regressão em `HistoricoClienteFinancialIsolationTests`. |
| TESTE NECESSÁRIO | Dois clientes nomes similares não compartilham histórico. |
| STATUS | MITIGADO — runtime Windows ainda deve ser validado. |
| COMMIT | (este PR) |
| REGRESSÃO | Proibido Contains de nome para finanças. |

## P0-04 — LicenseService local + SHA256 (CONFIRMADO, sem “servidor fake”)

| Campo | Valor |
|-------|-------|
| ID | P0-04 |
| PRIORIDADE | P0 / Crítico (comercial) |
| CATEGORIA | Licensing |
| ARQUIVO | `PrimoAutoEletrica/Services/LicenseService.cs` |
| LINHA | `ActivateLicense` ~AddYears(1); hardware via SHA256 MachineName/UserName; JSON local |
| PROBLEMA | Ativação local sem assinatura assimétrica; trivial forjar/alterar arquivo. |
| EVIDÊNCIA | CODE |
| RISCO | Bypass de licença. |
| COMO REPRODUZIR | Ativar com chave qualquer e editar JSON de licença. |
| CAUSA RAIZ | Licenciamento offline simplificado. |
| CORREÇÃO | Assinatura assimétrica + clock skew + binding; **não** inventar licensing server neste PR. |
| TESTE NECESSÁRIO | Rejeitar licença com assinatura inválida. |
| STATUS | ABERTO (documentado; fora do escopo de implementação Phase 0 além da honestidade). |
| COMMIT | n/a |
| REGRESSÃO | n/a |

## P0-07 — CI continue-on-error / \|\| true (CONFIRMADO → MITIGADO)

| Campo | Valor |
|-------|-------|
| ID | P0-07 |
| PRIORIDADE | P0 |
| CATEGORIA | Engenharia / Qualidade |
| ARQUIVO | `.github/workflows/ci.yml`, `performance-security.yml`, `code-quality.yml` |
| PROBLEMA | Security scan e checks vulneráveis mascarados com `continue-on-error` / `\|\| true`. |
| EVIDÊNCIA | CODE workflows |
| RISCO | Main verde com falhas de segurança. |
| CORREÇÃO | Jobs **GATE:** vs **INFO:**; CodeQL e vulnerability list passam a falhar o gate; Lighthouse/StyleCop/FxCop/License permanecem INFO. |
| STATUS | MITIGADO nos YAML deste PR. |
| COMMIT | (este PR) |
| REGRESSÃO | Não recolocar continue-on-error em GATE. |

## Outros críticos confirmados (sem fix destrutivo neste PR)

| ID | Problema | Status |
|----|----------|--------|
| P0-05 | Dinheiro SQLite REAL | ABERTO — documentado em DATA_INTEGRITY; sem migration destrutiva |
| P0-06 | DatabaseService / App.* service locator | ABERTO — ARCHITECTURE |
| P0-08 | Testes fracos / helpers inventados | MITIGADO parcial — novos testes reais PermissionService |
| P0-09 | Fragmentação de suites | ABERTO |
| P0-10 | (script) net6 “preso” | **REFUTADO neste branch** — TFM `net10.0-windows` desktop+API |

## TFM (correção ao narrative)

PrimoAutoEletrica/PrimoAutoEletrica.csproj:5:    <TargetFramework>net10.0-windows</TargetFramework>
PrimoAutoEletrica.Api/PrimoAutoEletrica.Api.csproj:5:    <TargetFramework>net10.0-windows</TargetFramework>

Hipótese “ainda em net6” está **desatualizada** para `audit/product-discovery-2026-09`. Verificar `main` separadamente se necessário.


## P0-04 — LicenseService (SCAFFOLD ONLY — não “consertado” com servidor fake)

| Campo | Valor |
|-------|-------|
| STATUS | ABERTO / DOCUMENTADO |
| EVIDÊNCIA | `ActivateLicense` grava JSON local; validade `AddYears(1)`; fingerprint SHA256 MachineName/UserName — **não** assinatura assimétrica |
| CORREÇÃO NESTE PR | Nenhuma implementação de licensing server (proibido fake). Apenas registro honesto do risco. |
| PRODUÇÃO | **NÃO** usar como proteção comercial real |

## Atualizacao 2026-09-20 (audit tip faddcab+)

| ID | Status atualizado |
|----|-------------------|
| P0-01 API JWT | **MITIGADO no audit** (`AddJwtBearer`, policies, `ApiJwtHttpTests`). **Ainda ABERTO em main** (sem merge). |
| P0-02 RBAC fail-open | **MITIGADO** (expandido CriticalPermissionCodes 2026-09-20) |
| P0-03 Historico por nome | **MITIGADO** |
| P0-04 License | **ABERTO comercial** / honesty flag `IsCommercialScaffoldOnly` |
| P0-01 ProblemDetails leak | **MITIGADO** (`SafeProblem`, 0 `ex.Message`) |

Merge audit→main: **adiado** por decisao do user (2026-09-20).
