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
        // Checks de tema claro/escuro e contraste.

        private void RunTemaModulosChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Tema:ClaroEscuroModulosPrincipais", () =>
            {
                var themeService = new ThemeService();
                var temaOriginal = themeService.GetCurrentTheme();
                var modulos = new[] { "Dashboard", "PDV", "Estoque", "ImportarNFe", "Fornecedores", "Relatorios" };
                MainWindow? window = null;

                try
                {
                    themeService.ApplyTheme(AppTheme.Light);
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    foreach (var modulo in modulos)
                    {
                        var navegou = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : window.NavigateToModuleForAutomation(modulo, forceReload: true);

                        if (!navegou || window.CurrentContentElement == null)
                        {
                            throw new InvalidOperationException($"O modulo {modulo} nao carregou para validar os temas.");
                        }

                        ValidarTemaAtual(AppTheme.Light, modulo);
                        ClickButton(window, "ThemeToggleButton");
                        ValidarTemaAtual(AppTheme.Dark, modulo);
                        ClickButton(window, "ThemeToggleButton");
                        ValidarTemaAtual(AppTheme.Light, modulo);
                    }

                    var caminhoTema = Path.Combine(App.RuntimeAppDataPath, "theme_settings.json");
                    if (!File.Exists(caminhoTema))
                    {
                        throw new InvalidOperationException("A preferencia de tema nao foi persistida no ambiente isolado do smoke.");
                    }
                }
                finally
                {
                    themeService.ApplyTheme(temaOriginal);
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }
                }
            });

            RunCheck(result, "Tema:DensidadeCompactaConfortavel", () =>
            {
                var densityService = new DisplayDensityService();
                MainWindow? window = null;

                try
                {
                    densityService.ApplyDensity(DisplayDensity.Comfortable);
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    var alturaConfortavel = ObterDoubleResource("DensityControlHeight");
                    if (alturaConfortavel < 39d)
                    {
                        throw new InvalidOperationException("A densidade confortavel nao aplicou a altura esperada dos controles.");
                    }

                    ClickButton(window, "DensityToggleButton");
                    WaitForUiIdle();

                    var alturaCompacta = ObterDoubleResource("DensityControlHeight");
                    if (alturaCompacta >= alturaConfortavel)
                    {
                        throw new InvalidOperationException("O botao de densidade nao alternou para modo compacto.");
                    }

                    var caminhoDensidade = Path.Combine(App.RuntimeAppDataPath, "density_settings.json");
                    if (!File.Exists(caminhoDensidade))
                    {
                        throw new InvalidOperationException("A preferencia de densidade nao foi persistida no ambiente isolado do smoke.");
                    }

                    ClickButton(window, "DensityToggleButton");
                    WaitForUiIdle();

                    var alturaRestaurada = ObterDoubleResource("DensityControlHeight");
                    if (alturaRestaurada < alturaConfortavel)
                    {
                        throw new InvalidOperationException("O botao de densidade nao retornou para modo confortavel.");
                    }
                }
                finally
                {
                    densityService.ApplyDensity(DisplayDensity.Comfortable);
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }
                }
            });
        }

        private static void ValidarTemaAtual(AppTheme temaEsperado, string modulo)
        {
            WaitForUiIdle();
            var trechoEsperado = temaEsperado == AppTheme.Light ? "Colors.Light.xaml" : "Colors.Dark.xaml";
            var dicionarioTema = Application.Current?.Resources.MergedDictionaries
                .FirstOrDefault(dictionary => dictionary.Source?.OriginalString.Contains("Themes/Colors.", StringComparison.OrdinalIgnoreCase) == true);

            if (dicionarioTema?.Source?.OriginalString.Contains(trechoEsperado, StringComparison.OrdinalIgnoreCase) != true)
            {
                throw new InvalidOperationException($"O tema {temaEsperado} nao foi aplicado ao modulo {modulo}.");
            }

            ValidarContrasteRecursos("PrimaryTextBrush", "SurfaceBrush", modulo, temaEsperado);
            ValidarContrasteRecursos("PrimaryTextBrush", "AppBackgroundBrush", modulo, temaEsperado);
            ValidarContrasteRecursos("InputForegroundBrush", "InputBackgroundBrush", modulo, temaEsperado);
        }

        private static void ValidarContrasteRecursos(string foregroundKey, string backgroundKey, string modulo, AppTheme tema)
        {
            var foreground = Application.Current?.TryFindResource(foregroundKey) as SolidColorBrush
                ?? throw new InvalidOperationException($"Recurso {foregroundKey} nao foi localizado no tema {tema}.");
            var background = Application.Current?.TryFindResource(backgroundKey) as SolidColorBrush
                ?? throw new InvalidOperationException($"Recurso {backgroundKey} nao foi localizado no tema {tema}.");
            var contraste = CalcularRazaoContraste(foreground.Color, background.Color);

            if (contraste < 4.5d)
            {
                throw new InvalidOperationException(
                    $"Contraste insuficiente no modulo {modulo}, tema {tema}: {foregroundKey}/{backgroundKey}={contraste:F2}.");
            }
        }

        private static double ObterDoubleResource(string resourceKey)
        {
            var valor = Application.Current?.TryFindResource(resourceKey)
                ?? throw new InvalidOperationException($"Recurso {resourceKey} nao foi localizado.");

            return valor switch
            {
                double numero => numero,
                int inteiro => inteiro,
                _ => throw new InvalidOperationException($"Recurso {resourceKey} nao e numerico.")
            };
        }

        private static double CalcularRazaoContraste(Color primeira, Color segunda)
        {
            var luminanciaPrimeira = CalcularLuminanciaRelativa(primeira);
            var luminanciaSegunda = CalcularLuminanciaRelativa(segunda);
            var clara = Math.Max(luminanciaPrimeira, luminanciaSegunda);
            var escura = Math.Min(luminanciaPrimeira, luminanciaSegunda);
            return (clara + 0.05d) / (escura + 0.05d);
        }

        private static double CalcularLuminanciaRelativa(Color color)
        {
            static double Linearizar(byte componente)
            {
                var normalizado = componente / 255d;
                return normalizado <= 0.03928d
                    ? normalizado / 12.92d
                    : Math.Pow((normalizado + 0.055d) / 1.055d, 2.4d);
            }

            return 0.2126d * Linearizar(color.R) +
                   0.7152d * Linearizar(color.G) +
                   0.0722d * Linearizar(color.B);
        }

    }
}
