using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.UserControls
{
    public partial class EstoqueControl : UserControl
    {
        private const decimal MargemBaixaPercentual = 20m;

        private readonly PermissionService _permissionService;
        private readonly EstoqueOperationalService _estoqueOperationalService;
        private readonly ProdutoEtiquetaService _produtoEtiquetaService;

        private List<Produto> _todosProdutos = new();
        private List<ProdutoGridItem> _gridItems = new();

        public EstoqueControl()
        {
            InitializeComponent();

            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);
            _estoqueOperationalService = new EstoqueOperationalService(App.Database, App.Logger);
            _produtoEtiquetaService = new ProdutoEtiquetaService();

            Focusable = true;
            PreviewKeyDown += EstoqueControl_PreviewKeyDown;
            Loaded += EstoqueControl_Loaded;
        }

        private void EstoqueControl_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarProdutos();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (BuscaProdutoTextBox != null)
                {
                    BuscaProdutoTextBox.Focus();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void EstoqueControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NovoProdutoButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                BuscaProdutoTextBox?.Focus();
                BuscaProdutoTextBox?.SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter
                && Keyboard.Modifiers == ModifierKeys.None
                && ProdutosDataGrid != null
                && ProdutosDataGrid.IsKeyboardFocusWithin
                && ObterProdutoSelecionado(null) != null)
            {
                EditarProdutoSelecionado(this);
                e.Handled = true;
            }
        }

        private void RetryEstoqueButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarProdutos();
        }

        private enum EstoquePainelEstado
        {
            Loading,
            Loaded,
            Empty,
            Error
        }

        private void DefinirEstadoPainel(EstoquePainelEstado estado)
        {
            if (EstoqueLoadingPanel != null)
                EstoqueLoadingPanel.Visibility = estado == EstoquePainelEstado.Loading ? Visibility.Visible : Visibility.Collapsed;
            if (EstoqueErrorPanel != null)
                EstoqueErrorPanel.Visibility = estado == EstoquePainelEstado.Error ? Visibility.Visible : Visibility.Collapsed;
            if (EstoqueEmptyPanel != null)
                EstoqueEmptyPanel.Visibility = estado == EstoquePainelEstado.Empty ? Visibility.Visible : Visibility.Collapsed;
            if (EstoqueContentGrid != null)
                EstoqueContentGrid.Visibility = estado == EstoquePainelEstado.Loaded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CarregarProdutos()
        {
            DefinirEstadoPainel(EstoquePainelEstado.Loading);

            try
            {
                _todosProdutos = App.Repositories.Produtos.ObterTodos();
                _estoqueOperationalService.EnriquecerProdutosComReservas(_todosProdutos);

                var gridItems = _todosProdutos
                    .Select(CriarGridItem)
                    .ToList();
                AplicarCurvaAbc(gridItems);

                _gridItems = gridItems
                    .OrderBy(item => item.Nome)
                    .ToList();

                AtualizarMetricas();
                AtualizarFiltros();
                AtualizarInsights();
                AplicarFiltros();

                DefinirEstadoPainel(_todosProdutos.Count == 0
                    ? EstoquePainelEstado.Empty
                    : EstoquePainelEstado.Loaded);
            }
            catch (Exception ex)
            {
                if (EstoqueErrorDescriptionText != null)
                {
                    EstoqueErrorDescriptionText.Text = ex.Message;
                }

                DefinirEstadoPainel(EstoquePainelEstado.Error);
                ExibirMensagem(
                    $"Erro ao carregar produtos:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxImage.Error,
                    ex);
            }
        }

        private void AtualizarMetricas()
        {
            var valorEstoque = _todosProdutos.Sum(produto => produto.ValorTotalEstoque);
            var estoqueBaixo = _todosProdutos.Count(produto => produto.QuantidadeDisponivel <= produto.QuantidadeMinima);
            var produtosVencidos = _todosProdutos.Count(ProdutoEstaVencido);
            var maisVendido = _gridItems
                .Where(item => item.TotalVendas > 0)
                .OrderByDescending(item => item.TotalVendas)
                .ThenByDescending(item => item.ReceitaEstimadaTotal)
                .ThenBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            var itensZerados = _todosProdutos.Count(produto => produto.QuantidadeDisponivel <= 0);

            TotalProdutosText.Text = _todosProdutos.Count.ToString();
            ValorEstoqueText.Text = valorEstoque.ToString("C2");
            EstoqueBaixoText.Text = estoqueBaixo.ToString();
            if (ItensZeradosText != null)
            {
                ItensZeradosText.Text = itensZerados.ToString();
            }
            ProdutosVencidosText.Text = produtosVencidos.ToString();
            MaisVendidoText.Text = maisVendido == null
                ? "Sem vendas registradas"
                : $"{maisVendido.Nome}\n{maisVendido.TotalVendas} un. | {maisVendido.ReceitaEstimadaTotal:C2}";
        }

        private void AtualizarFiltros()
        {
            AtualizarCombo(CategoriaFiltroComboBox, new[] { "Todos" }
                .Concat(_todosProdutos
                    .Select(produto => produto.Categoria)
                    .Where(categoria => !string.IsNullOrWhiteSpace(categoria))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(categoria => categoria))
                .ToList());

            AtualizarCombo(FornecedorFiltroComboBox, new[] { "Todos" }
                .Concat(_todosProdutos
                    .Select(produto => string.IsNullOrWhiteSpace(produto.Fornecedor) ? "Sem fornecedor" : produto.Fornecedor)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(fornecedor => fornecedor))
                .ToList());

            AtualizarCombo(StatusFiltroComboBox, new[]
            {
                "Todos",
                "Incompletos",
                "Estoque Baixo",
                "Estoque Alto",
                "Sem Fornecedor",
                "Sem Preco",
                "Sem Codigo/SKU",
                "Margem Baixa",
                "Margem Negativa",
                "Vencidos",
                "Prox. ao vencimento",
                "Produtos Parados",
                "Mais Vendidos",
                "Vendidos no Mes",
                "Curva A",
                "Curva B",
                "Curva C",
                "Sem foto"
            });
        }

        private static void AtualizarCombo(ComboBox comboBox, IEnumerable<string> items)
        {
            var current = comboBox.SelectedItem as string;

            comboBox.ItemsSource = items.ToList();

            comboBox.SelectedItem = current != null && comboBox.Items.Contains(current)
                ? current
                : comboBox.Items.Cast<object?>().FirstOrDefault();
        }

        private void AtualizarInsights()
        {
            var criticos = _gridItems.Where(item => item.EstoqueCritico).Take(3).Select(item => item.Nome).ToList();
            var excessoEstoque = _gridItems.Where(item => item.EstoqueAcimaMaximo).Take(3).Select(item => item.Nome).ToList();
            var vencendo = _gridItems.Where(item => item.VencimentoProximo).Take(3).Select(item => item.Nome).ToList();
            var altaMargem = _gridItems.Where(item => item.MargemLucro >= 50m).Take(3).Select(item => item.Nome).ToList();
            var baixaMargem = _gridItems.Where(item => item.MargemBaixa).Take(3).Select(item => item.Nome).ToList();
            var baixoGiro = _gridItems.Where(item => item.ProdutoParado).Take(3).Select(item => item.Nome).ToList();
            var maisVendidos = _gridItems
                .Where(item => item.TotalVendas > 0)
                .OrderByDescending(item => item.TotalVendas)
                .ThenByDescending(item => item.ReceitaEstimadaTotal)
                .Take(5)
                .Select(item => $"{item.Nome} ({item.TotalVendas} un.)")
                .ToList();
            var maisVendidosMes = _gridItems
                .Where(item => item.VendasUltimoMes > 0)
                .OrderByDescending(item => item.VendasUltimoMes)
                .ThenByDescending(item => item.ReceitaEstimadaMes)
                .Take(3)
                .Select(item => $"{item.Nome} ({item.VendasUltimoMes} un.)")
                .ToList();
            var incompletos = _gridItems.Count(item => item.Incompleto);
            var semFoto = _gridItems.Count(item => !item.TemImagem);
            var semCodigo = _gridItems.Count(item => item.SemCodigoOperacional);
            var semPreco = _gridItems.Count(item => item.PrecoVenda <= 0);
            var semFornecedor = _gridItems.Count(item => string.IsNullOrWhiteSpace(item.Fornecedor));
            var totalBaixaMargem = _gridItems.Count(item => item.MargemBaixa);
            var curvaA = _gridItems.Where(item => string.Equals(item.CurvaAbc, "A", StringComparison.OrdinalIgnoreCase)).ToList();
            var curvaB = _gridItems.Count(item => string.Equals(item.CurvaAbc, "B", StringComparison.OrdinalIgnoreCase));
            var curvaC = _gridItems.Count(item => string.Equals(item.CurvaAbc, "C", StringComparison.OrdinalIgnoreCase));
            var curvaATop = curvaA
                .OrderByDescending(item => item.ValorTotalEstoque)
                .Take(3)
                .Select(item => $"{item.Nome} ({item.ParticipacaoEstoquePercentual:N1}%)")
                .ToList();
            var valorMedio = _gridItems.Count == 0 ? 0m : _gridItems.Average(item => item.ValorTotalEstoque);
            var diasMedios = _gridItems.Where(item => item.DiasSemSaida > 0).Select(item => item.DiasSemSaida).DefaultIfEmpty(0).Average();

            EstoqueCriticoResumoText.Text = criticos.Count == 0
                ? "Nenhum item abaixo do minimo no momento."
                : $"{criticos.Count} itens em maior risco agora: {string.Join(", ", criticos)}.";

            if (excessoEstoque.Count > 0)
            {
                EstoqueCriticoResumoText.Text += $" Acima do maximo: {string.Join(", ", excessoEstoque)}.";
            }

            ProdutosVencimentoResumoText.Text = vencendo.Count == 0
                ? "Nenhum produto proximo ao vencimento foi encontrado."
                : $"{vencendo.Count} itens exigem conferencia de validade: {string.Join(", ", vencendo)}.";

            var resumoMargem = altaMargem.Count == 0
                ? "Sem oportunidades de margem elevada identificadas hoje."
                : $"{altaMargem.Count} itens com margem acima de 50%: {string.Join(", ", altaMargem)}.";
            var resumoAlertasComerciais = $"Alertas comerciais: {semPreco} sem preco, {semFornecedor} sem fornecedor e {totalBaixaMargem} com margem baixa.";
            if (baixaMargem.Count > 0)
            {
                resumoAlertasComerciais += $" Revisar: {string.Join(", ", baixaMargem)}.";
            }

            OportunidadeMargemResumoText.Text = $"{resumoMargem} {resumoAlertasComerciais}";

            MaisVendidosResumoText.Text = maisVendidos.Count == 0
                ? "Ranking de vendas ainda sem historico suficiente."
                : $"Mais vendidos: {string.Join(", ", maisVendidos)}. No mes: {(maisVendidosMes.Count == 0 ? "sem vendas registradas" : string.Join(", ", maisVendidosMes))}.";

            CurvaAbcResumoText.Text = curvaA.Count == 0
                ? "Curva ABC sem valor de estoque suficiente para classificar itens A/B/C."
                : $"Curva ABC: {curvaA.Count} item(ns) A, {curvaB} B e {curvaC} C. Top A: {string.Join(", ", curvaATop)}.";

            GiroEstoqueResumoText.Text = $"Giro medio estimado: {diasMedios:N0} dias sem saida por item monitorado.";
            ValorMedioProdutoResumoText.Text = $"Valor medio por item em estoque: {valorMedio:C}.";
            BaixoGiroResumoText.Text = baixoGiro.Count == 0
                ? $"Produtos para revisar: {incompletos} incompletos, {semFoto} sem foto e {semCodigo} sem codigo/SKU."
                : $"{baixoGiro.Count} itens parados: {string.Join(", ", baixoGiro)}. Produtos para revisar: {incompletos} incompletos, {semFoto} sem foto e {semCodigo} sem codigo/SKU.";
        }

        private void AplicarFiltros()
        {
            if (BuscaProdutoTextBox == null ||
                CategoriaFiltroComboBox == null ||
                FornecedorFiltroComboBox == null ||
                StatusFiltroComboBox == null ||
                ProdutosDataGrid == null)
            {
                return;
            }

            IEnumerable<ProdutoGridItem> consulta = _gridItems;

            var termo = BuscaProdutoTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                consulta = consulta.Where(item =>
                    Contem(item.Nome, termo)
                    || Contem(item.Codigo, termo)
                    || Contem(item.Categoria, termo)
                    || Contem(item.Modelo, termo)
                    || Contem(item.CodigoBarras, termo)
                    || Contem(item.SKU, termo)
                    || Contem(item.Fornecedor, termo)
                    || Contem(item.UnidadeMedida, termo)
                    || Contem(item.CurvaAbc, termo)
                    || Contem(item.NCM, termo)
                    || Contem(item.Localizacao, termo));
            }

            var categoria = CategoriaFiltroComboBox.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(categoria) &&
                !string.Equals(categoria, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                consulta = consulta.Where(item => string.Equals(item.Categoria, categoria, StringComparison.OrdinalIgnoreCase));
            }

            var fornecedor = FornecedorFiltroComboBox.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(fornecedor) &&
                !string.Equals(fornecedor, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                consulta = string.Equals(fornecedor, "Sem fornecedor", StringComparison.OrdinalIgnoreCase)
                    ? consulta.Where(item => string.IsNullOrWhiteSpace(item.Fornecedor))
                    : consulta.Where(item => string.Equals(item.Fornecedor, fornecedor, StringComparison.OrdinalIgnoreCase));
            }

            var status = StatusFiltroComboBox.SelectedItem as string;

            consulta = status switch
            {
                "Incompletos" => consulta.Where(item => item.Incompleto),
                "Estoque Baixo" => consulta.Where(item => item.EstoqueCritico),
                "Estoque Alto" => consulta.Where(item => item.EstoqueAcimaMaximo),
                "Sem Fornecedor" => consulta.Where(item => string.IsNullOrWhiteSpace(item.Fornecedor)),
                "Sem Preco" => consulta.Where(item => item.PrecoVenda <= 0),
                "Sem Codigo/SKU" => consulta.Where(item => item.SemCodigoOperacional),
                "Margem Baixa" => consulta.Where(item => item.MargemBaixa),
                "Margem Negativa" => consulta.Where(item => item.MargemLucro < 0),
                "Vencidos" => consulta.Where(item => item.ProdutoVencido),
                "Prox. ao vencimento" => consulta.Where(item => item.VencimentoProximo),
                "Produtos Parados" => consulta.Where(item => item.ProdutoParado),
                "Mais Vendidos" => consulta
                    .Where(item => item.TotalVendas > 0)
                    .OrderByDescending(item => item.TotalVendas)
                    .ThenByDescending(item => item.ReceitaEstimadaTotal),
                "Vendidos no Mes" => consulta
                    .Where(item => item.VendasUltimoMes > 0)
                    .OrderByDescending(item => item.VendasUltimoMes)
                    .ThenByDescending(item => item.ReceitaEstimadaMes),
                "Curva A" => consulta.Where(item => string.Equals(item.CurvaAbc, "A", StringComparison.OrdinalIgnoreCase)),
                "Curva B" => consulta.Where(item => string.Equals(item.CurvaAbc, "B", StringComparison.OrdinalIgnoreCase)),
                "Curva C" => consulta.Where(item => string.Equals(item.CurvaAbc, "C", StringComparison.OrdinalIgnoreCase)),
                "Sem foto" => consulta.Where(item => !item.TemImagem),
                _ => consulta
            };

            ProdutosDataGrid.ItemsSource = consulta.ToList();
            AtualizarFichaProdutoSelecionado();
        }
        private void ProdutosDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarFichaProdutoSelecionado();
        }

        private void AtualizarFichaProdutoSelecionado()
        {
            if (ProdutoFichaTituloText == null || ProdutoFichaDetalheText == null)
            {
                return;
            }

            var produto = ObterProdutoSelecionado(null);
            if (produto == null)
            {
                ProdutoFichaTituloText.Text = "Selecione um produto";
                ProdutoFichaDetalheText.Text = "A ficha mostra identificacao, estoque, precos, localizacao e fornecedor do item selecionado.";
                return;
            }

            ProdutoFichaTituloText.Text = produto.Nome;
            ProdutoFichaDetalheText.Text =
                $"Codigo: {produto.Codigo}\n" +
                $"Categoria: {produto.Categoria}\n" +
                $"Unidade: {produto.UnidadeMedida}\n" +
                $"Marca: {produto.Marca}\n" +
                $"Estoque: {produto.QuantidadeEstoque} | Min: {produto.QuantidadeMinima} | Disp: {produto.QuantidadeDisponivel}\n" +
                $"Custo: {produto.PrecoCompra:C2} | Venda: {produto.PrecoVenda:C2}\n" +
                $"Localizacao: {produto.Localizacao}\n" +
                $"Fornecedor: {(string.IsNullOrWhiteSpace(produto.Fornecedor) ? "-" : produto.Fornecedor)}\n" +
                $"Barras: {(string.IsNullOrWhiteSpace(produto.CodigoBarras) ? "-" : produto.CodigoBarras)} | SKU: {(string.IsNullOrWhiteSpace(produto.SKU) ? "-" : produto.SKU)}";
        }

        private void NovoProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ESTOQUE_CRIAR", "Voce nao possui permissao para cadastrar produtos no estoque."))
            {
                return;
            }

            var novoProdutoWindow = new NovoProdutoWindow();
            ConfigurarOwner(novoProdutoWindow);

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(novoProdutoWindow, "NovoProdutoWindow");
                return;
            }

            if (novoProdutoWindow.ShowDialog() == true)
            {
                CarregarProdutos();
            }
        }

        private void EditarProdutoSelecionadoButton_Click(object sender, RoutedEventArgs e)
        {
            EditarProdutoSelecionado(sender);
        }

        private void ProdutosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProdutosDataGrid.SelectedItem is ProdutoGridItem)
            {
                EditarProdutoSelecionado(sender);
            }
        }

        private void EditarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            EditarProdutoSelecionado(sender);
        }

        private void EditarProdutoSelecionado(object? sender)
        {
            try
            {
                if (!ValidarPermissao("ESTOQUE_EDITAR", "Voce nao possui permissao para editar produtos."))
                {
                    return;
                }

                var produto = ObterProdutoSelecionado(sender);

                if (produto == null)
                {
                    ExibirMensagem(
                        "Selecione um produto na tabela para editar.\n\nDica: clique em uma linha do estoque e depois em Editar produto, ou d├¬ duplo clique diretamente na linha.",
                        "Editar produto",
                        MessageBoxImage.Information);
                    return;
                }

                var editarProdutoWindow = new EditarProdutoWindow(produto);
                ConfigurarOwner(editarProdutoWindow);

                if (App.IsAutomatedTestMode)
                {
                    ValidarJanelaEmAutomacao(editarProdutoWindow, "EditarProdutoWindow");
                    return;
                }

                if (editarProdutoWindow.ShowDialog() == true)
                {
                    CarregarProdutos();

                    ExibirMensagem(
                        "Produto atualizado com sucesso.",
                        "Estoque",
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao editar produto:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxImage.Error,
                    ex);
            }
        }

        private void AjustarEstoqueButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PodeGerenciarAjustesEstoque())
            {
                ExibirMensagem(
                    "Voce nao possui permissao para ajustar estoque ou reajustar precos.",
                    UiText.T("AccessDenied"),
                    MessageBoxImage.Warning);
                return;
            }

            var ajusteEstoqueWindow = new AjusteEstoqueWindow();
            ConfigurarOwner(ajusteEstoqueWindow);

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(ajusteEstoqueWindow, "AjusteEstoqueWindow");
                return;
            }

            if (ajusteEstoqueWindow.ShowDialog() == true)
            {
                CarregarProdutos();
            }
        }

        private void EntradaEstoqueButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrarMovimentacaoDedicada(sender, "Entrada");
        }

        private void SaidaEstoqueButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrarMovimentacaoDedicada(sender, "Saida");
        }

        private void RegistrarMovimentacaoDedicada(object? sender, string operacao)
        {
            if (!ValidarPermissao("ESTOQUE_AJUSTAR", "Voce nao possui permissao para registrar entradas e saidas de estoque."))
            {
                return;
            }

            var produto = ObterProdutoSelecionado(sender);

            if (produto == null)
            {
                ExibirMensagem($"Selecione um produto para registrar {operacao.ToLowerInvariant()} de estoque.", "Estoque", MessageBoxImage.Information);
                return;
            }

            if (App.IsAutomatedTestMode)
            {
                if (App.IsSmokeTestMode && App.IsIsolatedAutomatedAppData)
                {
                    _estoqueOperationalService.RegistrarMovimentacaoManual(
                        produto.Id,
                        1,
                        operacao,
                        $"Movimentacao dedicada de {operacao.ToLowerInvariant()} validada pelo smoke test.",
                        App.Session.UserName);

                    CarregarProdutos();
                }

                App.Logger.LogInfo(
                    $"Movimentacao dedicada de {operacao} validada em automacao para produto {produto.Codigo}.",
                    "Estoque");
                return;
            }

            var isEntrada = string.Equals(operacao, "Entrada", StringComparison.OrdinalIgnoreCase);
            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = $"{operacao} de estoque",
                Header = isEntrada ? "Registrar entrada dedicada" : "Registrar saida dedicada",
                Subheader = $"Produto: {produto.Nome}\nCodigo: {produto.Codigo}\nEstoque atual: {produto.QuantidadeEstoque}\nReservado: {produto.QuantidadeReservada}\nDisponivel: {produto.QuantidadeDisponivel}",
                ValorLabel = "Quantidade",
                ObservacoesLabel = "Motivo da movimentacao",
                ConfirmButtonText = isEntrada ? "Registrar entrada" : "Registrar saida",
                PermitirZero = false,
                ObservacoesObrigatorias = true,
                ValorInicial = 1
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (decimal.Truncate(dialog.ValorInformado) != dialog.ValorInformado ||
                dialog.ValorInformado > int.MaxValue)
            {
                ExibirMensagem("A quantidade precisa ser um numero inteiro valido.", "Estoque", MessageBoxImage.Warning);
                return;
            }

            var quantidade = decimal.ToInt32(dialog.ValorInformado);
            var permitirDisponivelNegativo = false;
            var disponibilidadeFinal = produto.QuantidadeDisponivel - quantidade;

            if (!isEntrada && disponibilidadeFinal < 0)
            {
                if (!_permissionService.TemPermissaoCodigo("ESTOQUE_PERMITIR_NEGATIVO"))
                {
                    ExibirMensagem(
                        $"A saida excede a disponibilidade operacional do produto.\n\nDisponivel atual: {produto.QuantidadeDisponivel}\nQuantidade solicitada: {quantidade}",
                        "Estoque",
                        MessageBoxImage.Warning);
                    return;
                }

                permitirDisponivelNegativo = CriticalActionDialogService.ConfirmarAcao(
                    Window.GetWindow(this),
                    new CriticalActionRequest
                    {
                        WindowTitle = "Confirmar saida acima do disponivel",
                        Header = "Saida com disponibilidade negativa",
                        Summary = $"Voce esta prestes a registrar saida de {quantidade} unidade(s) para {produto.Nome}.",
                        Details = $"Codigo: {produto.Codigo}\nEstoque atual: {produto.QuantidadeEstoque}\nReservado: {produto.QuantidadeReservada}\nDisponivel atual: {produto.QuantidadeDisponivel}\nDisponivel apos saida: {disponibilidadeFinal}",
                        Impact = "A movimentacao pode comprometer itens ja reservados ou deixar o estoque fisico negativo. Use apenas sob responsabilidade gerencial.",
                        Keyword = "NEGATIVO",
                        ConfirmButtonText = "Registrar saida mesmo assim"
                    });

                if (!permitirDisponivelNegativo)
                {
                    return;
                }
            }

            try
            {
                _estoqueOperationalService.RegistrarMovimentacaoManual(
                    produto.Id,
                    quantidade,
                    operacao,
                    dialog.ObservacoesInformadas,
                    App.Session.UserName,
                    permitirDisponivelNegativo);

                CarregarProdutos();

                ExibirMensagem($"{operacao} registrada com sucesso.", "Estoque", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao registrar {operacao.ToLowerInvariant()}:\n{ex.Message}", UiText.T("Error"), MessageBoxImage.Error, ex);
            }
        }

        private void InventariarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ESTOQUE_INVENTARIAR", "Voce nao possui permissao para registrar inventario fisico."))
            {
                return;
            }

            var produto = ObterProdutoSelecionado(sender);

            if (produto == null)
            {
                ExibirMensagem("Selecione um produto para registrar o inventario.", "Inventario", MessageBoxImage.Information);
                return;
            }

            if (App.IsAutomatedTestMode)
            {
                RegistrarInventarioEmAutomacao(produto);
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Inventario de estoque",
                Header = "Contagem fisica do produto",
                Subheader = $"Produto: {produto.Nome}\nCodigo: {produto.Codigo}\nReservado atualmente: {produto.QuantidadeReservada}",
                ValorLabel = "Quantidade contada",
                ObservacoesLabel = "Motivo do inventario",
                ConfirmButtonText = "Registrar inventario",
                PermitirZero = true,
                ObservacoesObrigatorias = true,
                ValorInicial = produto.QuantidadeEstoque
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (decimal.Truncate(dialog.ValorInformado) != dialog.ValorInformado)
            {
                ExibirMensagem("A quantidade contada precisa ser um numero inteiro.", "Inventario", MessageBoxImage.Warning);
                return;
            }

            try
            {
                _estoqueOperationalService.RegistrarInventario(
                    produto.Id,
                    decimal.ToInt32(dialog.ValorInformado),
                    dialog.ObservacoesInformadas,
                    App.Session.UserName);

                CarregarProdutos();

                ExibirMensagem("Inventario registrado com sucesso.", "Inventario", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao registrar inventario:\n{ex.Message}", UiText.T("Error"), MessageBoxImage.Error, ex);
            }
        }

        private void HistoricoEstoqueButton_Click(object sender, RoutedEventArgs e)
        {
            var produto = ObterProdutoSelecionado(sender);

            if (produto == null)
            {
                ExibirMensagem("Selecione um produto para consultar o historico operacional.", "Historico", MessageBoxImage.Information);
                return;
            }

            try
            {
                var historico = _estoqueOperationalService.ObterHistoricoProduto(produto.Id, limite: 40);

                if (App.IsAutomatedTestMode)
                {
                    App.Logger.LogInfo(
                        $"Historico operacional validado em automacao para produto {produto.Codigo} com {historico.Count} registro(s).",
                        "Estoque");
                    return;
                }

                var window = new HistoricoEstoqueWindow(produto, historico);
                ConfigurarOwner(window);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao abrir historico operacional:\n{ex.Message}", UiText.T("Error"), MessageBoxImage.Error, ex);
            }
        }

        private void EtiquetaProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ESTOQUE_VER", "Voce nao possui permissao para gerar etiquetas de produtos."))
            {
                return;
            }

            var produto = ObterProdutoSelecionado(sender);

            if (produto == null)
            {
                ExibirMensagem("Selecione um produto para gerar a etiqueta.", "Etiqueta", MessageBoxImage.Information);
                return;
            }

            try
            {
                var resultado = _produtoEtiquetaService.GerarEtiquetas(produto);

                App.Logger.LogInfo(
                    $"Etiqueta gerada para produto {produto.Codigo} em {resultado.CaminhoArquivo}.",
                    "Estoque");

                if (App.IsAutomatedTestMode)
                {
                    return;
                }

                ExibirMensagem(
                    $"Etiqueta gerada com sucesso.\n\nArquivo: {resultado.CaminhoArquivo}\nQuantidade: {resultado.QuantidadeEtiquetas}",
                    "Etiqueta",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao gerar etiqueta:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxImage.Error,
                    ex);
            }
        }

        private void VisualizarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var produto = ObterProdutoSelecionado(sender);

                if (produto == null)
                {
                    return;
                }

                var detalhes =
                    $"Codigo: {produto.Codigo}\n" +
                    $"Nome: {produto.Nome}\n" +
                    $"Categoria: {produto.Categoria}\n" +
                    $"Fornecedor: {produto.Fornecedor}\n" +
                    $"Codigo de barras: {produto.CodigoBarras}\n" +
                    $"SKU: {produto.SKU}\n" +
                    $"Unidade: {produto.UnidadeMedida}\n" +
                    $"NCM: {produto.NCMS}\n" +
                    $"CEST: {produto.CEST}\n" +
                    $"CFOP: {produto.CFOP}\n" +
                    $"Estoque: {produto.QuantidadeEstoque}\n" +
                    $"Reservado: {produto.QuantidadeReservada}\n" +
                    $"Disponivel: {produto.QuantidadeDisponivel}\n" +
                    $"Preco compra: {produto.PrecoCompra:C2}\n" +
                    $"Preco venda: {produto.PrecoVenda:C2}\n" +
                    $"Margem: {produto.MargemLucro:N1}%\n" +
                    $"Ultima compra: {(produto.DataUltimaCompra?.ToString("dd/MM/yyyy") ?? "-")}\n" +
                    $"Ultima venda: {(produto.DataUltimaVenda?.ToString("dd/MM/yyyy") ?? "-")}\n" +
                    $"Localizacao: {produto.Localizacao}\n" +
                    $"Prateleira: {produto.Prateleira}\n" +
                    $"Gaveta: {produto.Gaveta}\n" +
                    $"Imagem: {(string.IsNullOrWhiteSpace(produto.ImagemUrl) ? "Sem imagem" : produto.ImagemUrl)}\n" +
                    $"Anexos: {ProdutoMediaService.DeserializeAttachmentPaths(produto.Anexos).Count}\n" +
                    $"Status: {(produto.Ativo ? "Ativo" : "Inativo")}";

                ExibirMensagem(detalhes, "Detalhes do Produto", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao visualizar produto:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxImage.Error,
                    ex);
            }
        }

        private void ExcluirProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("ESTOQUE_EXCLUIR", "Voce nao possui permissao para excluir produtos."))
                {
                    return;
                }

                var produto = ObterProdutoSelecionado(sender);

                if (produto == null)
                {
                    return;
                }

                if (!CriticalActionDialogService.ConfirmarExclusao(
                    Window.GetWindow(this),
                    "produto",
                    produto.Nome,
                    $"Codigo: {produto.Codigo}\nCategoria: {produto.Categoria}\nEstoque atual: {produto.QuantidadeEstoque}",
                    "O produto sera removido do cadastro local. Revise estoque, referencias e historicos relacionados antes de confirmar."))
                {
                    return;
                }

                var anexosParaRemover = ProdutoMediaService.DeserializeAttachmentPaths(produto.Anexos);

                App.Repositories.Produtos.Excluir(produto.Id);
                ProdutoMediaService.DeleteManagedImageIfOwned(produto.ImagemUrl);
                foreach (var anexo in anexosParaRemover)
                {
                    ProdutoMediaService.DeleteManagedAttachmentIfOwned(anexo);
                }

                CarregarProdutos();

                ExibirMensagem(
                    UiText.T("ProductDeleted"),
                    UiText.T("Success"),
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao excluir produto:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxImage.Error,
                    ex);
            }
        }

        private bool PodeGerenciarAjustesEstoque()
        {
            return _permissionService.TemPermissaoCodigo("ESTOQUE_AJUSTAR")
                || _permissionService.TemPermissaoCodigo("ESTOQUE_AJUSTAR_PRECO");
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            ExibirMensagem(mensagem, UiText.T("AccessDenied"), MessageBoxImage.Warning);
            return false;
        }

        private void BuscaProdutoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void CategoriaFiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void FornecedorFiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void StatusFiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private Produto? ObterProdutoSelecionado(object? sender)
        {
            if (sender is FrameworkElement { DataContext: ProdutoGridItem item })
            {
                return item.Produto;
            }

            return ProdutosDataGrid.SelectedItem is ProdutoGridItem selectedItem
                ? selectedItem.Produto
                : null;
        }

        private void RegistrarInventarioEmAutomacao(Produto produto)
        {
            try
            {
                _estoqueOperationalService.RegistrarInventario(
                    produto.Id,
                    produto.QuantidadeEstoque,
                    "Inventario validado automaticamente pelo smoke test.",
                    App.Session.UserName);

                CarregarProdutos();

                App.Logger.LogInfo(
                    $"Inventario operacional validado em automacao para produto {produto.Codigo}.",
                    "Estoque");
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao registrar inventario:\n{ex.Message}", UiText.T("Error"), MessageBoxImage.Error, ex);
            }
        }

        private static bool ProdutoEstaVencido(Produto produto)
        {
            return produto.ProdutoPerecivel
                && produto.DataValidade.HasValue
                && produto.DataValidade.Value.Date < DateTime.Today;
        }

        private static bool ProdutoVenceEmBreve(Produto produto)
        {
            return produto.ProdutoPerecivel
                && produto.DataValidade.HasValue
                && produto.DataValidade.Value.Date >= DateTime.Today
                && produto.DataValidade.Value.Date <= DateTime.Today.AddDays(30);
        }

        private static bool ProdutoEstaIncompleto(Produto produto)
        {
            return string.IsNullOrWhiteSpace(ProdutoMediaService.ResolveExistingPath(produto.ImagemUrl))
                || string.IsNullOrWhiteSpace(produto.CodigoBarras)
                || string.IsNullOrWhiteSpace(produto.SKU)
                || string.IsNullOrWhiteSpace(produto.Categoria)
                || string.IsNullOrWhiteSpace(produto.Fornecedor)
                || string.IsNullOrWhiteSpace(produto.UnidadeMedida)
                || string.IsNullOrWhiteSpace(produto.Localizacao)
                || string.IsNullOrWhiteSpace(produto.NCMS)
                || produto.PrecoVenda <= 0;
        }

        private static ProdutoGridItem CriarGridItem(Produto produto)
        {
            var temImagem = !string.IsNullOrWhiteSpace(ProdutoMediaService.ResolveExistingPath(produto.ImagemUrl));
            var incompleto = ProdutoEstaIncompleto(produto);
            var ultimaSaida = produto.DataUltimaVenda ?? produto.DataCadastro;
            var diasSemSaida = (int)Math.Max(0, (DateTime.Today - ultimaSaida.Date).TotalDays);
            var estoqueAcimaMaximo = produto.QuantidadeMaxima > 0 && produto.QuantidadeDisponivel > produto.QuantidadeMaxima;
            var produtoParado = produto.Ativo && produto.QuantidadeDisponivel > 0 && diasSemSaida >= 90;
            var semCodigoOperacional = string.IsNullOrWhiteSpace(produto.CodigoBarras) || string.IsNullOrWhiteSpace(produto.SKU);
            var margemBaixa = produto.PrecoVenda > 0 && produto.MargemLucro < MargemBaixaPercentual;

            var alerta = string.Empty;

            if (ProdutoEstaVencido(produto))
            {
                alerta = "Produto vencido";
            }
            else if (ProdutoVenceEmBreve(produto))
            {
                alerta = "Validade proxima";
            }
            else if (produto.QuantidadeDisponivel <= produto.QuantidadeMinima)
            {
                alerta = "Reposicao urgente";
            }
            else if (estoqueAcimaMaximo)
            {
                alerta = "Acima do maximo";
            }
            else if (string.IsNullOrWhiteSpace(produto.Fornecedor))
            {
                alerta = "Sem fornecedor";
            }
            else if (produto.PrecoVenda <= 0)
            {
                alerta = "Sem preco de venda";
            }
            else if (produto.MargemLucro < 0)
            {
                alerta = "Margem negativa";
            }
            else if (margemBaixa)
            {
                alerta = "Margem baixa";
            }
            else if (semCodigoOperacional)
            {
                alerta = "Sem codigo/SKU";
            }
            else if (produtoParado)
            {
                alerta = "Produto parado";
            }
            else if (incompleto)
            {
                alerta = "Cadastro incompleto";
            }

            return new ProdutoGridItem
            {
                Produto = produto,
                ImagemUrl = produto.ImagemUrl,
                TemImagem = temImagem,
                Incompleto = incompleto,
                Codigo = produto.Codigo,
                Nome = produto.Nome,
                Categoria = produto.Categoria,
                Modelo = produto.Modelo,
                Fornecedor = produto.Fornecedor,
                CodigoBarras = produto.CodigoBarras,
                SKU = produto.SKU,
                QuantidadeAnexos = ProdutoMediaService.DeserializeAttachmentPaths(produto.Anexos).Count,
                UnidadeMedida = produto.UnidadeMedida,
                NCM = produto.NCMS,
                Localizacao = produto.Localizacao,
                QuantidadeEstoque = produto.QuantidadeEstoque,
                QuantidadeMinima = produto.QuantidadeMinima,
                QuantidadeMaxima = produto.QuantidadeMaxima,
                QuantidadeReservada = produto.QuantidadeReservada,
                QuantidadeDisponivel = produto.QuantidadeDisponivel,
                PrecoCompra = produto.PrecoCompra,
                PrecoVenda = produto.PrecoVenda,
                MargemLucro = produto.MargemLucro,
                ValorTotalEstoque = produto.ValorTotalEstoque,
                DataUltimaCompra = produto.DataUltimaCompra,
                DataUltimaVenda = produto.DataUltimaVenda,
                TotalVendas = produto.TotalVendas,
                VendasUltimoMes = produto.VendasUltimoMes,
                VendasUltimoTrimestre = produto.VendasUltimoTrimestre,
                ReceitaEstimadaTotal = produto.TotalVendas * produto.PrecoVenda,
                ReceitaEstimadaMes = produto.VendasUltimoMes * produto.PrecoVenda,
                AlertaPrincipal = string.IsNullOrWhiteSpace(alerta) ? "Operacao normal" : alerta,
                EstoqueCritico = produto.QuantidadeDisponivel <= produto.QuantidadeMinima,
                EstoqueAcimaMaximo = estoqueAcimaMaximo,
                ProdutoVencido = ProdutoEstaVencido(produto),
                VencimentoProximo = ProdutoVenceEmBreve(produto),
                DiasSemSaida = diasSemSaida,
                ProdutoParado = produtoParado,
                SemCodigoOperacional = semCodigoOperacional,
                MargemBaixa = margemBaixa,
                StatusTexto = produto.QuantidadeDisponivel <= 0
                    ? "Zerado"
                    : produto.QuantidadeDisponivel <= produto.QuantidadeMinima
                        ? "Baixo"
                        : "OK"
            };
        }

        private static void AplicarCurvaAbc(IList<ProdutoGridItem> itens)
        {
            var valorTotalEstoque = itens.Sum(item => Math.Max(0m, item.ValorTotalEstoque));
            if (valorTotalEstoque <= 0)
            {
                foreach (var item in itens)
                {
                    item.CurvaAbc = "Sem valor";
                    item.ParticipacaoEstoquePercentual = 0m;
                    item.ParticipacaoAcumuladaPercentual = 0m;
                }

                return;
            }

            decimal acumulado = 0m;
            foreach (var item in itens
                         .OrderByDescending(item => item.ValorTotalEstoque)
                         .ThenBy(item => item.Nome, StringComparer.OrdinalIgnoreCase))
            {
                var valorItem = Math.Max(0m, item.ValorTotalEstoque);
                var acumuladoAntes = acumulado / valorTotalEstoque * 100m;
                acumulado += valorItem;

                var participacao = valorItem / valorTotalEstoque * 100m;
                var acumuladoPercentual = acumulado / valorTotalEstoque * 100m;

                item.ParticipacaoEstoquePercentual = participacao;
                item.ParticipacaoAcumuladaPercentual = acumuladoPercentual;
                item.CurvaAbc = acumuladoAntes < 80m
                    ? "A"
                    : acumuladoAntes < 95m
                        ? "B"
                        : "C";
            }
        }

        private static bool Contem(string? origem, string termo)
        {
            return !string.IsNullOrWhiteSpace(origem)
                && origem.Contains(termo, StringComparison.OrdinalIgnoreCase);
        }

        private static void ExibirMensagem(string mensagem, string titulo, MessageBoxImage imagem, Exception? ex = null)
        {
            if (App.IsAutomatedTestMode)
            {
                var texto = $"{titulo}: {mensagem}";

                if (imagem == MessageBoxImage.Error)
                {
                    App.Logger.LogError(texto, ex, "Estoque");
                }
                else if (imagem == MessageBoxImage.Warning)
                {
                    App.Logger.LogWarning(texto, "Estoque");
                }
                else
                {
                    App.Logger.LogInfo(texto, "Estoque");
                }

                return;
            }

            MessageBox.Show(mensagem, titulo, MessageBoxButton.OK, imagem);
        }

        private static void ValidarJanelaEmAutomacao(Window window, string contexto)
        {
            try
            {
                window.ApplyTemplate();
                window.Measure(new Size(1440, 900));
                window.Arrange(new Rect(0, 0, 1440, 900));
                window.UpdateLayout();

                if (window.Content is FrameworkElement content)
                {
                    content.ApplyTemplate();
                    content.Measure(new Size(1440, 900));
                    content.Arrange(new Rect(0, 0, 1440, 900));
                    content.UpdateLayout();
                }

                App.Logger.LogInfo($"Janela {contexto} validada em automacao sem abrir modal bloqueante.", "Estoque");
            }
            finally
            {
                if (window.IsVisible)
                {
                    window.Close();
                }
            }
        }

        private void ConfigurarOwner(Window dialog)
        {
            WindowOwnerHelper.ConfigureOwner(dialog, this);
        }

        private sealed class ProdutoGridItem
        {
            public Produto Produto { get; init; } = null!;
            public string ImagemUrl { get; init; } = string.Empty;
            public bool TemImagem { get; init; }
            public bool Incompleto { get; init; }
            public string Codigo { get; init; } = string.Empty;
            public string Nome { get; init; } = string.Empty;
            public string Categoria { get; init; } = string.Empty;
            public string Modelo { get; init; } = string.Empty;
            public string Fornecedor { get; init; } = string.Empty;
            public string CodigoBarras { get; init; } = string.Empty;
            public string SKU { get; init; } = string.Empty;
            public int QuantidadeAnexos { get; init; }
            public string UnidadeMedida { get; init; } = string.Empty;
            public string NCM { get; init; } = string.Empty;
            public string Localizacao { get; init; } = string.Empty;
            public int QuantidadeEstoque { get; init; }
            public int QuantidadeMinima { get; init; }
            public int QuantidadeMaxima { get; init; }
            public int QuantidadeReservada { get; init; }
            public int QuantidadeDisponivel { get; init; }
            public decimal PrecoCompra { get; init; }
            public decimal PrecoVenda { get; init; }
            public decimal MargemLucro { get; init; }
            public decimal ValorTotalEstoque { get; init; }
            public string CurvaAbc { get; set; } = "Sem valor";
            public decimal ParticipacaoEstoquePercentual { get; set; }
            public decimal ParticipacaoAcumuladaPercentual { get; set; }
            public DateTime? DataUltimaCompra { get; init; }
            public DateTime? DataUltimaVenda { get; init; }
            public int TotalVendas { get; init; }
            public int VendasUltimoMes { get; init; }
            public int VendasUltimoTrimestre { get; init; }
            public decimal ReceitaEstimadaTotal { get; init; }
            public decimal ReceitaEstimadaMes { get; init; }
            public string AlertaPrincipal { get; init; } = string.Empty;
            public bool EstoqueCritico { get; init; }
            public bool EstoqueAcimaMaximo { get; init; }
            public bool ProdutoVencido { get; init; }
            public bool VencimentoProximo { get; init; }
            public int DiasSemSaida { get; init; }
            public bool ProdutoParado { get; init; }
            public bool SemCodigoOperacional { get; init; }
            public bool MargemBaixa { get; init; }
            public string StatusTexto { get; init; } = string.Empty;
        }
    }
}
