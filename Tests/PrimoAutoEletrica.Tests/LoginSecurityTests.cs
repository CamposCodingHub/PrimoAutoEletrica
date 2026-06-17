using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class LoginSecurityTests : IDisposable
{
    private readonly string _tempRoot;

    public LoginSecurityTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-login-security-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void AdminInicial_DeveUsarSenhaTemporariaComHashETrocaObrigatoria()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var credencialPath = Directory
            .EnumerateFiles(_tempRoot, "credenciais-iniciais-admin.txt", SearchOption.AllDirectories)
            .Single();

        var senhaTemporaria = File.ReadLines(credencialPath)
            .Single(line => line.StartsWith("Senha temporaria:", StringComparison.OrdinalIgnoreCase))
            .Split(':', 2)[1]
            .Trim();

        int adminId;

        using (var connection = database.GetConnection())
        {
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Senha, ExigirTrocaSenha
                FROM Funcionarios
                WHERE lower(Email) = 'admin@primoauto.com'
                LIMIT 1;";

            using var reader = command.ExecuteReader();
            Assert.True(reader.Read(), "Administrador inicial nao foi criado.");

            adminId = reader.GetInt32(0);
            var senhaPersistida = reader.GetString(1);
            var exigirTrocaSenha = reader.GetInt32(2);

            Assert.StartsWith("PBKDF2$", senhaPersistida, StringComparison.Ordinal);
            Assert.DoesNotContain(senhaTemporaria, senhaPersistida, StringComparison.Ordinal);
            Assert.Equal(1, exigirTrocaSenha);
        }

        var loginTemporario = database.AutenticarFuncionarioDetalhado("admin@primoauto.com", senhaTemporaria);
        Assert.True(loginTemporario.IsSuccess);
        Assert.True(loginTemporario.RequiresPasswordChange);

        const string novaSenha = "NovaSenha2026";
        database.AlterarSenhaFuncionario(adminId, novaSenha, exigirTrocaSenha: false, operador: "Teste");

        var loginSenhaAntiga = database.AutenticarFuncionarioDetalhado("admin@primoauto.com", senhaTemporaria);
        Assert.False(loginSenhaAntiga.IsSuccess);

        var loginNovaSenha = database.AutenticarFuncionarioDetalhado("admin@primoauto.com", novaSenha);
        Assert.True(loginNovaSenha.IsSuccess);
        Assert.False(loginNovaSenha.RequiresPasswordChange);
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
