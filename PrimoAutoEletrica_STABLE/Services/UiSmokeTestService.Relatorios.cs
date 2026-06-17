using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks de relatorios e helpers de espera/arquivos gerados.

        private void RunRelatoriosOperationalChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Relatorios:CarregamentoAssincrono", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao sinalizou carga concluida.");

                if (string.IsNullOrWhiteSpace(control.ViewModel.ResumoAuditoria))
                {
                    throw new InvalidOperationException("A consulta operacional de auditoria nao retornou resumo visivel.");
                }
            });

            RunCheck(result, "Relatorios:AuditoriaPaginada", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao concluiu a carga inicial antes do filtro operacional.");

                control.ViewModel.StatusAuditoriaSelecionado = "Todos";
                control.ViewModel.CategoriaAuditoriaSelecionada = "Todas";
                control.ViewModel.TermoAuditoriaFiltro = "PDV";
                var filtroTask = control.ViewModel.AplicarFiltrosAuditoriaAsync();
                WaitForCondition(
                    () => filtroTask.IsCompleted && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "A consulta paginada de auditoria nao concluiu dentro do tempo esperado.");

                if (control.ViewModel.PaginaAuditoriaAtual < 1)
                {
                    throw new InvalidOperationException("A pagina atual da auditoria ficou invalida apos aplicar filtros.");
                }
            });

            RunCheck(result, "Relatorios:ConsistenciaOperacional", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao concluiu a carga inicial antes de expor a consistencia operacional.");

                if (string.IsNullOrWhiteSpace(control.ViewModel.ResumoConsistenciaOperacional))
                {
                    throw new InvalidOperationException("O resumo de consistencia operacional nao ficou visivel no workspace de relatorios.");
                }
            });

            RunCheck(result, "Relatorios:IndicadoresOperacionaisGerados", () =>
            {
                GarantirBancoIsoladoDoSmoke("geracao dos relatorios operacionais");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica nao foi preparada para os relatorios.");
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

                var venda = new Venda
                {
                    Id = Guid.NewGuid(),
                    Data = DateTime.Now.AddMinutes(-1),
                    Cliente = fixture.Cliente,
                    FormaPagamento = "PIX",
                    Status = "Concluida",
                    Usuario = fixture.Administrator.Nome,
                    Total = (fixture.Produto.PrecoVenda * 2) + 140m,
                    Itens = new List<ItemVenda>
                    {
                        new()
                        {
                            Produto = fixture.Produto,
                            ProdutoId = fixture.Produto.Id,
                            Tipo = "Produto",
                            Descricao = fixture.Produto.Nome,
                            Quantidade = 2,
                            PrecoUnitario = fixture.Produto.PrecoVenda,
                            CustoUnitario = fixture.Produto.PrecoCompra
                        },
                        new()
                        {
                            Tipo = "Servico",
                            Descricao = $"Diagnostico avancado relatorios {token}",
                            Quantidade = 1,
                            PrecoUnitario = 140m,
                            CustoUnitario = 20m
                        }
                    }
                };
                new VendaService(App.Database).RegistrarVenda(venda, atualizarEstoque: false);

                _ = CreatePersistedOrdemServico(fixture.Cliente, fixture.Veiculo, fixture.Produto);
                var ordemFinalizada = CreatePersistedOrdemServico(fixture.Cliente, fixture.Veiculo, fixture.Produto);
                ordemFinalizada.TecnicoId = fixture.Administrator.Id;
                ordemFinalizada.Status = "Entregue";
                ordemFinalizada.DataInicio = DateTime.Now.AddHours(-3);
                ordemFinalizada.DataConclusao = DateTime.Now.AddHours(-1);
                ordemFinalizada.DataEntrega = DateTime.Now;
                ordemFinalizada.TempoRealMinutos = 120;
                ordemFinalizada.ValorMaoObra = 60m;
                App.Repositories.OrdensServico.Atualizar(ordemFinalizada);

                var financeiro = new FinanceiroDatabaseService();
                financeiro.AdicionarMovimentacao(
                    "Entrada",
                    $"Entrada relatorios smoke {token}",
                    venda.Total,
                    DateTime.Today,
                    "Smoke",
                    "PIX",
                    origem: "SmokeRelatorios",
                    referenciaExterna: $"entrada-relatorios-{token}");
                financeiro.AdicionarMovimentacao(
                    "Saida",
                    $"Saida relatorios smoke {token}",
                    fixture.Produto.PrecoCompra,
                    DateTime.Today,
                    "Smoke",
                    "PIX",
                    origem: "SmokeRelatorios",
                    referenciaExterna: $"saida-relatorios-{token}");

                var control = new RelatoriosControl();
                control.ViewModel.DataInicio = DateTime.Today.AddDays(-1);
                control.ViewModel.DataFim = DateTime.Today.AddDays(1);
                control.ViewModel.FiltroOperador = string.Empty;
                control.ViewModel.FiltroVendedor = string.Empty;
                control.ViewModel.FiltroCliente = string.Empty;
                control.ViewModel.FiltroCategoria = string.Empty;
                control.ViewModel.FiltroMarca = string.Empty;
                control.ViewModel.FiltroFormaPagamento = string.Empty;
                control.ViewModel.FiltroStatus = string.Empty;
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao concluiu a carga dos indicadores operacionais.");

                var viewModel = control.ViewModel;
                if (viewModel.DadosEstoque.Count == 0 ||
                    viewModel.ProdutosCurvaA + viewModel.ProdutosCurvaB + viewModel.ProdutosCurvaC == 0)
                {
                    throw new InvalidOperationException("Relatorio Curva ABC nao classificou o estoque sintetico.");
                }

                ValidarResumoRelatorio(viewModel.ResumoCurvaAbc, "Curva ABC", "sem dados carregados");
                ValidarResumoRelatorio(viewModel.RankingProdutosParados, "Produtos parados", "ainda nao carregado");

                if (!viewModel.MargemPorProduto.Any(item =>
                        item.ProdutoId == fixture.Produto.Id &&
                        item.ReceitaTotal > 0 &&
                        item.LucroBruto > 0))
                {
                    throw new InvalidOperationException("Relatorio Margem por produto nao refletiu a venda sintetica.");
                }

                ValidarResumoRelatorio(viewModel.ResumoMargemProdutos, "Margem por produto", "ainda nao carregada");

                if (viewModel.VendasPorHora.Count == 0 || viewModel.VendasPorDia.Count == 0)
                {
                    throw new InvalidOperationException("Relatorios de vendas por hora/dia nao refletiram a venda sintetica.");
                }

                ValidarResumoRelatorio(viewModel.MelhorHorarioVendas, "Vendas por hora", "ainda nao carregado");
                ValidarResumoRelatorio(viewModel.MelhorDiaVendas, "Vendas por dia", "ainda nao carregado");
                ValidarResumoRelatorio(viewModel.ResumoDreOperacional, "DRE operacional", "ainda nao carregado");

                if (viewModel.ConciliacaoFinanceira.Count == 0)
                {
                    throw new InvalidOperationException("Relatorio de conciliacao financeira nao gerou linhas operacionais.");
                }

                ValidarResumoRelatorio(viewModel.ResumoConciliacaoFinanceira, "Conciliacao financeira", "ainda nao carregada");

                if (viewModel.OrdensServicoAbertas.Count == 0)
                {
                    throw new InvalidOperationException("Relatorio de OS abertas nao refletiu a OS sintetica.");
                }

                if (!viewModel.OrdensServicoFinalizadas.Any(item => item.Id == ordemFinalizada.Id && item.LucroBruto > 0))
                {
                    throw new InvalidOperationException("Relatorio de OS finalizadas nao refletiu a OS entregue sintetica.");
                }

                if (!viewModel.OrdensServicoPorTecnico.Any(item =>
                        string.Equals(item.TecnicoNome, fixture.Administrator.Nome, StringComparison.OrdinalIgnoreCase) &&
                        item.OrdensFinalizadas > 0))
                {
                    throw new InvalidOperationException("Relatorio de OS por tecnico nao agrupou a OS entregue pelo administrador sintetico.");
                }

                if (!viewModel.ServicosMaisRealizados.Any(item =>
                        item.Servico.Contains("Diagnostico", StringComparison.OrdinalIgnoreCase) &&
                        item.Quantidade > 0))
                {
                    throw new InvalidOperationException("Relatorio de servicos mais realizados nao refletiu servicos de OS/PDV.");
                }

                if (!viewModel.LucroPorServico.Any(item =>
                        item.Servico.Contains("Diagnostico", StringComparison.OrdinalIgnoreCase) &&
                        item.LucroBruto > 0))
                {
                    throw new InvalidOperationException("Relatorio de lucro por servico nao refletiu margem dos servicos sinteticos.");
                }

                ValidarResumoRelatorio(viewModel.ResumoOrdensServico, "Ordens de servico", "ainda nao carregados");
                ValidarResumoRelatorio(viewModel.ResumoServicosOperacionais, "Servicos operacionais", "ainda nao carregados");
            });

            RunCheck(result, "Relatorios:GradesSomenteLeitura", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var grades = FindVisualChildren<DataGrid>(control)
                    .Distinct()
                    .ToList();

                var gradesObrigatorias = new[]
                {
                    "OsAbertasRelatorioGrid",
                    "OsFinalizadasRelatorioGrid",
                    "OsPorTecnicoRelatorioGrid",
                    "ServicosMaisRealizadosRelatorioGrid",
                    "LucroPorServicoRelatorioGrid"
                };

                foreach (var gradeObrigatoria in gradesObrigatorias)
                {
                    if (FindElementByName<DataGrid>(control, gradeObrigatoria) == null)
                    {
                        throw new InvalidOperationException($"Grade obrigatoria de relatorios nao localizada: {gradeObrigatoria}.");
                    }
                }

                if (grades.Count < gradesObrigatorias.Length)
                {
                    throw new InvalidOperationException(
                        $"O workspace de relatorios deveria expor pelo menos {gradesObrigatorias.Length} grades, mas foram localizadas {grades.Count}.");
                }

                var editaveis = grades
                    .Select((grade, index) => new { grade, index })
                    .Where(item => !item.grade.IsReadOnly)
                    .Select(item => item.index + 1)
                    .ToList();
                if (editaveis.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Grades de relatorios ainda permitem edicao: {string.Join(", ", editaveis)}.");
                }

                foreach (var grade in grades.Where(grade => grade.Items.Count > 0))
                {
                    grade.SelectedIndex = 0;
                    if (grade.Columns.Count > 0)
                    {
                        grade.CurrentCell = new DataGridCellInfo(grade.SelectedItem, grade.Columns[0]);
                    }

                    if (grade.BeginEdit())
                    {
                        throw new InvalidOperationException("Uma grade de consulta dos relatorios entrou em modo de edicao.");
                    }
                }
            });

            RunCheck(result, "Relatorios:ExportacoesEvidencias", () =>
            {
                var control = new RelatoriosControl();
                PrepareElement(control);
                var cargaTask = control.ViewModel.CarregarDadosAsync();

                WaitForCondition(
                    () => cargaTask.IsCompleted && control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(10),
                    "O workspace de relatorios nao concluiu a carga inicial antes das exportacoes.");

                var pdfPath = control.ViewModel.ExportarPDF();
                var csvPath = control.ViewModel.ExportarExcel();
                var pacote = control.ViewModel.ExportarPacoteEvidencias();

                EnsureGeneratedFile(pdfPath, "PDF individual dos relatorios");
                EnsureGeneratedFile(csvPath, "CSV individual dos relatorios");
                EnsureGeneratedFile(pacote.PdfPath, "PDF do pacote de evidencias dos relatorios");
                EnsureGeneratedFile(pacote.CsvPath, "CSV do pacote de evidencias dos relatorios");
                EnsureGeneratedFile(pacote.ManifestoPath, "manifesto do pacote de evidencias dos relatorios");

                var manifesto = File.ReadAllText(pacote.ManifestoPath, Encoding.UTF8);
                if (!manifesto.Contains("[Totais]", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("Consistencia=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("CurvaABC=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("ProdutosParados=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("MargemPorProduto=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("VendasPorHora=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("VendasPorDia=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("DRE=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("Conciliacao=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("OrdensServico=", StringComparison.OrdinalIgnoreCase) ||
                    !manifesto.Contains("ServicosOperacionais=", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("O manifesto de evidencias dos relatorios nao contem os blocos operacionais esperados.");
                }

                if (!control.ViewModel.TemExportacaoGerada || string.IsNullOrWhiteSpace(control.ViewModel.ResumoUltimaExportacao))
                {
                    throw new InvalidOperationException("A tela de relatorios nao registrou a ultima exportacao gerada.");
                }
            });
        }

        private static void ValidarResumoRelatorio(string resumo, string relatorio, string marcadorPendente)
        {
            if (string.IsNullOrWhiteSpace(resumo) ||
                resumo.Contains(marcadorPendente, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Resumo do relatorio {relatorio} nao foi gerado: '{resumo}'.");
            }
        }

        private static void EnsureGeneratedFile(string path, string descricao)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || new FileInfo(path).Length == 0)
            {
                throw new InvalidOperationException($"{descricao} nao foi gerado corretamente em {path}.");
            }
        }

        private static void WaitForCondition(Func<bool> predicate, TimeSpan timeout, string failureMessage)
        {
            if (TryWaitForCondition(predicate, timeout))
            {
                return;
            }

            throw new InvalidOperationException(failureMessage);
        }

        private static bool TryWaitForCondition(Func<bool> predicate, TimeSpan timeout)
        {
            var startedAt = DateTime.UtcNow;
            while (DateTime.UtcNow - startedAt <= timeout)
            {
                if (predicate())
                {
                    return true;
                }

                PumpDispatcher();
                Thread.Sleep(25);
            }

            return false;
        }

        private static void PumpDispatcher()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new DispatcherOperationCallback(_ =>
                {
                    frame.Continue = false;
                    return null;
                }),
                null);
            Dispatcher.PushFrame(frame);
        }

    }
}
