using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class ExternalBackupEncryptionHonestyTests
    {
        [Fact]
        public async Task CreateZipAsync_ComSenha_LancaNotSupported_NaoFingeCriptografia()
        {
            var appData = Path.Combine(Path.GetTempPath(), "primox-bak-app-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(appData);
            var settings = new BackupSettings();
            var svc = new ExternalBackupService(appData, settings);
            var method = typeof(ExternalBackupService).GetMethod("CreateZipAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var src = Path.Combine(Path.GetTempPath(), "primox-bak-src-" + Guid.NewGuid().ToString("N"));
            var zip = Path.Combine(Path.GetTempPath(), "primox-bak-" + Guid.NewGuid().ToString("N") + ".zip");
            Directory.CreateDirectory(src);
            await File.WriteAllTextAsync(Path.Combine(src, "a.txt"), "dados");

            try
            {
                var task = (Task)method!.Invoke(svc, new object?[] { src, zip, "senha-secreta" })!;
                var ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await task);
                Assert.Contains("NAO esta implementada", ex.Message, StringComparison.OrdinalIgnoreCase);
                Assert.False(File.Exists(zip), "Nao deve criar ZIP desprotegido quando senha foi pedida");
            }
            finally
            {
                if (Directory.Exists(src)) Directory.Delete(src, true);
                if (File.Exists(zip)) File.Delete(zip);
                if (Directory.Exists(appData)) Directory.Delete(appData, true);
            }
        }
    }
}
