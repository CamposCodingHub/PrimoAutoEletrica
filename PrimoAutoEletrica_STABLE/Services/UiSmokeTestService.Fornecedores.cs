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
        // Checks de fornecedores, produto-fornecedor e exclusoes seguras.

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
                new FinanceiroDatabaseService().AdicionarContaPagar(
                    fornecedor.NomeFantasia,
                    $"Conta fornecedor smoke {DateTime.Now:HHmmssfff}",
                    432m,
                    DateTime.Today.AddDays(10),
                    "Compra NF-e",
                    $"Fornecedor={fornecedor.NomeFantasia}; Documento={fornecedor.CNPJ}",
                    origem: "SmokeFornecedorContaPagar",
                    referenciaExterna: Guid.NewGuid().ToString("N"));

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

                if (insights.TicketMedioCompra <= 0 || !insights.UltimaCompra.HasValue)
                {
                    throw new InvalidOperationException("Ticket medio ou ultima compra do fornecedor nao foi calculado.");
                }

                if (string.IsNullOrWhiteSpace(vinculo.NumeroUltimaNFe) ||
                    insights.ProdutosPrincipais.All(item => string.IsNullOrWhiteSpace(item.UltimaNFe)) ||
                    insights.HistoricoNotas.Count == 0)
                {
                    throw new InvalidOperationException("A ficha operacional nao consolidou a ultima NF-e e seu historico.");
                }

                if (insights.ContasPagarVinculadas <= 0 ||
                    insights.ValorContasPagarAberto <= 0 ||
                    insights.ContasPagarRecentes.Count == 0)
                {
                    throw new InvalidOperationException("A ficha operacional nao consolidou contas a pagar vinculadas ao fornecedor.");
                }

                var visualizarWindow = new VisualizarFornecedorWindow(fornecedor);
                try
                {
                    PrepareWindow(visualizarWindow);

                    var prazoText = FindElementByName<TextBlock>(visualizarWindow, "PrazoMedioEntregaText")?.Text;
                    var prazoPagamentoText = FindElementByName<TextBlock>(visualizarWindow, "PrazoMedioPagamentoText")?.Text;
                    var rankingText = FindElementByName<TextBlock>(visualizarWindow, "RankingText")?.Text;
                    var ultimaCompraText = FindElementByName<TextBlock>(visualizarWindow, "UltimaCompraText")?.Text;
                    var comprasText = FindElementByName<TextBlock>(visualizarWindow, "ComprasProdutoFornecedorText")?.Text;
                    var ticketMedioText = FindElementByName<TextBlock>(visualizarWindow, "TicketMedioCompraText")?.Text;
                    var produtosResumoText = FindElementByName<TextBlock>(visualizarWindow, "ProdutosRelacionadosResumoText")?.Text;
                    var contasPagarResumoText = FindElementByName<TextBlock>(visualizarWindow, "ContasPagarFornecedorResumoText")?.Text;
                    var produtosItems = FindElementByName<ItemsControl>(visualizarWindow, "ProdutosRelacionadosItemsControl");
                    var notasItems = FindElementByName<ItemsControl>(visualizarWindow, "NotasRecentesItemsControl");
                    var contasPagarItems = FindElementByName<ItemsControl>(visualizarWindow, "ContasPagarFornecedorItemsControl");

                    if (!string.Equals(prazoText, $"{insights.PrazoMedioEntregaDias} dia(s)", StringComparison.Ordinal) ||
                        !string.Equals(prazoPagamentoText, $"{fornecedor.PrazoMedioPagamentoDias} dia(s)", StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(rankingText) ||
                        !rankingText.StartsWith("#", StringComparison.Ordinal) ||
                        !string.Equals(ultimaCompraText, insights.UltimaCompra.Value.ToString("dd/MM/yyyy"), StringComparison.Ordinal) ||
                        !string.Equals(comprasText, $"{insights.QuantidadeComprasProdutoFornecedor} compra(s) vinculada(s)", StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(ticketMedioText) ||
                        string.Equals(ticketMedioText, "Sem historico", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(produtosResumoText) ||
                        string.IsNullOrWhiteSpace(contasPagarResumoText) ||
                        !contasPagarResumoText.Contains("aberto R$", StringComparison.OrdinalIgnoreCase) ||
                        produtosItems == null ||
                        produtosItems.Items.Count <= 0 ||
                        notasItems == null ||
                        notasItems.Items.Count <= 0 ||
                        contasPagarItems == null ||
                        contasPagarItems.Items.Count <= 0)
                    {
                        throw new InvalidOperationException(
                            "A janela de visualizacao nao exibiu todos os dados operacionais calculados do fornecedor.");
                    }
                }
                finally
                {
                    visualizarWindow.Close();
                }

                var fornecedoresControl = new FornecedoresControl();
                PrepareElement(fornecedoresControl);
            });
        }

        private void RunFornecedoresSegurancaExclusaoChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Fornecedores:AcoesSemExcluirNaColuna", () =>
            {
                var control = new FornecedoresControl();
                PrepareElement(control);

                var dataGrid = FindElementByName<DataGrid>(control, "FornecedoresDataGrid")
                    ?? throw new InvalidOperationException("FornecedoresDataGrid nao foi localizado.");
                var acoesColumn = dataGrid.Columns
                    .OfType<DataGridTemplateColumn>()
                    .SingleOrDefault(column => string.Equals(Convert.ToString(column.Header), "Ações", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("A coluna Acoes nao foi localizada na planilha de fornecedores.");
                var cellTemplateRoot = acoesColumn.CellTemplate?.LoadContent() as DependencyObject
                    ?? throw new InvalidOperationException("O template da coluna Acoes nao pode ser inspecionado.");
                var acoes = FindVisualChildren<Button>(cellTemplateRoot)
                    .Select(ExtractButtonText)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToList();

                if (acoes.Count != 2 ||
                    !acoes.Contains("Ver", StringComparer.OrdinalIgnoreCase) ||
                    !acoes.Contains("Editar", StringComparer.OrdinalIgnoreCase) ||
                    acoes.Any(text => text.Contains("Excluir", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        $"A coluna Acoes deve possuir somente Ver/Editar. Encontrado: {string.Join(", ", acoes)}.");
                }

                var excluirSelecionadoButton = FindElementByName<Button>(control, "ExcluirFornecedorSelecionadoButton")
                    ?? throw new InvalidOperationException("O botao externo de exclusao do fornecedor selecionado nao foi localizado.");
                if (!ExtractButtonText(excluirSelecionadoButton).Contains("Excluir fornecedor selecionado", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("O botao externo nao deixa claro que exclui somente o fornecedor selecionado.");
                }

                if (FindVisualChildren<Button>(dataGrid).Any(button => ReferenceEquals(button, excluirSelecionadoButton)))
                {
                    throw new InvalidOperationException("O botao Excluir fornecedor selecionado foi encontrado dentro da planilha.");
                }
            });

            RunCheck(result, "Fornecedores:ExcluirSomenteSelecionado", () =>
            {
                var repository = App.Repositories.Fornecedores;
                var fornecedorSelecionado = CriarFornecedorIsoladoSmoke("Selecionado");
                var fornecedorPreservado = CriarFornecedorIsoladoSmoke("Preservado");
                var hostWindow = new Window
                {
                    Content = new FornecedoresControl(),
                    Title = "Smoke Fornecedores Host"
                };
                AutomatedDialogSupervisor? supervisor = null;

                try
                {
                    repository.Inserir(fornecedorSelecionado);
                    repository.Inserir(fornecedorPreservado);
                    var idsAntes = repository.ObterTodos().Select(fornecedor => fornecedor.Id).ToHashSet();

                    ShowWindowForInteraction(hostWindow);
                    var control = hostWindow.Content as FornecedoresControl
                        ?? throw new InvalidOperationException("Host de fornecedores nao conseguiu carregar o controle.");
                    var dataGrid = FindElementByName<DataGrid>(control, "FornecedoresDataGrid")
                        ?? throw new InvalidOperationException("FornecedoresDataGrid nao foi localizado para validar a exclusao.");
                    var excluirSelecionadoButton = FindElementByName<Button>(control, "ExcluirFornecedorSelecionadoButton")
                        ?? throw new InvalidOperationException("Botao de exclusao do fornecedor selecionado nao foi localizado.");

                    WaitForCondition(
                        () => dataGrid.Items.OfType<Fornecedor>().Any(item => item.Id == fornecedorSelecionado.Id) &&
                              dataGrid.Items.OfType<Fornecedor>().Any(item => item.Id == fornecedorPreservado.Id),
                        TimeSpan.FromSeconds(5),
                        "A planilha nao carregou os dois fornecedores sinteticos para validar a exclusao individual.");

                    dataGrid.SelectedItem = dataGrid.Items.OfType<Fornecedor>().Single(item => item.Id == fornecedorSelecionado.Id);
                    dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                    WaitForUiIdle();
                    WaitForCondition(
                        () => excluirSelecionadoButton.IsEnabled,
                        TimeSpan.FromSeconds(5),
                        "O botao de exclusao nao foi habilitado apos selecionar um fornecedor.");

                    supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                    supervisor.Start();
                    ClickButton(control, "ExcluirFornecedorSelecionadoButton");

                    WaitForCondition(
                        () => repository.ObterPorId(fornecedorSelecionado.Id) == null,
                        TimeSpan.FromSeconds(5),
                        "O fornecedor selecionado continuou persistido apos a exclusao.");

                    var idsDepois = repository.ObterTodos().Select(fornecedor => fornecedor.Id).ToHashSet();
                    var idsRemovidos = idsAntes.Except(idsDepois).ToList();
                    if (idsRemovidos.Count != 1 || idsRemovidos[0] != fornecedorSelecionado.Id)
                    {
                        throw new InvalidOperationException(
                            $"A exclusao individual removeu IDs inesperados: {string.Join(", ", idsRemovidos)}.");
                    }

                    if (!idsDepois.Contains(fornecedorPreservado.Id))
                    {
                        throw new InvalidOperationException("O fornecedor nao selecionado foi removido indevidamente.");
                    }
                }
                finally
                {
                    supervisor?.Dispose();
                    CloseTransientWindows(hostWindow);

                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }

                    if (repository.ObterPorId(fornecedorSelecionado.Id) != null)
                    {
                        repository.Excluir(fornecedorSelecionado.Id);
                    }

                    if (repository.ObterPorId(fornecedorPreservado.Id) != null)
                    {
                        repository.Excluir(fornecedorPreservado.Id);
                    }
                }
            });
        }

        private void RunFornecedoresEdicaoFichaChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Fornecedores:EditarPrazoCategoriaContatoRefleteFicha", () =>
            {
                var repository = App.Repositories.Fornecedores;
                var fornecedor = CriarFornecedorIsoladoSmoke("Edicao");
                EditarFornecedorWindow? editarWindow = null;
                VisualizarFornecedorWindow? visualizarWindow = null;

                try
                {
                    repository.Inserir(fornecedor);
                    var persistido = repository.ObterPorId(fornecedor.Id)
                        ?? throw new InvalidOperationException("Fornecedor sintetico de edicao nao foi persistido.");
                    editarWindow = new EditarFornecedorWindow(persistido);
                    InitializeWindowForInteraction(editarWindow);
                    if (editarWindow.Content is FrameworkElement editarContent)
                    {
                        PrepareElement(editarContent);
                    }

                    SetTextBoxValue(editarWindow, "PrazoMedioPagamentoTextBox", "21");
                    SetTextBoxValue(editarWindow, "PrazoMedioEntregaTextBox", "12");
                    SetTextBoxValue(editarWindow, "CategoriaPreferencialTextBox", "Eletrica");
                    SetTextBoxValue(editarWindow, "ContatoPrincipalNomeTextBox", "Contato Smoke Atualizado");
                    SetTextBoxValue(editarWindow, "ContatoPrincipalCargoTextBox", "Compras");
                    SetTextBoxValue(editarWindow, "ContatoPrincipalTelefoneTextBox", "(11) 98888-7788");
                    SetTextBoxValue(editarWindow, "ContatoPrincipalEmailTextBox", "contato.edicao@primoauto.com");

                    var categoriaCombo = FindElementByName<ComboBox>(editarWindow, "CategoriaComboBox")
                        ?? throw new InvalidOperationException("CategoriaComboBox nao foi localizado na edicao do fornecedor.");
                    categoriaCombo.SelectedItem = categoriaCombo.Items
                        .OfType<ComboBoxItem>()
                        .Single(item => string.Equals(Convert.ToString(item.Content), "Materiais", StringComparison.OrdinalIgnoreCase));
                    PumpDispatcher();

                    InvokeButtonHandler(editarWindow, "SalvarButton_Click", null);

                    var atualizado = repository.ObterPorId(fornecedor.Id)
                        ?? throw new InvalidOperationException("Fornecedor desapareceu apos salvar a edicao.");
                    var contatoPrincipal = atualizado.Contatos.SingleOrDefault(contato => contato.Principal);
                    if (atualizado.PrazoMedioPagamentoDias != 21 ||
                        atualizado.PrazoMedioEntregaDias != 12 ||
                        !string.Equals(atualizado.Categoria, "Materiais", StringComparison.Ordinal) ||
                        !string.Equals(atualizado.CategoriaPreferencial, "Eletrica", StringComparison.Ordinal) ||
                        contatoPrincipal == null ||
                        !string.Equals(contatoPrincipal.Nome, "Contato Smoke Atualizado", StringComparison.Ordinal) ||
                        !string.Equals(contatoPrincipal.Cargo, "Compras", StringComparison.Ordinal) ||
                        !string.Equals(contatoPrincipal.Telefone, "(11) 98888-7788", StringComparison.Ordinal) ||
                        !string.Equals(contatoPrincipal.Email, "contato.edicao@primoauto.com", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Prazo, categoria ou contato principal nao foram persistidos pela janela de edicao. " +
                            $"PrazoPagamento={atualizado.PrazoMedioPagamentoDias}; PrazoEntrega={atualizado.PrazoMedioEntregaDias}; Categoria={atualizado.Categoria}; " +
                            $"Preferencial={atualizado.CategoriaPreferencial}; Contato={contatoPrincipal?.Nome}; " +
                            $"Cargo={contatoPrincipal?.Cargo}; Telefone={contatoPrincipal?.Telefone}; Email={contatoPrincipal?.Email}.");
                    }

                    visualizarWindow = new VisualizarFornecedorWindow(atualizado);
                    InitializeWindowForInteraction(visualizarWindow);
                    if (visualizarWindow.Content is FrameworkElement content)
                    {
                        PrepareElement(content);
                    }

                    var categoriaText = FindElementByName<TextBlock>(visualizarWindow, "CategoriaText")?.Text;
                    var categoriaPreferencialText = FindElementByName<TextBlock>(visualizarWindow, "CategoriaPreferencialText")?.Text;
                    var contatoText = FindElementByName<TextBlock>(visualizarWindow, "ContatoPrincipalText")?.Text;
                    var prazoText = FindElementByName<TextBlock>(visualizarWindow, "PrazoMedioEntregaText")?.Text;
                    var prazoPagamentoText = FindElementByName<TextBlock>(visualizarWindow, "PrazoMedioPagamentoText")?.Text;
                    if (!string.Equals(categoriaText, "Materiais", StringComparison.Ordinal) ||
                        !string.Equals(categoriaPreferencialText, "Eletrica", StringComparison.Ordinal) ||
                        !string.Equals(contatoText, "Contato Smoke Atualizado | Compras", StringComparison.Ordinal) ||
                        !string.Equals(prazoText, "12 dia(s)", StringComparison.Ordinal) ||
                        !string.Equals(prazoPagamentoText, "21 dia(s)", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("A ficha do fornecedor nao refletiu prazos, categoria ou contato editados.");
                    }
                }
                finally
                {
                    if (editarWindow?.IsVisible == true)
                    {
                        editarWindow.Close();
                    }

                    if (visualizarWindow?.IsVisible == true)
                    {
                        visualizarWindow.Close();
                    }

                    if (repository.ObterPorId(fornecedor.Id) != null)
                    {
                        repository.Excluir(fornecedor.Id);
                    }
                }
            });
        }

        private static Fornecedor CriarFornecedorIsoladoSmoke(string finalidade)
        {
            var token = $"{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
            return new Fornecedor
            {
                RazaoSocial = $"Fornecedor Exclusao {finalidade} {token} LTDA",
                NomeFantasia = $"Fornecedor Exclusao {finalidade} {token}",
                Categoria = "Pecas",
                CategoriaPreferencial = "Pecas",
                PrazoMedioPagamentoDias = 14,
                PrazoMedioEntregaDias = 2,
                Nota = 5,
                Observacoes = "Fornecedor sintetico isolado para validar exclusao individual.",
                DataCadastro = DateTime.Now,
                Ativo = true
            };
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
                command.Parameters.AddWithValue("@Status", StatusImportacao.Atualizado.ToString());
                command.Parameters.AddWithValue("@ProdutoExistenteId", produto.Id.ToString());
                command.Parameters.AddWithValue("@MotivoIgnorado", DBNull.Value);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static string CriarArquivoClienteSmoke(string prefixo, string conteudo)
        {
            var pasta = Path.Combine(
                App.RuntimeAppDataPath,
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
                App.RuntimeAppDataPath,
                "AutomatedTests",
                "ProdutosAnexos");
            Directory.CreateDirectory(pasta);

            var caminho = Path.Combine(pasta, $"{prefixo}-{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.txt");
            File.WriteAllText(caminho, conteudo, Encoding.UTF8);
            return caminho;
        }

    }
}
