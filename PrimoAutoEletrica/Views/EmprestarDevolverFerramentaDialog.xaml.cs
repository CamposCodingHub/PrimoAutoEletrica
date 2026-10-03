using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.Views
{
    public partial class EmprestarDevolverFerramentaDialog : Window
    {
        private readonly IFerramentaService _ferramentaService;
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly Guid? _ferramentaInicialId;
        private Ferramenta? _ferramentaAtual;

        public EmprestarDevolverFerramentaDialog(Guid? ferramentaId = null)
        {
            InitializeComponent();

            _ferramentaService = App.Services.GetService<IFerramentaService>()
                ?? new FerramentaService(App.Database, App.Logger);
            _funcionarioRepository = App.Repositories.Funcionarios;
            _ferramentaInicialId = ferramentaId;

            Loaded += EmprestarDevolverFerramentaDialog_Loaded;
        }

        private async void EmprestarDevolverFerramentaDialog_Loaded(object sender, RoutedEventArgs e)
        {
            InicializarCombos();

            if (_ferramentaInicialId.HasValue)
            {
                var f = await _ferramentaService.ObterPorIdAsync(_ferramentaInicialId.Value);
                if (f != null)
                {
                    CodigoBuscaTextBox.Text = f.CodigoPatrimonio;
                    await ExibirFerramentaAsync(f);
                }
            }
            else
            {
                CodigoBuscaTextBox.Focus();
            }
        }

        private void InicializarCombos()
        {
            // Funcionarios
            FuncionarioComboBox.Items.Clear();
            var funcionarios = _funcionarioRepository.ObterTodos().Where(f => f.Ativo).OrderBy(f => f.Nome);
            foreach (var func in funcionarios)
            {
                FuncionarioComboBox.Items.Add(new ComboBoxItem
                {
                    Content = func.Nome,
                    Tag = func.Id
                });
            }
            if (FuncionarioComboBox.Items.Count > 0)
            {
                FuncionarioComboBox.SelectedIndex = 0;
            }

            // Estados de conservacao
            EstadoConservacaoComboBox.Items.Clear();
            EstadoConservacaoComboBox.Items.Add(new ComboBoxItem { Content = "OK — Perfeito Estado e Limpo" });
            EstadoConservacaoComboBox.Items.Add(new ComboBoxItem { Content = "Avariado / Danificado / Em Falha" });
            EstadoConservacaoComboBox.Items.Add(new ComboBoxItem { Content = "Necessita Calibração / Manutenção" });
            EstadoConservacaoComboBox.Items.Add(new ComboBoxItem { Content = "Faltando Cabos / Acessórios" });
            EstadoConservacaoComboBox.SelectedIndex = 0;
        }

        private async void CodigoBuscaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await ProcessarBuscaCodigoAsync();
            }
        }

        private async void CodigoBuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var texto = CodigoBuscaTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(texto) && texto.Length >= 3)
            {
                await ProcessarBuscaCodigoAsync();
            }
        }

        private async Task ProcessarBuscaCodigoAsync()
        {
            var codigo = CodigoBuscaTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(codigo)) return;

            var f = await _ferramentaService.ObterPorCodigoOuSerieAsync(codigo);
            await ExibirFerramentaAsync(f);
        }

        private Task ExibirFerramentaAsync(Ferramenta? f)
        {
            _ferramentaAtual = f;

            if (f == null)
            {
                FerramentaDetalhesBorder.Visibility = Visibility.Collapsed;
                PainelRetirada.Visibility = Visibility.Collapsed;
                PainelDevolucao.Visibility = Visibility.Collapsed;
                PainelInstrucaoInicial.Visibility = Visibility.Visible;
                ConfirmarAcaoButton.IsEnabled = false;
                return Task.CompletedTask;
            }

            FerramentaDetalhesBorder.Visibility = Visibility.Visible;
            PainelInstrucaoInicial.Visibility = Visibility.Collapsed;

            FerramentaCodigoText.Text = f.CodigoPatrimonio;
            FerramentaNomeText.Text = f.Nome;
            FerramentaMarcaArmarioText.Text = $"{f.MarcaModelo} — Localização: {f.LocalizacaoArmario}";
            StatusBadgeText.Text = f.StatusDescricao;

            if (f.Status == StatusFerramenta.EmUso)
            {
                // Modo Devolucao
                PainelDevolucao.Visibility = Visibility.Visible;
                PainelRetirada.Visibility = Visibility.Collapsed;

                DevolucaoPosseInfoText.Text = $"👤 Técnico em Posse: {f.FuncionarioPosseAtualNome}\n🚗 Ordem de Serviço: {f.NumeroOSAtual ?? "Sem OS vinculada"}\n⏱️ Retirada: {f.DataHoraRetiradaAtual:dd/MM/yyyy HH:mm}";

                ConfirmarAcaoButton.Content = "Confirmar Devolução no Armário (Enter)";
                ConfirmarAcaoButton.IsEnabled = true;
            }
            else
            {
                // Modo Retirada
                PainelRetirada.Visibility = Visibility.Visible;
                PainelDevolucao.Visibility = Visibility.Collapsed;

                ConfirmarAcaoButton.Content = "Confirmar Retirada / Empréstimo (Enter)";
                ConfirmarAcaoButton.IsEnabled = true;
            }

            return Task.CompletedTask;
        }

        private async void ConfirmarAcaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ferramentaAtual == null) return;

            try
            {
                if (_ferramentaAtual.Status == StatusFerramenta.EmUso)
                {
                    // Devolucao
                    var estado = (EstadoConservacaoComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "OK";
                    var obs = ObservacoesDevolucaoTextBox.Text?.Trim();

                    await _ferramentaService.RegistrarDevolucaoAsync(
                        _ferramentaAtual.Id,
                        estadoConservacaoDevolucao: estado,
                        observacoesDevolucao: obs);

                    MessageBox.Show(
                        $"Ferramenta '{_ferramentaAtual.Nome}' devolvida ao armário com sucesso!",
                        "Devolução Confirmada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    // Retirada
                    var funcItem = FuncionarioComboBox.SelectedItem as ComboBoxItem;
                    if (funcItem == null || funcItem.Tag is not Guid funcId)
                    {
                        MessageBox.Show("Selecione o técnico responsável pela retirada.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var funcNome = funcItem.Content?.ToString() ?? "Técnico";
                    var osNumero = NumeroOSTextBox.Text?.Trim();

                    await _ferramentaService.RegistrarRetiradaAsync(
                        _ferramentaAtual.Id,
                        funcId,
                        funcNome,
                        ordemServicoId: null,
                        numeroOS: !string.IsNullOrEmpty(osNumero) ? osNumero : null,
                        previsaoDevolucao: DateTime.Now.AddHours(4));

                    MessageBox.Show(
                        $"Retirada da ferramenta '{_ferramentaAtual.Nome}' registrada com sucesso para {funcNome}!",
                        "Retirada Confirmada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha na operação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
