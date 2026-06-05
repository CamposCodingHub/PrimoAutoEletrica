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
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Executa uma validacao leve da camada de UI para garantir que os modulos
    /// principais possam ser construidos e preparados sem derrubar a aplicacao.
    /// </summary>
    public sealed class UiSmokeTestService
    {
        private static readonly Type[] CoreControlTypes =
        {
            typeof(DashboardControl),
            typeof(ClientesControl),
            typeof(VeiculosControl),
            typeof(OrcamentosControl),
            typeof(OrdensServicoControl),
            typeof(PDVControl),
            typeof(EstoqueControl),
            typeof(CatalogoPecasControl),
            typeof(ImportarNFeControl),
            typeof(FinanceiroControl),
            typeof(FornecedoresControl),
            typeof(FuncionariosControl),
            typeof(AgendamentosControl),
            typeof(RelatoriosControl)
        };

        private static readonly string[] CoreModules =
        {
            "Dashboard",
            "Clientes",
            "Veiculos",
            "Orcamentos",
            "OrdensServico",
            "PDV",
            "Estoque",
            "CatalogoPecas",
            "ImportarNFe",
            "Financeiro",
            "Fornecedores",
            "Funcionarios",
            "Agendamentos",
            "Relatorios"
        };

        private readonly LoggerService _logger;
        private readonly string _checkFilter;
        private UiSmokeFixture? _fixture;

        public UiSmokeTestService(LoggerService logger, string? checkFilter = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _checkFilter = checkFilter?.Trim() ?? string.Empty;
        }

        public UiSmokeTestRunResult Run()
        {
            var result = new UiSmokeTestRunResult();
            var syntheticUser = EnsureFuncionario("smoke-admin@primoauto.com", "Smoke Test", "Administrador");

            var inicioDetalhe = string.IsNullOrWhiteSpace(_checkFilter)
                ? "Inicio da validacao automatizada da interface."
                : $"Inicio da validacao automatizada da interface com filtro '{_checkFilter}'.";
            _logger.LogInfo("Iniciando smoke test de UI.");
            App.Audit.RegistrarSistema("SmokeTestUi", inicioDetalhe);

            App.Session.StartSession(syntheticUser);

            try
            {
                if (!string.IsNullOrWhiteSpace(_checkFilter))
                {
                    RunFilteredChecks(result, syntheticUser);
                    return FinalizarSmokeTest(result);
                }

                RunCheck(result, "MainWindow", () =>
                {
                    var window = new MainWindow(syntheticUser);
                    PrepareWindow(window);
                });

                RunCheck(result, "Dados:SeedBaseUi", () =>
                {
                    _fixture = EnsureSmokeFixture(syntheticUser);
                });

                RunProdutosCamposAnexosChecks(result);
                RunFornecedoresProdutoFornecedorChecks(result);
                RunClientesLgpdChecks(result);
                RunClientesAnexosAssinaturaChecks(result);
                RunVeiculosAlertasMidiaChecks(result);
                RunOrdensServicoMidiasChecklistFinanceiroChecks(result);
                RunOrcamentosConversoesPdfWhatsAppAlertasChecks(result);
                RunAgendamentosVisualizacoesConversoesChecks(result);
                RunFinanceiroGraficosAlertasChecks(result);
                RunImportarNFeRollbackChecks(result, syntheticUser);
                RunImportarNFeXmlRealRelancamentoChecks(result, syntheticUser);
                RunFuncionariosPermissoesAuditoriaChecks(result, syntheticUser);
                RunConfiguracoesComerciaisBackupChecks(result, syntheticUser);
                RunLoginSessaoSegurancaChecks(result, syntheticUser);

                RunMainWindowNavigationChecks(result, syntheticUser);

                var permissionService = new PermissionService(syntheticUser, _logger, App.Database);
                var navigationService = new NavigationService(permissionService, _logger);

                foreach (var module in CoreModules)
                {
                    RunCheck(result, $"Modulo:{module}", () =>
                    {
                        var control = navigationService.Navigate(module)
                            ?? throw new InvalidOperationException($"NavigationService retornou nulo para o modulo {module}.");

                        PrepareElement(control);
                    });
                }

                RunCheck(result, "Janela:ImportarNota", () =>
                {
                    var window = new ImportarNotaWindow();
                    PrepareWindow(window);
                });

                RunCheck(result, "Janela:ImportarCatalogoPecas", () =>
                {
                    var window = new ImportarCatalogoPecasWindow();
                    PrepareWindow(window);
                });

                RunDiscoveredWindows(result);
                RunDiscoveredUserControls(result);
                RunInteractiveMainWindowChecks(result, syntheticUser);
                RunPdvOperationalInteractionChecks(result);
                RunInteractiveCoreControlChecks(result);
                RunParameterizedWindows(result, syntheticUser);
                RunInteractiveWindowButtonChecks(result, syntheticUser);
                RunInteractiveDiscoveredUserControlChecks(result);
                RunRelatoriosOperationalChecks(result);
                RunNavigationServiceRegressionChecks(result, syntheticUser);
            }
            finally
            {
                App.Session.EndSession();
            }

            return FinalizarSmokeTest(result);
        }

        private UiSmokeTestRunResult FinalizarSmokeTest(UiSmokeTestRunResult result)
        {
            result.ReportPath = PersistReport(result);
            var summary = $"Smoke test UI concluido. Total={result.TotalChecks}, Sucesso={result.PassedChecks}, Falhas={result.FailedChecks}.";

            if (result.HasFailures)
            {
                _logger.LogError(summary);
                App.Audit.RegistrarSistema("SmokeTestUi", summary, "Error", false);
            }
            else
            {
                _logger.LogInfo(summary);
                App.Audit.RegistrarSistema("SmokeTestUi", summary);
            }

            return result;
        }

        private void RunFilteredChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            if (FiltroCombina("MainWindow"))
            {
                RunCheck(result, "MainWindow", () =>
                {
                    var window = new MainWindow(syntheticUser);
                    PrepareWindow(window);
                });
            }

            if (FiltroCombina("Dados:SeedBaseUi"))
            {
                RunCheck(result, "Dados:SeedBaseUi", () =>
                {
                    _fixture = EnsureSmokeFixture(syntheticUser);
                });
            }

            if (FiltroCombina("PreCheck") || FiltroCombina("SemModaisPresas"))
            {
                RunPreCheckSemModaisPresas(result, syntheticUser);
            }

            if (FiltroCombina("ImportarNFe"))
            {
                RunImportarNFeRollbackChecks(result, syntheticUser);
                RunImportarNFeXmlRealRelancamentoChecks(result, syntheticUser);
            }

            if (FiltroCombina("PDV"))
            {
                RunPdvOperationalInteractionChecks(result);
            }

            if (FiltroCombina("Fornecedores"))
            {
                RunFornecedoresProdutoFornecedorChecks(result);
            }

            if (FiltroCombina("Clientes"))
            {
                RunClientesLgpdChecks(result);
                RunClientesAnexosAssinaturaChecks(result);
            }

            if (FiltroCombina("Veiculos"))
            {
                RunVeiculosAlertasMidiaChecks(result);
            }

            if (FiltroCombina("OrdensServico"))
            {
                RunOrdensServicoMidiasChecklistFinanceiroChecks(result);
            }

            if (FiltroCombina("Orcamentos"))
            {
                RunOrcamentosConversoesPdfWhatsAppAlertasChecks(result);
            }

            if (FiltroCombina("Agendamentos"))
            {
                RunAgendamentosVisualizacoesConversoesChecks(result);
            }

            if (FiltroCombina("Financeiro"))
            {
                RunFinanceiroGraficosAlertasChecks(result);
            }

            if (FiltroCombina("Relatorios"))
            {
                RunRelatoriosOperationalChecks(result);
            }

            if (FiltroCombina("Funcionarios"))
            {
                RunFuncionariosPermissoesAuditoriaChecks(result, syntheticUser);
            }

            if (FiltroCombina("Configuracoes"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunConfiguracoesComerciaisBackupChecks(result, syntheticUser);
            }

            if (FiltroCombina("LoginSessao"))
            {
                RunLoginSessaoSegurancaChecks(result, syntheticUser);
            }

            if (result.TotalChecks == 0)
            {
                RunCheck(result, $"Filtro:{_checkFilter}", () =>
                {
                    throw new InvalidOperationException($"Nenhum smoke check conhecido combina com o filtro '{_checkFilter}'.");
                });
            }
        }

        private bool FiltroCombina(string valor)
        {
            return valor.Contains(_checkFilter, StringComparison.OrdinalIgnoreCase) ||
                   _checkFilter.Contains(valor, StringComparison.OrdinalIgnoreCase);
        }

        private void RunCheck(UiSmokeTestRunResult result, string name, Action action)
        {
            if (!DeveExecutarCheck(name))
            {
                return;
            }

            var timer = Stopwatch.StartNew();
            _logger.LogInfo($"Smoke test iniciando: {name}.");

            try
            {
                action();
                timer.Stop();

                const int warnThresholdMs = 30000; // 30s
                if (timer.ElapsedMilliseconds > warnThresholdMs)
                {
                    try { _logger.LogWarning($"Smoke test '{name}' demorou {timer.ElapsedMilliseconds} ms (acima de {warnThresholdMs} ms)."); } catch { }
                }

                result.Checks.Add(new UiSmokeTestCheckResult
                {
                    Name = name,
                    Success = true,
                    DurationMs = timer.ElapsedMilliseconds,
                    Message = "OK"
                });

                _logger.LogInfo($"Smoke test aprovado: {name} ({timer.ElapsedMilliseconds} ms).");
            }
            catch (Exception ex)
            {
                timer.Stop();

                result.Checks.Add(new UiSmokeTestCheckResult
                {
                    Name = name,
                    Success = false,
                    DurationMs = timer.ElapsedMilliseconds,
                    Message = ex.Message
                });

                _logger.LogError($"Smoke test falhou: {name}", ex);
            }
        }

        private bool DeveExecutarCheck(string name)
        {
            return string.IsNullOrWhiteSpace(_checkFilter) ||
                   name.Contains(_checkFilter, StringComparison.OrdinalIgnoreCase);
        }

        private void RunImportarNFeRollbackChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "ImportarNFe:RollbackProdutosAuditavel", () =>
            {
                var token = DateTime.Now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var nota = new NotaFiscalImportada
                {
                    ChaveAcesso = $"SMOKE-NFE-ROLLBACK-{token}",
                    Numero = $"RBK{token[^6..]}",
                    Serie = "1",
                    DataEmissao = DateTime.Today,
                    DataEntrada = DateTime.Now,
                    ValorTotal = 20m,
                    ValorProdutos = 20m,
                    Fornecedor = new FornecedorNota
                    {
                        Nome = "Fornecedor Smoke Rollback",
                        CNPJ = "12345678000199",
                        Telefone = "(11) 3333-0000"
                    },
                    Produtos = new List<ProdutoImportado>
                    {
                        new()
                        {
                            Codigo = $"RBK-{token[^8..]}",
                            CodigoBarras = $"RBK-{token[^8..]}",
                            Nome = $"Rele Rollback Smoke {token[^5..]}",
                            NCM = "85364100",
                            CFOP = "5102",
                            Quantidade = 2,
                            ValorUnitario = 10m,
                            ValorTotal = 20m,
                            UnidadeMedida = "UN",
                            Status = StatusImportacao.Novo,
                            SelecionadoParaImportacao = true,
                            AcaoPlanejada = AcoesPlanejadas.CriarNovo,
                            CategoriaSugerida = "Eletrica",
                            MargemAplicada = 100m,
                            PrecoVendaSugerido = 20m
                        }
                    }
                };

                var nfeService = new NFeService(App.Database);
                var importada = nfeService.ImportarNotaPreparada(nota, Guid.NewGuid(), syntheticUser.Nome);
                if (importada.Status != StatusImportacaoNota.Concluida)
                {
                    throw new InvalidOperationException($"Importacao sintetica nao concluiu: {importada.Status} - {importada.Erro}");
                }

                var produtoId = importada.Produtos.Single().ProdutoExistenteId
                    ?? throw new InvalidOperationException("Produto criado pela importacao sintetica nao recebeu ID para rollback.");

                if (App.Repositories.Produtos.ObterPorId(produtoId) == null)
                {
                    throw new InvalidOperationException("Produto sintetico importado nao foi localizado antes do rollback.");
                }

                var importacaoRepository = new ImportacaoRepository(App.Database);
                var totalRollbacksAntes = importacaoRepository.ObterTotalRollbacksAuditados();
                var resultado = importacaoRepository.DesfazerProdutosDaImportacao(
                    importada.Id,
                    "Smoke Test",
                    "Validacao automatizada do rollback auditavel.");

                if (resultado.ProdutosRemovidos != 1 || resultado.ProdutosBloqueados != 0)
                {
                    throw new InvalidOperationException($"Rollback sintetico inesperado: removidos={resultado.ProdutosRemovidos}; bloqueados={resultado.ProdutosBloqueados}; ignorados={resultado.ProdutosIgnorados}.");
                }

                if (App.Repositories.Produtos.ObterPorId(produtoId) != null)
                {
                    throw new InvalidOperationException("Produto sintetico continuou cadastrado apos rollback.");
                }

                if (importacaoRepository.ObterTotalRollbacksAuditados() <= totalRollbacksAntes)
                {
                    throw new InvalidOperationException("Rollback sintetico nao gravou auditoria propria.");
                }
            });

            RunCheck(result, "ImportarNFe:RollbackAtualizacaoComSnapshot", () =>
            {
                var token = DateTime.Now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var produtoExistente = new Produto
                {
                    Id = Guid.NewGuid(),
                    Codigo = $"UPD-{token[^8..]}",
                    CodigoBarras = $"OLD-{token[^8..]}",
                    Nome = $"Alternador Snapshot Smoke {token[^5..]}",
                    Descricao = "Produto existente para validar rollback de atualizacao NF-e.",
                    Categoria = "Alternadores",
                    Marca = "Smoke",
                    Modelo = "Snapshot",
                    QuantidadeEstoque = 5,
                    QuantidadeMinima = 1,
                    PrecoCompra = 10m,
                    PrecoVenda = 20m,
                    MargemLucro = 50m,
                    ValorTotalEstoque = 50m,
                    UnidadeMedida = "UN",
                    NCMS = "85011019",
                    CFOP = "5102",
                    Ativo = true,
                    DataCadastro = DateTime.Now,
                    DataUltimaCompra = DateTime.Now.AddDays(-10),
                    DataUltimaAtualizacao = DateTime.Now.AddDays(-10),
                    Observacoes = "Snapshot anterior do smoke NF-e."
                };

                var importacaoRepository = new ImportacaoRepository(App.Database);
                Guid importacaoId = Guid.Empty;
                var produtoInserido = false;
                var rollbackExecutado = false;

                try
                {
                    App.Repositories.Produtos.Inserir(produtoExistente);
                    produtoInserido = true;

                    var nota = new NotaFiscalImportada
                    {
                        ChaveAcesso = $"SMOKE-NFE-UPD-SNAPSHOT-{token}",
                        Numero = $"UPD{token[^6..]}",
                        Serie = "1",
                        DataEmissao = DateTime.Today,
                        DataEntrada = DateTime.Now,
                        ValorTotal = 36m,
                        ValorProdutos = 36m,
                        Fornecedor = new FornecedorNota
                        {
                            Nome = "Fornecedor Smoke Snapshot",
                            CNPJ = "99888777000166",
                            Telefone = "(11) 4444-0000"
                        },
                        Produtos = new List<ProdutoImportado>
                        {
                            new()
                            {
                                Codigo = produtoExistente.Codigo,
                                CodigoBarras = $"NEW-{token[^8..]}",
                                Nome = produtoExistente.Nome,
                                NCM = "85012000",
                                CFOP = "5405",
                                Quantidade = 3,
                                ValorUnitario = 12m,
                                ValorTotal = 36m,
                                UnidadeMedida = "PC",
                                Status = StatusImportacao.Atualizado,
                                ProdutoExistenteId = produtoExistente.Id,
                                SelecionadoParaImportacao = true,
                                AcaoPlanejada = AcoesPlanejadas.AtualizarExistente,
                                CategoriaSugerida = "Eletrica",
                                MargemAplicada = 150m,
                                PrecoVendaSugerido = 30m
                            }
                        }
                    };

                    var nfeService = new NFeService(App.Database);
                    var importada = nfeService.ImportarNotaPreparada(nota, Guid.NewGuid(), syntheticUser.Nome);
                    importacaoId = importada.Id;
                    if (importada.Status != StatusImportacaoNota.Concluida)
                    {
                        throw new InvalidOperationException($"Importacao de atualizacao nao concluiu: {importada.Status} - {importada.Erro}");
                    }

                    var itemImportado = importada.Produtos.Single();
                    if (itemImportado.Status != StatusImportacao.Atualizado ||
                        string.IsNullOrWhiteSpace(itemImportado.ProdutoSnapshotAnterior) ||
                        string.IsNullOrWhiteSpace(itemImportado.ProdutoSnapshotPosterior))
                    {
                        throw new InvalidOperationException("Importacao de atualizacao nao gravou snapshots anterior/posterior.");
                    }

                    var produtoAtualizado = App.Repositories.Produtos.ObterPorId(produtoExistente.Id)
                        ?? throw new InvalidOperationException("Produto atualizado nao foi localizado antes do rollback.");
                    if (produtoAtualizado.QuantidadeEstoque != 8 ||
                        produtoAtualizado.PrecoCompra != 12m ||
                        produtoAtualizado.PrecoVenda != 30m ||
                        !string.Equals(produtoAtualizado.CodigoBarras, $"NEW-{token[^8..]}", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Produto existente nao recebeu a atualizacao esperada pela NF-e.");
                    }

                    var totalRollbacksAntes = importacaoRepository.ObterTotalRollbacksAuditados();
                    var resultado = importacaoRepository.DesfazerProdutosDaImportacao(
                        importada.Id,
                        "Smoke Test",
                        "Validacao automatizada do rollback de atualizacao com snapshot.");
                    rollbackExecutado = true;

                    if (resultado.AtualizacoesRevertidas != 1 ||
                        resultado.ProdutosRemovidos != 0 ||
                        resultado.ProdutosBloqueados != 0 ||
                        resultado.AtualizacoesIgnoradas != 0)
                    {
                        throw new InvalidOperationException($"Rollback de atualizacao inesperado: restauradas={resultado.AtualizacoesRevertidas}; removidos={resultado.ProdutosRemovidos}; bloqueados={resultado.ProdutosBloqueados}; atualizacoesIgnoradas={resultado.AtualizacoesIgnoradas}.");
                    }

                    var produtoRestaurado = App.Repositories.Produtos.ObterPorId(produtoExistente.Id)
                        ?? throw new InvalidOperationException("Produto atualizado desapareceu apos rollback de snapshot.");
                    if (produtoRestaurado.QuantidadeEstoque != produtoExistente.QuantidadeEstoque ||
                        produtoRestaurado.PrecoCompra != produtoExistente.PrecoCompra ||
                        produtoRestaurado.PrecoVenda != produtoExistente.PrecoVenda ||
                        !string.Equals(produtoRestaurado.CodigoBarras, produtoExistente.CodigoBarras, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoRestaurado.NCMS, produtoExistente.NCMS, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoRestaurado.CFOP, produtoExistente.CFOP, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoRestaurado.UnidadeMedida, produtoExistente.UnidadeMedida, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Rollback de atualizacao nao restaurou os campos fiscais/comerciais esperados.");
                    }

                    if (importacaoRepository.ObterTotalRollbacksAuditados() <= totalRollbacksAntes)
                    {
                        throw new InvalidOperationException("Rollback de atualizacao nao gravou auditoria propria.");
                    }
                }
                finally
                {
                    if (rollbackExecutado && importacaoId != Guid.Empty && importacaoRepository.ObterImportacaoPorId(importacaoId) != null)
                    {
                        importacaoRepository.ExcluirImportacao(
                            importacaoId,
                            syntheticUser.Nome,
                            "Smoke snapshot: limpar historico apos rollback de atualizacao validado.");
                    }
                    else
                    {
                        LimparImportacaoNFeSeExistir(importacaoRepository, importacaoId, syntheticUser.Nome);
                    }

                    if (produtoInserido)
                    {
                        App.Repositories.Produtos.Excluir(produtoExistente.Id);
                    }
                }
            });
        }

        private void RunImportarNFeXmlRealRelancamentoChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "ImportarNFe:XmlRealExcluirRelancar", () =>
            {
                var token = DateTime.Now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var xmlPath = CriarXmlNFeRealTemporario(token, out var chaveAcesso);
                var nfeService = new NFeService(App.Database);
                var importacaoRepository = new ImportacaoRepository(App.Database);
                var primeiraImportacaoId = Guid.Empty;
                var relancamentoId = Guid.Empty;

                try
                {
                    var notaSimulada = nfeService.SimularImportacao(xmlPath);
                    if (notaSimulada.Status == StatusImportacaoNota.Erro ||
                        !string.Equals(notaSimulada.ChaveAcesso, chaveAcesso, StringComparison.OrdinalIgnoreCase) ||
                        notaSimulada.Produtos.Count == 0)
                    {
                        throw new InvalidOperationException($"XML real temporario nao foi preparado corretamente: {notaSimulada.Status} - {notaSimulada.Erro}");
                    }

                    if (notaSimulada.Produtos.Any(produto => produto.Status != StatusImportacao.Novo))
                    {
                        throw new InvalidOperationException("XML real temporario deveria preparar apenas produtos novos para permitir rollback completo.");
                    }

                    var exclusoesAntes = importacaoRepository.ObterTotalExclusoesAuditadas();
                    var rollbacksAntes = importacaoRepository.ObterTotalRollbacksAuditados();

                    var primeiraImportacao = nfeService.ImportarXml(xmlPath, Guid.NewGuid(), syntheticUser.Nome);
                    primeiraImportacaoId = primeiraImportacao.Id;
                    ValidarImportacaoNFeReal(primeiraImportacao, chaveAcesso, "primeira importacao");

                    var produtoIdsPrimeiraImportacao = ObterProdutoIdsCriados(primeiraImportacao);
                    var rollbackPrimeiro = importacaoRepository.DesfazerProdutosDaImportacao(
                        primeiraImportacao.Id,
                        syntheticUser.Nome,
                        "Smoke XML real: desfazer produtos antes de excluir historico e relancar.");

                    if (rollbackPrimeiro.ProdutosRemovidos != produtoIdsPrimeiraImportacao.Count)
                    {
                        throw new InvalidOperationException($"Rollback do XML real removeu {rollbackPrimeiro.ProdutosRemovidos} produto(s), esperado {produtoIdsPrimeiraImportacao.Count}.");
                    }

                    ValidarProdutosRemovidos(produtoIdsPrimeiraImportacao, "primeira importacao XML real");

                    importacaoRepository.ExcluirImportacao(
                        primeiraImportacao.Id,
                        syntheticUser.Nome,
                        "Smoke XML real: excluir historico para validar relancamento.");

                    if (importacaoRepository.ObterImportacaoPorId(primeiraImportacao.Id) != null)
                    {
                        throw new InvalidOperationException("Historico da primeira importacao XML real continuou visivel apos exclusao.");
                    }

                    var relancamento = nfeService.ImportarXml(xmlPath, Guid.NewGuid(), syntheticUser.Nome);
                    relancamentoId = relancamento.Id;
                    ValidarImportacaoNFeReal(relancamento, chaveAcesso, "relancamento");

                    var produtoIdsRelancamento = ObterProdutoIdsCriados(relancamento);
                    var rollbackRelancamento = importacaoRepository.DesfazerProdutosDaImportacao(
                        relancamento.Id,
                        syntheticUser.Nome,
                        "Smoke XML real: limpeza do relancamento validado.");

                    if (rollbackRelancamento.ProdutosRemovidos != produtoIdsRelancamento.Count)
                    {
                        throw new InvalidOperationException($"Rollback do relancamento XML real removeu {rollbackRelancamento.ProdutosRemovidos} produto(s), esperado {produtoIdsRelancamento.Count}.");
                    }

                    ValidarProdutosRemovidos(produtoIdsRelancamento, "relancamento XML real");

                    importacaoRepository.ExcluirImportacao(
                        relancamento.Id,
                        syntheticUser.Nome,
                        "Smoke XML real: limpeza apos relancamento validado.");

                    if (importacaoRepository.VerificarNotaDuplicada(chaveAcesso))
                    {
                        throw new InvalidOperationException("Chave do XML real continuou marcada como duplicada apos exclusao do relancamento.");
                    }

                    if (importacaoRepository.ObterTotalExclusoesAuditadas() < exclusoesAntes + 2)
                    {
                        throw new InvalidOperationException("Fluxo XML real nao registrou as duas exclusoes auditadas esperadas.");
                    }

                    if (importacaoRepository.ObterTotalRollbacksAuditados() < rollbacksAntes + 2)
                    {
                        throw new InvalidOperationException("Fluxo XML real nao registrou os dois rollbacks auditados esperados.");
                    }
                }
                finally
                {
                    LimparImportacaoNFeSeExistir(importacaoRepository, relancamentoId, syntheticUser.Nome);
                    LimparImportacaoNFeSeExistir(importacaoRepository, primeiraImportacaoId, syntheticUser.Nome);
                }
            });
        }

        private static void ValidarImportacaoNFeReal(NotaFiscalImportada importacao, string chaveAcesso, string etapa)
        {
            if (importacao.Status != StatusImportacaoNota.Concluida)
            {
                throw new InvalidOperationException($"A {etapa} do XML real nao concluiu: {importacao.Status} - {importacao.Erro}");
            }

            if (!string.Equals(importacao.ChaveAcesso, chaveAcesso, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"A {etapa} do XML real retornou chave divergente.");
            }

            if (importacao.Produtos.Count == 0 ||
                importacao.Produtos.Any(produto => produto.Status != StatusImportacao.Novo || produto.ProdutoExistenteId == null))
            {
                throw new InvalidOperationException($"A {etapa} do XML real nao criou todos os produtos esperados.");
            }
        }

        private static List<Guid> ObterProdutoIdsCriados(NotaFiscalImportada importacao)
        {
            return importacao.Produtos
                .Where(produto => produto.Status == StatusImportacao.Novo && produto.ProdutoExistenteId.HasValue)
                .Select(produto => produto.ProdutoExistenteId!.Value)
                .Distinct()
                .ToList();
        }

        private static void ValidarProdutosRemovidos(IEnumerable<Guid> produtoIds, string contexto)
        {
            foreach (var produtoId in produtoIds)
            {
                if (App.Repositories.Produtos.ObterPorId(produtoId) != null)
                {
                    throw new InvalidOperationException($"Produto {produtoId} da {contexto} continuou cadastrado apos rollback.");
                }
            }
        }

        private static void LimparImportacaoNFeSeExistir(ImportacaoRepository importacaoRepository, Guid importacaoId, string usuario)
        {
            if (importacaoId == Guid.Empty || importacaoRepository.ObterImportacaoPorId(importacaoId) == null)
            {
                return;
            }

            try
            {
                importacaoRepository.DesfazerProdutosDaImportacao(
                    importacaoId,
                    usuario,
                    "Limpeza defensiva do smoke XML real NF-e.");
            }
            catch (Exception ex)
            {
                App.Logger.LogWarning($"Limpeza defensiva do rollback NF-e ignorada para {importacaoId}: {ex.Message}");
            }

            try
            {
                importacaoRepository.ExcluirImportacao(
                    importacaoId,
                    usuario,
                    "Limpeza defensiva do smoke XML real NF-e.");
            }
            catch (Exception ex)
            {
                App.Logger.LogWarning($"Limpeza defensiva da exclusao NF-e ignorada para {importacaoId}: {ex.Message}");
            }
        }

        private static string CriarXmlNFeRealTemporario(string token, out string chaveAcesso)
        {
            var caminhoOriginal = ResolveProjectFile("Data", "NFeTeste.xml");
            var destinoDiretorio = Path.Combine(App.RuntimeLogDirectory, "nfe-smoke");
            Directory.CreateDirectory(destinoDiretorio);

            chaveAcesso = CriarChaveAcessoNFeSmoke(token);
            var documento = XDocument.Load(caminhoOriginal);
            XNamespace ns = "http://www.portalfiscal.inf.br/nfe";

            var infNFe = documento.Descendants(ns + "infNFe").FirstOrDefault()
                ?? throw new InvalidOperationException("XML real de NF-e nao possui tag infNFe.");
            infNFe.SetAttributeValue("Id", $"NFe{chaveAcesso}");

            var numeroNota = token.Length > 9 ? token[^9..] : token;
            documento.Descendants(ns + "nNF").FirstOrDefault()?.SetValue(numeroNota);
            documento.Descendants(ns + "cNF").FirstOrDefault()?.SetValue(numeroNota[^Math.Min(8, numeroNota.Length)..]);

            var item = 1;
            foreach (var produto in documento.Descendants(ns + "prod"))
            {
                var codigo = $"SMK-NFE-{token[^8..]}-{item:D2}";
                produto.Element(ns + "cProd")?.SetValue(codigo);
                produto.Element(ns + "cEAN")?.SetValue("SEM GTIN");
                produto.Element(ns + "xProd")?.SetValue($"Item Smoke NF-e Real {token[^8..]} {item:D2}");
                produto.Element(ns + "cEANTrib")?.SetValue("SEM GTIN");
                item++;
            }

            var destino = Path.Combine(destinoDiretorio, $"NFeTeste-smoke-{token}.xml");
            documento.Save(destino);
            return destino;
        }

        private static string CriarChaveAcessoNFeSmoke(string token)
        {
            var digits = new string(token.Where(char.IsDigit).ToArray());
            var baseDigits = $"43{digits}{DateTime.Now.Ticks}";
            return baseDigits.Length >= 44
                ? baseDigits[..44]
                : baseDigits.PadRight(44, '0');
        }

        private static string ResolveProjectFile(params string[] relativeParts)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Arquivo do projeto nao encontrado: {Path.Combine(relativeParts)}");
        }

        private void RunFuncionariosPermissoesAuditoriaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Funcionarios:AuditoriaProdutividadePermissoes", () =>
            {
                var funcionario = App.Repositories.Funcionarios.ObterPorId(syntheticUser.Id)
                    ?? throw new InvalidOperationException("Funcionario sintetico nao encontrado para validacao operacional.");

                App.Audit.Registrar(
                    categoria: "Funcionarios",
                    acao: "SmokeProdutividadeFuncionario",
                    entidade: "Funcionario",
                    entidadeId: funcionario.Id.ToString(),
                    detalhes: "Evento sintetico para validar painel operacional de funcionarios.");
                App.Audit.Registrar(
                    categoria: "Seguranca",
                    acao: "SmokePermissaoAcaoDetalhada",
                    entidade: "Funcionario",
                    entidadeId: funcionario.Id.ToString(),
                    detalhes: "Evento sintetico para validar auditoria e permissoes por acao.");

                var service = new FuncionarioOperationalService(App.Database);
                var painel = service.ObterPainelOperacional(funcionario);
                ValidarPainelOperacionalFuncionario(painel);

                var hostWindow = CreateHostWindow(new FuncionariosControl(), nameof(FuncionariosControl));
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not FuncionariosControl control)
                    {
                        throw new InvalidOperationException("Host de FuncionariosControl nao conseguiu carregar o controle.");
                    }

                    PrepareInteractiveSurface(control, typeof(FuncionariosControl));
                    var dataGrid = FindElementByName<DataGrid>(control, "FuncionariosDataGrid")
                        ?? throw new InvalidOperationException("Grade de funcionarios nao localizada.");
                    var funcionarioGrid = dataGrid.ItemsSource
                        ?.Cast<Funcionario>()
                        .FirstOrDefault(item => item.Id == funcionario.Id)
                        ?? dataGrid.ItemsSource?.Cast<Funcionario>().FirstOrDefault()
                        ?? throw new InvalidOperationException("Nenhum funcionario carregado na tela.");

                    dataGrid.SelectedItem = funcionarioGrid;
                    control.UpdateLayout();
                    PumpDispatcher();

                    if (control.UltimoPainelOperacional == null)
                    {
                        throw new InvalidOperationException("Painel operacional de funcionarios nao foi carregado na tela.");
                    }

                    ValidarPainelOperacionalFuncionario(control.UltimoPainelOperacional);

                    var produtividadeText = FindElementByName<TextBlock>(control, "FuncionarioProdutividadeResumoTextBlock")?.Text;
                    var permissoesText = FindElementByName<TextBlock>(control, "FuncionarioPermissoesAcoesTextBlock")?.Text;
                    if (string.IsNullOrWhiteSpace(produtividadeText) ||
                        string.IsNullOrWhiteSpace(permissoesText) ||
                        !permissoesText.Contains("Visualizar", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Painel visual de funcionarios nao exibiu produtividade/permissoes por acao.");
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

        private static void ValidarPainelOperacionalFuncionario(FuncionarioPainelOperacional painel)
        {
            if (painel.TotalAcoesAuditadas <= 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao encontrou acoes auditadas.");
            }

            if (painel.TotalPermissoes <= 0 || painel.ModulosLiberados.Count == 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao encontrou permissoes ativas do perfil.");
            }

            if (painel.PermissoesPorAcao.Count == 0 ||
                !painel.PermissoesPorAcao.Any(item => string.Equals(item.Acao, "Visualizar", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao consolidou permissoes por acao.");
            }

            if (painel.TotalPermissoesSensiveis <= 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao identificou permissoes sensiveis do perfil.");
            }

            if (string.IsNullOrWhiteSpace(painel.ProdutividadeResumo) ||
                string.IsNullOrWhiteSpace(painel.PermissoesResumo) ||
                string.IsNullOrWhiteSpace(painel.AcoesPermitidasResumo))
            {
                throw new InvalidOperationException("Painel operacional de funcionarios gerou resumos vazios.");
            }
        }

        private void RunConfiguracoesComerciaisBackupChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Configuracoes:ComercialBackupRestauracao", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var originalConfiguration = BusinessConfigurationService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
                var token = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var smokeDirectory = Path.Combine(App.RuntimeLogDirectory, "configuracoes-smoke");
                Directory.CreateDirectory(smokeDirectory);

                var logoPath = Path.Combine(smokeDirectory, $"logo-smoke-{token}.png");
                var bitmap = BitmapSource.Create(
                    1,
                    1,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null,
                    new byte[] { 0x1E, 0x8A, 0x5A, 0xFF },
                    4);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var logoStream = File.Create(logoPath))
                {
                    encoder.Save(logoStream);
                }

                var backupPath = Path.Combine(smokeDirectory, $"PrimoAutoEletrica_Backup_ConfigSmoke_{token}.db");
                ConfiguracoesSistemaWindow? window = null;

                try
                {
                    var smokeConfiguration = new BusinessConfiguration
                    {
                        CompanyDisplayName = "Smoke Auto Eletrica",
                        CompanyLegalName = "Smoke Auto Eletrica LTDA",
                        CompanyDocument = "12.345.678/0001-90",
                        CompanyPhone = "(11) 98888-0000",
                        CompanyAddress = "Rua Smoke Teste, 123",
                        LogoPath = logoPath,
                        ReceiptHeader = "Cabecalho smoke comprovante",
                        ReceiptFooter = "Rodape smoke comprovante"
                    };

                    BusinessConfigurationService.Save(App.RuntimeAppDataPath, smokeConfiguration);
                    var loadedConfiguration = BusinessConfigurationService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
                    if (!string.Equals(loadedConfiguration.CompanyDisplayName, smokeConfiguration.CompanyDisplayName, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Configuracao comercial nao foi persistida corretamente.");
                    }

                    var logoValidation = BusinessConfigurationService.ValidateLogoPath(loadedConfiguration.LogoPath);
                    if (!logoValidation.IsValid)
                    {
                        throw new InvalidOperationException($"Logo smoke nao foi validado: {logoValidation.Message}");
                    }

                    var comprovante = new VendaComprovanteService(loadedConfiguration).CriarDocumento(fixture.Venda);
                    var comprovanteTexto = new TextRange(comprovante.ContentStart, comprovante.ContentEnd).Text;
                    if (!comprovanteTexto.Contains("Smoke Auto Eletrica", StringComparison.Ordinal) ||
                        !comprovanteTexto.Contains("Rodape smoke comprovante", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Comprovante nao refletiu os dados comerciais configurados.");
                    }

                    App.Backups.CriarBackupManual(backupPath);
                    if (!App.Backups.VerificarBackup(backupPath))
                    {
                        throw new InvalidOperationException("Backup smoke nao passou na verificacao de integridade.");
                    }

                    window = new ConfiguracoesSistemaWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    SelectTabByHeader(window, "Comercial / Comprovante");

                    var companyText = FindElementByName<TextBox>(window, "CompanyDisplayNameTextBox")?.Text;
                    if (!string.Equals(companyText, smokeConfiguration.CompanyDisplayName, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Janela de configuracoes nao carregou o nome comercial persistido.");
                    }

                    SelectTabByHeader(window, "Backup");
                    SetTextBoxValue(window, "RestoreBackupPathTextBox", backupPath);
                    InvokeButtonHandler(window, "ValidarBackupRestauracaoButton_Click", null);

                    var restoreStatus = FindElementByName<TextBlock>(window, "RestoreBackupStatusTextBlock")?.Text;
                    if (string.IsNullOrWhiteSpace(restoreStatus) ||
                        !restoreStatus.Contains("Backup valido", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Janela de configuracoes nao validou o backup para restauracao segura.");
                    }

                    SelectTabByHeader(window, "Comercial / Comprovante");
                    InvokeButtonHandler(window, "GerarPreviaComprovanteButton_Click", null);
                    var previewText = FindElementByName<TextBlock>(window, "ReceiptPreviewTextBlock")?.Text;
                    if (string.IsNullOrWhiteSpace(previewText) ||
                        !previewText.Contains("Rodape smoke comprovante", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Previa do comprovante nao exibiu a configuracao comercial.");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }

                    BusinessConfigurationService.Save(App.RuntimeAppDataPath, originalConfiguration);
                }
            });
        }

        private void RunLoginSessaoSegurancaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "LoginSessao:MensagensLockoutLogoutPermissoes", () =>
            {
                var funcionarioLogin = EnsureFuncionario("smoke-login-seguranca@primoauto.com", "Smoke Login Seguranca", "Vendedor");
                var vendedor = EnsureFuncionario("smoke-vendedor@primoauto.com", "Smoke Vendedor", "Vendedor");
                var startedAt = DateTime.Now.AddSeconds(-2);
                MainWindow? mainWindow = null;

                try
                {
                    ResetLoginAttempts(funcionarioLogin.Id);
                    ValidarMensagensLogin();
                    ValidarBloqueioTemporarioLogin(funcionarioLogin);

                    ResetLoginAttempts(funcionarioLogin.Id);
                    var loginOk = App.Database.AutenticarFuncionarioDetalhado(funcionarioLogin.Email, "Workflow@123");
                    if (!loginOk.IsSuccess || loginOk.Funcionario == null)
                    {
                        throw new InvalidOperationException("Login valido nao foi aceito apos resetar tentativas de seguranca.");
                    }

                    App.Session.StartSession(funcionarioLogin);
                    var userSessionService = new UserSessionService(App.Database, _logger, App.Session);
                    userSessionService.CreateSession();
                    var sessionId = App.Session.SessionId;

                    if (GetUserSessionActiveFlag(sessionId) != 1)
                    {
                        throw new InvalidOperationException("Sessao de usuario nao foi registrada como ativa no banco.");
                    }

                    mainWindow = new MainWindow(funcionarioLogin);
                    ShowWindowForInteraction(mainWindow);
                    InvokeButtonHandler(mainWindow, "MenuSair_Click", null);

                    if (GetUserSessionActiveFlag(sessionId) != 0)
                    {
                        throw new InvalidOperationException("Logout pelo shell nao encerrou a sessao ativa no banco.");
                    }

                    FecharJanelasLoginAbertas();

                    App.Session.StartSession(vendedor);
                    var permissionService = new PermissionService(vendedor, _logger, App.Database);
                    if (permissionService.TemPermissao("Financeiro"))
                    {
                        throw new InvalidOperationException("Perfil Vendedor recebeu acesso indevido ao modulo Financeiro.");
                    }

                    if (!ExisteAuditoriaPermissaoNegadaDesde(startedAt, "Financeiro", "Vendedor"))
                    {
                        throw new InvalidOperationException("Permissao negada por perfil nao foi registrada na auditoria.");
                    }
                }
                finally
                {
                    ResetLoginAttempts(funcionarioLogin.Id);

                    if (mainWindow?.IsVisible == true)
                    {
                        mainWindow.Close();
                    }

                    FecharJanelasLoginAbertas();
                    App.Session.StartSession(syntheticUser);
                }
            });
        }

        private static void ValidarMensagensLogin()
        {
            var window = new LoginWindow();

            try
            {
                ShowWindowForInteraction(window);
                SetTextBoxValue(window, "EmailTextBox", string.Empty);
                SetPasswordBoxValue(window, "SenhaPasswordBox", string.Empty);
                SetTextBoxValue(window, "SenhaTextBox", string.Empty);

                ClickButton(window, "LoginButton");
                AssertLoginError(window, "Digite o e-mail.");

                SetTextBoxValue(window, "EmailTextBox", "smoke-login-seguranca@primoauto.com");
                SetPasswordBoxValue(window, "SenhaPasswordBox", string.Empty);
                SetTextBoxValue(window, "SenhaTextBox", string.Empty);

                ClickButton(window, "LoginButton");
                AssertLoginError(window, "Digite a senha.");
            }
            finally
            {
                if (window.IsVisible)
                {
                    window.Close();
                }
            }
        }

        private void ValidarBloqueioTemporarioLogin(Funcionario funcionario)
        {
            LoginAuthenticationResult ultimoResultado = LoginAuthenticationResult.Invalid("Nao executado.");

            for (var tentativa = 1; tentativa <= 5; tentativa++)
            {
                ultimoResultado = App.Database.AutenticarFuncionarioDetalhado(funcionario.Email, $"SenhaErrada{tentativa}");
            }

            if (!ultimoResultado.IsLocked ||
                !ultimoResultado.BloqueadoAte.HasValue ||
                !ultimoResultado.MensagemUsuario.Contains("temporariamente bloqueada", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Autenticacao nao bloqueou a conta apos tentativas invalidas.");
            }

            var loginDuranteBloqueio = App.Database.AutenticarFuncionarioDetalhado(funcionario.Email, "Workflow@123");
            if (!loginDuranteBloqueio.IsLocked)
            {
                throw new InvalidOperationException("Login correto foi aceito enquanto a conta estava temporariamente bloqueada.");
            }
        }

        private static void AssertLoginError(LoginWindow window, string expectedMessage)
        {
            var errorTextBlock = FindElementByName<TextBlock>(window, "ErrorMessageTextBlock")
                ?? throw new InvalidOperationException("Mensagem de erro do login nao foi localizada.");

            if (errorTextBlock.Visibility != Visibility.Visible ||
                !string.Equals(errorTextBlock.Text, expectedMessage, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Mensagem de login esperada '{expectedMessage}', obtida '{errorTextBlock.Text}'.");
            }

            if (errorTextBlock.Foreground is not SolidColorBrush brush ||
                brush.Color != Color.FromRgb(0xB9, 0x1C, 0x1C))
            {
                throw new InvalidOperationException("Contraste/cor da mensagem de erro do login nao corresponde ao padrao de alerta.");
            }
        }

        private static void SetPasswordBoxValue(DependencyObject root, string name, string value)
        {
            var passwordBox = FindElementByName<PasswordBox>(root, name)
                ?? throw new InvalidOperationException($"PasswordBox '{name}' nao foi localizado.");
            passwordBox.Password = value;
            PumpDispatcher();
        }

        private static void FecharJanelasLoginAbertas()
        {
            var loginWindows = Application.Current?.Windows
                .OfType<LoginWindow>()
                .ToList()
                ?? new List<LoginWindow>();

            foreach (var loginWindow in loginWindows)
            {
                if (loginWindow.IsVisible)
                {
                    loginWindow.Close();
                }
            }
        }

        private static void ResetLoginAttempts(int funcionarioId)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM LoginTentativasSeguranca
                WHERE FuncionarioId = @FuncionarioId;";
            command.Parameters.AddWithValue("@FuncionarioId", funcionarioId);
            command.ExecuteNonQuery();
        }

        private static int? GetUserSessionActiveFlag(Guid sessionId)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT IsActive
                FROM UserSessions
                WHERE SessionId = @SessionId
                ORDER BY LoginAt DESC
                LIMIT 1;";
            command.Parameters.AddWithValue("@SessionId", sessionId.ToString());

            var result = command.ExecuteScalar();
            return result == null || result == DBNull.Value
                ? null
                : Convert.ToInt32(result);
        }

        private static bool ExisteAuditoriaPermissaoNegadaDesde(DateTime startedAt, string modulo, string perfil)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(1)
                FROM AuditLogs
                WHERE Categoria = 'Seguranca'
                  AND Acao = 'PermissaoNegada'
                  AND Sucesso = 0
                  AND DataHora >= @StartedAt
                  AND COALESCE(Detalhes, '') LIKE @Modulo
                  AND COALESCE(Detalhes, '') LIKE @Perfil;";
            command.Parameters.AddWithValue("@StartedAt", startedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            command.Parameters.AddWithValue("@Modulo", $"%Alvo={modulo}%");
            command.Parameters.AddWithValue("@Perfil", $"%Perfil={perfil}%");

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private void RunClientesLgpdChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Clientes:LGPDAtalhosOperacionais", () =>
            {
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

                var control = new ClientesControl();
                PrepareElement(control);

                var orcamentoWindow = new NovoOrcamentoWindow(recarregado);
                try
                {
                    PrepareWindow(orcamentoWindow);
                }
                finally
                {
                    orcamentoWindow.Close();
                }
            });
        }

        private void RunClientesAnexosAssinaturaChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Clientes:AnexosAssinatura", () =>
            {
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
                    PrepareWindow(visualizarWindow);
                }
                finally
                {
                    visualizarWindow.Close();
                }

                var editarWindow = new EditarClienteWindow(recarregado);
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

        private void RunProdutosCamposAnexosChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Produtos:CamposAnexosOperacionais", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var produto = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)
                    ?? throw new InvalidOperationException("Produto sintetico nao encontrado para validacao de campos completos.");

                var fichaTecnica = CriarArquivoProdutoSmoke(
                    "ficha-tecnica",
                    $"Ficha tecnica sintetica do produto {produto.Id}.{Environment.NewLine}Material: Cobre estanhado.");
                var garantiaFornecedor = CriarArquivoProdutoSmoke(
                    "garantia-fornecedor",
                    $"Garantia sintetica vinculada ao produto {produto.Id}.{Environment.NewLine}Prazo: 90 dias.");

                produto.Cor = "Preto fosco";
                produto.Material = "Cobre estanhado";
                produto.Peso = "0,45 kg";
                produto.Dimensoes = "12x8x4 cm";
                produto.Anexos = ProdutoMediaService.PersistSelectedAttachments(
                    new[] { fichaTecnica, garantiaFornecedor },
                    produto.Id,
                    produto.Nome);

                App.Repositories.Produtos.Atualizar(produto);

                var recarregado = App.Repositories.Produtos.ObterPorId(produto.Id)
                    ?? throw new InvalidOperationException("Produto com campos completos nao foi recarregado.");

                if (!string.Equals(recarregado.Cor, "Preto fosco", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(recarregado.Material, "Cobre estanhado", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Cor e material do produto nao persistiram corretamente.");
                }

                var anexos = ProdutoMediaService.DeserializeAttachmentPaths(recarregado.Anexos);
                if (anexos.Count != 2 || anexos.Any(path => !File.Exists(path)))
                {
                    throw new InvalidOperationException($"Anexos do produto ficaram inconsistentes. Quantidade={anexos.Count}.");
                }

                if (anexos.Any(path => !ProdutoMediaService.IsSupportedAttachmentFile(path)))
                {
                    throw new InvalidOperationException("Um anexo persistido ficou com extensao nao suportada.");
                }

                var novoProdutoWindow = new NovoProdutoWindow();
                try
                {
                    PrepareWindow(novoProdutoWindow);
                }
                finally
                {
                    novoProdutoWindow.Close();
                }

                var editarProdutoWindow = new EditarProdutoWindow(recarregado);
                try
                {
                    PrepareWindow(editarProdutoWindow);
                }
                finally
                {
                    editarProdutoWindow.Close();
                }

                var estoqueControl = new EstoqueControl();
                PrepareElement(estoqueControl);
            });
        }

        private void RunFornecedoresProdutoFornecedorChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Fornecedores:ProdutoFornecedorComprasPrazosRanking", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var fornecedor = App.Repositories.Fornecedores.ObterPorId(fixture.Fornecedor.Id)
                    ?? throw new InvalidOperationException("Fornecedor sintetico nao encontrado para validacao operacional.");
                var produto = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)
                    ?? throw new InvalidOperationException("Produto sintetico nao encontrado para vinculo com fornecedor.");

                InserirImportacaoFornecedorProdutoSmoke(fornecedor, produto);

                var service = new FornecedorOperationalService(App.Database);
                var universo = App.Repositories.Fornecedores.ObterTodos();
                var insights = service.CriarInsights(fornecedor, universo);
                var vinculo = insights.ProdutoFornecedores.FirstOrDefault(item => item.ProdutoId == produto.Id)
                    ?? throw new InvalidOperationException("ProdutoFornecedor nao foi sincronizado para o fornecedor sintetico.");

                if (vinculo.QuantidadeCompras <= 0)
                {
                    throw new InvalidOperationException("ProdutoFornecedor nao registrou a quantidade de compras da NF-e sintetica.");
                }

                if (vinculo.ValorCompras <= 0 || vinculo.PrecoUltimaCompra <= 0)
                {
                    throw new InvalidOperationException("ProdutoFornecedor nao calculou valor/preco de compra da NF-e sintetica.");
                }

                if (vinculo.PrazoEntregaDias != fornecedor.PrazoMedioEntregaDias)
                {
                    throw new InvalidOperationException("Prazo operacional do ProdutoFornecedor divergiu do cadastro do fornecedor.");
                }

                if (insights.RankingGeral <= 0 || insights.QuantidadeComprasProdutoFornecedor <= 0)
                {
                    throw new InvalidOperationException("Ranking ou resumo de compras do fornecedor nao foi calculado.");
                }

                var visualizarWindow = new VisualizarFornecedorWindow(fornecedor);
                try
                {
                    PrepareWindow(visualizarWindow);
                }
                finally
                {
                    visualizarWindow.Close();
                }

                var fornecedoresControl = new FornecedoresControl();
                PrepareElement(fornecedoresControl);
            });
        }

        private static void InserirImportacaoFornecedorProdutoSmoke(Fornecedor fornecedor, Produto produto)
        {
            var token = DateTime.Now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            var importacaoId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var data = DateTime.Now;
            var quantidade = 3m;
            var valorUnitario = produto.PrecoCompra > 0 ? produto.PrecoCompra : 10m;
            var valorTotal = quantidade * valorUnitario;

            using var connection = App.Database.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO ImportacoesNFe
                    (
                        Id,
                        ChaveAcesso,
                        Numero,
                        Serie,
                        DataEmissao,
                        DataEntrada,
                        ValorTotal,
                        ValorProdutos,
                        Modelo,
                        FornecedorNome,
                        FornecedorCNPJ,
                        Status,
                        CaminhoArquivo,
                        Erro,
                        DataImportacao,
                        UsuarioNome
                    )
                    VALUES
                    (
                        @Id,
                        @ChaveAcesso,
                        @Numero,
                        @Serie,
                        @DataEmissao,
                        @DataEntrada,
                        @ValorTotal,
                        @ValorProdutos,
                        @Modelo,
                        @FornecedorNome,
                        @FornecedorCNPJ,
                        @Status,
                        @CaminhoArquivo,
                        @Erro,
                        @DataImportacao,
                        @UsuarioNome
                    );";
                command.Parameters.AddWithValue("@Id", importacaoId.ToString());
                command.Parameters.AddWithValue("@ChaveAcesso", $"SMOKE-FORNECEDOR-PF-{token}");
                command.Parameters.AddWithValue("@Numero", $"PF{token[^6..]}");
                command.Parameters.AddWithValue("@Serie", "1");
                command.Parameters.AddWithValue("@DataEmissao", data.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataEntrada", data.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@ValorTotal", valorTotal);
                command.Parameters.AddWithValue("@ValorProdutos", valorTotal);
                command.Parameters.AddWithValue("@Modelo", "55");
                command.Parameters.AddWithValue("@FornecedorNome", fornecedor.NomeFantasia);
                command.Parameters.AddWithValue("@FornecedorCNPJ", fornecedor.CNPJ);
                command.Parameters.AddWithValue("@Status", "Concluida");
                command.Parameters.AddWithValue("@CaminhoArquivo", $"smoke-produto-fornecedor-{token}.xml");
                command.Parameters.AddWithValue("@Erro", DBNull.Value);
                command.Parameters.AddWithValue("@DataImportacao", data.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@UsuarioNome", "Smoke Test");
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO ImportacoesItens
                    (
                        Id,
                        ImportacaoId,
                        Codigo,
                        Nome,
                        NCM,
                        CFOP,
                        Quantidade,
                        ValorUnitario,
                        ValorTotal,
                        UnidadeMedida,
                        Status,
                        ProdutoExistenteId,
                        MotivoIgnorado
                    )
                    VALUES
                    (
                        @Id,
                        @ImportacaoId,
                        @Codigo,
                        @Nome,
                        @NCM,
                        @CFOP,
                        @Quantidade,
                        @ValorUnitario,
                        @ValorTotal,
                        @UnidadeMedida,
                        @Status,
                        @ProdutoExistenteId,
                        @MotivoIgnorado
                    );";
                command.Parameters.AddWithValue("@Id", itemId.ToString());
                command.Parameters.AddWithValue("@ImportacaoId", importacaoId.ToString());
                command.Parameters.AddWithValue("@Codigo", produto.Codigo);
                command.Parameters.AddWithValue("@Nome", produto.Nome);
                command.Parameters.AddWithValue("@NCM", produto.NCMS);
                command.Parameters.AddWithValue("@CFOP", produto.CFOP);
                command.Parameters.AddWithValue("@Quantidade", quantidade);
                command.Parameters.AddWithValue("@ValorUnitario", valorUnitario);
                command.Parameters.AddWithValue("@ValorTotal", valorTotal);
                command.Parameters.AddWithValue("@UnidadeMedida", string.IsNullOrWhiteSpace(produto.UnidadeMedida) ? "UN" : produto.UnidadeMedida);
                command.Parameters.AddWithValue("@Status", "Importado");
                command.Parameters.AddWithValue("@ProdutoExistenteId", produto.Id.ToString());
                command.Parameters.AddWithValue("@MotivoIgnorado", DBNull.Value);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static string CriarArquivoClienteSmoke(string prefixo, string conteudo)
        {
            var pasta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "AutomatedTests",
                "ClientesAnexos");
            Directory.CreateDirectory(pasta);

            var caminho = Path.Combine(pasta, $"{prefixo}-{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.txt");
            File.WriteAllText(caminho, conteudo, Encoding.UTF8);
            return caminho;
        }

        private static string CriarArquivoProdutoSmoke(string prefixo, string conteudo)
        {
            var pasta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "AutomatedTests",
                "ProdutosAnexos");
            Directory.CreateDirectory(pasta);

            var caminho = Path.Combine(pasta, $"{prefixo}-{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.txt");
            File.WriteAllText(caminho, conteudo, Encoding.UTF8);
            return caminho;
        }

        private void RunVeiculosAlertasMidiaChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Veiculos:AlertasMidiaDocumentos", () =>
            {
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
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
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

        private void RunOrdensServicoMidiasChecklistFinanceiroChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "OrdensServico:MidiasChecklistFinanceiro", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var ordem = App.Repositories.OrdensServico.ObterPorId(fixture.OrdemServico.Id)
                    ?? throw new InvalidOperationException("OS sintetica nao encontrada para validacao operacional.");

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
                ordem.Status = "Entregue";

                App.Repositories.OrdensServico.Atualizar(ordem);

                var recarregada = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                    ?? throw new InvalidOperationException("OS com midias nao foi recarregada.");

                if (!string.Equals(recarregada.ChecklistSaida, ordem.ChecklistSaida, StringComparison.Ordinal) ||
                    string.Equals(recarregada.ChecklistSaida, recarregada.ChecklistEntrega, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Checklist de saida da OS nao persistiu separado do checklist de entrega.");
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

        private void RunOrcamentosConversoesPdfWhatsAppAlertasChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Orcamentos:ConversoesPdfWhatsAppAlertas", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var service = new OrcamentoDatabaseService();
                var orcamento = service.ObterOrcamentoPorId(fixture.Orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento sintetico nao encontrado para validacao operacional.");

                if (!OrcamentosControl.TryBuildWhatsAppShareUrl(orcamento, out var whatsappUrl, out var erroWhatsApp) ||
                    !whatsappUrl.StartsWith("https://wa.me/", StringComparison.OrdinalIgnoreCase) ||
                    !whatsappUrl.Contains(Uri.EscapeDataString(orcamento.Numero), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"URL de WhatsApp do orcamento nao foi montada corretamente: {erroWhatsApp}");
                }

                var pastaPdf = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PrimoAutoEletrica",
                    "AutomatedTests",
                    "Orcamentos");
                Directory.CreateDirectory(pastaPdf);
                var caminhoPdf = Path.Combine(pastaPdf, $"orcamento-{orcamento.Numero}-{Guid.NewGuid():N}.pdf");
                new OrcamentoPdfService().GerarPdfOrcamento(orcamento, caminhoPdf);
                if (!File.Exists(caminhoPdf) || new FileInfo(caminhoPdf).Length == 0)
                {
                    throw new InvalidOperationException("PDF sintetico do orcamento nao foi gerado corretamente.");
                }

                orcamento.Status = "Em Aberto";
                orcamento.DataValidade = DateTime.Today.AddDays(3);
                service.AtualizarOrcamento(orcamento);

                var alertaVm = new OrcamentosViewModel();
                var orcamentoComAlerta = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento com alerta nao foi recarregado.");
                alertaVm.SelecionarOrcamento(orcamentoComAlerta);
                if (!alertaVm.Alertas.Any(alerta => alerta.Contains("proximo do vencimento", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Alerta de vencimento do orcamento nao foi emitido no ViewModel.");
                }

                var conversaoOsVm = new OrcamentosViewModel();
                conversaoOsVm.AprovarOrcamento(orcamentoComAlerta);
                var aprovado = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento aprovado nao foi recarregado.");
                var ordem = conversaoOsVm.ConverterEmOrdemServico(aprovado);
                var convertidoEmOs = service.ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento convertido em OS nao foi recarregado.");

                if (!string.Equals(convertidoEmOs.Status, "Convertido em OS", StringComparison.OrdinalIgnoreCase) ||
                    !convertidoEmOs.OrdemServicoId.HasValue ||
                    !convertidoEmOs.DataConversaoOrdemServico.HasValue)
                {
                    throw new InvalidOperationException("Conversao do orcamento em OS nao persistiu status, data e vinculo.");
                }

                var ordemServicoId = convertidoEmOs.OrdemServicoId.GetValueOrDefault();
                var ordemPersistida = App.Repositories.OrdensServico.ObterPorId(ordemServicoId)
                    ?? throw new InvalidOperationException("OS gerada pelo orcamento nao foi localizada.");
                if (ordemPersistida.OrcamentoId != convertidoEmOs.Id ||
                    ordemPersistida.Itens.Count != convertidoEmOs.Itens.Count ||
                    !string.Equals(ordemPersistida.Origem, "Orcamento", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("OS gerada pelo orcamento nao manteve vinculo, origem ou itens esperados.");
                }

                var ordemReutilizada = conversaoOsVm.ConverterEmOrdemServico(convertidoEmOs);
                if (ordemReutilizada.Id != ordem.Id)
                {
                    throw new InvalidOperationException("Segunda conversao do mesmo orcamento criou uma OS duplicada.");
                }

                var orcamentoVenda = CreatePersistedOrcamento(fixture.Cliente, fixture.Produto);
                orcamentoVenda.CondicoesPagamento = "PIX";
                service.AtualizarOrcamento(orcamentoVenda);

                var conversaoVendaVm = new OrcamentosViewModel();
                var vendaAntes = service.ObterOrcamentoPorId(orcamentoVenda.Id)
                    ?? throw new InvalidOperationException("Orcamento para venda nao foi recarregado.");
                conversaoVendaVm.ConverterParaPDV(vendaAntes);

                var convertidoEmVenda = service.ObterOrcamentoPorId(orcamentoVenda.Id)
                    ?? throw new InvalidOperationException("Orcamento convertido em venda nao foi recarregado.");
                if (!string.Equals(convertidoEmVenda.Status, "Convertido em Venda", StringComparison.OrdinalIgnoreCase) ||
                    !convertidoEmVenda.DataConversaoVenda.HasValue)
                {
                    throw new InvalidOperationException("Conversao do orcamento em venda/PDV nao persistiu status e data.");
                }

                var contasReceber = new FinanceiroDatabaseService().ObterContasReceber();
                var contaIntegrada = contasReceber.Any(conta =>
                    string.Equals((string)conta.Origem, "OrcamentoContaReceber", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((string)conta.ReferenciaExterna, convertidoEmVenda.Id.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    (decimal)conta.Valor > 0);
                if (!contaIntegrada)
                {
                    throw new InvalidOperationException("Conversao do orcamento em venda nao integrou conta a receber ao financeiro.");
                }
            });
        }

        private void RunAgendamentosVisualizacoesConversoesChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Agendamentos:VisualizacoesFiltrosConversoes", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var hoje = DateTime.Today;
                var agendamentoHoje = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto, hoje, "Confirmado", "Urgente", $"AG-VIS-HOJE-{token}");
                var agendamentoSemana = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto, hoje.AddDays(3), "Agendado", "Normal", $"AG-VIS-SEM-{token}");
                var agendamentoMes = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto, hoje.AddDays(10), "Aguardando Cliente", "Alta", $"AG-VIS-MES-{token}");
                var agendamentoForaMes = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto, hoje.AddMonths(1).AddDays(3), "Confirmado", "Baixa", $"AG-VIS-FORA-{token}");

                var viewModel = new AgendamentosViewModel
                {
                    DataSelecionada = hoje
                };

                viewModel.VisualizacaoCalendario = "Diaria";
                ValidarAgendamentoPresente(viewModel, agendamentoHoje.Numero, "visao diaria");
                ValidarAgendamentoAusente(viewModel, agendamentoSemana.Numero, "visao diaria");

                viewModel.VisualizacaoCalendario = "Semanal";
                ValidarAgendamentoPresente(viewModel, agendamentoHoje.Numero, "visao semanal");
                ValidarAgendamentoPresente(viewModel, agendamentoSemana.Numero, "visao semanal");
                ValidarAgendamentoAusente(viewModel, agendamentoForaMes.Numero, "visao semanal");

                viewModel.VisualizacaoCalendario = "Mensal";
                ValidarAgendamentoPresente(viewModel, agendamentoHoje.Numero, "visao mensal");
                ValidarAgendamentoPresente(viewModel, agendamentoSemana.Numero, "visao mensal");
                ValidarAgendamentoPresente(viewModel, agendamentoMes.Numero, "visao mensal");
                ValidarAgendamentoAusente(viewModel, agendamentoForaMes.Numero, "visao mensal");

                viewModel.FiltroBusca = token;
                viewModel.FiltroStatus = "Confirmado";
                viewModel.FiltroPrioridade = "Urgente";
                viewModel.FiltroCliente = fixture.Cliente.Nome;
                viewModel.FiltroVeiculo = fixture.Veiculo.Placa;

                if (viewModel.AgendamentosFiltrados.Count != 1 ||
                    !string.Equals(viewModel.AgendamentosFiltrados[0].Numero, agendamentoHoje.Numero, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Filtros combinados de agendamentos nao localizaram apenas o atendimento esperado.");
                }

                var agendamentoService = new AgendamentoDatabaseService();
                var agendamentoParaOs = agendamentoService.ObterAgendamentoPorId(agendamentoSemana.Id)
                    ?? throw new InvalidOperationException("Agendamento para conversao em OS nao foi recarregado.");
                var ordem = agendamentoService.ConverterEmOrdemServico(agendamentoParaOs, "Smoke Test");
                var convertido = agendamentoService.ObterAgendamentoPorId(agendamentoSemana.Id)
                    ?? throw new InvalidOperationException("Agendamento convertido em OS nao foi recarregado.");

                if (convertido.OrdemServicoId != ordem.Id ||
                    !string.Equals(convertido.NumeroOS, ordem.Numero, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Conversao de agendamento em OS nao persistiu vinculo e numero da OS.");
                }

                var ordemReutilizada = agendamentoService.ConverterEmOrdemServico(convertido, "Smoke Test");
                if (ordemReutilizada.Id != ordem.Id)
                {
                    throw new InvalidOperationException("Segunda conversao do mesmo agendamento criou OS duplicada.");
                }

                var agendamentoParaOrcamento = agendamentoService.ObterAgendamentoPorId(agendamentoMes.Id)
                    ?? throw new InvalidOperationException("Agendamento para geracao de orcamento nao foi recarregado.");
                agendamentoParaOrcamento.CheckOut = DateTime.Now;
                agendamentoParaOrcamento.Status = "Finalizado";
                agendamentoService.AtualizarAgendamento(agendamentoParaOrcamento);

                var orcamentosAntes = new OrcamentoDatabaseService()
                    .ObterTodosOrcamentos()
                    .Count(o => o.Observacoes.Contains(agendamentoParaOrcamento.Numero, StringComparison.OrdinalIgnoreCase));
                var integracaoVm = new AgendamentosViewModel();
                integracaoVm.IntegrarComOrcamentos(agendamentoParaOrcamento);

                if (!agendamentoService.IntegracaoExecutada(agendamentoParaOrcamento.Id, "Orcamentos"))
                {
                    throw new InvalidOperationException("Integracao de agendamento com orcamentos nao foi registrada.");
                }

                var orcamentoService = new OrcamentoDatabaseService();
                var orcamentosGerados = orcamentoService
                    .ObterTodosOrcamentos()
                    .Where(o => o.Observacoes.Contains(agendamentoParaOrcamento.Numero, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (orcamentosGerados.Count <= orcamentosAntes ||
                    orcamentosGerados.All(o => o.Itens.Count == 0 || o.Total <= 0))
                {
                    throw new InvalidOperationException("Agendamento finalizado nao gerou orcamento operacional valido.");
                }

                integracaoVm.IntegrarComOrcamentos(agendamentoParaOrcamento);
                var totalDepoisReprocessamento = orcamentoService
                    .ObterTodosOrcamentos()
                    .Count(o => o.Observacoes.Contains(agendamentoParaOrcamento.Numero, StringComparison.OrdinalIgnoreCase));

                if (totalDepoisReprocessamento != orcamentosGerados.Count)
                {
                    throw new InvalidOperationException("Reprocessamento da integracao de orcamento gerou duplicidade.");
                }
            });
        }

        private static void ValidarAgendamentoPresente(AgendamentosViewModel viewModel, string numero, string contexto)
        {
            if (!viewModel.AgendamentosFiltrados.Any(a => string.Equals(a.Numero, numero, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Agendamento {numero} nao apareceu na {contexto}.");
            }
        }

        private static void ValidarAgendamentoAusente(AgendamentosViewModel viewModel, string numero, string contexto)
        {
            if (viewModel.AgendamentosFiltrados.Any(a => string.Equals(a.Numero, numero, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Agendamento {numero} apareceu indevidamente na {contexto}.");
            }
        }

        private void RunFinanceiroGraficosAlertasChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Financeiro:GraficosAlertasDivergencia", () =>
            {
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var financeiro = new FinanceiroDatabaseService();
                financeiro.AdicionarMovimentacao(
                    "Entrada",
                    $"Receita PIX Smoke {token}",
                    240m,
                    DateTime.Today,
                    "Smoke",
                    "PIX",
                    observacoes: "Validacao automatizada de grafico por forma de pagamento.",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"pix-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarMovimentacao(
                    "Entrada",
                    $"Receita sem forma Smoke {token}",
                    75m,
                    DateTime.Today,
                    "Smoke",
                    "",
                    observacoes: "Validacao automatizada de alerta de forma de pagamento.",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"sem-forma-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarMovimentacao(
                    "Saida",
                    $"Despesa Smoke {token}",
                    120m,
                    DateTime.Today,
                    "Smoke",
                    "PIX",
                    observacoes: "Validacao automatizada de fluxo diario.",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"saida-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarContaPagar(
                    $"Fornecedor Divergencia Smoke {token}",
                    $"Conta vencida smoke {token}",
                    2500m,
                    DateTime.Today.AddDays(-3),
                    "Smoke",
                    "Validacao automatizada de saldo projetado negativo.");
                financeiro.AdicionarContaReceber(
                    $"Cliente Inadimplente Smoke {token}",
                    $"Recebivel vencido smoke {token}",
                    150m,
                    DateTime.Today.AddDays(-2),
                    "Boleto",
                    "Validacao automatizada de inadimplencia.",
                    status: "Pendente",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"receber-vencido-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarContaReceber(
                    $"Cliente Pago Sem Data Smoke {token}",
                    $"Recebivel pago sem data smoke {token}",
                    90m,
                    DateTime.Today,
                    "PIX",
                    "Validacao automatizada de status sem data.",
                    status: "Pago",
                    dataPagamento: null,
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"pago-sem-data-{token}-{Guid.NewGuid():N}");

                var viewModel = new FinanceiroViewModel();
                if (viewModel.FluxoCaixaGrafico.Count < DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
                {
                    throw new InvalidOperationException("Grafico de fluxo de caixa nao carregou todos os dias do mes.");
                }

                var hojeGrafico = viewModel.FluxoCaixaGrafico.FirstOrDefault(item => item.Dia == DateTime.Today.ToString("dd"));
                if (hojeGrafico == null || hojeGrafico.Entradas <= 0 || hojeGrafico.Saidas <= 0)
                {
                    throw new InvalidOperationException("Grafico de fluxo de caixa nao refletiu entradas e saidas sinteticas do dia.");
                }

                if (!viewModel.FormasPagamentoGrafico.Any(item => string.Equals(item.Nome, "PIX", StringComparison.OrdinalIgnoreCase) && item.Valor > 0))
                {
                    throw new InvalidOperationException("Grafico de formas de pagamento nao carregou recebimentos PIX.");
                }

                if (string.IsNullOrWhiteSpace(viewModel.FluxoCaixaResumo) ||
                    string.IsNullOrWhiteSpace(viewModel.FormasPagamentoResumo) ||
                    viewModel.FluxoCaixaResumo.Contains("nao carregado", StringComparison.OrdinalIgnoreCase) ||
                    viewModel.FormasPagamentoResumo.Contains("nao carregadas", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Resumos dos graficos financeiros nao foram calculados.");
                }

                var tiposAlertas = viewModel.AlertasDivergencia
                    .Select(alerta => alerta.Tipo)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var alertaEsperado in new[]
                         {
                             "Contas a pagar vencidas",
                             "Inadimplencia",
                             "Status sem data",
                             "Saldo projetado negativo",
                             "Receita sem forma de pagamento"
                         })
                {
                    if (!tiposAlertas.Contains(alertaEsperado))
                    {
                        throw new InvalidOperationException($"Alerta financeiro esperado nao foi emitido: {alertaEsperado}.");
                    }
                }

                if (!string.Equals(viewModel.StatusFinanceiro, "Atencao", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Status financeiro nao refletiu alertas de alta severidade.");
                }

                var control = new FinanceiroControl();
                PrepareElement(control);
                if (control.ViewModel.AlertasDivergencia.Count == 0)
                {
                    throw new InvalidOperationException("FinanceiroControl nao expos alertas de divergencia na ViewModel.");
                }
            });
        }

        private void RunMainWindowNavigationChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunPreCheckSemModaisPresas(result, syntheticUser);

            RunCheck(result, "MainWindow:NavegacaoSequencial", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (!window.NavigateToModuleForAutomation("Clientes"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Clientes.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Clientes", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Modulo atual nao foi atualizado para Clientes.");
                    }

                    if (window.CurrentContentElement is not ClientesControl)
                    {
                        throw new InvalidOperationException("Conteudo atual nao corresponde ao controle de Clientes.");
                    }

                    if (!window.IsModuleHighlightedForAutomation("Clientes"))
                    {
                        throw new InvalidOperationException("Menu de Clientes nao ficou ativo apos a navegacao.");
                    }

                    if (!window.NavigateToModuleForAutomation("Estoque"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Estoque.");
                    }

                    if (!window.CanNavigateBack)
                    {
                        throw new InvalidOperationException("Historico de navegacao nao foi acumulado.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:VoltarHistorico", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    window.NavigateToModuleForAutomation("Clientes");
                    window.NavigateToModuleForAutomation("Estoque");

                    if (!window.NavigateBackForAutomation())
                    {
                        throw new InvalidOperationException("Falha ao voltar para o modulo anterior.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Clientes", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Volta de historico nao retornou para Clientes.");
                    }

                    if (window.CurrentContentElement is not ClientesControl)
                    {
                        throw new InvalidOperationException("Conteudo apos voltar nao corresponde ao controle esperado.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:AtualizarModulo", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    window.NavigateToModuleForAutomation("Clientes");
                    var firstInstance = window.CurrentContentElement
                        ?? throw new InvalidOperationException("Clientes nao foi carregado para o teste de refresh.");

                    if (!window.RefreshCurrentModuleForAutomation())
                    {
                        throw new InvalidOperationException("Refresh do modulo atual falhou.");
                    }

                    var refreshedInstance = window.CurrentContentElement
                        ?? throw new InvalidOperationException("Modulo nao permaneceu carregado apos o refresh.");

                    if (ReferenceEquals(firstInstance, refreshedInstance))
                    {
                        throw new InvalidOperationException("Refresh do modulo reutilizou a mesma instancia visual.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:PermissoesMenuVendedor", () =>
            {
                var vendedor = CreateSyntheticUser("Vendedor", "Smoke Vendedor");
                var window = new MainWindow(vendedor);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (!window.IsMenuEnabledForAutomation("Clientes"))
                    {
                        throw new InvalidOperationException("Menu de Clientes deveria estar habilitado para vendedor.");
                    }

                    if (window.IsMenuEnabledForAutomation("Financeiro"))
                    {
                        throw new InvalidOperationException("Menu Financeiro nao deveria estar habilitado para vendedor.");
                    }

                    if (window.IsMenuEnabledForAutomation("PDV"))
                    {
                        throw new InvalidOperationException("Menu PDV nao deveria estar habilitado para vendedor.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:BuscaGlobalNavegaModulo", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    var searchResult = new SearchResult
                    {
                        Titulo = "Estoque",
                        Subtitulo = "Modulo operacional de estoque",
                        Tipo = "Modulo",
                        Acao = "Estoque"
                    };

                    if (!window.NavigateFromSearchResultForAutomation(searchResult))
                    {
                        throw new InvalidOperationException("Busca global nao conseguiu navegar para o modulo esperado.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Estoque", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Busca global nao atualizou o modulo atual.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:ImportarNFeAutomacao", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (!window.OpenImportarNFeForAutomation())
                    {
                        throw new InvalidOperationException("A navegacao automatizada para a pagina de Importar NF-e falhou.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "ImportarNFe", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A pagina de Importar NF-e nao ficou ativa apos a navegacao automatizada.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:BuscaGlobalSobDemanda", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (window.IsGlobalSearchPrimedForAutomation())
                    {
                        throw new InvalidOperationException("A busca global nao deveria carregar o indice completo no startup.");
                    }

                    window.EnsureGlobalSearchReadyForAutomation();

                    if (!window.IsGlobalSearchPrimedForAutomation())
                    {
                        throw new InvalidOperationException("A busca global nao carregou o indice sob demanda.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:NotificacaoShellAcionavel", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    ShellNotificationService.PublishNavigationHint(
                        title: "Atalho de shell",
                        message: "Abrir Financeiro a partir do banner do shell.",
                        actionModule: "Financeiro",
                        actionLabel: "Abrir Financeiro",
                        source: "SmokeTest");

                    if (!window.HasShellNotificationForAutomation("Financeiro"))
                    {
                        throw new InvalidOperationException("O shell nao exibiu a notificacao acionavel esperada.");
                    }

                    if (!window.InvokeShellNotificationActionForAutomation())
                    {
                        throw new InvalidOperationException("A acao da notificacao do shell falhou.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Financeiro", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A notificacao do shell nao navegou para o modulo Financeiro.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:CruzadaAgendamentosParaOS", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    if (!window.NavigateToModuleForAutomation("Agendamentos"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Agendamentos.");
                    }

                    if (window.CurrentContentElement is not AgendamentosControl agendamentosControl)
                    {
                        throw new InvalidOperationException("O conteudo atual nao corresponde ao controle de Agendamentos.");
                    }

                    agendamentosControl.EmitirSugestaoOrdensServicoForAutomation();
                    if (!window.HasShellNotificationForAutomation("Ordens de Servico"))
                    {
                        throw new InvalidOperationException("A navegacao cruzada de Agendamentos nao apareceu no shell.");
                    }

                    if (!window.InvokeShellNotificationActionForAutomation())
                    {
                        throw new InvalidOperationException("A acao do shell para Ordens de Servico falhou.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "OrdensServico", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A acao cruzada nao levou ao modulo de Ordens de Servico.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:CruzadaOrcamentosParaFinanceiro", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    if (!window.NavigateToModuleForAutomation("Orcamentos"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Orcamentos.");
                    }

                    if (window.CurrentContentElement is not OrcamentosControl orcamentosControl)
                    {
                        throw new InvalidOperationException("O conteudo atual nao corresponde ao controle de Orcamentos.");
                    }

                    orcamentosControl.EmitirSugestaoFinanceiroForAutomation();
                    if (!window.HasShellNotificationForAutomation("Financeiro"))
                    {
                        throw new InvalidOperationException("A navegacao cruzada de Orcamentos nao apareceu no shell.");
                    }

                    if (!window.InvokeShellNotificationActionForAutomation())
                    {
                        throw new InvalidOperationException("A acao do shell para Financeiro falhou.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Financeiro", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A acao cruzada nao levou ao modulo Financeiro.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private void RunPreCheckSemModaisPresas(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "PreCheck:SemModaisPresasNavegacao", () =>
            {
                var window = new MainWindow(syntheticUser);
                var modulos = CoreModules
                    .Concat(new[] { "ImportarNFe", "Configuracoes" })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                try
                {
                    InitializeWindowForInteraction(window);
                    window.Show();
                    WaitForUiIdle();

                    foreach (var modulo in modulos)
                    {
                        var sucesso = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : string.Equals(modulo, "Configuracoes", StringComparison.OrdinalIgnoreCase)
                                ? window.OpenConfiguracoesForAutomation()
                                : window.NavigateToModuleForAutomation(modulo, forceReload: true);

                        if (!sucesso)
                        {
                            throw new InvalidOperationException($"Falha ao navegar para {modulo} no pre-check de modais.");
                        }

                        WaitForUiIdle();
                        var modaisVisiveis = ObterJanelasTransientesVisiveis(window);
                        if (modaisVisiveis.Count > 0)
                        {
                            throw new InvalidOperationException(
                                $"Modal/janela transiente permaneceu aberta apos navegar para {modulo}: {string.Join(", ", modaisVisiveis)}.");
                        }
                    }
                }
                finally
                {
                    CloseTransientWindows(window);
                    window.Close();
                }
            });
        }

        private void RunNavigationServiceRegressionChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "NavigationService:ModuloInexistente", () =>
            {
                var permissionService = new PermissionService(syntheticUser, _logger, App.Database);
                var navigationService = new NavigationService(permissionService, _logger);

                var control = navigationService.Navigate("ModuloInexistente");
                if (control != null)
                {
                    throw new InvalidOperationException("Servico de navegacao criou controle para um modulo inexistente.");
                }
            });

            RunCheck(result, "NavigationService:RefreshSemModulo", () =>
            {
                var permissionService = new PermissionService(syntheticUser, _logger, App.Database);
                var navigationService = new NavigationService(permissionService, _logger);

                var control = navigationService.RefreshCurrent();
                if (control != null)
                {
                    throw new InvalidOperationException("Refresh sem modulo atual deveria retornar nulo.");
                }
            });

            RunCheck(result, "NavigationService:PermissaoNegada", () =>
            {
                var vendedor = CreateSyntheticUser("Vendedor", "Vendedor Smoke");
                var permissionService = new PermissionService(vendedor, _logger, App.Database);
                var navigationService = new NavigationService(permissionService, _logger);

                var control = navigationService.Navigate("Financeiro");
                if (control != null)
                {
                    throw new InvalidOperationException("O servico de navegacao permitiu acesso indevido ao modulo Financeiro.");
                }
            });
        }

        private void RunRelatoriosOperationalChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Relatorios:CarregamentoAssincrono", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao sinalizou carga concluida.");

                if (string.IsNullOrWhiteSpace(control.ViewModel.ResumoAuditoria))
                {
                    throw new InvalidOperationException("A consulta operacional de auditoria nao retornou resumo visivel.");
                }
            });

            RunCheck(result, "Relatorios:AuditoriaPaginada", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao concluiu a carga inicial antes do filtro operacional.");

                control.ViewModel.StatusAuditoriaSelecionado = "Todos";
                control.ViewModel.CategoriaAuditoriaSelecionada = "Todas";
                control.ViewModel.TermoAuditoriaFiltro = "PDV";
                var filtroTask = control.ViewModel.AplicarFiltrosAuditoriaAsync();
                WaitForCondition(
                    () => filtroTask.IsCompleted && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "A consulta paginada de auditoria nao concluiu dentro do tempo esperado.");

                if (control.ViewModel.PaginaAuditoriaAtual < 1)
                {
                    throw new InvalidOperationException("A pagina atual da auditoria ficou invalida apos aplicar filtros.");
                }
            });

            RunCheck(result, "Relatorios:ConsistenciaOperacional", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao concluiu a carga inicial antes de expor a consistencia operacional.");

                if (string.IsNullOrWhiteSpace(control.ViewModel.ResumoConsistenciaOperacional))
                {
                    throw new InvalidOperationException("O resumo de consistencia operacional nao ficou visivel no workspace de relatorios.");
                }
            });

            RunCheck(result, "Relatorios:ExportacoesEvidencias", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao concluiu a carga inicial antes das exportacoes.");

                var pdfPath = control.ViewModel.ExportarPDF();
                var csvPath = control.ViewModel.ExportarExcel();
                var pacote = control.ViewModel.ExportarPacoteEvidencias();

                EnsureGeneratedFile(pdfPath, "PDF individual dos relatorios");
                EnsureGeneratedFile(csvPath, "CSV individual dos relatorios");
                EnsureGeneratedFile(pacote.PdfPath, "PDF do pacote de evidencias dos relatorios");
                EnsureGeneratedFile(pacote.CsvPath, "CSV do pacote de evidencias dos relatorios");
                EnsureGeneratedFile(pacote.ManifestoPath, "manifesto do pacote de evidencias dos relatorios");

                var manifesto = File.ReadAllText(pacote.ManifestoPath, Encoding.UTF8);
                if (!manifesto.Contains("[Totais]", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("Consistencia=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("Conciliacao=", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("O manifesto de evidencias dos relatorios nao contem os blocos operacionais esperados.");
                }

                if (!control.ViewModel.TemExportacaoGerada || string.IsNullOrWhiteSpace(control.ViewModel.ResumoUltimaExportacao))
                {
                    throw new InvalidOperationException("A tela de relatorios nao registrou a ultima exportacao gerada.");
                }
            });
        }

        private static void EnsureGeneratedFile(string path, string descricao)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || new FileInfo(path).Length == 0)
            {
                throw new InvalidOperationException($"{descricao} nao foi gerado corretamente em {path}.");
            }
        }

        private static void WaitForCondition(Func<bool> predicate, TimeSpan timeout, string failureMessage)
        {
            if (TryWaitForCondition(predicate, timeout))
            {
                return;
            }

            throw new InvalidOperationException(failureMessage);
        }

        private static bool TryWaitForCondition(Func<bool> predicate, TimeSpan timeout)
        {
            var startedAt = DateTime.UtcNow;
            while (DateTime.UtcNow - startedAt <= timeout)
            {
                if (predicate())
                {
                    return true;
                }

                PumpDispatcher();
                Thread.Sleep(25);
            }

            return false;
        }

        private static void PumpDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new DispatcherOperationCallback(_ =>
                {
                    frame.Continue = false;
                    return null;
                }),
                null);
            Dispatcher.PushFrame(frame);
        }

        private void RunDiscoveredWindows(UiSmokeTestRunResult result)
        {
            var excludedTypes = new HashSet<Type>
            {
                typeof(MainWindow),
                typeof(ImportarNotaWindow),
                typeof(ConfigurarPermissoesWindow),
                typeof(GerenciarPerfisWindow),
                typeof(EditarFornecedorWindow),
                typeof(EditarFuncionarioWindow),
                typeof(EditarClienteWindow),
                typeof(EditarProdutoWindow),
                typeof(HistoricoClienteWindow),
                typeof(NovoFuncionarioWindow),
                typeof(NovoOrcamentoWindow),
                typeof(NovoPerfilWindow),
                typeof(NovoVeiculoWindow),
                typeof(OrdemServicoWindow),
                typeof(SelecionarOrcamentoWindow),
                typeof(VisualizarFornecedorWindow),
                typeof(VisualizarVeiculoWindow),
                typeof(AdicionarFornecedorDialog)
            };

            var windowTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(Window).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null &&
                    !excludedTypes.Contains(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            foreach (var windowType in windowTypes)
            {
                RunCheck(result, $"Janela:{windowType.Name}", () =>
                {
                    if (Activator.CreateInstance(windowType) is not Window window)
                    {
                        throw new InvalidOperationException($"Falha ao instanciar a janela {windowType.FullName}.");
                    }

                    PrepareWindow(window);
                });
            }
        }

        private void RunDiscoveredUserControls(UiSmokeTestRunResult result)
        {
            var excludedTypes = new HashSet<Type>(CoreControlTypes);

            var controlTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(UserControl).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null &&
                    !excludedTypes.Contains(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            foreach (var controlType in controlTypes)
            {
                RunCheck(result, $"Controle:{controlType.Name}", () =>
                {
                    if (Activator.CreateInstance(controlType) is not FrameworkElement control)
                    {
                        throw new InvalidOperationException($"Falha ao instanciar o controle {controlType.FullName}.");
                    }

                    PrepareElement(control);
                });
            }
        }

        private void RunParameterizedWindows(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            foreach (var factory in BuildParameterizedWindowFactories(syntheticUser))
            {
                RunCheck(result, factory.Name, () =>
                {
                    var window = factory.Factory();
                    PrepareWindow(window);
                });
            }
        }

        private List<(string Name, Func<Window> Factory)> BuildParameterizedWindowFactories(Funcionario syntheticUser)
        {
            var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
            fixture.Produto.QuantidadeReservada = Math.Max(fixture.Produto.QuantidadeReservada, 2);

            var sampleHistoricoEstoque = new List<DadoAuditoria>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DataHora = DateTime.Now.AddMinutes(-20),
                    Usuario = syntheticUser.Nome,
                    Acao = "ReservaAgendamento",
                    Tabela = "Produto",
                    RegistroId = fixture.Produto.Id,
                    Categoria = "Estoque",
                    Severidade = "Info",
                    Sucesso = true,
                    ValorAnterior = "Estoque=12; Reservado=0; Disponivel=12",
                    ValorNovo = "Estoque=12; Reservado=2; Disponivel=10"
                }
            };

            return new List<(string Name, Func<Window> Factory)>
            {
                ("Janela:ConfigurarPermissoesWindow", () => new ConfigurarPermissoesWindow(syntheticUser)),
                ("Janela:GerenciarPerfisWindow", () => new GerenciarPerfisWindow(syntheticUser)),
                ("Janela:NovoFuncionarioWindow", () => new NovoFuncionarioWindow(syntheticUser)),
                ("Janela:NovoPerfilWindow", () => new NovoPerfilWindow(syntheticUser)),
                ("Janela:EditarFuncionarioWindow", () => new EditarFuncionarioWindow(syntheticUser, fixture.Funcionario)),
                ("Janela:EditarClienteWindow", () => new EditarClienteWindow(fixture.Cliente)),
                ("Janela:EditarProdutoWindow", () => new EditarProdutoWindow(fixture.Produto)),
                ("Janela:EditarFornecedorWindow", () => new EditarFornecedorWindow(fixture.Fornecedor)),
                ("Janela:HistoricoClienteWindow", () => new HistoricoClienteWindow(fixture.Cliente)),
                ("Janela:VisualizarFornecedorWindow", () => new VisualizarFornecedorWindow(fixture.Fornecedor)),
                ("Janela:VisualizarVeiculoWindow", () => new VisualizarVeiculoWindow(fixture.Veiculo, App.Database)),
                ("Janela:NovoVeiculoWindow", () => new NovoVeiculoWindow(App.Database, new Dictionary<Guid, Cliente>
                {
                    [fixture.Cliente.Id] = fixture.Cliente
                }, fixture.Veiculo, fixture.Cliente)),
                ("Janela:HistoricoEstoqueWindow", () => new HistoricoEstoqueWindow(fixture.Produto, sampleHistoricoEstoque)),
                ("Janela:OrdemServicoWindow", () => new OrdemServicoWindow(App.Database, null, null)),
                ("Janela:OperacaoCaixaWindow", () => new OperacaoCaixaWindow(new OperacaoCaixaRequest
                {
                    WindowTitle = "Operacao de caixa",
                    Header = "Fluxo visual padronizado",
                    Subheader = "Janela parametrizada usada pelo smoke test para validar o design system global.",
                    ValorLabel = "Valor operacional",
                    ObservacoesObrigatorias = true
                })),
                ("Janela:SelecionarOrcamentoWindow", () => new SelecionarOrcamentoWindow(new List<Orcamento> { fixture.Orcamento })),
                ("Janela:SelecionarVendaWindow", () => new SelecionarVendaWindow(new List<Venda> { fixture.Venda })),
                ("Janela:AdicionarFornecedorDialog", () => new AdicionarFornecedorDialog(fixture.Fornecedor.NomeFantasia))
            };
        }

        private void RunInteractiveMainWindowChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Interacao:MainWindow:Botoes", () =>
            {
                ExerciseWindowButtons(() => new MainWindow(syntheticUser), typeof(MainWindow));
            });
        }

        private void RunInteractiveCoreControlChecks(UiSmokeTestRunResult result)
        {
            foreach (var controlType in CoreControlTypes)
            {
                RunCheck(result, $"Interacao:Modulo:{controlType.Name}", () =>
                {
                    ExerciseHostedElementButtons(controlType);
                });
            }
        }

        private void RunInteractiveDiscoveredUserControlChecks(UiSmokeTestRunResult result)
        {
            var excludedTypes = new HashSet<Type>(CoreControlTypes);
            var controlTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(UserControl).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null &&
                    !excludedTypes.Contains(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            foreach (var controlType in controlTypes)
            {
                RunCheck(result, $"Interacao:Controle:{controlType.Name}", () =>
                {
                    ExerciseHostedElementButtons(controlType);
                });
            }
        }

        private void RunInteractiveWindowButtonChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            var discoveredExcludedTypes = new HashSet<Type>
            {
                typeof(MainWindow),
                typeof(ImportarNotaWindow),
                typeof(AjusteEstoqueWindow),
                typeof(ConfigurarPermissoesWindow),
                typeof(GerenciarPerfisWindow),
                typeof(EditarFornecedorWindow),
                typeof(EditarFuncionarioWindow),
                typeof(EditarClienteWindow),
                typeof(EditarProdutoWindow),
                typeof(HistoricoClienteWindow),
                typeof(NovoFuncionarioWindow),
                typeof(NovoOrcamentoWindow),
                typeof(NovoPerfilWindow),
                typeof(NovoVeiculoWindow),
                typeof(OrdemServicoWindow),
                typeof(SelecionarOrcamentoWindow),
                typeof(VisualizarFornecedorWindow),
                typeof(VisualizarVeiculoWindow),
                typeof(AdicionarFornecedorDialog)
            };

            var discoveredWindows = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(Window).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null &&
                    !discoveredExcludedTypes.Contains(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type => ($"Interacao:Janela:{type.Name}", WindowType: type, Factory: new Func<Window>(() => (Window)Activator.CreateInstance(type)!)))
                .ToList();

            var safeParameterizedFactories = BuildParameterizedWindowFactories(syntheticUser)
                .Where(factory => factory.Name is not "Janela:ConfigurarPermissoesWindow"
                    and not "Janela:GerenciarPerfisWindow"
                    and not "Janela:NovoPerfilWindow"
                    and not "Janela:NovoFuncionarioWindow"
                    and not "Janela:EditarFuncionarioWindow"
                    and not "Janela:NovoVeiculoWindow"
                    and not "Janela:OperacaoCaixaWindow"
                    and not "Janela:SelecionarOrcamentoWindow"
                    and not "Janela:SelecionarVendaWindow"
                    and not "Janela:AdicionarFornecedorDialog")
                .Select(factory =>
                {
                    var previewWindow = factory.Factory();
                    var windowType = previewWindow.GetType();
                    previewWindow.Close();
                    return ($"Interacao:{factory.Name}", WindowType: windowType, Factory: factory.Factory);
                })
                .ToList();

            foreach (var factory in discoveredWindows.Concat(safeParameterizedFactories))
            {
                RunCheck(result, factory.Item1, () =>
                {
                    ExerciseWindowButtons(factory.Factory, factory.WindowType);
                });
            }

            var statefulFactories = BuildParameterizedWindowFactories(syntheticUser)
                .Where(factory => factory.Name is "Janela:ConfigurarPermissoesWindow" or "Janela:GerenciarPerfisWindow")
                .Select(factory =>
                {
                    var previewWindow = factory.Factory();
                    var windowType = previewWindow.GetType();
                    previewWindow.Close();
                    return ($"Interacao:{factory.Name}", WindowType: windowType, Factory: factory.Factory);
                })
                .ToList();

            foreach (var factory in statefulFactories)
            {
                RunCheck(result, factory.Item1, () =>
                {
                    ExerciseWindowButtons(factory.Factory, factory.WindowType);
                });
            }
        }

        private void RunPdvOperationalInteractionChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "PDV:InteracaoCompletaTela", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var caixaService = new CaixaService(App.Database);
                var vendaService = new VendaService(App.Database);
                var hostWindow = new Window
                {
                    Content = new PDVControl(),
                    Title = "Smoke PDV Host"
                };
                AutomatedDialogSupervisor? supervisor = null;

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    var control = hostWindow.Content as PDVControl
                        ?? throw new InvalidOperationException("Host do PDV nao conseguiu carregar o controle principal.");
                    supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                    supervisor.Start();

                    WaitForCondition(
                        () => control.ViewModel.Produtos.Count > 0 && control.ViewModel.Clientes.Count > 0,
                        TimeSpan.FromSeconds(10),
                        "O PDV nao carregou produtos e clientes sinteticos para a simulacao.");

                    AdicionarProdutoAoCarrinhoParaAutomacao(control, fixture.Produto);
                    WaitForCondition(
                        () => control.ViewModel.Carrinho.Count > 0,
                        TimeSpan.FromSeconds(5),
                        "O PDV nao conseguiu montar um carrinho minimo antes de validar o bloqueio por caixa fechado.");

                    ClickButton(control, "PagamentoButton");
                    WaitForCondition(
                        () => !control.ViewModel.CaixaAberto && control.ViewModel.Carrinho.Count > 0,
                        TimeSpan.FromSeconds(5),
                        "O PDV nao preservou o carrinho ao bloquear o pagamento sem caixa aberto.");
                    RestoreWindowForInteraction(hostWindow);

                    ClickButton(control, "SuspenderVendaButton");
                    RestoreWindowForInteraction(hostWindow);
                    WaitForCondition(
                        () => control.ViewModel.Carrinho.Count == 0 &&
                              !control.ViewModel.VendasSuspensasResumo.StartsWith("Nenhuma", StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "O PDV nao suspendeu a venda atual corretamente.");
                    AssertWindowStillOperational(hostWindow, "suspensao de venda");

                    ClickButton(control, "RetomarVendaSuspensaButton");
                    RestoreWindowForInteraction(hostWindow);
                    WaitForCondition(
                        () => control.ViewModel.Carrinho.Count > 0 &&
                              control.ViewModel.VendasSuspensasResumo.StartsWith("Nenhuma", StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "O PDV nao retomou a venda suspensa corretamente.");
                    AssertWindowStillOperational(hostWindow, "retomada de venda suspensa");

                    ClickButton(control, "Abrir caixa");
                    WaitForCondition(
                        () => control.ViewModel.CaixaAberto,
                        TimeSpan.FromSeconds(10),
                        "O PDV nao abriu a sessao de caixa durante a simulacao.");
                    AssertWindowStillOperational(hostWindow, "abertura de caixa");

                    ClickButton(control, "Suprimento");
                    WaitForCondition(
                        () => control.ViewModel.SaldoCaixaAtual > 0,
                        TimeSpan.FromSeconds(10),
                        "O suprimento de caixa nao refletiu no saldo operacional.");
                    AssertWindowStillOperational(hostWindow, "suprimento");

                    ClickButton(control, "Sangria");
                    WaitForCondition(
                        () => control.ViewModel.CaixaAberto,
                        TimeSpan.FromSeconds(10),
                        "A sessao de caixa ficou inconsistente apos a sangria.");
                    AssertWindowStillOperational(hostWindow, "sangria");

                    control.ViewModel.ClienteSelecionado = fixture.Cliente;
                    SetTextBoxValue(control, "DescontoTextBox", "1,00");
                    ClickButton(control, "AplicarDescontoButton");
                    WaitForCondition(
                        () => control.ViewModel.Total > 0 && control.ViewModel.Carrinho.Count > 0,
                        TimeSpan.FromSeconds(5),
                        "O PDV nao montou o carrinho antes do pagamento.");

                    ClickButton(control, "MistoButton");
                    WaitForCondition(
                        () => string.Equals(control.ViewModel.FormaPagamentoSelecionada, "Misto", StringComparison.OrdinalIgnoreCase),
                        TimeSpan.FromSeconds(5),
                        "O PDV nao selecionou pagamento misto antes da finalizacao.");

                    var sessaoAntesPagamento = caixaService.ObterSessaoAbertaAtual();
                    var historicoAntesPagamento = vendaService.ObterHistoricoOperacional(
                        limite: 20,
                        caixaSessaoId: sessaoAntesPagamento?.Id,
                        inicio: DateTime.Now.AddMinutes(-10),
                        incluirCanceladas: true).Count;

                    ClickButton(control, "PagamentoButton");
                    WaitForCondition(
                        () =>
                        {
                            if (control.ViewModel.Carrinho.Count == 0 && control.ViewModel.UltimaVendaFinalizadaId.HasValue)
                            {
                                return true;
                            }

                            var historicoAtual = vendaService.ObterHistoricoOperacional(
                                limite: 20,
                                caixaSessaoId: sessaoAntesPagamento?.Id,
                                inicio: DateTime.Now.AddMinutes(-10),
                                incluirCanceladas: true).Count;

                            return historicoAtual > historicoAntesPagamento;
                        },
                        TimeSpan.FromSeconds(10),
                        $"O pagamento do PDV nao concluiu a venda esperada. " +
                        $"Carrinho={control.ViewModel.Carrinho.Count}; " +
                        $"UltimaVenda={control.ViewModel.UltimaVendaFinalizadaId?.ToString() ?? "null"}; " +
                        $"Subtotal={control.ViewModel.Subtotal:F2}; Total={control.ViewModel.Total:F2}; " +
                        $"CaixaAberto={control.ViewModel.CaixaAberto}; SaldoCaixa={control.ViewModel.SaldoCaixaAtual:F2}; " +
                        $"SessaoAntesPagamento={sessaoAntesPagamento?.Id.ToString() ?? "null"}; " +
                        $"HistoricoAntes={historicoAntesPagamento}; " +
                        $"HistoricoDepois={vendaService.ObterHistoricoOperacional(limite: 20, caixaSessaoId: sessaoAntesPagamento?.Id, inicio: DateTime.Now.AddMinutes(-10), incluirCanceladas: true).Count}.");
                    AssertWindowStillOperational(hostWindow, "finalizacao da venda");

                    ClickButton(control, "ReimprimirUltimaVendaButton");
                    AssertWindowStillOperational(hostWindow, "reimpressao");

                    ClickButton(control, "CancelarUltimaVendaConcluidaButton");
                    AssertWindowStillOperational(hostWindow, "cancelamento de venda concluida");

                    ClickButton(control, "Fechar caixa");
                    WaitForCondition(
                        () => !control.ViewModel.CaixaAberto,
                        TimeSpan.FromSeconds(10),
                        "O fechamento do caixa nao foi refletido no PDV.");
                    AssertWindowStillOperational(hostWindow, "fechamento de caixa");
                }
                finally
                {
                    supervisor?.Dispose();
                    CloseTransientWindows(hostWindow);
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });
        }

        private static void PrepareWindow(Window window)
        {
            try
            {
                ShowWindowForInteraction(window);

                if (window.Content is FrameworkElement content)
                {
                    PrepareElement(content);
                }

                window.UpdateLayout();
            }
            finally
            {
                TryCloseWindow(window, TimeSpan.FromSeconds(5));
            }
        }

        private static void InitializeWindowForInteraction(Window window)
        {
            window.Width = 1440;
            window.Height = 900;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = -10000;
            window.Top = -10000;
            window.ShowInTaskbar = false;
            window.ApplyTemplate();
            window.UpdateLayout();
        }

        private static void ShowWindowForInteraction(Window window)
        {
            InitializeWindowForInteraction(window);

            if (!window.IsVisible)
            {
                window.Show();
            }

            PumpDispatcher();

            // Aguarda a janela ser carregada e visivel com timeout curto
            var loaded = TryWaitForCondition(() => window.IsLoaded && window.IsVisible, TimeSpan.FromSeconds(5));
            if (!loaded)
            {
                try
                {
                    App.Logger.LogWarning($"Smoke test: janela {window.GetType().Name} nao ficou visivel apos 5s.");
                }
                catch { }
            }

            if (window.Content is FrameworkElement content)
            {
                try
                {
                    PrepareElement(content);
                }
                catch (InvalidOperationException)
                {
                    // Algumas views podem lançar durante preparacao; continuar tentando
                }
            }

            window.UpdateLayout();
            PumpDispatcher();
        }

        private static void RestoreWindowForInteraction(Window window)
        {
            if (window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished)
            {
                return;
            }

            try
            {
                if (!window.IsVisible)
                {
                    window.Show();
                }

                window.Activate();
                PumpDispatcher();

                if (window.Content is FrameworkElement content)
                {
                    PrepareElement(content);
                }

                window.UpdateLayout();
                PumpDispatcher();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void PrepareElement(FrameworkElement element)
        {
            element.ApplyTemplate();
            element.Measure(new Size(1440, 900));
            element.Arrange(new Rect(0, 0, 1440, 900));
            element.UpdateLayout();

            if (element is UserControl control && control.Content is FrameworkElement content)
            {
                content.ApplyTemplate();
                content.Measure(new Size(1440, 900));
                content.Arrange(new Rect(0, 0, 1440, 900));
                content.UpdateLayout();
            }
        }

        private void ExerciseHostedElementButtons(Type controlType)
        {
            if (controlType == typeof(VeiculosControl))
            {
                ExerciseVeiculosControlButtons();
                return;
            }

            if (controlType == typeof(EstoqueControl))
            {
                ExerciseEstoqueControlButtons();
                return;
            }

            if (controlType == typeof(FinanceiroControl))
            {
                ExerciseFinanceiroControlButtons();
                return;
            }

            if (controlType == typeof(AgendamentosControl))
            {
                ExerciseAgendamentosControlButtons();
                return;
            }

            if (controlType == typeof(RelatoriosControl))
            {
                ExerciseRelatoriosControlButtons();
                return;
            }

            ExerciseInteractionSurface(
                controlType,
                () =>
                {
                    if (Activator.CreateInstance(controlType) is not FrameworkElement root)
                    {
                        throw new InvalidOperationException($"Falha ao instanciar o controle {controlType.FullName}.");
                    }

                    return new InteractionSurface(CreateHostWindow(root, controlType.Name), root);
                });
        }

        private void ExerciseVeiculosControlButtons()
        {
            var hostWindow = CreateHostWindow(new VeiculosControl(), nameof(VeiculosControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not VeiculosControl control)
                {
                    throw new InvalidOperationException("Host de VeiculosControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(VeiculosControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                ClickButton(control, "NovoVeiculoButton");
                ClickButton(control, "ExportarVeiculosButton");
                ClickButton(control, "LimparFiltrosVeiculosButton");

                var dataGrid = FindElementByName<DataGrid>(control, "VeiculosDataGrid")
                    ?? throw new InvalidOperationException("VeiculosDataGrid nao foi localizado para a automacao dedicada.");

                WaitForCondition(
                    () => dataGrid.Items.Count > 0,
                    TimeSpan.FromSeconds(5),
                    "O modulo de veiculos nao carregou registros para exercitar as acoes por linha.");

                dataGrid.SelectedIndex = 0;
                if (dataGrid.SelectedItem != null)
                {
                    dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                    if (dataGrid.Columns.Count > 0)
                    {
                        dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.SelectedItem, dataGrid.Columns[0]);
                    }
                }

                WaitForUiIdle();

                InvokeButtonHandler(control, "VisualizarVeiculo_Click", dataGrid.SelectedItem);
                InvokeButtonHandler(control, "EditarVeiculo_Click", dataGrid.SelectedItem);
                InvokeButtonHandler(control, "ExcluirVeiculo_Click", dataGrid.SelectedItem);
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseEstoqueControlButtons()
        {
            var hostWindow = CreateHostWindow(new EstoqueControl(), nameof(EstoqueControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not EstoqueControl control)
                {
                    throw new InvalidOperationException("Host de EstoqueControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(EstoqueControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                ClickButton(control, "NovoProdutoButton");
                ClickButton(control, "AjustarEstoqueButton");

                var dataGrid = FindElementByName<DataGrid>(control, "ProdutosDataGrid")
                    ?? throw new InvalidOperationException("ProdutosDataGrid nao foi localizado para a automacao dedicada.");

                WaitForCondition(
                    () => dataGrid.Items.Count > 0,
                    TimeSpan.FromSeconds(5),
                    "O modulo de estoque nao carregou produtos para exercitar as acoes por linha.");

                var statusFiltro = FindElementByName<ComboBox>(control, "StatusFiltroComboBox")
                    ?? throw new InvalidOperationException("StatusFiltroComboBox nao foi localizado para validar filtros operacionais.");

                var statusDisponiveis = statusFiltro.Items.Cast<object?>()
                    .Select(item => item?.ToString() ?? string.Empty)
                    .ToList();

                if (!statusDisponiveis.Contains("Mais Vendidos") ||
                    !statusDisponiveis.Contains("Vendidos no Mes"))
                {
                    throw new InvalidOperationException("Filtros de ranking de vendas nao foram expostos no estoque.");
                }

                statusFiltro.SelectedItem = "Mais Vendidos";
                WaitForUiIdle();
                statusFiltro.SelectedItem = "Vendidos no Mes";
                WaitForUiIdle();
                statusFiltro.SelectedItem = "Todos";
                WaitForUiIdle();

                SelectFirstDataGridItem(dataGrid);

                ClickButton(control, "EntradaEstoqueButton");
                ClickButton(control, "SaidaEstoqueButton");
                ClickButton(control, "EtiquetaProdutoButton");
                ClickButton(control, "InventarioEstoqueButton");
                ClickButton(control, "HistoricoEstoqueHeaderButton");

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "VisualizarProdutoButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "EditarProdutoButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "AjustarEstoqueButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "InventariarProdutoButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "HistoricoEstoqueButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "ExcluirProdutoButton_Click", dataGrid.SelectedItem);
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseFinanceiroControlButtons()
        {
            var hostWindow = CreateHostWindow(new FinanceiroControl(), nameof(FinanceiroControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not FinanceiroControl control)
                {
                    throw new InvalidOperationException("Host de FinanceiroControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(FinanceiroControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                var contasPagarGrid = FindElementByName<DataGrid>(control, "ContasPagarDataGrid")
                    ?? throw new InvalidOperationException("ContasPagarDataGrid nao foi localizado para a automacao dedicada.");
                var contasReceberGrid = FindElementByName<DataGrid>(control, "ContasReceberDataGrid")
                    ?? throw new InvalidOperationException("ContasReceberDataGrid nao foi localizado para a automacao dedicada.");

                WaitForCondition(
                    () => contasPagarGrid.Items.Count > 0 && contasReceberGrid.Items.Count > 0,
                    TimeSpan.FromSeconds(10),
                    "O modulo financeiro nao carregou contas suficientes para exercitar as acoes operacionais.");

                InvokeButtonHandler(control, "AtualizarDadosButton_Click", null);
                InvokeButtonHandler(control, "ExportarRelatorioButton_Click", null);
                InvokeButtonHandler(control, "GerarPDFButton_Click", null);
                InvokeButtonHandler(control, "ImprimirButton_Click", null);
                InvokeButtonHandler(control, "FiltrosAvancadosButton_Click", null);

                InvokeButtonHandler(control, "FiltroContasPagarTodas_Click", null);
                InvokeButtonHandler(control, "FiltroContasPagarVencidas_Click", null);
                InvokeButtonHandler(control, "FiltroContasPagarHoje_Click", null);
                InvokeButtonHandler(control, "FiltroContasPagarSemana_Click", null);
                InvokeButtonHandler(control, "FiltroContasPagarTodas_Click", null);
                SelectFirstDataGridItem(contasPagarGrid);
                control.ViewModel.ContaPagarSelecionada = contasPagarGrid.SelectedItem as ContaPagar;
                InvokeButtonHandler(control, "BaixarContaPagarSelecionada_Click", null);

                InvokeButtonHandler(control, "FiltroContasReceberTodas_Click", null);
                InvokeButtonHandler(control, "FiltroContasReceberVencidas_Click", null);
                InvokeButtonHandler(control, "FiltroContasReceberHoje_Click", null);
                InvokeButtonHandler(control, "FiltroContasReceberSemana_Click", null);
                InvokeButtonHandler(control, "FiltroContasReceberTodas_Click", null);
                SelectFirstDataGridItem(contasReceberGrid);
                control.ViewModel.ContaReceberSelecionada = contasReceberGrid.SelectedItem as ContaReceber;
                InvokeButtonHandler(control, "BaixarContaReceberSelecionada_Click", null);
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseAgendamentosControlButtons()
        {
            var hostWindow = CreateHostWindow(new AgendamentosControl(), nameof(AgendamentosControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not AgendamentosControl control)
                {
                    throw new InvalidOperationException("Host de AgendamentosControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(AgendamentosControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                var viewModel = control.DataContext as AgendamentosViewModel
                    ?? throw new InvalidOperationException("AgendamentosControl nao expôs o ViewModel esperado.");
                var listView = FindElementByName<ListView>(control, "agendamentosListView")
                    ?? throw new InvalidOperationException("agendamentosListView nao foi localizado para a automacao dedicada.");
                var searchBox = FindElementByName<TextBox>(control, "searchBox");

                WaitForCondition(
                    () => listView.Items.Count > 0,
                    TimeSpan.FromSeconds(10),
                    "O modulo de agendamentos nao carregou itens suficientes para exercitar as acoes operacionais.");

                viewModel.AtualizarCommand.Execute(null);
                viewModel.NovoAgendamentoCommand.Execute(null);
                viewModel.EncaixeRapidoCommand.Execute(null);
                viewModel.ImprimirAgendaCommand.Execute(null);
                viewModel.ExportarPDFCommand.Execute(null);
                viewModel.FiltrosRapidosCommand.Execute(null);
                viewModel.ModoOficinaCommand.Execute(null);
                viewModel.LimparFiltrosCommand.Execute(null);

                if (searchBox != null)
                {
                    searchBox.Text = "SMOKE";
                    WaitForUiIdle();
                    searchBox.Text = string.Empty;
                    WaitForUiIdle();
                }

                SelectFirstListViewItem(listView);
                viewModel.AgendamentoSelecionado = listView.SelectedItem as Agendamento;

                viewModel.EditarCommand.Execute(null);
                viewModel.ReagendarCommand.Execute(null);
                viewModel.DuplicarCommand.Execute(null);
                viewModel.GerarOSCommand.Execute(null);
                viewModel.CheckInCommand.Execute(null);
                viewModel.EnviarWhatsAppCommand.Execute(null);
                viewModel.CheckOutCommand.Execute(null);
                viewModel.CancelarCommand.Execute(null);
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseRelatoriosControlButtons()
        {
            var hostWindow = CreateHostWindow(new RelatoriosControl(), nameof(RelatoriosControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not RelatoriosControl control)
                {
                    throw new InvalidOperationException("Host de RelatoriosControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(RelatoriosControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                WaitForCondition(
                    () => control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "O modulo de relatorios nao concluiu a carga inicial para a automacao dedicada.");

                AwaitUiTask(
                    control.ViewModel.CarregarDadosAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "A atualizacao dedicada de relatorios nao concluiu a carga esperada.");
                control.ViewModel.ExportarPDF();
                control.ViewModel.ExportarExcel();
                control.ViewModel.ExportarPacoteEvidencias();
                InvokeButtonHandler(control, "Imprimir_Click", null);
                control.ViewModel.AlternarFavorito();
                control.ViewModel.SalvarWorkspaceAtual();
                control.ViewModel.AlternarModoExecutivo();
                InvokeButtonHandler(control, "TelaCheia_Click", null);
                InvokeButtonHandler(control, "TelaCheia_Click", null);

                control.ViewModel.FiltroOperador = "Smoke";
                control.ViewModel.FiltroVendedor = "Administrador";
                AwaitUiTask(
                    control.ViewModel.AplicarFiltrosAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "A aplicacao de filtros dos relatorios nao concluiu no tempo esperado.");
                AwaitUiTask(
                    control.ViewModel.LimparFiltrosAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "A limpeza de filtros dos relatorios nao concluiu no tempo esperado.");

                control.ViewModel.SeveridadeAuditoriaSelecionada = "Info";
                control.ViewModel.StatusAuditoriaSelecionado = "Todos";
                control.ViewModel.UsuarioAuditoriaFiltro = string.Empty;
                control.ViewModel.TermoAuditoriaFiltro = "PDV";
                AwaitUiTask(
                    control.ViewModel.AplicarFiltrosAuditoriaAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "A consulta operacional da auditoria nao concluiu no tempo esperado.");
                AwaitUiTask(
                    control.ViewModel.AvancarPaginaAuditoriaAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "O avancar de pagina da auditoria nao concluiu no tempo esperado.");
                AwaitUiTask(
                    control.ViewModel.RetrocederPaginaAuditoriaAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "O retorno de pagina da auditoria nao concluiu no tempo esperado.");
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseWindowButtons(Func<Window> factory, Type rootType)
        {
            ExerciseInteractionSurface(
                rootType,
                () =>
                {
                    var window = factory();
                    return new InteractionSurface(window, window);
                });
        }

        private void ExerciseInteractionSurface(Type rootType, Func<InteractionSurface> surfaceFactory)
        {
            var descriptors = CaptureButtonDescriptors(rootType, surfaceFactory);
            if (descriptors.Count == 0)
            {
                _logger.LogInfo($"Nenhum botao interativo encontrado em {rootType.Name}; a validacao ficou restrita ao carregamento visual.");
                return;
            }

            foreach (var descriptor in descriptors)
            {
                AutomatedDialogSupervisor? supervisor = null;
                InteractionSurface? surface = null;

                try
                {
                    surface = surfaceFactory();
                    ShowWindowForInteraction(surface.HostWindow);
                    PrepareInteractiveSurface(surface.Root, rootType);
                    var searchRoot = ResolveInteractiveSearchRoot(surface);
                    PrepareButtonsForInteraction(searchRoot);

                    WaitForCondition(
                        () => FindVisualChildren<Button>(searchRoot).Any(IsButtonDiscoverable),
                        TimeSpan.FromSeconds(5),
                        $"Nenhum botao visivel foi carregado em {rootType.Name}.");

                    var targetButton = FindButtonByDescriptor(searchRoot, descriptor)
                        ?? throw new InvalidOperationException(
                            $"Botao '{descriptor.DisplayName}' nao foi reencontrado em {rootType.Name}. " +
                            $"Disponiveis agora: {DescribeVisibleButtons(searchRoot)}");

                    var buttonEnabled = TryWaitForCondition(
                        () =>
                        {
                            if (targetButton.IsEnabled)
                            {
                                return true;
                            }

                            PrepareButtonsForInteraction(searchRoot);
                            targetButton = FindButtonByDescriptor(searchRoot, descriptor) ?? targetButton;
                            return targetButton.IsEnabled;
                        },
                        TimeSpan.FromSeconds(5));

                    if (!buttonEnabled && !CanForceDisabledAutomationClick(targetButton))
                    {
                        throw new InvalidOperationException($"Botao '{descriptor.DisplayName}' permaneceu desabilitado em {rootType.Name}.");
                    }

                    supervisor = new AutomatedDialogSupervisor(surface.HostWindow, _fixture);
                    supervisor.Start();

                    if (!targetButton.IsEnabled)
                    {
                        _logger.LogWarning($"Smoke test forcou clique automatizado em botao desabilitado '{descriptor.DisplayName}' de {rootType.Name}.");
                    }

                    RaiseButtonClick(targetButton);
                    WaitForUiIdle();
                }
                finally
                {
                    supervisor?.Dispose();

                    if (surface != null)
                    {
                        CloseTransientWindows(surface.HostWindow);
                        CloseSurface(surface);
                    }
                }
            }
        }

        private List<ButtonDescriptor> CaptureButtonDescriptors(Type rootType, Func<InteractionSurface> surfaceFactory)
        {
            AutomatedDialogSupervisor? supervisor = null;
            InteractionSurface? surface = null;

            try
            {
                surface = surfaceFactory();
                ShowWindowForInteraction(surface.HostWindow);
                PrepareInteractiveSurface(surface.Root, rootType);
                var searchRoot = ResolveInteractiveSearchRoot(surface);
                PrepareButtonsForInteraction(searchRoot);

                supervisor = new AutomatedDialogSupervisor(surface.HostWindow, _fixture);
                supervisor.Start();

                var hasButtons = TryWaitForCondition(
                    () => FindVisualChildren<Button>(searchRoot).Any(IsButtonDiscoverable),
                    TimeSpan.FromSeconds(5));

                if (!hasButtons)
                {
                    return new List<ButtonDescriptor>();
                }

                return FindCandidateButtons(searchRoot, rootType)
                    .Select((button, index) => new ButtonDescriptor(index, button.Name, ExtractButtonText(button)))
                    .ToList();
            }
            finally
            {
                supervisor?.Dispose();

                if (surface != null)
                {
                    CloseTransientWindows(surface.HostWindow);
                    CloseSurface(surface);
                }
            }
        }

        private static Window CreateHostWindow(FrameworkElement content, string title)
        {
            return new Window
            {
                Title = title,
                Content = content
            };
        }

        private static void CloseSurface(InteractionSurface surface)
        {
            if (surface.HostWindow.IsVisible)
            {
                surface.HostWindow.Close();
            }
        }

        private static DependencyObject ResolveInteractiveSearchRoot(InteractionSurface surface)
        {
            if (surface.Root is Window windowRoot && windowRoot.Content is DependencyObject windowContent)
            {
                return windowContent;
            }

            return surface.Root;
        }

        private void PrepareInteractiveSurface(FrameworkElement root, Type rootType)
        {
            if (root is Window windowRoot)
            {
                if (windowRoot.Content is FrameworkElement content)
                {
                    PrepareElement(content);
                }
            }
            else
            {
                PrepareElement(root);
            }

            AutoPopulateInteractiveInputs(root, rootType);
            PrimeSelectors(root);

            if (root is Window refreshedWindowRoot)
            {
                if (refreshedWindowRoot.Content is FrameworkElement refreshedContent)
                {
                    PrepareElement(refreshedContent);
                }
            }
            else
            {
                PrepareElement(root);
            }

            PumpDispatcher();
        }

        private void AutoPopulateInteractiveInputs(DependencyObject root, Type rootType)
        {
            var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

            foreach (var textBox in FindVisualChildren<TextBox>(root))
            {
                if (!textBox.IsEnabled || textBox.IsReadOnly)
                {
                    continue;
                }

                if (ShouldSkipAutoFill(textBox.Name))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(textBox.Text) && rootType != typeof(LoginWindow))
                {
                    continue;
                }

                textBox.Text = BuildTextBoxValue(rootType, textBox.Name, token);
            }

            foreach (var passwordBox in FindVisualChildren<PasswordBox>(root))
            {
                if (!passwordBox.IsEnabled || !string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    continue;
                }

                passwordBox.Password = "Workflow@123";
            }

            foreach (var comboBox in FindVisualChildren<ComboBox>(root))
            {
                if (!comboBox.IsEnabled || comboBox.SelectedItem != null || comboBox.Items.Count == 0)
                {
                    continue;
                }

                comboBox.SelectedIndex = 0;
            }

            foreach (var datePicker in FindVisualChildren<DatePicker>(root))
            {
                if (!datePicker.IsEnabled || datePicker.SelectedDate.HasValue)
                {
                    continue;
                }

                datePicker.SelectedDate = DateTime.Today;
            }
        }

        private static void PrimeSelectors(DependencyObject root)
        {
            foreach (var dataGrid in FindVisualChildren<DataGrid>(root))
            {
                if (!dataGrid.IsEnabled || dataGrid.Items.Count == 0)
                {
                    continue;
                }

                if (dataGrid.SelectedIndex < 0)
                {
                    dataGrid.SelectedIndex = 0;
                }

                if (dataGrid.SelectedItem != null)
                {
                    dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                }

                if (dataGrid.Columns.Count > 0 && dataGrid.SelectedIndex >= 0)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.SelectedItem, dataGrid.Columns[0]);
                }
            }

            foreach (var listBox in FindVisualChildren<ListBox>(root))
            {
                if (!listBox.IsEnabled || listBox.Items.Count == 0)
                {
                    continue;
                }

                if (listBox.SelectedIndex < 0)
                {
                    listBox.SelectedIndex = 0;
                }

                if (listBox.SelectedItem != null)
                {
                    listBox.ScrollIntoView(listBox.SelectedItem);
                }
            }

            foreach (var listView in FindVisualChildren<ListView>(root))
            {
                if (!listView.IsEnabled || listView.Items.Count == 0)
                {
                    continue;
                }

                if (listView.SelectedIndex < 0)
                {
                    listView.SelectedIndex = 0;
                }

                if (listView.SelectedItem != null)
                {
                    listView.ScrollIntoView(listView.SelectedItem);
                }
            }

            foreach (var tabControl in FindVisualChildren<TabControl>(root))
            {
                if (tabControl.Items.Count > 0 && tabControl.SelectedIndex < 0)
                {
                    tabControl.SelectedIndex = 0;
                }
            }
        }

        private static void PrepareButtonsForInteraction(DependencyObject root)
        {
            PrimeSelectors(root);
            PumpDispatcher();
        }

        private string BuildTextBoxValue(Type rootType, string name, string token)
        {
            var normalizedName = name?.Trim() ?? string.Empty;
            var lowered = normalizedName.ToLowerInvariant();

            if (rootType == typeof(LoginWindow))
            {
                if (string.Equals(normalizedName, "EmailTextBox", StringComparison.Ordinal))
                {
                    return _fixture?.Administrator.Email ?? "smoke-admin@primoauto.com";
                }

                if (string.Equals(normalizedName, "SenhaTextBox", StringComparison.Ordinal))
                {
                    return "Workflow@123";
                }
            }

            if (lowered.Contains("email"))
            {
                return $"smoke.{token}@primoauto.com";
            }

            if (lowered.Contains("cpf"))
            {
                return GerarCpfValido(token);
            }

            if (lowered.Contains("cnpj"))
            {
                return GerarCnpjValido(token);
            }

            if (lowered.Contains("placa"))
            {
                return GerarPlacaValida(token);
            }

            if (lowered.Contains("cep"))
            {
                return "01001000";
            }

            if (lowered.Contains("telefone") || lowered.Contains("celular") || lowered.Contains("whatsapp"))
            {
                return "(11) 98888-0000";
            }

            if (lowered.Contains("senha"))
            {
                return "Workflow@123";
            }

            if (lowered.Contains("valor") || lowered.Contains("preco") || lowered.Contains("desconto") || lowered.Contains("salario") || lowered.Contains("total"))
            {
                return "10,00";
            }

            if (lowered.Contains("quantidade") || lowered.Contains("qtd") || lowered.Contains("estoque") || lowered.Contains("numero") || lowered.Contains("nota"))
            {
                return "1";
            }

            if (lowered.Contains("ano"))
            {
                return DateTime.Today.Year.ToString();
            }

            if (lowered.Contains("codigo") || lowered.Contains("sku"))
            {
                return $"SMK-{token}";
            }

            if (lowered.Contains("chassi"))
            {
                return $"9BWZZZ377VT{token.PadLeft(9, '0')[..9]}";
            }

            if (lowered.Contains("renavam"))
            {
                return token.PadLeft(11, '0')[..11];
            }

            if (lowered.Contains("razao") || lowered.Contains("fantasia") || lowered.Contains("nome"))
            {
                return $"Smoke {token}";
            }

            if (lowered.Contains("rua") || lowered.Contains("logradouro") || lowered.Contains("endereco"))
            {
                return "Rua Smoke Teste";
            }

            if (lowered.Contains("bairro"))
            {
                return "Centro";
            }

            if (lowered.Contains("cidade"))
            {
                return "Sao Paulo";
            }

            if (lowered.Contains("estado") || lowered.Contains("uf"))
            {
                return "SP";
            }

            if (lowered.Contains("observ") || lowered.Contains("descricao"))
            {
                return "Registro sintetico do smoke test.";
            }

            return $"Smoke {token}";
        }

        private static bool ShouldSkipAutoFill(string? name)
        {
            var lowered = name?.Trim().ToLowerInvariant() ?? string.Empty;
            return lowered.Contains("busca") || lowered.Contains("filtro") || lowered.Contains("search") || lowered.Contains("termo");
        }

        private static List<Button> FindCandidateButtons(DependencyObject root, Type rootType)
        {
            return FindVisualChildren<Button>(root)
                .Where(button =>
                    IsButtonDiscoverable(button) &&
                    button.IsEnabled &&
                    !ShouldSkipButton(rootType, button.Name, ExtractButtonText(button)))
                .ToList();
        }

        private static Button? FindButtonByDescriptor(DependencyObject root, ButtonDescriptor descriptor)
        {
            var buttons = FindVisualChildren<Button>(root)
                .Where(IsButtonDiscoverable)
                .ToList();

            if (!string.IsNullOrWhiteSpace(descriptor.Name))
            {
                var named = PreferEnabledButton(buttons.Where(button =>
                    string.Equals(button.Name, descriptor.Name, StringComparison.OrdinalIgnoreCase)));
                if (named != null)
                {
                    return named;
                }
            }

            if (!string.IsNullOrWhiteSpace(descriptor.Text))
            {
                var textMatch = PreferEnabledButton(buttons.Where(button =>
                {
                    var currentText = ExtractButtonText(button);
                    return string.Equals(currentText, descriptor.Text, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(currentText) &&
                            currentText.Contains(descriptor.Text, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(descriptor.Text) &&
                            descriptor.Text.Contains(currentText, StringComparison.OrdinalIgnoreCase));
                }));
                if (textMatch != null)
                {
                    return textMatch;
                }
            }

            return descriptor.Index >= 0 && descriptor.Index < buttons.Count
                ? PreferEnabledButton(new[] { buttons[descriptor.Index] })
                : null;
        }

        private static bool ShouldSkipButton(Type rootType, string? buttonName, string buttonText)
        {
            if (rootType == typeof(LoginWindow) &&
                string.Equals(buttonName, "CloseButton", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (rootType == typeof(MainWindow) &&
                string.Equals(buttonName, "MenuSair", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (rootType == typeof(PDVControl) &&
                (string.Equals(buttonName, "AbrirSelecionarClienteButton", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(buttonText, "Selecionar", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (typeof(Window).IsAssignableFrom(rootType) &&
                string.Equals(buttonText, "X", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static string ExtractButtonText(Button button)
        {
            var directText = button.Content switch
            {
                string text => text.Trim(),
                TextBlock textBlock => textBlock.Text.Trim(),
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(directText))
            {
                return directText;
            }

            var nestedText = string.Join(
                " ",
                FindVisualChildren<TextBlock>(button)
                    .Select(textBlock => textBlock.Text?.Trim())
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(nestedText) ? button.Name : nestedText;
        }

        private static string DescribeVisibleButtons(DependencyObject root)
        {
            var buttons = FindVisualChildren<Button>(root)
                .Where(IsButtonDiscoverable)
                .Select(button =>
                {
                    var name = string.IsNullOrWhiteSpace(button.Name) ? "(sem nome)" : button.Name;
                    var text = string.IsNullOrWhiteSpace(ExtractButtonText(button)) ? "(sem texto)" : ExtractButtonText(button);
                    return $"{name}='{text}'";
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return buttons.Count == 0
                ? "nenhum botao visivel"
                : string.Join(", ", buttons);
        }

        private static void RaiseButtonClick(Button button)
        {
            button.Focus();
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
        }

        private static bool IsButtonDiscoverable(Button button)
        {
            return button.Visibility == Visibility.Visible;
        }

        private static Button? PreferEnabledButton(IEnumerable<Button> buttons)
        {
            var candidates = buttons.ToList();
            return candidates.FirstOrDefault(button => button.IsEnabled)
                ?? candidates.FirstOrDefault();
        }

        private static bool CanForceDisabledAutomationClick(Button button)
        {
            if (button == null || button.Visibility != Visibility.Visible)
            {
                return false;
            }

            if (button.Tag == null && button.DataContext == null)
            {
                return false;
            }

            return HasAncestor<DataGridCell>(button)
                || HasAncestor<DataGridRow>(button)
                || HasAncestor<ListViewItem>(button)
                || HasAncestor<ListBoxItem>(button);
        }

        private static bool HasAncestor<TAncestor>(DependencyObject dependencyObject) where TAncestor : DependencyObject
        {
            DependencyObject? current = dependencyObject;
            while (current != null)
            {
                if (current is TAncestor)
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        private static void WaitForUiIdle(int cycles = 8)
        {
            for (var index = 0; index < cycles; index++)
            {
                PumpDispatcher();
                Thread.Sleep(75);
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            var visualMatches = CollectDescendants<T>(root, includeLogicalChildren: false);
            var matches = visualMatches.Count > 0
                ? visualMatches
                : CollectDescendants<T>(root, includeLogicalChildren: true);

            foreach (var match in matches)
            {
                yield return match;
            }
        }

        private static List<T> CollectDescendants<T>(DependencyObject root, bool includeLogicalChildren) where T : DependencyObject
        {
            var matches = new List<T>();
            var visited = new HashSet<DependencyObject>();
            var pending = new Stack<DependencyObject>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current == null || !visited.Add(current))
                {
                    continue;
                }

                if (current is T typedCurrent)
                {
                    matches.Add(typedCurrent);
                }

                foreach (var child in GetTraversalChildren(current, includeLogicalChildren))
                {
                    pending.Push(child);
                }
            }

            return matches;
        }

        private static IEnumerable<DependencyObject> GetTraversalChildren(DependencyObject root, bool includeLogicalChildren)
        {
            var visualChildrenCount = 0;
            try
            {
                visualChildrenCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch
            {
                visualChildrenCount = 0;
            }

            for (var index = 0; index < visualChildrenCount; index++)
            {
                DependencyObject? visualChild;
                try
                {
                    visualChild = VisualTreeHelper.GetChild(root, index);
                }
                catch
                {
                    visualChild = null;
                }

                if (visualChild != null)
                {
                    yield return visualChild;
                }
            }

            if (includeLogicalChildren && (root is FrameworkElement || root is FrameworkContentElement))
            {
                IEnumerable logicalChildren;
                try
                {
                    logicalChildren = LogicalTreeHelper.GetChildren(root);
                }
                catch
                {
                    yield break;
                }

                foreach (var logicalChild in logicalChildren.OfType<DependencyObject>())
                {
                    yield return logicalChild;
                }
            }
        }

        private static T? FindElementByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            return FindVisualChildren<T>(root)
                .FirstOrDefault(element => string.Equals(element.Name, name, StringComparison.Ordinal));
        }

        private static void SelectTabByHeader(DependencyObject root, string header)
        {
            var tabControl = FindVisualChildren<TabControl>(root).FirstOrDefault()
                ?? throw new InvalidOperationException($"Nenhum TabControl foi localizado para selecionar a aba '{header}'.");
            var tabItem = tabControl.Items
                .OfType<TabItem>()
                .FirstOrDefault(item => string.Equals(Convert.ToString(item.Header), header, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Aba '{header}' nao foi localizada.");

            tabControl.SelectedItem = tabItem;
            PumpDispatcher();

            if (root is FrameworkElement element)
            {
                element.UpdateLayout();
            }

            PumpDispatcher();
        }

        private static void SetTextBoxValue(DependencyObject root, string name, string value)
        {
            var textBox = FindElementByName<TextBox>(root, name)
                ?? throw new InvalidOperationException($"TextBox '{name}' nao foi localizado.");
            textBox.Text = value;
            PumpDispatcher();
        }

        private static void ClickButton(DependencyObject root, string nameOrText)
        {
            var button = PreferEnabledButton(
                FindVisualChildren<Button>(root)
                    .Where(candidate =>
                        IsButtonDiscoverable(candidate) &&
                        (string.Equals(candidate.Name, nameOrText, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(ExtractButtonText(candidate), nameOrText, StringComparison.OrdinalIgnoreCase))))
                ?? throw new InvalidOperationException($"Botao '{nameOrText}' nao foi localizado.");

            if (!button.IsEnabled)
            {
                throw new InvalidOperationException($"Botao '{nameOrText}' esta desabilitado durante o smoke test.");
            }

            RaiseButtonClick(button);
            WaitForUiIdle();
        }

        private static void SelectFirstDataGridItem(DataGrid dataGrid)
        {
            if (dataGrid.Items.Count == 0)
            {
                return;
            }

            dataGrid.SelectedIndex = 0;
            if (dataGrid.SelectedItem != null)
            {
                dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                if (dataGrid.Columns.Count > 0)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.SelectedItem, dataGrid.Columns[0]);
                }
            }

            WaitForUiIdle();
        }

        private static void SelectFirstListViewItem(ListView listView)
        {
            if (listView.Items.Count == 0)
            {
                return;
            }

            listView.SelectedIndex = 0;
            if (listView.SelectedItem != null)
            {
            listView.ScrollIntoView(listView.SelectedItem);
            }

            WaitForUiIdle();
        }

        private static void AwaitUiTask(Task task, Func<bool> completionPredicate, TimeSpan timeout, string failureMessage)
        {
            WaitForCondition(
                () => task.IsCompleted && completionPredicate(),
                timeout,
                failureMessage);

            if (task.IsFaulted)
            {
                throw task.Exception?.GetBaseException() ?? new InvalidOperationException(failureMessage);
            }
        }

        private static void InvokeButtonHandler(object target, string methodName, object? tag)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(target.GetType().FullName, methodName);

            var button = new Button
            {
                Tag = tag,
                DataContext = tag
            };

            method.Invoke(target, new object[] { button, new RoutedEventArgs(ButtonBase.ClickEvent, button) });
            WaitForUiIdle();
        }

        private static void AdicionarProdutoAoCarrinhoParaAutomacao(PDVControl control, Produto fixtureProduto)
        {
            var produto = control.ViewModel.Produtos.FirstOrDefault(item => item.Id == fixtureProduto.Id)
                ?? control.ViewModel.Produtos.FirstOrDefault()
                ?? throw new InvalidOperationException("Nenhum produto disponivel foi encontrado no PDV.");

            var method = typeof(PDVControl).GetMethod("AdicionarProdutoAoCarrinho", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(PDVControl).FullName, "AdicionarProdutoAoCarrinho");

            control.ViewModel.SelectedProduto = produto;
            method.Invoke(control, new object[] { produto });
            WaitForUiIdle();
        }

        private static void AssertWindowStillOperational(Window hostWindow, string contexto)
        {
            var continuaOperacional = TryWaitForCondition(
                () => hostWindow.IsLoaded && hostWindow.IsVisible,
                TimeSpan.FromSeconds(2));

            if (!continuaOperacional &&
                !hostWindow.Dispatcher.HasShutdownStarted &&
                !hostWindow.Dispatcher.HasShutdownFinished)
            {
                try
                {
                    if (!hostWindow.IsVisible)
                    {
                        hostWindow.Show();
                    }

                    hostWindow.Activate();
                    WaitForUiIdle();
                }
                catch (InvalidOperationException)
                {
                }

                continuaOperacional = TryWaitForCondition(
                    () => hostWindow.IsLoaded && hostWindow.IsVisible,
                    TimeSpan.FromSeconds(2));
            }

            if (!continuaOperacional)
            {
                if (hostWindow.Content is PDVControl control &&
                    control.ViewModel != null &&
                    !control.Dispatcher.HasShutdownStarted &&
                    !control.Dispatcher.HasShutdownFinished)
                {
                    try
                    {
                        PrepareElement(control);
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    return;
                }

                throw new InvalidOperationException($"A tela hospedeira do PDV foi fechada de forma inesperada apos {contexto}.");
            }
        }

        private static void CloseTransientWindows(Window keepWindow)
        {
            var windows = Application.Current?.Windows
                .OfType<Window>()
                .Where(window => !ReferenceEquals(window, keepWindow))
                .ToList()
                ?? new List<Window>();

            foreach (var window in windows)
            {
                if (window.IsVisible)
                {
                    window.Close();
                }
            }
        }

        private static List<string> ObterJanelasTransientesVisiveis(Window keepWindow)
        {
            return Application.Current?.Windows
                .OfType<Window>()
                .Where(window => !ReferenceEquals(window, keepWindow) && window.IsVisible)
                .Select(window =>
                    string.IsNullOrWhiteSpace(window.Title)
                        ? window.GetType().Name
                        : $"{window.GetType().Name}('{window.Title}')")
                .ToList()
                ?? new List<string>();
        }

        private static bool TryCloseWindow(Window window, TimeSpan? timeout = null)
        {
            if (window == null) return true;

            try
            {
                if (window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished)
                {
                    return true;
                }

                if (window.IsVisible)
                {
                    try
                    {
                        window.Close();
                    }
                    catch (InvalidOperationException)
                    {
                        // ignore
                    }
                }

                var wait = timeout ?? TimeSpan.FromSeconds(3);
                return TryWaitForCondition(() => !window.IsVisible || window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished, wait);
            }
            catch
            {
                return false;
            }
        }

        private string PersistReport(UiSmokeTestRunResult result)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "smoke-tests");
            Directory.CreateDirectory(baseDirectory);

            var path = Path.Combine(baseDirectory, $"ui-smoke-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt");
            var builder = new StringBuilder();

            builder.AppendLine("PRIMO AUTO ELETRICA - UI SMOKE TEST");
            builder.AppendLine($"GeneratedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
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

        private static Funcionario CreateSyntheticAdministrator()
        {
            return CreateSyntheticUser("Administrador", "Smoke Test");
        }

        private static Funcionario CreateSyntheticUser(string perfilAcesso, string nome)
        {
            return new Funcionario
            {
                Id = -1,
                Nome = nome,
                Email = $"{perfilAcesso.ToLowerInvariant()}@local",
                PerfilAcesso = perfilAcesso,
                Funcao = perfilAcesso,
                Status = "Ativo",
                Ativo = true,
                DataCadastro = DateTime.Now
            };
        }

        private static Cliente CreateSampleCliente()
        {
            var clienteId = Guid.NewGuid();
            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                Marca = "Fiat",
                Modelo = "Uno",
                Ano = DateTime.Today.Year.ToString(),
                Cor = "Branco",
                Placa = "ABC1D23",
                Chassi = "9BD00000000000001",
                Renavam = "12345678901",
                Motor = "1.0 Fire",
                Combustivel = "Flex",
                Quilometragem = 125000,
                Observacoes = "Veiculo sintetico para smoke test."
            };

            return new Cliente
            {
                Id = clienteId,
                Nome = "Cliente Smoke Test",
                CPF = "12345678900",
                Telefone = "(11) 99999-0000",
                WhatsApp = "(11) 99999-0000",
                Email = "cliente-smoke@local",
                CEP = "01001000",
                Rua = "Rua de Teste",
                Numero = "100",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Estado = "SP",
                Observacoes = "Cadastro sintetico usado na validacao automatizada.",
                ClienteVip = true,
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Now,
                OrigemConsentimentoLGPD = "Smoke test",
                AutorizaContatoWhatsApp = true,
                TotalGasto = 1250.50m,
                TotalServicos = 4,
                PontosFidelidade = 120,
                DataCadastro = DateTime.Today.AddMonths(-6),
                UltimaVisita = DateTime.Today.AddDays(-8),
                Veiculos = new List<Veiculo> { veiculo },
                HistoricoServicos = new List<HistoricoServico>
                {
                    new()
                    {
                        DataServico = DateTime.Today.AddDays(-15),
                        Descricao = "Revisao eletrica",
                        Observacoes = "Troca de fusivel e limpeza de conexoes",
                        TecnicoResponsavel = "Tecnico Smoke",
                        Valor = 280m
                    }
                }
            };
        }

        private static Fornecedor CreateSampleFornecedor()
        {
            return new Fornecedor
            {
                RazaoSocial = "Fornecedor Smoke LTDA",
                NomeFantasia = "Fornecedor Smoke",
                CNPJ = "12345678000199",
                InscricaoEstadual = "123456789",
                Telefone = "(11) 3333-0000",
                Celular = "(11) 98888-0000",
                Email = "fornecedor-smoke@local",
                Site = "https://example.local/fornecedor-smoke",
                CEP = "01001000",
                Rua = "Avenida Exemplo",
                Numero = "500",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Estado = "SP",
                Categoria = "Pecas",
                FormaPagamento = "Boleto",
                PrazoPagamento = "28 dias",
                PedidoMinimo = 350m,
                Nota = 5,
                Observacoes = "Fornecedor sintetico usado em smoke test.",
                Ativo = true,
                DataCadastro = DateTime.Today.AddMonths(-10),
                UltimaCompra = DateTime.Today.AddDays(-20),
                TotalCompras = 8450.35m
            };
        }

        private static Produto CreateSampleProduto()
        {
            return new Produto
            {
                Codigo = "SMK-001",
                Nome = "Rele Automotivo Smoke",
                Descricao = "Produto sintetico para validacao automatizada.",
                Categoria = "Eletrica",
                Marca = "Teste",
                Modelo = "12V",
                QuantidadeEstoque = 15,
                QuantidadeMinima = 3,
                QuantidadeMaxima = 40,
                Localizacao = "A1",
                Prateleira = "P1",
                Gaveta = "G1",
                PrecoCompra = 12.50m,
                PrecoVenda = 24.90m,
                Fornecedor = "Fornecedor Smoke",
                TelefoneFornecedor = "(11) 3333-0000",
                Observacoes = "Item sintetico para smoke test.",
                DataCadastro = DateTime.Today.AddMonths(-2),
                DataUltimaAtualizacao = DateTime.Today.AddDays(-2),
                Ativo = true
            };
        }

        private static Orcamento CreateSampleOrcamento(Cliente cliente)
        {
            return new Orcamento
            {
                Numero = "ORC-SMOKE-001",
                ClienteId = cliente.Id,
                Cliente = cliente,
                Status = "Em Aberto",
                DataCriacao = DateTime.Today.AddDays(-3),
                DataValidade = DateTime.Today.AddDays(7),
                Observacoes = "Orcamento sintetico usado em smoke test.",
                CondicoesPagamento = "PIX ou cartao",
                PrazoEntrega = "Imediato",
                Subtotal = 240m,
                Desconto = 10m,
                Total = 230m
            };
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
                Telefone = "(11) 98888-0000",
                DataAdmissao = DateTime.Today,
                Salario = 2500m,
                Status = "Ativo",
                DataCadastro = DateTime.Now,
                Ativo = true
            };

            funcionario.Id = App.Repositories.Funcionarios.Salvar(funcionario);
            return funcionario;
        }

        private UiSmokeFixture EnsureSmokeFixture(Funcionario administrator)
        {
            var fixture = new UiSmokeFixture
            {
                Administrator = administrator,
                Funcionario = EnsureFuncionario("smoke-vendedor@primoauto.com", "Smoke Vendedor", "Vendedor")
            };

            fixture.Fornecedor = CreatePersistedFornecedor();
            fixture.Cliente = CreatePersistedCliente();
            fixture.Veiculo = CreatePersistedVeiculo(fixture.Cliente.Id);
            fixture.Cliente.Veiculos.Add(fixture.Veiculo);
            fixture.Produto = CreatePersistedProduto(fixture.Fornecedor);
            fixture.Orcamento = CreatePersistedOrcamento(fixture.Cliente, fixture.Produto);
            fixture.OrdemServico = CreatePersistedOrdemServico(fixture.Cliente, fixture.Veiculo, fixture.Produto);
            fixture.Agendamento = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto);
            EnsureFinanceiroSeed(fixture.Cliente, fixture.Fornecedor);
            fixture.Venda = CreateSyntheticVenda(fixture.Cliente, fixture.Produto, administrator.Nome);

            return fixture;
        }

        private static Fornecedor CreatePersistedFornecedor()
        {
            var token = DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
            var fornecedor = new Fornecedor
            {
                RazaoSocial = $"Fornecedor Smoke {token} LTDA",
                NomeFantasia = $"Fornecedor Smoke {token}",
                CNPJ = GerarCnpjValido(token),
                InscricaoEstadual = token[^9..],
                Telefone = "(11) 3333-0000",
                Celular = "(11) 98888-0000",
                Email = $"fornecedor.{token}@primoauto.com",
                Site = "https://smoke.primoauto.local",
                CEP = "01001000",
                Rua = "Rua Smoke Fornecedor",
                Numero = "100",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Estado = "SP",
                Categoria = "Pecas",
                FormaPagamento = "Boleto",
                PrazoPagamento = "28 dias",
                PrazoMedioEntregaDias = 3,
                PedidoMinimo = 100m,
                Observacoes = "Fornecedor sintetico criado pelo smoke test.",
                DataCadastro = DateTime.Now,
                UltimaCompra = DateTime.Today.AddDays(-2),
                TotalCompras = 500m,
                Ativo = true
            };

            App.Repositories.Fornecedores.Inserir(fornecedor);
            return fornecedor;
        }

        private static Cliente CreatePersistedCliente()
        {
            var token = DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
            var cliente = new Cliente
            {
                Nome = $"Cliente Smoke {token}",
                CPF = GerarCpfValido(token),
                Telefone = "(11) 98888-0001",
                WhatsApp = "(11) 98888-0001",
                Email = $"cliente.{token}@primoauto.com",
                CEP = "01001000",
                Rua = "Rua Smoke Cliente",
                Numero = "101",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Estado = "SP",
                ClienteVip = true,
                TotalGasto = 420m,
                TotalServicos = 2,
                PontosFidelidade = 15,
                Observacoes = "Cliente sintetico criado pelo smoke test.",
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Now,
                OrigemConsentimentoLGPD = "Smoke test",
                AutorizaContatoWhatsApp = true,
                DataCadastro = DateTime.Now,
                UltimaVisita = DateTime.Today.AddDays(-3)
            };

            App.Repositories.Clientes.Inserir(cliente);
            return cliente;
        }

        private static Veiculo CreatePersistedVeiculo(Guid clienteId)
        {
            var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            var veiculo = new Veiculo
            {
                ClienteId = clienteId,
                Marca = "Volkswagen",
                Modelo = "Gol",
                Ano = "2019",
                Cor = "Prata",
                Placa = GerarPlacaValida(token),
                Chassi = $"9BWZZZ377VT{token.PadLeft(9, '0')[..9]}",
                Renavam = token.PadLeft(11, '0')[..11],
                Motor = "1.6 MSI",
                Combustivel = "Flex",
                Quilometragem = 78000,
                Observacoes = "Veiculo sintetico criado pelo smoke test."
            };

            App.Repositories.Clientes.SalvarVeiculo(veiculo);
            return veiculo;
        }

        private static Produto CreatePersistedProduto(Fornecedor fornecedor)
        {
            var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            var produto = new Produto
            {
                Codigo = $"SMK-{token}",
                Nome = $"Produto Smoke {token}",
                Descricao = "Produto sintetico para validacao automatizada da UI.",
                Categoria = "Eletrica",
                Marca = "Smoke",
                Modelo = "12V",
                Cor = "Preto",
                Material = "Cobre",
                Fornecedor = fornecedor.NomeFantasia,
                CNPJFornecedor = fornecedor.CNPJ,
                TelefoneFornecedor = fornecedor.Telefone,
                QuantidadeEstoque = 20,
                QuantidadeMinima = 2,
                QuantidadeMaxima = 40,
                Localizacao = "A1",
                Prateleira = "P1",
                Gaveta = "G1",
                PrecoCompra = 10m,
                PrecoVenda = 25m,
                MargemLucro = 60m,
                ValorTotalEstoque = 200m,
                SKU = $"SKU-{token}",
                CodigoBarras = token.PadLeft(12, '0')[..12],
                Observacoes = "Produto sintetico criado pelo smoke test.",
                DataCadastro = DateTime.Now,
                DataUltimaAtualizacao = DateTime.Now,
                Ativo = true
            };

            App.Repositories.Produtos.Inserir(produto);
            return produto;
        }

        private static Orcamento CreatePersistedOrcamento(Cliente cliente, Produto produto)
        {
            var service = new OrcamentoDatabaseService();
            var orcamentoId = Guid.NewGuid();
            var item = new OrcamentoItem
            {
                Id = Guid.NewGuid(),
                OrcamentoId = orcamentoId,
                ProdutoId = produto.Id,
                ProdutoNome = produto.Nome,
                ProdutoCodigo = produto.Codigo,
                ProdutoCategoria = produto.Categoria,
                ProdutoMarca = produto.Marca,
                ProdutoAplicacao = "Uso sintetico",
                Quantidade = 1,
                PrecoUnitario = produto.PrecoVenda,
                PrecoCusto = produto.PrecoCompra,
                Desconto = 0,
                Subtotal = produto.PrecoVenda,
                LucroEstimado = produto.PrecoVenda - produto.PrecoCompra,
                MargemLucro = produto.MargemLucro,
                EstoqueDisponivel = produto.QuantidadeEstoque,
                Observacoes = "Item sintetico do smoke test."
            };

            var orcamento = new Orcamento
            {
                Id = orcamentoId,
                ClienteId = cliente.Id,
                Cliente = cliente,
                Numero = service.GerarNumeroOrcamento(),
                Status = "Em Aberto",
                DataCriacao = DateTime.Now,
                DataValidade = DateTime.Today.AddDays(7),
                Subtotal = item.Subtotal,
                Desconto = 0,
                Acrescimo = 0,
                Total = item.Subtotal,
                MargemLucro = item.MargemLucro,
                LucroEstimado = item.LucroEstimado,
                Observacoes = "Orcamento sintetico do smoke test.",
                CondicoesPagamento = "PIX",
                PrazoEntrega = "Imediato",
                Itens = new List<OrcamentoItem> { item }
            };

            service.AdicionarOrcamento(orcamento);
            return orcamento;
        }

        private static OrdemServico CreatePersistedOrdemServico(Cliente cliente, Veiculo veiculo, Produto produto)
        {
            var ordem = new OrdemServico
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
                Origem = "SmokeTest",
                ProblemaRelatado = "Falha eletrica sintetica.",
                ObservacoesInternas = "OS criada pelo smoke test.",
                ChecklistEntrega = "Checklist sintetico",
                GarantiaObservacoes = "Garantia sintetica",
                GarantiaValidaAte = DateTime.Today.AddDays(30),
                DataAbertura = DateTime.Now,
                DataPrevisao = DateTime.Today.AddDays(1),
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
                        Observacoes = "Peca sintetica do smoke test."
                    },
                    new()
                    {
                        Tipo = "Servico",
                        Descricao = "Diagnostico eletrico",
                        Quantidade = 1,
                        ValorUnitario = 80m,
                        CustoUnitario = 0,
                        Observacoes = "Servico sintetico do smoke test."
                    }
                }
            };

            App.Repositories.OrdensServico.Inserir(ordem);
            return ordem;
        }

        private static Agendamento CreatePersistedAgendamento(
            Cliente cliente,
            Veiculo veiculo,
            Produto produto,
            DateTime? dataAgendamento = null,
            string status = "Confirmado",
            string prioridade = "Normal",
            string? numeroPrefixo = null)
        {
            var dataBase = (dataAgendamento ?? DateTime.Today).Date;
            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                Numero = $"{(string.IsNullOrWhiteSpace(numeroPrefixo) ? "AG-SMK" : numeroPrefixo)}-{DateTime.Now:HHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}",
                DataCriacao = DateTime.Now,
                DataAgendamento = dataBase,
                HoraInicio = dataBase.AddHours(10),
                HoraTermino = dataBase.AddHours(11),
                DuracaoEstimada = TimeSpan.FromHours(1),
                Status = string.IsNullOrWhiteSpace(status) ? "Confirmado" : status,
                Prioridade = string.IsNullOrWhiteSpace(prioridade) ? "Normal" : prioridade,
                TipoServico = "Diagnostico",
                CategoriaServico = "Oficina",
                DescricaoServico = "Agendamento sintetico do smoke test.",
                Observacoes = "Agendamento sintetico criado pelo smoke test.",
                ClienteId = cliente.Id,
                ClienteNome = cliente.Nome,
                ClienteTelefone = cliente.Telefone,
                ClienteEmail = cliente.Email,
                ClienteDocumento = cliente.CPF,
                ClienteVip = cliente.ClienteVip,
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
                TecnicoNome = "Smoke Tecnico",
                TecnicoEspecialidade = "Eletrica",
                TecnicoAtivo = true,
                ValorEstimado = produto.PrecoVenda + 90m,
                ValorReal = produto.PrecoVenda + 90m,
                ValorProdutos = produto.PrecoVenda,
                ValorServicos = 90m,
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
                        PrecoTotal = produto.PrecoVenda,
                        Reservado = false
                    }
                },
                Servicos = new List<AgendamentoServico>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Nome = "Diagnostico eletrico",
                        Categoria = "Oficina",
                        Valor = 90m,
                        TempoEstimado = TimeSpan.FromMinutes(45),
                        Status = "Pendente",
                        Observacoes = "Servico sintetico do smoke test."
                    }
                },
                Timeline = new List<AgendamentoTimeline>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DataHora = DateTime.Now,
                        Usuario = "Smoke",
                        Acao = "Criacao",
                        Detalhes = "Agendamento sintetico do smoke test.",
                        TipoAlteracao = "Cadastro"
                    }
                }
            };

            new AgendamentoDatabaseService().AdicionarAgendamento(agendamento);
            return agendamento;
        }

        private static void EnsureFinanceiroSeed(Cliente cliente, Fornecedor fornecedor)
        {
            var financeiro = new FinanceiroDatabaseService();
            financeiro.AdicionarContaPagar(
                fornecedor.NomeFantasia,
                "Conta sintetica do smoke test",
                120m,
                DateTime.Today.AddDays(7),
                "Estoque",
                "Seed automatizado do smoke test.");

            financeiro.AdicionarContaReceber(
                cliente.Nome,
                "Recebimento sintetico do smoke test",
                180m,
                DateTime.Today.AddDays(5),
                "PIX",
                "Seed automatizado do smoke test.",
                origem: "SmokeTest",
                referenciaExterna: Guid.NewGuid().ToString("N"));
        }

        private static Venda CreateSyntheticVenda(Cliente cliente, Produto produto, string usuario)
        {
            return new Venda
            {
                Id = Guid.NewGuid(),
                Data = DateTime.Now.AddMinutes(-15),
                Cliente = cliente,
                FormaPagamento = "PIX",
                Status = "Concluida",
                Usuario = usuario,
                Desconto = 0,
                Total = produto.PrecoVenda,
                Itens = new List<ItemVenda>
                {
                    new()
                    {
                        Produto = produto,
                        Quantidade = 1,
                        PrecoUnitario = produto.PrecoVenda,
                        Desconto = 0
                    }
                }
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
            var digito2 = CalcularDigitoCnpj(baseDigits + digito1);
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

        private static string GerarPlacaValida(string seed)
        {
            var digits = new string(seed.Where(char.IsDigit).ToArray()).PadLeft(4, '0');
            return $"SMK{digits[^4]}A{digits[^2..]}";
        }

        private sealed record InteractionSurface(Window HostWindow, FrameworkElement Root);
        private sealed record ButtonDescriptor(int Index, string Name, string Text)
        {
            public string DisplayName => string.IsNullOrWhiteSpace(Text) ? Name : Text;
        }

        private sealed class UiSmokeFixture
        {
            public Funcionario Administrator { get; set; } = null!;
            public Funcionario Funcionario { get; set; } = null!;
            public Cliente Cliente { get; set; } = null!;
            public Veiculo Veiculo { get; set; } = null!;
            public Produto Produto { get; set; } = null!;
            public Fornecedor Fornecedor { get; set; } = null!;
            public Orcamento Orcamento { get; set; } = null!;
            public OrdemServico OrdemServico { get; set; } = null!;
            public Agendamento Agendamento { get; set; } = null!;
            public Venda Venda { get; set; } = null!;
        }

        private sealed class AutomatedDialogSupervisor : IDisposable
        {
            private readonly Window _ownerWindow;
            private readonly UiSmokeFixture? _fixture;
            private readonly CancellationTokenSource _cancellation = new();
            private Thread? _workerThread;

            public AutomatedDialogSupervisor(Window ownerWindow, UiSmokeFixture? fixture)
            {
                _ownerWindow = ownerWindow;
                _fixture = fixture;
            }

            public void Start()
            {
                if (_workerThread != null)
                {
                    return;
                }

                _workerThread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "UiSmokeDialogSupervisor"
                };
                _workerThread.Start();
            }

            public void Dispose()
            {
                _cancellation.Cancel();
                if (_workerThread != null && _workerThread.IsAlive)
                {
                    _workerThread.Join(TimeSpan.FromSeconds(1));
                }
            }

            private void Run()
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    try
                    {
                        HandleNativeDialogs();
                        Application.Current?.Dispatcher.BeginInvoke(new Action(HandleManagedDialogs), DispatcherPriority.Background);
                    }
                    catch
                    {
                    }

                    Thread.Sleep(125);
                }
            }

            private void HandleManagedDialogs()
            {
                var windows = Application.Current?.Windows
                    .OfType<Window>()
                    .Where(window => window.IsVisible && !ReferenceEquals(window, _ownerWindow))
                    .ToList()
                    ?? new List<Window>();

                foreach (var window in windows)
                {
                    if (window is MainWindow)
                    {
                        continue;
                    }

                    if (window is OperacaoCaixaWindow)
                    {
                        TrySetText(window, "ValorTextBox", "10,00", overwrite: true);
                        TrySetText(window, "ObservacoesTextBox", "Automacao do smoke test.", overwrite: false);
                        TryClickButton(window, "ConfirmarButton", "Confirmar");
                        continue;
                    }

                    if (window is SelecionarVendaWindow)
                    {
                        SelectFirstDataGridItem(window);
                        TryClickButton(window, "SelecionarButton", "Selecionar venda", "Selecionar");
                        continue;
                    }

                    if (window is SelecionarClientePDVWindow)
                    {
                        SelectFirstDataGridItem(window);
                        TryClickButton(window, string.Empty, "Consumidor final", "Selecionar cliente", "Selecionar", "Cancelar");
                        continue;
                    }

                    if (window is SelecionarOrcamentoWindow)
                    {
                        SelectFirstDataGridItem(window);
                        TryClickButton(window, string.Empty, "Duplicar", "Abrir orcamento", "Selecionar");
                        continue;
                    }

                    if (window is ConfirmacaoCriticaWindow)
                    {
                        var keyword = FindElementByName<TextBlock>(window, "KeywordTextBlock")?.Text ?? "CONFIRMAR";
                        TrySetText(window, "ConfirmationTextBox", keyword, overwrite: true);
                        TryClickButton(window, "ConfirmarButton", "Confirmar");
                        continue;
                    }

                    if (window is AdicionarFornecedorDialog)
                    {
                        TryClickButton(window, string.Empty, "Sim", "Nao");
                        continue;
                    }

                    if (!TryClickButton(window, string.Empty, "Salvar", "Confirmar", "Selecionar", "OK", "Ok", "Fechar", "Cancelar", "Voltar"))
                    {
                        window.Close();
                    }
                }
            }

            private static void SelectFirstDataGridItem(DependencyObject root)
            {
                foreach (var grid in FindVisualChildren<DataGrid>(root))
                {
                    if (grid.Items.Count > 0 && grid.SelectedIndex < 0)
                    {
                        grid.SelectedIndex = 0;
                    }
                }
            }

            private static bool TryClickButton(DependencyObject root, string buttonName, params string[] buttonTexts)
            {
                var button = FindVisualChildren<Button>(root)
                    .FirstOrDefault(candidate =>
                        candidate.IsEnabled &&
                        IsButtonDiscoverable(candidate) &&
                        (!string.IsNullOrWhiteSpace(buttonName) && string.Equals(candidate.Name, buttonName, StringComparison.OrdinalIgnoreCase) ||
                         buttonTexts.Any(text => string.Equals(ExtractButtonText(candidate), text, StringComparison.OrdinalIgnoreCase))));

                if (button == null)
                {
                    return false;
                }

                RaiseButtonClick(button);
                PumpDispatcher();
                return true;
            }

            private static void TrySetText(DependencyObject root, string textBoxName, string value, bool overwrite)
            {
                var textBox = FindElementByName<TextBox>(root, textBoxName);
                if (textBox == null || !textBox.IsEnabled)
                {
                    return;
                }

                if (!overwrite && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    return;
                }

                textBox.Text = value;
                PumpDispatcher();
            }

            private static void HandleNativeDialogs()
            {
                NativeMethods.EnumerateWindows(handle =>
                {
                    if (!NativeMethods.IsWindowVisible(handle))
                    {
                        return true;
                    }

                    NativeMethods.GetWindowThreadProcessId(handle, out var processId);
                    if (processId != Environment.ProcessId)
                    {
                        return true;
                    }

                    var className = NativeMethods.GetClassName(handle);
                    if (!string.Equals(className, "#32770", StringComparison.Ordinal))
                    {
                        return true;
                    }

                    foreach (var buttonId in new[] { NativeMethods.IDCANCEL, NativeMethods.IDNO, NativeMethods.IDOK, NativeMethods.IDYES })
                    {
                        if (NativeMethods.TryClickDialogButton(handle, buttonId))
                        {
                            return true;
                        }
                    }

                    NativeMethods.SendClose(handle);
                    return true;
                });
            }
        }

        private static class NativeMethods
        {
            public const int IDOK = 1;
            public const int IDCANCEL = 2;
            public const int IDYES = 6;
            public const int IDNO = 7;
            private const uint WM_CLOSE = 0x0010;
            private const uint BM_CLICK = 0x00F5;

            private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

            [DllImport("user32.dll")]
            private static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            public static string GetClassName(IntPtr handle)
            {
                var builder = new StringBuilder(256);
                GetClassName(handle, builder, builder.Capacity);
                return builder.ToString();
            }

            public static bool EnumerateWindows(Func<IntPtr, bool> callback)
            {
                return EnumWindows((handle, _) => callback(handle), IntPtr.Zero);
            }

            public static bool TryClickDialogButton(IntPtr dialogHandle, int buttonId)
            {
                var buttonHandle = GetDlgItem(dialogHandle, buttonId);
                if (buttonHandle == IntPtr.Zero)
                {
                    return false;
                }

                SendMessage(buttonHandle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                return true;
            }

            public static void SendClose(IntPtr handle)
            {
                SendMessage(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }

    public sealed class UiSmokeTestRunResult
    {
        public List<UiSmokeTestCheckResult> Checks { get; } = new();
        public string ReportPath { get; set; } = string.Empty;
        public int TotalChecks => Checks.Count;
        public int PassedChecks => Checks.Count(check => check.Success);
        public int FailedChecks => Checks.Count(check => !check.Success);
        public bool HasFailures => FailedChecks > 0;
    }

    public sealed class UiSmokeTestCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public long DurationMs { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
