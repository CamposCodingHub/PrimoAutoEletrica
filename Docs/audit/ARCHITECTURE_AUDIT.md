# PRIMOX Workshop — ARCHITECTURE AUDIT

---

## 1. Arquitetura Atual

```
PrimoAutoEletrica.sln
├── PrimoAutoEletrica (WPF Desktop) — net10.0-windows
│   ├── Services/ (155 arquivos, ~3.5MB código)
│   ├── Views/ (101 arquivos)
│   ├── ViewModels/
│   ├── Models/
│   ├── Repositories/
│   ├── UserControls/
│   ├── Helpers/
│   ├── Converters/
│   ├── Themes/
│   └── Resources/
├── PrimoAutoEletrica.Api — net10.0-windows (API minimalista)
├── PrimoAutoEletrica.Tests — net10.0-windows (root, 8 arquivos)
├── Tests/PrimoAutoEletrica.Tests — net10.0-windows (28 arquivos, testes reais)
├── PrimoAutoEletrica.UiTests — net10.0 (FlaUI)
├── Tools/LocalSyncSimulator
└── Tools/DbConfigurator
```

## 2. Problemas Estruturais Identificados

### 2.1 God Object: DatabaseService
- DatabaseService.cs: 50KB
- DatabaseService.Migrations.cs: 72KB
- DatabaseService.OrdensServico.cs: 98KB
- DatabaseService.Produtos.cs: 40KB
- DatabaseService.AccessControl.cs: 60KB
- DatabaseService.Clientes.cs: 23KB
- **Total: ~340KB em um único serviço lógico (via partials)**

### 2.2 Services Monolíticos
- `FinanceiroDatabaseService.cs`: 114KB
- `RelatorioDatabaseService.cs`: 124KB
- `OrcamentoDatabaseService.cs`: 50KB
- `UiSmokeTestService.*.cs`: ~500KB+ (40+ partials)

### 2.3 Acoplamento Estático
- `App.Database`, `App.Repositories`, `App.Session`, `App.Audit` acessados globalmente
- `new DatabaseService()`, `new OrcamentoDatabaseService()` instanciados in-place
- `DependencyInjection/ServiceExtensions.cs` existe mas uso inconsistente

### 2.4 API Compartilha Tipos com Desktop
- `PrimoAutoEletrica.Api.csproj` referencia `PrimoAutoEletrica.csproj` diretamente
- API usa `net10.0-windows` (não portável) por causa desta referência
- Tipos de domínio (Orcamento, Produto, etc.) vivem no projeto WPF

## 3. Arquitetura Futura Recomendada

```
PRIMOX.Domain          — entidades, value objects, interfaces
PRIMOX.Application     — use cases, DTOs, interfaces de serviço
PRIMOX.Infrastructure  — repositórios, DB, integração externa
PRIMOX.Persistence     — SQLite/PostgreSQL, migrations
PRIMOX.Contracts       — DTOs API, eventos
PRIMOX.Api             — net10.0 (sem -windows)
PRIMOX.Desktop         — net10.0-windows (WPF)
PRIMOX.Mobile          — MAUI (futuro)
```

## 4. Migração Recomendada (Gradual)

1. **Extrair PRIMOX.Domain**: Models + interfaces (sem dependência de DB)
2. **Extrair PRIMOX.Contracts**: DTOs para API
3. **API portável**: Remover `-windows` do API.csproj
4. **Repositórios por domínio**: ClienteRepository, OrdemServicoRepository (já existem parcialmente)
5. **DI consistente**: Eliminar `new XxxService()` em Views
6. **Não reescrever tudo**: Migrar por bounded context

## 5. Padrões Positivos Encontrados

- `IClienteRepository`, `IOrdemServicoRepository`, `IProdutoRepository` — interfaces existem ✅
- `IPrimox360Service` — abstração com DI ✅
- `IFiscalProvider` — padrão provider ✅
- `CommunityToolkit.Mvvm` — MVVM toolkit ✅
- `Dapper` — micro ORM eficiente ✅
- `PermissionCheckResult` — value object correto ✅
- `MoneyCents` — value object monetário ✅
