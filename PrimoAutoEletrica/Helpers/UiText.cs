using PrimoAutoEletrica.Services;
using System.Windows;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Atalho tipado para textos de interacao (MessageBox / confirmacoes / toasts).
    /// Reutiliza LocalizationService — nao e uma arquitetura paralela.
    /// </summary>
    internal static class UiText
    {
        public static string T(string key, params object[] args)
            => LocalizationService.Instance.GetString(key, args);

        public static void Info(string messageKey, string? titleKey = null, params object[] messageArgs)
            => WindowInteractionHelper.ShowMessage(
                T(messageKey, messageArgs),
                T(titleKey ?? "Information"),
                MessageBoxImage.Information);

        public static void Success(string messageKey, string? titleKey = null, params object[] messageArgs)
            => WindowInteractionHelper.ShowMessage(
                T(messageKey, messageArgs),
                T(titleKey ?? "Success"),
                MessageBoxImage.Information);

        public static void Warning(string messageKey, string? titleKey = null, params object[] messageArgs)
            => WindowInteractionHelper.ShowMessage(
                T(messageKey, messageArgs),
                T(titleKey ?? "Warning"),
                MessageBoxImage.Warning);

        public static void Error(string messageKey, string? titleKey = null, params object[] messageArgs)
            => WindowInteractionHelper.ShowMessage(
                T(messageKey, messageArgs),
                T(titleKey ?? "Error"),
                MessageBoxImage.Error);

        public static void ErrorDetail(string messageKey, string detail, string? titleKey = null)
            => WindowInteractionHelper.ShowMessage(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, T(messageKey), detail),
                T(titleKey ?? "Error"),
                MessageBoxImage.Error);

        public static MessageBoxResult Confirm(
            string messageKey,
            string? titleKey = null,
            MessageBoxButton buttons = MessageBoxButton.YesNo,
            params object[] messageArgs)
        {
            if (App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo($"Confirm [{titleKey ?? "AreYouSure"}]: {T(messageKey, messageArgs)}", "UI");
                return MessageBoxResult.Yes;
            }

            return MessageBox.Show(
                T(messageKey, messageArgs),
                T(titleKey ?? "AreYouSure"),
                buttons,
                MessageBoxImage.Question);
        }
    }
}
