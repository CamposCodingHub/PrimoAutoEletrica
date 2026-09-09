using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PrimoAutoEletrica.UserControls
{
    /// <summary>Escola PRIMOX — ajuda para quem acabou de comprar e treina a equipe sozinho.</summary>
    public partial class HelpControl : UserControl
    {
        private readonly Dictionary<string, HelpTopic> _topics;
        private readonly Dictionary<string, string> _treeHeaders = new();
        private int _animIndex;
        private string? _currentTopicTag;

        public HelpControl()
        {
            InitializeComponent();
            _topics = HelpTopicsCatalog.Create();
            CacheTreeHeaders(HelpTreeView.Items);
            Loaded += (_, _) =>
            {
                if (string.IsNullOrEmpty(_currentTopicTag))
                    NavigateToTopic("comece-aqui");
            };
        }

        private void CacheTreeHeaders(ItemCollection items)
        {
            foreach (var obj in items)
            {
                if (obj is not TreeViewItem item) continue;
                if (item.Tag is string tag && item.Header is string header)
                    _treeHeaders[tag] = header;
                CacheTreeHeaders(item.Items);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text?.Trim() ?? "";
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(q) ? Visibility.Visible : Visibility.Collapsed;
            FilterTree(HelpTreeView.Items, q);
        }

        private bool FilterTree(ItemCollection items, string q)
        {
            var any = false;
            foreach (var obj in items)
            {
                if (obj is not TreeViewItem item) continue;
                var child = FilterTree(item.Items, q);
                var match = string.IsNullOrEmpty(q)
                    || (item.Header?.ToString()?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                    || MatchesTopic(item.Tag?.ToString(), q);
                var vis = string.IsNullOrEmpty(q) || match || child;
                item.Visibility = vis ? Visibility.Visible : Visibility.Collapsed;
                if (vis && !string.IsNullOrEmpty(q) && child) item.IsExpanded = true;
                any |= vis;
            }
            return any;
        }

        private bool MatchesTopic(string? tag, string q)
        {
            if (string.IsNullOrEmpty(tag) || !_topics.TryGetValue(tag, out var topic)) return false;
            if (topic.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) return true;
            if (topic.Purpose?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) return true;
            return topic.Sections.Any(s =>
                (s.Heading?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Content?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Steps?.Any(x => x.Contains(q, StringComparison.OrdinalIgnoreCase)) ?? false) ||
                (s.BulletPoints?.Any(x => x.Contains(q, StringComparison.OrdinalIgnoreCase)) ?? false) ||
                (s.ErrorFixes?.Any(ef => ef.Error.Contains(q, StringComparison.OrdinalIgnoreCase) || ef.Solution.Contains(q, StringComparison.OrdinalIgnoreCase)) ?? false));
        }

        private void HelpTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem { Tag: string tag } && _topics.ContainsKey(tag))
                NavigateToTopic(tag);
        }

        public void NavigateToTopic(string topicTag)
        {
            if (string.IsNullOrEmpty(topicTag) || !_topics.TryGetValue(topicTag, out var topic)) return;
            _currentTopicTag = topicTag;
            DisplayTopic(topicTag, topic);
            SelectTreeItem(HelpTreeView.Items, topicTag);
            ContentScroll?.ScrollToHome();
        }

        /// <summary>Reaplica o tópico atual após troca de tema (elementos criados em código).</summary>
        public void RefreshThemeBoundContent()
        {
            if (!string.IsNullOrEmpty(_currentTopicTag) && _topics.ContainsKey(_currentTopicTag))
                NavigateToTopic(_currentTopicTag);
        }

        private bool SelectTreeItem(ItemCollection items, string tag)
        {
            foreach (var obj in items)
            {
                if (obj is not TreeViewItem item) continue;
                if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsSelected = true;
                    item.BringIntoView();
                    return true;
                }
                if (SelectTreeItem(item.Items, tag))
                {
                    item.IsExpanded = true;
                    return true;
                }
            }
            return false;
        }

        private void DisplayTopic(string tag, HelpTopic topic)
        {
            ContentArea.Children.Clear();
            _animIndex = 0;

            var crumb = _treeHeaders.TryGetValue(tag, out var h) ? h : topic.Title;
            HeroBreadcrumb.Text = $"Ajuda  ›  {crumb}";
            HeroTitle.Text = topic.Title;
            HeroSubtitle.Text = topic.Purpose ?? "";

            if (!string.IsNullOrEmpty(topic.Purpose))
                Add(CreateCallout("important", "Em uma frase", topic.Purpose));

            if (topic.QuickLinks?.Length > 0)
            {
                Add(Title("Comece por estes atalhos"));
                Add(CreateQuickLinks(topic.QuickLinks));
            }

            if (topic.PageGuides?.Length > 0)
            {
                Add(CreateLegend());
                Add(Body("Clique na seta do card para abrir o conteúdo INTEIRO."));
                foreach (var p in topic.PageGuides)
                    Add(CreatePageExpander(p));
            }

            foreach (var section in topic.Sections)
            {
                if (!string.IsNullOrEmpty(section.Heading))
                    Add(Title(section.Heading));
                if (!string.IsNullOrEmpty(section.Content))
                    Add(Body(section.Content));
                if (section.Steps?.Length > 0)
                    Add(CreateSteps(section.Steps));
                if (section.BulletPoints != null)
                    foreach (var b in section.BulletPoints)
                        Add(Bullet(b));
                if (section.ErrorFixes != null)
                    foreach (var ef in section.ErrorFixes)
                        Add(CreateErrorCard(ef));
                if (!string.IsNullOrEmpty(section.ScreenshotKey))
                    Add(CreateMock(section.ScreenshotKey, section.ScreenshotCaption));
                if (section.Callouts != null)
                    foreach (var c in section.Callouts)
                        Add(CreateCallout(c.Kind, c.Title, c.Message));
                if (section.KeyboardShortcuts != null)
                    Add(Wrap(CreateShortcuts(section.KeyboardShortcuts)));
            }

            if (topic.Callouts != null)
                foreach (var c in topic.Callouts)
                    Add(CreateCallout(c.Kind, c.Title, c.Message));

            if (!string.IsNullOrEmpty(topic.Tip))
                Add(CreateCallout("important", "Dica de ouro", topic.Tip));

            if (topic.RelatedTopics?.Length > 0)
            {
                Add(Title("Leia também"));
                var wrap = new WrapPanel();
                foreach (var r in topic.RelatedTopics)
                {
                    if (!_topics.TryGetValue(r, out var rt)) continue;
                    var chipText = new TextBlock
                    {
                        Text = rt.Title,
                        FontSize = 12.5,
                        FontWeight = FontWeights.SemiBold
                    };
                    Bind(chipText, TextBlock.ForegroundProperty, "PrimaryBrush");
                    var chip = new Border
                    {
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(18),
                        Padding = new Thickness(14, 7, 14, 7),
                        Margin = new Thickness(0, 0, 8, 8),
                        Cursor = Cursors.Hand,
                        Child = chipText
                    };
                    Bind(chip, Border.BackgroundProperty, "SurfaceAltBrush");
                    Bind(chip, Border.BorderBrushProperty, "BorderBrush");
                    var local = r;
                    chip.MouseLeftButtonDown += (_, _) => NavigateToTopic(local);
                    wrap.Children.Add(chip);
                }
                Add(wrap);
            }
        }

        private void Add(UIElement el)
        {
            ContentArea.Children.Add(el);
            el.Opacity = 0;
            el.RenderTransform = new TranslateTransform(0, 8);
            var delay = TimeSpan.FromMilliseconds(Math.Min(_animIndex++ * 24, 200));
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            el.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { BeginTime = delay, EasingFunction = ease });
            ((TranslateTransform)el.RenderTransform).BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(180)) { BeginTime = delay, EasingFunction = ease });
        }

        private TextBlock Title(string text) => Tb(text, 17, FontWeights.SemiBold, "PrimaryTextBrush", 0, 18, 0, 10);
        private TextBlock Body(string text) => Tb(text, 14, FontWeights.Normal, "SecondaryTextBrush", 0, 0, 0, 10, true);

        private UIElement CreateQuickLinks(string[] tags)
        {
            var wrap = new WrapPanel();
            foreach (var tag in tags)
            {
                if (!_topics.TryGetValue(tag, out var topic)) continue;
                var accent = new Border
                {
                    Width = 28, Height = 4, CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                Bind(accent, Border.BackgroundProperty, "PrimaryBrush");

                var title = new TextBlock
                {
                    Text = topic.Title, FontSize = 13.5, FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                };
                Bind(title, TextBlock.ForegroundProperty, "PrimaryTextBrush");

                var card = new Border
                {
                    Width = 200,
                    Margin = new Thickness(0, 0, 12, 12),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(16),
                    Cursor = Cursors.Hand,
                    Child = new StackPanel { Children = { accent, title } }
                };
                Bind(card, Border.BackgroundProperty, "CardBackgroundBrush");
                Bind(card, Border.BorderBrushProperty, "BorderBrush");
                var local = tag;
                card.MouseLeftButtonDown += (_, _) => NavigateToTopic(local);
                wrap.Children.Add(card);
            }
            return wrap;
        }

        private Expander CreatePageExpander(HelpPageGuide page)
        {
            var header = new StackPanel { Margin = new Thickness(2) };
            header.Children.Add(Tb(page.PageName, 15, FontWeights.SemiBold, "PrimaryTextBrush"));
            header.Children.Add(Tb(page.Summary, 12.5, FontWeights.Normal, "SecondaryTextBrush", 0, 4, 0, 0, true));
            var badges = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            if (page.Important?.Length > 0) badges.Children.Add(Badge($"{page.Important.Length} importante", "InfoBrush", "InfoCardBackgroundBrush"));
            if (page.Cautions?.Length > 0) badges.Children.Add(Badge($"{page.Cautions.Length} atenção", "WarningBrush", "WarningCardBackgroundBrush"));
            if (page.Dangers?.Length > 0) badges.Children.Add(Badge($"{page.Dangers.Length} risco", "DangerBrush", "DangerCardBackgroundBrush"));
            header.Children.Add(badges);

            var details = new StackPanel { Margin = new Thickness(6, 8, 6, 10) };
            details.Children.Add(Tb("O que tem nesta tela", 13, FontWeights.SemiBold, "PrimaryTextBrush", 0, 0, 0, 6));
            if (page.YouWillFind != null)
                foreach (var x in page.YouWillFind) details.Children.Add(Bullet(x));
            if (page.Important != null)
                foreach (var m in page.Important) details.Children.Add(CreateCallout("important", "Importante", m));
            if (page.Cautions != null)
                foreach (var m in page.Cautions) details.Children.Add(CreateCallout("caution", "Atenção", m));
            if (page.Dangers != null)
                foreach (var m in page.Dangers) details.Children.Add(CreateCallout("danger", "Cuidado", m));
            if (!string.IsNullOrEmpty(page.RelatedTopicTag) && _topics.ContainsKey(page.RelatedTopicTag))
            {
                var link = Tb("Abrir guia completo →", 13, FontWeights.SemiBold, "PrimaryBrush", 0, 10, 0, 0);
                link.Cursor = Cursors.Hand;
                var dest = page.RelatedTopicTag!;
                link.MouseLeftButtonDown += (_, e) => { e.Handled = true; NavigateToTopic(dest); };
                details.Children.Add(link);
            }

            var expander = new Expander
            {
                Header = header,
                Content = details,
                IsExpanded = false,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 12, 14, 8),
                Margin = new Thickness(0, 0, 0, 12)
            };
            Bind(expander, Control.BackgroundProperty, "CardBackgroundBrush");
            Bind(expander, Control.BorderBrushProperty,
                page.Dangers?.Length > 0 ? "DangerBrush"
                : page.Cautions?.Length > 0 ? "WarningBrush"
                : "BorderBrush");
            expander.Expanded += (_, _) =>
            {
                if (expander.Content is FrameworkElement fe) fe.Height = double.NaN;
                expander.BringIntoView();
            };
            return expander;
        }

        private Border CreateSteps(string[] steps)
        {
            var panel = new StackPanel();
            for (var i = 0; i < steps.Length; i++)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 12) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var number = new TextBlock
                {
                    Text = (i + 1).ToString(), FontSize = 14, FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Bind(number, TextBlock.ForegroundProperty, "InverseTextBrush");
                var badge = new Border
                {
                    Width = 34, Height = 34, CornerRadius = new CornerRadius(17),
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = number
                };
                Bind(badge, Border.BackgroundProperty, "PrimaryBrush");
                var text = Tb(steps[i], 14, FontWeights.Normal, "PrimaryTextBrush", 14, 6, 0, 0, true);
                Grid.SetColumn(badge, 0);
                Grid.SetColumn(text, 1);
                row.Children.Add(badge);
                row.Children.Add(text);
                panel.Children.Add(row);
            }
            return Wrap(panel);
        }

        private Border CreateErrorCard(HelpErrorFix fix)
        {
            var stack = new StackPanel();
            stack.Children.Add(Tb("ERRO: " + fix.Error, 14, FontWeights.Bold, "DangerBrush"));
            stack.Children.Add(Tb("Por que dói: " + fix.Impact, 13, FontWeights.Normal, "SecondaryTextBrush", 0, 6, 0, 0, true));
            stack.Children.Add(CreateCallout("important", "O que fazer agora", fix.Solution));
            var card = new Border
            {
                BorderThickness = new Thickness(4, 1, 1, 1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12),
                Child = stack
            };
            Bind(card, Border.BackgroundProperty, "CardBackgroundBrush");
            Bind(card, Border.BorderBrushProperty, "DangerBrush");
            return card;
        }

        private UIElement CreateLegend()
        {
            var w = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            w.Children.Add(Badge("Importante", "InfoBrush", "InfoCardBackgroundBrush"));
            w.Children.Add(Badge("Atenção", "WarningBrush", "WarningCardBackgroundBrush"));
            w.Children.Add(Badge("Risco", "DangerBrush", "DangerCardBackgroundBrush"));
            return w;
        }

        private Border Badge(string text, string fg, string bg)
        {
            var label = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold };
            Bind(label, TextBlock.ForegroundProperty, fg);
            var badge = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 3, 10, 3),
                Margin = new Thickness(0, 0, 8, 4),
                Child = label
            };
            Bind(badge, Border.BackgroundProperty, bg);
            Bind(badge, Border.BorderBrushProperty, fg);
            return badge;
        }

        private Border CreateCallout(string kind, string title, string message)
        {
            var (accentKey, bgKey) = kind.ToLowerInvariant() switch
            {
                "danger" => ("DangerBrush", "DangerCardBackgroundBrush"),
                "caution" or "warning" => ("WarningBrush", "WarningCardBackgroundBrush"),
                _ => ("InfoBrush", "InfoCardBackgroundBrush")
            };
            var titleBlock = new TextBlock
            {
                Text = title.ToUpperInvariant(), FontSize = 11, FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Bind(titleBlock, TextBlock.ForegroundProperty, accentKey);
            var body = new TextBlock
            {
                Text = message, FontSize = 14, TextWrapping = TextWrapping.Wrap, LineHeight = 22
            };
            Bind(body, TextBlock.ForegroundProperty, "PrimaryTextBrush");
            var card = new Border
            {
                BorderThickness = new Thickness(4, 1, 1, 1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 8, 0, 6),
                Child = new StackPanel { Children = { titleBlock, body } }
            };
            Bind(card, Border.BackgroundProperty, bgKey);
            Bind(card, Border.BorderBrushProperty, accentKey);
            return card;
        }

        private UIElement Bullet(string text)
        {
            var row = new DockPanel { Margin = new Thickness(4, 4, 0, 4) };
            var dot = new Ellipse { Width = 7, Height = 7, Margin = new Thickness(4, 8, 10, 0), VerticalAlignment = VerticalAlignment.Top };
            Bind(dot, Shape.FillProperty, "PrimaryBrush");
            DockPanel.SetDock(dot, Dock.Left);
            row.Children.Add(dot);
            row.Children.Add(Tb(text, 14, FontWeights.Normal, "PrimaryTextBrush", 0, 0, 0, 0, true));
            return row;
        }

        private Border Wrap(UIElement child)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 4, 0, 10),
                Child = child
            };
            Bind(border, Border.BackgroundProperty, "CardBackgroundBrush");
            Bind(border, Border.BorderBrushProperty, "BorderBrush");
            return border;
        }

        private Border CreateMock(string key, string? caption)
        {
            var frame = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Margin = new Thickness(0, 8, 0, 14),
                ClipToBounds = true
            };
            Bind(frame, Border.BackgroundProperty, "SurfaceAltBrush");
            Bind(frame, Border.BorderBrushProperty, "BorderBrush");

            var stack = new StackPanel();
            var dock = new DockPanel();
            var badgeLabel = new TextBlock { Text = "EXEMPLO DE TELA", FontSize = 10, FontWeight = FontWeights.Bold };
            Bind(badgeLabel, TextBlock.ForegroundProperty, "PrimaryBrush");
            var badge = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Child = badgeLabel
            };
            Bind(badge, Border.BackgroundProperty, "PrimaryBackgroundBrush");
            DockPanel.SetDock(badge, Dock.Right);
            dock.Children.Add(badge);
            var captionTitle = new TextBlock
            {
                Text = caption ?? "Exemplo", FontSize = 12, FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0)
            };
            Bind(captionTitle, TextBlock.ForegroundProperty, "SecondaryTextBrush");
            dock.Children.Add(captionTitle);
            var chrome = new Border
            {
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 8, 12, 8),
                Child = dock
            };
            Bind(chrome, Border.BackgroundProperty, "CardBackgroundBrush");
            Bind(chrome, Border.BorderBrushProperty, "BorderBrush");
            stack.Children.Add(chrome);
            stack.Children.Add(new Border { Padding = new Thickness(14), Child = BuildMock(key) });
            if (!string.IsNullOrEmpty(caption))
            {
                var footText = new TextBlock
                {
                    Text = caption, FontSize = 12, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap
                };
                Bind(footText, TextBlock.ForegroundProperty, "MutedTextBrush");
                var foot = new Border
                {
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Padding = new Thickness(14, 10, 14, 10),
                    Child = footText
                };
                Bind(foot, Border.BackgroundProperty, "CardBackgroundBrush");
                Bind(foot, Border.BorderBrushProperty, "BorderBrush");
                stack.Children.Add(foot);
            }
            frame.Child = stack;
            return frame;
        }

        private UIElement BuildMock(string key) => key switch
        {
            "clientes" => MockLines("Clientes", "João Silva · Ativo", "Maria Souza · Ativo"),
            "veiculos" => MockLines("Veículo", "Placa ABC1D23", "Cliente João Silva"),
            "orcamentos" => MockLines("Orçamentos", "ORC-1042 Pendente", "ORC-1041 Aprovado"),
            "os" => MockLines("OS", "OS-221 Em andamento", "OS-220 Aguardando peça"),
            "pdv" => MockLines("PDV", "Total R$ 420,00", "Pagamento PIX"),
            "dashboard" => MockLines("Dashboard", "Faturamento R$ 48.200", "OS abertas 14"),
            "estoque" => MockLines("Estoque", "Bateria 60Ah · 12", "Farol H4 · 3 (baixo)"),
            "financeiro" => MockLines("Financeiro", "A receber R$ 12.450", "A pagar R$ 4.820"),
            "relatorios" => MockLines("Relatório", "Faturamento · PDF", "01–30/09/2026"),
            "kanban" => MockLines("Kanban", "Aberta · OS-221", "Aguardando · OS-220"),
            "agendamentos" => MockLines("Agenda", "09:00 João", "11:30 Maria"),
            "nfe" => MockLines("NF-e", "XML selecionado", "18 itens"),
            _ => MockLines("Exemplo", "Campo · Valor")
        };

        private UIElement MockLines(string title, params string[] lines)
        {
            var p = new StackPanel();
            p.Children.Add(Tb(title, 14, FontWeights.SemiBold, "PrimaryTextBrush", 0, 0, 0, 8));
            foreach (var line in lines)
            {
                var text = new TextBlock { Text = line, FontSize = 13 };
                Bind(text, TextBlock.ForegroundProperty, "PrimaryTextBrush");
                var row = new Border
                {
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(10, 8, 10, 8),
                    Child = text
                };
                Bind(row, Border.BackgroundProperty, "CardBackgroundBrush");
                Bind(row, Border.BorderBrushProperty, "BorderBrush");
                p.Children.Add(row);
            }
            return p;
        }

        private Grid CreateShortcuts(Dictionary<string, string> map)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var row = 0;
            foreach (var kv in map)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                var bgKey = row % 2 == 0 ? "SurfaceAltBrush" : "CardBackgroundBrush";
                var leftText = new TextBlock
                {
                    Text = kv.Key, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.SemiBold
                };
                Bind(leftText, TextBlock.ForegroundProperty, "PrimaryBrush");
                var left = new Border { Padding = new Thickness(12, 8, 0, 8), Child = leftText };
                Bind(left, Border.BackgroundProperty, bgKey);

                var rightText = new TextBlock { Text = kv.Value };
                Bind(rightText, TextBlock.ForegroundProperty, "PrimaryTextBrush");
                var right = new Border { Padding = new Thickness(12, 8, 0, 8), Child = rightText };
                Bind(right, Border.BackgroundProperty, bgKey);

                Grid.SetRow(left, row); Grid.SetColumn(left, 0);
                Grid.SetRow(right, row); Grid.SetColumn(right, 1);
                grid.Children.Add(left); grid.Children.Add(right);
                row++;
            }
            return grid;
        }

        private TextBlock Tb(string text, double size, FontWeight weight, string brush,
            double l = 0, double t = 0, double r = 0, double b = 0, bool wrap = true)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = size, FontWeight = weight,
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                LineHeight = size * 1.55, Margin = new Thickness(l, t, r, b)
            };
            Bind(tb, TextBlock.ForegroundProperty, brush);
            return tb;
        }

        /// <summary>
        /// DynamicResource via code — brushes seguem troca Light/Dark.
        /// Não usar FindResource/snapshot (congelava cores do tema anterior).
        /// </summary>
        private static void Bind(FrameworkElement element, DependencyProperty property, string resourceKey)
            => element.SetResourceReference(property, resourceKey);
    }

    public class HelpTopic
    {
        public string Title { get; set; } = "";
        public string? Purpose { get; set; }
        public string[]? QuickLinks { get; set; }
        public HelpSection[] Sections { get; set; } = Array.Empty<HelpSection>();
        public HelpPageGuide[]? PageGuides { get; set; }
        public HelpCallout[]? Callouts { get; set; }
        public string? Tip { get; set; }
        public string[]? RelatedTopics { get; set; }
    }

    public class HelpSection
    {
        public string? Heading { get; set; }
        public string? Content { get; set; }
        public string[]? BulletPoints { get; set; }
        public string[]? Steps { get; set; }
        public HelpErrorFix[]? ErrorFixes { get; set; }
        public HelpCallout[]? Callouts { get; set; }
        public string? ScreenshotKey { get; set; }
        public string? ScreenshotCaption { get; set; }
        public Dictionary<string, string>? KeyboardShortcuts { get; set; }
    }

    public class HelpCallout
    {
        public string Kind { get; set; } = "important";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class HelpPageGuide
    {
        public string PageName { get; set; } = "";
        public string Summary { get; set; } = "";
        public string[]? YouWillFind { get; set; }
        public string[]? Important { get; set; }
        public string[]? Cautions { get; set; }
        public string[]? Dangers { get; set; }
        public string? RelatedTopicTag { get; set; }
    }

    public class HelpErrorFix
    {
        public string Error { get; set; } = "";
        public string Impact { get; set; } = "";
        public string Solution { get; set; } = "";
    }
}
