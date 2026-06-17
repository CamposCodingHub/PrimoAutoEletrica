using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks de veiculos, alertas e midias.

        private void RunVeiculosCadastroCompletoChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Veiculos:CadastroCompletoPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("cadastro completo de veiculo pela tela");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica de veiculos nao foi preparada.");
                var cliente = App.Repositories.Clientes.ObterPorId(fixture.Cliente.Id)
                    ?? throw new InvalidOperationException("Cliente sintetico nao encontrado para cadastrar veiculo completo.");
                var clientes = new Dictionary<Guid, Cliente> { [cliente.Id] = cliente };
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var placaSeed = Math.Abs(Guid.NewGuid().GetHashCode()).ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
                var placa = $"SMK{placaSeed[^3]}A{placaSeed[^2..]}";
                var chassi = $"9BD{token.PadLeft(14, '0')[..14]}";
                var renavam = token.PadLeft(11, '0')[..11];
                var foto = CriarImagemPngSmoke("veiculo-cadastro-foto");
                var documento = CriarImagemPngSmoke("veiculo-cadastro-documento");
                var window = new NovoVeiculoWindow(App.Database, clientes, clientePreSelecionado: cliente);

                try
                {
                    ShowWindowForInteraction(window);

                    DefinirComboBoxTexto(window, "MarcaComboBox", "Toyota");
                    DefinirComboBoxTexto(window, "ModeloComboBox", "Hilux");
                    DefinirComboBoxTexto(window, "TipoVeiculoComboBox", "Utilitario");
                    SetTextBoxValue(window, "AnoTextBox", "2024");
                    SetTextBoxValue(window, "CorTextBox", "Prata");
                    DefinirComboBoxTexto(window, "SistemaEletricoComboBox", "12V");
                    SetTextBoxValue(window, "PlacaTextBox", placa);
                    SetTextBoxValue(window, "ChassiTextBox", chassi);
                    SetTextBoxValue(window, "RenavamTextBox", renavam);
                    SetTextBoxValue(window, "MotorTextBox", "2.8 Diesel");
                    DefinirComboBoxTexto(window, "CombustivelComboBox", "Diesel");
                    SetTextBoxValue(window, "QuilometragemTextBox", "48200");
                    SetTextBoxValue(window, "BateriaPrincipalTextBox", "Moura 95Ah");
                    SetTextBoxValue(window, "BateriaAuxiliarTextBox", "Auxiliar 60Ah");
                    SetTextBoxValue(window, "AlternadorTextBox", "120A revisado");
                    SetTextBoxValue(window, "MotorPartidaTextBox", "Bosch 12V");
                    SetTextBoxValue(window, "HistoricoTecnicoTextBox", "Histórico completo validado pela tela: carga, partida e aterramento.");
                    SetTextBoxValue(window, "ObservacoesRecorrentesTextBox", "Recorrência simulada: queda de tensão em chicote frontal.");
                    SetTextBoxValue(window, "ProblemaRecorrenteTextBox", "Falha intermitente no relé auxiliar.");
                    SetTextBoxValue(window, "ObservacaoTecnicoTextBox", "Conferir oxidação no conector antes do diagnóstico.");
                    SetTextBoxValue(window, "ObservacoesTextBox", "Veículo completo cadastrado pelo smoke test.");

                    var retorno = FindElementByName<DatePicker>(window, "RetornoDatePicker")
                        ?? throw new InvalidOperationException("RetornoDatePicker nao foi localizado.");
                    var garantia = FindElementByName<DatePicker>(window, "GarantiaDatePicker")
                        ?? throw new InvalidOperationException("GarantiaDatePicker nao foi localizado.");
                    var revisao = FindElementByName<DatePicker>(window, "RevisaoDatePicker")
                        ?? throw new InvalidOperationException("RevisaoDatePicker nao foi localizado.");
                    retorno.SelectedDate = DateTime.Today.AddDays(15);
                    garantia.SelectedDate = DateTime.Today.AddDays(90);
                    revisao.SelectedDate = DateTime.Today.AddMonths(6);
                    WaitForUiIdle();

                    window.CarregarMidiasParaAutomacao(foto, documento);

                    ClickButton(window, "SalvarVeiculoButton");
                    try
                    {
                        WaitForCondition(
                            () => !window.IsVisible,
                            TimeSpan.FromSeconds(5),
                            "A janela de novo veiculo nao fechou apos salvar.");
                    }
                    catch (Exception ex)
                    {
                        var placaErro = FindElementByName<TextBlock>(window, "PlacaErroText");
                        var marca = FindElementByName<ComboBox>(window, "MarcaComboBox")?.Text ?? string.Empty;
                        var modelo = FindElementByName<ComboBox>(window, "ModeloComboBox")?.Text ?? string.Empty;
                        var placaTela = FindElementByName<TextBox>(window, "PlacaTextBox")?.Text ?? string.Empty;
                        var km = FindElementByName<TextBox>(window, "QuilometragemTextBox")?.Text ?? string.Empty;
                        throw new InvalidOperationException(
                            $"A janela de novo veiculo nao fechou apos salvar. Diagnostico: Marca='{marca}', Modelo='{modelo}', Placa='{placaTela}', KM='{km}', PlacaErro='{placaErro?.Visibility}'.",
                            ex);
                    }

                    var veiculoCriado = App.Repositories.Clientes.ObterTodosVeiculos()
                        .FirstOrDefault(item => string.Equals(item.Placa, placa, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Veiculo cadastrado pela tela nao foi localizado no repositorio.");

                    if (veiculoCriado.ClienteId != cliente.Id ||
                        !string.Equals(veiculoCriado.Marca, "Toyota", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(veiculoCriado.Modelo, "Hilux", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(veiculoCriado.TipoVeiculo, "Utilitario", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(veiculoCriado.SistemaEletrico, "12V", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(veiculoCriado.Cor, "Prata", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(veiculoCriado.Combustivel, "Diesel", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(veiculoCriado.Chassi, chassi, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(veiculoCriado.Renavam, renavam, StringComparison.OrdinalIgnoreCase) ||
                        !veiculoCriado.ProblemaRecorrente.Contains("auxiliar", StringComparison.OrdinalIgnoreCase) ||
                        !veiculoCriado.HistoricoTecnico.Contains("carga", StringComparison.OrdinalIgnoreCase) ||
                        veiculoCriado.Quilometragem != 48200 ||
                        !veiculoCriado.RetornoRecomendadoEm.HasValue ||
                        !veiculoCriado.GarantiaValidaAte.HasValue ||
                        !veiculoCriado.ProximaRevisaoEm.HasValue ||
                        string.IsNullOrWhiteSpace(veiculoCriado.ImagemUrl) ||
                        !File.Exists(veiculoCriado.ImagemUrl) ||
                        VeiculoMediaService.TryCreatePreviewSource(veiculoCriado.ImagemUrl) == null ||
                        string.IsNullOrWhiteSpace(veiculoCriado.DocumentoImagemUrl) ||
                        !File.Exists(veiculoCriado.DocumentoImagemUrl) ||
                        VeiculoMediaService.TryCreatePreviewSource(veiculoCriado.DocumentoImagemUrl) == null)
                    {
                        throw new InvalidOperationException("Veiculo completo cadastrado pela tela ficou inconsistente apos persistencia.");
                    }
                }
                finally
                {
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                }
            });
        }

        private void RunVeiculosAlertasMidiaChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Veiculos:AlertasMidiaDocumentos", () =>
            {
                GarantirBancoIsoladoDoSmoke("alertas e midias de veiculos");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var veiculo = App.Repositories.Clientes.ObterTodosVeiculos()
                    .FirstOrDefault(v => v.Id == fixture.Veiculo.Id)
                    ?? throw new InvalidOperationException("Veiculo sintetico nao encontrado para validacao de alertas.");

                var origemFoto = CriarImagemPngSmoke("foto-veiculo");
                var origemDocumento = CriarImagemPngSmoke("documento-veiculo");
                veiculo.ImagemUrl = VeiculoMediaService.PersistSelectedImage(origemFoto, veiculo.Id, "Smoke Veiculo", "foto");
                veiculo.DocumentoImagemUrl = VeiculoMediaService.PersistSelectedImage(origemDocumento, veiculo.Id, "Smoke Veiculo", "documento");
                veiculo.TipoVeiculo = "Carro";
                veiculo.SistemaEletrico = "12V";
                veiculo.RetornoRecomendadoEm = DateTime.Today.AddDays(3);
                veiculo.GarantiaValidaAte = DateTime.Today.AddDays(45);
                veiculo.ProximaRevisaoEm = DateTime.Today.AddDays(10);
                App.Repositories.Clientes.SalvarVeiculo(veiculo);

                var recarregado = App.Repositories.Clientes.ObterTodosVeiculos()
                    .FirstOrDefault(v => v.Id == veiculo.Id)
                    ?? throw new InvalidOperationException("Veiculo com midias nao foi recarregado.");

                if (!File.Exists(recarregado.ImagemUrl) || VeiculoMediaService.TryCreatePreviewSource(recarregado.ImagemUrl) == null)
                {
                    throw new InvalidOperationException("Foto do veiculo nao persistiu como imagem valida.");
                }

                if (!File.Exists(recarregado.DocumentoImagemUrl) || VeiculoMediaService.TryCreatePreviewSource(recarregado.DocumentoImagemUrl) == null)
                {
                    throw new InvalidOperationException("Documento do veiculo nao persistiu como imagem valida.");
                }

                var clientes = new Dictionary<Guid, Cliente>
                {
                    [fixture.Cliente.Id] = fixture.Cliente
                };

                var viewModel = new VeiculoViewModel(
                    recarregado,
                    clientes,
                    Array.Empty<OrdemServico>(),
                    Array.Empty<Agendamento>(),
                    Array.Empty<Orcamento>());

                if (!viewModel.RetornoProximo || !viewModel.GarantiaAtiva || !string.Equals(viewModel.AlertaPrincipal, "Retorno", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Alertas de retorno/garantia do veiculo nao foram classificados corretamente.");
                }

                ValidarAlertaVeiculo(
                    new Veiculo
                    {
                        ClienteId = fixture.Cliente.Id,
                        Marca = "Volkswagen",
                        Modelo = "Gol",
                        Placa = "ABC1D23",
                        RetornoRecomendadoEm = DateTime.Today.AddDays(-2),
                        GarantiaValidaAte = DateTime.Today.AddDays(30)
                    },
                    clientes,
                    "Retorno vencido",
                    "atrasado");

                ValidarAlertaVeiculo(
                    new Veiculo
                    {
                        ClienteId = fixture.Cliente.Id,
                        Marca = "Volkswagen",
                        Modelo = "Gol",
                        Placa = "DEF4G56",
                        ProximaRevisaoEm = DateTime.Today.AddDays(-1)
                    },
                    clientes,
                    "Revisao vencida",
                    "atrasada");

                var visualizarWindow = new VisualizarVeiculoWindow(recarregado, App.Database);
                try
                {
                    PrepareWindow(visualizarWindow);
                    foreach (var panelName in new[]
                             {
                                 "ProntuarioEletricoPanel",
                                 "DefeitosDiagnosticosPanel",
                                 "PecasAplicadasPanel",
                                 "HistoricoOSPanel"
                             })
                    {
                        var panel = FindElementByName<StackPanel>(visualizarWindow, panelName)
                            ?? throw new InvalidOperationException($"Painel '{panelName}' nao foi localizado na ficha do veiculo.");
                        if (panel.Children.Count == 0)
                        {
                            throw new InvalidOperationException($"Painel '{panelName}' ficou vazio na ficha do veiculo.");
                        }
                    }
                }
                finally
                {
                    visualizarWindow.Close();
                }

                var editarWindow = new NovoVeiculoWindow(App.Database, clientes, recarregado, fixture.Cliente);
                try
                {
                    PrepareWindow(editarWindow);
                }
                finally
                {
                    editarWindow.Close();
                }
            });

            RunCheck(result, "Veiculos:ExportacaoCsvPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("exportacao CSV de veiculos");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var veiculo = App.Repositories.Clientes.ObterTodosVeiculos()
                    .FirstOrDefault(item => item.Id == fixture.Veiculo.Id)
                    ?? throw new InvalidOperationException("Veiculo sintetico nao encontrado para validar a exportacao.");
                var cliente = App.Repositories.Clientes.ObterPorId(fixture.Cliente.Id)
                    ?? throw new InvalidOperationException("Cliente sintetico nao encontrado para validar a exportacao de veiculos.");
                var hostWindow = CreateHostWindow(new VeiculosControl(), nameof(VeiculosControl));

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not VeiculosControl control)
                    {
                        throw new InvalidOperationException("Host de VeiculosControl nao conseguiu carregar a exportacao.");
                    }

                    var exportDir = Path.Combine(App.RuntimeAppDataPath, "Exports");
                    Directory.CreateDirectory(exportDir);
                    var inicio = DateTime.Now.AddSeconds(-1);

                    ClickButton(control, "ExportarVeiculosButton");

                    FileInfo? arquivo = null;
                    WaitForCondition(
                        () =>
                        {
                            arquivo = new DirectoryInfo(exportDir)
                                .GetFiles("veiculos_*.csv")
                                .Where(file => file.LastWriteTime >= inicio && file.Length > 0)
                                .OrderByDescending(file => file.LastWriteTime)
                                .FirstOrDefault();
                            return arquivo != null;
                        },
                        TimeSpan.FromSeconds(5),
                        "O botao Exportar veiculos nao gerou o CSV esperado.");

                    var conteudo = File.ReadAllText(arquivo!.FullName);
                    const string cabecalho = "Placa,Marca,Modelo,Ano,Tipo,SistemaEletrico,Cliente,Quilometragem,Retorno,Garantia,ProximaRevisao,Alerta,ResumoAlerta,Foto,Documento,OS,Agendamentos,Orcamentos";
                    if (!conteudo.Contains(cabecalho, StringComparison.Ordinal) ||
                        !conteudo.Contains(veiculo.Placa, StringComparison.OrdinalIgnoreCase) ||
                        !conteudo.Contains(cliente.Nome, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("CSV de veiculos nao contem cabecalho, placa e cliente esperados.");
                    }
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });
        }

        private static void ValidarAlertaVeiculo(
            Veiculo veiculo,
            Dictionary<Guid, Cliente> clientes,
            string alertaEsperado,
            string trechoResumoEsperado)
        {
            var viewModel = new VeiculoViewModel(
                veiculo,
                clientes,
                Array.Empty<OrdemServico>(),
                Array.Empty<Agendamento>(),
                Array.Empty<Orcamento>());

            if (!string.Equals(viewModel.AlertaPrincipal, alertaEsperado, StringComparison.OrdinalIgnoreCase) ||
                !viewModel.AlertaResumo.Contains(trechoResumoEsperado, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Alerta esperado '{alertaEsperado}' nao foi identificado para o veiculo {veiculo.Placa}.");
            }
        }

        private static string CriarImagemPngSmoke(string prefixo)
        {
            var pasta = Path.Combine(
                App.RuntimeAppDataPath,
                "AutomatedTests",
                "VeiculosMidia");
            Directory.CreateDirectory(pasta);

            var caminho = Path.Combine(pasta, $"{prefixo}-{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.png");
            var pixels = new byte[] { 0x1A, 0x7A, 0xFF, 0xFF };
            var bitmap = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                4);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = File.Create(caminho);
            encoder.Save(stream);
            return caminho;
        }

    }
}
