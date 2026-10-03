using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class CadastroFerramentaDialog : Window
    {
        private readonly IFerramentaService _ferramentaService;
        private readonly Ferramenta? _ferramentaExistente;
        private readonly CultureInfo _cultura = new CultureInfo("pt-BR");

        public CadastroFerramentaDialog(Ferramenta? ferramenta = null)
        {
            InitializeComponent();

            _ferramentaService = App.Services.GetService<IFerramentaService>()
                ?? new FerramentaService(App.Database, App.Logger);
            _ferramentaExistente = ferramenta;

            InicializarCategorias();
            CarregarDados();
        }

        private void InicializarCategorias()
        {
            CategoriaComboBox.Items.Clear();
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Diagnóstico Eletrônico (Scanners, Osciloscópios)", Tag = CategoriaFerramenta.DiagnosticoEletronico });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Medição Elétrica (Multímetros, Alicates)", Tag = CategoriaFerramenta.MedicaoEletrica });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Baterias & Carga (Testadores, Carregadores)", Tag = CategoriaFerramenta.BateriasECarga });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Elétrica & Montagem (Crimpadores, Solda)", Tag = CategoriaFerramenta.EletricaEMontagem });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Mecânica Geral (Torquímetros, Extratores)", Tag = CategoriaFerramenta.MecanicaGeral });
            CategoriaComboBox.SelectedIndex = 0;
        }

        private void CarregarDados()
        {
            if (_ferramentaExistente != null)
            {
                TituloModalText.Text = "Editar Ferramenta / Instrumento";
                CodigoPatrimonioTextBox.Text = _ferramentaExistente.CodigoPatrimonio;
                NomeTextBox.Text = _ferramentaExistente.Nome;
                MarcaModeloTextBox.Text = _ferramentaExistente.MarcaModelo;
                NumeroSerieTextBox.Text = _ferramentaExistente.NumeroSerie;
                LocalizacaoArmarioTextBox.Text = _ferramentaExistente.LocalizacaoArmario;
                ValorAquisicaoTextBox.Text = _ferramentaExistente.ValorAquisicao.ToString("N2", _cultura);
                ObservacoesTextBox.Text = _ferramentaExistente.Observacoes;

                foreach (ComboBoxItem item in CategoriaComboBox.Items)
                {
                    if (item.Tag is CategoriaFerramenta cat && cat == _ferramentaExistente.Categoria)
                    {
                        CategoriaComboBox.SelectedItem = item;
                        break;
                    }
                }

                RequerCalibracaoCheckBox.IsChecked = _ferramentaExistente.RequerCalibracaoPeriodica;
                IntervaloCalibracaoTextBox.Text = _ferramentaExistente.IntervaloCalibracaoDias.ToString();
                UltimaCalibracaoDatePicker.SelectedDate = _ferramentaExistente.UltimaCalibracao;
                PainelCalibracao.Visibility = _ferramentaExistente.RequerCalibracaoPeriodica ? Visibility.Visible : Visibility.Collapsed;

                ExcluirButton.Visibility = Visibility.Visible;
            }
            else
            {
                TituloModalText.Text = "Cadastrar Novo Equipamento";
                CodigoPatrimonioTextBox.Text = $"FER-{DateTime.Now:yyyyMMddHHmmss}".Substring(0, 12);
                LocalizacaoArmarioTextBox.Text = "Armário Geral";
                UltimaCalibracaoDatePicker.SelectedDate = DateTime.Today;
                ExcluirButton.Visibility = Visibility.Collapsed;
            }
        }

        private void RequerCalibracaoCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            PainelCalibracao.Visibility = RequerCalibracaoCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            var codigo = CodigoPatrimonioTextBox.Text?.Trim();
            var nome = NomeTextBox.Text?.Trim();
            var localizacao = LocalizacaoArmarioTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                MessageBox.Show("Informe o código de patrimônio do equipamento.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                CodigoPatrimonioTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                MessageBox.Show("Informe o nome/descrição da ferramenta.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                NomeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(localizacao))
            {
                localizacao = "Armário 01";
            }

            var ferramenta = _ferramentaExistente ?? new Ferramenta();
            ferramenta.CodigoPatrimonio = codigo;
            ferramenta.Nome = nome;
            ferramenta.MarcaModelo = MarcaModeloTextBox.Text?.Trim() ?? string.Empty;
            ferramenta.NumeroSerie = NumeroSerieTextBox.Text?.Trim() ?? string.Empty;
            ferramenta.LocalizacaoArmario = localizacao;
            ferramenta.Observacoes = ObservacoesTextBox.Text?.Trim() ?? string.Empty;

            if (CategoriaComboBox.SelectedItem is ComboBoxItem item && item.Tag is CategoriaFerramenta cat)
            {
                ferramenta.Categoria = cat;
            }

            if (decimal.TryParse(ValorAquisicaoTextBox.Text?.Trim(), NumberStyles.Any, _cultura, out var val))
            {
                ferramenta.ValorAquisicao = val;
            }

            ferramenta.RequerCalibracaoPeriodica = RequerCalibracaoCheckBox.IsChecked == true;
            if (ferramenta.RequerCalibracaoPeriodica)
            {
                if (int.TryParse(IntervaloCalibracaoTextBox.Text?.Trim(), out var dias) && dias > 0)
                {
                    ferramenta.IntervaloCalibracaoDias = dias;
                }
                else
                {
                    ferramenta.IntervaloCalibracaoDias = 365;
                }

                ferramenta.UltimaCalibracao = UltimaCalibracaoDatePicker.SelectedDate;
                if (ferramenta.UltimaCalibracao.HasValue)
                {
                    ferramenta.ProximaCalibracao = ferramenta.UltimaCalibracao.Value.AddDays(ferramenta.IntervaloCalibracaoDias);
                }
            }
            else
            {
                ferramenta.ProximaCalibracao = null;
            }

            try
            {
                await _ferramentaService.SalvarFerramentaAsync(ferramenta);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar ferramenta:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExcluirButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ferramentaExistente == null) return;

            var confirm = MessageBox.Show($"Deseja realmente remover o equipamento '{_ferramentaExistente.Nome}' ({_ferramentaExistente.CodigoPatrimonio}) da ferramentaria?", "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _ferramentaService.ExcluirFerramentaAsync(_ferramentaExistente.Id);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir ferramenta:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
