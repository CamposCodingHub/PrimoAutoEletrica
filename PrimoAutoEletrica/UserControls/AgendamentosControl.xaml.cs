using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.UserControls
{
    public partial class AgendamentosControl : UserControl
    {
        private DispatcherTimer _refreshTimer = null!;
        private AgendamentosViewModel _viewModel = null!;

        public AgendamentosControl()
        {
            InitializeComponent();
            _viewModel = App.Services.GetRequiredService<AgendamentosViewModel>();
            DataContext = _viewModel;

            InitializeTimers();
            InitializeEventHandlers();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void InitializeTimers()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += OnRefreshTimerTick;
            _refreshTimer.Start();
        }

        private void InitializeEventHandlers()
        {
            // Calendar selection changed
            if (FindName("calendarControl") is Calendar calendar)
            {
                calendar.SelectedDatesChanged += OnCalendarSelectionChanged;
            }

            // Search box
            if (FindName("searchBox") is TextBox searchBox)
            {
                searchBox.KeyDown += OnSearchBoxKeyDown;
            }

            // List view interactions
            if (FindName("agendamentosListView") is ListView listView)
            {
                listView.MouseDoubleClick += OnAgendamentoDoubleClick;
                listView.SelectionChanged += OnAgendamentoSelectionChanged;
            }

            // Keyboard shortcuts
            KeyDown += OnKeyDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer?.Start();
            _viewModel.CarregarAgendamentos();
            _viewModel.CarregarDashboard();
            _viewModel.CarregarTecnicos();
            _viewModel.CarregarVeiculos();
            _viewModel.CarregarAlertas();
            _viewModel.CarregarTimeline();
            ApplyEntranceAnimations();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer?.Stop();
        }

        private void OnRefreshTimerTick(object? sender, EventArgs e)
        {
            // Auto-refresh data every 30 seconds
            _viewModel.AtualizarDados();
        }

        private void OnCalendarSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.RecarregarVisualizacaoAtual();
            }
        }

        private void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.BuscarAgendamentos();
            }
        }

        private void OnAgendamentoDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.AgendamentoSelecionado != null)
            {
                _viewModel.EditarCommand.Execute(null);
            }
        }

        private void OnAgendamentoSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update details panel
            if (_viewModel.AgendamentoSelecionado != null)
            {
                AnimateDetailsPanel();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Keyboard shortcuts
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _viewModel.NovoAgendamentoCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                FocusSearchBox();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                _viewModel.AtualizarCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && _viewModel.AgendamentoSelecionado != null)
            {
                _viewModel.CancelarCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void ApplyEntranceAnimations()
        {
            // Animate header
            var headerAnimation = new DoubleAnimation
            {
                From = -50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            if (FindName("headerBorder") is Border header)
            {
                header.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(500)
                });
            }

            // Animate dashboard cards
            AnimateDashboardCards();
        }

        private void AnimateDashboardCards()
        {
            // Staggered animation for dashboard cards
            var cards = FindVisualChildren<Border>(this);
            int index = 0;
            
            foreach (var card in cards)
            {
                if (card.Name?.Contains("Card") == true || card.Style?.TargetType == typeof(Border))
                {
                    var animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300),
                        BeginTime = TimeSpan.FromMilliseconds(index * 50),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    card.BeginAnimation(OpacityProperty, animation);
                    index++;
                }
            }
        }

        private void AnimateDetailsPanel()
        {
            if (FindName("detailsPanel") is Border detailsPanel)
            {
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                detailsPanel.BeginAnimation(OpacityProperty, animation);
            }
        }

        private void FocusSearchBox()
        {
            if (FindName("searchBox") is TextBox searchBox)
            {
                searchBox.Focus();
                searchBox.SelectAll();
            }
        }

        private System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject? child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        // Drag and Drop support for calendar
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Agendamento)) != null)
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Agendamento)) is Agendamento agendamento)
            {
                // Handle drop on calendar date
                if (sender is Calendar calendar && calendar.SelectedDate.HasValue)
                {
                    _viewModel.ReagendarAgendamento(agendamento, calendar.SelectedDate.Value);
                }
            }
        }

        // Context menu support
        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Show context menu for agendamento
            if (_viewModel.AgendamentoSelecionado != null)
            {
                // Enable context menu items
            }
        }

        // Print support
        public void ImprimirAgenda()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(this, "Agenda de Agendamentos");
                }
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao imprimir a agenda de agendamentos.", ex);
                MessageBox.Show("Erro ao imprimir", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Export to PDF support
        public void ExportarParaPDF()
        {
            try
            {
                _viewModel.ExportarPDFCommand.Execute(null);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao exportar a agenda de agendamentos para PDF.", ex);
                MessageBox.Show("Erro ao exportar PDF", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EmitirSugestaoOrdensServicoForAutomation()
        {
            ShellNotificationService.PublishNavigationHint(
                title: "OS pronta para acompanhamento",
                message: "Um fluxo interno de agendamentos sinalizou acompanhamento no modulo de Ordens de Servico.",
                actionModule: "OrdensServico",
                actionLabel: "Abrir Ordens de Servico",
                details: "Atalho de navegacao cruzada para validacao automatizada.",
                type: ShellNotificationType.Info,
                source: "Agendamentos");
        }

        // Full screen mode
        public void AlternarTelaCheia()
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                if (window.WindowState == WindowState.Normal)
                {
                    window.WindowState = WindowState.Maximized;
                }
                else
                {
                    window.WindowState = WindowState.Normal;
                }
            }
        }

        // Workshop mode
        public void AlternarModoOficina()
        {
            _viewModel.ModoOficina = !_viewModel.ModoOficina;
            
            // Update UI based on mode
            if (_viewModel.ModoOficina)
            {
                // Show simplified workshop view
            }
            else
            {
                // Show full management view
            }
        }

        // Quick filter support
        public void AplicarFiltroRapido(string filtro)
        {
            switch (filtro.ToLower())
            {
                case "hoje":
                    _viewModel.DataSelecionada = DateTime.Today;
                    break;
                case "semana":
                    _viewModel.CarregarAgendamentosPorPeriodo(DateTime.Today, DateTime.Today.AddDays(7));
                    break;
                case "mes":
                    _viewModel.CarregarAgendamentosPorPeriodo(
                        new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                        new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)));
                    break;
                case "pendentes":
                    _viewModel.FiltroStatus = "Pendente";
                    break;
                case "concluidos":
                    _viewModel.FiltroStatus = "Concluído";
                    break;
                case "atrasados":
                    _viewModel.CarregarAgendamentosAtrasados();
                    break;
            }
        }

        // Notification support
        public void MostrarNotificacao(string mensagem, NotificationType tipo = NotificationType.Info)
        {
            var messageBoxImage = tipo switch
            {
                NotificationType.Success => MessageBoxImage.Information,
                NotificationType.Warning => MessageBoxImage.Warning,
                NotificationType.Error => MessageBoxImage.Error,
                _ => MessageBoxImage.Information
            };

            MessageBox.Show(mensagem, "Notificação", MessageBoxButton.OK, messageBoxImage);
        }

        // Validation support
        private bool ValidarAgendamento(Agendamento agendamento)
        {
            if (string.IsNullOrEmpty(agendamento.ClienteNome))
            {
                MostrarNotificacao("Cliente é obrigatório", NotificationType.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(agendamento.VeiculoPlaca))
            {
                MostrarNotificacao("Veículo é obrigatório", NotificationType.Warning);
                return false;
            }

            if (agendamento.DataAgendamento == default)
            {
                MostrarNotificacao("Data do agendamento é obrigatória", NotificationType.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(agendamento.TipoServico))
            {
                MostrarNotificacao("Tipo de serviço é obrigatório", NotificationType.Warning);
                return false;
            }

            return true;
        }

        // Auto-complete support
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox && searchBox.Text.Length >= 3)
            {
                _viewModel.BuscarSugestoes(searchBox.Text);
            }
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
