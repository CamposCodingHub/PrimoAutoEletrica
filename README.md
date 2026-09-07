# PrimoAutoEletrica

Sistema de gestão completa para autoelétricas, desenvolvido em .NET 9.0 com WPF, incluindo gestão de orçamentos, ordens de serviço, estoque, financeiro e muito mais.

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Funcionalidades](#funcionalidades)
- [Tecnologias](#tecnologias)
- [Pré-requisitos](#pré-requisitos)
- [Instalação](#instalação)
- [Build](#build)
- [Execução](#execução)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Desenvolvimento](#desenvolvimento)
- [Testes](#testes)
- [Contribuição](#contribuição)
- [Licença](#licença)

## 🎯 Visão Geral

O PrimoAutoEletrica é um sistema de gestão empresarial projetado especificamente para autoelétricas, oferecendo ferramentas completas para:

- Gestão de clientes e veículos
- Criação e acompanhamento de orçamentos
- Gestão de ordens de serviço com kanban
- Controle de estoque de peças
- Gestão financeira completa
- Agendamento de serviços
- Geração de relatórios
- Integração com sistemas contábeis

## ✨ Funcionalidades

### Gestão de Clientes
- Cadastro completo de clientes
- Histórico de veículos por cliente
- Controle de agendamentos
- Integração com notificações SMS/WhatsApp

### Orçamentos
- Criação de orçamentos detalhados
- Cálculo automático de margens
- Aprovação e conversão em ordens de serviço
- Geração de PDF

### Ordens de Serviço
- Dashboard kanban visual
- Acompanhamento em tempo real
- Integração com estoque
- Registro de diagnóstico e soluções

### Estoque
- Controle de peças e acessórios
- Classificação ABC
- Alertas de estoque baixo
- Gestão de fornecedores

### Financeiro
- Contas a pagar e receber
- Fluxo de caixa
- Exportação para sistemas contábeis (CSV, OFX, SPED)
- Relatórios financeiros

### Agendamentos
- Calendário de serviços
- Lembretes automáticos
- Alocação de mecânicos
- Integração com notificações

## 🛠 Tecnologias

- **Framework**: .NET 9.0
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Banco de Dados**: SQLite (com suporte para SQL Server)
- **ORM**: Dapper
- **Injeção de Dependência**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging
- **Testes**: xUnit, Moq, coverlet
- **CI/CD**: GitHub Actions
- **API REST**: ASP.NET Core Web API (em desenvolvimento)

## 📦 Pré-requisitos

- .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- Visual Studio 2022 ou VS Code
- Git

## 🚀 Instalação

1. Clone o repositório:
```bash
git clone https://github.com/seu-usuario/PrimoAutoEletrica.git
cd PrimoAutoEletrica
```

2. Restaure as dependências:
```bash
dotnet restore
```

3. Configure o banco de dados:
```bash
# O banco de dados SQLite será criado automaticamente na primeira execução
# Para usar SQL Server, configure a connection string em App.config
```

## 🔨 Build

### Build Completo
```bash
dotnet build
```

### Build em Release
```bash
dotnet build --configuration Release
```

### Build de Projetos Específicos
```bash
# Aplicação WPF
dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj

# API REST
dotnet build PrimoAutoEletrica.Api/PrimoAutoEletrica.Api.csproj

# Testes
dotnet build Tests/PrimoAutoEletrica.Tests/PrimoAutoEletrica.Tests.csproj
```

## ▶️ Execução

### Aplicação WPF
```bash
dotnet run --project PrimoAutoEletrica/PrimoAutoEletrica.csproj
```

### API REST
```bash
dotnet run --project PrimoAutoEletrica.Api/PrimoAutoEletrica.Api.csproj
```

A API estará disponível em: `http://localhost:5000`

### Swagger UI
Após iniciar a API, acesse: `http://localhost:5000/swagger`

## 📁 Estrutura do Projeto

```
PrimoAutoEletrica/
├── PrimoAutoEletrica/              # Aplicação WPF principal
│   ├── UserControls/               # Controles de usuário customizados
│   ├── ViewModels/                # ViewModels (MVVM)
│   ├── Services/                  # Serviços de negócio
│   ├── Models/                    # Modelos de dados
│   ├── Repositories/              # Acesso a dados
│   ├── Themes/                    # Estilos e temas
│   └── Resources/                 # Recursos estáticos
├── PrimoAutoEletrica.Api/         # API REST (ASP.NET Core)
│   ├── Controllers/              # Controladores API
│   └── Program.cs                # Configuração da API
├── Tests/                         # Projetos de teste
│   ├── PrimoAutoEletrica.Tests/   # Testes unitários
│   └── HomologacaoFisica/        # Testes de homologação
├── Docs/                          # Documentação
│   ├── ManualTecnico/            # Manual técnico
│   └── ManualUsuario/            # Manual do usuário
├── Tools/                         # Ferramentas auxiliares
└── .github/workflows/             # CI/CD
```

## 👨‍💻 Desenvolvimento

### Configuração do Ambiente

1. Instale o .NET 9.0 SDK
2. Clone o repositório
3. Restaure as dependências
4. Configure o ambiente de desenvolvimento

### Convenções de Código

- **MVVM**: Use ViewModels para lógica de apresentação
- **DI**: Use injeção de dependência via Microsoft.Extensions.DependencyInjection
- **Logging**: Use Microsoft.Extensions.Logging para logging estruturado
- **Testes**: Escreva testes unitários para novos recursos
- **Estilos**: Use os estilos definidos em `Themes/StandardTheme.xaml`

### Adicionando Novos Módulos

1. Crie o UserControl em `UserControls/`
2. Crie o ViewModel em `ViewModels/`
3. Registre o módulo no `NavigationService`:
```csharp
navigationService.RegisterModule("NomeModulo", typeof(MeuModuloControl));
```

### Configuração de Permissões

Adicione o código de permissão em `PermissionService.cs`:
```csharp
["NomeModulo"] = "MODULO_PERMISSAO_VER"
```

## 🧪 Testes

### Executar Todos os Testes
```bash
dotnet test
```

### Executar Testes com Cobertura
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Testes Unitários

Atualmente cobrindo:
- PermissionService
- NavigationService
- OrcamentoDatabaseService
- EstoqueViewModel
- FuncionariosViewModel

## 🔄 CI/CD

O projeto usa GitHub Actions para CI/CD:

- **Build**: .NET 9.0
- **Testes**: xUnit com cobertura de código
- **Publicação**: Artefatos de WPF e API
- **Trigger**: Push para main, Pull Requests

## 📝 Roadmap

### Curto Prazo (≤ 2 semanas)
- ✅ Refatorar NavigationService
- ✅ Centralizar verificação em PermissionService
- ✅ Adicionar testes unitários
- ✅ Limitar tamanho do cache
- ✅ Padronizar estilos com ResourceDictionary
- ⏳ Atualizar README

### Médio Prazo (1-3 meses)
- Migrar para MVVM completo
- Integrar Microsoft.Extensions.DependencyInjection
- Implementar Microsoft.Extensions.Logging
- Exportar relatórios PDF/Excel
- Implementar backup automático
- Suporte a múltiplos idiomas
- Testes de UI com White/Appium

### Longo Prazo (> 3 meses)
- Portar para .NET 6/8 + WPF Core
- Expor serviços via ASP.NET Core Web API
- Criar aplicativo móvel (MAUI)
- Integrar telemetria (Application Insights)
- Implementar ML para previsão de demanda
- Integração com fornecedores via API
- Design System com Material Design
- Auditar e Harden aplicação (OAuth2, 2FA)

## 🤝 Contribuição

Contribuições são bem-vindas! Por favor:

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona MinhaFeature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.

## 📞 Suporte

Para suporte e dúvidas:
- Abra uma issue no GitHub
- Consulte a documentação em `Docs/`
- Entre em contato com a equipe de desenvolvimento

## 🖼️ Screenshot da Tela Principal

```
┌─────────────────────────────────────────────────────────────┐
│  PrimoAutoEletrica                          ▤ □ ✕        │
├─────────────────────────────────────────────────────────────┤
│  Menu Lateral  │  Área Principal                            │
│               │                                             │
│  • Dashboard  │  [Conteúdo do módulo selecionado]          │
│  • Clientes   │                                             │
│  • Veículos   │                                             │
│  • Orçamentos │                                             │
│  • OS         │                                             │
│  • Estoque    │                                             │
│  • Financeiro │                                             │
│  • Relatórios │                                             │
│               │                                             │
└─────────────────────────────────────────────────────────────┘
```

---

Desenvolvido com ❤️ para a comunidade de autoelétricas brasileiras.