# MATRIZ MANDATO A–R — 2026-09-20 (BRT)

Branch: `audit/product-discovery-2026-09` tip **95aaf49**  
Desktop deploy: **09:37 BRT** → `%LocalAppData%\PrimoAutoEletrica\App`  
Regra: FEITO só com evidência. Sem PASS inventado.

| ID | Tema | Status | Evidência |
|----|------|--------|-----------|
| **A** | P0.01 SLN restore / Release build | **FEITO** | PR #3 `84afaa1`; audit cherry-pick `94a1f09`; `dotnet build -c Release` 0 erros |
| **B** | P0.02 API JWT real + 401/403 | **FEITO (audit)** / **NÃO em main** | `AddJwtBearer` + `ApiJwtHttpTests` |
| **C** | P0.03 RBAC fail-closed | **FEITO** | `PermissionService` + `PermissionServiceFailClosedTests` 8 PASS; commit `95aaf49` |
| **D** | P0.04 Integridade por IDs (finance/360) | **FEITO (escopo finance/360)** | Historico sem nome; Primox360 ID joins; doc `P0.04_ID_INTEGRITY.md` |
| **E** | P0.05 Licença comercial | **NÃO FEITO (produto)** / **FEITO (honesty)** | `IsCommercialScaffoldOnly=true`; sem RSA/server |
| **F** | P0.06 Contratos API | **FEITO (audit)** | `P0.06_API_CONTRACTS.md` + testes HTTP |
| **G** | P0.07 CI GATE vs INFO | **FEITO (classificação)** | GATE `continue-on-error:false`; INFO soft; `P0.07_CI_GATES.md` |
| **H** | P0.08 Suite oficial | **FEITO (docs)** | Oficial = `Tests/PrimoAutoEletrica.Tests`; raiz legado |
| **I** | P0.09 Consolidar suites / remanescentes | **PARCIAL** | Oficial definido; pasta raiz `PrimoAutoEletrica.Tests` ainda existe fora da SLN |
| **J** | P0.10 Money INTEGER cents | **NÃO FEITO** | Mapa em `P0.10_MONEY_REAL_MAP.md`; SQLite REAL permanece |
| **K** | P0.11 Backup “encryption” fake | **FEITO (honesty)** | throw se password; UI disabled |
| **L** | P0.12 Backup cloud fake | **FEITO (honesty)** | Sync cloud disabled; `SyncWithCloud=false` |
| **M** | Unit / security tests verdes | **FEITO (amostra)** | Security filter **56 PASS**; unit suite histórica ~232+ |
| **N** | UI smoke / ExhaustiveUi | **FEITO (sessão anterior)** | ExhaustiveUi 2424 PASS / 0 FAIL (~53.9% discovered); **não** = 100% modais |
| **O** | Deploy desktop PRIMOX Workshop | **FEITO** | Deploy 2026-09-20 ~09:37 BRT |
| **P** | Merge audit → main / net10 em main | **NÃO FEITO** | main ainda caminho legado; PRs #2/#3 separados |
| **Q** | Phases 1+ produto (portal, PIX, WhatsApp Cloud, frota, mobile) | **NÃO FEITO / parcial Phase1 local** | Phase1 DVI/360/lembretes locais já no branch; SaaS/cloud **NÃO** |
| **R** | Pronto SaaS / production API pública | **NÃO FEITO** | License scaffold; money REAL; main sem JWT; multi-filial false |

## Resumo executivo

- **P0 hardening no audit:** build, JWT, RBAC, IDs financeiros, honesty backup/license, CI classification, suite oficial documentada — **avançado com evidência**.
- **Ainda bloqueia “comercial cloud”:** money cents, license server, merge/net10 main, API em main, cobertura Exhaustive ≠ 100%, Phases 2–6 SaaS.
- **main:** NÃO assumir JWT/RBAC/net10 do audit até merge explícito.

## Comandos de revalidação

```
dotnet build PrimoAutoEletrica.sln -c Release
dotnet test Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj -c Release --filter "FullyQualifiedName~Security|FullyQualifiedName~ApiJwt|FullyQualifiedName~Permission"
Scripts\Deploy-ToInstalledApp.ps1
```
