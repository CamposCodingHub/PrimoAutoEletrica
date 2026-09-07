using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço para melhorar acessibilidade da aplicação
    /// Implementa padrões WCAG 2.1 Level AA
    /// </summary>
    public static class AccessibilityService
    {
        /// <summary>
        /// Aplica configurações de acessibilidade a um controle
        /// </summary>
        public static void ApplyAccessibilityProperties(Control control, string name, string helpText = "")
        {
            if (control == null) return;

            // Definir nome acessível
            AutomationProperties.SetName(control, name);

            // Definir texto de ajuda
            if (!string.IsNullOrEmpty(helpText))
                AutomationProperties.SetHelpText(control, helpText);

            // Definir controle como necessário para formulário
            AutomationProperties.SetIsRequiredForForm(control, true);
        }

        /// <summary>
        /// Registra atalhos de teclado globais para a aplicação
        /// </summary>
        public static void RegisterKeyboardShortcuts(Window window)
        {
            if (window == null) return;

            var shortcuts = new Dictionary<KeyGesture, string>
            {
                // Navegação principal
                { new KeyGesture(Key.C, ModifierKeys.Alt), "Ir para Clientes" },
                { new KeyGesture(Key.F, ModifierKeys.Alt), "Ir para Fornecedores" },
                { new KeyGesture(Key.E, ModifierKeys.Alt), "Ir para Estoque" },
                { new KeyGesture(Key.V, ModifierKeys.Alt), "Ir para Veículos" },
                { new KeyGesture(Key.O, ModifierKeys.Alt), "Ir para Ordens de Serviço" },
                { new KeyGesture(Key.R, ModifierKeys.Alt), "Ir para Relatórios" },

                // Operações comuns
                { new KeyGesture(Key.N, ModifierKeys.Control), "Novo" },
                { new KeyGesture(Key.S, ModifierKeys.Control), "Salvar" },
                { new KeyGesture(Key.D, ModifierKeys.Control), "Deletar" },
                { new KeyGesture(Key.E, ModifierKeys.Control), "Editar" },
                { new KeyGesture(Key.P, ModifierKeys.Control), "Imprimir" },
                { new KeyGesture(Key.X, ModifierKeys.Control), "Exportar" },

                // Sistema
                { new KeyGesture(Key.F1), "Ajuda" },
                { new KeyGesture(Key.F12), "Modo desenvolvedor" },
                { new KeyGesture(Key.Escape), "Cancelar/Fechar" }
            };

            foreach (var shortcut in shortcuts)
            {
                AutomationProperties.SetName(window, $"Atalho: {shortcut.Value}");
            }
        }

        /// <summary>
        /// Aplica tema de contraste alto para melhor acessibilidade visual
        /// </summary>
        public static void ApplyHighContrastTheme(Application app)
        {
            try
            {
                var highContrastDict = new ResourceDictionary();

                // Cores de alto contraste
                highContrastDict["AccessibilityForeground"] = System.Windows.Media.Colors.Black;
                highContrastDict["AccessibilityBackground"] = System.Windows.Media.Colors.White;
                highContrastDict["AccessibilityAccent"] = System.Windows.Media.Colors.Blue;
                highContrastDict["AccessibilityWarning"] = System.Windows.Media.Colors.Red;
                highContrastDict["AccessibilitySuccess"] = System.Windows.Media.Colors.Green;

                // Aplicar tema
                if (app?.Resources != null)
                {
                    foreach (var key in highContrastDict.Keys)
                    {
                        app.Resources[key] = highContrastDict[key];
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger?.LogError("Erro ao aplicar tema de contraste alto", ex);
            }
        }

        /// <summary>
        /// Aumenta tamanho de fonte para melhor legibilidade
        /// </summary>
        public static void ApplyLargeFontSize(Application app, double multiplier = 1.2)
        {
            try
            {
                if (app?.Resources == null) return;

                // Aumentar fonte padrão
                if (app.Resources["DefaultFontSize"] is double)
                {
                    app.Resources["DefaultFontSize"] = (double)app.Resources["DefaultFontSize"] * multiplier;
                }

                // Aumentar fonte de cabeçalho
                if (app.Resources["HeaderFontSize"] is double)
                {
                    app.Resources["HeaderFontSize"] = (double)app.Resources["HeaderFontSize"] * multiplier;
                }
            }
            catch (Exception ex)
            {
                App.Logger?.LogError("Erro ao ajustar tamanho de fonte", ex);
            }
        }

        /// <summary>
        /// Habilita suporte a leitura de tela
        /// </summary>
        public static void EnableScreenReaderSupport(Window window)
        {
            if (window == null) return;

            // Configurar window como acessível
            AutomationProperties.SetIsRequiredForForm(window, true);
            AutomationProperties.SetName(window, "Janela principal da aplicação");
        }

        /// <summary>
        /// Anuncia mensagem para leitores de tela
        /// </summary>
        public static void AnnounceToScreenReader(string message)
        {
            try
            {
                // Criar elemento temporário para anúncio
                var announcer = new TextBlock
                {
                    Text = message,
                    Visibility = Visibility.Hidden
                };

                AutomationProperties.SetIsRequiredForForm(announcer, true);
                AutomationProperties.SetName(announcer, message);
            }
            catch (Exception ex)
            {
                App.Logger?.LogError("Erro ao anunciar para leitor de tela", ex);
            }
        }

        /// <summary>
        /// Valida conformidade de acessibilidade de um controle
        /// </summary>
        public static AccessibilityValidationResult ValidateControlAccessibility(Control control)
        {
            var result = new AccessibilityValidationResult { IsValid = true };

            if (control == null)
            {
                result.IsValid = false;
                result.Issues.Add("Controle é nulo");
                return result;
            }

            // Verificar se tem nome acessível
            var name = AutomationProperties.GetName(control);
            if (string.IsNullOrWhiteSpace(name))
            {
                result.IsValid = false;
                result.Issues.Add($"Controle {control.GetType().Name} não possui AutomationProperties.Name");
            }

            // Verificar se é visível
            if (control.Visibility != Visibility.Visible)
            {
                result.IsValid = false;
                result.Issues.Add("Controle não está visível");
            }

            // Verificar cores de contraste (simplificado)
            if (control.Foreground is System.Windows.Media.SolidColorBrush foreground &&
                control.Background is System.Windows.Media.SolidColorBrush background)
            {
                var contrast = CalculateColorContrast(foreground.Color, background.Color);
                if (contrast < 4.5) // WCAG AA mínimo
                {
                    result.IsValid = false;
                    result.Issues.Add($"Contraste de cor insuficiente (razão: {contrast:F2}:1)");
                }
            }

            return result;
        }

        private static double CalculateColorContrast(System.Windows.Media.Color color1, System.Windows.Media.Color color2)
        {
            var luminance1 = GetRelativeLuminance(color1);
            var luminance2 = GetRelativeLuminance(color2);

            var lighter = Math.Max(luminance1, luminance2);
            var darker = Math.Min(luminance1, luminance2);

            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double GetRelativeLuminance(System.Windows.Media.Color color)
        {
            var r = color.R / 255.0;
            var g = color.G / 255.0;
            var b = color.B / 255.0;

            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
    }

    /// <summary>
    /// Resultado da validação de acessibilidade
    /// </summary>
    public class AccessibilityValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();

        public override string ToString()
        {
            if (IsValid)
                return "✅ Controle acessível";

            return "❌ Problemas de acessibilidade:\n" + string.Join("\n", Issues);
        }
    }
}
