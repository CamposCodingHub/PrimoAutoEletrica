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
        // Checks de clientes, LGPD, cadastro, anexos e assinatura.

        private void RunClientesLgpdChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Clientes:LGPDAtalhosOperacionais", () =>
            {
                GarantirBancoIsoladoDoSmoke("atalhos operacionais de clientes");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var cliente = App.Repositories.Clientes.ObterPorId(fixture.Cliente.Id)
                    ?? throw new InvalidOperationException("Cliente sintetico nao encontrado para validacao LGPD.");

                cliente.ConsentimentoLGPD = true;
                cliente.DataConsentimentoLGPD = DateTime.Now;
                cliente.OrigemConsentimentoLGPD = "Smoke test";
                cliente.AutorizaContatoWhatsApp = true;
                App.Repositories.Clientes.Atualizar(cliente);

                var recarregado = App.Repositories.Clientes.ObterPorId(cliente.Id)
                    ?? throw new InvalidOperationException("Cliente LGPD nao foi recarregado.");

                if (!recarregado.ConsentimentoLGPD ||
                    !recarregado.AutorizaContatoWhatsApp ||
                    !recarregado.DataConsentimentoLGPD.HasValue ||
                    !string.Equals(recarregado.OrigemConsentimentoLGPD, "Smoke test", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Dados LGPD do cliente nao persistiram corretamente.");
                }

                var hostWindow = CreateHostWindow(new ClientesControl(), nameof(ClientesControl));
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not ClientesControl control)
                    {
                        throw new InvalidOperationException("Host de ClientesControl nao conseguiu carregar os atalhos operacionais.");
                    }

                    var header = FindElementByName<Border>(control, "ClientesModulePageHeader")
                        ?? throw new InvalidOperationException("ModulePageHeader do perfil de relacionamento nao foi encontrado.");
                    if (header.Visibility != Visibility.Visible)
                    {
                        throw new InvalidOperationException("ModulePageHeader de clientes nao esta visivel.");
                    }

                    var contentGrid = FindElementByName<Grid>(control, "ClientesContentGrid")
                        ?? throw new InvalidOperationException("ClientesContentGrid nao foi encontrado.");
                    WaitForCondition(
                        () => contentGrid.Visibility == Visibility.Visible,
                        TimeSpan.FromSeconds(5),
                        "Painel Loaded de clientes nao ficou visivel.");

                    var dataGrid = FindElementByName<DataGrid>(control, "ClientesDataGrid")
                        ?? throw new InvalidOperationException("ClientesDataGrid nao foi localizado para validar os atalhos.");
                    WaitForCondition(
                        () => SelecionarClienteNaGrade(dataGrid, recarregado.Id),
                        TimeSpan.FromSeconds(5),
                        "O cliente sintetico nao apareceu na planilha de clientes.");

                    var inicio = DateTime.Now.AddSeconds(-1);
                    ClickButton(control, "WhatsAppClienteButton");
                    ClickButton(control, "NovaOsClienteButton");
                    ClickButton(control, "NovoOrcamentoClienteButton");

                    foreach (var acao in new[]
                             {
                                 "AbrirWhatsAppCliente",
                                 "AtalhoNovaOsCliente",
                                 "AtalhoNovoOrcamentoCliente"
                             })
                    {
                        if (!ExisteAuditoriaClienteDesde(inicio, recarregado.Id, acao))
                        {
                            throw new InvalidOperationException($"O atalho de cliente nao registrou a auditoria esperada: {acao}.");
                        }
                    }
                }
                finally
                {
                    CloseTransientWindows(hostWindow);
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });
        }


        private void RunClientesCadastroCompletoChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Clientes:CadastroCompletoPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("cadastro completo de cliente pela tela");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica de clientes nao foi preparada.");
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var cnpj = GerarCnpjValido(token);
                var documentoDigits = new string(cnpj.Where(char.IsDigit).ToArray());
                var email = $"cliente.cadastro.{token}@primoauto.local";
                var nome = $"Cliente Cadastro Completo {token}";
                var foto = CriarImagemPngSmoke("cliente-cadastro");
                var window = new NovoClienteWindow();
                AutomatedDialogSupervisor? supervisor = null;

                try
                {
                    ShowWindowForInteraction(window);
                    supervisor = new AutomatedDialogSupervisor(window, fixture);

                    DefinirComboBoxPorTag(window, "TipoPessoaComboBox", "Juridica");
                    DefinirComboBoxPorTag(window, "StatusClienteComboBox", "Ativo");
                    SetTextBoxValue(window, "NomeTextBox", nome);
                    SetTextBoxValue(window, "CpfTextBox", cnpj);
                    SetTextBoxValue(window, "RgTextBox", $"IE-{token}");
                    SetTextBoxValue(window, "TelefoneTextBox", "(11) 98888-1111");
                    SetTextBoxValue(window, "WhatsAppTextBox", "(11) 98888-1111");
                    SetTextBoxValue(window, "EmailTextBox", email);
                    SetTextBoxValue(window, "CepTextBox", "01001000");
                    SetTextBoxValue(window, "RuaTextBox", "Rua Smoke Cliente");
                    SetTextBoxValue(window, "NumeroTextBox", "123");
                    SetTextBoxValue(window, "BairroTextBox", "Centro");
                    SetTextBoxValue(window, "CidadeTextBox", "Sao Paulo");
                    SetTextBoxValue(window, "EstadoTextBox", "SP");
                    window.CarregarFotoParaAutomacao(foto);

                    ClickButton(window, "UploadDocumentoClienteButton");
                    supervisor.Start();
                    ClickButton(window, "AssinaturaClienteButton");
                    WaitForCondition(
                        () => (FindElementByName<TextBlock>(window, "AssinaturaStatusTextBlock")?.Text ?? string.Empty)
                            .Contains("vinculada", StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "A assinatura digital nao foi vinculada ao cadastro de cliente.");

                    var consentimento = FindElementByName<CheckBox>(window, "ConsentimentoLgpdCheckBox")
                        ?? throw new InvalidOperationException("ConsentimentoLgpdCheckBox nao foi localizado.");
                    var autorizaWhatsApp = FindElementByName<CheckBox>(window, "AutorizaWhatsAppCheckBox")
                        ?? throw new InvalidOperationException("AutorizaWhatsAppCheckBox nao foi localizado.");
                    consentimento.IsChecked = true;
                    WaitForUiIdle();
                    autorizaWhatsApp.IsChecked = true;
                    SetTextBoxValue(window, "PontosTextBox", "15");
                    SetTextBoxValue(window, "ObservacoesTextBox", "Cliente completo validado pelo smoke test.");

                    ClickButton(window, "SalvarClienteButton");
                    WaitForCondition(
                        () => !window.IsVisible,
                        TimeSpan.FromSeconds(5),
                        "A janela de novo cliente nao fechou apos salvar.");

                    var cliente = App.Repositories.Clientes.ObterTodos()
                        .FirstOrDefault(item => string.Equals(item.Email, email, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Cliente cadastrado pela tela nao foi localizado no repositorio.");
                    var clienteDocumentoDigits = new string((cliente.CPF ?? string.Empty).Where(char.IsDigit).ToArray());

                    if (!string.Equals(cliente.Nome, nome, StringComparison.Ordinal) ||
                        !string.Equals(cliente.TipoPessoa, "Juridica", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(clienteDocumentoDigits, documentoDigits, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(cliente.RG, $"IE-{token}", StringComparison.OrdinalIgnoreCase) ||
                        !cliente.Ativo ||
                        !cliente.ConsentimentoLGPD ||
                        !cliente.AutorizaContatoWhatsApp ||
                        cliente.DataConsentimentoLGPD == null ||
                        string.IsNullOrWhiteSpace(cliente.CaminhoDocumento) ||
                        !File.Exists(cliente.CaminhoDocumento) ||
                        string.IsNullOrWhiteSpace(cliente.CaminhoAssinatura) ||
                        !File.Exists(cliente.CaminhoAssinatura) ||
                        string.IsNullOrWhiteSpace(cliente.ImagemUrl) ||
                        !File.Exists(cliente.ImagemUrl) ||
                        ClienteMediaService.TryCreatePreviewSource(cliente.ImagemUrl) == null)
                    {
                        throw new InvalidOperationException("Cliente completo cadastrado pela tela ficou inconsistente apos persistencia.");
                    }

                    var editarWindow = new EditarClienteWindow(cliente);
                    try
                    {
                        ShowWindowForInteraction(editarWindow);
                        DefinirComboBoxPorTag(editarWindow, "StatusClienteComboBox", "Inativo");
                        ClickButton(editarWindow, "Salvar alteracoes");
                        WaitForCondition(
                            () => !editarWindow.IsVisible,
                            TimeSpan.FromSeconds(5),
                            "A janela de edicao de cliente nao fechou apos inativar o cadastro.");
                    }
                    finally
                    {
                        if (editarWindow.IsVisible)
                        {
                            editarWindow.Close();
                        }
                    }

                    var clienteInativo = App.Repositories.Clientes.ObterPorId(cliente.Id)
                        ?? throw new InvalidOperationException("Cliente editado nao foi recarregado para validar status.");
                    if (clienteInativo.Ativo ||
                        !string.Equals(clienteInativo.TipoPessoa, "Juridica", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(clienteInativo.RG, $"IE-{token}", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Edicao de status/tipo/RG do cliente nao persistiu corretamente.");
                    }
                }
                finally
                {
                    supervisor?.Dispose();
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                }
            });
        }

        private void RunClientesAnexosAssinaturaChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Clientes:AnexosAssinatura", () =>
            {
                GarantirBancoIsoladoDoSmoke("anexos e assinatura de clientes");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var cliente = App.Repositories.Clientes.ObterPorId(fixture.Cliente.Id)
                    ?? throw new InvalidOperationException("Cliente sintetico nao encontrado para validacao de anexos.");

                var documento = CriarArquivoClienteSmoke(
                    "documento-cliente",
                    $"Documento sintetico do cliente {cliente.Id} gerado pelo smoke test.");
                var assinatura = CriarArquivoClienteSmoke(
                    "assinatura-cliente",
                    $"TERMO DE ACEITE DIGITAL{Environment.NewLine}ClienteId: {cliente.Id}{Environment.NewLine}HashSHA256: smoke");

                cliente.CaminhoDocumento = documento;
                cliente.CaminhoAssinatura = assinatura;
                App.Repositories.Clientes.Atualizar(cliente);

                var recarregado = App.Repositories.Clientes.ObterPorId(cliente.Id)
                    ?? throw new InvalidOperationException("Cliente com anexos nao foi recarregado.");

                if (!File.Exists(recarregado.CaminhoDocumento) || !File.Exists(recarregado.CaminhoAssinatura))
                {
                    throw new InvalidOperationException("Documento ou assinatura do cliente nao ficaram acessiveis apos persistencia.");
                }

                var assinaturaConteudo = File.ReadAllText(recarregado.CaminhoAssinatura, Encoding.UTF8);
                if (!assinaturaConteudo.Contains("HashSHA256:", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Assinatura digital do cliente nao contem hash de rastreabilidade.");
                }

                var visualizarWindow = new VisualizarClienteWindow(recarregado);
                try
                {
                    ShowWindowForInteraction(visualizarWindow);
                    ClickButton(visualizarWindow, "Abrir documento");
                    ClickButton(visualizarWindow, "Abrir assinatura");
                }
                finally
                {
                    visualizarWindow.Close();
                }

                var editarWindow = new EditarClienteWindow(recarregado);
                try
                {
                    ShowWindowForInteraction(editarWindow);
                    ClickButton(editarWindow, "AbrirDocumentoButton");
                    ClickButton(editarWindow, "AbrirAssinaturaButton");
                }
                finally
                {
                    editarWindow.Close();
                }
            });
        }

    }
}
