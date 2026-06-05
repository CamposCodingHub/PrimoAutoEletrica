using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PrimoAutoEletrica.Helpers
{
    public static class TextSanitizerBehavior
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(TextSanitizerBehavior),
                new PropertyMetadata(false, OnEnabledChanged));

        public static bool GetEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnabledProperty);
        }

        public static void SetEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(EnabledProperty, value);
        }

        private static void OnEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is not FrameworkElement element)
            {
                return;
            }

            if ((bool)args.NewValue)
            {
                element.Loaded -= OnLoaded;
                element.Loaded += OnLoaded;
            }
            else
            {
                element.Loaded -= OnLoaded;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (sender is DependencyObject dependencyObject)
            {
                SanitizeTree(dependencyObject, new HashSet<DependencyObject>());
            }
        }

        private static void SanitizeTree(DependencyObject dependencyObject, HashSet<DependencyObject> visited)
        {
            if (!visited.Add(dependencyObject))
            {
                return;
            }

            SanitizeCurrent(dependencyObject);

            if (dependencyObject is Visual or Visual3D)
            {
                var visualChildren = VisualTreeHelper.GetChildrenCount(dependencyObject);
                for (var index = 0; index < visualChildren; index++)
                {
                    SanitizeTree(VisualTreeHelper.GetChild(dependencyObject, index), visited);
                }
            }

            foreach (var logicalChild in LogicalTreeHelper.GetChildren(dependencyObject))
            {
                if (logicalChild is DependencyObject child)
                {
                    SanitizeTree(child, visited);
                }
            }
        }

        private static void SanitizeCurrent(DependencyObject dependencyObject)
        {
            switch (dependencyObject)
            {
                case TextBlock textBlock when !HasBinding(textBlock, TextBlock.TextProperty):
                    textBlock.Text = UiTextSanitizer.SanitizeText(textBlock.Text);
                    break;

                case Run run when !HasBinding(run, Run.TextProperty):
                    run.Text = UiTextSanitizer.SanitizeText(run.Text);
                    break;

                case HeaderedContentControl headeredContentControl:
                    SanitizeHeader(headeredContentControl);
                    SanitizeContent(headeredContentControl);
                    break;

                case ContentControl contentControl:
                    SanitizeContent(contentControl);
                    break;

                case HeaderedItemsControl headeredItemsControl:
                    SanitizeHeader(headeredItemsControl);
                    break;

                case DataGrid dataGrid:
                    SanitizeDataGridHeaders(dataGrid);
                    break;
            }

            if (dependencyObject is FrameworkElement frameworkElement)
            {
                SanitizeToolTip(frameworkElement);
            }
        }

        private static void SanitizeContent(ContentControl contentControl)
        {
            if (HasBinding(contentControl, ContentControl.ContentProperty))
            {
                return;
            }

            if (contentControl.Content is string text)
            {
                contentControl.Content = UiTextSanitizer.SanitizeText(text);
            }
        }

        private static void SanitizeHeader(HeaderedContentControl control)
        {
            if (HasBinding(control, HeaderedContentControl.HeaderProperty))
            {
                return;
            }

            if (control.Header is string text)
            {
                control.Header = UiTextSanitizer.SanitizeText(text);
            }
        }

        private static void SanitizeHeader(HeaderedItemsControl control)
        {
            if (HasBinding(control, HeaderedItemsControl.HeaderProperty))
            {
                return;
            }

            if (control.Header is string text)
            {
                control.Header = UiTextSanitizer.SanitizeText(text);
            }
        }

        private static void SanitizeDataGridHeaders(DataGrid dataGrid)
        {
            foreach (var column in dataGrid.Columns)
            {
                if (column.Header is string text)
                {
                    column.Header = UiTextSanitizer.SanitizeText(text);
                }
            }
        }

        private static void SanitizeToolTip(FrameworkElement element)
        {
            if (HasBinding(element, FrameworkElement.ToolTipProperty))
            {
                return;
            }

            if (element.ToolTip is string text)
            {
                element.ToolTip = UiTextSanitizer.SanitizeText(text);
            }
        }

        private static bool HasBinding(DependencyObject dependencyObject, DependencyProperty dependencyProperty)
        {
            return BindingOperations.GetBindingBase(dependencyObject, dependencyProperty) is not null;
        }
    }
}
