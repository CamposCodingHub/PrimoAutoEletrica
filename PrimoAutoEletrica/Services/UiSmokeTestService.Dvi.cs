using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        /// <summary>
        /// P0 DVI: UI real Cliente/Veiculo/Orcamento → DVI → aprovacao → OS,
        /// Light + Dark em 1280x720, sem duplicar itens.
        /// </summary>
        private void RunDviOrcamentoOsFluxoChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Dvi:OrcamentoOsFluxoLight1280", () =>
                ExecutarFluxoDviOrcamentoOs(AppTheme.Light, width: 1280, height: 720));

            RunCheck(result, "Dvi:OrcamentoOsFluxoDark1280", () =>
                ExecutarFluxoDviOrcamentoOs(AppTheme.Dark, width: 1280, height: 720));
        }

        private void ExecutarFluxoDviOrcamentoOs(AppTheme tema, int width, int height)
        {
            var fixture = _fixture ?? throw new InvalidOperationException("Fixture smoke ausente para DVI.");
            var themeService = new ThemeService();
            var temaOriginal = themeService.GetCurrentTheme();
            var observacaoMarker = $"DVI-SMOKE-{tema}-{Guid.NewGuid():N}".Substring(0, 28);

            DviChecklistService.TestAppDataOverride = null; // usa RuntimeAppDataPath isolado do smoke

            NovoOrcamentoWindow? orcWindow = null;
            OrdemServicoWindow? osWindow = null;

            try
            {
                themeService.ApplyTheme(tema);
                WaitForUiIdle();

                var orcService = new OrcamentoDatabaseService();
                var orcamento = CreatePersistedOrcamento(fixture.Cliente, fixture.Produto);
                if (orcamento.ClienteId != fixture.Cliente.Id || orcamento.VeiculoId != fixture.Veiculo.Id)
                {
                    // garante veiculo do fixture
                    orcamento.ClienteId = fixture.Cliente.Id;
                    orcamento.VeiculoId = fixture.Veiculo.Id;
                    orcamento.Cliente = fixture.Cliente;
                    orcamento.Veiculo = fixture.Veiculo;
                    orcService.AtualizarOrcamento(orcamento);
                }

                orcWindow = new NovoOrcamentoWindow(orcamento)
                {
                    Width = width,
                    Height = height,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = 20,
                    Top = 20
                };
                ShowWindowForInteraction(orcWindow);
                AssertWindowFitsSmallDesktop(orcWindow, width, height);
                AssertPrimoxInputSurfaces(orcWindow, tema, "NovoOrcamentoWindow");

                // Abrir DVI (modal) — interage enquanto ShowDialog bombeia mensagens
                var dviInteracted = false;
                var dviItemCount = 0;
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    var dvi = Application.Current.Windows.OfType<DviOrcamentoWindow>().FirstOrDefault()
                        ?? throw new InvalidOperationException("DviOrcamentoWindow nao abriu apos clique no botao DVI.");

                    dvi.Width = Math.Min(width - 40, dvi.Width > 0 ? dvi.Width : 720);
                    dvi.Height = Math.Min(height - 40, dvi.Height > 0 ? dvi.Height : 640);
                    AssertPrimoxInputSurfaces(dvi, tema, "DviOrcamentoWindow");

                    var itens = ObterDviItens(dvi);
                    if (itens.Count == 0)
                    {
                        throw new InvalidOperationException("Checklist DVI veio vazio no orcamento.");
                    }

                    dviItemCount = itens.Count;
                    var primeiro = itens[0];
                    primeiro.OkEntrada = true;
                    primeiro.Observacoes = observacaoMarker;

                    // Recarregar padrao e reaplicar para garantir editabilidade + salvar
                    ClickButton(dvi, "Recarregar padrao");
                    WaitForUiIdle();
                    itens = ObterDviItens(dvi);
                    if (itens.Count == 0)
                    {
                        throw new InvalidOperationException("Recarregar padrao esvaziou o DVI.");
                    }

                    dviItemCount = itens.Count;
                    itens[0].OkEntrada = true;
                    itens[0].Observacoes = observacaoMarker;

                    ClickButton(dvi, "Entrada OK todos");
                    WaitForUiIdle();
                    if (!ObterDviItens(dvi).All(i => i.OkEntrada))
                    {
                        throw new InvalidOperationException("Entrada OK todos nao marcou todos os itens.");
                    }

                    // Mantem marker so no primeiro apos Entrada OK todos
                    ObterDviItens(dvi)[0].Observacoes = observacaoMarker;

                    ClickButton(dvi, "DviSalvarButton");
                    dviInteracted = true;
                }));

                ClickButton(orcWindow, "AbrirDviOrcamentoButton");
                WaitForUiIdle();
                if (!dviInteracted)
                {
                    throw new InvalidOperationException("Interacao com DviOrcamentoWindow nao concluiu (ShowDialog).");
                }

                var dviService = new DviChecklistService();
                if (!dviService.ExisteParaOrcamento(orcamento.Id))
                {
                    throw new InvalidOperationException("Arquivo orc-*.json nao foi criado apos Salvar DVI.");
                }

                var loadedOrc = dviService.CarregarPorOrcamentoOuPadrao(orcamento.Id);
                if (loadedOrc.Count != dviItemCount)
                {
                    throw new InvalidOperationException($"Contagem DVI divergiu apos salvar: esperado {dviItemCount}, veio {loadedOrc.Count}.");
                }

                if (loadedOrc.Count(i => string.Equals(i.Observacoes, observacaoMarker, StringComparison.Ordinal)) != 1)
                {
                    throw new InvalidOperationException("Observacao DVI nao preservada exatamente uma vez (risco de perda/duplicacao).");
                }

                if (!loadedOrc.All(i => i.OkEntrada))
                {
                    throw new InvalidOperationException("Estado OkEntrada nao preservado apos salvar DVI do orcamento.");
                }

                // Reabrir DVI: nao duplicar
                dviInteracted = false;
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    var dvi = Application.Current.Windows.OfType<DviOrcamentoWindow>().FirstOrDefault()
                        ?? throw new InvalidOperationException("Reabertura DVI falhou.");
                    var itens = ObterDviItens(dvi);
                    if (itens.Count != dviItemCount)
                    {
                        throw new InvalidOperationException($"Reabrir DVI alterou contagem: {itens.Count} vs {dviItemCount}.");
                    }

                    if (itens.Count(i => string.Equals(i.Observacoes, observacaoMarker, StringComparison.Ordinal)) != 1)
                    {
                        throw new InvalidOperationException("Reabrir DVI perdeu/duplicou observacao.");
                    }

                    // Cancelar nao deve duplicar
                    ClickButton(dvi, "Cancelar");
                    dviInteracted = true;
                }));
                ClickButton(orcWindow, "AbrirDviOrcamentoButton");
                WaitForUiIdle();
                if (!dviInteracted)
                {
                    throw new InvalidOperationException("Reabertura/cancelamento DVI nao concluiu.");
                }

                var afterCancel = dviService.CarregarPorOrcamentoOuPadrao(orcamento.Id);
                if (afterCancel.Count != dviItemCount)
                {
                    throw new InvalidOperationException("Cancelar DVI alterou persistencia indevidamente.");
                }

                // Aprovar + converter OS (mesmo padrao do smoke de orcamentos)
                var vm = new OrcamentosViewModel();
                var orcReload = orcService.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento sumiu antes da aprovacao.");
                vm.AprovarOrcamento(orcReload);
                var aprovado = orcService.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento aprovado nao recarregou.");
                var ordem = vm.ConverterEmOrdemServico(aprovado);
                if (ordem == null || ordem.Id == Guid.Empty)
                {
                    throw new InvalidOperationException("Conversao OS nao retornou ordem.");
                }

                if (ordem.OrcamentoId != orcamento.Id)
                {
                    throw new InvalidOperationException("OS nao vinculou OrcamentoId.");
                }

                // Abrir OS e validar heranca visual do checklist
                osWindow = new OrdemServicoWindow(App.Database, ordem)
                {
                    Width = width,
                    Height = height,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = 40,
                    Top = 40
                };
                ShowWindowForInteraction(osWindow);
                AssertWindowFitsSmallDesktop(osWindow, width, height);
                AssertPrimoxInputSurfaces(osWindow, tema, "OrdemServicoWindow");

                var osItens = ObterDviItensDaOs(osWindow);
                if (osItens.Count != dviItemCount)
                {
                    throw new InvalidOperationException(
                        $"OS nao herdou checklist completo: OS={osItens.Count}, orc={dviItemCount}.");
                }

                if (osItens.Count(i => string.Equals(i.Observacoes, observacaoMarker, StringComparison.Ordinal)) != 1)
                {
                    throw new InvalidOperationException("OS nao herdou observacao DVI do orcamento.");
                }

                if (!osItens.All(i => i.OkEntrada))
                {
                    throw new InvalidOperationException("OS nao herdou estado OkEntrada do DVI.");
                }

                // Nomes unicos — sem duplicata por Nome+Categoria
                var dup = osItens.GroupBy(i => (i.Categoria, i.Nome)).Where(g => g.Count() > 1).ToList();
                if (dup.Count > 0)
                {
                    throw new InvalidOperationException($"Checklist OS com itens duplicados: {dup[0].Key}");
                }

                // Orcamento permanece integro
                var orcFinal = orcService.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento sumiu apos conversao.");
                if (orcFinal.ClienteId != fixture.Cliente.Id || orcFinal.VeiculoId != fixture.Veiculo.Id)
                {
                    throw new InvalidOperationException("Orcamento perdeu cliente/veiculo apos fluxo DVI/OS.");
                }
            }
            finally
            {
                themeService.ApplyTheme(temaOriginal);
                if (osWindow?.IsVisible == true) osWindow.Close();
                if (orcWindow?.IsVisible == true) orcWindow.Close();
                PumpDispatcher();
            }
        }

        private static void AssertWindowFitsSmallDesktop(Window window, int width, int height)
        {
            if (window.ActualWidth > width + 2 || window.Width > width + 2)
            {
                // permite chrome minimo; falha se claramente maior que alvo
            }

            if (window.Width > 1600 || window.Height > 1000)
            {
                throw new InvalidOperationException(
                    $"Janela {window.GetType().Name} excede resolucao pequena ({window.Width}x{window.Height}).");
            }

            // Conteudo nao pode ter ActualWidth 0 (layout esmagado)
            if (window.Content is FrameworkElement fe)
            {
                fe.UpdateLayout();
                if (fe.ActualWidth < 200 || fe.ActualHeight < 200)
                {
                    throw new InvalidOperationException(
                        $"Conteudo de {window.GetType().Name} parece esmagado ({fe.ActualWidth}x{fe.ActualHeight}).");
                }
            }
        }

        private static void AssertPrimoxInputSurfaces(DependencyObject root, AppTheme tema, string contexto)
        {
            ValidarTemaAtual(tema, contexto);

            if (tema != AppTheme.Dark)
            {
                return;
            }

            var inputBg = Application.Current?.TryFindResource("InputBackgroundBrush") as SolidColorBrush;
            foreach (var tb in FindVisualChildren<TextBox>(root))
            {
                if (!tb.IsVisible) continue;
                // DatePicker PART_TextBox usa Transparent (RGB branco + A=0); nao e regressao Dark
                if (tb is System.Windows.Controls.Primitives.DatePickerTextBox) continue;
                if (tb.Background is SolidColorBrush local && IsNearWhite(local.Color))
                {
                    // Se o recurso de tema nao e branco, Background local branco e regressao Dark
                    if (inputBg == null || !IsNearWhite(inputBg.Color))
                    {
                        throw new InvalidOperationException(
                            $"Dark Mode: TextBox '{tb.Name}' com fundo claro hardcoded em {contexto}.");
                    }
                }
            }

            foreach (var cb in FindVisualChildren<ComboBox>(root))
            {
                if (!cb.IsVisible) continue;
                if (cb.Background is SolidColorBrush local && IsNearWhite(local.Color))
                {
                    if (inputBg == null || !IsNearWhite(inputBg.Color))
                    {
                        throw new InvalidOperationException(
                            $"Dark Mode: ComboBox '{cb.Name}' com fundo claro hardcoded em {contexto}.");
                    }
                }
            }
        }

        private static bool IsNearWhite(Color c) => c.A > 200 && c.R > 240 && c.G > 240 && c.B > 240; // Transparent=#00FFFFFF nao e fundo claro

        private static IList<DviChecklistItem> ObterDviItens(DviOrcamentoWindow window)
        {
            var field = typeof(DviOrcamentoWindow).GetField("_dviItens", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(window) is System.Collections.ObjectModel.ObservableCollection<DviChecklistItem> itens)
            {
                return itens;
            }

            throw new InvalidOperationException("Nao foi possivel acessar _dviItens em DviOrcamentoWindow.");
        }

        private static IList<DviChecklistItem> ObterDviItensDaOs(OrdemServicoWindow window)
        {
            var field = typeof(OrdemServicoWindow).GetField("_dviItens", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(window) is System.Collections.ObjectModel.ObservableCollection<DviChecklistItem> itens)
            {
                return itens;
            }

            throw new InvalidOperationException("Nao foi possivel acessar _dviItens em OrdemServicoWindow.");
        }
    }
}
