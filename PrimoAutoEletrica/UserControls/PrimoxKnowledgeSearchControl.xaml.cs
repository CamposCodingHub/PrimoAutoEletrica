using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation;
using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Knowledge;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.UserControls
{
    public partial class PrimoxKnowledgeSearchControl : UserControl
    {
        private readonly KnowledgeSearchViewModel _vm;

        public PrimoxKnowledgeSearchControl()
        {
            InitializeComponent();
            _vm = ResolveViewModel();
            DataContext = _vm;
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(KnowledgeSearchViewModel.StatusMessage)
                    or nameof(KnowledgeSearchViewModel.State)
                    or nameof(KnowledgeSearchViewModel.IsSearching)
                    or nameof(KnowledgeSearchViewModel.LastDurationText)
                    or nameof(KnowledgeSearchViewModel.FlatResults)
                    or nameof(KnowledgeSearchViewModel.Groups))
                {
                    SyncUiFromVm();
                }
            };
            SyncUiFromVm();
            Loaded += async (_, __) =>
            {
                try
                {
                    // Warm index once (D01–D17 + adapters) — failures surface as controlled error state.
                    if (App.Services?.GetService<IKnowledgeSearchService>() is { } search)
                    {
                        await search.EnsureIndexAsync().ConfigureAwait(true);
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = KnowledgeSearchService.ErrorMessage;
                    System.Diagnostics.Debug.WriteLine(ex.GetType().Name);
                }
            };
        }

        /// <summary>Exposes VM for UiSmoke dedicated Search:* scenarios.</summary>
        public KnowledgeSearchViewModel ViewModel => _vm;

        public TextBox SearchBox => SearchTextBox;

        private static KnowledgeSearchViewModel ResolveViewModel()
        {
            try
            {
                var fromDi = App.Services?.GetService<KnowledgeSearchViewModel>();
                if (fromDi != null) return fromDi;
            }
            catch
            {
                // fall through
            }

            // Fallback construction for design-time / early init (still no SQL in UI)
            IKnowledgeRepository? repo = null;
            try { repo = App.Repositories?.Knowledge; } catch { /* ignore */ }

            if (repo == null)
            {
                // Minimal in-memory path still uses retrieval adapters for D01–D17 via a throwaway DB later in search.
                // For smoke without App infra, create retrieval with a no-op-safe path:
                throw new InvalidOperationException("Infraestrutura de conhecimento indisponível para Buscar no PRIMOX.");
            }

            var retrieval = new KnowledgeRetrievalService(repo, App.Repositories?.OrdensServico, App.Logger);
            var search = new KnowledgeSearchService(retrieval, App.Logger);
            return new KnowledgeSearchViewModel(search, () =>
            {
                try
                {
                    var perms = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);
                    return perms.TemPermissao("Financeiro") || perms.TemPermissaoCodigo("FINANCEIRO_VER");
                }
                catch { return false; }
            });
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e) => await RunSearchAsync();

        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await RunSearchAsync();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            _vm.Clear();
            SyncUiFromVm();
            SearchTextBox.Focus();
        }

        private async Task RunSearchAsync()
        {
            _vm.QueryText = SearchTextBox.Text ?? string.Empty;
            SearchButton.IsEnabled = false;
            try
            {
                await _vm.SearchAsync();
            }
            finally
            {
                SearchButton.IsEnabled = true;
                SyncUiFromVm();
            }
        }

        private void SyncUiFromVm()
        {
            StatusText.Text = _vm.StatusMessage;
            DurationText.Text = string.IsNullOrWhiteSpace(_vm.LastDurationText) ? string.Empty : $"⏱ {_vm.LastDurationText}";
            LoadingText.Visibility = _vm.IsSearching ? Visibility.Visible : Visibility.Collapsed;

            ResultsPanel.Children.Clear();
            if (_vm.State == KnowledgeSearchUiState.Found)
            {
                foreach (var group in _vm.Groups)
                {
                    ResultsPanel.Children.Add(BuildGroupHeader(group.DisplayName, group.Items.Count));
                    foreach (var item in group.Items)
                    {
                        ResultsPanel.Children.Add(BuildResultCard(item));
                    }
                }
            }
        }

        private UIElement BuildGroupHeader(string title, int count)
        {
            return new Border
            {
                Margin = new Thickness(0, 8, 0, 8),
                Child = new TextBlock
                {
                    Text = $"{title} ({count})",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("PrimaryTextBrush")
                }
            };
        }

        private UIElement BuildResultCard(KnowledgeSearchMatch match)
        {
            var card = new Border
            {
                Background = (Brush)FindResource("CardBackgroundBrush"),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10),
                Tag = match
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(match.Code) ? match.Title : $"[{match.Code}] {match.Title}",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            });
            left.Children.Add(new TextBlock
            {
                Text = $"{match.Type} · origem {match.SourceType}/{match.SourceId} · relevância {match.Relevance:0.##}",
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 4),
                Foreground = (Brush)FindResource("SecondaryTextBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            left.Children.Add(new TextBlock
            {
                Text = match.Summary,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var openBtn = new Button
            {
                Content = "Abrir origem",
                MinWidth = 110,
                Height = 34,
                Margin = new Thickness(12, 0, 0, 0),
                Style = (Style)FindResource("SecondaryButton"),
                Tag = match,
                VerticalAlignment = VerticalAlignment.Top
            };
            System.Windows.Automation.AutomationProperties.SetName(openBtn, $"Abrir origem {match.Code}");
            openBtn.Click += (_, __) => _vm.OpenSource(match, this);
            Grid.SetColumn(openBtn, 1);
            grid.Children.Add(openBtn);

            card.Child = grid;
            return card;
        }

        /// <summary>UiSmoke helper — set query + search without clicking all buttons.</summary>
        public Task SmokeSearchAsync(string query)
        {
            SearchTextBox.Text = query ?? string.Empty;
            return RunSearchAsync();
        }

        public void SmokeClear()
        {
            ClearButton_Click(this, new RoutedEventArgs());
        }
    }
}

