using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models.AI;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimoAutoEletrica.UserControls
{
    public partial class CopilotFlyoutPanel : UserControl
    {
        private readonly IAIService _aiService;
        private readonly INavigationService? _navigationService;
        private readonly AIToolRegistry _toolRegistry;
        private readonly List<AIChatMessage> _messages = new();
        private bool _isProcessing;

        public event EventHandler? CloseRequested;

        public CopilotFlyoutPanel()
        {
            InitializeComponent();

            _toolRegistry = App.Services?.GetService<AIToolRegistry>()
                ?? new AIToolRegistry();
            _aiService = App.Services?.GetService<IAIService>()
                ?? new DeterministicFallbackAIService(_toolRegistry);
            _navigationService = App.Services?.GetService<INavigationService>();

            Loaded += CopilotFlyoutPanel_Loaded;
        }

        private void CopilotFlyoutPanel_Loaded(object sender, RoutedEventArgs e)
        {
            AtualizarBadgeEngine();

            if (_messages.Count == 0)
            {
                AdicionarMensagemBoasVindas();
            }

            InputTextBox.Focus();
        }

        private void AtualizarBadgeEngine()
        {
            if (_aiService.IsOnlineAvailable)
            {
                EngineBadgeText.Text = "Gemini Nuvem";
                EngineBadge.BorderBrush = FindResource("AccentBrush") as Brush;
                EngineBadgeText.Foreground = FindResource("AccentBrush") as Brush;
            }
            else
            {
                EngineBadgeText.Text = "Offline Técnico";
                EngineBadge.BorderBrush = FindResource("SuccessBrush") as Brush;
                EngineBadgeText.Foreground = FindResource("SuccessBrush") as Brush;
            }
        }

        private void AdicionarMensagemBoasVindas()
        {
            var msg = new AIChatMessage
            {
                Role = AIRole.Assistant,
                SenderName = "PRIMOX Copilot",
                Content = "Olá! Sou seu copiloto de diagnóstico elétrico e operação da oficina.\n\nDigite uma dúvida técnica (ex: 'DTC P0562', 'Como testar fuga de corrente?'), ou consulte o sistema (ex: 'Peças em falta', 'Ferramentas em uso').",
                SuggestedActions = new List<AISuggestedAction>
                {
                    new AISuggestedAction { Label = "⚡ DTC P0562", ActionType = "ExecuteQuery", Parameter = "DTC P0562" },
                    new AISuggestedAction { Label = "🔋 Consumo Parasita", ActionType = "ExecuteQuery", Parameter = "Como testar consumo parasita?" },
                    new AISuggestedAction { Label = "📦 Peças em Falta", ActionType = "ExecuteQuery", Parameter = "Quais produtos estão em falta?" },
                    new AISuggestedAction { Label = "🧰 Ferramentas em Uso", ActionType = "ExecuteQuery", Parameter = "Quem está com ferramentas em uso?" }
                }
            };

            _messages.Add(msg);
            RenderizarMensagem(msg);
        }

        private async void EnviarButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessarEnvioAsync();
        }

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                e.Handled = true;
                await ProcessarEnvioAsync();
            }
        }

        private async Task ProcessarEnvioAsync()
        {
            if (_isProcessing) return;

            var texto = InputTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(texto)) return;

            InputTextBox.Text = string.Empty;

            var userMsg = new AIChatMessage
            {
                Role = AIRole.User,
                Content = texto,
                Timestamp = DateTime.Now
            };

            _messages.Add(userMsg);
            RenderizarMensagem(userMsg);

            _isProcessing = true;
            ThinkingPanel.Visibility = Visibility.Visible;
            EnviarButton.IsEnabled = false;

            try
            {
                var request = new AIChatRequest
                {
                    Messages = new List<AIChatMessage>(_messages)
                };

                var response = await _aiService.ProcessarMensagemAsync(request);

                var assistantMsg = new AIChatMessage
                {
                    Role = AIRole.Assistant,
                    SenderName = response.ProviderUsed,
                    Content = response.Message,
                    SuggestedActions = response.SuggestedActions,
                    Timestamp = DateTime.Now
                };

                _messages.Add(assistantMsg);
                RenderizarMensagem(assistantMsg);
            }
            catch (Exception ex)
            {
                var errorMsg = new AIChatMessage
                {
                    Role = AIRole.Assistant,
                    SenderName = "Sistema",
                    Content = $"❌ Ocorreu uma instabilidade ao processar a consulta:\n{ex.Message}",
                    IsError = true,
                    Timestamp = DateTime.Now
                };
                _messages.Add(errorMsg);
                RenderizarMensagem(errorMsg);
            }
            finally
            {
                _isProcessing = false;
                ThinkingPanel.Visibility = Visibility.Collapsed;
                EnviarButton.IsEnabled = true;
                InputTextBox.Focus();
            }
        }

        private void RenderizarMensagem(AIChatMessage msg)
        {
            var isUser = msg.Role == AIRole.User;

            var bubble = new Border
            {
                CornerRadius = isUser ? new CornerRadius(14, 14, 2, 14) : new CornerRadius(14, 14, 14, 2),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = isUser ? new Thickness(48, 4, 0, 8) : new Thickness(0, 4, 48, 8),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Background = (Brush)FindResource(isUser ? "BrandBrush" : "SurfaceAltBrush"),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                BorderThickness = isUser ? new Thickness(0) : new Thickness(1)
            };

            var contentStack = new StackPanel();

            if (!isUser && !string.IsNullOrWhiteSpace(msg.SenderName))
            {
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                headerPanel.Children.Add(new TextBlock
                {
                    Text = $"🤖 {msg.SenderName}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("AccentBrush")
                });
                contentStack.Children.Add(headerPanel);
            }

            var textBlock = new TextBlock
            {
                Text = msg.Content,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = isUser
                    ? (Brush)FindResource("AccentButtonTextBrush")
                    : (Brush)FindResource("PrimaryTextBrush")
            };
            contentStack.Children.Add(textBlock);

            var timeText = new TextBlock
            {
                Text = msg.Timestamp.ToString("HH:mm"),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = isUser
                    ? new SolidColorBrush(Color.FromArgb(180, 255, 255, 255))
                    : (Brush)FindResource("MutedTextBrush")
            };
            contentStack.Children.Add(timeText);

            // Botoes de Acao Sugerida
            if (msg.SuggestedActions != null && msg.SuggestedActions.Count > 0)
            {
                var actionsWrap = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
                foreach (var action in msg.SuggestedActions)
                {
                    var btn = new Button
                    {
                        Content = action.Label,
                        Style = (Style)FindResource("FilterActionButton"),
                        Margin = new Thickness(0, 0, 6, 6),
                        Padding = new Thickness(8, 4, 8, 4),
                        FontSize = 11,
                        Tag = action
                    };
                    btn.Click += ActionButton_Click;
                    actionsWrap.Children.Add(btn);
                }
                contentStack.Children.Add(actionsWrap);
            }

            bubble.Child = contentStack;
            MessagesPanel.Children.Add(bubble);

            ChatScrollViewer.ScrollToEnd();
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is AISuggestedAction action)
            {
                switch (action.ActionType)
                {
                    case "Navigate":
                        _navigationService?.Navigate(action.Parameter);
                        break;

                    case "SearchStock":
                        _navigationService?.Navigate("Estoque");
                        break;

                    case "ExecuteQuery":
                        InputTextBox.Text = action.Parameter;
                        await ProcessarEnvioAsync();
                        break;

                    case "CopyText":
                        Clipboard.SetText(action.Parameter);
                        MessageBox.Show("Texto copiado para a área de transferência!", "Copilot", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
        }

        private async void PromptChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string prompt)
            {
                InputTextBox.Text = prompt;
                await ProcessarEnvioAsync();
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void LimparChatButton_Click(object sender, RoutedEventArgs e)
        {
            _messages.Clear();
            MessagesPanel.Children.Clear();
            AdicionarMensagemBoasVindas();
        }
    }
}
