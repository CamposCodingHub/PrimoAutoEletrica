using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.Views
{
    public sealed class CommandPaletteItem
    {
        public string Id { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string ShortcutHint { get; init; } = string.Empty;
        public Action? Execute { get; init; }
    }

    public partial class CommandPaletteWindow : Window
    {
        private readonly List<CommandPaletteItem> _allItems;
        private CommandPaletteItem? _selectedItem;

        public Action? SelectedAction => _selectedItem?.Execute;

        public CommandPaletteWindow(IEnumerable<CommandPaletteItem> items)
        {
            InitializeComponent();
            _allItems = items?.ToList() ?? new List<CommandPaletteItem>();
            AplicarFiltro(string.Empty);
            Loaded += (_, _) =>
            {
                FiltroTextBox.Focus();
                FiltroTextBox.SelectAll();
            };
        }

        private void AplicarFiltro(string termo)
        {
            var filtro = termo?.Trim() ?? string.Empty;
            IEnumerable<CommandPaletteItem> consulta = _allItems;

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                consulta = consulta.Where(i =>
                    i.Title.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    i.Subtitle.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    i.Category.Contains(filtro, StringComparison.OrdinalIgnoreCase));
            }

            var lista = consulta.Take(40).ToList();
            ComandosListBox.ItemsSource = lista;

            if (lista.Count > 0)
                ComandosListBox.SelectedIndex = 0;
        }

        private void FiltroTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltro(FiltroTextBox.Text);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                ExecutarSelecionado();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down)
            {
                MoverSelecao(1);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up)
            {
                MoverSelecao(-1);
                e.Handled = true;
            }
        }

        private void MoverSelecao(int delta)
        {
            if (ComandosListBox.Items.Count == 0)
                return;

            var index = ComandosListBox.SelectedIndex;
            if (index < 0)
                index = 0;
            else
                index = Math.Clamp(index + delta, 0, ComandosListBox.Items.Count - 1);

            ComandosListBox.SelectedIndex = index;
            ComandosListBox.ScrollIntoView(ComandosListBox.SelectedItem);
        }

        private void ComandosListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExecutarSelecionado();
        }

        private void ExecutarSelecionado()
        {
            if (ComandosListBox.SelectedItem is not CommandPaletteItem item || item.Execute == null)
                return;

            _selectedItem = item;
            DialogResult = true;
            Close();
        }
    }
}
