using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimoAutoEletrica.UserControls
{
    public partial class VeiculosControl : UserControl
    {
        private readonly VeiculosViewModel _viewModel;
        private readonly DatabaseService _databaseService;
        private readonly PermissionService _permissionService;
        private Dictionary<Guid, Cliente> _clientes = new();

        public VeiculosControl()
        {
            InitializeComponent();
            _viewModel = new VeiculosViewModel();
            DataContext = _viewModel;
            _databaseService = global::PrimoAutoEletrica.App.Database;
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);
            _clientes = App.Repositories.Clientes.ObterTodos().ToDictionary(c => c.Id, c => c);
            _viewModel.LoadVeiculos();
        }

        private void CarregarVeiculos()
        {
            _viewModel.LoadVeiculos();
        }

        private void AtualizarIndicadores()
        {
            // Indicators are bound directly to the ViewModel.
        }

        private void LimparFiltros_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetFilters();
        }

        private void NovoVeiculoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("VEICULOS_CRIAR", "Voce nao possui permissao para cadastrar veiculos."))
            {
                return;
            }

            var janela = new NovoVeiculoWindow(_databaseService, _clientes);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (janela.ShowDialog() == true)
            {
                CarregarVeiculos();
            }
        }

        private void ExportarVeiculosButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("VEICULOS_EXPORTAR", "Voce nao possui permissao para exportar dados de veiculos."))
            {
                App.Audit.RegistrarAcaoCritica(
                    "Veiculos",
                    "ExportarVeiculosNegado",
                    "Veiculo",
                    "Exportacao",
                    $"Usuario={App.Session.UserName}; Perfil={App.Session.AccessProfile}");
                return;
            }

            try
            {
                var itens = _viewModel.FilteredVeiculos.ToList();
                var exportDir = Path.Combine(App.RuntimeAppDataPath, "Exports");
                Directory.CreateDirectory(exportDir);
                var path = Path.Combine(exportDir, $"veiculos_{DateTime.Now:yyyyMMddHHmmss}.csv");

                var linhas = new List<string>
                {
                    "Placa,Marca,Modelo,Ano,Tipo,SistemaEletrico,Cliente,Quilometragem,Retorno,Garantia,ProximaRevisao,Alerta,ResumoAlerta,Foto,Documento,OS,Agendamentos,Orcamentos"
                };

                linhas.AddRange(itens.Select(item => string.Join(",",
                    Csv(item.PlacaFormatada),
                    Csv(item.Veiculo.Marca),
                    Csv(item.Veiculo.Modelo),
                    Csv(item.Veiculo.Ano),
                    Csv(item.TipoVeiculo),
                    Csv(item.Veiculo.SistemaEletrico),
                    Csv(item.NomeCliente),
                    Csv(item.Veiculo.Quilometragem.ToString()),
                    Csv(FormatarDataCsv(item.Veiculo.RetornoRecomendadoEm)),
                    Csv(FormatarDataCsv(item.Veiculo.GarantiaValidaAte)),
                    Csv(FormatarDataCsv(item.Veiculo.ProximaRevisaoEm)),
                    Csv(item.AlertaPrincipal),
                    Csv(item.AlertaResumo),
                    Csv(string.IsNullOrWhiteSpace(item.Veiculo.ImagemUrl) ? "Nao" : "Sim"),
                    Csv(string.IsNullOrWhiteSpace(item.Veiculo.DocumentoImagemUrl) ? "Nao" : "Sim"),
                    Csv(item.QuantidadeOs.ToString()),
                    Csv(item.QuantidadeAgendamentos.ToString()),
                    Csv(item.QuantidadeOrcamentos.ToString()))));

                File.WriteAllLines(path, linhas);

                App.Audit.RegistrarAcaoCritica(
                    "Veiculos",
                    "ExportarVeiculos",
                    "Veiculo",
                    "Exportacao",
                    $"Arquivo={path}; Registros={itens.Count}; Usuario={App.Session.UserName}");

                if (App.IsAutomatedTestMode)
                {
                    App.Logger.LogInfo($"Exportacao de veiculos validada em automacao: {path}");
                    return;
                }

                MessageBox.Show($"Arquivo exportado com sucesso:\n{path}", "Veiculos", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao exportar veiculos: {ex.Message}", "Veiculos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Csv(string? valor)
        {
            var texto = (valor ?? string.Empty).Replace("\"", "\"\"");
            return $"\"{texto}\"";
        }

        private static string FormatarDataCsv(DateTime? data)
        {
            return data?.ToString("dd/MM/yyyy") ?? string.Empty;
        }

        private void VisualizarVeiculo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is VeiculoViewModel veiculo)
            {
                var janela = new VisualizarVeiculoWindow(veiculo.Veiculo, _databaseService);
                WindowOwnerHelper.ConfigureOwner(janela, this);
                janela.ShowDialog();
                CarregarVeiculos();
            }
        }

        private void EditarVeiculo_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("VEICULOS_EDITAR", "Voce nao possui permissao para editar veiculos."))
            {
                return;
            }

            if (sender is Button button && button.Tag is VeiculoViewModel veiculo)
            {
                var janela = new NovoVeiculoWindow(_databaseService, _clientes, veiculo.Veiculo);
                WindowOwnerHelper.ConfigureOwner(janela, this);

                if (janela.ShowDialog() == true)
                {
                    CarregarVeiculos();
                }
            }
        }

        private void ExcluirVeiculo_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("VEICULOS_EXCLUIR", "Voce nao possui permissao para excluir veiculos."))
            {
                return;
            }

            if (sender is Button button && button.Tag is VeiculoViewModel veiculo)
            {
                if (CriticalActionDialogService.ConfirmarExclusao(
                    Window.GetWindow(this),
                    "veiculo",
                    veiculo.PlacaFormatada,
                    $"Resumo: {veiculo.MarcaModelo}\nCliente vinculado: {veiculo.NomeCliente}\nSistema: {veiculo.Veiculo.SistemaEletrico}\nQuilometragem: {veiculo.QuilometragemFormatada}",
                    "O veiculo sera removido do cadastro local e deixara de aparecer na carteira tecnica da oficina."))
                {
                    VeiculoMediaService.DeleteManagedImageIfOwned(veiculo.Veiculo.ImagemUrl);
                    VeiculoMediaService.DeleteManagedImageIfOwned(veiculo.Veiculo.DocumentoImagemUrl);
                    App.Repositories.Clientes.ExcluirVeiculo(veiculo.Veiculo.Id);
                    App.Audit.RegistrarAcaoCritica(
                        "Veiculos",
                        "ExcluirVeiculo",
                        "Veiculo",
                        veiculo.Veiculo.Id.ToString(),
                        $"Placa={veiculo.PlacaFormatada}; Cliente={veiculo.NomeCliente}");
                    CarregarVeiculos();
                }
            }
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            MessageBox.Show(mensagem, "Acesso negado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void VeiculosDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (VeiculosDataGrid.SelectedItem is VeiculoViewModel veiculo)
            {
                var janela = new VisualizarVeiculoWindow(veiculo.Veiculo, _databaseService);
                WindowOwnerHelper.ConfigureOwner(janela, this);
                janela.ShowDialog();
                CarregarVeiculos();
            }
        }
    }

    public class VeiculoViewModel
    {
        public Veiculo Veiculo { get; }
        public string NomeCliente { get; }
        public string TipoVeiculo { get; }
        public bool SemProprietario { get; }
        public bool RetornoProximo { get; }
        public bool GarantiaAtiva { get; }
        public bool TemAlertaTecnico { get; }
        public int QuantidadeOs { get; }
        public int QuantidadeAgendamentos { get; }
        public int QuantidadeOrcamentos { get; }
        public ImageSource? FotoPreview { get; }
        public Visibility FotoPlaceholderVisibility => FotoPreview == null ? Visibility.Visible : Visibility.Collapsed;
        public Brush AlertaBackground { get; }
        public Brush AlertaForeground { get; }
        public string AlertaPrincipal { get; }
        public string AlertaResumo { get; }

        public VeiculoViewModel(
            Veiculo veiculo,
            Dictionary<Guid, Cliente> clientes,
            IEnumerable<OrdemServico> ordens,
            IEnumerable<Agendamento> agendamentos,
            IEnumerable<Orcamento> orcamentos)
        {
            Veiculo = veiculo;
            TipoVeiculo = string.IsNullOrWhiteSpace(veiculo.TipoVeiculo)
                ? VeiculoProfileService.InferirTipoVeiculo(veiculo.Marca)
                : veiculo.TipoVeiculo;
            NomeCliente = veiculo.ClienteId.HasValue && clientes.TryGetValue(veiculo.ClienteId.Value, out var cliente)
                ? cliente.Nome
                : "Sem proprietario";
            SemProprietario = !veiculo.ClienteId.HasValue;

            var ordensVinculadas = ordens
                .Where(os => os.VeiculoId == veiculo.Id ||
                             (!string.IsNullOrWhiteSpace(os.PlacaSnapshot) &&
                              string.Equals(CadastroValidationHelper.NormalizarPlaca(os.PlacaSnapshot), CadastroValidationHelper.NormalizarPlaca(veiculo.Placa), StringComparison.Ordinal)))
                .ToList();
            var ordensIds = ordensVinculadas.Select(os => os.Id).ToHashSet();

            var agendamentosVinculados = agendamentos
                .Where(a => a.VeiculoId == veiculo.Id ||
                            string.Equals(CadastroValidationHelper.NormalizarPlaca(a.VeiculoPlaca), CadastroValidationHelper.NormalizarPlaca(veiculo.Placa), StringComparison.Ordinal))
                .ToList();

            var orcamentosVinculados = orcamentos
                .Where(o =>
                    (veiculo.ClienteId.HasValue && o.ClienteId == veiculo.ClienteId) ||
                    (o.OrdemServicoId.HasValue && ordensIds.Contains(o.OrdemServicoId.Value)))
                .GroupBy(o => o.Id)
                .Select(g => g.First())
                .ToList();

            QuantidadeOs = ordensVinculadas.Count;
            QuantidadeAgendamentos = agendamentosVinculados.Count;
            QuantidadeOrcamentos = orcamentosVinculados.Count;

            RetornoProximo = veiculo.RetornoRecomendadoEm.HasValue &&
                             veiculo.RetornoRecomendadoEm.Value.Date >= DateTime.Today &&
                             veiculo.RetornoRecomendadoEm.Value.Date <= DateTime.Today.AddDays(7);
            GarantiaAtiva = veiculo.GarantiaValidaAte.HasValue && veiculo.GarantiaValidaAte.Value.Date >= DateTime.Today;

            FotoPreview = VeiculoMediaService.TryCreatePreviewSource(veiculo.ImagemUrl);

            (AlertaPrincipal, AlertaResumo, AlertaBackground, AlertaForeground, TemAlertaTecnico) = CriarAlerta(veiculo, QuantidadeOs, QuantidadeAgendamentos);
        }

        public string PlacaFormatada => string.IsNullOrWhiteSpace(Veiculo.Placa) ? "-" : Veiculo.Placa.ToUpperInvariant();
        public string MarcaModelo => $"{Veiculo.Marca} {Veiculo.Modelo}".Trim();
        public string AnoCor => $"Ano {TextoOuTraco(Veiculo.Ano)} | {TextoOuTraco(Veiculo.Cor)}";
        public string SistemaResumo => $"{TextoOuTraco(TipoVeiculo)} | {TextoOuTraco(Veiculo.SistemaEletrico)}";
        public string BateriasResumo => $"Bat. principal: {TextoOuTraco(Veiculo.BateriaPrincipal)} | Auxiliar: {TextoOuTraco(Veiculo.BateriaAuxiliar)}";
        public string MotorResumo => $"Motor {TextoOuTraco(Veiculo.Motor)} | Alternador {TextoOuTraco(Veiculo.Alternador)} | Partida {TextoOuTraco(Veiculo.MotorPartida)}";
        public string QuilometragemFormatada => Veiculo.Quilometragem > 0 ? $"{Veiculo.Quilometragem:N0} km" : "Sem km informado";
        public string VinculosResumo => $"OS {QuantidadeOs} | Agenda {QuantidadeAgendamentos} | Orc {QuantidadeOrcamentos}";

        private static (string principal, string resumo, Brush background, Brush foreground, bool alerta) CriarAlerta(Veiculo veiculo, int quantidadeOs, int quantidadeAgendamentos)
        {
            if (!string.IsNullOrWhiteSpace(veiculo.ProblemaRecorrente))
            {
                return ("Recorrencia", veiculo.ProblemaRecorrente, CriarBrush("#FEE2E2"), CriarBrush("#B91C1C"), true);
            }

            if (veiculo.GarantiaValidaAte.HasValue && veiculo.GarantiaValidaAte.Value.Date < DateTime.Today)
            {
                return ("Garantia", $"Garantia vencida em {veiculo.GarantiaValidaAte.Value:dd/MM/yyyy}.", CriarBrush("#FEF3C7"), CriarBrush("#92400E"), true);
            }

            if (veiculo.RetornoRecomendadoEm.HasValue && veiculo.RetornoRecomendadoEm.Value.Date < DateTime.Today)
            {
                return ("Retorno vencido", $"Contato atrasado desde {veiculo.RetornoRecomendadoEm.Value:dd/MM/yyyy}.", CriarBrush("#FEE2E2"), CriarBrush("#B91C1C"), true);
            }

            if (veiculo.RetornoRecomendadoEm.HasValue && veiculo.RetornoRecomendadoEm.Value.Date <= DateTime.Today.AddDays(7))
            {
                return ("Retorno", $"Contato previsto para {veiculo.RetornoRecomendadoEm.Value:dd/MM/yyyy}.", CriarBrush("#DBEAFE"), CriarBrush("#1D4ED8"), true);
            }

            if (veiculo.ProximaRevisaoEm.HasValue && veiculo.ProximaRevisaoEm.Value.Date < DateTime.Today)
            {
                return ("Revisao vencida", $"Revisao atrasada desde {veiculo.ProximaRevisaoEm.Value:dd/MM/yyyy}.", CriarBrush("#FEE2E2"), CriarBrush("#B91C1C"), true);
            }

            if (veiculo.ProximaRevisaoEm.HasValue && veiculo.ProximaRevisaoEm.Value.Date <= DateTime.Today.AddDays(15))
            {
                return ("Revisao", $"Proxima revisao em {veiculo.ProximaRevisaoEm.Value:dd/MM/yyyy}.", CriarBrush("#FEF3C7"), CriarBrush("#92400E"), true);
            }

            var resumo = quantidadeOs > 0 || quantidadeAgendamentos > 0
                ? $"Historico com {quantidadeOs} OS e {quantidadeAgendamentos} agendamento(s)."
                : "Veiculo monitorado sem alerta imediato.";
            return ("Monitorado", resumo, CriarBrush("#DCFCE7"), CriarBrush("#047857"), false);
        }

        private static string TextoOuTraco(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();
        }

        private static SolidColorBrush CriarBrush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        }
    }
}
