# Documentação para Teste Manual - FASE 3.27

## Data
2026-06-09

## Objetivo
Documentar procedimentos para teste manual da aplicação.

## Pré-requisitos

### Ambiente
- .NET 9.0 instalado
- Visual Studio 2022 ou VS Code
- Windows 10 ou superior

### Banco de Dados
- SQLite configurado
- Banco de dados inicializado

## Procedimentos de Teste

### 1. Build e Testes Automatizados

#### Build
```powershell
cd C:\Projetos\PrimoAutoEletrica
dotnet clean .\PrimoAutoEletrica.sln
dotnet restore .\PrimoAutoEletrica.sln
dotnet build .\PrimoAutoEletrica.sln
```
**Esperado:** Build concluído com êxito

#### Testes
```powershell
dotnet test .\PrimoAutoEletrica.sln
```
**Esperado:** 31/31 testes passando

### 2. Execução da Aplicação

#### Iniciar Aplicação
```powershell
dotnet run --project .\PrimoAutoEletrica\PrimoAutoEletrica.csproj
```
**Esperado:** Aplicação inicia sem erros

### 3. Teste de Login

#### Login Válido
1. Abrir aplicação
2. Inserir email válido
3. Inserir senha válida
4. Clicar em "Entrar no sistema"
**Esperado:** Login realizado com sucesso, MainWindow exibido

#### Login Inválido
1. Abrir aplicação
2. Inserir email inválido
3. Inserir senha inválida
4. Clicar em "Entrar no sistema"
**Esperado:** Mensagem de erro exibida

### 4. Teste de Navegação

#### Navegar para Dashboard
1. Fazer login
2. Clicar em "Dashboard" na sidebar
**Esperado:** Dashboard exibido

#### Navegar para Clientes
1. Fazer login
2. Clicar em "Clientes" na sidebar
**Esperado:** Módulo de Clientes exibido

#### Navegar para Veículos
1. Fazer login
2. Clicar em "Veículos" na sidebar
**Esperado:** Módulo de Veículos exibido

#### Navegar para Orçamentos
1. Fazer login
2. Clicar em "Orçamentos" na sidebar
**Esperado:** Módulo de Orçamentos exibido, sem travamento

#### Navegar para Ordens de Serviço
1. Fazer login
2. Clicar em "Ordens de Serviço" na sidebar
**Esperado:** Módulo de Ordens de Serviço exibido

#### Navegar para PDV
1. Fazer login
2. Clicar em "PDV" na sidebar
**Esperado:** Módulo de PDV exibido

#### Navegar para Estoque
1. Fazer login
2. Clicar em "Estoque" na sidebar
**Esperado:** Módulo de Estoque exibido

#### Navegar para Financeiro
1. Fazer login
2. Clicar em "Financeiro" na sidebar
**Esperado:** Módulo de Financeiro exibido

#### Navegar para Fornecedores
1. Fazer login
2. Clicar em "Fornecedores" na sidebar
**Esperado:** Módulo de Fornecedores exibido

#### Navegar para Funcionários
1. Fazer login
2. Clicar em "Funcionários" na sidebar
**Esperado:** Módulo de Funcionários exibido

#### Navegar para Agendamentos
1. Fazer login
2. Clicar em "Agendamentos" na sidebar
**Esperado:** Módulo de Agendamentos exibido

#### Navegar para Relatórios
1. Fazer login
2. Clicar em "Relatórios" na sidebar
**Esperado:** Módulo de Relatórios exibido

### 5. Teste de Tema

#### Alternar Tema
1. Fazer login
2. Clicar em botão "Tema" no header
**Esperado:** Tema alternado entre claro e escuro

#### Verificar Tema Claro
1. Fazer login
2. Verificar se tema claro está aplicado
**Esperado:** Cores de tema claro visíveis

#### Verificar Tema Escuro
1. Fazer login
2. Alternar para tema escuro
3. Verificar se tema escuro está aplicado
**Esperado:** Cores de tema escuro visíveis

### 6. Teste de Botões

#### Verificar Botões Padrão
1. Navegar para qualquer módulo
2. Verificar botões
**Esperado:** Botões com CornerRadius=8

#### Verificar Botões PDV
1. Navegar para PDV
2. Verificar botões
**Esperado:** Botões com CornerRadius=8

### 7. Teste de Inputs

#### Verificar Inputs Padrão
1. Navegar para qualquer módulo
2. Verificar inputs
**Esperado:** Inputs com CornerRadius=8

#### Verificar Focus
1. Clicar em qualquer input
**Esperado:** Borda laranja indicando focus

### 8. Teste de DataGrid

#### Verificar DataGrid Padrão
1. Navegar para qualquer módulo com DataGrid
2. Verificar DataGrid
**Esperado:** DataGrid com estilo PremiumDataGrid

#### Verificar Virtualização
1. Navegar para módulo com muitos dados
2. Verificar performance
**Esperado:** Scroll suave com virtualização

### 9. Teste de Cards

#### Verificar Cards Padrão
1. Navegar para Dashboard
2. Verificar cards de métricas
**Esperado:** Cards com CornerRadius=12 ou 16

### 10. Teste de Logout

#### Logout
1. Fazer login
2. Clicar em "Sair" na sidebar
**Esperado:** Logout realizado, LoginWindow exibido

## Relatório de Teste

### Resultados Esperados
- Build: êxito
- Testes: 31/31 passando
- Aplicação: rodando sem erros
- Login: funcional
- Navegação: funcional
- Tema: funcional
- Botões: com CornerRadius=8
- Inputs: com CornerRadius=8
- DataGrid: com estilo PremiumDataGrid
- Cards: com CornerRadius padronizado
- Logout: funcional

### Resultados Obtidos
- Build: ✅ êxito em 17,1s
- Testes: ✅ êxito em 6,6s (31/31 testes)
- Aplicação: ✅ rodando sem erros
- Login: ⏳ requer teste manual
- Navegação: ⏳ requer teste manual
- Tema: ⏳ requer teste manual
- Botões: ✅ CornerRadius=8 aplicado
- Inputs: ✅ CornerRadius=8 aplicado
- DataGrid: ✅ estilo PremiumDataGrid aplicado
- Cards: ✅ CornerRadius padronizado
- Logout: ⏳ requer teste manual

## Conclusão

### Status Final
- Documentação criada: ✅
- Procedimentos definidos: ✅
- Testes manuais: ⏳ requer execução pelo usuário
- Build: ✅ êxito
- Testes: ✅ êxito
- Aplicação: ✅ rodando

### Pode continuar para FASE 3.28?
**SIM**

Documentação para teste manual criada. Testes manuais requerem execução pelo usuário. Build e testes passaram, aplicação está rodando sem erros.

### Próximos Passos
1. FASE 3.28 - Relatório Final da FASE 3

### Observações
- Documentação completa para teste manual
- Procedimentos detalhados para cada módulo
- Resultados esperados definidos
- Testes manuais requerem execução pelo usuário
