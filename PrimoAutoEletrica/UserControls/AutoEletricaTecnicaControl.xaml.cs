using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class AutoEletricaTecnicaControl : UserControl
    {
        private readonly AutoEletricaTecnicaViewModel _viewModel;
        private AutoEletricaTecnicaService _service = new();
        private AutoEletricaTecnicaSnapshot _snapshot = new();

        public AutoEletricaTecnicaControl()
        {
            InitializeComponent();
            _viewModel = App.Services.GetRequiredService<AutoEletricaTecnicaViewModel>();
            DataContext = _viewModel;
            Loaded += (_, _) =>
            {
                _viewModel.CarregarDadosCommand.Execute(null);
                CarregarDados();
            };
        }

        private void CarregarDados()
        {
            _snapshot = _service.CriarSnapshot();
            new AutoEletricaRoteiroPersistService().AplicarEm(_snapshot.RoteirosDiagnostico);

            RoteirosDiagnosticoListBox.ItemsSource = _snapshot.RoteirosDiagnostico;
            BibliotecaTecnicaListBox.ItemsSource = _snapshot.BibliotecaTecnica;
            DefeitosRecorrentesDataGrid.ItemsSource = _snapshot.DefeitosRecorrentes;
            ServicosTecnicosDataGrid.ItemsSource = _snapshot.ServicosTecnicos;
            SugestoesPecasListBox.ItemsSource = _snapshot.SugestoesPecas;

            if (_snapshot.RoteirosDiagnostico.Count > 0 && RoteirosDiagnosticoListBox.SelectedItem == null)
            {
                RoteirosDiagnosticoListBox.SelectedIndex = 0;
            }

            CarregarProntuario();
            AtualizarResumo();
        }

        private void CarregarProntuario()
        {
            if (_snapshot.Prontuario == null)
            {
                ProntuarioVeiculoTextBlock.Text = "Nenhum veiculo encontrado para exibir o prontuario eletrico.";
                ProntuarioItemsControl.ItemsSource = Array.Empty<ProntuarioEletricoCampo>();
                return;
            }

            ProntuarioVeiculoTextBlock.Text =
                $"{_snapshot.Prontuario.Veiculo} | Placa {_snapshot.Prontuario.Placa} | Sistema {_snapshot.Prontuario.SistemaEletrico}";
            ProntuarioItemsControl.ItemsSource = _snapshot.Prontuario.Campos;
        }

        private void AtualizarResumo()
        {
            ResumoTecnicoTextBlock.Text =
                $"{_snapshot.RoteirosDiagnostico.Count} roteiros | " +
                $"{_snapshot.BibliotecaTecnica.Count} itens de biblioteca | " +
                $"{_snapshot.ServicosTecnicos.Count} servicos tecnicos | " +
                $"{_snapshot.SugestoesPecas.Count} sugestoes de pecas | " +
                $"{_snapshot.DefeitosRecorrentes.Count} recorrencias mapeadas";
        }

        private void AtualizarDetalheRoteiro(DiagnosticoGuiadoRoteiro? roteiro)
        {
            if (roteiro == null)
            {
                RoteiroDetalheTituloTextBlock.Text = "Selecione um roteiro para visualizar os testes.";
                RoteiroTestesItemsControl.ItemsSource = Array.Empty<string>();
                RoteiroValoresItemsControl.ItemsSource = Array.Empty<string>();
                return;
            }

            RoteiroDetalheTituloTextBlock.Text = $"{roteiro.Codigo} - {roteiro.Titulo}";
            RoteiroTestesItemsControl.ItemsSource = roteiro.SequenciaTestes.Select((teste, index) => $"{index + 1}. {teste}");
            RoteiroValoresItemsControl.ItemsSource = roteiro.ValoresEsperados.Select(valor => $"- {valor}");
        }

        private void AtualizarTecnicaButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarDados();
        }

        private void RoteirosDiagnosticoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarDetalheRoteiro(RoteirosDiagnosticoListBox.SelectedItem as DiagnosticoGuiadoRoteiro);
        }

        private void GerarOrcamentoDiagnosticoButton_Click(object sender, RoutedEventArgs e)
        {
            var roteiro = RoteirosDiagnosticoListBox.SelectedItem as DiagnosticoGuiadoRoteiro
                ?? _snapshot.RoteirosDiagnostico.FirstOrDefault();
            var veiculoId = _snapshot.Prontuario?.VeiculoId;
            var veiculo = veiculoId.HasValue
                ? App.Repositories.Clientes.ObterTodosVeiculos().FirstOrDefault(item => item.Id == veiculoId.Value)
                : App.Repositories.Clientes.ObterTodosVeiculos().FirstOrDefault();

            if (roteiro == null || veiculo?.ClienteId == null)
            {
                AtualizarStatusOrcamento("Selecione um roteiro e um veiculo vinculado a cliente para gerar orcamento.");
                return;
            }

            try
            {
                var servico = _snapshot.ServicosTecnicos.FirstOrDefault(item =>
                    roteiro.ServicosSugeridos.Any(sugerido => item.Nome.Contains(sugerido, StringComparison.OrdinalIgnoreCase) ||
                                                              sugerido.Contains(item.Nome, StringComparison.OrdinalIgnoreCase)))
                    ?? _snapshot.ServicosTecnicos.First();

                var orcamento = _service.CriarOrcamentoAPartirDiagnostico(new OrcamentoDiagnosticoDraft
                {
                    ClienteId = veiculo.ClienteId.Value,
                    VeiculoId = veiculo.Id,
                    Roteiro = roteiro.Titulo,
                    Resultado = roteiro.Resultado,
                    Conclusao = roteiro.Conclusao,
                    ValorMaoObra = servico.ValorPadrao,
                    Pecas = roteiro.PecasSugeridas
                });

                AtualizarStatusOrcamento($"Orcamento {orcamento.Numero} gerado em rascunho a partir do diagnostico.");
            }
            catch (Exception ex)
            {
                AtualizarStatusOrcamento($"Falha ao gerar orcamento: {ex.Message}");
                if (!App.IsAutomatedTestMode)
                {
                    MessageBox.Show(
                        $"Falha ao gerar orcamento: {ex.Message}",
                        "Auto eletrica tecnica",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private void AtualizarStatusOrcamento(string mensagem)
        {
            ResumoTecnicoTextBlock.Text = mensagem;
            App.Logger.LogInfo($"Auto eletrica tecnica: {mensagem}");
        }

        private void GerarLaudoEletricoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var roteiro = RoteirosDiagnosticoListBox.SelectedItem as DiagnosticoGuiadoRoteiro
                    ?? _snapshot.RoteirosDiagnostico.FirstOrDefault();
                var path = CommercialDocumentActions.GerarLaudo(
                    roteiro,
                    _snapshot.Prontuario,
                    App.Session?.CurrentUser?.Nome);
                if (roteiro != null)
                {
                    new AutoEletricaRoteiroPersistService().SalvarResultado(roteiro);
                    new SintomaCausaHistoricoService().Registrar(roteiro, _snapshot.Prontuario);
                }
                CommercialDocumentActions.AbrirArquivo(path);
                MessageBox.Show($"Laudo gerado:\n{path}", "Laudo eletrico", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro ao gerar laudo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnviarLaudoWhatsAppButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var roteiro = RoteirosDiagnosticoListBox.SelectedItem as DiagnosticoGuiadoRoteiro
                    ?? _snapshot.RoteirosDiagnostico.FirstOrDefault();
                var path = CommercialDocumentActions.GerarLaudo(
                    roteiro,
                    _snapshot.Prontuario,
                    App.Session?.CurrentUser?.Nome);
                var telefone = string.Empty;
                if (_snapshot.Prontuario?.VeiculoId != null)
                {
                    var veiculo = App.Repositories.Clientes.ObterTodosVeiculos()
                        .FirstOrDefault(v => v.Id == _snapshot.Prontuario.VeiculoId);
                    if (veiculo != null)
                    {
                        var cliente = veiculo.ClienteId.HasValue ? App.Repositories.Clientes.ObterPorId(veiculo.ClienteId.Value) : null;
                        telefone = cliente?.WhatsApp ?? cliente?.Telefone ?? string.Empty;
                    }
                }

                var msg = $"Segue laudo tecnico auto eletrica ({roteiro?.Titulo ?? "diagnostico"}).";
                CommercialDocumentActions.EnviarWhatsAppArquivo(telefone, msg, path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro ao enviar laudo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
