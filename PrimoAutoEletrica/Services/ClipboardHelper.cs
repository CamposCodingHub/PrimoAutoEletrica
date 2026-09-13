using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Clipboard helpers with retry for CLIPBRD_E_CANT_OPEN races under automation/load.
    /// </summary>
    public static class ClipboardHelper
    {
        public static void SetTextWithRetry(string text, int attempts = 8, int delayMs = 40)
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
                    return;
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

            throw last ?? new InvalidOperationException("Falha ao gravar na area de transferencia.");
        }
    }
}
