# P0 Security validation — 2026-09-20 (audit)

Branch: `audit/product-discovery-2026-09`  
Tip base: `e0716ba` (+ commits deste pacote)

## Comando (sem mascarar)

```
dotnet test Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj -c Release --filter "FullyQualifiedName~ApiJwt|FullyQualifiedName~ApiAuthenticationGate|FullyQualifiedName~PermissionServiceFailClosed|FullyQualifiedName~LicenseScaffold|FullyQualifiedName~ExternalBackup"
```

Log bruto: `Docs/audit/2026-09-20/P0_SECURITY_TEST_RUN.txt`

## Resultado (1a rodada, pre-fix Detail)

| Metrica | Valor |
|---------|-------|
| Total | 27 |
| Pass | 27 |
| Fail | 0 |
| Skipped | 0 |

### JWT HTTP (Development)
| Caso | Resultado |
|------|-----------|
| Health sem token | 200 PASS |
| Orcamentos/Estoque/Financeiro sem token | 401 PASS |
| Token invalido | 401 PASS |
| Token expirado | 401 PASS |
| Token sem perm ORCAMENTO | 403 PASS |
| Token com perm / client dev | nao 401/403 PASS |
| SafeProblem body sem stack/SQL | PASS |

### JWT Production bootstrap
| Caso | Resultado |
|------|-----------|
| SigningKey CHANGE_ME | host nao sobe PASS |
| SigningKey DEV_ONLY_ | host nao sobe PASS |
| Chave forte: health 200 + orcamentos 401 | PASS |

### RBAC fail-closed
| Caso | Resultado |
|------|-----------|
| Lookup lanca → Unavailable | PASS |
| Garantir Unavailable → false | PASS |
| Persistido negado sem fallback | PASS |
| Critico sem linha → Denied | PASS |
| Admin bypass | PASS |

### Correcao adicional (neste commit)
- `PermissionCheckResult.Unavailable` Detail deixou de incluir `ex.Message` (codigo estavel `lookup_exception:TypeName`).
- Log interno continua com exception completa via `LogError(..., ex)`.

## Riscos restantes
- Token "com permissao" ainda pode retornar 500 se DB de teste da WebApplicationFactory nao estiver provisionada — aceito desde que nunca 401/403.
- UI desktop ValidarPermissao MessageBox: nao revalidada visualmente nesta etapa.
- main ainda sem JWT.

## Rodada 2 (pos-fix Detail + Unavailable_Detail test)
| Metrica | Valor |
|---------|-------|
| Total | **28** |
| Pass | **28** |
| Fail | **0** |
| Skipped | **0** |

Exit code: 0 (sem `|| true`). Log: `P0_SECURITY_TEST_RUN2.txt`
