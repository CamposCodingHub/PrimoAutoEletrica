# PRIMOX Workshop — CRITICAL BUGS (P0)

> Cada item segue o formato obrigatório da auditoria.
> Apenas itens com risco de: vulnerabilidade, corrupção de dados, exposição de dados, operação incorreta, prejuízo financeiro, erro grave de autorização, risco fiscal, impossibilidade de comercialização segura, ou falha arquitetural fundamental.

---

## P0.01 — API Endpoints Sem Autenticação

| Campo | Detalhe |
|---|---|
| **ID** | P0.01 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Segurança / API |
| **ARQUIVO** | `PrimoAutoEletrica.Api/Program.cs` |
| **LINHA** | 65-94, 126-128, 202-323 |
| **PROBLEMA** | API possuía pacotes JWT mas sem wiring efetivo de `AddAuthentication`/`AddJwtBearer`. Endpoints de negócio (orçamentos, estoque, financeiro) acessíveis sem autenticação. |
| **EVIDÊNCIA** | Código fonte verificado — lines 65-94 agora implementam auth; todos endpoints usam `.RequireAuthorization(policy)`. |
| **RISCO** | Operações de negócio acessíveis sem autenticação quando API exposta. |
| **COMO REPRODUZIR** | Antes da correção: `curl GET /api/orcamentos` retornava 200. |
| **CAUSA RAIZ** | Pacotes JWT importados mas middleware não configurado. |
| **CORREÇÃO** | JWT Bearer com `AddAuthentication`, `AddJwtBearer`, policies por permissão, `RequireAuthorization` em cada endpoint. `SafeProblem()` para erros internos. |
| **TESTE NECESSÁRIO** | `ApiJwtHttpTests` — sem token→401, token inválido→401, token expirado→401, sem permissão→403, com permissão→não 401/403, health anônimo→200. |
| **STATUS** | ✅ CORRIGIDO — evidência em `Tests/PrimoAutoEletrica.Tests/Security/ApiJwtHttpTests.cs` (10 testes). |
| **REGRESSÃO** | Nenhuma observada. |

---

## P0.02 — PermissionService Fail-Open em Operações Críticas

| Campo | Detalhe |
|---|---|
| **ID** | P0.02 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Segurança / Autorização |
| **ARQUIVO** | `PrimoAutoEletrica/Services/PermissionService.cs` |
| **LINHA** | 200-210, 266-277 |
| **PROBLEMA** | Em caso de erro de infraestrutura (DB down), permissões críticas poderiam usar fallback de perfil, liberando operações que deveriam ser bloqueadas. |
| **EVIDÊNCIA** | `PermissionCheckResult` tristate implementado (Allowed/Denied/Unavailable). `GarantirPermissaoCritica()` retorna false se Unavailable. 22 códigos em `CriticalPermissionCodes`. |
| **RISCO** | Exclusão de dados, operações financeiras, ajustes de estoque liberados em cenário de falha de infraestrutura. |
| **COMO REPRODUZIR** | Simular lookup que lança exception → verificar que operação crítica é bloqueada. |
| **CAUSA RAIZ** | Padrão original: erro = fallback de perfil. |
| **CORREÇÃO** | Fail-closed: Unavailable = bloqueia. Fallback somente para não-críticas e apenas quando sem linha persistida. |
| **TESTE NECESSÁRIO** | `PermissionServiceFailClosedTests` — 10 testes reais contra componente de produção. |
| **STATUS** | ✅ CORRIGIDO — evidenciado por testes. |
| **REGRESSÃO** | Nenhuma observada. |

---

## P0.03 — Relacionamento Financeiro por Comparação de Nomes

| Campo | Detalhe |
|---|---|
| **ID** | P0.03 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Integridade de Dados |
| **ARQUIVO** | `PrimoAutoEletrica/Views/HistoricoClienteWindow.xaml.cs`, `PrimoAutoEletrica/Services/Primox360Service.cs` |
| **LINHA** | HistoricoClienteWindow.xaml.cs:310-312, Primox360Service.cs:82,87 |
| **PROBLEMA** | Histórico financeiro associava clientes por comparação de nomes (Contains). Clientes homônimos ou com nomes parciais ("João da Silva" / "João da Silva Filho") poderiam ter dados financeiros cruzados. |
| **EVIDÊNCIA** | Comment na line 310: "matching financeiro por NOME removido". Primox360Service usa `ClienteId` exclusivamente. FinanceiroDatabaseService.ResolverClienteIdPorNome recusa homônimos (matches.Count == 1). |
| **RISCO** | Prejuízo financeiro — cliente vê dívida de outro; cobranças incorretas. |
| **COMO REPRODUZIR** | Criar "João da Silva" e "João da Silva Filho", criar contas separadas, verificar 360 de cada um. |
| **CAUSA RAIZ** | Design original usava nome como chave de associação. |
| **CORREÇÃO** | Relacionamentos por ID. Nome apenas para apresentação/busca. |
| **TESTE NECESSÁRIO** | `Primox360IdFinancialJoinTests` (source proof) + testes de runtime recomendados. |
| **STATUS** | ✅ CORRIGIDO — evidência em código + testes de source code. |
| **REGRESSÃO** | Nenhuma observada. |

---

## P0.04 — License Scaffold Sem Assinatura Criptográfica

| Campo | Detalhe |
|---|---|
| **ID** | P0.04 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Segurança / Comercialização |
| **ARQUIVO** | `PrimoAutoEletrica/Services/LicenseService.cs` |
| **LINHA** | 1-2, 111-138, 183-186 |
| **PROBLEMA** | Licença armazenada em JSON local sem assinatura criptográfica (RSA/ECDSA). `ActivateLicense()` gera licença localmente com validade arbitrária. Arquivo manipulável. |
| **EVIDÊNCIA** | Line 1: `SCAFFOLD_ONLY`. Line 13: `IsCommercialScaffoldOnly = true`. Sem verificação de assinatura digital. |
| **RISCO** | Impossibilidade de comercialização segura — licença pode ser copiada, editada ou forjada. |
| **COMO REPRODUZIR** | Editar `Config/license_*.json` → alterar ExpirationDate → licença aceita. |
| **CAUSA RAIZ** | Scaffold inicial sem license server. |
| **CORREÇÃO** | PLANEJADO — requer license server com assinatura RSA/ECDSA. Honestamente documentado como scaffold. |
| **TESTE NECESSÁRIO** | `LicenseScaffoldHonestyTests` (verifica que flag IsCommercialScaffoldOnly = true). |
| **STATUS** | ⚠️ DOCUMENTADO — não corrigível sem infraestrutura cloud. Flag de honestidade implementada. |
| **REGRESSÃO** | N/A |

---

## P0.05 — Plataforma .NET 6 (Legacy)

| Campo | Detalhe |
|---|---|
| **ID** | P0.05 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Plataforma |
| **ARQUIVO** | Todos os .csproj |
| **PROBLEMA** | Desktop estava em .NET 6 (EOL). |
| **EVIDÊNCIA** | Todos .csproj agora em `net10.0-windows` ou `net10.0`. |
| **STATUS** | ✅ CORRIGIDO — migração completa para .NET 10. |

---

## P0.06 — Security Tests Self-Validating

| Campo | Detalhe |
|---|---|
| **ID** | P0.06 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Testes / Segurança |
| **ARQUIVO** | `PrimoAutoEletrica.Tests/Security/SecurityTests.cs` |
| **LINHA** | 105-117, 188-211, 253-269 |
| **PROBLEMA** | Testes de segurança implementavam helpers internos (HashPassword, ValidateCredentials, CanAccess, ValidateSecureInput) e testavam esses helpers. Não testavam componentes reais do produto. |
| **EVIDÊNCIA** | `ValidateSecureInput` em line 105 — função local. `HashPassword` em line 188 — SHA256 inline. `ValidateCredentials` em line 207 — hardcoded admin/Admin@123. Nenhum usa `PasswordHasherService`, `PermissionService` ou API real. |
| **RISCO** | Pipeline reporta "Security Tests PASS" sem validar segurança real. |
| **CAUSA RAIZ** | Testes criados antes dos componentes de segurança. |
| **CORREÇÃO** | Arquivo excluído via `<Compile Remove>`. Testes reais criados: `PermissionServiceFailClosedTests`, `ApiJwtHttpTests`, `PasswordHasherRealTests`. |
| **TESTE NECESSÁRIO** | N/A — os testes reais são a correção. |
| **STATUS** | ✅ CORRIGIDO — 34+ testes de segurança reais em `Tests/PrimoAutoEletrica.Tests/Security/`. |
| **REGRESSÃO** | Nenhuma. |

---

## P0.07 — API Integration Tests com Assertions Fracas

| Campo | Detalhe |
|---|---|
| **ID** | P0.07 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Testes / API |
| **ARQUIVO** | `PrimoAutoEletrica.Tests/Api/ApiIntegrationTests.cs` |
| **LINHA** | 88-95, 119, 148-155, 192, 202, 227, 241 |
| **PROBLEMA** | Testes de API aceitam `OK || NotFound` ou `IsSuccessStatusCode || NotFound` em endpoints protegidos, sem enviar token JWT. Testes passam mesmo que auth esteja quebrado. |
| **EVIDÊNCIA** | Line 94: `Assert.Equal(HttpStatusCode.OK, response.StatusCode)` — endpoint protegido sem token, esperando 200. Line 119: `Assert.True(OK || NotFound)`. |
| **RISCO** | Auth pode ser removida sem que testes detectem. |
| **COMO REPRODUZIR** | Remover `RequireAuthorization` de endpoint → testes antigos ainda passam. |
| **CAUSA RAIZ** | Testes criados antes da implementação de auth. |
| **CORREÇÃO** | Testes reais criados em `ApiJwtHttpTests.cs` — validam 401/403/200 com precisão. Os testes antigos em `PrimoAutoEletrica.Tests/Api/` estão no projeto root (que não referencia a API). Os testes reais estão em `Tests/PrimoAutoEletrica.Tests/Security/ApiJwtHttpTests.cs`. |
| **STATUS** | ✅ MITIGADO — testes reais existem ao lado. Testes antigos no projeto root não executam porque não têm ProjectReference da API. |

---

## P0.08 — Test Suite Não Consolidada

| Campo | Detalhe |
|---|---|
| **ID** | P0.08 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Infraestrutura de Testes |
| **ARQUIVO** | `PrimoAutoEletrica.sln` |
| **PROBLEMA** | `Tests/PrimoAutoEletrica.Tests` com 28 testes reais (fiscal, permissão, DB, JWT) não estava no .sln. `dotnet test PrimoAutoEletrica.sln` não executava esses testes. |
| **EVIDÊNCIA** | Projeto agora confirmado no .sln. `dotnet test --list-tests` descobre os testes. |
| **STATUS** | ✅ VERIFICADO — projeto já estava adicionado ao .sln recentemente. |

---

## P0.09 — Colunas Monetárias como REAL (IEEE 754)

| Campo | Detalhe |
|---|---|
| **ID** | P0.09 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | Integridade de Dados / Financeiro |
| **ARQUIVO** | `DatabaseService.Migrations.cs`, `OrcamentoDatabaseService.cs`, `AgendamentoDatabaseService.cs`, `FinanceiroDatabaseService.cs`, `RelatorioDatabaseService.cs` |
| **PROBLEMA** | 50+ colunas monetárias usando `REAL` no SQLite. IEEE 754 double não tem precisão exata para valores decimais. |
| **EVIDÊNCIA** | grep encontra `REAL` em PrecoCompra, PrecoVenda, Total, Desconto, ValorCompras, Subtotal, etc. `MoneyCents` struct existe mas documenta "DB migration to INTEGER cents is NOT DONE". |
| **RISCO** | Erros de arredondamento em operações financeiras: parcelas, descontos, somas. |
| **COMO REPRODUZIR** | `MoneyCents.FromDecimal(0.10m) + MoneyCents.FromDecimal(0.20m)` = 30 centavos ✅, mas `0.1 + 0.2` em double = 0.30000000000000004 ≠ 0.3. |
| **CAUSA RAIZ** | SQLite não tem tipo DECIMAL nativo. Design original usou REAL. |
| **CORREÇÃO** | PLANEJADO — migração para INTEGER centavos requer plano detalhado de dados + rollback + validação. `MoneyCents` struct já existe como fundação. |
| **TESTE NECESSÁRIO** | `MoneyCentsTests` + `MoneyCentsExpandedTests` — 25+ testes de arredondamento, desconto, parcelamento, rateio. |
| **STATUS** | ⚠️ PLANIFICADO — MoneyCents struct e testes prontos. Migração de DB pendente. |

---

## P0.10 — CI Quality Gates Não Bloqueiam

| Campo | Detalhe |
|---|---|
| **ID** | P0.10 |
| **PRIORIDADE** | P0 — CRÍTICO |
| **CATEGORIA** | CI/CD |
| **ARQUIVO** | `.github/workflows/code-quality.yml`, `.github/workflows/performance-security.yml` |
| **LINHA** | code-quality:63,98; performance-security:120 |
| **PROBLEMA** | StyleCop, FxCop e CodeComplexity com `continue-on-error: true`. Quality Gates job usa `if: always()` e apenas gera summary — nunca falha o pipeline. |
| **EVIDÊNCIA** | `continue-on-error: true` em 3 jobs. Quality Gates job (lines 174-188) é apenas echo. |
| **RISCO** | Falsa sensação de qualidade — PR merge mesmo com vulnerabilidades. |
| **CORREÇÃO** | RECOMENDADO — criar branch protection rule + `ci-gate.yml` que consolide build+test+security como gates reais. |
| **STATUS** | ⚠️ DOCUMENTADO — CI principal (`ci.yml` build+test) É gate real. Workflows secundários são informativos. |
