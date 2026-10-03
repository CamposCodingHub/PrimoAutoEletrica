using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Valida persistência de banco de dados em ambiente temporário isolado.
    /// Testa criação de tabelas, CRUD básico, chaves estrangeiras, dados inválidos e valores nulos.
    /// </summary>
    public sealed class DatabasePersistenceTestService
    {
        private readonly LoggerService _logger;
        private readonly string _projectRoot;

        public DatabasePersistenceTestService(LoggerService logger, string projectRoot)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _projectRoot = projectRoot ?? throw new ArgumentNullException(nameof(projectRoot));
        }

        private static long ExecuteScalarLong(SqliteCommand command, string operation)
        {
            var value = command.ExecuteScalar();
            if (value is null || value == DBNull.Value)
            {
                throw new InvalidOperationException($"Falha ao {operation}: o banco nao retornou um identificador.");
            }

            return Convert.ToInt64(value);
        }

        public DatabasePersistenceTestResult Run()
        {
            var result = new DatabasePersistenceTestResult();
            _logger.LogInfo("Iniciando teste de persistência de banco de dados.");

            string tempDatabasePath = string.Empty;

            try
            {
                // 1. Criar banco limpo em ambiente temporário
                tempDatabasePath = CreateTempDatabase(result);

                // 2. Criar tabelas
                CreateTables(result, tempDatabasePath);

                // 3. Inserir cliente
                var clienteId = InsertCliente(result, tempDatabasePath);

                // 4. Editar cliente
                EditCliente(result, tempDatabasePath, clienteId);

                // 5. Excluir/desativar cliente
                DeleteCliente(result, tempDatabasePath, clienteId);

                // 6. Inserir veículo
                var veiculoId = InsertVeiculo(result, tempDatabasePath);

                // 7. Vincular veículo ao cliente
                LinkVeiculoToCliente(result, tempDatabasePath, veiculoId, clienteId);

                // 8. Inserir produto
                var produtoId = InsertProduto(result, tempDatabasePath);

                // 9. Movimentar estoque
                MovimentarEstoque(result, tempDatabasePath, produtoId);

                // 10. Criar fornecedor
                var fornecedorId = InsertFornecedor(result, tempDatabasePath);

                // 11. Inserir Ferramenta v2.0
                var ferramentaGuid = InsertFerramenta(result, tempDatabasePath);

                // 12. Movimentar Ferramenta v2.0 (retirada/devolução)
                MovimentarFerramenta(result, tempDatabasePath, ferramentaGuid);

                // 13. Inserir Pedido de Compra Anti-Ruptura v2.0
                var pedidoCompraGuid = InsertPedidoCompra(result, tempDatabasePath, fornecedorId, produtoId);

                // 14. Testar Concorrência Pessimista (RegistroBloqueios)
                TestRegistroBloqueio(result, tempDatabasePath);

                // 15. Criar orçamento
                var orcamentoId = InsertOrcamento(result, tempDatabasePath);

                // 16. Criar OS
                var osId = InsertOrdemServico(result, tempDatabasePath);

                // 17. Criar venda
                var vendaId = InsertVenda(result, tempDatabasePath);

                // 18. Criar lançamento financeiro
                var lancamentoId = InsertLancamentoFinanceiro(result, tempDatabasePath);

                // 19. Fechar conexão
                CloseConnection(result);

                // 20. Abrir conexão novamente
                ReopenConnection(result, tempDatabasePath);

                // 21. Conferir se tudo persistiu (inclusive novas tabelas v2.0)
                VerifyPersistence(result, tempDatabasePath);

                // 22. Testar chaves estrangeiras
                TestForeignKeys(result, tempDatabasePath);

                // 23. Testar dados inválidos
                TestInvalidData(result, tempDatabasePath);

                // 24. Testar valores nulos
                TestNullValues(result, tempDatabasePath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro durante teste de persistência: {ex.Message}");
                result.AddError("TestException", ex.Message);
            }
            finally
            {
                // Limpar banco temporário
                if (!string.IsNullOrEmpty(tempDatabasePath) && File.Exists(tempDatabasePath))
                {
                    try
                    {
                        File.Delete(tempDatabasePath);
                        _logger.LogInfo($"Banco temporário removido: {tempDatabasePath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Não foi possível remover banco temporário: {ex.Message}");
                    }
                }
            }

            result.ReportPath = PersistReport(result);
            return result;
        }

        private string CreateTempDatabase(DatabasePersistenceTestResult result)
        {
            _logger.LogInfo("Criando banco temporário...");

            var tempDir = Path.Combine(_projectRoot, "TestResults", "DatabaseTests");
            Directory.CreateDirectory(tempDir);

            var tempDatabasePath = Path.Combine(tempDir, $"TestDatabase_{Guid.NewGuid()}.db");

            using (var connection = new SqliteConnection($"Data Source={tempDatabasePath}"))
            {
                connection.Open();
                _logger.LogInfo($"Banco temporário criado: {tempDatabasePath}");
            }

            result.AddInfo("TempDatabaseCreated", tempDatabasePath);
            return tempDatabasePath;
        }

        private void CreateTables(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Criando tabelas...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                // Criar tabela Clientes
                var createClientes = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nome TEXT NOT NULL,
                        Email TEXT,
                        Telefone TEXT,
                        Ativo INTEGER DEFAULT 1,
                        DataCriacao TEXT
                    )";
                using (var command = new SqliteCommand(createClientes, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Criar tabela Veiculos
                var createVeiculos = @"
                    CREATE TABLE IF NOT EXISTS Veiculos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Placa TEXT NOT NULL,
                        Marca TEXT,
                        Modelo TEXT,
                        ClienteId INTEGER,
                        FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
                    )";
                using (var command = new SqliteCommand(createVeiculos, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Criar tabela Produtos
                var createProdutos = @"
                    CREATE TABLE IF NOT EXISTS Produtos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nome TEXT NOT NULL,
                        PrecoVenda REAL,
                        Estoque INTEGER DEFAULT 0
                    )";
                using (var command = new SqliteCommand(createProdutos, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Criar tabela Fornecedores
                var createFornecedores = @"
                    CREATE TABLE IF NOT EXISTS Fornecedores (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nome TEXT NOT NULL,
                        CNPJ TEXT
                    )";
                using (var command = new SqliteCommand(createFornecedores, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Criar tabela Ferramentas v2.0
                var createFerramentas = @"
                    CREATE TABLE IF NOT EXISTS Ferramentas (
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
                    )";
                using (var command = new SqliteCommand(createFerramentas, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Criar tabela MovimentacoesFerramentas v2.0
                var createMovimentacoes = @"
                    CREATE TABLE IF NOT EXISTS MovimentacoesFerramentas (
                        Id TEXT PRIMARY KEY,
                        FerramentaId TEXT NOT NULL,
                        CodigoPatrimonio TEXT,
                        FerramentaNome TEXT,
                        FuncionarioId TEXT NOT NULL,
                        FuncionarioNome TEXT NOT NULL,
                        OrdemServicoId TEXT,
                        NumeroOS TEXT,
                        DataRetirada TEXT NOT NULL,
                        PrevisaoDevolucao TEXT,
                        DataDevolucao TEXT,
                        EstadoConservacaoRetirada TEXT DEFAULT 'OK',
                        EstadoConservacaoDevolucao TEXT,
                        ObservacaoDevolucao TEXT,
                        RegistradoPor TEXT DEFAULT 'Sistema'
                    )";
                using (var command = new SqliteCommand(createMovimentacoes, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Criar tabela PedidosCompra v2.0
                var createPedidos = @"
                    CREATE TABLE IF NOT EXISTS PedidosCompra (
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
                    )";
                using (var command = new SqliteCommand(createPedidos, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Criar tabela PedidosCompraItens v2.0
                var createPedidosItens = @"
                    CREATE TABLE IF NOT EXISTS PedidosCompraItens (
                        Id TEXT PRIMARY KEY,
                        PedidoCompraId TEXT NOT NULL,
                        ProdutoId TEXT NOT NULL,
                        Codigo TEXT NOT NULL,
                        Descricao TEXT NOT NULL,
                        QuantidadePedida INTEGER NOT NULL,
                        QuantidadeRecebida INTEGER NOT NULL DEFAULT 0,
                        ValorUnitario REAL NOT NULL DEFAULT 0,
                        FOREIGN KEY (PedidoCompraId) REFERENCES PedidosCompra(Id) ON DELETE CASCADE
                    )";
                using (var command = new SqliteCommand(createPedidosItens, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Criar tabela RegistroBloqueios (Concorrência Pessimista)
                var createBloqueios = @"
                    CREATE TABLE IF NOT EXISTS RegistroBloqueios (
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
                    )";
                using (var command = new SqliteCommand(createBloqueios, connection))
                {
                    command.ExecuteNonQuery();
                }

                _logger.LogInfo("Tabelas criadas com sucesso (incluindo Ferramentaria, Compras e Bloqueios)");
                result.AddInfo("TablesCreated", "Clientes, Veiculos, Produtos, Fornecedores, Ferramentas, MovimentacoesFerramentas, PedidosCompra, PedidosCompraItens, RegistroBloqueios");
            }
        }

        private long InsertCliente(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo cliente...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var insertSql = @"
                    INSERT INTO Clientes (Nome, Email, Telefone, Ativo, DataCriacao)
                    VALUES (@Nome, @Email, @Telefone, @Ativo, @DataCriacao);
                    SELECT last_insert_rowid();";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@Nome", "Cliente Teste");
                    command.Parameters.AddWithValue("@Email", "cliente@teste.com");
                    command.Parameters.AddWithValue("@Telefone", "11999999999");
                    command.Parameters.AddWithValue("@Ativo", 1);
                    command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var id = ExecuteScalarLong(command, "inserir cliente");
                    _logger.LogInfo($"Cliente inserido com ID: {id}");
                    result.AddInfo("ClienteInserted", id.ToString());
                    return id;
                }
            }
        }

        private void EditCliente(DatabasePersistenceTestResult result, string databasePath, long clienteId)
        {
            _logger.LogInfo("Editando cliente...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var updateSql = "UPDATE Clientes SET Nome = @Nome WHERE Id = @Id";

                using (var command = new SqliteCommand(updateSql, connection))
                {
                    command.Parameters.AddWithValue("@Nome", "Cliente Teste Editado");
                    command.Parameters.AddWithValue("@Id", clienteId);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        result.AddError("ClienteEditFailed", "Nenhuma linha foi atualizada");
                    }
                    else
                    {
                        _logger.LogInfo("Cliente editado com sucesso");
                        result.AddInfo("ClienteEdited", "Sucesso");
                    }
                }
            }
        }

        private void DeleteCliente(DatabasePersistenceTestResult result, string databasePath, long clienteId)
        {
            _logger.LogInfo("Excluindo cliente...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var deleteSql = "UPDATE Clientes SET Ativo = 0 WHERE Id = @Id";

                using (var command = new SqliteCommand(deleteSql, connection))
                {
                    command.Parameters.AddWithValue("@Id", clienteId);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        result.AddError("ClienteDeleteFailed", "Nenhuma linha foi desativada");
                    }
                    else
                    {
                        _logger.LogInfo("Cliente desativado com sucesso");
                        result.AddInfo("ClienteDeleted", "Sucesso");
                    }
                }
            }
        }

        private long InsertVeiculo(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo veículo...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var insertSql = @"
                    INSERT INTO Veiculos (Placa, Marca, Modelo, ClienteId)
                    VALUES (@Placa, @Marca, @Modelo, @ClienteId);
                    SELECT last_insert_rowid();";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@Placa", "ABC1234");
                    command.Parameters.AddWithValue("@Marca", "Teste");
                    command.Parameters.AddWithValue("@Modelo", "Modelo Teste");
                    command.Parameters.AddWithValue("@ClienteId", DBNull.Value);

                    var id = ExecuteScalarLong(command, "inserir veiculo");
                    _logger.LogInfo($"Veículo inserido com ID: {id}");
                    result.AddInfo("VeiculoInserted", id.ToString());
                    return id;
                }
            }
        }

        private void LinkVeiculoToCliente(DatabasePersistenceTestResult result, string databasePath, long veiculoId, long clienteId)
        {
            _logger.LogInfo("Vinculando veículo ao cliente...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var updateSql = "UPDATE Veiculos SET ClienteId = @ClienteId WHERE Id = @Id";

                using (var command = new SqliteCommand(updateSql, connection))
                {
                    command.Parameters.AddWithValue("@ClienteId", clienteId);
                    command.Parameters.AddWithValue("@Id", veiculoId);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        result.AddError("VeiculoLinkFailed", "Nenhuma linha foi atualizada");
                    }
                    else
                    {
                        _logger.LogInfo("Veículo vinculado ao cliente com sucesso");
                        result.AddInfo("VeiculoLinked", "Sucesso");
                    }
                }
            }
        }

        private long InsertProduto(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo produto...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var insertSql = @"
                    INSERT INTO Produtos (Nome, PrecoVenda, Estoque)
                    VALUES (@Nome, @PrecoVenda, @Estoque);
                    SELECT last_insert_rowid();";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@Nome", "Produto Teste");
                    command.Parameters.AddWithValue("@PrecoVenda", 100.00);
                    command.Parameters.AddWithValue("@Estoque", 10);

                    var id = ExecuteScalarLong(command, "inserir produto");
                    _logger.LogInfo($"Produto inserido com ID: {id}");
                    result.AddInfo("ProdutoInserted", id.ToString());
                    return id;
                }
            }
        }

        private void MovimentarEstoque(DatabasePersistenceTestResult result, string databasePath, long produtoId)
        {
            _logger.LogInfo("Movimentando estoque...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var updateSql = "UPDATE Produtos SET Estoque = Estoque - 1 WHERE Id = @Id";

                using (var command = new SqliteCommand(updateSql, connection))
                {
                    command.Parameters.AddWithValue("@Id", produtoId);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        result.AddError("EstoqueMovementFailed", "Nenhuma linha foi atualizada");
                    }
                    else
                    {
                        _logger.LogInfo("Estoque movimentado com sucesso");
                        result.AddInfo("EstoqueMoved", "Sucesso");
                    }
                }
            }
        }

        private long InsertFornecedor(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo fornecedor...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var insertSql = @"
                    INSERT INTO Fornecedores (Nome, CNPJ)
                    VALUES (@Nome, @CNPJ);
                    SELECT last_insert_rowid();";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@Nome", "Fornecedor Teste");
                    command.Parameters.AddWithValue("@CNPJ", "12345678000100");

                    var id = ExecuteScalarLong(command, "inserir fornecedor");
                    _logger.LogInfo($"Fornecedor inserido com ID: {id}");
                    result.AddInfo("FornecedorInserted", id.ToString());
                    return id;
                }
            }
        }

        private Guid InsertFerramenta(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo ferramenta de teste v2.0...");
            var ferramentaId = Guid.NewGuid();

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var insertSql = @"
                    INSERT INTO Ferramentas (
                        Id, CodigoPatrimonio, Nome, Categoria, MarcaModelo, NumeroSerie,
                        LocalizacaoArmario, Status, ValorAquisicao, DataAquisicao, Ativo
                    )
                    VALUES (
                        @Id, @CodigoPatrimonio, @Nome, @Categoria, @MarcaModelo, @NumeroSerie,
                        @LocalizacaoArmario, @Status, @ValorAquisicao, @DataAquisicao, 1
                    );";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@Id", ferramentaId.ToString());
                    command.Parameters.AddWithValue("@CodigoPatrimonio", "FER-TEST-99");
                    command.Parameters.AddWithValue("@Nome", "Scanner Automotivo Diagnóstico");
                    command.Parameters.AddWithValue("@Categoria", 1);
                    command.Parameters.AddWithValue("@MarcaModelo", "Bosch KTS 590");
                    command.Parameters.AddWithValue("@NumeroSerie", "SN-8823910");
                    command.Parameters.AddWithValue("@LocalizacaoArmario", "Armário Especializado A");
                    command.Parameters.AddWithValue("@Status", 1); // Disponivel
                    command.Parameters.AddWithValue("@ValorAquisicao", 4500.00);
                    command.Parameters.AddWithValue("@DataAquisicao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    command.ExecuteNonQuery();
                    _logger.LogInfo($"Ferramenta inserida com ID: {ferramentaId}");
                    result.AddInfo("FerramentaInserted", ferramentaId.ToString());
                    return ferramentaId;
                }
            }
        }

        private void MovimentarFerramenta(DatabasePersistenceTestResult result, string databasePath, Guid ferramentaId)
        {
            _logger.LogInfo("Movimentando ferramenta (empréstimo de teste)...");
            var movId = Guid.NewGuid();
            var funcId = Guid.NewGuid();

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var movSql = @"
                        INSERT INTO MovimentacoesFerramentas (
                            Id, FerramentaId, CodigoPatrimonio, FerramentaNome, FuncionarioId,
                            FuncionarioNome, DataRetirada, PrevisaoDevolucao, EstadoConservacaoRetirada, RegistradoPor
                        )
                        VALUES (
                            @Id, @FerramentaId, @CodigoPatrimonio, @FerramentaNome, @FuncionarioId,
                            @FuncionarioNome, @DataRetirada, @PrevisaoDevolucao, @EstadoRetirada, @RegistradoPor
                        );";

                    using (var cmd = new SqliteCommand(movSql, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Id", movId.ToString());
                        cmd.Parameters.AddWithValue("@FerramentaId", ferramentaId.ToString());
                        cmd.Parameters.AddWithValue("@CodigoPatrimonio", "FER-TEST-99");
                        cmd.Parameters.AddWithValue("@FerramentaNome", "Scanner Automotivo Diagnóstico");
                        cmd.Parameters.AddWithValue("@FuncionarioId", funcId.ToString());
                        cmd.Parameters.AddWithValue("@FuncionarioNome", "Técnico Especialista");
                        cmd.Parameters.AddWithValue("@DataRetirada", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@PrevisaoDevolucao", DateTime.Now.AddHours(4).ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@EstadoRetirada", "OK - Calibrado");
                        cmd.Parameters.AddWithValue("@RegistradoPor", "Sistema Teste");
                        cmd.ExecuteNonQuery();
                    }

                    var updateSql = @"
                        UPDATE Ferramentas 
                        SET Status = 2, FuncionarioPosseAtualId = @FuncId, FuncionarioPosseAtualNome = @FuncNome
                        WHERE Id = @Id;";

                    using (var updateCmd = new SqliteCommand(updateSql, connection, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@FuncId", funcId.ToString());
                        updateCmd.Parameters.AddWithValue("@FuncNome", "Técnico Especialista");
                        updateCmd.Parameters.AddWithValue("@Id", ferramentaId.ToString());
                        updateCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _logger.LogInfo("Movimentação de ferramenta persistida com sucesso.");
                    result.AddInfo("FerramentaMovimentada", "Sucesso");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    result.AddError("FerramentaMovimentacaoFailed", ex.Message);
                }
            }
        }

        private Guid InsertPedidoCompra(DatabasePersistenceTestResult result, string databasePath, long fornecedorId, long produtoId)
        {
            _logger.LogInfo("Inserindo Pedido de Compra e itens v2.0...");
            var pedidoId = Guid.NewGuid();
            var itemId = Guid.NewGuid();

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var pedSql = @"
                        INSERT INTO PedidosCompra (
                            Id, Numero, FornecedorId, FornecedorNome, Status, ValorTotal, DataCriacao, CriadoPor
                        )
                        VALUES (
                            @Id, @Numero, @FornecedorId, @FornecedorNome, @Status, @ValorTotal, @DataCriacao, @CriadoPor
                        );";

                    using (var cmd = new SqliteCommand(pedSql, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Id", pedidoId.ToString());
                        cmd.Parameters.AddWithValue("@Numero", "PED-COMPRA-TEST-001");
                        cmd.Parameters.AddWithValue("@FornecedorId", fornecedorId.ToString());
                        cmd.Parameters.AddWithValue("@FornecedorNome", "Distribuidora Auto Peças");
                        cmd.Parameters.AddWithValue("@Status", 1); // Cotacao/Aberto
                        cmd.Parameters.AddWithValue("@ValorTotal", 500.00);
                        cmd.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@CriadoPor", "Comprador Automático");
                        cmd.ExecuteNonQuery();
                    }

                    var itemSql = @"
                        INSERT INTO PedidosCompraItens (
                            Id, PedidoCompraId, ProdutoId, Codigo, Descricao, QuantidadePedida, ValorUnitario
                        )
                        VALUES (
                            @Id, @PedidoCompraId, @ProdutoId, @Codigo, @Descricao, @QuantidadePedida, @ValorUnitario
                        );";

                    using (var itemCmd = new SqliteCommand(itemSql, connection, transaction))
                    {
                        itemCmd.Parameters.AddWithValue("@Id", itemId.ToString());
                        itemCmd.Parameters.AddWithValue("@PedidoCompraId", pedidoId.ToString());
                        itemCmd.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());
                        itemCmd.Parameters.AddWithValue("@Codigo", "PROD-TEST");
                        itemCmd.Parameters.AddWithValue("@Descricao", "Peça de Reposição");
                        itemCmd.Parameters.AddWithValue("@QuantidadePedida", 5);
                        itemCmd.Parameters.AddWithValue("@ValorUnitario", 100.00);
                        itemCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    _logger.LogInfo($"Pedido de compra '{pedidoId}' e item '{itemId}' inseridos com sucesso.");
                    result.AddInfo("PedidoCompraInserted", pedidoId.ToString());
                    return pedidoId;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    result.AddError("PedidoCompraInsertFailed", ex.Message);
                    return Guid.Empty;
                }
            }
        }

        private void TestRegistroBloqueio(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Testando tabela RegistroBloqueios...");
            var lockId = Guid.NewGuid();

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                var lockSql = @"
                    INSERT INTO RegistroBloqueios (
                        Id, Entidade, EntidadeId, UsuarioId, UsuarioNome, SessaoId,
                        Maquina, CriadoEm, ExpiraEm, Motivo, Ativo
                    )
                    VALUES (
                        @Id, @Entidade, @EntidadeId, @UsuarioId, @UsuarioNome, @SessaoId,
                        @Maquina, @CriadoEm, @ExpiraEm, @Motivo, 1
                    );";

                using (var cmd = new SqliteCommand(lockSql, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", lockId.ToString());
                    cmd.Parameters.AddWithValue("@Entidade", "OrdemServico");
                    cmd.Parameters.AddWithValue("@EntidadeId", "OS-2026-9999");
                    cmd.Parameters.AddWithValue("@UsuarioId", 1);
                    cmd.Parameters.AddWithValue("@UsuarioNome", "Operador Principal");
                    cmd.Parameters.AddWithValue("@SessaoId", Guid.NewGuid().ToString());
                    cmd.Parameters.AddWithValue("@Maquina", "TERMINAL-01");
                    cmd.Parameters.AddWithValue("@CriadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@ExpiraEm", DateTime.Now.AddMinutes(15).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Motivo", "Edição de itens da OS");
                    cmd.ExecuteNonQuery();

                    _logger.LogInfo("RegistroBloqueio inserido com sucesso.");
                    result.AddInfo("RegistroBloqueioTested", "Sucesso");
                }
            }
        }

        private long InsertOrcamento(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo orçamento...");

            // Nota: Tabela Orcamentos não foi criada, então esta é uma simulação
            // Em produção, seria necessário criar a tabela e inserir o orçamento
            _logger.LogInfo("Orçamento simulado (tabela não criada)");
            result.AddInfo("OrcamentoInserted", "Simulado");
            return 1;
        }

        private long InsertOrdemServico(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo ordem de serviço...");

            // Nota: Tabela OrdensServico não foi criada, então esta é uma simulação
            // Em produção, seria necessário criar a tabela e inserir a OS
            _logger.LogInfo("Ordem de serviço simulada (tabela não criada)");
            result.AddInfo("OrdemServicoInserted", "Simulado");
            return 1;
        }

        private long InsertVenda(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo venda...");

            // Nota: Tabela Vendas não foi criada, então esta é uma simulação
            // Em produção, seria necessário criar a tabela e inserir a venda
            _logger.LogInfo("Venda simulada (tabela não criada)");
            result.AddInfo("VendaInserted", "Simulado");
            return 1;
        }

        private long InsertLancamentoFinanceiro(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Inserindo lançamento financeiro...");

            // Nota: Tabela LancamentosFinanceiros não foi criada, então esta é uma simulação
            // Em produção, seria necessário criar a tabela e inserir o lançamento
            _logger.LogInfo("Lançamento financeiro simulado (tabela não criada)");
            result.AddInfo("LancamentoFinanceiroInserted", "Simulado");
            return 1;
        }

        private void CloseConnection(DatabasePersistenceTestResult result)
        {
            _logger.LogInfo("Fechando conexão...");
            // Conexão é fechada automaticamente pelo using
            result.AddInfo("ConnectionClosed", "Sucesso");
        }

        private void ReopenConnection(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Reabrindo conexão...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();
                _logger.LogInfo("Conexão reaberta com sucesso");
                result.AddInfo("ConnectionReopened", "Sucesso");
            }
        }

        private void VerifyPersistence(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Verificando persistência dos dados...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                // Verificar se cliente persistiu
                var countSql = "SELECT COUNT(*) FROM Clientes";
                using (var command = new SqliteCommand(countSql, connection))
                {
                    var count = Convert.ToInt32(command.ExecuteScalar() ?? 0);
                    if (count > 0)
                    {
                        _logger.LogInfo($"Persistência verificada: {count} clientes encontrados");
                        result.AddInfo("PersistenceVerified_Clientes", $"{count} clientes");
                    }
                    else
                    {
                        result.AddError("PersistenceFailed_Clientes", "Nenhum cliente encontrado após reabertura");
                    }
                }

                // Verificar Ferramentas persistidas
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM Ferramentas", connection))
                {
                    var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    if (count > 0)
                    {
                        _logger.LogInfo($"Persistência verificada: {count} ferramentas encontradas");
                        result.AddInfo("PersistenceVerified_Ferramentas", $"{count} ferramentas");
                    }
                    else
                    {
                        result.AddError("PersistenceFailed_Ferramentas", "Nenhuma ferramenta encontrada após reabertura");
                    }
                }

                // Verificar Movimentações de Ferramentas
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM MovimentacoesFerramentas", connection))
                {
                    var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    if (count > 0)
                    {
                        _logger.LogInfo($"Persistência verificada: {count} movimentações de ferramentas encontradas");
                        result.AddInfo("PersistenceVerified_MovimentacoesFerramentas", $"{count} movimentações");
                    }
                    else
                    {
                        result.AddError("PersistenceFailed_MovimentacoesFerramentas", "Nenhuma movimentação de ferramenta encontrada após reabertura");
                    }
                }

                // Verificar Pedidos de Compra
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM PedidosCompra", connection))
                {
                    var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    if (count > 0)
                    {
                        _logger.LogInfo($"Persistência verificada: {count} pedidos de compra encontrados");
                        result.AddInfo("PersistenceVerified_PedidosCompra", $"{count} pedidos");
                    }
                    else
                    {
                        result.AddError("PersistenceFailed_PedidosCompra", "Nenhum pedido de compra encontrado após reabertura");
                    }
                }

                // Verificar Itens dos Pedidos de Compra
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM PedidosCompraItens", connection))
                {
                    var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    if (count > 0)
                    {
                        _logger.LogInfo($"Persistência verificada: {count} itens de pedidos de compra encontrados");
                        result.AddInfo("PersistenceVerified_PedidosCompraItens", $"{count} itens");
                    }
                    else
                    {
                        result.AddError("PersistenceFailed_PedidosCompraItens", "Nenhum item de pedido de compra encontrado após reabertura");
                    }
                }

                // Verificar RegistroBloqueios
                using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM RegistroBloqueios", connection))
                {
                    var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    if (count > 0)
                    {
                        _logger.LogInfo($"Persistência verificada: {count} bloqueios de concorrência encontrados");
                        result.AddInfo("PersistenceVerified_RegistroBloqueios", $"{count} bloqueios");
                    }
                    else
                    {
                        result.AddError("PersistenceFailed_RegistroBloqueios", "Nenhum bloqueio encontrado após reabertura");
                    }
                }
            }
        }

        private void TestForeignKeys(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Testando chaves estrangeiras...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                // Tentar inserir veículo com ClienteId inválido
                try
                {
                    var insertSql = @"
                        INSERT INTO Veiculos (Placa, Marca, Modelo, ClienteId)
                        VALUES (@Placa, @Marca, @Modelo, @ClienteId);";

                    using (var command = new SqliteCommand(insertSql, connection))
                    {
                        command.Parameters.AddWithValue("@Placa", "XYZ9999");
                        command.Parameters.AddWithValue("@Marca", "Teste");
                        command.Parameters.AddWithValue("@Modelo", "Modelo Teste");
                        command.Parameters.AddWithValue("@ClienteId", 99999); // ID inválido

                        command.ExecuteNonQuery();
                        result.AddWarning("ForeignKeyNotEnforced", "Chave estrangeira não foi aplicada (SQLite padrão)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"Chave estrangeira aplicada: {ex.Message}");
                    result.AddInfo("ForeignKeyEnforced", "Sucesso");
                }
            }
        }

        private void TestInvalidData(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Testando dados inválidos...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                // Tentar inserir cliente sem nome (NOT NULL)
                try
                {
                    var insertSql = "INSERT INTO Clientes (Email, Telefone, Ativo, DataCriacao) VALUES (@Email, @Telefone, @Ativo, @DataCriacao)";

                    using (var command = new SqliteCommand(insertSql, connection))
                    {
                        command.Parameters.AddWithValue("@Email", "teste@teste.com");
                        command.Parameters.AddWithValue("@Telefone", "11999999999");
                        command.Parameters.AddWithValue("@Ativo", 1);
                        command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        command.ExecuteNonQuery();
                        result.AddError("InvalidDataAccepted", "Cliente sem nome foi aceito (deveria ser rejeitado)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"Dados inválidos rejeitados corretamente: {ex.Message}");
                    result.AddInfo("InvalidDataRejected", "Sucesso");
                }
            }
        }

        private void TestNullValues(DatabasePersistenceTestResult result, string databasePath)
        {
            _logger.LogInfo("Testando valores nulos...");

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                // Inserir cliente com campos nulos permitidos
                var insertSql = @"
                    INSERT INTO Clientes (Nome, Email, Telefone, Ativo, DataCriacao)
                    VALUES (@Nome, @Email, @Telefone, @Ativo, @DataCriacao);";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@Nome", "Cliente Nulo Teste");
                    command.Parameters.AddWithValue("@Email", DBNull.Value);
                    command.Parameters.AddWithValue("@Telefone", DBNull.Value);
                    command.Parameters.AddWithValue("@Ativo", 1);
                    command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    command.ExecuteNonQuery();
                    _logger.LogInfo("Cliente com valores nulos inserido com sucesso");
                    result.AddInfo("NullValuesAccepted", "Sucesso");
                }
            }
        }

        private string PersistReport(DatabasePersistenceTestResult result)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var reportDir = Path.Combine(_projectRoot, "TestResults", "DatabasePersistence");
            Directory.CreateDirectory(reportDir);

            var reportPath = Path.Combine(reportDir, $"DatabasePersistence_{timestamp}.md");
            var reportContent = GenerateReport(result);

            File.WriteAllText(reportPath, reportContent);
            _logger.LogInfo($"Relatório de persistência de banco salvo em: {reportPath}");

            return reportPath;
        }

        private string GenerateReport(DatabasePersistenceTestResult result)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Relatório de Teste de Persistência de Banco de Dados");
            sb.AppendLine();
            sb.AppendLine($"**Data/Hora:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Total de Erros:** {result.Errors.Count}");
            sb.AppendLine($"**Total de Avisos:** {result.Warnings.Count}");
            sb.AppendLine($"**Total de Informações:** {result.Infos.Count}");
            sb.AppendLine();
            sb.AppendLine("## Erros");
            sb.AppendLine();
            foreach (var error in result.Errors)
            {
                sb.AppendLine($"- **{error.Key}:** {error.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("## Avisos");
            sb.AppendLine();
            foreach (var warning in result.Warnings)
            {
                sb.AppendLine($"- **{warning.Key}:** {warning.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("## Informações");
            sb.AppendLine();
            foreach (var info in result.Infos)
            {
                sb.AppendLine($"- **{info.Key}:** {info.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("Gerado automaticamente por DatabasePersistenceTestService");

            return sb.ToString();
        }
    }

    public class DatabasePersistenceTestResult
    {
        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> Warnings { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> Infos { get; } = new Dictionary<string, string>();
        public string ReportPath { get; set; } = string.Empty;

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;

        public void AddError(string key, string message)
        {
            Errors[key] = message;
        }

        public void AddWarning(string key, string message)
        {
            Warnings[key] = message;
        }

        public void AddInfo(string key, string message)
        {
            Infos[key] = message;
        }
    }
}
