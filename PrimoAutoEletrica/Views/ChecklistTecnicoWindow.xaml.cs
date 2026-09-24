using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class ChecklistTecnicoWindow : Window
    {
        private readonly Guid _ordemServicoId;
        private readonly Guid _veiculoId;
        private readonly Guid? _clienteId;
        private readonly bool _isLinhaPesada;
        private readonly IChecklistTecnicoService _checklistService;
        private ChecklistTecnicoOS _checklist;
        private List<ChecklistTecnicoItem> _todosItens = new();

        public ChecklistTecnicoWindow(
            Guid ordemServicoId,
            Guid veiculoId,
            Guid? clienteId = null,
            string osNumero = "",
            string veiculoDescricao = "",
            bool isLinhaPesada = false,
            IChecklistTecnicoService? checklistService = null)
        {
            InitializeComponent();

            _ordemServicoId = ordemServicoId;
            _veiculoId = veiculoId;
            _clienteId = clienteId;
            _isLinhaPesada = isLinhaPesada;
            _checklistService = checklistService ?? new ChecklistTecnicoService();

            OsNumeroTextBlock.Text = string.IsNullOrWhiteSpace(osNumero) ? _ordemServicoId.ToString()[..8] : osNumero;
            TensaoContextoTextBlock.Text = isLinhaPesada ? "24V (Linha Pesada)" : "12V (Linha Leve)";
            ContextoSubheaderText.Text = $"Veículo: {veiculoDescricao} | Sistema {_checklistService?.GetType().Name} auditável";

            _checklist = _checklistService!.ObterOuCriarPadrao(_ordemServicoId, _veiculoId, _clienteId, _isLinhaPesada);
            _todosItens = _checklist.Itens?.ToList() ?? new List<ChecklistTecnicoItem>();

            PopularFiltrosSecao();
            CarregarDadosNaTela();
        }

        private void PopularFiltrosSecao()
        {
            var secoes = new List<string> { "Todas as Seções" };
            secoes.AddRange(_todosItens.Select(i => i.Secao).Distinct().OrderBy(s => s));
            SecaoFilterComboBox.ItemsSource = secoes;
            SecaoFilterComboBox.SelectedIndex = 0;
        }

        private void CarregarDadosNaTela()
        {
            TecnicoTextBox.Text = _checklist.TecnicoResponsavel;
            ObservacoesGeraisTextBox.Text = _checklist.ObservacoesGerais;
            StatusGeralTextBlock.Text = _checklist.Concluido ? "CONCLUÍDO" : "EM ANDAMENTO";

            AplicarFiltro();
            RecalcularMetricas();
        }

        private void AplicarFiltro()
        {
            var secaoSelecionada = SecaoFilterComboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(secaoSelecionada) || secaoSelecionada == "Todas as Seções")
            {
                ChecklistItemsControl.ItemsSource = null;
                ChecklistItemsControl.ItemsSource = _todosItens;
            }
            else
            {
                ChecklistItemsControl.ItemsSource = null;
                ChecklistItemsControl.ItemsSource = _todosItens.Where(i => i.Secao == secaoSelecionada).ToList();
            }
        }

        private void RecalcularMetricas()
        {
            var total = _todosItens.Count;
            var ok = _todosItens.Count(i => i.Status == ChecklistStatusEnum.OK);
            var atencao = _todosItens.Count(i => i.Status == ChecklistStatusEnum.Atencao);
            var critico = _todosItens.Count(i => i.Status == ChecklistStatusEnum.Critico);
            var pendentes = _todosItens.Count(i => i.Status == ChecklistStatusEnum.NaoTestado);

            BadgeOkText.Text = $"OK: {ok}";
            BadgeAtencaoText.Text = $"Atenção: {atencao}";
            BadgeCriticoText.Text = $"Crítico: {critico}";
            BadgePendentesText.Text = $"Pendentes: {pendentes}";

            var testados = total - pendentes;
            var pct = total > 0 ? (int)Math.Round((double)testados / total * 100.0) : 0;
            ChecklistProgressBar.Value = pct;
            PercentualText.Text = $"{pct}%";
        }

        private void SecaoFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltro();
        }

        private void MarcarTodosOkButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _todosItens)
            {
                if (item.Status == ChecklistStatusEnum.NaoTestado)
                {
                    item.Status = ChecklistStatusEnum.OK;
                }
            }

            AplicarFiltro();
            RecalcularMetricas();
        }

        private void StatusRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RecalcularMetricas();
        }

        private void StatusAtencao_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is ChecklistTecnicoItem item)
            {
                item.Status = ChecklistStatusEnum.Atencao;
            }
            RecalcularMetricas();
        }

        private void StatusCritico_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is ChecklistTecnicoItem item)
            {
                item.Status = ChecklistStatusEnum.Critico;
            }
            RecalcularMetricas();
        }

        private void StatusNaoDisponivel_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is ChecklistTecnicoItem item)
            {
                item.Status = ChecklistStatusEnum.NaoDisponivel;
            }
            RecalcularMetricas();
        }

        private void ValorMedido_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Força atualização da exibição do delta
            RecalcularMetricas();
        }

        private void ValorPosReparo_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Força atualização da exibição do delta
            RecalcularMetricas();
        }

        private void AnexarEvidenciaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ChecklistTecnicoItem item)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Selecionar Evidência Local de Inspeção",
                    Filter = "Imagens e Documentos (*.jpg;*.png;*.pdf)|*.jpg;*.jpeg;*.png;*.pdf|Todos os Arquivos (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    item.EvidenciaPath = dialog.FileName;
                    MessageBox.Show($"Evidência local vinculada ao item '{item.Descricao}':\n{dialog.FileName}", "Evidência Registrada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void SalvarProgressoButton_Click(object sender, RoutedEventArgs e)
        {
            SalvarEstadoAtual();
            MessageBox.Show("Progresso do Checklist salvo com sucesso!", "Checklist Técnico", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SalvarEstadoAtual()
        {
            _checklist.TecnicoResponsavel = TecnicoTextBox.Text.Trim();
            _checklist.ObservacoesGerais = ObservacoesGeraisTextBox.Text.Trim();
            _checklist.Itens = _todosItens;
            _checklistService.Salvar(_checklist);
        }

        private void ConcluirChecklistButton_Click(object sender, RoutedEventArgs e)
        {
            SalvarEstadoAtual();

            var pendentes = _todosItens.Count(i => i.Status == ChecklistStatusEnum.NaoTestado);
            if (pendentes > 0)
            {
                var resp = MessageBox.Show(
                    $"Ainda existem {pendentes} item(ns) pendente(s) de teste.\nDeseja concluir a inspeção assim mesmo?",
                    "Itens Pendentes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resp != MessageBoxResult.Yes) return;
            }

            var tecnico = TecnicoTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(tecnico))
            {
                tecnico = "Técnico Responsável";
            }

            _checklistService.Concluir(_checklist.Id, tecnico, ObservacoesGeraisTextBox.Text.Trim());
            _checklist.Concluido = true;
            StatusGeralTextBlock.Text = "CONCLUÍDO";

            MessageBox.Show("Checklist Técnico concluído e laudo pericial gravado com sucesso!", "Inspeção Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void AbrirDiagnosticoGuiadoButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current?.MainWindow is MainWindow mainWin)
            {
                try { mainWin.NavigateToModule("AutoEletricaTecnica"); } catch { }
            }

            MessageBox.Show(
                $"Navegando para o módulo Autoelétrica Técnica — Diagnóstico Guiado D01 a D06.\nUtilize os roteiros estruturados para investigação de anomalias detectadas no checklist da OS #{OsNumeroTextBlock.Text}.",
                "Diagnóstico Guiado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            SalvarEstadoAtual();
            Close();
        }
    }
}
