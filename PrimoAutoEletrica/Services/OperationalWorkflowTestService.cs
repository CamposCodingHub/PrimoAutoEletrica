using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Executa um roteiro operacional isolado para validar persistencia,
    /// integracoes basicas, auditoria, backup e bloqueio multiusuario.
    /// </summary>
    public sealed class OperationalWorkflowTestService
    {
        private readonly LoggerService _logger;

        public OperationalWorkflowTestService(LoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public WorkflowTestRunResult Run()
        {
            var result = new WorkflowTestRunResult
            {
                DatabasePath = App.Database.DatabasePath,
                RuntimeMode = App.RuntimeModeName,
                RuntimeAppDataPath = App.RuntimeAppDataPath
            };

            var startedAt = DateTime.Now;
            _logger.LogInfo("Iniciando workflow test operacional.");
            App.Audit.RegistrarSistema("WorkflowTest", $"Inicio do workflow test operacional em '{result.DatabasePath}'.");

            var admin = EnsureFuncionario("workflow-admin@primoauto.com", "Workflow Admin", "Administrador");
            var caixa = EnsureFuncionario("workflow-caixa@primoauto.com", "Workflow Caixa", "Caixa");
            App.Session.StartSession(admin);

            try
            {
                Cliente? cliente = null;
                Veiculo? veiculo = null;
                Produto? produto = null;
                Orcamento? orcamento = null;
                OrdemServico? ordem = null;
                Agendamento? agendamento = null;
                OrdemServico? ordemAgendada = null;
                Venda? venda = null;
                Venda? vendaCancelada = null;
                CaixaSessaoOperacional? sessaoCaixa = null;
                string? backupPath = null;

                RunCheck(result, "Ambiente:Isolado", () =>
                {
                    if (!App.IsWorkflowTestMode)
                    {
                        throw new InvalidOperationException("Workflow test executado fora do modo isolado.");
                    }

                    if (!File.Exists(App.Database.DatabasePath))
                    {
                        throw new FileNotFoundException("Banco isolado nao foi criado.", App.Database.DatabasePath);
                    }

                    if (!App.Database.DatabasePath.Contains("AutomatedTests", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Workflow test nao esta usando um banco isolado de automacao.");
                    }
                });

                RunCheck(result, "Usuarios:Seed", () =>
                {
                    var funcionarios = App.Repositories.Funcionarios.ObterTodos(false);
                    if (!funcionarios.Any(f => string.Equals(f.Email, admin.Email, StringComparison.OrdinalIgnoreCase)) ||
                        !funcionarios.Any(f => string.Equals(f.Email, caixa.Email, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("Usuarios sinteticos nao foram persistidos corretamente.");
                    }
                });

                RunCheck(result, "Seguranca:BloquearExclusaoPermissaoEssencial", () =>
                {
                    var permissaoEssencial = App.Database.ObterPermissoes()
                        .FirstOrDefault(permissao => permissao.Essencial)
                        ?? throw new InvalidOperationException("Nenhuma permissao essencial foi encontrada para o teste.");

                    try
                    {
                        App.Database.ExcluirPermissao(permissaoEssencial.Id, App.Session.UserName);
                        throw new InvalidOperationException("O sistema permitiu excluir uma permissao essencial.");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("essenciais", StringComparison.OrdinalIgnoreCase))
                    {
                    }

                    var permissaoPersistida = App.Database.ObterPermissoes()
                        .FirstOrDefault(permissao => permissao.Id == permissaoEssencial.Id);
                    if (permissaoPersistida == null)
                    {
                        throw new InvalidOperationException("A permissao essencial desapareceu apos a tentativa bloqueada de exclusao.");
                    }
                });

                RunCheck(result, "Seguranca:BloquearPerfilDuplicado", () =>
                {
                    var perfilExistente = App.Database.ObterPerfisAcesso()
                        .FirstOrDefault()
                        ?? throw new InvalidOperationException("Nenhum perfil existente foi encontrado para o teste.");
                    var quantidadeAntes = App.Database.ObterPerfisAcesso().Count;
                    var permissaoIds = App.Database.ObterPermissoesDoPerfil(perfilExistente.Id);

                    var duplicado = new PerfilAcesso
                    {
                        Nome = perfilExistente.Nome,
                        Descricao = "Perfil duplicado criado pelo workflow test.",
                        NivelHierarquico = perfilExistente.NivelHierarquico,
                        Ativo = true,
                        DataCriacao = DateTime.Now,
                        CriadoPor = App.Session.UserName,
                        PodeDeletar = true,
                        OrdemExibicao = 999
                    };

                    try
                    {
                        App.Database.CriarPerfilAcesso(duplicado, permissaoIds, App.Session.UserName);
                        throw new InvalidOperationException("O sistema permitiu criar um perfil duplicado.");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("ja esta cadastrado", StringComparison.OrdinalIgnoreCase))
                    {
                    }

                    var quantidadeDepois = App.Database.ObterPerfisAcesso().Count;
                    if (quantidadeDepois != quantidadeAntes)
                    {
                        throw new InvalidOperationException("A tentativa de criar perfil duplicado alterou a quantidade de perfis persistidos.");
                    }
                });

                RunCheck(result, "Seguranca:PermissoesGranularesOperacionais", () =>
                {
                    var vendedorPermissoes = new PermissionService(new Funcionario
                    {
                        Nome = "Workflow Vendedor",
                        PerfilAcesso = "Vendedor",
                        Funcao = "Vendedor",
                        Ativo = true
                    }, _logger, App.Database);
                    var tecnicoPermissoes = new PermissionService(new Funcionario
                    {
                        Nome = "Workflow Tecnico",
                        PerfilAcesso = "Tecnico",
                        Funcao = "Tecnico",
                        Ativo = true
                    }, _logger, App.Database);
                    var estoquistaPermissoes = new PermissionService(new Funcionario
                    {
                        Nome = "Workflow Estoquista",
                        PerfilAcesso = "Estoquista",
                        Funcao = "Estoquista",
                        Ativo = true
                    }, _logger, App.Database);

                    if (!vendedorPermissoes.TemPermissaoCodigo("AGENDAMENTOS_CRIAR") ||
                        !vendedorPermissoes.TemPermissaoCodigo("AGENDAMENTOS_COMPARTILHAR") ||
                        vendedorPermissoes.TemPermissaoCodigo("ESTOQUE_INVENTARIAR"))
                    {
                        throw new InvalidOperationException("A malha de permissoes do perfil Vendedor nao refletiu as acoes granulares esperadas.");
                    }

                    if (!tecnicoPermissoes.TemPermissaoCodigo("AGENDAMENTOS_CHECKIN") ||
                        !tecnicoPermissoes.TemPermissaoCodigo("AGENDAMENTOS_CHECKOUT") ||
                        tecnicoPermissoes.TemPermissaoCodigo("AGENDAMENTOS_COMPARTILHAR"))
                    {
                        throw new InvalidOperationException("A malha de permissoes do perfil Tecnico ficou inconsistente para o fluxo operacional.");
                    }

                    if (!estoquistaPermissoes.TemPermissaoCodigo("ESTOQUE_INVENTARIAR") ||
                        estoquistaPermissoes.TemPermissaoCodigo("PDV_REGISTRAR_VENDA"))
                    {
                        throw new InvalidOperationException("A malha de permissoes do perfil Estoquista nao isolou corretamente estoque e PDV.");
                    }
                });

                RunCheck(result, "Clientes:Criar", () =>
                {
                    cliente = CreateCliente();
                    App.Repositories.Clientes.Inserir(cliente);

                    var persisted = App.Repositories.Clientes.ObterPorId(cliente.Id)
                        ?? throw new InvalidOperationException("Cliente nao localizado apos insercao.");

                    if (!string.Equals(persisted.Nome, cliente.Nome, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Dados do cliente persistido nao conferem.");
                    }
                });

                RunCheck(result, "Veiculos:Criar", () =>
                {
                    if (cliente == null)
                    {
                        throw new InvalidOperationException("Cliente nao disponivel para cadastro do veiculo.");
                    }

                    veiculo = CreateVeiculo(cliente.Id);
                    App.Repositories.Clientes.SalvarVeiculo(veiculo);

                    var persisted = App.Repositories.Clientes.ObterVeiculosPorClienteId(cliente.Id)
                        .FirstOrDefault(item => item.Id == veiculo.Id)
                        ?? throw new InvalidOperationException("Veiculo nao localizado apos persistencia.");

                    if (!string.Equals(persisted.Placa, veiculo.Placa, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Placa do veiculo persistido nao confere.");
                    }
                });

                RunCheck(result, "Estoque:CriarProduto", () =>
                {
                    produto = CreateProduto();
                    App.Repositories.Produtos.Inserir(produto);

                    var persisted = App.Repositories.Produtos.ObterPorId(produto.Id)
                        ?? throw new InvalidOperationException("Produto nao localizado apos insercao.");

                    if (persisted.QuantidadeEstoque != produto.QuantidadeEstoque)
                    {
                        throw new InvalidOperationException("Quantidade inicial do produto nao confere.");
                    }
                });

                RunCheck(result, "Multiusuario:Bloqueio", () =>
                {
                    if (produto == null)
                    {
                        throw new InvalidOperationException("Produto nao disponivel para teste de bloqueio.");
                    }

                    var sessaoAdministrador = new AppSessionService();
                    sessaoAdministrador.StartSession(admin);
                    var sessaoCaixa = new AppSessionService();
                    sessaoCaixa.StartSession(caixa);

                    var lockAdmin = new RegistroBloqueioService(App.Database, sessaoAdministrador);
                    var lockCaixa = new RegistroBloqueioService(App.Database, sessaoCaixa);

                    var primeiro = lockAdmin.TentarBloquear("Produto", produto.Id.ToString(), "Workflow test", TimeSpan.FromMinutes(5));
                    if (!primeiro.Bloqueado)
                    {
                        throw new InvalidOperationException($"Falha ao bloquear registro com a primeira sessao: {primeiro.Mensagem}");
                    }

                    var segundo = lockCaixa.TentarBloquear("Produto", produto.Id.ToString(), "Workflow test", TimeSpan.FromMinutes(5));
                    if (segundo.Bloqueado)
                    {
                        throw new InvalidOperationException("A segunda sessao conseguiu sobrescrever um bloqueio ativo.");
                    }

                    lockAdmin.LiberarBloqueio("Produto", produto.Id.ToString());
                    var terceiro = lockCaixa.TentarBloquear("Produto", produto.Id.ToString(), "Workflow test", TimeSpan.FromMinutes(5));
                    if (!terceiro.Bloqueado)
                    {
                        throw new InvalidOperationException("O bloqueio nao foi liberado corretamente para a segunda sessao.");
                    }

                    lockCaixa.LiberarBloqueio("Produto", produto.Id.ToString());
                });

                RunCheck(result, "Orcamentos:Criar", () =>
                {
                    if (cliente == null || produto == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para criar orcamento.");
                    }

                    var service = new OrcamentoDatabaseService();
                    orcamento = CreateOrcamento(service, cliente, produto);
                    service.AdicionarOrcamento(orcamento);

                    var persisted = service.ObterOrcamentoPorId(orcamento.Id)
                        ?? throw new InvalidOperationException("Orcamento nao localizado apos persistencia.");

                    if (persisted.Itens.Count != 1 || persisted.Total != orcamento.Total)
                    {
                        throw new InvalidOperationException("Orcamento persistido nao manteve itens e total.");
                    }
                });

                RunCheck(result, "Orcamentos:AprovarEPdf", () =>
                {
                    if (cliente == null || orcamento == null)
                    {
                        throw new InvalidOperationException("Orcamento indisponivel para validar aprovacao e PDF.");
                    }

                    var service = new OrcamentoDatabaseService();
                    var clientePersistido = App.Repositories.Clientes.ObterPorId(cliente.Id)
                        ?? throw new InvalidOperationException("Cliente nao localizado antes da aprovacao do orcamento.");
                    var orcamentoPersistido = service.ObterOrcamentoPorId(orcamento.Id)
                        ?? throw new InvalidOperationException("Orcamento nao localizado antes da aprovacao.");
                    orcamentoPersistido.Cliente = clientePersistido;

                    var viewModel = new OrcamentosViewModel
                    {
                        UsuarioLogado = App.Session.UserName
                    };
                    viewModel.AprovarOrcamento(orcamentoPersistido);

                    var aprovado = service.ObterOrcamentoPorId(orcamento.Id)
                        ?? throw new InvalidOperationException("Orcamento nao localizado apos aprovacao.");
                    if (!string.Equals(aprovado.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) ||
                        !aprovado.DataAprovacao.HasValue)
                    {
                        throw new InvalidOperationException("A aprovacao do orcamento nao persistiu status e data corretamente.");
                    }

                    aprovado.Cliente = clientePersistido;
                    var pdfPath = BuildTempFilePath("orcamento-workflow", ".pdf");
                    new OrcamentoPdfService().GerarPdfOrcamento(aprovado, pdfPath);
                    EnsureGeneratedFile(pdfPath, "PDF do orcamento");

                    orcamento = aprovado;
                });

                RunCheck(result, "OS:Criar", () =>
                {
                    if (cliente == null || veiculo == null || produto == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para criar OS.");
                    }

                    ordem = CreateOrdemServico(cliente, veiculo, produto);
                    App.Repositories.OrdensServico.Inserir(ordem);

                    var persisted = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                        ?? throw new InvalidOperationException("OS nao localizada apos persistencia.");

                    if (persisted.Itens.Count < 2)
                    {
                        throw new InvalidOperationException("OS persistida sem os itens esperados.");
                    }
                });

                RunCheck(result, "OS:ChecklistGarantiaPainel", () =>
                {
                    if (ordem == null || produto == null)
                    {
                        throw new InvalidOperationException("OS indisponivel para validar checklist, garantia e painel operacional.");
                    }

                    var persisted = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                        ?? throw new InvalidOperationException("OS nao localizada para validar checklist e garantia.");

                    if (string.IsNullOrWhiteSpace(persisted.ChecklistEntrega) ||
                        string.IsNullOrWhiteSpace(persisted.GarantiaObservacoes) ||
                        !persisted.GarantiaValidaAte.HasValue)
                    {
                        throw new InvalidOperationException("Checklist ou garantia da OS nao foram persistidos corretamente.");
                    }

                    var painel = new OrdemServicoPainelItemViewModel(
                        persisted,
                        new Dictionary<int, Funcionario>(),
                        new Dictionary<Guid, Produto> { [produto.Id] = produto });

                    if (!painel.GarantiaResumo.Contains("Garantia valida ate", StringComparison.OrdinalIgnoreCase) ||
                        !painel.ChecklistEntrega.Contains("conector", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("O painel operacional da OS nao refletiu checklist e garantia.");
                    }
                });

                RunCheck(result, "Agendamento:ConverterEmOS", () =>
                {
                    if (cliente == null || veiculo == null || produto == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para converter agendamento em OS.");
                    }

                    var service = new AgendamentoDatabaseService();
                    agendamento = CreateAgendamento(cliente, veiculo, produto);
                    service.AdicionarAgendamento(agendamento);
                    ordemAgendada = service.ConverterEmOrdemServico(agendamento, App.Session.UserName);

                    var persistedAgendamento = service.ObterAgendamentoPorId(agendamento.Id)
                        ?? throw new InvalidOperationException("Agendamento nao localizado apos conversao em OS.");

                    if (!persistedAgendamento.OrdemServicoId.HasValue ||
                        persistedAgendamento.OrdemServicoId.Value != ordemAgendada.Id ||
                        string.IsNullOrWhiteSpace(persistedAgendamento.NumeroOS))
                    {
                        throw new InvalidOperationException("O vinculo do agendamento com a OS nao foi persistido.");
                    }

                    var persistedOrdem = App.Repositories.OrdensServico.ObterPorId(ordemAgendada.Id)
                        ?? throw new InvalidOperationException("OS criada a partir do agendamento nao foi localizada.");

                    if (persistedOrdem.Itens.Count < 2)
                    {
                        throw new InvalidOperationException("A OS gerada pelo agendamento nao manteve pecas e servicos esperados.");
                    }
                });

                RunCheck(result, "Estoque:ValidarReservaAgendamento", () =>
                {
                    if (agendamento == null || produto == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar reserva de estoque do agendamento.");
                    }

                    var estoqueOperational = new EstoqueOperationalService(App.Database, _logger);
                    var reservaAtiva = estoqueOperational.ObterReservaAtivaProduto(produto.Id);
                    if (reservaAtiva < 1)
                    {
                        throw new InvalidOperationException("A conversao do agendamento em OS nao ativou a reserva operacional esperada.");
                    }

                    var service = new AgendamentoDatabaseService();
                    var persistedAgendamento = service.ObterAgendamentoPorId(agendamento.Id)
                        ?? throw new InvalidOperationException("Agendamento nao localizado ao validar reservas operacionais.");
                    if (!persistedAgendamento.Produtos.Any(item => item.ProdutoId == produto.Id && item.Reservado))
                    {
                        throw new InvalidOperationException("O produto do agendamento nao ficou marcado como reservado.");
                    }
                });

                RunCheck(result, "Caixa:AbrirSessao", () =>
                {
                    App.Session.StartSession(caixa);
                    var caixaService = new CaixaService(App.Database);
                    sessaoCaixa = caixaService.AbrirCaixa(150m, "Abertura sintetica do workflow test.", "WF");

                    if (!sessaoCaixa.Aberto || sessaoCaixa.ValorEsperado != 150m)
                    {
                        throw new InvalidOperationException("A sessao operacional do caixa nao abriu com o saldo esperado.");
                    }
                });

                RunCheck(result, "PDV:RegistrarVenda", () =>
                {
                    if (cliente == null || produto == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para registrar venda.");
                    }

                    venda = CreateVenda(cliente, produto, App.Session.UserName);
                    venda.CaixaSessaoId = sessaoCaixa?.Id;
                    var vendaService = new VendaService(App.Database);
                    vendaService.RegistrarVenda(venda, atualizarEstoque: true);

                    var persisted = vendaService.ObterVendas(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1))
                        .FirstOrDefault(item => item.Id == venda.Id)
                        ?? throw new InvalidOperationException("Venda nao localizada apos persistencia.");

                    if (persisted.Itens.Count != venda.Itens.Count || persisted.Total != venda.Total)
                    {
                        throw new InvalidOperationException("Venda persistida nao manteve estrutura esperada.");
                    }
                });

                RunCheck(result, "PDV:HistoricoOperacional", () =>
                {
                    if (venda == null || sessaoCaixa == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar o historico operacional do PDV.");
                    }

                    var vendaService = new VendaService(App.Database);
                    var historico = vendaService.ObterHistoricoOperacional(
                        limite: 20,
                        caixaSessaoId: sessaoCaixa.Id,
                        inicio: DateTime.Today.AddDays(-1),
                        incluirCanceladas: true);
                    var vendaPersistida = historico.FirstOrDefault(item => item.Id == venda.Id)
                        ?? throw new InvalidOperationException("A venda registrada nao apareceu no historico operacional da sessao.");

                    var comprovanteService = new VendaComprovanteService();
                    var resumo = comprovanteService.CriarResumoOperacional(vendaPersistida);
                    if (!resumo.Contains(vendaPersistida.FormaPagamento, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("O resumo operacional do comprovante nao refletiu a forma de pagamento.");
                    }

                    var documento = comprovanteService.CriarDocumento(vendaPersistida);
                    if (documento.Blocks.Count == 0)
                    {
                        throw new InvalidOperationException("O comprovante gerado ficou vazio.");
                    }
                });

                RunCheck(result, "PDV:DiagnosticoImpressao", () =>
                {
                    var printerDiagnosticsService = new PrinterDiagnosticsService();
                    var snapshot = printerDiagnosticsService.CaptureSnapshot();

                    if (!snapshot.HasInstalledPrinters)
                    {
                        throw new InvalidOperationException(
                            string.IsNullOrWhiteSpace(snapshot.CaptureError)
                                ? "Nenhuma impressora instalada foi detectada para o diagnostico do PDV."
                                : $"Nenhuma impressora instalada foi detectada para o diagnostico do PDV. {snapshot.CaptureError}");
                    }

                    _logger.LogInfo($"Workflow test - diagnostico de impressao do PDV: {snapshot.BuildSummary()}", "PDV");
                    App.Audit.RegistrarSistema(
                        "WorkflowTest:PDVImpressao",
                        snapshot.BuildSummary(),
                        snapshot.HasPhysicalPrinter ? "Info" : "Warning",
                        true);

                    if (!snapshot.Printers.Any(printer => printer.IsDefault))
                    {
                        throw new InvalidOperationException("Nenhuma impressora padrao foi identificada no ambiente do PDV.");
                    }
                });

                RunCheck(result, "PDV:CancelarVendaHistorico", () =>
                {
                    if (cliente == null || produto == null || sessaoCaixa == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar cancelamento historico do PDV.");
                    }

                    var vendaService = new VendaService(App.Database);
                    vendaCancelada = CreateVenda(cliente, produto, App.Session.UserName);
                    vendaCancelada.Itens[0].Quantidade = 1;
                    vendaCancelada.Total = vendaCancelada.Itens[0].PrecoUnitario;
                    vendaCancelada.CaixaSessaoId = sessaoCaixa.Id;
                    vendaService.RegistrarVenda(vendaCancelada, atualizarEstoque: true);
                    vendaService.CancelarVenda(vendaCancelada.Id, "Cancelamento sintetico do workflow test.");

                    var historico = vendaService.ObterHistoricoOperacional(
                        limite: 20,
                        caixaSessaoId: sessaoCaixa.Id,
                        inicio: DateTime.Today.AddDays(-1),
                        incluirCanceladas: true);
                    var canceladaPersistida = historico.FirstOrDefault(item => item.Id == vendaCancelada.Id)
                        ?? throw new InvalidOperationException("A venda cancelada nao apareceu no historico operacional.");

                    if (!string.Equals(canceladaPersistida.Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A venda cancelada nao refletiu o status final no historico operacional.");
                    }
                });

                RunCheck(result, "Caixa:ValidarMovimentacao", () =>
                {
                    if (sessaoCaixa == null || venda == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar o caixa.");
                    }

                    var caixaService = new CaixaService(App.Database);
                    var sessaoPersistida = caixaService.ObterSessaoPorId(sessaoCaixa.Id)
                        ?? throw new InvalidOperationException("Sessao de caixa nao localizada apos a venda.");

                    if (sessaoPersistida.QuantidadeVendas < 1 || sessaoPersistida.TotalVendas < venda.Total)
                    {
                        throw new InvalidOperationException("O caixa nao refletiu a venda registrada.");
                    }

                    var movimentacoes = caixaService.ObterMovimentacoesSessao(sessaoCaixa.Id);
                    if (!movimentacoes.Any(m => m.Tipo == "Abertura") || !movimentacoes.Any(m => m.Tipo == "Venda"))
                    {
                        throw new InvalidOperationException("A trilha de movimentacoes do caixa ficou incompleta.");
                    }
                });

                RunCheck(result, "Estoque:ValidarBaixa", () =>
                {
                    if (produto == null || venda == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar baixa de estoque.");
                    }

                    var persisted = App.Repositories.Produtos.ObterPorId(produto.Id)
                        ?? throw new InvalidOperationException("Produto nao localizado apos venda.");

                    var quantidadeEsperada = produto.QuantidadeEstoque - venda.Itens.Sum(item => item.Quantidade);
                    if (persisted.QuantidadeEstoque != quantidadeEsperada)
                    {
                        throw new InvalidOperationException($"Estoque esperado={quantidadeEsperada}, atual={persisted.QuantidadeEstoque}.");
                    }
                });

                RunCheck(result, "Agendamento:FinalizarOS", () =>
                {
                    if (agendamento == null || ordemAgendada == null || produto == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para finalizar a OS vinculada ao agendamento.");
                    }

                    var service = new AgendamentoDatabaseService();
                    agendamento.CheckIn = DateTime.Now.AddHours(-1);
                    agendamento.CheckOut = DateTime.Now;
                    agendamento.Status = "Finalizado";
                    agendamento.DuracaoReal = TimeSpan.FromHours(1);

                    var finalizada = service.FinalizarOrdemServicoVinculada(agendamento, App.Session.UserName)
                        ?? throw new InvalidOperationException("A finalizacao da OS vinculada nao retornou ordem persistida.");

                    if (!string.Equals(finalizada.Status, "Entregue", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A OS vinculada ao agendamento nao foi entregue ao finalizar o atendimento.");
                    }

                    if (!finalizada.Itens.Any(i => i.ProdutoId == produto.Id && i.EstoqueMovimentado))
                    {
                        throw new InvalidOperationException("A baixa de estoque da OS vinculada nao foi marcada nos itens.");
                    }
                });

                RunCheck(result, "Agendamentos:TimelineEAlertas", () =>
                {
                    if (cliente == null || veiculo == null || produto == null || agendamento == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar timeline e alertas da agenda.");
                    }

                    var service = new AgendamentoDatabaseService();

                    var agendamentoAtrasado = CreateAgendamento(cliente, veiculo, produto);
                    agendamentoAtrasado.Numero = $"AG-WF-LATE-{DateTime.Now:HHmmss}";
                    agendamentoAtrasado.Status = "Confirmado";
                    agendamentoAtrasado.HoraInicio = DateTime.Now.AddHours(-2);
                    agendamentoAtrasado.HoraTermino = DateTime.Now.AddHours(1);
                    agendamentoAtrasado.CheckIn = null;
                    agendamentoAtrasado.CheckOut = null;
                    agendamentoAtrasado.AlertaPronto = false;
                    service.AdicionarAgendamento(agendamentoAtrasado);

                    var agendamentoProlongado = CreateAgendamento(cliente, veiculo, produto);
                    agendamentoProlongado.Numero = $"AG-WF-LONG-{DateTime.Now:HHmmss}";
                    agendamentoProlongado.Status = "Em Andamento";
                    agendamentoProlongado.HoraInicio = DateTime.Now.AddHours(-3);
                    agendamentoProlongado.HoraTermino = DateTime.Now.AddMinutes(-20);
                    agendamentoProlongado.CheckIn = DateTime.Now.AddHours(-2);
                    agendamentoProlongado.CheckOut = null;
                    agendamentoProlongado.AlertaPronto = false;
                    service.AdicionarAgendamento(agendamentoProlongado);

                    var agendamentoPronto = CreateAgendamento(cliente, veiculo, produto);
                    agendamentoPronto.Numero = $"AG-WF-DONE-{DateTime.Now:HHmmss}";
                    agendamentoPronto.Status = "Finalizado";
                    agendamentoPronto.CheckIn = DateTime.Now.AddHours(-1);
                    agendamentoPronto.CheckOut = DateTime.Now.AddMinutes(-10);
                    agendamentoPronto.AlertaPronto = true;
                    service.AdicionarAgendamento(agendamentoPronto);

                    service.AdicionarAgendamentoTimeline(new AgendamentoTimeline
                    {
                        Id = Guid.NewGuid(),
                        DataHora = DateTime.Now.AddMinutes(-30),
                        Usuario = App.Session.UserName,
                        Acao = "Recepcao",
                        Detalhes = "Checklist inicial concluido",
                        TipoAlteracao = "Manual"
                    }, agendamento.Id);

                    service.AdicionarAgendamentoTimeline(new AgendamentoTimeline
                    {
                        Id = Guid.NewGuid(),
                        DataHora = DateTime.Now.AddMinutes(-5),
                        Usuario = App.Session.UserName,
                        Acao = "Aprovacao",
                        Detalhes = "Cliente aprovou a execucao do atendimento",
                        TipoAlteracao = "Manual"
                    }, agendamento.Id);

                    var viewModel = new AgendamentosViewModel
                    {
                        AgendamentoSelecionado = service.ObterAgendamentoPorId(agendamento.Id)
                    };
                    viewModel.CarregarTimeline();
                    viewModel.CarregarAlertas();

                    if (viewModel.Timeline.Count < 2)
                    {
                        throw new InvalidOperationException("A timeline operacional nao carregou os eventos esperados.");
                    }

                    for (var indice = 1; indice < viewModel.Timeline.Count; indice++)
                    {
                        if (viewModel.Timeline[indice - 1].DataHora < viewModel.Timeline[indice].DataHora)
                        {
                            throw new InvalidOperationException("A timeline da agenda nao ficou ordenada do evento mais recente para o mais antigo.");
                        }
                    }

                    if (!viewModel.Timeline.Any(item => string.Equals(item.Acao, "Aprovacao", StringComparison.OrdinalIgnoreCase)) ||
                        !viewModel.Timeline.Any(item => string.Equals(item.Acao, "Recepcao", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("A timeline nao expôs os eventos operacionais esperados da agenda.");
                    }

                    if (!viewModel.Alertas.Any(alerta => string.Equals(alerta.Tipo, "Atraso", StringComparison.OrdinalIgnoreCase)) ||
                        !viewModel.Alertas.Any(alerta => string.Equals(alerta.Tipo, "Execucao prolongada", StringComparison.OrdinalIgnoreCase)) ||
                        !viewModel.Alertas.Any(alerta => alerta.Mensagem.Contains("pronto para entrega", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("Os alertas operacionais da agenda nao cobriram atraso, execucao prolongada e veiculo pronto.");
                    }
                });

                RunCheck(result, "Estoque:ReservaConsumidaNaOS", () =>
                {
                    if (agendamento == null || produto == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar consumo da reserva operacional.");
                    }

                    var estoqueOperational = new EstoqueOperationalService(App.Database, _logger);
                    if (estoqueOperational.ObterReservaAtivaProduto(produto.Id) != 0)
                    {
                        throw new InvalidOperationException("A reserva operacional do produto permaneceu ativa apos a entrega da OS.");
                    }

                    var service = new AgendamentoDatabaseService();
                    var persistedAgendamento = service.ObterAgendamentoPorId(agendamento.Id)
                        ?? throw new InvalidOperationException("Agendamento nao localizado apos a entrega da OS.");
                    if (persistedAgendamento.Produtos.Any(item => item.ProdutoId == produto.Id && item.Reservado))
                    {
                        throw new InvalidOperationException("O produto do agendamento continuou marcado como reservado apos a entrega.");
                    }
                });

                RunCheck(result, "Estoque:ValidarBaixaOS", () =>
                {
                    if (produto == null || venda == null || ordemAgendada == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar a baixa de estoque da OS.");
                    }

                    var persisted = App.Repositories.Produtos.ObterPorId(produto.Id)
                        ?? throw new InvalidOperationException("Produto nao localizado apos finalizar a OS.");

                    var quantidadeVendida = venda.Itens.Sum(item => item.Quantidade);
                    var quantidadeOs = ordemAgendada.Itens
                        .Where(item => item.ProdutoId == produto.Id)
                        .Sum(item => Convert.ToInt32(item.Quantidade, CultureInfo.InvariantCulture));
                    var quantidadeEsperada = produto.QuantidadeEstoque - quantidadeVendida - quantidadeOs;

                    if (persisted.QuantidadeEstoque != quantidadeEsperada)
                    {
                        throw new InvalidOperationException($"Estoque esperado apos OS={quantidadeEsperada}, atual={persisted.QuantidadeEstoque}.");
                    }
                });

                RunCheck(result, "Estoque:InventarioEHistorico", () =>
                {
                    if (produto == null)
                    {
                        throw new InvalidOperationException("Produto indisponivel para validar inventario e historico operacional.");
                    }

                    var estoqueOperational = new EstoqueOperationalService(App.Database, _logger);
                    var produtoPersistido = App.Repositories.Produtos.ObterPorId(produto.Id)
                        ?? throw new InvalidOperationException("Produto nao localizado antes do inventario.");
                    var quantidadeContada = produtoPersistido.QuantidadeEstoque + 1;

                    estoqueOperational.RegistrarInventario(
                        produto.Id,
                        quantidadeContada,
                        "Ajuste sintetico de inventario do workflow test.",
                        App.Session.UserName);

                    var aposInventario = App.Repositories.Produtos.ObterPorId(produto.Id)
                        ?? throw new InvalidOperationException("Produto nao localizado apos o inventario.");
                    if (aposInventario.QuantidadeEstoque != quantidadeContada)
                    {
                        throw new InvalidOperationException("A reconciliacao do inventario nao atualizou o saldo final do produto.");
                    }

                    var historico = estoqueOperational.ObterHistoricoProduto(produto.Id, limite: 20);
                    if (!historico.Any(item => string.Equals(item.Acao, "InventarioProduto", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("O historico operacional nao registrou o inventario do produto.");
                    }

                    if (!historico.Any(item => string.Equals(item.Acao, "ReservaAgendamento", StringComparison.OrdinalIgnoreCase)) ||
                        !historico.Any(item => string.Equals(item.Acao, "BaixaOrdemServico", StringComparison.OrdinalIgnoreCase)) ||
                        !historico.Any(item => string.Equals(item.Acao, "SaidaVendaProduto", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("O historico operacional do estoque nao ficou completo para reserva, venda e OS.");
                    }
                });

                RunCheck(result, "Financeiro:OSIntegrado", () =>
                {
                    if (ordemAgendada == null)
                    {
                        throw new InvalidOperationException("OS vinculada indisponivel para validar integracao financeira.");
                    }

                    var financeiro = new FinanceiroDatabaseService();
                    var referencia = ordemAgendada.Id.ToString();
                    var contas = financeiro.ObterContasReceber()
                        .Where(c =>
                            string.Equals((string)c.Origem, "OrdemServicoContaReceber", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals((string)c.ReferenciaExterna, referencia, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var movimentacoes = financeiro.ObterMovimentacoes()
                        .Where(m =>
                            string.Equals((string)m.Origem, "OrdemServicoMovimentacao", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals((string)m.ReferenciaExterna, referencia, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (contas.Count == 0 || movimentacoes.Count == 0)
                    {
                        throw new InvalidOperationException("A OS entregue nao gerou reflexos financeiros completos.");
                    }
                });

                RunCheck(result, "Financeiro:GerarLancamentos", () =>
                {
                    if (cliente == null || venda == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para gerar financeiro.");
                    }

                    var financeiro = new FinanceiroDatabaseService();
                    var referencia = venda.Id.ToString();
                    var movimentacoesIntegradas = financeiro.ObterMovimentacoes()
                        .Where(m => string.Equals(m.Origem, "PDVMovimentacao", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(m.ReferenciaExterna, referencia, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (movimentacoesIntegradas.Count == 0)
                    {
                        throw new InvalidOperationException("A venda do PDV nao gerou movimentacao financeira integrada.");
                    }

                    financeiro.AdicionarContaReceber(
                        cliente.Nome,
                        $"Venda workflow {referencia[..8]}",
                        venda.Total,
                        DateTime.Today.AddDays(15),
                        "PIX",
                        "Conta gerada automaticamente no workflow test.",
                        origem: "WorkflowTest",
                        referenciaExterna: referencia);

                    financeiro.AdicionarMovimentacao(
                        "Entrada",
                        $"Receita venda workflow {referencia[..8]}",
                        venda.Total,
                        DateTime.Today,
                        "Vendas",
                        "PIX",
                        observacoes: "Movimentacao sintetica do workflow test.",
                        origem: "WorkflowTest",
                        referenciaExterna: referencia);

                    var resumo = financeiro.ObterResumoFinanceiro(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
                    if (resumo.entradas < venda.Total)
                    {
                        throw new InvalidOperationException("Resumo financeiro nao refletiu a movimentacao de entrada.");
                    }
                });

                RunCheck(result, "Financeiro:BaixasEDre", () =>
                {
                    if (cliente == null)
                    {
                        throw new InvalidOperationException("Cliente indisponivel para validar baixas operacionais do financeiro.");
                    }

                    var financeiro = new FinanceiroDatabaseService();
                    var descricaoPagar = $"Fornecedor workflow {DateTime.Now:HHmmss}";
                    var descricaoReceber = $"Servico workflow {DateTime.Now:HHmmss}";
                    const decimal valorPagar = 67m;
                    const decimal valorReceber = 89m;

                    financeiro.AdicionarContaPagar(
                        "Fornecedor Workflow Financeiro",
                        descricaoPagar,
                        valorPagar,
                        DateTime.Today,
                        "Operacional",
                        "Conta sintetica para baixa de contas a pagar.");

                    financeiro.AdicionarContaReceber(
                        cliente.Nome,
                        descricaoReceber,
                        valorReceber,
                        DateTime.Today,
                        "PIX",
                        "Conta sintetica para baixa de contas a receber.",
                        status: "Pendente",
                        origem: "WorkflowTest",
                        referenciaExterna: Guid.NewGuid().ToString("N"));

                    var viewModel = new FinanceiroViewModel();
                    var (inicio, fim) = viewModel.ObterPeriodoFinanceiroAtual();
                    var drePendente = financeiro.ObterDemonstrativoResultado(inicio, fim);

                    viewModel.FiltrarContasPagar("todas");
                    viewModel.FiltrarContasReceber("todas");

                    var contaPagar = viewModel.ContasPagar.FirstOrDefault(conta =>
                        string.Equals(conta.Descricao, descricaoPagar, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Conta a pagar sintetica nao apareceu no workspace financeiro.");
                    var contaReceber = viewModel.ContasReceber.FirstOrDefault(conta =>
                        string.Equals(conta.Descricao, descricaoReceber, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Conta a receber sintetica nao apareceu no workspace financeiro.");

                    viewModel.ContaPagarSelecionada = contaPagar;
                    viewModel.ContaReceberSelecionada = contaReceber;
                    viewModel.RegistrarPagamentoContaPagar(contaPagar, "Transferencia");
                    viewModel.RegistrarRecebimentoContaReceber(contaReceber, "PIX");
                    viewModel.AtualizarDashboard();

                    var contaPagarLiquidada = financeiro.ObterContasPagar()
                        .FirstOrDefault(conta =>
                            string.Equals((string)conta.Descricao, descricaoPagar, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Conta a pagar sintetica nao foi localizada apos a baixa.");
                    var contaReceberLiquidada = financeiro.ObterContasReceber()
                        .FirstOrDefault(conta =>
                            string.Equals((string)conta.Descricao, descricaoReceber, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Conta a receber sintetica nao foi localizada apos o recebimento.");

                    if (!string.Equals((string)contaPagarLiquidada.Status, "Paga", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals((string)contaReceberLiquidada.Status, "Pago", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("As baixas operacionais nao atualizaram o status final das contas.");
                    }

                    var dreLiquidado = financeiro.ObterDemonstrativoResultado(inicio, fim);
                    if (dreLiquidado.DespesasConfirmadas < drePendente.DespesasConfirmadas + valorPagar ||
                        dreLiquidado.ReceitasConfirmadas < drePendente.ReceitasConfirmadas + valorReceber)
                    {
                        throw new InvalidOperationException("O DRE nao refletiu as baixas operacionais de contas.");
                    }

                    if (viewModel.DemonstrativoResultado.ContasPagarPendentes > drePendente.ContasPagarPendentes ||
                        viewModel.DemonstrativoResultado.ContasReceberPendentes > drePendente.ContasReceberPendentes)
                    {
                        throw new InvalidOperationException("O workspace financeiro nao recalculou os pendentes apos as baixas.");
                    }
                });

                RunCheck(result, "Orcamentos:ConverterEmVendaFinanceiro", () =>
                {
                    if (cliente == null || produto == null || orcamento == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar a conversao financeira do orcamento em venda.");
                    }

                    var service = new OrcamentoDatabaseService();
                    var financeiro = new FinanceiroDatabaseService();
                    var clientePersistido = App.Repositories.Clientes.ObterPorId(cliente.Id)
                        ?? throw new InvalidOperationException("Cliente nao localizado antes da conversao do orcamento em venda.");
                    var orcamentoPersistido = service.ObterOrcamentoPorId(orcamento.Id)
                        ?? throw new InvalidOperationException("Orcamento nao localizado antes da conversao em venda.");
                    var estoqueAntes = App.Repositories.Produtos.ObterPorId(produto.Id)?.QuantidadeEstoque
                        ?? throw new InvalidOperationException("Produto nao localizado antes da conversao do orcamento em venda.");

                    orcamentoPersistido.Cliente = clientePersistido;
                    var viewModel = new OrcamentosViewModel
                    {
                        UsuarioLogado = App.Session.UserName
                    };
                    viewModel.ConverterEmVenda(orcamentoPersistido);

                    var convertido = service.ObterOrcamentoPorId(orcamento.Id)
                        ?? throw new InvalidOperationException("Orcamento nao localizado apos conversao em venda.");
                    if (!string.Equals(convertido.Status, "Convertido em Venda", StringComparison.OrdinalIgnoreCase) ||
                        !convertido.DataConversaoVenda.HasValue)
                    {
                        throw new InvalidOperationException("A conversao do orcamento em venda nao persistiu status e data.");
                    }

                    var contas = financeiro.ObterContasReceber()
                        .Where(conta =>
                            string.Equals((string)conta.Origem, "OrcamentoContaReceber", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals((string)conta.ReferenciaExterna, orcamento.Id.ToString(), StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    var movimentacoes = financeiro.ObterMovimentacoes()
                        .Where(movimentacao =>
                            string.Equals(movimentacao.Origem, "OrcamentoMovimentacao", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(movimentacao.ReferenciaExterna, orcamento.Id.ToString(), StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (contas.Count != 1 || movimentacoes.Count != 1)
                    {
                        throw new InvalidOperationException("A conversao do orcamento em venda nao gerou integracao financeira completa e idempotente.");
                    }

                    if (!string.Equals((string)contas[0].Status, "Pago", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A conta a receber do orcamento convertido nao foi liquidada conforme a condicao de pagamento imediata.");
                    }

                    var estoqueDepois = App.Repositories.Produtos.ObterPorId(produto.Id)?.QuantidadeEstoque
                        ?? throw new InvalidOperationException("Produto nao localizado apos conversao do orcamento em venda.");
                    var quantidadeConvertida = convertido.Itens.Sum(item => item.Quantidade);
                    if (estoqueDepois != estoqueAntes - quantidadeConvertida)
                    {
                        throw new InvalidOperationException("A conversao do orcamento em venda nao refletiu a baixa de estoque esperada.");
                    }

                    orcamento = convertido;
                });

                RunCheck(result, "Orcamentos:ConverterEmOS", () =>
                {
                    if (cliente == null || produto == null)
                    {
                        throw new InvalidOperationException("Dados insuficientes para validar a conversao do orcamento em OS.");
                    }

                    var service = new OrcamentoDatabaseService();
                    var clientePersistido = App.Repositories.Clientes.ObterPorId(cliente.Id)
                        ?? throw new InvalidOperationException("Cliente nao localizado antes da conversao do segundo orcamento.");

                    var orcamentoOperacional = CreateOrcamento(
                        service,
                        clientePersistido,
                        produto,
                        quantidade: 1,
                        condicoesPagamento: "Boleto 30 dias",
                        observacoes: "Orcamento operacional convertido em OS pelo workflow test.");
                    service.AdicionarOrcamento(orcamentoOperacional);

                    var viewModel = new OrcamentosViewModel
                    {
                        UsuarioLogado = App.Session.UserName
                    };
                    var orcamentoPersistido = service.ObterOrcamentoPorId(orcamentoOperacional.Id)
                        ?? throw new InvalidOperationException("Segundo orcamento nao localizado antes da aprovacao.");
                    orcamentoPersistido.Cliente = clientePersistido;
                    viewModel.AprovarOrcamento(orcamentoPersistido);

                    var aprovado = service.ObterOrcamentoPorId(orcamentoOperacional.Id)
                        ?? throw new InvalidOperationException("Segundo orcamento nao localizado apos aprovacao.");
                    aprovado.Cliente = clientePersistido;
                    var ordemConvertida = viewModel.ConverterEmOrdemServico(aprovado);

                    var convertido = service.ObterOrcamentoPorId(orcamentoOperacional.Id)
                        ?? throw new InvalidOperationException("Segundo orcamento nao localizado apos conversao em OS.");
                    if (!string.Equals(convertido.Status, "Convertido em OS", StringComparison.OrdinalIgnoreCase) ||
                        !convertido.OrdemServicoId.HasValue ||
                        !convertido.DataConversaoOrdemServico.HasValue)
                    {
                        throw new InvalidOperationException("A conversao do orcamento em OS nao persistiu os vinculos esperados.");
                    }

                    var persistedOs = App.Repositories.OrdensServico.ObterPorId(ordemConvertida.Id)
                        ?? throw new InvalidOperationException("OS gerada a partir do orcamento nao foi localizada.");
                    if (!string.Equals(persistedOs.Origem, "Orcamento", StringComparison.OrdinalIgnoreCase) ||
                        persistedOs.Itens.Count != convertido.Itens.Count)
                    {
                        throw new InvalidOperationException("A OS gerada a partir do orcamento nao refletiu os itens e a origem operacional.");
                    }
                });

                RunCheck(result, "FluxoIntegrado:ClientesOrcamentosOsPdv:5x", () =>
                {
                    var orcamentoService = new OrcamentoDatabaseService();
                    var vendaService = new VendaService(App.Database);
                    var financeiro = new FinanceiroDatabaseService();

                    for (var ciclo = 1; ciclo <= 5; ciclo++)
                    {
                        var clienteCiclo = CreateCliente(usarCnpj: ciclo % 2 == 0);
                        App.Repositories.Clientes.Inserir(clienteCiclo);

                        var veiculoCiclo = CreateVeiculo(clienteCiclo.Id);
                        App.Repositories.Clientes.SalvarVeiculo(veiculoCiclo);

                        var produtoCiclo = CreateProduto();
                        App.Repositories.Produtos.Inserir(produtoCiclo);

                        var clientePersistido = App.Repositories.Clientes.ObterPorId(clienteCiclo.Id)
                            ?? throw new InvalidOperationException($"Cliente do ciclo {ciclo} nao foi localizado apos persistencia.");

                        var viewModel = new OrcamentosViewModel
                        {
                            UsuarioLogado = App.Session.UserName
                        };

                        var orcamentoVenda = CreateOrcamento(
                            orcamentoService,
                            clientePersistido,
                            produtoCiclo,
                            quantidade: 1,
                            condicoesPagamento: "PIX",
                            observacoes: $"Orcamento misto do ciclo {ciclo} para conversao em venda.",
                            incluirServico: true,
                            valorServico: 95m,
                            custoServico: 25m,
                            descricaoServico: $"Mao de obra tecnica ciclo {ciclo}");
                        orcamentoService.AdicionarOrcamento(orcamentoVenda);

                        var estoqueAntesVenda = App.Repositories.Produtos.ObterPorId(produtoCiclo.Id)?.QuantidadeEstoque
                            ?? throw new InvalidOperationException($"Produto do ciclo {ciclo} nao foi localizado antes da venda.");
                        var totalVendasAntes = vendaService.ObterVendas(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Count;
                        var contasAntes = financeiro.ObterContasReceber().Count(conta =>
                            string.Equals((string)conta.Origem, "OrcamentoContaReceber", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals((string)conta.ReferenciaExterna, orcamentoVenda.Id.ToString(), StringComparison.OrdinalIgnoreCase));

                        var orcamentoVendaPersistido = orcamentoService.ObterOrcamentoPorId(orcamentoVenda.Id)
                            ?? throw new InvalidOperationException($"Orcamento de venda do ciclo {ciclo} nao foi localizado.");
                        orcamentoVendaPersistido.Cliente = clientePersistido;
                        viewModel.AprovarOrcamento(orcamentoVendaPersistido);

                        var aprovadoVenda = orcamentoService.ObterOrcamentoPorId(orcamentoVenda.Id)
                            ?? throw new InvalidOperationException($"Orcamento de venda do ciclo {ciclo} nao foi localizado apos aprovacao.");
                        aprovadoVenda.Cliente = clientePersistido;
                        viewModel.ConverterEmVenda(aprovadoVenda);

                        var convertidoVenda = orcamentoService.ObterOrcamentoPorId(orcamentoVenda.Id)
                            ?? throw new InvalidOperationException($"Orcamento de venda do ciclo {ciclo} nao foi localizado apos conversao.");
                        if (!string.Equals(convertidoVenda.Status, "Convertido em Venda", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"O ciclo {ciclo} nao persistiu o status final do orcamento convertido em venda.");
                        }

                        var estoqueDepoisVenda = App.Repositories.Produtos.ObterPorId(produtoCiclo.Id)?.QuantidadeEstoque
                            ?? throw new InvalidOperationException($"Produto do ciclo {ciclo} nao foi localizado apos a venda.");
                        if (estoqueDepoisVenda != estoqueAntesVenda - 1)
                        {
                            throw new InvalidOperationException($"O ciclo {ciclo} nao refletiu a baixa de estoque esperada na venda.");
                        }

                        var vendasDepois = vendaService.ObterVendas(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
                        if (vendasDepois.Count <= totalVendasAntes)
                        {
                            throw new InvalidOperationException($"O ciclo {ciclo} nao registrou uma nova venda.");
                        }

                        var vendaPersistida = vendasDepois
                            .FirstOrDefault(vendaAtual =>
                                vendaAtual.Cliente?.Id == clientePersistido.Id &&
                                Math.Abs(vendaAtual.Total - convertidoVenda.Total) < 0.01m &&
                                vendaAtual.Itens.Any(item => string.Equals(item.Tipo, "Servico", StringComparison.OrdinalIgnoreCase)))
                            ?? throw new InvalidOperationException($"A venda mista do ciclo {ciclo} nao foi localizada com item de servico.");

                        if (!vendaPersistida.Itens.Any(item => item.UsaEstoque) ||
                            !vendaPersistida.Itens.Any(item => !item.UsaEstoque))
                        {
                            throw new InvalidOperationException($"A venda do ciclo {ciclo} nao preservou a composicao entre peca e mao de obra.");
                        }

                        var contasDepois = financeiro.ObterContasReceber().Count(conta =>
                            string.Equals((string)conta.Origem, "OrcamentoContaReceber", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals((string)conta.ReferenciaExterna, orcamentoVenda.Id.ToString(), StringComparison.OrdinalIgnoreCase));
                        if (contasDepois != contasAntes + 1)
                        {
                            throw new InvalidOperationException($"A integracao financeira do ciclo {ciclo} nao criou exatamente uma conta a receber.");
                        }

                        var orcamentoOs = CreateOrcamento(
                            orcamentoService,
                            clientePersistido,
                            produtoCiclo,
                            quantidade: 1,
                            condicoesPagamento: "Boleto 15 dias",
                            observacoes: $"Orcamento misto do ciclo {ciclo} para conversao em OS.",
                            incluirServico: true,
                            valorServico: 120m,
                            custoServico: 40m,
                            descricaoServico: $"Revisao tecnica ciclo {ciclo}");
                        orcamentoService.AdicionarOrcamento(orcamentoOs);

                        var orcamentoOsPersistido = orcamentoService.ObterOrcamentoPorId(orcamentoOs.Id)
                            ?? throw new InvalidOperationException($"Orcamento de OS do ciclo {ciclo} nao foi localizado.");
                        orcamentoOsPersistido.Cliente = clientePersistido;
                        viewModel.AprovarOrcamento(orcamentoOsPersistido);

                        var aprovadoOs = orcamentoService.ObterOrcamentoPorId(orcamentoOs.Id)
                            ?? throw new InvalidOperationException($"Orcamento de OS do ciclo {ciclo} nao foi localizado apos aprovacao.");
                        aprovadoOs.Cliente = clientePersistido;
                        var ordemConvertida = viewModel.ConverterEmOrdemServico(aprovadoOs);

                        var ordemPersistida = App.Repositories.OrdensServico.ObterPorId(ordemConvertida.Id)
                            ?? throw new InvalidOperationException($"OS do ciclo {ciclo} nao foi localizada apos conversao.");
                        if (!ordemPersistida.Itens.Any(item => string.Equals(item.Tipo, "Peca", StringComparison.OrdinalIgnoreCase)) ||
                            !ordemPersistida.Itens.Any(item => string.Equals(item.Tipo, "Servico", StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new InvalidOperationException($"A OS do ciclo {ciclo} nao preservou os tipos de item esperados.");
                        }

                        ordemPersistida.Status = "Em execucao";
                        ordemPersistida.ObservacoesInternas = $"{ordemPersistida.ObservacoesInternas} | Ciclo {ciclo} validado";
                        ordemPersistida.TempoRealMinutos += 15;
                        ordemPersistida.ClienteNomeSnapshot = clientePersistido.Nome;
                        ordemPersistida.TelefoneClienteSnapshot = clientePersistido.Telefone;
                        ordemPersistida.VeiculoDescricaoSnapshot = $"{veiculoCiclo.Marca} {veiculoCiclo.Modelo} {veiculoCiclo.Ano}";
                        ordemPersistida.PlacaSnapshot = veiculoCiclo.Placa;
                        App.Repositories.OrdensServico.Atualizar(ordemPersistida);

                        var ordemEditada = App.Repositories.OrdensServico.ObterPorId(ordemPersistida.Id)
                            ?? throw new InvalidOperationException($"OS do ciclo {ciclo} nao foi localizada apos a edicao.");
                        if (!string.Equals(ordemEditada.Status, "Em execucao", StringComparison.OrdinalIgnoreCase) ||
                            !ordemEditada.ObservacoesInternas.Contains($"Ciclo {ciclo}", StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException($"A edicao da OS do ciclo {ciclo} nao persistiu as alteracoes operacionais.");
                        }
                    }
                });

                RunCheck(result, "Caixa:FecharSessao", () =>
                {
                    if (sessaoCaixa == null)
                    {
                        throw new InvalidOperationException("Sessao de caixa indisponivel para fechamento.");
                    }

                    var caixaService = new CaixaService(App.Database);
                    var sessaoPersistida = caixaService.ObterSessaoPorId(sessaoCaixa.Id)
                        ?? throw new InvalidOperationException("Sessao de caixa nao localizada antes do fechamento.");

                    var sessaoFechada = caixaService.FecharCaixa(sessaoPersistida.ValorEsperado, "Fechamento sintetico do workflow test.");
                    if (sessaoFechada.Aberto || !string.Equals(sessaoFechada.Status, "Fechado", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("O fechamento do caixa nao foi persistido.");
                    }
                });

                RunCheck(result, "Auditoria:Validar", () =>
                {
                    var categorias = new[] { "Clientes", "Estoque", "PDV", "Financeiro", "Banco", "Caixa" };
                    var quantidade = CountAuditLogs(startedAt, categorias);
                    if (quantidade < 9)
                    {
                        throw new InvalidOperationException($"Quantidade de eventos de auditoria abaixo do esperado: {quantidade}.");
                    }
                });

                RunCheck(result, "Auditoria:ConsultaOperacional", () =>
                {
                    var relatorios = new RelatorioDatabaseService();
                    var consulta = relatorios.ObterAuditoriaPaginada(new AuditoriaOperacionalFiltro
                    {
                        DataInicio = startedAt.AddMinutes(-5),
                        DataFim = DateTime.Now.AddMinutes(5),
                        PaginaAtual = 1,
                        ItensPorPagina = 50,
                        Categoria = "PDV"
                    });

                    if (consulta.PaginaAtual != 1)
                    {
                        throw new InvalidOperationException("A consulta operacional de auditoria nao respeitou a pagina inicial.");
                    }

                    var categoriasDisponiveis = relatorios.ObterCategoriasAuditoriaOperacional(startedAt.AddMinutes(-5), DateTime.Now.AddMinutes(5));
                    if (!categoriasDisponiveis.Any())
                    {
                        throw new InvalidOperationException("A consulta operacional nao retornou categorias de auditoria.");
                    }
                });

                RunCheck(result, "Relatorios:ConsistenciaEExportacoes", () =>
                {
                    var relatorios = new RelatorioDatabaseService();
                    var dataInicio = DateTime.Today.AddDays(-7);
                    var dataFim = DateTime.Today.AddDays(7);
                    var resumoConsistencia = relatorios.ObterResumoConsistenciaOperacional(dataInicio, dataFim);

                    if (string.IsNullOrWhiteSpace(resumoConsistencia))
                    {
                        throw new InvalidOperationException("A consulta de consistencia operacional dos relatorios retornou vazio.");
                    }

                    if (!resumoConsistencia.Contains("Nenhuma inconsistencia operacional critica", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"A consulta de consistencia operacional apontou pendencias: {resumoConsistencia}");
                    }

                    var csvPath = BuildTempFilePath("relatorio-workflow", ".csv");
                    var exportService = new RelatorioExportService();
                    var dadosFinanceiros = relatorios.ObterDadosFinanceiros(dataInicio, dataFim, limit: 250);
                    var dadosVendas = relatorios.ObterDadosVendas(dataInicio, dataFim, limit: 250);
                    exportService.ExportarParaExcel(dadosFinanceiros, dadosVendas, csvPath);

                    if (dadosFinanceiros.Count == 0 && dadosVendas.Count == 0)
                    {
                        throw new InvalidOperationException("Os relatorios nao retornaram dados operacionais para exportacao.");
                    }

                    EnsureGeneratedFile(csvPath, "CSV da central de relatorios");
                });

                RunCheck(result, "Backup:CriarEVerificar", () =>
                {
                    backupPath = App.Backups.CriarBackupManual();
                    if (!App.Backups.VerificarBackup(backupPath))
                    {
                        throw new InvalidOperationException("Backup criado, mas reprovado na verificacao de integridade.");
                    }

                    if (CountRows("DatabaseBackups") <= 0)
                    {
                        throw new InvalidOperationException("Historico de backups nao foi registrado.");
                    }

                    var manifesto = App.Backups.LerManifesto(backupPath);
                    if (manifesto == null)
                    {
                        throw new InvalidOperationException("Manifesto do backup nao foi criado.");
                    }

                    if (string.IsNullOrWhiteSpace(manifesto.HashSha256))
                    {
                        throw new InvalidOperationException("Hash SHA256 nao foi gerado no manifesto.");
                    }

                    if (string.IsNullOrWhiteSpace(manifesto.VersaoSistema))
                    {
                        throw new InvalidOperationException("Versao do sistema nao foi registrada no manifesto.");
                    }
                });

                RunCheck(result, "Backup:AntesAtualizacao", () =>
                {
                    var backupAtualizacao = App.Backups.CriarBackupAntesAtualizacao();
                    if (!App.Backups.VerificarBackup(backupAtualizacao))
                    {
                        throw new InvalidOperationException("Backup antes de atualizacao falhou na verificacao.");
                    }

                    if (!File.Exists(backupAtualizacao))
                    {
                        throw new InvalidOperationException("Arquivo de backup antes de atualizacao nao existe.");
                    }
                });

                RunCheck(result, "Backup:AntesMigracao", () =>
                {
                    var backupMigracao = App.Backups.CriarBackupAntesMigracao();
                    if (!App.Backups.VerificarBackup(backupMigracao))
                    {
                        throw new InvalidOperationException("Backup antes de migracao falhou na verificacao.");
                    }

                    if (!File.Exists(backupMigracao))
                    {
                        throw new InvalidOperationException("Arquivo de backup antes de migracao nao existe.");
                    }
                });

                RunCheck(result, "Backup:NomePadronizado", () =>
                {
                    var backupManual = App.Backups.CriarBackupManual();
                    var fileName = Path.GetFileName(backupManual);

                    if (!fileName.StartsWith("PrimoAutoEletrica_Backup_"))
                    {
                        throw new InvalidOperationException($"Nome do backup nao segue padrao: {fileName}");
                    }

                    if (!fileName.EndsWith(".db"))
                    {
                        throw new InvalidOperationException($"Backup nao tem extensao .db: {fileName}");
                    }

                    var manifestoPath = Path.ChangeExtension(backupManual, ".json");
                    if (!File.Exists(manifestoPath))
                    {
                        throw new InvalidOperationException("Arquivo de manifesto JSON nao foi criado.");
                    }
                });

                RunCheck(result, "Banco:ResumoFinal", () =>
                {
                    if (CountRows("Clientes") < 1 ||
                        CountRows("Veiculos") < 1 ||
                        CountRows("Produtos") < 1 ||
                        CountRows("Orcamentos") < 1 ||
                        CountRows("OrdensServico") < 1 ||
                        CountRows("Vendas") < 1)
                    {
                        throw new InvalidOperationException("Fluxo operacional nao materializou todas as entidades esperadas.");
                    }
                });
            }
            finally
            {
                App.Session.EndSession();
            }

            result.ReportPath = PersistReport(result);
            var summary = $"Workflow test concluido. Total={result.TotalChecks}, Sucesso={result.PassedChecks}, Falhas={result.FailedChecks}.";

            if (result.HasFailures)
            {
                _logger.LogError(summary);
                App.Audit.RegistrarSistema("WorkflowTest", summary, "Error", false);
            }
            else
            {
                _logger.LogInfo(summary);
                App.Audit.RegistrarSistema("WorkflowTest", summary);
            }

            return result;
        }

        private void RunCheck(WorkflowTestRunResult result, string name, Action action)
        {
            var timer = Stopwatch.StartNew();

            try
            {
                action();
                timer.Stop();

                result.Checks.Add(new WorkflowTestCheckResult
                {
                    Name = name,
                    Success = true,
                    DurationMs = timer.ElapsedMilliseconds,
                    Message = "OK"
                });

                _logger.LogInfo($"Workflow test aprovado: {name} ({timer.ElapsedMilliseconds} ms).");
            }
            catch (Exception ex)
            {
                timer.Stop();

                result.Checks.Add(new WorkflowTestCheckResult
                {
                    Name = name,
                    Success = false,
                    DurationMs = timer.ElapsedMilliseconds,
                    Message = ex.Message
                });

                _logger.LogError($"Workflow test falhou: {name}", ex);
            }
        }

        private Funcionario EnsureFuncionario(string email, string nome, string perfil)
        {
            var existente = App.Repositories.Funcionarios.ObterTodos(false)
                .FirstOrDefault(funcionario => string.Equals(funcionario.Email, email, StringComparison.OrdinalIgnoreCase));

            if (existente != null)
            {
                return existente;
            }

            var funcionario = new Funcionario
            {
                Nome = nome,
                Email = email,
                Senha = PasswordHasherService.HashPassword("Workflow@123"),
                Funcao = perfil,
                PerfilAcesso = perfil,
                Telefone = "(11) 90000-0000",
                DataAdmissao = DateTime.Today,
                Salario = 2500m,
                Status = "Ativo",
                DataCadastro = DateTime.Now,
                Ativo = true
            };

            funcionario.Id = App.Repositories.Funcionarios.Salvar(funcionario);
            return funcionario;
        }

        private static Cliente CreateCliente(bool usarCnpj = false)
        {
            var token = CreateUniqueNumericToken(14);
            return new Cliente
            {
                Nome = $"Workflow Cliente {token}",
                CPF = usarCnpj ? GerarCnpjValido(token) : GerarCpfValido(token),
                Telefone = "(11) 98888-1000",
                WhatsApp = "(11) 98888-1000",
                Email = $"workflow.cliente.{token}@primoauto.com",
                Cidade = "Sao Paulo",
                Estado = "SP",
                Observacoes = "Cliente criado pelo workflow test.",
                DataCadastro = DateTime.Now
            };
        }

        private static Veiculo CreateVeiculo(Guid clienteId)
        {
            var placaAlfanumerica = CreateUniqueAlphaNumericToken(1);
            var placaNumerica = CreateUniqueNumericToken(2);
            var chassiSeed = CreateUniqueAlphaNumericToken(11);
            var renavamSeed = CreateUniqueNumericToken(11);

            return new Veiculo
            {
                ClienteId = clienteId,
                Marca = "Volkswagen",
                Modelo = "Gol",
                Ano = "2018",
                Cor = "Prata",
                Placa = $"WFT1{placaAlfanumerica}{placaNumerica}",
                Chassi = $"9BWZZZ377VT{chassiSeed}",
                Renavam = renavamSeed,
                Motor = "1.6 MSI",
                Combustivel = "Flex",
                Quilometragem = 84500,
                Observacoes = "Veiculo criado pelo workflow test."
            };
        }

        private static string GerarCpfValido(string seed)
        {
            var baseDigits = new string(seed.Where(char.IsDigit).ToArray());
            baseDigits = baseDigits.Length >= 9
                ? baseDigits[^9..]
                : baseDigits.PadLeft(9, '0');

            var digito1 = CalcularDigitoCpf(baseDigits);
            var digito2 = CalcularDigitoCpf(baseDigits + digito1);
            return $"{baseDigits}{digito1}{digito2}";
        }

        private static int CalcularDigitoCpf(string baseDigits)
        {
            var pesoInicial = baseDigits.Length + 1;
            var soma = 0;

            for (var indice = 0; indice < baseDigits.Length; indice++)
            {
                soma += (baseDigits[indice] - '0') * (pesoInicial - indice);
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        private static string GerarCnpjValido(string seed)
        {
            var baseDigits = new string(seed.Where(char.IsDigit).ToArray());
            baseDigits = baseDigits.Length >= 12
                ? baseDigits[^12..]
                : baseDigits.PadLeft(12, '0');

            var digito1 = CalcularDigitoCnpj(baseDigits);
            var digito2 = CalcularDigitoCnpj(baseDigits + digito1.ToString(CultureInfo.InvariantCulture));
            return $"{baseDigits}{digito1}{digito2}";
        }

        private static int CalcularDigitoCnpj(string baseDigits)
        {
            var pesos = baseDigits.Length == 12
                ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 }
                : new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            var soma = 0;
            for (var indice = 0; indice < baseDigits.Length; indice++)
            {
                soma += (baseDigits[indice] - '0') * pesos[indice];
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        private static string CreateUniqueNumericToken(int tamanho)
        {
            var baseToken = $"{DateTime.Now:yyyyMMddHHmmssfff}{Guid.NewGuid():N}";
            var digitos = new string(baseToken.Where(char.IsDigit).ToArray());
            return digitos.Length >= tamanho
                ? digitos[^tamanho..]
                : digitos.PadRight(tamanho, '0');
        }

        private static string CreateUniqueAlphaNumericToken(int tamanho)
        {
            var token = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return token.Length >= tamanho
                ? token[..tamanho]
                : token.PadRight(tamanho, 'A');
        }

        private static Produto CreateProduto()
        {
            var token = CreateUniqueAlphaNumericToken(8);
            return new Produto
            {
                Codigo = $"WF-{token}",
                Nome = $"Rele Workflow {token}",
                Descricao = "Produto sintetico para validacao operacional.",
                Categoria = "Eletrica",
                Marca = "Teste",
                Modelo = "12V",
                Fornecedor = "Fornecedor Workflow",
                QuantidadeEstoque = 12,
                QuantidadeMinima = 2,
                QuantidadeMaxima = 30,
                Localizacao = "WF-A1",
                Prateleira = "P1",
                Gaveta = "G1",
                PrecoCompra = 15m,
                PrecoVenda = 32m,
                ValorTotalEstoque = 180m,
                MargemLucro = 53.125m,
                Ativo = true,
                DataCadastro = DateTime.Now,
                DataUltimaAtualizacao = DateTime.Now
            };
        }

        private static Orcamento CreateOrcamento(
            OrcamentoDatabaseService service,
            Cliente cliente,
            Produto produto,
            int quantidade = 2,
            string condicoesPagamento = "PIX",
            string observacoes = "Orcamento criado no workflow test.",
            string prazoEntrega = "Imediato",
            bool incluirServico = false,
            decimal valorServico = 0m,
            decimal custoServico = 0m,
            string descricaoServico = "Mao de obra tecnica")
        {
            var orcamentoId = Guid.NewGuid();
            var itens = new List<OrcamentoItem>();

            var itemProduto = new OrcamentoItem
            {
                Id = Guid.NewGuid(),
                OrcamentoId = orcamentoId,
                ProdutoId = produto.Id,
                Tipo = "Produto",
                ProdutoNome = produto.Nome,
                ProdutoCodigo = produto.Codigo,
                ProdutoCategoria = produto.Categoria,
                ProdutoMarca = produto.Marca,
                ProdutoAplicacao = "Teste operacional",
                Quantidade = quantidade,
                PrecoUnitario = produto.PrecoVenda,
                PrecoCusto = produto.PrecoCompra,
                Desconto = 0,
                Subtotal = produto.PrecoVenda * quantidade,
                LucroEstimado = (produto.PrecoVenda - produto.PrecoCompra) * quantidade,
                MargemLucro = produto.MargemLucro,
                EstoqueDisponivel = produto.QuantidadeEstoque,
                Observacoes = "Item gerado pelo workflow test."
            };
            itens.Add(itemProduto);

            if (incluirServico)
            {
                var subtotalServico = Math.Max(0m, valorServico);
                var custoServicoValidado = Math.Max(0m, custoServico);
                var lucroServico = subtotalServico - custoServicoValidado;

                itens.Add(new OrcamentoItem
                {
                    Id = Guid.NewGuid(),
                    OrcamentoId = orcamentoId,
                    ProdutoId = null,
                    Tipo = "Servico",
                    ProdutoNome = descricaoServico,
                    ProdutoCodigo = "SERVICO",
                    ProdutoCategoria = "Mao de obra",
                    ProdutoMarca = "Interno",
                    ProdutoAplicacao = "Servico operacional",
                    Quantidade = 1,
                    PrecoUnitario = subtotalServico,
                    PrecoCusto = custoServicoValidado,
                    Desconto = 0,
                    Subtotal = subtotalServico,
                    LucroEstimado = lucroServico,
                    MargemLucro = subtotalServico > 0 ? (lucroServico / subtotalServico) * 100m : 0m,
                    EstoqueDisponivel = 0,
                    Observacoes = "Servico gerado pelo workflow test."
                });
            }

            var subtotal = itens.Sum(item => item.Subtotal);
            var lucroEstimado = itens.Sum(item => item.LucroEstimado);

            return new Orcamento
            {
                Id = orcamentoId,
                ClienteId = cliente.Id,
                Numero = service.GerarNumeroOrcamento(),
                Status = "Em Aberto",
                DataCriacao = DateTime.Now,
                DataValidade = DateTime.Today.AddDays(7),
                Subtotal = subtotal,
                Desconto = 0,
                Acrescimo = 0,
                Total = subtotal,
                MargemLucro = subtotal > 0 ? (lucroEstimado / subtotal) * 100m : 0m,
                LucroEstimado = lucroEstimado,
                ComissaoVendedor = 0,
                ImpostosEstimados = 0,
                Observacoes = observacoes,
                CondicoesPagamento = condicoesPagamento,
                PrazoEntrega = prazoEntrega,
                Itens = itens,
                Cliente = cliente
            };
        }

        private static OrdemServico CreateOrdemServico(Cliente cliente, Veiculo veiculo, Produto produto)
        {
            return new OrdemServico
            {
                Numero = App.Repositories.OrdensServico.GerarProximoNumero(),
                ClienteId = cliente.Id,
                VeiculoId = veiculo.Id,
                ClienteNomeSnapshot = cliente.Nome,
                TelefoneClienteSnapshot = cliente.Telefone,
                VeiculoDescricaoSnapshot = $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano}",
                PlacaSnapshot = veiculo.Placa,
                Status = "Aberta",
                Prioridade = "Normal",
                Origem = "WorkflowTest",
                ProblemaRelatado = "Falha eletrica intermitente.",
                ObservacoesInternas = "OS criada automaticamente para validacao.",
                ChecklistEntrega = "Checklist de conectores revisado e teste final executado.",
                GarantiaObservacoes = "Garantia operacional de 90 dias para a troca do componente.",
                DataAbertura = DateTime.Now,
                DataPrevisao = DateTime.Today.AddDays(1),
                GarantiaValidaAte = DateTime.Today.AddDays(90),
                Ativo = true,
                Itens = new List<OrdemServicoItem>
                {
                    new()
                    {
                        ProdutoId = produto.Id,
                        Tipo = "Peca",
                        Descricao = produto.Nome,
                        Quantidade = 1,
                        ValorUnitario = produto.PrecoVenda,
                        CustoUnitario = produto.PrecoCompra,
                        Observacoes = "Peca reservada no workflow test."
                    },
                    new()
                    {
                        Tipo = "Servico",
                        Descricao = "Diagnostico eletrico",
                        Quantidade = 1,
                        ValorUnitario = 90m,
                        CustoUnitario = 0,
                        Observacoes = "Servico sintetico"
                    }
                }
            };
        }

        private static Agendamento CreateAgendamento(Cliente cliente, Veiculo veiculo, Produto produto)
        {
            return new Agendamento
            {
                Id = Guid.NewGuid(),
                Numero = $"AG-WF-{DateTime.Now:HHmmss}",
                DataCriacao = DateTime.Now,
                DataAgendamento = DateTime.Today,
                HoraInicio = DateTime.Today.AddHours(9),
                HoraTermino = DateTime.Today.AddHours(11),
                DuracaoEstimada = TimeSpan.FromHours(2),
                Status = "Confirmado",
                Prioridade = "Normal",
                TipoServico = "Diagnostico eletrico",
                CategoriaServico = "Oficina",
                DescricaoServico = "Diagnostico e troca de componente.",
                Observacoes = "Agendamento sintetico do workflow test.",
                ClienteId = cliente.Id,
                ClienteNome = cliente.Nome,
                ClienteTelefone = cliente.Telefone,
                ClienteEmail = cliente.Email,
                ClienteDocumento = cliente.CPF,
                ClienteVip = false,
                ClienteTotalGasto = cliente.TotalGasto,
                ClienteAtendimentos = cliente.TotalServicos,
                ClienteUltimaVisita = cliente.UltimaVisita,
                VeiculoId = veiculo.Id,
                VeiculoPlaca = veiculo.Placa,
                VeiculoModelo = veiculo.Modelo,
                VeiculoMarca = veiculo.Marca,
                VeiculoAno = veiculo.Ano,
                VeiculoCor = veiculo.Cor,
                VeiculoCombustivel = veiculo.Combustivel,
                VeiculoQuilometragem = veiculo.Quilometragem,
                TecnicoId = Guid.Empty,
                TecnicoNome = "Workflow Tecnico",
                TecnicoEspecialidade = "Eletrica",
                TecnicoAtivo = true,
                ValorEstimado = produto.PrecoVenda + 120m,
                ValorReal = produto.PrecoVenda + 120m,
                ValorProdutos = produto.PrecoVenda,
                ValorServicos = 120m,
                FormaPagamento = "A definir",
                Produtos = new List<AgendamentoProduto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProdutoId = produto.Id,
                        ProdutoNome = produto.Nome,
                        ProdutoCodigo = produto.Codigo,
                        Quantidade = 1,
                        PrecoUnitario = produto.PrecoVenda,
                        PrecoTotal = produto.PrecoVenda
                    }
                },
                Servicos = new List<AgendamentoServico>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Nome = "Diagnostico eletrico",
                        Categoria = "Oficina",
                        Valor = 120m,
                        TempoEstimado = TimeSpan.FromHours(1),
                        Status = "Pendente",
                        Observacoes = "Servico sintetico do workflow test."
                    }
                },
                Timeline = new List<AgendamentoTimeline>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DataHora = DateTime.Now,
                        Usuario = "Workflow",
                        Acao = "Criacao",
                        Detalhes = "Agendamento criado no workflow test.",
                        TipoAlteracao = "Cadastro"
                    }
                }
            };
        }

        private static Venda CreateVenda(Cliente cliente, Produto produto, string usuario)
        {
            return new Venda
            {
                Cliente = cliente,
                FormaPagamento = "PIX",
                Desconto = 0,
                Usuario = usuario,
                Itens = new List<ItemVenda>
                {
                    new()
                    {
                        Produto = produto,
                        Quantidade = 2,
                        PrecoUnitario = produto.PrecoVenda,
                        Desconto = 0
                    }
                },
                Total = produto.PrecoVenda * 2
            };
        }

        private static string BuildTempFilePath(string prefix, string extension)
        {
            return Path.Combine(
                Path.GetTempPath(),
                $"{prefix}-{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}");
        }

        private static void EnsureGeneratedFile(string path, string descricao)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new InvalidOperationException($"{descricao} nao foi gerado no caminho esperado.");
            }

            var fileInfo = new FileInfo(path);
            if (fileInfo.Length <= 0)
            {
                throw new InvalidOperationException($"{descricao} foi gerado, mas ficou vazio.");
            }
        }

        private int CountRows(string tableName)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private int CountAuditLogs(DateTime startedAt, IReadOnlyCollection<string> categorias)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            var parametros = categorias.Select((_, index) => $"@Categoria{index}").ToList();
            command.CommandText = $@"
                SELECT COUNT(*)
                FROM AuditLogs
                WHERE datetime(DataHora) >= datetime(@StartedAt)
                  AND Categoria IN ({string.Join(", ", parametros)});";
            command.Parameters.AddWithValue("@StartedAt", startedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            for (var index = 0; index < categorias.Count; index++)
            {
                command.Parameters.AddWithValue($"@Categoria{index}", categorias.ElementAt(index));
            }

            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private string PersistReport(WorkflowTestRunResult result)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "workflow-tests");
            Directory.CreateDirectory(baseDirectory);

            var path = Path.Combine(baseDirectory, $"workflow-test-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt");
            var builder = new StringBuilder();

            builder.AppendLine("PRIMO AUTO ELETRICA - WORKFLOW TEST");
            builder.AppendLine($"GeneratedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"RuntimeMode: {result.RuntimeMode}");
            builder.AppendLine($"RuntimeAppDataPath: {result.RuntimeAppDataPath}");
            builder.AppendLine($"DatabasePath: {result.DatabasePath}");
            builder.AppendLine($"TotalChecks: {result.TotalChecks}");
            builder.AppendLine($"PassedChecks: {result.PassedChecks}");
            builder.AppendLine($"FailedChecks: {result.FailedChecks}");
            builder.AppendLine();

            foreach (var check in result.Checks)
            {
                builder.AppendLine($"[{(check.Success ? "PASS" : "FAIL")}] {check.Name} ({check.DurationMs} ms)");
                builder.AppendLine($"Message: {check.Message}");
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            return path;
        }
    }

    public sealed class WorkflowTestRunResult
    {
        public List<WorkflowTestCheckResult> Checks { get; } = new();
        public string ReportPath { get; set; } = string.Empty;
        public string RuntimeMode { get; set; } = string.Empty;
        public string RuntimeAppDataPath { get; set; } = string.Empty;
        public string DatabasePath { get; set; } = string.Empty;
        public int TotalChecks => Checks.Count;
        public int PassedChecks => Checks.Count(check => check.Success);
        public int FailedChecks => Checks.Count(check => !check.Success);
        public bool HasFailures => FailedChecks > 0;
    }

    public sealed class WorkflowTestCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public long DurationMs { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
