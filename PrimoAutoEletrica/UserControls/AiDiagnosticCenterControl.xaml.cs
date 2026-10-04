using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using PrimoAutoEletrica.Models.AI;
using PrimoAutoEletrica.Models.BibliotecaTecnica;
using PrimoAutoEletrica.Models.Circuitos;
using PrimoAutoEletrica.Models.DiagnosticoGuiado;
using PrimoAutoEletrica.Models.SureTrack;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PrimoAutoEletrica.UserControls
{
    public partial class AiDiagnosticCenterControl : UserControl
    {
        private readonly IAIService _aiService;
        private readonly INavigationService? _navigationService;
        private readonly AutomotiveWebSearchService _webSearchService;
        private readonly ThemeService? _themeService;
        private readonly IWorkOrderAiBridgeService? _bridgeService;
        private readonly ISureTrackService? _sureTrackService;
        private readonly IBibliotecaTecnicaService? _bibliotecaTecnicaService;
        private readonly ITroubleshootingFlowService? _troubleshootingService;
        private readonly ICalculadoraQuedaTensaoService? _calculadoraQuedaTensaoService;
        private List<PinagemModulo> _modulosBiblioteca = new();
        private List<CentralEletricaFusivel> _centraisBiblioteca = new();
        private List<VeiculoPesado24VEspecificacao> _pesadosBiblioteca = new();
        private List<FluxogramaDiagnostico> _fluxogramas = new();
        private FluxogramaDiagnostico? _fluxogramaSelecionado;
        private FluxogramaPasso? _passoAtual;
        private readonly List<string> _trilhaPassos = new();
        private ResultadoQuedaTensao? _ultimoResultadoQueda;
        private FluxogramaOpcaoResposta? _ultimaConclusao;
        private readonly List<AIChatMessage> _messages = new();
        private bool _isProcessing;
        private double _currentZoom = 1.0;
        private double _diagramImageZoom = 1.0;
        private string? _currentDiagramImagePath;
        private string _selectedProbeDetail = string.Empty;
        private CircuitGraph? _currentCircuitGraph;
        private readonly Dictionary<string, (Line LineElement, CircuitWire WireData, Brush OriginalBrush, double OriginalThickness)> _wireVisualMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (Border BorderElement, CircuitNode NodeData)> _nodeVisualMap = new(StringComparer.OrdinalIgnoreCase);

        public AiDiagnosticCenterControl()
        {
            InitializeComponent();

            var toolRegistry = App.Services?.GetService<AIToolRegistry>() ?? new AIToolRegistry();
            _aiService = App.Services?.GetService<IAIService>() ?? new DeterministicFallbackAIService(toolRegistry);
            _navigationService = App.Services?.GetService<INavigationService>();
            _webSearchService = new AutomotiveWebSearchService();
            _themeService = App.Services?.GetService<ThemeService>();
            _bridgeService = App.Services?.GetService<IWorkOrderAiBridgeService>();
            _sureTrackService = App.Services?.GetService<ISureTrackService>();
            var db = App.Services?.GetService<DatabaseService>() ?? new DatabaseService();
            _bibliotecaTecnicaService = App.Services?.GetService<IBibliotecaTecnicaService>() ?? new BibliotecaTecnicaService(db);
            _troubleshootingService = App.Services?.GetService<ITroubleshootingFlowService>() ?? new TroubleshootingFlowService(db);
            _calculadoraQuedaTensaoService = App.Services?.GetService<ICalculadoraQuedaTensaoService>() ?? new CalculadoraQuedaTensaoService(db);

            Loaded += AiDiagnosticCenterControl_Loaded;
            Unloaded += AiDiagnosticCenterControl_Unloaded;
        }

        private void AiDiagnosticCenterControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
                _themeService.ThemeChanged += OnThemeChanged;
            }

            AtualizarBadgeEngine();

            if (_messages.Count == 0)
            {
                AdicionarBoasVindas();
            }

            // Desenhar circuito inicial selecionado
            RenderizarCircuitoSelecionado();

            // Carregar estatísticas e casos empíricos SureTrack
            _ = CarregarSureTrackAsync();

            // Carregar Biblioteca Técnica (Pinagens, Centrais de Fusíveis e Linha Pesada 24V)
            _ = CarregarBibliotecaTecnicaAsync();

            // Carregar Fluxogramas de Diagnóstico Guiado e Histórico de Queda de Tensão
            _ = CarregarFluxogramasDiagnosticoAsync();
            _ = CarregarHistoricoCalculosQuedaTensaoAsync();

            InputTextBox.Focus();
        }

        private void AiDiagnosticCenterControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AtualizarBadgeEngine();
                RenderizarCircuitoSelecionado();
            });
        }

        private void AtualizarBadgeEngine()
        {
            if (_aiService.IsOnlineAvailable)
            {
                EngineBadgeText.Text = "⚡ Modo Nuvem Híbrido (Gemini + RAG Oficina)";
                EngineBadge.SetResourceReference(Border.BorderBrushProperty, "BrandBrush");
                EngineBadgeText.SetResourceReference(TextBlock.ForegroundProperty, "BrandBrush");
            }
            else
            {
                EngineBadgeText.Text = "🛡️ Modo Blindado 100% Offline (RAG Técnico Especialista)";
                EngineBadge.SetResourceReference(Border.BorderBrushProperty, "SuccessBrush");
                EngineBadgeText.SetResourceReference(TextBlock.ForegroundProperty, "SuccessBrush");
            }
        }

        private void AdicionarBoasVindas()
        {
            var msg = new AIChatMessage
            {
                Role = AIRole.Assistant,
                SenderName = "PRIMOX Copilot Neural",
                Content = "Olá! Seja bem-vindo ao **Centro de Inteligência Artificial & Laboratório de Diagnóstico PRIMOX**! ⚡\n\nSou seu assistente de bancada especializado em Auto Elétrica Automotiva. Aqui temos total espaço para analisar **circuitos elétricos, pinagens de relés DIN, quedas de tensão, testes com multímetro e osciloscópio**.\n\nQual problema elétrico você está enfrentando no veículo hoje?",
                SuggestedActions = new List<AISuggestedAction>
                {
                    new AISuggestedAction { Label = "🔑 Partida: Tec-Tec", ActionType = "ExecuteQuery", Parameter = "Carro faz tec tec e não liga" },
                    new AISuggestedAction { Label = "💡 Farol Alto Aceso Direto", ActionType = "ExecuteQuery", Parameter = "Farol alto ficou aceso direto com lâmpada 100W" },
                    new AISuggestedAction { Label = "❄️ Ventoinha Fiat Fire", ActionType = "ExecuteQuery", Parameter = "Uno Mille Fire ventoinha não liga" },
                    new AISuggestedAction { Label = "🔋 Consumo Parasita", ActionType = "ExecuteQuery", Parameter = "Como testar consumo parasita de bateria?" },
                    new AISuggestedAction { Label = "📐 Ver Esquema de Arrefecimento", ActionType = "ShowDiagram", Parameter = "VENTOINHA" }
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
                    int caret = InputTextBox.CaretIndex;
                    InputTextBox.Text = InputTextBox.Text.Insert(caret, Environment.NewLine);
                    InputTextBox.CaretIndex = caret + Environment.NewLine.Length;
                    e.Handled = true;
                }
                else
                {
                    e.Handled = true;
                    await ProcessarEnvioAsync();
                }
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

            // Sincronizar automaticamente o esquema elétrico da bancada com o assunto abordado
            AjustarCircuitoConformeTexto(texto);

            // Sincronizar busca web caso o usuário tenha pedido esquema, diagrama ou pinagem
            if (texto.Contains("esquema", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("diagrama", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("pinagem", StringComparison.OrdinalIgnoreCase) ||
                texto.Contains("manual", StringComparison.OrdinalIgnoreCase))
            {
                WorkbenchTabControl.SelectedItem = TabBuscaWeb;
                WebSearchTextBox.Text = texto;
                _ = ExecutarBuscaWebAsync();
            }

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

                // Se a resposta incluir um diagrama de imagem localizado ou baixado, carregar na bancada
                var diagAction = response.SuggestedActions?.FirstOrDefault(a => a.ActionType == "ShowDiagramImage");
                if (diagAction != null && !string.IsNullOrWhiteSpace(diagAction.Parameter))
                {
                    CarregarDiagramaGrafico(diagAction.Parameter);
                }

                // Também verificar se a resposta sugere um circuito específico
                AjustarCircuitoConformeTexto(response.Message);
            }
            catch (Exception ex)
            {
                var errorMsg = new AIChatMessage
                {
                    Role = AIRole.Assistant,
                    SenderName = "Sistema",
                    Content = $"❌ Ocorreu uma instabilidade no processamento:\n{ex.Message}",
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

        private void AjustarCircuitoConformeTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;
            var t = texto.ToLowerInvariant();

            if (t.Contains("ventoinha") || t.Contains("arrefecimento") || t.Contains("temperatura") || t.Contains("fervendo") || t.Contains("fire"))
            {
                CircuitSelectorComboBox.SelectedIndex = 0;
            }
            else if (t.Contains("tec tec") || t.Contains("partida") || t.Contains("arranque") || t.Contains("linha 50") || t.Contains("solenoide") || t.Contains("comutador"))
            {
                CircuitSelectorComboBox.SelectedIndex = 1;
            }
            else if (t.Contains("farol") || t.Contains("rele") || t.Contains("relé") || t.Contains("100w") || t.Contains("lâmpada") || t.Contains("lampada"))
            {
                CircuitSelectorComboBox.SelectedIndex = 2;
            }
            else if (t.Contains("fuga") || t.Contains("parasita") || t.Contains("descarrega") || t.Contains("dorme") || t.Contains("consumo"))
            {
                CircuitSelectorComboBox.SelectedIndex = 3;
            }
            else if (t.Contains("tampa") || t.Contains("traseira") || t.Contains("porta-malas") || t.Contains("porta malas") || t.Contains("sanfona") || t.Contains("chicote"))
            {
                CircuitSelectorComboBox.SelectedIndex = 4;
            }
            else if (t.Contains("can") || t.Contains("obd") || t.Contains("scanner") || t.Contains("p0") || t.Contains("diagnose"))
            {
                CircuitSelectorComboBox.SelectedIndex = 5;
            }
        }

        private void RenderizarMensagem(AIChatMessage msg)
        {
            var isUser = msg.Role == AIRole.User;

            var bubble = new Border
            {
                CornerRadius = isUser ? new CornerRadius(16, 16, 4, 16) : new CornerRadius(16, 16, 16, 4),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = isUser ? new Thickness(40, 5, 0, 8) : new Thickness(0, 5, 40, 8),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                BorderThickness = isUser ? new Thickness(0) : new Thickness(1)
            };

            if (isUser)
            {
                bubble.SetResourceReference(Border.BackgroundProperty, "BrandBrush");
            }
            else
            {
                bubble.SetResourceReference(Border.BackgroundProperty, "CardBackgroundBrush");
                bubble.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            }

            var contentStack = new StackPanel();

            if (!isUser && !string.IsNullOrWhiteSpace(msg.SenderName))
            {
                var headerPanel = new Border
                {
                    Padding = new Thickness(6, 2, 8, 2),
                    CornerRadius = new CornerRadius(6),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                headerPanel.SetResourceReference(Border.BackgroundProperty, "BrandSoftBrush");

                var headerText = new TextBlock
                {
                    Text = $"🤖 {msg.SenderName}",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                };
                headerText.SetResourceReference(TextBlock.ForegroundProperty, "BrandBrush");
                headerPanel.Child = headerText;
                contentStack.Children.Add(headerPanel);
            }

            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 20
            };
            if (isUser)
            {
                textBlock.SetResourceReference(TextBlock.ForegroundProperty, "AccentButtonTextBrush");
            }
            else
            {
                textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");
            }
            PreencherInlinesFormatados(textBlock, msg.Content, isUser);
            contentStack.Children.Add(textBlock);

            var timeText = new TextBlock
            {
                Text = msg.Timestamp.ToString("HH:mm"),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 5, 0, 0)
            };
            if (isUser)
            {
                timeText.SetResourceReference(TextBlock.ForegroundProperty, "AccentButtonTextBrush");
                timeText.Opacity = 0.85;
            }
            else
            {
                timeText.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
            }
            contentStack.Children.Add(timeText);

            // Card de Proposta de Peças e Serviços para OS
            if (msg.ProposedItems != null && msg.ProposedItems.Count > 0)
            {
                var proposalCard = new Border
                {
                    Margin = new Thickness(0, 10, 0, 4),
                    Padding = new Thickness(12, 10, 12, 10),
                    CornerRadius = new CornerRadius(10),
                    BorderThickness = new Thickness(1)
                };
                proposalCard.SetResourceReference(Border.BackgroundProperty, "SurfaceAltBrush");
                proposalCard.SetResourceReference(Border.BorderBrushProperty, "BrandBrush");

                var proposalStack = new StackPanel();

                var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
                var iconText = new TextBlock { Text = "⚡ ", FontSize = 12 };
                iconText.SetResourceReference(TextBlock.ForegroundProperty, "BrandBrush");
                var titleText = new TextBlock
                {
                    Text = $"Sugestão de Peças & Mão de Obra ({msg.ProposedItems.Count} itens identificados)",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };
                titleText.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");
                titleStack.Children.Add(iconText);
                titleStack.Children.Add(titleText);
                proposalStack.Children.Add(titleStack);

                foreach (var item in msg.ProposedItems)
                {
                    var itemGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var descText = new TextBlock
                    {
                        Text = $"• [{(item.Tipo == "Servico" ? "SERVIÇO" : "PEÇA")}] {item.Descricao}",
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap
                    };
                    descText.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");
                    Grid.SetColumn(descText, 0);

                    var priceText = new TextBlock
                    {
                        Text = $"{item.PrecoSugerido:C}",
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    priceText.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryBrush");
                    Grid.SetColumn(priceText, 1);

                    itemGrid.Children.Add(descText);
                    itemGrid.Children.Add(priceText);
                    proposalStack.Children.Add(itemGrid);
                }

                var btnInserirOS = new Button
                {
                    Content = "🛒 Inserir na Ordem de Serviço (1-Click)",
                    Style = TryFindResource("ModalAccentButton") as Style ?? TryFindResource("ActionChipButton") as Style,
                    Margin = new Thickness(0, 8, 0, 0),
                    Height = 30,
                    Padding = new Thickness(12, 0, 12, 0),
                    FontSize = 11,
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
                        Style = TryFindResource("ActionChipButton") as Style,
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

                    case "ShowDiagram":
                        WorkbenchTabControl.SelectedItem = TabCircuitos;
                        if (action.Parameter.Contains("VENTOINHA", StringComparison.OrdinalIgnoreCase))
                            CircuitSelectorComboBox.SelectedIndex = 0;
                        else if (action.Parameter.Contains("PARTIDA", StringComparison.OrdinalIgnoreCase))
                            CircuitSelectorComboBox.SelectedIndex = 1;
                        else if (action.Parameter.Contains("FAROL", StringComparison.OrdinalIgnoreCase))
                            CircuitSelectorComboBox.SelectedIndex = 2;
                        break;

                    case "ShowDiagramImage":
                        CarregarDiagramaGrafico(action.Parameter);
                        break;

                    case "OpenWebSearch":
                        WorkbenchTabControl.SelectedItem = TabBuscaWeb;
                        WebSearchTextBox.Text = action.Parameter;
                        await ExecutarBuscaWebAsync();
                        break;

                    case "Navigate":
                        _navigationService?.Navigate(action.Parameter);
                        break;

                    case "ExecuteQuery":
                        InputTextBox.Text = action.Parameter;
                        await ProcessarEnvioAsync();
                        break;

                    case "CopyText":
                        Clipboard.SetText(action.Parameter);
                        MessageBox.Show("Procedimento copiado com sucesso para a área de transferência!", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void NovoChatButton_Click(object sender, RoutedEventArgs e)
        {
            _messages.Clear();
            MessagesPanel.Children.Clear();
            AdicionarBoasVindas();
        }

        private void PesquisarWebAtalhoButton_Click(object sender, RoutedEventArgs e)
        {
            WorkbenchTabControl.SelectedItem = TabBuscaWeb;
            WebSearchTextBox.Focus();
            WebSearchTextBox.SelectAll();
        }

        private void AjudaAtalhosButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "⚡ DICAS DO CENTRO DE INTELIGÊNCIA ARTIFICIAL:\n\n" +
                "1. Diálogo Socrático: Descreva o sintoma. O assistente perguntará modelo, ano ou testes já feitos para indicar o diagnóstico exato.\n" +
                "2. Esquemas Vetoriais: Clique nos botões circulares A, B, C, D do circuito para ler as tensões corretas no multímetro.\n" +
                "3. Zoom da Bancada: Use os botões [+] e [-] para aproximar os detalhes dos diagramas.\n" +
                "4. Teclas de Atalho: Pressione Enter para enviar a mensagem diretamente ou Shift+Enter para quebrar linhas.",
                "Guia do PRIMOX Neural Copilot",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================================================
        // CONTROLES DE ZOOM DA BANCADA DE ESQUEMAS
        // =========================================================================
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Min(2.5, _currentZoom + 0.15);
            AplicarZoom();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Max(0.6, _currentZoom - 0.15);
            AplicarZoom();
        }

        private void ZoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = 1.0;
            AplicarZoom();
        }

        private void AplicarZoom()
        {
            DiagramScaleTransform.ScaleX = _currentZoom;
            DiagramScaleTransform.ScaleY = _currentZoom;
            ZoomResetButton.Content = $"{Math.Round(_currentZoom * 100)}%";
        }

        // =========================================================================
        // VISUALIZADOR DE DIAGRAMAS GRÁFICOS (IMAGENS HD & WEB / CACHE)
        // =========================================================================
        public void CarregarDiagramaGrafico(string? caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho)) return;

            try
            {
                string caminhoFinal = caminho;
                if (!Path.IsPathRooted(caminhoFinal))
                {
                    caminhoFinal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, caminhoFinal);
                }

                if (!File.Exists(caminhoFinal))
                {
                    var nomeArquivo = Path.GetFileName(caminhoFinal);
                    var alternativo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Diagramas", nomeArquivo);
                    if (File.Exists(alternativo))
                    {
                        caminhoFinal = alternativo;
                    }
                    else
                    {
                        var cacheAlt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiagramasCache", nomeArquivo);
                        if (File.Exists(cacheAlt))
                        {
                            caminhoFinal = cacheAlt;
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(caminhoFinal, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                DiagramImageDisplay.Source = bitmap;
                DiagramImageDisplay.Visibility = Visibility.Visible;
                DiagramEmptyStatePanel.Visibility = Visibility.Collapsed;

                _diagramImageZoom = 1.0;
                DiagramImageScale.ScaleX = 1.0;
                DiagramImageScale.ScaleY = 1.0;
                DiagramImageZoomText.Text = "100%";
                _currentDiagramImagePath = caminhoFinal;

                var nomeCurto = Path.GetFileName(caminhoFinal);
                DiagramImageInfoText.Text = $"{bitmap.PixelWidth} × {bitmap.PixelHeight} px | Arquivo: {nomeCurto}";

                if (nomeCurto.Contains("hb20", StringComparison.OrdinalIgnoreCase))
                {
                    DiagramImageTitleText.Text = "🚗 Hyundai HB20 1.0 12V (2012-2015) - Bosch ME 17.9.11.1";
                    DiagramImageBadgeText.Text = "✅ Esquema Completo HD";
                }
                else if (nomeCurto.Contains("gol", StringComparison.OrdinalIgnoreCase))
                {
                    DiagramImageTitleText.Text = "🚗 VW Gol G5 1.0 / 1.6 - Marelli IAW 4GV";
                    DiagramImageBadgeText.Text = "✅ Injeção Eletrônica";
                }
                else
                {
                    DiagramImageTitleText.Text = $"🚗 Diagrama Automotivo: {Path.GetFileNameWithoutExtension(nomeCurto)}";
                    DiagramImageBadgeText.Text = "🌐 Baixado / Acervo";
                }

                WorkbenchTabControl.SelectedItem = TabDiagramaGrafico;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AiDiagnosticCenterControl] Erro ao carregar imagem de diagrama: {ex.Message}");
            }
        }

        private void ExemploHb20Button_Click(object sender, RoutedEventArgs e)
        {
            var p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Diagramas", "hb20_1.0_bosch_me17.9.11.png");
            if (File.Exists(p1))
            {
                CarregarDiagramaGrafico(p1);
                return;
            }
            var p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Diagramas", "hb20_sete_treinamentos.png");
            if (File.Exists(p2))
            {
                CarregarDiagramaGrafico(p2);
            }
        }

        private void ExemploGolButton_Click(object sender, RoutedEventArgs e)
        {
            var p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Diagramas", "gol_g5_iaw_4gv.jpg");
            if (File.Exists(p1))
            {
                CarregarDiagramaGrafico(p1);
            }
        }

        private void ImgZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            _diagramImageZoom = Math.Min(4.0, Math.Round(_diagramImageZoom + 0.15, 2));
            AtualizarZoomImagem();
        }

        private void ImgZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            _diagramImageZoom = Math.Max(0.2, Math.Round(_diagramImageZoom - 0.15, 2));
            AtualizarZoomImagem();
        }

        private void ImgZoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            _diagramImageZoom = 1.0;
            AtualizarZoomImagem();
        }

        private void ImgZoomFitButton_Click(object sender, RoutedEventArgs e)
        {
            if (DiagramImageDisplay.Source is BitmapSource bs && DiagramImageScrollViewer.ActualWidth > 50)
            {
                var larguraDisponivel = DiagramImageScrollViewer.ActualWidth - 30;
                var ratio = larguraDisponivel / bs.PixelWidth;
                _diagramImageZoom = Math.Max(0.2, Math.Min(3.0, Math.Round(ratio, 2)));
                AtualizarZoomImagem();
            }
        }

        private void AtualizarZoomImagem()
        {
            DiagramImageScale.ScaleX = _diagramImageZoom;
            DiagramImageScale.ScaleY = _diagramImageZoom;
            DiagramImageZoomText.Text = $"{Math.Round(_diagramImageZoom * 100)}%";
        }

        private void DiagramImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.None)
            {
                if (e.Delta > 0)
                {
                    _diagramImageZoom = Math.Min(4.0, Math.Round(_diagramImageZoom + 0.10, 2));
                }
                else if (e.Delta < 0)
                {
                    _diagramImageZoom = Math.Max(0.2, Math.Round(_diagramImageZoom - 0.10, 2));
                }
                AtualizarZoomImagem();
                e.Handled = true;
            }
        }

        private void ImgAbrirExternoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentDiagramImagePath) && File.Exists(_currentDiagramImagePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(_currentDiagramImagePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Não foi possível abrir o visualizador externo: {ex.Message}", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Nenhuma imagem de esquema carregada para abrir.", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImgSalvarButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentDiagramImagePath) || !File.Exists(_currentDiagramImagePath))
            {
                MessageBox.Show("Nenhum diagrama carregado para salvar.", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var extensao = Path.GetExtension(_currentDiagramImagePath);
            var nomePadrao = Path.GetFileName(_currentDiagramImagePath);

            var sfd = new SaveFileDialog
            {
                FileName = nomePadrao,
                Filter = $"Imagem (*{extensao})|*{extensao}|Todos os Arquivos|*.*",
                Title = "Salvar Esquema Elétrico"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_currentDiagramImagePath, sfd.FileName, true);
                    MessageBox.Show("Diagrama exportado com sucesso!", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar arquivo: {ex.Message}", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // =========================================================================
        // RENDERIZADOR DINÂMICO DE CIRCUITOS ELÉTRICOS VETORIAIS
        // =========================================================================
        private void CircuitSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderizarCircuitoSelecionado();
        }

        private void RenderizarCircuitoSelecionado()
        {
            if (SchematicCanvas == null) return;
            SchematicCanvas.Children.Clear();
            _wireVisualMap.Clear();
            _nodeVisualMap.Clear();

            int index = CircuitSelectorComboBox?.SelectedIndex ?? 0;
            _currentCircuitGraph = CircuitGraph.ObterPorIndice(index);

            var brandBrush = (TryFindResource("BrandBrush") as Brush) ?? Brushes.DarkOrange;
            var textBrush = (TryFindResource("PrimaryTextBrush") as Brush) ?? Brushes.White;
            var cardBg = (TryFindResource("CardBackgroundBrush") as Brush) ?? Brushes.DarkSlateGray;
            var borderBrush = (TryFindResource("BorderBrush") as Brush) ?? Brushes.Gray;

            // 1. Título do Esquema
            AdicionarTexto(40, 20, _currentCircuitGraph.Title.ToUpperInvariant(), 13, true, brandBrush);

            // 2. Renderizar Fios Interativos
            foreach (var wire in _currentCircuitGraph.Wires)
            {
                DesenharLinhaInterativa(wire);
            }

            // 3. Renderizar Nós / Componentes
            foreach (var node in _currentCircuitGraph.Nodes.Values)
            {
                if (node.NodeType == CircuitNodeType.Ground)
                {
                    DesenharSimboloTerra(node.X, node.Y, textBrush);
                }
                else
                {
                    DesenharCaixaComponenteInterativo(node, cardBg, borderBrush, brandBrush);
                }
            }

            // 4. Renderizar Pontos de Teste (Probes)
            foreach (var probe in _currentCircuitGraph.Probes)
            {
                AdicionarPontoTesteInterativo(probe);
            }

            // 5. Dica Técnica do Circuito
            if (!string.IsNullOrWhiteSpace(_currentCircuitGraph.TechnicalTip))
            {
                AdicionarTexto(40, 360, $"💡 {_currentCircuitGraph.TechnicalTip}", 11, false, brandBrush);
            }

            // Dica de instrução na barra
            if (ActiveWireInfoText != null)
            {
                ActiveWireInfoText.Text = $"⚡ Circuito Ativo: {_currentCircuitGraph.Title} ({_currentCircuitGraph.Wires.Count} fios, {_currentCircuitGraph.Probes.Count} pontos de teste)";
            }
        }

        private void DesenharLinhaInterativa(CircuitWire wire)
        {
            Brush corFio;
            try
            {
                corFio = (Brush)new BrushConverter().ConvertFromString(wire.NeonHighlightColorHex)!;
            }
            catch
            {
                corFio = Brushes.Cyan;
            }

            var line = new Line
            {
                X1 = wire.X1,
                Y1 = wire.Y1,
                X2 = wire.X2,
                Y2 = wire.Y2,
                Stroke = corFio,
                StrokeThickness = wire.Thickness,
                Cursor = Cursors.Hand,
                ToolTip = new ToolTip { Content = wire.ObterResumoTecnico() }
            };

            if (wire.IsDashed)
            {
                line.StrokeDashArray = new DoubleCollection { 4, 3 };
            }

            // Eventos interativos de mouse para acionar Trace Wire
            line.MouseEnter += (s, e) => ExecutarTraceWire(wire.Id);
            line.MouseLeftButtonDown += (s, e) => ExecutarTraceWire(wire.Id);

            _wireVisualMap[wire.Id] = (line, wire, corFio, wire.Thickness);
            SchematicCanvas.Children.Add(line);
        }

        private void DesenharCaixaComponenteInterativo(CircuitNode node, Brush fundo, Brush borda, Brush brandBrush)
        {
            var borderBrush = node.NodeType switch
            {
                CircuitNodeType.PowerSource => brandBrush,
                CircuitNodeType.Fuse => (TryFindResource("DangerBrush") as Brush) ?? Brushes.Red,
                CircuitNodeType.RelayTerminal => (TryFindResource("InfoBrush") as Brush) ?? Brushes.DodgerBlue,
                CircuitNodeType.Switch => (TryFindResource("WarningBrush") as Brush) ?? Brushes.Gold,
                CircuitNodeType.Actuator => brandBrush,
                _ => borda
            };

            var rect = new Border
            {
                Width = node.Width,
                Height = node.Height,
                Background = fundo,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6),
                Cursor = Cursors.Hand,
                ToolTip = new ToolTip { Content = $"Componente: {node.ComponentName}\nTipo: {node.NodeType}\nTerminal: {node.TerminalCode}" }
            };

            var tb = new TextBlock
            {
                Text = node.Label,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");

            rect.Child = tb;
            Canvas.SetLeft(rect, node.X);
            Canvas.SetTop(rect, node.Y);

            // Clique no componente destaca fios conectados
            rect.MouseLeftButtonDown += (s, e) =>
            {
                if (_currentCircuitGraph != null)
                {
                    var firstWire = _currentCircuitGraph.Wires.FirstOrDefault(w => w.FromNodeId == node.Id || w.ToNodeId == node.Id);
                    if (firstWire != null)
                    {
                        ExecutarTraceWire(firstWire.Id);
                    }
                }
            };

            _nodeVisualMap[node.Id] = (rect, node);
            SchematicCanvas.Children.Add(rect);
        }

        private void AdicionarPontoTesteInterativo(CircuitProbePoint probe)
        {
            var btn = new Button
            {
                Content = probe.Id,
                Style = TryFindResource("ProbePointButton") as Style,
                Tag = probe,
                ToolTip = new ToolTip { Content = $"Ponto {probe.Id}: {probe.Title}\nChave Ligada: {probe.KeyOnVoltage}\nClique para ver tolerâncias e diagnóstico" }
            };

            btn.Click += (s, e) =>
            {
                _selectedProbeDetail = probe.ObterResumoFormatado();
                ProbeDetailTextBlock.Text = $"⚡ Ponto {probe.Id} ({probe.Title}): Repouso: {probe.KeyOffVoltage} | Ligado: {probe.KeyOnVoltage} | Funcionando: {probe.EngineRunningVoltage} | Dica: {probe.DiagnosticInstructions}";
            };

            Canvas.SetLeft(btn, probe.X - 14);
            Canvas.SetTop(btn, probe.Y - 14);
            SchematicCanvas.Children.Add(btn);
        }

        public void ExecutarTraceWire(string wireId)
        {
            if (_currentCircuitGraph == null) return;

            var (highlightedWires, _) = _currentCircuitGraph.TraceWireBfs(wireId);
            var targetWire = _currentCircuitGraph.Wires.FirstOrDefault(w => string.Equals(w.Id, wireId, StringComparison.OrdinalIgnoreCase));

            foreach (var kvp in _wireVisualMap)
            {
                var isHighlighted = highlightedWires.Contains(kvp.Key);
                if (isHighlighted)
                {
                    kvp.Value.LineElement.Opacity = 1.0;
                    try
                    {
                        var neonBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(kvp.Value.WireData.NeonHighlightColorHex)!;
                        kvp.Value.LineElement.Stroke = neonBrush;
                    }
                    catch { }
                    kvp.Value.LineElement.StrokeThickness = kvp.Value.OriginalThickness + 3.0;
                }
                else
                {
                    kvp.Value.LineElement.Opacity = 0.20;
                    kvp.Value.LineElement.Stroke = kvp.Value.OriginalBrush;
                    kvp.Value.LineElement.StrokeThickness = kvp.Value.OriginalThickness;
                }
            }

            if (targetWire != null)
            {
                if (ActiveWireInfoText != null)
                {
                    ActiveWireInfoText.Text = $"⚡ Trace Wire: {targetWire.ObterResumoTecnico()} ({highlightedWires.Count} segmento(s) contíguos)";
                }
                ProbeDetailTextBlock.Text = $"⚡ Condutor Selecionado: {targetWire.ObterResumoTecnico()}. Malha elétrica destacada em Neon.";
            }
        }

        public void DestacarPorLinha(string tipoLinha)
        {
            if (_currentCircuitGraph == null) return;

            var matchingWires = _currentCircuitGraph.ObterFiosPorLinha(tipoLinha);

            foreach (var kvp in _wireVisualMap)
            {
                var isMatch = matchingWires.Contains(kvp.Key);
                if (isMatch)
                {
                    kvp.Value.LineElement.Opacity = 1.0;
                    try
                    {
                        var neonBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(kvp.Value.WireData.NeonHighlightColorHex)!;
                        kvp.Value.LineElement.Stroke = neonBrush;
                    }
                    catch { }
                    kvp.Value.LineElement.StrokeThickness = kvp.Value.OriginalThickness + 2.5;
                }
                else
                {
                    kvp.Value.LineElement.Opacity = 0.20;
                    kvp.Value.LineElement.Stroke = kvp.Value.OriginalBrush;
                    kvp.Value.LineElement.StrokeThickness = kvp.Value.OriginalThickness;
                }
            }

            if (ActiveWireInfoText != null)
            {
                ActiveWireInfoText.Text = $"⚡ Filtro: {tipoLinha} ({matchingWires.Count} condutores encontrados)";
            }
        }

        public void ResetarDestaqueTrace()
        {
            foreach (var kvp in _wireVisualMap)
            {
                kvp.Value.LineElement.Opacity = 1.0;
                kvp.Value.LineElement.Stroke = kvp.Value.OriginalBrush;
                kvp.Value.LineElement.StrokeThickness = kvp.Value.OriginalThickness;
            }

            if (ActiveWireInfoText != null)
            {
                ActiveWireInfoText.Text = "💡 Clique em qualquer fio ou pino para rastrear a malha contígua (Trace Wire Neon)";
            }
        }

        private void TraceLinha30Button_Click(object sender, RoutedEventArgs e) => DestacarPorLinha("30");
        private void TraceLinha15Button_Click(object sender, RoutedEventArgs e) => DestacarPorLinha("15");
        private void TraceLinha87Button_Click(object sender, RoutedEventArgs e) => DestacarPorLinha("87");
        private void TraceLinha31Button_Click(object sender, RoutedEventArgs e) => DestacarPorLinha("31");
        private void TraceResetButton_Click(object sender, RoutedEventArgs e) => ResetarDestaqueTrace();

        // =========================================================================
        // HELPERS PARA CONSTRUÇÃO GRÁFICA NO CANVAS
        // =========================================================================
        private void DesenharCaixaComponente(double x, double y, double w, double h, string texto, Brush fundo, Brush borda)
        {
            var rect = new Border
            {
                Width = w,
                Height = h,
                Background = fundo,
                BorderBrush = borda,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6)
            };

            var tb = new TextBlock
            {
                Text = texto,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");

            rect.Child = tb;
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            SchematicCanvas.Children.Add(rect);
        }

        private void DesenharLinha(double x1, double y1, double x2, double y2, Brush cor, double espessura, bool tracejada = false)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = cor,
                StrokeThickness = espessura
            };

            if (tracejada)
            {
                line.StrokeDashArray = new DoubleCollection { 4, 3 };
            }

            SchematicCanvas.Children.Add(line);
        }

        private void DesenharSimboloTerra(double x, double y, Brush cor)
        {
            DesenharLinha(x - 14, y, x + 14, y, cor, 2);
            DesenharLinha(x - 9, y + 4, x + 9, y + 4, cor, 2);
            DesenharLinha(x - 4, y + 8, x + 4, y + 8, cor, 2);
        }

        private void AdicionarPontoTeste(double x, double y, string letra, string explicacao)
        {
            var btn = new Button
            {
                Content = letra,
                Style = TryFindResource("ProbePointButton") as Style,
                Tag = explicacao
            };

            btn.Click += (s, e) =>
            {
                _selectedProbeDetail = explicacao;
                ProbeDetailTextBlock.Text = explicacao;
            };

            Canvas.SetLeft(btn, x - 14);
            Canvas.SetTop(btn, y - 14);
            SchematicCanvas.Children.Add(btn);
        }

        private void AdicionarTexto(double x, double y, string texto, double tamanho, bool negrito, Brush cor)
        {
            var tb = new TextBlock
            {
                Text = texto,
                FontSize = tamanho,
                FontWeight = negrito ? FontWeights.Bold : FontWeights.Normal,
                Foreground = cor
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            SchematicCanvas.Children.Add(tb);
        }

        private async void EnviarTesteParaChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedProbeDetail))
            {
                MessageBox.Show("Por favor, clique em um dos pontos marcados com as letras A, B, C ou D no esquema elétrico acima primeiro!", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            InputTextBox.Text = $"Como devo testar na bancada este ponto do circuito: {_selectedProbeDetail}";
            await ProcessarEnvioAsync();
        }

        // =========================================================================
        // BUSCA TÉCNICA NA INTERNET (WEB SEARCH)
        // =========================================================================
        private async void ExecutarBuscaWebButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecutarBuscaWebAsync();
        }

        private async void WebSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ExecutarBuscaWebAsync();
            }
        }

        private async Task ExecutarBuscaWebAsync()
        {
            var query = WebSearchTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query)) return;

            ExecutarBuscaWebButton.IsEnabled = false;
            ExecutarBuscaWebButton.Content = "Buscando... ⏳";
            WebSearchResultsPanel.Children.Clear();

            var loadingBorder = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 8)
            };
            loadingBorder.SetResourceReference(Border.BackgroundProperty, "SurfaceAltBrush");
            loadingBorder.SetResourceReference(Border.BorderBrushProperty, "BrandBrush");
            var loadingTb = new TextBlock
            {
                Text = $"🔍 Pesquisando diagramas, pinagens e manuais técnicos para: '{query}'...",
                FontWeight = FontWeights.SemiBold
            };
            loadingTb.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");
            loadingBorder.Child = loadingTb;
            WebSearchResultsPanel.Children.Add(loadingBorder);

            try
            {
                var resultado = await _webSearchService.PesquisarAsync(query);
                WebSearchResultsPanel.Children.Clear();

                if (!resultado.Sucesso || resultado.Resultados.Count == 0)
                {
                    var erroBorder = new Border
                    {
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(14)
                    };
                    erroBorder.SetResourceReference(Border.BackgroundProperty, "SurfaceAltBrush");
                    erroBorder.SetResourceReference(Border.BorderBrushProperty, "WarningBrush");
                    var erroTb = new TextBlock
                    {
                        Text = $"⚠️ Nenhum diagrama retornado diretamente para a consulta. Tente termos como 'esquema eletrico injecao [modelo]' ou 'diagrama partida [modelo]'.",
                        TextWrapping = TextWrapping.Wrap
                    };
                    erroTb.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");
                    erroBorder.Child = erroTb;
                    WebSearchResultsPanel.Children.Add(erroBorder);
                    return;
                }

                foreach (var item in resultado.Resultados)
                {
                    var card = new Border
                    {
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    card.SetResourceReference(Border.BackgroundProperty, "CardBackgroundBrush");
                    card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

                    var sp = new StackPanel();

                    var titleTb = new TextBlock
                    {
                        Text = item.Titulo,
                        FontWeight = FontWeights.Bold,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap
                    };
                    titleTb.SetResourceReference(TextBlock.ForegroundProperty, "BrandBrush");
                    sp.Children.Add(titleTb);

                    var snippetTb = new TextBlock
                    {
                        Text = item.Snippet,
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 6)
                    };
                    snippetTb.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");
                    sp.Children.Add(snippetTb);

                    var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

                    var linkBtn = new Button
                    {
                        Content = $"🌐 Abrir no Navegador ({item.Dominio})",
                        Style = TryFindResource("SecondaryButton") as Style,
                        Height = 28,
                        Padding = new Thickness(10, 0, 10, 0),
                        Margin = new Thickness(0, 0, 8, 0),
                        Tag = item.Url
                    };
                    linkBtn.Click += (s, e) =>
                    {
                        if ((s as FrameworkElement)?.Tag is string url && !string.IsNullOrEmpty(url))
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                            }
                            catch { }
                        }
                    };
                    btnPanel.Children.Add(linkBtn);

                    var askAiBtn = new Button
                    {
                        Content = "💬 Analisar com Copilot",
                        Style = TryFindResource("ActionChipButton") as Style,
                        Tag = $"Encontrei este manual técnico: '{item.Titulo}'. Você pode me orientar sobre: {item.Snippet}?"
                    };
                    askAiBtn.Click += async (s, e) =>
                    {
                        if ((s as FrameworkElement)?.Tag is string prompt)
                        {
                            InputTextBox.Text = prompt;
                            await ProcessarEnvioAsync();
                        }
                    };
                    btnPanel.Children.Add(askAiBtn);

                    sp.Children.Add(btnPanel);
                    card.Child = sp;
                    WebSearchResultsPanel.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                WebSearchResultsPanel.Children.Clear();
                var cardErro = new Border
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14)
                };
                cardErro.SetResourceReference(Border.BackgroundProperty, "SurfaceAltBrush");
                cardErro.SetResourceReference(Border.BorderBrushProperty, "DangerBrush");
                var cardErroTb = new TextBlock
                {
                    Text = $"❌ Erro ao consultar a internet: {ex.Message}"
                };
                cardErroTb.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
                cardErro.Child = cardErroTb;
                WebSearchResultsPanel.Children.Add(cardErro);
            }
            finally
            {
                ExecutarBuscaWebButton.IsEnabled = true;
                ExecutarBuscaWebButton.Content = "🔍 Buscar Diagramas";
            }
        }

        // =========================================================================
        // PARSER DE FORMATAÇÃO MARKDOWN LEVE PARA O TEXTBLOCK
        // =========================================================================
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

                // Títulos Markdown
                if (linha.StartsWith("### "))
                {
                    var run = new Run(linha.Substring(4))
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 13.5
                    };
                    textBlock.Inlines.Add(run);
                    continue;
                }

                if (linha.StartsWith("## ") || linha.StartsWith("# "))
                {
                    var clean = linha.TrimStart('#', ' ');
                    var run = new Run(clean)
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 14.5
                    };
                    textBlock.Inlines.Add(run);
                    continue;
                }

                // Bullets e formatação de texto comum
                ProcessarLinhaComNegrito(textBlock, linha, isUser);
            }
        }

        private static void ProcessarLinhaComNegrito(TextBlock textBlock, string linha, bool isUser)
        {
            var partes = linha.Split(new[] { "**" }, StringSplitOptions.None);
            for (int p = 0; p < partes.Length; p++)
            {
                var texto = partes[p];
                if (string.IsNullOrEmpty(texto)) continue;

                bool isBold = (p % 2 == 1);
                ProcessarLinksETexto(textBlock, texto, isBold, isUser);
            }
        }

        private static void ProcessarLinksETexto(TextBlock textBlock, string texto, bool isBold, bool isUser)
        {
            var match = System.Text.RegularExpressions.Regex.Match(texto, @"\[(.*?)\]\((.*?)\)");
            if (match.Success)
            {
                int idx = 0;
                while (match.Success)
                {
                    if (match.Index > idx)
                    {
                        var pre = texto.Substring(idx, match.Index - idx);
                        textBlock.Inlines.Add(new Run(pre) { FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal });
                    }

                    var linkText = match.Groups[1].Value;
                    var linkUrl = match.Groups[2].Value;

                    var hyperlink = new Hyperlink(new Run(linkText))
                    {
                        NavigateUri = linkUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? new Uri(linkUrl) : null,
                        FontWeight = FontWeights.Bold
                    };
                    hyperlink.SetResourceReference(Hyperlink.ForegroundProperty, isUser ? "AccentButtonTextBrush" : "BrandBrush");
                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        try
                        {
                            if (e.Uri != null)
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                            }
                        }
                        catch { }
                    };
                    textBlock.Inlines.Add(hyperlink);

                    idx = match.Index + match.Length;
                    match = match.NextMatch();
                }

                if (idx < texto.Length)
                {
                    var pos = texto.Substring(idx);
                    textBlock.Inlines.Add(new Run(pos) { FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal });
                }
            }
            else
            {
                textBlock.Inlines.Add(new Run(texto) { FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal });
            }
        }

        #region Métodos de Operação da Base SureTrack da Oficina

        private async Task CarregarSureTrackAsync(string? termo = null)
        {
            if (_sureTrackService == null) return;

            try
            {
                SureTrackConsultaResultado resultado;
                if (string.IsNullOrWhiteSpace(termo))
                {
                    resultado = await _sureTrackService.ConsultarEstatisticasAsync(null, null, null);
                }
                else
                {
                    var partes = termo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string? dtc = partes.FirstOrDefault(p => System.Text.RegularExpressions.Regex.IsMatch(p, @"^[PBUS]\d{4}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
                    string? modelo = partes.FirstOrDefault(p => !string.Equals(p, dtc, StringComparison.OrdinalIgnoreCase));
                    resultado = await _sureTrackService.ConsultarEstatisticasAsync(modelo, dtc, termo);
                }

                if (SureTrackBadgeTotalTextBlock != null)
                {
                    SureTrackBadgeTotalTextBlock.Text = $"({resultado.TotalCasosAnalisados} casos confirmados)";
                }

                if (SureTrackProbabilidadeItemsControl != null)
                {
                    SureTrackProbabilidadeItemsControl.ItemsSource = resultado.Estatisticas;
                }

                if (SureTrackDica15MinTextBlock != null)
                {
                    if (resultado.DicasAtalho15Min.Count > 0)
                    {
                        SureTrackDica15MinTextBlock.Text = string.Join("\n\n👉 ", resultado.DicasAtalho15Min);
                    }
                    else
                    {
                        SureTrackDica15MinTextBlock.Text = "Para diagnósticos preliminares, meça sempre alimentação (Linha 30/15) e aterramento (Linha 31) antes de condenar módulos.";
                    }
                }

                if (SureTrackCasosDataGrid != null)
                {
                    SureTrackCasosDataGrid.ItemsSource = resultado.CasosIndividuais;
                    if (resultado.CasosIndividuais.Count > 0)
                    {
                        SureTrackCasosDataGrid.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger?.LogWarning($"SureTrack UI: Erro ao carregar casos: {ex.Message}");
            }
        }

        private void SureTrackSearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = CarregarSureTrackAsync(SureTrackSearchTextBox.Text);
        }

        private void SureTrackSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ = CarregarSureTrackAsync(SureTrackSearchTextBox.Text);
            }
        }

        private void SureTrackLimparButton_Click(object sender, RoutedEventArgs e)
        {
            SureTrackSearchTextBox.Text = string.Empty;
            _ = CarregarSureTrackAsync();
        }

        private void SureTrackChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                SureTrackSearchTextBox.Text = tag;
                _ = CarregarSureTrackAsync(tag);
            }
        }

        private void EnviarCasoParaCopilotButton_Click(object sender, RoutedEventArgs e)
        {
            if (SureTrackCasosDataGrid.SelectedItem is CasoResolvidoSureTrack caso)
            {
                string prompt = $"Qual o procedimento detalhado para o caso do {caso.VeiculoFormatado}: {caso.SintomaPrincipal} (DTC {caso.CodigosDTC})?";
                InputTextBox.Text = prompt;
                EnviarButton_Click(sender, e);
            }
        }

        #endregion

        #region Biblioteca Técnica (Pinagens, Centrais de Fusíveis & Linha Pesada 24V)

        private async Task CarregarBibliotecaTecnicaAsync(string? moduloTermo = null, string? centralTermo = null)
        {
            if (_bibliotecaTecnicaService == null) return;

            try
            {
                string? montadora = null;
                if (ModuloMontadoraComboBox?.SelectedItem is ComboBoxItem itemMontadora)
                {
                    var val = itemMontadora.Content?.ToString();
                    if (!string.IsNullOrWhiteSpace(val) && !val.StartsWith("Todas")) montadora = val;
                }

                string? tensao = null;
                if (ModuloTensaoComboBox?.SelectedItem is ComboBoxItem itemTensao)
                {
                    var val = itemTensao.Content?.ToString();
                    if (!string.IsNullOrWhiteSpace(val) && !val.StartsWith("Todas")) tensao = val;
                }

                _modulosBiblioteca = await _bibliotecaTecnicaService.ObterModulosAsync(moduloTermo, montadora, tensao);
                if (ModuloSelecionadoComboBox != null)
                {
                    ModuloSelecionadoComboBox.ItemsSource = _modulosBiblioteca;
                    if (_modulosBiblioteca.Count > 0)
                    {
                        ModuloSelecionadoComboBox.SelectedIndex = 0;
                    }
                }

                _centraisBiblioteca = await _bibliotecaTecnicaService.ObterCentraisEletricasAsync(centralTermo);
                if (CentralSelecionadaComboBox != null)
                {
                    CentralSelecionadaComboBox.ItemsSource = _centraisBiblioteca;
                    if (_centraisBiblioteca.Count > 0)
                    {
                        CentralSelecionadaComboBox.SelectedIndex = 0;
                    }
                }

                _pesadosBiblioteca = await _bibliotecaTecnicaService.ObterEspecificacoesPesadosAsync();
                if (PesadoSelecionadoComboBox != null)
                {
                    PesadoSelecionadoComboBox.ItemsSource = _pesadosBiblioteca;
                    if (_pesadosBiblioteca.Count > 0)
                    {
                        PesadoSelecionadoComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger?.LogWarning($"Biblioteca Técnica UI: Erro ao carregar dados: {ex.Message}");
            }
        }

        private void ModuloSelecionadoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModuloSelecionadoComboBox.SelectedItem is PinagemModulo modulo)
            {
                ExibirModuloSelecionado(modulo);
            }
        }

        private void ExibirModuloSelecionado(PinagemModulo modulo)
        {
            if (ModuloTituloTextBlock != null) ModuloTituloTextBlock.Text = modulo.NomeModulo;
            if (ModuloTensaoTextBlock != null) ModuloTensaoTextBlock.Text = modulo.TensaoOperacao;
            if (ModuloAplicacaoTextBlock != null) ModuloAplicacaoTextBlock.Text = modulo.ModelosAplicacao;
            if (ModuloConectoresTextBlock != null) ModuloConectoresTextBlock.Text = modulo.DescricaoConectores;
            if (ModuloObsTextBlock != null) ModuloObsTextBlock.Text = modulo.ObservacoesTecnicas;
            if (ModuloPinosDataGrid != null) ModuloPinosDataGrid.ItemsSource = modulo.Pinos;
        }

        private void ModuloBuscarButton_Click(object sender, RoutedEventArgs e)
        {
            _ = CarregarBibliotecaTecnicaAsync(ModuloBuscaTextBox?.Text, null);
        }

        private void ModuloBuscaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ = CarregarBibliotecaTecnicaAsync(ModuloBuscaTextBox?.Text, null);
            }
        }

        private void ModuloFiltro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                _ = CarregarBibliotecaTecnicaAsync(ModuloBuscaTextBox?.Text, null);
            }
        }

        private void CopiarPinagemButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModuloSelecionadoComboBox.SelectedItem is PinagemModulo modulo)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"PINAGEM TÉCNICA PRIMOX: {modulo.NomeModulo} ({modulo.Montadora} - {modulo.TensaoOperacao})");
                sb.AppendLine($"Aplicação: {modulo.ModelosAplicacao}");
                sb.AppendLine($"Conectores: {modulo.DescricaoConectores}");
                sb.AppendLine("------------------------------------------------------------------");
                foreach (var p in modulo.Pinos)
                {
                    sb.AppendLine($"[{p.Conector} - {p.NumeroPino}] {p.FuncaoSinal} | Tipo: {p.TipoSinal} | Fio: {p.CorFio} | Tensão: {p.TensaoEsperada} | Obs: {p.ObservacoesTecnicas}");
                }
                try
                {
                    Clipboard.SetText(sb.ToString());
                    MessageBox.Show("Dados da pinagem copiados com sucesso para a Área de Transferência!", "PRIMOX Biblioteca Técnica", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    // Fallback seguro de área de transferência
                }
            }
        }

        private void ConsultarPinagemNoCopilotButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModuloSelecionadoComboBox.SelectedItem is PinagemModulo modulo)
            {
                string prompt = $"Qual o roteiro de testes e medição com multímetro para o módulo {modulo.NomeModulo} no {modulo.ModelosAplicacao}?";
                InputTextBox.Text = prompt;
                EnviarButton_Click(sender, e);
            }
        }

        private void CentralSelecionadaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CentralSelecionadaComboBox.SelectedItem is CentralEletricaFusivel central)
            {
                ExibirCentralSelecionada(central);
            }
        }

        private void ExibirCentralSelecionada(CentralEletricaFusivel central)
        {
            if (CentralTituloTextBlock != null) CentralTituloTextBlock.Text = central.Titulo;
            if (CentralDetalheTextBlock != null) CentralDetalheTextBlock.Text = $"Localização: {central.Localizacao} | Tensão: {central.TensaoNominal} | Aplicação: {central.ModelosAplicacao}";
            if (CentralFusiveisDataGrid != null) CentralFusiveisDataGrid.ItemsSource = central.Fusiveis;
            if (CentralRelesDataGrid != null) CentralRelesDataGrid.ItemsSource = central.Reles;
        }

        private void CentralBuscarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltrarCentralPorTexto();
        }

        private void CentralBuscaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FiltrarCentralPorTexto();
            }
        }

        private void FiltrarCentralPorTexto()
        {
            var termo = CentralBuscaTextBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(termo))
            {
                if (CentralSelecionadaComboBox.SelectedItem is CentralEletricaFusivel central)
                {
                    ExibirCentralSelecionada(central);
                }
                return;
            }

            if (CentralSelecionadaComboBox.SelectedItem is CentralEletricaFusivel c)
            {
                var fusiveisFiltrados = c.Fusiveis.Where(f =>
                    f.Numero.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    f.CircuitoProtegido.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    f.ReleAssociado.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    f.CorPadrao.Contains(termo, StringComparison.OrdinalIgnoreCase)).ToList();

                var relesFiltrados = c.Reles.Where(r =>
                    r.Posicao.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    r.NomeFuncao.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    r.PinagemReferencia.Contains(termo, StringComparison.OrdinalIgnoreCase)).ToList();

                if (CentralFusiveisDataGrid != null) CentralFusiveisDataGrid.ItemsSource = fusiveisFiltrados;
                if (CentralRelesDataGrid != null) CentralRelesDataGrid.ItemsSource = relesFiltrados;
            }
        }

        private void PesadoSelecionadoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PesadoSelecionadoComboBox.SelectedItem is VeiculoPesado24VEspecificacao pesado)
            {
                ExibirPesadoSelecionado(pesado);
            }
        }

        private void ExibirPesadoSelecionado(VeiculoPesado24VEspecificacao pesado)
        {
            if (PesadoTensaoTextBlock != null) PesadoTensaoTextBlock.Text = pesado.TensaoSistema;
            if (PesadoAlternadorTextBlock != null) PesadoAlternadorTextBlock.Text = pesado.AlternadorEspecificacao;
            if (PesadoBateriasTextBlock != null) PesadoBateriasTextBlock.Text = pesado.BateriasEspecificacao;
            if (PesadoStandbyTextBlock != null) PesadoStandbyTextBlock.Text = pesado.ConsumoStandbyMaximo;
            if (PesadoTorqueTextBlock != null) PesadoTorqueTextBlock.Text = pesado.TorqueCabecote;
            if (PesadoFolgaTextBlock != null) PesadoFolgaTextBlock.Text = pesado.FolgaValvulas;
            if (PesadoGasTextBlock != null) PesadoGasTextBlock.Text = pesado.ArCondicionadoGasGramas;
            if (PesadoOleoTextBlock != null) PesadoOleoTextBlock.Text = pesado.ArCondicionadoOleoTipo;
            if (PesadoDicasTextBlock != null) PesadoDicasTextBlock.Text = pesado.DicasEletricasChassi;
        }

        #endregion

        #region 9. DIAGNÓSTICO GUIADO & CALCULADORA DE QUEDA DE TENSÃO

        private async Task CarregarFluxogramasDiagnosticoAsync()
        {
            if (_troubleshootingService == null) return;
            try
            {
                _fluxogramas = await _troubleshootingService.ObterFluxogramasAsync();
                Dispatcher.Invoke(() =>
                {
                    if (FluxogramasComboBox != null)
                    {
                        FluxogramasComboBox.ItemsSource = _fluxogramas;
                        if (_fluxogramas.Count > 0 && FluxogramasComboBox.SelectedIndex < 0)
                        {
                            FluxogramasComboBox.SelectedIndex = 0;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar fluxogramas: {ex.Message}");
            }
        }

        private void FluxogramasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FluxogramasComboBox.SelectedItem is FluxogramaDiagnostico fluxo)
            {
                _fluxogramaSelecionado = fluxo;
                IniciarFluxograma(fluxo);
            }
        }

        private void IniciarFluxograma(FluxogramaDiagnostico fluxo)
        {
            _trilhaPassos.Clear();
            _trilhaPassos.Add($"Início: {fluxo.Titulo}");
            AtualizarTrilhaTexto();

            if (CategoriaFluxoTextBlock != null) CategoriaFluxoTextBlock.Text = fluxo.Categoria;

            if (fluxo.Passos.Count > 0)
            {
                ExibirPasso(fluxo.Passos[0]);
            }
        }

        private void ExibirPasso(FluxogramaPasso passo)
        {
            _passoAtual = passo;
            if (CardConclusaoBorder != null) CardConclusaoBorder.Visibility = Visibility.Collapsed;
            if (CardPassoAtualBorder != null) CardPassoAtualBorder.Visibility = Visibility.Visible;

            if (PassoTituloTextBlock != null) PassoTituloTextBlock.Text = $"Passo {passo.PassoNumero}: {passo.TituloPasso}";
            if (InstrucaoTesteTextBlock != null) InstrucaoTesteTextBlock.Text = passo.InstrucaoTeste;
            if (FerramentaRecomendadaTextBlock != null) FerramentaRecomendadaTextBlock.Text = passo.FerramentaRecomendada;
            if (PontoMedicaoTextBlock != null) PontoMedicaoTextBlock.Text = passo.PontoMedicao;
            if (ValorEsperadoTextBlock != null) ValorEsperadoTextBlock.Text = passo.ValorEsperado;

            if (OpcoesRespostaItemsControl != null)
            {
                OpcoesRespostaItemsControl.ItemsSource = passo.Opcoes;
            }
        }

        private void AvancarOpcaoFluxograma_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is FluxogramaOpcaoResposta opcao)
            {
                _trilhaPassos.Add($"Passo {_passoAtual?.PassoNumero ?? 1}: {opcao.TextoBotao}");
                AtualizarTrilhaTexto();

                if (opcao.EhConclusao)
                {
                    ExibirConclusao(opcao);
                }
                else if (opcao.ProximoPassoNumero.HasValue && _fluxogramaSelecionado != null)
                {
                    var proximo = _fluxogramaSelecionado.Passos.FirstOrDefault(p => p.PassoNumero == opcao.ProximoPassoNumero.Value);
                    if (proximo != null)
                    {
                        ExibirPasso(proximo);
                    }
                    else
                    {
                        ExibirConclusao(opcao);
                    }
                }
            }
        }

        private void ExibirConclusao(FluxogramaOpcaoResposta opcao)
        {
            _ultimaConclusao = opcao;
            if (CardPassoAtualBorder != null) CardPassoAtualBorder.Visibility = Visibility.Collapsed;
            if (CardConclusaoBorder != null)
            {
                CardConclusaoBorder.Visibility = Visibility.Visible;
                if (ConclusaoDiagnosticaTextBlock != null) ConclusaoDiagnosticaTextBlock.Text = opcao.ConclusaoDiagnostica ?? "Diagnóstico concluído.";
                if (AcaoRecomendadaTextBlock != null) AcaoRecomendadaTextBlock.Text = opcao.AcaoRecomendada ?? "Executar procedimento de bancada.";
                if (PecaSugeridaTextBlock != null) PecaSugeridaTextBlock.Text = !string.IsNullOrWhiteSpace(opcao.PecaSugerida) ? opcao.PecaSugerida : (opcao.ServicoSugerido ?? "Revisão Geral");
            }
        }

        private void ReiniciarFluxogramaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_fluxogramaSelecionado != null)
            {
                IniciarFluxograma(_fluxogramaSelecionado);
            }
        }

        private void AtualizarTrilhaTexto()
        {
            if (TrilhaPassosTextBlock != null)
            {
                TrilhaPassosTextBlock.Text = string.Join(" ➔ ", _trilhaPassos);
            }
        }

        private void InserirPecaFluxogramaEmOSButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ultimaConclusao == null)
            {
                MessageBox.Show("Nenhuma conclusão ou peça selecionada para inserção.", "PRIMOX Copilot", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var itemNome = !string.IsNullOrWhiteSpace(_ultimaConclusao.PecaSugerida)
                ? _ultimaConclusao.PecaSugerida
                : (_ultimaConclusao.ServicoSugerido ?? "Serviço Elétrico");

            var tipoItem = !string.IsNullOrWhiteSpace(_ultimaConclusao.PecaSugerida) ? "Peca" : "Servico";

            var proposta = new AIPartOrServiceProposal
            {
                Tipo = tipoItem,
                Descricao = itemNome,
                Quantidade = 1,
                PrecoSugerido = 120.0m,
                Selecionado = true
            };

            AbrirDialogoPonteOS(new List<AIPartOrServiceProposal> { proposta });
        }

        private void CalcularQuedaTensaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_calculadoraQuedaTensaoService == null) return;

            double.TryParse((TensaoFonteTextBox?.Text ?? "12.60").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vFonte);
            double.TryParse((TensaoCargaTextBox?.Text ?? "11.40").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vCarga);
            double.TryParse((CorrenteAmperesTextBox?.Text ?? "15.0").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var corrente);
            double.TryParse((ComprimentoCaboTextBox?.Text ?? "2.5").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var comp);

            var tipoCircuito = (TipoCircuitoComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Potência";
            var ident = IdentificacaoCircuitoTextBox?.Text ?? "Circuito de Potência";
            var placa = PlacaCalculoTextBox?.Text;

            var param = new ParametrosCalculoQuedaTensao
            {
                TensaoFonteVolts = vFonte > 0 ? vFonte : 12.60,
                TensaoCargaVolts = vCarga > 0 ? vCarga : 11.40,
                CorrenteAmperes = corrente > 0 ? corrente : 15.0,
                ComprimentoCaboMetros = comp > 0 ? comp : 2.5,
                TipoCircuito = tipoCircuito,
                IdentificacaoCircuito = ident,
                VeiculoPlaca = placa
            };

            _ultimoResultadoQueda = _calculadoraQuedaTensaoService.CalcularQuedaTensao(param);
            ExibirResultadoQueda(_ultimoResultadoQueda);
        }

        private void ExibirResultadoQueda(ResultadoQuedaTensao res)
        {
            if (QuedaTensaoTextBlock != null) QuedaTensaoTextBlock.Text = $"{res.QuedaTensaoVolts:F2} V ({res.PercentualQueda:F1}%)";
            if (ResistenciaParasitaTextBlock != null) ResistenciaParasitaTextBlock.Text = $"{res.ResistenciaParasitaOhms:F4} Ω";
            if (PotenciaDissipadaTextBlock != null) PotenciaDissipadaTextBlock.Text = $"{res.PotenciaDissipadaWatts:F2} W";
            if (BitolaSugeridaTextBlock != null) BitolaSugeridaTextBlock.Text = $"{res.SecaoMinimaRecomendadaMm2:F2} mm²";
            if (DiagnosticoCalculoTextBlock != null) DiagnosticoCalculoTextBlock.Text = res.DiagnosticoTecnico;
            if (AcaoCalculoTextBlock != null) AcaoCalculoTextBlock.Text = res.AcaoRecomendada;

            if (StatusConformidadeBadge != null && StatusConformidadeTextBlock != null)
            {
                switch (res.StatusConformidade)
                {
                    case StatusConformidadeQuedaTensao.Conforme:
                        StatusConformidadeBadge.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Verde #10B981
                        StatusConformidadeTextBlock.Text = "CONFORME (SAE/DIN)";
                        break;
                    case StatusConformidadeQuedaTensao.Toleravel:
                        StatusConformidadeBadge.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amarelo #F59E0B
                        StatusConformidadeTextBlock.Text = "TOLERÁVEL (LIMIAR)";
                        break;
                    default:
                        StatusConformidadeBadge.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Vermelho #EF4444
                        StatusConformidadeTextBlock.Text = "NÃO CONFORME (CRÍTICA)";
                        break;
                }
            }
        }

        private async void SalvarHistoricoCalculoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ultimoResultadoQueda == null)
            {
                CalcularQuedaTensaoButton_Click(sender, e);
            }

            if (_ultimoResultadoQueda == null || _calculadoraQuedaTensaoService == null) return;

            var hist = new HistoricoCalculoQuedaTensao
            {
                DataHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                IdentificacaoCircuito = IdentificacaoCircuitoTextBox?.Text ?? "Circuito Elétrico",
                VeiculoPlaca = PlacaCalculoTextBox?.Text,
                TipoCircuito = (TipoCircuitoComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Potência",
                TensaoFonteVolts = _ultimoResultadoQueda.TensaoFonteVolts,
                TensaoCargaVolts = _ultimoResultadoQueda.TensaoCargaVolts,
                CorrenteAmperes = _ultimoResultadoQueda.CorrenteAmperes,
                QuedaTensaoVolts = _ultimoResultadoQueda.QuedaTensaoVolts,
                ResistenciaParasitaOhms = _ultimoResultadoQueda.ResistenciaParasitaOhms,
                PotenciaDissipadaWatts = _ultimoResultadoQueda.PotenciaDissipadaWatts,
                StatusConformidade = _ultimoResultadoQueda.StatusConformidade.ToString(),
                DiagnosticoTecnico = _ultimoResultadoQueda.DiagnosticoTecnico,
                AcaoRecomendada = _ultimoResultadoQueda.AcaoRecomendada
            };

            var ok = await _calculadoraQuedaTensaoService.SalvarHistoricoAsync(hist);
            if (ok)
            {
                await CarregarHistoricoCalculosQuedaTensaoAsync();
                MessageBox.Show("Laudo de queda de tensão salvo no histórico da oficina com sucesso!", "PRIMOX Workshop", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task CarregarHistoricoCalculosQuedaTensaoAsync()
        {
            if (_calculadoraQuedaTensaoService == null) return;
            try
            {
                var historico = await _calculadoraQuedaTensaoService.ObterHistoricoAsync();
                Dispatcher.Invoke(() =>
                {
                    if (HistoricoCalculosDataGrid != null)
                    {
                        HistoricoCalculosDataGrid.ItemsSource = historico;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar histórico: {ex.Message}");
            }
        }

        private void CopiarLaudoQuedaTensaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ultimoResultadoQueda == null)
            {
                CalcularQuedaTensaoButton_Click(sender, e);
            }

            if (_ultimoResultadoQueda != null)
            {
                Clipboard.SetText(_ultimoResultadoQueda.ObterResumoFormatado());
                MessageBox.Show("Laudo técnico de queda de tensão copiado para a área de transferência!", "PRIMOX Workshop", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EnviarLaudoParaCopilotButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ultimoResultadoQueda == null)
            {
                CalcularQuedaTensaoButton_Click(sender, e);
            }

            if (_ultimoResultadoQueda != null)
            {
                InputTextBox.Text = $"Calcular queda de tensão com {_ultimoResultadoQueda.TensaoFonteVolts:F2}V na fonte, {_ultimoResultadoQueda.TensaoCargaVolts:F2}V na carga e {_ultimoResultadoQueda.CorrenteAmperes:F1}A";
                EnviarButton_Click(sender, e);
            }
        }

        #endregion
    }
}
