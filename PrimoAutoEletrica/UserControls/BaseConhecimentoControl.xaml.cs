using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.UserControls
{
    public partial class BaseConhecimentoControl : UserControl
    {
        private readonly IKnowledgeService _knowledgeService;
        private readonly IAssistantService _assistantService;
        private List<TechnicalKnowledgeEntry> _todosArtigos = new();
        private List<DiagnosticCase> _todosCasos = new();
        private bool _isInitialized = false;

        public BaseConhecimentoControl()
        {
            InitializeComponent();
            _knowledgeService = new KnowledgeService();
            _assistantService = new AssistantService();
            _isInitialized = true;

            Loaded += async (s, e) => await CarregarDadosAsync();
        }

        public async Task CarregarDadosAsync()
        {
            try
            {
                var artigos = await _knowledgeService.ListarArtigosAsync();
                _todosArtigos = artigos.ToList();

                var casos = await _knowledgeService.ListarCasosAsync();
                _todosCasos = casos.ToList();

                AtualizarIndicadores();
                AplicarFiltrosArtigos();
                AplicarFiltrosCasos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar base de conhecimento: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarIndicadores()
        {
            TotalArtigosText.Text = _todosArtigos.Count.ToString();
            TotalCasosText.Text = _todosCasos.Count.ToString();
            Casos24VText.Text = _todosCasos.Count(c => c.Voltage == "24V").ToString();
            Casos12VText.Text = _todosCasos.Count(c => c.Voltage == "12V").ToString();
        }

        private void AplicarFiltrosArtigos()
        {
            if (!_isInitialized || ArtigosDataGrid == null)
            {
                return;
            }

            var filtrados = _todosArtigos.AsEnumerable();

            var busca = BuscaArtigoTextBox?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                filtrados = filtrados.Where(a =>
                    a.Code.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    a.Title.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    a.Symptom.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    a.PossibleCauses.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (a.Tags != null && a.Tags.Contains(busca, StringComparison.OrdinalIgnoreCase)));
            }

            if (FiltroSistemaArtigoCombo?.SelectedItem is ComboBoxItem sisItem && sisItem.Content?.ToString() is string sis && !sis.StartsWith("Todos", StringComparison.OrdinalIgnoreCase))
            {
                filtrados = filtrados.Where(a => a.System.Contains(sis, StringComparison.OrdinalIgnoreCase));
            }

            if (FiltroTensaoArtigoCombo?.SelectedItem is ComboBoxItem tensaoItem && tensaoItem.Content?.ToString() is string tensao && !tensao.StartsWith("Todas", StringComparison.OrdinalIgnoreCase))
            {
                filtrados = filtrados.Where(a => string.Equals(a.Voltage, tensao, StringComparison.OrdinalIgnoreCase));
            }

            ArtigosDataGrid.ItemsSource = filtrados.ToList();
        }

        private void AplicarFiltrosCasos()
        {
            if (!_isInitialized || CasosDataGrid == null)
            {
                return;
            }

            var filtrados = _todosCasos.AsEnumerable();

            var busca = BuscaCasoTextBox?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                filtrados = filtrados.Where(c =>
                    c.Code.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    c.Title.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    c.VehicleModel.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    c.Symptom.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    c.ConfirmedCause.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (c.DtcCodes != null && c.DtcCodes.Contains(busca, StringComparison.OrdinalIgnoreCase)));
            }

            if (FiltroSistemaCasoCombo?.SelectedItem is ComboBoxItem sisItem && sisItem.Content?.ToString() is string sis && !sis.StartsWith("Todos", StringComparison.OrdinalIgnoreCase))
            {
                filtrados = filtrados.Where(c => c.System.Contains(sis, StringComparison.OrdinalIgnoreCase));
            }

            if (FiltroTensaoCasoCombo?.SelectedItem is ComboBoxItem tensaoItem && tensaoItem.Content?.ToString() is string tensao && !tensao.StartsWith("Todas", StringComparison.OrdinalIgnoreCase))
            {
                filtrados = filtrados.Where(c => string.Equals(c.Voltage, tensao, StringComparison.OrdinalIgnoreCase));
            }

            CasosDataGrid.ItemsSource = filtrados.ToList();
        }

        private async void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            await CarregarDadosAsync();
        }

        private void VerDetalhesButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainKnowledgeTabControl.SelectedIndex == 0)
            {
                AbrirArtigoDetalhes();
            }
            else if (MainKnowledgeTabControl.SelectedIndex == 1)
            {
                AbrirCasoDetalhes();
            }
            else
            {
                MessageBox.Show("Selecione um artigo ou caso nas abas anteriores.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ArtigosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AbrirArtigoDetalhes();
        }

        private void CasosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AbrirCasoDetalhes();
        }

        private void AbrirArtigoDetalhes()
        {
            if (ArtigosDataGrid.SelectedItem is not TechnicalKnowledgeEntry artigo)
            {
                MessageBox.Show("Selecione um artigo da lista para visualizar os detalhes.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CasoTecnicoDialog(artigo)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }

        private void AbrirCasoDetalhes()
        {
            if (CasosDataGrid.SelectedItem is not DiagnosticCase caso)
            {
                MessageBox.Show("Selecione um caso real da lista para visualizar os detalhes.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CasoTecnicoDialog(caso)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }

        private async void PromoverOSButton_Click(object sender, RoutedEventArgs e)
        {
            // Prompt limpo para número de OS
            var window = new Window
            {
                Title = "Promover OS para Caso Técnico",
                Width = 420,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = (System.Windows.Media.Brush)FindResource("AppBackgroundBrush")
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            var lbl = new TextBlock
            {
                Text = "Informe o número da Ordem de Serviço concluída (Ex: OS-001):",
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var txt = new TextBox
            {
                Height = 36,
                Style = (Style)FindResource("PremiumTextBox"),
                Margin = new Thickness(0, 0, 0, 15)
            };
            var btnOk = new Button
            {
                Content = "Confirmar",
                Height = 36,
                Style = (Style)FindResource("PagePrimaryActionButton"),
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 100
            };

            stack.Children.Add(lbl);
            stack.Children.Add(txt);
            stack.Children.Add(btnOk);
            window.Content = stack;

            string? osNumero = null;
            btnOk.Click += (s, ev) =>
            {
                osNumero = txt.Text?.Trim();
                window.DialogResult = true;
                window.Close();
            };

            if (window.ShowDialog() != true || string.IsNullOrWhiteSpace(osNumero))
                return;

            try
            {
                var osRepo = App.Repositories.OrdensServico;
                var os = osRepo.ObterTodos().FirstOrDefault(o => string.Equals(o.Numero, osNumero, StringComparison.OrdinalIgnoreCase));
                if (os == null)
                {
                    MessageBox.Show($"Ordem de Serviço '{osNumero}' não foi localizada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var caso = await _knowledgeService.CriarCasoAPartirDeOSAsync(
                    os.Id,
                    sistema: "Sistema Elétrico",
                    causaConfirmada: string.IsNullOrWhiteSpace(os.DiagnosticoFinal) ? "Causa confirmada em ensaio de oficina" : os.DiagnosticoFinal,
                    solucao: "Procedimento corretivo aplicado e validado operacionalmente.");

                MessageBox.Show($"Caso técnico '{caso.Code}' gerado com sucesso a partir da OS '{os.Numero}'!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                await CarregarDadosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro ao Promover OS", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ConsultarAssistButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecutarConsultaAssistAsync();
        }

        private async void AssistQueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ExecutarConsultaAssistAsync();
            }
        }

        private async Task ExecutarConsultaAssistAsync()
        {
            var query = AssistQueryTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Informe uma pergunta, sintoma elétrico ou código DTC.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                AssistQueryTextBox.Focus();
                return;
            }

            try
            {
                ConsultarAssistButton.IsEnabled = false;
                AssistRespostaTextBlock.Text = "Processando evidências no acervo grounded da oficina...";

                var resp = await _assistantService.ConsultarAsync(query);

                AssistRespostaTextBlock.Text = resp.AnswerMarkdown;
                AssistConfiancaTextBlock.Text = $"Nível de Evidência: {resp.ConfidenceLevel}";

                if (resp.RecommendedActions != null && resp.RecommendedActions.Count > 0)
                {
                    AssistChecklistTextBlock.Text = string.Join("\n", resp.RecommendedActions.Select((c, i) => $"{i + 1}. {c}"));
                }
                else
                {
                    AssistChecklistTextBlock.Text = "1. Confirmar tensão em repouso com multímetro True RMS.\n2. Inspecionar aterramentos e pontos de massa quanto a oxidação.";
                }

                if (resp.CitedSources != null && resp.CitedSources.Count > 0)
                {
                    AssistFontesTextBlock.Text = string.Join("\n", resp.CitedSources.Select(s => $"• [{s.SourceCode}] {s.SourceTitle} — {s.RelevanceExplanation}"));
                }
                else
                {
                    AssistFontesTextBlock.Text = "• Acervo Geral de Boletins e Casos Reais PRIMOX";
                }
            }
            catch (Exception ex)
            {
                AssistRespostaTextBlock.Text = $"Erro ao processar consulta: {ex.Message}";
            }
            finally
            {
                ConsultarAssistButton.IsEnabled = true;
            }
        }

        private async void SugestaoRapida_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string texto)
            {
                AssistQueryTextBox.Text = texto;
                await ExecutarConsultaAssistAsync();
            }
        }

        private void BuscaArtigoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltrosArtigos();
        }

        private void FiltroArtigoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            AplicarFiltrosArtigos();
        }

        private void LimparFiltrosArtigo_Click(object sender, RoutedEventArgs e)
        {
            BuscaArtigoTextBox.Text = string.Empty;
            FiltroSistemaArtigoCombo.SelectedIndex = 0;
            FiltroTensaoArtigoCombo.SelectedIndex = 0;
            AplicarFiltrosArtigos();
        }

        private void BuscaCasoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            AplicarFiltrosCasos();
        }

        private void FiltroCasoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            AplicarFiltrosCasos();
        }

        private void LimparFiltrosCaso_Click(object sender, RoutedEventArgs e)
        {
            BuscaCasoTextBox.Text = string.Empty;
            FiltroSistemaCasoCombo.SelectedIndex = 0;
            FiltroTensaoCasoCombo.SelectedIndex = 0;
            AplicarFiltrosCasos();
        }
    }
}
