using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Tipos internos, supervisor de dialogs e chamadas nativas.

        private sealed record InteractionSurface(Window HostWindow, FrameworkElement Root);
        private sealed record ButtonDescriptor(int Index, string Name, string Text)
        {
            public string DisplayName => string.IsNullOrWhiteSpace(Text) ? Name : Text;
        }

        private sealed class UiSmokeFixture
        {
            public Funcionario Administrator { get; set; } = null!;
            public Funcionario Funcionario { get; set; } = null!;
            public Cliente Cliente { get; set; } = null!;
            public Veiculo Veiculo { get; set; } = null!;
            public Produto Produto { get; set; } = null!;
            public Fornecedor Fornecedor { get; set; } = null!;
            public Orcamento Orcamento { get; set; } = null!;
            public OrdemServico OrdemServico { get; set; } = null!;
            public Agendamento Agendamento { get; set; } = null!;
            public Venda Venda { get; set; } = null!;
        }

        private sealed class AutomatedDialogSupervisor : IDisposable
        {
            private readonly Window _ownerWindow;
            private readonly UiSmokeFixture? _fixture;
            private readonly CancellationTokenSource _cancellation = new();
            private Thread? _workerThread;

            public AutomatedDialogSupervisor(Window ownerWindow, UiSmokeFixture? fixture)
            {
                _ownerWindow = ownerWindow;
                _fixture = fixture;
            }

            public void Start()
            {
                if (_workerThread != null)
                {
                    return;
                }

                _workerThread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "UiSmokeDialogSupervisor"
                };
                _workerThread.Start();
            }

            public void Dispose()
            {
                _cancellation.Cancel();
                if (_workerThread != null && _workerThread.IsAlive)
                {
                    _workerThread.Join(TimeSpan.FromSeconds(1));
                }
            }

            private void Run()
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    try
                    {
                        HandleNativeDialogs();
                        Application.Current?.Dispatcher.BeginInvoke(new Action(HandleManagedDialogs), DispatcherPriority.Background);
                    }
                    catch
                    {
                    }

                    Thread.Sleep(125);
                }
            }

            private void HandleManagedDialogs()
            {
                var windows = Application.Current?.Windows
                    .OfType<Window>()
                    .Where(window => window.IsVisible && !ReferenceEquals(window, _ownerWindow))
                    .ToList()
                    ?? new List<Window>();

                foreach (var window in windows)
                {
                    if (window is MainWindow)
                    {
                        continue;
                    }

                    if (window is OperacaoCaixaWindow)
                    {
                        TrySetText(window, "ValorTextBox", "10,00", overwrite: true);
                        TrySetText(window, "ObservacoesTextBox", "Automacao do smoke test.", overwrite: false);
                        TryClickButton(window, "ConfirmarButton", "Confirmar");
                        continue;
                    }

                    if (window is SelecionarVendaWindow)
                    {
                        SelectFirstDataGridItem(window);
                        TryClickButton(window, "SelecionarButton", "Selecionar venda", "Selecionar");
                        continue;
                    }

                    if (window is SelecionarClientePDVWindow)
                    {
                        SelectFirstDataGridItem(window);
                        TryClickButton(window, string.Empty, "Consumidor final", "Selecionar cliente", "Selecionar", "Cancelar");
                        continue;
                    }

                    if (window is SelecionarOrcamentoWindow)
                    {
                        SelectFirstDataGridItem(window);
                        TryClickButton(window, string.Empty, "Duplicar", "Abrir orcamento", "Selecionar");
                        continue;
                    }

                    if (window is ConfirmacaoCriticaWindow)
                    {
                        var keyword = FindElementByName<TextBlock>(window, "KeywordTextBlock")?.Text ?? "CONFIRMAR";
                        TrySetText(window, "ConfirmationTextBox", keyword, overwrite: true);
                        TryClickButton(window, "ConfirmarButton", "Confirmar");
                        continue;
                    }

                    if (window is AdicionarFornecedorDialog)
                    {
                        TryClickButton(window, string.Empty, "Sim", "Nao");
                        continue;
                    }

                    if (!TryClickButton(window, string.Empty, "Salvar", "Confirmar", "Selecionar", "OK", "Ok", "Fechar", "Cancelar", "Voltar"))
                    {
                        window.Close();
                    }
                }
            }

            private static void SelectFirstDataGridItem(DependencyObject root)
            {
                foreach (var grid in FindVisualChildren<DataGrid>(root))
                {
                    if (grid.Items.Count > 0 && grid.SelectedIndex < 0)
                    {
                        grid.SelectedIndex = 0;
                    }
                }
            }

            private static bool TryClickButton(DependencyObject root, string buttonName, params string[] buttonTexts)
            {
                var button = FindVisualChildren<Button>(root)
                    .FirstOrDefault(candidate =>
                        candidate.IsEnabled &&
                        IsButtonDiscoverable(candidate) &&
                        (!string.IsNullOrWhiteSpace(buttonName) && string.Equals(candidate.Name, buttonName, StringComparison.OrdinalIgnoreCase) ||
                         buttonTexts.Any(text => string.Equals(ExtractButtonText(candidate), text, StringComparison.OrdinalIgnoreCase))));

                if (button == null)
                {
                    return false;
                }

                RaiseButtonClick(button);
                PumpDispatcher();
                return true;
            }

            private static void TrySetText(DependencyObject root, string textBoxName, string value, bool overwrite)
            {
                var textBox = FindElementByName<TextBox>(root, textBoxName);
                if (textBox == null || !textBox.IsEnabled)
                {
                    return;
                }

                if (!overwrite && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    return;
                }

                textBox.Text = value;
                PumpDispatcher();
            }

            private static void HandleNativeDialogs()
            {
                NativeMethods.EnumerateWindows(handle =>
                {
                    if (!NativeMethods.IsWindowVisible(handle))
                    {
                        return true;
                    }

                    NativeMethods.GetWindowThreadProcessId(handle, out var processId);
                    if (processId != Environment.ProcessId)
                    {
                        return true;
                    }

                    var className = NativeMethods.GetClassName(handle);
                    if (!string.Equals(className, "#32770", StringComparison.Ordinal))
                    {
                        return true;
                    }

                    foreach (var buttonId in new[] { NativeMethods.IDCANCEL, NativeMethods.IDNO, NativeMethods.IDOK, NativeMethods.IDYES })
                    {
                        if (NativeMethods.TryClickDialogButton(handle, buttonId))
                        {
                            return true;
                        }
                    }

                    NativeMethods.SendClose(handle);
                    return true;
                });
            }
        }

        private static class NativeMethods
        {
            public const int IDOK = 1;
            public const int IDCANCEL = 2;
            public const int IDYES = 6;
            public const int IDNO = 7;
            private const uint WM_CLOSE = 0x0010;
            private const uint BM_CLICK = 0x00F5;

            private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

            [DllImport("user32.dll")]
            private static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            public static string GetClassName(IntPtr handle)
            {
                var builder = new StringBuilder(256);
                GetClassName(handle, builder, builder.Capacity);
                return builder.ToString();
            }

            public static bool EnumerateWindows(Func<IntPtr, bool> callback)
            {
                return EnumWindows((handle, _) => callback(handle), IntPtr.Zero);
            }

            public static bool TryClickDialogButton(IntPtr dialogHandle, int buttonId)
            {
                var buttonHandle = GetDlgItem(dialogHandle, buttonId);
                if (buttonHandle == IntPtr.Zero)
                {
                    return false;
                }

                SendMessage(buttonHandle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                return true;
            }

            public static void SendClose(IntPtr handle)
            {
                SendMessage(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}
