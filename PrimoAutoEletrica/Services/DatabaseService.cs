using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
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

        public string DatabasePath => _databasePath;

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

            var settings = settingsOverride ?? DatabaseConnectionSettingsService.LoadOrCreateDefault(appDataPath, _logger);
            var providerPlan = DatabaseProviderPlanService.Avaliar(settings);
            if (!settings.IsSQLite)
            {
                Logger.LogWarning($"Provider '{settings.Provider}' configurado, mas o runtime atual ainda usa SQLite ate a migracao completa. Pendencias: {string.Join(" | ", providerPlan.Pendencias)}");
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

        // =====================================================
        // INICIALIZAÇÃO
        // =====================================================
        private void EnsureDatabaseInitialized()
        {
            var normalizedPath = Path.GetFullPath(_databasePath);

            lock (InitializationLock)
            {
                if (InitializedDatabases.Contains(normalizedPath))
                {
                    return;
                }

                try
                {
                    Logger.LogInfo($"Inicializando estrutura do banco SQLite em '{normalizedPath}'.");
                    InitializeDatabase();
                    InitializedDatabases.Add(normalizedPath);
                    Logger.LogInfo($"Estrutura do banco SQLite validada em '{normalizedPath}'.");
                }
                catch (Exception ex)
                {
                    Logger.LogCritical($"Falha ao inicializar estrutura do banco SQLite em '{normalizedPath}'.", ex);
                    throw;
                }
            }
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
                PRAGMA foreign_keys=OFF;
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
                    DataCadastro TEXT NOT NULL,
                    DataUltimoLogin TEXT,
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

                // Se a coluna RG não existe, recriar a tabela
                if (!hasRGColumn)
                {
                    var dropCommand = connection.CreateCommand();
                    dropCommand.CommandText = "DROP TABLE IF EXISTS Clientes;";
                    dropCommand.ExecuteNonQuery();

                    // Criar tabela de Clientes com estrutura correta
                    var clientesCommand = connection.CreateCommand();
                    clientesCommand.CommandText = @"
                        CREATE TABLE Clientes
                        (
                            Id TEXT PRIMARY KEY,
                            Nome TEXT NOT NULL,
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
        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
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
        private void InserirUsuariosPadrao(SqliteConnection connection)
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
                "Administrador"
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
            SqliteConnection connection,
            string nome,
            string email,
            string senha,
            string funcao,
            string perfil
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

                command.CommandText = @"
                    SELECT
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
                        DataCadastro,
                        DataUltimoLogin,
                        Ativo
                    FROM Funcionarios
                    WHERE lower(Email) = lower(@Email)
                      AND Ativo = 1
                    LIMIT 1
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
                    DataAdmissao = DateTime.Parse(reader.GetString(8)),
                    Salario = Convert.ToDecimal(reader.GetDouble(9)),
                    Status = reader.GetString(10),
                    DataCadastro = DateTime.Parse(reader.GetString(11)),
                    DataUltimoLogin = reader.IsDBNull(12) ? null : DateTime.Parse(reader.GetString(12)),
                    Ativo = reader.GetBoolean(13)
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

        private LoginAuthenticationResult RegistrarFalhaLogin(SqliteConnection connection, int funcionarioId)
        {
            var estadoAtual = ObterEstadoTentativasLogin(connection, funcionarioId);
            var tentativasFalhas = estadoAtual.TentativasFalhas + 1;
            DateTime? bloqueadoAte = null;

            if (tentativasFalhas >= MaxFailedLoginAttempts)
            {
                bloqueadoAte = DateTime.Now.Add(LoginLockoutDuration);
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
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

        private void ResetarTentativasLogin(SqliteConnection connection, int funcionarioId, bool registrarSucesso)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
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

        private static (int TentativasFalhas, DateTime? BloqueadoAte) ObterEstadoTentativasLogin(SqliteConnection connection, int funcionarioId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
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
                BloqueadoAte: reader.IsDBNull(1) || !DateTime.TryParse(reader.GetString(1), out var bloqueadoAte)
                    ? null
                    : bloqueadoAte);
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
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Seguranca",
                "SenhaMigradaParaHash",
                "Funcionario",
                funcionarioId.ToString());
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

        // =====================================================
        // CLIENTES - CRUD
        // =====================================================
        public List<Cliente> ObterTodosClientes()
        {
            if (UsarClienteRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Clientes.ObterTodos();

            var clientes = new List<Cliente>();

            try
            {
                using var connection = GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        Id,
                        Nome,
                        CPF,
                        RG,
                        DataNascimento,
                        Telefone,
                        WhatsApp,
                        Email,
                        CEP,
                        Rua,
                        Numero,
                        Bairro,
                        Cidade,
                        Estado,
                        Ativo,
                        ClienteVip,
                        TotalGasto,
                        TotalServicos,
                        PontosFidelidade,
                        Observacoes,
                        CaminhoDocumento,
                        CaminhoAssinatura,
                        DataCadastro,
                        UltimaVisita,
                        ImagemUrl
                    FROM Clientes
                    ORDER BY DataCadastro DESC
                ";

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    clientes.Add(new Cliente
                    {
                        Id = reader.GetGuid(0),
                        Nome = reader.GetString(1),
                        CPF = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        RG = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        DataNascimento = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                        Telefone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        WhatsApp = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        Email = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        CEP = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        Rua = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        Numero = reader.IsDBNull(10) ? "" : reader.GetString(10),
                        Bairro = reader.IsDBNull(11) ? "" : reader.GetString(11),
                        Cidade = reader.IsDBNull(12) ? "" : reader.GetString(12),
                        Estado = reader.IsDBNull(13) ? "" : reader.GetString(13),
                        Ativo = reader.GetBoolean(14),
                        ClienteVip = reader.GetBoolean(15),
                        TotalGasto = reader.IsDBNull(16) ? 0 : Convert.ToDecimal(reader.GetDouble(16)),
                        TotalServicos = reader.IsDBNull(17) ? 0 : reader.GetInt32(17),
                        PontosFidelidade = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                        Observacoes = reader.IsDBNull(19) ? "" : reader.GetString(19),
                        CaminhoDocumento = reader.IsDBNull(20) ? "" : reader.GetString(20),
                        CaminhoAssinatura = reader.IsDBNull(21) ? "" : reader.GetString(21),
                        DataCadastro = DateTime.Parse(reader.GetString(22)),
                        UltimaVisita = reader.IsDBNull(23) ? null : DateTime.Parse(reader.GetString(23)),
                        ImagemUrl = reader.IsDBNull(24) ? "" : reader.GetString(24)
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao obter clientes no banco de dados.", ex);
                throw;
            }

            foreach (var cliente in clientes)
            {
                cliente.Veiculos = ObterVeiculosPorClienteId(cliente.Id);
                cliente.HistoricoServicos = ObterHistoricoServicosPorClienteId(cliente.Id);
            }

            return clientes;
        }

        public Cliente? ObterClientePorId(Guid id)
        {
            if (UsarClienteRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Clientes.ObterPorId(id);

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    Nome,
                    CPF,
                    RG,
                    DataNascimento,
                    Telefone,
                    WhatsApp,
                    Email,
                    CEP,
                    Rua,
                    Numero,
                    Bairro,
                    Cidade,
                    Estado,
                    Ativo,
                    ClienteVip,
                    TotalGasto,
                    TotalServicos,
                    PontosFidelidade,
                    Observacoes,
                    CaminhoDocumento,
                    CaminhoAssinatura,
                    DataCadastro,
                    UltimaVisita,
                    ImagemUrl
                FROM Clientes
                WHERE Id = @Id
                LIMIT 1
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            var cliente = new Cliente
            {
                Id = reader.GetGuid(0),
                Nome = reader.GetString(1),
                CPF = reader.IsDBNull(2) ? "" : reader.GetString(2),
                RG = reader.IsDBNull(3) ? "" : reader.GetString(3),
                DataNascimento = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                Telefone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                WhatsApp = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Email = reader.IsDBNull(7) ? "" : reader.GetString(7),
                CEP = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Rua = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Numero = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Bairro = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Cidade = reader.IsDBNull(12) ? "" : reader.GetString(12),
                Estado = reader.IsDBNull(13) ? "" : reader.GetString(13),
                Ativo = reader.GetBoolean(14),
                ClienteVip = reader.GetBoolean(15),
                TotalGasto = reader.IsDBNull(16) ? 0 : Convert.ToDecimal(reader.GetDouble(16)),
                TotalServicos = reader.IsDBNull(17) ? 0 : reader.GetInt32(17),
                PontosFidelidade = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                Observacoes = reader.IsDBNull(19) ? "" : reader.GetString(19),
                CaminhoDocumento = reader.IsDBNull(20) ? "" : reader.GetString(20),
                CaminhoAssinatura = reader.IsDBNull(21) ? "" : reader.GetString(21),
                DataCadastro = DateTime.Parse(reader.GetString(22)),
                UltimaVisita = reader.IsDBNull(23) ? null : DateTime.Parse(reader.GetString(23)),
                ImagemUrl = reader.IsDBNull(24) ? "" : reader.GetString(24)
            };

            cliente.Veiculos = ObterVeiculosPorClienteId(cliente.Id);
            cliente.HistoricoServicos = ObterHistoricoServicosPorClienteId(cliente.Id);

            return cliente;
        }

        public void InserirCliente(Cliente cliente)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.Inserir(cliente);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = @"
        INSERT INTO Clientes
        (
            Id,
            Nome,
            CPF,
            RG,
            DataNascimento,
            Telefone,
            WhatsApp,
            Email,
            CEP,
            Rua,
            Numero,
            Bairro,
            Cidade,
            Estado,
            Ativo,
            ClienteVip,
            TotalGasto,
            TotalServicos,
            PontosFidelidade,
            Observacoes,
            CaminhoDocumento,
            CaminhoAssinatura,
            DataCadastro,
            UltimaVisita,
            ImagemUrl
        )
        VALUES
        (
            @Id,
            @Nome,
            @CPF,
            @RG,
            @DataNascimento,
            @Telefone,
            @WhatsApp,
            @Email,
            @CEP,
            @Rua,
            @Numero,
            @Bairro,
            @Cidade,
            @Estado,
            @Ativo,
            @ClienteVip,
            @TotalGasto,
            @TotalServicos,
            @PontosFidelidade,
            @Observacoes,
            @CaminhoDocumento,
            @CaminhoAssinatura,
            @DataCadastro,
            @UltimaVisita,
            @ImagemUrl
        )
    ";

            // =========================
            // PARÂMETROS
            // =========================

            command.Parameters.AddWithValue(
                "@Id",
                cliente.Id.ToString()
            );

            command.Parameters.AddWithValue(
                "@Nome",
                string.IsNullOrWhiteSpace(cliente.Nome)
                    ? DBNull.Value
                    : cliente.Nome
            );

            command.Parameters.AddWithValue(
                "@CPF",
                string.IsNullOrWhiteSpace(cliente.CPF)
                    ? DBNull.Value
                    : cliente.CPF
            );

            command.Parameters.AddWithValue(
                "@RG",
                string.IsNullOrWhiteSpace(cliente.RG)
                    ? DBNull.Value
                    : cliente.RG
            );

            command.Parameters.AddWithValue(
                "@DataNascimento",
                cliente.DataNascimento.HasValue
                    ? cliente.DataNascimento.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@Telefone",
                string.IsNullOrWhiteSpace(cliente.Telefone)
                    ? DBNull.Value
                    : cliente.Telefone
            );

            command.Parameters.AddWithValue(
                "@WhatsApp",
                string.IsNullOrWhiteSpace(cliente.WhatsApp)
                    ? DBNull.Value
                    : cliente.WhatsApp
            );

            command.Parameters.AddWithValue(
                "@Email",
                string.IsNullOrWhiteSpace(cliente.Email)
                    ? DBNull.Value
                    : cliente.Email
            );

            command.Parameters.AddWithValue(
                "@CEP",
                string.IsNullOrWhiteSpace(cliente.CEP)
                    ? DBNull.Value
                    : cliente.CEP
            );

            command.Parameters.AddWithValue(
                "@Rua",
                string.IsNullOrWhiteSpace(cliente.Rua)
                    ? DBNull.Value
                    : cliente.Rua
            );

            command.Parameters.AddWithValue(
                "@Numero",
                string.IsNullOrWhiteSpace(cliente.Numero)
                    ? DBNull.Value
                    : cliente.Numero
            );

            command.Parameters.AddWithValue(
                "@Bairro",
                string.IsNullOrWhiteSpace(cliente.Bairro)
                    ? DBNull.Value
                    : cliente.Bairro
            );

            command.Parameters.AddWithValue(
                "@Cidade",
                string.IsNullOrWhiteSpace(cliente.Cidade)
                    ? DBNull.Value
                    : cliente.Cidade
            );

            command.Parameters.AddWithValue(
                "@Estado",
                string.IsNullOrWhiteSpace(cliente.Estado)
                    ? DBNull.Value
                    : cliente.Estado
            );

            command.Parameters.AddWithValue(
                "@Ativo",
                cliente.Ativo ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "@ClienteVip",
                cliente.ClienteVip ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "@TotalGasto",
                cliente.TotalGasto
            );

            command.Parameters.AddWithValue(
                "@TotalServicos",
                cliente.TotalServicos
            );

            command.Parameters.AddWithValue(
                "@PontosFidelidade",
                cliente.PontosFidelidade
            );

            command.Parameters.AddWithValue(
                "@Observacoes",
                string.IsNullOrWhiteSpace(cliente.Observacoes)
                    ? DBNull.Value
                    : cliente.Observacoes
            );

            command.Parameters.AddWithValue(
                "@CaminhoDocumento",
                string.IsNullOrWhiteSpace(cliente.CaminhoDocumento)
                    ? DBNull.Value
                    : cliente.CaminhoDocumento
            );

            command.Parameters.AddWithValue(
                "@CaminhoAssinatura",
                string.IsNullOrWhiteSpace(cliente.CaminhoAssinatura)
                    ? DBNull.Value
                    : cliente.CaminhoAssinatura
            );

            command.Parameters.AddWithValue(
                "@DataCadastro",
                cliente.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss")
            );

            command.Parameters.AddWithValue(
                "@UltimaVisita",
                cliente.UltimaVisita.HasValue
                    ? cliente.UltimaVisita.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@ImagemUrl",
                string.IsNullOrWhiteSpace(cliente.ImagemUrl)
                    ? DBNull.Value
                    : cliente.ImagemUrl
            );

            // =========================
            // EXECUTA
            // =========================

            command.ExecuteNonQuery();
            SalvarVeiculosDoCliente(cliente.Id, cliente.Veiculos, connection, transaction);
            AtualizarResumoCliente(connection, transaction, cliente.Id);
            transaction.Commit();
        }
        public void AtualizarCliente(Cliente cliente)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.Atualizar(cliente);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Clientes
                SET
                    Nome = @Nome,
                    CPF = @CPF,
                    RG = @RG,
                    DataNascimento = @DataNascimento,
                    Telefone = @Telefone,
                    WhatsApp = @WhatsApp,
                    Email = @Email,
                    CEP = @CEP,
                    Rua = @Rua,
                    Numero = @Numero,
                    Bairro = @Bairro,
                    Cidade = @Cidade,
                    Estado = @Estado,
                    Ativo = @Ativo,
                    ClienteVip = @ClienteVip,
                    TotalGasto = @TotalGasto,
                    TotalServicos = @TotalServicos,
                    PontosFidelidade = @PontosFidelidade,
                    Observacoes = @Observacoes,
                    CaminhoDocumento = @CaminhoDocumento,
                    CaminhoAssinatura = @CaminhoAssinatura,
                    UltimaVisita = @UltimaVisita,
                    ImagemUrl = @ImagemUrl
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Nome", ToDbNullableString(cliente.Nome));
            command.Parameters.AddWithValue("@CPF", ToDbNullableString(cliente.CPF));
            command.Parameters.AddWithValue("@RG", ToDbNullableString(cliente.RG));
            command.Parameters.AddWithValue("@DataNascimento", ToDbNullableDate(cliente.DataNascimento, "yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Telefone", ToDbNullableString(cliente.Telefone));
            command.Parameters.AddWithValue("@WhatsApp", ToDbNullableString(cliente.WhatsApp));
            command.Parameters.AddWithValue("@Email", ToDbNullableString(cliente.Email));
            command.Parameters.AddWithValue("@CEP", ToDbNullableString(cliente.CEP));
            command.Parameters.AddWithValue("@Rua", ToDbNullableString(cliente.Rua));
            command.Parameters.AddWithValue("@Numero", ToDbNullableString(cliente.Numero));
            command.Parameters.AddWithValue("@Bairro", ToDbNullableString(cliente.Bairro));
            command.Parameters.AddWithValue("@Cidade", ToDbNullableString(cliente.Cidade));
            command.Parameters.AddWithValue("@Estado", ToDbNullableString(cliente.Estado));
            command.Parameters.AddWithValue("@Ativo", cliente.Ativo ? 1 : 0);
            command.Parameters.AddWithValue("@ClienteVip", cliente.ClienteVip ? 1 : 0);
            command.Parameters.AddWithValue("@TotalGasto", cliente.TotalGasto);
            command.Parameters.AddWithValue("@TotalServicos", cliente.TotalServicos);
            command.Parameters.AddWithValue("@PontosFidelidade", cliente.PontosFidelidade);
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(cliente.Observacoes));
            command.Parameters.AddWithValue("@CaminhoDocumento", ToDbNullableString(cliente.CaminhoDocumento));
            command.Parameters.AddWithValue("@CaminhoAssinatura", ToDbNullableString(cliente.CaminhoAssinatura));
            command.Parameters.AddWithValue("@UltimaVisita", ToDbNullableDate(cliente.UltimaVisita, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ImagemUrl", ToDbNullableString(cliente.ImagemUrl));
            command.Parameters.AddWithValue("@Id", cliente.Id.ToString());

            command.ExecuteNonQuery();
            SalvarVeiculosDoCliente(cliente.Id, cliente.Veiculos, connection, transaction);
            AtualizarResumoCliente(connection, transaction, cliente.Id);
            transaction.Commit();
        }

        public void ExcluirCliente(Guid id)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.Excluir(id);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            if (ClientePossuiOrdensServico(connection, transaction, id))
                throw new InvalidOperationException("O cliente possui ordens de servico cadastradas. Inative o cadastro em vez de excluir.");

            ExcluirVeiculosDoCliente(id, connection, transaction);

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                DELETE FROM Clientes
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            command.ExecuteNonQuery();
            transaction.Commit();
        }

        // =====================================================
        // PRODUTOS - CRUD
        // =====================================================
        public List<Produto> ObterTodosProdutos()
        {
            if (UsarProdutoRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Produtos.ObterTodos();

            var produtos = new List<Produto>();

            try
            {
                using var connection = GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();

                command.CommandText = @"
            SELECT
                Id,
                Codigo,
                Nome,
                Descricao,
                Categoria,
                Marca,
                Modelo,
                Fornecedor,
                CNPJFornecedor,
                ContatoFornecedor,
                TelefoneFornecedor,
                QuantidadeEstoque,
                QuantidadeMinima,
                QuantidadeMaxima,
                Localizacao,
                Prateleira,
                Gaveta,
                PrecoCompra,
                PrecoVenda,
                MargemLucro,
                ValorTotalEstoque,
                UnidadeMedida,
                Peso,
                Dimensoes,
                Cor,
                Material,
                CodigoBarras,
                SKU,
                NCMS,
                CEST,
                CFOP,
                Ativo,
                ProdutoPerecivel,
                DataValidade,
                DataFabricacao,
                Lote,
                DataCadastro,
                DataUltimaCompra,
                DataUltimaVenda,
                DataUltimaAtualizacao,
                Observacoes,
                ImagemUrl,
                Anexos,
                TotalVendas,
                TotalFaturado,
                VendasUltimoMes,
                VendasUltimoTrimestre
            FROM Produtos
            ORDER BY DataCadastro DESC
        ";

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    try
                    {
                        var produto = new Produto
                        {
                            Id = reader.IsDBNull(0)
                                ? Guid.NewGuid()
                                : Guid.Parse(reader.GetString(0)),

                            Codigo = reader.IsDBNull(1)
                                ? ""
                                : reader.GetString(1),

                            Nome = reader.IsDBNull(2)
                                ? ""
                                : reader.GetString(2),

                            Descricao = reader.IsDBNull(3)
                                ? ""
                                : reader.GetString(3),

                            Categoria = reader.IsDBNull(4)
                                ? ""
                                : reader.GetString(4),

                            Marca = reader.IsDBNull(5)
                                ? ""
                                : reader.GetString(5),

                            Modelo = reader.IsDBNull(6)
                                ? ""
                                : reader.GetString(6),

                            Fornecedor = reader.IsDBNull(7)
                                ? ""
                                : reader.GetString(7),

                            CNPJFornecedor = reader.IsDBNull(8)
                                ? ""
                                : reader.GetString(8),

                            ContatoFornecedor = reader.IsDBNull(9)
                                ? ""
                                : reader.GetString(9),

                            TelefoneFornecedor = reader.IsDBNull(10)
                                ? ""
                                : reader.GetString(10),

                            QuantidadeEstoque = reader.IsDBNull(11)
                                ? 0
                                : reader.GetInt32(11),

                            QuantidadeMinima = reader.IsDBNull(12)
                                ? 0
                                : reader.GetInt32(12),

                            QuantidadeMaxima = reader.IsDBNull(13)
                                ? 0
                                : reader.GetInt32(13),

                            Localizacao = reader.IsDBNull(14)
                                ? ""
                                : reader.GetString(14),

                            Prateleira = reader.IsDBNull(15)
                                ? ""
                                : reader.GetString(15),

                            Gaveta = reader.IsDBNull(16)
                                ? ""
                                : reader.GetString(16),

                            PrecoCompra = reader.IsDBNull(17)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(17)),

                            PrecoVenda = reader.IsDBNull(18)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(18)),

                            MargemLucro = reader.IsDBNull(19)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(19)),

                            ValorTotalEstoque = reader.IsDBNull(20)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(20)),

                            UnidadeMedida = reader.IsDBNull(21)
                                ? ""
                                : reader.GetString(21),

                            Peso = reader.IsDBNull(22)
                                ? ""
                                : reader.GetString(22),

                            Dimensoes = reader.IsDBNull(23)
                                ? ""
                                : reader.GetString(23),

                            Cor = reader.IsDBNull(24)
                                ? ""
                                : reader.GetString(24),

                            Material = reader.IsDBNull(25)
                                ? ""
                                : reader.GetString(25),

                            CodigoBarras = reader.IsDBNull(26)
                                ? ""
                                : reader.GetString(26),

                            SKU = reader.IsDBNull(27)
                                ? ""
                                : reader.GetString(27),

                            NCMS = reader.IsDBNull(28)
                                ? ""
                                : reader.GetString(28),

                            CEST = reader.IsDBNull(29)
                                ? ""
                                : reader.GetString(29),

                            CFOP = reader.IsDBNull(30)
                                ? ""
                                : reader.GetString(30),

                            Ativo = !reader.IsDBNull(31)
                                && reader.GetBoolean(31),

                            ProdutoPerecivel = !reader.IsDBNull(32)
                                && reader.GetBoolean(32),

                            DataValidade = reader.IsDBNull(33)
                                ? null
                                : DateTime.Parse(reader.GetString(33)),

                            DataFabricacao = reader.IsDBNull(34)
                                ? null
                                : DateTime.Parse(reader.GetString(34)),

                            Lote = reader.IsDBNull(35)
                                ? ""
                                : reader.GetString(35),

                            DataCadastro = reader.IsDBNull(36)
                                ? DateTime.Now
                                : DateTime.Parse(reader.GetString(36)),

                            DataUltimaCompra = reader.IsDBNull(37)
                                ? null
                                : DateTime.Parse(reader.GetString(37)),

                            DataUltimaVenda = reader.IsDBNull(38)
                                ? null
                                : DateTime.Parse(reader.GetString(38)),

                            DataUltimaAtualizacao = reader.IsDBNull(39)
                                ? null
                                : DateTime.Parse(reader.GetString(39)),

                            Observacoes = reader.IsDBNull(40)
                                ? ""
                                : reader.GetString(40),

                            ImagemUrl = reader.IsDBNull(41)
                                ? ""
                                : reader.GetString(41),

                            Anexos = reader.IsDBNull(42)
                                ? ""
                                : reader.GetString(42),

                            TotalVendas = reader.IsDBNull(43)
                                ? 0
                                : reader.GetInt32(43),

                            TotalFaturado = reader.IsDBNull(44)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(44)),

                            VendasUltimoMes = reader.IsDBNull(45)
                                ? 0
                                : reader.GetInt32(45),

                            VendasUltimoTrimestre = reader.IsDBNull(46)
                                ? 0
                                : reader.GetInt32(46)
                        };

                        produtos.Add(produto);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Erro ao materializar um produto durante a leitura da listagem.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao obter produtos no banco de dados.", ex);
            }

            return produtos;
        }
        public Produto? ObterProdutoPorId(Guid id)
        {
            if (UsarProdutoRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Produtos.ObterPorId(id);

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    Codigo,
                    Nome,
                    Descricao,
                    Categoria,
                    Marca,
                    Modelo,
                    Fornecedor,
                    CNPJFornecedor,
                    ContatoFornecedor,
                    TelefoneFornecedor,
                    QuantidadeEstoque,
                    QuantidadeMinima,
                    QuantidadeMaxima,
                    Localizacao,
                    Prateleira,
                    Gaveta,
                    PrecoCompra,
                    PrecoVenda,
                    MargemLucro,
                    ValorTotalEstoque,
                    UnidadeMedida,
                    Peso,
                    Dimensoes,
                    Cor,
                    Material,
                    CodigoBarras,
                    SKU,
                    NCMS,
                    CEST,
                    CFOP,
                    Ativo,
                    ProdutoPerecivel,
                    DataValidade,
                    DataFabricacao,
                    Lote,
                    DataCadastro,
                    DataUltimaCompra,
                    DataUltimaVenda,
                    DataUltimaAtualizacao,
                    Observacoes,
                    ImagemUrl,
                    Anexos,
                    TotalVendas,
                    TotalFaturado,
                    VendasUltimoMes,
                    VendasUltimoTrimestre
                FROM Produtos
                WHERE Id = @Id
                LIMIT 1
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Produto
            {
                Id = Guid.Parse(reader.GetString(0)),
                Codigo = reader.GetString(1),
                Nome = reader.GetString(2),
                Descricao = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Categoria = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Marca = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Modelo = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Fornecedor = reader.IsDBNull(7) ? "" : reader.GetString(7),
                CNPJFornecedor = reader.IsDBNull(8) ? "" : reader.GetString(8),
                ContatoFornecedor = reader.IsDBNull(9) ? "" : reader.GetString(9),
                TelefoneFornecedor = reader.IsDBNull(10) ? "" : reader.GetString(10),
                QuantidadeEstoque = reader.GetInt32(11),
                QuantidadeMinima = reader.GetInt32(12),
                QuantidadeMaxima = reader.GetInt32(13),
                Localizacao = reader.IsDBNull(14) ? "" : reader.GetString(14),
                Prateleira = reader.IsDBNull(15) ? "" : reader.GetString(15),
                Gaveta = reader.IsDBNull(16) ? "" : reader.GetString(16),
                PrecoCompra = reader.IsDBNull(17) ? 0 : Convert.ToDecimal(reader.GetDouble(17)),
                PrecoVenda = reader.IsDBNull(18) ? 0 : Convert.ToDecimal(reader.GetDouble(18)),
                MargemLucro = reader.IsDBNull(19) ? 0 : Convert.ToDecimal(reader.GetDouble(19)),
                ValorTotalEstoque = reader.IsDBNull(20) ? 0 : Convert.ToDecimal(reader.GetDouble(20)),
                UnidadeMedida = reader.IsDBNull(21) ? "" : reader.GetString(21),
                Peso = reader.IsDBNull(22) ? "" : reader.GetString(22),
                Dimensoes = reader.IsDBNull(23) ? "" : reader.GetString(23),
                Cor = reader.IsDBNull(24) ? "" : reader.GetString(24),
                Material = reader.IsDBNull(25) ? "" : reader.GetString(25),
                CodigoBarras = reader.IsDBNull(26) ? "" : reader.GetString(26),
                SKU = reader.IsDBNull(27) ? "" : reader.GetString(27),
                NCMS = reader.IsDBNull(28) ? "" : reader.GetString(28),
                CEST = reader.IsDBNull(29) ? "" : reader.GetString(29),
                CFOP = reader.IsDBNull(30) ? "" : reader.GetString(30),
                Ativo = reader.GetBoolean(31),
                ProdutoPerecivel = reader.GetBoolean(32),
                DataValidade = reader.IsDBNull(33) ? null : DateTime.Parse(reader.GetString(33)),
                DataFabricacao = reader.IsDBNull(34) ? null : DateTime.Parse(reader.GetString(34)),
                Lote = reader.IsDBNull(35) ? "" : reader.GetString(35),
                DataCadastro = DateTime.Parse(reader.GetString(36)),
                DataUltimaCompra = reader.IsDBNull(37) ? null : DateTime.Parse(reader.GetString(37)),
                DataUltimaVenda = reader.IsDBNull(38) ? null : DateTime.Parse(reader.GetString(38)),
                DataUltimaAtualizacao = reader.IsDBNull(39) ? null : DateTime.Parse(reader.GetString(39)),
                Observacoes = reader.IsDBNull(40) ? "" : reader.GetString(40),
                ImagemUrl = reader.IsDBNull(41) ? "" : reader.GetString(41),
                Anexos = reader.IsDBNull(42) ? "" : reader.GetString(42),
                TotalVendas = reader.GetInt32(43),
                TotalFaturado = reader.IsDBNull(44) ? 0 : Convert.ToDecimal(reader.GetDouble(44)),
                VendasUltimoMes = reader.GetInt32(45),
                VendasUltimoTrimestre = reader.GetInt32(46)
            };
        }

        public void InserirProduto(Produto produto)
        {
            if (UsarProdutoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Produtos.Inserir(produto);
                return;
            }

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
        INSERT INTO Produtos
        (
            Id,
            Codigo,
            Nome,
            Descricao,
            Categoria,
            Marca,
            Modelo,
            Fornecedor,
            CNPJFornecedor,
            ContatoFornecedor,
            TelefoneFornecedor,
            QuantidadeEstoque,
            QuantidadeMinima,
            QuantidadeMaxima,
            Localizacao,
            Prateleira,
            Gaveta,
            PrecoCompra,
            PrecoVenda,
            MargemLucro,
            ValorTotalEstoque,
            UnidadeMedida,
            Peso,
            Dimensoes,
            Cor,
            Material,
            CodigoBarras,
            SKU,
            NCMS,
            CEST,
            CFOP,
            Ativo,
            ProdutoPerecivel,
            DataValidade,
            DataFabricacao,
            Lote,
            DataCadastro,
            DataUltimaCompra,
            DataUltimaVenda,
            DataUltimaAtualizacao,
            Observacoes,
            ImagemUrl,
            Anexos,
            TotalVendas,
            TotalFaturado,
            VendasUltimoMes,
            VendasUltimoTrimestre
        )
        VALUES
        (
            @Id,
            @Codigo,
            @Nome,
            @Descricao,
            @Categoria,
            @Marca,
            @Modelo,
            @Fornecedor,
            @CNPJFornecedor,
            @ContatoFornecedor,
            @TelefoneFornecedor,
            @QuantidadeEstoque,
            @QuantidadeMinima,
            @QuantidadeMaxima,
            @Localizacao,
            @Prateleira,
            @Gaveta,
            @PrecoCompra,
            @PrecoVenda,
            @MargemLucro,
            @ValorTotalEstoque,
            @UnidadeMedida,
            @Peso,
            @Dimensoes,
            @Cor,
            @Material,
            @CodigoBarras,
            @SKU,
            @NCMS,
            @CEST,
            @CFOP,
            @Ativo,
            @ProdutoPerecivel,
            @DataValidade,
            @DataFabricacao,
            @Lote,
            @DataCadastro,
            @DataUltimaCompra,
            @DataUltimaVenda,
            @DataUltimaAtualizacao,
            @Observacoes,
            @ImagemUrl,
            @Anexos,
            @TotalVendas,
            @TotalFaturado,
            @VendasUltimoMes,
            @VendasUltimoTrimestre
        )
    ";

            // =========================
            // PARÂMETROS
            // =========================

            command.Parameters.AddWithValue("@Id", produto.Id.ToString());

            command.Parameters.AddWithValue(
                "@Codigo",
                string.IsNullOrWhiteSpace(produto.Codigo)
                    ? DBNull.Value
                    : produto.Codigo
            );

            command.Parameters.AddWithValue(
                "@Nome",
                string.IsNullOrWhiteSpace(produto.Nome)
                    ? DBNull.Value
                    : produto.Nome
            );

            command.Parameters.AddWithValue(
                "@Descricao",
                string.IsNullOrWhiteSpace(produto.Descricao)
                    ? DBNull.Value
                    : produto.Descricao
            );

            command.Parameters.AddWithValue(
                "@Categoria",
                string.IsNullOrWhiteSpace(produto.Categoria)
                    ? DBNull.Value
                    : produto.Categoria
            );

            command.Parameters.AddWithValue(
                "@Marca",
                string.IsNullOrWhiteSpace(produto.Marca)
                    ? DBNull.Value
                    : produto.Marca
            );

            command.Parameters.AddWithValue(
                "@Modelo",
                string.IsNullOrWhiteSpace(produto.Modelo)
                    ? DBNull.Value
                    : produto.Modelo
            );

            command.Parameters.AddWithValue(
                "@Fornecedor",
                string.IsNullOrWhiteSpace(produto.Fornecedor)
                    ? DBNull.Value
                    : produto.Fornecedor
            );

            command.Parameters.AddWithValue(
                "@CNPJFornecedor",
                string.IsNullOrWhiteSpace(produto.CNPJFornecedor)
                    ? DBNull.Value
                    : produto.CNPJFornecedor
            );

            command.Parameters.AddWithValue(
                "@ContatoFornecedor",
                string.IsNullOrWhiteSpace(produto.ContatoFornecedor)
                    ? DBNull.Value
                    : produto.ContatoFornecedor
            );

            command.Parameters.AddWithValue(
                "@TelefoneFornecedor",
                string.IsNullOrWhiteSpace(produto.TelefoneFornecedor)
                    ? DBNull.Value
                    : produto.TelefoneFornecedor
            );

            command.Parameters.AddWithValue("@QuantidadeEstoque", produto.QuantidadeEstoque);
            command.Parameters.AddWithValue("@QuantidadeMinima", produto.QuantidadeMinima);
            command.Parameters.AddWithValue("@QuantidadeMaxima", produto.QuantidadeMaxima);

            command.Parameters.AddWithValue(
                "@Localizacao",
                string.IsNullOrWhiteSpace(produto.Localizacao)
                    ? DBNull.Value
                    : produto.Localizacao
            );

            command.Parameters.AddWithValue(
                "@Prateleira",
                string.IsNullOrWhiteSpace(produto.Prateleira)
                    ? DBNull.Value
                    : produto.Prateleira
            );

            command.Parameters.AddWithValue(
                "@Gaveta",
                string.IsNullOrWhiteSpace(produto.Gaveta)
                    ? DBNull.Value
                    : produto.Gaveta
            );

            command.Parameters.AddWithValue("@PrecoCompra", produto.PrecoCompra);
            command.Parameters.AddWithValue("@PrecoVenda", produto.PrecoVenda);
            command.Parameters.AddWithValue("@MargemLucro", produto.MargemLucro);
            command.Parameters.AddWithValue("@ValorTotalEstoque", produto.ValorTotalEstoque);

            command.Parameters.AddWithValue(
                "@UnidadeMedida",
                string.IsNullOrWhiteSpace(produto.UnidadeMedida)
                    ? DBNull.Value
                    : produto.UnidadeMedida
            );

            command.Parameters.AddWithValue("@Peso", produto.Peso);

            command.Parameters.AddWithValue(
                "@Dimensoes",
                string.IsNullOrWhiteSpace(produto.Dimensoes)
                    ? DBNull.Value
                    : produto.Dimensoes
            );

            command.Parameters.AddWithValue(
                "@Cor",
                string.IsNullOrWhiteSpace(produto.Cor)
                    ? DBNull.Value
                    : produto.Cor
            );

            command.Parameters.AddWithValue(
                "@Material",
                string.IsNullOrWhiteSpace(produto.Material)
                    ? DBNull.Value
                    : produto.Material
            );

            command.Parameters.AddWithValue(
                "@CodigoBarras",
                string.IsNullOrWhiteSpace(produto.CodigoBarras)
                    ? DBNull.Value
                    : produto.CodigoBarras
            );

            command.Parameters.AddWithValue(
                "@SKU",
                string.IsNullOrWhiteSpace(produto.SKU)
                    ? DBNull.Value
                    : produto.SKU
            );

            command.Parameters.AddWithValue(
                "@NCMS",
                string.IsNullOrWhiteSpace(produto.NCMS)
                    ? DBNull.Value
                    : produto.NCMS
            );

            command.Parameters.AddWithValue(
                "@CEST",
                string.IsNullOrWhiteSpace(produto.CEST)
                    ? DBNull.Value
                    : produto.CEST
            );

            command.Parameters.AddWithValue(
                "@CFOP",
                string.IsNullOrWhiteSpace(produto.CFOP)
                    ? DBNull.Value
                    : produto.CFOP
            );

            command.Parameters.AddWithValue("@Ativo", produto.Ativo ? 1 : 0);

            command.Parameters.AddWithValue(
                "@ProdutoPerecivel",
                produto.ProdutoPerecivel ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "@DataValidade",
                produto.DataValidade.HasValue
                    ? produto.DataValidade.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@DataFabricacao",
                produto.DataFabricacao.HasValue
                    ? produto.DataFabricacao.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@Lote",
                string.IsNullOrWhiteSpace(produto.Lote)
                    ? DBNull.Value
                    : produto.Lote
            );

            command.Parameters.AddWithValue(
                "@DataCadastro",
                produto.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss")
            );

            command.Parameters.AddWithValue(
                "@DataUltimaCompra",
                produto.DataUltimaCompra.HasValue
                    ? produto.DataUltimaCompra.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@DataUltimaVenda",
                produto.DataUltimaVenda.HasValue
                    ? produto.DataUltimaVenda.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@DataUltimaAtualizacao",
                produto.DataUltimaAtualizacao.HasValue
                    ? produto.DataUltimaAtualizacao.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@Observacoes",
                string.IsNullOrWhiteSpace(produto.Observacoes)
                    ? DBNull.Value
                    : produto.Observacoes
            );

            command.Parameters.AddWithValue(
                "@ImagemUrl",
                string.IsNullOrWhiteSpace(produto.ImagemUrl)
                    ? DBNull.Value
                    : produto.ImagemUrl
            );

            command.Parameters.AddWithValue(
                "@Anexos",
                string.IsNullOrWhiteSpace(produto.Anexos)
                    ? DBNull.Value
                    : produto.Anexos
            );

            command.Parameters.AddWithValue("@TotalVendas", produto.TotalVendas);
            command.Parameters.AddWithValue("@TotalFaturado", produto.TotalFaturado);
            command.Parameters.AddWithValue("@VendasUltimoMes", produto.VendasUltimoMes);
            command.Parameters.AddWithValue("@VendasUltimoTrimestre", produto.VendasUltimoTrimestre);

            command.ExecuteNonQuery();
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Estoque",
                "ProdutoCriado",
                "Produto",
                produto.Id.ToString(),
                $"Codigo={produto.Codigo}; Nome={produto.Nome}; Estoque={produto.QuantidadeEstoque}; PrecoVenda={produto.PrecoVenda:C}");
        }
        public void AtualizarProduto(Produto produto)
        {
            if (UsarProdutoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Produtos.Atualizar(produto);
                return;
            }

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Produtos
                SET
                    Codigo = @Codigo,
                    Nome = @Nome,
                    Descricao = @Descricao,
                    Categoria = @Categoria,
                    Marca = @Marca,
                    Modelo = @Modelo,
                    Fornecedor = @Fornecedor,
                    CNPJFornecedor = @CNPJFornecedor,
                    ContatoFornecedor = @ContatoFornecedor,
                    TelefoneFornecedor = @TelefoneFornecedor,
                    QuantidadeEstoque = @QuantidadeEstoque,
                    QuantidadeMinima = @QuantidadeMinima,
                    QuantidadeMaxima = @QuantidadeMaxima,
                    Localizacao = @Localizacao,
                    Prateleira = @Prateleira,
                    Gaveta = @Gaveta,
                    PrecoCompra = @PrecoCompra,
                    PrecoVenda = @PrecoVenda,
                    MargemLucro = @MargemLucro,
                    ValorTotalEstoque = @ValorTotalEstoque,
                    UnidadeMedida = @UnidadeMedida,
                    Peso = @Peso,
                    Dimensoes = @Dimensoes,
                    Cor = @Cor,
                    Material = @Material,
                    CodigoBarras = @CodigoBarras,
                    SKU = @SKU,
                    NCMS = @NCMS,
                    CEST = @CEST,
                    CFOP = @CFOP,
                    Ativo = @Ativo,
                    ProdutoPerecivel = @ProdutoPerecivel,
                    DataValidade = @DataValidade,
                    DataFabricacao = @DataFabricacao,
                    Lote = @Lote,
                    DataUltimaCompra = @DataUltimaCompra,
                    DataUltimaVenda = @DataUltimaVenda,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao,
                    Observacoes = @Observacoes,
                    ImagemUrl = @ImagemUrl,
                    Anexos = @Anexos,
                    TotalVendas = @TotalVendas,
                    TotalFaturado = @TotalFaturado,
                    VendasUltimoMes = @VendasUltimoMes,
                    VendasUltimoTrimestre = @VendasUltimoTrimestre
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Codigo", ToDbNullableString(produto.Codigo));
            command.Parameters.AddWithValue("@Nome", ToDbNullableString(produto.Nome));
            command.Parameters.AddWithValue("@Descricao", ToDbNullableString(produto.Descricao));
            command.Parameters.AddWithValue("@Categoria", ToDbNullableString(produto.Categoria));
            command.Parameters.AddWithValue("@Marca", ToDbNullableString(produto.Marca));
            command.Parameters.AddWithValue("@Modelo", ToDbNullableString(produto.Modelo));
            command.Parameters.AddWithValue("@Fornecedor", ToDbNullableString(produto.Fornecedor));
            command.Parameters.AddWithValue("@CNPJFornecedor", ToDbNullableString(produto.CNPJFornecedor));
            command.Parameters.AddWithValue("@ContatoFornecedor", ToDbNullableString(produto.ContatoFornecedor));
            command.Parameters.AddWithValue("@TelefoneFornecedor", ToDbNullableString(produto.TelefoneFornecedor));
            command.Parameters.AddWithValue("@QuantidadeEstoque", produto.QuantidadeEstoque);
            command.Parameters.AddWithValue("@QuantidadeMinima", produto.QuantidadeMinima);
            command.Parameters.AddWithValue("@QuantidadeMaxima", produto.QuantidadeMaxima);
            command.Parameters.AddWithValue("@Localizacao", ToDbNullableString(produto.Localizacao));
            command.Parameters.AddWithValue("@Prateleira", ToDbNullableString(produto.Prateleira));
            command.Parameters.AddWithValue("@Gaveta", ToDbNullableString(produto.Gaveta));
            command.Parameters.AddWithValue("@PrecoCompra", produto.PrecoCompra);
            command.Parameters.AddWithValue("@PrecoVenda", produto.PrecoVenda);
            command.Parameters.AddWithValue("@MargemLucro", produto.MargemLucro);
            command.Parameters.AddWithValue("@ValorTotalEstoque", produto.ValorTotalEstoque);
            command.Parameters.AddWithValue("@UnidadeMedida", ToDbNullableString(produto.UnidadeMedida));
            command.Parameters.AddWithValue("@Peso", ToDbNullableString(produto.Peso));
            command.Parameters.AddWithValue("@Dimensoes", ToDbNullableString(produto.Dimensoes));
            command.Parameters.AddWithValue("@Cor", ToDbNullableString(produto.Cor));
            command.Parameters.AddWithValue("@Material", ToDbNullableString(produto.Material));
            command.Parameters.AddWithValue("@CodigoBarras", ToDbNullableString(produto.CodigoBarras));
            command.Parameters.AddWithValue("@SKU", ToDbNullableString(produto.SKU));
            command.Parameters.AddWithValue("@NCMS", ToDbNullableString(produto.NCMS));
            command.Parameters.AddWithValue("@CEST", ToDbNullableString(produto.CEST));
            command.Parameters.AddWithValue("@CFOP", ToDbNullableString(produto.CFOP));
            command.Parameters.AddWithValue("@Ativo", produto.Ativo ? 1 : 0);
            command.Parameters.AddWithValue("@ProdutoPerecivel", produto.ProdutoPerecivel ? 1 : 0);
            command.Parameters.AddWithValue("@DataValidade", ToDbNullableDate(produto.DataValidade, "yyyy-MM-dd"));
            command.Parameters.AddWithValue("@DataFabricacao", ToDbNullableDate(produto.DataFabricacao, "yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Lote", ToDbNullableString(produto.Lote));
            command.Parameters.AddWithValue("@DataUltimaCompra", ToDbNullableDate(produto.DataUltimaCompra, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataUltimaVenda", ToDbNullableDate(produto.DataUltimaVenda, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", ToDbNullableDate(produto.DataUltimaAtualizacao, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(produto.Observacoes));
            command.Parameters.AddWithValue("@ImagemUrl", ToDbNullableString(produto.ImagemUrl));
            command.Parameters.AddWithValue("@Anexos", ToDbNullableString(produto.Anexos));
            command.Parameters.AddWithValue("@TotalVendas", produto.TotalVendas);
            command.Parameters.AddWithValue("@TotalFaturado", produto.TotalFaturado);
            command.Parameters.AddWithValue("@VendasUltimoMes", produto.VendasUltimoMes);
            command.Parameters.AddWithValue("@VendasUltimoTrimestre", produto.VendasUltimoTrimestre);
            command.Parameters.AddWithValue("@Id", produto.Id.ToString());

            command.ExecuteNonQuery();
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Estoque",
                "ProdutoAtualizado",
                "Produto",
                produto.Id.ToString(),
                $"Codigo={produto.Codigo}; Nome={produto.Nome}; Estoque={produto.QuantidadeEstoque}; PrecoVenda={produto.PrecoVenda:C}");
        }

        public void ExcluirProduto(Guid id)
        {
            if (UsarProdutoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Produtos.Excluir(id);
                return;
            }

            var produto = ObterProdutoPorId(id);

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM Produtos
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            command.ExecuteNonQuery();
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Estoque",
                "ProdutoExcluido",
                "Produto",
                id.ToString(),
                produto == null
                    ? "Produto nao localizado antes da exclusao."
                    : $"Codigo={produto.Codigo}; Nome={produto.Nome}; Estoque={produto.QuantidadeEstoque}; PrecoVenda={produto.PrecoVenda:C}");
        }

        public void InserirProdutosEmMassa(string caminhoArquivo)
        {
            if (UsarProdutoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Produtos.InserirEmMassa(caminhoArquivo);
                return;
            }

            if (!File.Exists(caminhoArquivo))
                throw new FileNotFoundException($"Arquivo não encontrado: {caminhoArquivo}");

            var linhas = File.ReadAllLines(caminhoArquivo);
            int produtosInseridos = 0;

            foreach (var linha in linhas)
            {
                // Ignorar linhas de comentário ou vazias
                if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith("#"))
                    continue;

                var partes = linha.Split(';');

                if (partes.Length < 13)
                    continue;

                try
                {
                    var produto = new Produto
                    {
                        Id = Guid.NewGuid(),
                        Codigo = partes[0], // Usar o nome como código temporariamente
                        Nome = partes[0], // A primeira coluna é o NOME
                        Descricao = partes.Length > 12 ? partes[12] : "",
                        Categoria = partes[1],
                        Marca = partes[2],
                        Modelo = partes[3],
                        QuantidadeEstoque = int.TryParse(partes[6], out int quantidade) ? quantidade : 0,
                        QuantidadeMinima = int.TryParse(partes[7], out int minima) ? minima : 0,
                        QuantidadeMaxima = 0,
                        Localizacao = partes[8],
                        Prateleira = partes[9],
                        Gaveta = partes[10],
                        PrecoCompra = decimal.TryParse(partes[4], out decimal precoCompra) ? precoCompra : 0,
                        PrecoVenda = decimal.TryParse(partes[5], out decimal precoVenda) ? precoVenda : 0,
                        MargemLucro = 0,
                        ValorTotalEstoque = 0,
                        Fornecedor = partes[11],
                        TelefoneFornecedor = "",
                        Ativo = true,
                        ProdutoPerecivel = false,
                        DataCadastro = DateTime.Now,
                        Observacoes = partes.Length > 12 ? partes[12] : "",
                        TotalVendas = 0,
                        TotalFaturado = 0,
                        VendasUltimoMes = 0,
                        VendasUltimoTrimestre = 0
                    };

                    // Calcular valor total do estoque
                    produto.ValorTotalEstoque = produto.QuantidadeEstoque * produto.PrecoCompra;

                    // Calcular margem de lucro
                    if (produto.PrecoVenda > 0)
                    {
                        produto.MargemLucro = ((produto.PrecoVenda - produto.PrecoCompra) / produto.PrecoVenda) * 100;
                    }

                    InserirProduto(produto);
                    produtosInseridos++;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Erro ao inserir produto importado da linha '{linha}'.", ex);
                }
            }

            Logger.LogInfo($"Importacao em massa de produtos concluida com {produtosInseridos} item(ns) inserido(s).");
        }
    }
}

