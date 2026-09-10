using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PrimoAutoEletrica.ViewModels
{
    public class PDVViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Produto> Produtos { get; } = new();

        public ObservableCollection<Cliente> Clientes { get; } = new();

        public ObservableCollection<ItemVenda> Carrinho { get; } = new();

        public ObservableCollection<string> FormasPagamento { get; } = new()
        {
            "Dinheiro",
            "PIX",
            "Debito",
            "Credito",
            "Boleto"
        };

        private Produto? _selectedProduto;
        public Produto? SelectedProduto
        {
            get => _selectedProduto;
            set => SetProperty(ref _selectedProduto, value);
        }

        private Cliente? _clienteSelecionado;
        public Cliente? ClienteSelecionado
        {
            get => _clienteSelecionado;
            set => SetProperty(ref _clienteSelecionado, value);
        }

        private decimal _total;
        public decimal Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }

        private decimal _descontoGeral;
        public decimal DescontoGeral
        {
            get => _descontoGeral;
            set
            {
                var valorSeguro = Math.Max(0m, value);

                if (SetProperty(ref _descontoGeral, valorSeguro))
                {
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        private int _quantidadeItens;
        public int QuantidadeItens
        {
            get => _quantidadeItens;
            set => SetProperty(ref _quantidadeItens, value);
        }

        private decimal _subtotal;
        public decimal Subtotal
        {
            get => _subtotal;
            set => SetProperty(ref _subtotal, value);
        }

        private decimal _lucroEstimado;
        public decimal LucroEstimado
        {
            get => _lucroEstimado;
            set => SetProperty(ref _lucroEstimado, value);
        }

        private string _formaPagamentoSelecionada = "Dinheiro";
        public string FormaPagamentoSelecionada
        {
            get => _formaPagamentoSelecionada;
            set
            {
                var forma = string.IsNullOrWhiteSpace(value) ? "Dinheiro" : value.Trim();
                SetProperty(ref _formaPagamentoSelecionada, forma);
            }
        }

        private string _pagamentoMistoResumo = string.Empty;
        public string PagamentoMistoResumo
        {
            get => _pagamentoMistoResumo;
            set => SetProperty(ref _pagamentoMistoResumo, value?.Trim() ?? string.Empty);
        }

        private string _vendasSuspensasResumo = UiText.T("NoSuspendedSales");
        public string VendasSuspensasResumo
        {
            get => _vendasSuspensasResumo;
            set => SetProperty(ref _vendasSuspensasResumo, string.IsNullOrWhiteSpace(value) ? UiText.T("NoSuspendedSales") : value);
        }

        private bool _caixaAberto;
        public bool CaixaAberto
        {
            get => _caixaAberto;
            set => SetProperty(ref _caixaAberto, value);
        }

        private string _statusCaixa = "Fechado";
        public string StatusCaixa
        {
            get => _statusCaixa;
            set => SetProperty(ref _statusCaixa, string.IsNullOrWhiteSpace(value) ? "Fechado" : value);
        }

        private string _numeroCaixa = "01";
        public string NumeroCaixa
        {
            get => _numeroCaixa;
            set => SetProperty(ref _numeroCaixa, string.IsNullOrWhiteSpace(value) ? "01" : value);
        }

        private string _caixaResumo = "Abra o caixa para iniciar as vendas.";
        public string CaixaResumo
        {
            get => _caixaResumo;
            set => SetProperty(ref _caixaResumo, string.IsNullOrWhiteSpace(value) ? "Abra o caixa para iniciar as vendas." : value);
        }

        private decimal _saldoCaixaAtual;
        public decimal SaldoCaixaAtual
        {
            get => _saldoCaixaAtual;
            set => SetProperty(ref _saldoCaixaAtual, value);
        }

        private int _vendasSessao;
        public int VendasSessao
        {
            get => _vendasSessao;
            set => SetProperty(ref _vendasSessao, value);
        }

        private Guid? _ultimaVendaFinalizadaId;
        public Guid? UltimaVendaFinalizadaId
        {
            get => _ultimaVendaFinalizadaId;
            set => SetProperty(ref _ultimaVendaFinalizadaId, value);
        }

        private DateTime _dataAtual = DateTime.Now;
        public DateTime DataAtual
        {
            get => _dataAtual;
            set => SetProperty(ref _dataAtual, value);
        }

        private DateTime _horaAtual = DateTime.Now;
        public DateTime HoraAtual
        {
            get => _horaAtual;
            set => SetProperty(ref _horaAtual, value);
        }

        public string UsuarioAtual
        {
            get
            {
                try
                {
                    var nome = global::PrimoAutoEletrica.App.Session?.UserName;

                    if (!string.IsNullOrWhiteSpace(nome))
                    {
                        return nome;
                    }
                }
                catch
                {
                    // Mantém fallback seguro para não quebrar binding do PDV.
                }

                return "ADMIN";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void AtualizarTotal()
        {
            var subtotalCalculado = Carrinho.Sum(i => i.Subtotal);
            var quantidadeCalculada = Carrinho.Sum(i => i.Quantidade);
            var lucroCalculado = Carrinho.Sum(i =>
            {
                var custoUnitario = i.Produto?.PrecoCompra ?? i.CustoUnitario;
                return (i.PrecoUnitario - custoUnitario) * i.Quantidade;
            });

            Subtotal = Math.Max(0m, subtotalCalculado);
            QuantidadeItens = Math.Max(0, quantidadeCalculada);
            LucroEstimado = lucroCalculado;

            if (DescontoGeral > Subtotal)
            {
                DescontoGeral = Subtotal;
            }

            if (DescontoGeral < 0)
            {
                DescontoGeral = 0;
            }

            Total = Math.Max(0m, Subtotal - DescontoGeral);
        }

        public void AtualizarHora()
        {
            DataAtual = DateTime.Now;
            HoraAtual = DateTime.Now;

            OnPropertyChanged(nameof(UsuarioAtual));
        }

        public void AtualizarCaixa(CaixaSessaoOperacional? sessao)
        {
            CaixaAberto = sessao?.Aberto == true;
            StatusCaixa = sessao?.Status ?? "Fechado";
            NumeroCaixa = sessao?.NumeroCaixa ?? "01";
            SaldoCaixaAtual = sessao?.ValorEsperado ?? 0m;
            VendasSessao = sessao?.QuantidadeVendas ?? 0;

            CaixaResumo = sessao == null
                ? "Abra o caixa para iniciar as vendas."
                : $"Aberto em {sessao.DataAbertura:dd/MM/yyyy HH:mm} por {sessao.OperadorNome}.";

            OnPropertyChanged(nameof(UsuarioAtual));
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
