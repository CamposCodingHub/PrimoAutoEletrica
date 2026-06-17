# Guia de Estilo - PrimoAutoEletrica

**Versão:** 1.0  
**Data:** 2026-06-09  
**Projeto:** PrimoAutoEletrica

---

## 1. Estrutura de Pastas

```
PrimoAutoEletrica/
├── Models/              # Modelos de dados
├── ViewModels/          # ViewModels (MVVM)
├── Views/               # Windows e Dialogs
│   ├── Clientes/        # Views específicas de módulos
│   ├── PDV/            # Views específicas de módulos
│   └── Relatorios/     # Views específicas de módulos
├── UserControls/        # UserControls reutilizáveis
├── Services/            # Serviços de negócio
│   ├── Catalogo/       # Sub-serviços
│   └── DatabaseProviders/ # Sub-serviços
├── Repositories/        # Repositórios de dados
├── Converters/          # Converters de dados
├── Helpers/             # Helpers e utilitários
├── Themes/              # Temas e estilos XAML
├── Data/                # Dados estáticos e arquivos
│   └── Repositories/    # Repositórios de dados estáticos
├── Docs/                # Documentação
└── Scripts/             # Scripts de automação
```

---

## 2. Convenções de Nomenclatura

### 2.1 Namespaces

- **Padrão:** `PrimoAutoEletrica.[Pasta]`
- **Exemplos:**
  - `PrimoAutoEletrica.Models`
  - `PrimoAutoEletrica.ViewModels`
  - `PrimoAutoEletrica.Views`
  - `PrimoAutoEletrica.Views.Clientes`
  - `PrimoAutoEletrica.UserControls`
  - `PrimoAutoEletrica.Services`
  - `PrimoAutoEletrica.Repositories`
  - `PrimoAutoEletrica.Converters`
  - `PrimoAutoEletrica.Helpers`

### 2.2 Classes

- **Padrão:** PascalCase
- **Exemplos:**
  - `Cliente`
  - `Produto`
  - `OrdemServico`
  - `ClienteViewModel`
  - `ProdutoService`

### 2.3 Interfaces

- **Padrão:** PascalCase com prefixo `I`
- **Exemplos:**
  - `IClienteRepository`
  - `IProdutoRepository`
  - `INavigationService`

### 2.4 Propriedades

- **Padrão:** PascalCase
- **Exemplos:**
  - `Nome`
  - `CPF`
  - `DataCadastro`
  - `TotalGasto`

### 2.5 Métodos

- **Padrão:** PascalCase
- **Exemplos:**
  - `ObterTodos`
  - `Salvar`
  - `Atualizar`
  - `Excluir`

### 2.6 Parâmetros

- **Padrão:** camelCase
- **Exemplos:**
  - `clienteId`
  - `nome`
  - `dataCadastro`

### 2.7 Campos Privados

- **Padrão:** _camelCase
- **Exemplos:**
  - `_clienteRepository`
  - `_loggerService`

### 2.8 Constantes

- **Padrão:** PascalCase
- **Exemplos:**
  - `MaxRetryAttempts`
  - `DefaultTimeout`

### 2.9 Variáveis Locais

- **Padrão:** camelCase
- **Exemplos:**
  - `cliente`
  - `total`
  - `dataAtual`

### 2.10 Arquivos

- **Padrão:** PascalCase
- **Exemplos:**
  - `Cliente.cs`
  - `ClienteViewModel.cs`
  - `ClienteService.cs`
  - `ClientesControl.xaml`
  - `NovoClienteWindow.xaml`

---

## 3. Convenções de Código

### 3.1 Organização de Arquivos

```csharp
using System;
using System.Collections.Generic;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public class ClienteService
    {
        // Campos privados
        private readonly IClienteRepository _clienteRepository;

        // Construtor
        public ClienteService(IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }

        // Propriedades públicas
        public int TotalClientes { get; private set; }

        // Métodos públicos
        public Cliente ObterPorId(int id)
        {
            return _clienteRepository.ObterPorId(id);
        }

        // Métodos privados
        private void ValidarCliente(Cliente cliente)
        {
            // Validação
        }
    }
}
```

### 3.2 Propriedades

```csharp
// Propriedade simples
public string Nome { get; set; } = string.Empty;

// Propriedade com validação
private string _nome;
public string Nome
{
    get => _nome;
    set
    {
        if (_nome != value)
        {
            _nome = value;
            OnPropertyChanged();
        }
    }
}

// Propriedade computada
public string Documento => string.IsNullOrWhiteSpace(CPF)
    ? RG
    : CadastroValidationHelper.FormatarDocumento(CPF);
```

### 3.3 Métodos

```csharp
// Método simples
public Cliente ObterPorId(int id)
{
    return _clienteRepository.ObterPorId(id);
}

// Método assíncrono
public async Task<Cliente> ObterPorIdAsync(int id)
{
    return await _clienteRepository.ObterPorIdAsync(id);
}

// Método com múltiplos parâmetros
public void Salvar(Cliente cliente, bool notificar = true)
{
    _clienteRepository.Salvar(cliente);
    if (notificar)
    {
        NotificarAlteracao();
    }
}
```

### 3.4 Comentários

```csharp
// Comentário de linha simples

/// <summary>
/// Obtém um cliente por ID
/// </summary>
/// <param name="id">ID do cliente</param>
/// <returns>Cliente encontrado</returns>
public Cliente ObterPorId(int id)
{
    return _clienteRepository.ObterPorId(id);
}

/*
 * Comentário de bloco
 * para explicações mais detalhadas
 */
```

---

## 4. Convenções XAML

### 4.1 Estrutura de Arquivos XAML

```xml
<UserControl x:Class="PrimoAutoEletrica.UserControls.ClientesControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <!-- Conteúdo -->
    </Grid>
</UserControl>
```

### 4.2 Nomenclatura de Elementos

- **Padrão:** PascalCase
- **Exemplos:**
  - `ClientesDataGrid`
  - `NomeTextBox`
  - `SalvarButton`

### 4.3 Resources

- **Padrão:** PascalCase
- **Exemplos:**
  - `CardBackgroundBrush`
  - `PrimaryBrush`
  - `CardShadowEffect`

### 4.4 Estilos

- **Padrão:** PascalCase
- **Exemplos:**
  - `CardBorder`
  - `MetricCardStyle`
  - `DashboardCard`

---

## 5. Convenções MVVM

### 5.1 ViewModel Base

```csharp
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

### 5.2 RelayCommand

```csharp
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object parameter) => _execute();
    public event EventHandler CanExecuteChanged;
}
```

### 5.3 ViewModel Padrão

```csharp
public class ClienteViewModel : ViewModelBase
{
    private Cliente _cliente;
    public Cliente Cliente
    {
        get => _cliente;
        set
        {
            _cliente = value;
            OnPropertyChanged();
        }
    }

    public ICommand SalvarCommand { get; }

    public ClienteViewModel()
    {
        SalvarCommand = new RelayCommand(Salvar, CanSalvar);
    }

    private void Salvar()
    {
        // Lógica de salvar
    }

    private bool CanSalvar()
    {
        return Cliente != null && !string.IsNullOrWhiteSpace(Cliente.Nome);
    }
}
```

---

## 6. Convenções de Banco de Dados

### 6.1 Nomenclatura de Tabelas

- **Padrão:** PascalCase
- **Exemplos:**
  - `Clientes`
  - `Produtos`
  - `OrdensServico`

### 6.2 Nomenclatura de Colunas

- **Padrão:** PascalCase
- **Exemplos:**
  - `Nome`
  - `CPF`
  - `DataCadastro`

### 6.3 Chaves Primárias

- **Padrão:** `Id`
- **Exemplos:**
  - `Clientes.Id`
  - `Produtos.Id`

### 6.4 Chaves Estrangeiras

- **Padrão:** `[Tabela]Id`
- **Exemplos:**
  - `ClienteId`
  - `ProdutoId`

---

## 7. Convenções de Testes

### 7.1 Nomenclatura de Testes

- **Padrão:** `[MétodoTestado]_[Cenário]_[ResultadoEsperado]`
- **Exemplos:**
  - `ObterPorId_ClienteExistente_RetornaCliente`
  - `Salvar_ClienteInvalido_LancaExcecao`

### 7.2 Estrutura de Testes

```csharp
[Fact]
public void ObterPorId_ClienteExistente_RetornaCliente()
{
    // Arrange
    var cliente = new Cliente { Nome = "João" };
    _repository.Salvar(cliente);

    // Act
    var resultado = _service.ObterPorId(cliente.Id);

    // Assert
    Assert.NotNull(resultado);
    Assert.Equal("João", resultado.Nome);
}
```

---

## 8. Boas Práticas

### 8.1 Performance

- Usar `async/await` para operações I/O
- Evitar bloqueios de thread
- Usar `StringBuilder` para concatenação de strings
- Usar `List<T>` em vez de `ArrayList`
- Usar `foreach` em vez de `for` quando possível

### 8.2 Segurança

- Nunca armazenar senhas em texto plano
- Usar hashing para senhas
- Validar entrada do usuário
- Usar parâmetros em queries SQL
- Sanitizar input de usuário

### 8.3 Manutenibilidade

- Escrever código legível
- Adicionar comentários quando necessário
- Seguir princípios SOLID
- Evitar código duplicado
- Usar injeção de dependência

### 8.4 Tratamento de Erros

- Usar `try/catch` para exceções esperadas
- Logar erros para diagnóstico
- Fornecer mensagens de erro claras
- Não capturar `Exception` genérico sem motivo
- Usar `finally` para limpeza de recursos

---

## 9. Ferramentas e Configurações

### 9.1 EditorConfig

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.xaml]
indent_style = space
indent_size = 2
```

### 9.2 .editorconfig

O projeto deve ter um arquivo `.editorconfig` na raiz para padronizar a formatação entre diferentes editores.

---

## 10. Referências

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [MVVM Pattern](https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern)

---

**Este guia deve ser atualizado conforme o projeto evolui.**
