# PADRÃO DE CÓDIGO - PRIMO AUTO ELÉTRICA

**Data:** 2026-06-10  
**Projeto:** PrimoAutoEletrica  
**Versão:** 1.0

---

## 1. VISÃO GERAL

Este documento define os padrões de código a serem seguidos no desenvolvimento do sistema PrimoAutoEletrica.

---

## 2. NAMING CONVENTIONS

### 2.1 Classes
- **Padrão:** PascalCase
- **Exemplo:** `ClienteViewModel`, `DatabaseService`, `ClienteRepository`

### 2.2 Interfaces
- **Padrão:** PascalCase com prefixo I
- **Exemplo:** `IClienteRepository`, `ILoggerService`, `INavigationService`

### 2.3 Métodos
- **Padrão:** PascalCase
- **Exemplo:** `ObterTodos`, `AdicionarCliente`, `ValidarDados`

### 2.4 Propriedades
- **Padrão:** PascalCase
- **Exemplo:** `Nome`, `Id`, `DataCriacao`

### 2.5 Campos privados
- **Padrão:** camelCase com prefixo underscore
- **Exemplo:** `_clienteService`, `_logger`, `_database`

### 2.6 Constantes
- **Padrão:** PascalCase
- **Exemplo:** `MaxTentativas`, `TimeoutPadrao`

### 2.7 Variáveis locais
- **Padrão:** camelCase
- **Exemplo:** `clienteId`, `nomeCliente`, `resultado`

### 2.8 Parâmetros
- **Padrão:** camelCase
- **Exemplo:** `clienteId`, `nome`, `opcoes`

---

## 3. ESTRUTURA DE ARQUIVOS

### 3.1 Ordem de elementos em uma classe
1. Usings
2. Namespace
3. Comentário de classe (XML documentation)
4. Declaração de classe
5. Campos privados
6. Propriedades públicas
7. Construtores
8. Métodos públicos
9. Métodos privados
10. Eventos

### 3.2 Exemplo
```csharp
using System;
using System.Collections.Generic;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.ViewModels
{
    /// <summary>
    /// ViewModel para gestão de clientes
    /// </summary>
    public class ClienteViewModel : INotifyPropertyChanged
    {
        private readonly IClienteRepository _clienteRepository;
        private string _nome;
        
        public string Nome
        {
            get => _nome;
            set
            {
                _nome = value;
                OnPropertyChanged(nameof(Nome));
            }
        }
        
        public ClienteViewModel(IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }
        
        public void CarregarClientes()
        {
            // Implementação
        }
        
        private void OnPropertyChanged(string propertyName)
        {
            // Implementação
        }
    }
}
```

---

## 4. COMENTÁRIOS

### 4.1 XML Documentation
- **Quando usar:** Em APIs públicas (classes, métodos, propriedades públicas)
- **Formato:**
```csharp
/// <summary>
/// Obtém todos os clientes do sistema
/// </summary>
/// <returns>Lista de clientes</returns>
public List<Cliente> ObterTodos()
{
    // Implementação
}
```

### 4.2 Comentários de código
- **Quando usar:** Em código complexo ou não óbvio
- **Formato:** `// Comentário`
- **Evitar:** Comentários óbvios como `// Incrementa contador`

### 4.3 Comentários TODO
- **Formato:** `// TODO: Descrição da tarefa`
- **Uso:** Marcar código que precisa de revisão ou implementação futura

---

## 5. FORMATAÇÃO

### 5.1 Indentação
- **Tamanho:** 4 espaços
- **Não usar:** Tabs

### 5.2 Chaves
- **Estilo:** K&R (chave na mesma linha)
- **Exemplo:**
```csharp
if (condicao)
{
    // código
}
else
{
    // código
}
```

### 5.3 Linhas em branco
- Uma linha em branco entre métodos
- Uma linha em branco entre grupos lógicos
- Não usar múltiplas linhas em branco consecutivas

### 5.4 Tamanho de linha
- **Máximo:** 120 caracteres
- **Preferência:** < 100 caracteres

---

## 6. PADRÕES DE CÓDIGO

### 6.1 MVVM
- **Model:** Apenas dados e lógica de negócio
- **ViewModel:** Mediator entre Model e View, contém lógica de apresentação
- **View:** Apenas XAML, código-behind mínimo

### 6.2 Dependency Injection
- Injetar dependências via construtor
- Usar interfaces para serviços e repositórios
- Não criar instâncias diretamente

### 6.3 Repository Pattern
- Um repository por entidade
- Métodos padrão: ObterTodos, ObterPorId, Adicionar, Atualizar, Excluir
- Queries específicas em métodos separados

### 6.4 Async/Await
- Usar async/await para operações I/O
- Não bloquear thread da UI
- Configurar await corretamente (ConfigureAwait(false) em código de biblioteca)

---

## 7. TRATAMENTO DE ERROS

### 7.1 Try-Catch
- Usar try-catch em pontos críticos
- Logar sempre que possível
- Mensagens amigáveis para usuário

### 7.2 Exemplo
```csharp
try
{
    var cliente = _clienteRepository.ObterPorId(id);
    return cliente;
}
catch (Exception ex)
{
    _logger.LogError("Erro ao obter cliente", ex);
    throw new ApplicationException("Erro ao obter cliente", ex);
}
```

### 7.3 Exceções personalizadas
- Criar exceções personalizadas para erros de negócio
- Herdar de ApplicationException ou Exception
- Incluir mensagem descritiva

---

## 8. VALIDAÇÃO

### 8.1 Validação em ViewModel
- Validar dados antes de enviar para repository
- Usar INotifyDataErrorInfo para validação
- Mensagens de erro amigáveis

### 8.2 Validação em Repository
- Validar dados antes de persistir
- Lançar exceções em caso de erro
- Logar erros de validação

### 8.3 Validação em Service
- Validar regras de negócio
- Validar permissões
- Validar consistência de dados

---

## 9. PERFORMANCE

### 9.1 Lazy Loading
- Carregar dados apenas quando necessário
- Usar Lazy<T> quando apropriado
- Não carregar dados desnecessários

### 9.2 Caching
- Cachear dados estáticos
- Cachear configurações
- Invalidar cache quando necessário

### 9.3 Virtualização
- Usar virtualização em listas grandes
- VirtualizingStackPanel em WPF
- Paginação quando necessário

---

## 10. SEGURANÇA

### 10.1 Senhas
- Nunca armazenar senha pura
- Usar hash de senha
- Usar salt quando apropriado

### 10.2 SQL Injection
- Usar parâmetros em queries SQL
- Nunca concatenar strings em queries
- Usar ORM quando possível

### 10.3 XSS
- Sanitizar entrada do usuário
- Escapar HTML quando necessário
- Validar dados antes de exibir

---

## 11. TESTES

### 11.1 Testes unitários
- Testar métodos isoladamente
- Usar mocks para dependências
- Nomes de testes descritivos

### 11.2 Exemplo
```csharp
[Fact]
public void ObterTodos_DeveRetornarListaDeClientes()
{
    // Arrange
    var repository = new ClienteRepositoryMock();
    
    // Act
    var clientes = repository.ObterTodos();
    
    // Assert
    Assert.NotNull(clientes);
    Assert.True(clientes.Count > 0);
}
```

### 11.3 Testes de integração
- Testar fluxos completos
- Usar banco de dados de teste
- Limpar dados após teste

---

## 12. BOAS PRÁTICAS

### 12.1 DRY (Don't Repeat Yourself)
- Não duplicar código
- Extrair código comum para métodos
- Usar herança quando apropriado

### 12.2 KISS (Keep It Simple, Stupid)
- Manter código simples
- Evitar complexidade desnecessária
- Preferir código legível a código inteligente

### 12.3 YAGNI (You Aren't Gonna Need It)
- Não implementar funcionalidades não solicitadas
- Evitar over-engineering
- Implementar apenas o necessário

### 12.4 SOLID
- **S**ingle Responsibility: Uma classe, uma responsabilidade
- **O**pen/Closed: Aberto para extensão, fechado para modificação
- **L**iskov Substitution: Subtipos devem ser substituíveis
- **I**nterface Segregation: Interfaces específicas
- **D**ependency Inversion: Depender de abstrações, não de implementações

---

## 13. REVISÃO DE CÓDIGO

### 13.1 Checklist
- [ ] Código segue padrões de naming
- [ ] Código está bem formatado
- [ ] Comentários são necessários e úteis
- [ ] Tratamento de erros está presente
- [ ] Validação está presente
- [ ] Código é testável
- [ ] Código é performático
- [ ] Código é seguro
- [ ] Código é legível
- [ ] Código é mantível

### 13.2 Antes de commit
1. Executar testes
2. Verificar formatação
3. Remover código comentado
4. Remover código não usado
5. Atualizar documentação

---

## 14. FERRAMENTAS

### 14.1 Linting
- Usar PSScriptAnalyzer para scripts PowerShell
- Usar StyleCop para C# (opcional)
- Usar ReSharper (opcional)

### 14.2 Formatação
- Usar formatador automático do IDE
- Configurar para seguir padrões do projeto

### 14.3 Análise estática
- Usar SonarQube (opcional)
- Usar Code Analysis do Visual Studio (opcional)

---

**Última atualização:** 2026-06-10
