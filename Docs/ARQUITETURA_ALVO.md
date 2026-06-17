# ARQUITETURA ALVO - PRIMO AUTO ELÉTRICA

**Data:** 2026-06-10  
**Projeto:** PrimoAutoEletrica  
**Versão:** 1.0

---

## 1. VISÃO GERAL

Este documento descreve a arquitetura alvo desejada para o sistema PrimoAutoEletrica, focando em organização, manutenibilidade e escalabilidade.

---

## 2. ESTRUTURA DE PASTAS ALVO

```
PrimoAutoEletrica/
├── Models/                 # Modelos de dados
├── ViewModels/            # ViewModels (MVVM)
├── Views/                  # Views (Windows)
├── UserControls/           # UserControls reutilizáveis
├── Services/               # Serviços de negócio
├── Repositories/           # Repositórios de dados
├── Interfaces/             # Interfaces (NOVA)
├── Helpers/                # Helpers utilitários
├── Converters/             # Converters WPF
├── Themes/                 # Temas e estilos
├── Data/                   # Arquivos de dados estáticos
├── Docs/                   # Documentação
├── Scripts/                # Scripts PowerShell
├── Reports/                # Relatórios gerados
└── Tests/
    └── PrimoAutoEletrica.Tests/  # Testes unitários
```

---

## 3. PADRÕES E PRÁTICAS

### 3.1 MVVM (Model-View-ViewModel)
- **Model:** Representa dados e lógica de negócio
- **View:** Interface do usuário (XAML)
- **ViewModel:** Mediator entre Model e View, contém lógica de apresentação

### 3.2 Dependency Injection
- Usar injeção de dependência para serviços
- Interfaces definidas em pasta `Interfaces/`
- Implementação em pastas específicas

### 3.3 Repository Pattern
- Repositórios em pasta `Repositories/`
- Interfaces em pasta `Interfaces/`
- Abstração de acesso a dados

### 3.4 Service Layer
- Serviços em pasta `Services/`
- Lógica de negócio complexa
- Orquestração de múltiplos repositórios

---

## 4. ORGANIZAÇÃO POR RESPONSABILIDADE

### 4.1 Models
- Entidades de domínio
- DTOs (Data Transfer Objects)
- ViewModels de API (se necessário)

### 4.2 ViewModels
- Um ViewModel por View
- Implementar INotifyPropertyChanged
- Commands para ações
- Validação de dados

### 4.3 Views
- Windows (MainWindow, LoginWindow, etc.)
- UserControls reutilizáveis
- Separação de lógica de apresentação

### 4.4 Services
- Serviços de negócio
- Serviços de infraestrutura (Logger, Database, etc.)
- Serviços de integração

### 4.5 Repositories
- CRUD básico
- Queries complexas
- Abstração de banco de dados

### 4.6 Interfaces
- Contratos para serviços
- Contratos para repositórios
- Facilita testes e manutenção

---

## 5. NAMING CONVENTIONS

### 5.1 Classes
- PascalCase: `ClienteViewModel`, `DatabaseService`

### 5.2 Métodos
- PascalCase: `ObterTodos`, `AdicionarCliente`

### 5.3 Propriedades
- PascalCase: `Nome`, `Id`

### 5.4 Campos privados
- camelCase com underscore: `_clienteService`

### 5.5 Interfaces
- Prefixo I: `IClienteRepository`, `ILoggerService`

---

## 6. REFATORAÇÕES FUTURAS

### 6.1 Arquivos Grandes
- Dividir `UiSmokeTestService.cs` se > 500 linhas
- Dividir `OperationalWorkflowTestService.cs` se > 500 linhas
- Dividir `RelatorioDatabaseService.cs` se > 500 linhas
- Dividir `AgendamentoDatabaseService.cs` se > 500 linhas

### 6.2 Services a Revisar
- `DatabaseService.cs` - considerar partial classes
- `AgendamentosViewModel.cs` - simplificar se muito complexo
- `RelatoriosViewModel.cs` - simplificar se muito complexo
- `FinanceiroViewModel.cs` - simplificar se muito complexo

---

## 7. PADRÕES DE CÓDIGO

### 7.1 Comentários
- XML documentation para APIs públicas
- Comentários apenas quando necessário
- Evitar comentários óbvios

### 7.2 Tratamento de Erros
- Try-catch em pontos críticos
- Log de erros sempre
- Mensagens amigáveis para usuário

### 7.3 Validação
- Validação em ViewModel
- Validação em Repository (camada de dados)
- Validação em Service (regras de negócio)

---

## 8. INTEGRAÇÕES

### 8.1 Banco de Dados
- SQLite como banco principal
- Migrations versionadas
- Backup automático

### 8.2 Logging
- LoggerService centralizado
- Logs em arquivo
- Logs críticos em fallback

### 8.3 Auditoria
- AuditLogService para ações críticas
- Registro de login/logout
- Registro de operações sensíveis

---

## 9. PERFORMANCE

### 9.1 Lazy Loading
- Carregar dados apenas quando necessário
- Virtualização de listas grandes

### 9.2 Caching
- Cachear dados estáticos
- Cachear configurações

### 9.3 Async/Await
- Operações I/O assíncronas
- Não bloquear thread da UI

---

## 10. SEGURANÇA

### 10.1 Permissões
- Verificar permissões antes de ações
- Bloqueio por perfil
- Auditoria de ações

### 10.2 Senhas
- Hash de senhas
- Nunca armazenar senha pura
- Política de senha mínima

---

## 11. TESTES

### 11.1 Testes Unitários
- Testes de Services
- Testes de Repositories
- Testes de ViewModels

### 11.2 Testes de Integração
- Testes de fluxo completo
- Testes de banco de dados

### 11.3 Testes de UI
- UI Smoke Test
- Workflow Test

---

## 12. DOCUMENTAÇÃO

### 12.1 Documentação de Código
- XML documentation para APIs públicas
- Comentários em código complexo

### 12.2 Documentação de Usuário
- Manual do usuário
- Guia de instalação
- FAQ

### 12.3 Documentação Técnica
- Arquitetura
- Banco de dados
- Design system

---

**Última atualização:** 2026-06-10
