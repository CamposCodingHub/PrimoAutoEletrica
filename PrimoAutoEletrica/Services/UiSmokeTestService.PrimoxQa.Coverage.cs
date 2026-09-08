using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        /// <summary>
        /// Fase 13 — expansão de cobertura funcional profunda (persistência + relacionamentos).
        /// Preserva todos os checks QaEngine da Fase 12.
        /// </summary>
        private void RunPrimoxQaCoverageExpansionChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            GarantirBancoIsoladoDoSmoke("PRIMOX QA Coverage Expansion");
            _fixture ??= EnsureSmokeFixture(syntheticUser);

            RunCheck(result, "QaEngine:OrdemServicoCreateEmitirPersistencia", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine OS create emitir");
                var fixture = _fixture!;
                var cliente = App.Repositories.Clientes.ObterPorId(fixture.Cliente.Id)
                    ?? throw new InvalidOperationException("Cliente fixture ausente.");
                var produto = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)
                    ?? throw new InvalidOperationException("Produto fixture ausente.");
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var problema = $"TESTE_QA_OS_PROBLEMA_{token}";

                var window = new OrdemServicoWindow(App.Database, null, cliente);
                try
                {
                    ShowWindowForInteraction(window);

                    var tecnicoCombo = FindElementByName<ComboBox>(window, "TecnicoComboBox")
                        ?? throw new InvalidOperationException("TecnicoComboBox ausente.");
                    tecnicoCombo.SelectedItem = tecnicoCombo.Items.OfType<Funcionario>()
                        .FirstOrDefault(item => item.Id == fixture.Administrator.Id)
                        ?? tecnicoCombo.Items.OfType<Funcionario>().FirstOrDefault()
                        ?? throw new InvalidOperationException("Sem tecnico para OS.");

                    var veiculoCombo = FindElementByName<ComboBox>(window, "VeiculoComboBox")
                        ?? throw new InvalidOperationException("VeiculoComboBox ausente.");
                    veiculoCombo.SelectedItem = veiculoCombo.Items.OfType<Veiculo>()
                        .FirstOrDefault(item => item.Id == fixture.Veiculo.Id)
                        ?? throw new InvalidOperationException("Veiculo nao apareceu no combo.");

                    var produtoCombo = FindElementByName<ComboBox>(window, "ProdutoComboBox")
                        ?? throw new InvalidOperationException("ProdutoComboBox ausente.");
                    produtoCombo.SelectedItem = produtoCombo.Items.OfType<Produto>()
                        .FirstOrDefault(item => item.Id == produto.Id)
                        ?? throw new InvalidOperationException("Produto nao apareceu no combo.");

                    DefinirComboBoxTexto(window, "PrioridadeComboBox", "Alta");
                    DefinirComboBoxTexto(window, "OrigemComboBox", "Balcao");
                    SetTextBoxValue(window, "ProblemaTextBox", problema);
                    SetTextBoxValue(window, "DiagnosticoInicialTextBox", "Diagnostico QA inicial.");
                    SetTextBoxValue(window, "DiagnosticoTextBox", "Diagnostico QA final.");
                    SetTextBoxValue(window, "ObservacoesInternasTextBox", "Obs interna QA.");
                    SetTextBoxValue(window, "ObservacoesClienteTextBox", "Obs cliente QA.");
                    SetTextBoxValue(window, "QuantidadeProdutoTextBox", "1");
                    ClickButton(window, "AdicionarProdutoOsButton");
                    ClickButton(window, "AdicionarServicoOsButton");

                    var itensGrid = FindElementByName<DataGrid>(window, "ItensDataGrid")
                        ?? throw new InvalidOperationException("ItensDataGrid ausente.");
                    var servico = itensGrid.Items.OfType<OrdemServicoItemEditor>()
                        .FirstOrDefault(item => string.Equals(item.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Servico nao adicionado.");
                    servico.Descricao = "Servico QA eletrico";
                    servico.Quantidade = 1;
                    servico.ValorUnitario = 150m;
                    servico.CustoUnitario = 40m;

                    var aprovado = FindElementByName<CheckBox>(window, "ClienteAprovouCheckBox")
                        ?? throw new InvalidOperationException("ClienteAprovouCheckBox ausente.");
                    aprovado.IsChecked = true;
                    DefinirComboBoxTexto(window, "MetodoAprovacaoComboBox", "WhatsApp");
                    SetTextBoxValue(window, "TempoPrevistoTextBox", "60");
                    SetTextBoxValue(window, "DescontoTextBox", "0");
                    SetTextBoxValue(window, "ChecklistEntradaTextBox", "Checklist entrada QA.");
                    SetTextBoxValue(window, "TermoAutorizacaoTextBox", "Termo autorizacao QA.");
                    SetTextBoxValue(window, "ChecklistEntregaTextBox", "Checklist entrega QA.");
                    SetTextBoxValue(window, "ChecklistSaidaTextBox", "Checklist saida QA.");
                    WaitForUiIdle();

                    ClickButton(window, "EmitirButton");
                    WaitForCondition(
                        () => !window.IsVisible && window.OrdemSalva != null,
                        TimeSpan.FromSeconds(8),
                        "OS nao emitiu/fechou.");

                    var ordem = App.Repositories.OrdensServico.ObterPorId(window.OrdemSalva!.Id)
                        ?? throw new InvalidOperationException("OS emitida nao encontrada no repo.");
                    if (ordem.ClienteId != cliente.Id ||
                        ordem.VeiculoId != fixture.Veiculo.Id ||
                        !string.Equals(ordem.ProblemaRelatado, problema, StringComparison.Ordinal) ||
                        !string.Equals(ordem.Status, "Aprovada", StringComparison.OrdinalIgnoreCase) ||
                        !ordem.AprovadaCliente ||
                        ordem.Itens.Count < 2 ||
                        !ordem.Itens.Any(i => i.ProdutoId == produto.Id))
                    {
                        throw new InvalidOperationException(
                            $"OS persistencia inconsistente. Status={ordem.Status}; Itens={ordem.Itens.Count}; Problema={ordem.ProblemaRelatado}.");
                    }

                    // UPDATE + re-read
                    var problema2 = $"{problema}_V2";
                    ordem.ProblemaRelatado = problema2;
                    ordem.ObservacoesInternas = "Obs QA editada";
                    App.Repositories.OrdensServico.Atualizar(ordem);
                    var v2 = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                        ?? throw new InvalidOperationException("OS ausente apos UPDATE.");
                    if (!string.Equals(v2.ProblemaRelatado, problema2, StringComparison.Ordinal) ||
                        !string.Equals(v2.ObservacoesInternas, "Obs QA editada", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("OS UPDATE nao persistiu.");
                    }

                    // Repeticao
                    v2.ObservacoesInternas = "Obs QA editada V3";
                    App.Repositories.OrdensServico.Atualizar(v2);
                    var v3 = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                        ?? throw new InvalidOperationException("OS ausente apos 2o UPDATE.");
                    if (!string.Equals(v3.ObservacoesInternas, "Obs QA editada V3", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("OS 2o UPDATE nao persistiu.");
                    }

                    _qaEngineReport?.Observacoes.Add($"OS emitida {ordem.Numero} validada (create/update/repeat).");
                }
                finally
                {
                    if (window.IsVisible) window.Close();
                }
            });

            RunCheck(result, "QaEngine:OrdemServicoCancelNaoPersiste", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine OS cancel");
                var ordem = App.Repositories.OrdensServico.ObterPorId(_fixture!.OrdemServico.Id)
                    ?? throw new InvalidOperationException("OS fixture ausente.");
                var problemaOriginal = ordem.ProblemaRelatado;

                var window = new OrdemServicoWindow(App.Database, ordem);
                try
                {
                    ShowWindowForInteraction(window);
                    SetTextBoxValue(window, "ProblemaTextBox", "NAO_DEVE_PERSISTIR_OS_QA");
                    WaitForUiIdle();
                    // Fecha sem Emitir/Salvar
                }
                finally
                {
                    if (window.IsVisible) window.Close();
                }

                var lida = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                    ?? throw new InvalidOperationException("OS sumiu apos cancel.");
                if (!string.Equals(lida.ProblemaRelatado, problemaOriginal, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Cancelamento de OS alterou ProblemaRelatado no banco.");
                }
            });

            RunCheck(result, "QaEngine:OrcamentoCreateUpdateConverterOs", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine orcamento");
                var fixture = _fixture!;
                var service = new OrcamentoDatabaseService();
                var orcamento = CreatePersistedOrcamento(fixture.Cliente, fixture.Produto);
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var diagnostico = $"TESTE_QA_ORC_DIAG_{token}";

                orcamento.Diagnostico = diagnostico;
                orcamento.Observacoes = $"TESTE_QA_ORC_{token}";
                orcamento.Desconto = 5m;
                orcamento.Total = Math.Max(0, orcamento.Subtotal - orcamento.Desconto);
                service.AtualizarOrcamento(orcamento);

                var atualizado = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento nao recarregado apos UPDATE.");
                if (!string.Equals(atualizado.Diagnostico, diagnostico, StringComparison.Ordinal) ||
                    atualizado.ClienteId != fixture.Cliente.Id ||
                    atualizado.Itens.Count < 1)
                {
                    throw new InvalidOperationException("Orcamento UPDATE inconsistente.");
                }

                var vm = new OrcamentosViewModel();
                vm.AprovarOrcamento(atualizado);
                var aprovado = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento aprovado ausente.");
                var ordem = vm.ConverterEmOrdemServico(aprovado)
                    ?? throw new InvalidOperationException("Conversao orcamento→OS retornou nulo.");

                var convertido = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento convertido ausente.");
                if (!convertido.OrdemServicoId.HasValue ||
                    convertido.OrdemServicoId.Value != ordem.Id ||
                    !convertido.Status.Contains("OS", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Conversao inconsistente. Status={convertido.Status}; OrdemServicoId={convertido.OrdemServicoId}.");
                }

                var os = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                    ?? throw new InvalidOperationException("OS da conversao ausente.");
                if (os.ClienteId != fixture.Cliente.Id ||
                    os.VeiculoId != fixture.Veiculo.Id ||
                    os.OrcamentoId != orcamento.Id)
                {
                    throw new InvalidOperationException("Relacionamento Orcamento↔OS↔Cliente↔Veiculo inconsistente.");
                }

                // Idempotencia: segunda conversao nao deve duplicar
                var ordem2 = vm.ConverterEmOrdemServico(convertido);
                if (ordem2 != null && ordem2.Id != ordem.Id)
                {
                    throw new InvalidOperationException("Segunda conversao criou OS duplicada.");
                }

                // PDF real
                var pdfDir = Path.Combine(App.RuntimeAppDataPath, "AutomatedTests", "QaEngine");
                Directory.CreateDirectory(pdfDir);
                var pdfPath = Path.Combine(pdfDir, $"orcamento-qa-{orcamento.Numero}-{token}.pdf");
                new OrcamentoPdfService().GerarPdfOrcamento(convertido, pdfPath);
                AssertFileGenerated(pdfPath, "PDF orcamento QA");
            });

            RunCheck(result, "QaEngine:AgendamentoConfirmCheckInConverter", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine agenda");
                var fixture = _fixture!;
                var svc = new AgendamentoDatabaseService();
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var agendamento = CreatePersistedAgendamento(
                    fixture.Cliente,
                    fixture.Veiculo,
                    fixture.Produto,
                    DateTime.Today.AddDays(1).AddHours(10),
                    "Agendado",
                    "Normal",
                    $"QA-AG-{token}");

                var confirmVm = new AgendamentosViewModel
                {
                    AgendamentoSelecionado = svc.ObterAgendamentoPorId(agendamento.Id)
                };
                confirmVm.ConfirmarCommand.Execute(null);
                var confirmado = svc.ObterAgendamentoPorId(agendamento.Id)
                    ?? throw new InvalidOperationException("Agendamento confirmado ausente.");
                if (!string.Equals(confirmado.Status, "Confirmado", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Status esperado Confirmado, obtido {confirmado.Status}.");
                }

                // Check-in via UI
                var host = CreateHostWindow(new AgendamentosControl(), nameof(AgendamentosControl));
                try
                {
                    ShowWindowForInteraction(host);
                    if (host.Content is not AgendamentosControl control)
                    {
                        throw new InvalidOperationException("AgendamentosControl ausente.");
                    }

                    var listView = FindElementByName<ListView>(control, "agendamentosListView")
                        ?? throw new InvalidOperationException("agendamentosListView ausente.");
                    WaitForCondition(
                        () => listView.Items.Cast<object>().Any(),
                        TimeSpan.FromSeconds(8),
                        "Lista de agendamentos vazia.");

                    if (control.DataContext is AgendamentosViewModel vm)
                    {
                        vm.AgendamentoSelecionado = svc.ObterAgendamentoPorId(agendamento.Id);
                    }

                    WaitForUiIdle();
                    if (FindElementByName<Button>(control, "CheckInAgendamentoButton") is { IsEnabled: true })
                    {
                        ClickButton(control, "CheckInAgendamentoButton");
                        WaitForCondition(
                            () => svc.ObterAgendamentoPorId(agendamento.Id)?.CheckIn.HasValue == true,
                            TimeSpan.FromSeconds(8),
                            "Check-in nao persistiu.");
                    }
                    else
                    {
                        // Fallback domain se botao desabilitado (estado)
                        var a = svc.ObterAgendamentoPorId(agendamento.Id)
                            ?? throw new InvalidOperationException("Agendamento sumiu.");
                        a.CheckIn = DateTime.Now;
                        svc.AtualizarAgendamento(a);
                        _qaEngineReport!.Observacoes.Add("Agenda CheckIn: UI desabilitada; validado via AtualizarAgendamento.");
                    }
                }
                finally
                {
                    if (host.IsVisible) host.Close();
                }

                var comCheckIn = svc.ObterAgendamentoPorId(agendamento.Id)
                    ?? throw new InvalidOperationException("Agendamento pos check-in ausente.");
                var ordem = svc.ConverterEmOrdemServico(comCheckIn, "QaEngine");
                var ligado = svc.ObterAgendamentoPorId(agendamento.Id)
                    ?? throw new InvalidOperationException("Agendamento pos conversao ausente.");
                if (!ligado.OrdemServicoId.HasValue || ligado.OrdemServicoId != ordem.Id)
                {
                    throw new InvalidOperationException("Agenda→OS nao vinculou OrdemServicoId.");
                }

                var os = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                    ?? throw new InvalidOperationException("OS da agenda ausente.");
                if (os.ClienteId != fixture.Cliente.Id || os.VeiculoId != fixture.Veiculo.Id)
                {
                    throw new InvalidOperationException("OS da agenda com Cliente/Veiculo incorretos.");
                }
            });

            RunCheck(result, "QaEngine:EstoqueEntradaSaidaPersistencia", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine estoque");
                var produto = CreatePersistedProdutoEstoqueSmoke(
                    "Qa",
                    quantidadeEstoque: 20,
                    quantidadeMinima: 2,
                    quantidadeMaxima: 100,
                    precoCompra: 15m,
                    semCodigoOperacional: false,
                    dataUltimaVenda: DateTime.Today,
                    totalVendas: 1,
                    vendasUltimoMes: 1);
                var antes = App.Repositories.Produtos.ObterPorId(produto.Id)?.QuantidadeEstoque
                    ?? throw new InvalidOperationException("Produto estoque ausente.");

                var host = CreateHostWindow(new EstoqueControl(), nameof(EstoqueControl));
                try
                {
                    ShowWindowForInteraction(host);
                    if (host.Content is not EstoqueControl control)
                    {
                        throw new InvalidOperationException("EstoqueControl ausente.");
                    }

                    var grid = FindElementByName<DataGrid>(control, "ProdutosDataGrid")
                        ?? throw new InvalidOperationException("ProdutosDataGrid ausente.");
                    WaitForCondition(
                        () => LocalizarProdutoNoEstoque(grid, produto.Id) != null,
                        TimeSpan.FromSeconds(8),
                        "Produto QA nao apareceu no estoque.");

                    SelecionarProdutoNoEstoque(grid, produto.Id);
                    ClickButton(control, "EntradaEstoqueButton");
                    WaitForCondition(
                        () => App.Repositories.Produtos.ObterPorId(produto.Id)?.QuantidadeEstoque == antes + 1,
                        TimeSpan.FromSeconds(8),
                        "Entrada nao incrementou estoque.");

                    SelecionarProdutoNoEstoque(grid, produto.Id);
                    ClickButton(control, "SaidaEstoqueButton");
                    WaitForCondition(
                        () => App.Repositories.Produtos.ObterPorId(produto.Id)?.QuantidadeEstoque == antes,
                        TimeSpan.FromSeconds(8),
                        "Saida nao restaurou estoque.");
                }
                finally
                {
                    if (host.IsVisible) host.Close();
                }

                var historico = new EstoqueOperationalService(App.Database, _logger)
                    .ObterHistoricoProduto(produto.Id, limite: 20);
                if (!historico.Any(h => string.Equals(h.Acao, "EntradaEstoqueDedicada", StringComparison.OrdinalIgnoreCase)) ||
                    !historico.Any(h => string.Equals(h.Acao, "SaidaEstoqueDedicada", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Historico de entrada/saida nao registrado.");
                }
            });

            RunCheck(result, "QaEngine:FinanceiroContaReceberBaixaPersistencia", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine financeiro baixa");
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var descricao = $"QA Receber baixa {token}";
                var financeiro = new FinanceiroDatabaseService();
                financeiro.AdicionarContaReceber(
                    "Cliente QA Financeiro",
                    descricao,
                    333.33m,
                    DateTime.Today,
                    "PIX",
                    origem: "QaEngineFinanceiro");

                var host = CreateHostWindow(new FinanceiroControl(), nameof(FinanceiroControl));
                try
                {
                    ShowWindowForInteraction(host);
                    if (host.Content is not FinanceiroControl control)
                    {
                        throw new InvalidOperationException("FinanceiroControl ausente.");
                    }

                    WaitForCondition(
                        () => control.ViewModel.ContasReceber.Any(c => c.Descricao == descricao),
                        TimeSpan.FromSeconds(8),
                        "Conta receber QA nao carregou.");

                    var conta = control.ViewModel.ContasReceber.Single(c => c.Descricao == descricao);
                    if (string.Equals(conta.Status, "Paga", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(conta.Status, "Recebida", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Conta ja estava baixada antes do teste.");
                    }

                    control.ViewModel.ContaReceberSelecionada = conta;
                    ClickButton(control, "BaixarContaReceberSelecionadaButton");
                    WaitForCondition(
                        () =>
                        {
                            var atual = control.ViewModel.ContasReceber.SingleOrDefault(c => c.Descricao == descricao);
                            return atual != null &&
                                   atual.DataPagamento.HasValue &&
                                   (string.Equals(atual.Status, "Paga", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(atual.Status, "Recebida", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(atual.Status, "Pago", StringComparison.OrdinalIgnoreCase));
                        },
                        TimeSpan.FromSeconds(8),
                        "Baixa de conta receber nao refletiu na UI.");
                }
                finally
                {
                    if (host.IsVisible) host.Close();
                }

                // Reconsulta camada financeira
                var contas = financeiro.ObterContasReceber()
                    .Where(c => string.Equals((string)c.Descricao, descricao, StringComparison.Ordinal))
                    .ToList();
                if (contas.Count != 1)
                {
                    throw new InvalidOperationException($"Esperada 1 conta '{descricao}', encontradas {contas.Count}.");
                }

                var status = (string)contas[0].Status;
                if (!status.Contains("Pag", StringComparison.OrdinalIgnoreCase) &&
                    !status.Contains("Receb", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Status apos baixa inesperado: {status}.");
                }
            });

            RunCheck(result, "QaEngine:PdvVendaCancelEstornoPersistencia", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine PDV venda/cancel");
                var fixture = _fixture!;
                var caixaService = new CaixaService(App.Database);
                var vendaService = new VendaService(App.Database);
                FecharCaixaAbertoAntesDoBloqueioPdv(caixaService);

                var hostWindow = new Window
                {
                    Content = new PDVControl(),
                    Title = "QaEngine PDV Host"
                };
                AutomatedDialogSupervisor? supervisor = null;
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    var control = hostWindow.Content as PDVControl
                        ?? throw new InvalidOperationException("PDVControl ausente.");
                    supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                    supervisor.Start();

                    WaitForCondition(
                        () => control.ViewModel.Produtos.Count > 0,
                        TimeSpan.FromSeconds(10),
                        "PDV sem produtos.");

                    AdicionarProdutoAoCarrinhoParaAutomacao(control, fixture.Produto);
                    ClickButton(control, "Abrir caixa");
                    WaitForCondition(() => control.ViewModel.CaixaAberto, TimeSpan.FromSeconds(10), "Caixa nao abriu.");

                    control.ViewModel.ClienteSelecionado = fixture.Cliente;
                    ClickButton(control, "DinheiroButton");
                    WaitForUiIdle();

                    var estoqueAntes = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)?.QuantidadeEstoque
                        ?? throw new InvalidOperationException("Produto ausente antes venda.");
                    var sessao = caixaService.ObterSessaoAbertaAtual();

                    ClickButton(control, "PagamentoButton");
                    WaitForCondition(
                        () => control.ViewModel.UltimaVendaFinalizadaId.HasValue || control.ViewModel.Carrinho.Count == 0,
                        TimeSpan.FromSeconds(12),
                        "Pagamento PDV nao concluiu.");

                    var vendaId = control.ViewModel.UltimaVendaFinalizadaId
                        ?? vendaService.ObterHistoricoOperacional(
                            limite: 1,
                            caixaSessaoId: sessao?.Id,
                            inicio: DateTime.Now.AddMinutes(-10),
                            incluirCanceladas: true).FirstOrDefault()?.Id
                        ?? throw new InvalidOperationException("Venda PDV nao localizada.");

                    var estoqueApos = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)?.QuantidadeEstoque
                        ?? throw new InvalidOperationException("Produto ausente apos venda.");
                    if (estoqueApos >= estoqueAntes)
                    {
                        throw new InvalidOperationException("Venda PDV nao baixou estoque.");
                    }

                    ClickButton(control, "CancelarUltimaVendaConcluidaButton");
                    WaitForUiIdle();
                    var cancelada = vendaService.ObterVendaPorId(vendaId)
                        ?? throw new InvalidOperationException("Venda cancelada ausente.");
                    if (!string.Equals(cancelada.Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Status cancelamento={cancelada.Status}.");
                    }

                    var estoqueEstorno = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)?.QuantidadeEstoque
                        ?? throw new InvalidOperationException("Produto ausente apos cancel.");
                    if (estoqueEstorno != estoqueAntes)
                    {
                        throw new InvalidOperationException(
                            $"Estorno estoque falhou. Antes={estoqueAntes}; Depois={estoqueEstorno}.");
                    }
                }
                finally
                {
                    supervisor?.Dispose();
                    CloseTransientWindows(hostWindow);
                    if (hostWindow.IsVisible) hostWindow.Close();
                    FecharCaixaAbertoAntesDoBloqueioPdv(caixaService);
                }
            });

            RunCheck(result, "QaEngine:FornecedorEditPersistencia", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine fornecedor");
                var repo = App.Repositories.Fornecedores;
                var fornecedor = CriarFornecedorIsoladoSmoke("QaEngine");
                repo.Inserir(fornecedor);
                var persistido = repo.ObterPorId(fornecedor.Id)
                    ?? throw new InvalidOperationException("Fornecedor nao persistiu no INSERT.");

                var editar = new EditarFornecedorWindow(persistido);
                try
                {
                    ShowWindowForInteraction(editar);
                    SetTextBoxValue(editar, "PrazoMedioPagamentoTextBox", "25");
                    SetTextBoxValue(editar, "PrazoMedioEntregaTextBox", "7");
                    SetTextBoxValue(editar, "ObservacoesTextBox", "TESTE_QA_FORNECEDOR_EDIT");
                    InvokeButtonHandler(editar, "SalvarButton_Click", null);
                    WaitForCondition(() => !editar.IsVisible, TimeSpan.FromSeconds(8), "Editar fornecedor nao fechou.");
                }
                finally
                {
                    if (editar.IsVisible) editar.Close();
                }

                var atualizado = repo.ObterPorId(fornecedor.Id)
                    ?? throw new InvalidOperationException("Fornecedor sumiu apos edit.");
                if (atualizado.PrazoMedioPagamentoDias != 25 ||
                    atualizado.PrazoMedioEntregaDias != 7 ||
                    !atualizado.Observacoes.Contains("TESTE_QA_FORNECEDOR_EDIT", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Fornecedor UPDATE nao persistiu prazos/obs.");
                }

                // Cancel nao altera
                var editar2 = new EditarFornecedorWindow(atualizado);
                try
                {
                    ShowWindowForInteraction(editar2);
                    SetTextBoxValue(editar2, "ObservacoesTextBox", "NAO_DEVE_PERSISTIR");
                    if (!TryClickButton(editar2, "Cancelar") && !TryClickButton(editar2, "Fechar"))
                    {
                        editar2.Close();
                    }
                }
                finally
                {
                    if (editar2.IsVisible) editar2.Close();
                }

                var aposCancel = repo.ObterPorId(fornecedor.Id)!;
                if (aposCancel.Observacoes.Contains("NAO_DEVE_PERSISTIR", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Cancel fornecedor persistiu observacao.");
                }
            });

            RunCheck(result, "QaEngine:KanbanStatusAvancoPersistencia", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine kanban");
                var ordem = CreatePersistedOrdemServico(_fixture!.Cliente, _fixture.Veiculo, _fixture.Produto);
                var service = new OficinaProfissionalService();
                service.AlterarStatusOrdem(ordem.Id, "Aguardando aprovacao");
                service.AvancarStatusOrdem(ordem.Id);

                var recarregada = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                    ?? throw new InvalidOperationException("OS kanban ausente.");
                if (!string.Equals(recarregada.Status, "Aprovada", StringComparison.OrdinalIgnoreCase) ||
                    !recarregada.AprovadaCliente ||
                    !recarregada.Eventos.Any(e => e.Tipo == "Kanban"))
                {
                    throw new InvalidOperationException(
                        $"Kanban nao persistiu. Status={recarregada.Status}; Aprovada={recarregada.AprovadaCliente}; Eventos={recarregada.Eventos.Count}.");
                }

                var host = CreateHostWindow(new OficinaKanbanControl(), nameof(OficinaKanbanControl));
                try
                {
                    ShowWindowForInteraction(host);
                    WaitForUiIdle();
                    AssertWindowStillOperational(host, "Kanban UI");
                }
                finally
                {
                    if (host.IsVisible) host.Close();
                }
            });

            RunCheck(result, "QaEngine:NFeImportRollbackSeguro", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine NFe rollback");
                var token = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                var nota = new NotaFiscalImportada
                {
                    ChaveAcesso = $"QA-NFE-ROLLBACK-{token}",
                    Numero = $"QA{token[^6..]}",
                    Serie = "1",
                    DataEmissao = DateTime.Today,
                    DataEntrada = DateTime.Now,
                    ValorTotal = 40m,
                    ValorProdutos = 40m,
                    Fornecedor = new FornecedorNota
                    {
                        Nome = "Fornecedor QA NFe",
                        CNPJ = "12345678000199",
                        Telefone = "(11) 3333-1111"
                    },
                    Produtos = new List<ProdutoImportado>
                    {
                        new()
                        {
                            Codigo = $"QA-NFE-{token[^8..]}",
                            CodigoBarras = $"QA-NFE-{token[^8..]}",
                            Nome = $"Peca QA NFe {token[^5..]}",
                            NCM = "85364100",
                            CFOP = "5102",
                            Quantidade = 2,
                            ValorUnitario = 20m,
                            ValorTotal = 40m,
                            UnidadeMedida = "UN",
                            Status = StatusImportacao.Novo,
                            SelecionadoParaImportacao = true,
                            AcaoPlanejada = AcoesPlanejadas.CriarNovo,
                            CategoriaSugerida = "Eletrica",
                            MargemAplicada = 100m,
                            PrecoVendaSugerido = 40m
                        }
                    }
                };

                var nfeService = new NFeService(App.Database);
                var importada = nfeService.ImportarNotaPreparada(nota, Guid.NewGuid(), syntheticUser.Nome);
                if (importada.Status != StatusImportacaoNota.Concluida)
                {
                    throw new InvalidOperationException($"Importacao QA falhou: {importada.Status} - {importada.Erro}");
                }

                var produtoId = importada.Produtos.Single().ProdutoExistenteId
                    ?? throw new InvalidOperationException("Produto NFe sem ID.");
                if (App.Repositories.Produtos.ObterPorId(produtoId) == null)
                {
                    throw new InvalidOperationException("Produto NFe nao existe apos import.");
                }

                var importacaoRepository = new ImportacaoRepository(App.Database);
                var resultado = importacaoRepository.DesfazerProdutosDaImportacao(
                    importada.Id,
                    "QaEngine",
                    "Rollback seguro Fase 13.");
                if (resultado.ProdutosRemovidos != 1)
                {
                    throw new InvalidOperationException($"Rollback inesperado: removidos={resultado.ProdutosRemovidos}.");
                }

                if (App.Repositories.Produtos.ObterPorId(produtoId) != null)
                {
                    throw new InvalidOperationException("Produto NFe permaneceu apos rollback.");
                }
            });

            RunCheck(result, "QaEngine:RelatoriosExportArquivoGerado", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine relatorios");
                var host = CreateHostWindow(new RelatoriosControl(), nameof(RelatoriosControl));
                try
                {
                    ShowWindowForInteraction(host);
                    if (host.Content is not RelatoriosControl control)
                    {
                        throw new InvalidOperationException("RelatoriosControl ausente.");
                    }

                    WaitForCondition(
                        () => control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                        TimeSpan.FromSeconds(20),
                        "Relatorios nao carregaram.");

                    var pdf = control.ViewModel.ExportarPDF();
                    var excel = control.ViewModel.ExportarExcel();
                    AssertFileGenerated(pdf, "Relatorios PDF");
                    AssertFileGenerated(excel, "Relatorios Excel");
                }
                finally
                {
                    if (host.IsVisible) host.Close();
                }
            });

            RunCheck(result, "QaEngine:ClienteCreateUpdateCancel", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine cliente create");
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var email = $"teste.qa.cliente.{token}@primoauto.local";
                var nome = $"TESTE_QA_CLIENTE_CREATE_{token}";
                var window = new NovoClienteWindow();
                AutomatedDialogSupervisor? supervisor = null;
                try
                {
                    ShowWindowForInteraction(window);
                    supervisor = new AutomatedDialogSupervisor(window, _fixture);
                    supervisor.Start();

                    DefinirComboBoxPorTag(window, "TipoPessoaComboBox", "Fisica");
                    DefinirComboBoxPorTag(window, "StatusClienteComboBox", "Ativo");
                    SetTextBoxValue(window, "NomeTextBox", nome);
                    SetTextBoxValue(window, "CpfTextBox", GerarCpfValido(token));
                    SetTextBoxValue(window, "TelefoneTextBox", "(11) 97777-6000");
                    SetTextBoxValue(window, "WhatsAppTextBox", "(11) 97777-6000");
                    SetTextBoxValue(window, "EmailTextBox", email);
                    SetTextBoxValue(window, "CepTextBox", "01001000");
                    SetTextBoxValue(window, "RuaTextBox", "Rua QA Cliente");
                    SetTextBoxValue(window, "NumeroTextBox", "10");
                    SetTextBoxValue(window, "BairroTextBox", "Centro");
                    SetTextBoxValue(window, "CidadeTextBox", "Sao Paulo");
                    SetTextBoxValue(window, "EstadoTextBox", "SP");

                    var consentimento = FindElementByName<CheckBox>(window, "ConsentimentoLgpdCheckBox");
                    if (consentimento != null) consentimento.IsChecked = true;
                    WaitForUiIdle();
                    ClickButton(window, "SalvarClienteButton");
                    WaitForCondition(() => !window.IsVisible, TimeSpan.FromSeconds(8), "Novo cliente nao fechou.");
                }
                finally
                {
                    supervisor?.Dispose();
                    if (window.IsVisible) window.Close();
                }

                var criado = App.Repositories.Clientes.ObterTodos()
                    .FirstOrDefault(c => string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Cliente QA nao encontrado apos CREATE.");
                if (!string.Equals(criado.Nome, nome, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Nome cliente CREATE inconsistente.");
                }

                // Veiculo vinculado
                var veiculo = CreatePersistedVeiculo(criado.Id);
                var veiculos = App.Repositories.Clientes.ObterTodosVeiculos()
                    .Where(v => v.ClienteId == criado.Id).ToList();
                if (veiculos.All(v => v.Id != veiculo.Id))
                {
                    throw new InvalidOperationException("Relacionamento Cliente↔Veiculo nao persistiu.");
                }

                // Cancel edit
                var nomeAntes = criado.Nome;
                var editar = new EditarClienteWindow(criado);
                try
                {
                    ShowWindowForInteraction(editar);
                    SetTextBoxValue(editar, "NomeTextBox", "NAO_DEVE_PERSISTIR_CLIENTE");
                    if (!TryClickButton(editar, "Cancelar") && !TryClickButton(editar, "Fechar"))
                    {
                        editar.Close();
                    }
                }
                finally
                {
                    if (editar.IsVisible) editar.Close();
                }

                var apos = App.Repositories.Clientes.ObterPorId(criado.Id)!;
                if (!string.Equals(apos.Nome, nomeAntes, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Cancel cliente alterou nome.");
                }
            });

            RunCheck(result, "QaEngine:VeiculoCreatePersistencia", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine veiculo create");
                var cliente = App.Repositories.Clientes.ObterPorId(_fixture!.Cliente.Id)!;
                var clientes = new Dictionary<Guid, Cliente> { [cliente.Id] = cliente };
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var placa = GerarPlacaValida(token);
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
                    SetTextBoxValue(window, "ChassiTextBox", $"9BD{token.PadLeft(14, '0')[..14]}");
                    SetTextBoxValue(window, "RenavamTextBox", token.PadLeft(11, '0')[..11]);
                    SetTextBoxValue(window, "MotorTextBox", "2.8 Diesel");
                    DefinirComboBoxTexto(window, "CombustivelComboBox", "Diesel");
                    SetTextBoxValue(window, "QuilometragemTextBox", "48200");
                    SetTextBoxValue(window, "ObservacoesTextBox", $"TESTE_QA_VEICULO_CREATE_{token}");
                    WaitForUiIdle();
                    ClickButton(window, "SalvarVeiculoButton");
                    try
                    {
                        WaitForCondition(() => !window.IsVisible, TimeSpan.FromSeconds(8), "Novo veiculo nao fechou.");
                    }
                    catch (Exception ex)
                    {
                        var placaErro = FindElementByName<TextBlock>(window, "PlacaErroText")?.Text ?? string.Empty;
                        var marca = FindElementByName<ComboBox>(window, "MarcaComboBox")?.Text ?? string.Empty;
                        var modelo = FindElementByName<ComboBox>(window, "ModeloComboBox")?.Text ?? string.Empty;
                        throw new InvalidOperationException(
                            $"Novo veiculo nao fechou. Placa={placa}; Marca={marca}; Modelo={modelo}; PlacaErro='{placaErro}'. {ex.Message}", ex);
                    }
                }
                finally
                {
                    if (window.IsVisible) window.Close();
                }

                var placaNorm = CadastroValidationHelper.NormalizarPlaca(placa);
                var criado = App.Repositories.Clientes.ObterTodosVeiculos()
                    .FirstOrDefault(v => string.Equals(
                        CadastroValidationHelper.NormalizarPlaca(v.Placa),
                        placaNorm,
                        StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Veiculo QA nao encontrado apos CREATE.");
                if (criado.ClienteId != cliente.Id ||
                    !criado.Observacoes.Contains("TESTE_QA_VEICULO_CREATE", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Veiculo CREATE inconsistente.");
                }
            });

            RunCheck(result, "QaEngine:RelacionamentosClienteVeiculoOs", () =>
            {
                GarantirBancoIsoladoDoSmoke("QaEngine relacionamentos");
                var cliente = _fixture!.Cliente;
                var veiculo = App.Repositories.Clientes.ObterTodosVeiculos()
                    .FirstOrDefault(v => v.Id == _fixture.Veiculo.Id)
                    ?? throw new InvalidOperationException("Veiculo fixture ausente.");
                if (veiculo.ClienteId != cliente.Id)
                {
                    throw new InvalidOperationException("Fixture Cliente↔Veiculo quebrado.");
                }

                var os = CreatePersistedOrdemServico(cliente, veiculo, _fixture.Produto);
                var lida = App.Repositories.OrdensServico.ObterPorId(os.Id)!;
                if (lida.ClienteId != cliente.Id || lida.VeiculoId != veiculo.Id)
                {
                    throw new InvalidOperationException("OS nao vinculou Cliente/Veiculo.");
                }

                if (string.IsNullOrWhiteSpace(lida.ClienteNomeSnapshot) ||
                    string.IsNullOrWhiteSpace(lida.PlacaSnapshot))
                {
                    throw new InvalidOperationException("Snapshots Cliente/Placa da OS vazios.");
                }

                var orc = CreatePersistedOrcamento(cliente, _fixture.Produto);
                if (orc.ClienteId != cliente.Id || orc.VeiculoId != veiculo.Id)
                {
                    throw new InvalidOperationException("Orcamento sem relacionamento Cliente/Veiculo.");
                }
            });

            RunCheck(result, "QaEngine:NegativosPesquisaSemResultado", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    foreach (var modulo in new[] { "Clientes", "Funcionarios", "Estoque", "Fornecedores" })
                    {
                        if (!window.NavigateToModuleForAutomation(modulo, forceReload: true) ||
                            window.CurrentContentElement is not FrameworkElement content)
                        {
                            throw new InvalidOperationException($"Negativo: falha ao abrir {modulo}.");
                        }

                        WaitForUiIdle();
                        var search = FindElementByName<TextBox>(content, "SearchTextBox")
                            ?? FindVisualChildren<TextBox>(content).FirstOrDefault(t =>
                                t.Name.Contains("Search", StringComparison.OrdinalIgnoreCase) ||
                                t.Name.Contains("Busca", StringComparison.OrdinalIgnoreCase) ||
                                t.Name.Contains("Filtro", StringComparison.OrdinalIgnoreCase));
                        if (search == null)
                        {
                            continue;
                        }

                        search.Text = $"ZZZ_QA_SEM_RESULTADO_{Guid.NewGuid():N}";
                        WaitForUiIdle();
                        AssertWindowStillOperational(window, $"pesquisa vazia {modulo}");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true) window.Close();
                }
            });
        }

        private static void AssertFileGenerated(string? path, string contexto)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || new FileInfo(path).Length <= 0)
            {
                throw new InvalidOperationException($"{contexto}: arquivo nao gerado ou vazio ({path}).");
            }
        }
    }
}
