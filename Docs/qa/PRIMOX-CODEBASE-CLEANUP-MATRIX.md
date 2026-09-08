# PRIMOX — Codebase Cleanup Matrix 1.0

**Audit:** 2026-09-08 · **Nenhuma remoção executada** nesta auditoria (conservador).  
**Tag:** `v1.0.0`→`a4ad6fe` intacta · WIP Help/Deploy **PRESERVADO**

| Path | Tipo | Categoria | Referências | Usado por | Risco | Decisão | Evidência |
|------|------|-----------|-------------|-----------|-------|---------|-----------|
| Helpers/AccessibilityChromeHealer.cs | Helper | CURRENT | Exhaustive/UI | QA a11y | Baixo | KEEP | P15E-015 |
| Services/NFeService.cs | Service | CURRENT | UI/smoke | Import NF-e | — | KEEP | REAL+TESTADO |
| Services/Fiscal/NFeEmissaoService.cs | 0-byte | FUTURE PLACEHOLDER | Docs gaps | Marker emissão | Médio se apagar | KEEP — FUTURE | Documentado NÃO IMPLEMENTADO |
| Services/NotificationService.cs | Service | PLACEHOLDER | DI | Sem UI caller | Médio (fake success) | KEEP — fix future | Delay+true |
| Services/FilialService.cs | Service | SCAFFOLD | Login UI | Seleção filial | Alto se “vender” | KEEP — FUTURE | Hardcoded |
| Services/ExternalBackupService.cs | Service | ORPHAN | 0 | — | Baixo | DEPRECATE | Zero refs |
| Services/CpfFieldEncryptionService.cs | 0-byte | EMPTY | 0 | — | Baixo | REMOVE — SAFE* | Sem tipos |
| Services/DemandForecastService.cs | 0-byte | EMPTY | 0 | — | Baixo | REMOVE — SAFE* | Sem tipos |
| Services/IntegracoesConfigService.cs | 0-byte | EMPTY | 0 | — | Baixo | REMOVE — SAFE* | Sem tipos |
| Services/LgpdPortabilityService.cs | 0-byte | EMPTY | 0 | — | Baixo | REMOVE — SAFE* | Sem tipos |
| Services/MarketplaceFornecedorService.cs | 0-byte | EMPTY | 0 | — | Baixo | REMOVE — SAFE* | Sem tipos |
| Services/TelemetryService.cs | 0-byte | EMPTY | 0 | — | Baixo | REMOVE — SAFE* | Sem tipos |
| ViewModels/FuncionariosViewModel.cs | VM | ORPHAN UI | DI+tests | FuncionariosControl code-behind | Médio | KEEP — LEGACY / DEPRECATE | Product truth RETAIN |
| ViewModels/RelatoriosModernoViewModel.cs | VM | ORPHAN UI | DI+tests | RelatoriosViewModel usado | Médio | KEEP — LEGACY / DEPRECATE | — |
| ViewModels/EstoqueViewModel.cs | VM | ORPHAN UI | DI+tests | EstoqueControl code-behind | Médio | KEEP — LEGACY / DEPRECATE | — |
| ViewModels/PrinterManagementViewModel.cs | VM | ORPHAN UI | DI | — | Baixo | KEEP — LEGACY / DEPRECATE | — |
| Scripts/Deploy-ToInstalledApp.ps1 | Script | WIP | bat | Deploy local | — | KEEP — WIP | git untracked |
| Scripts/Atualizar-PrimoAuto.bat | Script | WIP | — | Deploy | — | KEEP — WIP | — |
| Scripts/Run-Keycloak.ps1 | Script | EMPTY | 0 | — | Baixo | REMOVE — SAFE* | 0 bytes |
| Scripts/Run-UiSmoke.ps1 | Script | QA | CI/dev | Smoke | — | KEEP — QA | — |
| UiSmokeTestService*.cs | QA | QA ENGINE | Exhaustive | Regressão | — | KEEP — QA | Patrimônio |
| Installer/PrimoAutoEletrica.iss | Installer | CURRENT | Build-PrimoX | Comercial | — | KEEP | — |
| .github/workflows/* | CI | CURRENT | GitHub | CI/CD | — | KEEP | 5 workflows root |
| PrimoAutoEletrica/.github/workflows/ci.yml | CI | LEGACY? | Nested | UNKNOWN | Baixo | UNKNOWN | Possível duplicata |
| PrimoAutoEletrica.Maui/*.csproj | Project | EMPTY/SCAFFOLD | — | — | Baixo | UNKNOWN | Empty csproj reportado |
| PrimoAutoEletrica/Api/*.csproj | Project | STUB | Nested | — | Baixo | UNKNOWN | Preferir Api root |
| Tools/LocalSyncSimulator | Tool | QA/DEV | — | LAN sync sim | — | KEEP — QA | — |
| Tools/DbConfigurator | Tool | DEV | — | DB config | — | KEEP | — |
| System.Security.Cryptography.Xml pkg | NuGet | ORPHAN | 0 usages | — | Baixo | DEPRECATE package* | Sem SignedXml |
| HelpControl WIP | UI | WIP | — | Help | — | KEEP — WIP | Não commit nesta audit |
| Calendar themes | XAML | CURRENT | DatePicker | UI | — | KEEP | Não tocar CalendarItem |

\* REMOVE — SAFE = candidato para **aprovação humana**; **não removido** nesta execução.

## Remoções executadas

**Nenhuma.**
