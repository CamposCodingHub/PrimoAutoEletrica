using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.ViewModels
{
    public class EstoqueViewModel : INotifyPropertyChanged
    {
        private readonly EstoqueOperationalService _estoqueOperationalService;
        private readonly ProdutoEtiquetaService _produtoEtiquetaService;

        private List<Produto> _todosProdutos = new();
        private List<Produto> _filtrados = new();
        private string _textoBusca = string.Empty;
        private string _totalProdutosText = "0";
        private string _produtosBaixoEstoqueText = "0";
        private string _valorTotalEstoqueText = "R$ 0,00";
        private string _pageInfoText = "Pagina 1/1";
        private int _page = 1;
        private int _pageSize = 50;
        private int _totalPages = 1;

        public ObservableCollection<Produto> ProdutosVisiveis { get; } = new();

        public string TextoBusca
        {
            get => _textoBusca;
            set => SetField(ref _textoBusca, value);
        }

        public string TotalProdutosText { get => _totalProdutosText; set => SetField(ref _totalProdutosText, value); }
        public string ProdutosBaixoEstoqueText { get => _produtosBaixoEstoqueText; set => SetField(ref _produtosBaixoEstoqueText, value); }
        public string ValorTotalEstoqueText { get => _valorTotalEstoqueText; set => SetField(ref _valorTotalEstoqueText, value); }
        public string PageInfoText { get => _pageInfoText; set => SetField(ref _pageInfoText, value); }
        public int Page
        {
            get => _page;
            set { if (SetField(ref _page, Math.Max(1, value))) AplicarPagina(); }
        }
        public bool CanGoPrevious => Page > 1;
        public bool CanGoNext => Page < _totalPages;

        public EstoqueViewModel()
        {
            _estoqueOperationalService = new EstoqueOperationalService(App.Database, App.Logger);
            _produtoEtiquetaService = new ProdutoEtiquetaService();
        }

        public void CarregarProdutos()
        {
            try
            {
                _todosProdutos = App.Repositories.Produtos.ObterTodos()
                    .Where(p => p.Ativo)
                    .OrderBy(p => p.Nome)
                    .ToList();
                _estoqueOperationalService.EnriquecerProdutosComReservas(_todosProdutos);
                EnriquecerExibicao(_todosProdutos);
                FiltrarProdutos();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar produtos no estoque", ex);
            }
        }

        public void FiltrarProdutos()
        {
            if (string.IsNullOrWhiteSpace(TextoBusca))
            {
                _filtrados = _todosProdutos.ToList();
            }
            else
            {
                var termos = TextoBusca.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                _filtrados = _todosProdutos
                    .Where(p => termos.All(termo =>
                        p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                        p.Codigo.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                        (p.Marca != null && p.Marca.Contains(termo, StringComparison.OrdinalIgnoreCase))))
                    .ToList();
            }

            _page = 1;
            OnPropertyChanged(nameof(Page));
            AplicarPagina();
            AtualizarEstatisticas();
        }

        public void NextPage() { if (CanGoNext) Page++; }
        public void PreviousPage() { if (CanGoPrevious) Page--; }

        public Produto? ObterProdutoPorId(Guid id) =>
            _todosProdutos.FirstOrDefault(p => p.Id == id) ?? App.Repositories.Produtos.ObterPorId(id);

        public string GerarEtiquetaPdf(Produto produto)
        {
            var pasta = Path.Combine(App.RuntimeLogDirectory, "etiquetas");
            Directory.CreateDirectory(pasta);
            var resultado = _produtoEtiquetaService.GerarEtiquetas(produto, pasta);
            return resultado.CaminhoArquivo;
        }

        public void AbrirEtiqueta(Produto produto)
        {
            var caminho = GerarEtiquetaPdf(produto);
            Process.Start(new ProcessStartInfo { FileName = caminho, UseShellExecute = true });
        }

        private void AplicarPagina()
        {
            var paged = PagingHelper.Page(_filtrados, Page, _pageSize);
            _totalPages = Math.Max(1, paged.TotalPages);
            if (_page > _totalPages) { _page = _totalPages; OnPropertyChanged(nameof(Page)); }

            ProdutosVisiveis.Clear();
            foreach (var p in paged.Items)
                ProdutosVisiveis.Add(p);

            PageInfoText = $"Pagina {Page}/{_totalPages} ({paged.TotalItems} produtos)";
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }

        private static void EnriquecerExibicao(List<Produto> produtos)
        {
            var valorTotal = produtos.Sum(p => p.PrecoVenda * Math.Max(p.QuantidadeEstoque, 0));
            var acumulado = 0m;

            foreach (var p in produtos)
            {
                if (p.PrecoVenda > 0)
                    p.MargemLucro = ((p.PrecoVenda - p.PrecoCompra) / p.PrecoVenda) * 100m;
                p.ValorTotalEstoque = p.PrecoVenda * p.QuantidadeEstoque;
                p.StatusTexto = p.QuantidadeDisponivel <= 0 ? "Zerado"
                    : p.QuantidadeDisponivel <= p.QuantidadeMinima ? "Baixo"
                    : "OK";
                p.AlertaPrincipal = p.QuantidadeDisponivel <= p.QuantidadeMinima ? "Reposque critico" : string.Empty;
                p.ReceitaEstimadaTotal = p.PrecoVenda * p.TotalVendas;
                p.TemImagem = !string.IsNullOrWhiteSpace(p.ImagemUrl) && File.Exists(p.ImagemUrl);
                p.QuantidadeAnexos = string.IsNullOrWhiteSpace(p.Anexos)
                    ? 0
                    : p.Anexos.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries).Length;
            }

            foreach (var p in produtos.OrderByDescending(x => x.ValorTotalEstoque))
            {
                acumulado += Math.Max(p.ValorTotalEstoque, 0);
                var pct = valorTotal <= 0 ? 100m : acumulado / valorTotal * 100m;
                p.ParticipacaoEstoquePercentual = valorTotal <= 0 ? 0 : (p.ValorTotalEstoque / valorTotal) * 100m;
                p.CurvaAbc = pct <= 20 ? "A" : pct <= 50 ? "B" : "C";
            }
        }

        private void AtualizarEstatisticas()
        {
            TotalProdutosText = _filtrados.Count.ToString("N0");
            ProdutosBaixoEstoqueText = _filtrados.Count(p => p.QuantidadeDisponivel <= p.QuantidadeMinima).ToString("N0");
            ValorTotalEstoqueText = _filtrados.Sum(p => p.PrecoVenda * p.QuantidadeEstoque).ToString("C2");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
