using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PrimoAutoEletrica.Helpers
{
    internal static class WindowOwnerHelper
    {
        public static void ConfigureOwner(Window dialog, FrameworkElement? source = null)
        {
            if (dialog == null)
            {
                return;
            }

            // Nunca atribuir Owner em janela ja fechada.
            if (!CanAcceptOwner(dialog))
            {
                return;
            }

            var owner = ResolveOwner(dialog, source);
            if (owner == null)
            {
                return;
            }

            try
            {
                dialog.Owner = owner;
            }
            catch (InvalidOperationException)
            {
                // Owner invalido (nao exibida / fechada / em shutdown) — ignora com seguranca.
            }
        }

        public static Window? ResolveOwner(Window dialog, FrameworkElement? source = null)
        {
            var owner = source != null ? Window.GetWindow(source) : null;
            if (IsValidOwner(owner, dialog))
            {
                return owner;
            }

            owner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(window => IsValidOwner(window, dialog) && window.IsActive)
                ?? Application.Current?.Windows
                    .OfType<Window>()
                    .FirstOrDefault(window => IsValidOwner(window, dialog) && window.IsVisible)
                ?? Application.Current?.MainWindow;

            return IsValidOwner(owner, dialog) ? owner : null;
        }

        private static bool CanAcceptOwner(Window dialog)
        {
            try
            {
                return dialog != null
                    && !dialog.Dispatcher.HasShutdownStarted
                    && !dialog.Dispatcher.HasShutdownFinished;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidOwner(Window? candidate, Window dialog)
        {
            if (candidate == null || ReferenceEquals(candidate, dialog))
            {
                return false;
            }

            try
            {
                if (!candidate.IsLoaded || !candidate.IsVisible)
                {
                    return false;
                }

                if (candidate.Dispatcher.HasShutdownStarted || candidate.Dispatcher.HasShutdownFinished)
                {
                    return false;
                }

                // Exige HWND/source real — evita "Owner on a Window that has not been shown".
                return PresentationSource.FromVisual(candidate) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
