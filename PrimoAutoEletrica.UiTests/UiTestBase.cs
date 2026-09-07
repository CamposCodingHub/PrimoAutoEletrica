using System;
using System.Diagnostics;
using System.IO;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using PrimoAutoEletrica.UiTests.Helpers;

namespace PrimoAutoEletrica.UiTests
{
    /// <summary>
    /// Classe base para testes UI que inicia a aplicação e fornece objetos de automação.
    /// </summary>
    public abstract class UiTestBase : IDisposable
    {
        protected Application App { get; private set; }
        protected UIA3Automation Automation { get; private set; }
        protected Window MainWindow { get; private set; }
        protected string TempDbPath { get; private set; }

        protected UiTestBase()
        {
            var exePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..",
                "PrimoAutoEletrica",
                "bin",
                "Debug",
                "net6.0-windows",
                "PrimoAutoEletrica.exe"));

            if (!File.Exists(exePath))
                throw new FileNotFoundException($"Executável não encontrado em {exePath}");

            // Cria banco de dados temporário para o teste
            TempDbPath = TestDatabaseHelper.CreateTempDatabase();

            // Inicia a aplicação em modo de teste, passando caminho do banco temporário
            App = Application.Launch(exePath, $"--test-mode --test-db \"{TempDbPath}\"");
            Automation = new UIA3Automation();
            MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(30));
            if (MainWindow == null)
                throw new InvalidOperationException("Janela principal não apareceu dentro do tempo limite.");
        }

        public void Dispose()
        {
            try
            {
                Automation?.Dispose();
                if (!App.HasExited)
                    App.Close();
                // Opcional: deletar o banco temporário
                if (!string.IsNullOrEmpty(TempDbPath) && File.Exists(TempDbPath))
                    File.Delete(TempDbPath);
            }
            catch { /* Ignorar erros de limpeza */ }
        }
    }
}
