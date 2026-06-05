using PrimoAutoEletrica.Services;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Helpers
{
    internal static class WindowInteractionHelper
    {
        public static void CloseWithDialogResult(Window window, bool? dialogResult = null, string area = "UI")
        {
            if (window == null)
            {
                return;
            }

            var applied = false;
            if (dialogResult.HasValue)
            {
                try
                {
                    window.DialogResult = dialogResult;
                    applied = true;
                }
                catch (InvalidOperationException ex)
                {
                    if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
                    {
                        global::PrimoAutoEletrica.App.Logger.LogInfo(
                            $"DialogResult suprimido em automacao para '{window.GetType().Name}': {ex.Message}",
                            area);
                    }
                }
            }

            if (!applied || window.IsVisible)
            {
                window.Close();
            }
        }

        public static void ShowMessage(
            string message,
            string title,
            MessageBoxImage image,
            string area = "UI",
            Exception? ex = null)
        {
            if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
            {
                var text = string.IsNullOrWhiteSpace(title) ? message : $"{title}: {message}";
                if (image == MessageBoxImage.Error || image == MessageBoxImage.Hand)
                {
                    global::PrimoAutoEletrica.App.Logger.LogError(text, ex, area);
                }
                else if (image == MessageBoxImage.Warning || image == MessageBoxImage.Exclamation)
                {
                    global::PrimoAutoEletrica.App.Logger.LogWarning(text, area);
                }
                else
                {
                    global::PrimoAutoEletrica.App.Logger.LogInfo(text, area);
                }

                return;
            }

            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }

        public static void LogAutomationExternalAction(string message, string area = "UI")
        {
            if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
            {
                global::PrimoAutoEletrica.App.Logger.LogInfo(message, area);
            }
        }
    }
}
