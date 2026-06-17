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
        // Checks de ordens de servico, midias, checklist e financeiro.

        private void RunOrdensServicoMidiasChecklistFinanceiroChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "OrdensServico:CadastroCompletoPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("cadastro completo de ordem de servico pela tela");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var cliente = App.Repositories.Clientes.ObterPorId(fixture.Cliente.Id)
                    ?? throw new InvalidOperationException("Cliente sintetico nao encontrado para cadastro completo de OS.");
                var produto = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)
                    ?? throw new InvalidOperationException("Produto sintetico nao encontrado para cadastro completo de OS.");
                var fotoAntes = CriarImagemPngSmoke("os-cadastro-antes");
                var fotoDepois = CriarImagemPngSmoke("os-cadastro-depois");
                var assinatura = CriarImagemPngSmoke("os-cadastro-assinatura");
                var window = new OrdemServicoWindow(App.Database, null, cliente);

                try
                {
                    ShowWindowForInteraction(window);

                    var tecnicoCombo = FindElementByName<ComboBox>(window, "TecnicoComboBox")
                        ?? throw new InvalidOperationException("TecnicoComboBox nao foi localizado.");
                    tecnicoCombo.SelectedItem = tecnicoCombo.Items
                        .OfType<Funcionario>()
                        .FirstOrDefault(item => item.Id == fixture.Administrator.Id)
                        ?? tecnicoCombo.Items.OfType<Funcionario>().FirstOrDefault()
                        ?? throw new InvalidOperationException("Nenhum tecnico disponivel para OS completa.");

                    var veiculoCombo = FindElementByName<ComboBox>(window, "VeiculoComboBox")
                        ?? throw new InvalidOperationException("VeiculoComboBox nao foi localizado.");
                    veiculoCombo.SelectedItem = veiculoCombo.Items
                        .OfType<Veiculo>()
                        .FirstOrDefault(item => item.Id == fixture.Veiculo.Id)
                        ?? throw new InvalidOperationException("Veiculo sintetico nao apareceu no combo da OS.");

                    var produtoCombo = FindElementByName<ComboBox>(window, "ProdutoComboBox")
                        ?? throw new InvalidOperationException("ProdutoComboBox nao foi localizado.");
                    produtoCombo.SelectedItem = produtoCombo.Items
                        .OfType<Produto>()
                        .FirstOrDefault(item => item.Id == produto.Id)
                        ?? throw new InvalidOperationException("Produto sintetico nao apareceu no combo da OS.");

                    DefinirComboBoxTexto(window, "PrioridadeComboBox", "Alta");
                    DefinirComboBoxTexto(window, "OrigemComboBox", "Balcao");
                    SetTextBoxValue(window, "ProblemaTextBox", "Cliente relata falha intermitente de partida e queda de tensao.");
                    SetTextBoxValue(window, "DiagnosticoInicialTextBox", "Teste inicial identificou baixa carga e oxidacao no aterramento.");
                    SetTextBoxValue(window, "DiagnosticoTextBox", "Diagnostico final: limpeza do aterramento e substituicao preventiva do rele auxiliar.");
                    SetTextBoxValue(window, "ObservacoesInternasTextBox", "Usar EPI e conferir torque dos terminais antes da entrega.");
                    SetTextBoxValue(window, "ObservacoesClienteTextBox", "Cliente orientado sobre revisao eletrica em 90 dias.");
                    SetTextBoxValue(window, "QuantidadeProdutoTextBox", "1");
                    ClickButton(window, "AdicionarProdutoOsButton");
                    ClickButton(window, "AdicionarServicoOsButton");

                    var itensGrid = FindElementByName<DataGrid>(window, "ItensDataGrid")
                        ?? throw new InvalidOperationException("ItensDataGrid nao foi localizada na OS.");
                    var servico = itensGrid.Items
                        .OfType<OrdemServicoItemEditor>()
                        .FirstOrDefault(item => string.Equals(item.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Item de servico nao foi adicionado na OS.");
                    servico.Descricao = "Diagnostico eletrico completo";
                    servico.Quantidade = 1;
                    servico.ValorUnitario = 180m;
                    servico.CustoUnitario = 40m;
                    servico.Observacoes = "Servico cadastrado pela tela no smoke.";
                    WaitForUiIdle();

                    var aprovado = FindElementByName<CheckBox>(window, "ClienteAprovouCheckBox")
                        ?? throw new InvalidOperationException("ClienteAprovouCheckBox nao foi localizado.");
                    aprovado.IsChecked = true;
                    DefinirComboBoxTexto(window, "MetodoAprovacaoComboBox", "WhatsApp");
                    SetTextBoxValue(window, "TempoPrevistoTextBox", "90");
                    SetTextBoxValue(window, "TempoRealTextBox", "75");
                    SetTextBoxValue(window, "DescontoTextBox", "5,00");
                    SetTextBoxValue(window, "ChecklistEntradaTextBox", "Entrada: bateria, alternador, luzes e conectores conferidos.");
                    SetTextBoxValue(window, "TermoAutorizacaoTextBox", "Cliente autorizou diagnostico, testes eletricos e execucao dos servicos aprovados nesta OS.");
                    SetTextBoxValue(window, "ChecklistEntregaTextBox", "Entrega: partida, carga e orientacao ao cliente conferidas.");
                    SetTextBoxValue(window, "ChecklistSaidaTextBox", "Saida: luzes, carga final e torque dos terminais validados.");
                    SetTextBoxValue(window, "GarantiaObservacoesTextBox", "Garantia smoke de 90 dias para servico eletrico.");

                    var previsao = FindElementByName<DatePicker>(window, "DataPrevisaoDatePicker")
                        ?? throw new InvalidOperationException("DataPrevisaoDatePicker nao foi localizado.");
                    var garantia = FindElementByName<DatePicker>(window, "GarantiaValidaAteDatePicker")
                        ?? throw new InvalidOperationException("GarantiaValidaAteDatePicker nao foi localizado.");
                    previsao.SelectedDate = DateTime.Today.AddDays(2);
                    garantia.SelectedDate = DateTime.Today.AddDays(90);
                    WaitForUiIdle();

                    window.CarregarMidiasParaAutomacao(new[] { fotoAntes }, new[] { fotoDepois }, assinatura);

                    ClickButton(window, "EmitirButton");
                    WaitForCondition(
                        () => !window.IsVisible && window.OrdemSalva != null,
                        TimeSpan.FromSeconds(5),
                        "A janela de OS nao fechou apos emitir a ordem completa.");

                    var ordem = App.Repositories.OrdensServico.ObterPorId(window.OrdemSalva!.Id)
                        ?? throw new InvalidOperationException("OS completa emitida pela tela nao foi localizada.");
                    if (ordem.ClienteId != cliente.Id ||
                        ordem.VeiculoId != fixture.Veiculo.Id ||
                        !string.Equals(ordem.Status, "Aprovada", StringComparison.OrdinalIgnoreCase) ||
                        !ordem.AprovadaCliente ||
                        !string.Equals(ordem.MetodoAprovacao, "WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                        !ordem.GarantiaValidaAte.HasValue ||
                        !ordem.DataPrevisao.HasValue ||
                        ordem.TempoPrevistoMinutos != 90 ||
                        ordem.TempoRealMinutos != 75 ||
                        ordem.Itens.Count < 2 ||
                        !ordem.Itens.Any(item => item.ProdutoId == produto.Id && string.Equals(item.Tipo, "Peca", StringComparison.OrdinalIgnoreCase)) ||
                        !ordem.Itens.Any(item => string.Equals(item.Descricao, "Diagnostico eletrico completo", StringComparison.OrdinalIgnoreCase)) ||
                        string.IsNullOrWhiteSpace(ordem.DiagnosticoInicial) ||
                        string.IsNullOrWhiteSpace(ordem.DiagnosticoFinal) ||
                        string.IsNullOrWhiteSpace(ordem.TermoAutorizacao) ||
                        string.Equals(ordem.ChecklistEntrada, ordem.ChecklistSaida, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("OS completa emitida pela tela ficou inconsistente apos persistencia.");
                    }

                    ValidarMidiaOrdemServico(ordem.FotosAntes, "foto antes da OS completa");
                    ValidarMidiaOrdemServico(ordem.FotosDepois, "foto depois da OS completa");
                    if (OrdemServicoMediaService.TryCreatePreviewSource(ordem.AssinaturaClienteUrl) == null)
                    {
                        throw new InvalidOperationException("Assinatura da OS completa nao gerou preview valido.");
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

            RunCheck(result, "OrdensServico:MidiasChecklistFinanceiro", () =>
            {
                GarantirBancoIsoladoDoSmoke("entrega e financeiro de ordens de servico");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var ordem = App.Repositories.OrdensServico.ObterPorId(fixture.OrdemServico.Id)
                    ?? throw new InvalidOperationException("OS sintetica nao encontrada para validacao operacional.");
                var produto = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)
                    ?? throw new InvalidOperationException("Produto sintetico da OS nao foi localizado para validar baixa de estoque.");
                var estoqueAntesFinalizacao = produto.QuantidadeEstoque;

                var fotoAntes = OrdemServicoMediaService.PersistSelectedImage(
                    CriarImagemPngSmoke("os-antes"),
                    ordem.Id,
                    ordem.Numero,
                    "antes");
                var fotoDepois = OrdemServicoMediaService.PersistSelectedImage(
                    CriarImagemPngSmoke("os-depois"),
                    ordem.Id,
                    ordem.Numero,
                    "depois");
                var assinatura = OrdemServicoMediaService.PersistSelectedImage(
                    CriarImagemPngSmoke("os-assinatura"),
                    ordem.Id,
                    ordem.Numero,
                    "assinatura");

                ordem.ChecklistEntrada = "Entrada smoke: bateria, alternador e conectores conferidos.";
                ordem.ChecklistEntrega = "Entrega smoke: servico explicado e garantia informada.";
                ordem.ChecklistSaida = "Saida smoke: luzes, partida e carga final validadas.";
                ordem.FotosAntes = OrdemServicoMediaService.SerializePaths(new[] { fotoAntes });
                ordem.FotosDepois = OrdemServicoMediaService.SerializePaths(new[] { fotoDepois });
                ordem.AssinaturaClienteUrl = assinatura;
                ordem.GarantiaObservacoes = "Garantia smoke vinculada a OS entregue.";
                ordem.GarantiaValidaAte = DateTime.Today.AddDays(90);
                ordem.Status = "Finalizada";

                App.Repositories.OrdensServico.Atualizar(ordem);

                var recarregada = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                    ?? throw new InvalidOperationException("OS com midias nao foi recarregada.");

                if (!string.Equals(recarregada.ChecklistSaida, ordem.ChecklistSaida, StringComparison.Ordinal) ||
                    string.Equals(recarregada.ChecklistSaida, recarregada.ChecklistEntrega, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Checklist de saida da OS nao persistiu separado do checklist de entrega.");
                }

                var produtoAposFinalizacao = App.Repositories.Produtos.ObterPorId(produto.Id)
                    ?? throw new InvalidOperationException("Produto da OS desapareceu apos finalizacao.");
                if (produtoAposFinalizacao.QuantidadeEstoque != estoqueAntesFinalizacao - 1)
                {
                    throw new InvalidOperationException(
                        $"OS finalizada nao baixou estoque da peca. Antes={estoqueAntesFinalizacao}; Depois={produtoAposFinalizacao.QuantidadeEstoque}.");
                }

                if (!recarregada.Itens.Any(item =>
                        item.ProdutoId == produto.Id &&
                        string.Equals(item.Tipo, "Peca", StringComparison.OrdinalIgnoreCase) &&
                        item.EstoqueMovimentado))
                {
                    throw new InvalidOperationException("Item de peca da OS finalizada nao foi marcado como estoque movimentado.");
                }

                var historicoEstoque = new EstoqueOperationalService(App.Database, _logger)
                    .ObterHistoricoProduto(produto.Id, limite: 20);
                if (!historicoEstoque.Any(item => string.Equals(item.Acao, "BaixaOrdemServico", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Historico de estoque nao registrou a baixa por OS finalizada.");
                }

                App.Repositories.OrdensServico.Atualizar(recarregada);
                var produtoAposRegravacao = App.Repositories.Produtos.ObterPorId(produto.Id)
                    ?? throw new InvalidOperationException("Produto da OS desapareceu apos regravacao.");
                if (produtoAposRegravacao.QuantidadeEstoque != produtoAposFinalizacao.QuantidadeEstoque)
                {
                    throw new InvalidOperationException("Regravar a OS finalizada baixou estoque novamente.");
                }

                ValidarMidiaOrdemServico(recarregada.FotosAntes, "foto antes da OS");
                ValidarMidiaOrdemServico(recarregada.FotosDepois, "foto depois da OS");

                if (OrdemServicoMediaService.TryCreatePreviewSource(recarregada.AssinaturaClienteUrl) == null)
                {
                    throw new InvalidOperationException("Assinatura da OS nao gerou preview valido.");
                }

                if (!recarregada.GarantiaValidaAte.HasValue || recarregada.GarantiaValidaAte.Value.Date < DateTime.Today)
                {
                    throw new InvalidOperationException("Garantia da OS entregue nao persistiu com validade futura.");
                }

                var hostWindow = CreateHostWindow(new OrdensServicoControl(), nameof(OrdensServicoControl));
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not OrdensServicoControl control)
                    {
                        throw new InvalidOperationException("Host de OrdensServicoControl nao conseguiu carregar a OS sintetica.");
                    }

                    var ordensListBox = FindElementByName<ListBox>(control, "OrdensListBox")
                        ?? throw new InvalidOperationException("OrdensListBox nao foi localizada para validar a entrega.");
                    WaitForCondition(
                        () => SelecionarOrdemNaLista(ordensListBox, ordem.Id),
                        TimeSpan.FromSeconds(5),
                        "A OS sintetica nao apareceu na lista operacional.");

                    ClickButton(control, "AvancarStatusButton");
                    WaitForCondition(
                        () => string.Equals(
                            App.Repositories.OrdensServico.ObterPorId(ordem.Id)?.Status,
                            "Aguardando pagamento",
                            StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "O botao Avancar nao moveu a OS finalizada para aguardando pagamento.");

                    ClickButton(control, "AvancarStatusButton");
                    WaitForCondition(
                        () => string.Equals(
                            App.Repositories.OrdensServico.ObterPorId(ordem.Id)?.Status,
                            "Entregue",
                            StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "O botao Avancar nao entregou a OS pronta.");

                    ClickButton(control, "GerarFinanceiroButton");
                    ClickButton(control, "EnviarClienteButton");
                    ClickButton(control, "ImprimirOsButton");
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }

                recarregada = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                    ?? throw new InvalidOperationException("OS entregue pela tela nao foi recarregada.");
                if (!string.Equals(recarregada.Status, "Entregue", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("OS nao permaneceu entregue apos as acoes da tela.");
                }

                var financeiro = new FinanceiroDatabaseService();
                var contasReceber = financeiro.ObterContasReceber();
                var contaIntegrada = contasReceber.Any(conta =>
                    string.Equals((string)conta.Origem, "OrdemServicoContaReceber", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string)conta.ReferenciaExterna, recarregada.Id.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    (decimal)conta.Valor > 0);

                if (!contaIntegrada)
                {
                    throw new InvalidOperationException("OS entregue nao gerou conta a receber integrada ao financeiro.");
                }

                var editarWindow = new OrdemServicoWindow(App.Database, recarregada);
                try
                {
                    PrepareWindow(editarWindow);
                }
                finally
                {
                    editarWindow.Close();
                }
            });
        }

        private static void ValidarMidiaOrdemServico(string midiasSerializadas, string descricao)
        {
            var caminhos = OrdemServicoMediaService.DeserializePaths(midiasSerializadas);
            if (caminhos.Count != 1 ||
                !File.Exists(caminhos[0]) ||
                OrdemServicoMediaService.TryCreatePreviewSource(caminhos[0]) == null)
            {
                throw new InvalidOperationException($"A {descricao} nao persistiu como imagem valida.");
            }
        }

    }
}
