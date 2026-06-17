-- Script de criaÃ§Ã£o do schema SQL Server para Primo Auto Eletrica
-- Baseado no schema SQLite existente

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

-- Controle de migrations/schema
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SchemaMigrations')
BEGIN
    CREATE TABLE SchemaMigrations (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        Descricao NVARCHAR(300) NOT NULL,
        AplicadaEm DATETIME NOT NULL DEFAULT GETDATE(),
        VersaoAplicacao NVARCHAR(50),
        Maquina NVARCHAR(200)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SchemaVersion')
BEGIN
    CREATE TABLE SchemaVersion (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Version NVARCHAR(40) NOT NULL UNIQUE,
        AppliedAt DATETIME NOT NULL DEFAULT GETDATE(),
        Description NVARCHAR(300) NOT NULL
    );
END

-- Historico de backups
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatabaseBackups')
BEGIN
    CREATE TABLE DatabaseBackups (
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        CaminhoArquivo NVARCHAR(500) NOT NULL,
        Tipo NVARCHAR(50) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        Detalhes NVARCHAR(MAX),
        CriadoEm DATETIME NOT NULL DEFAULT GETDATE()
    );
END

-- Bloqueios legados mantidos para compatibilidade de health check
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RegistroBloqueios')
BEGIN
    CREATE TABLE RegistroBloqueios (
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        Entidade NVARCHAR(120) NOT NULL,
        EntidadeId NVARCHAR(120) NOT NULL,
        UsuarioId INT,
        UsuarioNome NVARCHAR(200),
        SessaoId NVARCHAR(120),
        Maquina NVARCHAR(200),
        CriadoEm DATETIME NOT NULL DEFAULT GETDATE(),
        ExpiraEm DATETIME,
        Motivo NVARCHAR(500),
        Ativo BIT NOT NULL DEFAULT 1
    );
END

IF COL_LENGTH('RegistroBloqueios', 'Maquina') IS NULL
    ALTER TABLE RegistroBloqueios ADD Maquina NVARCHAR(200);
IF COL_LENGTH('RegistroBloqueios', 'Motivo') IS NULL
    ALTER TABLE RegistroBloqueios ADD Motivo NVARCHAR(500);

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
        PerfilAcesso NVARCHAR(50) NOT NULL DEFAULT 'MecÃ¢nico',
        Telefone NVARCHAR(30),
        Foto NVARCHAR(500),
        DataAdmissao DATETIME NOT NULL,
        Salario DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Ativo',
        Observacoes NVARCHAR(MAX),
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        DataUltimoLogin DATETIME,
        ExigirTrocaSenha BIT NOT NULL DEFAULT 0,
        DataUltimaAlteracao DATETIME,
        RowVersion INT NOT NULL DEFAULT 0,
        Ativo BIT NOT NULL DEFAULT 1
    );
    
    CREATE INDEX IX_Funcionarios_Email ON Funcionarios(Email);
    CREATE INDEX IX_Funcionarios_CPF ON Funcionarios(CPF);
END

IF COL_LENGTH('Funcionarios', 'Observacoes') IS NULL
    ALTER TABLE Funcionarios ADD Observacoes NVARCHAR(MAX);
IF COL_LENGTH('Funcionarios', 'Foto') IS NULL
    ALTER TABLE Funcionarios ADD Foto NVARCHAR(500);
IF COL_LENGTH('Funcionarios', 'ExigirTrocaSenha') IS NULL
    ALTER TABLE Funcionarios ADD ExigirTrocaSenha BIT NOT NULL CONSTRAINT DF_Funcionarios_ExigirTrocaSenha DEFAULT 0;
IF COL_LENGTH('Funcionarios', 'DataUltimaAlteracao') IS NULL
    ALTER TABLE Funcionarios ADD DataUltimaAlteracao DATETIME;
IF COL_LENGTH('Funcionarios', 'RowVersion') IS NULL
    ALTER TABLE Funcionarios ADD RowVersion INT NOT NULL CONSTRAINT DF_Funcionarios_RowVersion DEFAULT 0;

-- Tabela ConfiguracoesSistema
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConfiguracoesSistema')
BEGIN
    CREATE TABLE ConfiguracoesSistema (
        Chave NVARCHAR(120) PRIMARY KEY,
        Valor NVARCHAR(MAX) NOT NULL,
        Grupo NVARCHAR(80) NOT NULL,
        AtualizadoEm DATETIME NOT NULL DEFAULT GETDATE(),
        AtualizadoPor NVARCHAR(200)
    );

    CREATE INDEX IX_ConfiguracoesSistema_Grupo ON ConfiguracoesSistema(Grupo);
END

-- Tabela Clientes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Clientes')
BEGIN
    CREATE TABLE Clientes (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        TipoPessoa NVARCHAR(20) NOT NULL DEFAULT 'Fisica',
        CPF NVARCHAR(20),
        RG NVARCHAR(30),
        CNPJ NVARCHAR(20),
        DataNascimento DATETIME,
        Email NVARCHAR(200),
        Telefone NVARCHAR(30),
        Celular NVARCHAR(30),
        WhatsApp NVARCHAR(30),
        Endereco NVARCHAR(500),
        Rua NVARCHAR(200),
        Numero NVARCHAR(30),
        Bairro NVARCHAR(100),
        Cidade NVARCHAR(100),
        Estado NVARCHAR(2),
        CEP NVARCHAR(10),
        ClienteVip BIT NOT NULL DEFAULT 0,
        TotalGasto DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalServicos INT NOT NULL DEFAULT 0,
        PontosFidelidade INT NOT NULL DEFAULT 0,
        ConsentimentoLGPD BIT NOT NULL DEFAULT 0,
        DataConsentimentoLGPD DATETIME,
        OrigemConsentimentoLGPD NVARCHAR(200),
        AutorizaContatoWhatsApp BIT NOT NULL DEFAULT 0,
        Observacoes NVARCHAR(MAX),
        CaminhoDocumento NVARCHAR(500),
        CaminhoAssinatura NVARCHAR(500),
        UltimaVisita DATETIME,
        ImagemUrl NVARCHAR(500),
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        DataUltimaAlteracao DATETIME,
        UpdatedBy INT,
        RowVersion INT NOT NULL DEFAULT 0,
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
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Codigo NVARCHAR(50) NOT NULL UNIQUE,
        Nome NVARCHAR(200) NOT NULL,
        Descricao NVARCHAR(1000),
        Categoria NVARCHAR(100),
        Marca NVARCHAR(100),
        Modelo NVARCHAR(100),
        FornecedorId UNIQUEIDENTIFIER,
        Fornecedor NVARCHAR(200),
        CNPJFornecedor NVARCHAR(20),
        ContatoFornecedor NVARCHAR(200),
        TelefoneFornecedor NVARCHAR(30),
        QuantidadeEstoque INT NOT NULL DEFAULT 0,
        QuantidadeMinima INT NOT NULL DEFAULT 0,
        QuantidadeMaxima INT NOT NULL DEFAULT 0,
        Localizacao NVARCHAR(200),
        Prateleira NVARCHAR(80),
        Gaveta NVARCHAR(80),
        PrecoCompra DECIMAL(18,2) NOT NULL DEFAULT 0,
        MargemLucro DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorTotalEstoque DECIMAL(18,2) NOT NULL DEFAULT 0,
        UnidadeMedida NVARCHAR(20),
        Peso NVARCHAR(50),
        Dimensoes NVARCHAR(100),
        Cor NVARCHAR(80),
        Material NVARCHAR(120),
        CodigoBarras NVARCHAR(100),
        SKU NVARCHAR(100),
        NCMS NVARCHAR(20),
        CEST NVARCHAR(20),
        CFOP NVARCHAR(20),
        ProdutoPerecivel BIT NOT NULL DEFAULT 0,
        DataValidade DATETIME,
        DataFabricacao DATETIME,
        Lote NVARCHAR(100),
        DataUltimaCompra DATETIME,
        DataUltimaVenda DATETIME,
        DataUltimaAtualizacao DATETIME,
        Observacoes NVARCHAR(MAX),
        ImagemUrl NVARCHAR(500),
        Anexos NVARCHAR(MAX),
        Unidade NVARCHAR(20) NOT NULL DEFAULT 'UN',
        PrecoCusto DECIMAL(18,2),
        PrecoVenda DECIMAL(18,2) NOT NULL,
        EstoqueAtual INT NOT NULL DEFAULT 0,
        EstoqueMinimo INT NOT NULL DEFAULT 0,
        EstoqueMaximo INT NOT NULL DEFAULT 0,
        TotalVendas INT NOT NULL DEFAULT 0,
        TotalFaturado DECIMAL(18,2) NOT NULL DEFAULT 0,
        VendasUltimoMes INT NOT NULL DEFAULT 0,
        VendasUltimoTrimestre INT NOT NULL DEFAULT 0,
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        DataUltimaAlteracao DATETIME,
        UpdatedBy INT,
        RowVersion INT NOT NULL DEFAULT 0,
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
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Nome NVARCHAR(200),
        RazaoSocial NVARCHAR(200) NOT NULL,
        NomeFantasia NVARCHAR(200) NOT NULL,
        CNPJ NVARCHAR(20),
        InscricaoEstadual NVARCHAR(40),
        Email NVARCHAR(200),
        Telefone NVARCHAR(30),
        Celular NVARCHAR(30),
        WhatsAppVendedor NVARCHAR(30),
        Site NVARCHAR(200),
        Endereco NVARCHAR(500),
        CEP NVARCHAR(10),
        Rua NVARCHAR(200),
        Numero NVARCHAR(30),
        Complemento NVARCHAR(120),
        Bairro NVARCHAR(100),
        Cidade NVARCHAR(100),
        Estado NVARCHAR(2),
        FormaPagamento NVARCHAR(100),
        PrazoPagamento NVARCHAR(100),
        PrazoMedioPagamentoDias INT NOT NULL DEFAULT 0,
        PrazoMedioEntregaDias INT NOT NULL DEFAULT 0,
        PedidoMinimo DECIMAL(18,2) NOT NULL DEFAULT 0,
        Categoria NVARCHAR(100) NOT NULL DEFAULT 'Pecas',
        CategoriaPreferencial NVARCHAR(100),
        Nota INT NOT NULL DEFAULT 5,
        Observacoes NVARCHAR(MAX),
        UltimaCompra DATETIME,
        TotalCompras DECIMAL(18,2) NOT NULL DEFAULT 0,
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        DataUltimaAlteracao DATETIME,
        UpdatedBy INT,
        RowVersion INT NOT NULL DEFAULT 0,
        Ativo BIT NOT NULL DEFAULT 1
    );
    
    CREATE INDEX IX_Fornecedores_CNPJ ON Fornecedores(CNPJ);
    CREATE INDEX IX_Fornecedores_Email ON Fornecedores(Email);
END

-- Tabela ContatosFornecedor
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContatosFornecedor')
BEGIN
    CREATE TABLE ContatosFornecedor (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        FornecedorId UNIQUEIDENTIFIER NOT NULL,
        Nome NVARCHAR(200) NOT NULL,
        Cargo NVARCHAR(100),
        Email NVARCHAR(200),
        Telefone NVARCHAR(30),
        Celular NVARCHAR(30),
        Principal BIT NOT NULL DEFAULT 0,
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (FornecedorId) REFERENCES Fornecedores(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_ContatosFornecedor_FornecedorId ON ContatosFornecedor(FornecedorId);
END

-- Tabela ProdutoFornecedores
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProdutoFornecedores')
BEGIN
    CREATE TABLE ProdutoFornecedores (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ProdutoId UNIQUEIDENTIFIER NOT NULL,
        FornecedorId UNIQUEIDENTIFIER NOT NULL,
        CodigoProduto NVARCHAR(100),
        NomeProduto NVARCHAR(200),
        CategoriaProduto NVARCHAR(100),
        CodigoFornecedor NVARCHAR(100),
        PrecoUltimaCompra DECIMAL(18,2) NOT NULL DEFAULT 0,
        QuantidadeUltimaCompra DECIMAL(18,3) NOT NULL DEFAULT 0,
        PrazoEntregaDias INT NOT NULL DEFAULT 0,
        QuantidadeCompras INT NOT NULL DEFAULT 0,
        ValorCompras DECIMAL(18,2) NOT NULL DEFAULT 0,
        DataUltimaCompra DATETIME,
        ChaveUltimaNFe NVARCHAR(100),
        NumeroUltimaNFe NVARCHAR(50),
        Ativo BIT NOT NULL DEFAULT 1,
        Origem NVARCHAR(100),
        Observacoes NVARCHAR(MAX),
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        DataUltimaAtualizacao DATETIME,
        FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id),
        FOREIGN KEY (FornecedorId) REFERENCES Fornecedores(Id)
    );

    CREATE UNIQUE INDEX IX_ProdutoFornecedores_Produto_Fornecedor
        ON ProdutoFornecedores(ProdutoId, FornecedorId);
    CREATE INDEX IX_ProdutoFornecedores_Fornecedor
        ON ProdutoFornecedores(FornecedorId, Ativo, DataUltimaCompra DESC);
    CREATE INDEX IX_ProdutoFornecedores_Produto
        ON ProdutoFornecedores(ProdutoId);
END

-- Tabela ImportacoesNFe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportacoesNFe')
BEGIN
    CREATE TABLE ImportacoesNFe (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        ChaveAcesso NVARCHAR(100),
        Numero NVARCHAR(50),
        Serie NVARCHAR(20),
        DataEmissao NVARCHAR(30),
        DataEntrada NVARCHAR(30),
        ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorProdutos DECIMAL(18,2) NOT NULL DEFAULT 0,
        Modelo NVARCHAR(20),
        FornecedorNome NVARCHAR(200),
        FornecedorCNPJ NVARCHAR(30),
        Status NVARCHAR(30) NOT NULL,
        CaminhoArquivo NVARCHAR(500),
        Erro NVARCHAR(MAX),
        DataImportacao NVARCHAR(30) NOT NULL,
        UsuarioId NVARCHAR(40),
        UsuarioNome NVARCHAR(200)
    );
    
    CREATE UNIQUE INDEX IX_ImportacoesNFe_ChaveAcesso_Unica
        ON ImportacoesNFe(ChaveAcesso)
        WHERE ChaveAcesso IS NOT NULL AND ChaveAcesso <> '';
    CREATE INDEX IX_ImportacoesNFe_DataImportacao ON ImportacoesNFe(DataImportacao);
    CREATE INDEX IX_ImportacoesNFe_Fornecedor ON ImportacoesNFe(FornecedorNome, FornecedorCNPJ);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportacoesItens')
BEGIN
    CREATE TABLE ImportacoesItens (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        ImportacaoId NVARCHAR(40) NOT NULL,
        Codigo NVARCHAR(120),
        Nome NVARCHAR(300),
        NCM NVARCHAR(30),
        CFOP NVARCHAR(30),
        Quantidade DECIMAL(18,4) NOT NULL DEFAULT 0,
        ValorUnitario DECIMAL(18,4) NOT NULL DEFAULT 0,
        ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        UnidadeMedida NVARCHAR(30),
        Status NVARCHAR(40) NOT NULL,
        ProdutoExistenteId NVARCHAR(40),
        MotivoIgnorado NVARCHAR(MAX),
        SelecionadoParaImportacao INT NOT NULL DEFAULT 1,
        AcaoPlanejada NVARCHAR(80),
        CategoriaSugerida NVARCHAR(120),
        MargemAplicada DECIMAL(18,4) NOT NULL DEFAULT 0,
        PrecoVendaSugerido DECIMAL(18,4) NOT NULL DEFAULT 0,
        ProdutoVinculadoReferencia NVARCHAR(200),
        CodigoBarras NVARCHAR(120),
        ObservacaoConferencia NVARCHAR(MAX),
        ProdutoSnapshotAnterior NVARCHAR(MAX),
        ProdutoSnapshotPosterior NVARCHAR(MAX),
        FOREIGN KEY (ImportacaoId) REFERENCES ImportacoesNFe(Id)
    );

    CREATE INDEX IX_ImportacoesItens_ImportacaoId ON ImportacoesItens(ImportacaoId);
    CREATE INDEX IX_ImportacoesItens_Codigo ON ImportacoesItens(Codigo);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportacoesNFeExclusoes')
BEGIN
    CREATE TABLE ImportacoesNFeExclusoes (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        ImportacaoId NVARCHAR(40) NOT NULL,
        ChaveAcesso NVARCHAR(100),
        Numero NVARCHAR(50),
        Serie NVARCHAR(20),
        FornecedorNome NVARCHAR(200),
        FornecedorCNPJ NVARCHAR(30),
        QuantidadeProdutos INT NOT NULL DEFAULT 0,
        ProdutosNovos INT NOT NULL DEFAULT 0,
        ProdutosAtualizados INT NOT NULL DEFAULT 0,
        ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        StatusOriginal NVARCHAR(40),
        UsuarioImportacao NVARCHAR(200),
        DataImportacaoOriginal NVARCHAR(30),
        DataExclusao NVARCHAR(30) NOT NULL,
        UsuarioExclusao NVARCHAR(200),
        Motivo NVARCHAR(MAX)
    );

    CREATE INDEX IX_ImportacoesNFeExclusoes_DataExclusao ON ImportacoesNFeExclusoes(DataExclusao DESC);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportacoesNFeRollbacks')
BEGIN
    CREATE TABLE ImportacoesNFeRollbacks (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        ImportacaoId NVARCHAR(40) NOT NULL,
        ChaveAcesso NVARCHAR(100),
        Numero NVARCHAR(50),
        Serie NVARCHAR(20),
        FornecedorNome NVARCHAR(200),
        FornecedorCNPJ NVARCHAR(30),
        DataRollback NVARCHAR(30) NOT NULL,
        UsuarioRollback NVARCHAR(200),
        Motivo NVARCHAR(MAX),
        TotalItens INT NOT NULL DEFAULT 0,
        ProdutosRemovidos INT NOT NULL DEFAULT 0,
        ProdutosBloqueados INT NOT NULL DEFAULT 0,
        ProdutosIgnorados INT NOT NULL DEFAULT 0,
        AtualizacoesRevertidas INT NOT NULL DEFAULT 0,
        AtualizacoesIgnoradas INT NOT NULL DEFAULT 0,
        Detalhes NVARCHAR(MAX)
    );

    CREATE INDEX IX_ImportacoesNFeRollbacks_DataRollback ON ImportacoesNFeRollbacks(DataRollback DESC);
END

-- Tabela Vendas
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Vendas')
BEGIN
    CREATE TABLE Vendas (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Numero NVARCHAR(50),
        Data NVARCHAR(30) NOT NULL,
        ClienteId UNIQUEIDENTIFIER NULL,
        ClienteNome NVARCHAR(200),
        Total DECIMAL(18,2) NOT NULL DEFAULT 0,
        DataVenda DATETIME NOT NULL DEFAULT GETDATE(),
        ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        Desconto DECIMAL(18,2) NOT NULL DEFAULT 0,
        Usuario NVARCHAR(200),
        QuantidadeItens DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pendente',
        FormaPagamento NVARCHAR(50),
        CaixaSessaoId NVARCHAR(40),
        DataCancelamento NVARCHAR(30),
        CanceladoPor NVARCHAR(200),
        MotivoCancelamento NVARCHAR(MAX),
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
    CREATE INDEX IX_Vendas_Data ON Vendas(Data);
    CREATE INDEX IX_Vendas_DataVenda ON Vendas(DataVenda);
END

-- Tabela VendaItens
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VendaItens')
BEGIN
    CREATE TABLE VendaItens (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        VendaId UNIQUEIDENTIFIER NOT NULL,
        ProdutoId UNIQUEIDENTIFIER NULL,
        Tipo NVARCHAR(30) NOT NULL DEFAULT 'Produto',
        DescricaoItem NVARCHAR(500),
        ProdutoNome NVARCHAR(200),
        Quantidade INT NOT NULL DEFAULT 0,
        PrecoUnitario DECIMAL(18,2) NOT NULL DEFAULT 0,
        CustoUnitario DECIMAL(18,2) NOT NULL DEFAULT 0,
        Desconto DECIMAL(18,2) NOT NULL DEFAULT 0,
        Subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (VendaId) REFERENCES Vendas(Id) ON DELETE CASCADE,
        FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id)
    );
    
    CREATE INDEX IX_VendaItens_VendaId ON VendaItens(VendaId);
    CREATE INDEX IX_VendaItens_ProdutoId ON VendaItens(ProdutoId);
END

-- Tabelas de Agendamentos
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Agendamentos')
BEGIN
    CREATE TABLE Agendamentos (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        Numero NVARCHAR(50),
        DataCriacao NVARCHAR(30),
        DataAgendamento NVARCHAR(30),
        HoraInicio NVARCHAR(20),
        HoraTermino NVARCHAR(20),
        DuracaoEstimada NVARCHAR(50),
        DuracaoReal NVARCHAR(50),
        Status NVARCHAR(50),
        Prioridade NVARCHAR(50),
        TipoServico NVARCHAR(120),
        CategoriaServico NVARCHAR(120),
        DescricaoServico NVARCHAR(MAX),
        Observacoes NVARCHAR(MAX),
        ClienteId NVARCHAR(40),
        ClienteNome NVARCHAR(200),
        ClienteTelefone NVARCHAR(50),
        ClienteEmail NVARCHAR(200),
        ClienteDocumento NVARCHAR(50),
        ClienteVip INT NOT NULL DEFAULT 0,
        ClienteTotalGasto DECIMAL(18,2) NOT NULL DEFAULT 0,
        ClienteAtendimentos INT NOT NULL DEFAULT 0,
        ClienteUltimaVisita NVARCHAR(30),
        VeiculoId NVARCHAR(40),
        VeiculoPlaca NVARCHAR(20),
        VeiculoModelo NVARCHAR(120),
        VeiculoMarca NVARCHAR(120),
        VeiculoAno NVARCHAR(20),
        VeiculoCor NVARCHAR(80),
        VeiculoCombustivel NVARCHAR(80),
        VeiculoQuilometragem INT NOT NULL DEFAULT 0,
        VeiculoObservacoes NVARCHAR(MAX),
        TecnicoId NVARCHAR(40),
        TecnicoNome NVARCHAR(200),
        TecnicoEspecialidade NVARCHAR(160),
        TecnicoAtivo INT NOT NULL DEFAULT 0,
        OrdemServicoId NVARCHAR(40),
        NumeroOS NVARCHAR(50),
        DataInicioOS NVARCHAR(30),
        DataConclusaoOS NVARCHAR(30),
        ValorEstimado DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorReal DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorPago DECIMAL(18,2) NOT NULL DEFAULT 0,
        FormaPagamento NVARCHAR(80),
        Pago INT NOT NULL DEFAULT 0,
        DataPagamento NVARCHAR(30),
        CheckIn NVARCHAR(30),
        CheckOut NVARCHAR(30),
        CheckInObservacoes NVARCHAR(MAX),
        CheckOutObservacoes NVARCHAR(MAX),
        CheckInFotos NVARCHAR(MAX),
        CheckOutFotos NVARCHAR(MAX),
        ValorProdutos DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorServicos DECIMAL(18,2) NOT NULL DEFAULT 0,
        Recorrente INT NOT NULL DEFAULT 0,
        TipoRecorrencia NVARCHAR(80),
        IntervaloRecorrencia INT NOT NULL DEFAULT 0,
        ProximaRecorrencia NVARCHAR(30),
        LembreteWhatsApp INT NOT NULL DEFAULT 0,
        LembreteEmail INT NOT NULL DEFAULT 0,
        DataLembrete NVARCHAR(30),
        LembreteEnviado INT NOT NULL DEFAULT 0,
        AlertaAtraso INT NOT NULL DEFAULT 0,
        AlertaPecaFaltando INT NOT NULL DEFAULT 0,
        AlertaPronto INT NOT NULL DEFAULT 0,
        AvaliacaoCliente INT NOT NULL DEFAULT 0,
        AvaliacaoComentario NVARCHAR(MAX),
        DataCancelamento NVARCHAR(30),
        MotivoCancelamento NVARCHAR(MAX),
        CanceladoPor NVARCHAR(40),
        DataReagendamento NVARCHAR(30),
        DataAgendamentoAnterior NVARCHAR(30),
        MotivoReagendamento NVARCHAR(MAX)
    );

    CREATE INDEX IX_Agendamentos_DataAgendamento ON Agendamentos(DataAgendamento);
    CREATE INDEX IX_Agendamentos_ClienteId ON Agendamentos(ClienteId);
    CREATE INDEX IX_Agendamentos_TecnicoId ON Agendamentos(TecnicoId);
    CREATE INDEX IX_Agendamentos_Status ON Agendamentos(Status);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgendamentoProdutos')
BEGIN
    CREATE TABLE AgendamentoProdutos (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        AgendamentoId NVARCHAR(40),
        ProdutoId NVARCHAR(40),
        ProdutoNome NVARCHAR(200),
        ProdutoCodigo NVARCHAR(80),
        Quantidade INT NOT NULL DEFAULT 0,
        PrecoUnitario DECIMAL(18,2) NOT NULL DEFAULT 0,
        PrecoTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        Reservado INT NOT NULL DEFAULT 0,
        DataReserva NVARCHAR(30),
        FOREIGN KEY (AgendamentoId) REFERENCES Agendamentos(Id)
    );

    CREATE INDEX IX_AgendamentoProdutos_AgendamentoId ON AgendamentoProdutos(AgendamentoId);
    CREATE INDEX IX_AgendamentoProdutos_ProdutoId ON AgendamentoProdutos(ProdutoId);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgendamentoServicos')
BEGIN
    CREATE TABLE AgendamentoServicos (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        AgendamentoId NVARCHAR(40),
        Nome NVARCHAR(200),
        Categoria NVARCHAR(120),
        Valor DECIMAL(18,2) NOT NULL DEFAULT 0,
        TempoEstimado NVARCHAR(50),
        TempoReal NVARCHAR(50),
        TecnicoResponsavel NVARCHAR(200),
        Status NVARCHAR(50),
        Observacoes NVARCHAR(MAX),
        Concluido INT NOT NULL DEFAULT 0,
        DataConclusao NVARCHAR(30),
        FOREIGN KEY (AgendamentoId) REFERENCES Agendamentos(Id)
    );

    CREATE INDEX IX_AgendamentoServicos_AgendamentoId ON AgendamentoServicos(AgendamentoId);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgendamentoTimeline')
BEGIN
    CREATE TABLE AgendamentoTimeline (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        AgendamentoId NVARCHAR(40),
        DataHora NVARCHAR(30),
        Usuario NVARCHAR(200),
        Acao NVARCHAR(120),
        Detalhes NVARCHAR(MAX),
        TipoAlteracao NVARCHAR(80),
        FOREIGN KEY (AgendamentoId) REFERENCES Agendamentos(Id)
    );

    CREATE INDEX IX_AgendamentoTimeline_AgendamentoId ON AgendamentoTimeline(AgendamentoId);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AlertasAgendamento')
BEGIN
    CREATE TABLE AlertasAgendamento (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        Tipo NVARCHAR(80),
        Mensagem NVARCHAR(MAX),
        Severidade NVARCHAR(50),
        DataGeracao NVARCHAR(30),
        Lido INT NOT NULL DEFAULT 0,
        Origem NVARCHAR(120),
        AgendamentoId NVARCHAR(40)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgendamentoIntegracoes')
BEGIN
    CREATE TABLE AgendamentoIntegracoes (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        AgendamentoId NVARCHAR(40) NOT NULL,
        Tipo NVARCHAR(120) NOT NULL,
        Detalhes NVARCHAR(MAX),
        DataCriacao NVARCHAR(30) NOT NULL,
        UNIQUE (AgendamentoId, Tipo)
    );

    CREATE INDEX IX_AgendamentoIntegracoes_AgendamentoId ON AgendamentoIntegracoes(AgendamentoId);
END

-- Tabelas Financeiras
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContasPagar')
BEGIN
    CREATE TABLE ContasPagar (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Fornecedor NVARCHAR(200) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        Valor DECIMAL(18,2) NOT NULL,
        DataVencimento NVARCHAR(30) NOT NULL,
        DataPagamento NVARCHAR(30),
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pendente',
        Categoria NVARCHAR(120),
        Observacoes NVARCHAR(MAX),
        DataCriacao NVARCHAR(30) NOT NULL,
        Origem NVARCHAR(120),
        ReferenciaExterna NVARCHAR(160)
    );

    CREATE UNIQUE INDEX IX_ContasPagar_Integracao
        ON ContasPagar (Origem, ReferenciaExterna)
        WHERE Origem IS NOT NULL AND ReferenciaExterna IS NOT NULL;
    CREATE INDEX IX_ContasPagar_DataVencimento ON ContasPagar(DataVencimento);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContasReceber')
BEGIN
    CREATE TABLE ContasReceber (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Cliente NVARCHAR(200) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        Valor DECIMAL(18,2) NOT NULL,
        DataVencimento NVARCHAR(30) NOT NULL,
        DataPagamento NVARCHAR(30),
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pendente',
        FormaPagamento NVARCHAR(80),
        Observacoes NVARCHAR(MAX),
        DataCriacao NVARCHAR(30) NOT NULL,
        Origem NVARCHAR(120),
        ReferenciaExterna NVARCHAR(160)
    );

    CREATE UNIQUE INDEX IX_ContasReceber_Integracao
        ON ContasReceber (Origem, ReferenciaExterna)
        WHERE Origem IS NOT NULL AND ReferenciaExterna IS NOT NULL;
    CREATE INDEX IX_ContasReceber_DataVencimento ON ContasReceber(DataVencimento);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MovimentacoesFinanceiras')
BEGIN
    CREATE TABLE MovimentacoesFinanceiras (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Tipo NVARCHAR(50) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        Valor DECIMAL(18,2) NOT NULL,
        Data NVARCHAR(30) NOT NULL,
        Categoria NVARCHAR(120),
        FormaPagamento NVARCHAR(80),
        ReferenciaId INT,
        Observacoes NVARCHAR(MAX),
        DataCriacao NVARCHAR(30) NOT NULL,
        Origem NVARCHAR(120),
        ReferenciaExterna NVARCHAR(160)
    );

    CREATE UNIQUE INDEX IX_MovimentacoesFinanceiras_Integracao
        ON MovimentacoesFinanceiras (Origem, ReferenciaExterna)
        WHERE Origem IS NOT NULL AND ReferenciaExterna IS NOT NULL;
    CREATE INDEX IX_MovimentacoesFinanceiras_Data ON MovimentacoesFinanceiras(Data);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CaixaSessoes')
BEGIN
    CREATE TABLE CaixaSessoes (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        NumeroCaixa NVARCHAR(20) NOT NULL,
        DataAbertura NVARCHAR(30) NOT NULL,
        DataFechamento NVARCHAR(30),
        OperadorId INT,
        OperadorNome NVARCHAR(200) NOT NULL,
        PerfilOperador NVARCHAR(120),
        ValorAbertura DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorEsperado DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorInformadoFechamento DECIMAL(18,2),
        TotalVendas DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalSangrias DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalSuprimentos DECIMAL(18,2) NOT NULL DEFAULT 0,
        QuantidadeVendas INT NOT NULL DEFAULT 0,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Fechado',
        Observacoes NVARCHAR(MAX),
        DataCriacao NVARCHAR(30) NOT NULL,
        DataUltimaMovimentacao NVARCHAR(30)
    );

    CREATE INDEX IX_CaixaSessoes_Status_Data ON CaixaSessoes(Status, DataAbertura DESC);
    CREATE INDEX IX_CaixaSessoes_Operador_Status ON CaixaSessoes(OperadorId, Status);
    CREATE UNIQUE INDEX UX_CaixaSessoes_Operador_Aberto
        ON CaixaSessoes(OperadorId)
        WHERE OperadorId IS NOT NULL AND Status = 'Aberto';
    CREATE UNIQUE INDEX UX_CaixaSessoes_Numero_Aberto
        ON CaixaSessoes(NumeroCaixa)
        WHERE Status = 'Aberto';
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MovimentacoesCaixa')
BEGIN
    CREATE TABLE MovimentacoesCaixa (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        CaixaSessaoId UNIQUEIDENTIFIER NOT NULL,
        Data NVARCHAR(30) NOT NULL,
        Tipo NVARCHAR(40) NOT NULL,
        ValorMovimento DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorInicial DECIMAL(18,2) NOT NULL DEFAULT 0,
        ValorFinal DECIMAL(18,2) NOT NULL DEFAULT 0,
        Sangrias DECIMAL(18,2) NOT NULL DEFAULT 0,
        Suprimentos DECIMAL(18,2) NOT NULL DEFAULT 0,
        Diferenca DECIMAL(18,2) NOT NULL DEFAULT 0,
        Operador NVARCHAR(200),
        FormaPagamento NVARCHAR(80),
        ReferenciaId NVARCHAR(120),
        Observacoes NVARCHAR(MAX),
        FOREIGN KEY (CaixaSessaoId) REFERENCES CaixaSessoes(Id)
    );

    CREATE INDEX IX_MovimentacoesCaixa_Sessao_Data ON MovimentacoesCaixa(CaixaSessaoId, Data DESC);
    CREATE INDEX IX_MovimentacoesCaixa_Tipo_Data ON MovimentacoesCaixa(Tipo, Data DESC);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MetasFinanceiras')
BEGIN
    CREATE TABLE MetasFinanceiras (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(200) NOT NULL,
        ValorMeta DECIMAL(18,2) NOT NULL,
        ValorAtual DECIMAL(18,2) NOT NULL DEFAULT 0,
        DataInicio NVARCHAR(30) NOT NULL,
        DataFim NVARCHAR(30) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Em Andamento',
        Descricao NVARCHAR(MAX)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Relatorios')
BEGIN
    CREATE TABLE Relatorios (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        Tipo NVARCHAR(120) NOT NULL,
        Nome NVARCHAR(200) NOT NULL,
        DataGeracao NVARCHAR(30) NOT NULL,
        DataInicio NVARCHAR(30) NOT NULL,
        DataFim NVARCHAR(30) NOT NULL,
        UsuarioGerou NVARCHAR(200) NOT NULL,
        Status NVARCHAR(80) NOT NULL,
        FiltrosAplicados NVARCHAR(MAX),
        ValorTotal DECIMAL(18,2),
        TotalRegistros INT,
        CaminhoArquivo NVARCHAR(MAX),
        Observacoes NVARCHAR(MAX)
    );

    CREATE INDEX IX_Relatorios_DataGeracao ON Relatorios(DataGeracao);
    CREATE INDEX IX_Relatorios_Tipo ON Relatorios(Tipo);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Auditoria')
BEGIN
    CREATE TABLE Auditoria (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        DataHora NVARCHAR(30) NOT NULL,
        Usuario NVARCHAR(200) NOT NULL,
        Acao NVARCHAR(200) NOT NULL,
        Tabela NVARCHAR(160) NOT NULL,
        RegistroId NVARCHAR(80) NOT NULL,
        ValorAnterior NVARCHAR(MAX),
        ValorNovo NVARCHAR(MAX),
        IP NVARCHAR(80)
    );

    CREATE INDEX IX_Auditoria_DataHora ON Auditoria(DataHora);
    CREATE INDEX IX_Auditoria_Usuario ON Auditoria(Usuario);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Metas')
BEGIN
    CREATE TABLE Metas (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        Tipo NVARCHAR(120) NOT NULL,
        Periodo NVARCHAR(120) NOT NULL,
        MetaValor DECIMAL(18,2) NOT NULL,
        ValorAtual DECIMAL(18,2),
        PercentualAtingido DECIMAL(18,2),
        Responsavel NVARCHAR(200),
        DataInicio NVARCHAR(30) NOT NULL,
        DataFim NVARCHAR(30) NOT NULL,
        Status NVARCHAR(80) NOT NULL
    );

    CREATE INDEX IX_Metas_Periodo ON Metas(DataInicio, DataFim);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Alertas')
BEGIN
    CREATE TABLE Alertas (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        Tipo NVARCHAR(120) NOT NULL,
        Mensagem NVARCHAR(MAX) NOT NULL,
        Severidade NVARCHAR(80) NOT NULL,
        DataGeracao NVARCHAR(30) NOT NULL,
        Lido INT NOT NULL DEFAULT 0,
        Origem NVARCHAR(160)
    );

    CREATE INDEX IX_Alertas_Lido ON Alertas(Lido);
    CREATE INDEX IX_Alertas_DataGeracao ON Alertas(DataGeracao);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Timeline')
BEGIN
    CREATE TABLE Timeline (
        Id NVARCHAR(40) NOT NULL PRIMARY KEY,
        DataHora NVARCHAR(30) NOT NULL,
        TipoEvento NVARCHAR(120) NOT NULL,
        Descricao NVARCHAR(MAX) NOT NULL,
        Usuario NVARCHAR(200) NOT NULL,
        Valor DECIMAL(18,2),
        Categoria NVARCHAR(120)
    );

    CREATE INDEX IX_Timeline_DataHora ON Timeline(DataHora);
    CREATE INDEX IX_Timeline_Categoria ON Timeline(Categoria);
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
        ImagemUrl NVARCHAR(500),
        DocumentoImagemUrl NVARCHAR(500),
        TipoVeiculo NVARCHAR(50),
        SistemaEletrico NVARCHAR(20),
        Motor NVARCHAR(50),
        Combustivel NVARCHAR(30),
        BateriaPrincipal NVARCHAR(120),
        BateriaAuxiliar NVARCHAR(120),
        BateriaInstalada NVARCHAR(160),
        BateriaMarca NVARCHAR(100),
        BateriaAmperagem NVARCHAR(50),
        BateriaDataInstalacao DATETIME,
        Alternador NVARCHAR(120),
        MotorPartida NVARCHAR(120),
        Quilometragem INT NOT NULL DEFAULT 0,
        TesteTensaoRepouso NVARCHAR(50),
        TesteTensaoPartida NVARCHAR(50),
        TesteCargaAlternador NVARCHAR(50),
        CorrenteFuga NVARCHAR(50),
        EstadoAterramentos NVARCHAR(500),
        ChicotesReparados NVARCHAR(1000),
        FusiveisSubstituidos NVARCHAR(500),
        RelesSubstituidos NVARCHAR(500),
        LampadasSubstituidas NVARCHAR(500),
        AcessoriosInstalados NVARCHAR(1000),
        ObservacoesTecnicasEletricas NVARCHAR(2000),
        FotosTecnicas NVARCHAR(2000),
        HistoricoTecnico NVARCHAR(2000),
        ObservacoesEletricasRecorrentes NVARCHAR(2000),
        ProblemaRecorrente NVARCHAR(1000),
        ObservacaoImportanteTecnico NVARCHAR(1000),
        RetornoRecomendadoEm DATETIME,
        GarantiaValidaAte DATETIME,
        ProximaRevisaoEm DATETIME,
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
        ClienteId UNIQUEIDENTIFIER NOT NULL,
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
        DiagnosticoInicial NVARCHAR(1000),
        DiagnosticoFinal NVARCHAR(1000),
        ObservacoesInternas NVARCHAR(1000),
        ObservacoesCliente NVARCHAR(1000),
        ChecklistEntrada NVARCHAR(2000),
        ChecklistEntrega NVARCHAR(1000),
        ChecklistSaida NVARCHAR(2000),
        FotosAntes NVARCHAR(MAX),
        FotosDepois NVARCHAR(MAX),
        GarantiaObservacoes NVARCHAR(1000),
        TermoAutorizacao NVARCHAR(2000),
        AssinaturaClienteUrl NVARCHAR(500),
        AprovadaCliente BIT NOT NULL DEFAULT 0,
        MetodoAprovacao NVARCHAR(50),
        DataAbertura DATETIME NOT NULL DEFAULT GETDATE(),
        DataPrevisao DATETIME,
        DataAprovacao DATETIME,
        DataInicio DATETIME,
        DataConclusao DATETIME,
        DataEntrega DATETIME,
        GarantiaValidaAte DATETIME,
        TempoPrevistoMinutos INT NOT NULL DEFAULT 0,
        TempoRealMinutos INT NOT NULL DEFAULT 0,
        OrcamentoId UNIQUEIDENTIFIER NULL,
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
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrdemServicoId UNIQUEIDENTIFIER NOT NULL,
        ProdutoId UNIQUEIDENTIFIER,
        Tipo NVARCHAR(50) NOT NULL DEFAULT 'Servico',
        Descricao NVARCHAR(500),
        Quantidade DECIMAL(18,2) NOT NULL DEFAULT 1,
        ValorUnitario DECIMAL(18,2) NOT NULL DEFAULT 0,
        CustoUnitario DECIMAL(18,2) NOT NULL DEFAULT 0,
        Observacoes NVARCHAR(1000),
        OrdemExibicao INT NOT NULL DEFAULT 0,
        EstoqueMovimentado BIT NOT NULL DEFAULT 0,
        DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (OrdemServicoId) REFERENCES OrdensServico(Id) ON DELETE CASCADE,
        FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id)
    );
    
    CREATE INDEX IX_OrdemServicoItens_OrdemServicoId ON OrdemServicoItens(OrdemServicoId);
    CREATE INDEX IX_OrdemServicoItens_ProdutoId ON OrdemServicoItens(ProdutoId);
END

-- Tabela Orcamentos
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orcamentos')
BEGIN
    CREATE TABLE Orcamentos (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ClienteId UNIQUEIDENTIFIER NULL,
        VeiculoId UNIQUEIDENTIFIER NULL,
        VendedorId UNIQUEIDENTIFIER NULL,
        Numero NVARCHAR(50),
        Status NVARCHAR(50),
        DataCriacao DATETIME NOT NULL DEFAULT GETDATE(),
        DataValidade DATETIME NULL,
        DataAprovacao DATETIME NULL,
        DataConversaoVenda DATETIME NULL,
        DataConversaoOrdemServico DATETIME NULL,
        OrdemServicoId UNIQUEIDENTIFIER NULL,
        Subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        Desconto DECIMAL(18,2) NOT NULL DEFAULT 0,
        DescontoTipo NVARCHAR(20) NOT NULL DEFAULT 'Valor',
        DescontoPercentual DECIMAL(18,2) NOT NULL DEFAULT 0,
        Acrescimo DECIMAL(18,2) NOT NULL DEFAULT 0,
        Total DECIMAL(18,2) NOT NULL DEFAULT 0,
        MargemLucro DECIMAL(18,2) NOT NULL DEFAULT 0,
        LucroEstimado DECIMAL(18,2) NOT NULL DEFAULT 0,
        ComissaoVendedor DECIMAL(18,2) NOT NULL DEFAULT 0,
        ImpostosEstimados DECIMAL(18,2) NOT NULL DEFAULT 0,
        Observacoes NVARCHAR(MAX),
        Diagnostico NVARCHAR(MAX),
        CondicoesPagamento NVARCHAR(500),
        PrazoEntrega NVARCHAR(200),
        FOREIGN KEY (ClienteId) REFERENCES Clientes(Id),
        FOREIGN KEY (VeiculoId) REFERENCES Veiculos(Id),
        FOREIGN KEY (OrdemServicoId) REFERENCES OrdensServico(Id)
    );

    CREATE INDEX IX_Orcamentos_ClienteId ON Orcamentos(ClienteId);
    CREATE INDEX IX_Orcamentos_VeiculoId ON Orcamentos(VeiculoId);
    CREATE INDEX IX_Orcamentos_Numero ON Orcamentos(Numero);
    CREATE INDEX IX_Orcamentos_DataCriacao ON Orcamentos(DataCriacao DESC);
END

-- Tabela OrcamentoItens
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrcamentoItens')
BEGIN
    CREATE TABLE OrcamentoItens (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrcamentoId UNIQUEIDENTIFIER NOT NULL,
        ProdutoId UNIQUEIDENTIFIER NULL,
        Tipo NVARCHAR(30) NOT NULL DEFAULT 'Produto',
        ProdutoNome NVARCHAR(200),
        ProdutoCodigo NVARCHAR(80),
        ProdutoCategoria NVARCHAR(120),
        ProdutoMarca NVARCHAR(120),
        ProdutoAplicacao NVARCHAR(300),
        Quantidade INT NOT NULL DEFAULT 1,
        PrecoUnitario DECIMAL(18,2) NOT NULL DEFAULT 0,
        PrecoCusto DECIMAL(18,2) NOT NULL DEFAULT 0,
        Desconto DECIMAL(18,2) NOT NULL DEFAULT 0,
        Subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        LucroEstimado DECIMAL(18,2) NOT NULL DEFAULT 0,
        MargemLucro DECIMAL(18,2) NOT NULL DEFAULT 0,
        EstoqueDisponivel INT NOT NULL DEFAULT 0,
        Observacoes NVARCHAR(MAX),
        FOREIGN KEY (OrcamentoId) REFERENCES Orcamentos(Id) ON DELETE CASCADE,
        FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id)
    );

    CREATE INDEX IX_OrcamentoItens_OrcamentoId ON OrcamentoItens(OrcamentoId);
    CREATE INDEX IX_OrcamentoItens_ProdutoId ON OrcamentoItens(ProdutoId);
END

-- Tabela OrdemServicoEventos
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrdemServicoEventos')
BEGIN
    CREATE TABLE OrdemServicoEventos (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrdemServicoId UNIQUEIDENTIFIER NOT NULL,
        DataEvento DATETIME NOT NULL DEFAULT GETDATE(),
        Titulo NVARCHAR(200) NOT NULL,
        Descricao NVARCHAR(1000),
        Tipo NVARCHAR(50),
        Usuario NVARCHAR(200),
        Ativo BIT NOT NULL DEFAULT 1,
        FOREIGN KEY (OrdemServicoId) REFERENCES OrdensServico(Id) ON DELETE CASCADE
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
        FuncionarioId INT NOT NULL PRIMARY KEY,
        TentativasFalhas INT NOT NULL DEFAULT 0,
        BloqueadoAte DATETIME,
        UltimaFalhaEm DATETIME,
        UltimoSucessoEm DATETIME,
        AtualizadoEm DATETIME NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (FuncionarioId) REFERENCES Funcionarios(Id)
    );
    
    CREATE INDEX IX_LoginTentativasSeguranca_BloqueadoAte ON LoginTentativasSeguranca(BloqueadoAte);
END

IF COL_LENGTH('LoginTentativasSeguranca', 'FuncionarioId') IS NULL
    ALTER TABLE LoginTentativasSeguranca ADD FuncionarioId INT NULL;
IF COL_LENGTH('LoginTentativasSeguranca', 'TentativasFalhas') IS NULL
    ALTER TABLE LoginTentativasSeguranca ADD TentativasFalhas INT NOT NULL CONSTRAINT DF_LoginTentativas_TentativasFalhas DEFAULT 0;
IF COL_LENGTH('LoginTentativasSeguranca', 'BloqueadoAte') IS NULL
    ALTER TABLE LoginTentativasSeguranca ADD BloqueadoAte DATETIME;
IF COL_LENGTH('LoginTentativasSeguranca', 'UltimaFalhaEm') IS NULL
    ALTER TABLE LoginTentativasSeguranca ADD UltimaFalhaEm DATETIME;
IF COL_LENGTH('LoginTentativasSeguranca', 'UltimoSucessoEm') IS NULL
    ALTER TABLE LoginTentativasSeguranca ADD UltimoSucessoEm DATETIME;
IF COL_LENGTH('LoginTentativasSeguranca', 'AtualizadoEm') IS NULL
    ALTER TABLE LoginTentativasSeguranca ADD AtualizadoEm DATETIME NOT NULL CONSTRAINT DF_LoginTentativas_AtualizadoEm DEFAULT GETDATE();

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
    CREATE UNIQUE INDEX UX_RecordLocks_Entity_Active ON RecordLocks(EntityType, EntityId) WHERE IsActive = 1;
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

