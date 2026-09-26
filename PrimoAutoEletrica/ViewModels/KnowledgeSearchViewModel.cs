using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Knowledge;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.ViewModels
{
    /// <summary>
    /// C2.2 — Buscar no PRIMOX ViewModel. UI → VM → KnowledgeSearchService (no SQL here).
    /// </summary>
    public sealed class KnowledgeSearchViewModel : INotifyPropertyChanged
    {
        private readonly IKnowledgeSearchService _searchService;
        private readonly Func<bool> _hasFinancePermission;
        private CancellationTokenSource? _cts;
        private string _queryText = string.Empty;
        private string _statusMessage = KnowledgeSearchService.EmptyQueryMessage;
        private KnowledgeSearchUiState _state = KnowledgeSearchUiState.NoQuery;
        private bool _isSearching;
        private string _lastDurationText = string.Empty;
        private KnowledgeSearchMatch? _selected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<KnowledgeSearchGroup> Groups { get; } = new();
        public ObservableCollection<KnowledgeSearchMatch> FlatResults { get; } = new();

        public string QueryText
        {
            get => _queryText;
            set
            {
                if (Set(ref _queryText, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(CanClear));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => Set(ref _statusMessage, value ?? string.Empty);
        }

        public KnowledgeSearchUiState State
        {
            get => _state;
            private set
            {
                if (Set(ref _state, value))
                {
                    OnPropertyChanged(nameof(IsNoQuery));
                    OnPropertyChanged(nameof(IsSearching));
                    OnPropertyChanged(nameof(IsNoResults));
                    OnPropertyChanged(nameof(IsFound));
                    OnPropertyChanged(nameof(IsError));
                    OnPropertyChanged(nameof(ShowResults));
                }
            }
        }

        public bool IsSearchingFlag
        {
            get => _isSearching;
            private set
            {
                if (Set(ref _isSearching, value))
                {
                    OnPropertyChanged(nameof(IsSearching));
                }
            }
        }

        public bool IsNoQuery => State == KnowledgeSearchUiState.NoQuery;
        public bool IsSearching => State == KnowledgeSearchUiState.Searching || _isSearching;
        public bool IsNoResults => State == KnowledgeSearchUiState.NoResults;
        public bool IsFound => State == KnowledgeSearchUiState.Found;
        public bool IsError => State == KnowledgeSearchUiState.Error;
        public bool ShowResults => State == KnowledgeSearchUiState.Found && FlatResults.Count > 0;
        public bool CanClear => !string.IsNullOrWhiteSpace(QueryText);

        public string LastDurationText
        {
            get => _lastDurationText;
            private set => Set(ref _lastDurationText, value ?? string.Empty);
        }

        public KnowledgeSearchMatch? SelectedResult
        {
            get => _selected;
            set => Set(ref _selected, value);
        }

        public KnowledgeSearchViewModel(
            IKnowledgeSearchService searchService,
            Func<bool>? hasFinancePermission = null)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _hasFinancePermission = hasFinancePermission ?? (() => false);
        }

        public async Task SearchAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            IsSearchingFlag = true;
            State = KnowledgeSearchUiState.Searching;
            StatusMessage = "Pesquisando no acervo PRIMOX...";

            try
            {
                var response = await _searchService.SearchAsync(new KnowledgeSearchQuery
                {
                    Text = QueryText,
                    MaxResults = 40,
                    Limit = 40,
                    HasFinancePermission = _hasFinancePermission()
                }, ct).ConfigureAwait(true);

                ApplyResponse(response);
            }
            catch (OperationCanceledException)
            {
                // superseded
            }
            catch (Exception ex)
            {
                State = KnowledgeSearchUiState.Error;
                StatusMessage = KnowledgeSearchService.ErrorMessage;
                LastDurationText = string.Empty;
                Groups.Clear();
                FlatResults.Clear();
                System.Diagnostics.Debug.WriteLine("KnowledgeSearchViewModel: " + ex.GetType().Name);
            }
            finally
            {
                IsSearchingFlag = false;
            }
        }

        public void Clear()
        {
            _cts?.Cancel();
            QueryText = string.Empty;
            Groups.Clear();
            FlatResults.Clear();
            SelectedResult = null;
            State = KnowledgeSearchUiState.NoQuery;
            StatusMessage = KnowledgeSearchService.EmptyQueryMessage;
            LastDurationText = string.Empty;
        }

        public void OpenSource(KnowledgeSearchMatch? match, FrameworkElement? ownerElement = null)
        {
            if (match == null)
            {
                return;
            }

            if (match.Item != null &&
                KnowledgeSearchService.IsFinanciallyRestricted(match.Item) &&
                !_hasFinancePermission())
            {
                MessageBox.Show(
                    "Você não tem permissão para abrir esta origem financeira.",
                    "Acesso negado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (TryOpenDomainDialog(match, ownerElement))
                {
                    return;
                }

                var summary = new Window
                {
                    Title = $"Origem — {match.Code} {match.Title}".Trim(),
                    Width = 640,
                    Height = 480,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                try
                {
                    summary.Background = (Brush)Application.Current.FindResource("AppBackgroundBrush");
                }
                catch
                {
                    // design-time / missing resource
                }

                if (ownerElement != null)
                {
                    WindowOwnerHelper.ConfigureOwner(summary, ownerElement);
                }
                else
                {
                    WindowOwnerHelper.ConfigureOwner(summary);
                }

                var scroll = new ScrollViewer { Padding = new Thickness(16) };
                var panel = new StackPanel();
                panel.Children.Add(MakeText($"Título: {match.Title}", true));
                panel.Children.Add(MakeText($"Tipo: {match.Type}"));
                panel.Children.Add(MakeText($"Origem: {match.SourceType} / {match.SourceId}"));
                panel.Children.Add(MakeText($"Relevância (retrieval): {match.Relevance:0.##}"));
                panel.Children.Add(MakeText($"Resumo: {match.Summary}"));
                if (match.Item != null)
                {
                    if (!string.IsNullOrWhiteSpace(match.Item.Symptom))
                        panel.Children.Add(MakeText($"Sintoma: {match.Item.Symptom}"));
                    if (!string.IsNullOrWhiteSpace(match.Item.Diagnosis))
                        panel.Children.Add(MakeText($"Diagnóstico: {match.Item.Diagnosis}"));
                    if (!string.IsNullOrWhiteSpace(match.Item.Solution))
                        panel.Children.Add(MakeText($"Solução: {match.Item.Solution}"));
                }

                scroll.Content = panel;
                summary.Content = scroll;
                summary.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível abrir a origem: " + ex.Message,
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private bool TryOpenDomainDialog(KnowledgeSearchMatch match, FrameworkElement? ownerElement)
        {
            try
            {
                if (App.Repositories?.Knowledge == null)
                {
                    return false;
                }

                if (string.Equals(match.SourceType, nameof(TechnicalKnowledgeEntry), StringComparison.OrdinalIgnoreCase)
                    && Guid.TryParse(match.SourceId, out var kid))
                {
                    var artigo = App.Repositories.Knowledge.ObterArtigoPorIdAsync(kid).GetAwaiter().GetResult();
                    if (artigo != null)
                    {
                        var dlg = new CasoTecnicoDialog(artigo);
                        if (ownerElement != null) WindowOwnerHelper.ConfigureOwner(dlg, ownerElement);
                        else WindowOwnerHelper.ConfigureOwner(dlg);
                        dlg.ShowDialog();
                        return true;
                    }
                }

                if (string.Equals(match.SourceType, nameof(DiagnosticCase), StringComparison.OrdinalIgnoreCase)
                    && Guid.TryParse(match.SourceId, out var cid))
                {
                    var caso = App.Repositories.Knowledge.ObterCasoPorIdAsync(cid).GetAwaiter().GetResult();
                    if (caso != null)
                    {
                        var dlg = new CasoTecnicoDialog(caso);
                        if (ownerElement != null) WindowOwnerHelper.ConfigureOwner(dlg, ownerElement);
                        else WindowOwnerHelper.ConfigureOwner(dlg);
                        dlg.ShowDialog();
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static TextBlock MakeText(string text, bool bold = false)
        {
            Brush? brush = null;
            try { brush = (Brush)Application.Current.FindResource("PrimaryTextBrush"); } catch { /* ignore */ }

            return new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = brush ?? Brushes.Black
            };
        }

        private void ApplyResponse(KnowledgeSearchResponse response)
        {
            Groups.Clear();
            FlatResults.Clear();
            foreach (var g in response.Groups)
            {
                Groups.Add(g);
            }

            foreach (var r in response.Results)
            {
                FlatResults.Add(r);
            }

            State = response.State;
            StatusMessage = response.Message;
            LastDurationText = response.DurationMs >= 0 ? $"{response.DurationMs} ms" : string.Empty;
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}