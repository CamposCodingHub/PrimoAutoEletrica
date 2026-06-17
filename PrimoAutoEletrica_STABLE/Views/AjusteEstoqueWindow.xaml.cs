using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public abstract class ProdutoAjusteBase : INotifyPropertyChanged
    {
        private bool _selecionado;

        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;

        public bool Selecionado
        {
            get => _selecionado;
            set
            {
                if (_selecionado == value)
                    return;

                _selecionado = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProdutoAjustePreco : ProdutoAjusteBase
    {
        public decimal PrecoVenda { get; set; }
        public decimal NovoPreco { get; set; }
        public decimal Diferenca { get; set; }
    }

    public class ProdutoAjusteLote : ProdutoAjusteBase
    {
        public int QuantidadeEstoque { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }

    public partial class AjusteEstoqueWindow : Window
    {
        private readonly PermissionService _permissionService;
        private List<Produto> _todosProdutos;
        private List<Produto> _produtosFiltrados;
        private List<ProdutoAjusteLote> _produtosAjusteLote = new();
        private List<ProdutoAjustePreco> _produtosAjustePreco = new();

        public AjusteEstoqueWindow()
        {
            InitializeComponent();
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger);

            _todosProdutos = App.Repositories.Produtos.ObterTodos();
            _produtosFiltrados = new List<Produto>(_todosProdutos);

            CarregarProdutos();
            AtualizarContadoresSelecao();
            AtualizarResumo();
        }

        private void CarregarProdutos()
        {
            ProdutoComboBox.ItemsSource = _produtosFiltrados;

            var idsSelecionados = _produtosAjusteLote
                .Where(p => p.Selecionado)
                .Select(p => p.Id)
                .ToHashSet();

            _produtosAjusteLote = _produtosFiltrados.Select(p => new ProdutoAjusteLote
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nome = p.Nome,
                QuantidadeEstoque = p.QuantidadeEstoque,
                Categoria = p.Categoria,
                Marca = p.Marca,
                StatusText = GetStatusText(p.QuantidadeEstoque, p.QuantidadeMinima),
                StatusColor = GetStatusColor(p.QuantidadeEstoque, p.QuantidadeMinima),
                Selecionado = idsSelecionados.Contains(p.Id)
            }).ToList();

            foreach (var produto in _produtosAjusteLote)
                produto.PropertyChanged += ProdutoAjusteLote_PropertyChanged;

            ProdutosDataGrid.ItemsSource = _produtosAjusteLote;
            AtualizarContadoresSelecao();
        }

        private void CarregarProdutosPreco()
        {
            var idsSelecionados = _produtosAjustePreco
                .Where(p => p.Selecionado)
                .Select(p => p.Id)
                .ToHashSet();

            _produtosAjustePreco = _produtosFiltrados.Select(p => new ProdutoAjustePreco
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nome = p.Nome,
                PrecoVenda = p.PrecoVenda,
                NovoPreco = CalcularNovoPreco(p.PrecoVenda),
                Diferenca = CalcularDiferenca(p.PrecoVenda),
                Categoria = p.Categoria,
                Marca = p.Marca,
                Selecionado = idsSelecionados.Contains(p.Id)
            }).ToList();

            foreach (var produto in _produtosAjustePreco)
                produto.PropertyChanged += ProdutoAjustePreco_PropertyChanged;

            PrecoDataGrid.ItemsSource = _produtosAjustePreco;
            AtualizarContadoresSelecao();
            AtualizarResumoPreco();
        }

        private decimal CalcularNovoPreco(decimal precoAtual)
        {
            if (!decimal.TryParse(PrecoValorTextBox.Text, out var valorAjuste) || valorAjuste == 0)
                return precoAtual;

            var isPorcentagem = PrecoPorcentagemRadio.IsChecked == true;
            var isAumentar = PrecoAumentarRadio.IsChecked == true;

            if (isPorcentagem)
            {
                var fator = valorAjuste / 100m;
                var novoPrecoPercentual = isAumentar
                    ? precoAtual * (1 + fator)
                    : precoAtual * (1 - fator);

                return decimal.Round(Math.Max(0, novoPrecoPercentual), 2);
            }

            var novoPrecoValor = isAumentar
                ? precoAtual + valorAjuste
                : precoAtual - valorAjuste;

            return decimal.Round(Math.Max(0, novoPrecoValor), 2);
        }

        private decimal CalcularDiferenca(decimal precoAtual)
        {
            var novoPreco = CalcularNovoPreco(precoAtual);
            return novoPreco - precoAtual;
        }

        private static string GetStatusText(int quantidade, int minima)
        {
            if (quantidade == 0)
                return "Sem Estoque";

            if (quantidade <= minima)
                return "Critico";

            return "Normal";
        }

        private static string GetStatusColor(int quantidade, int minima)
        {
            if (quantidade == 0)
                return "#EF4444";

            if (quantidade <= minima)
                return "#F59E0B";

            return "#10B981";
        }

        private void ProdutoAjusteLote_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProdutoAjusteBase.Selecionado))
                return;

            AtualizarContadoresSelecao();
            AtualizarResumo();
        }

        private void ProdutoAjustePreco_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProdutoAjusteBase.Selecionado))
                return;

            AtualizarContadoresSelecao();
            AtualizarResumoPreco();
        }

        private void RecarregarProdutosFiltradosNoModoAtual()
        {
            CarregarProdutos();

            if (AjustePrecoRadio.IsChecked == true)
            {
                CarregarProdutosPreco();
                return;
            }

            AtualizarResumo();
        }

        private void AjusteUnitarioRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (AjusteUnitarioPanel != null && AjusteLotePanel != null && AjustePrecoPanel != null)
            {
                AjusteUnitarioPanel.Visibility = Visibility.Visible;
                AjusteLotePanel.Visibility = Visibility.Collapsed;
                AjustePrecoPanel.Visibility = Visibility.Collapsed;
            }

            if (_produtosFiltrados != null)
                AtualizarResumo();
        }

        private void AjusteLoteRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (AjusteUnitarioPanel != null && AjusteLotePanel != null && AjustePrecoPanel != null)
            {
                AjusteUnitarioPanel.Visibility = Visibility.Collapsed;
                AjusteLotePanel.Visibility = Visibility.Visible;
                AjustePrecoPanel.Visibility = Visibility.Collapsed;
            }

            if (_produtosFiltrados != null)
                AtualizarResumo();
        }

        private void AjustePrecoRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (AjusteUnitarioPanel != null && AjusteLotePanel != null && AjustePrecoPanel != null)
            {
                AjusteUnitarioPanel.Visibility = Visibility.Collapsed;
                AjusteLotePanel.Visibility = Visibility.Collapsed;
                AjustePrecoPanel.Visibility = Visibility.Visible;
            }

            if (_produtosFiltrados != null)
                CarregarProdutosPreco();
        }

        private void ProdutoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProdutoComboBox.SelectedItem is Produto produto)
            {
                EstoqueAtualText.Text = $"Estoque Atual: {produto.QuantidadeEstoque}";
                AtualizarResumo();
            }
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_todosProdutos == null)
                return;

            var textoBusca = BuscaTextBox.Text.ToLower();

            _produtosFiltrados = _todosProdutos
                .Where(p => p.Nome.ToLower().Contains(textoBusca)
                    || p.Codigo.ToLower().Contains(textoBusca)
                    || p.Categoria.ToLower().Contains(textoBusca))
                .ToList();

            RecarregarProdutosFiltradosNoModoAtual();
        }

        private void FiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_todosProdutos == null)
                return;

            if (FiltroComboBox.SelectedItem is ComboBoxItem item)
            {
                var filtro = item.Content.ToString();

                switch (filtro)
                {
                    case "Todos":
                        _produtosFiltrados = new List<Produto>(_todosProdutos);
                        break;
                    case "Estoque Critico":
                        _produtosFiltrados = _todosProdutos
                            .Where(p => p.QuantidadeEstoque > 0 && p.QuantidadeEstoque <= p.QuantidadeMinima)
                            .ToList();
                        break;
                    case "Sem Estoque":
                        _produtosFiltrados = _todosProdutos
                            .Where(p => p.QuantidadeEstoque == 0)
                            .ToList();
                        break;
                    case "Com Estoque":
                        _produtosFiltrados = _todosProdutos
                            .Where(p => p.QuantidadeEstoque > 0)
                            .ToList();
                        break;
                }

                RecarregarProdutosFiltradosNoModoAtual();
            }
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AjustePrecoRadio.IsChecked == true)
                {
                    if (!ValidarPermissao("ESTOQUE_AJUSTAR_PRECO", "Voce nao possui permissao para reajustar precos no estoque."))
                    {
                        return;
                    }
                }
                else if (!ValidarPermissao("ESTOQUE_AJUSTAR", "Voce nao possui permissao para ajustar entradas e saidas de estoque."))
                {
                    return;
                }

                var ajusteSalvo = false;

                if (AjusteUnitarioRadio.IsChecked == true)
                    ajusteSalvo = SalvarAjusteUnitario();
                else if (AjusteLoteRadio.IsChecked == true)
                    ajusteSalvo = SalvarAjusteLote();
                else if (AjustePrecoRadio.IsChecked == true)
                    ajusteSalvo = SalvarAjustePreco();

                if (!ajusteSalvo)
                    return;

                MessageBox.Show(
                    "Ajuste realizado com sucesso!",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao salvar ajuste: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool SalvarAjusteUnitario()
        {
            if (ProdutoComboBox.SelectedItem is not Produto produto)
            {
                MessageBox.Show(
                    "Por favor, selecione um produto.",
                    "Produto Nao Selecionado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(QuantidadeTextBox.Text, out var quantidade) || quantidade <= 0)
            {
                MessageBox.Show(
                    "Por favor, informe uma quantidade valida.",
                    "Quantidade Invalida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            var produtoCompleto = App.Repositories.Produtos.ObterPorId(produto.Id);
            if (produtoCompleto == null)
            {
                MessageBox.Show(
                    "Produto nao encontrado no banco de dados.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            var isEntrada = EntradaRadio.IsChecked == true;
            var ajuste = isEntrada ? quantidade : -quantidade;
            var estoqueAnterior = produtoCompleto.QuantidadeEstoque;
            var estoqueFinal = produtoCompleto.QuantidadeEstoque + ajuste;
            var motivo = ObterTextoComboBox(MotivoComboBox);

            if (estoqueFinal < 0 && !ValidarEstoqueNegativo(produtoCompleto, estoqueAnterior, estoqueFinal, isEntrada, quantidade, motivo))
            {
                return false;
            }

            produtoCompleto.QuantidadeEstoque = estoqueFinal;
            produtoCompleto.ValorTotalEstoque = produtoCompleto.QuantidadeEstoque * produtoCompleto.PrecoCompra;
            produtoCompleto.DataUltimaAtualizacao = DateTime.Now;
            produtoCompleto.Observacoes = AdicionarObservacao(
                produtoCompleto.Observacoes,
                $"Ajuste de estoque em {DateTime.Now:dd/MM/yyyy HH:mm}: {(isEntrada ? "Entrada" : "Saida")} de {quantidade} - Motivo: {motivo}"
            );

            App.Repositories.Produtos.Atualizar(produtoCompleto);
            RegistrarAjusteEstoque(
                "AjusteEstoqueUnitario",
                produtoCompleto,
                estoqueAnterior,
                estoqueFinal,
                quantidade,
                isEntrada ? "Entrada" : "Saida",
                motivo);
            return true;
        }

        private bool SalvarAjusteLote()
        {
            if (!int.TryParse(LoteQuantidadeTextBox.Text, out var quantidade) || quantidade <= 0)
            {
                MessageBox.Show(
                    "Por favor, informe uma quantidade valida.",
                    "Quantidade Invalida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            var isEntrada = LoteEntradaRadio.IsChecked == true;
            var ajuste = isEntrada ? quantidade : -quantidade;
            var motivo = ObterTextoComboBox(LoteMotivoComboBox);
            var correlationId = Guid.NewGuid().ToString("N");

            ProdutosDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ProdutosDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var produtosSelecionados = ObterProdutosSelecionados(
                _produtosAjusteLote
                    .Where(p => p.Selecionado)
                    .Select(p => p.Id)
            );

            if (produtosSelecionados.Count == 0)
            {
                MessageBox.Show(
                    "Por favor, selecione pelo menos um produto marcando o checkbox.",
                    "Nenhum Produto Selecionado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            var produtosComEstoqueNegativo = produtosSelecionados
                .Where(produto => produto.QuantidadeEstoque + ajuste < 0)
                .ToList();

            if (produtosComEstoqueNegativo.Count > 0
                && !ValidarEstoqueNegativoLote(produtosComEstoqueNegativo, ajuste, quantidade, isEntrada, motivo))
            {
                return false;
            }

            foreach (var produto in produtosSelecionados)
            {
                var estoqueAnterior = produto.QuantidadeEstoque;
                produto.QuantidadeEstoque += ajuste;
                produto.ValorTotalEstoque = produto.QuantidadeEstoque * produto.PrecoCompra;
                produto.DataUltimaAtualizacao = DateTime.Now;
                produto.Observacoes = AdicionarObservacao(
                    produto.Observacoes,
                    $"Ajuste em lote em {DateTime.Now:dd/MM/yyyy HH:mm}: {(isEntrada ? "Entrada" : "Saida")} de {quantidade} - Motivo: {motivo}"
                );

                App.Repositories.Produtos.Atualizar(produto);
                RegistrarAjusteEstoque(
                    "AjusteEstoqueLoteItem",
                    produto,
                    estoqueAnterior,
                    produto.QuantidadeEstoque,
                    quantidade,
                    isEntrada ? "Entrada" : "Saida",
                    motivo,
                    correlationId);
            }

            App.Audit.RegistrarAcaoCritica(
                "Estoque",
                "AjusteEstoqueLoteResumo",
                "Produto",
                correlationId,
                $"Produtos={produtosSelecionados.Count}; Operacao={(isEntrada ? "Entrada" : "Saida")}; QuantidadePorProduto={quantidade}; AjusteTotal={ajuste * produtosSelecionados.Count}; Motivo={motivo}");
            return true;
        }

        private bool SalvarAjustePreco()
        {
            if (!decimal.TryParse(PrecoValorTextBox.Text, out var valorAjuste) || valorAjuste == 0)
            {
                MessageBox.Show(
                    "Por favor, informe um valor de ajuste valido.",
                    "Valor Invalido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            PrecoDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            PrecoDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var motivo = ObterTextoComboBox(PrecoMotivoComboBox);
            var isPorcentagem = PrecoPorcentagemRadio.IsChecked == true;
            var operacaoPreco = PrecoAumentarRadio.IsChecked == true ? "Aumentar" : "Diminuir";
            var correlationId = Guid.NewGuid().ToString("N");
            var produtosSelecionados = ObterProdutosSelecionados(
                _produtosAjustePreco
                    .Where(p => p.Selecionado)
                    .Select(p => p.Id)
            );

            if (produtosSelecionados.Count == 0)
            {
                MessageBox.Show(
                    "Por favor, selecione pelo menos um produto marcando o checkbox.",
                    "Nenhum Produto Selecionado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            foreach (var produto in produtosSelecionados)
            {
                var precoAtual = produto.PrecoVenda;
                var margemAnterior = produto.MargemLucro;
                var novoPreco = CalcularNovoPreco(precoAtual);
                var diferenca = novoPreco - precoAtual;

                produto.PrecoVenda = novoPreco;
                produto.MargemLucro = novoPreco > 0
                    ? ((novoPreco - produto.PrecoCompra) / novoPreco) * 100
                    : 0;
                produto.DataUltimaAtualizacao = DateTime.Now;
                produto.Observacoes = AdicionarObservacao(
                    produto.Observacoes,
                    $"Ajuste de preco em {DateTime.Now:dd/MM/yyyy HH:mm}: {precoAtual:C} -> {novoPreco:C} ({FormatarMoedaComSinal(diferenca)}) - Motivo: {motivo}"
                );

                try
                {
                    App.Repositories.Produtos.Atualizar(produto);
                    RegistrarAjustePreco(
                        produto,
                        precoAtual,
                        novoPreco,
                        margemAnterior,
                        produto.MargemLucro,
                        valorAjuste,
                        isPorcentagem ? "Percentual" : "Valor",
                        operacaoPreco,
                        motivo,
                        correlationId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erro ao atualizar produto {produto.Nome}: {ex.Message}",
                        "Erro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }
            }

            App.Audit.RegistrarAcaoCritica(
                "Estoque",
                "AjustePrecoLoteResumo",
                "Produto",
                correlationId,
                $"Produtos={produtosSelecionados.Count}; Tipo={(isPorcentagem ? "Percentual" : "Valor")}; Operacao={operacaoPreco}; ValorInformado={valorAjuste:F2}; Motivo={motivo}");
            return true;
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AtualizarResumo()
        {
            if (_produtosFiltrados == null)
                return;

            if (AjusteUnitarioRadio.IsChecked == true)
            {
                if (ProdutoComboBox.SelectedItem is Produto produto)
                {
                    if (int.TryParse(QuantidadeTextBox.Text, out var quantidade))
                    {
                        var isEntrada = EntradaRadio.IsChecked == true;
                        var ajuste = isEntrada ? quantidade : -quantidade;
                        var estoqueFinal = produto.QuantidadeEstoque + ajuste;

                        ProdutosAjustarText.Text = "1";
                        TotalAjusteText.Text = $"{ajuste:+0;-0}";
                        EstoqueFinalText.Text = estoqueFinal.ToString();
                    }
                    else
                    {
                        ProdutosAjustarText.Text = "1";
                        TotalAjusteText.Text = "0";
                        EstoqueFinalText.Text = produto.QuantidadeEstoque.ToString();
                    }
                }
                else
                {
                    ProdutosAjustarText.Text = "0";
                    TotalAjusteText.Text = "0";
                    EstoqueFinalText.Text = "0";
                }
            }
            else if (AjusteLoteRadio.IsChecked == true)
            {
                var produtosSelecionados = _produtosAjusteLote
                    .Where(p => p.Selecionado)
                    .ToList();

                if (int.TryParse(LoteQuantidadeTextBox.Text, out var quantidade))
                {
                    var isEntrada = LoteEntradaRadio.IsChecked == true;
                    var ajuste = isEntrada ? quantidade : -quantidade;
                    var totalProdutos = produtosSelecionados.Count;
                    var totalAjuste = ajuste * totalProdutos;
                    var estoqueAtualTotal = produtosSelecionados.Sum(p => p.QuantidadeEstoque);
                    var estoqueFinal = estoqueAtualTotal + totalAjuste;

                    ProdutosAjustarText.Text = totalProdutos.ToString();
                    TotalAjusteText.Text = $"{totalAjuste:+0;-0}";
                    EstoqueFinalText.Text = estoqueFinal.ToString();
                }
                else
                {
                    ProdutosAjustarText.Text = produtosSelecionados.Count.ToString();
                    TotalAjusteText.Text = "0";
                    EstoqueFinalText.Text = produtosSelecionados.Sum(p => p.QuantidadeEstoque).ToString();
                }
            }
            else if (AjustePrecoRadio.IsChecked == true)
            {
                AtualizarResumoPreco();
            }
        }

        private void QuantidadeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AtualizarResumo();
        }

        private void LoteQuantidadeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AtualizarResumo();
        }

        private void EntradaRadio_Checked(object sender, RoutedEventArgs e)
        {
            AtualizarResumo();
        }

        private void SaidaRadio_Checked(object sender, RoutedEventArgs e)
        {
            AtualizarResumo();
        }

        private void LoteEntradaRadio_Checked(object sender, RoutedEventArgs e)
        {
            AtualizarResumo();
        }

        private void LoteSaidaRadio_Checked(object sender, RoutedEventArgs e)
        {
            AtualizarResumo();
        }

        private void PrecoPorcentagemRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_produtosFiltrados != null)
                CarregarProdutosPreco();
        }

        private void PrecoValorRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_produtosFiltrados != null)
                CarregarProdutosPreco();
        }

        private void PrecoAumentarRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_produtosFiltrados != null)
                CarregarProdutosPreco();
        }

        private void PrecoDiminuirRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_produtosFiltrados != null)
                CarregarProdutosPreco();
        }

        private void PrecoValorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_produtosFiltrados != null)
                CarregarProdutosPreco();
        }

        private void AtualizarResumoPreco()
        {
            if (_produtosAjustePreco == null)
                return;

            var produtosSelecionados = _produtosAjustePreco
                .Where(p => p.Selecionado)
                .ToList();

            var totalProdutos = produtosSelecionados.Count;
            var somaPrecosAtuais = produtosSelecionados.Sum(p => p.PrecoVenda);
            var somaNovosPrecos = produtosSelecionados.Sum(p => p.NovoPreco);
            var diferencaTotal = somaNovosPrecos - somaPrecosAtuais;

            ProdutosAjustarText.Text = totalProdutos.ToString();
            TotalAjusteText.Text = FormatarMoedaComSinal(diferencaTotal);
            EstoqueFinalText.Text = somaNovosPrecos.ToString("C");
        }

        private void AtualizarContadoresSelecao()
        {
            SelecionadosCountText.Text = $"{_produtosAjusteLote.Count(p => p.Selecionado)} selecionados";
            PrecoSelecionadosCountText.Text = $"{_produtosAjustePreco.Count(p => p.Selecionado)} selecionados";
        }

        private List<Produto> ObterProdutosSelecionados(IEnumerable<Guid> ids)
        {
            return ids
                .Select(id => App.Repositories.Produtos.ObterPorId(id))
                .OfType<Produto>()
                .ToList();
        }

        private static string ObterTextoComboBox(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? string.Empty;

            return comboBox.SelectedItem?.ToString()
                ?? comboBox.Text
                ?? string.Empty;
        }

        private static string AdicionarObservacao(string observacoesAtuais, string novaObservacao)
        {
            if (string.IsNullOrWhiteSpace(observacoesAtuais))
                return novaObservacao;

            return $"{observacoesAtuais}{Environment.NewLine}{novaObservacao}";
        }

        private static string FormatarMoedaComSinal(decimal valor)
        {
            if (valor > 0)
                return $"+{valor:C}";

            if (valor < 0)
                return $"-{Math.Abs(valor):C}";

            return 0m.ToString("C");
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            MessageBox.Show(mensagem, "Acesso negado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private bool ValidarEstoqueNegativo(
            Produto produto,
            int estoqueAnterior,
            int estoqueFinal,
            bool isEntrada,
            int quantidade,
            string motivo)
        {
            if (!ValidarPermissao("ESTOQUE_PERMITIR_NEGATIVO", "Estoque negativo exige permissao de gerente ou administrador."))
            {
                return false;
            }

            return CriticalActionDialogService.ConfirmarAcao(
                this,
                new CriticalActionRequest
                {
                    WindowTitle = "Confirmar estoque negativo",
                    Header = "Ajuste unitario com saldo negativo",
                    Summary = $"Voce esta prestes a deixar o produto {produto.Nome} com estoque {estoqueFinal}.",
                    Details = $"Codigo: {produto.Codigo}\nEstoque anterior: {estoqueAnterior}\nOperacao: {(isEntrada ? "Entrada" : "Saida")} de {quantidade}\nMotivo: {motivo}",
                    Impact = "O saldo ficara abaixo de zero e exigira acompanhamento operacional imediato para evitar movimentacoes inconsistentes.",
                    Keyword = "NEGATIVO",
                    ConfirmButtonText = "Aplicar ajuste mesmo assim"
                });
        }

        private bool ValidarEstoqueNegativoLote(
            IReadOnlyCollection<Produto> produtos,
            int ajuste,
            int quantidade,
            bool isEntrada,
            string motivo)
        {
            if (!ValidarPermissao("ESTOQUE_PERMITIR_NEGATIVO", "Estoque negativo exige permissao de gerente ou administrador."))
            {
                return false;
            }

            var amostra = string.Join(", ", produtos.Take(3).Select(p => p.Nome));
            var sufixo = produtos.Count > 3 ? $" e mais {produtos.Count - 3}" : string.Empty;

            return CriticalActionDialogService.ConfirmarAcao(
                this,
                new CriticalActionRequest
                {
                    WindowTitle = "Confirmar estoque negativo",
                    Header = "Ajuste em lote com saldo negativo",
                    Summary = $"Voce esta prestes a deixar {produtos.Count} produto(s) com estoque negativo.",
                    Details = $"Operacao: {(isEntrada ? "Entrada" : "Saida")} de {quantidade} por produto\nAjuste por item: {ajuste:+0;-0}\nProdutos afetados: {amostra}{sufixo}\nMotivo: {motivo}",
                    Impact = "Todos os produtos listados ficarao abaixo de zero ate regularizacao posterior. Use apenas sob responsabilidade gerencial.",
                    Keyword = "NEGATIVO",
                    ConfirmButtonText = "Aplicar ajuste em lote"
                });
        }

        private static string CriarSnapshotEstoque(Produto produto, int quantidadeEstoque)
        {
            return $"Codigo={produto.Codigo}; Nome={produto.Nome}; Estoque={quantidadeEstoque}; ValorTotal={(quantidadeEstoque * produto.PrecoCompra):C}";
        }

        private void RegistrarAjusteEstoque(
            string acao,
            Produto produto,
            int estoqueAnterior,
            int estoqueNovo,
            int quantidade,
            string operacao,
            string motivo,
            string? correlationId = null)
        {
            App.Audit.Registrar(
                categoria: "Estoque",
                acao: acao,
                entidade: "Produto",
                entidadeId: produto.Id.ToString(),
                detalhes: $"Produto={produto.Nome}; Operacao={operacao}; Quantidade={quantidade}; Motivo={motivo}",
                valorAnterior: CriarSnapshotEstoque(produto, estoqueAnterior),
                valorNovo: CriarSnapshotEstoque(produto, estoqueNovo),
                correlationId: correlationId);
        }

        private void RegistrarAjustePreco(
            Produto produto,
            decimal precoAnterior,
            decimal precoNovo,
            decimal margemAnterior,
            decimal margemNova,
            decimal valorInformado,
            string tipoRegra,
            string operacao,
            string motivo,
            string correlationId)
        {
            App.Audit.Registrar(
                categoria: "Estoque",
                acao: "AjustePrecoLoteItem",
                entidade: "Produto",
                entidadeId: produto.Id.ToString(),
                detalhes: $"Produto={produto.Nome}; TipoRegra={tipoRegra}; Operacao={operacao}; ValorInformado={valorInformado:F2}; Motivo={motivo}",
                valorAnterior: $"Codigo={produto.Codigo}; PrecoVenda={precoAnterior:C}; Margem={margemAnterior:F2}%",
                valorNovo: $"Codigo={produto.Codigo}; PrecoVenda={precoNovo:C}; Margem={margemNova:F2}%",
                correlationId: correlationId);
        }
    }
}


