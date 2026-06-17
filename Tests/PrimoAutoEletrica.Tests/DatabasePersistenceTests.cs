using System;
using System.IO;
using System.Data.SQLite;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class DatabasePersistenceTests
    {
        private readonly string _testDatabasePath;

        public DatabasePersistenceTests()
        {
            var tempPath = Path.GetTempPath();
            _testDatabasePath = Path.Combine(tempPath, $"test_database_{Guid.NewGuid()}.db");
        }

        private void Dispose()
        {
            if (File.Exists(_testDatabasePath))
            {
                try
                {
                    File.Delete(_testDatabasePath);
                }
                catch
                {
                    // Ignora erro ao deletar arquivo
                }
            }
        }

        [Fact]
        public void Database_DeveCriarArquivoSQLite()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                Assert.True(File.Exists(_testDatabasePath), "Arquivo de banco de dados não foi criado");
            }
        }

        [Fact]
        public void Database_DeveConectarComSucesso()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                Assert.Equal(System.Data.ConnectionState.Open, connection.State);
            }
        }

        [Fact]
        public void Database_DeveCriarTabelaClientes()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        Telefone TEXT,
                        WhatsApp TEXT,
                        Email TEXT,
                        DataCadastro TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
                
                // Verifica se a tabela foi criada
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Clientes'";
                var result = command.ExecuteScalar();
                Assert.Equal("Clientes", result);
            }
        }

        [Fact]
        public void Database_DeveInserirCliente()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Cria tabela
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        Telefone TEXT,
                        WhatsApp TEXT,
                        Email TEXT,
                        DataCadastro TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
                
                // Insere cliente
                command.CommandText = @"
                    INSERT INTO Clientes (Id, Nome, Telefone, WhatsApp, Email, DataCadastro)
                    VALUES (@Id, @Nome, @Telefone, @WhatsApp, @Email, @DataCadastro)";
                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@Nome", "Cliente Teste");
                command.Parameters.AddWithValue("@Telefone", "11999999999");
                command.Parameters.AddWithValue("@WhatsApp", "11999999999");
                command.Parameters.AddWithValue("@Email", "teste@example.com");
                command.Parameters.AddWithValue("@DataCadastro", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
                
                // Verifica se o cliente foi inserido
                command.CommandText = "SELECT COUNT(*) FROM Clientes";
                var count = Convert.ToInt32(command.ExecuteScalar());
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void Database_DeveSelecionarCliente()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            var clienteId = Guid.NewGuid().ToString();
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Cria tabela
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        Telefone TEXT,
                        WhatsApp TEXT,
                        Email TEXT,
                        DataCadastro TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
                
                // Insere cliente
                command.CommandText = @"
                    INSERT INTO Clientes (Id, Nome, Telefone, WhatsApp, Email, DataCadastro)
                    VALUES (@Id, @Nome, @Telefone, @WhatsApp, @Email, @DataCadastro)";
                command.Parameters.AddWithValue("@Id", clienteId);
                command.Parameters.AddWithValue("@Nome", "Cliente Teste");
                command.Parameters.AddWithValue("@Telefone", "11999999999");
                command.Parameters.AddWithValue("@WhatsApp", "11999999999");
                command.Parameters.AddWithValue("@Email", "teste@example.com");
                command.Parameters.AddWithValue("@DataCadastro", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
                
                // Seleciona cliente
                command.CommandText = "SELECT Nome FROM Clientes WHERE Id = @Id";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@Id", clienteId);
                var nome = command.ExecuteScalar() as string;
                
                Assert.Equal("Cliente Teste", nome);
            }
        }

        [Fact]
        public void Database_DeveAtualizarCliente()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            var clienteId = Guid.NewGuid().ToString();
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Cria tabela
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        Telefone TEXT,
                        WhatsApp TEXT,
                        Email TEXT,
                        DataCadastro TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
                
                // Insere cliente
                command.CommandText = @"
                    INSERT INTO Clientes (Id, Nome, Telefone, WhatsApp, Email, DataCadastro)
                    VALUES (@Id, @Nome, @Telefone, @WhatsApp, @Email, @DataCadastro)";
                command.Parameters.AddWithValue("@Id", clienteId);
                command.Parameters.AddWithValue("@Nome", "Cliente Teste");
                command.Parameters.AddWithValue("@Telefone", "11999999999");
                command.Parameters.AddWithValue("@WhatsApp", "11999999999");
                command.Parameters.AddWithValue("@Email", "teste@example.com");
                command.Parameters.AddWithValue("@DataCadastro", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
                
                // Atualiza cliente
                command.CommandText = @"
                    UPDATE Clientes SET Nome = @Nome WHERE Id = @Id";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@Nome", "Cliente Atualizado");
                command.Parameters.AddWithValue("@Id", clienteId);
                command.ExecuteNonQuery();
                
                // Verifica atualização
                command.CommandText = "SELECT Nome FROM Clientes WHERE Id = @Id";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@Id", clienteId);
                var nome = command.ExecuteScalar() as string;
                
                Assert.Equal("Cliente Atualizado", nome);
            }
        }

        [Fact]
        public void Database_DeveDeletarCliente()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            var clienteId = Guid.NewGuid().ToString();
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Cria tabela
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        Telefone TEXT,
                        WhatsApp TEXT,
                        Email TEXT,
                        DataCadastro TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
                
                // Insere cliente
                command.CommandText = @"
                    INSERT INTO Clientes (Id, Nome, Telefone, WhatsApp, Email, DataCadastro)
                    VALUES (@Id, @Nome, @Telefone, @WhatsApp, @Email, @DataCadastro)";
                command.Parameters.AddWithValue("@Id", clienteId);
                command.Parameters.AddWithValue("@Nome", "Cliente Teste");
                command.Parameters.AddWithValue("@Telefone", "11999999999");
                command.Parameters.AddWithValue("@WhatsApp", "11999999999");
                command.Parameters.AddWithValue("@Email", "teste@example.com");
                command.Parameters.AddWithValue("@DataCadastro", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
                
                // Deleta cliente
                command.CommandText = "DELETE FROM Clientes WHERE Id = @Id";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@Id", clienteId);
                command.ExecuteNonQuery();
                
                // Verifica deleção
                command.CommandText = "SELECT COUNT(*) FROM Clientes WHERE Id = @Id";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@Id", clienteId);
                var count = Convert.ToInt32(command.ExecuteScalar());
                
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public void Database_TransactionCommit_DevePersistirDados()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Cria tabela
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        Telefone TEXT,
                        WhatsApp TEXT,
                        Email TEXT,
                        DataCadastro TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
                
                // Inicia transação
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"
                            INSERT INTO Clientes (Id, Nome, Telefone, WhatsApp, Email, DataCadastro)
                            VALUES (@Id, @Nome, @Telefone, @WhatsApp, @Email, @DataCadastro)";
                        command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                        command.Parameters.AddWithValue("@Nome", "Cliente Transaction");
                        command.Parameters.AddWithValue("@Telefone", "11999999999");
                        command.Parameters.AddWithValue("@WhatsApp", "11999999999");
                        command.Parameters.AddWithValue("@Email", "teste@example.com");
                        command.Parameters.AddWithValue("@DataCadastro", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.ExecuteNonQuery();
                        
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                
                // Verifica se os dados foram persistidos
                command.CommandText = "SELECT COUNT(*) FROM Clientes";
                command.Transaction = null;
                var count = Convert.ToInt32(command.ExecuteScalar());
                
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void Database_TransactionRollback_DeveReverterDados()
        {
            var connectionString = $"Data Source={_testDatabasePath};Version=3;";
            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                // Cria tabela
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id TEXT PRIMARY KEY,
                        Nome TEXT NOT NULL,
                        Telefone TEXT,
                        WhatsApp TEXT,
                        Email TEXT,
                        DataCadastro TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
                
                // Inicia transação
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"
                            INSERT INTO Clientes (Id, Nome, Telefone, WhatsApp, Email, DataCadastro)
                            VALUES (@Id, @Nome, @Telefone, @WhatsApp, @Email, @DataCadastro)";
                        command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                        command.Parameters.AddWithValue("@Nome", "Cliente Transaction Rollback");
                        command.Parameters.AddWithValue("@Telefone", "11999999999");
                        command.Parameters.AddWithValue("@WhatsApp", "11999999999");
                        command.Parameters.AddWithValue("@Email", "teste@example.com");
                        command.Parameters.AddWithValue("@DataCadastro", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.ExecuteNonQuery();
                        
                        // Força rollback
                        transaction.Rollback();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                
                // Verifica se os dados não foram persistidos
                command.CommandText = "SELECT COUNT(*) FROM Clientes";
                command.Transaction = null;
                var count = Convert.ToInt32(command.ExecuteScalar());
                
                Assert.Equal(0, count);
            }
        }
    }
}
