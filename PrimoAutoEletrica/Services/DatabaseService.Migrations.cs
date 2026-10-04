using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        private void ApplyDatabaseMigrations(DbConnection connection)
        {
            InitializeMigrationSchema(connection);

            ApplyMigration(connection, "202605210001", "Indices operacionais para consultas criticas", CriarIndicesOperacionais);
            ApplyMigration(connection, "202605210002", "Colunas de concorrencia e ultima alteracao", CriarColunasConcorrencia);
            ApplyMigration(connection, "202605210003", "Tabela de bloqueio inteligente de registros", CriarTabelaBloqueios);
            ApplyMigration(connection, "202605210004", "Tabela de historico de backups", CriarTabelaHistoricoBackups);
            ApplyMigration(connection, "202605210005", "Detalhamento de auditoria com valores e correlacao", ExpandirAuditoria);
            ApplyMigration(connection, "202605210006", "Indices condicionais de integridade", CriarIndicesCondicionais);
            ApplyMigration(connection, "202605230001", "Tabela de seguranca para tentativas de login", CriarTabelaTentativasLogin);
            ApplyMigration(connection, "202605240001", "CPF de funcionario para cadastro e validacao central", AdicionarCpfFuncionario);
            ApplyMigration(connection, "202605240002", "Operacao real de caixa e ciclo de venda profissional", CriarEstruturaCaixaEVendaProfissional);
            ApplyMigration(connection, "202605280001", "Tabela de sessoes ativas para monitoramento multiusuario", CriarTabelaSessoesUsuario);
            ApplyMigration(connection, "202605280002", "Tabela de bloqueios ativos para edicao concorrente", CriarTabelaRecordLocks);
            ApplyMigration(connection, "202605310001", "Imagem persistida de clientes e conferencia de importacao NF-e", ExpandirClientesEImportacoesNfe);
            ApplyMigration(connection, "202605310002", "Expansao operacional de veiculos e ordens de servico", ExpandirVeiculosEOrdensServico);
            ApplyMigration(connection, "202605310003", "Vinculos operacionais de fornecedores e foto de funcionarios", ExpandirFornecedoresEFuncionarios);
            ApplyMigration(connection, "202605310004", "Venda e orcamento com itens de servico e mao de obra", ExpandirVendaItensParaServicos);
            ApplyMigration(connection, "202606010001", "LGPD e consentimento de contato para clientes", ExpandirClientesLgpd);
            ApplyMigration(connection, "202606010002", "Anexos operacionais para produtos", ExpandirProdutosComAnexos);
            ApplyMigration(connection, "202606030001", "Estrutura persistente para catalogo de pecas e historico de importacao", CriarEstruturaCatalogoPecas);
            ApplyMigration(connection, "202606040001", "ProdutoFornecedor operacional com compras e prazos", CriarEstruturaProdutoFornecedorOperacional);
            ApplyMigration(connection, "202606110001", "Politica de troca obrigatoria de senha", AdicionarPoliticaTrocaSenhaFuncionarios);
            ApplyMigration(connection, "202606110002", "Tipo de pessoa para cadastro de clientes", AdicionarTipoPessoaClientes);
            ApplyMigration(connection, "202606110003", "Orcamentos com veiculo, diagnostico e desconto percentual", ExpandirOrcamentosVeiculoDiagnosticoDesconto);
            ApplyMigration(connection, "202606110004", "Termo de autorizacao para ordens de servico", AdicionarTermoAutorizacaoOrdensServico);
            ApplyMigration(connection, "202606110005", "Observacoes operacionais para funcionarios", AdicionarObservacoesFuncionarios);
            ApplyMigration(connection, "202606110006", "Configuracoes do sistema persistidas no banco", CriarConfiguracoesSistema);
            ApplyMigration(connection, "202606150001", "Correcoes enterprise de integridade SQLite", CorrigirIntegridadeSqliteEnterprise);
            ApplyMigration(connection, "202609060001", "Soft delete LGPD em entidades principais", AdicionarSoftDeleteLgpd);
            ApplyMigration(connection, "202609080001", "Fundacao fiscal: operacoes, documentos e eventos", CriarEstruturaFiscalFoundation);
            ApplyMigration(connection, "202610030001", "Gestao de compras: pedidos, itens e controle de reposicao", CriarEstruturaGestaoCompras);
            ApplyMigration(connection, "202610030002", "Gestao de ferramental: catalogo, movimentacoes e rastreio de posse", CriarEstruturaFerramentaria);
            ApplyMigration(connection, "202610030003", "Multi-filial corporativo e transferencias de estoque inter-lojas", CriarEstruturaMultiFilialETransferencias);
            ApplyMigration(connection, "202610030004", "Gestao de frotas e contratos corporativos B2B", CriarEstruturaGestaoFrotas);
            ApplyMigration(connection, "202610040001", "DVI 2.0: Inspecoes digitais veiculares, itens categorizados e laudo fotografico", CriarEstruturaDviInspecoes);
            ApplyMigration(connection, "202610040002", "SureTrack: Casos resolvidos da oficina, estatisticas de falhas e atalhos de diagnostico", CriarEstruturaSureTrack);
            ApplyMigration(connection, "202610040003", "Biblioteca Tecnica: Pinagens de Modulos ECU/BCM, Centrais de Fusiveis e Linha Pesada 24V", CriarEstruturaBibliotecaTecnica);
            ApplyMigration(connection, "202610040004", "Diagnostico Guiado: Fluxogramas de Troubleshooting e Calculadora de Queda de Tensao", CriarEstruturaDiagnosticoGuiado);
        }

        private static void InitializeMigrationSchema(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS SchemaMigrations
                (
                    Id TEXT PRIMARY KEY,
                    Descricao TEXT NOT NULL,
                    AplicadaEm TEXT NOT NULL,
                    VersaoAplicacao TEXT,
                    Maquina TEXT
                );

                CREATE TABLE IF NOT EXISTS SchemaVersion
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Version TEXT NOT NULL UNIQUE,
                    AppliedAt TEXT NOT NULL,
                    Description TEXT NOT NULL
                );

                INSERT OR IGNORE INTO SchemaVersion
                (
                    Version,
                    AppliedAt,
                    Description
                )
                SELECT
                    Id,
                    AplicadaEm,
                    Descricao
                FROM SchemaMigrations;";
            command.ExecuteNonQuery();
        }

        private static void ApplyMigration(
            DbConnection connection,
            string id,
            string descricao,
            Action<DbConnection, DbTransaction> migration)
        {
            if (MigrationExists(connection, id))
            {
                return;
            }

            using var transaction = connection.BeginTransaction();

            migration(connection, transaction);

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO SchemaMigrations
                (
                    Id,
                    Descricao,
                    AplicadaEm,
                    VersaoAplicacao,
                    Maquina
                )
                VALUES
                (
                    @Id,
                    @Descricao,
                    @AplicadaEm,
                    @VersaoAplicacao,
                    @Maquina
                );";
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Descricao", descricao);
            command.Parameters.AddWithValue("@AplicadaEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@VersaoAplicacao", typeof(DatabaseService).Assembly.GetName().Version?.ToString() ?? "dev");
            command.Parameters.AddWithValue("@Maquina", Environment.MachineName);
            command.ExecuteNonQuery();

            RegisterSchemaVersion(connection, transaction, id, descricao);

            transaction.Commit();
        }

        private static void RegisterSchemaVersion(
            DbConnection connection,
            DbTransaction transaction,
            string version,
            string description)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT OR IGNORE INTO SchemaVersion
                (
                    Version,
                    AppliedAt,
                    Description
                )
                VALUES
                (
                    @Version,
                    @AppliedAt,
                    @Description
                );";
            command.Parameters.AddWithValue("@Version", version);
            command.Parameters.AddWithValue("@AppliedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Description", description);
            command.ExecuteNonQuery();
        }

        public List<string> GetAppliedMigrations()
        {
            var migrations = new List<string>();

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Version
                FROM SchemaVersion
                ORDER BY AppliedAt, Version;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                migrations.Add(reader.GetString(0));
            }

            return migrations;
        }

        private static bool MigrationExists(DbConnection connection, string id)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM SchemaMigrations WHERE Id = @Id LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id);
            return command.ExecuteScalar() != null;
        }

        private static void CriarIndicesOperacionais(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Produtos_Codigo ON Produtos (Codigo);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Produtos_Nome ON Produtos (Nome);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Produtos_Ativo_Estoque ON Produtos (Ativo, QuantidadeEstoque);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Clientes_Nome ON Clientes (Nome);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Clientes_CPF ON Clientes (CPF);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Clientes_Ativo_Vip_Nome ON Clientes (Ativo, ClienteVip, Nome);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Funcionarios_Email_Ativo ON Funcionarios (Email, Ativo);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Fornecedores_CNPJ ON Fornecedores (CNPJ);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Veiculos_ClienteId ON Veiculos (ClienteId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Veiculos_Placa ON Veiculos (Placa);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Vendas_Data ON Vendas (Data);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_VendaItens_VendaId ON VendaItens (VendaId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_VendaItens_ProdutoId ON VendaItens (ProdutoId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ImportacoesNFe_ChaveAcesso ON ImportacoesNFe (ChaveAcesso);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Produtos_Categoria_Marca_Nome ON Produtos (Categoria, Marca, Nome);");

            if (MigrationTableExists(connection, transaction, "Orcamentos"))
            {
                ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Orcamentos_Data_Status ON Orcamentos (DataCriacao DESC, Status);");
            }

            if (MigrationTableExists(connection, transaction, "MovimentacoesFinanceiras"))
            {
                ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_MovimentacoesFinanceiras_Data_Tipo_Categoria ON MovimentacoesFinanceiras (Data DESC, Tipo, Categoria);");
            }

            if (MigrationTableExists(connection, transaction, "ContasPagar"))
            {
                ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ContasPagar_Status_Vencimento ON ContasPagar (Status, DataVencimento);");
            }

            if (MigrationTableExists(connection, transaction, "ContasReceber"))
            {
                ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ContasReceber_Status_Vencimento ON ContasReceber (Status, DataVencimento);");
            }
        }

        private static void CriarColunasConcorrencia(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "Produtos", "RowVersion", "ALTER TABLE Produtos ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "Produtos", "DataUltimaAlteracao", "ALTER TABLE Produtos ADD COLUMN DataUltimaAlteracao TEXT;");
            EnsureMigrationColumn(connection, transaction, "Clientes", "RowVersion", "ALTER TABLE Clientes ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "Clientes", "DataUltimaAlteracao", "ALTER TABLE Clientes ADD COLUMN DataUltimaAlteracao TEXT;");
            EnsureMigrationColumn(connection, transaction, "Funcionarios", "RowVersion", "ALTER TABLE Funcionarios ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "Funcionarios", "DataUltimaAlteracao", "ALTER TABLE Funcionarios ADD COLUMN DataUltimaAlteracao TEXT;");
            EnsureMigrationColumn(connection, transaction, "Fornecedores", "RowVersion", "ALTER TABLE Fornecedores ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "Fornecedores", "DataUltimaAlteracao", "ALTER TABLE Fornecedores ADD COLUMN DataUltimaAlteracao TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "RowVersion", "ALTER TABLE Veiculos ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "DataUltimaAlteracao", "ALTER TABLE Veiculos ADD COLUMN DataUltimaAlteracao TEXT;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "RowVersion", "ALTER TABLE OrdensServico ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "DataUltimaAlteracao", "ALTER TABLE OrdensServico ADD COLUMN DataUltimaAlteracao TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "Agendamentos", "RowVersion", "ALTER TABLE Agendamentos ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "Agendamentos", "DataUltimaAlteracao", "ALTER TABLE Agendamentos ADD COLUMN DataUltimaAlteracao TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "Orcamentos", "RowVersion", "ALTER TABLE Orcamentos ADD COLUMN RowVersion INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "Orcamentos", "DataUltimaAlteracao", "ALTER TABLE Orcamentos ADD COLUMN DataUltimaAlteracao TEXT;");
        }

        private static void ExpandirProdutosComAnexos(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "Produtos", "Anexos", "ALTER TABLE Produtos ADD COLUMN Anexos TEXT;");
        }

        private static void CriarEstruturaCatalogoPecas(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS CatalogoPecas
                (
                    Id TEXT PRIMARY KEY,
                    CodigoFabricante TEXT,
                    CodigoNormalizado TEXT,
                    Marca TEXT,
                    Nome TEXT,
                    Descricao TEXT,
                    Categoria TEXT,
                    Subcategoria TEXT,
                    Linha TEXT,
                    Aplicacao TEXT,
                    VeiculoAplicacao TEXT,
                    AnoInicial INTEGER NULL,
                    AnoFinal INTEGER NULL,
                    Voltagem TEXT,
                    Amperagem TEXT,
                    QuantidadeTerminais TEXT,
                    TipoProduto TEXT,
                    PaginaCatalogo TEXT,
                    FonteCatalogo TEXT,
                    ArquivoOrigem TEXT,
                    ObservacoesTecnicas TEXT,
                    ImagemUrl TEXT,
                    ImagemLocal TEXT,
                    StatusRevisao TEXT,
                    ProdutoEstoqueId TEXT NULL,
                    DataImportacao TEXT,
                    DataAtualizacao TEXT,
                    Ativo INTEGER NOT NULL DEFAULT 1
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS CatalogoImportacoes
                (
                    Id TEXT PRIMARY KEY,
                    DataImportacao TEXT,
                    ArquivoNome TEXT,
                    ArquivoCaminho TEXT,
                    TipoArquivo TEXT,
                    FonteCatalogo TEXT,
                    MarcaDetectada TEXT,
                    TotalLidos INTEGER NOT NULL DEFAULT 0,
                    TotalImportados INTEGER NOT NULL DEFAULT 0,
                    TotalDuplicados INTEGER NOT NULL DEFAULT 0,
                    TotalComErro INTEGER NOT NULL DEFAULT 0,
                    Status TEXT,
                    Resumo TEXT,
                    LogDetalhado TEXT,
                    Usuario TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS CatalogoImportacaoErros
                (
                    Id TEXT PRIMARY KEY,
                    ImportacaoId TEXT,
                    LinhaOrigem TEXT,
                    CodigoDetectado TEXT,
                    MensagemErro TEXT,
                    ConteudoOriginal TEXT,
                    DataErro TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IDX_CatalogoPecas_CodigoNormalizado ON CatalogoPecas (CodigoNormalizado);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IDX_CatalogoPecas_Marca ON CatalogoPecas (Marca);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IDX_CatalogoPecas_Categoria ON CatalogoPecas (Categoria);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IDX_CatalogoPecas_StatusRevisao ON CatalogoPecas (StatusRevisao);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IDX_CatalogoPecas_ProdutoEstoqueId ON CatalogoPecas (ProdutoEstoqueId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IDX_CatalogoImportacoes_DataImportacao ON CatalogoImportacoes (DataImportacao DESC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IDX_CatalogoImportacaoErros_ImportacaoId ON CatalogoImportacaoErros (ImportacaoId);");
        }

        private static void CriarEstruturaProdutoFornecedorOperacional(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS ProdutoFornecedores
                (
                    Id TEXT PRIMARY KEY,
                    ProdutoId TEXT NOT NULL,
                    FornecedorId TEXT NOT NULL,
                    CodigoProduto TEXT,
                    NomeProduto TEXT,
                    CategoriaProduto TEXT,
                    CodigoFornecedor TEXT,
                    PrecoUltimaCompra REAL NOT NULL DEFAULT 0,
                    QuantidadeUltimaCompra REAL NOT NULL DEFAULT 0,
                    PrazoEntregaDias INTEGER NOT NULL DEFAULT 0,
                    QuantidadeCompras INTEGER NOT NULL DEFAULT 0,
                    ValorCompras REAL NOT NULL DEFAULT 0,
                    DataUltimaCompra TEXT,
                    ChaveUltimaNFe TEXT,
                    NumeroUltimaNFe TEXT,
                    Ativo INTEGER NOT NULL DEFAULT 1,
                    Origem TEXT,
                    Observacoes TEXT,
                    DataCadastro TEXT NOT NULL,
                    DataUltimaAtualizacao TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE UNIQUE INDEX IF NOT EXISTS IX_ProdutoFornecedores_Produto_Fornecedor ON ProdutoFornecedores (ProdutoId, FornecedorId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ProdutoFornecedores_Fornecedor ON ProdutoFornecedores (FornecedorId, Ativo, DataUltimaCompra DESC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ProdutoFornecedores_Produto ON ProdutoFornecedores (ProdutoId);");
        }

        private static void CriarTabelaBloqueios(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS RegistroBloqueios
                (
                    Id TEXT PRIMARY KEY,
                    Entidade TEXT NOT NULL,
                    EntidadeId TEXT NOT NULL,
                    UsuarioId INTEGER,
                    UsuarioNome TEXT,
                    SessaoId TEXT,
                    Maquina TEXT,
                    CriadoEm TEXT NOT NULL,
                    ExpiraEm TEXT NOT NULL,
                    Motivo TEXT,
                    Ativo INTEGER NOT NULL DEFAULT 1
                );");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_RegistroBloqueios_Entidade ON RegistroBloqueios (Entidade, EntidadeId, Ativo);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_RegistroBloqueios_ExpiraEm ON RegistroBloqueios (ExpiraEm);");
        }

        private static void CriarTabelaHistoricoBackups(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS DatabaseBackups
                (
                    Id TEXT PRIMARY KEY,
                    Caminho TEXT NOT NULL,
                    Tipo TEXT NOT NULL,
                    TamanhoBytes INTEGER NOT NULL DEFAULT 0,
                    CriadoEm TEXT NOT NULL,
                    VerificadoEm TEXT,
                    HashSha256 TEXT,
                    Status TEXT NOT NULL DEFAULT 'Criado',
                    Mensagem TEXT
                );");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_DatabaseBackups_CriadoEm ON DatabaseBackups (CriadoEm DESC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_DatabaseBackups_Status ON DatabaseBackups (Status);");
        }

        private static void ExpandirAuditoria(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "AuditLogs", "ValorAnterior", "ALTER TABLE AuditLogs ADD COLUMN ValorAnterior TEXT;");
            EnsureMigrationColumn(connection, transaction, "AuditLogs", "ValorNovo", "ALTER TABLE AuditLogs ADD COLUMN ValorNovo TEXT;");
            EnsureMigrationColumn(connection, transaction, "AuditLogs", "CorrelationId", "ALTER TABLE AuditLogs ADD COLUMN CorrelationId TEXT;");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_AuditLogs_Usuario_Data ON AuditLogs (UsuarioId, DataHora DESC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_AuditLogs_Sucesso_Severidade ON AuditLogs (Sucesso, Severidade);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_AuditLogs_CorrelationId ON AuditLogs (CorrelationId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_AuditLogs_Categoria_Data ON AuditLogs (Categoria, DataHora DESC);");
        }

        private static void CriarIndicesCondicionais(DbConnection connection, DbTransaction transaction)
        {
            if (!ExisteDuplicidadeImportacaoNFe(connection, transaction))
            {
                ExecuteMigrationCommand(connection, transaction, @"
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_ImportacoesNFe_ChaveAcesso_Unica
                    ON ImportacoesNFe (ChaveAcesso)
                    WHERE ChaveAcesso IS NOT NULL AND trim(ChaveAcesso) <> '';");
            }

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_PerfisAcesso_Nome_Ativo
                ON PerfisAcesso (Nome)
                WHERE Ativo = 1;");
        }

        private static void CriarTabelaTentativasLogin(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS LoginTentativasSeguranca
                (
                    FuncionarioId INTEGER PRIMARY KEY,
                    TentativasFalhas INTEGER NOT NULL DEFAULT 0,
                    BloqueadoAte TEXT,
                    UltimaFalhaEm TEXT,
                    UltimoSucessoEm TEXT,
                    AtualizadoEm TEXT NOT NULL,
                    FOREIGN KEY (FuncionarioId) REFERENCES Funcionarios(Id)
                );");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_LoginTentativasSeguranca_BloqueadoAte ON LoginTentativasSeguranca (BloqueadoAte);");
        }

        private static void AdicionarCpfFuncionario(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "Funcionarios", "CPF", "ALTER TABLE Funcionarios ADD COLUMN CPF TEXT;");
        }

        private static void AdicionarPoliticaTrocaSenhaFuncionarios(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "Funcionarios", "ExigirTrocaSenha", "ALTER TABLE Funcionarios ADD COLUMN ExigirTrocaSenha INTEGER NOT NULL DEFAULT 0;");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Funcionarios_ExigirTrocaSenha ON Funcionarios (ExigirTrocaSenha, Ativo);");
        }

        private static void CriarConfiguracoesSistema(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS ConfiguracoesSistema
                (
                    Chave TEXT PRIMARY KEY,
                    Valor TEXT NOT NULL,
                    Grupo TEXT NOT NULL,
                    AtualizadoEm TEXT NOT NULL,
                    AtualizadoPor TEXT
                );");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ConfiguracoesSistema_Grupo ON ConfiguracoesSistema (Grupo);");
        }

        private static void CriarEstruturaCaixaEVendaProfissional(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS CaixaSessoes
                (
                    Id TEXT PRIMARY KEY,
                    NumeroCaixa TEXT NOT NULL,
                    DataAbertura TEXT NOT NULL,
                    DataFechamento TEXT,
                    OperadorId INTEGER,
                    OperadorNome TEXT NOT NULL,
                    PerfilOperador TEXT,
                    ValorAbertura REAL NOT NULL DEFAULT 0,
                    ValorEsperado REAL NOT NULL DEFAULT 0,
                    ValorInformadoFechamento REAL,
                    TotalVendas REAL NOT NULL DEFAULT 0,
                    TotalSangrias REAL NOT NULL DEFAULT 0,
                    TotalSuprimentos REAL NOT NULL DEFAULT 0,
                    QuantidadeVendas INTEGER NOT NULL DEFAULT 0,
                    Status TEXT NOT NULL DEFAULT 'Fechado',
                    Observacoes TEXT,
                    DataCriacao TEXT NOT NULL,
                    DataUltimaMovimentacao TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS MovimentacoesCaixa
                (
                    Id TEXT PRIMARY KEY,
                    CaixaSessaoId TEXT NOT NULL,
                    Data TEXT NOT NULL,
                    Tipo TEXT NOT NULL,
                    ValorMovimento REAL NOT NULL DEFAULT 0,
                    ValorInicial REAL NOT NULL DEFAULT 0,
                    ValorFinal REAL NOT NULL DEFAULT 0,
                    Sangrias REAL NOT NULL DEFAULT 0,
                    Suprimentos REAL NOT NULL DEFAULT 0,
                    Diferenca REAL NOT NULL DEFAULT 0,
                    Operador TEXT,
                    FormaPagamento TEXT,
                    ReferenciaId TEXT,
                    Observacoes TEXT,
                    FOREIGN KEY (CaixaSessaoId) REFERENCES CaixaSessoes(Id)
                );");

            EnsureMigrationColumn(connection, transaction, "Vendas", "Status", "ALTER TABLE Vendas ADD COLUMN Status TEXT NOT NULL DEFAULT 'Concluida';");
            EnsureMigrationColumn(connection, transaction, "Vendas", "CaixaSessaoId", "ALTER TABLE Vendas ADD COLUMN CaixaSessaoId TEXT;");
            EnsureMigrationColumn(connection, transaction, "Vendas", "DataCancelamento", "ALTER TABLE Vendas ADD COLUMN DataCancelamento TEXT;");
            EnsureMigrationColumn(connection, transaction, "Vendas", "CanceladoPor", "ALTER TABLE Vendas ADD COLUMN CanceladoPor TEXT;");
            EnsureMigrationColumn(connection, transaction, "Vendas", "MotivoCancelamento", "ALTER TABLE Vendas ADD COLUMN MotivoCancelamento TEXT;");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_CaixaSessoes_Status_Data ON CaixaSessoes (Status, DataAbertura DESC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_CaixaSessoes_Operador_Status ON CaixaSessoes (OperadorId, Status);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_MovimentacoesCaixa_Sessao_Data ON MovimentacoesCaixa (CaixaSessaoId, Data DESC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_MovimentacoesCaixa_Tipo_Data ON MovimentacoesCaixa (Tipo, Data DESC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Vendas_Status_Data ON Vendas (Status, Data DESC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Vendas_Data_Status_Cliente ON Vendas (Data DESC, Status, ClienteId);");
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE UNIQUE INDEX IF NOT EXISTS UX_CaixaSessoes_Operador_Aberto
                ON CaixaSessoes (OperadorId)
                WHERE OperadorId IS NOT NULL AND Status = 'Aberto';");
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE UNIQUE INDEX IF NOT EXISTS UX_CaixaSessoes_Numero_Aberto
                ON CaixaSessoes (NumeroCaixa)
                WHERE Status = 'Aberto';");
        }

        private static void CriarTabelaSessoesUsuario(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS UserSessions
                (
                    Id TEXT PRIMARY KEY,
                    SessionId TEXT NOT NULL,
                    UserId INTEGER NOT NULL,
                    NomeUsuario TEXT NOT NULL,
                    Perfil TEXT,
                    MachineName TEXT,
                    MachineUserName TEXT,
                    IpAddress TEXT,
                    LoginAt TEXT NOT NULL,
                    LastSeenAt TEXT NOT NULL,
                    LogoutAt TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    AppVersion TEXT,
                    DatabaseProvider TEXT
                );");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_UserSessions_SessionId ON UserSessions (SessionId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_UserSessions_UserId ON UserSessions (UserId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_UserSessions_IsActive ON UserSessions (IsActive);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_UserSessions_LastSeenAt ON UserSessions (LastSeenAt DESC);");
        }

        private static void CriarTabelaRecordLocks(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS RecordLocks
                (
                    Id TEXT PRIMARY KEY,
                    EntityType TEXT NOT NULL,
                    EntityId TEXT NOT NULL,
                    EntityDescription TEXT,
                    LockedByUserId INTEGER NOT NULL,
                    LockedByUserName TEXT,
                    SessionId TEXT,
                    MachineName TEXT,
                    LockedAt TEXT NOT NULL,
                    ExpiresAt TEXT NOT NULL,
                    LastRenewedAt TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_RecordLocks_Entity ON RecordLocks (EntityType, EntityId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_RecordLocks_LockedBy ON RecordLocks (LockedByUserId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_RecordLocks_SessionId ON RecordLocks (SessionId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_RecordLocks_IsActive ON RecordLocks (IsActive);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_RecordLocks_ExpiresAt ON RecordLocks (ExpiresAt);");
        }

        private static void CorrigirIntegridadeSqliteEnterprise(DbConnection connection, DbTransaction transaction)
        {
            if (MigrationTableExists(connection, transaction, "Clientes"))
            {
                EnsureMigrationColumn(connection, transaction, "Clientes", "RG", "ALTER TABLE Clientes ADD COLUMN RG TEXT;");
            }

            CorrigirSchemaContatosFornecedor(connection, transaction);

            if (MigrationTableExists(connection, transaction, "RecordLocks"))
            {
                ExecuteMigrationCommand(connection, transaction, @"
                    UPDATE RecordLocks
                    SET IsActive = 0
                    WHERE IsActive = 1
                      AND ExpiresAt IS NOT NULL
                      AND datetime(ExpiresAt) <= datetime('now');");

                ExecuteMigrationCommand(connection, transaction, @"
                    UPDATE RecordLocks
                    SET IsActive = 0
                    WHERE IsActive = 1
                      AND rowid NOT IN
                      (
                          SELECT MAX(rowid)
                          FROM RecordLocks
                          WHERE IsActive = 1
                          GROUP BY EntityType, EntityId
                      );");

                ExecuteMigrationCommand(connection, transaction, @"
                    CREATE UNIQUE INDEX IF NOT EXISTS UX_RecordLocks_Entity_Active
                    ON RecordLocks (EntityType, EntityId)
                    WHERE IsActive = 1;");
            }
        }

        private static void CorrigirSchemaContatosFornecedor(DbConnection connection, DbTransaction transaction)
        {
            if (!MigrationTableExists(connection, transaction, "ContatosFornecedor") ||
                !MigrationTableExists(connection, transaction, "Fornecedores"))
            {
                return;
            }

            var fornecedorIdType = ObterTipoColuna(connection, transaction, "ContatosFornecedor", "FornecedorId");
            var temCascade = ContatosFornecedorTemCascade(connection, transaction);

            if (string.Equals(fornecedorIdType, "TEXT", StringComparison.OrdinalIgnoreCase) && temCascade)
            {
                ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ContatosFornecedor_FornecedorId ON ContatosFornecedor (FornecedorId);");
                return;
            }

            ExecuteMigrationCommand(connection, transaction, "DROP TABLE IF EXISTS ContatosFornecedor_MigracaoEnterprise;");
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE ContatosFornecedor_MigracaoEnterprise
                (
                    Id TEXT PRIMARY KEY,
                    FornecedorId TEXT NOT NULL,
                    Nome TEXT NOT NULL,
                    Cargo TEXT,
                    Telefone TEXT,
                    Email TEXT,
                    Principal INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (FornecedorId) REFERENCES Fornecedores(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                INSERT INTO ContatosFornecedor_MigracaoEnterprise
                (
                    Id,
                    FornecedorId,
                    Nome,
                    Cargo,
                    Telefone,
                    Email,
                    Principal
                )
                SELECT
                    Id,
                    CAST(FornecedorId AS TEXT),
                    Nome,
                    Cargo,
                    Telefone,
                    Email,
                    Principal
                FROM ContatosFornecedor
                WHERE EXISTS
                (
                    SELECT 1
                    FROM Fornecedores
                    WHERE Fornecedores.Id = CAST(ContatosFornecedor.FornecedorId AS TEXT)
                );");

            ExecuteMigrationCommand(connection, transaction, "DROP TABLE ContatosFornecedor;");
            ExecuteMigrationCommand(connection, transaction, "ALTER TABLE ContatosFornecedor_MigracaoEnterprise RENAME TO ContatosFornecedor;");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ContatosFornecedor_FornecedorId ON ContatosFornecedor (FornecedorId);");
        }

        private static void ExpandirClientesEImportacoesNfe(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "Clientes", "ImagemUrl", "ALTER TABLE Clientes ADD COLUMN ImagemUrl TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "SelecionadoParaImportacao", "ALTER TABLE ImportacoesItens ADD COLUMN SelecionadoParaImportacao INTEGER NOT NULL DEFAULT 1;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "AcaoPlanejada", "ALTER TABLE ImportacoesItens ADD COLUMN AcaoPlanejada TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "CategoriaSugerida", "ALTER TABLE ImportacoesItens ADD COLUMN CategoriaSugerida TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "MargemAplicada", "ALTER TABLE ImportacoesItens ADD COLUMN MargemAplicada REAL NOT NULL DEFAULT 0;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "PrecoVendaSugerido", "ALTER TABLE ImportacoesItens ADD COLUMN PrecoVendaSugerido REAL NOT NULL DEFAULT 0;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "ProdutoVinculadoReferencia", "ALTER TABLE ImportacoesItens ADD COLUMN ProdutoVinculadoReferencia TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "CodigoBarras", "ALTER TABLE ImportacoesItens ADD COLUMN CodigoBarras TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "ObservacaoConferencia", "ALTER TABLE ImportacoesItens ADD COLUMN ObservacaoConferencia TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "ProdutoSnapshotAnterior", "ALTER TABLE ImportacoesItens ADD COLUMN ProdutoSnapshotAnterior TEXT;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesItens", "ProdutoSnapshotPosterior", "ALTER TABLE ImportacoesItens ADD COLUMN ProdutoSnapshotPosterior TEXT;");

            EnsureMigrationColumnIfTableExists(connection, transaction, "ImportacoesNFeRollbacks", "AtualizacoesRevertidas", "ALTER TABLE ImportacoesNFeRollbacks ADD COLUMN AtualizacoesRevertidas INTEGER NOT NULL DEFAULT 0;");
        }

        private static void ExpandirVeiculosEOrdensServico(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "Veiculos", "ImagemUrl", "ALTER TABLE Veiculos ADD COLUMN ImagemUrl TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "DocumentoImagemUrl", "ALTER TABLE Veiculos ADD COLUMN DocumentoImagemUrl TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "TipoVeiculo", "ALTER TABLE Veiculos ADD COLUMN TipoVeiculo TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "SistemaEletrico", "ALTER TABLE Veiculos ADD COLUMN SistemaEletrico TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "BateriaPrincipal", "ALTER TABLE Veiculos ADD COLUMN BateriaPrincipal TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "BateriaAuxiliar", "ALTER TABLE Veiculos ADD COLUMN BateriaAuxiliar TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "BateriaInstalada", "ALTER TABLE Veiculos ADD COLUMN BateriaInstalada TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "BateriaMarca", "ALTER TABLE Veiculos ADD COLUMN BateriaMarca TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "BateriaAmperagem", "ALTER TABLE Veiculos ADD COLUMN BateriaAmperagem TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "BateriaDataInstalacao", "ALTER TABLE Veiculos ADD COLUMN BateriaDataInstalacao TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "Alternador", "ALTER TABLE Veiculos ADD COLUMN Alternador TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "MotorPartida", "ALTER TABLE Veiculos ADD COLUMN MotorPartida TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "TesteTensaoRepouso", "ALTER TABLE Veiculos ADD COLUMN TesteTensaoRepouso TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "TesteTensaoPartida", "ALTER TABLE Veiculos ADD COLUMN TesteTensaoPartida TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "TesteCargaAlternador", "ALTER TABLE Veiculos ADD COLUMN TesteCargaAlternador TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "CorrenteFuga", "ALTER TABLE Veiculos ADD COLUMN CorrenteFuga TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "EstadoAterramentos", "ALTER TABLE Veiculos ADD COLUMN EstadoAterramentos TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "ChicotesReparados", "ALTER TABLE Veiculos ADD COLUMN ChicotesReparados TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "FusiveisSubstituidos", "ALTER TABLE Veiculos ADD COLUMN FusiveisSubstituidos TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "RelesSubstituidos", "ALTER TABLE Veiculos ADD COLUMN RelesSubstituidos TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "LampadasSubstituidas", "ALTER TABLE Veiculos ADD COLUMN LampadasSubstituidas TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "AcessoriosInstalados", "ALTER TABLE Veiculos ADD COLUMN AcessoriosInstalados TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "ObservacoesTecnicasEletricas", "ALTER TABLE Veiculos ADD COLUMN ObservacoesTecnicasEletricas TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "FotosTecnicas", "ALTER TABLE Veiculos ADD COLUMN FotosTecnicas TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "HistoricoTecnico", "ALTER TABLE Veiculos ADD COLUMN HistoricoTecnico TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "ObservacoesEletricasRecorrentes", "ALTER TABLE Veiculos ADD COLUMN ObservacoesEletricasRecorrentes TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "ProblemaRecorrente", "ALTER TABLE Veiculos ADD COLUMN ProblemaRecorrente TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "ObservacaoImportanteTecnico", "ALTER TABLE Veiculos ADD COLUMN ObservacaoImportanteTecnico TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "RetornoRecomendadoEm", "ALTER TABLE Veiculos ADD COLUMN RetornoRecomendadoEm TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "GarantiaValidaAte", "ALTER TABLE Veiculos ADD COLUMN GarantiaValidaAte TEXT;");
            EnsureMigrationColumn(connection, transaction, "Veiculos", "ProximaRevisaoEm", "ALTER TABLE Veiculos ADD COLUMN ProximaRevisaoEm TEXT;");

            EnsureMigrationColumn(connection, transaction, "OrdensServico", "ChecklistEntrada", "ALTER TABLE OrdensServico ADD COLUMN ChecklistEntrada TEXT;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "ChecklistSaida", "ALTER TABLE OrdensServico ADD COLUMN ChecklistSaida TEXT;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "FotosAntes", "ALTER TABLE OrdensServico ADD COLUMN FotosAntes TEXT;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "FotosDepois", "ALTER TABLE OrdensServico ADD COLUMN FotosDepois TEXT;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "DiagnosticoInicial", "ALTER TABLE OrdensServico ADD COLUMN DiagnosticoInicial TEXT;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "DiagnosticoFinal", "ALTER TABLE OrdensServico ADD COLUMN DiagnosticoFinal TEXT;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "TempoPrevistoMinutos", "ALTER TABLE OrdensServico ADD COLUMN TempoPrevistoMinutos INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "TempoRealMinutos", "ALTER TABLE OrdensServico ADD COLUMN TempoRealMinutos INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "OrcamentoId", "ALTER TABLE OrdensServico ADD COLUMN OrcamentoId TEXT;");
            EnsureMigrationColumn(connection, transaction, "OrdensServico", "AssinaturaClienteUrl", "ALTER TABLE OrdensServico ADD COLUMN AssinaturaClienteUrl TEXT;");
        }

        private static void ExpandirFornecedoresEFuncionarios(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "Funcionarios", "Foto", "ALTER TABLE Funcionarios ADD COLUMN Foto TEXT;");
            EnsureMigrationColumn(connection, transaction, "Funcionarios", "Observacoes", "ALTER TABLE Funcionarios ADD COLUMN Observacoes TEXT;");

            EnsureMigrationColumn(connection, transaction, "Fornecedores", "WhatsAppVendedor", "ALTER TABLE Fornecedores ADD COLUMN WhatsAppVendedor TEXT;");
            EnsureMigrationColumn(connection, transaction, "Fornecedores", "PrazoMedioPagamentoDias", "ALTER TABLE Fornecedores ADD COLUMN PrazoMedioPagamentoDias INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "Fornecedores", "PrazoMedioEntregaDias", "ALTER TABLE Fornecedores ADD COLUMN PrazoMedioEntregaDias INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "Fornecedores", "CategoriaPreferencial", "ALTER TABLE Fornecedores ADD COLUMN CategoriaPreferencial TEXT;");

            EnsureMigrationColumn(connection, transaction, "Produtos", "FornecedorId", "ALTER TABLE Produtos ADD COLUMN FornecedorId TEXT;");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Produtos_FornecedorId ON Produtos (FornecedorId);");

            ExecuteMigrationCommand(connection, transaction, @"
                UPDATE Produtos
                SET FornecedorId = (
                    SELECT f.Id
                    FROM Fornecedores f
                    WHERE replace(replace(replace(lower(COALESCE(f.CNPJ, '')), '.', ''), '/', ''), '-', '') =
                          replace(replace(replace(lower(COALESCE(Produtos.CNPJFornecedor, '')), '.', ''), '/', ''), '-', '')
                    LIMIT 1
                )
                WHERE trim(COALESCE(FornecedorId, '')) = ''
                  AND trim(COALESCE(CNPJFornecedor, '')) <> '';");

            ExecuteMigrationCommand(connection, transaction, @"
                UPDATE Produtos
                SET FornecedorId = (
                    SELECT f.Id
                    FROM Fornecedores f
                    WHERE lower(trim(COALESCE(f.NomeFantasia, ''))) = lower(trim(COALESCE(Produtos.Fornecedor, '')))
                       OR lower(trim(COALESCE(f.RazaoSocial, ''))) = lower(trim(COALESCE(Produtos.Fornecedor, '')))
                    LIMIT 1
                )
                WHERE trim(COALESCE(FornecedorId, '')) = ''
                  AND trim(COALESCE(Fornecedor, '')) <> '';");
        }

        private static void AdicionarObservacoesFuncionarios(DbConnection connection, DbTransaction transaction)
        {
            EnsureMigrationColumn(connection, transaction, "Funcionarios", "Observacoes", "ALTER TABLE Funcionarios ADD COLUMN Observacoes TEXT;");
        }

        private static void ExpandirVendaItensParaServicos(DbConnection connection, DbTransaction transaction)
        {
            if (!MigrationTableExists(connection, transaction, "VendaItens"))
            {
                return;
            }

            EnsureMigrationColumn(connection, transaction, "VendaItens", "Tipo", "ALTER TABLE VendaItens ADD COLUMN Tipo TEXT NOT NULL DEFAULT 'Produto';");
            EnsureMigrationColumn(connection, transaction, "VendaItens", "DescricaoItem", "ALTER TABLE VendaItens ADD COLUMN DescricaoItem TEXT;");
            EnsureMigrationColumn(connection, transaction, "VendaItens", "CustoUnitario", "ALTER TABLE VendaItens ADD COLUMN CustoUnitario REAL NOT NULL DEFAULT 0;");

            ExecuteMigrationCommand(connection, transaction, "DROP TABLE IF EXISTS VendaItens_MigracaoServico;");
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE VendaItens_MigracaoServico
                (
                    Id TEXT PRIMARY KEY,
                    VendaId TEXT NOT NULL,
                    ProdutoId TEXT,
                    Tipo TEXT NOT NULL DEFAULT 'Produto',
                    DescricaoItem TEXT,
                    ProdutoNome TEXT NOT NULL,
                    Quantidade INTEGER NOT NULL,
                    PrecoUnitario REAL NOT NULL,
                    CustoUnitario REAL NOT NULL DEFAULT 0,
                    Desconto REAL NOT NULL DEFAULT 0,
                    Subtotal REAL NOT NULL,
                    FOREIGN KEY (VendaId) REFERENCES Vendas(Id)
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                INSERT INTO VendaItens_MigracaoServico
                (
                    Id,
                    VendaId,
                    ProdutoId,
                    Tipo,
                    DescricaoItem,
                    ProdutoNome,
                    Quantidade,
                    PrecoUnitario,
                    CustoUnitario,
                    Desconto,
                    Subtotal
                )
                SELECT
                    Id,
                    VendaId,
                    CASE
                        WHEN trim(COALESCE(ProdutoId, '')) = '' THEN NULL
                        ELSE ProdutoId
                    END,
                    COALESCE(Tipo, 'Produto'),
                    CASE
                        WHEN trim(COALESCE(DescricaoItem, '')) = '' THEN ProdutoNome
                        ELSE DescricaoItem
                    END,
                    ProdutoNome,
                    Quantidade,
                    PrecoUnitario,
                    COALESCE(CustoUnitario, 0),
                    Desconto,
                    Subtotal
                FROM VendaItens;");

            ExecuteMigrationCommand(connection, transaction, "DROP TABLE VendaItens;");
            ExecuteMigrationCommand(connection, transaction, "ALTER TABLE VendaItens_MigracaoServico RENAME TO VendaItens;");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_VendaItens_VendaId ON VendaItens (VendaId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_VendaItens_ProdutoId ON VendaItens (ProdutoId);");
        }

        private static void ExpandirClientesLgpd(DbConnection connection, DbTransaction transaction)
        {
            if (!MigrationTableExists(connection, transaction, "Clientes"))
            {
                return;
            }

            EnsureMigrationColumn(connection, transaction, "Clientes", "ConsentimentoLGPD", "ALTER TABLE Clientes ADD COLUMN ConsentimentoLGPD INTEGER NOT NULL DEFAULT 0;");
            EnsureMigrationColumn(connection, transaction, "Clientes", "DataConsentimentoLGPD", "ALTER TABLE Clientes ADD COLUMN DataConsentimentoLGPD TEXT;");
            EnsureMigrationColumn(connection, transaction, "Clientes", "OrigemConsentimentoLGPD", "ALTER TABLE Clientes ADD COLUMN OrigemConsentimentoLGPD TEXT;");
            EnsureMigrationColumn(connection, transaction, "Clientes", "AutorizaContatoWhatsApp", "ALTER TABLE Clientes ADD COLUMN AutorizaContatoWhatsApp INTEGER NOT NULL DEFAULT 0;");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Clientes_LGPD_Contato ON Clientes (ConsentimentoLGPD, AutorizaContatoWhatsApp, Nome);");
        }

        private static void AdicionarSoftDeleteLgpd(DbConnection connection, DbTransaction transaction)
        {
            foreach (var tabela in new[] { "Clientes", "Veiculos", "Produtos", "Orcamentos", "Fornecedores", "Funcionarios", "OrdensServico" })
            {
                if (!MigrationTableExists(connection, transaction, tabela)) continue;
                EnsureMigrationColumn(connection, transaction, tabela, "IsDeleted", $"ALTER TABLE {tabela} ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;");
                EnsureMigrationColumn(connection, transaction, tabela, "ExcluidoEm", $"ALTER TABLE {tabela} ADD COLUMN ExcluidoEm TEXT;");
                EnsureMigrationColumn(connection, transaction, tabela, "ExcluidoPor", $"ALTER TABLE {tabela} ADD COLUMN ExcluidoPor TEXT;");
                ExecuteMigrationCommand(connection, transaction, $"CREATE INDEX IF NOT EXISTS IX_{tabela}_IsDeleted ON {tabela} (IsDeleted);");
            }
        }

        private static void CriarEstruturaFiscalFoundation(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS FiscalOperations
                (
                    Id TEXT PRIMARY KEY,
                    IdempotencyKey TEXT NOT NULL UNIQUE,
                    DocumentType INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    Environment INTEGER NOT NULL,
                    Provider INTEGER NOT NULL,
                    OriginModule TEXT,
                    OrdemServicoId TEXT,
                    VendaId TEXT,
                    OrcamentoId TEXT,
                    ProviderDocumentId TEXT,
                    LastErrorKind TEXT,
                    LastErrorMessage TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS FiscalDocuments
                (
                    Id TEXT PRIMARY KEY,
                    OperationId TEXT NOT NULL,
                    DocumentType INTEGER NOT NULL,
                    Numero TEXT,
                    Serie TEXT,
                    ChaveAcesso TEXT,
                    Status INTEGER NOT NULL,
                    Environment INTEGER NOT NULL,
                    Provider INTEGER NOT NULL,
                    Protocolo TEXT,
                    Reason TEXT,
                    XmlEnviadoPath TEXT,
                    XmlAutorizadoPath TEXT,
                    OrdemServicoId TEXT,
                    VendaId TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (OperationId) REFERENCES FiscalOperations(Id)
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS FiscalEvents
                (
                    Id TEXT PRIMARY KEY,
                    OperationId TEXT NOT NULL,
                    EventType TEXT NOT NULL,
                    Message TEXT,
                    ProviderCode TEXT,
                    CorrelationId TEXT,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (OperationId) REFERENCES FiscalOperations(Id)
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_FiscalOperations_IdempotencyKey ON FiscalOperations (IdempotencyKey);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_FiscalOperations_Status_Env ON FiscalOperations (Status, Environment);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_FiscalDocuments_OperationId ON FiscalDocuments (OperationId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_FiscalDocuments_ChaveAcesso ON FiscalDocuments (ChaveAcesso);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_FiscalEvents_OperationId ON FiscalEvents (OperationId);");
        }

        private static void CriarEstruturaGestaoCompras(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS PedidosCompra
                (
                    Id TEXT PRIMARY KEY,
                    Numero TEXT NOT NULL UNIQUE,
                    FornecedorId TEXT NOT NULL,
                    FornecedorNome TEXT NOT NULL,
                    FornecedorCNPJ TEXT,
                    FornecedorTelefone TEXT,
                    FornecedorEmail TEXT,
                    Status INTEGER NOT NULL DEFAULT 1,
                    ValorTotal REAL NOT NULL DEFAULT 0,
                    DataCriacao TEXT NOT NULL,
                    DataEnvioCotacao TEXT,
                    PrevisaoEntrega TEXT,
                    DataRecebimento TEXT,
                    ChaveNFeVinculada TEXT,
                    NumeroNFe TEXT,
                    FormaPagamento TEXT,
                    CondicaoPagamento TEXT,
                    Observacoes TEXT,
                    CriadoPor TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS PedidosCompraItens
                (
                    Id TEXT PRIMARY KEY,
                    PedidoCompraId TEXT NOT NULL,
                    ProdutoId TEXT NOT NULL,
                    Codigo TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    QuantidadePedida INTEGER NOT NULL,
                    QuantidadeRecebida INTEGER NOT NULL DEFAULT 0,
                    ValorUnitario REAL NOT NULL DEFAULT 0,
                    FOREIGN KEY (PedidoCompraId) REFERENCES PedidosCompra(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PedidosCompra_Status ON PedidosCompra (Status);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PedidosCompra_FornecedorId ON PedidosCompra (FornecedorId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PedidosCompra_DataCriacao ON PedidosCompra (DataCriacao);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PedidosCompraItens_PedidoId ON PedidosCompraItens (PedidoCompraId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PedidosCompraItens_ProdutoId ON PedidosCompraItens (ProdutoId);");

            EnsureMigrationColumnIfTableExists(connection, transaction, "Produtos", "LeadTimeDias", "ALTER TABLE Produtos ADD COLUMN LeadTimeDias INTEGER NOT NULL DEFAULT 3;");
            EnsureMigrationColumnIfTableExists(connection, transaction, "Produtos", "EstoqueSeguranca", "ALTER TABLE Produtos ADD COLUMN EstoqueSeguranca INTEGER NOT NULL DEFAULT 2;");
        }

        private static void CriarEstruturaFerramentaria(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS Ferramentas
                (
                    Id TEXT PRIMARY KEY,
                    CodigoPatrimonio TEXT NOT NULL UNIQUE,
                    Nome TEXT NOT NULL,
                    Categoria INTEGER NOT NULL DEFAULT 1,
                    MarcaModelo TEXT,
                    NumeroSerie TEXT,
                    LocalizacaoArmario TEXT,
                    Status INTEGER NOT NULL DEFAULT 1,
                    ValorAquisicao REAL DEFAULT 0,
                    DataAquisicao TEXT,
                    RequerCalibracaoPeriodica INTEGER DEFAULT 0,
                    IntervaloCalibracaoDias INTEGER DEFAULT 365,
                    UltimaCalibracao TEXT,
                    ProximaCalibracao TEXT,
                    FuncionarioPosseAtualId TEXT,
                    FuncionarioPosseAtualNome TEXT,
                    OrdemServicoAtualId TEXT,
                    NumeroOSAtual TEXT,
                    DataHoraRetiradaAtual TEXT,
                    Observacoes TEXT,
                    Ativo INTEGER DEFAULT 1
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS MovimentacoesFerramentas
                (
                    Id TEXT PRIMARY KEY,
                    FerramentaId TEXT NOT NULL,
                    CodigoPatrimonio TEXT,
                    FerramentaNome TEXT,
                    FuncionarioId TEXT NOT NULL,
                    FuncionarioNome TEXT,
                    OrdemServicoId TEXT,
                    NumeroOS TEXT,
                    DataRetirada TEXT NOT NULL,
                    PrevisaoDevolucao TEXT,
                    DataDevolucao TEXT,
                    EstadoConservacaoRetirada TEXT,
                    EstadoConservacaoDevolucao TEXT,
                    ObservacaoDevolucao TEXT,
                    RegistradoPor TEXT,
                    FOREIGN KEY(FerramentaId) REFERENCES Ferramentas(Id)
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Ferramentas_Status ON Ferramentas (Status);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Ferramentas_Categoria ON Ferramentas (Categoria);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Ferramentas_PosseFunc ON Ferramentas (FuncionarioPosseAtualId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Ferramentas_OSAtual ON Ferramentas (OrdemServicoAtualId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_MovimentacoesFerramentas_FerramentaId ON MovimentacoesFerramentas (FerramentaId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_MovimentacoesFerramentas_FuncionarioId ON MovimentacoesFerramentas (FuncionarioId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_MovimentacoesFerramentas_OSId ON MovimentacoesFerramentas (OrdemServicoId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_MovimentacoesFerramentas_DataRetirada ON MovimentacoesFerramentas (DataRetirada);");
        }

        private static void CriarEstruturaMultiFilialETransferencias(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS Filiais
                (
                    Id TEXT PRIMARY KEY,
                    Codigo TEXT NOT NULL UNIQUE,
                    Nome TEXT NOT NULL,
                    Endereco TEXT,
                    Cidade TEXT,
                    Estado TEXT,
                    Cnpj TEXT,
                    Telefone TEXT,
                    Email TEXT,
                    Gerente TEXT,
                    IsMatriz INTEGER NOT NULL DEFAULT 0,
                    Ativa INTEGER NOT NULL DEFAULT 1,
                    DataAbertura TEXT,
                    CapacidadeEstoque INTEGER DEFAULT 0,
                    Observacoes TEXT,
                    DataCadastro TEXT,
                    DataUltimaAtualizacao TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS TransferenciasEstoque
                (
                    Id TEXT PRIMARY KEY,
                    NumeroTransferencia TEXT NOT NULL UNIQUE,
                    FilialOrigemId TEXT NOT NULL,
                    FilialOrigemNome TEXT NOT NULL,
                    FilialDestinoId TEXT NOT NULL,
                    FilialDestinoNome TEXT NOT NULL,
                    Status INTEGER NOT NULL DEFAULT 1,
                    DataSolicitacao TEXT NOT NULL,
                    DataEnvio TEXT,
                    DataRecebimento TEXT,
                    ResponsavelSolicitacao TEXT,
                    ResponsavelEnvio TEXT,
                    ResponsavelRecebimento TEXT,
                    Observacoes TEXT,
                    ValorTotalEstimado REAL DEFAULT 0
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS TransferenciasEstoqueItens
                (
                    Id TEXT PRIMARY KEY,
                    TransferenciaId TEXT NOT NULL,
                    ProdutoId INTEGER NOT NULL,
                    Codigo TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    QuantidadeEnviada INTEGER NOT NULL,
                    QuantidadeRecebida INTEGER NOT NULL DEFAULT 0,
                    ValorUnitario REAL NOT NULL DEFAULT 0,
                    FOREIGN KEY (TransferenciaId) REFERENCES TransferenciasEstoque(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Filiais_Codigo ON Filiais (Codigo);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Transferencias_Status ON TransferenciasEstoque (Status);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Transferencias_Origem ON TransferenciasEstoque (FilialOrigemId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Transferencias_Destino ON TransferenciasEstoque (FilialDestinoId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_TransferenciasItens_TransferenciaId ON TransferenciasEstoqueItens (TransferenciaId);");
        }

        private static void CriarEstruturaGestaoFrotas(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS ContratosFrotas
                (
                    Id TEXT PRIMARY KEY,
                    ClienteId INTEGER NOT NULL,
                    ClienteNome TEXT NOT NULL,
                    NumeroContrato TEXT NOT NULL UNIQUE,
                    Descricao TEXT,
                    DataInicio TEXT NOT NULL,
                    DataVencimento TEXT NOT NULL,
                    DescontoPecasPercentual REAL NOT NULL DEFAULT 0,
                    DescontoServicosPercentual REAL NOT NULL DEFAULT 0,
                    ValorHoraTecnicaNegociada REAL NOT NULL DEFAULT 0,
                    DiaFechamentoFatura INTEGER NOT NULL DEFAULT 30,
                    DiasVencimentoBoleto INTEGER NOT NULL DEFAULT 15,
                    LimiteCreditoMensal REAL NOT NULL DEFAULT 50000,
                    ExigeAutorizacaoPrevia INTEGER NOT NULL DEFAULT 1,
                    Status INTEGER NOT NULL DEFAULT 1,
                    Observacoes TEXT,
                    DataCadastro TEXT NOT NULL
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS VeiculosFrotas
                (
                    Id TEXT PRIMARY KEY,
                    VeiculoId INTEGER NOT NULL,
                    ContratoFrotaId TEXT NOT NULL,
                    PrefixoFrota TEXT NOT NULL,
                    Placa TEXT NOT NULL,
                    MarcaModelo TEXT,
                    MotoristaResponsavel TEXT,
                    CentroCusto TEXT,
                    KmAtual INTEGER NOT NULL DEFAULT 0,
                    HorimetroAtual INTEGER NOT NULL DEFAULT 0,
                    UltimaRevisaoKm INTEGER NOT NULL DEFAULT 0,
                    IntervaloRevisaoKm INTEGER NOT NULL DEFAULT 10000,
                    Ativo INTEGER NOT NULL DEFAULT 1,
                    FOREIGN KEY (ContratoFrotaId) REFERENCES ContratosFrotas(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS FaturasFrotas
                (
                    Id TEXT PRIMARY KEY,
                    ContratoFrotaId TEXT NOT NULL,
                    ClienteId INTEGER NOT NULL,
                    ClienteNome TEXT NOT NULL,
                    NumeroFatura TEXT NOT NULL UNIQUE,
                    PeriodoInicio TEXT NOT NULL,
                    PeriodoFim TEXT NOT NULL,
                    DataEmissao TEXT NOT NULL,
                    DataVencimento TEXT NOT NULL,
                    ValorBruto REAL NOT NULL DEFAULT 0,
                    ValorDescontosContratuais REAL NOT NULL DEFAULT 0,
                    Status INTEGER NOT NULL DEFAULT 1,
                    LinhaDigitavelBoleto TEXT,
                    PixCopiaECola TEXT,
                    Observacoes TEXT,
                    FOREIGN KEY (ContratoFrotaId) REFERENCES ContratosFrotas(Id)
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS FaturasFrotasItens
                (
                    Id TEXT PRIMARY KEY,
                    FaturaFrotaId TEXT NOT NULL,
                    OrdemServicoId INTEGER NOT NULL,
                    NumeroOS TEXT NOT NULL,
                    PlacaVeiculo TEXT NOT NULL,
                    PrefixoVeiculo TEXT,
                    DataOS TEXT NOT NULL,
                    ValorTotalOriginal REAL NOT NULL DEFAULT 0,
                    ValorComDescontoContrato REAL NOT NULL DEFAULT 0,
                    FOREIGN KEY (FaturaFrotaId) REFERENCES FaturasFrotas(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ContratosFrotas_ClienteId ON ContratosFrotas (ClienteId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_VeiculosFrotas_ContratoId ON VeiculosFrotas (ContratoFrotaId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_VeiculosFrotas_Placa ON VeiculosFrotas (Placa);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_FaturasFrotas_ContratoId ON FaturasFrotas (ContratoFrotaId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_FaturasFrotasItens_FaturaId ON FaturasFrotasItens (FaturaFrotaId);");
        }

        private static void CriarEstruturaDviInspecoes(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS InspecoesDvi (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrdemServicoId INTEGER,
                    VeiculoId INTEGER,
                    ClienteId INTEGER,
                    PlacaVeiculo TEXT NOT NULL,
                    ModeloVeiculo TEXT NOT NULL,
                    ClienteNome TEXT NOT NULL,
                    ClienteTelefone TEXT,
                    DataInspecao TEXT NOT NULL,
                    ResponsavelTecnico TEXT NOT NULL,
                    StatusAprovacao INTEGER NOT NULL DEFAULT 0,
                    ObservacoesGerais TEXT,
                    TokenAprovacaoRemota TEXT,
                    DataAprovacao TEXT,
                    ValorTotalEstimado REAL NOT NULL DEFAULT 0
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS InspecoesDviItens (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InspecaoDviId INTEGER NOT NULL,
                    Categoria TEXT NOT NULL,
                    NomeItem TEXT NOT NULL,
                    Severidade INTEGER NOT NULL DEFAULT 1,
                    ObservacaoTecnica TEXT,
                    ValorEstimadoReparo REAL,
                    AprovadoPeloCliente INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (InspecaoDviId) REFERENCES InspecoesDvi(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS InspecoesDviFotos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InspecaoDviItemId INTEGER NOT NULL,
                    CaminhoArquivo TEXT NOT NULL,
                    Descricao TEXT,
                    AnotacoesJson TEXT,
                    CriadoEm TEXT NOT NULL,
                    FOREIGN KEY (InspecaoDviItemId) REFERENCES InspecoesDviItens(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_InspecoesDvi_OrdemServicoId ON InspecoesDvi (OrdemServicoId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_InspecoesDvi_Placa ON InspecoesDvi (PlacaVeiculo);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_InspecoesDvi_Data ON InspecoesDvi (DataInspecao);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_InspecoesDviItens_InspecaoId ON InspecoesDviItens (InspecaoDviId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_InspecoesDviFotos_ItemId ON InspecoesDviFotos (InspecaoDviItemId);");
        }

        private static void CriarEstruturaSureTrack(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS CasosResolvidosSureTrack (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrdemServicoOrigemId TEXT,
                    OrdemServicoOrigemNumero TEXT,
                    Montadora TEXT NOT NULL,
                    Modelo TEXT NOT NULL,
                    Motorizacao TEXT,
                    Ano INTEGER NOT NULL DEFAULT 0,
                    SintomaPrincipal TEXT NOT NULL,
                    CodigosDTC TEXT,
                    CausaRaizDetectada TEXT NOT NULL,
                    ProcedimentoSolucao TEXT NOT NULL,
                    PecasSubstituidasJson TEXT,
                    DicaTesteRapido TEXT,
                    DataResolucao TEXT NOT NULL,
                    OcorrenciasConfirmadas INTEGER NOT NULL DEFAULT 1,
                    OrigemCaso TEXT NOT NULL DEFAULT 'OficinaLocal'
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_SureTrack_Modelo ON CasosResolvidosSureTrack (Modelo);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_SureTrack_DTC ON CasosResolvidosSureTrack (CodigosDTC);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_SureTrack_Sintoma ON CasosResolvidosSureTrack (SintomaPrincipal);");
        }

        private static void CriarEstruturaBibliotecaTecnica(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS ModulosEletronicos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CodigoModulo TEXT NOT NULL UNIQUE,
                    NomeModulo TEXT NOT NULL,
                    Montadora TEXT NOT NULL,
                    ModelosAplicacao TEXT NOT NULL,
                    SistemaTipo TEXT NOT NULL,
                    TensaoOperacao TEXT NOT NULL DEFAULT '12V',
                    DescricaoConectores TEXT,
                    ObservacoesTecnicas TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS PinosConectores (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ModuloId INTEGER NOT NULL,
                    Conector TEXT NOT NULL,
                    NumeroPino TEXT NOT NULL,
                    FuncaoSinal TEXT NOT NULL,
                    TipoSinal TEXT NOT NULL,
                    CorFio TEXT,
                    TensaoEsperada TEXT,
                    ObservacoesTecnicas TEXT,
                    FOREIGN KEY (ModuloId) REFERENCES ModulosEletronicos(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS CentraisEletricas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CodigoCentral TEXT NOT NULL UNIQUE,
                    Titulo TEXT NOT NULL,
                    Montadora TEXT NOT NULL,
                    ModelosAplicacao TEXT NOT NULL,
                    Localizacao TEXT NOT NULL,
                    TensaoNominal TEXT NOT NULL DEFAULT '12V',
                    FusiveisJson TEXT NOT NULL,
                    RelesJson TEXT NOT NULL
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS EspecificacoesLinhaPesada24V (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Montadora TEXT NOT NULL,
                    Modelo TEXT NOT NULL UNIQUE,
                    TensaoSistema TEXT NOT NULL,
                    AlternadorEspecificacao TEXT NOT NULL,
                    BateriasEspecificacao TEXT NOT NULL,
                    ConsumoStandbyMaximo TEXT,
                    TorqueCabecote TEXT,
                    FolgaValvulas TEXT,
                    ArCondicionadoGasGramas TEXT,
                    ArCondicionadoOleoTipo TEXT,
                    DicasEletricasChassi TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ModulosEletronicos_Codigo ON ModulosEletronicos (CodigoModulo);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_ModulosEletronicos_Montadora ON ModulosEletronicos (Montadora);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PinosConectores_ModuloId ON PinosConectores (ModuloId);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_CentraisEletricas_Codigo ON CentraisEletricas (CodigoCentral);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_CentraisEletricas_Montadora ON CentraisEletricas (Montadora);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_LinhaPesada24V_Modelo ON EspecificacoesLinhaPesada24V (Modelo);");
        }

        private static void CriarEstruturaDiagnosticoGuiado(DbConnection connection, DbTransaction transaction)
        {
            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS FluxogramasDiagnostico (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Codigo TEXT NOT NULL UNIQUE,
                    Titulo TEXT NOT NULL,
                    Categoria TEXT NOT NULL,
                    DescricaoSintoma TEXT NOT NULL,
                    SistemaVeicular TEXT NOT NULL DEFAULT '12V Leve / Flex'
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS FluxogramasPassos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FluxogramaId INTEGER NOT NULL,
                    PassoNumero INTEGER NOT NULL,
                    TituloPasso TEXT NOT NULL,
                    InstrucaoTeste TEXT NOT NULL,
                    FerramentaRecomendada TEXT NOT NULL,
                    PontoMedicao TEXT NOT NULL,
                    ValorEsperado TEXT NOT NULL,
                    ObservacaoSeguranca TEXT,
                    OpcoesJson TEXT NOT NULL,
                    FOREIGN KEY (FluxogramaId) REFERENCES FluxogramasDiagnostico(Id) ON DELETE CASCADE
                );");

            ExecuteMigrationCommand(connection, transaction, @"
                CREATE TABLE IF NOT EXISTS HistoricoCalculosQuedaTensao (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DataHora TEXT NOT NULL,
                    IdentificacaoCircuito TEXT NOT NULL,
                    VeiculoPlaca TEXT,
                    TipoCircuito TEXT NOT NULL DEFAULT 'Potência',
                    TensaoFonteVolts REAL NOT NULL,
                    TensaoCargaVolts REAL NOT NULL,
                    CorrenteAmperes REAL NOT NULL,
                    QuedaTensaoVolts REAL NOT NULL,
                    ResistenciaParasitaOhms REAL NOT NULL,
                    PotenciaDissipadaWatts REAL NOT NULL,
                    StatusConformidade TEXT NOT NULL,
                    DiagnosticoTecnico TEXT,
                    AcaoRecomendada TEXT
                );");

            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Fluxogramas_Codigo ON FluxogramasDiagnostico (Codigo);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Fluxogramas_Categoria ON FluxogramasDiagnostico (Categoria);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_FluxogramasPassos_FluxoId ON FluxogramasPassos (FluxogramaId, PassoNumero);");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_HistoricoQuedaTensao_Data ON HistoricoCalculosQuedaTensao (DataHora DESC);");
        }

        private static void AdicionarTipoPessoaClientes(DbConnection connection, DbTransaction transaction)
        {
            if (!MigrationTableExists(connection, transaction, "Clientes"))
            {
                return;
            }

            EnsureMigrationColumn(connection, transaction, "Clientes", "TipoPessoa", "ALTER TABLE Clientes ADD COLUMN TipoPessoa TEXT NOT NULL DEFAULT 'Fisica';");
            ExecuteMigrationCommand(connection, transaction, @"
                UPDATE Clientes
                SET TipoPessoa = CASE
                    WHEN length(replace(replace(replace(replace(COALESCE(CPF, ''), '.', ''), '/', ''), '-', ''), ' ', '')) > 11 THEN 'Juridica'
                    ELSE 'Fisica'
                END
                WHERE trim(COALESCE(TipoPessoa, '')) = '';");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Clientes_TipoPessoa_Status ON Clientes (TipoPessoa, Ativo, Nome);");
        }

        private static void ExpandirOrcamentosVeiculoDiagnosticoDesconto(DbConnection connection, DbTransaction transaction)
        {
            if (!MigrationTableExists(connection, transaction, "Orcamentos"))
            {
                return;
            }

            EnsureMigrationColumn(connection, transaction, "Orcamentos", "VeiculoId", "ALTER TABLE Orcamentos ADD COLUMN VeiculoId TEXT;");
            EnsureMigrationColumn(connection, transaction, "Orcamentos", "Diagnostico", "ALTER TABLE Orcamentos ADD COLUMN Diagnostico TEXT;");
            EnsureMigrationColumn(connection, transaction, "Orcamentos", "DescontoTipo", "ALTER TABLE Orcamentos ADD COLUMN DescontoTipo TEXT NOT NULL DEFAULT 'Valor';");
            EnsureMigrationColumn(connection, transaction, "Orcamentos", "DescontoPercentual", "ALTER TABLE Orcamentos ADD COLUMN DescontoPercentual REAL NOT NULL DEFAULT 0;");
            ExecuteMigrationCommand(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Orcamentos_Cliente_Veiculo_Status ON Orcamentos (ClienteId, VeiculoId, Status);");
        }

        private static void AdicionarTermoAutorizacaoOrdensServico(DbConnection connection, DbTransaction transaction)
        {
            if (!MigrationTableExists(connection, transaction, "OrdensServico"))
            {
                return;
            }

            EnsureMigrationColumn(connection, transaction, "OrdensServico", "TermoAutorizacao", "ALTER TABLE OrdensServico ADD COLUMN TermoAutorizacao TEXT;");
        }

        private static bool ExisteDuplicidadeImportacaoNFe(DbConnection connection, DbTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT 1
                FROM ImportacoesNFe
                WHERE ChaveAcesso IS NOT NULL
                  AND trim(ChaveAcesso) <> ''
                GROUP BY ChaveAcesso
                HAVING COUNT(*) > 1
                LIMIT 1;";
            return command.ExecuteScalar() != null;
        }

        private static void EnsureMigrationColumnIfTableExists(
            DbConnection connection,
            DbTransaction transaction,
            string tableName,
            string columnName,
            string alterSql)
        {
            if (!MigrationTableExists(connection, transaction, tableName))
            {
                return;
            }

            EnsureMigrationColumn(connection, transaction, tableName, columnName, alterSql);
        }

        private static void EnsureMigrationColumn(
            DbConnection connection,
            DbTransaction transaction,
            string tableName,
            string columnName,
            string alterSql)
        {
            using var pragma = connection.CreateCommand();
            pragma.Transaction = transaction;
            pragma.CommandText = $"PRAGMA table_info({tableName});";

            using (var reader = pragma.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            ExecuteMigrationCommand(connection, transaction, alterSql);
        }

        private static bool MigrationTableExists(DbConnection connection, DbTransaction transaction, string tableName)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT 1
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = @Name
                LIMIT 1;";
            command.Parameters.AddWithValue("@Name", tableName);
            return command.ExecuteScalar() != null;
        }

        private static string ObterTipoColuna(
            DbConnection connection,
            DbTransaction transaction,
            string tableName,
            string columnName)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"PRAGMA table_info({tableName});";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                }
            }

            return string.Empty;
        }

        private static bool ContatosFornecedorTemCascade(DbConnection connection, DbTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "PRAGMA foreign_key_list(ContatosFornecedor);";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var table = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var from = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                var onDelete = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);

                if (string.Equals(table, "Fornecedores", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(from, "FornecedorId", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(onDelete, "CASCADE", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ExecuteMigrationCommand(DbConnection connection, DbTransaction transaction, string sql)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
