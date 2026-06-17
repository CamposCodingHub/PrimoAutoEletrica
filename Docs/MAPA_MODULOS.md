# MAPA DE MÓDULOS - PRIMO AUTO ELÉTRICA

**Data:** 2026-06-10  
**Projeto:** PrimoAutoEletrica  
**Versão:** 1.0

---

## 1. VISÃO GERAL

Este documento mapeia todos os módulos do sistema PrimoAutoEletrica, suas responsabilidades e interações.

---

## 2. MÓDULOS PRINCIPAIS

### 2.1 Dashboard
**Responsabilidade:** Visão geral do sistema
- Cards de métricas
- Ações rápidas
- Alertas importantes
- Gráficos de faturamento

**ViewModel:** `DashboardViewModel.cs`  
**View:** `DashboardControl.xaml`  
**UserControl:** `DashboardControl.xaml.cs`

---

### 2.2 Clientes
**Responsabilidade:** Gestão de clientes
- Cadastro de clientes
- Edição de clientes
- Visualização de histórico
- Busca e filtros

**ViewModel:** `ClientesViewModel.cs`  
**View:** `ClientesWindow.xaml`  
**Repository:** `ClienteRepository.cs`

---

### 2.3 Veículos
**Responsabilidade:** Gestão de veículos
- Cadastro de veículos
- Edição de veículos
- Vinculação com clientes
- Histórico de serviços

**ViewModel:** `VeiculosViewModel.cs`  
**View:** `VeiculosWindow.xaml`  
**Repository:** `VeiculoRepository.cs`

---

### 2.4 Orçamentos
**Responsabilidade:** Gestão de orçamentos
- Criação de orçamentos
- Edição de orçamentos
- Aprovação/recusa
- Conversão em OS

**ViewModel:** `OrcamentosViewModel.cs`  
**View:** `OrcamentosWindow.xaml`  
**Repository:** `OrcamentoRepository.cs`

---

### 2.5 Ordens de Serviço
**Responsabilidade:** Gestão de ordens de serviço
- Abertura de OS
- Diagnóstico
- Execução de serviços
- Finalização e entrega

**ViewModel:** `OrdensServicoViewModel.cs`  
**View:** `OrdensServicoWindow.xaml`  
**Repository:** `OrdemServicoRepository.cs`

---

### 2.6 PDV (Ponto de Venda)
**Responsabilidade:** Vendas no balcão
- Busca de produtos
- Adição de itens
- Pagamentos
- Emissão de comprovante

**ViewModel:** `PdvViewModel.cs`  
**View:** `PdvControl.xaml`  
**Repository:** `VendaRepository.cs`

---

### 2.7 Caixa
**Responsabilidade:** Gestão de caixa
- Abertura de caixa
- Fechamento de caixa
- Sangria e suprimento
- Relatório de fechamento

**ViewModel:** `CaixaViewModel.cs`  
**View:** `CaixaWindow.xaml`  
**Repository:** `CaixaRepository.cs`

---

### 2.8 Estoque
**Responsabilidade:** Gestão de estoque
- Cadastro de produtos
- Entrada e saída
- Ajuste de estoque
- Estoque crítico

**ViewModel:** `EstoqueViewModel.cs`  
**View:** `EstoqueWindow.xaml`  
**Repository:** `ProdutoRepository.cs`

---

### 2.9 Importação NF-e
**Responsabilidade:** Importação de notas fiscais
- Leitura de XML
- Atualização de estoque
- Criação de fornecedores
- Geração de contas a pagar

**ViewModel:** `ImportacaoNfeViewModel.cs`  
**View:** `ImportacaoNfeWindow.xaml`  
**Service:** `NfeImportService.cs`

---

### 2.10 Financeiro
**Responsabilidade:** Gestão financeira
- Contas a pagar
- Contas a receber
- Fluxo de caixa
- Relatórios financeiros

**ViewModel:** `FinanceiroViewModel.cs`  
**View:** `FinanceiroWindow.xaml`  
**Repository:** `FinanceiroRepository.cs`

---

### 2.11 Fornecedores
**Responsabilidade:** Gestão de fornecedores
- Cadastro de fornecedores
- Edição de fornecedores
- Histórico de compras
- Produtos fornecidos

**ViewModel:** `FornecedoresViewModel.cs`  
**View:** `FornecedoresWindow.xaml`  
**Repository:** `FornecedorRepository.cs`

---

### 2.12 Funcionários
**Responsabilidade:** Gestão de funcionários
- Cadastro de funcionários
- Edição de funcionários
- Perfil e permissões
- Produtividade

**ViewModel:** `FuncionariosViewModel.cs`  
**View:** `FuncionariosWindow.xaml`  
**Repository:** `FuncionarioRepository.cs`

---

### 2.13 Agendamentos
**Responsabilidade:** Gestão de agenda
- Agendamento de serviços
- Reagendamento
- Cancelamento
- Visualização de agenda

**ViewModel:** `AgendamentosViewModel.cs`  
**View:** `AgendamentosWindow.xaml`  
**Repository:** `AgendamentoRepository.cs`

---

### 2.14 Relatórios
**Responsabilidade:** Geração de relatórios
- Relatórios financeiros
- Relatórios de estoque
- Relatórios de vendas
- Exportação de dados

**ViewModel:** `RelatoriosViewModel.cs`  
**View:** `RelatoriosWindow.xaml`  
**Service:** `RelatorioService.cs`

---

### 2.15 Configurações
**Responsabilidade:** Configurações do sistema
- Dados da oficina
- Configurações operacionais
- Mensagens padrão
- Backup automático

**ViewModel:** `ConfiguracoesViewModel.cs`  
**View:** `ConfiguracoesWindow.xaml`  
**Service:** `ConfiguracaoService.cs`

---

## 3. SERVIÇOS DE INFRAESTRUTURA

### 3.1 DatabaseService
**Responsabilidade:** Acesso ao banco de dados
- Conexão com SQLite
- Inicialização do banco
- Aplicação de migrations

**Arquivo:** `Services/DatabaseService.cs`

---

### 3.2 LoggerService
**Responsabilidade:** Logging
- Log de informações
- Log de erros
- Log de avisos
- Log crítico

**Arquivo:** `Services/LoggerService.cs`

---

### 3.3 AuditLogService
**Responsabilidade:** Auditoria
- Registro de ações
- Registro de login/logout
- Registro de operações críticas

**Arquivo:** `Services/AuditLogService.cs`

---

### 3.4 DatabaseBackupService
**Responsabilidade:** Backup do banco
- Backup manual
- Backup automático
- Verificação de integridade
- Restauração

**Arquivo:** `Services/DatabaseBackupService.cs`

---

### 3.5 ThemeService
**Responsabilidade:** Gestão de temas
- Aplicação de tema claro/escuro
- Persistência de tema
- Troca dinâmica de tema

**Arquivo:** `Services/ThemeService.cs`

---

### 3.6 NavigationService
**Responsabilidade:** Navegação
- Navegação entre módulos
- Verificação de permissões
- Histórico de navegação

**Arquivo:** `Services/NavigationService.cs`

---

### 3.7 PermissionService
**Responsabilidade:** Permissões
- Verificação de permissões
- Matriz de permissões
- Bloqueio de ações

**Arquivo:** `Services/PermissionService.cs`

---

## 4. USERCONTROLS REUTILIZÁVEIS

### 4.1 DashboardControl
**Responsabilidade:** Exibição do dashboard

### 4.2 PdvControl
**Responsabilidade:** Interface do PDV

### 4.3 ClienteCard
**Responsabilidade:** Exibição de cliente em card

### 4.4 ProdutoCard
**Responsabilidade:** Exibição de produto em card

### 4.5 MetricCard
**Responsabilidade:** Exibição de métricas

---

## 5. INTERAÇÕES ENTRE MÓDULOS

### 5.1 Cliente → Veículo
- Um cliente pode ter múltiplos veículos
- Veículo vinculado a cliente

### 5.2 Veículo → Orçamento
- Orçamento vinculado a veículo
- Orçamento vinculado a cliente

### 5.3 Orçamento → Ordem de Serviço
- Orçamento pode ser convertido em OS
- OS herda dados do orçamento

### 5.4 Ordem de Serviço → Venda
- OS pode gerar venda
- Venda vincula produtos e serviços

### 5.5 Venda → Caixa
- Venda registra entrada no caixa
- Pagamento vinculado ao caixa

### 5.6 Produto → Estoque
- Produto controla estoque
- Venda baixa estoque
- Compra aumenta estoque

### 5.7 NF-e → Produto
- Importação atualiza custo
- Importação atualiza estoque
- Importação cria fornecedor

---

## 6. FLUXOS DE TRABALHO

### 6.1 Fluxo de Orçamento
1. Cliente entra no sistema
2. Veículo é selecionado
3. Orçamento é criado
4. Itens são adicionados
5. Orçamento é enviado ao cliente
6. Cliente aprova ou recusa
7. Se aprovado, converte em OS

### 6.2 Fluxo de Ordem de Serviço
1. OS é aberta (do orçamento ou direto)
2. Diagnóstico é realizado
3. Serviços são executados
4. Peças são usadas
5. OS é finalizada
6. Venda é gerada
7. Pagamento é registrado
8. Veículo é entregue

### 6.3 Fluxo de Venda no PDV
1. Cliente entra no sistema
2. Produtos são buscados
3. Itens são adicionados
4. Pagamento é realizado
5. Comprovante é emitido
6. Estoque é baixado
7. Caixa é atualizado

---

## 7. PERMISSÕES POR MÓDULO

### 7.1 Administrador
- Acesso total a todos os módulos
- Pode gerenciar usuários
- Pode gerenciar configurações

### 7.2 Gerente
- Acesso a todos os módulos exceto configurações
- Pode visualizar relatórios financeiros

### 7.3 Mecânico
- Acesso a OS, veículos, agendamentos
- Pode visualizar histórico de clientes

### 7.4 Vendedor
- Acesso a clientes, orçamentos, PDV
- Pode criar orçamentos
- Pode realizar vendas

### 7.5 Caixa
- Acesso a PDV, caixa
- Pode abrir e fechar caixa
- Pode realizar vendas

### 7.6 Almoxarife
- Acesso a estoque, fornecedores
- Pode gerenciar produtos
- Pode importar NF-e

---

**Última atualização:** 2026-06-10
