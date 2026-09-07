using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.ViewModels
{
    /// <summary>
    /// ViewModel moderno para relatórios com suporte a exportação PDF/Excel
    /// Implementa pattern MVVM com ObservableObject do MVVM Toolkit
    /// </summary>
    public partial class RelatoriosModernoViewModel : BaseViewModel
    {
        private readonly RelatorioDatabaseService _relatorioDatabaseService;
        private readonly RelatorioExportService _exportService;
        private readonly LoggerService _loggerService;

        // Observables para dados de relatório
        [ObservableProperty]
        private ObservableCollection<DadoFinanceiro> dadosFinanceiros;

        [ObservableProperty]
        private ObservableCollection<DadoVenda> dadosVendas;

        [ObservableProperty]
        private ObservableCollection<DadoOrdemServicoRelatorio> ordensAbertas;

        [ObservableProperty]
        private ObservableCollection<DadoOrdemServicoRelatorio> ordensFinalizadas;

        [ObservableProperty]
        private ObservableCollection<DadoOrdemServicoTecnico> ordensPorTecnico;

        [ObservableProperty]
        private ObservableCollection<DadoServicoRelatorio> servicosMaisRealizados;

        [ObservableProperty]
        private ObservableCollection<DadoServicoRelatorio> lucroPorServico;

        // Estados de carregamento e filtro
        [ObservableProperty]
        private bool isCarregando;

        [ObservableProperty]
        private string mensagemStatus;

        [ObservableProperty]
        private string mensagemErro;

        [ObservableProperty]
        private DateTime dataInicio;

        [ObservableProperty]
        private DateTime dataFim;

        [ObservableProperty]
        private string tipoRelatorioSelecionado;

        [ObservableProperty]
        private bool podeFazerExportacao;

        // Resumo financeiro
        [ObservableProperty]
        private decimal faturamentoTotal;

        [ObservableProperty]
        private decimal despesasTotal;

        [ObservableProperty]
        private decimal lucroLiquido;

        [ObservableProperty]
        private decimal ticketMedio;

        [ObservableProperty]
        private int totalVendas;

        [ObservableProperty]
        private int totalOrdenAbertas;

        [ObservableProperty]
        private int totalOrdenFinalizadas;

        public RelatoriosModernoViewModel()
        {
            _relatorioDatabaseService = new RelatorioDatabaseService();
            _exportService = new RelatorioExportService();
            _loggerService = App.Logger ?? new LoggerService();

            // Inicializar collections
            DadosFinanceiros = new ObservableCollection<DadoFinanceiro>();
            DadosVendas = new ObservableCollection<DadoVenda>();
            OrdensAbertas = new ObservableCollection<DadoOrdemServicoRelatorio>();
            OrdensFinalizadas = new ObservableCollection<DadoOrdemServicoRelatorio>();
            OrdensPorTecnico = new ObservableCollection<DadoOrdemServicoTecnico>();
            ServicosMaisRealizados = new ObservableCollection<DadoServicoRelatorio>();
            LucroPorServico = new ObservableCollection<DadoServicoRelatorio>();

            // Inicializar datas (mês atual)
            DataFim = DateTime.Now;
            DataInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            TipoRelatorioSelecionado = "Completo";
            MensagemStatus = "Pronto para carregar dados";
            PodeFazerExportacao = false;
        }

        /// <summary>
        /// Carrega todos os dados de relatório conforme período e tipo selecionado
        /// </summary>
        [RelayCommand]
        public async Task CarregarDadosAsync()
        {
            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;
                MensagemStatus = "Carregando dados...";
                PodeFazerExportacao = false;

                // Limpar dados anteriores
                DadosFinanceiros.Clear();
                DadosVendas.Clear();
                OrdensAbertas.Clear();
                OrdensFinalizadas.Clear();
                OrdensPorTecnico.Clear();
                ServicosMaisRealizados.Clear();
                LucroPorServico.Clear();

                // Carregar dados em paralelo
                await Task.WhenAll(
                    CarregarFinanceiroAsync(),
                    CarregarVendasAsync(),
                    CarregarOrdensServicoAsync(),
                    CarregarServicosMaisRealizadosAsync()
                );

                // Calcular resumos
                AtualizarResumos();

                MensagemStatus = $"Dados carregados com sucesso - {DadosFinanceiros.Count} movimentações";
                PodeFazerExportacao = true;
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao carregar dados: {ex.Message}";
                _loggerService?.LogError("Erro em RelatoriosModernoViewModel.CarregarDados", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        /// <summary>
        /// Exporta relatório em formato PDF
        /// </summary>
        [RelayCommand]
        public async Task ExportarPdfAsync()
        {
            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;
                MensagemStatus = "Gerando PDF...";

                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var nomeArquivo = $"Relatorio_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var caminhoCompleto = Path.Combine(desktop, nomeArquivo);

                await Task.Run(() =>
                {
                    _exportService.ExportarParaPDF(
                        DadosFinanceiros.ToList(),
                        DadosVendas.ToList(),
                        caminhoCompleto,
                        OrdensAbertas.ToList(),
                        OrdensFinalizadas.ToList(),
                        OrdensPorTecnico.ToList(),
                        ServicosMaisRealizados.ToList(),
                        LucroPorServico.ToList()
                    );
                });

                MensagemStatus = $"PDF exportado com sucesso: {nomeArquivo}";
                _loggerService?.LogInfo($"PDF exportado: {caminhoCompleto}");
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao exportar PDF: {ex.Message}";
                _loggerService?.LogError("Erro em RelatoriosModernoViewModel.ExportarPdf", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        /// <summary>
        /// Exporta relatório em formato Excel
        /// </summary>
        [RelayCommand]
        public async Task ExportarExcelAsync()
        {
            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;
                MensagemStatus = "Gerando Excel...";

                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var nomeArquivo = $"Relatorio_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var caminhoCompleto = Path.Combine(desktop, nomeArquivo);

                await Task.Run(() =>
                {
                    _exportService.ExportarParaExcel(
                        DadosFinanceiros.ToList(),
                        DadosVendas.ToList(),
                        caminhoCompleto,
                        OrdensAbertas.ToList(),
                        OrdensFinalizadas.ToList(),
                        OrdensPorTecnico.ToList(),
                        ServicosMaisRealizados.ToList(),
                        LucroPorServico.ToList()
                    );
                });

                MensagemStatus = $"Excel exportado com sucesso: {nomeArquivo}";
                _loggerService?.LogInfo($"Excel exportado: {caminhoCompleto}");
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao exportar Excel: {ex.Message}";
                _loggerService?.LogError("Erro em RelatoriosModernoViewModel.ExportarExcel", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        /// <summary>
        /// Altera o período do relatório
        /// </summary>
        [RelayCommand]
        public void AlterarPeriodo(string periodo)
        {
            try
            {
                DataFim = DateTime.Now;

                DataInicio = periodo switch
                {
                    "Hoje" => DateTime.Now.Date,
                    "Semana" => DateTime.Now.AddDays(-7),
                    "Mês" => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    "Trimestre" => DateTime.Now.AddMonths(-3),
                    "Ano" => new DateTime(DateTime.Now.Year, 1, 1),
                    _ => DataInicio
                };

                MensagemStatus = $"Período alterado para {periodo}";
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao alterar período: {ex.Message}";
            }
        }

        // Métodos privados de carregamento
        private async Task CarregarFinanceiroAsync()
        {
            try
            {
                var dados = await Task.Run(() =>
                    _relatorioDatabaseService.ObterDadosFinanceiros(DataInicio, DataFim)
                );

                foreach (var dado in dados)
                {
                    DadosFinanceiros.Add(dado);
                }
            }
            catch (Exception ex)
            {
                _loggerService?.LogError("Erro ao carregar dados financeiros", ex);
            }
        }

        private async Task CarregarVendasAsync()
        {
            try
            {
                var dados = await Task.Run(() =>
                    _relatorioDatabaseService.ObterDadosVendas(DataInicio, DataFim)
                );

                foreach (var dado in dados)
                {
                    DadosVendas.Add(dado);
                }
            }
            catch (Exception ex)
            {
                _loggerService?.LogError("Erro ao carregar dados de vendas", ex);
            }
        }

        private async Task CarregarOrdensServicoAsync()
        {
            try
            {
                // TODO: Implementar quando métodos forem adicionados ao RelatorioDatabaseService
                /*
                var abertas = await Task.Run(() =>
                    _relatorioDatabaseService.ObterOrdensServicoAbertas()
                );

                var finalizadas = await Task.Run(() =>
                    _relatorioDatabaseService.ObterOrdensServicoFinalizadas(DataInicio, DataFim)
                );

                var porTecnico = await Task.Run(() =>
                    _relatorioDatabaseService.ObterOrdensServicoPorTecnico(DataInicio, DataFim)
                );

                foreach (var ordem in abertas)
                    OrdensAbertas.Add(ordem);

                foreach (var ordem in finalizadas)
                    OrdensFinalizadas.Add(ordem);

                foreach (var ordem in porTecnico)
                    OrdensPorTecnico.Add(ordem);
                */
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _loggerService?.LogError("Erro ao carregar ordens de serviço", ex);
            }
        }

        private async Task CarregarServicosMaisRealizadosAsync()
        {
            try
            {
                // TODO: Implementar quando métodos forem adicionados ao RelatorioDatabaseService
                /*
                var servicos = await Task.Run(() =>
                    _relatorioDatabaseService.ObterServicosMaisRealizados(DataInicio, DataFim, 10)
                );

                var lucros = await Task.Run(() =>
                    _relatorioDatabaseService.ObterServicosPorLucro(DataInicio, DataFim, 10)
                );

                foreach (var servico in servicos)
                    ServicosMaisRealizados.Add(servico);

                foreach (var servico in lucros)
                    LucroPorServico.Add(servico);
                */
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _loggerService?.LogError("Erro ao carregar serviços", ex);
            }
        }

        private void AtualizarResumos()
        {
            try
            {
                // Calcular resumos financeiros
                FaturamentoTotal = DadosFinanceiros
                    .Where(d => d.Tipo == "Receita")
                    .Sum(d => d.Valor);

                DespesasTotal = DadosFinanceiros
                    .Where(d => d.Tipo == "Despesa")
                    .Sum(d => d.Valor);

                LucroLiquido = FaturamentoTotal - DespesasTotal;

                // Resumos de vendas
                TotalVendas = DadosVendas.Count;
                TicketMedio = TotalVendas > 0 
                    ? DadosVendas.Sum(v => v.ValorTotal) / TotalVendas 
                    : 0;

                // Resumos de ordens
                TotalOrdenAbertas = OrdensAbertas.Count;
                TotalOrdenFinalizadas = OrdensFinalizadas.Count;
            }
            catch (Exception ex)
            {
                _loggerService?.LogError("Erro ao atualizar resumos", ex);
            }
        }
    }
}
