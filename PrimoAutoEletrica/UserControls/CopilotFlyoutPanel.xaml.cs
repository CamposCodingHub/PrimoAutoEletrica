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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimoAutoEletrica.UserControls
{
    public partial class CopilotFlyoutPanel : UserControl
    {
        private readonly IAIService _aiService;
        private readonly INavigationService? _navigationService;
        private readonly AIToolRegistry _toolRegistry;
        private readonly IWorkOrderAiBridgeService? _bridgeService;
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
            _bridgeService = App.Services?.GetService<IWorkOrderAiBridgeService>();

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
                var accentBrush = (TryFindResource("AccentBrush") as Brush) 
                    ?? (TryFindResource("BrandBrush") as Brush) 
                    ?? Brushes.DarkOrange;
                EngineBadge.BorderBrush = accentBrush;
                EngineBadgeText.Foreground = accentBrush;
            }
            else
            {
                EngineBadgeText.Text = "Offline Técnico";
                var successBrush = (TryFindResource("SuccessBrush") as Brush) ?? Brushes.ForestGreen;
                EngineBadge.BorderBrush = successBrush;
                EngineBadgeText.Foreground = successBrush;
            }
        }

        private void AdicionarMensagemBoasVindas()
        {
            var msg = new AIChatMessage
            {
                Role = AIRole.Assistant,
                SenderName = "PRIMOX Copilot",
                Content = "Olá! Sou o PRIMOX Copilot, seu assistente inteligente especializado em Auto Elétrica e no sistema da oficina.\n\n📚 Tenho manuais técnicos completos (partida pesada, tec-tec, alternador, fuga de carga, ventoinha, curtos, relés) e manuais operacionais de todas as telas do PRIMOX (Ordens de Serviço, PDV/Caixa, Orçamentos, Estoque, Ferramentas e NF-e).\n\nComo posso ajudar você agora?",
                SuggestedActions = new List<AISuggestedAction>
                {
                    new AISuggestedAction { Label = "🔑 Defeito: Tec-Tec", ActionType = "ExecuteQuery", Parameter = "Carro faz tec tec e não liga" },
                    new AISuggestedAction { Label = "📘 Manual: Abrir OS", ActionType = "ExecuteQuery", Parameter = "Como abrir uma nova Ordem de Serviço?" },
                    new AISuggestedAction { Label = "🔋 Consumo Parasita", ActionType = "ExecuteQuery", Parameter = "Como testar consumo parasita?" },
                    new AISuggestedAction { Label = "🛒 Manual: Fechar Caixa", ActionType = "ExecuteQuery", Parameter = "Como funciona o PDV e fechar o caixa?" },
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

        private async void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Shift+Enter insere quebra de linha no texto
                    int caretIndex = InputTextBox.CaretIndex;
                    InputTextBox.Text = InputTextBox.Text.Insert(caretIndex, Environment.NewLine);
                    InputTextBox.CaretIndex = caretIndex + Environment.NewLine.Length;
                    e.Handled = true;
                }
                else
                {
                    // Enter direto envia a mensagem imediatamente!
                    e.Handled = true;
                    await ProcessarEnvioAsync();
                }
            }
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
                    ProposedItems = response.ProposedItems,
                    Timestamp = DateTime.Now
                };

                // Análise automática de ponte IA ➔ OS se houver serviço disponível e propostas
                if (_bridgeService != null && (assistantMsg.ProposedItems == null || assistantMsg.ProposedItems.Count == 0))
                {
                    try
                    {
                        var propostas = await _bridgeService.AnalisarTextoEProporItensAsync(texto + " " + response.Message);
                        if (propostas.Count > 0)
                        {
                            assistantMsg.ProposedItems = propostas;
                        }
                    }
                    catch { }
                }

                // Se houver itens propostos e não houver ainda o botão de inserir na OS, adiciona ação sugerida
                if (assistantMsg.ProposedItems != null && assistantMsg.ProposedItems.Count > 0)
                {
                    if (!assistantMsg.SuggestedActions.Any(a => a.ActionType == "InsertWorkOrderItem"))
                    {
                        assistantMsg.SuggestedActions.Insert(0, new AISuggestedAction
                        {
                            Label = $"🛒 Inserir {assistantMsg.ProposedItems.Count} itens na OS",
                            ActionType = "InsertWorkOrderItem"
                        });
                    }
                }

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
                CornerRadius = isUser ? new CornerRadius(16, 16, 4, 16) : new CornerRadius(16, 16, 16, 4),
                Padding = new Thickness(14, 11, 14, 11),
                Margin = isUser ? new Thickness(48, 5, 0, 9) : new Thickness(0, 5, 48, 9),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Background = isUser 
                    ? ((TryFindResource("BrandBrush") as Brush) ?? Brushes.DarkOrange)
                    : ((TryFindResource("CardBackgroundBrush") as Brush) ?? (TryFindResource("SurfaceBrush") as Brush) ?? Brushes.White),
                BorderBrush = (TryFindResource("BorderBrush") as Brush) ?? Brushes.LightGray,
                BorderThickness = isUser ? new Thickness(0) : new Thickness(1)
            };

            var contentStack = new StackPanel();

            if (!isUser && !string.IsNullOrWhiteSpace(msg.SenderName))
            {
                var headerPanel = new Border
                {
                    Padding = new Thickness(6, 2, 8, 2),
                    CornerRadius = new CornerRadius(6),
                    Background = (TryFindResource("BrandSoftBrush") as Brush) ?? (TryFindResource("SurfaceAltBrush") as Brush) ?? Brushes.GhostWhite,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var headerText = new TextBlock
                {
                    Text = $"🤖 {msg.SenderName}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = (TryFindResource("BrandBrush") as Brush) ?? Brushes.DarkOrange
                };
                headerPanel.Child = headerText;
                contentStack.Children.Add(headerPanel);
            }

            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 19,
                Foreground = isUser
                    ? ((TryFindResource("AccentButtonTextBrush") as Brush) ?? Brushes.White)
                    : ((TryFindResource("PrimaryTextBrush") as Brush) ?? Brushes.Black)
            };
            PreencherInlinesFormatados(textBlock, msg.Content, isUser);
            contentStack.Children.Add(textBlock);

            var timeText = new TextBlock
            {
                Text = msg.Timestamp.ToString("HH:mm"),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = isUser
                    ? new SolidColorBrush(Color.FromArgb(190, 255, 255, 255))
                    : ((TryFindResource("MutedTextBrush") as Brush) ?? Brushes.Gray)
            };
            contentStack.Children.Add(timeText);

            // Card de Proposta de Peças e Serviços para OS
            if (msg.ProposedItems != null && msg.ProposedItems.Count > 0)
            {
                var proposalCard = new Border
                {
                    Margin = new Thickness(0, 10, 0, 4),
                    Padding = new Thickness(10, 8, 10, 8),
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    Background = (TryFindResource("SurfaceAltBrush") as Brush) ?? Brushes.GhostWhite,
                    BorderBrush = (TryFindResource("BrandBrush") as Brush) ?? Brushes.DarkOrange
                };

                var proposalStack = new StackPanel();
                var titleText = new TextBlock
                {
                    Text = $"⚡ Sugestão OS ({msg.ProposedItems.Count} itens identificados)",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = (TryFindResource("BrandBrush") as Brush) ?? Brushes.DarkOrange,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                proposalStack.Children.Add(titleText);

                foreach (var item in msg.ProposedItems.Take(4))
                {
                    var itemText = new TextBlock
                    {
                        Text = $"• [{(item.Tipo == "Servico" ? "SERV" : "PEÇA")}] {item.Descricao} - {item.PrecoSugerido:C}",
                        FontSize = 10,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = (TryFindResource("PrimaryTextBrush") as Brush) ?? Brushes.Black,
                        Margin = new Thickness(0, 1, 0, 1)
                    };
                    proposalStack.Children.Add(itemText);
                }

                var btnInserirOS = new Button
                {
                    Content = "🛒 Inserir na OS (1-Click)",
                    Style = TryFindResource("FilterActionButton") as Style,
                    Margin = new Thickness(0, 6, 0, 0),
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                btnInserirOS.Click += (s, e) =>
                {
                    AbrirDialogoPonteOS(msg.ProposedItems);
                };
                proposalStack.Children.Add(btnInserirOS);

                proposalCard.Child = proposalStack;
                contentStack.Children.Add(proposalCard);
            }

            // Botoes de Acao Sugerida
            if (msg.SuggestedActions != null && msg.SuggestedActions.Count > 0)
            {
                var actionsWrap = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
                foreach (var action in msg.SuggestedActions)
                {
                    var btn = new Button
                    {
                        Content = action.Label,
                        Style = TryFindResource("FilterActionButton") as Style,
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
                    case "InsertWorkOrderItem":
                        AbrirDialogoPonteOS(_messages.LastOrDefault(m => m.ProposedItems != null && m.ProposedItems.Count > 0)?.ProposedItems);
                        break;

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

        private void AbrirDialogoPonteOS(List<AIPartOrServiceProposal>? itens)
        {
            if (itens == null || itens.Count == 0)
            {
                MessageBox.Show("Nenhuma peça ou serviço foi identificado nesta mensagem para vincular à OS.", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var bridgeService = _bridgeService ?? App.Services?.GetService<IWorkOrderAiBridgeService>() ?? new WorkOrderAiBridgeService(App.Services?.GetService<DatabaseService>()!);
            var dialog = new Views.SelecionarOrdemServicoDialog(bridgeService, itens)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.ItensInseridosComSucesso > 0)
            {
                var sysMsg = new AIChatMessage
                {
                    Role = AIRole.Assistant,
                    SenderName = "PRIMOX Copilot",
                    Content = $"✅ **Ponte OS Executada com Sucesso!**\n\n{dialog.ItensInseridosComSucesso} item(ns) foram vinculados e gravados na Ordem de Serviço **#{dialog.OrdemServicoDestino?.Numero}** ({dialog.OrdemServicoDestino?.ClienteNomeSnapshot}).\n\nO orçamento e os registros de auditoria foram atualizados no sistema.",
                    Timestamp = DateTime.Now
                };
                _messages.Add(sysMsg);
                RenderizarMensagem(sysMsg);
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

        private void ExpandirTelaCheiaButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            _navigationService?.Navigate("AiDiagnosticCenter");
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

        private static void PreencherInlinesFormatados(TextBlock textBlock, string markdownText, bool isUser)
        {
            if (string.IsNullOrEmpty(markdownText)) return;

            textBlock.Inlines.Clear();
            var linhas = markdownText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < linhas.Length; i++)
            {
                var linha = linhas[i];

                if (i > 0)
                {
                    textBlock.Inlines.Add(new LineBreak());
                }

                if (string.IsNullOrWhiteSpace(linha))
                {
                    continue;
                }

                // Parse de negrito marcado por **texto**
                var partes = linha.Split(new[] { "**" }, StringSplitOptions.None);
                for (int p = 0; p < partes.Length; p++)
                {
                    if (string.IsNullOrEmpty(partes[p])) continue;

                    if (p % 2 == 1) // Índice ímpar = negrito
                    {
                        var boldRun = new Run(partes[p])
                        {
                            FontWeight = FontWeights.Bold
                        };
                        textBlock.Inlines.Add(boldRun);
                    }
                    else
                    {
                        var normalRun = new Run(partes[p]);
                        textBlock.Inlines.Add(normalRun);
                    }
                }
            }
        }
    }
}
