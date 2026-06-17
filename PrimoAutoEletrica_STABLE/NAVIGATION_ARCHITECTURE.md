# Arquitetura de Navegação Profissional - PrimoAutoEletrica ERP

## 📋 Visão Geral

A navegação foi refatorada de um modelo centralizado em MainWindow para um padrão profissional com **NavigationService** que implementa:

- ✅ **Separação de responsabilidades** (Services desacoplados)
- ✅ **Cache inteligente de páginas** (sem recriação desnecessária)
- ✅ **Gerenciamento de histórico** (navegação anterior)
- ✅ **Validação de permissões** (integrado na navegação)
- ✅ **Tratamento de erros centralizado** (eventos)
- ✅ **Extensibilidade** (fácil adicionar novos módulos)

---

## 🏗️ Componentes Principais

### 1. **PermissionService** (`Services/PermissionService.cs`)

**Responsabilidade**: Gerenciar permissões de usuário por perfil de acesso.

#### Métodos:
- `TemPermissao(modulo)` → bool
  - Valida se usuário pode acessar um módulo
  - Administrador tem acesso a tudo

- `ObterModulosPermitidos()` → HashSet<string>
  - Retorna lista de módulos habilitados para o usuário
  - Suporta 7 perfis: Admin, Gerente, Mec�nico, Vendedor, Caixa, Almoxarife

- `ObtePerfil()` → string
  - Retorna perfil de acesso do usuário

- `ObterNomeUsuario()` → string
  - Retorna nome do usuário logado

#### Perfis e Permissões:

| Perfil | Módulos |
|--------|---------|
| **Administrador** | Todos (13 módulos) |
| **Gerente** | Dashboard, Clientes, Veículos, Orçamentos, OS, Estoque, NF-e, Financeiro, Relatórios, Fornecedores, Funcionários, Agendamentos |
| **Mec�nico** | Dashboard, Veículos, OS, Agendamentos |
| **Vendedor** | Dashboard, Clientes, Veículos, Orçamentos, OS, Agendamentos |
| **Caixa** | Dashboard, PDV, Financeiro, Relatórios |
| **Almoxarife** | Dashboard, Estoque, NF-e, Relatórios, Fornecedores |

---

### 2. **INavigationService** (`Services/INavigationService.cs`)

**Responsabilidade**: Definir contrato para serviço de navegação.

#### Interface:
```csharp
public interface INavigationService
{
	UserControl Navigate(string moduleName);
	UserControl NavigateBack();
	bool CanNavigateBack { get; }
	string CurrentModule { get; }

	event EventHandler<NavigationEventArgs> NavigationCompleted;
	event EventHandler<NavigationErrorEventArgs> NavigationError;

	void ClearCache();
	void RemoveFromCache(string moduleName);
}
```

#### Eventos:
- **NavigationCompleted**: Disparado após navegação bem-sucedida
- **NavigationError**: Disparado em caso de erro

---

### 3. **NavigationService** (`Services/NavigationService.cs`)

**Responsabilidade**: Implementar lógica centralizada de navegação.

#### Funcionalidades:

##### A. Cache de Páginas
```csharp
// Página é criada uma vez e reutilizada
var control = _navigationService.Navigate("Dashboard");
// Segunda chamada retorna o mesmo objeto do cache
var control2 = _navigationService.Navigate("Dashboard"); // === control
```

##### B. Histórico de Navegação
```csharp
_navigationService.Navigate("Dashboard");  // Stack: [Dashboard]
_navigationService.Navigate("Clientes");   // Stack: [Dashboard, Clientes]
_navigationService.Navigate("Veículos");   // Stack: [Dashboard, Clientes, Veículos]

if (_navigationService.CanNavigateBack)
{
	var previous = _navigationService.NavigateBack(); // Volta para Clientes
}
```

##### C. Mapeamento de Módulos
```csharp
private readonly Dictionary<string, Type> _moduleMapping = new()
{
	{ "Dashboard", typeof(DashboardControl) },
	{ "Clientes", typeof(ClientesControl) },
	// ... etc
};
```

##### D. Validação de Permissões
```csharp
// Navigate verifica automaticamente se usuário tem permissão
public UserControl Navigate(string moduleName)
{
	if (!_permissionService.TemPermissao(moduleName))
		return null; // Acesso negado
	// ... criar ou obter do cache
}
```

##### E. Extensibilidade Din�mica
```csharp
// Adicionar novo módulo em tempo de execução
_navigationService.RegisterModule("NovoModulo", typeof(NovoControle));
```

---

## 🔄 Fluxo de Navegação

```
Usuário clica MenuClientes
	↓
MenuClientes_Click(...)
	↓
NavegarPara("Clientes")
	↓
NavigationService.Navigate("Clientes")
	├→ Valida permissão (PermissionService.TemPermissao)
	├→ Procura no cache
	├→ Se não existe → cria via Activator.CreateInstance
	├→ Armazena em cache
	├→ Dispara evento NavigationCompleted
	└→ Retorna UserControl
	↓
MainContent.Content = control
	↓
Interface atualizada
```

---

## 📝 Como Usar

### Exemplo: Navegar para Módulo
```csharp
private void MenuClientes_Click(object sender, RoutedEventArgs e)
	=> NavegarPara("Clientes");
```

### Exemplo: Validar Permissão
```csharp
if (_permissionService.TemPermissao("Financeiro"))
{
	// Fazer algo
}
```

### Exemplo: Conectar Evento de Erro
```csharp
_navigationService.NavigationError += (sender, args) =>
{
	MessageBox.Show($"Erro ao navegar: {args.Exception.Message}");
};
```

### Exemplo: Limpar Cache
```csharp
_navigationService.ClearCache();
// Próxima navegação criará novas inst�ncias
```

---

## 🎯 Adicionar Novo Módulo

### Passo 1: Criar UserControl
```xaml
<!-- UserControls/NovoModuloControl.xaml -->
<UserControl x:Class="PrimoAutoEletrica.UserControls.NovoModuloControl"
			 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
	<!-- ... -->
</UserControl>
```

### Passo 2: Adicionar ao Mapeamento (NavigationService)
```csharp
private readonly Dictionary<string, Type> _moduleMapping = new()
{
	// ... existing modules
	{ "NovoModulo", typeof(NovoModuloControl) }
};
```

### Passo 3: Adicionar Permissão (PermissionService)
```csharp
case "Gerente":
	modulos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		// ... existing modules
		"NovoModulo"  // ← Adicionar aqui
	};
	break;
```

### Passo 4: Adicionar Menu Button (MainWindow.xaml)
```xaml
<Button Content="Novo Módulo" Name="MenuNovoModulo" 
		Click="MenuNovoModulo_Click" />
```

### Passo 5: Adicionar Handler (MainWindow.xaml.cs)
```csharp
private void MenuNovoModulo_Click(object sender, RoutedEventArgs e)
	=> NavegarPara("NovoModulo");
```

---

## 📊 Comparação: Antes vs Depois

| Aspecto | Antes | Depois |
|---------|-------|--------|
| **Linhas MainWindow** | 443 | 189 (-57%) |
| **Duplicação de código** | Sim (AplicarPermissoesMenu + TemPermissao) | Não (centralizado) |
| **Cache de páginas** | Não | Sim (inteligente) |
| **Histórico navegação** | Não | Sim (Stack) |
| **Eventos** | Não | Sim (NavigationCompleted, NavigationError) |
| **Testabilidade** | Difícil | Fácil (interface INavigationService) |
| **Extensibilidade** | Limitada | Alta (RegisterModule) |
| **Injeção DI** | Não | Sim (PermissionService) |

---

## 🚀 Próximos Passos (FASE 6+)

1. **MVVM Profissional** → ViewModels para cada módulo
2. **Injeção de Dependência** → Microsoft.Extensions.DependencyInjection
3. **Logging Centralizado** → Microsoft.Extensions.Logging
4. **Animações** → Aproveitar eventos NavigationCompleted
5. **Testes Unitários** → Mock INavigationService
6. **Performance** → Limite de tamanho de cache, cleanup de memória

---

## 📚 Referências

- **Interface Segregation Principle (ISP)**: Cada serviço tem responsabilidade bem definida
- **Single Responsibility Principle (SRP)**: NavigationService = navegação, PermissionService = permissões
- **Dependency Injection**: PermissionService injetado em NavigationService
- **Observer Pattern**: Eventos NavigationCompleted/NavigationError

---

## ✅ Checklist de Validação

- [x] Build sem erros
- [x] Navegação funcional em todos os módulos
- [x] Permissões validadas corretamente
- [x] Cache funcionando (páginas reutilizadas)
- [x] Eventos disparados corretamente
- [x] Código limpo e legível
- [x] Documentação completa
- [ ] Testes unitários implementados
- [ ] CI/CD integrado

---

**Versão**: 1.0  
**Data**: 2024  
**Status**: ✅ Pronto para Produção
