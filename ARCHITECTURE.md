# Arquitetura do Sistema PrimoAutoEletrica

## Visão Geral

O PrimoAutoEletrica é um sistema de gestão para autoelétricas desenvolvido em .NET 9.0 com WPF para a interface desktop e ASP.NET Core para a API REST. O sistema segue os princípios de arquitetura em camadas e MVVM (Model-View-ViewModel) para a interface WPF.

## Estrutura do Projeto

```
PrimoAutoEletrica/
├── PrimoAutoEletrica/              # Aplicação WPF principal
│   ├── Models/                     # Modelos de dados
│   ├── ViewModels/                 # ViewModels para MVVM
│   ├── Views/                      # Janelas e diálogos
│   ├── UserControls/               # Controles de usuário reutilizáveis
│   ├── Services/                   # Serviços de negócio
│   ├── Repositories/              # Acesso a dados
│   ├── Helpers/                    # Utilitários e helpers
│   ├── Converters/                # Conversores de dados para UI
│   ├── Themes/                     # Estilos e recursos XAML
│   └── App.xaml                    # Ponto de entrada da aplicação
├── PrimoAutoEletrica.Api/         # API REST ASP.NET Core
│   └── Program.cs                 # Configuração da API
├── Tests/                         # Projetos de teste
│   └── PrimoAutoEletrica.Tests/   # Testes unitários
└── README.md                      # Documentação do projeto
```

## Camadas da Arquitetura

### 1. Camada de Apresentação (WPF)

A camada de apresentação segue o padrão MVVM:

- **Models**: Classes que representam as entidades de negócio
- **ViewModels**: Classes que conectam Models e Views, contendo lógica de apresentação
- **Views**: Arquivos XAML que definem a interface do usuário
- **UserControls**: Componentes reutilizáveis de interface

#### Principais UserControls:
- `DashboardControl`: Visão geral do sistema
- `ClientesControl`: Gestão de clientes
- `VeiculosControl`: Gestão de veículos
- `OrcamentosControl`: Gestão de orçamentos
- `EstoqueControl`: Gestão de estoque e produtos
- `FinanceiroControl`: Gestão financeira
- `FuncionariosControl`: Gestão de funcionários

### 2. Camada de Serviços

A camada de serviços contém a lógica de negócio e orquestração:

#### Serviços Principais:
- `NavigationService`: Gerencia navegação entre módulos
- `PermissionService`: Controla permissões de acesso
- `LoggerService`: Gerencia logs estruturados
- `DatabaseService`: Gerencia conexões e transações de banco
- `MigrationService`: Gerencia migrações de banco de dados
- `NotificationService`: Envio de notificações (SMS/WhatsApp)
- `OrcamentoDatabaseService`: Operações com orçamentos
- `EstoqueOperationalService`: Operações de estoque
- `FinanceiroDatabaseService`: Operações financeiras
- `ContabilExportService`: Exportação para sistemas contábeis

### 3. Camada de Acesso a Dados

A camada de acesso a dados gerencia a persistência:

#### Repositories:
- `ClienteRepository`: Operações com clientes
- `VeiculoRepository`: Operações com veículos
- `FuncionarioRepository`: Operações com funcionários
- `ProdutoRepository`: Operações com produtos
- `OrcamentoRepository`: Operações com orçamentos
- `OrdemServicoRepository`: Operações com ordens de serviço

### 4. Camada de API REST

A API expõe os serviços via HTTP:

#### Endpoints Principais:
- `GET /api/health`: Health check
- `GET /api/orcamentos`: Listar orçamentos
- `GET /api/orcamentos/{id}`: Obter orçamento por ID
- `POST /api/orcamentos`: Criar orçamento
- `PUT /api/orcamentos/{id}`: Atualizar orçamento
- `DELETE /api/orcamentos/{id}`: Excluir orçamento
- `GET /api/estoque`: Listar produtos em estoque
- `GET /api/financeiro`: Listar movimentações financeiras

## Padrões e Práticas

### MVVM (Model-View-ViewModel)

- **Separation of Concerns**: Separação clara entre UI e lógica de negócio
- **Data Binding**: Ligação automática entre View e ViewModel
- **Commands**: Padrão Command para ações de usuário
- **INotifyPropertyChanged**: Notificação de mudanças de propriedades

### Dependency Injection

- **Service Locator**: Padrão Service Locator para resolução de dependências
- **Singleton Services**: Serviços únicos como LoggerService e DatabaseService
- **Repository Pattern**: Padrão Repository para acesso a dados

### Logging Estruturado

- **Níveis de Log**: Info, Warning, Error, Critical
- **Contexto de Usuário**: Logs incluem informações do usuário atual
- **Múltiplos Destinos**: Arquivos de log global, estruturado e por área
- **Metadados**: Timestamp, nível, área, usuário, stack trace

### Gerenciamento de Permissões

- **Baseada em Perfis**: Perfis de acesso (Administrador, Vendedor, Caixa, etc.)
- **Validação Centralizada**: PermissionService centraliza validações
- **Códigos de Permissão**: Códigos granulares para cada operação
- **Validação em Tempo Real**: Verificação de permissões antes de operações

### Migrações de Banco de Dados

- **Versionamento**: Cada migração tem um ID único e descrição
- **Execução Controlada**: Migrações executadas apenas se não aplicadas
- **Rollback**: Suporte a rollback de migrações
- **Verificação de Integridade**: Checksums para verificar consistência

## Tecnologias

### Desktop (WPF)
- .NET 9.0
- WPF (Windows Presentation Foundation)
- XAML para UI
- SQLite para banco de dados local
- System.Data.SQLite para acesso a dados

### API REST
- ASP.NET Core 9.0
- Entity Framework Core
- Swagger/OpenAPI para documentação
- CORS para comunicação cross-origin

### Testes
- xUnit para testes unitários
- Moq para mocking
- SQLite in-memory para testes de banco

## Segurança

### Criptografia
- **ProtectedData**: Proteção de dados sensíveis usando DPAPI
- **Hashing**: Hash de senhas usando algoritmos seguros
- **Connection Strings**: Criptografia de strings de conexão

### Controle de Acesso
- **Autenticação**: Sistema de login com sessões
- **Autorização**: Validação de permissões por operação
- **Auditoria**: Registro de ações críticas
- **Politica de Senha**: Exigência de troca periódica

## Performance

### Cache
- **Page Cache**: Cache de páginas do NavigationService com política LRU
- **Limite de Cache**: Configuração de tamanho máximo do cache
- **Invalidação**: Remoção manual de itens do cache

### Banco de Dados
- **Índices**: Índices estratégicos para consultas frequentes
- **Transações**: Transações atômicas para operações críticas
- **Connection Pooling**: Pool de conexões para otimização

## Monitoramento e Diagnóstico

### Logging
- **Logs Estruturados**: Formato JSON para análise
- **Contexto de Aplicação**: Versão, ambiente, usuário
- **Logs por Área**: Separação por módulos funcionais
- **Health Checks**: Endpoint para verificação de saúde

### Auditoria
- **Registro de Ações**: Ações críticas registradas
- **Correlation IDs**: Rastreamento de operações relacionadas
- **Tentativas de Login**: Registro de tentativas de autenticação

## Escalabilidade

### Horizontal Scaling
- **API REST**: Pode ser escalada horizontalmente
- **Load Balancing**: Suporte a balanceamento de carga
- **Stateless API**: API não mantém estado entre requisições

### Vertical Scaling
- **Otimização de Queries**: Melhoria de performance de consultas
- **Cache Distribuído**: Possibilidade de implementar cache distribuído
- **Separação de Banco**: Possibilidade de separar banco de dados

## Manutenibilidade

### Código
- **SOLID Principles**: Princípios de design orientado a objetos
- **Clean Code**: Código limpo e bem documentado
- **Code Reviews**: Processo de revisão de código
- **Comments**: Comentários apenas quando necessário

### Testes
- **Cobertura de Testes**: Meta de cobertura > 80%
- **Testes Unitários**: Testes isolados de componentes
- **Testes de Integração**: Testes de integração entre componentes
- **Testes de UI**: Testes automatizados de interface

### Documentação
- **README.md**: Documentação geral do projeto
- **ARCHITECTURE.md**: Documentação de arquitetura
- **Code Comments**: Comentários em código complexo
- **API Documentation**: Documentação automática via Swagger

## Implantação

### Desktop
- **ClickOnce**: Instalação via ClickOnce
- **MSI**: Pacote de instalação MSI
- **XCopy**: Instalação simples por cópia de arquivos
- **Auto-Update**: Sistema de atualização automática

### API
- **Docker**: Containerização com Docker
- **IIS**: Hospedagem em IIS
- **Azure App Service**: Hospedagem em Azure
- **Self-Hosted**: Auto-hospedagem

## Suporte e Manutenção

### Versionamento
- **Semantic Versioning**: Versionamento semântico (Major.Minor.Patch)
- **Release Notes**: Notas de lançamento
- **Changelog**: Registro de mudanças

### Backup
- **Automated Backups**: Backups automáticos do banco
- **RetentionPolicy**: Política de retenção de backups
- **Recovery Procedures**: Procedimentos de recuperação

## Próximos Passos

### Curto Prazo
- Expandir testes de integração
- Implementar CI/CD completo
- Melhorar documentação de API

### Médio Prazo
- Implementar cache distribuído
- Adicionar suporte a múltiplos bancos
- Melhorar performance de consultas

### Longo Prazo
- Migrar para .NET 8+ mais recente
- Implementar microservices
- Adicionar suporte a mobile

## Contato e Suporte

Para dúvidas sobre a arquitetura ou implementação, consulte:
- **Repository**: [URL do repositório]
- **Issues**: [URL de issues]
- **Documentation**: [URL de documentação]
- **Email**: [email de suporte]