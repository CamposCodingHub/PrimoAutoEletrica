using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.ViewModels
{
    public class FluxoCaixaChartItem
    {
        public string Dia { get; set; } = "";
        public decimal Entradas { get; set; }
        public decimal Saidas { get; set; }
        public decimal Lucro { get; set; }
        public double EntradasAltura { get; set; }
        public double SaidasAltura { get; set; }
        public string LucroCor { get; set; } = "#10B981";
    }

    public class FormaPagamentoChartItem
    {
        public string Nome { get; set; } = "";
        public decimal Valor { get; set; }
        public double Percentual { get; set; }
        public double LarguraBarra { get; set; }
        public string Cor { get; set; } = "#64748B";
        public string ParticipacaoTexto => $"{Percentual:P0} do total";
    }

    public class FinanceiroAlertaDivergencia
    {
        public string Tipo { get; set; } = "";
        public string Mensagem { get; set; } = "";
        public string Severidade { get; set; } = "Baixa";
        public string Detalhes { get; set; } = "";
        public decimal Valor { get; set; }
        public string Cor { get; set; } = "#64748B";
    }

    public class CardFinanceiro : INotifyPropertyChanged
    {
        private string _icone = "";
        public string Icone
        {
            get => _icone;
            set { _icone = UiTextSanitizer.SanitizeIcon(value); OnPropertyChanged(); }
        }

        private string _titulo = "";
        public string Titulo
        {
            get => _titulo;
            set { _titulo = UiTextSanitizer.SanitizeText(value); OnPropertyChanged(); }
        }

        private decimal _valor = 0;
        public decimal Valor
        {
            get => _valor;
            set { _valor = value; OnPropertyChanged(); }
        }

        private string _comparacao = "";
        public string Comparacao
        {
            get => _comparacao;
            set { _comparacao = UiTextSanitizer.SanitizeText(value); OnPropertyChanged(); }
        }

        private string _tendencia = "";
        public string Tendencia
        {
            get => _tendencia;
            set { _tendencia = UiTextSanitizer.SanitizeText(value); OnPropertyChanged(); }
        }

        private string _corComparacao = "#28A745";
        public string CorComparacao
        {
            get => _corComparacao;
            set { _corComparacao = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ContaPagar : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Fornecedor { get; set; } = "";
        public string Descricao { get; set; } = "";
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string Status { get; set; } = "Pendente";
        public string Categoria { get; set; } = "";
        public string Observacoes { get; set; } = "";
        public DateTime DataCriacao { get; set; }
        public string Origem { get; set; } = "";
        public string ReferenciaExterna { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ContaReceber : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Cliente { get; set; } = "";
        public string Descricao { get; set; } = "";
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string Status { get; set; } = "Pendente";
        public string FormaPagamento { get; set; } = "";
        public string Observacoes { get; set; } = "";
        public DateTime DataCriacao { get; set; }
        public string Origem { get; set; } = "";
        public string ReferenciaExterna { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class FinanceiroViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly FinanceiroDatabaseService _financeiroDatabaseService;

        // Header
        private string _operadorNome = "Sistema";
        public string OperadorNome
        {
            get => _operadorNome;
            set { _operadorNome = value; OnPropertyChanged(); }
        }

        private string _caixaNumero = "01";
        public string CaixaNumero
        {
            get => _caixaNumero;
            set { _caixaNumero = value; OnPropertyChanged(); }
        }

        private DateTime _dataAtual = DateTime.Now;
        public DateTime DataAtual
        {
            get => _dataAtual;
            set { _dataAtual = value; OnPropertyChanged(); }
        }

        private DateTime _horaAtual = DateTime.Now;
        public DateTime HoraAtual
        {
            get => _horaAtual;
            set { _horaAtual = value; OnPropertyChanged(); }
        }

        private string _statusFinanceiro = "Normal";
        public string StatusFinanceiro
        {
            get => _statusFinanceiro;
            set { _statusFinanceiro = value; OnPropertyChanged(); }
        }

        // Cards Financeiros - Linha 1
        public ObservableCollection<CardFinanceiro> CardsFinanceirosLinha1 { get; set; } = new();
        
        // Cards Financeiros - Linha 2
        public ObservableCollection<CardFinanceiro> CardsFinanceirosLinha2 { get; set; } = new();
        
        // Cards Financeiros - Linha 3
        public ObservableCollection<CardFinanceiro> CardsFinanceirosLinha3 { get; set; } = new();

        // Fluxo de Caixa
        private decimal _totalEntradas = 0;
        public decimal TotalEntradas
        {
            get => _totalEntradas;
            set { _totalEntradas = value; OnPropertyChanged(); }
        }

        private decimal _totalSaidas = 0;
        public decimal TotalSaidas
        {
            get => _totalSaidas;
            set { _totalSaidas = value; OnPropertyChanged(); }
        }

        private decimal _saldoAtual = 0;
        public decimal SaldoAtual
        {
            get => _saldoAtual;
            set { _saldoAtual = value; OnPropertyChanged(); }
        }

        private decimal _saldoPrevisto = 0;
        public decimal SaldoPrevisto
        {
            get => _saldoPrevisto;
            set { _saldoPrevisto = value; OnPropertyChanged(); }
        }

        private DemonstrativoResultadoFinanceiro _demonstrativoResultado = new();
        public DemonstrativoResultadoFinanceiro DemonstrativoResultado
        {
            get => _demonstrativoResultado;
            private set { _demonstrativoResultado = value; OnPropertyChanged(); }
        }

        // Graficos
        public ObservableCollection<FluxoCaixaChartItem> FluxoCaixaGrafico { get; } = new();
        public ObservableCollection<FormaPagamentoChartItem> FormasPagamentoGrafico { get; } = new();
        public ObservableCollection<FinanceiroAlertaDivergencia> AlertasDivergencia { get; } = new();

        private string _fluxoCaixaResumo = "Fluxo ainda nao carregado.";
        public string FluxoCaixaResumo
        {
            get => _fluxoCaixaResumo;
            private set { _fluxoCaixaResumo = value; OnPropertyChanged(); }
        }

        private string _formasPagamentoResumo = "Formas de pagamento ainda nao carregadas.";
        public string FormasPagamentoResumo
        {
            get => _formasPagamentoResumo;
            private set { _formasPagamentoResumo = value; OnPropertyChanged(); }
        }

        private string _resumoPlanoFinanceiro = "Indicadores executivos ainda nao carregados.";
        public string ResumoPlanoFinanceiro
        {
            get => _resumoPlanoFinanceiro;
            private set { _resumoPlanoFinanceiro = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CardFinanceiro> IndicadoresPlanoFinanceiro { get; } = new();
        public ObservableCollection<LucroFinanceiroItem> LucroPorOrdemServico { get; } = new();
        public ObservableCollection<LucroFinanceiroItem> LucroPorProduto { get; } = new();
        public ObservableCollection<LucroFinanceiroItem> LucroPorServico { get; } = new();
        public ObservableCollection<DespesaTipoResumo> DespesasPorTipo { get; } = new();
        public ObservableCollection<RecebimentoFormaPagamentoResumo> RecebimentosPorFormaPagamento { get; } = new();
        public ObservableCollection<CaixaOperadorResumo> CaixaPorOperador { get; } = new();
        public ObservableCollection<FaturamentoPeriodoFinanceiro> FaturamentoPorPeriodo { get; } = new();

        // Contas a Pagar
        public ObservableCollection<ContaPagar> ContasPagar { get; set; } = new();
        public ObservableCollection<ContaPagar> ContasPagarFiltradas { get; set; } = new();

        private ContaPagar? _contaPagarSelecionada;
        public ContaPagar? ContaPagarSelecionada
        {
            get => _contaPagarSelecionada;
            set { _contaPagarSelecionada = value; OnPropertyChanged(); OnPropertyChanged(nameof(FichaContaResumo)); }
        }

        // Contas a Receber
        public ObservableCollection<ContaReceber> ContasReceber { get; set; } = new();
        public ObservableCollection<ContaReceber> ContasReceberFiltradas { get; set; } = new();

        private ContaReceber? _contaReceberSelecionada;
        public ContaReceber? ContaReceberSelecionada
        {
            get => _contaReceberSelecionada;
            set { _contaReceberSelecionada = value; OnPropertyChanged(); OnPropertyChanged(nameof(FichaContaResumo)); }
        }

        private string _filtroContasPagarAtual = "todas";
        private string _filtroContasReceberAtual = "todas";
        private string _textoBuscaPagar = string.Empty;
        private string _textoBuscaReceber = string.Empty;

        public string TextoBuscaPagar
        {
            get => _textoBuscaPagar;
            set
            {
                if (_textoBuscaPagar == value) return;
                _textoBuscaPagar = value ?? string.Empty;
                OnPropertyChanged();
                FiltrarContasPagar(_filtroContasPagarAtual);
            }
        }

        public string TextoBuscaReceber
        {
            get => _textoBuscaReceber;
            set
            {
                if (_textoBuscaReceber == value) return;
                _textoBuscaReceber = value ?? string.Empty;
                OnPropertyChanged();
                FiltrarContasReceber(_filtroContasReceberAtual);
            }
        }

        public string PulseReceberText { get; private set; } = "0";
        public string PulsePagarText { get; private set; } = "0";
        public string PulseVencendoHojeText { get; private set; } = "0";
        public string PulseVencidasText { get; private set; } = "0";
        public string PulseSaldoText { get; private set; } = "R$ 0,00";

        public string FichaContaResumo
        {
            get
            {
                if (ContaPagarSelecionada != null && ContaReceberSelecionada == null)
                    return MontarFichaPagar(ContaPagarSelecionada);
                if (ContaReceberSelecionada != null && ContaPagarSelecionada == null)
                    return MontarFichaReceber(ContaReceberSelecionada);
                if (ContaReceberSelecionada != null)
                    return MontarFichaReceber(ContaReceberSelecionada);
                if (ContaPagarSelecionada != null)
                    return MontarFichaPagar(ContaPagarSelecionada);
                return "Selecione uma conta a pagar ou a receber para ver a ficha operacional.";
            }
        }

        public FinanceiroViewModel()
        {
            _databaseService = global::PrimoAutoEletrica.App.Database;
            _financeiroDatabaseService = new FinanceiroDatabaseService();
            OperadorNome = global::PrimoAutoEletrica.App.Session.UserName;
            CarregarDadosIniciais();
            CarregarDadosGraficos();
            CarregarContasDoBanco();
            CarregarIndicadoresPlanoFinanceiro();
            RecalcularAlertasDivergencia();
            AtualizarPulseFinanceiro();
        }

        private void CarregarDadosIniciais()
        {
            // Limpa os cards
            CardsFinanceirosLinha1.Clear();
            CardsFinanceirosLinha2.Clear();
            CardsFinanceirosLinha3.Clear();

            // Datas para cálculos
            var hoje = DateTime.Today;
            var primeiroDiaMes = new DateTime(hoje.Year, hoje.Month, 1);
            var ultimoDiaMes = primeiroDiaMes.AddMonths(1).AddDays(-1);

            // Movimentações do mês
            var movimentacoes = _financeiroDatabaseService.ObterMovimentacoes(primeiroDiaMes, ultimoDiaMes, limit: 1200);

            // Entradas e saídas do mês
            decimal entradasMes = 0, saidasMes = 0, entradasHoje = 0, saidasHoje = 0;
            foreach (var mov in movimentacoes)
            {
                if (DateTime.TryParse(mov.Data, out DateTime dataMov))
                {
                    if (dataMov >= primeiroDiaMes && dataMov <= ultimoDiaMes)
                    {
                        if (TipoMovimentoEh(mov.Tipo, "Entrada")) entradasMes += Convert.ToDecimal(mov.Valor);
                        if (TipoMovimentoEh(mov.Tipo, "Saida")) saidasMes += Convert.ToDecimal(mov.Valor);
                    }
                    if (dataMov == hoje)
                    {
                        if (TipoMovimentoEh(mov.Tipo, "Entrada")) entradasHoje += Convert.ToDecimal(mov.Valor);
                        if (TipoMovimentoEh(mov.Tipo, "Saida")) saidasHoje += Convert.ToDecimal(mov.Valor);
                    }
                }
            }
            decimal saldoAtual = entradasMes - saidasMes;
            decimal lucroHoje = entradasHoje - saidasHoje;

            // Cards Linha 1
            CardsFinanceirosLinha1.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Saldo Atual",
                Valor = saldoAtual,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = saldoAtual >= 0 ? "#28A745" : "#DC3545"
            });
            CardsFinanceirosLinha1.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Entradas Hoje",
                Valor = entradasHoje,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = "#28A745"
            });
            CardsFinanceirosLinha1.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Saídas Hoje",
                Valor = saidasHoje,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = "#DC3545"
            });
            CardsFinanceirosLinha1.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Lucro do Dia",
                Valor = lucroHoje,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = lucroHoje >= 0 ? "#28A745" : "#DC3545"
            });
            CardsFinanceirosLinha1.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Faturamento Mensal",
                Valor = entradasMes,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = "#28A745"
            });

            // Contas a pagar/receber
            var contasPagar = _financeiroDatabaseService.ObterContasPagar();
            var contasReceber = _financeiroDatabaseService.ObterContasReceber();
            decimal totalPagar = 0, totalReceber = 0, totalVencidas = 0;
            int qtdPagar = 0, qtdReceber = 0, qtdVencidas = 0;
            foreach (var c in contasPagar)
            {
                if (DateTime.TryParse(c.DataVencimento, out DateTime dv))
                {
                    if (dv < hoje && !ContaPagarEstaLiquidada(c.Status))
                    {
                        totalVencidas += Convert.ToDecimal(c.Valor);
                        qtdVencidas++;
                    }
                    if (!ContaPagarEstaLiquidada(c.Status))
                    {
                        totalPagar += Convert.ToDecimal(c.Valor);
                        qtdPagar++;
                    }
                }
            }
            foreach (var c in contasReceber)
            {
                if (DateTime.TryParse(c.DataVencimento, out DateTime dv))
                {
                    if (!ContaReceberEstaLiquidada(c.Status))
                    {
                        totalReceber += Convert.ToDecimal(c.Valor);
                        qtdReceber++;
                    }
                }
            }

            // Cards Linha 2
            CardsFinanceirosLinha2.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Contas Vencidas",
                Valor = totalVencidas,
                Comparacao = $"{qtdVencidas} contas",
                Tendencia = "",
                CorComparacao = totalVencidas > 0 ? "#DC3545" : "#28A745"
            });
            CardsFinanceirosLinha2.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Contas a Receber",
                Valor = totalReceber,
                Comparacao = $"{qtdReceber} contas",
                Tendencia = "",
                CorComparacao = "#FFC107"
            });
            CardsFinanceirosLinha2.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Contas a Pagar",
                Valor = totalPagar,
                Comparacao = $"{qtdPagar} contas",
                Tendencia = "",
                CorComparacao = "#17A2B8"
            });
            CardsFinanceirosLinha2.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Caixa Disponível",
                Valor = saldoAtual,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = saldoAtual >= 0 ? "#28A745" : "#DC3545"
            });
            CardsFinanceirosLinha2.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Valor em Bancos",
                Valor = 0, // Implementar integração bancária se necessário
                Comparacao = "",
                Tendencia = "",
                CorComparacao = "#28A745"
            });

            // Cards Linha 3 (formas de pagamento)
            var formasPagamento = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var mov in movimentacoes)
            {
                if (DateTime.TryParse(mov.Data, out DateTime dataMov))
                {
                    if (dataMov >= primeiroDiaMes && dataMov <= ultimoDiaMes && TipoMovimentoEh(mov.Tipo, "Entrada"))
                    {
                        string forma = string.IsNullOrWhiteSpace(mov.FormaPagamento) ? "Outros" : UiTextSanitizer.SanitizeText(mov.FormaPagamento);
                        if (!formasPagamento.ContainsKey(forma))
                            formasPagamento[forma] = 0;
                        formasPagamento[forma] += Convert.ToDecimal(mov.Valor);
                    }
                }
            }
            var totalPix = ObterTotalForma(formasPagamento, "PIX");
            var totalCartao = ObterTotalForma(formasPagamento, "Debito", "Débito") + ObterTotalForma(formasPagamento, "Credito", "Crédito");

            CardsFinanceirosLinha3.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Total em PIX",
                Valor = totalPix,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = "#28A745"
            });
            CardsFinanceirosLinha3.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Total em Cartão",
                Valor = totalCartao,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = "#28A745"
            });
            // Ticket médio

            int qtdEntradas = 0;
            foreach (var mov in movimentacoes)
            {
                if (DateTime.TryParse(mov.Data, out DateTime dataMov))
                {
                    if (dataMov >= primeiroDiaMes && dataMov <= ultimoDiaMes && TipoMovimentoEh(mov.Tipo, "Entrada"))
                        qtdEntradas++;
                }
            }
            decimal ticketMedio = qtdEntradas > 0 ? entradasMes / qtdEntradas : 0;
            CardsFinanceirosLinha3.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Ticket Médio",
                Valor = ticketMedio,
                Comparacao = "",
                Tendencia = "",
                CorComparacao = "#28A745"
            });
            // Produtos mais vendidos e inadimplentes: implementar se houver dados disponíveis
            CardsFinanceirosLinha3.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Produtos Mais Vendidos",
                Valor = 0, // Implementar se houver dados
                Comparacao = "",
                Tendencia = "",
                CorComparacao = "#28A745"
            });
            CardsFinanceirosLinha3.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Clientes Inadimplentes",
                Valor = totalVencidas,
                Comparacao = $"{qtdVencidas} titulos",
                Tendencia = "",
                CorComparacao = "#DC3545"
            });

            // Fluxo de Caixa
            TotalEntradas = entradasMes;
            TotalSaidas = saidasMes;
            SaldoAtual = saldoAtual;
            SaldoPrevisto = saldoAtual + totalReceber - totalPagar;
            DemonstrativoResultado = _financeiroDatabaseService.ObterDemonstrativoResultado(primeiroDiaMes, ultimoDiaMes);
        }

        private void CarregarDadosGraficos()
        {
            FluxoCaixaGrafico.Clear();
            FormasPagamentoGrafico.Clear();

            var hoje = DateTime.Today;
            var primeiroDia = new DateTime(hoje.Year, hoje.Month, 1);
            var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);

            var movimentacoes = _financeiroDatabaseService.ObterMovimentacoes(primeiroDia, ultimoDia, limit: 1200);

            var diasNoMes = (ultimoDia - primeiroDia).Days + 1;
            var entradasPorDia = new double[diasNoMes];
            var saidasPorDia = new double[diasNoMes];

            foreach (var mov in movimentacoes)
            {
                if (DateTime.TryParse(mov.Data, out DateTime dataMov))
                {
                    if (dataMov >= primeiroDia && dataMov <= ultimoDia)
                    {
                        int dia = (dataMov - primeiroDia).Days;
                        if (TipoMovimentoEh(mov.Tipo, "Entrada"))
                            entradasPorDia[dia] += Convert.ToDouble(mov.Valor);
                        if (TipoMovimentoEh(mov.Tipo, "Saida"))
                            saidasPorDia[dia] += Convert.ToDouble(mov.Valor);
                    }
                }
            }

            var lucroPorDia = new double[diasNoMes];
            for (int i = 0; i < diasNoMes; i++)
            {
                lucroPorDia[i] = entradasPorDia[i] - saidasPorDia[i];
            }

            var maiorValorDiario = Math.Max(
                1d,
                Math.Max(
                    entradasPorDia.DefaultIfEmpty(0).Max(),
                    saidasPorDia.DefaultIfEmpty(0).Max()));

            for (int i = 0; i < diasNoMes; i++)
            {
                FluxoCaixaGrafico.Add(new FluxoCaixaChartItem
                {
                    Dia = primeiroDia.AddDays(i).ToString("dd"),
                    Entradas = Convert.ToDecimal(entradasPorDia[i]),
                    Saidas = Convert.ToDecimal(saidasPorDia[i]),
                    Lucro = Convert.ToDecimal(lucroPorDia[i]),
                    EntradasAltura = entradasPorDia[i] <= 0 ? 6 : Math.Max(10, (entradasPorDia[i] / maiorValorDiario) * 140),
                    SaidasAltura = saidasPorDia[i] <= 0 ? 6 : Math.Max(10, (saidasPorDia[i] / maiorValorDiario) * 140),
                    LucroCor = lucroPorDia[i] >= 0 ? "#10B981" : "#EF4444"
                });
            }

            var formasPagamento = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var mov in movimentacoes)
            {
                if (DateTime.TryParse(mov.Data, out DateTime dataMov))
                {
                    if (dataMov >= primeiroDia && dataMov <= ultimoDia && TipoMovimentoEh(mov.Tipo, "Entrada"))
                    {
                        string forma = string.IsNullOrWhiteSpace(mov.FormaPagamento) ? "Outros" : mov.FormaPagamento;
                        if (!formasPagamento.ContainsKey(forma))
                            formasPagamento[forma] = 0;
                        formasPagamento[forma] += Convert.ToDouble(mov.Valor);
                    }
                }
            }

            var coresPadrao = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "PIX", "#10B981" },
                { "Dinheiro", "#F59E0B" },
                { "Debito", "#06B6D4" },
                { "Credito", "#6366F1" },
                { "Boleto", "#EF4444" },
                { "Outros", "#64748B" }
            };

            var totalFormas = formasPagamento.Values.Sum();
            const double larguraMaximaBarra = 220d;

            foreach (var kvp in formasPagamento.OrderByDescending(item => item.Value))
            {
                var chaveCor = NormalizarChaveFormaPagamento(kvp.Key);
                var percentual = totalFormas <= 0 ? 0 : kvp.Value / totalFormas;
                var cor = coresPadrao.ContainsKey(chaveCor) ? coresPadrao[chaveCor] : coresPadrao["Outros"];

                FormasPagamentoGrafico.Add(new FormaPagamentoChartItem
                {
                    Nome = kvp.Key,
                    Valor = Convert.ToDecimal(kvp.Value),
                    Percentual = percentual,
                    LarguraBarra = Math.Max(18, larguraMaximaBarra * percentual),
                    Cor = cor
                });
            }

            var diasComMovimento = FluxoCaixaGrafico.Count(item => item.Entradas > 0 || item.Saidas > 0);
            var melhorDia = FluxoCaixaGrafico.OrderByDescending(item => item.Lucro).FirstOrDefault();
            var piorDia = FluxoCaixaGrafico.OrderBy(item => item.Lucro).FirstOrDefault();
            FluxoCaixaResumo = diasComMovimento == 0
                ? "Sem movimentacoes no mes atual."
                : $"{diasComMovimento} dia(s) com movimento. Melhor dia: {melhorDia?.Dia} ({melhorDia?.Lucro:C}); ponto de atencao: {piorDia?.Dia} ({piorDia?.Lucro:C}).";

            var principalForma = FormasPagamentoGrafico.OrderByDescending(item => item.Valor).FirstOrDefault();
            FormasPagamentoResumo = principalForma == null
                ? "Sem recebimentos por forma de pagamento no mes atual."
                : $"Principal forma: {principalForma.Nome} com {principalForma.Valor:C} ({principalForma.Percentual:P0}).";
        }

        private static string NormalizarChaveFormaPagamento(string formaPagamento)
        {
            if (string.IsNullOrWhiteSpace(formaPagamento))
            {
                return "Outros";
            }

            var texto = formaPagamento.Trim().ToLowerInvariant();

            return texto switch
            {
                "pix" => "PIX",
                "dinheiro" => "Dinheiro",
                "debito" => "Debito",
                "débito" => "Debito",
                "credito" => "Credito",
                "crédito" => "Credito",
                "boleto" => "Boleto",
                _ => "Outros"
            };
        }

        public void CarregarContasDoBanco()
        {
            // Carregar contas a pagar
            var contasPagarData = _financeiroDatabaseService.ObterContasPagar();
            ContasPagar.Clear();
            foreach (var conta in contasPagarData)
            {
                ContasPagar.Add(new ContaPagar
                {
                    Id = conta.Id,
                    Fornecedor = conta.Fornecedor,
                    Descricao = conta.Descricao,
                    Valor = conta.Valor,
                    DataVencimento = DateTime.Parse(conta.DataVencimento),
                    DataPagamento = conta.DataPagamento != null ? DateTime.Parse(conta.DataPagamento) : null,
                    Status = conta.Status,
                    Categoria = conta.Categoria,
                    Observacoes = conta.Observacoes,
                    DataCriacao = DateTime.Parse(conta.DataCriacao),
                    Origem = conta.Origem,
                    ReferenciaExterna = conta.ReferenciaExterna
                });
            }
            ContasPagarFiltradas = new ObservableCollection<ContaPagar>(ContasPagar);
            ContaPagarSelecionada = ContasPagarFiltradas.FirstOrDefault();
            OnPropertyChanged(nameof(ContasPagarFiltradas));

            // Carregar contas a receber
            var contasReceberData = _financeiroDatabaseService.ObterContasReceber();
            ContasReceber.Clear();
            foreach (var conta in contasReceberData)
            {
                ContasReceber.Add(new ContaReceber
                {
                    Id = conta.Id,
                    Cliente = conta.Cliente,
                    Descricao = conta.Descricao,
                    Valor = conta.Valor,
                    DataVencimento = DateTime.Parse(conta.DataVencimento),
                    DataPagamento = conta.DataPagamento != null ? DateTime.Parse(conta.DataPagamento) : null,
                    Status = conta.Status,
                    FormaPagamento = conta.FormaPagamento,
                    Observacoes = conta.Observacoes,
                    DataCriacao = DateTime.Parse(conta.DataCriacao),
                    Origem = conta.Origem,
                    ReferenciaExterna = conta.ReferenciaExterna
                });
            }
            ContasReceberFiltradas = new ObservableCollection<ContaReceber>(ContasReceber);
            ContaReceberSelecionada = ContasReceberFiltradas.FirstOrDefault();
            OnPropertyChanged(nameof(ContasReceberFiltradas));
            AtualizarPulseFinanceiro();
        }

        public void AtualizarDashboard()
        {
            DataAtual = DateTime.Now;
            HoraAtual = DateTime.Now;

            CarregarDadosIniciais();
            CarregarDadosGraficos();
            CarregarContasDoBanco();
            CarregarIndicadoresPlanoFinanceiro();
            RecalcularAlertasDivergencia();
            AtualizarPulseFinanceiro();
        }

        private void CarregarIndicadoresPlanoFinanceiro()
        {
            var (inicio, fim) = ObterPeriodoFinanceiroAtual();
            var resumo = _financeiroDatabaseService.ObterResumoExecutivoFinanceiro(inicio, fim);

            ReplaceCollection(LucroPorOrdemServico, _financeiroDatabaseService.ObterLucroPorOrdemServico(inicio, fim, 8));
            ReplaceCollection(LucroPorProduto, _financeiroDatabaseService.ObterLucroPorProduto(inicio, fim, 8));
            ReplaceCollection(LucroPorServico, _financeiroDatabaseService.ObterLucroPorServico(inicio, fim, 8));
            ReplaceCollection(DespesasPorTipo, _financeiroDatabaseService.ObterDespesasPorTipo(inicio, fim));
            ReplaceCollection(RecebimentosPorFormaPagamento, _financeiroDatabaseService.ObterRecebimentosPorFormaPagamento(inicio, fim));
            ReplaceCollection(CaixaPorOperador, _financeiroDatabaseService.ObterCaixaPorOperador(inicio, fim, 8));
            ReplaceCollection(FaturamentoPorPeriodo, _financeiroDatabaseService.ObterFaturamentoPorPeriodo(inicio, fim, 12));

            IndicadoresPlanoFinanceiro.Clear();
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Faturamento periodo",
                Valor = resumo.FaturamentoPeriodo,
                Comparacao = $"{inicio:dd/MM} a {fim:dd/MM}",
                CorComparacao = "#10B981"
            });
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Lucro por OS",
                Valor = resumo.LucroOrdensServico,
                Comparacao = $"{LucroPorOrdemServico.Count} OS analisada(s)",
                CorComparacao = resumo.LucroOrdensServico >= 0 ? "#10B981" : "#EF4444"
            });
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Lucro por produto",
                Valor = resumo.LucroProdutos,
                Comparacao = $"{LucroPorProduto.Count} produto(s)",
                CorComparacao = resumo.LucroProdutos >= 0 ? "#10B981" : "#EF4444"
            });
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Lucro por servico",
                Valor = resumo.LucroServicos,
                Comparacao = $"{LucroPorServico.Count} servico(s)",
                CorComparacao = resumo.LucroServicos >= 0 ? "#10B981" : "#EF4444"
            });
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Despesas fixas",
                Valor = resumo.DespesasFixas,
                Comparacao = ObterQuantidadeDespesa("fix"),
                CorComparacao = "#F59E0B"
            });
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Despesas variaveis",
                Valor = resumo.DespesasVariaveis,
                Comparacao = ObterQuantidadeDespesa("vari"),
                CorComparacao = "#F97316"
            });
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Recebimentos por forma",
                Valor = resumo.RecebimentosPorForma,
                Comparacao = $"{RecebimentosPorFormaPagamento.Count} forma(s)",
                CorComparacao = "#0284C7"
            });
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Caixa por operador",
                Valor = resumo.SaldoCaixaOperadores,
                Comparacao = $"{CaixaPorOperador.Count} operador(es)",
                CorComparacao = resumo.SaldoCaixaOperadores >= 0 ? "#10B981" : "#EF4444"
            });
            IndicadoresPlanoFinanceiro.Add(new CardFinanceiro
            {
                Icone = "*",
                Titulo = "Inadimplencia",
                Valor = resumo.Inadimplencia,
                Comparacao = resumo.Inadimplencia > 0 ? "Cobrar vencidos" : "Sem atraso critico",
                CorComparacao = resumo.Inadimplencia > 0 ? "#EF4444" : "#10B981"
            });

            var topProduto = LucroPorProduto.OrderByDescending(item => item.LucroBruto).FirstOrDefault();
            var topServico = LucroPorServico.OrderByDescending(item => item.LucroBruto).FirstOrDefault();
            ResumoPlanoFinanceiro =
                $"Periodo {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}. " +
                $"Resultado geral {resumo.ResultadoGeral:C}. " +
                $"Produto destaque: {topProduto?.Referencia ?? "sem vendas no periodo"}. " +
                $"Servico destaque: {topServico?.Referencia ?? "sem servicos no periodo"}.";

            OnPropertyChanged(nameof(IndicadoresPlanoFinanceiro));
        }

        public void RecalcularAlertasDivergencia()
        {
            AlertasDivergencia.Clear();

            var hoje = DateTime.Today;
            var contasPagarVencidas = ContasPagar
                .Where(conta => conta.DataVencimento.Date < hoje && !ContaPagarEstaLiquidada(conta.Status))
                .ToList();
            var contasReceberVencidas = ContasReceber
                .Where(conta => conta.DataVencimento.Date < hoje && !ContaReceberEstaLiquidada(conta.Status))
                .ToList();
            var receberPagasSemData = ContasReceber
                .Where(conta => ContaReceberEstaLiquidada(conta.Status) && !conta.DataPagamento.HasValue)
                .ToList();
            var pagarPagasSemData = ContasPagar
                .Where(conta => ContaPagarEstaLiquidada(conta.Status) && !conta.DataPagamento.HasValue)
                .ToList();

            if (contasPagarVencidas.Count > 0)
            {
                AdicionarAlerta(
                    "Contas a pagar vencidas",
                    $"{contasPagarVencidas.Count} titulo(s) vencido(s) aguardando baixa.",
                    "Alta",
                    contasPagarVencidas.Sum(conta => conta.Valor),
                    "Regularize os pagamentos atrasados ou reagende vencimentos com justificativa.",
                    "#DC3545");
            }

            if (contasReceberVencidas.Count > 0)
            {
                AdicionarAlerta(
                    "Inadimplencia",
                    $"{contasReceberVencidas.Count} titulo(s) vencido(s) a receber.",
                    "Alta",
                    contasReceberVencidas.Sum(conta => conta.Valor),
                    "Priorize cobranca e conciliacao antes de considerar o valor no caixa disponivel.",
                    "#F59E0B");
            }

            if (receberPagasSemData.Count > 0 || pagarPagasSemData.Count > 0)
            {
                AdicionarAlerta(
                    "Status sem data",
                    $"{receberPagasSemData.Count + pagarPagasSemData.Count} titulo(s) liquidado(s) sem data de pagamento.",
                    "Media",
                    receberPagasSemData.Sum(conta => conta.Valor) + pagarPagasSemData.Sum(conta => conta.Valor),
                    "Revise titulos com status liquidado sem data para manter a conciliacao correta.",
                    "#D97706");
            }

            if (SaldoPrevisto < 0)
            {
                AdicionarAlerta(
                    "Saldo projetado negativo",
                    $"Saldo projetado em {SaldoPrevisto:C}.",
                    "Alta",
                    Math.Abs(SaldoPrevisto),
                    "O contas a pagar pendente supera caixa e recebiveis do periodo.",
                    "#DC3545");
            }

            if (TotalSaidas > TotalEntradas && TotalEntradas > 0)
            {
                AdicionarAlerta(
                    "Saidas acima das entradas",
                    $"Saidas do mes superam entradas em {(TotalSaidas - TotalEntradas):C}.",
                    "Media",
                    TotalSaidas - TotalEntradas,
                    "Confira despesas extraordinarias e vendas pendentes de conciliacao.",
                    "#F97316");
            }

            var (inicio, fim) = ObterPeriodoFinanceiroAtual();
            var movimentacoesEntradaSemForma = _financeiroDatabaseService
                .ObterMovimentacoes(inicio, fim, limit: 1200)
                .Where(mov => TipoMovimentoEh((string)mov.Tipo, "Entrada") && string.IsNullOrWhiteSpace((string)mov.FormaPagamento))
                .ToList();

            if (movimentacoesEntradaSemForma.Count > 0)
            {
                AdicionarAlerta(
                    "Receita sem forma de pagamento",
                    $"{movimentacoesEntradaSemForma.Count} entrada(s) sem forma de pagamento.",
                    "Baixa",
                    movimentacoesEntradaSemForma.Sum(mov => (decimal)mov.Valor),
                    "Classifique a forma de pagamento para melhorar os graficos e a conciliacao.",
                    "#0284C7");
            }

            var diferencaFluxoDre = Math.Abs((TotalEntradas - TotalSaidas) - DemonstrativoResultado.ResultadoOperacional);
            if (diferencaFluxoDre > 0.01m)
            {
                AdicionarAlerta(
                    "Divergencia fluxo x DRE",
                    $"Diferenca de {diferencaFluxoDre:C} entre fluxo mensal e DRE operacional.",
                    "Alta",
                    diferencaFluxoDre,
                    "Revise filtros, datas e integracoes financeiras antes de fechar o periodo.",
                    "#7C3AED");
            }

            StatusFinanceiro = AlertasDivergencia.Any(alerta => alerta.Severidade == "Alta")
                ? "Atencao"
                : AlertasDivergencia.Count > 0 ? "Monitorar" : "Normal";
            OnPropertyChanged(nameof(AlertasDivergencia));
        }

        private void AdicionarAlerta(string tipo, string mensagem, string severidade, decimal valor, string detalhes, string cor)
        {
            AlertasDivergencia.Add(new FinanceiroAlertaDivergencia
            {
                Tipo = tipo,
                Mensagem = mensagem,
                Severidade = severidade,
                Valor = valor,
                Detalhes = detalhes,
                Cor = cor
            });
        }

        public void AtualizarHora()
        {
            HoraAtual = DateTime.Now;
        }

        public void AtualizarDadosDoPDV(decimal valorVenda, string formaPagamento)
        {
            // Atualiza entradas com venda do PDV
            TotalEntradas += valorVenda;
            SaldoAtual += valorVenda;
            
            // Atualiza card de entradas hoje
            if (CardsFinanceirosLinha1.Count > 1)
            {
                CardsFinanceirosLinha1[1].Valor += valorVenda;
            }
            
            // Atualiza card de lucro do dia
            if (CardsFinanceirosLinha1.Count > 3)
            {
                CardsFinanceirosLinha1[3].Valor += valorVenda * 0.3m; // Assumindo 30% de lucro
            }
            
            OnPropertyChanged(nameof(TotalEntradas));
            OnPropertyChanged(nameof(SaldoAtual));
        }

        public void RegistrarPagamentoContaPagar(ContaPagar conta, string formaPagamento = "Transferencia")
        {
            ArgumentNullException.ThrowIfNull(conta);
            _financeiroDatabaseService.BaixarContaPagar(
                conta.Id,
                DateTime.Today,
                formaPagamento,
                $"Baixa operacional da conta a pagar '{conta.Descricao}'.");
            AtualizarDashboard();
        }

        public void RegistrarRecebimentoContaReceber(ContaReceber conta, string formaPagamento = "PIX")
        {
            ArgumentNullException.ThrowIfNull(conta);
            _financeiroDatabaseService.BaixarContaReceber(
                conta.Id,
                DateTime.Today,
                formaPagamento,
                $"Baixa operacional da conta a receber '{conta.Descricao}'.");
            AtualizarDashboard();
        }

        // Filtros de Contas a Pagar
        public void FiltrarContasPagar(string filtro)
        {
            _filtroContasPagarAtual = string.IsNullOrWhiteSpace(filtro) ? "todas" : filtro;
            ContasPagarFiltradas.Clear();

            var hoje = DateTime.Today;
            IEnumerable<ContaPagar> consulta = ContasPagar;

            switch (_filtroContasPagarAtual.ToLowerInvariant())
            {
                case "vencidas":
                    consulta = ContasPagar.Where(c => c.DataVencimento < hoje && !ContaPagarEstaLiquidada(c.Status));
                    break;
                case "hoje":
                    consulta = ContasPagar.Where(c => c.DataVencimento.Date == hoje);
                    break;
                case "semana":
                    var fimSemana = hoje.AddDays(7);
                    consulta = ContasPagar.Where(c => c.DataVencimento >= hoje && c.DataVencimento <= fimSemana);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(TextoBuscaPagar))
            {
                var termo = TextoBuscaPagar.Trim();
                consulta = consulta.Where(c =>
                    ContemTexto(c.Fornecedor, termo) ||
                    ContemTexto(c.Descricao, termo) ||
                    ContemTexto(c.Categoria, termo) ||
                    ContemTexto(c.Origem, termo) ||
                    ContemTexto(c.ReferenciaExterna, termo) ||
                    ContemTexto(c.Status, termo));
            }

            foreach (var conta in consulta)
                ContasPagarFiltradas.Add(conta);

            if (ContaPagarSelecionada != null && !ContasPagarFiltradas.Contains(ContaPagarSelecionada))
            {
                ContaPagarSelecionada = ContasPagarFiltradas.FirstOrDefault();
            }

            OnPropertyChanged(nameof(FichaContaResumo));
        }

        // Filtros de Contas a Receber
        public void FiltrarContasReceber(string filtro)
        {
            _filtroContasReceberAtual = string.IsNullOrWhiteSpace(filtro) ? "todas" : filtro;
            ContasReceberFiltradas.Clear();

            var hoje = DateTime.Today;
            IEnumerable<ContaReceber> consulta = ContasReceber;

            switch (_filtroContasReceberAtual.ToLowerInvariant())
            {
                case "vencidas":
                    consulta = ContasReceber.Where(c => c.DataVencimento < hoje && !ContaReceberEstaLiquidada(c.Status));
                    break;
                case "hoje":
                    consulta = ContasReceber.Where(c => c.DataVencimento.Date == hoje);
                    break;
                case "semana":
                    var fimSemana = hoje.AddDays(7);
                    consulta = ContasReceber.Where(c => c.DataVencimento >= hoje && c.DataVencimento <= fimSemana);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(TextoBuscaReceber))
            {
                var termo = TextoBuscaReceber.Trim();
                consulta = consulta.Where(c =>
                    ContemTexto(c.Cliente, termo) ||
                    ContemTexto(c.Descricao, termo) ||
                    ContemTexto(c.FormaPagamento, termo) ||
                    ContemTexto(c.Origem, termo) ||
                    ContemTexto(c.ReferenciaExterna, termo) ||
                    ContemTexto(c.Status, termo));
            }

            foreach (var conta in consulta)
                ContasReceberFiltradas.Add(conta);

            if (ContaReceberSelecionada != null && !ContasReceberFiltradas.Contains(ContaReceberSelecionada))
            {
                ContaReceberSelecionada = ContasReceberFiltradas.FirstOrDefault();
            }

            OnPropertyChanged(nameof(FichaContaResumo));
        }

        private void AtualizarPulseFinanceiro()
        {
            var hoje = DateTime.Today;
            var receberPendentes = ContasReceber.Where(c => !ContaReceberEstaLiquidada(c.Status)).ToList();
            var pagarPendentes = ContasPagar.Where(c => !ContaPagarEstaLiquidada(c.Status)).ToList();

            PulseReceberText = receberPendentes.Sum(c => c.Valor).ToString("C2");
            PulsePagarText = pagarPendentes.Sum(c => c.Valor).ToString("C2");
            PulseVencendoHojeText = (
                receberPendentes.Count(c => c.DataVencimento.Date == hoje) +
                pagarPendentes.Count(c => c.DataVencimento.Date == hoje)).ToString();
            PulseVencidasText = (
                receberPendentes.Count(c => c.DataVencimento.Date < hoje) +
                pagarPendentes.Count(c => c.DataVencimento.Date < hoje)).ToString();
            PulseSaldoText = SaldoAtual.ToString("C2");

            OnPropertyChanged(nameof(PulseReceberText));
            OnPropertyChanged(nameof(PulsePagarText));
            OnPropertyChanged(nameof(PulseVencendoHojeText));
            OnPropertyChanged(nameof(PulseVencidasText));
            OnPropertyChanged(nameof(PulseSaldoText));
            OnPropertyChanged(nameof(FichaContaResumo));
        }

        private static string MontarFichaPagar(ContaPagar conta) =>
            $"Conta a pagar #{conta.Id}\n" +
            $"Fornecedor: {conta.Fornecedor}\n" +
            $"Descricao: {conta.Descricao}\n" +
            $"Valor: {conta.Valor:C2}\n" +
            $"Vencimento: {conta.DataVencimento:dd/MM/yyyy}\n" +
            $"Status: {conta.Status}\n" +
            $"Pagamento: {(conta.DataPagamento?.ToString("dd/MM/yyyy") ?? "-")}\n" +
            $"Categoria: {(string.IsNullOrWhiteSpace(conta.Categoria) ? "-" : conta.Categoria)}\n" +
            $"Origem: {(string.IsNullOrWhiteSpace(conta.Origem) ? "-" : conta.Origem)}\n" +
            $"Referencia: {(string.IsNullOrWhiteSpace(conta.ReferenciaExterna) ? "-" : conta.ReferenciaExterna)}\n" +
            $"Observacoes: {(string.IsNullOrWhiteSpace(conta.Observacoes) ? "-" : conta.Observacoes)}";

        private static string MontarFichaReceber(ContaReceber conta) =>
            $"Conta a receber #{conta.Id}\n" +
            $"Cliente: {conta.Cliente}\n" +
            $"Descricao: {conta.Descricao}\n" +
            $"Valor: {conta.Valor:C2}\n" +
            $"Vencimento: {conta.DataVencimento:dd/MM/yyyy}\n" +
            $"Status: {conta.Status}\n" +
            $"Recebimento: {(conta.DataPagamento?.ToString("dd/MM/yyyy") ?? "-")}\n" +
            $"Forma: {(string.IsNullOrWhiteSpace(conta.FormaPagamento) ? "-" : conta.FormaPagamento)}\n" +
            $"Origem: {(string.IsNullOrWhiteSpace(conta.Origem) ? "-" : conta.Origem)}\n" +
            $"Referencia: {(string.IsNullOrWhiteSpace(conta.ReferenciaExterna) ? "-" : conta.ReferenciaExterna)}\n" +
            $"Observacoes: {(string.IsNullOrWhiteSpace(conta.Observacoes) ? "-" : conta.Observacoes)}";

        private static bool ContemTexto(string? origem, string termo) =>
            !string.IsNullOrWhiteSpace(origem) &&
            origem.Contains(termo, StringComparison.OrdinalIgnoreCase);

        public (DateTime inicio, DateTime fim) ObterPeriodoFinanceiroAtual()
        {
            var hoje = DateTime.Today;
            var inicio = new DateTime(hoje.Year, hoje.Month, 1);
            var fim = inicio.AddMonths(1).AddDays(-1);
            return (inicio, fim);
        }

        public Dictionary<string, double> ObterResumoFormasPagamentoAtual()
        {
            return FormasPagamentoGrafico.ToDictionary(
                item => item.Nome,
                item => Convert.ToDouble(item.Valor));
        }

        private string ObterQuantidadeDespesa(string termo)
        {
            var item = DespesasPorTipo.FirstOrDefault(despesa =>
                despesa.Tipo.Contains(termo, StringComparison.OrdinalIgnoreCase));

            return item == null
                ? "0 lancamento(s)"
                : $"{item.Quantidade} lancamento(s)";
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        private static bool TipoMovimentoEh(string? tipo, string esperado)
        {
            return UiTextSanitizer.EqualsNormalized(tipo, esperado);
        }

        private static decimal ObterTotalForma(IReadOnlyDictionary<string, decimal> formasPagamento, params string[] aliases)
        {
            return formasPagamento
                .Where(forma => aliases.Any(alias => UiTextSanitizer.EqualsNormalized(forma.Key, alias)))
                .Sum(forma => forma.Value);
        }

        private static bool ContaPagarEstaLiquidada(string? status)
        {
            return UiTextSanitizer.EqualsNormalized(status, "Paga");
        }

        private static bool ContaReceberEstaLiquidada(string? status)
        {
            return UiTextSanitizer.EqualsNormalized(status, "Pago") ||
                   UiTextSanitizer.EqualsNormalized(status, "Recebida");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}


