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
        // Checks de agendamentos, visualizacoes e conversoes.

        private void RunAgendamentosVisualizacoesConversoesChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Agendamentos:VisualizacoesFiltrosConversoes", () =>
            {
                GarantirBancoIsoladoDoSmoke("visualizacoes e conversoes de agendamentos");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var hoje = DateTime.Today;
                var inicioSemana = hoje.AddDays(-(((int)hoje.DayOfWeek + 6) % 7));
                var dataSemana = Enumerable.Range(0, 7)
                    .Select(offset => inicioSemana.AddDays(offset))
                    .First(data =>
                        data.Date != hoje &&
                        data.Month == hoje.Month &&
                        data.Year == hoje.Year);
                var dataMes = Enumerable.Range(1, DateTime.DaysInMonth(hoje.Year, hoje.Month))
                    .Select(dia => new DateTime(hoje.Year, hoje.Month, dia))
                    .First(data =>
                        data.Date != hoje &&
                        data.Date != dataSemana.Date &&
                        (data.Date < inicioSemana.Date || data.Date > inicioSemana.AddDays(6).Date));
                var agendamentoHoje = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto, hoje, "Confirmado", "Urgente", $"AG-VIS-HOJE-{token}");
                var agendamentoSemana = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto, dataSemana, "Agendado", "Normal", $"AG-VIS-SEM-{token}");
                var agendamentoMes = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto, dataMes, "Aguardando Cliente", "Alta", $"AG-VIS-MES-{token}");
                var agendamentoForaMes = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto, hoje.AddMonths(1), "Confirmado", "Baixa", $"AG-VIS-FORA-{token}");

                var viewModel = new AgendamentosViewModel
                {
                    DataSelecionada = hoje
                };

                viewModel.VisualizacaoCalendario = "Diaria";
                ValidarAgendamentoPresente(viewModel, agendamentoHoje.Numero, "visao diaria");
                ValidarAgendamentoAusente(viewModel, agendamentoSemana.Numero, "visao diaria");

                viewModel.VisualizacaoCalendario = "Semanal";
                ValidarAgendamentoPresente(viewModel, agendamentoHoje.Numero, "visao semanal");
                ValidarAgendamentoPresente(viewModel, agendamentoSemana.Numero, "visao semanal");
                ValidarAgendamentoAusente(viewModel, agendamentoForaMes.Numero, "visao semanal");

                viewModel.VisualizacaoCalendario = "Mensal";
                ValidarAgendamentoPresente(viewModel, agendamentoHoje.Numero, "visao mensal");
                ValidarAgendamentoPresente(viewModel, agendamentoSemana.Numero, "visao mensal");
                ValidarAgendamentoPresente(viewModel, agendamentoMes.Numero, "visao mensal");
                ValidarAgendamentoAusente(viewModel, agendamentoForaMes.Numero, "visao mensal");

                viewModel.FiltroBusca = token;
                viewModel.FiltroStatus = "Confirmado";
                viewModel.FiltroPrioridade = "Urgente";
                viewModel.FiltroCliente = fixture.Cliente.Nome;
                viewModel.FiltroVeiculo = fixture.Veiculo.Placa;

                if (viewModel.AgendamentosFiltrados.Count != 1 ||
                    !string.Equals(viewModel.AgendamentosFiltrados[0].Numero, agendamentoHoje.Numero, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Filtros combinados de agendamentos nao localizaram apenas o atendimento esperado.");
                }

                var agendamentoService = new AgendamentoDatabaseService();
                var agendamentoConfirmacao = CreatePersistedAgendamento(
                    fixture.Cliente,
                    fixture.Veiculo,
                    fixture.Produto,
                    hoje.AddDays(3),
                    "Agendado",
                    "Normal",
                    $"AG-CONF-{token}");
                var confirmacaoVm = new AgendamentosViewModel
                {
                    AgendamentoSelecionado = agendamentoService.ObterAgendamentoPorId(agendamentoConfirmacao.Id)
                };
                confirmacaoVm.ConfirmarCommand.Execute(null);
                var confirmado = agendamentoService.ObterAgendamentoPorId(agendamentoConfirmacao.Id)
                    ?? throw new InvalidOperationException("Agendamento confirmado nao foi recarregado.");
                if (!string.Equals(confirmado.Status, "Confirmado", StringComparison.OrdinalIgnoreCase) ||
                    !confirmado.LembreteWhatsApp ||
                    !confirmado.DataLembrete.HasValue)
                {
                    throw new InvalidOperationException("Comando Confirmar nao persistiu status confirmado e lembrete WhatsApp programado.");
                }

                var lembreteVm = new AgendamentosViewModel
                {
                    AgendamentoSelecionado = agendamentoService.ObterAgendamentoPorId(agendamentoHoje.Id)
                };
                lembreteVm.EnviarLembreteWhatsAppCommand.Execute(null);
                var lembreteEnviado = agendamentoService.ObterAgendamentoPorId(agendamentoHoje.Id)
                    ?? throw new InvalidOperationException("Agendamento com lembrete nao foi recarregado.");
                if (!lembreteEnviado.LembreteEnviado || !lembreteEnviado.DataLembrete.HasValue)
                {
                    throw new InvalidOperationException("Envio de lembrete WhatsApp nao marcou rastreabilidade no agendamento.");
                }

                var agendamentoParaReagendar = agendamentoService.ObterAgendamentoPorId(agendamentoForaMes.Id)
                    ?? throw new InvalidOperationException("Agendamento para reagendamento nao foi recarregado.");
                var dataAnterior = agendamentoParaReagendar.DataAgendamento.Date;
                var novaData = hoje.AddMonths(2).Date;
                var reagendamentoVm = new AgendamentosViewModel
                {
                    DataSelecionada = novaData,
                    AgendamentoSelecionado = agendamentoParaReagendar
                };
                reagendamentoVm.ReagendarCommand.Execute(null);

                var reagendado = agendamentoService.ObterAgendamentoPorId(agendamentoForaMes.Id)
                    ?? throw new InvalidOperationException("Agendamento reagendado nao foi recarregado.");
                if (reagendado.DataAgendamento.Date != novaData ||
                    reagendado.DataAgendamentoAnterior?.Date != dataAnterior ||
                    !reagendado.DataReagendamento.HasValue)
                {
                    throw new InvalidOperationException("Comando Reagendar nao persistiu data anterior, nova data e rastreabilidade.");
                }

                var agendamentoParaOs = agendamentoService.ObterAgendamentoPorId(agendamentoSemana.Id)
                    ?? throw new InvalidOperationException("Agendamento para conversao em OS nao foi recarregado.");
                var ordem = agendamentoService.ConverterEmOrdemServico(agendamentoParaOs, "Smoke Test");
                var convertido = agendamentoService.ObterAgendamentoPorId(agendamentoSemana.Id)
                    ?? throw new InvalidOperationException("Agendamento convertido em OS nao foi recarregado.");

                if (convertido.OrdemServicoId != ordem.Id ||
                    !string.Equals(convertido.NumeroOS, ordem.Numero, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Conversao de agendamento em OS nao persistiu vinculo e numero da OS.");
                }

                var ordemReutilizada = agendamentoService.ConverterEmOrdemServico(convertido, "Smoke Test");
                if (ordemReutilizada.Id != ordem.Id)
                {
                    throw new InvalidOperationException("Segunda conversao do mesmo agendamento criou OS duplicada.");
                }

                var agendamentoParaOrcamento = agendamentoService.ObterAgendamentoPorId(agendamentoMes.Id)
                    ?? throw new InvalidOperationException("Agendamento para geracao de orcamento nao foi recarregado.");
                agendamentoParaOrcamento.CheckOut = DateTime.Now;
                agendamentoParaOrcamento.Status = "Finalizado";
                agendamentoService.AtualizarAgendamento(agendamentoParaOrcamento);

                var orcamentosAntes = new OrcamentoDatabaseService()
                    .ObterTodosOrcamentos()
                    .Count(o => o.Observacoes.Contains(agendamentoParaOrcamento.Numero, StringComparison.OrdinalIgnoreCase));
                var integracaoVm = new AgendamentosViewModel();
                integracaoVm.IntegrarComOrcamentos(agendamentoParaOrcamento);

                if (!agendamentoService.IntegracaoExecutada(agendamentoParaOrcamento.Id, "Orcamentos"))
                {
                    throw new InvalidOperationException("Integracao de agendamento com orcamentos nao foi registrada.");
                }

                var orcamentoService = new OrcamentoDatabaseService();
                var orcamentosGerados = orcamentoService
                    .ObterTodosOrcamentos()
                    .Where(o => o.Observacoes.Contains(agendamentoParaOrcamento.Numero, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (orcamentosGerados.Count <= orcamentosAntes ||
                    orcamentosGerados.All(o => o.Itens.Count == 0 || o.Total <= 0))
                {
                    throw new InvalidOperationException("Agendamento finalizado nao gerou orcamento operacional valido.");
                }

                integracaoVm.IntegrarComOrcamentos(agendamentoParaOrcamento);
                var totalDepoisReprocessamento = orcamentoService
                    .ObterTodosOrcamentos()
                    .Count(o => o.Observacoes.Contains(agendamentoParaOrcamento.Numero, StringComparison.OrdinalIgnoreCase));

                if (totalDepoisReprocessamento != orcamentosGerados.Count)
                {
                    throw new InvalidOperationException("Reprocessamento da integracao de orcamento gerou duplicidade.");
                }
            });

            RunCheck(result, "Agendamentos:CheckInCheckOutPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("check-in e check-out de agendamentos pela tela");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var agendamento = CreatePersistedAgendamento(
                    fixture.Cliente,
                    fixture.Veiculo,
                    fixture.Produto,
                    DateTime.Today,
                    "Confirmado",
                    "Urgente",
                    "AG-TELA");
                var agendamentoService = new AgendamentoDatabaseService();
                var hostWindow = CreateHostWindow(new AgendamentosControl(), nameof(AgendamentosControl));

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not AgendamentosControl control)
                    {
                        throw new InvalidOperationException("Host de AgendamentosControl nao conseguiu carregar o atendimento sintetico.");
                    }

                    var header = FindElementByName<Border>(control, "AgendaModulePageHeader")
                        ?? throw new InvalidOperationException("ModulePageHeader da central de agendamentos nao foi encontrado.");
                    if (header.Visibility != Visibility.Visible)
                    {
                        throw new InvalidOperationException("ModulePageHeader da agenda nao esta visivel.");
                    }

                    var contentScroll = FindElementByName<ScrollViewer>(control, "AgendaContentScroll")
                        ?? throw new InvalidOperationException("AgendaContentScroll nao foi encontrado.");
                    WaitForCondition(
                        () => contentScroll.Visibility == Visibility.Visible,
                        TimeSpan.FromSeconds(5),
                        "Painel Loaded da agenda nao ficou visivel.");

                    var calendar = FindElementByName<Calendar>(control, "calendarControl")
                        ?? throw new InvalidOperationException("calendarControl nao foi localizado na central de agendamentos.");
                    if (calendar.DisplayDateStart.HasValue || calendar.DisplayDateEnd.HasValue)
                    {
                        throw new InvalidOperationException("calendarControl nao deve ter DisplayDateStart/End (regressao de clamp).");
                    }

                    var listView = FindElementByName<ListView>(control, "agendamentosListView")
                        ?? throw new InvalidOperationException("agendamentosListView nao foi localizada para validar entrada e saida.");
                    if (FindElementByName<Button>(control, "ConfirmarAgendamentoButton") == null)
                    {
                        throw new InvalidOperationException("Botao ConfirmarAgendamentoButton nao foi localizado na agenda.");
                    }

                    WaitForCondition(
                        () => SelecionarAgendamentoNaLista(listView, agendamento.Id),
                        TimeSpan.FromSeconds(5),
                        "O agendamento sintetico nao apareceu na agenda operacional.");

                    ClickButton(control, "CheckInAgendamentoButton");
                    WaitForCondition(
                        () =>
                        {
                            var atual = agendamentoService.ObterAgendamentoPorId(agendamento.Id);
                            return atual?.CheckIn.HasValue == true &&
                                   string.Equals(atual.Status, "Em Andamento", StringComparison.OrdinalIgnoreCase);
                        },
                        TimeSpan.FromSeconds(5),
                        "O botao Entrada nao registrou o check-in do agendamento.");

                    WaitForCondition(
                        () => SelecionarAgendamentoNaLista(listView, agendamento.Id),
                        TimeSpan.FromSeconds(5),
                        "O agendamento nao permaneceu selecionavel apos o check-in.");
                    ClickButton(control, "CheckOutAgendamentoButton");

                    WaitForCondition(
                        () =>
                        {
                            var atual = agendamentoService.ObterAgendamentoPorId(agendamento.Id);
                            return atual?.CheckOut.HasValue == true &&
                                   string.Equals(atual.Status, "Finalizado", StringComparison.OrdinalIgnoreCase);
                        },
                        TimeSpan.FromSeconds(5),
                        "O botao Saida nao registrou o check-out do agendamento.");

                    if (!agendamentoService.IntegracaoExecutada(agendamento.Id, "Orcamentos") ||
                        !agendamentoService.IntegracaoExecutada(agendamento.Id, "Estoque"))
                    {
                        throw new InvalidOperationException("O check-out pela tela nao registrou as integracoes esperadas.");
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

        private static bool SelecionarAgendamentoNaLista(ListView listView, Guid agendamentoId)
        {
            var item = listView.Items
                .OfType<Agendamento>()
                .FirstOrDefault(agendamento => agendamento.Id == agendamentoId);
            if (item == null)
            {
                return false;
            }

            listView.SelectedItem = item;
            listView.ScrollIntoView(item);
            WaitForUiIdle();
            return true;
        }

        private static void ValidarAgendamentoPresente(AgendamentosViewModel viewModel, string numero, string contexto)
        {
            if (!viewModel.AgendamentosFiltrados.Any(a => string.Equals(a.Numero, numero, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Agendamento {numero} nao apareceu na {contexto}.");
            }
        }

        private static void ValidarAgendamentoAusente(AgendamentosViewModel viewModel, string numero, string contexto)
        {
            if (viewModel.AgendamentosFiltrados.Any(a => string.Equals(a.Numero, numero, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Agendamento {numero} apareceu indevidamente na {contexto}.");
            }
        }

    }
}
