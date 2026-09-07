using System;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunGlobalComponentsChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Components:RecursosGlobaisPrimox", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    string[] requiredStyles =
                    {
                        "PrimaryButton",
                        "SecondaryButton",
                        "GhostButton",
                        "DangerButton",
                        "IconButton",
                        "PremiumTextBox",
                        "FormFieldLabel",
                        "FormFieldErrorText",
                        "InputError",
                        "StatusBadgeSuccess",
                        "StatusBadgeWarning",
                        "StatusBadgeDanger",
                        "StatusBadgeInfo",
                        "ToastSuccessSurface",
                        "ToastErrorSurface",
                        "LoadingStatePanel",
                        "ErrorStatePanel",
                        "PrimoxEmptyStatePanel",
                        "ConfirmationDialogSurface",
                        "ConfirmDangerButton",
                        "ModulePageHeader",
                        "PrimoxFocusVisual",
                        "PremiumDataGrid"
                    };

                    foreach (var key in requiredStyles)
                    {
                        if (window.TryFindResource(key) == null)
                        {
                            throw new InvalidOperationException($"Recurso global ausente: {key}");
                        }
                    }

                    string[] toastBrushes =
                    {
                        "ToastSuccessBackgroundBrush",
                        "ToastWarningBackgroundBrush",
                        "ToastErrorBackgroundBrush",
                        "ToastInfoBackgroundBrush"
                    };

                    foreach (var brush in toastBrushes)
                    {
                        if (window.TryFindResource(brush) is not System.Windows.Media.Brush)
                        {
                            throw new InvalidOperationException($"Brush de toast ausente: {brush}");
                        }
                    }

                    // Smoke Light/Dark leve dos recursos de toast
                    var theme = new ThemeService();
                    var original = theme.GetCurrentTheme();
                    try
                    {
                        theme.ApplyTheme(AppTheme.Dark);
                        WaitForUiIdle();
                        if (Application.Current.TryFindResource("ToastInfoBackgroundBrush") is not System.Windows.Media.Brush)
                        {
                            throw new InvalidOperationException("Toast brush indisponivel no Dark.");
                        }

                        theme.ApplyTheme(AppTheme.Light);
                        WaitForUiIdle();
                        if (Application.Current.TryFindResource("PrimoxFocusVisual") == null)
                        {
                            throw new InvalidOperationException("Focus visual ausente apos troca Light.");
                        }
                    }
                    finally
                    {
                        theme.ApplyTheme(original);
                    }

                    window.Width = 1366;
                    window.Height = 768;
                    window.WindowState = WindowState.Normal;
                    window.UpdateLayout();
                    WaitForUiIdle();

                    if (window.TryFindResource("PrimaryButton") is not Style primary
                        || primary.TargetType != typeof(Button))
                    {
                        throw new InvalidOperationException("PrimaryButton invalido.");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }
                }
            });
        }
    }
}
