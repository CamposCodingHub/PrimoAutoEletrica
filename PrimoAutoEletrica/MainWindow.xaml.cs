using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Catalogo;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica
{
    public partial class MainWindow : Window
    {
        private readonly Funcionario _funcionarioLogado;
        private readonly LoggerService _logger;
        private readonly INavigationService _navigationService;
        private readonly PermissionService _permissionService;
        private readonly ThemeService _themeService;
        private readonly DisplayDensityService _densityService;
        private readonly SidebarLayoutService _sidebarLayoutService;
        private readonly UserSessionService _userSessionService;
        private readonly LocalizationService _localizationService;
        private SessionInactivityService? _sessionInactivityService;
        private readonly DispatcherTimer _shellNotificationTimer;
        private readonly Dictionary<string, Button> _menuButtons;
        private int _navigationHistoryDepth;
        private bool _logoutInProgress;
        private bool _refreshEmAndamento;
        private bool _commandPaletteOpen;
        private object? _refreshButtonContentOriginal;
        private ShellNotificationRequest? _currentShellNotification;
        private readonly Dictionary<string, string> _moduleDescriptions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard"] = "Visao executiva consolidada da operacao e indicadores prioritarios.",
            ["Clientes"] = "Cadastro, relacionamento e historico comercial dos clientes.",
            ["Veiculos"] = "Base unificada de veiculos, placas e dados tecnicos.",
            ["AutoEletricaTecnica"] = "Diagnostico guiado, prontuario eletrico, biblioteca tecnica e sugestoes de pecas.",
            ["Orcamentos"] = "Fluxo comercial de propostas, negociacoes e conversoes.",
            ["OrdensServico"] = "Execucao operacional da oficina com status e acompanhamento.",
            ["OficinaKanban"] = "Kanban profissional da oficina com checklist, timeline e comunicacao.",
            ["PDV"] = "Atendimento rapido, carrinho de venda e fechamento de caixa.",
            ["Estoque"] = "Controle de produtos, movimentacoes e reposicao.",
            ["CatalogoPecas"] = "Base tecnica importada de catalogos, com revisao antes da conversao para estoque.",
            ["ImportarNFe"] = "Central de importacao de XMLs, historico fiscal e conferencia operacional.",
            ["Financeiro"] = "Receitas, despesas, fluxo de caixa e acompanhamento financeiro.",
            ["Fornecedores"] = "Relacionamento com parceiros, compras e abastecimento.",
            ["Funcionarios"] = "Equipe, perfis de acesso e administracao interna.",
            ["Agendamentos"] = "Planejamento operacional e agenda inteligente da oficina.",
            ["Relatorios"] = "Analise gerencial, indicadores e visao executiva."
        };

        public MainWindow(Funcionario funcionarioLogado)
        {
            InitializeComponent();

            _funcionarioLogado = funcionarioLogado ?? throw new ArgumentNullException(nameof(funcionarioLogado));
            _logger = App.Logger;
            _menuButtons = CriarMapaMenus();
            _shellNotificationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(9)
            };
            _shellNotificationTimer.Tick += (_, _) => OcultarNotificacaoShell();

            _permissionService = new PermissionService(_funcionarioLogado, _logger);
            _navigationService = new NavigationService(_permissionService, _logger);
            _themeService = new ThemeService();
            _densityService = new DisplayDensityService();
            _sidebarLayoutService = new SidebarLayoutService();
            _userSessionService = new UserSessionService(App.Database, _logger, App.Session);
            _localizationService = LocalizationService.Instance;
            ConfigurarMonitorInatividade();

            _navigationService.NavigationCompleted += OnNavigationCompleted;
            _navigationService.NavigationError += OnNavigationError;
            _navigationService.NavigationStateChanged += OnNavigationStateChanged;
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;

            // Aplicar tema salvo ao iniciar
            _themeService.ApplyTheme(_themeService.GetCurrentTheme());
            AtualizarTextoBotaoTema();
            _densityService.ApplyDensity(_densityService.GetCurrentDensity());
            AtualizarTextoBotaoDensidade();
            AplicarLayoutSidebar(_sidebarLayoutService.IsExpanded);

            // Configurar idioma
            ConfigurarIdioma();

            CarregarInformacoesUsuario();
            AplicarPermissoesMenu();
            ConfigurarBuscaGlobal();
            ConfigurarNotificacoesShell();
            RegistrarComandosShell();
            AtualizarEstadoNavegacao();
            NavegarPara("Dashboard");
        }

        private void SidebarToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _sidebarLayoutService.Toggle();
            AplicarLayoutSidebar(_sidebarLayoutService.IsExpanded);
        }

        private void CommandCenterButton_Click(object sender, RoutedEventArgs e) => AbrirCommandPalette();

        private void MenuHelp_Click(object sender, RoutedEventArgs e) => NavegarPara("Help");

        private void AplicarLayoutSidebar(bool expanded)
        {
            SidebarColumn.Width = new GridLength(expanded
                ? SidebarLayoutService.ExpandedWidth
                : SidebarLayoutService.CollapsedWidth);

            SidebarRoot.Padding = expanded ? new Thickness(12, 16, 12, 16) : new Thickness(8, 12, 8, 12);
            SidebarBrandExpanded.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            SidebarBrandCompact.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
            SidebarToggleGlyph.Text = expanded ? "«" : "»";
            SidebarToggleButton.ToolTip = expanded ? "Recolher menu" : "Expandir menu";
            SidebarUserPanel.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;

            var labelVisibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            foreach (var text in FindTaggedTextBlocksLogical(SidebarRoot, "SidebarLabel"))
            {
                text.Visibility = labelVisibility;
            }

            foreach (var section in FindTaggedTextBlocksLogical(SidebarRoot, "SidebarSection"))
            {
                section.Visibility = labelVisibility;
            }

            foreach (var button in EnumerarBotoesSidebar())
            {
                button.HorizontalContentAlignment = expanded ? HorizontalAlignment.Left : HorizontalAlignment.Center;
                button.Padding = expanded ? new Thickness(10, 0, 10, 0) : new Thickness(0);
                if (button.Content is Panel panel)
                {
                    foreach (var child in panel.Children.OfType<FrameworkElement>())
                    {
                        if (child is Image image)
                        {
                            image.Margin = expanded ? new Thickness(0, 0, 10, 0) : new Thickness(0);
                            image.HorizontalAlignment = HorizontalAlignment.Center;
                        }
                    }
                }
            }

            AtualizarChipsCommandBar();
        }

        private void AtualizarChipsCommandBar()
        {
            if (HeaderDateChip == null)
            {
                return;
            }

            HeaderDateChip.Visibility = ActualWidth >= 1500 ? Visibility.Visible : Visibility.Collapsed;
        }

        private IEnumerable<Button> EnumerarBotoesSidebar()
        {
            foreach (var button in _menuButtons.Values)
            {
                yield return button;
            }

            yield return MenuConfiguracoes;
            yield return MenuHelp;
            yield return MenuSair;
        }

        private static IEnumerable<TextBlock> FindTaggedTextBlocksLogical(DependencyObject root, string tag)
        {
            if (root is TextBlock text && string.Equals(text.Tag as string, tag, StringComparison.Ordinal))
            {
                yield return text;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                foreach (var nested in FindTaggedTextBlocksLogical(child, tag))
                {
                    yield return nested;
                }
            }

            if (root is ContentControl { Content: DependencyObject content })
            {
                foreach (var nested in FindTaggedTextBlocksLogical(content, tag))
                {
                    yield return nested;
                }
            }
        }

        private void ConfigurarIdioma()
        {
            // Definir idioma atual no ComboBox
            var currentCulture = _localizationService.CurrentCulture;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == currentCulture.Name)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                var languageCode = selectedItem.Tag.ToString();
                if (!string.IsNullOrEmpty(languageCode))
                {
                    _localizationService.SetLanguage(languageCode);
                    // Aqui você pode adicionar lógica para atualizar UI se necessário
                }
            }
        }

        // Commands para atalhos de teclado
        public ICommand NavigateToDashboardCommand => new RelayCommand(() => NavegarPara("Dashboard"));
        public ICommand NavigateToClientsCommand => new RelayCommand(() => NavegarPara("Clientes"));
        public ICommand NavigateToInventoryCommand => new RelayCommand(() => NavegarPara("Estoque"));
        public ICommand NavigateToFinanceCommand => new RelayCommand(() => NavegarPara("Financeiro"));
        public ICommand NavigateToReportsCommand => new RelayCommand(() => NavegarPara("Relatorios"));
        public ICommand NavigateToSettingsCommand => new RelayCommand(() => AbrirConfiguracoesSistema());

        private void RegistrarComandosShell()
        {
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.OpenCommandPalette, (_, _) => AbrirCommandPalette()));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.ShowShortcutsHelp, (_, _) => AbrirAjudaAtalhos()));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.NavigateHelp, (_, _) => NavegarPara("Help")));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.NavigateOrcamentos, (_, _) => NavegarPara("Orcamentos")));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.NavigateOrdensServico, (_, _) => NavegarPara("OrdensServico")));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.NavigateClientes, (_, _) => NavegarPara("Clientes")));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.NavigateEstoque, (_, _) => NavegarPara("Estoque")));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.NavigateSettings, (_, _) => AbrirConfiguracoesSistema()));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.RefreshModule, (_, _) => AtualizarModuloComFeedback()));
            CommandBindings.Add(new CommandBinding(ShellRoutedCommands.FocusGlobalSearch, (_, _) => FocarBuscaGlobal()));
        }

        private static string ObterCategoriaCommandCenter(string moduleKey)
        {
            return moduleKey switch
            {
                "Dashboard" or "Agendamentos" or "Orcamentos" or "OrdensServico"
                    or "OficinaKanban" or "PDV" or "ImportarNFe" => "Operação",
                "Clientes" or "Veiculos" or "AutoEletricaTecnica" or "Estoque"
                    or "CatalogoPecas" or "Fornecedores" or "Funcionarios" => "Cadastros",
                "Financeiro" or "Relatorios" => "Gestão",
                _ => "Sistema"
            };
        }

        private void AbrirAjudaAtalhos()
        {
            var janela = new AtalhosTecladoWindow();
            WindowOwnerHelper.ConfigureOwner(janela, this);
            janela.ShowDialog();
        }

        private void FocarBuscaGlobal() => GlobalSearch.FocusSearch();

        private void AtualizarModuloComFeedback()
        {
            if (_refreshEmAndamento)
                return;

            _refreshEmAndamento = true;
            _refreshButtonContentOriginal ??= RefreshButton.Content;
            RefreshButton.IsEnabled = false;
            RefreshButton.Content = "Atualizando...";

            try
            {
                AtualizarModuloAtual();
            }
            finally
            {
                RefreshButton.Content = _refreshButtonContentOriginal;
                AtualizarEstadoNavegacao();
                _refreshEmAndamento = false;
            }
        }

        private void AbrirCommandPalette()
        {
            if (_commandPaletteOpen)
                return;

            _commandPaletteOpen = true;
            try
            {
                var janela = new CommandPaletteWindow(CriarItensCommandPalette());
                WindowOwnerHelper.ConfigureOwner(janela, this);
                if (janela.ShowDialog() == true)
                    janela.SelectedAction?.Invoke();
            }
            finally
            {
                _commandPaletteOpen = false;
            }
        }

        private List<CommandPaletteItem> CriarItensCommandPalette()
        {
            var itens = new List<CommandPaletteItem>
            {
                new()
                {
                    Id = "palette-help-shortcuts",
                    Title = "Atalhos do teclado",
                    Subtitle = "Lista dos atalhos implementados",
                    Category = "Ajuda",
                    ShortcutHint = "Ctrl+Shift+/",
                    Execute = AbrirAjudaAtalhos
                },
                new()
                {
                    Id = "palette-global-search",
                    Title = "Buscar registros",
                    Subtitle = "Foca a busca global do cabecalho",
                    Category = "Busca",
                    Execute = FocarBuscaGlobal
                },
                new()
                {
                    Id = "palette-refresh",
                    Title = "Atualizar modulo",
                    Subtitle = "Recarrega a tela atual",
                    Category = "Sistema",
                    ShortcutHint = "F5",
                    Execute = AtualizarModuloComFeedback
                }
            };

            foreach (var module in _moduleDescriptions)
            {
                if (!_permissionService.TemPermissao(module.Key))
                    continue;

                var key = module.Key;
                itens.Add(new CommandPaletteItem
                {
                    Id = $"nav-{key}",
                    Title = $"Abrir {key}",
                    Subtitle = module.Value,
                    Category = ObterCategoriaCommandCenter(key),
                    ShortcutHint = key switch
                    {
                        "Orcamentos" => "F2",
                        "OrdensServico" => "F3",
                        "Clientes" => "F4",
                        "Estoque" => "F6",
                        _ => string.Empty
                    },
                    Execute = () => NavegarPara(key)
                });
            }

            itens.Add(new CommandPaletteItem
            {
                Id = "nav-help",
                Title = "Abrir Ajuda",
                Subtitle = "Central de ajuda do sistema",
                Category = "Ajuda",
                ShortcutHint = "F1",
                Execute = () => NavegarPara("Help")
            });

            if (_permissionService.TemPermissaoCodigo("SISTEMA_CONFIGURAR"))
            {
                itens.Add(new CommandPaletteItem
                {
                    Id = "nav-config",
                    Title = "Abrir Configuracoes",
                    Subtitle = "Preferencias e administracao do sistema",
                    Category = "Sistema",
                    ShortcutHint = "F12",
                    Execute = () => AbrirConfiguracoesSistema()
                });
            }

            if (_permissionService.TemPermissaoCodigo("CLIENTES_CRIAR"))
            {
                itens.Add(new CommandPaletteItem
                {
                    Id = "new-cliente",
                    Title = "Novo cliente",
                    Subtitle = "Abre o cadastro de cliente",
                    Category = "Criar",
                    Execute = AbrirNovoClienteRapido
                });
            }

            if (_permissionService.TemPermissaoCodigo("VEICULOS_CRIAR"))
            {
                itens.Add(new CommandPaletteItem
                {
                    Id = "new-veiculo",
                    Title = "Novo veiculo",
                    Subtitle = "Abre o cadastro de veiculo",
                    Category = "Criar",
                    Execute = AbrirNovoVeiculoRapido
                });
            }

            if (_permissionService.TemPermissaoCodigo("ORCAMENTOS_CRIAR"))
            {
                itens.Add(new CommandPaletteItem
                {
                    Id = "new-orcamento",
                    Title = "Novo orcamento",
                    Subtitle = "Abre a tela de novo orcamento",
                    Category = "Criar",
                    Execute = AbrirNovoOrcamentoRapido
                });
            }

            if (_permissionService.TemPermissaoCodigo("ORDENS_SERVICO_EDITAR"))
            {
                itens.Add(new CommandPaletteItem
                {
                    Id = "new-os",
                    Title = "Nova OS",
                    Subtitle = "Abre a criacao de ordem de servico",
                    Category = "Criar",
                    Execute = AbrirNovaOsRapido
                });
            }

            return itens
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Title)
                .ToList();
        }

        private void AbrirNovoClienteRapido()
        {
            var janela = new Views.Clientes.NovoClienteWindow();
            WindowOwnerHelper.ConfigureOwner(janela, this);
            if (janela.ShowDialog() == true)
            {
                GlobalSearch.InvalidateSearchData();
                if (string.Equals(_navigationService.CurrentModule, "Clientes", StringComparison.OrdinalIgnoreCase))
                    AtualizarModuloAtual();
            }
        }

        private void AbrirNovoVeiculoRapido()
        {
            var clientes = App.Repositories.Clientes.ObterTodos().ToDictionary(c => c.Id, c => c);
            var janela = new NovoVeiculoWindow(App.Database, clientes);
            WindowOwnerHelper.ConfigureOwner(janela, this);
            if (janela.ShowDialog() == true)
            {
                GlobalSearch.InvalidateSearchData();
                if (string.Equals(_navigationService.CurrentModule, "Veiculos", StringComparison.OrdinalIgnoreCase))
                    AtualizarModuloAtual();
            }
        }

        private void AbrirNovoOrcamentoRapido()
        {
            var janela = new NovoOrcamentoWindow();
            WindowOwnerHelper.ConfigureOwner(janela, this);
            if (janela.ShowDialog() == true)
            {
                GlobalSearch.InvalidateSearchData();
                if (string.Equals(_navigationService.CurrentModule, "Orcamentos", StringComparison.OrdinalIgnoreCase))
                    AtualizarModuloAtual();
            }
        }

        private void AbrirNovaOsRapido()
        {
            var janela = new OrdemServicoWindow(App.Database);
            WindowOwnerHelper.ConfigureOwner(janela, this);
            if (janela.ShowDialog() == true)
            {
                GlobalSearch.InvalidateSearchData();
                if (string.Equals(_navigationService.CurrentModule, "OrdensServico", StringComparison.OrdinalIgnoreCase))
                    AtualizarModuloAtual();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _sessionInactivityService?.Start();
            SizeChanged += (_, _) => AtualizarChipsCommandBar();
            AplicarLayoutSidebar(_sidebarLayoutService.IsExpanded);
            AtualizarChipsCommandBar();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            ShellNotificationService.NotificationPublished -= OnShellNotificationPublished;
            _shellNotificationTimer.Stop();

            if (_sessionInactivityService == null)
            {
                return;
            }

            _sessionInactivityService.SessionExpired -= OnSessionExpired;
            _sessionInactivityService.Dispose();
        }

        public string CurrentModuleName => _navigationService.CurrentModule;
        public bool CanNavigateBack => _navigationService.CanNavigateBack;
        public FrameworkElement? CurrentContentElement => MainContent.Content as FrameworkElement;

        private void CarregarInformacoesUsuario()
        {
            var nomeUsuario = _permissionService.ObterNomeUsuario();
            var perfil = _permissionService.ObterPerfil();

            Title = $"PRIMOX - {nomeUsuario} ({perfil})";
            SidebarUserNameText.Text = nomeUsuario;
            SidebarProfileText.Text = perfil;
            HeaderUserNameText.Text = nomeUsuario;
            HeaderProfileText.Text = perfil;
            HeaderDateText.Text = DateTime.Now.ToString("dd/MM/yyyy");
            CarregarInformacoesSistema();
        }

        private void CarregarInformacoesSistema()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionText = version == null
                ? "v0.0.0"
                : $"v{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";

            var settings = DatabaseConnectionSettingsService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
            var bancoDescricao = settings.IsSQLite
                ? $"SQLite ({Path.GetFileName(App.Database.DatabasePath)})"
                : $"SqlServer planejado ({settings.SqlServerHost}/{settings.SqlServerDatabase})";

            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var dataBuild = File.Exists(assemblyPath)
                ? File.GetLastWriteTime(assemblyPath)
                : DateTime.Now;

            HeaderSystemInfoText.Text = $"{versionText} | Ambiente {App.RuntimeModeName} | Banco {bancoDescricao} | Build {dataBuild:dd/MM/yyyy HH:mm}";
        }

        private void AplicarPermissoesMenu()
        {
            var modulosPermitidos = _permissionService.ObterModulosPermitidos();

            MenuDashboard.IsEnabled = modulosPermitidos.Contains("Dashboard");
            MenuClientes.IsEnabled = modulosPermitidos.Contains("Clientes");
            MenuVeiculos.IsEnabled = modulosPermitidos.Contains("Veiculos");
            MenuAutoEletricaTecnica.IsEnabled = _permissionService.TemPermissao("AutoEletricaTecnica");
            MenuOrcamentos.IsEnabled = modulosPermitidos.Contains("Orcamentos");
            MenuOS.IsEnabled = modulosPermitidos.Contains("OrdensServico");
            MenuOficinaKanban.IsEnabled = _permissionService.TemPermissao("OficinaKanban");
            MenuPDV.IsEnabled = modulosPermitidos.Contains("PDV");
            MenuEstoque.IsEnabled = modulosPermitidos.Contains("Estoque");
            MenuCatalogoPecas.IsEnabled = modulosPermitidos.Contains("CatalogoPecas");
            MenuImportarNFe.IsEnabled = modulosPermitidos.Contains("ImportarNFe");
            MenuFinanceiro.IsEnabled = modulosPermitidos.Contains("Financeiro");
            MenuRelatorios.IsEnabled = modulosPermitidos.Contains("Relatorios");
            MenuFornecedores.IsEnabled = modulosPermitidos.Contains("Fornecedores");
            MenuFuncionarios.IsEnabled = modulosPermitidos.Contains("Funcionarios");
            MenuAgendamento.IsEnabled = modulosPermitidos.Contains("Agendamentos");
            MenuHelp.IsEnabled = true;
            MenuConfiguracoes.IsEnabled = _permissionService.TemPermissaoCodigo("SISTEMA_CONFIGURAR");
            MenuConfiguracoes.Visibility = MenuConfiguracoes.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ConfigurarBuscaGlobal()
        {
            GlobalSearch.ResultSelected += (_, args) =>
            {
                ProcessarSelecaoBusca(args.Result);
            };

            GlobalSearch.ConfigureSearchDataProvider(CriarIndiceBuscaGlobal);
        }

        private void ConfigurarNotificacoesShell()
        {
            ShellNotificationService.NotificationPublished -= OnShellNotificationPublished;
            ShellNotificationService.NotificationPublished += OnShellNotificationPublished;
        }

        private List<SearchResult> CriarIndiceBuscaGlobal()
        {
            var results = new List<SearchResult>();

            foreach (var module in _moduleDescriptions)
            {
                if (_permissionService.TemPermissao(module.Key))
                {
                    results.Add(new SearchResult
                    {
                        Titulo = module.Key,
                        Subtitulo = module.Value,
                        Tipo = "Modulo",
                        Acao = module.Key
                    });
                }
            }

            if (_permissionService.TemPermissaoCodigo("SISTEMA_CONFIGURAR"))
            {
                results.Add(new SearchResult
                {
                    Titulo = "Configuracoes do Sistema",
                    Subtitulo = "Timeout de sessao, atalhos administrativos e informacoes do ambiente.",
                    Tipo = "Sistema",
                    Acao = "ConfiguracoesSistema"
                });
            }

            AdicionarRegistrosBusca(results);
            return results;
        }

        private void AdicionarRegistrosBusca(List<SearchResult> results)
        {
            try
            {
                foreach (var cliente in App.Repositories.Clientes.ObterTodos().Take(300))
                {
                    results.Add(new SearchResult
                    {
                        Titulo = cliente.Nome,
                        Subtitulo = $"Cliente | {cliente.Telefone} | {cliente.Documento}",
                        Tipo = "Cliente",
                        Id = cliente.Id.ToString(),
                        Acao = "Clientes"
                    });
                }

                foreach (var veiculo in App.Repositories.Clientes.ObterTodosVeiculos().Take(300))
                {
                    results.Add(new SearchResult
                    {
                        Titulo = string.IsNullOrWhiteSpace(veiculo.Placa) ? $"{veiculo.Marca} {veiculo.Modelo}" : veiculo.Placa,
                        Subtitulo = $"Veiculo | {veiculo.Marca} {veiculo.Modelo} {veiculo.Ano}",
                        Tipo = "Veiculo",
                        Id = veiculo.Id.ToString(),
                        Acao = "Veiculos"
                    });
                }

                foreach (var produto in App.Repositories.Produtos.ObterTodos().Take(300))
                {
                    results.Add(new SearchResult
                    {
                        Titulo = produto.Nome,
                        Subtitulo = $"Produto | {produto.Codigo} | Estoque {produto.QuantidadeEstoque}",
                        Tipo = "Produto",
                        Id = produto.Id.ToString(),
                        Acao = "Estoque"
                    });
                }

                foreach (var item in new CatalogoPecasService().ObterTodos().Take(250))
                {
                    results.Add(new SearchResult
                    {
                        Titulo = item.CodigoFabricante,
                        Subtitulo = $"Catalogo | {item.Marca} | {item.NomeExibicao}",
                        Tipo = "Catalogo",
                        Id = item.Id.ToString(),
                        Acao = "CatalogoPecas"
                    });
                }

                foreach (var ordem in App.Repositories.OrdensServico.ObterTodos(incluirInativas: true).Take(200))
                {
                    results.Add(new SearchResult
                    {
                        Titulo = ordem.Numero,
                        Subtitulo = $"OS | {ordem.ClienteNomeSnapshot} | {ordem.Status}",
                        Tipo = "OS",
                        Id = ordem.Id.ToString(),
                        Acao = "OrdensServico"
                    });
                }

                var agendamentoService = new AgendamentoDatabaseService();
                foreach (var agendamento in agendamentoService.ObterTodosAgendamentos().Take(200))
                {
                    results.Add(new SearchResult
                    {
                        Titulo = agendamento.Numero,
                        Subtitulo = $"Agendamento | {agendamento.ClienteNome} | {agendamento.DataAgendamento:dd/MM/yyyy}",
                        Tipo = "Agendamento",
                        Id = agendamento.Id.ToString(),
                        Acao = "Agendamentos"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Busca global carregada parcialmente: {ex.Message}");
            }
        }

        private static string ResolverModuloBusca(SearchResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.Acao))
            {
                return result.Acao;
            }

            return result.Tipo switch
            {
                "Cliente" => "Clientes",
                "Veiculo" => "Veiculos",
                "Produto" => "Estoque",
                "Catalogo" => "CatalogoPecas",
                "Agendamento" => "Agendamentos",
                "OS" => "OrdensServico",
                _ => string.Empty
            };
        }

        private bool ProcessarSelecaoBusca(SearchResult result)
        {
            var destino = string.Equals(result.Acao, "ConfiguracoesSistema", StringComparison.OrdinalIgnoreCase)
                ? "ConfiguracoesSistema"
                : ResolverModuloBusca(result);

            if (string.Equals(result.Acao, "ConfiguracoesSistema", StringComparison.OrdinalIgnoreCase))
            {
                var sucessoConfiguracoes = AbrirConfiguracoesSistema();
                RegistrarEventoShell("BuscaGlobalSelecionada", result.Tipo, result.Titulo, $"Destino={destino}; Sucesso={sucessoConfiguracoes}", sucessoConfiguracoes, sucessoConfiguracoes ? "Info" : "Warning");
                return sucessoConfiguracoes;
            }

            if (string.IsNullOrWhiteSpace(destino))
            {
                RegistrarEventoShell("BuscaGlobalSelecionada", result.Tipo, result.Titulo, "DestinoNaoResolvido", sucesso: false, severidade: "Warning");
                ExibirNotificacaoShell(new ShellNotificationRequest
                {
                    Title = "Busca sem destino",
                    Message = $"Nao foi possivel determinar o destino para '{result.Titulo}'.",
                    Details = $"Tipo: {result.Tipo}",
                    Type = ShellNotificationType.Warning,
                    Source = "BuscaGlobal"
                });
                return false;
            }

            var sucesso = NavegarPara(destino);
            RegistrarEventoShell("BuscaGlobalSelecionada", result.Tipo, result.Titulo, $"Destino={destino}; Sucesso={sucesso}", sucesso, sucesso ? "Info" : "Warning");
            return sucesso;
        }

        private void MenuDashboard_Click(object sender, RoutedEventArgs e) => NavegarPara("Dashboard");
        private void MenuClientes_Click(object sender, RoutedEventArgs e) => NavegarPara("Clientes");
        private void MenuVeiculos_Click(object sender, RoutedEventArgs e) => NavegarPara("Veiculos");
        private void MenuAutoEletricaTecnica_Click(object sender, RoutedEventArgs e) => NavegarPara("AutoEletricaTecnica");
        private void MenuOrcamentos_Click(object sender, RoutedEventArgs e) => NavegarPara("Orcamentos");
        private void MenuOS_Click(object sender, RoutedEventArgs e) => NavegarPara("OrdensServico");
        private void MenuOficinaKanban_Click(object sender, RoutedEventArgs e) => NavegarPara("OficinaKanban");
        private void MenuPDV_Click(object sender, RoutedEventArgs e) => NavegarPara("PDV");
        private void MenuEstoque_Click(object sender, RoutedEventArgs e) => NavegarPara("Estoque");
        private void MenuCatalogoPecas_Click(object sender, RoutedEventArgs e) => NavegarPara("CatalogoPecas");
        private void MenuFinanceiro_Click(object sender, RoutedEventArgs e) => NavegarPara("Financeiro");
        private void MenuFornecedores_Click(object sender, RoutedEventArgs e) => NavegarPara("Fornecedores");
        private void MenuFuncionarios_Click(object sender, RoutedEventArgs e) => NavegarPara("Funcionarios");
        private void MenuAgendamento_Click(object sender, RoutedEventArgs e) => NavegarPara("Agendamentos");
        private void MenuRelatorios_Click(object sender, RoutedEventArgs e) => NavegarPara("Relatorios");
        private void MenuConfiguracoes_Click(object sender, RoutedEventArgs e) => AbrirConfiguracoesSistema();

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavegarVoltar();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            AtualizarModuloComFeedback();
        }

        private void MenuImportarNFe_Click(object sender, RoutedEventArgs e)
        {
            AbrirImportarNFe();
        }

        private void MenuSair_Click(object sender, RoutedEventArgs e)
        {
            var result = App.IsAutomatedTestMode
                ? MessageBoxResult.Yes
                : MessageBox.Show(
                    "Deseja realmente sair do sistema?",
                    "Confirmacao",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _logger.LogInfo($"Logout solicitado por '{_funcionarioLogado.Email}'.");
            App.Audit.RegistrarLogin("Logout", _funcionarioLogado.Email, sucesso: true);
            EncerrarSessaoParaLogin();
        }

        public bool NavigateToModuleForAutomation(string moduleName, bool forceReload = false)
        {
            return NavegarPara(moduleName, forceReload);
        }

        public bool NavigateBackForAutomation()
        {
            return NavegarVoltar();
        }

        public bool RefreshCurrentModuleForAutomation()
        {
            return AtualizarModuloAtual();
        }

        public bool NavigateFromSearchResultForAutomation(SearchResult result)
        {
            return ProcessarSelecaoBusca(result);
        }

        public bool IsGlobalSearchPrimedForAutomation()
        {
            return GlobalSearch.IsDataLoaded;
        }

        public void EnsureGlobalSearchReadyForAutomation()
        {
            GlobalSearch.EnsureDataLoadedForAutomation();
        }

        public bool HasShellNotificationForAutomation(string expectedSnippet)
        {
            if (_currentShellNotification == null || string.IsNullOrWhiteSpace(expectedSnippet))
            {
                return false;
            }

            return _currentShellNotification.Title.Contains(expectedSnippet, StringComparison.OrdinalIgnoreCase)
                || _currentShellNotification.Message.Contains(expectedSnippet, StringComparison.OrdinalIgnoreCase)
                || _currentShellNotification.Details.Contains(expectedSnippet, StringComparison.OrdinalIgnoreCase);
        }

        public bool InvokeShellNotificationActionForAutomation()
        {
            return ExecutarAcaoNotificacaoAtual();
        }

        public bool OpenImportarNFeForAutomation()
        {
            return AbrirImportarNFe(automationOnly: true);
        }

        public bool OpenConfiguracoesForAutomation()
        {
            return AbrirConfiguracoesSistema(automationOnly: true);
        }

        public void InvalidarCacheModulo(string moduleName)
        {
            _navigationService.RemoveFromCache(moduleName);
        }

        public bool IsMenuEnabledForAutomation(string moduleName)
        {
            return _menuButtons.TryGetValue(moduleName, out var button) && button.IsEnabled;
        }

        public bool IsModuleHighlightedForAutomation(string moduleName)
        {
            if (!_menuButtons.TryGetValue(moduleName, out var button))
            {
                return false;
            }

            var activeStyle = TryFindResource("SidebarItemActive") as Style;
            return ReferenceEquals(button.Style, activeStyle)
                || button.FontWeight == FontWeights.SemiBold
                || button.BorderThickness.Left > 0;
        }

        public bool ToggleSidebarForAutomation()
        {
            _sidebarLayoutService.Toggle();
            AplicarLayoutSidebar(_sidebarLayoutService.IsExpanded);
            return true;
        }

        public bool IsSidebarExpandedForAutomation() => _sidebarLayoutService.IsExpanded;

        public double GetSidebarWidthForAutomation() => SidebarColumn.Width.Value;

        public double GetCommandBarHeightForAutomation()
        {
            if (Content is Grid root
                && root.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetColumn(g) == 1) is Grid contentGrid
                && contentGrid.RowDefinitions.Count > 0
                && contentGrid.RowDefinitions[0].Height.IsAbsolute)
            {
                return contentGrid.RowDefinitions[0].Height.Value;
            }

            return 56d;
        }

        public int ValidateCommandCenterForAutomation()
        {
            var itens = CriarItensCommandPalette();
            if (itens.Count == 0)
            {
                throw new InvalidOperationException("Command Center sem itens.");
            }

            var janela = new CommandPaletteWindow(itens);
            try
            {
                WindowOwnerHelper.ConfigureOwner(janela, this);
                janela.ShowInTaskbar = false;
                janela.WindowStartupLocation = WindowStartupLocation.Manual;
                janela.Left = -10000;
                janela.Top = -10000;
                janela.Show();
                janela.UpdateLayout();
                WaitUiPump();

                var count = janela.VisibleItemCount;
                if (count == 0)
                {
                    throw new InvalidOperationException("Command Center nao listou comandos.");
                }

                return count;
            }
            finally
            {
                if (janela.IsVisible)
                {
                    janela.Close();
                }
            }
        }

        private static void WaitUiPump()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        private bool NavegarPara(string moduleName, bool forceReload = false)
        {
            try
            {
                UserControl? control;
                if (forceReload && string.Equals(_navigationService.CurrentModule, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    control = _navigationService.RefreshCurrent();
                }
                else
                {
                    if (forceReload)
                    {
                        _navigationService.RemoveFromCache(moduleName);
                    }

                    control = _navigationService.Navigate(moduleName);
                }

                if (control == null)
                {
                    App.Audit.RegistrarNavegacao(moduleName, sucesso: false, "NavigationService retornou controle nulo.");
                    ExibirErroCarregamento($"Nao foi possivel carregar o modulo {moduleName}.");
                    ExibirNotificacaoShell(new ShellNotificationRequest
                    {
                        Title = "Modulo indisponivel",
                        Message = $"O modulo '{moduleName}' nao pode ser carregado agora.",
                        Details = "Revise permissao, dependencias e logs da navegacao para continuar.",
                        Type = ShellNotificationType.Warning,
                        Source = "Navegacao"
                    });
                    return false;
                }

                MainContent.Content = control;
                AtualizarContextoModulo(moduleName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao navegar para o modulo '{moduleName}'.", ex);
                ExibirErro($"Erro ao navegar para {moduleName}: {ex.Message}");
                return false;
            }
        }

        private bool NavegarVoltar()
        {
            try
            {
                var control = _navigationService.NavigateBack();
                if (control == null)
                {
                    AtualizarEstadoNavegacao();
                    RegistrarEventoShell("VoltarHistorico", "Modulo", _navigationService.CurrentModule, "SemHistoricoDisponivel", sucesso: false, severidade: "Warning");
                    return false;
                }

                MainContent.Content = control;
                AtualizarContextoModulo(_navigationService.CurrentModule);
                RegistrarEventoShell("VoltarHistorico", "Modulo", _navigationService.CurrentModule, "Sucesso");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao voltar na navegacao.", ex);
                ExibirErro($"Erro ao voltar na navegacao: {ex.Message}");
                return false;
            }
        }

        private bool AtualizarModuloAtual()
        {
            if (string.IsNullOrWhiteSpace(_navigationService.CurrentModule))
            {
                AtualizarEstadoNavegacao();
                RegistrarEventoShell("AtualizarModuloAtual", "Modulo", "SemModulo", "Refresh solicitado sem modulo ativo.", sucesso: false, severidade: "Warning");
                return false;
            }

            try
            {
                var control = _navigationService.RefreshCurrent();
                if (control == null)
                {
                    ExibirErroCarregamento($"Nao foi possivel atualizar o modulo {_navigationService.CurrentModule}.");
                    return false;
                }

                MainContent.Content = control;
                AtualizarContextoModulo(_navigationService.CurrentModule);
                _logger.LogInfo($"Modulo atualizado manualmente: {_navigationService.CurrentModule}");
                App.Audit.RegistrarAcaoCritica("Navegacao", "AtualizarModulo", "Modulo", _navigationService.CurrentModule);
                RegistrarEventoShell("AtualizarModuloAtual", "Modulo", _navigationService.CurrentModule, "Sucesso");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao atualizar o modulo '{_navigationService.CurrentModule}'.", ex);
                ExibirErro($"Erro ao atualizar {_navigationService.CurrentModule}: {ex.Message}");
                return false;
            }
        }

        private bool AbrirImportarNFe(bool automationOnly = false)
        {
            if (!_permissionService.TemPermissao("ImportarNFe"))
            {
                RegistrarEventoShell("AbrirImportarNFe", "Modulo", "ImportarNFe", "Permissao negada.", sucesso: false, severidade: "Warning");
                ExibirNotificacaoShell(new ShellNotificationRequest
                {
                    Title = "Acesso negado",
                    Message = "Sua sessao nao possui permissao para acessar a importacao de NF-e.",
                    Type = ShellNotificationType.Warning,
                    Source = "Fiscal"
                });
                return false;
            }

            try
            {
                _logger.LogInfo("Abertura da pagina de importacao NF-e.");
                App.Audit.RegistrarAcaoCritica("Fiscal", "AbrirImportacaoNFe", "Modulo", "ImportarNFe");

                var sucesso = NavegarPara("ImportarNFe", forceReload: automationOnly);
                RegistrarEventoShell("AbrirImportarNFe", "Modulo", "ImportarNFe", automationOnly ? "Automacao" : "Sucesso", sucesso);
                return sucesso;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao abrir a pagina de importacao NF-e.", ex);
                ExibirErro($"Erro ao abrir Importar NF-e: {ex.Message}");
                return false;
            }
        }

        private bool AbrirConfiguracoesSistema(bool automationOnly = false)
        {
            if (!_permissionService.TemPermissaoCodigo("SISTEMA_CONFIGURAR"))
            {
                RegistrarEventoShell("AbrirConfiguracoesSistema", "Janela", "ConfiguracoesSistema", "Permissao negada.", sucesso: false, severidade: "Warning");
                ExibirNotificacaoShell(new ShellNotificationRequest
                {
                    Title = "Acesso negado",
                    Message = "Sua sessao nao possui permissao para abrir as configuracoes do sistema.",
                    Type = ShellNotificationType.Warning,
                    Source = "Sistema"
                });
                return false;
            }

            try
            {
                var configuracoesWindow = new ConfiguracoesSistemaWindow(_funcionarioLogado);

                if (automationOnly)
                {
                    PrepararJanelaModalParaAutomacao(configuracoesWindow);
                    RegistrarEventoShell("AbrirConfiguracoesSistema", "Janela", "ConfiguracoesSistema", "Automacao");
                    return true;
                }

                if (IsLoaded && IsVisible)
                {
                    configuracoesWindow.Owner = this;
                }

                var configuracoesAtualizadas = configuracoesWindow.ShowDialog() == true
                    ? configuracoesWindow.ConfiguracoesSalvas
                    : null;

                if (configuracoesAtualizadas != null)
                {
                    ConfigurarMonitorInatividade(configuracoesAtualizadas);
                    CarregarInformacoesSistema();
                    GlobalSearch.InvalidateSearchData();
                }

                RegistrarEventoShell("AbrirConfiguracoesSistema", "Janela", "ConfiguracoesSistema", configuracoesAtualizadas == null ? "FechadoSemSalvar" : "ConfiguracoesAtualizadas");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao abrir a tela de configuracoes do sistema.", ex);
                ExibirErro($"Erro ao abrir Configuracoes: {ex.Message}");
                return false;
            }
        }

        private void OnNavigationCompleted(object? sender, NavigationEventArgs e)
        {
            _logger.LogInfo($"Navegacao concluida para o modulo '{e.ModuleName}'.");
            App.Audit.RegistrarNavegacao(e.ModuleName, sucesso: true);
        }

        private void OnNavigationError(object? sender, NavigationErrorEventArgs e)
        {
            _logger.LogError($"Erro de navegacao para o modulo '{e.ModuleName}'.", e.Exception);
            App.Audit.RegistrarErro("Navegacao", "ErroNavegacao", e.Exception ?? new InvalidOperationException("Erro de navegacao sem excecao."), "Modulo", e.ModuleName);
            var unauthorized = e.Exception is UnauthorizedAccessException;
            ExibirNotificacaoShell(new ShellNotificationRequest
            {
                Title = unauthorized ? "Acesso negado ao modulo" : "Falha de navegacao",
                Message = unauthorized
                    ? $"Sua sessao nao tem permissao para acessar '{e.ModuleName}'."
                    : $"Nao foi possivel abrir o modulo '{e.ModuleName}'.",
                Details = e.Exception?.Message ?? "Erro sem detalhe adicional.",
                Type = unauthorized ? ShellNotificationType.Warning : ShellNotificationType.Error,
                Source = "Navegacao"
            });

            if (!unauthorized)
            {
                ExibirErro($"Erro de navegacao para {e.ModuleName}: {e.Exception?.Message}");
            }
        }

        private void OnNavigationStateChanged(object? sender, NavigationStateChangedEventArgs e)
        {
            _navigationHistoryDepth = e.HistoryDepth;
            AtualizarEstadoNavegacao();
        }

        private void OnSessionExpired(object? sender, SessionInactivityEventArgs e)
        {
            if (_logoutInProgress || App.IsAutomatedTestMode)
            {
                return;
            }

            var detalhes = $"Inatividade de {e.IdleFor.TotalMinutes:F0} minutos. Ultima atividade em {e.LastActivityAt:dd/MM/yyyy HH:mm:ss}.";
            _logger.LogWarning($"Sessao encerrada por inatividade para '{_funcionarioLogado.Email}'. {detalhes}");
            App.Audit.RegistrarLogin("LogoutInatividade", _funcionarioLogado.Email, sucesso: true, detalhes);

            EncerrarSessaoParaLogin(
                exibirMensagem: true,
                mensagem: "Sua sessao foi encerrada por inatividade. Faca login novamente para continuar.");
        }

        private void EncerrarSessaoParaLogin(bool exibirMensagem = false, string? mensagem = null)
        {
            if (_logoutInProgress)
            {
                return;
            }

            _logoutInProgress = true;
            _sessionInactivityService?.Stop();
            _userSessionService.EndSession();
            App.Session.EndSession();

            var loginWindow = new LoginWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();

            if (exibirMensagem && !App.IsAutomatedTestMode && !string.IsNullOrWhiteSpace(mensagem))
            {
                MessageBox.Show(
                    mensagem,
                    "Sessao encerrada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            Close();
        }

        private void ExibirErro(string mensagem)
        {
            if (App.IsSmokeTestMode)
            {
                _logger.LogError($"Dialogo de erro suprimido durante smoke test: {mensagem}");
                return;
            }

            MessageBox.Show(mensagem, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ExibirErroCarregamento(string mensagem)
        {
            MainContent.Content = new TextBlock
            {
                Text = mensagem,
                Foreground = Brushes.Red,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap
            };
        }

        private Dictionary<string, Button> CriarMapaMenus()
        {
            return new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dashboard"] = MenuDashboard,
                ["Clientes"] = MenuClientes,
                ["Veiculos"] = MenuVeiculos,
                ["AutoEletricaTecnica"] = MenuAutoEletricaTecnica,
                ["Orcamentos"] = MenuOrcamentos,
                ["OrdensServico"] = MenuOS,
                ["OficinaKanban"] = MenuOficinaKanban,
                ["PDV"] = MenuPDV,
                ["Estoque"] = MenuEstoque,
                ["CatalogoPecas"] = MenuCatalogoPecas,
                ["ImportarNFe"] = MenuImportarNFe,
                ["Financeiro"] = MenuFinanceiro,
                ["Fornecedores"] = MenuFornecedores,
                ["Funcionarios"] = MenuFuncionarios,
                ["Agendamentos"] = MenuAgendamento,
                ["Relatorios"] = MenuRelatorios,
                ["Help"] = MenuHelp,
                ["Ajuda"] = MenuHelp
            };
        }

        private void AtualizarContextoModulo(string moduleName)
        {
            CurrentModuleText.Text = _menuButtons.TryGetValue(moduleName, out var menuButton)
                ? ObterRotuloMenu(menuButton, moduleName)
                : moduleName;
            HeaderContextText.Text = _moduleDescriptions.TryGetValue(moduleName, out var description)
                ? description
                : "Modulo carregado com navegacao centralizada.";

            AtualizarMenuAtivo(moduleName);
        }

        private void AtualizarMenuAtivo(string moduleName)
        {
            foreach (var menu in _menuButtons.Values)
            {
                if (TryFindResource("SidebarItem") is Style sidebarItemStyle)
                {
                    menu.Style = sidebarItemStyle;
                }
            }

            if (_menuButtons.TryGetValue(moduleName, out var activeButton))
            {
                if (TryFindResource("SidebarItemActive") is Style sidebarItemActiveStyle)
                {
                    activeButton.Style = sidebarItemActiveStyle;
                }
            }
        }

        private void AtualizarEstadoNavegacao()
        {
            BackButton.IsEnabled = _navigationService.CanNavigateBack;
            RefreshButton.IsEnabled = !string.IsNullOrWhiteSpace(_navigationService.CurrentModule);

            var moduloAtual = string.IsNullOrWhiteSpace(_navigationService.CurrentModule)
                ? "sem modulo ativo"
                : _navigationService.CurrentModule;
            var statusHistorico = _navigationHistoryDepth <= 1
                ? "historico inicial"
                : $"{_navigationHistoryDepth} modulos no historico";

            NavigationStateText.Text = $"{moduloAtual} | {statusHistorico}";
        }

        private void OnShellNotificationPublished(object? sender, ShellNotificationRequest request)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnShellNotificationPublished(sender, request));
                return;
            }

            ExibirNotificacaoShell(request);
        }

        private void ExibirNotificacaoShell(ShellNotificationRequest request, bool reiniciarTemporizador = true)
        {
            _currentShellNotification = request;
            ShellNotificationTitleText.Text = string.IsNullOrWhiteSpace(request.Title) ? "Atualizacao do sistema" : request.Title;
            ShellNotificationMessageText.Text = request.Message;
            ShellNotificationDetailsText.Text = request.Details;
            ShellNotificationDetailsText.Visibility = string.IsNullOrWhiteSpace(request.Details) ? Visibility.Collapsed : Visibility.Visible;
            ShellNotificationActionButton.Visibility = string.IsNullOrWhiteSpace(request.ActionModule) || string.IsNullOrWhiteSpace(request.ActionLabel)
                ? Visibility.Collapsed
                : Visibility.Visible;
            ShellNotificationActionButton.Content = request.ActionLabel;

            var (bgKey, borderKey, fgKey) = request.Type switch
            {
                ShellNotificationType.Success => ("ToastSuccessBackgroundBrush", "ToastSuccessBorderBrush", "ToastSuccessForegroundBrush"),
                ShellNotificationType.Warning => ("ToastWarningBackgroundBrush", "ToastWarningBorderBrush", "ToastWarningForegroundBrush"),
                ShellNotificationType.Error => ("ToastErrorBackgroundBrush", "ToastErrorBorderBrush", "ToastErrorForegroundBrush"),
                _ => ("ToastInfoBackgroundBrush", "ToastInfoBorderBrush", "ToastInfoForegroundBrush")
            };

            ShellNotificationBorder.Background = TryFindResource(bgKey) as Brush
                ?? new SolidColorBrush(Color.FromRgb(239, 246, 255));
            ShellNotificationBorder.BorderBrush = TryFindResource(borderKey) as Brush
                ?? new SolidColorBrush(Color.FromRgb(59, 130, 246));
            var foreground = TryFindResource(fgKey) as Brush
                ?? new SolidColorBrush(Color.FromRgb(30, 64, 175));
            ShellNotificationTitleText.Foreground = foreground;
            ShellNotificationMessageText.Foreground = foreground;
            ShellNotificationBorder.Visibility = Visibility.Visible;

            if (reiniciarTemporizador)
            {
                _shellNotificationTimer.Stop();
                _shellNotificationTimer.Start();
            }
        }

        private void OcultarNotificacaoShell()
        {
            _shellNotificationTimer.Stop();
            _currentShellNotification = null;
            ShellNotificationBorder.Visibility = Visibility.Collapsed;
            ShellNotificationActionButton.Visibility = Visibility.Collapsed;
            ShellNotificationDetailsText.Visibility = Visibility.Collapsed;
        }

        private bool ExecutarAcaoNotificacaoAtual()
        {
            if (_currentShellNotification == null || string.IsNullOrWhiteSpace(_currentShellNotification.ActionModule))
            {
                return false;
            }

            var actionModule = _currentShellNotification.ActionModule;
            var sucesso = string.Equals(actionModule, "ConfiguracoesSistema", StringComparison.OrdinalIgnoreCase)
                ? AbrirConfiguracoesSistema()
                : string.Equals(actionModule, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                    ? AbrirImportarNFe()
                    : NavegarPara(actionModule);

            RegistrarEventoShell(
                "NotificacaoShellAcionada",
                "Modulo",
                actionModule,
                $"Origem={_currentShellNotification.Source}; Sucesso={sucesso}",
                sucesso,
                sucesso ? "Info" : "Warning");

            if (sucesso)
            {
                OcultarNotificacaoShell();
            }

            return sucesso;
        }

        private void ShellNotificationActionButton_Click(object sender, RoutedEventArgs e)
        {
            ExecutarAcaoNotificacaoAtual();
        }

        private void ShellNotificationDismissButton_Click(object sender, RoutedEventArgs e)
        {
            OcultarNotificacaoShell();
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _themeService.ToggleTheme();
            AtualizarTextoBotaoTema();
        }

        private void DensityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _densityService.ToggleDensity();
            AtualizarTextoBotaoDensidade();
        }

        private void AtualizarTextoBotaoTema()
        {
            if (ThemeToggleButton != null)
            {
                var temaEscuroAtivo = _themeService.GetCurrentTheme() == AppTheme.Dark;
                ThemeToggleButton.Content = temaEscuroAtivo ? "☀ Claro" : "☾ Escuro";
                ThemeToggleButton.ToolTip = temaEscuroAtivo
                    ? "Alternar para o tema claro"
                    : "Alternar para o tema escuro";
            }
        }

        private void AtualizarTextoBotaoDensidade()
        {
            if (DensityToggleButton != null)
            {
                var modoCompactoAtivo = _densityService.GetCurrentDensity() == DisplayDensity.Compact;
                DensityToggleButton.Content = modoCompactoAtivo ? "Conforto" : "Compacto";
                DensityToggleButton.ToolTip = modoCompactoAtivo
                    ? "Alternar para modo confortavel"
                    : "Alternar para modo compacto";
            }
        }

        private static string ObterRotuloMenu(Button button, string fallback)
        {
            if (button.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            {
                return tag;
            }

            return button.Content?.ToString() ?? fallback;
        }

        private void RegistrarEventoShell(string acao, string entidade, string entidadeId, string detalhes, bool sucesso = true, string severidade = "Info")
        {
            App.Audit.Registrar(
                categoria: "Navegacao",
                acao: acao,
                entidade: entidade,
                entidadeId: entidadeId,
                detalhes: detalhes,
                severidade: severidade,
                sucesso: sucesso);
        }

        private static void PrepararJanelaModalParaAutomacao(Window window)
        {
            try
            {
                window.Width = 1440;
                window.Height = 900;
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = -10000;
                window.Top = -10000;
                window.ShowInTaskbar = false;
                window.ApplyTemplate();
                window.UpdateLayout();
            }
            finally
            {
                window.Close();
            }
        }

        private void ConfigurarMonitorInatividade(DatabaseConnectionSettings? settings = null)
        {
            if (App.IsAutomatedTestMode)
            {
                _sessionInactivityService = null;
                return;
            }

            var configuracoes = settings ?? DatabaseConnectionSettingsService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
            var novoMonitor = new SessionInactivityService(App.Session, _logger, configuracoes.GetSessionInactivityTimeout());

            if (_sessionInactivityService != null)
            {
                _sessionInactivityService.SessionExpired -= OnSessionExpired;
                _sessionInactivityService.Dispose();
            }

            _sessionInactivityService = novoMonitor;
            _sessionInactivityService.SessionExpired += OnSessionExpired;

            if (IsLoaded)
            {
                _sessionInactivityService.Start();
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Atalhos oficiais: InputBindings + CommandBindings.
            // Fallback Ctrl+K quando o foco em TextBox impede o InputBinding.
            if (e.Key == System.Windows.Input.Key.K &&
                System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                AbrirCommandPalette();
                e.Handled = true;
            }
        }
    }
}
