using System;
using System.IO;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class MigrationSchemaTests : IDisposable
{
    private readonly string _tempRoot;

    public MigrationSchemaTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-migration-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void DatabaseService_DeveCriarSchemaVersionERegistrarMigracoes()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var appliedMigrations = database.GetAppliedMigrations();

        Assert.Contains("202605210001", appliedMigrations);
        Assert.Contains("202606040001", appliedMigrations);
        Assert.Contains("202606150001", appliedMigrations);
        Assert.Contains("202609080001", appliedMigrations);

        using var connection = database.GetSqliteConnection();
        connection.Open();

        Assert.True(TableExists(connection, "SchemaVersion"));
        Assert.True(TableExists(connection, "FiscalOperations"));
        Assert.True(TableExists(connection, "FiscalDocuments"));
        Assert.True(TableExists(connection, "FiscalEvents"));
        Assert.True(ColumnExists(connection, "Clientes", "RG"));
        Assert.True(ColumnExists(connection, "SchemaVersion", "Id"));
        Assert.True(ColumnExists(connection, "SchemaVersion", "Version"));
        Assert.True(ColumnExists(connection, "SchemaVersion", "AppliedAt"));
        Assert.True(ColumnExists(connection, "SchemaVersion", "Description"));
        Assert.True(IndexExists(connection, "UX_RecordLocks_Entity_Active"));
        Assert.True(ForeignKeyOnDeleteCascadeExists(connection, "ContatosFornecedor", "Fornecedores"));

        using var foreignKeysCommand = connection.CreateCommand();
        foreignKeysCommand.CommandText = "PRAGMA foreign_keys;";
        Assert.Equal(1, Convert.ToInt32(foreignKeysCommand.ExecuteScalar()));

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM SchemaVersion;";
        var count = Convert.ToInt32(countCommand.ExecuteScalar());

        Assert.True(count >= appliedMigrations.Count);
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 1
            FROM sqlite_master
            WHERE type = 'table'
              AND name = @Name
            LIMIT 1;";
        command.Parameters.AddWithValue("@Name", tableName);
        return command.ExecuteScalar() != null;
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IndexExists(SqliteConnection connection, string indexName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 1
            FROM sqlite_master
            WHERE type = 'index'
              AND name = @Name
            LIMIT 1;";
        command.Parameters.AddWithValue("@Name", indexName);
        return command.ExecuteScalar() != null;
    }

    private static bool ForeignKeyOnDeleteCascadeExists(SqliteConnection connection, string tableName, string referencedTable)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list({tableName});";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var table = reader.GetString(2);
            var onDelete = reader.GetString(6);
            if (string.Equals(table, referencedTable, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(onDelete, "CASCADE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Cleanup failure should not hide the assertion result.
        }
    }
}
