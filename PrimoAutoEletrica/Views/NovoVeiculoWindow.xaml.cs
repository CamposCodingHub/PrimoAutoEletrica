using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class NovoVeiculoWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly Dictionary<Guid, Cliente> _clientes;
        private readonly Veiculo? _veiculoExistente;
        private readonly Cliente? _clientePreSelecionado;

        private string _fotoPersistidaAtual = string.Empty;
        private string _documentoPersistidoAtual = string.Empty;
        private string _novoArquivoFotoSelecionado = string.Empty;
        private string _novoArquivoDocumentoSelecionado = string.Empty;
        private bool _removerFotoSolicitado;
        private bool _removerDocumentoSolicitado;

        private static readonly Dictionary<string, List<string>> ModelosPorMarca = new()
        {
            ["Fiat"] = new() { "Uno", "Palio", "Siena", "Strada", "Toro", "Mobi", "Argo", "Cronos", "Pulse", "Fastback", "Tipo", "Fiorino" },
            ["Volkswagen"] = new() { "Gol", "Voyage", "Polo", "Virtus", "Nivus", "T-Cross", "Taos", "Jetta", "Saveiro", "Amarok" },
            ["Chevrolet"] = new() { "Onix", "Prisma", "Cobalt", "Spin", "Tracker", "Captiva", "Equinox", "S10", "Montana", "Corsa", "Astra" },
            ["Ford"] = new() { "Ka", "Fiesta", "EcoSport", "Focus", "Fusion", "Ranger", "Edge", "Territory", "Transit" },
            ["Toyota"] = new() { "Corolla", "Etios", "Yaris", "Hilux", "SW4", "RAV4", "Camry", "Prius" },
            ["Honda"] = new() { "Civic", "City", "Fit", "HR-V", "CR-V", "WR-V", "Accord" },
            ["Renault"] = new() { "Clio", "Sandero", "Logan", "Duster", "Kwid", "Captur", "Oroch", "Megane" },
            ["Nissan"] = new() { "March", "Versa", "Kicks", "Sentra", "Frontier", "Leaf" },
            ["Hyundai"] = new() { "HB20", "i30", "Elantra", "Sonata", "Tucson", "Creta", "ix35", "Santa Fe" },
            ["Jeep"] = new() { "Renegade", "Compass", "Cherokee", "Wrangler", "Gladiator" },
            ["Mitsubishi"] = new() { "Lancer", "ASX", "Outlander", "Pajero", "L200" },
            ["Scania"] = new() { "P-Series", "G-Series", "R-Series", "S-Series" },
            ["Volvo"] = new() { "FH", "FM", "VHD", "VM" },
            ["Mercedes-Benz"] = new() { "Actros", "Atego", "Arocs", "Zetros", "Unimog" },
            ["Ford Cargo"] = new() { "1113", "1313", "1517", "1717", "2428", "3133", "4432" },
            ["VW Truck"] = new() { "Delivery", "Constellation", "Meteor", "Worker" },
            ["Iveco"] = new() { "Daily", "Eurocargo", "Stralis", "S-Way" },
            ["MAN"] = new() { "TGL", "TGM", "TGS", "TGX" },
            ["DAF"] = new() { "LF", "CF", "XF" }
        };

        public NovoVeiculoWindow(DatabaseService databaseService, Dictionary<Guid, Cliente> clientes, Veiculo? veiculoExistente = null, Cliente? clientePreSelecionado = null)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _clientes = clientes;
            _veiculoExistente = veiculoExistente;
            _clientePreSelecionado = clientePreSelecionado;

            TipoVeiculoComboBox.SelectionChanged += TipoVeiculoComboBox_SelectionChanged;

            CarregarClientes();
            AtualizarPreviewFoto();
            AtualizarPreviewDocumento();

            if (_clientePreSelecionado != null)
            {
                SelecionarClientePreSelecionado(_clientePreSelecionado.Id);
            }

            if (_veiculoExistente != null)
            {
                CarregarVeiculoExistente();
                TituloText.Text = "Editar Veiculo";
            }
        }

        private void MarcaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MarcaComboBox.SelectedItem is ComboBoxItem item)
            {
                AtualizarModelosParaMarca(item.Content?.ToString());
            }
        }

        private void MarcaComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AtualizarModelosParaMarca(MarcaComboBox.Text?.Trim());
        }

        private void TipoVeiculoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SistemaEletricoComboBox.Text))
            {
                SistemaEletricoComboBox.Text = VeiculoProfileService.InferirSistemaEletrico(TipoVeiculoComboBox.Text);
            }
        }

        private void CarregarClientes()
        {
            ClienteComboBox.Items.Clear();

            var clientesOrdenados = _clientes.Values
                .OrderBy(c => c.Nome)
                .Select(c => new ClienteComboItem
                {
                    Cliente = c,
                    DisplayText = string.IsNullOrWhiteSpace(c.CPF)
                        ? c.Nome
                        : $"{c.Nome} - {c.CPF}"
                })
                .ToList();

            foreach (var item in clientesOrdenados)
            {
                ClienteComboBox.Items.Add(item);
            }
        }

        private void SelecionarClientePreSelecionado(Guid clienteId)
        {
            foreach (ClienteComboItem item in ClienteComboBox.Items)
            {
                if (item.Cliente.Id == clienteId)
                {
                    ClienteComboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void AtualizarModelosParaMarca(string? marca)
        {
            ModeloComboBox.Items.Clear();

            if (string.IsNullOrWhiteSpace(marca))
            {
                return;
            }

            if (ModelosPorMarca.TryGetValue(marca, out var modelos))
            {
                foreach (var modelo in modelos)
                {
                    ModeloComboBox.Items.Add(modelo);
                }
            }

            if (string.IsNullOrWhiteSpace(TipoVeiculoComboBox.Text))
            {
                TipoVeiculoComboBox.Text = VeiculoProfileService.InferirTipoVeiculo(marca);
            }

            if (string.IsNullOrWhiteSpace(SistemaEletricoComboBox.Text))
            {
                SistemaEletricoComboBox.Text = VeiculoProfileService.InferirSistemaEletrico(TipoVeiculoComboBox.Text);
            }
        }

        private void CarregarVeiculoExistente()
        {
            if (_veiculoExistente == null)
            {
                return;
            }

            MarcaComboBox.Text = _veiculoExistente.Marca;
            AtualizarModelosParaMarca(_veiculoExistente.Marca);
            ModeloComboBox.Text = _veiculoExistente.Modelo;
            AnoTextBox.Text = _veiculoExistente.Ano;
            CorTextBox.Text = _veiculoExistente.Cor;
            PlacaTextBox.Text = _veiculoExistente.Placa;
            ChassiTextBox.Text = _veiculoExistente.Chassi;
            RenavamTextBox.Text = _veiculoExistente.Renavam;
            TipoVeiculoComboBox.Text = _veiculoExistente.TipoVeiculo;
            SistemaEletricoComboBox.Text = _veiculoExistente.SistemaEletrico;
            MotorTextBox.Text = _veiculoExistente.Motor;
            CombustivelComboBox.Text = _veiculoExistente.Combustivel;
            BateriaPrincipalTextBox.Text = _veiculoExistente.BateriaPrincipal;
            BateriaAuxiliarTextBox.Text = _veiculoExistente.BateriaAuxiliar;
            BateriaInstaladaTextBox.Text = _veiculoExistente.BateriaInstalada;
            BateriaMarcaTextBox.Text = _veiculoExistente.BateriaMarca;
            BateriaAmperagemTextBox.Text = _veiculoExistente.BateriaAmperagem;
            BateriaInstalacaoDatePicker.SelectedDate = _veiculoExistente.BateriaDataInstalacao?.Date;
            AlternadorTextBox.Text = _veiculoExistente.Alternador;
            MotorPartidaTextBox.Text = _veiculoExistente.MotorPartida;
            TensaoRepousoTextBox.Text = _veiculoExistente.TesteTensaoRepouso;
            TensaoPartidaTextBox.Text = _veiculoExistente.TesteTensaoPartida;
            CargaAlternadorTextBox.Text = _veiculoExistente.TesteCargaAlternador;
            CorrenteFugaTextBox.Text = _veiculoExistente.CorrenteFuga;
            EstadoAterramentosTextBox.Text = _veiculoExistente.EstadoAterramentos;
            ChicotesReparadosTextBox.Text = _veiculoExistente.ChicotesReparados;
            FusiveisSubstituidosTextBox.Text = _veiculoExistente.FusiveisSubstituidos;
            RelesSubstituidosTextBox.Text = _veiculoExistente.RelesSubstituidos;
            LampadasSubstituidasTextBox.Text = _veiculoExistente.LampadasSubstituidas;
            AcessoriosInstaladosTextBox.Text = _veiculoExistente.AcessoriosInstalados;
            ObservacoesTecnicasEletricasTextBox.Text = _veiculoExistente.ObservacoesTecnicasEletricas;
            FotosTecnicasTextBox.Text = _veiculoExistente.FotosTecnicas;
            QuilometragemTextBox.Text = _veiculoExistente.Quilometragem.ToString();
            HistoricoTecnicoTextBox.Text = _veiculoExistente.HistoricoTecnico;
            ObservacoesRecorrentesTextBox.Text = _veiculoExistente.ObservacoesEletricasRecorrentes;
            ProblemaRecorrenteTextBox.Text = _veiculoExistente.ProblemaRecorrente;
            ObservacaoTecnicoTextBox.Text = _veiculoExistente.ObservacaoImportanteTecnico;
            RetornoDatePicker.SelectedDate = _veiculoExistente.RetornoRecomendadoEm?.Date;
            GarantiaDatePicker.SelectedDate = _veiculoExistente.GarantiaValidaAte?.Date;
            RevisaoDatePicker.SelectedDate = _veiculoExistente.ProximaRevisaoEm?.Date;
            ObservacoesTextBox.Text = _veiculoExistente.Observacoes;

            _fotoPersistidaAtual = _veiculoExistente.ImagemUrl;
            _documentoPersistidoAtual = _veiculoExistente.DocumentoImagemUrl;
            AtualizarPreviewFoto();
            AtualizarPreviewDocumento();

            if (_veiculoExistente.ClienteId.HasValue)
            {
                SelecionarClientePreSelecionado(_veiculoExistente.ClienteId.Value);
            }
        }

        private void AtualizarPreviewFoto()
        {
            var origem = !string.IsNullOrWhiteSpace(_novoArquivoFotoSelecionado)
                ? _novoArquivoFotoSelecionado
                : (_removerFotoSolicitado ? string.Empty : _fotoPersistidaAtual);

            var preview = VeiculoMediaService.TryCreatePreviewSource(origem);
            FotoPreviewImage.Source = preview;
            FotoPreviewImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            FotoPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;

            FotoStatusTextBlock.Text = preview == null
                ? "Adicione uma imagem frontal ou lateral para facilitar a triagem."
                : Path.GetFileName(origem);
        }

        private void AtualizarPreviewDocumento()
        {
            var origem = !string.IsNullOrWhiteSpace(_novoArquivoDocumentoSelecionado)
                ? _novoArquivoDocumentoSelecionado
                : (_removerDocumentoSolicitado ? string.Empty : _documentoPersistidoAtual);

            var preview = VeiculoMediaService.TryCreatePreviewSource(origem);
            DocumentoPreviewImage.Source = preview;
            DocumentoPreviewImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            DocumentoPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;

            DocumentoStatusTextBlock.Text = preview == null
                ? "Anexe CRLV ou documento visual para atendimento."
                : Path.GetFileName(origem);
        }

        private void SelecionarFotoButton_Click(object sender, RoutedEventArgs e)
        {
            SelecionarImagemVeiculo(tipoDocumento: false);
        }

        private void RemoverFotoButton_Click(object sender, RoutedEventArgs e)
        {
            _novoArquivoFotoSelecionado = string.Empty;
            _removerFotoSolicitado = true;
            AtualizarPreviewFoto();
        }

        private void SelecionarDocumentoButton_Click(object sender, RoutedEventArgs e)
        {
            SelecionarImagemVeiculo(tipoDocumento: true);
        }

        private void RemoverDocumentoButton_Click(object sender, RoutedEventArgs e)
        {
            _novoArquivoDocumentoSelecionado = string.Empty;
            _removerDocumentoSolicitado = true;
            AtualizarPreviewDocumento();
        }

        private void SelecionarImagemVeiculo(bool tipoDocumento)
        {
            if (App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo($"Selecao de {(tipoDocumento ? "documento" : "foto")} do veiculo ignorada em automacao.");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = VeiculoMediaService.SupportedImageFilter,
                CheckFileExists = true,
                Multiselect = false,
                Title = tipoDocumento ? "Selecionar documento do veiculo" : "Selecionar foto do veiculo"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            if (!VeiculoMediaService.IsSupportedImageFile(dialog.FileName))
            {
                WindowInteractionHelper.ShowMessage(
                    "Selecione uma imagem valida (.jpg, .jpeg, .png ou .webp).",
                    "Arquivo invalido",
                    MessageBoxImage.Warning,
                    "Veiculos");
                return;
            }

            if (tipoDocumento)
            {
                _novoArquivoDocumentoSelecionado = dialog.FileName;
                _removerDocumentoSolicitado = false;
                AtualizarPreviewDocumento();
            }
            else
            {
                _novoArquivoFotoSelecionado = dialog.FileName;
                _removerFotoSolicitado = false;
                AtualizarPreviewFoto();
            }
        }

        public void CarregarMidiasParaAutomacao(string? caminhoFoto, string? caminhoDocumento)
        {
            if (!App.IsAutomatedTestMode)
            {
                throw new InvalidOperationException("Carga automatizada de midias de veiculo disponivel apenas em modo de teste.");
            }

            if (!string.IsNullOrWhiteSpace(caminhoFoto))
            {
                if (!File.Exists(caminhoFoto) || !VeiculoMediaService.IsSupportedImageFile(caminhoFoto))
                {
                    throw new InvalidOperationException("Foto automatizada do veiculo nao existe ou usa extensao nao suportada.");
                }

                _novoArquivoFotoSelecionado = caminhoFoto;
                _removerFotoSolicitado = false;
            }

            if (!string.IsNullOrWhiteSpace(caminhoDocumento))
            {
                if (!File.Exists(caminhoDocumento) || !VeiculoMediaService.IsSupportedImageFile(caminhoDocumento))
                {
                    throw new InvalidOperationException("Documento automatizado do veiculo nao existe ou usa extensao nao suportada.");
                }

                _novoArquivoDocumentoSelecionado = caminhoDocumento;
                _removerDocumentoSolicitado = false;
            }

            AtualizarPreviewFoto();
            AtualizarPreviewDocumento();
        }

        private void PlacaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var placa = CadastroValidationHelper.NormalizarPlaca(PlacaTextBox.Text);

            if (string.IsNullOrWhiteSpace(placa) || placa.Length < 3)
            {
                PlacaErroText.Visibility = Visibility.Collapsed;
                return;
            }

            var veiculos = App.Repositories.Clientes.ObterTodosVeiculos();
            var placaExiste = veiculos.Any(v =>
                CadastroValidationHelper.NormalizarPlaca(v.Placa) == placa &&
                (_veiculoExistente == null || v.Id != _veiculoExistente.Id));

            PlacaErroText.Visibility = placaExiste ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            WindowInteractionHelper.CloseWithDialogResult(this, false, "Veiculos");
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            WindowInteractionHelper.CloseWithDialogResult(this, false, "Veiculos");
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos())
            {
                return;
            }

            try
            {
                var veiculo = _veiculoExistente ?? new Veiculo();
                veiculo.Id = veiculo.Id == Guid.Empty ? Guid.NewGuid() : veiculo.Id;

                veiculo.Marca = MarcaComboBox.Text?.Trim() ?? string.Empty;
                veiculo.Modelo = ModeloComboBox.Text?.Trim() ?? string.Empty;
                veiculo.Ano = AnoTextBox.Text?.Trim() ?? string.Empty;
                veiculo.Cor = CorTextBox.Text?.Trim() ?? string.Empty;
                veiculo.Placa = CadastroValidationHelper.NormalizarPlaca(PlacaTextBox.Text);
                veiculo.Chassi = ChassiTextBox.Text?.Trim().ToUpperInvariant() ?? string.Empty;
                veiculo.Renavam = RenavamTextBox.Text?.Trim() ?? string.Empty;
                veiculo.TipoVeiculo = string.IsNullOrWhiteSpace(TipoVeiculoComboBox.Text)
                    ? VeiculoProfileService.InferirTipoVeiculo(veiculo.Marca)
                    : TipoVeiculoComboBox.Text.Trim();
                veiculo.SistemaEletrico = string.IsNullOrWhiteSpace(SistemaEletricoComboBox.Text)
                    ? VeiculoProfileService.InferirSistemaEletrico(veiculo.TipoVeiculo)
                    : SistemaEletricoComboBox.Text.Trim();
                veiculo.Motor = MotorTextBox.Text?.Trim() ?? string.Empty;
                veiculo.Combustivel = CombustivelComboBox.Text?.Trim() ?? string.Empty;
                veiculo.BateriaPrincipal = BateriaPrincipalTextBox.Text?.Trim() ?? string.Empty;
                veiculo.BateriaAuxiliar = BateriaAuxiliarTextBox.Text?.Trim() ?? string.Empty;
                veiculo.BateriaInstalada = BateriaInstaladaTextBox.Text?.Trim() ?? string.Empty;
                veiculo.BateriaMarca = BateriaMarcaTextBox.Text?.Trim() ?? string.Empty;
                veiculo.BateriaAmperagem = BateriaAmperagemTextBox.Text?.Trim() ?? string.Empty;
                veiculo.BateriaDataInstalacao = BateriaInstalacaoDatePicker.SelectedDate?.Date;
                veiculo.Alternador = AlternadorTextBox.Text?.Trim() ?? string.Empty;
                veiculo.MotorPartida = MotorPartidaTextBox.Text?.Trim() ?? string.Empty;
                veiculo.TesteTensaoRepouso = TensaoRepousoTextBox.Text?.Trim() ?? string.Empty;
                veiculo.TesteTensaoPartida = TensaoPartidaTextBox.Text?.Trim() ?? string.Empty;
                veiculo.TesteCargaAlternador = CargaAlternadorTextBox.Text?.Trim() ?? string.Empty;
                veiculo.CorrenteFuga = CorrenteFugaTextBox.Text?.Trim() ?? string.Empty;
                veiculo.EstadoAterramentos = EstadoAterramentosTextBox.Text?.Trim() ?? string.Empty;
                veiculo.ChicotesReparados = ChicotesReparadosTextBox.Text?.Trim() ?? string.Empty;
                veiculo.FusiveisSubstituidos = FusiveisSubstituidosTextBox.Text?.Trim() ?? string.Empty;
                veiculo.RelesSubstituidos = RelesSubstituidosTextBox.Text?.Trim() ?? string.Empty;
                veiculo.LampadasSubstituidas = LampadasSubstituidasTextBox.Text?.Trim() ?? string.Empty;
                veiculo.AcessoriosInstalados = AcessoriosInstaladosTextBox.Text?.Trim() ?? string.Empty;
                veiculo.ObservacoesTecnicasEletricas = ObservacoesTecnicasEletricasTextBox.Text?.Trim() ?? string.Empty;
                veiculo.FotosTecnicas = FotosTecnicasTextBox.Text?.Trim() ?? string.Empty;
                veiculo.HistoricoTecnico = HistoricoTecnicoTextBox.Text?.Trim() ?? string.Empty;
                veiculo.ObservacoesEletricasRecorrentes = ObservacoesRecorrentesTextBox.Text?.Trim() ?? string.Empty;
                veiculo.ProblemaRecorrente = ProblemaRecorrenteTextBox.Text?.Trim() ?? string.Empty;
                veiculo.ObservacaoImportanteTecnico = ObservacaoTecnicoTextBox.Text?.Trim() ?? string.Empty;
                veiculo.RetornoRecomendadoEm = RetornoDatePicker.SelectedDate?.Date;
                veiculo.GarantiaValidaAte = GarantiaDatePicker.SelectedDate?.Date;
                veiculo.ProximaRevisaoEm = RevisaoDatePicker.SelectedDate?.Date;
                veiculo.Observacoes = ObservacoesTextBox.Text?.Trim() ?? string.Empty;

                veiculo.Quilometragem = int.TryParse(QuilometragemTextBox.Text, out var quilometragem)
                    ? quilometragem
                    : 0;

                if (_clientePreSelecionado != null)
                {
                    veiculo.ClienteId = _clientePreSelecionado.Id;
                }
                else if (ClienteComboBox.SelectedItem is ClienteComboItem item)
                {
                    veiculo.ClienteId = item.Cliente.Id;
                }
                else
                {
                    // Não deveria chegar aqui devido à validação, mas por segurança:
                    WindowInteractionHelper.ShowMessage("Cliente nao selecionado. Operacao cancelada.", "Erro de validacao", MessageBoxImage.Error, "Veiculos");
                    return;
                }

                PersistirMidias(veiculo);
                App.Repositories.Clientes.SalvarVeiculo(veiculo);

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Veiculos");
            }
            catch (InvalidOperationException ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro de validacao: {ex.Message}",
                    "Validacao",
                    MessageBoxImage.Warning,
                    "Veiculos");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao salvar veiculo: {ex.Message}",
                    "Erro",
                    MessageBoxImage.Error,
                    "Veiculos",
                    ex);
            }
        }

        private void PersistirMidias(Veiculo veiculo)
        {
            var descricao = VeiculoProfileService.MontarDescricao(veiculo.Marca, veiculo.Modelo, veiculo.Placa);

            if (_removerFotoSolicitado)
            {
                VeiculoMediaService.DeleteManagedImageIfOwned(_fotoPersistidaAtual);
                _fotoPersistidaAtual = string.Empty;
                veiculo.ImagemUrl = string.Empty;
            }
            else
            {
                veiculo.ImagemUrl = _fotoPersistidaAtual;
            }

            if (!string.IsNullOrWhiteSpace(_novoArquivoFotoSelecionado))
            {
                var novoCaminho = VeiculoMediaService.PersistSelectedImage(_novoArquivoFotoSelecionado, veiculo.Id, descricao, "foto");
                if (!string.IsNullOrWhiteSpace(_fotoPersistidaAtual) &&
                    !string.Equals(_fotoPersistidaAtual, novoCaminho, StringComparison.OrdinalIgnoreCase))
                {
                    VeiculoMediaService.DeleteManagedImageIfOwned(_fotoPersistidaAtual);
                }

                veiculo.ImagemUrl = novoCaminho;
            }

            if (_removerDocumentoSolicitado)
            {
                VeiculoMediaService.DeleteManagedImageIfOwned(_documentoPersistidoAtual);
                _documentoPersistidoAtual = string.Empty;
                veiculo.DocumentoImagemUrl = string.Empty;
            }
            else
            {
                veiculo.DocumentoImagemUrl = _documentoPersistidoAtual;
            }

            if (!string.IsNullOrWhiteSpace(_novoArquivoDocumentoSelecionado))
            {
                var novoDocumento = VeiculoMediaService.PersistSelectedImage(_novoArquivoDocumentoSelecionado, veiculo.Id, descricao, "documento");
                if (!string.IsNullOrWhiteSpace(_documentoPersistidoAtual) &&
                    !string.Equals(_documentoPersistidoAtual, novoDocumento, StringComparison.OrdinalIgnoreCase))
                {
                    VeiculoMediaService.DeleteManagedImageIfOwned(_documentoPersistidoAtual);
                }

                veiculo.DocumentoImagemUrl = novoDocumento;
            }
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(MarcaComboBox.Text))
            {
                WindowInteractionHelper.ShowMessage("Informe a marca do veiculo.", "Campo obrigatorio", MessageBoxImage.Warning, "Veiculos");
                MarcaComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(ModeloComboBox.Text))
            {
                WindowInteractionHelper.ShowMessage("Informe o modelo do veiculo.", "Campo obrigatorio", MessageBoxImage.Warning, "Veiculos");
                ModeloComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PlacaTextBox.Text))
            {
                WindowInteractionHelper.ShowMessage("Informe a placa do veiculo.", "Campo obrigatorio", MessageBoxImage.Warning, "Veiculos");
                PlacaTextBox.Focus();
                return false;
            }

            var erroPlaca = CadastroValidationHelper.ValidarPlaca(PlacaTextBox.Text, obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroPlaca))
            {
                WindowInteractionHelper.ShowMessage(erroPlaca, "Placa invalida", MessageBoxImage.Warning, "Veiculos");
                PlacaTextBox.Focus();
                return false;
            }

            if (PlacaErroText.Visibility == Visibility.Visible)
            {
                WindowInteractionHelper.ShowMessage("Esta placa ja esta cadastrada no sistema.", "Placa duplicada", MessageBoxImage.Warning, "Veiculos");
                PlacaTextBox.Focus();
                return false;
            }

            if (_clientePreSelecionado == null && ClienteComboBox.SelectedItem is not ClienteComboItem)
            {
                WindowInteractionHelper.ShowMessage("Selecione o cliente proprietario do veiculo.", "Campo obrigatorio", MessageBoxImage.Warning, "Veiculos");
                ClienteComboBox.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(QuilometragemTextBox.Text) &&
                (!int.TryParse(QuilometragemTextBox.Text, out var quilometragem) || quilometragem < 0))
            {
                WindowInteractionHelper.ShowMessage("Informe uma quilometragem valida.", "Valor invalido", MessageBoxImage.Warning, "Veiculos");
                QuilometragemTextBox.Focus();
                return false;
            }

            return true;
        }

        private sealed class ClienteComboItem
        {
            public Cliente Cliente { get; set; } = null!;
            public string DisplayText { get; set; } = string.Empty;
        }
    }
}
