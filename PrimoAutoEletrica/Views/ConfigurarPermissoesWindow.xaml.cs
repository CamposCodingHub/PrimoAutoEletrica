using Microsoft.Win32;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.Views
{
    public partial class ConfigurarPermissoesWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly Funcionario _funcionarioLogado;
        private List<Permissao> _permissoes = new();
        private Permissao? _permissaoSelecionada;

        public ConfigurarPermissoesWindow(Funcionario funcionarioLogado)
        {
            InitializeComponent();
            _databaseService = App.Database;
            _funcionarioLogado = funcionarioLogado;

            Loaded += ConfigurarPermissoesWindow_Loaded;
        }

        private void ConfigurarPermissoesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarPermissoes();
            CarregarFiltros();
            AtualizarEstatisticas();
        }

        private void CarregarPermissoes()
        {
            try
            {
                _permissoes = _databaseService.ObterPermissoes();
                AtualizarDataGrid();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar permissoes.", ex);
                MessageBox.Show($"Erro ao carregar permissoes: {ex.Message}", UiText.T("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CarregarFiltros()
        {
            ModuloFilterComboBox.Items.Clear();
            ModuloFilterComboBox.Items.Add("Todos");
            foreach (var modulo in _permissoes.Select(p => p.Modulo).Distinct().OrderBy(m => m))
            {
                ModuloFilterComboBox.Items.Add(modulo);
            }
            ModuloFilterComboBox.SelectedIndex = 0;

            AcaoFilterComboBox.Items.Clear();
            AcaoFilterComboBox.Items.Add("Todas");
            foreach (var acao in _permissoes.Select(p => p.Acao).Distinct().OrderBy(a => a))
            {
                AcaoFilterComboBox.Items.Add(acao);
            }
            AcaoFilterComboBox.SelectedIndex = 0;
        }

        private void AtualizarDataGrid()
        {
            var filtroModulo = ModuloFilterComboBox.SelectedItem?.ToString();
            var filtroAcao = AcaoFilterComboBox.SelectedItem?.ToString();

            var permissoesFiltradas = _permissoes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filtroModulo) && filtroModulo != "Todos")
            {
                permissoesFiltradas = permissoesFiltradas.Where(p => p.Modulo == filtroModulo);
            }

            if (!string.IsNullOrWhiteSpace(filtroAcao) && filtroAcao != "Todas")
            {
                permissoesFiltradas = permissoesFiltradas.Where(p => p.Acao == filtroAcao);
            }

            PermissoesDataGrid.ItemsSource = permissoesFiltradas
                .OrderBy(p => p.OrdemExibicao)
                .ThenBy(p => p.Nome)
                .Select(p => new PermissaoGridItem(
                    p.Id,
                    p.Codigo,
                    p.Nome,
                    p.Modulo,
                    p.Acao,
                    p.Descricao,
                    p.Ativo ? "Ativa" : "Inativa"))
                .ToList();
        }

        private void AtualizarEstatisticas()
        {
            TotalPermissoesTextBlock.Text = _permissoes.Count.ToString();
            PermissoesAtivasTextBlock.Text = _permissoes.Count(p => p.Ativo).ToString();
            PermissoesInativasTextBlock.Text = _permissoes.Count(p => !p.Ativo).ToString();
            PermissoesEssenciaisTextBlock.Text = _permissoes.Count(p => p.Essencial).ToString();
        }

        private void ModuloFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarDataGrid();
        }

        private void AcaoFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarDataGrid();
        }

        private void PermissoesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PermissoesDataGrid.SelectedItem is not PermissaoGridItem selectedItem)
            {
                DetalhesPanel.Visibility = Visibility.Collapsed;
                NenhumaPermissaoSelecionadaTextBlock.Visibility = Visibility.Visible;
                return;
            }

            _permissaoSelecionada = _permissoes.FirstOrDefault(p => p.Id == selectedItem.Id);

            if (_permissaoSelecionada != null)
            {
                CarregarDetalhesPermissao();
                DetalhesPanel.Visibility = Visibility.Visible;
                NenhumaPermissaoSelecionadaTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void CarregarDetalhesPermissao()
        {
            if (_permissaoSelecionada == null)
            {
                return;
            }

            CodigoPermissaoTextBox.Text = _permissaoSelecionada.Codigo;
            NomePermissaoTextBox.Text = _permissaoSelecionada.Nome;
            DescricaoPermissaoTextBox.Text = _permissaoSelecionada.Descricao;

            ConfigurarEstadoEdicaoPermissao(false);
        }

        private void NovaPermissaoButton_Click(object sender, RoutedEventArgs e)
        {
            _permissaoSelecionada = null;
            PermissoesDataGrid.SelectedItem = null;
            CodigoPermissaoTextBox.Text = "";
            NomePermissaoTextBox.Text = "";
            DescricaoPermissaoTextBox.Text = "";

            DetalhesPanel.Visibility = Visibility.Visible;
            NenhumaPermissaoSelecionadaTextBlock.Visibility = Visibility.Collapsed;
            ConfigurarEstadoEdicaoPermissao(true);

            CodigoPermissaoTextBox.Focus();
        }

        private void EditarPermissaoButton_Click(object sender, RoutedEventArgs e)
        {
            TrySelecionarPermissaoPeloContexto(sender);

            if (_permissaoSelecionada == null || _permissaoSelecionada.Essencial)
            {
                return;
            }

            ConfigurarEstadoEdicaoPermissao(true);
            CodigoPermissaoTextBox.Focus();
        }

        private void ExcluirPermissaoButton_Click(object sender, RoutedEventArgs e)
        {
            TrySelecionarPermissaoPeloContexto(sender);

            if (_permissaoSelecionada == null)
            {
                return;
            }

            if (_permissaoSelecionada.Essencial)
            {
                MessageBox.Show("Esta permissao nao pode ser excluida pois e essencial para o funcionamento do sistema.", UiText.T("Warning"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!CriticalActionDialogService.ConfirmarExclusao(
                this,
                "permissao",
                _permissaoSelecionada.Nome,
                $"Codigo: {_permissaoSelecionada.Codigo}\nModulo: {_permissaoSelecionada.Modulo}\nAcao: {_permissaoSelecionada.Acao}",
                "A permissao sera removida do cadastro administrativo. Revise perfis e operacoes dependentes antes de confirmar."))
            {
                return;
            }

            try
            {
                _databaseService.ExcluirPermissao(_permissaoSelecionada.Id, _funcionarioLogado.Nome);
                MessageBox.Show("Permissao excluida com sucesso!", UiText.T("Success"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CarregarPermissoes();
                CarregarFiltros();
                AtualizarEstatisticas();

                _permissaoSelecionada = null;
                PermissoesDataGrid.SelectedItem = null;
                DetalhesPanel.Visibility = Visibility.Collapsed;
                NenhumaPermissaoSelecionadaTextBlock.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao excluir permissao.", ex);
                MessageBox.Show($"Erro ao excluir permissao: {ex.Message}", UiText.T("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SalvarPermissaoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarFormularioPermissao())
                {
                    return;
                }

                if (_permissaoSelecionada?.Essencial == true)
                {
                    MessageBox.Show("Permissoes essenciais nao podem ser editadas por esta tela.", "Permissao essencial",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var codigo = CodigoPermissaoTextBox.Text.Trim().ToUpperInvariant();
                var permissao = new Permissao
                {
                    Id = _permissaoSelecionada?.Id ?? 0,
                    Codigo = codigo,
                    Nome = NomePermissaoTextBox.Text.Trim(),
                    Descricao = DescricaoPermissaoTextBox.Text.Trim(),
                    Modulo = ExtrairModuloDoCodigo(codigo),
                    Acao = ExtrairAcaoDoCodigo(codigo),
                    Ativo = _permissaoSelecionada?.Ativo ?? true,
                    Essencial = _permissaoSelecionada?.Essencial ?? false,
                    OrdemExibicao = _permissaoSelecionada?.OrdemExibicao ?? _permissoes.Count + 1
                };

                var criandoNovaPermissao = permissao.Id == 0;
                if (!CriticalActionDialogService.ConfirmarAcao(
                    this,
                    new CriticalActionRequest
                    {
                        WindowTitle = criandoNovaPermissao ? "Criar permissao" : "Atualizar permissao",
                        Header = criandoNovaPermissao ? "Cadastro de permissao administrativa" : "Alteracao de permissao administrativa",
                        Summary = criandoNovaPermissao
                            ? $"Voce esta prestes a criar a permissao '{permissao.Codigo}'."
                            : $"Voce esta prestes a atualizar a permissao '{permissao.Codigo}'.",
                        Details = $"Nome: {permissao.Nome}\nModulo: {permissao.Modulo}\nAcao: {permissao.Acao}\nStatus: {(permissao.Ativo ? "Ativa" : "Inativa")}",
                        Impact = "Mudancas nesta permissao podem alterar o acesso de perfis e operacoes administrativas do sistema.",
                        Keyword = "SALVAR",
                        ConfirmButtonText = criandoNovaPermissao ? "Criar permissao" : "Salvar alteracoes"
                    }))
                {
                    return;
                }

                _databaseService.SalvarPermissao(permissao, _funcionarioLogado.Nome);
                App.Logger.LogInfo($"Permissao '{permissao.Codigo}' salva por '{_funcionarioLogado.Nome}'.");

                MessageBox.Show("Permissao salva com sucesso!", UiText.T("Success"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CarregarPermissoes();
                CarregarFiltros();
                AtualizarEstatisticas();

                _permissaoSelecionada = null;
                PermissoesDataGrid.SelectedItem = null;
                DetalhesPanel.Visibility = Visibility.Collapsed;
                NenhumaPermissaoSelecionadaTextBlock.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao salvar permissao.", ex);
                MessageBox.Show($"Erro ao salvar permissao: {ex.Message}", UiText.T("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidarFormularioPermissao()
        {
            if (string.IsNullOrWhiteSpace(CodigoPermissaoTextBox.Text))
            {
                MessageBox.Show("O codigo da permissao e obrigatorio.", UiText.T("Validation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodigoPermissaoTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(NomePermissaoTextBox.Text))
            {
                MessageBox.Show("O nome da permissao e obrigatorio.", UiText.T("Validation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NomePermissaoTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(DescricaoPermissaoTextBox.Text))
            {
                MessageBox.Show("A descricao da permissao e obrigatoria.", UiText.T("Validation"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DescricaoPermissaoTextBox.Focus();
                return false;
            }

            return true;
        }

        private void CancelarPermissaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_permissaoSelecionada != null)
            {
                CarregarDetalhesPermissao();
                DetalhesPanel.Visibility = Visibility.Visible;
                NenhumaPermissaoSelecionadaTextBlock.Visibility = Visibility.Collapsed;
                return;
            }

            ConfigurarEstadoEdicaoPermissao(false);
            DetalhesPanel.Visibility = Visibility.Collapsed;
            NenhumaPermissaoSelecionadaTextBlock.Visibility = Visibility.Visible;
        }

        private void ExportarPermissoesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Arquivo CSV (*.csv)|*.csv|Arquivo TXT (*.txt)|*.txt",
                    FileName = $"permissoes_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }

                var csv = new StringBuilder();
                csv.AppendLine("Codigo,Nome,Modulo,Acao,Descricao,Status");

                foreach (var permissao in _permissoes.OrderBy(p => p.OrdemExibicao))
                {
                    csv.AppendLine(string.Join(",",
                        EscapeCsv(permissao.Codigo),
                        EscapeCsv(permissao.Nome),
                        EscapeCsv(permissao.Modulo),
                        EscapeCsv(permissao.Acao),
                        EscapeCsv(permissao.Descricao),
                        EscapeCsv(permissao.Ativo ? "Ativa" : "Inativa")));
                }

                System.IO.File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);

                MessageBox.Show("Permissoes exportadas com sucesso!", UiText.T("Success"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao exportar permissoes.", ex);
                MessageBox.Show($"Erro ao exportar permissoes: {ex.Message}", UiText.T("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfigurarEstadoEdicaoPermissao(bool emEdicao)
        {
            var bloqueada = _permissaoSelecionada?.Essencial == true;
            var somenteLeitura = !emEdicao || bloqueada;

            CodigoPermissaoTextBox.IsReadOnly = somenteLeitura;
            NomePermissaoTextBox.IsReadOnly = somenteLeitura;
            DescricaoPermissaoTextBox.IsReadOnly = somenteLeitura;
            SalvarPermissaoButton.IsEnabled = emEdicao && !bloqueada;
            CancelarPermissaoButton.IsEnabled = emEdicao;
        }

        private void TrySelecionarPermissaoPeloContexto(object sender)
        {
            if (sender is not FrameworkElement { DataContext: PermissaoGridItem permissaoGridItem })
            {
                return;
            }

            PermissoesDataGrid.SelectedItem = permissaoGridItem;
            _permissaoSelecionada = _permissoes.FirstOrDefault(p => p.Id == permissaoGridItem.Id);

            if (_permissaoSelecionada == null)
            {
                return;
            }

            CarregarDetalhesPermissao();
            DetalhesPanel.Visibility = Visibility.Visible;
            NenhumaPermissaoSelecionadaTextBlock.Visibility = Visibility.Collapsed;
        }

        private static string ExtrairModuloDoCodigo(string codigo)
        {
            var upper = codigo.Trim().ToUpperInvariant();
            if (upper.StartsWith("ORDENS_SERVICO", StringComparison.OrdinalIgnoreCase))
            {
                return "OrdensServico";
            }

            if (upper.StartsWith("IMPORTAR_NFE", StringComparison.OrdinalIgnoreCase))
            {
                return "ImportarNFe";
            }

            var prefixo = upper.Split('_', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Sistema";
            return prefixo switch
            {
                "VEICULOS" => "Veiculos",
                "ORCAMENTOS" => "Orcamentos",
                "RELATORIOS" => "Relatorios",
                "FUNCIONARIOS" => "Funcionarios",
                _ => char.ToUpperInvariant(prefixo[0]) + prefixo[1..].ToLowerInvariant()
            };
        }

        private static string ExtrairAcaoDoCodigo(string codigo)
        {
            var partes = codigo.Split('_', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length > 1 ? partes[^1] : "Ver";
        }

        private static string EscapeCsv(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private sealed record PermissaoGridItem(
            int Id,
            string Codigo,
            string Nome,
            string Modulo,
            string Acao,
            string Descricao,
            string Status);
    }
}
