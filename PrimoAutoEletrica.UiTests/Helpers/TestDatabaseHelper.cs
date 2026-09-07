using System;
using System.IO;

namespace PrimoAutoEletrica.UiTests.Helpers
{
    /// <summary>
    /// Auxilia na criação de um banco de dados SQLite temporário para a execução dos testes UI.
    /// Copia o arquivo seed "primoauto.seed.db" para um diretório temporário e devolve o caminho completo.
    /// O teste deve iniciar a aplicação passando o parâmetro "--test-db <caminho>".
    /// </summary>
    public static class TestDatabaseHelper
    {
        /// <summary>
        /// Cria um banco de dados temporário a partir do seed e retorna o caminho.
        /// </summary>
        public static string CreateTempDatabase()
        {
            var assemblyDir = AppContext.BaseDirectory;
            var seedPath = Path.Combine(assemblyDir, "..", "..", "..", "PrimoAutoEletrica", "Data", "primoauto.seed.db");
            if (!File.Exists(seedPath))
                throw new FileNotFoundException("Arquivo seed do banco não encontrado.", seedPath);

            var tempDir = Path.Combine(Path.GetTempPath(), "PrimoAutoEletricaUITests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tempDbPath = Path.Combine(tempDir, "primoauto.db");
            File.Copy(seedPath, tempDbPath);
            return tempDbPath;
        }
    }
}
