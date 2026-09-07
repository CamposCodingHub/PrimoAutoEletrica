using System;
using System.Drawing.Imaging;
using FlaUI.Core.AutomationElements;
using FlaUI.Core;

namespace PrimoAutoEletrica.UiTests.Helpers
{
    /// <summary>
    /// Salva capturas de tela da janela principal quando um teste falha.
    /// Os arquivos são gravados em "TestResults/Screenshots" dentro da pasta do teste.
    /// </summary>
    public static class ScreenshotHelper
    {
        private static readonly string ScreenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "Screenshots");

        static ScreenshotHelper()
        {
            Directory.CreateDirectory(ScreenshotsDir);
        }

        /// <summary>
        /// Captura a tela da <paramref name="window"/> e salva com um nome único.
        /// </summary>
        public static void Capture(Window window, string testName)
        {
            if (window == null) return;
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filePath = Path.Combine(ScreenshotsDir, $"{testName}_{timestamp}.png");
            try
            {
                window.Capture().Save(filePath, ImageFormat.Png);
            }
            catch
            {
                // Ignorar falhas de captura — isso não deve impedir o teste.
            }
        }
    }
}
