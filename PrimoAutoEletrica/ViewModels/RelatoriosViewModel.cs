using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using PrimoAutoEletrica.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PrimoAutoEletrica.ViewModels
{
    public class RelatoriosViewModel : INotifyPropertyChanged
    {
        private readonly RelatorioDatabaseService _relatorioDatabaseService;
        private readonly DatabaseService _databaseService;
        private readonly WorkspacePreferenceService _workspacePreferenceService;
        private const int DataPageSize = 250;
        private const int AuditPageSize = 100;
        private DispatcherTimer? _timer;
        private bool _dadosCarregados;
        private bool _isLoading;
        private bool _hasLoadError;
        private string _loadingMessage = "";
        private string _loadErrorMessage = "";
        private string _resumoAuditoria = "";
        private string _resumoConsistenciaOperacional = "";
        private string _categoriaAuditoriaSelecionada = "Todas";
        private string _severidadeAuditoriaSelecionada = "Todas";
        private string _statusAuditoriaSelecionado = "Todos";
        private string _usuarioAuditoriaFiltro = "";
        private string _termoAuditoriaFiltro = "";
        private int _paginaAuditoriaAtual = 1;
        private int _totalAuditoria;
        private int _totalPaginasAuditoria;

        // Header Executivo
        private string _usuarioLogado = "Administrador";
        private DateTime _dataAtual = DateTime.Now;
        private string _horaAtual = DateTime.Now.ToString("HH:mm:ss");
        private string _periodoSelecionado = "Mês Atual";
        private string _empresaAtual = "Primo Auto Elétrica";
        private string _statusSistema = "Online";
        private decimal _resumoFaturamento = 0;
        private bool _relatorioFavorito;
        private bool _modoExecutivoAtivo;

        // Dashboard Cards
        private decimal _faturamentoTotal = 0;
        private decimal _lucroLiquido = 0;
        private decimal _vendasHoje = 0;
        private decimal _ticketMedio = 0;
        private int _totalClientes = 0;
        private int _produtosVendidos = 0;
        private decimal _fluxoCaixa = 0;
        private decimal _contasRecebidas = 0;
        private decimal _contasPendentes = 0;
        private decimal _inadimplencia = 0;
        private decimal _margemLucro = 0;
        private decimal _metaMensal = 0;
        private decimal _conversaoOrcamentos = 0;
        private decimal _crescimentoMensal = 0;
        private string _operadorDestaque = "";
        private string _clienteDestaque = "";
        private decimal _caixaAtual = 0;
        private int _vendasCanceladas = 0;
        private int _produtosSemGiro = 0;
        private int _produtosCurvaA = 0;
        private int _produtosCurvaB = 0;
        private int _produtosCurvaC = 0;
        private string _resumoCurvaAbc = "Curva ABC sem dados carregados.";
        private string _rankingProdutosParados = "Ranking de giro ainda nao carregado.";
        private string _resumoMargemProdutos = "Margem por produto ainda nao carregada.";
        private string _melhorHorarioVendas = "Sem vendas no periodo selecionado.";
        private string _melhorDiaVendas = "Sem vendas no periodo selecionado.";
        private string _resumoInadimplenciaDetalhada = "Inadimplencia ainda nao carregada.";
        private string _resumoDreOperacional = "DRE operacional ainda nao carregado.";
        private string _resumoConciliacaoFinanceira = "Conciliacao financeira ainda nao carregada.";
        private string _resumoOrdensServico = "Relatorios de OS ainda nao carregados.";
        private string _resumoServicosOperacionais = "Relatorios de servicos ainda nao carregados.";
        private string _ultimoCaminhoExportacao = "";
        private string _resumoUltimaExportacao = "Nenhuma exportacao gerada nesta sessao.";
        private DemonstrativoResultadoFinanceiro _demonstrativoResultado = new();

        // Filtros
        private DateTime _dataInicio = DateTime.Now.AddDays(-30);
        private DateTime _dataFim = DateTime.Now;
        private string _filtroOperador = "";
        private string _filtroVendedor = "";
        private string _filtroCliente = "";
        private string _filtroCategoria = "";
        private string _filtroMarca = "";
        private string _filtroFormaPagamento = "";
        private string _filtroStatus = "";

        // Coleções
        public ObservableCollection<DadoFinanceiro> DadosFinanceiros { get; set; }
        public ObservableCollection<DadoVenda> DadosVendas { get; set; }
        public ObservableCollection<DadoMargemProduto> MargemPorProduto { get; set; }
        public ObservableCollection<DadoVendaPeriodo> VendasPorHora { get; set; }
        public ObservableCollection<DadoVendaPeriodo> VendasPorDia { get; set; }
        public ObservableCollection<DadoInadimplencia> InadimplenciaDetalhada { get; set; }
        public ObservableCollection<DadoConciliacaoFinanceira> ConciliacaoFinanceira { get; set; }
        public ObservableCollection<DadoOrdemServicoRelatorio> OrdensServicoAbertas { get; set; }
        public ObservableCollection<DadoOrdemServicoRelatorio> OrdensServicoFinalizadas { get; set; }
        public ObservableCollection<DadoOrdemServicoTecnico> OrdensServicoPorTecnico { get; set; }
        public ObservableCollection<DadoServicoRelatorio> ServicosMaisRealizados { get; set; }
        public ObservableCollection<DadoServicoRelatorio> LucroPorServico { get; set; }
        public ObservableCollection<DadoEstoque> DadosEstoque { get; set; }
        public ObservableCollection<DadoCliente> DadosClientes { get; set; }
        public ObservableCollection<DadoOrcamento> DadosOrcamentos { get; set; }
        public ObservableCollection<DadoCaixa> DadosCaixa { get; set; }
        public ObservableCollection<DadoMeta> Metas { get; set; }
        public ObservableCollection<DadoAlerta> Alertas { get; set; }
        public ObservableCollection<DadoAuditoria> Auditoria { get; set; }
        public ObservableCollection<DadoTimeline> Timeline { get; set; }
        public ObservableCollection<DadoComparativo> Comparativos { get; set; }
        public ObservableCollection<string> CategoriasAuditoriaDisponiveis { get; set; }

        // Relatórios Personalizáveis
        public ObservableCollection<string> ColunasDisponiveis { get; set; }
        public ObservableCollection<string> ColunasSelecionadas { get; set; }
        public ObservableCollection<string> RelatoriosSalvos { get; set; }

        // Dashboard Cards
        public ObservableCollection<DashboardCard> DashboardCards { get; set; }

        public RelatoriosViewModel()
        {
            _relatorioDatabaseService = new RelatorioDatabaseService();
            _databaseService = global::PrimoAutoEletrica.App.Database;
            _workspacePreferenceService = new WorkspacePreferenceService();
            UsuarioLogado = global::PrimoAutoEletrica.App.Session.UserName;

            DadosFinanceiros = new ObservableCollection<DadoFinanceiro>();
            DadosVendas = new ObservableCollection<DadoVenda>();
            MargemPorProduto = new ObservableCollection<DadoMargemProduto>();
            VendasPorHora = new ObservableCollection<DadoVendaPeriodo>();
            VendasPorDia = new ObservableCollection<DadoVendaPeriodo>();
            InadimplenciaDetalhada = new ObservableCollection<DadoInadimplencia>();
            ConciliacaoFinanceira = new ObservableCollection<DadoConciliacaoFinanceira>();
            OrdensServicoAbertas = new ObservableCollection<DadoOrdemServicoRelatorio>();
            OrdensServicoFinalizadas = new ObservableCollection<DadoOrdemServicoRelatorio>();
            OrdensServicoPorTecnico = new ObservableCollection<DadoOrdemServicoTecnico>();
            ServicosMaisRealizados = new ObservableCollection<DadoServicoRelatorio>();
            LucroPorServico = new ObservableCollection<DadoServicoRelatorio>();
            DadosEstoque = new ObservableCollection<DadoEstoque>();
            DadosClientes = new ObservableCollection<DadoCliente>();
            DadosOrcamentos = new ObservableCollection<DadoOrcamento>();
            DadosCaixa = new ObservableCollection<DadoCaixa>();
            Metas = new ObservableCollection<DadoMeta>();
            Alertas = new ObservableCollection<DadoAlerta>();
            Auditoria = new ObservableCollection<DadoAuditoria>();
            Timeline = new ObservableCollection<DadoTimeline>();
            Comparativos = new ObservableCollection<DadoComparativo>();
            DashboardCards = new ObservableCollection<DashboardCard>();
            CategoriasAuditoriaDisponiveis = new ObservableCollection<string>();

            // Relatórios Personalizáveis
            ColunasDisponiveis = new ObservableCollection<string>
            {
                "Data", "Tipo", "Categoria", "Descrição", "Valor", "Forma Pagamento", 
                "Usuário", "Cliente", "Vendedor", "Produto", "Status", "Lucro"
            };
            ColunasSelecionadas = new ObservableCollection<string>();
            RelatoriosSalvos = new ObservableCollection<string>();

            CarregarPreferenciasWorkspace();
            ConfigurarTimer();
        }

        private void ConfigurarTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(60); // Increased from 1s to 60s for performance
            _timer.Tick += (s, args) =>
            {
                HoraAtual = DateTime.Now.ToString("HH:mm:ss");
                DataAtual = DateTime.Now;
            };
            _timer.Start();
        }

        public void CarregarDados()
        {
            CarregarDadosAsync().GetAwaiter().GetResult();
        }

        public async Task CarregarDadosAsync(bool recarregarAuditoria = true)
        {
            if (IsLoading)
            {
                return;
            }

            var inicioCarga = DateTime.Now;
            IsLoading = true;
            LoadingMessage = _dadosCarregados
                ? "Atualizando central de inteligencia..."
                : "Carregando central de inteligencia...";
            StatusSistema = "Carregando";

            try
            {
                var snapshot = await Task.Run(CriarSnapshot);
                AplicarSnapshot(snapshot);
                await CarregarAuditoriaAsync(redefinirPagina: !_dadosCarregados || recarregarAuditoria, usarIndicadorVisual: false);

                HasLoadError = false;
                LoadErrorMessage = string.Empty;
                _dadosCarregados = true;
                OnPropertyChanged(nameof(DadosCarregados));
                NotificarEstadoConteudo();
                var duracao = DateTime.Now - inicioCarga;
                StatusSistema = $"Online ({duracao.TotalSeconds:F1}s)";
                global::PrimoAutoEletrica.App.Logger.LogInfo(
                    $"Workspace de relatorios carregado com {DadosVendas.Count} vendas, {DadosClientes.Count} clientes e {Auditoria.Count} eventos de auditoria exibidos.",
                    "Relatorios");
            }
            catch (Exception ex)
            {
                HasLoadError = true;
                LoadErrorMessage = ex.Message;
                StatusSistema = "Falha na carga";
                NotificarEstadoConteudo();
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao carregar a central de relatorios.", ex, "Relatorios");
                throw;
            }
            finally
            {
                LoadingMessage = string.Empty;
                IsLoading = false;
                NotificarEstadoConteudo();
            }
        }

        public async Task AplicarFiltrosAsync()
        {
            AtualizarPeriodoSelecionado();
            PaginaAuditoriaAtual = 1;
            await CarregarDadosAsync();
        }

        public async Task LimparFiltrosAsync()
        {
            DataInicio = DateTime.Now.AddDays(-30);
            DataFim = DateTime.Now;
            FiltroOperador = "";
            FiltroVendedor = "";
            FiltroCliente = "";
            FiltroCategoria = "";
            FiltroMarca = "";
            FiltroFormaPagamento = "";
            FiltroStatus = "";
            CategoriaAuditoriaSelecionada = "Todas";
            SeveridadeAuditoriaSelecionada = "Todas";
            StatusAuditoriaSelecionado = "Todos";
            UsuarioAuditoriaFiltro = "";
            TermoAuditoriaFiltro = "";
            PaginaAuditoriaAtual = 1;
            await AplicarFiltrosAsync();
        }

        public async Task DefinirPeriodoRapidoAsync(string preset)
        {
            var hoje = DateTime.Today;
            switch ((preset ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "hoje":
                    DataInicio = hoje;
                    DataFim = hoje;
                    break;
                case "ontem":
                    DataInicio = hoje.AddDays(-1);
                    DataFim = hoje.AddDays(-1);
                    break;
                case "7dias":
                    DataInicio = hoje.AddDays(-6);
                    DataFim = hoje;
                    break;
                case "30dias":
                    DataInicio = hoje.AddDays(-29);
                    DataFim = hoje;
                    break;
                case "mesatual":
                    DataInicio = new DateTime(hoje.Year, hoje.Month, 1);
                    DataFim = hoje;
                    break;
                case "mesanterior":
                    var inicioMesAtual = new DateTime(hoje.Year, hoje.Month, 1);
                    DataInicio = inicioMesAtual.AddMonths(-1);
                    DataFim = inicioMesAtual.AddDays(-1);
                    break;
                default:
                    return;
            }

            await AplicarFiltrosAsync();
        }

        public async Task AplicarFiltrosAuditoriaAsync(bool redefinirPagina = true)
        {
            await CarregarAuditoriaAsync(redefinirPagina, usarIndicadorVisual: true);
        }

        public async Task AvancarPaginaAuditoriaAsync()
        {
            if (!TemProximaPaginaAuditoria)
            {
                return;
            }

            PaginaAuditoriaAtual++;
            await CarregarAuditoriaAsync(redefinirPagina: false, usarIndicadorVisual: true);
        }

        public async Task RetrocederPaginaAuditoriaAsync()
        {
            if (!TemPaginaAnteriorAuditoria)
            {
                return;
            }

            PaginaAuditoriaAtual--;
            await CarregarAuditoriaAsync(redefinirPagina: false, usarIndicadorVisual: true);
        }

        private void CarregarDadosFinanceiros()
        {
            DadosFinanceiros.Clear();
            var dados = _relatorioDatabaseService.ObterDadosFinanceiros(DataInicio, DataFim);
            
            // Paginação para performance - carregar apenas os primeiros 1000 registros
            foreach (var dado in dados.Take(1000))
                DadosFinanceiros.Add(dado);

            FaturamentoTotal = _relatorioDatabaseService.ObterFaturamentoTotal(DataInicio, DataFim);
            LucroLiquido = _relatorioDatabaseService.ObterLucroLiquido(DataInicio, DataFim);
            FluxoCaixa = FaturamentoTotal - DadosFinanceiros.Where(d => d.Tipo == "Despesa").Sum(d => d.Valor);
        }

        private void CarregarDadosVendas()
        {
            DadosVendas.Clear();
            var dados = _relatorioDatabaseService.ObterDadosVendas(DataInicio, DataFim);
            
            // Paginação para performance - carregar apenas os primeiros 1000 registros
            foreach (var dado in dados.Take(1000))
                DadosVendas.Add(dado);

            VendasHoje = DadosVendas.Where(v => v.Data.Date == DateTime.Now.Date).Sum(v => v.ValorTotal);
            TicketMedio = _relatorioDatabaseService.ObterTicketMedio(DataInicio, DataFim);
            ProdutosVendidos = DadosVendas.Sum(v => v.ItensQuantidade);
            VendasCanceladas = DadosVendas.Count(v => v.Status == "Cancelada");
            
            if (DadosVendas.Any())
            {
                OperadorDestaque = DadosVendas.GroupBy(v => v.VendedorNome)
                    .OrderByDescending(g => g.Sum(v => v.ValorTotal))
                    .First().Key;
                
                ClienteDestaque = DadosVendas.GroupBy(v => v.ClienteNome)
                    .OrderByDescending(g => g.Sum(v => v.ValorTotal))
                    .First().Key;
            }
        }

        private void CarregarDadosEstoque()
        {
            DadosEstoque.Clear();
            var dados = _relatorioDatabaseService.ObterDadosEstoque();
            
            // Paginação para performance - carregar apenas os primeiros 1000 registros
            foreach (var dado in dados.Take(1000))
                DadosEstoque.Add(dado);

            ProdutosSemGiro = dados.Count(p => p.GiroMensal == 0);
        }

        private void CarregarDadosClientes()
        {
            DadosClientes.Clear();
            var dados = _relatorioDatabaseService.ObterDadosClientes();
            
            // Paginação para performance - carregar apenas os primeiros 1000 registros
            foreach (var dado in dados.Take(1000))
                DadosClientes.Add(dado);

            TotalClientes = dados.Count;
            Inadimplencia = dados.Where(c => c.Status == "Inadimplente").Sum(c => c.Inadimplencia);
        }

        private void CarregarDadosOrcamentos()
        {
            DadosOrcamentos.Clear();
            var dados = _relatorioDatabaseService.ObterDadosOrcamentos(DataInicio, DataFim);
            
            // Paginação para performance - carregar apenas os primeiros 1000 registros
            foreach (var dado in dados.Take(1000))
                DadosOrcamentos.Add(dado);

            ConversaoOrcamentos = DadosOrcamentos.Any() ? (decimal)(DadosOrcamentos.Count(o => o.Status == "Aprovado") * 100.0 / DadosOrcamentos.Count) : 0;
        }

        private void CarregarDadosCaixa()
        {
            DadosCaixa.Clear();
            var dados = _relatorioDatabaseService.ObterDadosCaixa(DataInicio, DataFim);
            
            // Paginação para performance - carregar apenas os primeiros 1000 registros
            foreach (var dado in dados.Take(1000))
                DadosCaixa.Add(dado);

            CaixaAtual = dados
                .OrderByDescending(c => c.Data)
                .Select(c => c.ValorFinal)
                .FirstOrDefault();
        }

        private void CarregarMetas()
        {
            Metas.Clear();
            var dados = _relatorioDatabaseService.ObterMetas();
            foreach (var dado in dados)
                Metas.Add(dado);

            var metaVendas = dados.FirstOrDefault(m => m.Tipo == "Vendas" && m.Periodo == "Mensal");
            if (metaVendas != null)
                MetaMensal = metaVendas.MetaValor;
        }

        private void CarregarAlertas()
        {
            Alertas.Clear();
            var dados = _relatorioDatabaseService.ObterAlertas();
            foreach (var dado in dados)
                Alertas.Add(dado);
        }

        private void CarregarAuditoria()
        {
            Auditoria.Clear();
            var dados = _relatorioDatabaseService.ObterAuditoria(DataInicio, DataFim);
            foreach (var dado in dados)
                Auditoria.Add(dado);
        }

        private void CarregarTimeline()
        {
            Timeline.Clear();
            var dados = _relatorioDatabaseService.ObterTimeline(DataInicio, DataFim);
            foreach (var dado in dados)
                Timeline.Add(dado);
        }

        private void CarregarComparativos()
        {
            Comparativos.Clear();
            var comparativo = _relatorioDatabaseService.ObterComparativoMensal(DateTime.Now.Year, DateTime.Now.Month);
            Comparativos.Add(comparativo);
            CrescimentoMensal = comparativo.PercentualVariacao;
        }

        private RelatorioSnapshot CriarSnapshot()
        {
            var dadosFinanceiros = _relatorioDatabaseService.ObterDadosFinanceiros(
                DataInicio,
                DataFim,
                FiltroCategoria,
                FiltroFormaPagamento,
                DataPageSize);
            var dadosVendas = _relatorioDatabaseService.ObterDadosVendas(
                DataInicio,
                DataFim,
                FiltroVendedor,
                FiltroCliente,
                FiltroFormaPagamento,
                FiltroStatus,
                DataPageSize);
            var margemPorProduto = _relatorioDatabaseService.ObterMargemPorProduto(
                DataInicio,
                DataFim,
                FiltroCategoria,
                100);
            var vendasPorHora = _relatorioDatabaseService.ObterVendasPorHora(DataInicio, DataFim);
            var vendasPorDia = _relatorioDatabaseService.ObterVendasPorDia(DataInicio, DataFim);
            var inadimplenciaDetalhada = _relatorioDatabaseService.ObterInadimplenciaDetalhada(
                DateTime.Today,
                FiltroCliente,
                100);
            var conciliacaoFinanceira = _relatorioDatabaseService.ObterConciliacaoFinanceira(DataInicio, DataFim);
            var ordensServicoAbertas = _relatorioDatabaseService.ObterOrdensServicoRelatorio(DataInicio, DataFim, finalizadas: false, 100);
            var ordensServicoFinalizadas = _relatorioDatabaseService.ObterOrdensServicoRelatorio(DataInicio, DataFim, finalizadas: true, 100);
            var ordensServicoPorTecnico = _relatorioDatabaseService.ObterOrdensServicoPorTecnico(DataInicio, DataFim, 100);
            var servicosRelatorio = _relatorioDatabaseService.ObterServicosRelatorio(DataInicio, DataFim, 100);
            var servicosMaisRealizados = servicosRelatorio
                .OrderByDescending(item => item.Quantidade)
                .ThenByDescending(item => item.ReceitaTotal)
                .ThenBy(item => item.Servico, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var lucroPorServico = servicosRelatorio
                .OrderByDescending(item => item.LucroBruto)
                .ThenByDescending(item => item.ReceitaTotal)
                .ThenBy(item => item.Servico, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var dadosEstoque = _relatorioDatabaseService.ObterDadosEstoque(FiltroCategoria, FiltroMarca, DataPageSize);
            var dadosClientes = _relatorioDatabaseService.ObterDadosClientes(FiltroCliente, DataPageSize);
            var dadosOrcamentos = _relatorioDatabaseService.ObterDadosOrcamentos(
                DataInicio,
                DataFim,
                FiltroCliente,
                FiltroStatus,
                DataPageSize);
            var dadosCaixa = _relatorioDatabaseService.ObterDadosCaixa(DataInicio, DataFim, DataPageSize);
            var metas = _relatorioDatabaseService.ObterMetas();
            var alertas = _relatorioDatabaseService.ObterAlertas();
            var timeline = _relatorioDatabaseService.ObterTimeline(DataInicio, DataFim);
            var comparativo = _relatorioDatabaseService.ObterComparativoMensal(DateTime.Now.Year, DateTime.Now.Month);
            var demonstrativoResultado = _relatorioDatabaseService.ObterDemonstrativoResultado(DataInicio, DataFim);

            return new RelatorioSnapshot
            {
                DadosFinanceiros = dadosFinanceiros,
                DadosVendas = dadosVendas,
                MargemPorProduto = margemPorProduto,
                VendasPorHora = vendasPorHora,
                VendasPorDia = vendasPorDia,
                InadimplenciaDetalhada = inadimplenciaDetalhada,
                ConciliacaoFinanceira = conciliacaoFinanceira,
                OrdensServicoAbertas = ordensServicoAbertas,
                OrdensServicoFinalizadas = ordensServicoFinalizadas,
                OrdensServicoPorTecnico = ordensServicoPorTecnico,
                ServicosMaisRealizados = servicosMaisRealizados,
                LucroPorServico = lucroPorServico,
                DadosEstoque = dadosEstoque,
                DadosClientes = dadosClientes,
                DadosOrcamentos = dadosOrcamentos,
                DadosCaixa = dadosCaixa,
                Metas = metas,
                Alertas = alertas,
                Timeline = timeline,
                Comparativo = comparativo,
                DemonstrativoResultado = demonstrativoResultado,
                FaturamentoTotal = _relatorioDatabaseService.ObterFaturamentoTotal(DataInicio, DataFim),
                LucroLiquido = _relatorioDatabaseService.ObterLucroLiquido(DataInicio, DataFim),
                TicketMedio = _relatorioDatabaseService.ObterTicketMedio(DataInicio, DataFim),
                TotalClientes = _relatorioDatabaseService.ObterTotalClientes(FiltroCliente),
                ProdutosSemGiro = _relatorioDatabaseService.ObterProdutosSemGiro(FiltroCategoria, FiltroMarca),
                ConversaoOrcamentos = _relatorioDatabaseService.ObterConversaoOrcamentos(DataInicio, DataFim),
                ResumoConsistenciaOperacional = _relatorioDatabaseService.ObterResumoConsistenciaOperacional(DataInicio, DataFim)
            };
        }

        private void AplicarSnapshot(RelatorioSnapshot snapshot)
        {
            ReplaceCollection(DadosFinanceiros, snapshot.DadosFinanceiros);
            ReplaceCollection(DadosVendas, snapshot.DadosVendas);
            ReplaceCollection(MargemPorProduto, snapshot.MargemPorProduto);
            ReplaceCollection(VendasPorHora, snapshot.VendasPorHora);
            ReplaceCollection(VendasPorDia, snapshot.VendasPorDia);
            ReplaceCollection(InadimplenciaDetalhada, snapshot.InadimplenciaDetalhada);
            ReplaceCollection(ConciliacaoFinanceira, snapshot.ConciliacaoFinanceira);
            ReplaceCollection(OrdensServicoAbertas, snapshot.OrdensServicoAbertas);
            ReplaceCollection(OrdensServicoFinalizadas, snapshot.OrdensServicoFinalizadas);
            ReplaceCollection(OrdensServicoPorTecnico, snapshot.OrdensServicoPorTecnico);
            ReplaceCollection(ServicosMaisRealizados, snapshot.ServicosMaisRealizados);
            ReplaceCollection(LucroPorServico, snapshot.LucroPorServico);
            ReplaceCollection(DadosEstoque, snapshot.DadosEstoque);
            ReplaceCollection(DadosClientes, snapshot.DadosClientes);
            ReplaceCollection(DadosOrcamentos, snapshot.DadosOrcamentos);
            ReplaceCollection(DadosCaixa, snapshot.DadosCaixa);
            ReplaceCollection(Metas, snapshot.Metas);
            ReplaceCollection(Alertas, snapshot.Alertas);
            ReplaceCollection(Timeline, snapshot.Timeline);

            Comparativos.Clear();
            Comparativos.Add(snapshot.Comparativo);

            DemonstrativoResultado = snapshot.DemonstrativoResultado;
            ResumoDreOperacional = CriarResumoDreOperacional(snapshot.DemonstrativoResultado);
            FaturamentoTotal = snapshot.FaturamentoTotal;
            LucroLiquido = snapshot.LucroLiquido;
            TicketMedio = snapshot.TicketMedio;
            TotalClientes = snapshot.TotalClientes;
            ProdutosSemGiro = snapshot.ProdutosSemGiro;
            ProdutosCurvaA = snapshot.DadosEstoque.Count(item => string.Equals(item.CurvaAbc, "A", StringComparison.OrdinalIgnoreCase));
            ProdutosCurvaB = snapshot.DadosEstoque.Count(item => string.Equals(item.CurvaAbc, "B", StringComparison.OrdinalIgnoreCase));
            ProdutosCurvaC = snapshot.DadosEstoque.Count(item => string.Equals(item.CurvaAbc, "C", StringComparison.OrdinalIgnoreCase));
            ResumoCurvaAbc = CriarResumoCurvaAbc(snapshot.DadosEstoque);
            RankingProdutosParados = CriarRankingProdutosParados(snapshot.DadosEstoque);
            ResumoMargemProdutos = CriarResumoMargemProdutos(snapshot.MargemPorProduto);
            MelhorHorarioVendas = CriarMelhorHorarioVendas(snapshot.VendasPorHora);
            MelhorDiaVendas = CriarMelhorDiaVendas(snapshot.VendasPorDia);
            ResumoInadimplenciaDetalhada = CriarResumoInadimplenciaDetalhada(snapshot.InadimplenciaDetalhada);
            ResumoConciliacaoFinanceira = CriarResumoConciliacaoFinanceira(snapshot.ConciliacaoFinanceira);
            ResumoOrdensServico = CriarResumoOrdensServico(snapshot.OrdensServicoAbertas, snapshot.OrdensServicoFinalizadas, snapshot.OrdensServicoPorTecnico);
            ResumoServicosOperacionais = CriarResumoServicosOperacionais(snapshot.ServicosMaisRealizados, snapshot.LucroPorServico);
            MargemLucro = CalcularMargemGeral(snapshot.MargemPorProduto);
            ConversaoOrcamentos = snapshot.ConversaoOrcamentos;
            ResumoConsistenciaOperacional = snapshot.ResumoConsistenciaOperacional;
            CrescimentoMensal = snapshot.Comparativo.PercentualVariacao;
            ResumoFaturamento = snapshot.FaturamentoTotal;
            FluxoCaixa = snapshot.FaturamentoTotal - snapshot.DadosFinanceiros
                .Where(dado => string.Equals(dado.Tipo, "Despesa", StringComparison.OrdinalIgnoreCase))
                .Sum(dado => dado.Valor);
            VendasHoje = snapshot.DadosVendas
                .Where(venda => venda.Data.Date == DateTime.Now.Date)
                .Sum(venda => venda.ValorTotal);
            ProdutosVendidos = snapshot.DadosVendas.Sum(venda => venda.ItensQuantidade);
            VendasCanceladas = snapshot.DadosVendas.Count(venda => string.Equals(venda.Status, "Cancelada", StringComparison.OrdinalIgnoreCase));
            OperadorDestaque = snapshot.DadosVendas.Any()
                ? snapshot.DadosVendas
                    .GroupBy(venda => venda.VendedorNome)
                    .OrderByDescending(grupo => grupo.Sum(venda => venda.ValorTotal))
                    .First().Key
                : string.Empty;
            ClienteDestaque = snapshot.DadosVendas.Any()
                ? snapshot.DadosVendas
                    .GroupBy(venda => venda.ClienteNome)
                    .OrderByDescending(grupo => grupo.Sum(venda => venda.ValorTotal))
                    .First().Key
                : string.Empty;
            Inadimplencia = snapshot.InadimplenciaDetalhada.Sum(item => item.Valor);
            CaixaAtual = snapshot.DadosCaixa
                .OrderByDescending(caixa => caixa.Data)
                .Select(caixa => caixa.ValorFinal)
                .FirstOrDefault();
            ContasRecebidas = snapshot.DemonstrativoResultado.ReceitasConfirmadas;
            ContasPendentes = snapshot.DemonstrativoResultado.ContasReceberPendentes;

            var metaVendas = snapshot.Metas.FirstOrDefault(meta =>
                string.Equals(meta.Tipo, "Vendas", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(meta.Periodo, "Mensal", StringComparison.OrdinalIgnoreCase));
            MetaMensal = metaVendas?.MetaValor ?? 0;

            CalcularDashboardCards();
            AtualizarPulseOperacional();
            NotificarEstadoConteudo();
        }

        private void AtualizarPulseOperacional()
        {
            PulseFaturamentoText = FaturamentoTotal.ToString("C2");
            PulseTicketMedioText = TicketMedio.ToString("C2");
            PulseOsAbertasText = OrdensServicoAbertas.Count.ToString();
            PulseOsFinalizadasText = OrdensServicoFinalizadas.Count.ToString();
            PulseClientesText = TotalClientes.ToString();
            OnPropertyChanged(nameof(PulseFaturamentoText));
            OnPropertyChanged(nameof(PulseTicketMedioText));
            OnPropertyChanged(nameof(PulseOsAbertasText));
            OnPropertyChanged(nameof(PulseOsFinalizadasText));
            OnPropertyChanged(nameof(PulseClientesText));
        }

        private void NotificarEstadoConteudo()
        {
            OnPropertyChanged(nameof(TemDadosRelatorio));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowContentState));
        }

        private static string CriarResumoCurvaAbc(IReadOnlyCollection<DadoEstoque> dadosEstoque)
        {
            if (dadosEstoque.Count == 0)
            {
                return "Nenhum produto encontrado para montar a curva ABC.";
            }

            var curvaA = dadosEstoque
                .Where(item => string.Equals(item.CurvaAbc, "A", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.ValorTotal)
                .ToList();

            if (curvaA.Count == 0)
            {
                return "Produtos sem valor de estoque suficiente para classificar a curva ABC.";
            }

            var valorA = curvaA.Sum(item => item.ValorTotal);
            var valorTotal = dadosEstoque.Sum(item => item.ValorTotal);
            var participacao = valorTotal <= 0 ? 0 : valorA / valorTotal * 100m;
            var principais = curvaA
                .Take(3)
                .Select(item => $"{item.ProdutoNome} ({item.ParticipacaoEstoquePercentual:N1}%)");

            return $"Curva A concentra {participacao:N1}% do valor analisado. Principais itens: {string.Join(", ", principais)}.";
        }

        private static string CriarRankingProdutosParados(IEnumerable<DadoEstoque> dadosEstoque)
        {
            var parados = dadosEstoque
                .Where(item => item.QuantidadeAtual > 0 && item.DiasSemMovimentacao >= 90)
                .OrderByDescending(item => item.DiasSemMovimentacao)
                .ThenByDescending(item => item.ValorTotal)
                .Take(5)
                .Select(item => $"{item.ProdutoNome} ({item.DiasSemMovimentacao} dias)")
                .ToList();

            return parados.Count == 0
                ? UiText.T("NoIdleProducts90Days")
                : $"Produtos parados em destaque: {string.Join(", ", parados)}.";
        }

        private static string CriarResumoMargemProdutos(IReadOnlyCollection<DadoMargemProduto> dados)
        {
            if (dados.Count == 0)
            {
                return "Nenhuma venda com itens encontrada no periodo selecionado.";
            }

            var receita = dados.Sum(item => item.ReceitaTotal);
            var lucro = dados.Sum(item => item.LucroBruto);
            var margem = receita > 0 ? lucro / receita * 100m : 0m;
            var destaque = dados
                .OrderByDescending(item => item.LucroBruto)
                .FirstOrDefault();

            return destaque is null
                ? $"Margem bruta do periodo: {margem:N1}%."
                : $"Margem bruta {margem:N1}% no periodo. Maior lucro: {destaque.ProdutoNome} ({destaque.LucroBruto:C}).";
        }

        private static string CriarMelhorHorarioVendas(IReadOnlyCollection<DadoVendaPeriodo> dados)
        {
            var melhor = dados
                .OrderByDescending(item => item.Faturamento)
                .ThenByDescending(item => item.QuantidadeVendas)
                .FirstOrDefault();

            return melhor is null
                ? "Sem vendas agrupadas por horario no periodo."
                : $"{melhor.Periodo}: {melhor.Faturamento:C} em {melhor.QuantidadeVendas} venda(s).";
        }

        private static string CriarMelhorDiaVendas(IReadOnlyCollection<DadoVendaPeriodo> dados)
        {
            var melhor = dados
                .OrderByDescending(item => item.Faturamento)
                .ThenByDescending(item => item.QuantidadeVendas)
                .FirstOrDefault();

            return melhor is null
                ? "Sem vendas agrupadas por dia no periodo."
                : $"{melhor.Periodo}: {melhor.Faturamento:C} em {melhor.QuantidadeVendas} venda(s).";
        }

        private static string CriarResumoInadimplenciaDetalhada(IReadOnlyCollection<DadoInadimplencia> dados)
        {
            if (dados.Count == 0)
            {
                return UiText.T("NoOverdueTitlesOpen");
            }

            var total = dados.Sum(item => item.Valor);
            var clientes = dados
                .Select(item => item.Cliente)
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            var maiorAtraso = dados.OrderByDescending(item => item.DiasAtraso).First();

            return $"{dados.Count} titulo(s) vencido(s), {clientes} cliente(s), total {total:C}. Maior atraso: {maiorAtraso.Cliente} ({maiorAtraso.DiasAtraso} dias).";
        }

        private static string CriarResumoDreOperacional(DemonstrativoResultadoFinanceiro demonstrativo)
        {
            var margemOperacional = demonstrativo.ReceitasConfirmadas > 0
                ? demonstrativo.ResultadoOperacional / demonstrativo.ReceitasConfirmadas * 100m
                : 0m;

            return $"Resultado operacional {demonstrativo.ResultadoOperacional:C} ({margemOperacional:N1}%). Projetado: {demonstrativo.ResultadoProjetado:C}.";
        }

        private static string CriarResumoConciliacaoFinanceira(IReadOnlyCollection<DadoConciliacaoFinanceira> dados)
        {
            if (dados.Count == 0)
            {
                return "Sem vendas ou entradas financeiras no periodo selecionado.";
            }

            var totalVendas = dados.Sum(item => item.TotalVendas);
            var totalEntradas = dados.Sum(item => item.EntradasFinanceiras);
            var diferenca = totalVendas - totalEntradas;
            var divergentes = dados.Count(item => Math.Abs(item.Diferenca) > 0.01m);

            return divergentes == 0
                ? $"Conciliado por forma de pagamento. Vendas e entradas somam {totalVendas:C}."
                : $"{divergentes} forma(s) com divergencia. Vendas: {totalVendas:C}; entradas: {totalEntradas:C}; diferenca: {diferenca:C}.";
        }

        private static string CriarResumoOrdensServico(
            IReadOnlyCollection<DadoOrdemServicoRelatorio> abertas,
            IReadOnlyCollection<DadoOrdemServicoRelatorio> finalizadas,
            IReadOnlyCollection<DadoOrdemServicoTecnico> porTecnico)
        {
            var valorAberto = abertas.Sum(item => item.ValorTotal);
            var valorFinalizado = finalizadas.Sum(item => item.ValorTotal);
            var tecnicoDestaque = porTecnico
                .OrderByDescending(item => item.OrdensFinalizadas)
                .ThenByDescending(item => item.ValorTotal)
                .FirstOrDefault();

            var destaque = tecnicoDestaque == null
                ? "sem tecnico em destaque"
                : $"{tecnicoDestaque.TecnicoNome} ({tecnicoDestaque.OrdensFinalizadas} finalizada(s))";

            return $"{abertas.Count} OS aberta(s) ({valorAberto:C}) e {finalizadas.Count} OS finalizada(s) ({valorFinalizado:C}) no periodo; destaque tecnico: {destaque}.";
        }

        private static string CriarResumoServicosOperacionais(
            IReadOnlyCollection<DadoServicoRelatorio> maisRealizados,
            IReadOnlyCollection<DadoServicoRelatorio> lucroPorServico)
        {
            if (maisRealizados.Count == 0)
            {
                return "Nenhum servico operacional encontrado no periodo selecionado.";
            }

            var maisExecutado = maisRealizados
                .OrderByDescending(item => item.Quantidade)
                .ThenByDescending(item => item.ReceitaTotal)
                .First();
            var maiorLucro = lucroPorServico
                .OrderByDescending(item => item.LucroBruto)
                .FirstOrDefault() ?? maisExecutado;
            var receita = maisRealizados.Sum(item => item.ReceitaTotal);
            var lucro = maisRealizados.Sum(item => item.LucroBruto);

            return $"{maisExecutado.Servico} lidera com {maisExecutado.Quantidade:N0} execucao(oes). Lucro bruto total {lucro:C} sobre {receita:C}; maior lucro: {maiorLucro.Servico} ({maiorLucro.LucroBruto:C}).";
        }

        private static decimal CalcularMargemGeral(IEnumerable<DadoMargemProduto> dados)
        {
            var receita = dados.Sum(item => item.ReceitaTotal);
            if (receita <= 0)
            {
                return 0m;
            }

            return dados.Sum(item => item.LucroBruto) / receita;
        }

        private async Task CarregarAuditoriaAsync(bool redefinirPagina, bool usarIndicadorVisual)
        {
            if (redefinirPagina)
            {
                PaginaAuditoriaAtual = 1;
            }

            if (usarIndicadorVisual)
            {
                IsLoading = true;
                LoadingMessage = "Consultando auditoria operacional...";
            }

            try
            {
                var filtro = new AuditoriaOperacionalFiltro
                {
                    DataInicio = DataInicio,
                    DataFim = DataFim,
                    Categoria = string.Equals(CategoriaAuditoriaSelecionada, "Todas", StringComparison.OrdinalIgnoreCase) ? string.Empty : CategoriaAuditoriaSelecionada,
                    Severidade = string.Equals(SeveridadeAuditoriaSelecionada, "Todas", StringComparison.OrdinalIgnoreCase) ? string.Empty : SeveridadeAuditoriaSelecionada,
                    Status = string.Equals(StatusAuditoriaSelecionado, "Todos", StringComparison.OrdinalIgnoreCase) ? string.Empty : StatusAuditoriaSelecionado,
                    Usuario = UsuarioAuditoriaFiltro,
                    TermoLivre = TermoAuditoriaFiltro,
                    PaginaAtual = PaginaAuditoriaAtual,
                    ItensPorPagina = AuditPageSize
                };

                var resultado = await Task.Run(() => _relatorioDatabaseService.ObterAuditoriaPaginada(filtro));
                var categorias = await Task.Run(() => _relatorioDatabaseService.ObterCategoriasAuditoriaOperacional(DataInicio, DataFim));

                ReplaceCollection(Auditoria, resultado.Itens);
                AtualizarCategoriasAuditoria(categorias);
                TotalAuditoria = resultado.TotalItens;
                TotalPaginasAuditoria = resultado.TotalPaginas;
                PaginaAuditoriaAtual = resultado.PaginaAtual;
                ResumoAuditoria = resultado.TotalItens == 0
                    ? "Nenhum evento encontrado para os filtros atuais."
                    : $"Mostrando {Auditoria.Count} de {resultado.TotalItens} eventos - pagina {resultado.PaginaAtual}/{Math.Max(1, resultado.TotalPaginas)}.";
            }
            catch (Exception ex)
            {
                ResumoAuditoria = "Falha ao consultar auditoria operacional.";
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao carregar auditoria operacional.", ex, "Auditoria");
                throw;
            }
            finally
            {
                if (usarIndicadorVisual)
                {
                    LoadingMessage = string.Empty;
                    IsLoading = false;
                }
            }
        }

        private void AtualizarCategoriasAuditoria(IEnumerable<string> categorias)
        {
            var categoriaAtual = CategoriaAuditoriaSelecionada;
            CategoriasAuditoriaDisponiveis.Clear();
            CategoriasAuditoriaDisponiveis.Add("Todas");

            foreach (var categoria in categorias.Where(valor => !string.IsNullOrWhiteSpace(valor)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                CategoriasAuditoriaDisponiveis.Add(categoria);
            }

            if (!string.IsNullOrWhiteSpace(categoriaAtual) &&
                CategoriasAuditoriaDisponiveis.Any(valor => string.Equals(valor, categoriaAtual, StringComparison.OrdinalIgnoreCase)))
            {
                CategoriaAuditoriaSelecionada = categoriaAtual;
            }
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        private void CalcularDashboardCards()
        {
            DashboardCards.Clear();

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Faturamento Total",
                Valor = FaturamentoTotal.ToString("C"),
                Comparacao = $"{CrescimentoMensal:F1}%",
                Tendencia = CrescimentoMensal >= 0 ? "Positivo" : "Negativo",
                Cor = "#28A745"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Lucro Líquido",
                Valor = LucroLiquido.ToString("C"),
                Comparacao = $"{(LucroLiquido / (FaturamentoTotal > 0 ? FaturamentoTotal : 1) * 100):F1}%",
                Tendencia = "Positivo",
                Cor = "#007BFF"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Vendas Hoje",
                Valor = VendasHoje.ToString("C"),
                Comparacao = $"{DadosVendas.Count(v => v.Data.Date == DateTime.Now.Date)} vendas",
                Tendencia = "Positivo",
                Cor = "#6F42C1"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Ticket Médio",
                Valor = TicketMedio.ToString("C"),
                Comparacao = "Média por venda",
                Tendencia = "Neutro",
                Cor = "#FD7E14"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Total Clientes",
                Valor = TotalClientes.ToString(),
                Comparacao = $"{DadosClientes.Count(c => c.Status == "VIP")} VIP",
                Tendencia = "Positivo",
                Cor = "#20C997"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Produtos Vendidos",
                Valor = ProdutosVendidos.ToString(),
                Comparacao = "Total itens",
                Tendencia = "Positivo",
                Cor = "#17A2B8"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Fluxo de Caixa",
                Valor = FluxoCaixa.ToString("C"),
                Comparacao = "Entradas - Saídas",
                Tendencia = FluxoCaixa >= 0 ? "Positivo" : "Negativo",
                Cor = FluxoCaixa >= 0 ? "#28A745" : "#DC3545"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Contas Recebidas",
                Valor = ContasRecebidas.ToString("C"),
                Comparacao = "Pagas",
                Tendencia = "Positivo",
                Cor = "#28A745"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Contas Pendentes",
                Valor = ContasPendentes.ToString("C"),
                Comparacao = "A receber",
                Tendencia = "Atenção",
                Cor = "#FFC107"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Inadimplência",
                Valor = Inadimplencia.ToString("C"),
                Comparacao = $"{(Inadimplencia / (FaturamentoTotal > 0 ? FaturamentoTotal : 1) * 100):F1}%",
                Tendencia = "Negativo",
                Cor = "#DC3545"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Margem de Lucro",
                Valor = MargemLucro.ToString("P"),
                Comparacao = "Rentabilidade",
                Tendencia = MargemLucro >= 0.20m ? "Positivo" : "Atenção",
                Cor = MargemLucro >= 0.20m ? "#28A745" : "#FFC107"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Meta Mensal",
                Valor = MetaMensal.ToString("C"),
                Comparacao = $"{(FaturamentoTotal / (MetaMensal > 0 ? MetaMensal : 1) * 100):F1}% atingido",
                Tendencia = "Em andamento",
                Cor = "#6F42C1"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Conversão Orçamentos",
                Valor = ConversaoOrcamentos.ToString("F1") + "%",
                Comparacao = "Aprovados",
                Tendencia = ConversaoOrcamentos >= 50 ? "Positivo" : "Atenção",
                Cor = ConversaoOrcamentos >= 50 ? "#28A745" : "#FFC107"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Crescimento Mensal",
                Valor = CrescimentoMensal.ToString("F1") + "%",
                Comparacao = "vs mês anterior",
                Tendencia = CrescimentoMensal >= 0 ? "Positivo" : "Negativo",
                Cor = CrescimentoMensal >= 0 ? "#28A745" : "#DC3545"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Operador Destaque",
                Valor = OperadorDestaque,
                Comparacao = "Melhor performance",
                Tendencia = "Positivo",
                Cor = "#007BFF"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Cliente Destaque",
                Valor = ClienteDestaque,
                Comparacao = "Maior compra",
                Tendencia = "Positivo",
                Cor = "#FFD700"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Caixa Atual",
                Valor = CaixaAtual.ToString("C"),
                Comparacao = "Disponível",
                Tendencia = "Neutro",
                Cor = "#17A2B8"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Vendas Canceladas",
                Valor = VendasCanceladas.ToString(),
                Comparacao = "Total canceladas",
                Tendencia = "Negativo",
                Cor = "#DC3545"
            });

            DashboardCards.Add(new DashboardCard
            {
                Icone = "*",
                Titulo = "Produtos Sem Giro",
                Valor = ProdutosSemGiro.ToString(),
                Comparacao = "Parados",
                Tendencia = "Atenção",
                Cor = "#FFC107"
            });

            ResumoFaturamento = FaturamentoTotal;
        }

        // Filtros
        public void AplicarFiltros()
        {
            AplicarFiltrosAsync().GetAwaiter().GetResult();
        }

        public void LimparFiltros()
        {
            LimparFiltrosAsync().GetAwaiter().GetResult();
        }

        public void SalvarWorkspaceAtual()
        {
            _workspacePreferenceService.SaveReportPreferences(UsuarioLogado, BuildCurrentWorkspacePreferences());
            StatusSistema = ModoExecutivoAtivo ? "Executivo salvo" : "Workspace salvo";
        }

        public void AlternarFavorito()
        {
            RelatorioFavorito = !RelatorioFavorito;
            SalvarWorkspaceAtual();
        }

        public void AlternarModoExecutivo()
        {
            ModoExecutivoAtivo = !ModoExecutivoAtivo;
            StatusSistema = ModoExecutivoAtivo ? "Modo executivo" : "Online";
            SalvarWorkspaceAtual();
        }

        // Exportação
        public string ExportarPDF(string? caminhoArquivo = null)
        {
            var exportService = new RelatorioExportService();
            var caminho = caminhoArquivo ?? CriarCaminhoExportacao("Relatorio", ".pdf");
            exportService.ExportarParaPDF(
                DadosFinanceiros.ToList(),
                DadosVendas.ToList(),
                caminho,
                OrdensServicoAbertas.ToList(),
                OrdensServicoFinalizadas.ToList(),
                OrdensServicoPorTecnico.ToList(),
                ServicosMaisRealizados.ToList(),
                LucroPorServico.ToList());
            RegistrarExportacao("PDF", caminho);
            return caminho;
        }

        public string ExportarExcel(string? caminhoArquivo = null)
        {
            var exportService = new RelatorioExportService();
            var caminho = caminhoArquivo ?? CriarCaminhoExportacao("Relatorio_Excel", ".csv");
            exportService.ExportarParaExcel(
                DadosFinanceiros.ToList(),
                DadosVendas.ToList(),
                caminho,
                OrdensServicoAbertas.ToList(),
                OrdensServicoFinalizadas.ToList(),
                OrdensServicoPorTecnico.ToList(),
                ServicosMaisRealizados.ToList(),
                LucroPorServico.ToList());
            RegistrarExportacao("Excel/CSV", caminho);
            return caminho;
        }

        public string ExportarCSV(string? caminhoArquivo = null)
        {
            var exportService = new RelatorioExportService();
            var caminho = caminhoArquivo ?? CriarCaminhoExportacao("Relatorio_Financeiro", ".csv");
            exportService.ExportarParaCSV(DadosFinanceiros.ToList(), caminho);
            RegistrarExportacao("CSV financeiro", caminho);
            return caminho;
        }

        public RelatorioPacoteEvidencias ExportarPacoteEvidencias(string? diretorioDestino = null)
        {
            if (!_dadosCarregados)
            {
                CarregarDados();
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            var diretorio = diretorioDestino ?? CriarDiretorioPacoteEvidencias(timestamp);
            Directory.CreateDirectory(diretorio);

            var pdfPath = Path.Combine(diretorio, $"RelatorioExecutivo_{timestamp}.pdf");
            var csvPath = Path.Combine(diretorio, $"RelatorioAnalitico_{timestamp}.csv");
            var manifestoPath = Path.Combine(diretorio, $"ManifestoEvidencias_{timestamp}.txt");

            var exportService = new RelatorioExportService();
            exportService.ExportarParaPDF(
                DadosFinanceiros.ToList(),
                DadosVendas.ToList(),
                pdfPath,
                OrdensServicoAbertas.ToList(),
                OrdensServicoFinalizadas.ToList(),
                OrdensServicoPorTecnico.ToList(),
                ServicosMaisRealizados.ToList(),
                LucroPorServico.ToList());
            exportService.ExportarParaExcel(
                DadosFinanceiros.ToList(),
                DadosVendas.ToList(),
                csvPath,
                OrdensServicoAbertas.ToList(),
                OrdensServicoFinalizadas.ToList(),
                OrdensServicoPorTecnico.ToList(),
                ServicosMaisRealizados.ToList(),
                LucroPorServico.ToList());
            GerarManifestoEvidencias(manifestoPath, pdfPath, csvPath);

            var pacote = new RelatorioPacoteEvidencias(diretorio, pdfPath, csvPath, manifestoPath);
            UltimoCaminhoExportacao = pacote.Diretorio;
            ResumoUltimaExportacao = $"Pacote de evidencias gerado com {pacote.TotalArquivos} arquivo(s): {pacote.Diretorio}";
            StatusSistema = "Evidencias exportadas";

            global::PrimoAutoEletrica.App.Logger.LogInfo(
                $"Pacote de evidencias de relatorios gerado em {pacote.Diretorio}.",
                "Relatorios");

            return pacote;
        }

        private void GerarManifestoEvidencias(string manifestoPath, string pdfPath, string csvPath)
        {
            var linhas = new List<string>
            {
                "PRIMO AUTO ELETRICA - PACOTE DE EVIDENCIAS DE RELATORIOS",
                $"GeradoEm={DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Usuario={UsuarioLogado}",
                $"Periodo={DataInicio:yyyy-MM-dd} ate {DataFim:yyyy-MM-dd}",
                $"FiltroOperador={FiltroOperador}",
                $"FiltroVendedor={FiltroVendedor}",
                $"FiltroCliente={FiltroCliente}",
                $"FiltroCategoria={FiltroCategoria}",
                $"FiltroMarca={FiltroMarca}",
                $"FiltroFormaPagamento={FiltroFormaPagamento}",
                $"FiltroStatus={FiltroStatus}",
                "",
                "[Arquivos]",
                $"PDF={pdfPath}",
                $"CSV={csvPath}",
                "",
                "[Totais]",
                $"Financeiro={DadosFinanceiros.Count}",
                $"Vendas={DadosVendas.Count}",
                $"MargemPorProduto={MargemPorProduto.Count}",
                $"VendasPorHora={VendasPorHora.Count}",
                $"VendasPorDia={VendasPorDia.Count}",
                $"InadimplenciaDetalhada={InadimplenciaDetalhada.Count}",
                $"ConciliacaoFinanceira={ConciliacaoFinanceira.Count}",
                $"OrdensServicoAbertas={OrdensServicoAbertas.Count}",
                $"OrdensServicoFinalizadas={OrdensServicoFinalizadas.Count}",
                $"OrdensServicoPorTecnico={OrdensServicoPorTecnico.Count}",
                $"ServicosMaisRealizados={ServicosMaisRealizados.Count}",
                $"LucroPorServico={LucroPorServico.Count}",
                $"Estoque={DadosEstoque.Count}",
                $"Clientes={DadosClientes.Count}",
                $"Orcamentos={DadosOrcamentos.Count}",
                $"Caixa={DadosCaixa.Count}",
                $"AuditoriaExibida={Auditoria.Count}",
                "",
                "[Indicadores]",
                $"FaturamentoTotal={FaturamentoTotal}",
                $"LucroLiquido={LucroLiquido}",
                $"TicketMedio={TicketMedio}",
                $"FluxoCaixa={FluxoCaixa}",
                $"Inadimplencia={Inadimplencia}",
                $"ConversaoOrcamentos={ConversaoOrcamentos}",
                $"ResultadoOperacional={DemonstrativoResultado.ResultadoOperacional}",
                $"ResultadoProjetado={DemonstrativoResultado.ResultadoProjetado}",
                "",
                "[Resumos]",
                $"CurvaABC={ResumoCurvaAbc}",
                $"ProdutosParados={RankingProdutosParados}",
                $"Margem={ResumoMargemProdutos}",
                $"MelhorHorario={MelhorHorarioVendas}",
                $"MelhorDia={MelhorDiaVendas}",
                $"Inadimplencia={ResumoInadimplenciaDetalhada}",
                $"DRE={ResumoDreOperacional}",
                $"Conciliacao={ResumoConciliacaoFinanceira}",
                $"OrdensServico={ResumoOrdensServico}",
                $"ServicosOperacionais={ResumoServicosOperacionais}",
                $"Auditoria={ResumoAuditoria}",
                $"Consistencia={ResumoConsistenciaOperacional}"
            };

            File.WriteAllLines(manifestoPath, linhas, Encoding.UTF8);
        }

        private static string CriarCaminhoExportacao(string prefixo, string extensao)
        {
            var diretorio = ObterDiretorioBaseExportacao();
            Directory.CreateDirectory(diretorio);
            return Path.Combine(diretorio, $"{prefixo}_{DateTime.Now:yyyyMMdd_HHmmssfff}{extensao}");
        }

        private static string CriarDiretorioPacoteEvidencias(string timestamp)
        {
            var diretorioBase = ObterDiretorioBaseExportacao();
            return Path.Combine(diretorioBase, $"Evidencias_Relatorios_{timestamp}");
        }

        private static string ObterDiretorioBaseExportacao()
        {
            if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
            {
                return Path.Combine(global::PrimoAutoEletrica.App.RuntimeLogDirectory, "relatorios-exportacoes");
            }

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrWhiteSpace(desktop))
            {
                return Path.Combine(desktop, "PrimoAutoEletrica-Relatorios");
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PrimoAutoEletrica", "Relatorios");
        }

        private void RegistrarExportacao(string tipo, string caminho)
        {
            UltimoCaminhoExportacao = caminho;
            ResumoUltimaExportacao = $"{tipo} gerado em {caminho}";
            StatusSistema = $"{tipo} exportado";
            global::PrimoAutoEletrica.App.Logger.LogInfo($"{tipo} de relatorios gerado em {caminho}.", "Relatorios");
        }

        // Relatórios Personalizáveis
        public void AdicionarColuna(string coluna)
        {
            if (!ColunasSelecionadas.Contains(coluna))
            {
                ColunasSelecionadas.Add(coluna);
            }
        }

        public void RemoverColuna(string coluna)
        {
            ColunasSelecionadas.Remove(coluna);
        }

        public void SalvarRelatorioPersonalizado(string nome)
        {
            if (!string.IsNullOrEmpty(nome) && ColunasSelecionadas.Any())
            {
                RelatoriosSalvos.Add(nome);
            }
        }

        public void CarregarRelatorioPersonalizado(string nome)
        {
            // Carregar configurações do relatório salvo
        }

        // Busca Inteligente Global
        public List<object> BuscarGlobal(string termo)
        {
            var resultados = new List<object>();

            // Buscar em clientes
            var clientesEncontrados = DadosClientes.Where(c => 
                c.Nome.ToLower().Contains(termo.ToLower()) ||
                c.Telefone.Contains(termo) ||
                c.Email.ToLower().Contains(termo.ToLower())).ToList();
            resultados.AddRange(clientesEncontrados);

            // Buscar em produtos (via estoque)
            var produtosEncontrados = DadosEstoque.Where(p =>
                p.ProdutoNome.ToLower().Contains(termo.ToLower()) ||
                p.ProdutoCodigo.ToLower().Contains(termo.ToLower())).ToList();
            resultados.AddRange(produtosEncontrados);

            // Buscar em vendas
            var vendasEncontradas = DadosVendas.Where(v =>
                v.ClienteNome.ToLower().Contains(termo.ToLower()) ||
                v.VendedorNome.ToLower().Contains(termo.ToLower())).ToList();
            resultados.AddRange(vendasEncontradas);

            // Buscar em orçamentos
            var orcamentosEncontrados = DadosOrcamentos.Where(o =>
                o.ClienteNome.ToLower().Contains(termo.ToLower()) ||
                o.VendedorNome.ToLower().Contains(termo.ToLower()) ||
                o.Numero.Contains(termo)).ToList();
            resultados.AddRange(orcamentosEncontrados);

            return resultados;
        }

        // Integrações com módulos
        // Integração com PDV
        public List<DadoVenda> ObterVendasPDV(DateTime dataInicio, DateTime dataFim)
        {
            return DadosVendas.Where(v => v.Data >= dataInicio && v.Data <= dataFim).ToList();
        }

        // Integração com Financeiro
        public List<DadoFinanceiro> ObterMovimentacoesFinanceiras(DateTime dataInicio, DateTime dataFim)
        {
            return DadosFinanceiros.Where(d => d.Data >= dataInicio && d.Data <= dataFim).ToList();
        }

        // Integração com Estoque
        public List<DadoEstoque> ObterEstoqueCritico()
        {
            return DadosEstoque.Where(e => e.QuantidadeAtual <= e.QuantidadeMinima).ToList();
        }

        // Integração com Clientes
        public List<DadoCliente> ObterClientesAtivos()
        {
            return DadosClientes.Where(c => c.Status == "Ativo" || c.Status == "VIP").ToList();
        }

        // Integração com Orçamentos
        public List<DadoOrcamento> ObterOrcamentosPendentes()
        {
            return DadosOrcamentos.Where(o =>
                o.Status == "Rascunho" ||
                o.Status == "Enviado" ||
                o.Status == "Em Aberto" ||
                o.Status == "Aguardando Cliente").ToList();
        }

        // Integração com Compras
        public List<DadoFinanceiro> ObterCompras(DateTime dataInicio, DateTime dataFim)
        {
            return DadosFinanceiros.Where(d => d.Tipo == "Despesa" && d.Categoria == "Compras" && d.Data >= dataInicio && d.Data <= dataFim).ToList();
        }

        // Integração com Produtos
        public List<DadoEstoque> ObterProdutosMaisVendidos(int quantidade)
        {
            return DadosEstoque.OrderByDescending(e => e.GiroMensal).Take(quantidade).ToList();
        }

        // Integração com Caixa
        public List<DadoCaixa> ObterMovimentacoesCaixa(DateTime dataInicio, DateTime dataFim)
        {
            return DadosCaixa.Where(c => c.Data >= dataInicio && c.Data <= dataFim).ToList();
        }

        // Integração com Usuários
        public List<DadoVenda> ObterVendasPorOperador(string operador)
        {
            return DadosVendas.Where(v => v.VendedorNome == operador).ToList();
        }

        // Integração com Fornecedores
        public List<DadoFinanceiro> ObterPagamentosFornecedores(DateTime dataInicio, DateTime dataFim)
        {
            return DadosFinanceiros.Where(d => d.Tipo == "Despesa" && d.Categoria == "Fornecedor" && d.Data >= dataInicio && d.Data <= dataFim).ToList();
        }

        // Integração com Vendedores
        public Dictionary<string, decimal> ObterPerformanceVendedores()
        {
            return DadosVendas.GroupBy(v => v.VendedorNome)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.ValorTotal));
        }

        // Properties
        public string UsuarioLogado
        {
            get => _usuarioLogado;
            set { _usuarioLogado = value; OnPropertyChanged(); }
        }

        public bool RelatorioFavorito
        {
            get => _relatorioFavorito;
            set { _relatorioFavorito = value; OnPropertyChanged(); }
        }

        public bool ModoExecutivoAtivo
        {
            get => _modoExecutivoAtivo;
            set { _modoExecutivoAtivo = value; OnPropertyChanged(); }
        }

        public DateTime DataAtual
        {
            get => _dataAtual;
            set { _dataAtual = value; OnPropertyChanged(); }
        }

        public string HoraAtual
        {
            get => _horaAtual;
            set { _horaAtual = value; OnPropertyChanged(); }
        }

        public string PeriodoSelecionado
        {
            get => _periodoSelecionado;
            set { _periodoSelecionado = value; OnPropertyChanged(); }
        }

        public string EmpresaAtual
        {
            get => _empresaAtual;
            set { _empresaAtual = value; OnPropertyChanged(); }
        }

        public string StatusSistema
        {
            get => _statusSistema;
            set { _statusSistema = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set { _loadingMessage = value; OnPropertyChanged(); }
        }

        public bool HasLoadError
        {
            get => _hasLoadError;
            private set { _hasLoadError = value; OnPropertyChanged(); }
        }

        public string LoadErrorMessage
        {
            get => _loadErrorMessage;
            private set { _loadErrorMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Ticket medio = AVG(ValorTotal) de vendas concluidas no periodo (fonte: Vendas via ObterTicketMedio).
        /// Faturamento = soma de vendas concluidas (ObterFaturamentoTotal). Nao e faturamento/qtd OS.
        /// </summary>
        public string PulseFaturamentoText { get; private set; } = "R$ 0,00";
        public string PulseTicketMedioText { get; private set; } = "R$ 0,00";
        public string PulseOsAbertasText { get; private set; } = "0";
        public string PulseOsFinalizadasText { get; private set; } = "0";
        public string PulseClientesText { get; private set; } = "0";

        public bool TemDadosRelatorio =>
            OrdensServicoAbertas.Count > 0
            || OrdensServicoFinalizadas.Count > 0
            || DadosVendas.Count > 0
            || DadosEstoque.Count > 0
            || DadosClientes.Count > 0
            || FaturamentoTotal != 0m
            || TicketMedio != 0m;

        public bool ShowEmptyState =>
            _dadosCarregados && !IsLoading && !HasLoadError && !TemDadosRelatorio;

        public bool ShowContentState =>
            _dadosCarregados && !IsLoading && !HasLoadError;

        public decimal ResumoFaturamento
        {
            get => _resumoFaturamento;
            set { _resumoFaturamento = value; OnPropertyChanged(); }
        }

        public decimal FaturamentoTotal
        {
            get => _faturamentoTotal;
            set { _faturamentoTotal = value; OnPropertyChanged(); }
        }

        public decimal LucroLiquido
        {
            get => _lucroLiquido;
            set { _lucroLiquido = value; OnPropertyChanged(); }
        }

        public decimal VendasHoje
        {
            get => _vendasHoje;
            set { _vendasHoje = value; OnPropertyChanged(); }
        }

        public decimal TicketMedio
        {
            get => _ticketMedio;
            set { _ticketMedio = value; OnPropertyChanged(); }
        }

        public int TotalClientes
        {
            get => _totalClientes;
            set { _totalClientes = value; OnPropertyChanged(); }
        }

        public int ProdutosVendidos
        {
            get => _produtosVendidos;
            set { _produtosVendidos = value; OnPropertyChanged(); }
        }

        public decimal FluxoCaixa
        {
            get => _fluxoCaixa;
            set { _fluxoCaixa = value; OnPropertyChanged(); }
        }

        public decimal ContasRecebidas
        {
            get => _contasRecebidas;
            set { _contasRecebidas = value; OnPropertyChanged(); }
        }

        public decimal ContasPendentes
        {
            get => _contasPendentes;
            set { _contasPendentes = value; OnPropertyChanged(); }
        }

        public decimal Inadimplencia
        {
            get => _inadimplencia;
            set { _inadimplencia = value; OnPropertyChanged(); }
        }

        public decimal MargemLucro
        {
            get => _margemLucro;
            set { _margemLucro = value; OnPropertyChanged(); }
        }

        public decimal MetaMensal
        {
            get => _metaMensal;
            set { _metaMensal = value; OnPropertyChanged(); }
        }

        public decimal ConversaoOrcamentos
        {
            get => _conversaoOrcamentos;
            set { _conversaoOrcamentos = value; OnPropertyChanged(); }
        }

        public decimal CrescimentoMensal
        {
            get => _crescimentoMensal;
            set { _crescimentoMensal = value; OnPropertyChanged(); }
        }

        public string OperadorDestaque
        {
            get => _operadorDestaque;
            set { _operadorDestaque = value; OnPropertyChanged(); }
        }

        public string ClienteDestaque
        {
            get => _clienteDestaque;
            set { _clienteDestaque = value; OnPropertyChanged(); }
        }

        public decimal CaixaAtual
        {
            get => _caixaAtual;
            set { _caixaAtual = value; OnPropertyChanged(); }
        }

        public int VendasCanceladas
        {
            get => _vendasCanceladas;
            set { _vendasCanceladas = value; OnPropertyChanged(); }
        }

        public int ProdutosSemGiro
        {
            get => _produtosSemGiro;
            set { _produtosSemGiro = value; OnPropertyChanged(); }
        }

        public int ProdutosCurvaA
        {
            get => _produtosCurvaA;
            set { _produtosCurvaA = value; OnPropertyChanged(); }
        }

        public int ProdutosCurvaB
        {
            get => _produtosCurvaB;
            set { _produtosCurvaB = value; OnPropertyChanged(); }
        }

        public int ProdutosCurvaC
        {
            get => _produtosCurvaC;
            set { _produtosCurvaC = value; OnPropertyChanged(); }
        }

        public string ResumoCurvaAbc
        {
            get => _resumoCurvaAbc;
            set { _resumoCurvaAbc = value; OnPropertyChanged(); }
        }

        public string RankingProdutosParados
        {
            get => _rankingProdutosParados;
            set { _rankingProdutosParados = value; OnPropertyChanged(); }
        }

        public string ResumoMargemProdutos
        {
            get => _resumoMargemProdutos;
            set { _resumoMargemProdutos = value; OnPropertyChanged(); }
        }

        public string MelhorHorarioVendas
        {
            get => _melhorHorarioVendas;
            set { _melhorHorarioVendas = value; OnPropertyChanged(); }
        }

        public string MelhorDiaVendas
        {
            get => _melhorDiaVendas;
            set { _melhorDiaVendas = value; OnPropertyChanged(); }
        }

        public string ResumoInadimplenciaDetalhada
        {
            get => _resumoInadimplenciaDetalhada;
            set { _resumoInadimplenciaDetalhada = value; OnPropertyChanged(); }
        }

        public string ResumoDreOperacional
        {
            get => _resumoDreOperacional;
            set { _resumoDreOperacional = value; OnPropertyChanged(); }
        }

        public string ResumoConciliacaoFinanceira
        {
            get => _resumoConciliacaoFinanceira;
            set { _resumoConciliacaoFinanceira = value; OnPropertyChanged(); }
        }

        public string ResumoOrdensServico
        {
            get => _resumoOrdensServico;
            set { _resumoOrdensServico = value; OnPropertyChanged(); }
        }

        public string ResumoServicosOperacionais
        {
            get => _resumoServicosOperacionais;
            set { _resumoServicosOperacionais = value; OnPropertyChanged(); }
        }

        public string UltimoCaminhoExportacao
        {
            get => _ultimoCaminhoExportacao;
            set
            {
                _ultimoCaminhoExportacao = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TemExportacaoGerada));
            }
        }

        public string ResumoUltimaExportacao
        {
            get => _resumoUltimaExportacao;
            set { _resumoUltimaExportacao = value; OnPropertyChanged(); }
        }

        public bool TemExportacaoGerada => !string.IsNullOrWhiteSpace(UltimoCaminhoExportacao);

        public DemonstrativoResultadoFinanceiro DemonstrativoResultado
        {
            get => _demonstrativoResultado;
            set { _demonstrativoResultado = value; OnPropertyChanged(); }
        }

        public DateTime DataInicio
        {
            get => _dataInicio;
            set { _dataInicio = value; OnPropertyChanged(); }
        }

        public DateTime DataFim
        {
            get => _dataFim;
            set { _dataFim = value; OnPropertyChanged(); }
        }

        public string FiltroOperador
        {
            get => _filtroOperador;
            set { _filtroOperador = value; OnPropertyChanged(); }
        }

        public string FiltroVendedor
        {
            get => _filtroVendedor;
            set { _filtroVendedor = value; OnPropertyChanged(); }
        }

        public string FiltroCliente
        {
            get => _filtroCliente;
            set { _filtroCliente = value; OnPropertyChanged(); }
        }

        public string FiltroCategoria
        {
            get => _filtroCategoria;
            set { _filtroCategoria = value; OnPropertyChanged(); }
        }

        public string FiltroMarca
        {
            get => _filtroMarca;
            set { _filtroMarca = value; OnPropertyChanged(); }
        }

        public string FiltroFormaPagamento
        {
            get => _filtroFormaPagamento;
            set { _filtroFormaPagamento = value; OnPropertyChanged(); }
        }

        public string FiltroStatus
        {
            get => _filtroStatus;
            set { _filtroStatus = value; OnPropertyChanged(); }
        }

        public string CategoriaAuditoriaSelecionada
        {
            get => _categoriaAuditoriaSelecionada;
            set { _categoriaAuditoriaSelecionada = value; OnPropertyChanged(); }
        }

        public string SeveridadeAuditoriaSelecionada
        {
            get => _severidadeAuditoriaSelecionada;
            set { _severidadeAuditoriaSelecionada = value; OnPropertyChanged(); }
        }

        public string StatusAuditoriaSelecionado
        {
            get => _statusAuditoriaSelecionado;
            set { _statusAuditoriaSelecionado = value; OnPropertyChanged(); }
        }

        public string UsuarioAuditoriaFiltro
        {
            get => _usuarioAuditoriaFiltro;
            set { _usuarioAuditoriaFiltro = value; OnPropertyChanged(); }
        }

        public string TermoAuditoriaFiltro
        {
            get => _termoAuditoriaFiltro;
            set { _termoAuditoriaFiltro = value; OnPropertyChanged(); }
        }

        public int PaginaAuditoriaAtual
        {
            get => _paginaAuditoriaAtual;
            set
            {
                _paginaAuditoriaAtual = value < 1 ? 1 : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TemPaginaAnteriorAuditoria));
                OnPropertyChanged(nameof(TemProximaPaginaAuditoria));
            }
        }

        public int TotalAuditoria
        {
            get => _totalAuditoria;
            set { _totalAuditoria = value; OnPropertyChanged(); }
        }

        public int TotalPaginasAuditoria
        {
            get => _totalPaginasAuditoria;
            set
            {
                _totalPaginasAuditoria = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TemPaginaAnteriorAuditoria));
                OnPropertyChanged(nameof(TemProximaPaginaAuditoria));
            }
        }

        public string ResumoAuditoria
        {
            get => _resumoAuditoria;
            set { _resumoAuditoria = value; OnPropertyChanged(); }
        }

        public string ResumoConsistenciaOperacional
        {
            get => _resumoConsistenciaOperacional;
            set { _resumoConsistenciaOperacional = value; OnPropertyChanged(); }
        }

        public bool TemPaginaAnteriorAuditoria => PaginaAuditoriaAtual > 1;

        public bool TemProximaPaginaAuditoria => TotalPaginasAuditoria > 0 && PaginaAuditoriaAtual < TotalPaginasAuditoria;

        public bool DadosCarregados => _dadosCarregados;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void CarregarPreferenciasWorkspace()
        {
            var preferences = _workspacePreferenceService.LoadReportPreferences(UsuarioLogado);

            RelatorioFavorito = preferences.IsFavorite;
            ModoExecutivoAtivo = preferences.ExecutiveModeEnabled;
            DataInicio = preferences.DataInicio;
            DataFim = preferences.DataFim;
            FiltroOperador = preferences.FiltroOperador;
            FiltroVendedor = preferences.FiltroVendedor;
            FiltroCliente = preferences.FiltroCliente;
            FiltroCategoria = preferences.FiltroCategoria;
            FiltroMarca = preferences.FiltroMarca;
            FiltroFormaPagamento = preferences.FiltroFormaPagamento;
            FiltroStatus = preferences.FiltroStatus;

            AtualizarPeriodoSelecionado();
            StatusSistema = ModoExecutivoAtivo ? "Modo executivo" : "Online";
        }

        private ReportWorkspacePreferences BuildCurrentWorkspacePreferences()
        {
            return new ReportWorkspacePreferences
            {
                IsFavorite = RelatorioFavorito,
                ExecutiveModeEnabled = ModoExecutivoAtivo,
                DataInicio = DataInicio,
                DataFim = DataFim,
                FiltroOperador = FiltroOperador,
                FiltroVendedor = FiltroVendedor,
                FiltroCliente = FiltroCliente,
                FiltroCategoria = FiltroCategoria,
                FiltroMarca = FiltroMarca,
                FiltroFormaPagamento = FiltroFormaPagamento,
                FiltroStatus = FiltroStatus
            };
        }

        private void AtualizarPeriodoSelecionado()
        {
            var totalDias = Math.Max(1, (DataFim.Date - DataInicio.Date).Days + 1);
            PeriodoSelecionado = totalDias switch
            {
                1 => $"Dia {DataInicio:dd/MM/yyyy}",
                <= 7 => $"{totalDias} dias",
                <= 31 => "Periodo mensal",
                _ => $"{DataInicio:dd/MM/yyyy} ate {DataFim:dd/MM/yyyy}"
            };
        }

        private sealed class RelatorioSnapshot
        {
            public List<DadoFinanceiro> DadosFinanceiros { get; init; } = new();
            public List<DadoVenda> DadosVendas { get; init; } = new();
            public List<DadoMargemProduto> MargemPorProduto { get; init; } = new();
            public List<DadoVendaPeriodo> VendasPorHora { get; init; } = new();
            public List<DadoVendaPeriodo> VendasPorDia { get; init; } = new();
            public List<DadoInadimplencia> InadimplenciaDetalhada { get; init; } = new();
            public List<DadoConciliacaoFinanceira> ConciliacaoFinanceira { get; init; } = new();
            public List<DadoOrdemServicoRelatorio> OrdensServicoAbertas { get; init; } = new();
            public List<DadoOrdemServicoRelatorio> OrdensServicoFinalizadas { get; init; } = new();
            public List<DadoOrdemServicoTecnico> OrdensServicoPorTecnico { get; init; } = new();
            public List<DadoServicoRelatorio> ServicosMaisRealizados { get; init; } = new();
            public List<DadoServicoRelatorio> LucroPorServico { get; init; } = new();
            public List<DadoEstoque> DadosEstoque { get; init; } = new();
            public List<DadoCliente> DadosClientes { get; init; } = new();
            public List<DadoOrcamento> DadosOrcamentos { get; init; } = new();
            public List<DadoCaixa> DadosCaixa { get; init; } = new();
            public List<DadoMeta> Metas { get; init; } = new();
            public List<DadoAlerta> Alertas { get; init; } = new();
            public List<DadoTimeline> Timeline { get; init; } = new();
            public DadoComparativo Comparativo { get; init; } = new();
            public DemonstrativoResultadoFinanceiro DemonstrativoResultado { get; init; } = new();
            public decimal FaturamentoTotal { get; init; }
            public decimal LucroLiquido { get; init; }
            public decimal TicketMedio { get; init; }
            public int TotalClientes { get; init; }
            public int ProdutosSemGiro { get; init; }
            public decimal ConversaoOrcamentos { get; init; }
            public string ResumoConsistenciaOperacional { get; init; } = string.Empty;
        }
    }

    public sealed class RelatorioPacoteEvidencias
    {
        public RelatorioPacoteEvidencias(string diretorio, string pdfPath, string csvPath, string manifestoPath)
        {
            Diretorio = diretorio;
            PdfPath = pdfPath;
            CsvPath = csvPath;
            ManifestoPath = manifestoPath;
        }

        public string Diretorio { get; }
        public string PdfPath { get; }
        public string CsvPath { get; }
        public string ManifestoPath { get; }
        public int TotalArquivos => Arquivos.Count;
        public IReadOnlyList<string> Arquivos => new[] { PdfPath, CsvPath, ManifestoPath };
    }
}


