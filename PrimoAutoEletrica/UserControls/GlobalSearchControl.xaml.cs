using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.UserControls
{
    public partial class GlobalSearchControl : UserControl
    {
        public ObservableCollection<SearchResult> SearchResults { get; set; }
        private List<SearchResult> _allResults;
        private Func<List<SearchResult>>? _dataProvider;
        private bool _dataLoaded;

        public event EventHandler<SearchResultSelectedEventArgs>? ResultSelected;

        public GlobalSearchControl()
        {
            InitializeComponent();
            SearchResults = new ObservableCollection<SearchResult>();
            _allResults = new List<SearchResult>();
            AutocompleteItems.ItemsSource = SearchResults;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.Trim();
            ClearButton.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Collapsed : Visibility.Visible;

            if (string.IsNullOrEmpty(searchText) || searchText.Length < 2)
            {
                AutocompletePopup.IsOpen = false;
                return;
            }

            EnsureSearchDataLoaded();

            var filtered = _allResults
                .Where(r => r.Titulo.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            r.Subtitulo.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            r.Tipo.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();

            SearchResults.Clear();
            foreach (var result in filtered)
            {
                SearchResults.Add(result);
            }

            AutocompletePopup.IsOpen = SearchResults.Count > 0;
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResults.Count > 0)
            {
                NavigateToResult(SearchResults[0]);
                AutocompletePopup.IsOpen = false;
            }
            else if (e.Key == Key.Escape)
            {
                AutocompletePopup.IsOpen = false;
                SearchTextBox.Text = string.Empty;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchTextBox.Focus();
            AutocompletePopup.IsOpen = false;
        }

        private void ResultItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is SearchResult result)
            {
                NavigateToResult(result);
                AutocompletePopup.IsOpen = false;
            }
        }

        private void NavigateToResult(SearchResult result)
        {
            SearchTextBox.Text = string.Empty;
            ResultSelected?.Invoke(this, new SearchResultSelectedEventArgs(result));
        }

        public void LoadSearchData(List<SearchResult> results)
        {
            _allResults = results ?? new List<SearchResult>();
            _dataLoaded = true;
        }

        public void ConfigureSearchDataProvider(Func<List<SearchResult>> dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            InvalidateSearchData();
        }

        public void InvalidateSearchData()
        {
            _allResults = new List<SearchResult>();
            SearchResults.Clear();
            _dataLoaded = false;
        }

        public bool IsDataLoaded => _dataLoaded;

        public void EnsureDataLoadedForAutomation()
        {
            EnsureSearchDataLoaded();
        }

        private void EnsureSearchDataLoaded()
        {
            if (_dataLoaded)
            {
                return;
            }

            _allResults = _dataProvider?.Invoke() ?? new List<SearchResult>();
            _dataLoaded = true;
        }
    }

    public class SearchResultSelectedEventArgs : EventArgs
    {
        public SearchResultSelectedEventArgs(SearchResult result)
        {
            Result = result;
        }

        public SearchResult Result { get; }
    }

    public class SearchResult
    {
        public string Titulo { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
    }
}
