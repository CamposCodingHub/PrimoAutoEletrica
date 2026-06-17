using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class BackupServiceTests
    {
        [Fact]
        public void BackupService_DeveExistir()
        {
            // Verifica se o arquivo DatabaseBackupService.cs existe
            var backupServicePath = @"..\..\..\PrimoAutoEletrica\Services\DatabaseBackupService.cs";
            Assert.True(System.IO.File.Exists(backupServicePath), "DatabaseBackupService.cs deve existir");
        }

        [Fact]
        public void BackupService_DeveTerMetodoCriarBackup()
        {
            // Verifica se o arquivo contém o método CriarBackup
            var backupServicePath = @"..\..\..\PrimoAutoEletrica\Services\DatabaseBackupService.cs";
            var content = System.IO.File.ReadAllText(backupServicePath);
            Assert.Contains("CriarBackup", content, "BackupService deve ter método CriarBackup");
        }

        [Fact]
        public void BackupService_DeveTerMetodoRestaurarBackup()
        {
            // Verifica se o arquivo contém o método RestaurarBackup
            var backupServicePath = @"..\..\..\PrimoAutoEletrica\Services\DatabaseBackupService.cs";
            var content = System.IO.File.ReadAllText(backupServicePath);
            Assert.Contains("RestaurarBackup", content, "BackupService deve ter método RestaurarBackup");
        }

        [Fact]
        public void BackupService_DeveTerMetodoVerificarIntegridade()
        {
            // Verifica se o arquivo contém o método VerificarIntegridade
            var backupServicePath = @"..\..\..\PrimoAutoEletrica\Services\DatabaseBackupService.cs";
            var content = System.IO.File.ReadAllText(backupServicePath);
            Assert.Contains("VerificarIntegridade", content, "BackupService deve ter método VerificarIntegridade");
        }
    }
}
