using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// NET10-22 — contraste do header/setas do Calendar nativo no Dark
    /// sem substituir ControlTemplate de CalendarItem.
    /// Conservador: só Foreground em PART_* + TextBlocks de título;
    /// Paths de seta apenas se forem glifos pequenos (evita blocos sólidos).
    /// </summary>
    public static class CalendarContrastHealer
    {
        private static readonly DependencyProperty AttachedProperty =
            DependencyProperty.RegisterAttached(
                "CalendarContrastHealerAttached",
                typeof(bool),
                typeof(CalendarContrastHealer),
                new PropertyMetadata(false));

        private static bool _classHandlerRegistered;

        public static void Register(Services.ThemeService? themeService = null)
        {
            if (!_classHandlerRegistered)
            {
                EventManager.RegisterClassHandler(
                    typeof(Calendar),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(OnCalendarLoaded));
                _classHandlerRegistered = true;
            }

            // Theme re-heal is handled from ThemeService.ApplyTheme → HealAllOpen.
            _ = themeService;
        }

        public static void Attach(Calendar? calendar)
        {
            if (calendar == null)
            {
                return;
            }

            if (Equals(calendar.GetValue(AttachedProperty), true))
            {
                Heal(calendar);
                return;
            }

            calendar.SetValue(AttachedProperty, true);
            calendar.DisplayDateChanged += (_, _) => ScheduleHeal(calendar);
            calendar.DisplayModeChanged += (_, _) => ScheduleHeal(calendar);
            calendar.SelectedDatesChanged += (_, _) => ScheduleHeal(calendar);
            Heal(calendar);
        }

        public static void HealAllOpen()
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            foreach (Window window in app.Windows)
            {
                HealSubtree(window);
            }
        }

        public static void HealSubtree(DependencyObject? root)
        {
            if (root == null)
            {
                return;
            }

            if (root is Calendar calendar)
            {
                Attach(calendar);
            }

            if (root is Visual || root is System.Windows.Media.Media3D.Visual3D)
            {
                var count = VisualTreeHelper.GetChildrenCount(root);
                for (var i = 0; i < count; i++)
                {
                    HealSubtree(VisualTreeHelper.GetChild(root, i));
                }
            }
        }

        public static void Heal(Calendar calendar)
        {
            if (calendar == null)
            {
                return;
            }

            try
            {
                var brush = ResolvePrimaryTextBrush(calendar);
                if (brush == null)
                {
                    return;
                }

                foreach (var button in FindVisualChildren<Button>(calendar))
                {
                    if (button.Name is not ("PART_PreviousButton" or "PART_NextButton" or "PART_HeaderButton"))
                    {
                        continue;
                    }

                    button.Foreground = brush;
                    TextElement.SetForeground(button, brush);
                    button.Opacity = 1.0;

                    foreach (var text in FindVisualChildren<TextBlock>(button))
                    {
                        text.Foreground = brush;
                        text.Opacity = 1.0;
                    }

                    // Somente glifos pequenos de seta — nunca preencher Path grande (vira bloco sólido).
                    if (button.Name is "PART_PreviousButton" or "PART_NextButton")
                    {
                        foreach (var path in FindVisualChildren<Path>(button))
                        {
                            if (path.ActualWidth <= 14 && path.ActualHeight <= 14 && path.ActualWidth > 0)
                            {
                                path.Fill = brush;
                                path.Stroke = brush;
                                path.Opacity = 1.0;
                            }
                        }
                    }
                }

                foreach (var text in FindVisualChildren<TextBlock>(calendar))
                {
                    if (IsInsideDayOrMonthButton(text))
                    {
                        continue;
                    }

                    var content = text.Text?.Trim() ?? string.Empty;
                    // Day-of-week headers (S/T/Q) e título do mês no header.
                    if (content.Length is >= 1 and <= 24)
                    {
                        // Evitar sobrescrever textos de outros overlays longos.
                        if (content.Length <= 3 || content.Contains(" de ", StringComparison.OrdinalIgnoreCase))
                        {
                            text.Foreground = brush;
                            text.Opacity = 1.0;
                        }
                    }
                }
            }
            catch
            {
                // heal best-effort
            }
        }

        private static void OnCalendarLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Calendar calendar)
            {
                Attach(calendar);
            }
        }

        private static void ScheduleHeal(Calendar calendar)
        {
            calendar.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() => Heal(calendar)));
        }

        private static Brush? ResolvePrimaryTextBrush(FrameworkElement element)
        {
            if (element.TryFindResource("PrimaryTextBrush") is Brush fromElement)
            {
                return fromElement;
            }

            if (Application.Current?.TryFindResource("PrimaryTextBrush") is Brush fromApp)
            {
                return fromApp;
            }

            return null;
        }

        private static bool IsInsideDayOrMonthButton(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is CalendarDayButton or CalendarButton)
                {
                    return true;
                }

                if (current is Button button &&
                    button.Name is "PART_PreviousButton" or "PART_NextButton" or "PART_HeaderButton")
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null)
            {
                yield break;
            }

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var nested in FindVisualChildren<T>(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
