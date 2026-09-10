using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;

using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.ViewModels
{
    public class OrcamentosViewModel : INotifyPropertyChanged
    {
        private static LoggerService Logger => global::PrimoAutoEletrica.App.Logger;
        private static readonly TimeSpan ProdutosCacheDuration = TimeSpan.FromMinutes(2);
        private static IReadOnlyList<Produto>? _produtosAtivosCache;
        private static DateTime _produtosAtivosCacheAt = DateTime.MinValue;
        private readonly OrcamentoDatabaseService _orcamentoDatabaseService;
        private readonly DatabaseService _databaseService;
        private readonly FinanceiroDatabaseService _financeiroDatabaseService;
        private readonly VendaService _vendaService;

        public string UsuarioLogado { get; set; } = "Sistema";
        public string DataAtual { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
        public string HoraAtual { get; set; } = DateTime.Now.ToString("HH:mm");
        public ObservableCollection<DashboardCard> DashboardCards { get; set; } = new();

        private ObservableCollection<Orcamento> _orcamentos = new();
        public ObservableCollection<Orcamento> Orcamentos
        {
            get => _orcamentos;
            set { _orcamentos = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Orcamento> _ultimosOrcamentos = new();
        public ObservableCollection<Orcamento> UltimosOrcamentos
        {
            get => _ultimosOrcamentos;
            set { _ultimosOrcamentos = value; OnPropertyChanged(); }
        }

        private Orcamento? _orcamentoAtual;
        public Orcamento? OrcamentoAtual
        {
            get => _orcamentoAtual;
            set { _orcamentoAtual = value; OnPropertyChanged(); }
        }

        private ObservableCollection<OrcamentoItem> _itensCarrinho = new();
        public ObservableCollection<OrcamentoItem> ItensCarrinho
        {
            get => _itensCarrinho;
            set { _itensCarrinho = value; OnPropertyChanged(); }
        }

        private decimal _subtotal;
        public decimal Subtotal
        {
            get => _subtotal;
            set { _subtotal = value; OnPropertyChanged(); }
        }

        private decimal _desconto;
        public decimal Desconto
        {
            get => _desconto;
            set { _desconto = value; OnPropertyChanged(); CalcularTotais(); }
        }

        private decimal _acrescimo;
        public decimal Acrescimo
        {
            get => _acrescimo;
            set { _acrescimo = value; OnPropertyChanged(); CalcularTotais(); }
        }

        private decimal _total;
        public decimal Total
        {
            get => _total;
            set { _total = value; OnPropertyChanged(); }
        }

        private decimal _margemLucro;
        public decimal MargemLucro
        {
            get => _margemLucro;
            set { _margemLucro = value; OnPropertyChanged(); }
        }

        private decimal _lucroEstimado;
        public decimal LucroEstimado
        {
            get => _lucroEstimado;
            set { _lucroEstimado = value; OnPropertyChanged(); }
        }

        private int _quantidadeItens;
        public int QuantidadeItens
        {
            get => _quantidadeItens;
            set { _quantidadeItens = value; OnPropertyChanged(); }
        }

        // Propriedades para os controles de Orçamento
        private string _nome = string.Empty;
        public string Nome
        {
            get => _nome;
            set { _nome = value; OnPropertyChanged(); }
        }

        private string _telefone = string.Empty;
        public string Telefone
        {
            get => _telefone;
            set { _telefone = value; OnPropertyChanged(); }
        }

        private string _documento = string.Empty;
        public string Documento
        {
            get => _documento;
            set { _documento = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Produto> _produtos = new();
        public ObservableCollection<Produto> Produtos
        {
            get => _produtos;
            set { _produtos = value; OnPropertyChanged(); }
        }

        private decimal _comissaoVendedor;
        public decimal ComissaoVendedor
        {
            get => _comissaoVendedor;
            set { _comissaoVendedor = value; OnPropertyChanged(); }
        }

        private decimal _impostosEstimados;
        public decimal ImpostosEstimados
        {
            get => _impostosEstimados;
            set { _impostosEstimados = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _historicoCompras = new();
        public ObservableCollection<string> HistoricoCompras
        {
            get => _historicoCompras;
            set { _historicoCompras = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _historicoNegociacao = new();
        public ObservableCollection<string> HistoricoNegociacao
        {
            get => _historicoNegociacao;
            set { _historicoNegociacao = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _timelineComercial = new();
        public ObservableCollection<string> TimelineComercial
        {
            get => _timelineComercial;
            set { _timelineComercial = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _alertas = new();
        public ObservableCollection<string> Alertas
        {
            get => _alertas;
            set { _alertas = value; OnPropertyChanged(); }
        }

        private Cliente? _clienteAtual;
        public Cliente? ClienteAtual
        {
            get => _clienteAtual;
            private set
            {
                _clienteAtual = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusVip));
                OnPropertyChanged(nameof(HistoricoComprasResumo));
                OnPropertyChanged(nameof(UltimaCompraResumo));
                OnPropertyChanged(nameof(ValorTotalGastoResumo));
                OnPropertyChanged(nameof(InadimplenciaResumo));
            }
        }

        public string StatusVip => ClienteAtual?.ClienteVip == true ? "VIP" : "Padrao";
        public string HistoricoComprasResumo => ClienteAtual == null
            ? "Nenhum cliente vinculado"
            : $"{ClienteAtual.TotalServicos} servico(s) e {ClienteAtual.PontosFidelidade} ponto(s) de fidelidade";
        public string UltimaCompraResumo => ClienteAtual?.UltimaVisita?.ToString("dd/MM/yyyy") ?? "Sem historico";
        public decimal ValorTotalGastoResumo => ClienteAtual?.TotalGasto ?? 0m;
        public string InadimplenciaResumo => "Sem alertas financeiros";

        private DispatcherTimer _timer;

        public OrcamentosViewModel()
        {
            try
            {
                _orcamentoDatabaseService = new OrcamentoDatabaseService();
                _databaseService = global::PrimoAutoEletrica.App.Database;
                _financeiroDatabaseService = new FinanceiroDatabaseService();
                _vendaService = new VendaService(_databaseService);
                UsuarioLogado = global::PrimoAutoEletrica.App.Session.UserName;

                CarregarProdutos();
                CarregarOrcamentos();
                AtualizarDashboard();

                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) }; // Increased from 30s to 60s for performance
                _timer.Tick += (s, e) => {
                    DataAtual = DateTime.Now.ToString("dd/MM/yyyy");
                    HoraAtual = DateTime.Now.ToString("HH:mm");
                    OnPropertyChanged(nameof(DataAtual));
                    OnPropertyChanged(nameof(HoraAtual));
                };
                _timer.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Falha ao inicializar OrcamentosViewModel: {ex.Message}", ex);
                throw;
            }
        }

        public void CarregarOrcamentos()
        {
            var currentId = OrcamentoAtual?.Id;
            var orcamentos = _orcamentoDatabaseService.ObterTodosOrcamentos();
            Orcamentos = new ObservableCollection<Orcamento>(orcamentos);
            UltimosOrcamentos = new ObservableCollection<Orcamento>(orcamentos.Take(10));
            SelecionarOrcamento(
                Orcamentos.FirstOrDefault(o => currentId.HasValue && o.Id == currentId.Value) ??
                Orcamentos.FirstOrDefault());
        }

        public void CarregarProdutos()
        {
            Produtos = new ObservableCollection<Produto>(ObterProdutosAtivosCache());
        }

        public void AtualizarDashboard()
        {
            DashboardCards.Clear();
            var orcamentos = Orcamentos.ToList();

            foreach (var orcamento in orcamentos)
            {
                orcamento.Status = UiTextSanitizer.SanitizeText(orcamento.Status);
            }

            var orcamentosHoje = orcamentos.Count(o => o.DataCriacao.Date == DateTime.Today);
            var valorTotal = orcamentos.Sum(o => o.Total);
            var aprovados = orcamentos.Count(o => o.Status == "Aprovado");
            var pendentes = orcamentos.Count(o =>
                UiTextSanitizer.EqualsNormalized(o.Status, "Rascunho") ||
                UiTextSanitizer.EqualsNormalized(o.Status, "Em Aberto") ||
                UiTextSanitizer.EqualsNormalized(o.Status, "Enviado") ||
                UiTextSanitizer.EqualsNormalized(o.Status, "Aguardando Cliente") ||
                UiTextSanitizer.EqualsNormalized(o.Status, "Em Negociacao"));
            var perdidos = orcamentos.Count(o => o.Status == "Recusado" || o.Status == "Cancelado");
            var convertidos = orcamentos.Count(o =>
                UiTextSanitizer.EqualsNormalized(o.Status, "Convertido em Venda") ||
                UiTextSanitizer.EqualsNormalized(o.Status, "Convertido em OS"));
            var taxaConversao = orcamentos.Count > 0 ? ((decimal)convertidos / orcamentos.Count * 100).ToString("F0") + "%" : "0%";
            var ticketMedio = orcamentos.Count > 0 ? orcamentos.Average(o => o.Total) : 0;
            var lucroTotal = orcamentos.Sum(o => o.LucroEstimado);
            var vencendo = orcamentos.Count(o =>
                o.DataValidade.HasValue &&
                o.DataValidade.Value.Date <= DateTime.Today.AddDays(7) &&
                (UiTextSanitizer.EqualsNormalized(o.Status, "Rascunho") ||
                 UiTextSanitizer.EqualsNormalized(o.Status, "Em Aberto") ||
                 UiTextSanitizer.EqualsNormalized(o.Status, "Enviado")));

            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("QuotesToday"), Valor = orcamentosHoje, Comparacao = "+8%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("TotalQuotedValue"), Valor = valorTotal.ToString("C"), Comparacao = "+12%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("ApprovedPlural"), Valor = aprovados, Comparacao = "+3%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("Pending"), Valor = pendentes, Comparacao = "-2%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("LostPlural"), Valor = perdidos, Comparacao = "-1%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("ConversionRate"), Valor = taxaConversao, Comparacao = "+5%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("AverageTicket"), Valor = ticketMedio.ToString("C"), Comparacao = "+7%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = "Cliente que Mais Orça", Valor = "João Silva", Comparacao = "" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = "Produto Mais Orçado", Valor = "Bateria 60Ah", Comparacao = "" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("EstimatedProfit"), Valor = lucroTotal.ToString("C"), Comparacao = "+9%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("SalesFromQuotes"), Valor = convertidos, Comparacao = "+2%" });
            DashboardCards.Add(new DashboardCard { Icone = "*", Titulo = UiText.T("QuotesExpiring"), Valor = vencendo, Comparacao = "" });
        }

        private static IReadOnlyList<Produto> ObterProdutosAtivosCache(bool forceRefresh = false)
        {
            var agora = DateTime.Now;
            if (!forceRefresh &&
                _produtosAtivosCache != null &&
                agora - _produtosAtivosCacheAt < ProdutosCacheDuration)
            {
                return _produtosAtivosCache;
            }

            _produtosAtivosCache = App.Repositories.Produtos
                .ObterTodos()
                .Where(produto => produto.Ativo)
                .OrderBy(produto => produto.Nome)
                .ToList();
            _produtosAtivosCacheAt = agora;
            return _produtosAtivosCache;
        }

        public void NovoOrcamento()
        {
            OrcamentoAtual = new Orcamento
            {
                Numero = _orcamentoDatabaseService.GerarNumeroOrcamento(),
                Status = "Rascunho",
                DataCriacao = DateTime.Now,
                DataValidade = DateTime.Today.AddDays(30)
            };

            ItensCarrinho.Clear();
            Subtotal = 0;
            Desconto = 0;
            Acrescimo = 0;
            Total = 0;
            MargemLucro = 0;
            LucroEstimado = 0;
            QuantidadeItens = 0;
            ClienteAtual = null;
            Nome = string.Empty;
            Telefone = string.Empty;
            Documento = string.Empty;
            HistoricoCompras.Clear();
            HistoricoNegociacao.Clear();
            TimelineComercial.Clear();
            Alertas.Clear();
        }

        public void AdicionarItemAoCarrinho(Produto produto, int quantidade)
        {
            if (OrcamentoAtual == null)
            {
                NovoOrcamento();
            }

            var itemExistente = ItensCarrinho.FirstOrDefault(i => i.UsaEstoque && i.ProdutoId == produto.Id);
            
            if (itemExistente != null)
            {
                itemExistente.Quantidade += quantidade;
                itemExistente.Subtotal = itemExistente.Quantidade * itemExistente.PrecoUnitario;
                itemExistente.LucroEstimado = (itemExistente.PrecoUnitario - itemExistente.PrecoCusto) * itemExistente.Quantidade;
            }
            else
            {
                var novoItem = new OrcamentoItem
                {
                    Id = Guid.NewGuid(),
                    OrcamentoId = OrcamentoAtual?.Id ?? Guid.NewGuid(),
                    ProdutoId = produto.Id,
                    Tipo = "Produto",
                    ProdutoNome = produto.Nome,
                    ProdutoCodigo = produto.Codigo,
                    ProdutoCategoria = produto.Categoria,
                    ProdutoMarca = produto.Marca,
                    ProdutoAplicacao = "",
                    Quantidade = quantidade,
                    PrecoUnitario = produto.PrecoVenda,
                    PrecoCusto = produto.PrecoCompra,
                    Desconto = 0,
                    Subtotal = quantidade * produto.PrecoVenda,
                    LucroEstimado = (produto.PrecoVenda - produto.PrecoCompra) * quantidade,
                    MargemLucro = produto.PrecoVenda > 0 ? ((produto.PrecoVenda - produto.PrecoCompra) / produto.PrecoVenda * 100) : 0,
                    EstoqueDisponivel = produto.QuantidadeEstoque
                };

                ItensCarrinho.Add(novoItem);
            }

            CalcularTotais();
        }

        public void AdicionarMaoDeObraAoCarrinho(string descricao, int quantidade = 1, decimal precoUnitario = 0m, decimal precoCusto = 0m)
        {
            if (OrcamentoAtual == null)
            {
                NovoOrcamento();
            }

            ItensCarrinho.Add(new OrcamentoItem
            {
                Id = Guid.NewGuid(),
                OrcamentoId = OrcamentoAtual?.Id ?? Guid.NewGuid(),
                Tipo = "Servico",
                ProdutoNome = string.IsNullOrWhiteSpace(descricao) ? "Mao de obra tecnica" : descricao.Trim(),
                ProdutoCodigo = "SERVICO",
                ProdutoCategoria = "Mao de obra",
                ProdutoMarca = "Oficina",
                ProdutoAplicacao = string.Empty,
                Quantidade = Math.Max(1, quantidade),
                PrecoUnitario = Math.Max(0m, precoUnitario),
                PrecoCusto = Math.Max(0m, precoCusto),
                Desconto = 0,
                EstoqueDisponivel = 0
            });

            CalcularTotais();
        }

        public void RemoverItemDoCarrinho(OrcamentoItem item)
        {
            ItensCarrinho.Remove(item);
            CalcularTotais();
        }

        public void CalcularTotais()
        {
            Subtotal = ItensCarrinho.Sum(i => i.Subtotal);
            Total = Subtotal - Desconto + Acrescimo;
            QuantidadeItens = ItensCarrinho.Sum(i => i.Quantidade);
            LucroEstimado = ItensCarrinho.Sum(i => i.LucroEstimado);
            MargemLucro = Total > 0 ? (LucroEstimado / Total * 100) : 0;
        }

        public void SalvarOrcamento()
        {
            if (OrcamentoAtual == null) return;

            OrcamentoAtual.Itens = ItensCarrinho.ToList();
            OrcamentoAtual.Subtotal = Subtotal;
            OrcamentoAtual.Desconto = Desconto;
            OrcamentoAtual.Acrescimo = Acrescimo;
            OrcamentoAtual.Total = Total;
            OrcamentoAtual.MargemLucro = MargemLucro;
            OrcamentoAtual.LucroEstimado = LucroEstimado;
            OrcamentoAtual.ComissaoVendedor = Total * 0.05m; // 5% de comissão
            OrcamentoAtual.ImpostosEstimados = Total * 0.18m; // 18% de impostos

            if (Orcamentos.Any(o => o.Id == OrcamentoAtual.Id))
            {
                _orcamentoDatabaseService.AtualizarOrcamento(OrcamentoAtual);
            }
            else
            {
                _orcamentoDatabaseService.AdicionarOrcamento(OrcamentoAtual);
            }

            CarregarOrcamentos();
            AtualizarDashboard();
            SelecionarOrcamento(Orcamentos.FirstOrDefault(o => o.Id == OrcamentoAtual.Id));
        }

        public void DuplicarOrcamento(Orcamento orcamentoOriginal)
        {
            var novoOrcamento = new Orcamento
            {
                Id = Guid.NewGuid(),
                ClienteId = orcamentoOriginal.ClienteId,
                VeiculoId = orcamentoOriginal.VeiculoId,
                VendedorId = orcamentoOriginal.VendedorId,
                Numero = _orcamentoDatabaseService.GerarNumeroOrcamento(),
                Status = "Rascunho",
                DataCriacao = DateTime.Now,
                DataValidade = DateTime.Today.AddDays(30),
                Observacoes = orcamentoOriginal.Observacoes,
                Diagnostico = orcamentoOriginal.Diagnostico,
                DescontoTipo = orcamentoOriginal.DescontoTipo,
                DescontoPercentual = orcamentoOriginal.DescontoPercentual,
                CondicoesPagamento = orcamentoOriginal.CondicoesPagamento,
                PrazoEntrega = orcamentoOriginal.PrazoEntrega
            };

            foreach (var item in orcamentoOriginal.Itens)
            {
                novoOrcamento.Itens.Add(new OrcamentoItem
                {
                    Id = Guid.NewGuid(),
                    OrcamentoId = novoOrcamento.Id,
                    ProdutoId = item.ProdutoId,
                    Tipo = item.Tipo,
                    ProdutoNome = item.ProdutoNome,
                    ProdutoCodigo = item.ProdutoCodigo,
                    ProdutoCategoria = item.ProdutoCategoria,
                    ProdutoMarca = item.ProdutoMarca,
                    ProdutoAplicacao = item.ProdutoAplicacao,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    PrecoCusto = item.PrecoCusto,
                    Desconto = item.Desconto,
                    Subtotal = item.Subtotal,
                    LucroEstimado = item.LucroEstimado,
                    MargemLucro = item.MargemLucro,
                    EstoqueDisponivel = item.EstoqueDisponivel,
                    Observacoes = item.Observacoes
                });
            }

            novoOrcamento.Subtotal = novoOrcamento.Itens.Sum(i => i.Subtotal);
            novoOrcamento.Desconto = Math.Min(orcamentoOriginal.Desconto, novoOrcamento.Subtotal);
            novoOrcamento.Acrescimo = Math.Max(0m, orcamentoOriginal.Acrescimo);
            novoOrcamento.Total = Math.Max(0m, novoOrcamento.Subtotal - novoOrcamento.Desconto + novoOrcamento.Acrescimo);
            novoOrcamento.LucroEstimado = novoOrcamento.Itens.Sum(i => i.LucroEstimado);

            _orcamentoDatabaseService.AdicionarOrcamento(novoOrcamento);
            CarregarOrcamentos();
            AtualizarDashboard();
            SelecionarOrcamento(Orcamentos.FirstOrDefault(o => o.Id == novoOrcamento.Id) ?? novoOrcamento);
        }

        public void ExcluirOrcamento(Guid id)
        {
            _orcamentoDatabaseService.ExcluirOrcamento(id);
            CarregarOrcamentos();
            AtualizarDashboard();
        }

        public void ConverterEmVenda(Orcamento orcamento)
        {
            if (UiTextSanitizer.EqualsNormalized(orcamento.Status, "Convertido em Venda"))
            {
                return;
            }

            if (!orcamento.Itens.Any())
            {
                throw new InvalidOperationException("Nao e possivel converter um orcamento sem itens em venda.");
            }

            var cliente = orcamento.Cliente
                ?? (orcamento.ClienteId.HasValue ? ObterClientePorId(orcamento.ClienteId.Value) : null);

            var itensVenda = orcamento.Itens.Select(item =>
            {
                if (item.UsaEstoque)
                {
                    var produto = item.ProdutoId.HasValue
                        ? ObterProdutoPorId(item.ProdutoId.Value)
                        : null;

                    if (produto == null)
                    {
                        throw new InvalidOperationException($"Produto do orcamento nao encontrado: {item.ProdutoNome}.");
                    }

                    return new ItemVenda
                    {
                        Produto = produto,
                        ProdutoId = produto.Id,
                        Tipo = "Produto",
                        Descricao = item.ProdutoNome,
                        Quantidade = item.Quantidade,
                        PrecoUnitario = item.PrecoUnitario,
                        CustoUnitario = item.PrecoCusto,
                        Desconto = item.Desconto
                    };
                }

                return new ItemVenda
                {
                    Tipo = "Servico",
                    Descricao = item.ProdutoNome,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    CustoUnitario = item.PrecoCusto,
                    Desconto = item.Desconto
                };
            }).ToList();

            var venda = new Venda
            {
                Data = DateTime.Now,
                Cliente = cliente,
                Itens = itensVenda,
                Total = orcamento.Total,
                FormaPagamento = string.IsNullOrWhiteSpace(orcamento.CondicoesPagamento) ? "A definir" : orcamento.CondicoesPagamento.Trim(),
                Desconto = orcamento.Desconto,
                Usuario = UsuarioLogado
            };

            _vendaService.RegistrarVenda(venda);
            RegistrarReceitaFinanceiro(orcamento, cliente);

            orcamento.Status = "Convertido em Venda";
            orcamento.DataConversaoVenda = DateTime.Now;
            _orcamentoDatabaseService.AtualizarOrcamento(orcamento);
            CarregarOrcamentos();
            AtualizarDashboard();
            SelecionarOrcamento(Orcamentos.FirstOrDefault(o => o.Id == orcamento.Id));
            Logger.LogInfo($"Orcamento '{orcamento.Id}' convertido em venda '{venda.Id}'.");
        }

        public void AprovarOrcamento(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento);

            if (!orcamento.Itens.Any())
            {
                throw new InvalidOperationException("Nao e possivel aprovar um orcamento sem itens.");
            }

            orcamento.Status = "Aprovado";
            orcamento.DataAprovacao ??= DateTime.Now;
            _orcamentoDatabaseService.AtualizarOrcamento(orcamento);
            CarregarOrcamentos();
            AtualizarDashboard();
            SelecionarOrcamento(Orcamentos.FirstOrDefault(o => o.Id == orcamento.Id));
            Logger.LogInfo($"Orcamento '{orcamento.Id}' aprovado comercialmente.");
        }

        public OrdemServico ConverterEmOrdemServico(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento);

            if (orcamento.OrdemServicoId.HasValue)
            {
                var ordemExistente = App.Repositories.OrdensServico.ObterPorId(orcamento.OrdemServicoId.Value);
                if (ordemExistente != null)
                {
                    return ordemExistente;
                }
            }

            if (!orcamento.Itens.Any())
            {
                throw new InvalidOperationException("Nao e possivel converter um orcamento sem itens em OS.");
            }

            var cliente = orcamento.Cliente
                ?? (orcamento.ClienteId.HasValue ? ObterClientePorId(orcamento.ClienteId.Value) : null)
                ?? throw new InvalidOperationException("O orcamento precisa ter um cliente valido antes da conversao em OS.");

            var veiculo = orcamento.Veiculo
                ?? (orcamento.VeiculoId.HasValue
                    ? cliente.Veiculos.FirstOrDefault(v => v.Id == orcamento.VeiculoId.Value)
                    : null)
                ?? cliente.Veiculos.FirstOrDefault();
            var totalItens = orcamento.Itens.Sum(item => item.Subtotal);
            var diagnostico = string.IsNullOrWhiteSpace(orcamento.Diagnostico)
                ? "Executar atendimento conforme itens aprovados no orçamento."
                : orcamento.Diagnostico.Trim();

            var ordem = new OrdemServico
            {
                Numero = App.Repositories.OrdensServico.GerarProximoNumero(),
                ClienteId = cliente.Id,
                VeiculoId = veiculo?.Id,
                ClienteNomeSnapshot = cliente.Nome,
                TelefoneClienteSnapshot = string.IsNullOrWhiteSpace(cliente.Telefone) ? cliente.WhatsApp : cliente.Telefone,
                VeiculoDescricaoSnapshot = veiculo == null ? "Veiculo nao informado" : $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano}".Trim(),
                PlacaSnapshot = veiculo?.Placa ?? string.Empty,
                Status = UiTextSanitizer.EqualsNormalized(orcamento.Status, "Aprovado") ? "Aprovada" : "Aguardando aprovacao",
                Prioridade = "Normal",
                Origem = "Orcamento",
                ProblemaRelatado = $"OS criada a partir do orcamento {orcamento.Numero}.",
                Diagnostico = diagnostico,
                DiagnosticoInicial = $"Recebido a partir do orcamento {orcamento.Numero}. Diagnostico comercial: {diagnostico}",
                DiagnosticoFinal = diagnostico,
                ObservacoesCliente = orcamento.Observacoes,
                ObservacoesInternas = $"Condicoes de pagamento: {orcamento.CondicoesPagamento}; Prazo de entrega: {orcamento.PrazoEntrega}; Desconto: {orcamento.Desconto:C} ({orcamento.DescontoTipo})",
                ChecklistEntrada = string.IsNullOrWhiteSpace(orcamento.Observacoes)
                    ? "Entrada vinculada ao orçamento aprovado."
                    : $"Entrada vinculada ao orçamento aprovado. Observacoes comerciais: {orcamento.Observacoes}",
                AprovadaCliente = UiTextSanitizer.EqualsNormalized(orcamento.Status, "Aprovado"),
                MetodoAprovacao = "Orcamento",
                DataAbertura = DateTime.Now,
                DataPrevisao = (orcamento.DataValidade ?? DateTime.Today.AddDays(1)).Date.AddHours(18),
                OrcamentoId = orcamento.Id,
                Desconto = Math.Min(orcamento.Desconto, totalItens),
                Itens = orcamento.Itens.Select((item, indice) => new OrdemServicoItem
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = item.UsaEstoque ? item.ProdutoId : null,
                    Tipo = item.UsaEstoque ? "Peca" : "Servico",
                    Descricao = item.ProdutoNome,
                    Quantidade = item.Quantidade,
                    ValorUnitario = item.PrecoUnitario,
                    CustoUnitario = item.PrecoCusto,
                    Observacoes = item.Observacoes,
                    OrdemExibicao = indice + 1
                }).ToList(),
                Eventos = new List<OrdemServicoEvento>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DataEvento = DateTime.Now,
                        Titulo = "OS criada a partir de orcamento",
                        Descricao = $"Conversao do orcamento {orcamento.Numero} em ordem de servico.",
                        Tipo = "Conversao",
                        Usuario = UsuarioLogado
                    }
                }
            };

            App.Repositories.OrdensServico.Inserir(ordem);
            var persistida = App.Repositories.OrdensServico.ObterPorId(ordem.Id) ?? ordem;

            orcamento.Status = "Convertido em OS";
            orcamento.DataAprovacao ??= DateTime.Now;
            orcamento.DataConversaoOrdemServico = DateTime.Now;
            orcamento.OrdemServicoId = persistida.Id;
            _orcamentoDatabaseService.AtualizarOrcamento(orcamento);

            CarregarOrcamentos();
            AtualizarDashboard();
            SelecionarOrcamento(Orcamentos.FirstOrDefault(o => o.Id == orcamento.Id));
            Logger.LogInfo($"Orcamento '{orcamento.Id}' convertido em OS '{persistida.Id}'.");
            return persistida;
        }

        // Integração com PDV - Converter orçamento em venda no PDV
        public void ConverterParaPDV(Orcamento orcamento)
        {
            ConverterEmVenda(orcamento);
        }

        // Integração com Clientes - Buscar cliente por ID
        public Cliente? ObterClientePorId(Guid clienteId)
        {
            return App.Repositories.Clientes.ObterPorId(clienteId);
        }

        // Integração com Produtos - Buscar produto por ID
        public Produto? ObterProdutoPorId(Guid produtoId)
        {
            return App.Repositories.Produtos.ObterPorId(produtoId);
        }

        public void SelecionarOrcamento(Orcamento? orcamento)
        {
            OrcamentoAtual = orcamento;

            if (orcamento == null)
            {
                ItensCarrinho = new ObservableCollection<OrcamentoItem>();
                Subtotal = 0;
                Desconto = 0;
                Acrescimo = 0;
                Total = 0;
                MargemLucro = 0;
                LucroEstimado = 0;
                QuantidadeItens = 0;
                ClienteAtual = null;
                Nome = string.Empty;
                Telefone = string.Empty;
                Documento = string.Empty;
                HistoricoCompras.Clear();
                HistoricoNegociacao.Clear();
                TimelineComercial.Clear();
                Alertas.Clear();
                return;
            }

            ItensCarrinho = new ObservableCollection<OrcamentoItem>(orcamento.Itens);
            Subtotal = orcamento.Subtotal;
            Desconto = orcamento.Desconto;
            Acrescimo = orcamento.Acrescimo;
            Total = orcamento.Total;
            MargemLucro = orcamento.MargemLucro;
            LucroEstimado = orcamento.LucroEstimado;
            QuantidadeItens = ItensCarrinho.Sum(item => item.Quantidade);

            var cliente = orcamento.Cliente
                ?? (orcamento.ClienteId.HasValue ? ObterClientePorId(orcamento.ClienteId.Value) : null);

            AtualizarResumoCliente(cliente);
            AtualizarHistoricoComercial(orcamento);
        }

        public void DefinirClienteAtual(Cliente cliente)
        {
            if (OrcamentoAtual == null)
            {
                NovoOrcamento();
            }

            if (OrcamentoAtual == null)
            {
                return;
            }

            OrcamentoAtual.ClienteId = cliente.Id;
            OrcamentoAtual.Cliente = cliente;
            AtualizarResumoCliente(cliente);
        }

        private void AtualizarResumoCliente(Cliente? cliente)
        {
            ClienteAtual = cliente;
            Nome = cliente?.Nome ?? string.Empty;
            Telefone = cliente?.Telefone ?? string.Empty;
            Documento = cliente?.Documento ?? string.Empty;

            HistoricoCompras.Clear();
            if (cliente != null)
            {
                HistoricoCompras.Add($"{cliente.TotalServicos} servico(s) concluido(s)");
                if (cliente.TotalGasto > 0)
                {
                    HistoricoCompras.Add($"Total gasto: {cliente.TotalGasto:C}");
                }
            }
        }

        private void AtualizarHistoricoComercial(Orcamento orcamento)
        {
            HistoricoNegociacao.Clear();
            TimelineComercial.Clear();
            Alertas.Clear();

            HistoricoNegociacao.Add($"Orcamento {orcamento.Numero} em status {orcamento.Status}.");
            HistoricoNegociacao.Add($"Criado em {orcamento.DataCriacao:dd/MM/yyyy HH:mm}.");

            TimelineComercial.Add($"Criacao do orcamento {orcamento.Numero}");
            if (orcamento.DataValidade.HasValue)
            {
                TimelineComercial.Add($"Validade ate {orcamento.DataValidade.Value:dd/MM/yyyy}");
            }

            if (orcamento.DataAprovacao.HasValue)
            {
                TimelineComercial.Add($"Aprovado em {orcamento.DataAprovacao.Value:dd/MM/yyyy HH:mm}");
            }

            if (orcamento.DataConversaoVenda.HasValue)
            {
                TimelineComercial.Add($"Convertido em venda em {orcamento.DataConversaoVenda.Value:dd/MM/yyyy HH:mm}");
            }

            if (orcamento.DataConversaoOrdemServico.HasValue)
            {
                TimelineComercial.Add($"Convertido em OS em {orcamento.DataConversaoOrdemServico.Value:dd/MM/yyyy HH:mm}");
            }

            if (orcamento.DataValidade.HasValue &&
                orcamento.DataValidade.Value.Date <= DateTime.Today.AddDays(7) &&
                (UiTextSanitizer.EqualsNormalized(orcamento.Status, "Rascunho") ||
                 UiTextSanitizer.EqualsNormalized(orcamento.Status, "Em Aberto") ||
                 UiTextSanitizer.EqualsNormalized(orcamento.Status, "Enviado")))
            {
                Alertas.Add("Orcamento proximo do vencimento.");
            }

            if (!orcamento.Itens.Any())
            {
                Alertas.Add("Carrinho sem itens.");
            }

            if (UiTextSanitizer.EqualsNormalized(orcamento.Status, "Convertido em OS") && !orcamento.OrdemServicoId.HasValue)
            {
                Alertas.Add("Conversao em OS sem vinculo persistido.");
            }
        }

        // Integração com Financeiro - Registrar receita
        public void RegistrarReceitaFinanceiro(Orcamento orcamento)
        {
            RegistrarReceitaFinanceiro(orcamento, orcamento.Cliente ?? (orcamento.ClienteId.HasValue ? ObterClientePorId(orcamento.ClienteId.Value) : null));
        }

        private void RegistrarReceitaFinanceiro(Orcamento orcamento, Cliente? cliente)
        {
            orcamento.Cliente ??= cliente;
            _financeiroDatabaseService.RegistrarReceitaOrcamento(orcamento);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class DashboardCard
    {
        private string _icone = string.Empty;
        private string _titulo = string.Empty;
        private object _valor = string.Empty;
        private string _comparacao = string.Empty;
        private string _tendencia = string.Empty;

        public string Icone
        {
            get => _icone;
            set => _icone = UiTextSanitizer.SanitizeIcon(value);
        }

        public string Titulo
        {
            get => _titulo;
            set => _titulo = UiTextSanitizer.SanitizeText(value);
        }

        public object Valor
        {
            get => _valor;
            set => _valor = UiTextSanitizer.SanitizeValue(value);
        }

        public string Comparacao
        {
            get => _comparacao;
            set => _comparacao = UiTextSanitizer.SanitizeText(value);
        }

        public string Tendencia
        {
            get => _tendencia;
            set => _tendencia = UiTextSanitizer.SanitizeText(value);
        }

        public string Cor { get; set; } = string.Empty;
    }
}

