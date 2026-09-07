using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunCalendarInteractionChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Calendar:NativoSemEstilo", () =>
            {
                var calendar = new Calendar
                {
                    Style = null,
                    CalendarDayButtonStyle = null,
                    CalendarButtonStyle = null,
                    CalendarItemStyle = null,
                    Width = 320,
                    Height = 320,
                    DisplayDate = new DateTime(2026, 9, 15),
                    SelectedDate = new DateTime(2026, 9, 15)
                };

                using var host = ShowTransientHost(calendar);
                WaitForUiIdle(4);

                var days = FindVisualChildren<CalendarDayButton>(calendar).ToList();
                if (days.Count < 28)
                    throw new InvalidOperationException($"Calendar nativo nao populou dias (encontrados={days.Count}).");

                var target = new DateTime(2026, 9, 1);
                calendar.SelectedDate = target;
                WaitForUiIdle(2);
                if (calendar.SelectedDate != target)
                    throw new InvalidOperationException("Calendar nativo nao aceitou SelectedDate=1.");

                var beforeMonth = calendar.DisplayDate;
                calendar.DisplayDate = beforeMonth.AddMonths(1);
                WaitForUiIdle(2);
                if (calendar.DisplayDate.Month == beforeMonth.Month && calendar.DisplayDate.Year == beforeMonth.Year)
                    throw new InvalidOperationException("Calendar nativo nao navegou para o mes seguinte.");

                calendar.DisplayDate = new DateTime(2026, 9, 15);
                calendar.SelectedDate = new DateTime(2026, 9, 15);
                calendar.DisplayDateStart = new DateTime(2026, 9, 15);
                calendar.DisplayDateEnd = new DateTime(2026, 9, 15);
                WaitForUiIdle(2);

                var monthBeforeClamp = calendar.DisplayDate;
                calendar.DisplayDate = monthBeforeClamp.AddMonths(1);
                WaitForUiIdle(2);
                if (calendar.DisplayDate.Month != monthBeforeClamp.Month || calendar.DisplayDate.Year != monthBeforeClamp.Year)
                    throw new InvalidOperationException(
                        "DisplayDateStart/End iguais deveriam impedir navegacao de mes (regressao Agendamentos).");
            });

            RunCheck(result, "Calendar:TematizadoInteracao", () =>
            {
                var calendar = new Calendar
                {
                    Width = 320,
                    Height = 320,
                    DisplayDate = new DateTime(2026, 9, 15),
                    SelectedDate = new DateTime(2026, 9, 15)
                };

                using var host = ShowTransientHost(calendar);
                WaitForUiIdle(6);

                var days = FindVisualChildren<CalendarDayButton>(calendar).ToList();
                if (days.Count < 28)
                    throw new InvalidOperationException($"Calendar tematizado nao populou dias (encontrados={days.Count}).");

                if (days.Count(d => d.Content != null) < 28)
                    throw new InvalidOperationException("Calendar tematizado sem Content nos dias.");

                calendar.SelectedDate = new DateTime(2026, 9, 10);
                WaitForUiIdle(2);
                if (calendar.SelectedDate != new DateTime(2026, 9, 10))
                    throw new InvalidOperationException("Calendar tematizado nao manteve SelectedDate.");

                if (!FindVisualChildren<CalendarDayButton>(calendar).Any(d => d.IsSelected))
                    throw new InvalidOperationException("Nenhum CalendarDayButton com IsSelected apos selecao.");

                var monthBefore = calendar.DisplayDate;
                calendar.DisplayDate = monthBefore.AddMonths(1);
                WaitForUiIdle(2);
                if (calendar.DisplayDate <= monthBefore)
                    throw new InvalidOperationException("Calendar tematizado nao avancou o mes.");

                var nav = FindVisualChildren<Button>(calendar)
                    .Where(e => e.Name is "PART_PreviousButton" or "PART_NextButton" or "PART_HeaderButton")
                    .ToList();
                if (nav.Count < 3)
                    throw new InvalidOperationException($"PART nav incompletos (encontrados={nav.Count}).");
                if (nav.Any(b => !b.IsEnabled))
                    throw new InvalidOperationException("Botoes de navegacao desabilitados sem clamp.");
            });

            RunCheck(result, "Calendar:AgendamentosSemClamp", () =>
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                var window = new MainWindow(syntheticUser);
                try
                {
                    ShowWindowForInteraction(window);
                    if (!window.NavigateToModuleForAutomation("Agendamentos", forceReload: true) ||
                        window.CurrentContentElement is not AgendamentosControl control)
                    {
                        throw new InvalidOperationException("Agendamentos nao carregou.");
                    }

                    WaitForUiIdle(8);
                    var calendar = FindVisualChildren<Calendar>(control).FirstOrDefault()
                                   ?? control.FindName("calendarControl") as Calendar;
                    if (calendar == null)
                        throw new InvalidOperationException("calendarControl ausente.");

                    if (calendar.DisplayDateStart.HasValue || calendar.DisplayDateEnd.HasValue)
                        throw new InvalidOperationException(
                            $"Agendamentos ainda restringe o Calendar (Start={calendar.DisplayDateStart}, End={calendar.DisplayDateEnd}).");

                    var target = DateTime.Today.AddDays(3).Date;
                    calendar.SelectedDate = target;
                    WaitForUiIdle(4);
                    if (calendar.SelectedDate?.Date != target)
                        throw new InvalidOperationException($"SelectedDate nao mudou para {target:yyyy-MM-dd}.");

                    var days = FindVisualChildren<CalendarDayButton>(calendar).ToList();
                    if (days.Count < 28)
                        throw new InvalidOperationException($"Agendamentos Calendar sem dias ({days.Count}).");

                    var selectable = days.Count(d => d.IsEnabled && !d.IsBlackedOut);
                    if (selectable < 28)
                        throw new InvalidOperationException($"Poucos dias interagiveis ({selectable}).");

                    var monthBefore = calendar.DisplayDate;
                    calendar.DisplayDate = monthBefore.AddMonths(1);
                    WaitForUiIdle(2);
                    if (calendar.DisplayDate.Month == monthBefore.Month && calendar.DisplayDate.Year == monthBefore.Year)
                        throw new InvalidOperationException("Agendamentos Calendar nao permite navegar mes.");
                }
                finally
                {
                    if (window.IsVisible)
                        window.Close();
                }
            });

            RunCheck(result, "Calendar:QaVisualDarkLight", () =>
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                var themeService = new ThemeService();
                var temaOriginal = themeService.GetCurrentTheme();
                var outDir = Path.Combine(AppContext.BaseDirectory, "Logs", "qa-visual");
                Directory.CreateDirectory(outDir);

                MainWindow? window = null;
                try
                {
                    foreach (var tema in new[] { AppTheme.Light, AppTheme.Dark })
                    {
                        themeService.ApplyTheme(tema);
                        window?.Close();
                        window = new MainWindow(syntheticUser);
                        ShowWindowForInteraction(window);

                        if (!window.NavigateToModuleForAutomation("Agendamentos", forceReload: true) ||
                            window.CurrentContentElement is not AgendamentosControl control)
                        {
                            throw new InvalidOperationException($"Agendamentos nao carregou no tema {tema}.");
                        }

                        WaitForUiIdle(8);
                        var calendar = FindVisualChildren<Calendar>(control).FirstOrDefault()
                                       ?? control.FindName("calendarControl") as Calendar;
                        if (calendar == null)
                            throw new InvalidOperationException($"calendarControl ausente no tema {tema}.");

                        var days = FindVisualChildren<CalendarDayButton>(calendar).ToList();
                        if (days.Count < 28)
                            throw new InvalidOperationException($"Tema {tema}: poucos dias ({days.Count}).");

                        var withContent = days.Count(d => d.Content != null);
                        if (withContent < 28)
                            throw new InvalidOperationException($"Tema {tema}: dias sem Content ({withContent}).");

                        var selectable = days.Count(d => d.IsEnabled && !d.IsBlackedOut);
                        if (selectable < 28)
                            throw new InvalidOperationException($"Tema {tema}: poucos dias clicaveis ({selectable}).");

                        var target = DateTime.Today.AddDays(5).Date;
                        calendar.SelectedDate = target;
                        WaitForUiIdle(3);
                        if (calendar.SelectedDate?.Date != target)
                            throw new InvalidOperationException($"Tema {tema}: SelectedDate nao mudou.");

                        if (!FindVisualChildren<CalendarDayButton>(calendar).Any(d => d.IsSelected))
                            throw new InvalidOperationException($"Tema {tema}: nenhum dia IsSelected.");

                        var monthBefore = calendar.DisplayDate;
                        calendar.DisplayDate = monthBefore.AddMonths(1);
                        WaitForUiIdle(3);
                        if (calendar.DisplayDate.Month == monthBefore.Month)
                            throw new InvalidOperationException($"Tema {tema}: navegacao de mes falhou.");

                        calendar.DisplayDate = monthBefore;
                        WaitForUiIdle(2);

                        var pngPath = Path.Combine(outDir, $"agendamentos-calendar-{tema.ToString().ToLowerInvariant()}.png");
                        WaitForUiIdle(2);
                        CaptureElementPng(calendar, pngPath);
                        if (!File.Exists(pngPath) || new FileInfo(pngPath).Length < 1024)
                            throw new InvalidOperationException($"Tema {tema}: captura visual invalida em {pngPath}.");
                    }
                }
                finally
                {
                    themeService.ApplyTheme(temaOriginal);
                    if (window?.IsVisible == true)
                        window.Close();
                }
            });
        }

        private static void CaptureElementPng(FrameworkElement element, string path)
        {
            element.UpdateLayout();
            element.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));

            var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
            var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
            if (width < 8 || height < 8)
                throw new InvalidOperationException($"Elemento sem tamanho renderizado ({width}x{height}).");

            var dpi = 96d;
            var rtb = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(element);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var stream = File.Create(path);
            encoder.Save(stream);
        }

        private static IDisposable ShowTransientHost(FrameworkElement element)
        {
            var host = new Window
            {
                Width = 400,
                Height = 420,
                Content = element,
                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false,
                Topmost = true,
                Opacity = 0.01
            };
            host.Show();
            element.UpdateLayout();
            return new TransientWindowScope(host);
        }

        private sealed class TransientWindowScope : IDisposable
        {
            private Window? _window;
            public TransientWindowScope(Window window) => _window = window;
            public void Dispose()
            {
                try { _window?.Close(); } catch { }
                _window = null;
            }
        }
    }
}
