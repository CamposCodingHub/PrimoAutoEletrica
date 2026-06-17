# Banco de Dados - PrimoAutoEletrica

**Data:** 2026-06-09  
**Projeto:** PrimoAutoEletrica  
**Versão:** 1.0  
**Tipo:** SQLite

---

## 1. Visão Geral

O sistema utiliza SQLite como banco de dados principal. O arquivo do banco é armazenado em:
```
%LocalApplicationData%\PrimoAutoEletrica\database.db
```

O acesso ao banco é gerenciado pelo `DatabaseService` e seus arquivos parciais.

---

## 2. Tabelas

### 2.1 Clientes

**Nome:** `Clientes`

**Campos:**
- `Id` (TEXT, PK) - Identificador único (GUID)
- `Nome` (TEXT) - Nome do cliente
- `TipoPessoa` (TEXT) - `Fisica` ou `Juridica`
- `CPF` (TEXT) - CPF/CNPJ normalizado do cliente
- `RG` (TEXT) - RG/IE do cliente
- `DataNascimento` (TEXT) - Data de nascimento
- `Telefone` (TEXT) - Telefone
- `WhatsApp` (TEXT) - WhatsApp
- `Email` (TEXT) - Email
- `CEP` (TEXT) - CEP
- `Rua` (TEXT) - Rua
- `Numero` (TEXT) - Número
- `Bairro` (TEXT) - Bairro
- `Cidade` (TEXT) - Cidade
- `Estado` (TEXT) - Estado
- `Ativo` (INTEGER) - Status ativo (0/1)
- `ClienteVip` (INTEGER) - Cliente VIP (0/1)
- `TotalGasto` (REAL) - Total gasto
- `TotalServicos` (INTEGER) - Total de serviços
- `PontosFidelidade` (INTEGER) - Pontos de fidelidade
- `ConsentimentoLGPD` (INTEGER) - Consentimento LGPD (0/1)
- `DataConsentimentoLGPD` (TEXT) - Data do consentimento
- `OrigemConsentimentoLGPD` (TEXT) - Origem do consentimento
- `AutorizaContatoWhatsApp` (INTEGER) - Autoriza contato WhatsApp (0/1)
- `Observacoes` (TEXT) - Observações
- `CaminhoDocumento` (TEXT) - Caminho do documento
- `CaminhoAssinatura` (TEXT) - Caminho da assinatura
- `DataCadastro` (TEXT) - Data de cadastro
- `UltimaVisita` (TEXT) - Última visita
- `ImagemUrl` (TEXT) - URL da imagem

**Chave Primária:** `Id`

**Relacionamentos:**
- Um cliente pode ter muitos veículos
- Um cliente pode ter muitos orçamentos
- Um cliente pode ter muitas ordens de serviço
- Um cliente pode ter muitas vendas

**Uso no Sistema:**
- Cadastro de clientes
- Histórico de serviços
- Fidelidade
- LGPD

---

### 2.2 Veículos

**Nome:** `Veiculos`

**Campos:**
- `Id` (TEXT, PK) - Identificador único (GUID)
- `ClienteId` (TEXT, FK) - ID do cliente
- `Placa` (TEXT) - Placa do veículo
- `Marca` (TEXT) - Marca
- `Modelo` (TEXT) - Modelo
- `Ano` (INTEGER) - Ano
- `Cor` (TEXT) - Cor
- `Chassi` (TEXT) - Chassi
- `Renavam` (TEXT) - Renavam
- `Tipo` (TEXT) - Tipo (carro, moto, caminhão, etc.)
- `SistemaEletrico` (TEXT) - Sistema elétrico (12V, 24V, híbrido)
- `Combustivel` (TEXT) - Combustível
- `Quilometragem` (INTEGER) - Quilometragem
- `Observacoes` (TEXT) - Observações

**Chave Primária:** `Id`

**Chave Estrangeira:** `ClienteId` → `Clientes.Id`

**Relacionamentos:**
- Um veículo pertence a um cliente
- Um veículo pode ter muitas ordens de serviço

**Uso no Sistema:**
- Cadastro de veículos
- Histórico de serviços por veículo
- Diagnóstico elétrico

---

### 2.3 Produtos

**Nome:** `Produtos`

**Campos:**
- `Id` (TEXT, PK) - Identificador único (GUID)
- `Codigo` (TEXT) - Código interno
- `Nome` (TEXT) - Nome
- `Descricao` (TEXT) - Descrição
- `Categoria` (TEXT) - Categoria
- `Marca` (TEXT) - Marca
- `Modelo` (TEXT) - Modelo
- `FornecedorId` (TEXT, FK) - ID do fornecedor
- `Fornecedor` (TEXT) - Nome do fornecedor
- `CNPJFornecedor` (TEXT) - CNPJ do fornecedor
- `ContatoFornecedor` (TEXT) - Contato do fornecedor
- `TelefoneFornecedor` (TEXT) - Telefone do fornecedor
- `QuantidadeEstoque` (INTEGER) - Quantidade em estoque
- `QuantidadeMinima` (INTEGER) - Quantidade mínima
- `QuantidadeMaxima` (INTEGER) - Quantidade máxima
- `Localizacao` (TEXT) - Localização física
- `Prateleira` (TEXT) - Prateleira
- `Gaveta` (TEXT) - Gaveta
- `PrecoCompra` (REAL) - Preço de compra
- `PrecoVenda` (REAL) - Preço de venda
- `MargemLucro` (REAL) - Margem de lucro
- `ValorTotalEstoque` (REAL) - Valor total do estoque
- `UnidadeMedida` (TEXT) - Unidade de medida
- `Peso` (TEXT) - Peso
- `Dimensoes` (TEXT) - Dimensões
- `Cor` (TEXT) - Cor
- `Material` (TEXT) - Material
- `CodigoBarras` (TEXT) - Código de barras
- `SKU` (TEXT) - SKU
- `NCMS` (TEXT) - NCM
- `CEST` (TEXT) - CEST
- `CFOP` (TEXT) - CFOP
- `Ativo` (INTEGER) - Status ativo (0/1)
- `ProdutoPerecivel` (INTEGER) - Produto perecível (0/1)
- `DataValidade` (TEXT) - Data de validade
- `DataFabricacao` (TEXT) - Data de fabricação
- `Lote` (TEXT) - Lote
- `DataCadastro` (TEXT) - Data de cadastro
- `DataUltimaCompra` (TEXT) - Data da última compra
- `DataUltimaVenda` (TEXT) - Data da última venda
- `DataUltimaAtualizacao` (TEXT) - Data da última atualização
- `Observacoes` (TEXT) - Observações
- `ImagemUrl` (TEXT) - URL da imagem
- `Anexos` (TEXT) - Anexos
- `TotalVendas` (INTEGER) - Total de vendas
- `TotalFaturado` (REAL) - Total faturado
- `VendasUltimoMes` (INTEGER) - Vendas último mês
- `VendasUltimoTrimestre` (INTEGER) - Vendas último trimestre
- `QuantidadeReservada` (INTEGER) - Quantidade reservada

**Chave Primária:** `Id`

**Chave Estrangeira:** `FornecedorId` → `Fornecedores.Id`

**Relacionamentos:**
- Um produto pode ter muitos fornecedores
- Um produto pode estar em muitas vendas
- Um produto pode estar em muitas ordens de serviço

**Uso no Sistema:**
- Cadastro de produtos
- Controle de estoque
- Vendas
- Orçamentos
- Ordens de serviço

---

### 2.4 Fornecedores

**Nome:** `Fornecedores`

**Campos:**
- `Id` (TEXT, PK) - Identificador único (GUID)
- `Nome` (TEXT) - Nome/Razão social
- `CNPJ` (TEXT) - CNPJ
- `IE` (TEXT) - Inscrição estadual
- `Telefone` (TEXT) - Telefone
- `WhatsApp` (TEXT) - WhatsApp
- `Email` (TEXT) - Email
- `Endereco` (TEXT) - Endereço
- `ContatoResponsavel` (TEXT) - Contato responsável
- `PrazoMedioPagamentoDias` (INTEGER) - Prazo medio de pagamento (dias)
- `Observacoes` (TEXT) - Observações
- `DataCadastro` (TEXT) - Data de cadastro

**Chave Primária:** `Id`

**Relacionamentos:**
- Um fornecedor pode fornecer muitos produtos
- Um fornecedor pode ter muitas notas fiscais

**Uso no Sistema:**
- Cadastro de fornecedores
- Importação de NF-e
- Contas a pagar

---

### 2.4.1 Contas a pagar

**Nome:** `ContasPagar`

**Campos principais:**
- `Id` (INTEGER, PK) - Identificador sequencial
- `Fornecedor` (TEXT) - Fornecedor ou favorecido
- `Descricao` (TEXT) - Descrição do compromisso
- `Valor` (REAL) - Valor a pagar
- `DataVencimento` (TEXT) - Data de vencimento
- `DataPagamento` (TEXT) - Data de baixa, quando pago
- `Status` (TEXT) - `Pendente`, `Pago`, `Liquidado` ou `Cancelado`
- `Categoria` (TEXT) - Categoria financeira
- `Observacoes` (TEXT) - Observações operacionais
- `DataCriacao` (TEXT) - Data de criação
- `Origem` (TEXT) - Origem da integração, por exemplo `ImportacaoNFeContaPagar`
- `ReferenciaExterna` (TEXT) - Referência idempotente da integração, usando a chave da NF-e quando disponível

**Índices:**
- `IX_ContasPagar_Integracao` - Índice único parcial em `Origem` + `ReferenciaExterna` para evitar duplicidade de contas geradas por NF-e.

**Uso no Sistema:**
- Contas a pagar manuais
- Contas a pagar geradas automaticamente na importação de NF-e
- Relatórios de compromissos financeiros

---

### 2.5 Funcionários

**Nome:** `Funcionarios`

**Campos:**
- `Id` (INTEGER, PK) - Identificador único
- `Nome` (TEXT) - Nome
- `CPF` (TEXT) - CPF
- `Email` (TEXT) - Email
- `Senha` (TEXT) - Senha (hash)
- `Funcao` (TEXT) - Função
- `Cargo` (TEXT) - Cargo
- `PerfilAcesso` (TEXT) - Perfil de acesso
- `Telefone` (TEXT) - Telefone
- `Foto` (TEXT) - Foto
- `DataAdmissao` (TEXT) - Data de admissão
- `Salario` (REAL) - Salário
- `Status` (TEXT) - Status (`Aberta`, `Em diagnostico`, `Aguardando aprovacao`, `Aguardando peca`, `Em execucao`, `Finalizada`, `Aguardando pagamento`, `Entregue`, `Cancelada`)
- `Observacoes` (TEXT) - Observacoes operacionais
- `DataCadastro` (TEXT) - Data de cadastro
- `DataUltimoLogin` (TEXT) - Data do último login
- `ExigirTrocaSenha` (INTEGER) - Obriga troca de senha no próximo login quando 1
- `Ativo` (INTEGER) - Status ativo (0/1)

**Chave Primária:** `Id`

**Relacionamentos:**
- Um funcionário pode executar muitas ordens de serviço
- Um funcionário pode operar muitos caixas

**Uso no Sistema:**
- Cadastro de funcionários
- Controle de acesso
- Permissões
- Login

---

### 2.6 Orçamentos

**Nome:** `Orcamentos`

**Campos:**
- `Id` (TEXT, PK) - Identificador único (GUID)
- `ClienteId` (TEXT, FK) - ID do cliente
- `VeiculoId` (TEXT, FK) - ID do veículo
- `VendedorId` (TEXT, FK) - ID do vendedor, quando informado
- `Numero` (TEXT) - Número do orçamento
- `DataCriacao` (TEXT) - Data de criação
- `DataValidade` (TEXT) - Data de validade
- `Status` (TEXT) - Status (`Rascunho`, `Enviado`, `Aprovado`, `Recusado`, `Vencido`, `Convertido em Venda`, `Convertido em OS`)
- `DataConversaoVenda` (TEXT) - Data de conversão em venda/PDV
- `DataConversaoOrdemServico` (TEXT) - Data de conversão em OS
- `OrdemServicoId` (TEXT, FK) - OS gerada a partir do orçamento
- `Subtotal` (REAL) - Subtotal
- `Desconto` (REAL) - Desconto final aplicado em reais
- `DescontoTipo` (TEXT) - `Valor` ou `Percentual`
- `DescontoPercentual` (REAL) - Percentual informado quando `DescontoTipo = Percentual`
- `Acrescimo` (REAL) - Acréscimos comerciais
- `Total` (REAL) - Total
- `Observacoes` (TEXT) - Observações
- `Diagnostico` (TEXT) - Diagnóstico técnico informado na proposta
- `CondicoesPagamento` (TEXT) - Condições comerciais de pagamento
- `PrazoEntrega` (TEXT) - Prazo de entrega
- `DataAprovacao` (TEXT) - Data de aprovação

**Chave Primária:** `Id`

**Chaves Estrangeiras:**
- `ClienteId` → `Clientes.Id`
- `VeiculoId` → `Veiculos.Id`

**Relacionamentos:**
- Um orçamento pertence a um cliente
- Um orçamento pertence a um veículo
- Um orçamento pode ter muitos itens
- Um orçamento pode ser convertido em ordem de serviço

**Uso no Sistema:**
- Criação de orçamentos
- Aprovação de orçamentos
- Envio por WhatsApp e geração de PDF pela camada de serviço
- Conversão em ordem de serviço

---

### 2.7 OrdemServico

**Nome:** `OrdemServico`

**Campos:**
- `Id` (TEXT, PK) - Identificador único (GUID)
- `Numero` (TEXT) - Número da OS
- `ClienteId` (TEXT, FK) - ID do cliente
- `VeiculoId` (TEXT, FK) - ID do veículo
- `TecnicoId` (INTEGER, FK) - ID do técnico
- `DataEntrada` (TEXT) - Data de entrada
- `PrevisaoEntrega` (TEXT) - Previsão de entrega
- `DataConclusao` (TEXT) - Data de conclusão
- `Status` (TEXT) - Status
- `DefeitoReclamado` (TEXT) - Defeito reclamado
- `Diagnostico` (TEXT) - Diagnóstico
- `ServicosExecutados` (TEXT) - Serviços executados
- `Observacoes` (TEXT) - Observações
- `Subtotal` (REAL) - Subtotal
- `Desconto` (REAL) - Desconto
- `Total` (REAL) - Total
- `Garantia` (TEXT) - Garantia
- `TermoAutorizacao` (TEXT) - Termo de autorização do cliente
- `DataCadastro` (TEXT) - Data de cadastro

**Chave Primária:** `Id`

**Chaves Estrangeiras:**
- `ClienteId` → `Clientes.Id`
- `VeiculoId` → `Veiculos.Id`
- `TecnicoId` → `Funcionarios.Id`

**Relacionamentos:**
- Uma ordem de serviço pertence a um cliente
- Uma ordem de serviço pertence a um veículo
- Uma ordem de serviço é executada por um técnico
- Uma ordem de serviço pode ter muitos itens
- Uma ordem de serviço pode ter muitos eventos

**Uso no Sistema:**
- Criação de ordens de serviço
- Acompanhamento de status
- Histórico de serviços

---

### 2.8 Vendas

**Nome:** `Vendas`

**Campos:**
- `Id` (TEXT, PK) - Identificador único (GUID)
- `Numero` (TEXT) - Número da venda
- `ClienteId` (TEXT, FK) - ID do cliente
- `OrdemServicoId` (TEXT, FK) - ID da ordem de serviço
- `DataVenda` (TEXT) - Data da venda
- `Subtotal` (REAL) - Subtotal
- `Desconto` (REAL) - Desconto
- `Total` (REAL) - Total
- `FormaPagamento` (TEXT) - Forma de pagamento
- `Status` (TEXT) - Status
- `Observacoes` (TEXT) - Observações
- `OperadorId` (INTEGER, FK) - ID do operador
- `CaixaId` (INTEGER, FK) - ID do caixa

**Chave Primária:** `Id`

**Chaves Estrangeiras:**
- `ClienteId` → `Clientes.Id`
- `OrdemServicoId` → `OrdemServico.Id`
- `OperadorId` → `Funcionarios.Id`
- `CaixaId` → `CaixaOperacional.Id`

**Relacionamentos:**
- Uma venda pode pertencer a um cliente
- Uma venda pode estar vinculada a uma ordem de serviço
- Uma venda é realizada por um operador
- Uma venda pertence a um caixa
- Uma venda pode ter muitos itens

**Uso no Sistema:**
- PDV
- Vendas
- Caixa
- Financeiro

---

### 2.9 Agendamentos

**Nome:** `Agendamentos`

**Campos:**
- `Id` (TEXT, PK) - Identificador único (GUID)
- `ClienteId` (TEXT, FK) - ID do cliente
- `VeiculoId` (TEXT, FK) - ID do veículo
- `TecnicoId` (INTEGER, FK) - ID do técnico
- `DataAgendamento` (TEXT) - Data do agendamento
- `Horario` (TEXT) - Horário
- `ServicoPrevisto` (TEXT) - Serviço previsto
- `Status` (TEXT) - Status
- `Confirmado` (INTEGER) - Confirmado (0/1)
- `Observacoes` (TEXT) - Observações
- `DataCadastro` (TEXT) - Data de cadastro

**Chave Primária:** `Id`

**Chaves Estrangeiras:**
- `ClienteId` → `Clientes.Id`
- `VeiculoId` → `Veiculos.Id`
- `TecnicoId` → `Funcionarios.Id`

**Relacionamentos:**
- Um agendamento pertence a um cliente
- Um agendamento pertence a um veículo
- Um agendamento é atribuído a um técnico

**Uso no Sistema:**
- Agenda
- Agendamentos
- Lembretes

---

### 2.10 CaixaOperacional

**Nome:** `CaixaOperacional`

**Campos:**
- `Id` (INTEGER, PK) - Identificador único
- `Numero` (TEXT) - Número do caixa
- `DataAbertura` (TEXT) - Data de abertura
- `DataFechamento` (TEXT) - Data de fechamento
- `SaldoInicial` (REAL) - Saldo inicial
- `SaldoFinal` (REAL) - Saldo final
- `TotalEntradas` (REAL) - Total de entradas
- `TotalSaidas` (REAL) - Total de saídas
- `OperadorId` (INTEGER, FK) - ID do operador
- `Status` (TEXT) - Status (aberto, fechado)
- `Observacoes` (TEXT) - Observações

**Chave Primária:** `Id`

**Chave Estrangeira:** `OperadorId` → `Funcionarios.Id`

**Relacionamentos:**
- Um caixa é operado por um funcionário
- Um caixa pode ter muitas vendas
- Um caixa pode ter muitas operações

**Uso no Sistema:**
- Abertura/fechamento de caixa
- Sangria/suprimento
- Controle de fluxo de caixa

---

### 2.11 PerfilAcesso

**Nome:** `PerfilAcesso`

**Campos:**
- `Id` (INTEGER, PK) - Identificador único
- `Nome` (TEXT) - Nome do perfil
- `Descricao` (TEXT) - Descrição
- `Ativo` (INTEGER) - Status ativo (0/1)

**Chave Primária:** `Id`

**Relacionamentos:**
- Um perfil pode ter muitas permissões
- Um perfil pode ter muitos funcionários

**Uso no Sistema:**
- Controle de acesso
- Permissões

---

### 2.12 Permissoes

**Nome:** `Permissoes`

**Campos:**
- `Id` (INTEGER, PK) - Identificador único
- `Nome` (TEXT) - Nome da permissão
- `Descricao` (TEXT) - Descrição
- `Modulo` (TEXT) - Módulo
- `Acao` (TEXT) - Ação (ver, criar, editar, excluir)
- `Codigo` (TEXT) - Código único usado por `PermissionService`
- `Ativo` (INTEGER) - Status ativo (0/1)
- `Essencial` (INTEGER) - Permissão mínima de visualização/acesso

**Chave Primária:** `Id`

**Relacionamentos:**
- Uma permissão pode estar em muitos perfis

**Uso no Sistema:**
- Controle de acesso
- Permissões granulares

---

### 2.13 PerfisAcesso e PerfilPermissoes

**Nomes:** `PerfisAcesso`, `PerfilPermissoes`

**Campos:**
- `PerfisAcesso.Id` (INTEGER, PK) - ID do perfil
- `PerfisAcesso.Nome` (TEXT) - Nome do perfil
- `PerfilId` (INTEGER, FK) - ID do perfil
- `PermissaoId` (INTEGER, FK) - ID da permissão
- `Concedida` (INTEGER) - Permissão concedida quando 1
- `Ativa` (INTEGER) - Vínculo ativo quando 1

**Chave Única:** (`PerfilId`, `PermissaoId`)

**Chaves Estrangeiras:**
- `PerfilId` → `PerfisAcesso.Id`
- `PermissaoId` → `Permissoes.Id`

**Relacionamentos:**
- Tabela de relacionamento muitos-para-muitos entre perfis e permissões

**Uso no Sistema:**
- Controle de acesso
- Permissões

---

### 2.14 Auditoria

**Nome:** `Auditoria`

**Campos:**
- `Id` (INTEGER, PK) - Identificador único
- `DataHora` (TEXT) - Data e hora
- `Usuario` (TEXT) - Usuário
- `Acao` (TEXT) - Ação
- `Modulo` (TEXT) - Módulo
- `Detalhes` (TEXT) - Detalhes
- `Ip` (TEXT) - IP

**Chave Primária:** `Id`

**Uso no Sistema:**
- Log de auditoria
- Rastreabilidade de ações

---

## 3. Índices

### 3.1 Índices Principais

- `Clientes.Id` - Índice único
- `Clientes.CPF` - Índice único
- `Clientes.Email` - Índice único
- `Veiculos.Id` - Índice único
- `Veiculos.Placa` - Índice único
- `Produtos.Id` - Índice único
- `Produtos.Codigo` - Índice único
- `Produtos.CodigoBarras` - Índice único
- `Fornecedores.Id` - Índice único
- `Fornecedores.CNPJ` - Índice único
- `Funcionarios.Id` - Índice único
- `Funcionarios.Email` - Índice único
- `Orcamentos.Id` - Índice único
- `Orcamentos.Numero` - Índice único
- `OrdemServico.Id` - Índice único
- `OrdemServico.Numero` - Índice único
- `Vendas.Id` - Índice único
- `Vendas.Numero` - Índice único
- `Agendamentos.Id` - Índice único
- `CaixaOperacional.Id` - Índice único
- `CaixaOperacional.Numero` - Índice único

---

## 4. Relacionamentos

### 4.1 Relacionamentos Um-para-Muitos

- **Clientes → Veiculos** (1:N)
- **Clientes → Orcamentos** (1:N)
- **Clientes → OrdemServico** (1:N)
- **Clientes → Vendas** (1:N)
- **Clientes → Agendamentos** (1:N)
- **Veiculos → OrdemServico** (1:N)
- **Fornecedores → Produtos** (1:N)
- **Funcionarios → OrdemServico** (1:N)
- **Funcionarios → Vendas** (1:N)
- **Funcionarios → CaixaOperacional** (1:N)
- **Funcionarios → Agendamentos** (1:N)
- **Produtos → Vendas** (1:N)
- **Produtos → OrdemServico** (1:N)
- **Orcamentos → OrcamentoItem** (1:N)
- **OrdemServico → OrdemServicoItem** (1:N)
- **OrdemServico → OrdemServicoEvento** (1:N)
- **Vendas → ItemVenda** (1:N)
- **CaixaOperacional → Vendas** (1:N)

### 4.2 Relacionamentos Muitos-para-Muitos

- **PerfilAcesso ↔ Permissao** (via `PerfilPermissao`)

---

## 5. Observações Importantes

1. **Tipo de Dados:** O SQLite não tem tipos nativos para GUID, DATE, BOOLEAN. São usados TEXT para GUID/DATE e INTEGER para BOOLEAN.
2. **Chaves Estrangeiras:** O SQLite não impõe restrições de chave estrangeira por padrão. A integridade referencial é mantida pela aplicação.
3. **Índices:** Índices são criados para melhorar performance em consultas frequentes.
4. **Backup:** O backup é feito copiando o arquivo do banco de dados.
5. **Migrações:** As migrações são gerenciadas pelo `DatabaseService.Migrations.cs`.

---

## 6. Controle de Versão do Banco

### 6.1 SchemaVersion

**Nome:** `SchemaVersion`

**Campos:**
- `Id` (INTEGER, PK, AUTOINCREMENT) - Identificador sequencial da versão aplicada.
- `Version` (TEXT, UNIQUE, NOT NULL) - Código da migration ou versão lógica aplicada.
- `AppliedAt` (TEXT, NOT NULL) - Data/hora da aplicação da versão.
- `Description` (TEXT, NOT NULL) - Descrição operacional da alteração.

**Uso no Sistema:**
- Permite saber a versão de schema existente no banco do cliente.
- Espelha as migrations aplicadas por `SchemaMigrations`.
- Dá base para atualizações futuras sem perda de dados.

### 6.2 SchemaMigrations

**Nome:** `SchemaMigrations`

**Campos:**
- `Id` (TEXT, PK) - Código da migration aplicada pelo runtime.
- `Descricao` (TEXT, NOT NULL) - Descrição da migration.
- `AplicadaEm` (TEXT, NOT NULL) - Data/hora da aplicação.
- `VersaoAplicacao` (TEXT) - Versão da aplicação que aplicou a migration.
- `Maquina` (TEXT) - Estação onde a migration foi aplicada.

**Uso no Sistema:**
- Controle interno usado por `DatabaseService.Migrations.cs`.
- Evita reaplicar alterações já executadas.
- Alimenta automaticamente a tabela `SchemaVersion`.

## 7. Migrações Controladas

Estrutura criada:

- `Migrations/001_Initial.sql`
- `Migrations/002_AddPermissions.sql`
- `Migrations/003_AddWorkshopKanban.sql`
- `Migrations/004_AddLoginSecurity.sql`
- `Migrations/005_AddClienteTipoPessoa.sql`
- `Migrations/006_AddOrcamentoVeiculoDiagnosticoDesconto.sql`
- `Migrations/007_AddOrdemServicoTermoAutorizacao.sql`
- `Migrations/008_AddContasPagarIntegracaoNFe.sql`
- `Migrations/009_AddFornecedorPrazoMedioPagamento.sql`
- `Migrations/010_AddFuncionarioObservacoes.sql`

O runtime atual continua aplicando migrations por código em `DatabaseService.Migrations.cs` e em serviços especializados como `FinanceiroDatabaseService`, porque várias alterações são condicionais e precisam consultar tabelas/colunas existentes antes de executar `ALTER TABLE`.

Critérios cobertos:

- Banco novo cria `SchemaMigrations` e `SchemaVersion`.
- Banco antigo com `SchemaMigrations` passa a sincronizar `SchemaVersion` sem reaplicar migrations.
- Teste `MigrationSchemaTests` cria banco temporário, inicializa `DatabaseService` e valida tabela/campos/versões aplicadas.

## 8. Backup, Restauração e Integridade

Serviço principal: `DatabaseBackupService`.

Operações disponíveis:

- `CriarBackupManual`: gera backup sob demanda.
- `CriarBackupAutomaticoDiario`: gera backup diário no startup.
- `CriarBackupAntesMigracao`: gera backup verificado antes de migration.
- `CriarBackupAntesAtualizacao`: gera backup verificado antes de atualização.
- `RestaurarBackup`: valida integridade, cria backup de segurança e restaura.
- `VerificarBackup` e `VerificarIntegridade`: executam `PRAGMA integrity_check`.

Tela operacional:

- `ConfiguracoesSistemaWindow` possui aba de backup manual, restauração segura, validação de caminho `.db`, bloqueio contra restaurar o banco atual e mensagem clara para o usuário.

## 9. Proteção de Dados Reais

Proteções existentes:

- Confirmações críticas centralizadas em fluxos sensíveis.
- Auditoria para login, negação de permissão, backup, restauração e operações críticas.
- Funcionários são inativados em vez de removidos fisicamente.
- Clientes com ordens de serviço são bloqueados contra exclusão física pelo repository.
- PDV e fornecedores possuem fluxos testados de exclusão/cancelamento controlado.
- Validações de CPF/CNPJ/placa duplicados são tratadas nos cadastros e repositories dedicados.
- Segurança e permissões estão detalhadas em `Docs/MATRIZ_PERMISSOES_SEGURANCA.md`.

Diretriz mantida:

- Novos fluxos devem preferir `Ativo = false`/inativação quando houver histórico operacional ou fiscal associado.

---

**Gerado automaticamente para FASE 5.1 - Criar documentação do banco**
