using System;
using System.IO;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests;

/// <summary>
/// FULL ASSURANCE-12 — service-level authorization for backup/restore.
/// </summary>
public sealed class BackupAuthorizationTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly DatabaseService _database;
    private readonly LoggerService _logger;
    private readonly DatabaseBackupService _backups;

    public BackupAuthorizationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-backup-authz-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        var backupDir = Path.Combine(_tempRoot, "Backups");
        Directory.CreateDirectory(backupDir);
        _logger = new LoggerService();
        _database = new DatabaseService(_tempRoot, logger: _logger);
        _backups = new DatabaseBackupService(_database, _logger, backupDir);
    }

    [Fact]
    public void RestaurarBackupAuthorized_RejectsOperatorWithoutSistemaConfigurar()
    {
        var operador = new Funcionario
        {
            Id = 9001,
            Nome = "OPERATOR_TEST",
            Email = "operator_test@qa.local",
            PerfilAcesso = "Vendedor",
            Ativo = true
        };
        var perms = new PermissionService(operador, _logger, _database);
        Assert.False(perms.TemPermissaoCodigo("SISTEMA_CONFIGURAR"));

        var dummy = Path.Combine(_tempRoot, "Backups", "dummy.db");
        File.WriteAllText(dummy, "not-a-real-backup");

        Assert.Throws<UnauthorizedAccessException>(() =>
            _backups.RestaurarBackupAuthorized(dummy, operador, perms, criarBackupSeguranca: false));
    }

    [Fact]
    public void CriarBackupManualAuthorized_RejectsOperatorWithoutSistemaConfigurar()
    {
        var operador = new Funcionario
        {
            Id = 9002,
            Nome = "USER_TEST",
            Email = "user_test@qa.local",
            PerfilAcesso = "Tecnico",
            Ativo = true
        };
        var perms = new PermissionService(operador, _logger, _database);
        Assert.False(perms.TemPermissaoCodigo("SISTEMA_CONFIGURAR"));

        Assert.Throws<UnauthorizedAccessException>(() =>
            _backups.CriarBackupManualAuthorized(null, operador, perms));
    }

    [Fact]
    public void RestaurarBackup_RejectsPathOutsideAuthorizedRoots()
    {
        var outside = Path.Combine(Path.GetTempPath(), $"primo-outside-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outside);
        var canary = Path.Combine(outside, "CANARY_OUTSIDE.txt");
        File.WriteAllText(canary, "outside");

        try
        {
            Assert.Throws<UnauthorizedAccessException>(() =>
                _backups.RestaurarBackup(canary, criarBackupSeguranca: false));
        }
        finally
        {
            try { Directory.Delete(outside, true); } catch { }
        }
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
            // ignore cleanup races
        }
    }
}
