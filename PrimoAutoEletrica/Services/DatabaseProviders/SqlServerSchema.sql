-- Script de criação do schema SQL Server para Primo Auto Eletrica
-- Baseado no schema SQLite existente

-- Tabela Funcionarios
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Funcionarios')
BEGIN
    CREATE TABLE Funcionarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        CPF NVARCHAR(20),
        Email NVARCHAR(200) NOT NULL UNIQUE,
        Senha NVARCHAR(500) NOT NULL,
        Funcao NVARCHAR(100) NOT NULL,
        PerfilAcesso NVARCHAR(50) NOT NULL DEFAULT 'Mecânico',
        Telefone NVARCHAR(30),
        DataAdmissao DATETIME NOT NULL,
        Salario DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Ativo',
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        DataUltimoLogin DATETIME,
        Ativo BIT NOT NULL DEFAULT 1
    );
    
    CREATE INDEX IX_Funcionarios_Email ON Funcionarios(Email);
    CREATE INDEX IX_Funcionarios_CPF ON Funcionarios(CPF);
END

-- Tabela Clientes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Clientes')
BEGIN
    CREATE TABLE Clientes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        CPF NVARCHAR(20),
        CNPJ NVARCHAR(20),
        Email NVARCHAR(200),
        Telefone NVARCHAR(30),
        Celular NVARCHAR(30),
        Endereco NVARCHAR(500),
        Bairro NVARCHAR(100),
        Cidade NVARCHAR(100),
        Estado NVARCHAR(2),
        CEP NVARCHAR(10),
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedBy INT,
        RowVersion TIMESTAMP NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1
    );
    
    CREATE INDEX IX_Clientes_CPF ON Clientes(CPF);
    CREATE INDEX IX_Clientes_CNPJ ON Clientes(CNPJ);
    CREATE INDEX IX_Clientes_Email ON Clientes(Email);
END

-- Tabela Produtos
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Produtos')
BEGIN
    CREATE TABLE Produtos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Codigo NVARCHAR(50) NOT NULL UNIQUE,
        Nome NVARCHAR(200) NOT NULL,
        Descricao NVARCHAR(1000),
        Categoria NVARCHAR(100),
        Marca NVARCHAR(100),
        Modelo NVARCHAR(100),
        Cor NVARCHAR(80),
        Material NVARCHAR(120),
        Anexos NVARCHAR(MAX),
        Unidade NVARCHAR(20) NOT NULL DEFAULT 'UN',
        PrecoCusto DECIMAL(18,2),
        PrecoVenda DECIMAL(18,2) NOT NULL,
        EstoqueAtual INT NOT NULL DEFAULT 0,
        EstoqueMinimo INT NOT NULL DEFAULT 0,
        EstoqueMaximo INT NOT NULL DEFAULT 0,
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedBy INT,
        RowVersion TIMESTAMP NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1
    );
    
    CREATE INDEX IX_Produtos_Codigo ON Produtos(Codigo);
    CREATE INDEX IX_Produtos_Nome ON Produtos(Nome);
    CREATE INDEX IX_Produtos_Categoria ON Produtos(Categoria);
END

-- Tabela Fornecedores
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Fornecedores')
BEGIN
    CREATE TABLE Fornecedores (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        CNPJ NVARCHAR(20),
        Email NVARCHAR(200),
        Telefone NVARCHAR(30),
        Celular NVARCHAR(30),
        Endereco NVARCHAR(500),
        Bairro NVARCHAR(100),
        Cidade NVARCHAR(100),
        Estado NVARCHAR(2),
        CEP NVARCHAR(10),
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedBy INT,
        RowVersion TIMESTAMP NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1
    );
    
    CREATE INDEX IX_Fornecedores_CNPJ ON Fornecedores(CNPJ);
    CREATE INDEX IX_Fornecedores_Email ON Fornecedores(Email);
END

-- Tabela ContatosFornecedor
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContatosFornecedor')
BEGIN
    CREATE TABLE ContatosFornecedor (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FornecedorId INT NOT NULL,
        Nome NVARCHAR(200) NOT NULL,
        Cargo NVARCHAR(100),
        Email NVARCHAR(200),
        Telefone NVARCHAR(30),
        Celular NVARCHAR(30),
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (FornecedorId) REFERENCES Fornecedores(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_ContatosFornecedor_FornecedorId ON ContatosFornecedor(FornecedorId);
END

-- Tabela ImportacoesNFe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportacoesNFe')
BEGIN
    CREATE TABLE ImportacoesNFe (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ChaveNFe NVARCHAR(100) NOT NULL UNIQUE,
        NumeroNFe INT NOT NULL,
        SerieNFe INT NOT NULL,
        DataEmissao DATETIME NOT NULL,
        DataEntrada DATETIME NOT NULL,
        FornecedorId INT,
        ValorTotal DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pendente',
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (FornecedorId) REFERENCES Fornecedores(Id)
    );
    
    CREATE INDEX IX_ImportacoesNFe_ChaveNFe ON ImportacoesNFe(ChaveNFe);
    CREATE INDEX IX_ImportacoesNFe_FornecedorId ON ImportacoesNFe(FornecedorId);
END

-- Tabela Vendas
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Vendas')
BEGIN
    CREATE TABLE Vendas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Numero NVARCHAR(50) NOT NULL UNIQUE,
        ClienteId INT NOT NULL,
        DataVenda DATETIME NOT NULL DEFAULT GETDATE(),
        ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        Desconto DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pendente',
        FormaPagamento NVARCHAR(50),
        Observacoes NVARCHAR(1000),
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedBy INT,
        RowVersion TIMESTAMP NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
    );
    
    CREATE INDEX IX_Vendas_Numero ON Vendas(Numero);
    CREATE INDEX IX_Vendas_ClienteId ON Vendas(ClienteId);
    CREATE INDEX IX_Vendas_DataVenda ON Vendas(DataVenda);
END

-- Tabela VendaItens
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VendaItens')
BEGIN
    CREATE TABLE VendaItens (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        VendaId INT NOT NULL,
        ProdutoId INT NOT NULL,
        Quantidade INT NOT NULL,
        PrecoUnitario DECIMAL(18,2) NOT NULL,
        Desconto DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorTotal DECIMAL(18,2) NOT NULL,
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (VendaId) REFERENCES Vendas(Id) ON DELETE CASCADE,
        FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id)
    );
    
    CREATE INDEX IX_VendaItens_VendaId ON VendaItens(VendaId);
    CREATE INDEX IX_VendaItens_ProdutoId ON VendaItens(ProdutoId);
END

-- Tabela Veiculos
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Veiculos')
BEGIN
    CREATE TABLE Veiculos (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        ClienteId UNIQUEIDENTIFIER NOT NULL,
        Marca NVARCHAR(100),
        Modelo NVARCHAR(100),
        Ano NVARCHAR(10),
        Cor NVARCHAR(50),
        Placa NVARCHAR(20),
        Chassi NVARCHAR(50),
        Renavam NVARCHAR(20),
        Motor NVARCHAR(50),
        Combustivel NVARCHAR(30),
        Quilometragem INT NOT NULL DEFAULT 0,
        Observacoes NVARCHAR(1000),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedBy INT,
        RowVersion TIMESTAMP NOT NULL,
        FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
    );
    
    CREATE INDEX IX_Veiculos_ClienteId ON Veiculos(ClienteId);
    CREATE INDEX IX_Veiculos_Placa ON Veiculos(Placa);
END

-- Tabela OrdensServico
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrdensServico')
BEGIN
    CREATE TABLE OrdensServico (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        Numero NVARCHAR(50) NOT NULL UNIQUE,
        ClienteId INT NOT NULL,
        VeiculoId UNIQUEIDENTIFIER,
        TecnicoId INT,
        AgendamentoId NVARCHAR(50),
        ClienteNomeSnapshot NVARCHAR(200),
        TelefoneClienteSnapshot NVARCHAR(30),
        VeiculoDescricaoSnapshot NVARCHAR(200),
        PlacaSnapshot NVARCHAR(20),
        Status NVARCHAR(20) NOT NULL,
        Prioridade NVARCHAR(20) NOT NULL,
        Origem NVARCHAR(50),
        ProblemaRelatado NVARCHAR(1000),
        Diagnostico NVARCHAR(1000),
        ObservacoesInternas NVARCHAR(1000),
        ObservacoesCliente NVARCHAR(1000),
        ChecklistEntrega NVARCHAR(1000),
        GarantiaObservacoes NVARCHAR(1000),
        AprovadaCliente BIT NOT NULL DEFAULT 0,
        MetodoAprovacao NVARCHAR(50),
        DataAbertura DATETIME NOT NULL DEFAULT GETDATE(),
        DataPrevisao DATETIME,
        DataAprovacao DATETIME,
        DataInicio DATETIME,
        DataConclusao DATETIME,
        DataEntrega DATETIME,
        GarantiaValidaAte DATETIME,
        ValorMaoObra DECIMAL(18,2) NOT NULL DEFAULT 0,
        Desconto DECIMAL(18,2) NOT NULL DEFAULT 0,
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedBy INT,
        RowVersion TIMESTAMP NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (ClienteId) REFERENCES Clientes(Id),
        FOREIGN KEY (TecnicoId) REFERENCES Funcionarios(Id),
        FOREIGN KEY (VeiculoId) REFERENCES Veiculos(Id)
    );
    
    CREATE INDEX IX_OrdensServico_Numero ON OrdensServico(Numero);
    CREATE INDEX IX_OrdensServico_ClienteId ON OrdensServico(ClienteId);
    CREATE INDEX IX_OrdensServico_VeiculoId ON OrdensServico(VeiculoId);
    CREATE INDEX IX_OrdensServico_Status ON OrdensServico(Status);
    CREATE INDEX IX_OrdensServico_DataAbertura ON OrdensServico(DataAbertura);
END

-- Tabela OrdemServicoItens
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrdemServicoItens')
BEGIN
    CREATE TABLE OrdemServicoItens (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrdemServicoId UNIQUEIDENTIFIER NOT NULL,
        ProdutoId INT,
        Descricao NVARCHAR(500),
        Quantidade INT NOT NULL DEFAULT 1,
        PrecoUnitario DECIMAL(18,2) NOT NULL,
        Desconto DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorTotal DECIMAL(18,2) NOT NULL,
        Tipo NVARCHAR(50) NOT NULL DEFAULT 'Servico',
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (OrdemServicoId) REFERENCES OrdensServico(Id) ON DELETE CASCADE,
        FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id)
    );
    
    CREATE INDEX IX_OrdemServicoItens_OrdemServicoId ON OrdemServicoItens(OrdemServicoId);
    CREATE INDEX IX_OrdemServicoItens_ProdutoId ON OrdemServicoItens(ProdutoId);
END

-- Tabela OrdemServicoEventos
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrdemServicoEventos')
BEGIN
    CREATE TABLE OrdemServicoEventos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrdemServicoId UNIQUEIDENTIFIER NOT NULL,
        TipoEvento NVARCHAR(50) NOT NULL,
        Descricao NVARCHAR(1000),
        DataEvento DATETIME NOT NULL DEFAULT GETDATE(),
        UsuarioId INT,
        UsuarioNome NVARCHAR(200),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (OrdemServicoId) REFERENCES OrdensServico(Id) ON DELETE CASCADE,
        FOREIGN KEY (UsuarioId) REFERENCES Funcionarios(Id)
    );
    
    CREATE INDEX IX_OrdemServicoEventos_OrdemServicoId ON OrdemServicoEventos(OrdemServicoId);
    CREATE INDEX IX_OrdemServicoEventos_DataEvento ON OrdemServicoEventos(DataEvento);
END

-- Tabela AuditLogs
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        DataHora DATETIME NOT NULL DEFAULT GETDATE(),
        Categoria NVARCHAR(50) NOT NULL,
        Acao NVARCHAR(100) NOT NULL,
        Entidade NVARCHAR(100),
        EntidadeId NVARCHAR(100),
        Detalhes NVARCHAR(MAX),
        ValorAnterior NVARCHAR(MAX),
        ValorNovo NVARCHAR(MAX),
        Severidade NVARCHAR(20) NOT NULL,
        Sucesso BIT NOT NULL DEFAULT 1,
        UsuarioId INT,
        UsuarioNome NVARCHAR(200),
        Perfil NVARCHAR(50),
        SessaoId NVARCHAR(100),
        Maquina NVARCHAR(200),
        CorrelationId NVARCHAR(100)
    );
    
    CREATE INDEX IX_AuditLogs_DataHora ON AuditLogs(DataHora DESC);
    CREATE INDEX IX_AuditLogs_Categoria_Acao ON AuditLogs(Categoria, Acao);
    CREATE INDEX IX_AuditLogs_Entidade ON AuditLogs(Entidade, EntidadeId);
END

-- Tabela Permissoes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Permissoes')
BEGIN
    CREATE TABLE Permissoes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        Modulo NVARCHAR(50) NOT NULL,
        Acao NVARCHAR(50) NOT NULL,
        Codigo NVARCHAR(100) NOT NULL UNIQUE,
        Ativo BIT NOT NULL DEFAULT 1,
        DataCriacao DATETIME NOT NULL DEFAULT GETDATE(),
        Essencial BIT NOT NULL DEFAULT 0,
        OrdemExibicao INT NOT NULL DEFAULT 0
    );
    
    CREATE UNIQUE INDEX UX_Permissoes_Codigo ON Permissoes(Codigo);
END

-- Tabela PerfisAcesso
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PerfisAcesso')
BEGIN
    CREATE TABLE PerfisAcesso (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL UNIQUE,
        Descricao NVARCHAR(500) NOT NULL,
        NivelHierarquico NVARCHAR(50) NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        DataCriacao DATETIME NOT NULL DEFAULT GETDATE(),
        DataUltimaModificacao DATETIME,
        CriadoPor NVARCHAR(200),
        ModificadoPor NVARCHAR(200),
        PodeDeletar BIT NOT NULL DEFAULT 1,
        OrdemExibicao INT NOT NULL DEFAULT 0
    );
    
    CREATE UNIQUE INDEX UX_PerfisAcesso_Nome ON PerfisAcesso(Nome);
END

-- Tabela PerfilPermissoes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PerfilPermissoes')
BEGIN
    CREATE TABLE PerfilPermissoes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PerfilId INT NOT NULL,
        PermissaoId INT NOT NULL,
        Concedida BIT NOT NULL DEFAULT 1,
        DataConcessao DATETIME NOT NULL DEFAULT GETDATE(),
        ConcedidaPor NVARCHAR(200),
        DataRevogacao DATETIME,
        RevogadaPor NVARCHAR(200),
        Ativa BIT NOT NULL DEFAULT 1,
        Justificativa NVARCHAR(500),
        UNIQUE (PerfilId, PermissaoId),
        FOREIGN KEY (PerfilId) REFERENCES PerfisAcesso(Id) ON DELETE CASCADE,
        FOREIGN KEY (PermissaoId) REFERENCES Permissoes(Id) ON DELETE CASCADE
    );
    
    CREATE UNIQUE INDEX UX_PerfilPermissoes_Perfil_Permissao ON PerfilPermissoes(PerfilId, PermissaoId);
END

-- Tabela LoginTentativasSeguranca
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoginTentativasSeguranca')
BEGIN
    CREATE TABLE LoginTentativasSeguranca (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(200) NOT NULL,
        DataTentativa DATETIME NOT NULL DEFAULT GETDATE(),
        Sucesso BIT NOT NULL DEFAULT 0,
        IP NVARCHAR(50),
        Maquina NVARCHAR(200),
        MotivoFalha NVARCHAR(500)
    );
    
    CREATE INDEX IX_LoginTentativasSeguranca_Email ON LoginTentativasSeguranca(Email);
    CREATE INDEX IX_LoginTentativasSeguranca_DataTentativa ON LoginTentativasSeguranca(DataTentativa DESC);
END

-- Tabela UserSessions (Multiusuario)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserSessions')
BEGIN
    CREATE TABLE UserSessions (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        SessionId UNIQUEIDENTIFIER NOT NULL,
        UserId INT NOT NULL,
        NomeUsuario NVARCHAR(200) NOT NULL,
        Perfil NVARCHAR(50),
        MachineName NVARCHAR(200) NOT NULL,
        MachineUserName NVARCHAR(200),
        IpAddress NVARCHAR(50),
        LoginAt DATETIME NOT NULL DEFAULT GETDATE(),
        LastSeenAt DATETIME NOT NULL DEFAULT GETDATE(),
        LogoutAt DATETIME,
        IsActive BIT NOT NULL DEFAULT 1,
        AppVersion NVARCHAR(50),
        DatabaseProvider NVARCHAR(50),
        Observacao NVARCHAR(500),
        FOREIGN KEY (UserId) REFERENCES Funcionarios(Id)
    );
    
    CREATE INDEX IX_UserSessions_SessionId ON UserSessions(SessionId);
    CREATE INDEX IX_UserSessions_UserId ON UserSessions(UserId);
    CREATE INDEX IX_UserSessions_MachineName ON UserSessions(MachineName);
    CREATE INDEX IX_UserSessions_IsActive ON UserSessions(IsActive);
    CREATE INDEX IX_UserSessions_LastSeenAt ON UserSessions(LastSeenAt DESC);
END

-- Tabela RecordLocks (Multiusuario)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RecordLocks')
BEGIN
    CREATE TABLE RecordLocks (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NOT NULL,
        EntityDescription NVARCHAR(500),
        LockedByUserId INT NOT NULL,
        LockedByUserName NVARCHAR(200) NOT NULL,
        SessionId UNIQUEIDENTIFIER NOT NULL,
        MachineName NVARCHAR(200) NOT NULL,
        LockedAt DATETIME NOT NULL DEFAULT GETDATE(),
        ExpiresAt DATETIME NOT NULL,
        LastRenewedAt DATETIME NOT NULL DEFAULT GETDATE(),
        IsActive BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (LockedByUserId) REFERENCES Funcionarios(Id)
    );
    
    CREATE INDEX IX_RecordLocks_Entity ON RecordLocks(EntityType, EntityId);
    CREATE INDEX IX_RecordLocks_LockedBy ON RecordLocks(LockedByUserId);
    CREATE INDEX IX_RecordLocks_SessionId ON RecordLocks(SessionId);
    CREATE INDEX IX_RecordLocks_IsActive ON RecordLocks(IsActive);
    CREATE INDEX IX_RecordLocks_ExpiresAt ON RecordLocks(ExpiresAt);
END

-- Tabela SystemEvents (Multiusuario - ChangeLog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemEvents')
BEGIN
    CREATE TABLE SystemEvents (
        Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
        EventType NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100),
        EntityId NVARCHAR(100),
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedByUserId INT NOT NULL,
        MachineName NVARCHAR(200) NOT NULL,
        Payload NVARCHAR(MAX),
        Processed BIT NOT NULL DEFAULT 0,
        ProcessedAt DATETIME,
        FOREIGN KEY (CreatedByUserId) REFERENCES Funcionarios(Id)
    );
    
    CREATE INDEX IX_SystemEvents_EventType ON SystemEvents(EventType);
    CREATE INDEX IX_SystemEvents_Entity ON SystemEvents(EntityType, EntityId);
    CREATE INDEX IX_SystemEvents_CreatedAt ON SystemEvents(CreatedAt DESC);
    CREATE INDEX IX_SystemEvents_Processed ON SystemEvents(Processed);
END

PRINT 'Schema SQL Server criado com sucesso.';
