using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class LoggerServiceTests
    {
        [Fact]
        public void LoggerService_DeveExistir()
        {
            // Verifica se o arquivo LoggerService.cs existe
            var loggerServicePath = @"..\..\..\PrimoAutoEletrica\Services\LoggerService.cs";
            Assert.True(System.IO.File.Exists(loggerServicePath), "LoggerService.cs deve existir");
        }

        [Fact]
        public void LoggerService_DeveTerMetodoLogInfo()
        {
            // Verifica se o arquivo contém o método LogInfo
            var loggerServicePath = @"..\..\..\PrimoAutoEletrica\Services\LoggerService.cs";
            var content = System.IO.File.ReadAllText(loggerServicePath);
            Assert.Contains("LogInfo", content, "LoggerService deve ter método LogInfo");
        }

        [Fact]
        public void LoggerService_DeveTerMetodoLogError()
        {
            // Verifica se o arquivo contém o método LogError
            var loggerServicePath = @"..\..\..\PrimoAutoEletrica\Services\LoggerService.cs";
            var content = System.IO.File.ReadAllText(loggerServicePath);
            Assert.Contains("LogError", content, "LoggerService deve ter método LogError");
        }

        [Fact]
        public void LoggerService_DeveTerMetodoLogWarning()
        {
            // Verifica se o arquivo contém o método LogWarning
            var loggerServicePath = @"..\..\..\PrimoAutoEletrica\Services\LoggerService.cs";
            var content = System.IO.File.ReadAllText(loggerServicePath);
            Assert.Contains("LogWarning", content, "LoggerService deve ter método LogWarning");
        }
    }
}
