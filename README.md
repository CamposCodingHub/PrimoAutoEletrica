# PRIMOX Workshop (PrimoAutoEletrica)

Sistema de gestão para oficina / autoelétrica — **WPF desktop** (`net6.0-windows`).

**Status:** Release Candidate técnico auditado (Fase 14)  
**Versão:** `1.0.0-rc.1`  
**Relatório:** [Docs/qa/FASE14-RELEASE-CANDIDATE-REPORT.md](Docs/qa/FASE14-RELEASE-CANDIDATE-REPORT.md)

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

Scripts locais (podem estar fora do Git em WIP):

- `Scripts/Deploy-ToInstalledApp.ps1` — build + copia para pasta do atalho “Primo*” na área de trabalho  
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
| InformationalVersion | `1.0.0-rc.1` |
| AssemblyVersion | `1.0.0.0` |
| Nota | `1.3.0` em docs antigos era legado de status; RC comercial recomendado: **1.0.0-rc.1** até confirmação de GA |

---

## Documentação

- [PROJECT_STATUS.md](PROJECT_STATUS.md) — fases PRIMOX e evidências  
- [Docs/qa/FASE14-RELEASE-CANDIDATE-REPORT.md](Docs/qa/FASE14-RELEASE-CANDIDATE-REPORT.md) — relatório final Fase 14  
- [Docs/qa/primox-coverage-matrix-fase14.md](Docs/qa/primox-coverage-matrix-fase14.md) — matriz de cobertura  

---

## Licença / uso

Uso interno / comercial do produto Primo Auto Elétrica. Consulte o proprietário do repositório para licenciamento.
