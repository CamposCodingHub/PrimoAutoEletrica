# PRIMOX — Codebase Cleanup Matrix 1.0 (atualizada — Sanitization 1.0)

**Audit base:** 2026-09-08  
**Sanitization:** 2026-09-08 — remoções SAFE executadas + placeholders neutralizados  
**Tag:** `v1.0.0`→`a4ad6fe` intacta · Deploy WIP **PRESERVADO**

| Path | Tipo | Categoria | Referências | Usado por | Risco | Decisão | Evidência | Commit remoção |
|------|------|-----------|-------------|-----------|-------|---------|-----------|----------------|
| Helpers/AccessibilityChromeHealer.cs | Helper | CURRENT | Exhaustive/UI | QA a11y | Baixo | KEEP | P15E-015 | — |
| Services/NFeService.cs | Service | CURRENT | UI/smoke | Import NF-e | — | KEEP | REAL+TESTADO | — |
| Services/Fiscal/NFeEmissaoService.cs | 0-byte | FUTURE PLACEHOLDER | Docs | Marker emissão | Médio se apagar | KEEP — FUTURE | NÃO IMPLEMENTADO | — |
| Services/NotificationService.cs | Service | HONEST | DI | Sem UI caller | — | KEEP | Retorna false + NaoConfigurado | — |
| Services/FilialService.cs | Service | HONEST SCAFFOLD | Login | Unidade local | — | KEEP | Sem SP/RJ fake; sem diálogo multi | — |
| Services/ExternalBackupService.cs | Service | ORPHAN | 0 | — | Baixo | DEPRECATE | Zero refs | — |
| Services/CpfFieldEncryptionService.cs | 0-byte | EMPTY | 0 | — | Baixo | **REMOVED** | Sem tipos | chore(cleanup) |
| Services/DemandForecastService.cs | 0-byte | EMPTY | 0 | — | Baixo | **REMOVED** | Sem tipos | chore(cleanup) |
| Services/IntegracoesConfigService.cs | 0-byte | EMPTY | 0 | — | Baixo | **REMOVED** | Sem tipos | chore(cleanup) |
| Services/LgpdPortabilityService.cs | 0-byte | EMPTY | 0 | — | Baixo | **REMOVED** | Sem tipos | chore(cleanup) |
| Services/MarketplaceFornecedorService.cs | 0-byte | EMPTY | 0 | — | Baixo | **REMOVED** | Sem tipos | chore(cleanup) |
| Services/TelemetryService.cs | 0-byte | EMPTY | 0 | — | Baixo | **REMOVED** | Sem tipos | chore(cleanup) |
| ViewModels/FuncionariosViewModel.cs | VM | ORPHAN UI | DI+tests | code-behind | Médio | KEEP — LEGACY | — | — |
| ViewModels/RelatoriosModernoViewModel.cs | VM | ORPHAN UI | DI+tests | — | Médio | KEEP — LEGACY | — | — |
| ViewModels/EstoqueViewModel.cs | VM | ORPHAN UI | DI+tests | — | Médio | KEEP — LEGACY | — | — |
| ViewModels/PrinterManagementViewModel.cs | VM | ORPHAN UI | DI | — | Baixo | KEEP — LEGACY | — | — |
| Scripts/Deploy-ToInstalledApp.ps1 | Script | WIP | bat | Deploy | — | KEEP — WIP | untracked | — |
| Scripts/Atualizar-PrimoAuto.bat | Script | WIP | — | Deploy | — | KEEP — WIP | — | — |
| Scripts/Run-Keycloak.ps1 | Script | EMPTY | 0 | — | Baixo | **REMOVED** | 0 bytes | chore(cleanup) |
| Scripts/Run-UiSmoke.ps1 | Script | QA | CI/dev | Smoke | — | KEEP — QA | — | — |
| UiSmokeTestService*.cs | QA | QA ENGINE | Exhaustive | Regressão | — | KEEP — QA | +HelpCenter check | — |
| Installer/PrimoAutoEletrica.iss | Installer | CURRENT | Build | Comercial | — | KEEP | — | — |
| .github/workflows/* | CI | CURRENT | GitHub | CI/CD | — | KEEP | — | — |
| PrimoAutoEletrica/.github/workflows/ci.yml | CI | LEGACY? | Nested | UNKNOWN | Baixo | UNKNOWN | REQUIRES FUTURE DECISION | — |
| PrimoAutoEletrica.Maui/*.csproj | Project | EMPTY/SCAFFOLD | — | — | Baixo | UNKNOWN | REQUIRES FUTURE DECISION | — |
| PrimoAutoEletrica/Api/*.csproj | Project | STUB | Nested | — | Baixo | UNKNOWN | REQUIRES FUTURE DECISION | — |
| Tools/LocalSyncSimulator | Tool | QA/DEV | — | LAN | — | KEEP — QA | — | — |
| Tools/DbConfigurator | Tool | DEV | — | DB | — | KEEP | — | — |
| System.Security.Cryptography.Xml pkg | NuGet | ORPHAN | 0 | — | Baixo | DEPRECATE package* | Sem SignedXml | — |
| HelpControl + HelpTopicsCatalog | UI | CURRENT | Menu/F1 | Ajuda | — | KEEP | Help Center 1.0 | — |
| Calendar themes | XAML | CURRENT | DatePicker | UI | — | KEEP | — | — |

## Remoções executadas (Sanitization 1.0)

1. `PrimoAutoEletrica/Services/CpfFieldEncryptionService.cs`
2. `PrimoAutoEletrica/Services/DemandForecastService.cs`
3. `PrimoAutoEletrica/Services/IntegracoesConfigService.cs`
4. `PrimoAutoEletrica/Services/LgpdPortabilityService.cs`
5. `PrimoAutoEletrica/Services/MarketplaceFornecedorService.cs`
6. `PrimoAutoEletrica/Services/TelemetryService.cs`
7. `Scripts/Run-Keycloak.ps1`

**Não removido:** `NFeEmissaoService.cs` (KEEP — FUTURE), ExternalBackupService (DEPRECATE), VMs órfãs, UNKNOWN projects, Cryptography.Xml package (só deprecar).
