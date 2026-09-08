using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// P15E-015 — identifica ações chrome sem texto (DataGrid SelectAll, DatePicker PART_Button).
    /// </summary>
    public static class AccessibilityChromeHealer
    {
        public static void HealSubtree(DependencyObject? root)
        {
            if (root == null)
            {
                return;
            }

            if (root is ButtonBase button)
            {
                HealButton(button);
            }

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

        public static void HealButton(ButtonBase button)
        {
            if (button == null)
            {
                return;
            }

            try
            {
                if (IsDataGridSelectAllButton(button))
                {
                    EnsureIdentity(button, SelectAllLabel(), SelectAllLabel());
                    return;
                }

                if (IsDatePickerDropDownButton(button))
                {
                    EnsureIdentity(button, OpenCalendarLabel(), OpenCalendarLabel());
                }
            }
            catch
            {
                // heal best-effort
            }
        }

        public static bool IsDataGridSelectAllButton(ButtonBase button)
        {
            if (button?.Command == null)
            {
                return false;
            }

            if (ReferenceEquals(button.Command, DataGrid.SelectAllCommand))
            {
                return true;
            }

            return button.Command is RoutedCommand routed
                   && string.Equals(routed.Name, "SelectAll", System.StringComparison.Ordinal)
                   && IsInsideDataGrid(button);
        }

        public static bool IsDatePickerDropDownButton(ButtonBase button)
        {
            if (button == null)
            {
                return false;
            }

            if (string.Equals(button.Name, "PART_Button", System.StringComparison.Ordinal))
            {
                return IsInsideDatePicker(button);
            }

            return false;
        }

        private static void EnsureIdentity(ButtonBase button, string automationName, string toolTip)
        {
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)))
            {
                AutomationProperties.SetName(button, automationName);
            }

            if (button.ToolTip == null || string.IsNullOrWhiteSpace(button.ToolTip.ToString()))
            {
                button.ToolTip = toolTip;
            }
        }

        private static bool IsInsideDataGrid(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is DataGrid)
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        private static bool IsInsideDatePicker(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is DatePicker)
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        private static string SelectAllLabel()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return lang switch
            {
                "en" => "Select all",
                "es" => "Seleccionar todo",
                _ => "Selecionar tudo"
            };
        }

        private static string OpenCalendarLabel()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return lang switch
            {
                "en" => "Open calendar",
                "es" => "Abrir calendario",
                _ => "Abrir calendario"
            };
        }
    }
}
