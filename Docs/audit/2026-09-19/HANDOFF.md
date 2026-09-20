# HANDOFF — Phase 0 (executor blocked on machineId)

## Blocker
- Shell/Read `machineId=de411c5d-4243-4085-bf33-8c3d241d7f2f` **ignored** → Linux box only
- No `gh` auth / no Windows deploy from this subagent
- Same pattern as prior estoque-grid handoff

## Package
`/workspace/phase0-audit-2026-09-19.tgz`

## Parent steps
```
CopyFromBox box_path=/workspace/phase0-audit-2026-09-19.tgz
  computer_path=C:\Users\campo\Downloads\phase0-audit-2026-09-19.tgz
  machineId=de411c5d-4243-4085-bf33-8c3d241d7f2f
```

Windows:
```powershell
cd C:\Users\campo\Downloads
tar -xzf phase0-audit-2026-09-19.tgz
cd phase0-audit-2026-09-19
# Review Apply-Phase0.ps1 then:
powershell -File .\Apply-Phase0.ps1
cd C:\Projetos\PrimoAutoEletrica
git checkout audit/product-discovery-2026-09
dotnet build PrimoAutoEletrica\PrimoAutoEletrica.csproj -c Release
dotnet test PrimoAutoEletrica.Tests\PrimoAutoEletrica.Tests.csproj -c Release --filter "FullyQualifiedName~PermissionServiceFailClosedTests|FullyQualifiedName~HistoricoClienteFinancialIsolationTests|FullyQualifiedName~ApiAuthenticationGateTests"
# If desktop OK:
powershell -File Scripts\Deploy-ToInstalledApp.ps1
gh auth setup-git
git add -A
git commit -m "fix(security): Phase 0 fail-closed permissions, historico by Id, CI gates, audit docs"
git push
```

## Do NOT tell owner
- That Phases 2–6 are done
- That all modals were tested
- That API/SaaS/fiscal/sync are ready
