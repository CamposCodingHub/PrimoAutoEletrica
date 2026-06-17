using Xunit;
using System;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Tests
{
    public class AuditLogServiceTests
    {
        [Fact]
        public void AuditLogService_DeveExistir()
        {
            // Verifica se o arquivo AuditLogService.cs existe
            var auditLogServicePath = @"..\..\..\PrimoAutoEletrica\Services\AuditLogService.cs";
            Assert.True(System.IO.File.Exists(auditLogServicePath), "AuditLogService.cs deve existir");
        }

        [Fact]
        public void AuditLogService_DeveTerMetodoRegistrarAcao()
        {
            // Verifica se o arquivo contém o método RegistrarAcao
            var auditLogServicePath = @"..\..\..\PrimoAutoEletrica\Services\AuditLogService.cs";
            var content = System.IO.File.ReadAllText(auditLogServicePath);
            Assert.Contains("RegistrarAcao", content, "AuditLogService deve ter método RegistrarAcao");
        }

        [Fact]
        public void AuditLogService_DeveTerMetodoRegistrarAcaoCritica()
        {
            // Verifica se o arquivo contém o método RegistrarAcaoCritica
            var auditLogServicePath = @"..\..\..\PrimoAutoEletrica\Services\AuditLogService.cs";
            var content = System.IO.File.ReadAllText(auditLogServicePath);
            Assert.Contains("RegistrarAcaoCritica", content, "AuditLogService deve ter método RegistrarAcaoCritica");
        }

        [Fact]
        public void AuditLogService_DeveTerMetodoObterHistorico()
        {
            // Verifica se o arquivo contém o método ObterHistorico
            var auditLogServicePath = @"..\..\..\PrimoAutoEletrica\Services\AuditLogService.cs";
            var content = System.IO.File.ReadAllText(auditLogServicePath);
            Assert.Contains("ObterHistorico", content, "AuditLogService deve ter método ObterHistorico");
        }
    }
}
