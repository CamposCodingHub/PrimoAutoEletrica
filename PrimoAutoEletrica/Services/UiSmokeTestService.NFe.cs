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
        // Checks de importacao NF-e, rollback e XML real.

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

                var financeiro = new FinanceiroDatabaseService();
                var contasPagarNFe = financeiro.ObterContasPagar()
                    .Where(conta =>
                        string.Equals((string)conta.Origem, "ImportacaoNFeContaPagar", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals((string)conta.ReferenciaExterna, importada.ChaveAcesso, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (contasPagarNFe.Count != 1 ||
                    (decimal)contasPagarNFe[0].Valor != importada.ValorTotal ||
                    !((string)contasPagarNFe[0].Descricao).Contains(importada.Numero, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Importacao NF-e nao gerou conta a pagar integrada ao financeiro.");
                }

                financeiro.RegistrarContaPagarNFe(importada);
                var contasPagarAposUpsert = financeiro.ObterContasPagar()
                    .Count(conta =>
                        string.Equals((string)conta.Origem, "ImportacaoNFeContaPagar", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals((string)conta.ReferenciaExterna, importada.ChaveAcesso, StringComparison.OrdinalIgnoreCase));
                if (contasPagarAposUpsert != 1)
                {
                    throw new InvalidOperationException("Upsert da conta a pagar da NF-e duplicou o compromisso financeiro.");
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
            RunCheck(result, "ImportarNFe:TelaExcluirSelecionadoDesfazerRelancar", () =>
            {
                var token = DateTime.Now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var xmlPath = CriarXmlNFeRealTemporario(token, out var chaveAcesso);
                var xmlPreservadoPath = CriarXmlNFeRealTemporario($"{token}7", out var chavePreservada);
                var nfeService = new NFeService(App.Database);
                var importacaoRepository = new ImportacaoRepository(App.Database);
                var primeiraImportacaoId = Guid.Empty;
                var importacaoPreservadaId = Guid.Empty;
                var relancamentoId = Guid.Empty;
                Window? hostWindow = null;
                AutomatedDialogSupervisor? supervisor = null;

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

                    var importacaoPreservada = nfeService.ImportarXml(xmlPreservadoPath, Guid.NewGuid(), syntheticUser.Nome);
                    importacaoPreservadaId = importacaoPreservada.Id;
                    ValidarImportacaoNFeReal(importacaoPreservada, chavePreservada, "importacao preservada");

                    var produtoIdsPrimeiraImportacao = ObterProdutoIdsCriados(primeiraImportacao);
                    var produtoIdsPreservados = ObterProdutoIdsCriados(importacaoPreservada);
                    var idsAntes = importacaoRepository.ObterHistoricoImportacoes(500)
                        .Select(importacao => importacao.Id)
                        .ToHashSet();

                    var control = new ImportarNFeControl();
                    control.HabilitarAlteracoesDestrutivasParaSmoke();
                    hostWindow = new Window
                    {
                        Content = control,
                        Title = "Smoke Importar NF-e Host"
                    };

                    ShowWindowForInteraction(hostWindow);

                    var historicoDataGrid = FindElementByName<DataGrid>(control, "HistoricoDataGrid")
                        ?? throw new InvalidOperationException("HistoricoDataGrid nao foi localizado na pagina Importar NF-e.");
                    var totalImportacoesText = FindElementByName<TextBlock>(control, "TotalImportacoesText")
                        ?? throw new InvalidOperationException("Card Total de importacoes nao foi localizado.");
                    var produtosCriadosText = FindElementByName<TextBlock>(control, "ProdutosCriadosText")
                        ?? throw new InvalidOperationException("Card Produtos criados nao foi localizado.");
                    var pendenciasText = FindElementByName<TextBlock>(control, "PendenciasText")
                        ?? throw new InvalidOperationException("Resumo de pendencias nao foi localizado.");
                    var ultimaImportacaoText = FindElementByName<TextBlock>(control, "UltimaImportacaoText")
                        ?? throw new InvalidOperationException("Resumo da ultima importacao nao foi localizado.");
                    var atualizarHistoricoButton = FindElementByName<Button>(control, "AtualizarHistoricoButton")
                        ?? throw new InvalidOperationException("Botao Atualizar historico nao foi localizado.");
                    var desfazerButton = FindElementByName<Button>(control, "DesfazerProdutosImportacaoButton")
                        ?? throw new InvalidOperationException("Botao Desfazer produtos nao foi localizado.");
                    var excluirButton = FindElementByName<Button>(control, "ExcluirImportacaoSelecionadaButton")
                        ?? throw new InvalidOperationException("Botao Excluir XML selecionado nao foi localizado.");

                    ClickButton(control, "AtualizarHistoricoButton");
                    WaitForUiIdle();

                    WaitForCondition(
                        () => HistoricoNFeContem(historicoDataGrid, primeiraImportacao.Id) &&
                              HistoricoNFeContem(historicoDataGrid, importacaoPreservada.Id),
                        TimeSpan.FromSeconds(10),
                        "A pagina Importar NF-e nao carregou as duas importacoes sinteticas.");

                    var totalAntes = ParseIntText(totalImportacoesText.Text, "Total de importacoes");
                    if (totalAntes != historicoDataGrid.Items.Count || totalAntes < 2)
                    {
                        throw new InvalidOperationException($"Card Total de importacoes divergiu da grade. Card={totalAntes}; Grade={historicoDataGrid.Items.Count}.");
                    }

                    var produtosCriadosAntes = ParseIntText(produtosCriadosText.Text, "Produtos criados");
                    if (produtosCriadosAntes < produtoIdsPrimeiraImportacao.Count + produtoIdsPreservados.Count)
                    {
                        throw new InvalidOperationException("Card Produtos criados nao refletiu os XMLs carregados no historico.");
                    }

                    if (!pendenciasText.Text.Contains($"Exclusoes auditadas: {exclusoesAntes}", StringComparison.Ordinal) ||
                        !pendenciasText.Text.Contains($"Rollbacks auditados: {rollbacksAntes}", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Painel de pendencias nao exibiu os totais de auditoria atuais.");
                    }

                    SelecionarImportacaoNoHistorico(historicoDataGrid, primeiraImportacao.Id);
                    if (!desfazerButton.IsEnabled || !excluirButton.IsEnabled)
                    {
                        throw new InvalidOperationException("Os botoes Desfazer produtos e Excluir XML selecionado nao foram habilitados apos a selecao.");
                    }

                    supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                    supervisor.Start();

                    ClickButton(control, "DesfazerProdutosImportacaoButton");
                    WaitForCondition(
                        () => produtoIdsPrimeiraImportacao.All(id => App.Repositories.Produtos.ObterPorId(id) == null),
                        TimeSpan.FromSeconds(5),
                        "O clique real em Desfazer produtos nao removeu os produtos seguros da importacao selecionada.");

                    if (importacaoRepository.ObterImportacaoPorId(primeiraImportacao.Id) == null)
                    {
                        throw new InvalidOperationException("Desfazer produtos removeu indevidamente o historico da importacao.");
                    }

                    if (produtoIdsPreservados.Any(id => App.Repositories.Produtos.ObterPorId(id) == null))
                    {
                        throw new InvalidOperationException("Desfazer produtos afetou produtos de uma importacao nao selecionada.");
                    }

                    if (importacaoRepository.ObterTotalRollbacksAuditados() < rollbacksAntes + 1)
                    {
                        throw new InvalidOperationException("O clique real em Desfazer produtos nao registrou auditoria de rollback.");
                    }

                    SelecionarImportacaoNoHistorico(historicoDataGrid, primeiraImportacao.Id);
                    ClickButton(control, "ExcluirImportacaoSelecionadaButton");
                    WaitForCondition(
                        () => importacaoRepository.ObterImportacaoPorId(primeiraImportacao.Id) == null &&
                              !HistoricoNFeContem(historicoDataGrid, primeiraImportacao.Id),
                        TimeSpan.FromSeconds(5),
                        "O clique real em Excluir XML selecionado nao removeu a nota do historico.");

                    if (importacaoRepository.ObterImportacaoPorId(importacaoPreservada.Id) == null ||
                        !HistoricoNFeContem(historicoDataGrid, importacaoPreservada.Id))
                    {
                        throw new InvalidOperationException("Excluir XML selecionado removeu ou ocultou uma importacao nao selecionada.");
                    }

                    var idsDepoisExclusao = importacaoRepository.ObterHistoricoImportacoes(500)
                        .Select(importacao => importacao.Id)
                        .ToHashSet();
                    var idsRemovidos = idsAntes.Except(idsDepoisExclusao).ToList();
                    if (idsRemovidos.Count != 1 || idsRemovidos[0] != primeiraImportacao.Id)
                    {
                        throw new InvalidOperationException($"A exclusao pela tela removeu IDs inesperados: {string.Join(", ", idsRemovidos)}.");
                    }

                    var totalDepoisExclusao = ParseIntText(totalImportacoesText.Text, "Total de importacoes apos exclusao");
                    if (totalDepoisExclusao != totalAntes - 1 || totalDepoisExclusao != historicoDataGrid.Items.Count)
                    {
                        throw new InvalidOperationException("Card Total de importacoes nao foi atualizado corretamente apos excluir o XML selecionado.");
                    }

                    if (importacaoRepository.ObterTotalExclusoesAuditadas() < exclusoesAntes + 1)
                    {
                        throw new InvalidOperationException("O clique real em Excluir XML selecionado nao registrou auditoria.");
                    }

                    var relancamento = nfeService.ImportarXml(xmlPath, Guid.NewGuid(), syntheticUser.Nome);
                    relancamentoId = relancamento.Id;
                    ValidarImportacaoNFeReal(relancamento, chaveAcesso, "relancamento");

                    ClickButton(control, "AtualizarHistoricoButton");
                    WaitForCondition(
                        () => HistoricoNFeContem(historicoDataGrid, relancamento.Id) &&
                              HistoricoNFeContem(historicoDataGrid, importacaoPreservada.Id),
                        TimeSpan.FromSeconds(5),
                        "O relancamento do mesmo XML nao apareceu no historico atualizado.");

                    SelecionarImportacaoNoHistorico(historicoDataGrid, relancamento.Id);
                    if (!ultimaImportacaoText.Text.Contains(relancamento.Numero, StringComparison.Ordinal) ||
                        !ultimaImportacaoText.Text.Contains(relancamento.Fornecedor.Nome, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Painel Ultima importacao nao refletiu o XML relancado selecionado.");
                    }

                    var totalDepoisRelancamento = ParseIntText(totalImportacoesText.Text, "Total de importacoes apos relancamento");
                    if (totalDepoisRelancamento != totalAntes || totalDepoisRelancamento != historicoDataGrid.Items.Count)
                    {
                        throw new InvalidOperationException("Historico/card nao voltaram ao total esperado apos relancar o mesmo XML.");
                    }

                    if (!pendenciasText.Text.Contains($"Exclusoes auditadas: {exclusoesAntes + 1}", StringComparison.Ordinal) ||
                        !pendenciasText.Text.Contains($"Rollbacks auditados: {rollbacksAntes + 1}", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Painel de pendencias nao refletiu as auditorias geradas pelos botoes da tela.");
                    }
                }
                finally
                {
                    supervisor?.Dispose();
                    if (hostWindow != null)
                    {
                        CloseTransientWindows(hostWindow);
                        if (hostWindow.IsVisible)
                        {
                            hostWindow.Close();
                        }
                    }

                    LimparImportacaoNFeSeExistir(importacaoRepository, relancamentoId, syntheticUser.Nome);
                    LimparImportacaoNFeSeExistir(importacaoRepository, primeiraImportacaoId, syntheticUser.Nome);
                    LimparImportacaoNFeSeExistir(importacaoRepository, importacaoPreservadaId, syntheticUser.Nome);
                }
            });
        }

        private static bool HistoricoNFeContem(DataGrid dataGrid, Guid importacaoId)
        {
            return dataGrid.Items.Cast<object>().Any(item => ObterImportacaoIdDoHistorico(item) == importacaoId);
        }

        private static void SelecionarImportacaoNoHistorico(DataGrid dataGrid, Guid importacaoId)
        {
            var item = dataGrid.Items.Cast<object>()
                .SingleOrDefault(candidato => ObterImportacaoIdDoHistorico(candidato) == importacaoId)
                ?? throw new InvalidOperationException($"Importacao {importacaoId} nao foi localizada na grade.");

            dataGrid.SelectedItem = item;
            dataGrid.ScrollIntoView(item);
            WaitForUiIdle();
        }

        private static Guid ObterImportacaoIdDoHistorico(object item)
        {
            var valor = item.GetType()
                .GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(item);

            return valor is Guid id ? id : Guid.Empty;
        }

        private static object? LocalizarProdutoNoEstoque(DataGrid dataGrid, Guid produtoId)
        {
            return dataGrid.Items.Cast<object>()
                .FirstOrDefault(item =>
                    item.GetType()
                        .GetProperty("Produto", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(item) is Produto produto &&
                    produto.Id == produtoId);
        }

        private static void SelecionarProdutoNoEstoque(DataGrid dataGrid, Guid produtoId)
        {
            var item = LocalizarProdutoNoEstoque(dataGrid, produtoId)
                ?? throw new InvalidOperationException($"Produto {produtoId} nao foi localizado na grade de estoque.");

            dataGrid.SelectedItem = item;
            dataGrid.ScrollIntoView(item);
            WaitForUiIdle();
        }

        private static int LerPropriedadeInteira(object item, string propriedade)
        {
            var valor = item.GetType()
                .GetProperty(propriedade, BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(item);

            return valor == null
                ? throw new InvalidOperationException($"A propriedade '{propriedade}' nao foi localizada no item do estoque.")
                : Convert.ToInt32(valor, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static List<string> ObterNomesProdutosEstoque(DataGrid dataGrid)
        {
            return dataGrid.Items.Cast<object>()
                .Select(item => item.GetType()
                    .GetProperty("Nome", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(item)
                    ?.ToString() ?? string.Empty)
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .ToList();
        }

        private static void ValidarFiltroEstoque(
            ComboBox statusFiltro,
            DataGrid dataGrid,
            string status,
            string produtoEsperado,
            IEnumerable<string> produtosAusentes)
        {
            if (!statusFiltro.Items.Cast<object?>().Any(item =>
                    string.Equals(item?.ToString(), status, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"O filtro operacional '{status}' nao foi exposto no estoque.");
            }

            statusFiltro.SelectedItem = status;
            WaitForUiIdle();

            var nomes = ObterNomesProdutosEstoque(dataGrid);
            if (!nomes.Contains(produtoEsperado, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"O filtro '{status}' nao exibiu o produto esperado '{produtoEsperado}'.");
            }

            var indevidos = produtosAusentes
                .Where(produto => nomes.Contains(produto, StringComparer.Ordinal))
                .ToList();
            if (indevidos.Count > 0)
            {
                throw new InvalidOperationException($"O filtro '{status}' exibiu produtos indevidos: {string.Join(", ", indevidos)}.");
            }
        }

        private static int ParseIntText(string text, string descricao)
        {
            if (int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new InvalidOperationException($"{descricao} nao possui um numero inteiro valido: '{text}'.");
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

    }
}
