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
        // Checks financeiros, graficos, alertas e filtros.

        private void RunFinanceiroGraficosAlertasChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Financeiro:GraficosAlertasDivergencia", () =>
            {
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var financeiro = new FinanceiroDatabaseService();
                financeiro.AdicionarMovimentacao(
                    "Entrada",
                    $"Receita PIX Smoke {token}",
                    240m,
                    DateTime.Today,
                    "Smoke",
                    "PIX",
                    observacoes: "Validacao automatizada de grafico por forma de pagamento.",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"pix-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarMovimentacao(
                    "Entrada",
                    $"Receita sem forma Smoke {token}",
                    75m,
                    DateTime.Today,
                    "Smoke",
                    "",
                    observacoes: "Validacao automatizada de alerta de forma de pagamento.",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"sem-forma-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarMovimentacao(
                    "Saida",
                    $"Despesa Smoke {token}",
                    120m,
                    DateTime.Today,
                    "Smoke",
                    "PIX",
                    observacoes: "Validacao automatizada de fluxo diario.",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"saida-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarContaPagar(
                    $"Fornecedor Divergencia Smoke {token}",
                    $"Conta vencida smoke {token}",
                    2500m,
                    DateTime.Today.AddDays(-3),
                    "Smoke",
                    "Validacao automatizada de saldo projetado negativo.");
                financeiro.AdicionarContaReceber(
                    $"Cliente Inadimplente Smoke {token}",
                    $"Recebivel vencido smoke {token}",
                    150m,
                    DateTime.Today.AddDays(-2),
                    "Boleto",
                    "Validacao automatizada de inadimplencia.",
                    status: "Pendente",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"receber-vencido-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarContaReceber(
                    $"Cliente Pago Sem Data Smoke {token}",
                    $"Recebivel pago sem data smoke {token}",
                    90m,
                    DateTime.Today,
                    "PIX",
                    "Validacao automatizada de status sem data.",
                    status: "Pago",
                    dataPagamento: null,
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"pago-sem-data-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarMovimentacao(
                    "Saida",
                    $"Aluguel fixo smoke {token}",
                    310m,
                    DateTime.Today,
                    "Despesa fixa aluguel",
                    "PIX",
                    observacoes: "Validacao automatizada de despesas fixas.",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"fixa-{token}-{Guid.NewGuid():N}");
                financeiro.AdicionarMovimentacao(
                    "Saida",
                    $"Compra variavel smoke {token}",
                    145m,
                    DateTime.Today,
                    "Compra pecas variavel",
                    "PIX",
                    observacoes: "Validacao automatizada de despesas variaveis.",
                    origem: "SmokeFinanceiro",
                    referenciaExterna: $"variavel-{token}-{Guid.NewGuid():N}");

                var caixaService = new CaixaService(App.Database);
                var sessaoCaixa = caixaService.ObterSessaoAbertaAtual() ??
                    caixaService.AbrirCaixa(80m, "Abertura sintetica para indicadores financeiros.", "SMK");
                var vendaExecutiva = new Venda
                {
                    Id = Guid.NewGuid(),
                    Data = DateTime.Now,
                    Cliente = fixture.Cliente,
                    FormaPagamento = "PIX",
                    Status = "Concluida",
                    Usuario = fixture.Administrator.Nome,
                    CaixaSessaoId = sessaoCaixa.Id,
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
                            Descricao = $"Servico financeiro smoke {token}",
                            Quantidade = 1,
                            PrecoUnitario = 160m,
                            CustoUnitario = 35m
                        }
                    }
                };
                new VendaService(App.Database).RegistrarVenda(vendaExecutiva, atualizarEstoque: false);

                var ordemFinanceira = App.Repositories.OrdensServico.ObterPorId(fixture.OrdemServico.Id)
                    ?? throw new InvalidOperationException("OS sintetica nao foi localizada para indicadores financeiros.");
                ordemFinanceira.Status = "Entregue";
                ordemFinanceira.DataInicio ??= DateTime.Now.AddHours(-2);
                ordemFinanceira.DataConclusao ??= DateTime.Now;
                ordemFinanceira.DataEntrega = DateTime.Now;
                App.Repositories.OrdensServico.Atualizar(ordemFinanceira);

                var viewModel = new FinanceiroViewModel();
                if (viewModel.FluxoCaixaGrafico.Count < DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
                {
                    throw new InvalidOperationException("Grafico de fluxo de caixa nao carregou todos os dias do mes.");
                }

                var hojeGrafico = viewModel.FluxoCaixaGrafico.FirstOrDefault(item => item.Dia == DateTime.Today.ToString("dd"));
                if (hojeGrafico == null || hojeGrafico.Entradas <= 0 || hojeGrafico.Saidas <= 0)
                {
                    throw new InvalidOperationException("Grafico de fluxo de caixa nao refletiu entradas e saidas sinteticas do dia.");
                }

                if (!viewModel.FormasPagamentoGrafico.Any(item => string.Equals(item.Nome, "PIX", StringComparison.OrdinalIgnoreCase) && item.Valor > 0))
                {
                    throw new InvalidOperationException("Grafico de formas de pagamento nao carregou recebimentos PIX.");
                }

                if (string.IsNullOrWhiteSpace(viewModel.FluxoCaixaResumo) ||
                    string.IsNullOrWhiteSpace(viewModel.FormasPagamentoResumo) ||
                    viewModel.FluxoCaixaResumo.Contains("nao carregado", StringComparison.OrdinalIgnoreCase) ||
                    viewModel.FormasPagamentoResumo.Contains("nao carregadas", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Resumos dos graficos financeiros nao foram calculados.");
                }

                var tiposAlertas = viewModel.AlertasDivergencia
                    .Select(alerta => alerta.Tipo)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var alertaEsperado in new[]
                         {
                             "Contas a pagar vencidas",
                             "Inadimplencia",
                             "Status sem data",
                             "Saldo projetado negativo",
                             "Receita sem forma de pagamento"
                         })
                {
                    if (!tiposAlertas.Contains(alertaEsperado))
                    {
                        throw new InvalidOperationException($"Alerta financeiro esperado nao foi emitido: {alertaEsperado}.");
                    }
                }

                if (!string.Equals(viewModel.StatusFinanceiro, "Atencao", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Status financeiro nao refletiu alertas de alta severidade.");
                }

                ValidarIndicadoresPlanoFinanceiro(viewModel, token);

                var control = new FinanceiroControl();
                PrepareElement(control);
                if (control.ViewModel.AlertasDivergencia.Count == 0)
                {
                    throw new InvalidOperationException("FinanceiroControl nao expos alertas de divergencia na ViewModel.");
                }

                if (FindElementByName<Border>(control, "PlanoFinanceiroExecutivoPanel") == null ||
                    FindElementByName<DataGrid>(control, "LucroOsFinanceiroGrid") == null ||
                    FindElementByName<DataGrid>(control, "CaixaOperadorFinanceiroGrid") == null)
                {
                    throw new InvalidOperationException("FinanceiroControl nao renderizou o painel executivo financeiro.");
                }
            });

            RunCheck(result, "Financeiro:FiltrosBaixasPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("filtros e baixas financeiras");

                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var financeiro = new FinanceiroDatabaseService();
                var pagarVencida = $"Pagar vencida smoke {token}";
                var pagarHoje = $"Pagar hoje smoke {token}";
                var pagarSemana = $"Pagar semana smoke {token}";
                var receberVencida = $"Receber vencida smoke {token}";
                var receberHoje = $"Receber hoje smoke {token}";
                var receberSemana = $"Receber semana smoke {token}";

                financeiro.AdicionarContaPagar("Fornecedor Smoke Filtros", pagarVencida, 101m, DateTime.Today.AddDays(-2), "Smoke");
                financeiro.AdicionarContaPagar("Fornecedor Smoke Filtros", pagarHoje, 102m, DateTime.Today, "Smoke");
                financeiro.AdicionarContaPagar("Fornecedor Smoke Filtros", pagarSemana, 103m, DateTime.Today.AddDays(4), "Smoke");
                financeiro.AdicionarContaReceber("Cliente Smoke Filtros", receberVencida, 201m, DateTime.Today.AddDays(-2), "PIX", origem: "SmokeFinanceiro");
                financeiro.AdicionarContaReceber("Cliente Smoke Filtros", receberHoje, 202m, DateTime.Today, "PIX", origem: "SmokeFinanceiro");
                financeiro.AdicionarContaReceber("Cliente Smoke Filtros", receberSemana, 203m, DateTime.Today.AddDays(4), "PIX", origem: "SmokeFinanceiro");

                var hostWindow = CreateHostWindow(new FinanceiroControl(), nameof(FinanceiroControl));
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not FinanceiroControl control)
                    {
                        throw new InvalidOperationException("Host de FinanceiroControl nao conseguiu carregar filtros e baixas.");
                    }

                    WaitForCondition(
                        () => control.ViewModel.ContasPagar.Any(conta => conta.Descricao == pagarHoje) &&
                              control.ViewModel.ContasReceber.Any(conta => conta.Descricao == receberHoje),
                        TimeSpan.FromSeconds(5),
                        "As contas sinteticas nao foram carregadas na tela Financeiro.");

                    ClickButton(control, "FiltroContasPagarVencidasButton");
                    ValidarFiltroFinanceiro(control.ViewModel.ContasPagarFiltradas, pagarVencida, new[] { pagarHoje, pagarSemana }, "pagar vencidas");
                    ClickButton(control, "FiltroContasPagarHojeButton");
                    ValidarFiltroFinanceiro(control.ViewModel.ContasPagarFiltradas, pagarHoje, new[] { pagarVencida, pagarSemana }, "pagar hoje");
                    ClickButton(control, "FiltroContasPagarSemanaButton");
                    ValidarFiltroFinanceiro(control.ViewModel.ContasPagarFiltradas, pagarSemana, new[] { pagarVencida }, "pagar semana");
                    ClickButton(control, "FiltroContasPagarTodasButton");

                    var contaPagar = control.ViewModel.ContasPagar.Single(conta => conta.Descricao == pagarHoje);
                    control.ViewModel.ContaPagarSelecionada = contaPagar;
                    ClickButton(control, "BaixarContaPagarSelecionadaButton");
                    WaitForCondition(
                        () =>
                        {
                            var atualizada = control.ViewModel.ContasPagar.SingleOrDefault(conta => conta.Descricao == pagarHoje);
                            return atualizada != null &&
                                   string.Equals(atualizada.Status, "Paga", StringComparison.OrdinalIgnoreCase) &&
                                   atualizada.DataPagamento.HasValue;
                        },
                        TimeSpan.FromSeconds(5),
                        "A baixa da conta a pagar selecionada nao foi refletida na tela.");

                    ClickButton(control, "FiltroContasReceberVencidasButton");
                    ValidarFiltroFinanceiro(control.ViewModel.ContasReceberFiltradas, receberVencida, new[] { receberHoje, receberSemana }, "receber vencidas");
                    ClickButton(control, "FiltroContasReceberHojeButton");
                    ValidarFiltroFinanceiro(control.ViewModel.ContasReceberFiltradas, receberHoje, new[] { receberVencida, receberSemana }, "receber hoje");
                    ClickButton(control, "FiltroContasReceberSemanaButton");
                    ValidarFiltroFinanceiro(control.ViewModel.ContasReceberFiltradas, receberSemana, new[] { receberVencida }, "receber semana");
                    ClickButton(control, "FiltroContasReceberTodasButton");

                    var contaReceber = control.ViewModel.ContasReceber.Single(conta => conta.Descricao == receberHoje);
                    control.ViewModel.ContaReceberSelecionada = contaReceber;
                    ClickButton(control, "BaixarContaReceberSelecionadaButton");
                    WaitForCondition(
                        () =>
                        {
                            var atualizada = control.ViewModel.ContasReceber.SingleOrDefault(conta => conta.Descricao == receberHoje);
                            return atualizada != null &&
                                   string.Equals(atualizada.Status, "Pago", StringComparison.OrdinalIgnoreCase) &&
                                   atualizada.DataPagamento.HasValue;
                        },
                        TimeSpan.FromSeconds(5),
                        "A baixa da conta a receber selecionada nao foi refletida na tela.");
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });

            RunCheck(result, "Financeiro:ExportacaoArquivoPelaTela", () =>
            {
                var hostWindow = CreateHostWindow(new FinanceiroControl(), nameof(FinanceiroControl));
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not FinanceiroControl control)
                    {
                        throw new InvalidOperationException("Host de FinanceiroControl nao conseguiu carregar a exportacao.");
                    }

                    var diretorio = Path.Combine(App.RuntimeLogDirectory, "financeiro-smoke");
                    Directory.CreateDirectory(diretorio);
                    var inicio = DateTime.Now.AddSeconds(-1);

                    ClickButton(control, "ExportarRelatorioButton");

                    FileInfo? arquivo = null;
                    WaitForCondition(
                        () =>
                        {
                            arquivo = new DirectoryInfo(diretorio)
                                .GetFiles("RelatorioFinanceiro_*.csv")
                                .Where(file => file.LastWriteTime >= inicio && file.Length > 0)
                                .OrderByDescending(file => file.LastWriteTime)
                                .FirstOrDefault();
                            return arquivo != null;
                        },
                        TimeSpan.FromSeconds(5),
                        "O botao Exportar nao gerou o CSV financeiro esperado.");

                    var conteudo = File.ReadAllText(arquivo!.FullName, Encoding.UTF8);
                    foreach (var secao in new[] { "DRE;ReceitasConfirmadas", "ContasPagar;Fornecedor", "ContasReceber;Cliente" })
                    {
                        if (!conteudo.Contains(secao, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException($"CSV financeiro nao contem a secao esperada: {secao}.");
                        }
                    }
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });
        }

        private static bool SelecionarOrdemNaLista(ListBox listBox, Guid ordemId)
        {
            var item = listBox.Items
                .OfType<OrdemServicoPainelItemViewModel>()
                .FirstOrDefault(ordem => ordem.Id == ordemId);
            if (item == null)
            {
                return false;
            }

            listBox.SelectedItem = item;
            listBox.ScrollIntoView(item);
            WaitForUiIdle();
            return true;
        }

        private static void ValidarIndicadoresPlanoFinanceiro(FinanceiroViewModel viewModel, string token)
        {
            if (viewModel.IndicadoresPlanoFinanceiro.Count < 9)
            {
                throw new InvalidOperationException("Painel financeiro executivo nao carregou todos os indicadores exigidos pelo plano.");
            }

            if (string.IsNullOrWhiteSpace(viewModel.ResumoPlanoFinanceiro) ||
                viewModel.ResumoPlanoFinanceiro.Contains("ainda nao carreg", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Resumo executivo financeiro nao foi calculado.");
            }

            var titulos = viewModel.IndicadoresPlanoFinanceiro
                .Select(card => card.Titulo)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var tituloEsperado in new[]
                     {
                         "Faturamento periodo",
                         "Lucro por OS",
                         "Lucro por produto",
                         "Lucro por servico",
                         "Despesas fixas",
                         "Despesas variaveis",
                         "Recebimentos por forma",
                         "Caixa por operador",
                         "Inadimplencia"
                     })
            {
                if (!titulos.Contains(tituloEsperado))
                {
                    throw new InvalidOperationException($"Indicador financeiro ausente: {tituloEsperado}.");
                }
            }

            if (!viewModel.LucroPorOrdemServico.Any(item => item.LucroBruto > 0))
            {
                throw new InvalidOperationException("Lucro por OS nao refletiu a OS sintetica.");
            }

            if (!viewModel.LucroPorProduto.Any(item => item.LucroBruto > 0))
            {
                throw new InvalidOperationException("Lucro por produto nao refletiu venda ou OS sintetica.");
            }

            if (!viewModel.LucroPorServico.Any(item =>
                    item.LucroBruto > 0 &&
                    (item.Referencia.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                     item.Referencia.Contains("Diagnostico", StringComparison.OrdinalIgnoreCase))))
            {
                throw new InvalidOperationException("Lucro por servico nao refletiu servicos sinteticos.");
            }

            if (!viewModel.DespesasPorTipo.Any(item => item.Tipo.Contains("fix", StringComparison.OrdinalIgnoreCase) && item.Valor > 0) ||
                !viewModel.DespesasPorTipo.Any(item => item.Tipo.Contains("vari", StringComparison.OrdinalIgnoreCase) && item.Valor > 0))
            {
                throw new InvalidOperationException("Despesas fixas e variaveis nao foram classificadas.");
            }

            if (!viewModel.RecebimentosPorFormaPagamento.Any(item =>
                    string.Equals(item.FormaPagamento, "PIX", StringComparison.OrdinalIgnoreCase) &&
                    item.Valor > 0))
            {
                throw new InvalidOperationException("Recebimentos por forma de pagamento nao consolidaram PIX.");
            }

            if (!viewModel.CaixaPorOperador.Any(item => item.TotalVendas > 0 || item.Entradas > 0))
            {
                throw new InvalidOperationException("Caixa por operador nao refletiu movimentacao sintetica.");
            }

            if (!viewModel.FaturamentoPorPeriodo.Any(item => item.Data.Date == DateTime.Today && item.Receitas > 0))
            {
                throw new InvalidOperationException("Faturamento por periodo nao consolidou o dia atual.");
            }
        }

        private static void ValidarFiltroFinanceiro<T>(
            IEnumerable<T> itens,
            string descricaoEsperada,
            IEnumerable<string> descricoesAusentes,
            string contexto)
        {
            var descricoes = itens
                .Select(item => item?.GetType().GetProperty("Descricao")?.GetValue(item)?.ToString() ?? string.Empty)
                .ToHashSet(StringComparer.Ordinal);

            if (!descricoes.Contains(descricaoEsperada))
            {
                throw new InvalidOperationException($"Filtro {contexto} nao exibiu a conta esperada '{descricaoEsperada}'.");
            }

            var inesperadas = descricoesAusentes.Where(descricoes.Contains).ToList();
            if (inesperadas.Count > 0)
            {
                throw new InvalidOperationException($"Filtro {contexto} exibiu contas indevidas: {string.Join(", ", inesperadas)}.");
            }
        }

        private static void GarantirBancoIsoladoDoSmoke(string contexto)
        {
            var sqliteIsolado = App.Database.DatabasePath.Contains("AutomatedTests", StringComparison.OrdinalIgnoreCase);
            var sqlServerIsolado = string.Equals(App.Database.RuntimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase) &&
                (App.Database.DatabasePath.Contains("Smoke", StringComparison.OrdinalIgnoreCase) ||
                 App.RuntimeAppDataPath.Contains("AutomatedTests", StringComparison.OrdinalIgnoreCase) ||
                 App.RuntimeAppDataPath.Contains("TestResults", StringComparison.OrdinalIgnoreCase));

            if (!App.IsSmokeTestMode || (!sqliteIsolado && !sqlServerIsolado))
            {
                throw new InvalidOperationException($"{contexto} so pode alterar dados no banco isolado do smoke test.");
            }
        }

    }
}
