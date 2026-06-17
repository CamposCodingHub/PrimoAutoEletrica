using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class MigrationTests
    {
        [Fact]
        public void DatabaseServiceMigrations_DeveExistir()
        {
            // Verifica se o arquivo DatabaseService.Migrations.cs existe
            var migrationsPath = @"..\..\..\PrimoAutoEletrica\Services\DatabaseService.Migrations.cs";
            Assert.True(System.IO.File.Exists(migrationsPath), "DatabaseService.Migrations.cs deve existir");
        }

        [Fact]
        public void DatabaseServiceMigrations_DeveTerMetodoApplyDatabaseMigrations()
        {
            // Verifica se o arquivo contém o método ApplyDatabaseMigrations
            var migrationsPath = @"..\..\..\PrimoAutoEletrica\Services\DatabaseService.Migrations.cs";
            var content = System.IO.File.ReadAllText(migrationsPath);
            Assert.Contains("ApplyDatabaseMigrations", content, "DatabaseService.Migrations deve ter método ApplyDatabaseMigrations");
        }

        [Fact]
        public void DatabaseServiceMigrations_DeveTerMetodoApplyMigration()
        {
            // Verifica se o arquivo contém o método ApplyMigration
            var migrationsPath = @"..\..\..\PrimoAutoEletrica\Services\DatabaseService.Migrations.cs";
            var content = System.IO.File.ReadAllText(migrationsPath);
            Assert.Contains("ApplyMigration", content, "DatabaseService.Migrations deve ter método ApplyMigration");
        }

        [Fact]
        public void DatabaseServiceMigrations_DeveTerMetodoGetAppliedMigrations()
        {
            // Verifica se o arquivo contém o método GetAppliedMigrations
            var migrationsPath = @"..\..\..\PrimoAutoEletrica\Services\DatabaseService.Migrations.cs";
            var content = System.IO.File.ReadAllText(migrationsPath);
            Assert.Contains("GetAppliedMigrations", content, "DatabaseService.Migrations deve ter método GetAppliedMigrations");
        }
    }
}
