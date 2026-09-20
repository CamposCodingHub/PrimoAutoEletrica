# P0 Build / SLN / Unit — 2026-09-20

Branch tip: `e9cd965`  
main: **nao tocado**

## SLN
- Projetos no .sln: **8**
- Referencias .csproj inexistentes: **0**

## Build
```
dotnet build PrimoAutoEletrica.sln -c Release
```
- Erros: **0**
- Avisos: ~84 (CA1416/CS8632/xUnit1031 — nao bloqueantes)
- Log: `P0_BUILD_RELEASE.txt` (parcial no Tee)

## Suite oficial
```
dotnet test Tests\PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj -c Release
```
- Pass: **264**
- Fail: **0**
- Skipped: **0**
- Log: `P0_UNIT_FULL.txt`

## CI (codigo local — nao executado no GitHub nesta sessao)
| Workflow | continue-on-error | Classificacao |
|----------|-------------------|---------------|
| GATE jobs em ci.yml | (verificar por job) | GATE |
| INFO StyleCop em code-quality.yml | **true** | INFO OK soft-fail |
| INFO FxCop (se presente) | true | INFO |

**CI GitHub Actions desta tip: NAO EXECUTADO nesta sessao** (sem claim verde remoto).

## Deploy desktop
- `Deploy-ToInstalledApp.ps1` OK ~10:05 BRT
