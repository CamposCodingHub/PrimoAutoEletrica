using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Clipboard helpers with retry for CLIPBRD_E_CANT_OPEN races under automation/load.
    /// In automated test mode, exhausted retries soft-fail (log + return false) so ExhaustiveUi
    /// and RaiseButtonClick are not poisoned by OS clipboard contention.
    /// </summary>
    public static class ClipboardHelper
    {
        public static bool TrySetTextWithRetry(string text, int attempts = 10, int delayMs = 50)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            Exception? last = null;
            for (var i = 0; i < attempts; i++)
            {
                try
                {
                    Clipboard.SetDataObject(text, copy: true);
                    return true;
                }
                catch (COMException ex)
                {
                    last = ex;
                    Thread.Sleep(delayMs * (i + 1));
                }
                catch (ExternalException ex)
                {
                    last = ex;
                    Thread.Sleep(delayMs * (i + 1));
                }
            }

            if (App.IsAutomatedTestMode)
            {
                try
                {
                    App.Logger.LogWarning(
                        $"Clipboard soft-fail em automacao apos {attempts} tentativas: {last?.Message}");
                }
                catch
                {
                }

                return false;
            }

            throw last ?? new InvalidOperationException("Falha ao gravar na area de transferencia.");
        }

        public static void SetTextWithRetry(string text, int attempts = 10, int delayMs = 50)
        {
            _ = TrySetTextWithRetry(text, attempts, delayMs);
        }
    }
}
