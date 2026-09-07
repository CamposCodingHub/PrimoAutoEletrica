using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.ViewModels
{
    public class EstoqueViewModel : INotifyPropertyChanged
    {
        private const decimal MargemBaixaPercentual = 20m;

        private readonly EstoqueOperationalService _estoqueOperationalService;
        private readonly ProdutoEtiquetaService _produtoEtiquetaService;

        private List<Produto> _todosProdutos = new();
        private string _textoBusca = string.Empty;
        private string _totalProdutosText = "0";
        private string _produtosBaixoEstoqueText = "0";
        private string _valorTotalEstoqueText = "R$ 0,00";

        public ObservableCollection<ProdutoGridItem> GridItems { get; } = new();

        public string TextoBusca
        {
            get => _textoBusca;
            set => SetField(ref _textoBusca, value);
        }

        public string TotalProdutosText
        {
            get => _totalProdutosText;
            set => SetField(ref _totalProdutosText, value);
        }

        public string ProdutosBaixoEstoqueText
        {
            get => _produtosBaixoEstoqueText;
            set => SetField(ref _produtosBaixoEstoqueText, value);
        }

        public string ValorTotalEstoqueText
        {
            get => _valorTotalEstoqueText;
            set => SetField(ref _valorTotalEstoqueText, value);
        }

        public EstoqueViewModel()
        {
            _estoqueOperationalService = new EstoqueOperationalService(App.Database, App.Logger);
            _produtoEtiquetaService = new ProdutoEtiquetaService();
        }

        public void CarregarProdutos()
        {
            try
            {
                _todosProdutos = App.Repositories.Produtos.ObterTodos();
                _estoqueOperationalService.EnriquecerProdutosComReservas(_todosProdutos);

                var gridItems = _todosProdutos
                    .Select(CriarGridItem)
                    .ToList();
                AplicarCurvaAbc(gridItems);

                GridItems.Clear();
                foreach (var item in gridItems.OrderBy(i => i.Nome))
                {
                    GridItems.Add(item);
                }

                AtualizarEstatisticas();
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
                CarregarProdutos();
                return;
            }

            var termos = TextoBusca.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var filtrados = _todosProdutos
                .Where(p => termos.All(termo =>
                    p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    p.Codigo.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    (p.Marca != null && p.Marca.Contains(termo, StringComparison.OrdinalIgnoreCase))))
                .Select(CriarGridItem)
                .ToList();

            GridItems.Clear();
            foreach (var item in filtrados.OrderBy(i => i.Nome))
            {
                GridItems.Add(item);
            }

            AtualizarEstatisticas();
        }

        private ProdutoGridItem CriarGridItem(Produto produto)
        {
            return new ProdutoGridItem
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Codigo = produto.Codigo,
                Marca = produto.Marca,
                Categoria = produto.Categoria,
                PrecoVenda = produto.PrecoVenda,
                PrecoCusto = produto.PrecoCompra,
                EstoqueAtual = produto.QuantidadeEstoque,
                EstoqueMinimo = produto.QuantidadeMinima,
                EstoqueReservado = produto.QuantidadeReservada,
                Disponivel = produto.QuantidadeDisponivel,
                MargemLucro = CalcularMargemLucro(produto.PrecoCompra, produto.PrecoVenda),
                ClassificacaoAbc = "C"
            };
        }

        private void AplicarCurvaAbc(List<ProdutoGridItem> items)
        {
            if (items.Count == 0) return;

            var valorTotal = items.Sum(i => i.PrecoVenda * i.EstoqueAtual);
            var valorAcumulado = 0m;

            foreach (var item in items.OrderByDescending(i => i.PrecoVenda * i.EstoqueAtual))
            {
                valorAcumulado += item.PrecoVenda * item.EstoqueAtual;
                var percentual = valorAcumulado / valorTotal * 100;

                if (percentual <= 20)
                    item.ClassificacaoAbc = "A";
                else if (percentual <= 50)
                    item.ClassificacaoAbc = "B";
                else
                    item.ClassificacaoAbc = "C";
            }
        }

        private void AtualizarEstatisticas()
        {
            TotalProdutosText = GridItems.Count.ToString("N0");
            ProdutosBaixoEstoqueText = GridItems.Count(i => i.Disponivel <= i.EstoqueMinimo).ToString("N0");
            ValorTotalEstoqueText = GridItems.Sum(i => i.PrecoVenda * i.EstoqueAtual).ToString("C2");
        }

        private decimal CalcularMargemLucro(decimal custo, decimal venda)
        {
            if (venda <= 0) return 0;
            return ((venda - custo) / venda) * 100;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ProdutoGridItem
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Categoria { get; set; }
        public decimal PrecoVenda { get; set; }
        public decimal PrecoCusto { get; set; }
        public int EstoqueAtual { get; set; }
        public int EstoqueMinimo { get; set; }
        public int EstoqueReservado { get; set; }
        public int Disponivel { get; set; }
        public decimal MargemLucro { get; set; }
        public string ClassificacaoAbc { get; set; } = "C";
    }
}