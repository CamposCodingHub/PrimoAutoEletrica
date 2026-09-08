using System.Windows;
using System.Windows.Media;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// P15E-001 — garante FocusVisualStyle válido (evita UnsetValue do tema Windows).
    /// </summary>
    public static class FocusVisualStyleHealer
    {
        public static Style? ResolveSafeStyle(FrameworkElement? scope = null)
        {
            object? resource = null;
            if (scope != null)
            {
                resource = scope.TryFindResource(SystemParameters.FocusVisualStyleKey)
                    ?? scope.TryFindResource("PrimoxFocusVisual");
            }

            resource ??= Application.Current?.TryFindResource(SystemParameters.FocusVisualStyleKey)
                ?? Application.Current?.TryFindResource("PrimoxFocusVisual");

            return resource as Style;
        }

        public static void HealSubtree(DependencyObject root)
        {
            if (root == null)
            {
                return;
            }

            var style = ResolveSafeStyle(root as FrameworkElement);
            HealElement(root, style);

            if (root is Visual || root is System.Windows.Media.Media3D.Visual3D)
            {
                var count = VisualTreeHelper.GetChildrenCount(root);
                for (var i = 0; i < count; i++)
                {
                    HealSubtree(VisualTreeHelper.GetChild(root, i));
                }
            }
            else
            {
                foreach (var child in LogicalTreeHelper.GetChildren(root))
                {
                    if (child is DependencyObject dep)
                    {
                        HealSubtree(dep);
                    }
                }
            }
        }

        public static void HealElement(DependencyObject element, Style? style)
        {
            if (element is not FrameworkElement fe || !fe.Focusable)
            {
                return;
            }

            try
            {
                // Força valor local válido — remove expressão DynamicResource quebrada do tema.
                fe.SetValue(FrameworkElement.FocusVisualStyleProperty, style);
            }
            catch
            {
                try
                {
                    fe.ClearValue(FrameworkElement.FocusVisualStyleProperty);
                    fe.SetValue(FrameworkElement.FocusVisualStyleProperty, style);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
