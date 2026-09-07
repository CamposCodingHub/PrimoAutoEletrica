using System;
using System.IO;

namespace PrimoAutoEletrica.UiTests.Helpers
{
    /// <summary>
    /// Helper para ler o arquivo de log da aplicação durante os testes.
    /// Assume que a aplicação grava logs em  Logs/app.log relativo ao diretório de execução.
    /// </summary>
    public static class LogCapture
    {
        /// <summary>
        /// Retorna o conteúdo completo do log da aplicação.
        /// </summary>
        public static string ReadLog()
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "app.log");
            if (!File.Exists(logPath))
                return string.Empty;
            return File.ReadAllText(logPath);
        }
    }
}
