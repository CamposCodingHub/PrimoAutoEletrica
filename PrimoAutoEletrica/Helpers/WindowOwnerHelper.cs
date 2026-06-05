using System.Linq;
using System.Windows;

namespace PrimoAutoEletrica.Helpers
{
    internal static class WindowOwnerHelper
    {
        public static void ConfigureOwner(Window dialog, FrameworkElement? source = null)
        {
            var owner = ResolveOwner(dialog, source);
            if (owner != null)
            {
                dialog.Owner = owner;
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

        private static bool IsValidOwner(Window? candidate, Window dialog)
        {
            return candidate != null
                && !ReferenceEquals(candidate, dialog)
                && candidate.IsLoaded
                && candidate.IsVisible
                && !candidate.Dispatcher.HasShutdownStarted
                && !candidate.Dispatcher.HasShutdownFinished;
        }
    }
}
