using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class DatabaseProviderRuntimeTests : IDisposable
{
    private readonly string _tempRoot;

    public DatabaseProviderRuntimeTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-provider-runtime-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [Theory]
    [InlineData("SQL Server")]
    [InlineData("SQLServer")]
    [InlineData("SqlServer")]
    public void Settings_DeveNormalizarSqlServer(string provider)
    {
        var settings = new DatabaseConnectionSettings
        {
            Provider = provider,
            SqlServerHost = "localhost",
            SqlServerDatabase = "PrimoAutoEletrica"
        };

        DatabaseConnectionSettingsService.Save(_tempRoot, settings);
        var loaded = DatabaseConnectionSettingsService.LoadOrCreateDefault(_tempRoot);

        Assert.Equal("SqlServer", loaded.Provider);
        Assert.True(loaded.IsSqlServer);
    }

    [Fact]
    public void DatabaseService_DeveInicializarSqlServerLocalDbSemFallback_QuandoDisponivel()
    {
        if (!TryStartLocalDb())
        {
            return;
        }

        var databaseName = $"PrimoAutoEletrica_Test_{Guid.NewGuid():N}";
        DropSqlServerDatabase(databaseName);

        var settings = new DatabaseConnectionSettings
        {
            Provider = "SqlServer",
            SqlServerHost = @"(localdb)\MSSQLLocalDB",
            SqlServerDatabase = databaseName,
            UseWindowsAuthentication = true,
            EncryptSqlServerConnection = false,
            TrustSqlServerCertificate = true,
            AllowUnsupportedSqlServerRuntimeFallback = false,
            CommandTimeoutSeconds = 5
        };

        try
        {
            var database = new DatabaseService(_tempRoot, settings, new LoggerService());

            Assert.Equal("SqlServer", database.RuntimeProvider);
            Assert.False(database.IsUsingUnsupportedProviderFallback);

            using var connection = database.GetConnection();
            Assert.IsType<SqlConnection>(connection);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Funcionarios WHERE Email = @Email;";
            var emailParameter = command.CreateParameter();
            emailParameter.ParameterName = "@Email";
            emailParameter.Value = "admin@primoauto.com";
            command.Parameters.Add(emailParameter);
            Assert.Equal(1, Convert.ToInt32(command.ExecuteScalar()));
        }
        finally
        {
            DropSqlServerDatabase(databaseName);
        }
    }

    private static bool TryStartLocalDb()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "sqllocaldb",
                Arguments = "start MSSQLLocalDB",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(10_000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void DropSqlServerDatabase(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_'))
        {
            throw new ArgumentException("Nome de banco invalido para teste.", nameof(databaseName));
        }

        try
        {
            var connectionString = new SqlConnectionStringBuilder
            {
                DataSource = @"(localdb)\MSSQLLocalDB",
                InitialCatalog = "master",
                IntegratedSecurity = true,
                Encrypt = false,
                TrustServerCertificate = true,
                ConnectTimeout = 5
            }.ToString();

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                IF DB_ID(N'{databaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{databaseName}];
                END;";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Cleanup best effort para nao mascarar o resultado principal.
        }
    }

    [Fact]
    public void DatabaseService_DevePermitirFallbackSqliteExplicitoParaSqlServerEmMigracao()
    {
        var settings = new DatabaseConnectionSettings
        {
            Provider = "SqlServer",
            SQLitePath = "fallback.db",
            SqlServerHost = "localhost",
            SqlServerDatabase = "PrimoAutoEletrica",
            AllowUnsupportedSqlServerRuntimeFallback = true
        };

        var database = new DatabaseService(_tempRoot, settings, new LoggerService());

        Assert.Equal("SqlServer", database.ConfiguredProvider);
        Assert.Equal("SQLite", database.RuntimeProvider);
        Assert.True(database.IsUsingUnsupportedProviderFallback);
        Assert.EndsWith("fallback.db", database.DatabasePath, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        try
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Cleanup failure should not hide the assertion result.
        }
    }
}
