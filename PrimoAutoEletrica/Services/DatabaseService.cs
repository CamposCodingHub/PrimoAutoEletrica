using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.DatabaseProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        private static readonly object InitializationLock = new();
        private static readonly HashSet<string> InitializedDatabases = new(StringComparer.OrdinalIgnoreCase);
        private const int MaxFailedLoginAttempts = 5;
        private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);

        private readonly LoggerService _logger;
        private readonly string _databasePath;
        private readonly string _connectionString;
        private readonly string _configuredProvider;
        private readonly string _runtimeProvider;
        private readonly bool _isUsingUnsupportedProviderFallback;
        private readonly IDatabaseProvider? _databaseProvider;

        public string DatabasePath => _databasePath;
        public string ConfiguredProvider => _configuredProvider;
        public string RuntimeProvider => _runtimeProvider;
        public bool IsUsingUnsupportedProviderFallback => _isUsingUnsupportedProviderFallback;

        public DatabaseService(
            string? appDataPathOverride = null,
            DatabaseConnectionSettings? settingsOverride = null,
            LoggerService? logger = null)
        {
            _logger = logger ?? new LoggerService();

            var appDataPath = string.IsNullOrWhiteSpace(appDataPathOverride)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica")
                : appDataPathOverride;

            Directory.CreateDirectory(appDataPath);

            // Protecao: smoke/workflow nunca pode abrir o AppData de producao.
            if (App.IsAutomatedTestMode)
            {
                var productionRoot = Path.GetFullPath(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica"));
                var resolvedRoot = Path.GetFullPath(appDataPath);
                var isolado =
                    resolvedRoot.Contains("AutomatedTests", StringComparison.OrdinalIgnoreCase) ||
                    resolvedRoot.Contains("TestResults", StringComparison.OrdinalIgnoreCase) ||
                    resolvedRoot.Contains("Smoke", StringComparison.OrdinalIgnoreCase) ||
                    resolvedRoot.Contains("workflow-test", StringComparison.OrdinalIgnoreCase) ||
                    resolvedRoot.Contains("ui-smoke-test", StringComparison.OrdinalIgnoreCase);

                if (!isolado ||
                    string.Equals(resolvedRoot, productionRoot, StringComparison.OrdinalIgnoreCase) ||
                    resolvedRoot.StartsWith(productionRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Modo automatizado recusou banco fora de isolamento. AppData='{resolvedRoot}'.");
                }
            }

            var settings = settingsOverride ?? DatabaseConnectionSettingsService.LoadOrCreateDefault(appDataPath, _logger);
            var providerPlan = DatabaseProviderPlanService.Avaliar(settings);
            _configuredProvider = settings.Provider;

            if (settings.IsSqlServer && !settings.AllowUnsupportedSqlServerRuntimeFallback)
            {
                _databaseProvider = DatabaseProviderFactory.CreateProvider(settings, _logger, appDataPath);
                _runtimeProvider = "SqlServer";
                _isUsingUnsupportedProviderFallback = false;
                _connectionString = _databaseProvider.ConnectionString;
                _databasePath = _databaseProvider.DatabaseName;

                EnsureDatabaseInitialized();
                return;
            }

            _runtimeProvider = "SQLite";

            if (settings.IsSqlServer)
            {
                _isUsingUnsupportedProviderFallback = true;
                Logger.LogCritical(
                    $"Provider '{settings.Provider}' configurado com fallback explicito. O runtime operacional permanece SQLite ate a migracao completa. " +
                    $"Pendencias: {string.Join(" | ", providerPlan.Pendencias)}");
            }
            else if (!settings.IsSQLite)
            {
                Logger.LogWarning($"Provider desconhecido '{settings.Provider}'. Runtime operacional permanecera SQLite.");
            }

            _databasePath = settings.ResolveSqlitePath(appDataPath);
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = true,
                DefaultTimeout = settings.CommandTimeoutSeconds
            }.ToString();

            EnsureDatabaseInitialized();
        }

        private LoggerService Logger => _logger;

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
        }

        private static object ToDbNullableDate(DateTime? value, string format)
        {
            return value.HasValue
                ? value.Value.ToString(format)
                : DBNull.Value;
        }
        private static bool IsSqlServerConnection(DbConnection connection)
        {
            var typeName = connection.GetType().FullName ?? string.Empty;
            return typeName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime ReadDateTimeValue(DbDataReader reader, int index)
        {
            var value = reader.GetValue(index);
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(Convert.ToString(value), out var parsed)
                ? parsed
                : DateTime.MinValue;
        }

        private static DateTime? ReadNullableDateTimeValue(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            var value = reader.GetValue(index);
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(Convert.ToString(value), out var parsed)
                ? parsed
                : null;
        }

        // =====================================================
        // INICIALIZAÇÃO
        // =====================================================
        private void EnsureDatabaseInitialized()
        {
            var initializationKey = string.Equals(_runtimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase)
                ? $"SqlServer:{_connectionString}"
                : Path.GetFullPath(_databasePath);

            lock (InitializationLock)
            {
                if (InitializedDatabases.Contains(initializationKey))
                {
                    return;
                }

                try
                {
                    Logger.LogInfo($"Inicializando estrutura do banco {_runtimeProvider} em '{initializationKey}'.");

                    if (string.Equals(_runtimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
                    {
                        InitializeSqlServerDatabase();
                    }
                    else
                    {
                        InitializeDatabase();
                    }

                    InitializedDatabases.Add(initializationKey);
                    Logger.LogInfo($"Estrutura do banco {_runtimeProvider} validada em '{initializationKey}'.");
                }
                catch (Exception ex)
                {
                    Logger.LogCritical($"Falha ao inicializar estrutura do banco {_runtimeProvider} em '{initializationKey}'.", ex);
                    throw;
                }
            }
        }

        private void InitializeSqlServerDatabase()
        {
            if (_databaseProvider == null)
            {
                throw new InvalidOperationException("Provider SQL Server nao foi inicializado.");
            }

            if (!_databaseProvider.CreateDatabaseIfNotExists())
            {
                throw new InvalidOperationException("Nao foi possivel criar ou localizar o banco SQL Server configurado.");
            }

            if (!_databaseProvider.CreateSchema())
            {
                throw new InvalidOperationException("Nao foi possivel criar ou atualizar o schema SQL Server.");
            }

            var validation = _databaseProvider.ValidateSchema();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Schema SQL Server invalido: {validation.Message}");
            }

            using var connection = GetConnection();
            connection.Open();
            SeedPermissoes(connection);
            SeedPerfis(connection);
            InserirUsuariosPadrao(connection);
        }
        private void InitializeDatabase()
        {
            using var connection = GetConnection();

            connection.Open();

            // MELHORA PERFORMANCE E EVITA LOCK
            var pragma = connection.CreateCommand();

            pragma.CommandText = @"
                PRAGMA journal_mode=WAL;
                PRAGMA synchronous=NORMAL;
                PRAGMA temp_store=MEMORY;
                PRAGMA foreign_keys=ON;
            ";

            pragma.ExecuteNonQuery();

            var command = connection.CreateCommand();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Funcionarios
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nome TEXT NOT NULL,
                    CPF TEXT,
                    Email TEXT UNIQUE NOT NULL,
                    Senha TEXT NOT NULL,
                    Funcao TEXT NOT NULL,
                    PerfilAcesso TEXT NOT NULL DEFAULT 'Mecânico',
                    Telefone TEXT,
                    Foto TEXT,
                    DataAdmissao TEXT NOT NULL,
                    Salario REAL NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'Ativo',
                    Observacoes TEXT,
                    DataCadastro TEXT NOT NULL,
                    DataUltimoLogin TEXT,
                    ExigirTrocaSenha INTEGER NOT NULL DEFAULT 0,
                    Ativo INTEGER NOT NULL DEFAULT 1
                );
            ";

            command.ExecuteNonQuery();

            InitializeAccessControlSchema(connection);
            InitializeAuditSchema(connection);

            // Verificar se a tabela Clientes existe e tem a estrutura correta
            var checkTableCommand = connection.CreateCommand();
            checkTableCommand.CommandText = @"
                SELECT name FROM sqlite_master WHERE type='table' AND name='Clientes';
            ";

            var tableExists = checkTableCommand.ExecuteScalar() != null;

            if (tableExists)
            {
                // Verificar se a coluna RG existe
                var checkColumnCommand = connection.CreateCommand();
                checkColumnCommand.CommandText = @"
                    PRAGMA table_info(Clientes);
                ";

                bool hasRGColumn = false;
                using (var reader = checkColumnCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader.GetString(1);
                        if (columnName == "RG")
                        {
                            hasRGColumn = true;
                            break;
                        }
                    }
                }

                // Se a coluna RG não existe, migrar incrementalmente sem apagar clientes existentes.
                if (!hasRGColumn)
                {
                    var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = "ALTER TABLE Clientes ADD COLUMN RG TEXT;";
                    alterCommand.ExecuteNonQuery();
                }
            }
            else
            {
                // Criar tabela de Clientes
                var clientesCommand = connection.CreateCommand();
                clientesCommand.CommandText = @"
                    CREATE TABLE Clientes
                    (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        TipoPessoa TEXT NOT NULL DEFAULT 'Fisica',
                        CPF TEXT,
                        RG TEXT,
                        DataNascimento TEXT,
                        Telefone TEXT,
                        WhatsApp TEXT,
                        Email TEXT,
                        CEP TEXT,
                        Rua TEXT,
                        Numero TEXT,
                        Bairro TEXT,
                        Cidade TEXT,
                        Estado TEXT,
                        Ativo INTEGER NOT NULL DEFAULT 1,
                        ClienteVip INTEGER NOT NULL DEFAULT 0,
                        TotalGasto REAL NOT NULL DEFAULT 0,
                        TotalServicos INTEGER NOT NULL DEFAULT 0,
                        PontosFidelidade INTEGER NOT NULL DEFAULT 0,
                        ConsentimentoLGPD INTEGER NOT NULL DEFAULT 0,
                        DataConsentimentoLGPD TEXT,
                        OrigemConsentimentoLGPD TEXT,
                        AutorizaContatoWhatsApp INTEGER NOT NULL DEFAULT 0,
                        Observacoes TEXT,
                        CaminhoDocumento TEXT,
                        CaminhoAssinatura TEXT,
                        DataCadastro TEXT NOT NULL,
                        UltimaVisita TEXT,
                        ImagemUrl TEXT
                    );
                ";
                clientesCommand.ExecuteNonQuery();
            }

            // Criar tabela de Produtos
            var produtosCommand = connection.CreateCommand();
            produtosCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Produtos
                (
                    Id TEXT PRIMARY KEY,
                    Codigo TEXT NOT NULL,
                    Nome TEXT NOT NULL,
                    Descricao TEXT,
                    Categoria TEXT,
                    Marca TEXT,
                    Modelo TEXT,
                    FornecedorId TEXT,
                    Fornecedor TEXT,
                    CNPJFornecedor TEXT,
                    ContatoFornecedor TEXT,
                    TelefoneFornecedor TEXT,
                    QuantidadeEstoque INTEGER NOT NULL DEFAULT 0,
                    QuantidadeMinima INTEGER NOT NULL DEFAULT 0,
                    QuantidadeMaxima INTEGER NOT NULL DEFAULT 0,
                    Localizacao TEXT,
                    Prateleira TEXT,
                    Gaveta TEXT,
                    PrecoCompra REAL NOT NULL DEFAULT 0,
                    PrecoVenda REAL NOT NULL DEFAULT 0,
                    MargemLucro REAL NOT NULL DEFAULT 0,
                    ValorTotalEstoque REAL NOT NULL DEFAULT 0,
                    UnidadeMedida TEXT,
                    Peso TEXT,
                    Dimensoes TEXT,
                    Cor TEXT,
                    Material TEXT,
                    CodigoBarras TEXT,
                    SKU TEXT,
                    NCMS TEXT,
                    CEST TEXT,
                    CFOP TEXT,
                    Ativo INTEGER NOT NULL DEFAULT 1,
                    ProdutoPerecivel INTEGER NOT NULL DEFAULT 0,
                    DataValidade TEXT,
                    DataFabricacao TEXT,
                    Lote TEXT,
                    DataCadastro TEXT NOT NULL,
                    DataUltimaCompra TEXT,
                    DataUltimaVenda TEXT,
                    DataUltimaAtualizacao TEXT,
                    Observacoes TEXT,
                    ImagemUrl TEXT,
                    Anexos TEXT,
                    TotalVendas INTEGER NOT NULL DEFAULT 0,
                    TotalFaturado REAL NOT NULL DEFAULT 0,
                    VendasUltimoMes INTEGER NOT NULL DEFAULT 0,
                    VendasUltimoTrimestre INTEGER NOT NULL DEFAULT 0
                );
            ";
            produtosCommand.ExecuteNonQuery();

            // Criar tabela de Importações NF-e
            var importacoesCommand = connection.CreateCommand();
            importacoesCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS ImportacoesNFe
                (
                    Id TEXT PRIMARY KEY,
                    ChaveAcesso TEXT,
                    Numero TEXT,
                    Serie TEXT,
                    DataEmissao TEXT,
                    DataEntrada TEXT,
                    ValorTotal REAL NOT NULL DEFAULT 0,
                    ValorProdutos REAL NOT NULL DEFAULT 0,
                    Modelo TEXT,
                    FornecedorNome TEXT,
                    FornecedorCNPJ TEXT,
                    Status TEXT,
                    CaminhoArquivo TEXT,
                    Erro TEXT,
                    DataImportacao TEXT,
                    UsuarioId TEXT,
                    UsuarioNome TEXT
                );
            ";
            importacoesCommand.ExecuteNonQuery();

            // Criar tabela de Itens de Importação
            var importacoesItensCommand = connection.CreateCommand();
            importacoesItensCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS ImportacoesItens
                (
                    Id TEXT PRIMARY KEY,
                    ImportacaoId TEXT NOT NULL,
                    Codigo TEXT,
                    Nome TEXT,
                    NCM TEXT,
                    CFOP TEXT,
                    Quantidade REAL NOT NULL DEFAULT 0,
                    ValorUnitario REAL NOT NULL DEFAULT 0,
                    ValorTotal REAL NOT NULL DEFAULT 0,
                    UnidadeMedida TEXT,
                    Status TEXT,
                    ProdutoExistenteId TEXT,
                    MotivoIgnorado TEXT,
                    ProdutoSnapshotAnterior TEXT,
                    ProdutoSnapshotPosterior TEXT,
                    FOREIGN KEY (ImportacaoId) REFERENCES ImportacoesNFe(Id)
                );
            ";
            importacoesItensCommand.ExecuteNonQuery();

            // Criar tabela de Fornecedores
            var fornecedoresCommand = connection.CreateCommand();
            fornecedoresCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Fornecedores
                (
                    Id TEXT PRIMARY KEY,
                    RazaoSocial TEXT NOT NULL,
                    NomeFantasia TEXT NOT NULL,
                    CNPJ TEXT,
                    InscricaoEstadual TEXT,
                    Telefone TEXT,
                    Celular TEXT,
                    WhatsAppVendedor TEXT,
                    Email TEXT,
                    Site TEXT,
                    CEP TEXT,
                    Rua TEXT,
                    Numero TEXT,
                    Complemento TEXT,
                    Bairro TEXT,
                    Cidade TEXT,
                    Estado TEXT,
                    FormaPagamento TEXT,
                    PrazoPagamento TEXT,
                    PrazoMedioPagamentoDias INTEGER NOT NULL DEFAULT 0,
                    PrazoMedioEntregaDias INTEGER NOT NULL DEFAULT 0,
                    PedidoMinimo REAL NOT NULL DEFAULT 0,
                    Categoria TEXT NOT NULL DEFAULT 'Peças',
                    CategoriaPreferencial TEXT,
                    Ativo INTEGER NOT NULL DEFAULT 1,
                    Nota INTEGER NOT NULL DEFAULT 5,
                    Observacoes TEXT,
                    DataCadastro TEXT NOT NULL,
                    UltimaCompra TEXT,
                    TotalCompras REAL NOT NULL DEFAULT 0
                );
            ";
            fornecedoresCommand.ExecuteNonQuery();

            // Criar tabela de Contatos de Fornecedor
            var contatosFornecedorCommand = connection.CreateCommand();
            contatosFornecedorCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS ContatosFornecedor
                (
                    Id TEXT PRIMARY KEY,
                    FornecedorId TEXT NOT NULL,
                    Nome TEXT NOT NULL,
                    Cargo TEXT,
                    Telefone TEXT,
                    Email TEXT,
                    Principal INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (FornecedorId) REFERENCES Fornecedores(Id)
                );
            ";
            contatosFornecedorCommand.ExecuteNonQuery();

            var produtoFornecedoresCommand = connection.CreateCommand();
            produtoFornecedoresCommand.CommandText = @"
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
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_ProdutoFornecedores_Produto_Fornecedor
                    ON ProdutoFornecedores (ProdutoId, FornecedorId);

                CREATE INDEX IF NOT EXISTS IX_ProdutoFornecedores_Fornecedor
                    ON ProdutoFornecedores (FornecedorId, Ativo, DataUltimaCompra DESC);

                CREATE INDEX IF NOT EXISTS IX_ProdutoFornecedores_Produto
                    ON ProdutoFornecedores (ProdutoId);
            ";
            produtoFornecedoresCommand.ExecuteNonQuery();

            var vendasCommand = connection.CreateCommand();
            vendasCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Vendas
                (
                    Id TEXT PRIMARY KEY,
                    Data TEXT NOT NULL,
                    ClienteId TEXT,
                    ClienteNome TEXT,
                    Total REAL NOT NULL,
                    FormaPagamento TEXT NOT NULL,
                    Desconto REAL NOT NULL DEFAULT 0,
                    Usuario TEXT NOT NULL,
                    QuantidadeItens INTEGER NOT NULL DEFAULT 0
                );
            ";
            vendasCommand.ExecuteNonQuery();

            var vendaItensCommand = connection.CreateCommand();
            vendaItensCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS VendaItens
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
                );
            ";
            vendaItensCommand.ExecuteNonQuery();

            InitializeVeiculosSchema(connection);
            InitializeOrdensServicoSchema(connection);

            ApplyDatabaseMigrations(connection);

            InserirUsuariosPadrao(connection);
        }

        // =====================================================
        // CONEXÃO
        // =====================================================
        public DbConnection GetConnection()
        {
            if (string.Equals(_runtimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase) && _databaseProvider != null)
            {
                return _databaseProvider.CreateConnection();
            }

            return CreateSqliteConnection();
        }

        public SqliteConnection GetSqliteConnection()
        {
            if (!string.Equals(_runtimeProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Esta operacao de manutencao e exclusiva do runtime SQLite.");
            }

            return CreateSqliteConnection();
        }

        private SqliteConnection CreateSqliteConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.StateChange += (_, args) =>
            {
                if (args.CurrentState != System.Data.ConnectionState.Open)
                {
                    return;
                }

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA foreign_keys=ON;";
                command.ExecuteNonQuery();
            };

            return connection;
        }

        private static bool UsarProdutoRepositoryBridge()
        {
            return true;
        }

        private static bool UsarClienteRepositoryBridge()
        {
            return true;
        }

        private static bool UsarOrdemServicoRepositoryBridge()
        {
            return true;
        }

        // =====================================================
        // USUÁRIOS PADRÃO
        // =====================================================
        private void InserirUsuariosPadrao(DbConnection connection)
        {
            var checkCommand = connection.CreateCommand();

            checkCommand.CommandText =
                "SELECT COUNT(*) FROM Funcionarios";

            int total = Convert.ToInt32(
                checkCommand.ExecuteScalar()
            );

            if (total > 0)
                return;

            var senhaTemporaria = GerarSenhaTemporariaAdministrador();

            InserirFuncionario(
                connection,
                "Administrador",
                "admin@primoauto.com",
                senhaTemporaria,
                "Administrador do Sistema",
                "Administrador",
                exigirTrocaSenha: true
            );

            RegistrarCredencialInicialAdministrador(senhaTemporaria);
        }

        private void RegistrarCredencialInicialAdministrador(string senhaTemporaria)
        {
            var diretorioBanco = Path.GetDirectoryName(_databasePath)
                ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var caminhoCredencial = Path.Combine(diretorioBanco, "credenciais-iniciais-admin.txt");

            File.WriteAllLines(
                caminhoCredencial,
                new[]
                {
                    "PrimoAutoEletrica - credenciais iniciais",
                    $"Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    "Usuario: admin@primoauto.com",
                    $"Senha temporaria: {senhaTemporaria}",
                    "Troque esta senha no primeiro acesso e remova este arquivo depois da homologacao."
                });

            Logger.LogWarning($"Senha temporaria do administrador inicial gerada em '{caminhoCredencial}'. Troque no primeiro acesso.");
        }

        private static string GerarSenhaTemporariaAdministrador()
        {
            const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$%*-_";
            var senha = new char[18];
            for (var i = 0; i < senha.Length; i++)
            {
                senha[i] = caracteres[RandomNumberGenerator.GetInt32(caracteres.Length)];
            }

            return new string(senha);
        }

        // =====================================================
        // INSERIR FUNCIONÁRIO
        // =====================================================
        private void InserirFuncionario(
            DbConnection connection,
            string nome,
            string email,
            string senha,
            string funcao,
            string perfil,
            bool exigirTrocaSenha = false
        )
        {
            var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Funcionarios
                (
                    Nome,
                    CPF,
                    Email,
                    Senha,
                    Funcao,
                    PerfilAcesso,
                    Telefone,
                    DataAdmissao,
                    Salario,
                    Status,
                    DataCadastro,
                    ExigirTrocaSenha,
                    Ativo
                )
                VALUES
                (
                    @Nome,
                    @CPF,
                    @Email,
                    @Senha,
                    @Funcao,
                    @Perfil,
                    @Telefone,
                    @DataAdmissao,
                    @Salario,
                    @Status,
                    @DataCadastro,
                    @ExigirTrocaSenha,
                    @Ativo
                )
            ";

            command.Parameters.AddWithValue("@Nome", nome);
            command.Parameters.AddWithValue("@CPF", DBNull.Value);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@Senha", PasswordHasherService.HashPassword(senha));
            command.Parameters.AddWithValue("@Funcao", funcao);
            command.Parameters.AddWithValue("@Perfil", perfil);
            command.Parameters.AddWithValue("@Telefone", "(17) 99999-9999");

            command.Parameters.AddWithValue(
                "@DataAdmissao",
                DateTime.Now.ToString("yyyy-MM-dd")
            );

            command.Parameters.AddWithValue("@Salario", 5000);
            command.Parameters.AddWithValue("@Status", "Ativo");

            command.Parameters.AddWithValue(
                "@DataCadastro",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            );

            command.Parameters.AddWithValue("@Ativo", 1);
            command.Parameters.AddWithValue("@ExigirTrocaSenha", exigirTrocaSenha ? 1 : 0);

            command.ExecuteNonQuery();
        }

        // =====================================================
        // LOGIN
        // =====================================================
        public Funcionario? AutenticarFuncionario(
            string email,
            string senha
        )
        {
            return AutenticarFuncionarioDetalhado(email, senha).Funcionario;
        }

        public LoginAuthenticationResult AutenticarFuncionarioDetalhado(
            string email,
            string senha
        )
        {
            var emailNormalizado = email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(emailNormalizado) || string.IsNullOrWhiteSpace(senha))
            {
                return LoginAuthenticationResult.Invalid("Email ou senha invalidos.");
            }

            Funcionario? funcionario;

            using (var connection = GetConnection())
            {
                connection.Open();

                var command = connection.CreateCommand();

                var selectPrefix = IsSqlServerConnection(connection) ? "SELECT TOP (1)" : "SELECT";
                var limitSuffix = IsSqlServerConnection(connection) ? string.Empty : "LIMIT 1";
                command.CommandText = $@"
                    {selectPrefix}
                        Id,
                        Nome,
                        CPF,
                        Email,
                        Senha,
                        Funcao,
                        PerfilAcesso,
                        Telefone,
                        DataAdmissao,
                        Salario,
                        Status,
                        COALESCE(Observacoes, ''),
                        DataCadastro,
                        DataUltimoLogin,
                        ExigirTrocaSenha,
                        Ativo
                    FROM Funcionarios
                    WHERE lower(Email) = lower(@Email)
                      AND Ativo = 1
                    {limitSuffix}
                ";

                command.Parameters.AddWithValue("@Email", emailNormalizado);

                using var reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return LoginAuthenticationResult.Invalid("Email ou senha invalidos.");
                }

                funcionario = new Funcionario
                {
                    Id = reader.GetInt32(0),
                    Nome = reader.GetString(1),
                    CPF = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Email = reader.GetString(3),
                    Senha = reader.GetString(4),
                    Funcao = reader.GetString(5),
                    PerfilAcesso = reader.GetString(6),
                    Telefone = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    DataAdmissao = ReadDateTimeValue(reader, 8),
                    Salario = Convert.ToDecimal(reader.GetValue(9)),
                    Status = reader.GetString(10),
                    Observacoes = reader.GetString(11),
                    DataCadastro = ReadDateTimeValue(reader, 12),
                    DataUltimoLogin = ReadNullableDateTimeValue(reader, 13),
                    ExigirTrocaSenha = !reader.IsDBNull(14) && Convert.ToInt32(reader.GetValue(14)) == 1,
                    Ativo = !reader.IsDBNull(15) && Convert.ToBoolean(reader.GetValue(15))
                };
            }

            using (var connection = GetConnection())
            {
                connection.Open();

                var estadoAtual = ObterEstadoTentativasLogin(connection, funcionario.Id);
                if (estadoAtual.BloqueadoAte.HasValue)
                {
                    if (estadoAtual.BloqueadoAte.Value > DateTime.Now)
                    {
                        return CriarResultadoBloqueado(estadoAtual.BloqueadoAte.Value, estadoAtual.TentativasFalhas);
                    }

                    ResetarTentativasLogin(connection, funcionario.Id, registrarSucesso: false);
                }

                if (!PasswordHasherService.VerifyPassword(senha, funcionario.Senha))
                {
                    return RegistrarFalhaLogin(connection, funcionario.Id);
                }

                ResetarTentativasLogin(connection, funcionario.Id, registrarSucesso: true);
            }

            if (PasswordHasherService.NeedsRehash(funcionario.Senha))
            {
                var senhaAtualizada = PasswordHasherService.HashPassword(senha);
                AtualizarSenhaFuncionario(funcionario.Id, senhaAtualizada);
                funcionario.Senha = senhaAtualizada;
            }

            AtualizarUltimoLogin(funcionario.Id);

            return LoginAuthenticationResult.Success(funcionario);
        }

        private LoginAuthenticationResult RegistrarFalhaLogin(DbConnection connection, int funcionarioId)
        {
            var estadoAtual = ObterEstadoTentativasLogin(connection, funcionarioId);
            var tentativasFalhas = estadoAtual.TentativasFalhas + 1;
            DateTime? bloqueadoAte = null;

            if (tentativasFalhas >= MaxFailedLoginAttempts)
            {
                bloqueadoAte = DateTime.Now.Add(LoginLockoutDuration);
            }

            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    MERGE LoginTentativasSeguranca WITH (HOLDLOCK) AS Target
                    USING (SELECT @FuncionarioId AS FuncionarioId) AS Source
                    ON Target.FuncionarioId = Source.FuncionarioId
                    WHEN MATCHED THEN
                        UPDATE SET TentativasFalhas = @TentativasFalhas,
                                   BloqueadoAte = @BloqueadoAte,
                                   UltimaFalhaEm = @UltimaFalhaEm,
                                   AtualizadoEm = @AtualizadoEm
                    WHEN NOT MATCHED THEN
                        INSERT (FuncionarioId, TentativasFalhas, BloqueadoAte, UltimaFalhaEm, AtualizadoEm)
                        VALUES (@FuncionarioId, @TentativasFalhas, @BloqueadoAte, @UltimaFalhaEm, @AtualizadoEm);"
                : @"
                    INSERT INTO LoginTentativasSeguranca
                    (
                        FuncionarioId,
                        TentativasFalhas,
                        BloqueadoAte,
                        UltimaFalhaEm,
                        AtualizadoEm
                    )
                    VALUES
                    (
                        @FuncionarioId,
                        @TentativasFalhas,
                        @BloqueadoAte,
                        @UltimaFalhaEm,
                        @AtualizadoEm
                    )
                    ON CONFLICT(FuncionarioId)
                    DO UPDATE SET TentativasFalhas = excluded.TentativasFalhas,
                                  BloqueadoAte = excluded.BloqueadoAte,
                                  UltimaFalhaEm = excluded.UltimaFalhaEm,
                                  AtualizadoEm = excluded.AtualizadoEm;";
            command.Parameters.AddWithValue("@FuncionarioId", funcionarioId);
            command.Parameters.AddWithValue("@TentativasFalhas", tentativasFalhas);
            command.Parameters.AddWithValue("@BloqueadoAte", ToDbNullableDate(bloqueadoAte, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@UltimaFalhaEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@AtualizadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();

            if (bloqueadoAte.HasValue)
            {
                return CriarResultadoBloqueado(bloqueadoAte.Value, tentativasFalhas);
            }

            var tentativasRestantes = Math.Max(0, MaxFailedLoginAttempts - tentativasFalhas);
            var mensagemTentativas = tentativasRestantes switch
            {
                1 => "Email ou senha invalidos. Resta 1 tentativa antes do bloqueio temporario.",
                > 1 => $"Email ou senha invalidos. Restam {tentativasRestantes} tentativas antes do bloqueio temporario.",
                _ => "Email ou senha invalidos."
            };

            return LoginAuthenticationResult.Invalid(mensagemTentativas, tentativasFalhas, tentativasRestantes);
        }

        private void ResetarTentativasLogin(DbConnection connection, int funcionarioId, bool registrarSucesso)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    MERGE LoginTentativasSeguranca WITH (HOLDLOCK) AS Target
                    USING (SELECT @FuncionarioId AS FuncionarioId) AS Source
                    ON Target.FuncionarioId = Source.FuncionarioId
                    WHEN MATCHED THEN
                        UPDATE SET TentativasFalhas = 0,
                                   BloqueadoAte = NULL,
                                   UltimoSucessoEm = @UltimoSucessoEm,
                                   AtualizadoEm = @AtualizadoEm
                    WHEN NOT MATCHED THEN
                        INSERT (FuncionarioId, TentativasFalhas, BloqueadoAte, UltimoSucessoEm, AtualizadoEm)
                        VALUES (@FuncionarioId, 0, NULL, @UltimoSucessoEm, @AtualizadoEm);"
                : @"
                    INSERT INTO LoginTentativasSeguranca
                    (
                        FuncionarioId,
                        TentativasFalhas,
                        BloqueadoAte,
                        UltimoSucessoEm,
                        AtualizadoEm
                    )
                    VALUES
                    (
                        @FuncionarioId,
                        0,
                        NULL,
                        @UltimoSucessoEm,
                        @AtualizadoEm
                    )
                    ON CONFLICT(FuncionarioId)
                    DO UPDATE SET TentativasFalhas = 0,
                                  BloqueadoAte = NULL,
                                  UltimoSucessoEm = @UltimoSucessoEm,
                                  AtualizadoEm = @AtualizadoEm;";
            command.Parameters.AddWithValue("@FuncionarioId", funcionarioId);
            command.Parameters.AddWithValue("@UltimoSucessoEm", registrarSucesso
                ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                : DBNull.Value);
            command.Parameters.AddWithValue("@AtualizadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        private static (int TentativasFalhas, DateTime? BloqueadoAte) ObterEstadoTentativasLogin(DbConnection connection, int funcionarioId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT TOP (1) TentativasFalhas, BloqueadoAte
                    FROM LoginTentativasSeguranca
                    WHERE FuncionarioId = @FuncionarioId;"
                : @"
                    SELECT TentativasFalhas, BloqueadoAte
                    FROM LoginTentativasSeguranca
                    WHERE FuncionarioId = @FuncionarioId
                    LIMIT 1;";
            command.Parameters.AddWithValue("@FuncionarioId", funcionarioId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return (0, null);
            }

            return (
                TentativasFalhas: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                BloqueadoAte: ReadNullableDateTimeValue(reader, 1));
        }

        private static LoginAuthenticationResult CriarResultadoBloqueado(DateTime bloqueadoAte, int tentativasFalhas)
        {
            var mensagem = $"Conta temporariamente bloqueada ate {bloqueadoAte:dd/MM/yyyy HH:mm}. Aguarde alguns minutos antes de tentar novamente.";
            return LoginAuthenticationResult.Locked(bloqueadoAte, tentativasFalhas, mensagem);
        }

        private void AtualizarSenhaFuncionario(int funcionarioId, string senhaHash)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Funcionarios
                SET Senha = @Senha
                WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Senha", senhaHash);
            command.Parameters.AddWithValue("@Id", funcionarioId);
            command.ExecuteNonQuery();

            Logger.LogInfo($"Senha do funcionario '{funcionarioId}' migrada para hash seguro.");
        }

        public void AlterarSenhaFuncionario(int funcionarioId, string novaSenha, bool exigirTrocaSenha = false, string operador = "")
        {
            if (funcionarioId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(funcionarioId), "Funcionario invalido.");
            }

            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 8)
            {
                throw new InvalidOperationException("A nova senha deve ter pelo menos 8 caracteres.");
            }

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Funcionarios
                SET Senha = @Senha,
                    ExigirTrocaSenha = @ExigirTrocaSenha,
                    RowVersion = COALESCE(RowVersion, 0) + 1,
                    DataUltimaAlteracao = @DataUltimaAlteracao
                WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Senha", PasswordHasherService.HashPassword(novaSenha));
            command.Parameters.AddWithValue("@ExigirTrocaSenha", exigirTrocaSenha ? 1 : 0);
            command.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Id", funcionarioId);

            if (command.ExecuteNonQuery() != 1)
            {
                throw new InvalidOperationException("Funcionario nao encontrado para alteracao de senha.");
            }

            try
            {
                global::PrimoAutoEletrica.App.Audit.Registrar(
                    categoria: "Seguranca",
                    acao: exigirTrocaSenha ? "SenhaTemporariaDefinida" : "SenhaAlterada",
                    entidade: "Funcionario",
                    entidadeId: funcionarioId.ToString(),
                    detalhes: string.IsNullOrWhiteSpace(operador) ? "Operador=Sistema" : $"Operador={operador.Trim()}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Falha ao auditar alteracao de senha do funcionario '{funcionarioId}': {ex.Message}");
            }
        }

        // =====================================================
        // ATUALIZA LOGIN
        // =====================================================
        private void AtualizarUltimoLogin(int funcionarioId)
        {
            using var connection = GetConnection();

            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Funcionarios
                SET DataUltimoLogin = @Data
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue(
                "@Data",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            );

            command.Parameters.AddWithValue(
                "@Id",
                funcionarioId
            );

            command.ExecuteNonQuery();
        }

        // =====================================================
        // PERMISSÕES
        // =====================================================
        public bool TemPermissao(
            string perfilUsuario,
            string permissaoNecessaria
        )
        {
            if (perfilUsuario == "Administrador")
                return true;

            switch (permissaoNecessaria)
            {
                case "Dashboard":
                    return true;

                case "Clientes":
                case "Veiculos":
                    return perfilUsuario == "Vendedor"
                        || perfilUsuario == "Gerente";

                case "Orcamentos":
                    return perfilUsuario == "Vendedor"
                        || perfilUsuario == "Gerente";

                case "OrdensServico":
                    return true;

                case "PDV":
                    return perfilUsuario == "Caixa"
                        || perfilUsuario == "Gerente";

                case "Estoque":
                    return perfilUsuario == "Almoxarife"
                        || perfilUsuario == "Gerente";

                case "Financeiro":
                    return perfilUsuario == "Caixa"
                        || perfilUsuario == "Gerente";

                case "Relatorios":
                    return perfilUsuario == "Caixa"
                        || perfilUsuario == "Gerente"
                        || perfilUsuario == "Almoxarife";

                case "Fornecedores":
                    return perfilUsuario == "Almoxarife"
                        || perfilUsuario == "Gerente";

                case "Funcionarios":
                    return perfilUsuario == "Gerente";

                case "Agendamentos":
                    return perfilUsuario == "Mecânico"
                        || perfilUsuario == "Vendedor"
                        || perfilUsuario == "Gerente";

                default:
                    return false;
            }
        }

    }
}
