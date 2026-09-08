# PRIMOX Workshop (PrimoAutoEletrica)

Sistema de gestão para oficina / autoelétrica — **WPF desktop** (`net6.0-windows`).

**Status:** **PRIMOX Workshop 1.0.0** (Release Gate GO · Packaging 15B GO)  
**Versão:** `1.0.0` (tag `v1.0.0` → `a4ad6fe`)  
**Relatórios:** [Release Gate](Docs/qa/PRIMOX-RELEASE-GATE-1.0.0.md) · [Packaging 15B](Docs/qa/PRIMOX-COMMERCIAL-PACKAGING-REPORT.md) · [Installation E2E 15C](Docs/qa/PRIMOX-INSTALLATION-E2E-REPORT.md) · [Instalação](INSTALLATION.md)

> O site/marketing PRIMOX **não** faz parte deste repositório nesta fase.

---

## Visão geral

Aplicação desktop para operação diária de oficina:

- Clientes e veículos  
- Ordens de serviço e dossiê técnico  
- Orçamentos  
- Agenda / check-in  
- Estoque e fornecedores  
- Financeiro  
- PDV  
- Relatórios (PDF/Excel)  
- Kanban de oficina  
- Importação NF-e (sem emissão fiscal real automatizada)  
- Design System PRIMOX (Light/Dark)

---

## Requisitos

- Windows 10/11  
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (runtime roll-forward para SDKs mais novos pode ser necessário: `$env:DOTNET_ROLL_FORWARD='LatestMajor'`)  
- Visual Studio 2022 ou Cursor/VS Code  

---

## Build

```powershell
$env:DOTNET_ROLL_FORWARD='LatestMajor'
dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Debug
```

Release:

```powershell
dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Release
```

**Critério:** 0 erros.

---

## Execução

```powershell
$env:DOTNET_ROLL_FORWARD='LatestMajor'
dotnet run --project PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Debug
```

Na primeira execução o SQLite é criado automaticamente (AppData / configuração da estação).

---

## Deploy / instalação

**Canal comercial oficial (Inno Setup):**

```powershell
# Pré-requisito: Inno Setup 6 (ISCC)
winget install JRSoftware.InnoSetup

$env:DOTNET_ROLL_FORWARD='LatestMajor'
.\Scripts\Build-PrimoXCommercialRelease.ps1 -Version 1.0.0
```

Saídas em `artifacts/` (não versionado): Setup `PRIMOX-Workshop-Setup-1.0.0.exe` + SHA256.  
Validação E2E do pacote instalado:

```powershell
.\Scripts\Test-InstalledPackageE2E.ps1 -Version 1.0.0 -SkipQaEngine
```

Detalhes: [Installer/README_INSTALADOR.md](Installer/README_INSTALADOR.md) · [INSTALLATION.md](INSTALLATION.md).

**Desenvolvimento (não oficial):**

- `Scripts/Deploy-ToInstalledApp.ps1` — build + copia para LocalAppData\App  
- `Scripts/Atualizar-PrimoAuto.bat` — atalho para o deploy  

Ícone da aplicação: `PrimoAutoEletrica/icon.ico` (`ApplicationIcon` no csproj).

---

## QA / testes automatizados

Filtros principais (banco **isolado** `AutomatedTests/ui-smoke-test-*` — nunca produção):

```powershell
$env:DOTNET_ROLL_FORWARD='LatestMajor'
Scripts\Run-UiSmoke.ps1 -Framework net6.0-windows -SmokeFilter DeepQa
Scripts\Run-UiSmoke.ps1 -Framework net6.0-windows -SmokeFilter QaEngine
```

| Suite | Escopo |
| ----- | ------ |
| DeepQa | Inventário permanente, LongRun, a11y, capturas, botões, Funcionários |
| QaEngine | CRUD/persistência, finalização Fase 14, LongRun 5, matriz de cobertura |

Evidências recentes: `Docs/qa/` e `PrimoAutoEletrica/bin/Debug/net6.0-windows/Logs/qa-engine/`.

---

## Arquitetura (resumo)

```
PrimoAutoEletrica/                 # WPF principal
  Themes/                         # Design System PRIMOX
  UserControls/                   # Módulos navegáveis
  Views/                          # Janelas / dialogs
  Services/                       # Domínio + UiSmoke / PrimoxQaEngine
  Repositories/                   # Dapper + SQLite/SQL Server
  ViewModels/
Scripts/                          # Smoke + deploy
Docs/qa/                          # Relatórios de cobertura / RC
PROJECT_STATUS.md                 # Status detalhado por fase
```

- UI: WPF + Design System PRIMOX  
- Dados: SQLite (padrão) / SQL Server (configurável)  
- DI: Microsoft.Extensions.DependencyInjection  
- Navegação: `NavigationService` + shell  

**Regras desta fase de fechamento:** não alterar schema; não mudar regras de negócio sem necessidade; não redesign.

---

## Known issues / limitações

1. Header nativo do Calendar em Dark — contraste limitado (CalendarItem custom bloqueado)  
2. Cores hardcoded em print/chips/converters — auditadas, sem mass-replace  
3. Emissão NF-e real — **não testável** em smoke (requer integração fiscal)  
4. Indicador/ícone de “fase” dedicado — **não localizado** no código  
5. `FuncionariosViewModel` — candidato a órfão, **retido** (DI + testes)  
6. Cobertura 100% de execução real de todos os botões — **não reivindicada**  

---

## Versão

| Campo | Valor |
| ----- | ----- |
| InformationalVersion | `1.0.0` |
| AssemblyVersion | `1.0.0.0` |
| Nota | Promovido de `1.0.0-rc.1` (HEAD `b184501`) após Release Gate GO. Tag: `v1.0.0`. |

---

## Documentação

- [PROJECT_STATUS.md](PROJECT_STATUS.md) — fases PRIMOX e evidências  
- [Docs/qa/FASE14-RELEASE-CANDIDATE-REPORT.md](Docs/qa/FASE14-RELEASE-CANDIDATE-REPORT.md) — relatório final Fase 14  
- [Docs/qa/primox-coverage-matrix-fase14.md](Docs/qa/primox-coverage-matrix-fase14.md) — matriz de cobertura  

---

## Licença / uso

Uso interno / comercial do produto Primo Auto Elétrica. Consulte o proprietário do repositório para licenciamento.
