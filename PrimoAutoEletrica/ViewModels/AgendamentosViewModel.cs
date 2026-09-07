using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PrimoAutoEletrica.ViewModels
{
    public class AgendamentosViewModel : INotifyPropertyChanged
    {
        private static LoggerService Logger => global::PrimoAutoEletrica.App.Logger;
        private readonly AgendamentoDatabaseService _agendamentoService;
        private readonly FinanceiroDatabaseService _financeiroDatabaseService;
        private readonly VendaService _vendaService;
        private readonly AgendamentoReportService _reportService;
        private readonly DatabaseService _databaseService;
        private readonly PermissionService _permissionService;
        private Agendamento? _agendamentoSelecionado;
        private string _filtroBusca = string.Empty;
        private DateTime _dataSelecionada = DateTime.Today;
        private DateTime _dataInicioPeriodo = DateTime.Today.AddDays(-7);
        private DateTime _dataFimPeriodo = DateTime.Today.AddDays(30);
        private string _filtroStatus = "Todos";
        private string _filtroTecnico = "Todos";
        private string _filtroPrioridade = "Todas";
        private string _filtroCliente = string.Empty;
        private string _filtroVeiculo = string.Empty;
        private string _visualizacaoCalendario = "Diária";
        private string _horaAtual = DateTime.Now.ToString("HH:mm:ss");
        private string _dataAtual = DateTime.Now.ToString("dd/MM/yyyy");
        private string _usuarioLogado = "Administrador";
        private bool _modoOficina = false;
        private bool _telaCheia = false;

        // Dashboard Cards
        private int _totalAgendamentosHoje;
        private int _servicosConcluidos;
        private int _servicosEmAndamento;
        private int _servicosPendentes;
        private int _veiculosNaOficina;
        private int _tecnicosAtivos;
        private decimal _faturamentoPrevisto;
        private decimal _ticketMedio;
        private TimeSpan _tempoMedioAtendimento;
        private int _clientesAguardando;
        private int _osAbertas;
        private int _servicosCancelados;
        private decimal _taxaOcupacao;
        private decimal _taxaRetornoClientes;

        public ObservableCollection<Agendamento> Agendamentos { get; set; } = new();
        public ObservableCollection<Agendamento> AgendamentosFiltrados { get; set; } = new();
        public ObservableCollection<Agendamento> AgendamentosHoje { get; set; } = new();
        public ObservableCollection<Agendamento> AgendamentosSemana { get; set; } = new();
        public ObservableCollection<Agendamento> AgendamentosMes { get; set; } = new();
        public ObservableCollection<Agendamento> AgendamentosEmAndamento { get; set; } = new();
        public ObservableCollection<Agendamento> AgendamentosPendentes { get; set; } = new();
        public ObservableCollection<Agendamento> AgendamentosConcluidos { get; set; } = new();
        public ObservableCollection<Agendamento> AgendamentosCancelados { get; set; } = new();
        public ObservableCollection<TecnicoAgendamento> Tecnicos { get; set; } = new();
        public ObservableCollection<VeiculoAgendamento> Veiculos { get; set; } = new();
        public ObservableCollection<ClienteAgendamento> Clientes { get; set; } = new();
        public ObservableCollection<AlertaAgendamento> Alertas { get; set; } = new();
        public ObservableCollection<AgendamentoTimeline> Timeline { get; set; } = new();
        public ObservableCollection<DashboardAgendamento> DashboardCards { get; set; } = new();
        public ObservableCollection<string> StatusOptions { get; set; } = new();
        public ObservableCollection<string> PrioridadeOptions { get; set; } = new();
        public ObservableCollection<string> VisualizacaoOptions { get; set; } = new();

        public Agendamento? AgendamentoSelecionado
        {
            get => _agendamentoSelecionado;
            set
            {
                if (ReferenceEquals(_agendamentoSelecionado, value))
                {
                    return;
                }

                _agendamentoSelecionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string FiltroBusca
        {
            get => _filtroBusca;
            set { _filtroBusca = value; FiltrarAgendamentos(); OnPropertyChanged(); }
        }

        public DateTime DataSelecionada
        {
            get => _dataSelecionada;
            set { _dataSelecionada = value.Date; AtualizarVisualizacao(); OnPropertyChanged(); }
        }

        public DateTime DataInicioPeriodo
        {
            get => _dataInicioPeriodo;
            set { _dataInicioPeriodo = value; CarregarAgendamentosPorPeriodo(); OnPropertyChanged(); }
        }

        public DateTime DataFimPeriodo
        {
            get => _dataFimPeriodo;
            set { _dataFimPeriodo = value; CarregarAgendamentosPorPeriodo(); OnPropertyChanged(); }
        }

        public string FiltroStatus
        {
            get => _filtroStatus;
            set { _filtroStatus = UiTextSanitizer.SanitizeText(value); FiltrarAgendamentos(); OnPropertyChanged(); }
        }

        public string FiltroTecnico
        {
            get => _filtroTecnico;
            set { _filtroTecnico = value; FiltrarAgendamentos(); OnPropertyChanged(); }
        }

        public string FiltroPrioridade
        {
            get => _filtroPrioridade;
            set { _filtroPrioridade = value; FiltrarAgendamentos(); OnPropertyChanged(); }
        }

        public string FiltroCliente
        {
            get => _filtroCliente;
            set { _filtroCliente = value; FiltrarAgendamentos(); OnPropertyChanged(); }
        }

        public string FiltroVeiculo
        {
            get => _filtroVeiculo;
            set { _filtroVeiculo = value; FiltrarAgendamentos(); OnPropertyChanged(); }
        }

        public string VisualizacaoCalendario
        {
            get => _visualizacaoCalendario;
            set { _visualizacaoCalendario = UiTextSanitizer.SanitizeText(value); AtualizarVisualizacao(); OnPropertyChanged(); }
        }

        public string HoraAtual
        {
            get => _horaAtual;
            set { _horaAtual = value; OnPropertyChanged(); }
        }

        public string DataAtual
        {
            get => _dataAtual;
            set { _dataAtual = value; OnPropertyChanged(); }
        }

        public string UsuarioLogado
        {
            get => _usuarioLogado;
            set { _usuarioLogado = value; OnPropertyChanged(); }
        }

        public bool ModoOficina
        {
            get => _modoOficina;
            set { _modoOficina = value; OnPropertyChanged(); }
        }

        public bool TelaCheia
        {
            get => _telaCheia;
            set { _telaCheia = value; OnPropertyChanged(); }
        }

        // Dashboard Properties
        public int TotalAgendamentosHoje
        {
            get => _totalAgendamentosHoje;
            set { _totalAgendamentosHoje = value; OnPropertyChanged(); }
        }

        public int ServicosConcluidos
        {
            get => _servicosConcluidos;
            set { _servicosConcluidos = value; OnPropertyChanged(); }
        }

        public int ServicosEmAndamento
        {
            get => _servicosEmAndamento;
            set { _servicosEmAndamento = value; OnPropertyChanged(); }
        }

        public int ServicosPendentes
        {
            get => _servicosPendentes;
            set { _servicosPendentes = value; OnPropertyChanged(); }
        }

        public int VeiculosNaOficina
        {
            get => _veiculosNaOficina;
            set { _veiculosNaOficina = value; OnPropertyChanged(); }
        }

        public int TecnicosAtivos
        {
            get => _tecnicosAtivos;
            set { _tecnicosAtivos = value; OnPropertyChanged(); }
        }

        public decimal FaturamentoPrevisto
        {
            get => _faturamentoPrevisto;
            set { _faturamentoPrevisto = value; OnPropertyChanged(); }
        }

        public decimal TicketMedio
        {
            get => _ticketMedio;
            set { _ticketMedio = value; OnPropertyChanged(); }
        }

        public TimeSpan TempoMedioAtendimento
        {
            get => _tempoMedioAtendimento;
            set { _tempoMedioAtendimento = value; OnPropertyChanged(); }
        }

        public int ClientesAguardando
        {
            get => _clientesAguardando;
            set { _clientesAguardando = value; OnPropertyChanged(); }
        }

        public int OsAbertas
        {
            get => _osAbertas;
            set { _osAbertas = value; OnPropertyChanged(); }
        }

        public int ServicosCancelados
        {
            get => _servicosCancelados;
            set { _servicosCancelados = value; OnPropertyChanged(); }
        }

        public decimal TaxaOcupacao
        {
            get => _taxaOcupacao;
            set { _taxaOcupacao = value; OnPropertyChanged(); }
        }

        public decimal TaxaRetornoClientes
        {
            get => _taxaRetornoClientes;
            set { _taxaRetornoClientes = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AdicionarCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand RemoverCommand { get; }
        public ICommand AtualizarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand ConfirmarCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }
        public ICommand BuscarCommand { get; }
        public ICommand NovoAgendamentoCommand { get; }
        public ICommand EncaixeRapidoCommand { get; }
        public ICommand ReagendarCommand { get; }
        public ICommand DuplicarCommand { get; }
        public ICommand ConverterEmOSCommand { get; }
        public ICommand GerarOSCommand { get; }
        public ICommand ImprimirAgendaCommand { get; }
        public ICommand ExportarPDFCommand { get; }
        public ICommand ExportarExcelCommand { get; }
        public ICommand FiltrosRapidosCommand { get; }
        public ICommand ModoOficinaCommand { get; }
        public ICommand TelaCheiaCommand { get; }
        public ICommand AtualizarHoraCommand { get; }
        public ICommand CarregarDashboardCommand { get; }
        public ICommand CarregarTecnicosCommand { get; }
        public ICommand CarregarVeiculosCommand { get; }
        public ICommand CarregarClientesCommand { get; }
        public ICommand CarregarAlertasCommand { get; }
        public ICommand CarregarTimelineCommand { get; }
        public ICommand LimparFiltrosCommand { get; }
        public ICommand AtualizarAgendamentoCommand { get; }
        public ICommand EnviarLembreteWhatsAppCommand { get; }
        public ICommand EnviarLembreteEmailCommand { get; }
        public ICommand GerarRelatorioCommand { get; }
        public ICommand FecharDetalhesCommand { get; }
        public ICommand EnviarWhatsAppCommand { get; }

        private DispatcherTimer? _timer;

        public event PropertyChangedEventHandler? PropertyChanged;

        private static void SanitizarAgendamento(Agendamento agendamento)
        {
            agendamento.Status = UiTextSanitizer.SanitizeText(agendamento.Status);
            agendamento.Prioridade = UiTextSanitizer.SanitizeText(agendamento.Prioridade);
            agendamento.TipoServico = UiTextSanitizer.SanitizeText(agendamento.TipoServico);
            agendamento.ClienteNome = UiTextSanitizer.SanitizeText(agendamento.ClienteNome);
            agendamento.VeiculoModelo = UiTextSanitizer.SanitizeText(agendamento.VeiculoModelo);
            agendamento.TecnicoNome = UiTextSanitizer.SanitizeText(agendamento.TecnicoNome);
        }

        private static void SanitizarOpcoes(ObservableCollection<string> opcoes)
        {
            for (var index = 0; index < opcoes.Count; index++)
            {
                opcoes[index] = UiTextSanitizer.SanitizeText(opcoes[index]);
            }
        }

        private static List<Agendamento> SanitizarAgendamentos(IEnumerable<Agendamento> agendamentos)
        {
            var lista = agendamentos.ToList();
            foreach (var agendamento in lista)
            {
                SanitizarAgendamento(agendamento);
            }

            return lista;
        }

        private static List<Agendamento> FiltrarAgendamentosPorData(IEnumerable<Agendamento> agendamentos, DateTime data)
        {
            var dataBase = data.Date;
            return agendamentos
                .Where(a => a.DataAgendamento.Date == dataBase)
                .OrderBy(a => a.HoraInicio ?? dataBase)
                .ToList();
        }

        private static List<Agendamento> FiltrarAgendamentosPorPeriodo(IEnumerable<Agendamento> agendamentos, DateTime dataInicio, DateTime dataFim)
        {
            var inicio = dataInicio.Date;
            var fim = dataFim.Date;

            return agendamentos
                .Where(a => a.DataAgendamento.Date >= inicio && a.DataAgendamento.Date <= fim)
                .OrderBy(a => a.DataAgendamento)
                .ThenBy(a => a.HoraInicio ?? a.DataAgendamento.Date)
                .ToList();
        }

        private static DateTime ObterInicioSemana(DateTime data)
        {
            var diaSemana = data.Date.DayOfWeek;
            var deslocamento = diaSemana == DayOfWeek.Sunday ? -6 : (int)DayOfWeek.Monday - (int)diaSemana;
            return data.Date.AddDays(deslocamento);
        }

        private static void AtualizarColecao<T>(ObservableCollection<T> colecao, IEnumerable<T> itens)
        {
            colecao.Clear();
            foreach (var item in itens)
            {
                colecao.Add(item);
            }
        }

        private List<Agendamento> ObterTodosAgendamentosSanitizados()
        {
            return SanitizarAgendamentos(_agendamentoService.ObterTodosAgendamentos());
        }

        public AgendamentosViewModel()
        {
            _agendamentoService = new AgendamentoDatabaseService();
            _financeiroDatabaseService = new FinanceiroDatabaseService();
            _vendaService = new VendaService();
            _reportService = new AgendamentoReportService();
            _databaseService = global::PrimoAutoEletrica.App.Database;
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger);
            UsuarioLogado = global::PrimoAutoEletrica.App.Session.UserName;
            _visualizacaoCalendario = UiTextSanitizer.SanitizeText(_visualizacaoCalendario);

            foreach (var status in new[] { "Todos", "Agendado", "Confirmado", "Aguardando Cliente", "Em Andamento", "Aguardando Peça", "Pausado", "Finalizado", "Cancelado", "Entregue" })
                StatusOptions.Add(status);
            foreach (var prioridade in new[] { "Todas", "Baixa", "Normal", "Alta", "Urgente" })
                PrioridadeOptions.Add(prioridade);
            foreach (var visualizacao in new[] { "Diária", "Semanal", "Mensal", "Timeline" })
                VisualizacaoOptions.Add(visualizacao);
            SanitizarOpcoes(StatusOptions);
            SanitizarOpcoes(VisualizacaoOptions);

            // Initialize commands
            AdicionarCommand = new RelayCommand(AdicionarAgendamento);
            EditarCommand = new RelayCommand(EditarAgendamento, () => AgendamentoSelecionado != null);
            RemoverCommand = new RelayCommand(RemoverAgendamento, () => AgendamentoSelecionado != null);
            AtualizarCommand = new RelayCommand(AtualizarLista);
            CancelarCommand = new RelayCommand(CancelarAgendamento, () => AgendamentoSelecionado != null);
            ConfirmarCommand = new RelayCommand(ConfirmarAgendamento, () => AgendamentoSelecionado != null);
            CheckInCommand = new RelayCommand(CheckIn, () => AgendamentoSelecionado != null);
            CheckOutCommand = new RelayCommand(CheckOut, () => AgendamentoSelecionado != null);
            BuscarCommand = new RelayCommand(FiltrarAgendamentos);
            NovoAgendamentoCommand = new RelayCommand(NovoAgendamento);
            EncaixeRapidoCommand = new RelayCommand(EncaixeRapido);
            ReagendarCommand = new RelayCommand(Reagendar, () => AgendamentoSelecionado != null);
            DuplicarCommand = new RelayCommand(Duplicar, () => AgendamentoSelecionado != null);
            ConverterEmOSCommand = new RelayCommand(ConverterEmOS, () => AgendamentoSelecionado != null);
            GerarOSCommand = new RelayCommand(GerarOS, () => AgendamentoSelecionado != null);
            ImprimirAgendaCommand = new RelayCommand(ImprimirAgenda);
            ExportarPDFCommand = new RelayCommand(ExportarPDF);
            ExportarExcelCommand = new RelayCommand(ExportarExcel);
            FiltrosRapidosCommand = new RelayCommand(FiltrosRapidos);
            ModoOficinaCommand = new RelayCommand(ToggleModoOficina);
            TelaCheiaCommand = new RelayCommand(ToggleTelaCheia);
            AtualizarHoraCommand = new RelayCommand(AtualizarHora);
            CarregarDashboardCommand = new RelayCommand(CarregarDashboard);
            CarregarTecnicosCommand = new RelayCommand(CarregarTecnicos);
            CarregarVeiculosCommand = new RelayCommand(CarregarVeiculos);
            CarregarClientesCommand = new RelayCommand(CarregarClientes);
            CarregarAlertasCommand = new RelayCommand(CarregarAlertas);
            CarregarTimelineCommand = new RelayCommand(CarregarTimeline);
            LimparFiltrosCommand = new RelayCommand(LimparFiltros);
            AtualizarAgendamentoCommand = new RelayCommand(AtualizarAgendamento, () => AgendamentoSelecionado != null);
            EnviarLembreteWhatsAppCommand = new RelayCommand(EnviarLembreteWhatsApp, () => AgendamentoSelecionado != null);
            EnviarLembreteEmailCommand = new RelayCommand(EnviarLembreteEmail, () => AgendamentoSelecionado != null);
            GerarRelatorioCommand = new RelayCommand(GerarRelatorio);
            FecharDetalhesCommand = new RelayCommand(FecharDetalhes);
            EnviarWhatsAppCommand = new RelayCommand(EnviarWhatsApp, () => AgendamentoSelecionado != null);

            // Configure timer for real-time updates
            ConfigurarTimer();

            // Load initial data
            CarregarDadosIniciais();
        }

        private void ConfigurarTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(60); // Increased from 1s to 60s for performance
            _timer.Tick += (s, args) => AtualizarHora();
            _timer.Start();
        }

        private void CarregarDadosIniciais()
        {
            var todosAgendamentos = ObterTodosAgendamentosSanitizados();
            var agendamentosHoje = FiltrarAgendamentosPorData(todosAgendamentos, DateTime.Today);

            CarregarAgendamentosPorData(todosAgendamentos);
            CarregarTecnicos(todosAgendamentos);
            CarregarVeiculos(agendamentosHoje);
            CarregarClientes(todosAgendamentos);
            CarregarAlertas(agendamentosHoje);
            CarregarDashboard(todosAgendamentos, agendamentosHoje);
            AtualizarHora();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            ExibirMensagem(mensagem, "Acesso negado", System.Windows.MessageBoxImage.Warning);
            return false;
        }

        private void AtualizarHora()
        {
            HoraAtual = DateTime.Now.ToString("HH:mm:ss");
            DataAtual = DateTime.Now.ToString("dd/MM/yyyy");
        }

        public void AtualizarLista()
        {
            var todosAgendamentos = ObterTodosAgendamentosSanitizados();
            var agendamentosHoje = FiltrarAgendamentosPorData(todosAgendamentos, DateTime.Today);

            AtualizarColecao(Agendamentos, todosAgendamentos);
            FiltrarAgendamentos();
            CarregarTecnicos(todosAgendamentos);
            CarregarVeiculos(agendamentosHoje);
            CarregarClientes(todosAgendamentos);
            CarregarAlertas(agendamentosHoje);
            CarregarDashboard(todosAgendamentos, agendamentosHoje);
        }

        private void CarregarAgendamentosPorData()
        {
            CarregarAgendamentosPorData(null);
        }

        private void CarregarAgendamentosPorData(List<Agendamento>? todosAgendamentos)
        {
            var agendamentosDia = todosAgendamentos is null
                ? SanitizarAgendamentos(_agendamentoService.ObterAgendamentosPorData(DataSelecionada))
                : FiltrarAgendamentosPorData(todosAgendamentos, DataSelecionada);

            AtualizarColecao(AgendamentosHoje, agendamentosDia);
            AtualizarColecao(Agendamentos, agendamentosDia);
            FiltrarAgendamentos();
        }

        private void CarregarAgendamentosPorPeriodo()
        {
            CarregarAgendamentosPorPeriodo(null);
        }

        private void CarregarAgendamentosPorPeriodo(List<Agendamento>? todosAgendamentos)
        {
            var agendamentosPeriodo = todosAgendamentos is null
                ? SanitizarAgendamentos(_agendamentoService.ObterAgendamentosPorPeriodo(DataInicioPeriodo, DataFimPeriodo))
                : FiltrarAgendamentosPorPeriodo(todosAgendamentos, DataInicioPeriodo, DataFimPeriodo);

            AtualizarColecao(AgendamentosSemana, agendamentosPeriodo);
            AtualizarColecao(Agendamentos, agendamentosPeriodo);
            FiltrarAgendamentos();
        }

        private void FiltrarAgendamentos()
        {
            var filtrados = Agendamentos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroBusca))
            {
                filtrados = filtrados.Where(a =>
                    a.ClienteNome.Contains(FiltroBusca, StringComparison.OrdinalIgnoreCase) ||
                    a.VeiculoPlaca.Contains(FiltroBusca, StringComparison.OrdinalIgnoreCase) ||
                    a.Status.Contains(FiltroBusca, StringComparison.OrdinalIgnoreCase) ||
                    a.Numero.Contains(FiltroBusca, StringComparison.OrdinalIgnoreCase) ||
                    a.VeiculoModelo.Contains(FiltroBusca, StringComparison.OrdinalIgnoreCase));
            }

            if (FiltroStatus != "Todos")
            {
                filtrados = filtrados.Where(a => UiTextSanitizer.EqualsNormalized(a.Status, FiltroStatus));
            }

            if (FiltroPrioridade != "Todas")
            {
                filtrados = filtrados.Where(a => a.Prioridade == FiltroPrioridade);
            }

            if (!string.IsNullOrWhiteSpace(FiltroTecnico) && FiltroTecnico != "Todos")
            {
                filtrados = filtrados.Where(a => a.TecnicoNome.Contains(FiltroTecnico, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(FiltroCliente))
            {
                filtrados = filtrados.Where(a => a.ClienteNome.Contains(FiltroCliente, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(FiltroVeiculo))
            {
                filtrados = filtrados.Where(a => a.VeiculoPlaca.Contains(FiltroVeiculo, StringComparison.OrdinalIgnoreCase) ||
                    a.VeiculoModelo.Contains(FiltroVeiculo, StringComparison.OrdinalIgnoreCase));
            }

            AgendamentosFiltrados.Clear();
            foreach (var ag in filtrados)
                AgendamentosFiltrados.Add(ag);

            // Update status collections
            AtualizarColecoesStatus();
        }

        private void AtualizarColecoesStatus()
        {
            AgendamentosEmAndamento.Clear();
            AgendamentosPendentes.Clear();
            AgendamentosConcluidos.Clear();
            AgendamentosCancelados.Clear();

            foreach (var ag in AgendamentosFiltrados)
            {
                switch (ag.Status)
                {
                    case "Em Andamento":
                        AgendamentosEmAndamento.Add(ag);
                        break;
                    case "Agendado":
                    case "Confirmado":
                    case "Aguardando Cliente":
                        AgendamentosPendentes.Add(ag);
                        break;
                    case "Finalizado":
                    case "Entregue":
                        AgendamentosConcluidos.Add(ag);
                        break;
                    case "Cancelado":
                        AgendamentosCancelados.Add(ag);
                        break;
                }
            }
        }

        private void AtualizarVisualizacao()
        {
            var visualizacao = UiTextSanitizer.NormalizeKey(VisualizacaoCalendario);
            if (visualizacao == "DIARIA")
            {
                DefinirPeriodoAtual(DataSelecionada.Date, DataSelecionada.Date);
                CarregarAgendamentosPorData();
                return;
            }

            if (visualizacao == "SEMANAL")
            {
                var inicioSemana = ObterInicioSemana(DataSelecionada);
                DefinirPeriodoAtual(inicioSemana, inicioSemana.AddDays(6));
                CarregarAgendamentosPorPeriodo();
                return;
            }

            if (visualizacao == "MENSAL")
            {
                var inicioMesNormalizado = new DateTime(DataSelecionada.Year, DataSelecionada.Month, 1);
                var fimMesNormalizado = inicioMesNormalizado.AddMonths(1).AddDays(-1);
                DefinirPeriodoAtual(inicioMesNormalizado, fimMesNormalizado);
                var agendamentosMesNormalizado = _agendamentoService.ObterAgendamentosPorPeriodo(inicioMesNormalizado, fimMesNormalizado);
                AgendamentosMes.Clear();
                foreach (var ag in agendamentosMesNormalizado)
                {
                    SanitizarAgendamento(ag);
                    AgendamentosMes.Add(ag);
                }
                AtualizarColecao(Agendamentos, AgendamentosMes);
                FiltrarAgendamentos();
                return;
            }

            if (visualizacao == "TIMELINE")
            {
                CarregarAgendamentosPorPeriodo();
                return;
            }

            switch (VisualizacaoCalendario)
            {
                case "Diária":
                    CarregarAgendamentosPorData();
                    break;
                case "Semanal":
                    CarregarAgendamentosPorPeriodo();
                    break;
                case "Mensal":
                    var inicioMes = new DateTime(DataSelecionada.Year, DataSelecionada.Month, 1);
                    var fimMes = inicioMes.AddMonths(1).AddDays(-1);
                    var agendamentosMes = _agendamentoService.ObterAgendamentosPorPeriodo(inicioMes, fimMes);
                    AgendamentosMes.Clear();
                    foreach (var ag in agendamentosMes)
                        AgendamentosMes.Add(ag);
                    AtualizarColecao(Agendamentos, AgendamentosMes);
                    FiltrarAgendamentos();
                    break;
            }
        }

        private void DefinirPeriodoAtual(DateTime inicio, DateTime fim)
        {
            _dataInicioPeriodo = inicio.Date;
            _dataFimPeriodo = fim.Date;
            OnPropertyChanged(nameof(DataInicioPeriodo));
            OnPropertyChanged(nameof(DataFimPeriodo));
        }

        public void CarregarDashboard()
        {
            var todosAgendamentos = ObterTodosAgendamentosSanitizados();
            var agendamentosHoje = FiltrarAgendamentosPorData(todosAgendamentos, DateTime.Today);
            CarregarDashboard(todosAgendamentos, agendamentosHoje);
        }

        private void CarregarDashboard(List<Agendamento> todosAgendamentos, List<Agendamento> agendamentosHoje)
        {
            TotalAgendamentosHoje = agendamentosHoje.Count;
            ServicosConcluidos = todosAgendamentos.Count(a => a.Status == "Finalizado" || a.Status == "Entregue");
            ServicosEmAndamento = todosAgendamentos.Count(a => a.Status == "Em Andamento");
            ServicosPendentes = todosAgendamentos.Count(a => a.Status == "Agendado" || a.Status == "Confirmado" || a.Status == "Aguardando Cliente");
            VeiculosNaOficina = todosAgendamentos.Count(a => a.Status == "Em Andamento" && a.CheckIn.HasValue && !a.CheckOut.HasValue);
            TecnicosAtivos = Tecnicos.Count(t => t.Disponivel);
            FaturamentoPrevisto = agendamentosHoje.Sum(a => a.ValorEstimado);
            var agendamentosComValor = todosAgendamentos.Where(a => a.ValorReal > 0).ToList();
            TicketMedio = agendamentosComValor.Count > 0 ? agendamentosComValor.Average(a => a.ValorReal) : 0;
            ClientesAguardando = todosAgendamentos.Count(a => a.Status == "Aguardando Cliente");
            OsAbertas = todosAgendamentos.Count(a => a.OrdemServicoId.HasValue);
            ServicosCancelados = todosAgendamentos.Count(a => a.Status == "Cancelado");
            TaxaOcupacao = todosAgendamentos.Count > 0 && TecnicosAtivos > 0
                ? (decimal)ServicosEmAndamento / TecnicosAtivos * 100
                : 0;

            // Calculate average service time
            var servicosComTempo = todosAgendamentos.Where(a => a.DuracaoReal > TimeSpan.Zero).ToList();
            TempoMedioAtendimento = servicosComTempo.Count > 0
                ? TimeSpan.FromTicks((long)servicosComTempo.Average(a => a.DuracaoReal.Ticks))
                : TimeSpan.Zero;

            // Calculate return rate
            var clientesUnicos = todosAgendamentos.Select(a => a.ClienteId).Distinct().Count();
            var clientesRecorrentes = todosAgendamentos.GroupBy(a => a.ClienteId).Count(g => g.Count() > 1);
            TaxaRetornoClientes = clientesUnicos > 0 ? (decimal)clientesRecorrentes / clientesUnicos * 100 : 0;

            // Update dashboard cards
            AtualizarDashboardCards();
        }

        private void AtualizarDashboardCards()
        {
            // Cards legados mantidos para compatibilidade; a UI principal usa propriedades reais no OpsPulse.
            // Comparacao/Tendencia nao inventam percentuais — apenas rotulos descritivos dos dados.
            DashboardCards.Clear();
            DashboardCards.Add(new DashboardAgendamento
            {
                Icone = "*",
                Titulo = "Agendamentos Hoje",
                Valor = TotalAgendamentosHoje,
                Comparacao = "Contagem real do dia",
                Tendencia = "Hoje",
                Cor = "#2ECC71"
            });
            DashboardCards.Add(new DashboardAgendamento
            {
                Icone = "*",
                Titulo = "Serviços Concluídos",
                Valor = ServicosConcluidos,
                Comparacao = "Finalizado ou Entregue",
                Tendencia = "Status real",
                Cor = "#3498DB"
            });
            DashboardCards.Add(new DashboardAgendamento
            {
                Icone = "*",
                Titulo = "Em Andamento",
                Valor = ServicosEmAndamento,
                Comparacao = "Status Em Andamento",
                Tendencia = "Status real",
                Cor = "#F39C12"
            });
            DashboardCards.Add(new DashboardAgendamento
            {
                Icone = "*",
                Titulo = "Pendentes",
                Valor = ServicosPendentes,
                Comparacao = "Agendado / Confirmado / Aguardando",
                Tendencia = "Status real",
                Cor = "#E74C3C"
            });
            DashboardCards.Add(new DashboardAgendamento
            {
                Icone = "*",
                Titulo = "Cancelados",
                Valor = ServicosCancelados,
                Comparacao = "Status Cancelado",
                Tendencia = "Status real",
                Cor = "#95A5A6"
            });
            DashboardCards.Add(new DashboardAgendamento
            {
                Icone = "*",
                Titulo = "Faturamento Previsto (hoje)",
                Valor = $"R$ {FaturamentoPrevisto:F2}",
                Comparacao = "Soma de ValorEstimado do dia",
                Tendencia = "Hoje",
                Cor = "#27AE60"
            });
        }

        public void CarregarTecnicos()
        {
            CarregarTecnicos(ObterTodosAgendamentosSanitizados());
        }

        private void CarregarTecnicos(List<Agendamento> todosAgendamentos)
        {
            Tecnicos.Clear();
            var funcionarios = App.Repositories.Funcionarios.ObterTodos(false).Where(f => f.Cargo?.ToLower().Contains("técnico") == true || f.Cargo?.ToLower().Contains("mecanico") == true);
            var agendamentosPorTecnico = todosAgendamentos
                .Where(a => !string.IsNullOrWhiteSpace(a.TecnicoNome))
                .GroupBy(a => a.TecnicoNome, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var funcionario in funcionarios)
            {
                if (!agendamentosPorTecnico.TryGetValue(funcionario.Nome, out var agendamentosTecnico))
                {
                    agendamentosTecnico = new List<Agendamento>();
                }

                var servicosConcluidos = agendamentosTecnico.Count(a => a.Status == "Finalizado" || a.Status == "Entregue");
                var servicosAndamento = agendamentosTecnico.Count(a => a.Status == "Em Andamento");

                Tecnicos.Add(new TecnicoAgendamento
                {
                    Id = new Guid(funcionario.Id, 0, 0, new byte[8]),
                    Nome = funcionario.Nome,
                    Foto = funcionario.Foto ?? string.Empty,
                    Especialidade = funcionario.Cargo ?? "Técnico",
                    ServicosAndamento = servicosAndamento,
                    ServicosConcluidos = servicosConcluidos,
                    Produtividade = servicosConcluidos > 0 ? (decimal)servicosConcluidos / (servicosConcluidos + servicosAndamento) * 100 : 0,
                    TempoMedio = CalculateTempoMedio(agendamentosTecnico),
                    Avaliacao = 4.5m,
                    OcupacaoDiaria = servicosAndamento,
                    Disponivel = servicosAndamento < 5,
                    EmPausa = false
                });
            }

            TecnicosAtivos = Tecnicos.Count(t => t.Disponivel);
        }

        private TimeSpan CalculateTempoMedio(List<Agendamento> agendamentos)
        {
            var comTempo = agendamentos.Where(a => a.DuracaoReal > TimeSpan.Zero).ToList();
            return comTempo.Any() 
                ? TimeSpan.FromTicks((long)comTempo.Average(a => a.DuracaoReal.Ticks)) 
                : TimeSpan.Zero;
        }

        public void CarregarVeiculos()
        {
            var agendamentosHoje = SanitizarAgendamentos(_agendamentoService.ObterAgendamentosPorData(DateTime.Today));
            CarregarVeiculos(agendamentosHoje);
        }

        private void CarregarVeiculos(List<Agendamento> agendamentosHoje)
        {
            Veiculos.Clear();

            foreach (var agendamento in agendamentosHoje.Where(a => a.Status == "Em Andamento" || a.Status == "Agendado"))
            {
                Veiculos.Add(new VeiculoAgendamento
                {
                    Id = agendamento.VeiculoId,
                    Placa = agendamento.VeiculoPlaca,
                    Modelo = agendamento.VeiculoModelo,
                    Marca = agendamento.VeiculoMarca,
                    Ano = agendamento.VeiculoAno,
                    Cor = agendamento.VeiculoCor,
                    Quilometragem = agendamento.VeiculoQuilometragem,
                    Status = agendamento.Status,
                    Entrada = agendamento.CheckIn,
                    SaidaPrevista = agendamento.HoraTermino,
                    Servicos = agendamento.Servicos.Select(s => s.Nome).ToList()
                });
            }

            VeiculosNaOficina = Veiculos.Count(v => v.Status == "Em Andamento");
        }

        public void CarregarClientes()
        {
            CarregarClientes(ObterTodosAgendamentosSanitizados());
        }

        private void CarregarClientes(List<Agendamento> todosAgendamentos)
        {
            Clientes.Clear();
            var agendamentosRecentes = FiltrarAgendamentosPorPeriodo(todosAgendamentos, DateTime.Today.AddDays(-30), DateTime.Today);
            var agendamentosRecentesPorCliente = agendamentosRecentes
                .GroupBy(a => a.ClienteId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var todosAgendamentosPorCliente = todosAgendamentos
                .GroupBy(a => a.ClienteId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var clientes = App.Repositories.Clientes.ObterTodos();

            foreach (var cliente in clientes)
            {
                if (!agendamentosRecentesPorCliente.TryGetValue(cliente.Id, out var agendamentosCliente) || !agendamentosCliente.Any())
                    continue;

                todosAgendamentosPorCliente.TryGetValue(cliente.Id, out var todosAgendamentosCliente);
                todosAgendamentosCliente ??= agendamentosCliente;

                Clientes.Add(new ClienteAgendamento
                {
                    Id = cliente.Id,
                    Nome = cliente.Nome,
                    Telefone = cliente.Telefone,
                    Email = cliente.Email,
                    Documento = cliente.Documento,
                    Vip = cliente.TotalGasto > 5000,
                    TotalGasto = cliente.TotalGasto,
                    Atendimentos = todosAgendamentosCliente.Count,
                    UltimaVisita = todosAgendamentosCliente.Any() ? todosAgendamentosCliente.Max(a => a.DataAgendamento) : null,
                    Veiculos = new List<string>(),
                    ServicosRecentes = agendamentosCliente.Take(3).Select(a => a.TipoServico).ToList()
                });
            }

            ClientesAguardando = Clientes.Count;
        }

        public void CarregarAlertas()
        {
            var agendamentosHoje = SanitizarAgendamentos(_agendamentoService.ObterAgendamentosPorData(DateTime.Today));
            CarregarAlertas(agendamentosHoje);
        }

        private void CarregarAlertas(List<Agendamento> agendamentosHoje)
        {
            Alertas.Clear();
            var agora = DateTime.Now;

            foreach (var agendamento in agendamentosHoje)
            {
                var status = agendamento.Status ?? string.Empty;
                var inicioPrevisto = CombinarDataHoraAgendamento(agendamento.DataAgendamento, agendamento.HoraInicio);
                var terminoPrevisto = CombinarDataHoraAgendamento(agendamento.DataAgendamento, agendamento.HoraTermino);

                if (inicioPrevisto.HasValue &&
                    terminoPrevisto.HasValue &&
                    terminoPrevisto.Value < inicioPrevisto.Value)
                {
                    terminoPrevisto = terminoPrevisto.Value.AddDays(1);
                }

                if ((status == "Agendado" || status == "Confirmado" || status == "Aguardando Cliente") &&
                    inicioPrevisto.HasValue &&
                    agora > inicioPrevisto.Value &&
                    !agendamento.CheckIn.HasValue)
                {
                    Alertas.Add(new AlertaAgendamento
                    {
                        Id = Guid.NewGuid(),
                        Tipo = "Atraso",
                        Mensagem = $"Cliente {agendamento.ClienteNome} com check-in atrasado no agendamento {agendamento.Numero}",
                        Severidade = "Alta",
                        DataGeracao = agora,
                        Lido = false,
                        Origem = "Agendamento",
                        AgendamentoId = agendamento.Id
                    });
                }

                if (status == "Em Andamento" &&
                    terminoPrevisto.HasValue &&
                    agora > terminoPrevisto.Value &&
                    !agendamento.CheckOut.HasValue)
                {
                    Alertas.Add(new AlertaAgendamento
                    {
                        Id = Guid.NewGuid(),
                        Tipo = "Execucao prolongada",
                        Mensagem = $"Atendimento {agendamento.Numero} extrapolou o horario previsto de oficina",
                        Severidade = "Média",
                        DataGeracao = agora,
                        Lido = false,
                        Origem = "Oficina",
                        AgendamentoId = agendamento.Id
                    });
                }

                if (agendamento.AlertaPecaFaltando)
                {
                    Alertas.Add(new AlertaAgendamento
                    {
                        Id = Guid.NewGuid(),
                        Tipo = "Peça Faltando",
                        Mensagem = $"Peça faltando para agendamento {agendamento.Numero}",
                        Severidade = "Alta",
                        DataGeracao = agora,
                        Lido = false,
                        Origem = "Estoque",
                        AgendamentoId = agendamento.Id
                    });
                }

                if (agendamento.AlertaPronto || status == "Finalizado")
                {
                    Alertas.Add(new AlertaAgendamento
                    {
                        Id = Guid.NewGuid(),
                        Tipo = "Veículo Pronto",
                        Mensagem = $"Veículo {agendamento.VeiculoPlaca} pronto para entrega",
                        Severidade = "Média",
                        DataGeracao = agora,
                        Lido = false,
                        Origem = "Agendamento",
                        AgendamentoId = agendamento.Id
                    });
                }
            }
        }

        private static DateTime? CombinarDataHoraAgendamento(DateTime dataAgendamento, DateTime? horario)
        {
            if (!horario.HasValue)
            {
                return null;
            }

            return dataAgendamento.Date.Add(horario.Value.TimeOfDay);
        }

        public void CarregarTimeline()
        {
            Timeline.Clear();
            if (AgendamentoSelecionado != null)
            {
                var agendamento = _agendamentoService.ObterAgendamentoPorId(AgendamentoSelecionado.Id);
                if (agendamento != null)
                {
                    foreach (var item in agendamento.Timeline
                        .OrderByDescending(t => t.DataHora))
                    {
                        Timeline.Add(item);
                    }
                }
            }
        }

        private void AdicionarAgendamento()
        {
            if (!ValidarPermissao("AGENDAMENTOS_CRIAR", "Voce nao possui permissao para criar agendamentos."))
            {
                return;
            }

            var novo = new Agendamento
            {
                Id = Guid.NewGuid(),
                Numero = _agendamentoService.GerarNumeroAgendamento(),
                DataCriacao = DateTime.Now,
                DataAgendamento = DataSelecionada,
                Status = "Agendado",
                Prioridade = "Normal",
                TipoServico = "Serviço Geral",
                CategoriaServico = "Geral",
                DescricaoServico = "Novo agendamento",
                ClienteId = Guid.Empty,
                ClienteNome = "Cliente não informado",
                ClienteTelefone = "",
                ClienteEmail = "",
                ClienteDocumento = "",
                ClienteVip = false,
                ClienteTotalGasto = 0,
                ClienteAtendimentos = 0,
                VeiculoId = Guid.Empty,
                VeiculoPlaca = "",
                VeiculoModelo = "",
                VeiculoMarca = "",
                VeiculoAno = "",
                VeiculoCor = "",
                VeiculoCombustivel = "",
                VeiculoQuilometragem = 0,
                VeiculoObservacoes = "",
                TecnicoId = Guid.Empty,
                TecnicoNome = "Não atribuído",
                TecnicoEspecialidade = "",
                TecnicoAtivo = true,
                DuracaoEstimada = TimeSpan.FromHours(1),
                DuracaoReal = TimeSpan.Zero,
                ValorEstimado = 0,
                ValorReal = 0,
                ValorPago = 0,
                FormaPagamento = "",
                Pago = false,
                ValorProdutos = 0,
                ValorServicos = 0,
                Recorrente = false,
                TipoRecorrencia = "",
                IntervaloRecorrencia = 0,
                LembreteWhatsApp = false,
                LembreteEmail = false,
                LembreteEnviado = false,
                AlertaAtraso = false,
                AlertaPecaFaltando = false,
                AlertaPronto = false,
                AvaliacaoCliente = 0,
                AvaliacaoComentario = "",
                Produtos = new List<AgendamentoProduto>(),
                Servicos = new List<AgendamentoServico>(),
                Timeline = new List<AgendamentoTimeline>()
            };
            _agendamentoService.AdicionarAgendamento(novo);
            AtualizarLista();
            AgendamentoSelecionado = novo;
            AdicionarTimeline(novo.Id, "Criação", "Agendamento criado", "Sistema");
        }

        private void NovoAgendamento()
        {
            AdicionarAgendamento();
        }

        private void EncaixeRapido()
        {
            if (!ValidarPermissao("AGENDAMENTOS_CRIAR", "Voce nao possui permissao para criar encaixes rapidos."))
            {
                return;
            }

            var novo = new Agendamento
            {
                Id = Guid.NewGuid(),
                Numero = _agendamentoService.GerarNumeroAgendamento(),
                DataCriacao = DateTime.Now,
                DataAgendamento = DateTime.Today,
                HoraInicio = DateTime.Now.AddMinutes(15),
                HoraTermino = DateTime.Now.AddHours(1),
                Status = "Confirmado",
                Prioridade = "Urgente"
            };
            _agendamentoService.AdicionarAgendamento(novo);
            AtualizarLista();
            AgendamentoSelecionado = novo;
            AdicionarTimeline(novo.Id, "Encaixe Rápido", "Agendamento de encaixe criado", "Sistema");
        }

        private void EditarAgendamento()
        {
            if (!ValidarPermissao("AGENDAMENTOS_EDITAR", "Voce nao possui permissao para editar agendamentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento != null)
            {
                var agendamentoId = agendamento.Id;
                _agendamentoService.AtualizarAgendamento(agendamento);
                AtualizarLista();
                AdicionarTimeline(agendamentoId, "Edição", "Agendamento editado", UsuarioLogado);
            }
        }

        private void AtualizarAgendamento()
        {
            if (!ValidarPermissao("AGENDAMENTOS_EDITAR", "Voce nao possui permissao para atualizar agendamentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento != null)
            {
                var agendamentoId = agendamento.Id;
                _agendamentoService.AtualizarAgendamento(agendamento);
                AtualizarLista();
                AdicionarTimeline(agendamentoId, "Atualização", "Agendamento atualizado", UsuarioLogado);
            }
        }

        private void RemoverAgendamento()
        {
            if (!ValidarPermissao("AGENDAMENTOS_CANCELAR", "Voce nao possui permissao para remover agendamentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento != null
                && CriticalActionDialogService.ConfirmarExclusao(
                    Application.Current?.MainWindow,
                    "agendamento",
                    agendamento.Numero,
                    $"Cliente: {agendamento.ClienteNome}\nVeiculo: {agendamento.VeiculoPlaca}\nData: {agendamento.DataAgendamento:dd/MM/yyyy}",
                    "O agendamento sera cancelado como removido pelo usuario e deixara de seguir no fluxo operacional atual."))
            {
                var agendamentoId = agendamento.Id;
                _agendamentoService.CancelarAgendamento(agendamentoId, "Removido pelo usuário");
                App.Audit.RegistrarAcaoCritica(
                    "Agendamentos",
                    "RemoverAgendamento",
                    "Agendamento",
                    agendamentoId.ToString(),
                    $"Numero={agendamento.Numero}; Cliente={agendamento.ClienteNome}");
                AtualizarLista();
                AdicionarTimeline(agendamentoId, "Remoção", "Agendamento removido", UsuarioLogado);
            }
        }

        private void CancelarAgendamento()
        {
            if (!ValidarPermissao("AGENDAMENTOS_CANCELAR", "Voce nao possui permissao para cancelar agendamentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento != null
                && CriticalActionDialogService.ConfirmarCancelamento(
                    Application.Current?.MainWindow,
                    "agendamento",
                    agendamento.Numero,
                    $"Cliente: {agendamento.ClienteNome}\nVeiculo: {agendamento.VeiculoPlaca}\nStatus atual: {agendamento.Status}",
                    "O status sera marcado como cancelado e o fluxo operacional sera interrompido para este atendimento."))
            {
                var agendamentoId = agendamento.Id;
                _agendamentoService.CancelarAgendamento(agendamentoId, "Cancelado pelo usuário");
                App.Audit.RegistrarAcaoCritica(
                    "Agendamentos",
                    "CancelarAgendamento",
                    "Agendamento",
                    agendamentoId.ToString(),
                    $"Numero={agendamento.Numero}; Cliente={agendamento.ClienteNome}");
                AtualizarLista();
                AdicionarTimeline(agendamentoId, "Cancelamento", "Agendamento cancelado", UsuarioLogado);
            }
        }

        private void ConfirmarAgendamento()
        {
            if (!ValidarPermissao("AGENDAMENTOS_EDITAR", "Voce nao possui permissao para confirmar agendamentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento == null)
            {
                return;
            }

            var agendamentoId = agendamento.Id;
            agendamento.Status = "Confirmado";
            agendamento.LembreteWhatsApp = true;
            agendamento.DataLembrete ??= agendamento.DataAgendamento.Date.AddHours(8);
            _agendamentoService.AtualizarAgendamento(agendamento);
            App.Audit.RegistrarAcaoCritica(
                "Agendamentos",
                "ConfirmarAgendamento",
                "Agendamento",
                agendamentoId.ToString(),
                $"Numero={agendamento.Numero}; Cliente={agendamento.ClienteNome}; Data={agendamento.DataAgendamento:dd/MM/yyyy}");
            AtualizarLista();
            AdicionarTimeline(agendamentoId, "Confirmacao", "Agendamento confirmado e lembrete WhatsApp programado", UsuarioLogado);
        }

        private void Reagendar()
        {
            if (!ValidarPermissao("AGENDAMENTOS_REAGENDAR", "Voce nao possui permissao para reagendar atendimentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento != null)
            {
                var agendamentoId = agendamento.Id;
                agendamento.DataAgendamentoAnterior = agendamento.DataAgendamento;
                agendamento.DataAgendamento = DataSelecionada;
                agendamento.DataReagendamento = DateTime.Now;
                _agendamentoService.AtualizarAgendamento(agendamento);
                App.Audit.RegistrarAcaoCritica(
                    "Agendamentos",
                    "Reagendar",
                    "Agendamento",
                    agendamentoId.ToString(),
                    $"Numero={agendamento.Numero}; NovaData={DataSelecionada:dd/MM/yyyy}");
                AtualizarLista();
                AdicionarTimeline(agendamentoId, "Reagendamento", $"Reagendado para {DataSelecionada:dd/MM/yyyy}", UsuarioLogado);
            }
        }

        private void Duplicar()
        {
            if (!ValidarPermissao("AGENDAMENTOS_DUPLICAR", "Voce nao possui permissao para duplicar agendamentos."))
            {
                return;
            }

            if (AgendamentoSelecionado != null)
            {
                var novo = new Agendamento
                {
                    Id = Guid.NewGuid(),
                    Numero = _agendamentoService.GerarNumeroAgendamento(),
                    DataCriacao = DateTime.Now,
                    DataAgendamento = DataSelecionada,
                    ClienteId = AgendamentoSelecionado.ClienteId,
                    ClienteNome = AgendamentoSelecionado.ClienteNome,
                    ClienteTelefone = AgendamentoSelecionado.ClienteTelefone,
                    ClienteEmail = AgendamentoSelecionado.ClienteEmail,
                    ClienteDocumento = AgendamentoSelecionado.ClienteDocumento,
                    VeiculoId = AgendamentoSelecionado.VeiculoId,
                    VeiculoPlaca = AgendamentoSelecionado.VeiculoPlaca,
                    VeiculoModelo = AgendamentoSelecionado.VeiculoModelo,
                    VeiculoMarca = AgendamentoSelecionado.VeiculoMarca,
                    VeiculoAno = AgendamentoSelecionado.VeiculoAno,
                    VeiculoCor = AgendamentoSelecionado.VeiculoCor,
                    VeiculoCombustivel = AgendamentoSelecionado.VeiculoCombustivel,
                    VeiculoQuilometragem = AgendamentoSelecionado.VeiculoQuilometragem,
                    TipoServico = AgendamentoSelecionado.TipoServico,
                    CategoriaServico = AgendamentoSelecionado.CategoriaServico,
                    DescricaoServico = AgendamentoSelecionado.DescricaoServico,
                    ValorEstimado = AgendamentoSelecionado.ValorEstimado,
                    DuracaoEstimada = AgendamentoSelecionado.DuracaoEstimada,
                    TecnicoId = AgendamentoSelecionado.TecnicoId,
                    TecnicoNome = AgendamentoSelecionado.TecnicoNome,
                    TecnicoEspecialidade = AgendamentoSelecionado.TecnicoEspecialidade,
                    Status = "Agendado",
                    Prioridade = AgendamentoSelecionado.Prioridade
                };
                _agendamentoService.AdicionarAgendamento(novo);
                AtualizarLista();
                AgendamentoSelecionado = novo;
                AdicionarTimeline(novo.Id, "Duplicação", "Agendamento duplicado", UsuarioLogado);
            }
        }

        private void ConverterEmOS()
        {
            if (!ValidarPermissao("AGENDAMENTOS_GERAR_OS", "Voce nao possui permissao para gerar OS a partir de agendamentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento != null
                && CriticalActionDialogService.ConfirmarAcao(
                    Application.Current?.MainWindow,
                    new CriticalActionRequest
                    {
                        WindowTitle = "Converter em ordem de servico",
                        Header = "Conversao de agendamento em OS",
                        Summary = $"Voce esta prestes a converter o agendamento '{agendamento.Numero}' em uma ordem de servico.",
                        Details = $"Cliente: {agendamento.ClienteNome}\nVeiculo: {agendamento.VeiculoPlaca}\nServico: {agendamento.TipoServico}",
                        Impact = "O agendamento sera movido para a execucao operacional e passara a carregar numero proprio de OS para os proximos passos do atendimento.",
                        Keyword = "CONVERTER",
                        ConfirmButtonText = "Converter em OS"
                    }))
            {
                var agendamentoId = agendamento.Id;
                var ordem = _agendamentoService.ConverterEmOrdemServico(agendamento, UsuarioLogado);
                App.Audit.RegistrarAcaoCritica(
                    "Agendamentos",
                    "ConverterEmOS",
                    "Agendamento",
                    agendamentoId.ToString(),
                    $"Numero={agendamento.Numero}; NumeroOS={ordem.Numero}; OrdemServicoId={ordem.Id}");
                ShellNotificationService.PublishNavigationHint(
                    title: "OS criada a partir do agendamento",
                    message: $"O agendamento {agendamento.Numero} foi convertido na OS {ordem.Numero}.",
                    actionModule: "OrdensServico",
                    actionLabel: "Abrir Ordens de Servico",
                    details: $"Cliente: {agendamento.ClienteNome}",
                    type: ShellNotificationType.Success,
                    source: "Agendamentos");
                AtualizarLista();
                AdicionarTimeline(agendamentoId, "Conversão OS", $"Convertido em OS {ordem.Numero}", UsuarioLogado);
            }
        }

        private void GerarOS()
        {
            ConverterEmOS();
        }

        private void CheckIn()
        {
            if (!ValidarPermissao("AGENDAMENTOS_CHECKIN", "Voce nao possui permissao para registrar check-in nos atendimentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento != null)
            {
                var agendamentoId = agendamento.Id;
                agendamento.CheckIn = DateTime.Now;
                agendamento.Status = "Em Andamento";
                agendamento.AlertaAtraso = false;
                _agendamentoService.ReservarProdutosDoAgendamento(agendamento, UsuarioLogado);
                _agendamentoService.AtualizarAgendamento(agendamento);
                App.Audit.RegistrarAcaoCritica(
                    "Agendamentos",
                    "CheckIn",
                    "Agendamento",
                    agendamentoId.ToString(),
                    $"Numero={agendamento.Numero}; Cliente={agendamento.ClienteNome}");
                AtualizarLista();
                AdicionarTimeline(agendamentoId, "Check-In", "Veículo entrou na oficina", UsuarioLogado);
            }
        }

        private void CheckOut()
        {
            if (!ValidarPermissao("AGENDAMENTOS_CHECKOUT", "Voce nao possui permissao para registrar check-out nos atendimentos."))
            {
                return;
            }

            var agendamento = AgendamentoSelecionado;
            if (agendamento != null
                && CriticalActionDialogService.ConfirmarAcao(
                    Application.Current?.MainWindow,
                    new CriticalActionRequest
                    {
                        WindowTitle = "Finalizar atendimento",
                        Header = "Check-out com integracoes",
                        Summary = $"Voce esta prestes a finalizar o agendamento '{agendamento.Numero}'.",
                        Details = $"Cliente: {agendamento.ClienteNome}\nVeiculo: {agendamento.VeiculoPlaca}\nValor estimado: {agendamento.ValorEstimado:C}",
                        Impact = "Ao confirmar, o atendimento sera finalizado e o sistema podera disparar integracoes com financeiro, estoque, PDV e orcamentos.",
                        Keyword = "FINALIZAR",
                        ConfirmButtonText = "Finalizar atendimento"
                    }))
            {
                var agendamentoId = agendamento.Id;
                agendamento.CheckOut = DateTime.Now;
                agendamento.Status = "Finalizado";
                agendamento.AlertaPronto = true;
                agendamento.DuracaoReal = agendamento.CheckIn.HasValue
                    ? agendamento.CheckOut.Value - agendamento.CheckIn.Value
                    : TimeSpan.Zero;

                var ordemVinculada = _agendamentoService.FinalizarOrdemServicoVinculada(agendamento, UsuarioLogado);
                _agendamentoService.AtualizarAgendamento(agendamento);
                if (ordemVinculada == null)
                {
                    // Integrações automáticas ao finalizar serviço sem OS vinculada.
                    IntegrarComFinanceiro(agendamento);
                    IntegrarComEstoque(agendamento);
                    IntegrarComPDV(agendamento);
                    IntegrarComOrcamentos(agendamento);
                }

                App.Audit.RegistrarAcaoCritica(
                    "Agendamentos",
                    "CheckOut",
                    "Agendamento",
                    agendamentoId.ToString(),
                    ordemVinculada == null
                        ? $"Numero={agendamento.Numero}; Cliente={agendamento.ClienteNome}; Duracao={agendamento.DuracaoReal}"
                        : $"Numero={agendamento.Numero}; Cliente={agendamento.ClienteNome}; Duracao={agendamento.DuracaoReal}; OS={ordemVinculada.Numero}");
                AtualizarLista();
                AdicionarTimeline(
                    agendamentoId,
                    "Check-Out",
                    ordemVinculada == null ? "Veículo entregue" : $"Veículo entregue e OS {ordemVinculada.Numero} finalizada",
                    UsuarioLogado);

                if (ordemVinculada != null)
                {
                    ShellNotificationService.PublishNavigationHint(
                        title: "Atendimento finalizado com OS",
                        message: $"O atendimento {agendamento.Numero} finalizou a OS {ordemVinculada.Numero}.",
                        actionModule: "OrdensServico",
                        actionLabel: "Revisar OS",
                        details: $"Cliente: {agendamento.ClienteNome}",
                        type: ShellNotificationType.Success,
                        source: "Agendamentos");
                }
            }
        }

        private void ImprimirAgenda()
        {
            if (!ValidarPermissao("AGENDAMENTOS_IMPRIMIR", "Voce nao possui permissao para imprimir a agenda."))
            {
                return;
            }

            try
            {
                var reportData = GerarRelatorioAtual();

                if (App.IsAutomatedTestMode)
                {
                    var documento = CriarDocumentoImpressao(reportData);
                    documento.PageWidth = 794;
                    documento.PageHeight = 1123;
                    documento.PagePadding = new Thickness(48);
                    documento.ColumnWidth = 698;
                    App.Logger.LogInfo("Impressao da agenda validada em automacao sem abrir dialogo de impressora.", "Agendamentos");
                    return;
                }

                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var documento = CriarDocumentoImpressao(reportData);
                    printDialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)documento).DocumentPaginator, reportData.Titulo);
                    App.Audit.RegistrarAcaoCritica(
                        "Agendamentos",
                        "AgendaImpressa",
                        "RelatorioAgenda",
                        DateTime.Now.ToString("yyyyMMddHHmmss"),
                        reportData.Titulo);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Falha ao imprimir agenda de agendamentos.", ex);
                ExibirMensagem($"Erro ao imprimir agenda: {ex.Message}", "Erro", System.Windows.MessageBoxImage.Error, ex);
            }
        }

        private void ExportarPDF()
        {
            if (!ValidarPermissao("AGENDAMENTOS_EXPORTAR", "Voce nao possui permissao para exportar a agenda."))
            {
                return;
            }

            try
            {
                if (App.IsAutomatedTestMode)
                {
                    var diretorio = ObterDiretorioAutomacao();
                    var caminhoPdf = Path.Combine(diretorio, $"Agenda_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    var reportDataAutomacao = GerarRelatorioAtual();
                    _reportService.ExportarParaPDF(reportDataAutomacao, caminhoPdf);
                    App.Audit.RegistrarAcaoCritica(
                        "Agendamentos",
                        "AgendaExportadaPdfAutomacao",
                        "RelatorioAgenda",
                        DateTime.Now.ToString("yyyyMMddHHmmss"),
                        caminhoPdf);
                    App.Logger.LogInfo($"Agenda exportada em PDF validada em automacao em '{caminhoPdf}'.", "Agendamentos");
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Arquivo PDF (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = $"Agenda_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() != true)
                {
                    return;
                }

                var reportData = GerarRelatorioAtual();
                _reportService.ExportarParaPDF(reportData, saveDialog.FileName);
                App.Audit.RegistrarAcaoCritica(
                    "Agendamentos",
                    "AgendaExportadaPdf",
                    "RelatorioAgenda",
                    DateTime.Now.ToString("yyyyMMddHHmmss"),
                    saveDialog.FileName);
                ExibirMensagem("Agenda exportada em PDF com sucesso!", "Exportar PDF", System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Falha ao exportar agenda para PDF.", ex);
                ExibirMensagem($"Erro ao exportar PDF: {ex.Message}", "Erro", System.Windows.MessageBoxImage.Error, ex);
            }
        }

        private void ExportarExcel()
        {
            if (!ValidarPermissao("AGENDAMENTOS_EXPORTAR", "Voce nao possui permissao para exportar a agenda."))
            {
                return;
            }

            try
            {
                if (App.IsAutomatedTestMode)
                {
                    var diretorio = ObterDiretorioAutomacao();
                    var caminhoCsv = Path.Combine(diretorio, $"Agenda_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                    var reportDataAutomacao = GerarRelatorioAtual();
                    var conteudoCsvAutomacao = _reportService.ExportarParaCSV(reportDataAutomacao);
                    _reportService.SalvarArquivo(conteudoCsvAutomacao, caminhoCsv);
                    App.Audit.RegistrarAcaoCritica(
                        "Agendamentos",
                        "AgendaExportadaExcelAutomacao",
                        "RelatorioAgenda",
                        DateTime.Now.ToString("yyyyMMddHHmmss"),
                        caminhoCsv);
                    App.Logger.LogInfo($"Agenda exportada em CSV validada em automacao em '{caminhoCsv}'.", "Agendamentos");
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV compativel com Excel (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"Agenda_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() != true)
                {
                    return;
                }

                var reportData = GerarRelatorioAtual();
                var conteudoCsv = _reportService.ExportarParaCSV(reportData);
                _reportService.SalvarArquivo(conteudoCsv, saveDialog.FileName);
                App.Audit.RegistrarAcaoCritica(
                    "Agendamentos",
                    "AgendaExportadaExcel",
                    "RelatorioAgenda",
                    DateTime.Now.ToString("yyyyMMddHHmmss"),
                    saveDialog.FileName);
                ExibirMensagem("Agenda exportada em CSV compativel com Excel!", "Exportar Excel", System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Falha ao exportar agenda para CSV/Excel.", ex);
                ExibirMensagem($"Erro ao exportar Excel: {ex.Message}", "Erro", System.Windows.MessageBoxImage.Error, ex);
            }
        }

        private void FiltrosRapidos()
        {
            try
            {
                // Limpa os filtros atuais para facilitar a busca
                FiltroBusca = string.Empty;
                FiltroStatus = "Todos";
                FiltroTecnico = "Todos";
                FiltroPrioridade = "Todas";
                FiltrarAgendamentos();
                ExibirMensagem("Filtros limpos! Agora você pode buscar livremente.", "Filtros", System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Erro ao aplicar filtros rápidos: {ex.Message}", "Erro", System.Windows.MessageBoxImage.Error, ex);
            }
        }

        private void ToggleModoOficina()
        {
            ModoOficina = !ModoOficina;
        }

        private void ToggleTelaCheia()
        {
            TelaCheia = !TelaCheia;
        }

        private void LimparFiltros()
        {
            FiltroBusca = string.Empty;
            FiltroStatus = "Todos";
            FiltroTecnico = "Todos";
            FiltroPrioridade = "Todas";
            FiltroCliente = string.Empty;
            FiltroVeiculo = string.Empty;
            FiltrarAgendamentos();
        }

        private void EnviarLembreteWhatsApp()
        {
            if (!ValidarPermissao("AGENDAMENTOS_COMPARTILHAR", "Voce nao possui permissao para enviar lembretes de agendamento."))
            {
                return;
            }

            if (AgendamentoSelecionado != null && AbrirWhatsApp(AgendamentoSelecionado, true))
            {
                AgendamentoSelecionado.LembreteEnviado = true;
                AgendamentoSelecionado.DataLembrete = DateTime.Now;
                _agendamentoService.AtualizarAgendamento(AgendamentoSelecionado);
                AdicionarTimeline(AgendamentoSelecionado.Id, "Lembrete WhatsApp", "Lembrete enviado via WhatsApp", UsuarioLogado);
            }
        }

        private void EnviarLembreteEmail()
        {
            if (!ValidarPermissao("AGENDAMENTOS_COMPARTILHAR", "Voce nao possui permissao para enviar lembretes de agendamento por e-mail."))
            {
                return;
            }

            if (AgendamentoSelecionado != null && AbrirEmailAgendamento(AgendamentoSelecionado, true))
            {
                AgendamentoSelecionado.LembreteEnviado = true;
                AgendamentoSelecionado.DataLembrete = DateTime.Now;
                _agendamentoService.AtualizarAgendamento(AgendamentoSelecionado);
                AdicionarTimeline(AgendamentoSelecionado.Id, "Lembrete Email", "Lembrete enviado via Email", UsuarioLogado);
            }
        }

        private void FecharDetalhes()
        {
            AgendamentoSelecionado = null;
        }

        private void EnviarWhatsApp()
        {
            if (!ValidarPermissao("AGENDAMENTOS_COMPARTILHAR", "Voce nao possui permissao para compartilhar atualizacoes do agendamento por WhatsApp."))
            {
                return;
            }

            if (AgendamentoSelecionado != null && AbrirWhatsApp(AgendamentoSelecionado, false))
            {
                AdicionarTimeline(AgendamentoSelecionado.Id, "WhatsApp", "Mensagem enviada via WhatsApp", UsuarioLogado);
            }
        }

        private static bool AbrirWhatsApp(Agendamento agendamento, bool lembrete)
        {
            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(agendamento.ClienteTelefone, out var telefone))
            {
                ExibirMensagem("Cliente sem telefone valido cadastrado.", "WhatsApp", System.Windows.MessageBoxImage.Warning);
                return false;
            }

            var mensagem = lembrete
                ? $"Ola {agendamento.ClienteNome}, este e um lembrete do seu agendamento {agendamento.Numero} para {agendamento.DataAgendamento:dd/MM/yyyy}."
                : $"Ola {agendamento.ClienteNome}, segue o acompanhamento do agendamento {agendamento.Numero} referente ao servico {agendamento.TipoServico}.";

            if (App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo(
                    $"WhatsApp validado em automacao para agendamento {agendamento.Numero} ({(lembrete ? "lembrete" : "status")}).",
                    "Agendamentos");
                return true;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://wa.me/{telefone}?text={Uri.EscapeDataString(mensagem)}",
                UseShellExecute = true
            });

            return true;
        }

        private static bool AbrirEmailAgendamento(Agendamento agendamento, bool lembrete)
        {
            var erroEmail = CadastroValidationHelper.ValidarEmail(agendamento.ClienteEmail, obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroEmail))
            {
                ExibirMensagem(erroEmail, "E-mail", System.Windows.MessageBoxImage.Warning);
                return false;
            }

            var assunto = lembrete
                ? $"Lembrete de agendamento {agendamento.Numero}"
                : $"Atualizacao do agendamento {agendamento.Numero}";

            var corpo = lembrete
                ? $"Ola {agendamento.ClienteNome}, lembramos do seu agendamento em {agendamento.DataAgendamento:dd/MM/yyyy} para o servico {agendamento.TipoServico}."
                : $"Ola {agendamento.ClienteNome}, enviamos uma atualizacao do seu agendamento {agendamento.Numero} para o veiculo {agendamento.VeiculoPlaca}.";

            if (App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo(
                    $"E-mail validado em automacao para agendamento {agendamento.Numero} ({(lembrete ? "lembrete" : "status")}).",
                    "Agendamentos");
                return true;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = $"mailto:{agendamento.ClienteEmail}?subject={Uri.EscapeDataString(assunto)}&body={Uri.EscapeDataString(corpo)}",
                UseShellExecute = true
            });

            return true;
        }

        private void GerarRelatorio()
        {
            if (!ValidarPermissao("AGENDAMENTOS_EXPORTAR", "Voce nao possui permissao para gerar relatorios operacionais da agenda."))
            {
                return;
            }

            try
            {
                if (App.IsAutomatedTestMode)
                {
                    var diretorio = ObterDiretorioAutomacao();
                    var caminhoHtml = Path.Combine(diretorio, $"Agenda_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                    var reportDataAutomacao = GerarRelatorioAtual();
                    var htmlAutomacao = _reportService.ExportarParaHTML(reportDataAutomacao);
                    _reportService.SalvarArquivo(htmlAutomacao, caminhoHtml);
                    App.Logger.LogInfo($"Relatorio operacional HTML validado em automacao em '{caminhoHtml}'.", "Agendamentos");
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Relatorio HTML (*.html)|*.html",
                    DefaultExt = "html",
                    FileName = $"Agenda_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() != true)
                {
                    return;
                }

                var reportData = GerarRelatorioAtual();
                var html = _reportService.ExportarParaHTML(reportData);
                _reportService.SalvarArquivo(html, saveDialog.FileName);
                ExibirMensagem("Relatorio operacional gerado com sucesso!", "Relatorio", System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Falha ao gerar relatorio HTML de agendamentos.", ex);
                ExibirMensagem($"Erro ao gerar relatorio: {ex.Message}", "Erro", System.Windows.MessageBoxImage.Error, ex);
            }
        }

        private static string ObterDiretorioAutomacao()
        {
            var diretorio = Path.Combine(App.RuntimeLogDirectory, "agendamentos-smoke");
            Directory.CreateDirectory(diretorio);
            return diretorio;
        }

        private static void ExibirMensagem(string mensagem, string titulo, System.Windows.MessageBoxImage imagem, Exception? ex = null)
        {
            if (App.IsAutomatedTestMode)
            {
                var texto = $"{titulo}: {mensagem}";
                if (imagem == System.Windows.MessageBoxImage.Error)
                {
                    App.Logger.LogError(texto, ex, "Agendamentos");
                }
                else if (imagem == System.Windows.MessageBoxImage.Warning)
                {
                    App.Logger.LogWarning(texto, "Agendamentos");
                }
                else
                {
                    App.Logger.LogInfo(texto, "Agendamentos");
                }

                return;
            }

            System.Windows.MessageBox.Show(mensagem, titulo, System.Windows.MessageBoxButton.OK, imagem);
        }

        private ReportData GerarRelatorioAtual()
        {
            var visualizacao = VisualizacaoCalendario ?? string.Empty;

            if (visualizacao.StartsWith("Sem", StringComparison.OrdinalIgnoreCase))
            {
                var inicioSemana = DataSelecionada.Date.AddDays(-(int)DataSelecionada.DayOfWeek + (DataSelecionada.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
                var fimSemana = inicioSemana.AddDays(6);
                return _reportService.GerarRelatorioSemanal(inicioSemana, fimSemana);
            }

            if (visualizacao.StartsWith("Men", StringComparison.OrdinalIgnoreCase))
            {
                return _reportService.GerarRelatorioMensal(DataSelecionada.Year, DataSelecionada.Month);
            }

            if (visualizacao.StartsWith("Tim", StringComparison.OrdinalIgnoreCase))
            {
                return _reportService.GerarRelatorioSemanal(DataInicioPeriodo.Date, DataFimPeriodo.Date);
            }

            return _reportService.GerarRelatorioDiario(DataSelecionada.Date);
        }

        private System.Windows.Documents.FlowDocument CriarDocumentoImpressao(ReportData reportData)
        {
            var documento = new System.Windows.Documents.FlowDocument
            {
                PagePadding = new System.Windows.Thickness(40),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12
            };

            documento.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(reportData.Titulo))
            {
                FontSize = 20,
                FontWeight = System.Windows.FontWeights.Bold
            });

            documento.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(
                $"Periodo: {reportData.Periodo}\nGerado em: {reportData.DataGeracao:dd/MM/yyyy HH:mm}")));

            documento.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(
                $"Total: {reportData.TotalAgendamentos} | Concluidos: {reportData.AgendamentosConcluidos} | Pendentes: {reportData.AgendamentosPendentes} | Em andamento: {reportData.AgendamentosEmAndamento}")));

            var tabela = new System.Windows.Documents.Table();
            tabela.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(90) });
            tabela.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(180) });
            tabela.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(150) });
            tabela.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(120) });

            var grupo = new System.Windows.Documents.TableRowGroup();
            tabela.RowGroups.Add(grupo);

            var cabecalho = new System.Windows.Documents.TableRow();
            cabecalho.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Numero"))) { FontWeight = System.Windows.FontWeights.Bold });
            cabecalho.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Cliente"))) { FontWeight = System.Windows.FontWeights.Bold });
            cabecalho.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Servico"))) { FontWeight = System.Windows.FontWeights.Bold });
            cabecalho.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Status"))) { FontWeight = System.Windows.FontWeights.Bold });
            grupo.Rows.Add(cabecalho);

            foreach (var agendamento in reportData.Agendamentos.Take(30))
            {
                var linha = new System.Windows.Documents.TableRow();
                linha.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(agendamento.Numero))));
                linha.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(agendamento.ClienteNome))));
                linha.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(agendamento.TipoServico))));
                linha.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(agendamento.Status))));
                grupo.Rows.Add(linha);
            }

            documento.Blocks.Add(tabela);
            return documento;
        }

        private void AdicionarTimeline(Guid agendamentoId, string acao, string detalhes, string usuario)
        {
            var timelineItem = new AgendamentoTimeline
            {
                Id = Guid.NewGuid(),
                DataHora = DateTime.Now,
                Usuario = usuario,
                Acao = acao,
                Detalhes = detalhes,
                TipoAlteracao = "Manual"
            };
            // Adicionar ao banco de dados
            _agendamentoService.AdicionarAgendamentoTimeline(timelineItem, agendamentoId);

            // Atualizar o agendamento selecionado se necessário
            if (AgendamentoSelecionado != null && AgendamentoSelecionado.Id == agendamentoId)
            {
                if (AgendamentoSelecionado.Timeline == null)
                    AgendamentoSelecionado.Timeline = new System.Collections.Generic.List<AgendamentoTimeline>();
                AgendamentoSelecionado.Timeline.Insert(0, timelineItem);
            }
        }

        // Integrações com outros módulos
        public Cliente? ObterClientePorId(Guid clienteId)
        {
            return App.Repositories.Clientes.ObterPorId(clienteId);
        }

        public Produto? ObterProdutoPorId(Guid produtoId)
        {
            return App.Repositories.Produtos.ObterPorId(produtoId);
        }

        public void IntegrarComFinanceiro(Agendamento agendamento)
        {
            try
            {
                _financeiroDatabaseService.RegistrarReceitaAgendamento(agendamento);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Falha na integracao do agendamento '{agendamento.Id}' com o financeiro.", ex);
            }
        }

        public void IntegrarComEstoque(Agendamento agendamento)
        {
            try
            {
                const string tipoIntegracao = "Estoque";
                if (_agendamentoService.IntegracaoExecutada(agendamento.Id, tipoIntegracao))
                {
                    return;
                }

                var produtos = ObterProdutosOperacionais(agendamento);
                if (produtos.Count == 0)
                {
                    _agendamentoService.RegistrarIntegracao(agendamento.Id, tipoIntegracao, "Sem produtos para baixar.");
                    return;
                }

                BaixarProdutosDoEstoque(produtos, agendamento.Id);
                _agendamentoService.RegistrarIntegracao(agendamento.Id, tipoIntegracao, $"{produtos.Count} produto(s) baixado(s).");
                Logger.LogInfo($"Estoque integrado para o agendamento '{agendamento.Id}' com {produtos.Count} produto(s).");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Falha na integracao do agendamento '{agendamento.Id}' com o estoque.", ex);
            }
        }

        public void IntegrarComPDV(Agendamento agendamento)
        {
            try
            {
                const string tipoIntegracao = "PDV";
                if (_agendamentoService.IntegracaoExecutada(agendamento.Id, tipoIntegracao))
                {
                    return;
                }

                var produtos = ObterProdutosOperacionais(agendamento);
                if (produtos.Count == 0)
                {
                    _agendamentoService.RegistrarIntegracao(agendamento.Id, tipoIntegracao, "Sem produtos para registrar como venda.");
                    return;
                }

                var venda = CriarVendaDoAgendamento(agendamento, produtos);
                if (venda.Itens.Count == 0 || venda.Total <= 0)
                {
                    _agendamentoService.RegistrarIntegracao(agendamento.Id, tipoIntegracao, "Venda nao gerada por falta de itens validos.");
                    return;
                }

                _vendaService.RegistrarVenda(venda, atualizarEstoque: false);
                _agendamentoService.RegistrarIntegracao(agendamento.Id, tipoIntegracao, $"Venda {venda.Id} registrada no PDV.");
                Logger.LogInfo($"Agendamento '{agendamento.Id}' registrado no PDV como venda '{venda.Id}'.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Falha na integracao do agendamento '{agendamento.Id}' com o PDV.", ex);
            }
        }

        public void IntegrarComOrcamentos(Agendamento agendamento)
        {
            try
            {
                const string tipoIntegracao = "Orcamentos";
                if (_agendamentoService.IntegracaoExecutada(agendamento.Id, tipoIntegracao))
                {
                    return;
                }

                var orcamento = CriarOrcamentoOperacionalDoAgendamento(agendamento);
                if (orcamento == null)
                {
                    _agendamentoService.RegistrarIntegracao(
                        agendamento.Id,
                        tipoIntegracao,
                        "Sem itens ou valor operacional para gerar orcamento.");
                    return;
                }

                var orcamentoService = new OrcamentoDatabaseService();
                orcamentoService.AdicionarOrcamento(orcamento);
                _agendamentoService.RegistrarIntegracao(
                    agendamento.Id,
                    tipoIntegracao,
                    $"Orcamento {orcamento.Numero} gerado a partir do agendamento.");
                ShellNotificationService.PublishNavigationHint(
                    title: "Orcamento gerado pelo agendamento",
                    message: $"O agendamento {agendamento.Numero} gerou o orcamento {orcamento.Numero}.",
                    actionModule: "Orcamentos",
                    actionLabel: "Abrir Orcamentos",
                    details: $"Cliente: {agendamento.ClienteNome}",
                    type: ShellNotificationType.Success,
                    source: "Agendamentos");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Falha na integracao do agendamento '{agendamento.Id}' com orcamentos.", ex);
            }
        }

        private Orcamento? CriarOrcamentoOperacionalDoAgendamento(Agendamento agendamento)
        {
            var orcamentoId = Guid.NewGuid();
            var itens = new List<OrcamentoItem>();

            foreach (var produtoAgendamento in ObterProdutosOperacionais(agendamento))
            {
                var produto = ObterProdutoPorId(produtoAgendamento.ProdutoId);
                var precoUnitario = produtoAgendamento.PrecoUnitario > 0
                    ? produtoAgendamento.PrecoUnitario
                    : produto?.PrecoVenda ?? 0m;
                var precoCusto = produto?.PrecoCompra ?? 0m;

                itens.Add(new OrcamentoItem
                {
                    Id = Guid.NewGuid(),
                    OrcamentoId = orcamentoId,
                    ProdutoId = produtoAgendamento.ProdutoId,
                    Tipo = "Produto",
                    ProdutoNome = string.IsNullOrWhiteSpace(produtoAgendamento.ProdutoNome)
                        ? produto?.Nome ?? "Produto do agendamento"
                        : produtoAgendamento.ProdutoNome,
                    ProdutoCodigo = string.IsNullOrWhiteSpace(produtoAgendamento.ProdutoCodigo)
                        ? produto?.Codigo ?? string.Empty
                        : produtoAgendamento.ProdutoCodigo,
                    ProdutoCategoria = produto?.Categoria ?? "Pecas",
                    ProdutoMarca = produto?.Marca ?? string.Empty,
                    ProdutoAplicacao = agendamento.TipoServico,
                    Quantidade = Math.Max(1, produtoAgendamento.Quantidade),
                    PrecoUnitario = precoUnitario,
                    PrecoCusto = precoCusto,
                    EstoqueDisponivel = produto?.QuantidadeEstoque ?? 0,
                    Observacoes = $"Item gerado pelo agendamento {agendamento.Numero}."
                });
            }

            foreach (var servicoAgendamento in ObterServicosOperacionais(agendamento))
            {
                var valor = servicoAgendamento.Valor > 0 ? servicoAgendamento.Valor : agendamento.ValorServicos;
                itens.Add(new OrcamentoItem
                {
                    Id = Guid.NewGuid(),
                    OrcamentoId = orcamentoId,
                    Tipo = "Servico",
                    ProdutoNome = string.IsNullOrWhiteSpace(servicoAgendamento.Nome)
                        ? agendamento.TipoServico
                        : servicoAgendamento.Nome,
                    ProdutoCodigo = "SERVICO",
                    ProdutoCategoria = string.IsNullOrWhiteSpace(servicoAgendamento.Categoria)
                        ? "Mao de obra"
                        : servicoAgendamento.Categoria,
                    ProdutoMarca = "Oficina",
                    ProdutoAplicacao = agendamento.DescricaoServico,
                    Quantidade = 1,
                    PrecoUnitario = Math.Max(0m, valor),
                    PrecoCusto = 0m,
                    Observacoes = $"Servico gerado pelo agendamento {agendamento.Numero}."
                });
            }

            if (itens.Count == 0)
            {
                var valorReferencia = agendamento.ValorReal > 0 ? agendamento.ValorReal : agendamento.ValorEstimado;
                if (valorReferencia <= 0)
                {
                    return null;
                }

                itens.Add(new OrcamentoItem
                {
                    Id = Guid.NewGuid(),
                    OrcamentoId = orcamentoId,
                    Tipo = "Servico",
                    ProdutoNome = string.IsNullOrWhiteSpace(agendamento.TipoServico)
                        ? "Atendimento de oficina"
                        : agendamento.TipoServico,
                    ProdutoCodigo = "SERVICO",
                    ProdutoCategoria = string.IsNullOrWhiteSpace(agendamento.CategoriaServico)
                        ? "Mao de obra"
                        : agendamento.CategoriaServico,
                    ProdutoMarca = "Oficina",
                    ProdutoAplicacao = agendamento.DescricaoServico,
                    Quantidade = 1,
                    PrecoUnitario = valorReferencia,
                    PrecoCusto = 0m,
                    Observacoes = $"Item sintetizado pelo agendamento {agendamento.Numero}."
                });
            }

            var cliente = agendamento.ClienteId == Guid.Empty ? null : ObterClientePorId(agendamento.ClienteId);
            var veiculo = cliente?.Veiculos.FirstOrDefault(v => v.Id == agendamento.VeiculoId);

            return new Orcamento
            {
                Id = orcamentoId,
                ClienteId = agendamento.ClienteId == Guid.Empty ? null : agendamento.ClienteId,
                VeiculoId = agendamento.VeiculoId == Guid.Empty ? null : agendamento.VeiculoId,
                Numero = new OrcamentoDatabaseService().GerarNumeroOrcamento(),
                Status = "Convertido em Venda",
                DataCriacao = agendamento.DataCriacao == default ? DateTime.Now : agendamento.DataCriacao,
                DataValidade = (agendamento.CheckOut ?? DateTime.Now).Date.AddDays(7),
                DataAprovacao = agendamento.CheckOut ?? DateTime.Now,
                DataConversaoVenda = agendamento.CheckOut ?? DateTime.Now,
                Observacoes = $"Orcamento operacional gerado automaticamente a partir do agendamento {agendamento.Numero}.",
                Diagnostico = string.IsNullOrWhiteSpace(agendamento.Observacoes)
                    ? $"Atendimento concluido a partir do agendamento {agendamento.Numero}."
                    : agendamento.Observacoes,
                DescontoTipo = "Valor",
                CondicoesPagamento = string.IsNullOrWhiteSpace(agendamento.FormaPagamento) ? "A definir" : agendamento.FormaPagamento,
                PrazoEntrega = "Atendimento realizado",
                Itens = itens,
                Cliente = cliente,
                Veiculo = veiculo
            };
        }

        private List<AgendamentoProduto> ObterProdutosOperacionais(Agendamento agendamento)
        {
            if (agendamento.Produtos != null && agendamento.Produtos.Count > 0)
            {
                return agendamento.Produtos.Where(p => p.Quantidade > 0).ToList();
            }

            return _agendamentoService
                .ObterProdutosDoAgendamento(agendamento.Id)
                .Where(p => p.Quantidade > 0)
                .ToList();
        }

        private List<AgendamentoServico> ObterServicosOperacionais(Agendamento agendamento)
        {
            if (agendamento.Servicos != null && agendamento.Servicos.Count > 0)
            {
                return agendamento.Servicos
                    .Where(s => s.Valor > 0 || !string.IsNullOrWhiteSpace(s.Nome))
                    .ToList();
            }

            return _agendamentoService
                .ObterServicosDoAgendamento(agendamento.Id)
                .Where(s => s.Valor > 0 || !string.IsNullOrWhiteSpace(s.Nome))
                .ToList();
        }

        private void BaixarProdutosDoEstoque(List<AgendamentoProduto> produtos, Guid agendamentoId)
        {
            App.Repositories.Produtos.BaixarProdutosDoEstoquePorAgendamento(produtos, agendamentoId);
        }

        private Venda CriarVendaDoAgendamento(Agendamento agendamento, List<AgendamentoProduto> produtos)
        {
            var venda = new Venda
            {
                Data = agendamento.CheckOut ?? DateTime.Now,
                Cliente = new Cliente
                {
                    Id = agendamento.ClienteId,
                    Nome = string.IsNullOrWhiteSpace(agendamento.ClienteNome) ? "Cliente nao informado" : agendamento.ClienteNome,
                    Telefone = agendamento.ClienteTelefone,
                    Email = agendamento.ClienteEmail,
                    CPF = agendamento.ClienteDocumento
                },
                FormaPagamento = string.IsNullOrWhiteSpace(agendamento.FormaPagamento) ? "A definir" : agendamento.FormaPagamento,
                Usuario = UsuarioLogado
            };

            foreach (var produtoAgendamento in produtos)
            {
                var produto = ObterProdutoPorId(produtoAgendamento.ProdutoId);
                if (produto == null)
                {
                    throw new InvalidOperationException($"Produto '{produtoAgendamento.ProdutoNome}' nao encontrado para registrar venda.");
                }

                venda.Itens.Add(new ItemVenda
                {
                    Produto = produto,
                    Quantidade = produtoAgendamento.Quantidade,
                    PrecoUnitario = produtoAgendamento.PrecoUnitario,
                    Desconto = 0
                });
            }

            venda.Total = venda.Itens.Sum(i => i.Subtotal);
            return venda;
        }

        public void CarregarAgendamentos()
        {
            AtualizarLista();
            CarregarAgendamentosPorData();
        }

        public void CarregarAgendamentosPorData(DateTime data)
        {
            _dataSelecionada = data.Date;
            OnPropertyChanged(nameof(DataSelecionada));
            CarregarAgendamentosPorData();
        }

        public void CarregarAgendamentosPorPeriodo(DateTime inicio, DateTime fim)
        {
            _dataInicioPeriodo = inicio.Date;
            _dataFimPeriodo = fim.Date;
            OnPropertyChanged(nameof(DataInicioPeriodo));
            OnPropertyChanged(nameof(DataFimPeriodo));
            CarregarAgendamentosPorPeriodo();
            FiltrarAgendamentos();
        }

        public void RecarregarVisualizacaoAtual()
        {
            AtualizarVisualizacao();
        }

        public void BuscarAgendamentos()
        {
            FiltrarAgendamentos();
        }

        public void AtualizarDados()
        {
            AtualizarLista();
            CarregarTecnicos();
            CarregarVeiculos();
            CarregarClientes();
            CarregarAlertas();
            CarregarTimeline();
            AtualizarHora();
        }

        public void ReagendarAgendamento(Agendamento agendamento, DateTime novaData)
        {
            AgendamentoSelecionado = agendamento;
            DataSelecionada = novaData.Date;
            Reagendar();
        }

        public void CarregarAgendamentosAtrasados()
        {
            var atrasados = _agendamentoService
                .ObterTodosAgendamentos()
                .Where(a => a.Status == "Agendado" && a.DataAgendamento.Date < DateTime.Today)
                .ToList();

            Agendamentos.Clear();
            foreach (var agendamento in atrasados)
            {
                Agendamentos.Add(agendamento);
            }

            FiltrarAgendamentos();
        }

        public void BuscarSugestoes(string termo)
        {
            if (string.IsNullOrWhiteSpace(termo))
            {
                return;
            }

            FiltroBusca = termo;
        }
    }
}


