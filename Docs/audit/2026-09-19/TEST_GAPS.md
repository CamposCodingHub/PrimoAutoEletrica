# H — TEST_GAPS

## Lacunas

- SecurityTests legados com validadores inventados (não batem no produto)
- ApiIntegrationTests assumem endpoints públicos (frágil)
- Poucos testes de PermissionService reais (agora adicionados)
- Sem teste e2e Windows de fail-closed em DB corrompido
- Gate JWT de produção ainda Skip até implementação

## Adicionados neste PR

- `PermissionServiceFailClosedTests`
- `HistoricoClienteFinancialIsolationTests`
- `ApiAuthenticationGateTests` (documenta gap + Skip gate)

## Comando focado (Windows)

```powershell
dotnet test PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj -c Release --filter "FullyQualifiedName~PermissionServiceFailClosedTests|FullyQualifiedName~HistoricoClienteFinancialIsolationTests|FullyQualifiedName~ApiAuthenticationGateTests"
```
