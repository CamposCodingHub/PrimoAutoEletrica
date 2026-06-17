using System;
using System.IO;
using System.Linq;
using System.Windows;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks da Fase 9: recursos profissionais de oficina.

        private void RunOficinaKanbanChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "OficinaKanban:SnapshotTimelineComunicacao", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("Fixture do smoke nao inicializada para o Kanban.");
                var ordem = CreatePersistedOrdemServico(fixture.Cliente, fixture.Veiculo, fixture.Produto);
                var service = new OficinaProfissionalService();

                var snapshot = service.CriarSnapshot(fixture.Cliente.Id, fixture.Veiculo.Id);

                if (snapshot.ChecklistPadrao.Count < 20)
                {
                    throw new InvalidOperationException("Checklist visual padrao nao contem todos os itens profissionais exigidos.");
                }

                var card = snapshot.Kanban.SelectMany(coluna => coluna.Cards).FirstOrDefault(c => c.Id == ordem.Id);
                if (card == null)
                {
                    throw new InvalidOperationException("OS sintetica nao apareceu no Kanban profissional.");
                }

                if (string.IsNullOrWhiteSpace(card.ServicoPrincipal) ||
                    string.IsNullOrWhiteSpace(card.Cliente) ||
                    string.IsNullOrWhiteSpace(card.Veiculo) ||
                    string.IsNullOrWhiteSpace(card.Placa) ||
                    card.ValorEstimado <= 0)
                {
                    throw new InvalidOperationException("Card Kanban nao trouxe os dados operacionais obrigatorios.");
                }

                if (!snapshot.Timeline.Any(item => item.Tipo.Contains("OS", StringComparison.OrdinalIgnoreCase)) ||
                    !snapshot.Timeline.Any(item => item.Tipo.Contains("Orcamento", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Timeline do cliente/veiculo nao consolidou OS e orcamentos.");
                }

                if (snapshot.Mensagens.Count < 8 ||
                    !snapshot.Mensagens.Any(m => m.Codigo == "PRONTO_RETIRADA") ||
                    !snapshot.Mensagens.Any(m => m.Codigo == "COBRANCA_AMIGAVEL"))
                {
                    throw new InvalidOperationException("Modelos de comunicacao com cliente estao incompletos.");
                }

                var url = OficinaProfissionalService.CriarWhatsAppUrl(fixture.Cliente.Telefone, snapshot.Mensagens[0].Mensagem);
                if (!url.StartsWith("https://wa.me/", StringComparison.OrdinalIgnoreCase) || !url.Contains("text=", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("URL de WhatsApp profissional nao foi montada corretamente.");
                }

                service.AlterarStatusOrdem(ordem.Id, "Aguardando aprovacao");
                service.AvancarStatusOrdem(ordem.Id);
                var recarregada = App.Repositories.OrdensServico.ObterPorId(ordem.Id)
                    ?? throw new InvalidOperationException("OS do Kanban nao foi recarregada apos alteracao de status.");

                if (!string.Equals(recarregada.Status, "Aprovada", StringComparison.OrdinalIgnoreCase) ||
                    !recarregada.AprovadaCliente ||
                    !recarregada.DataAprovacao.HasValue ||
                    !recarregada.Eventos.Any(e => e.Tipo == "Kanban"))
                {
                    throw new InvalidOperationException("Alteracao de status pelo Kanban nao persistiu rastreabilidade/aprovacao.");
                }
            });

            RunCheck(result, "OficinaKanban:OrcamentoWhatsAppAprovacaoRecusa", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("Fixture do smoke nao inicializada para orcamentos do Kanban.");
                var service = new OficinaProfissionalService();
                var orcamentoAprovado = CreatePersistedOrcamento(fixture.Cliente, fixture.Produto);
                var orcamentoRecusado = CreatePersistedOrcamento(fixture.Cliente, fixture.Produto);

                var mensagem = OficinaProfissionalService.CriarMensagemOrcamentoWhatsApp(orcamentoAprovado);
                if (!mensagem.Contains("Primo Auto Eletrica", StringComparison.OrdinalIgnoreCase) ||
                    !mensagem.Contains("Cliente:", StringComparison.OrdinalIgnoreCase) ||
                    !mensagem.Contains("Veiculo:", StringComparison.OrdinalIgnoreCase) ||
                    !mensagem.Contains("Valor total:", StringComparison.OrdinalIgnoreCase) ||
                    !mensagem.Contains("Validade:", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Mensagem de aprovacao por WhatsApp do orcamento esta incompleta.");
                }

                service.RegistrarEnvioOrcamentoWhatsApp(orcamentoAprovado.Id);
                service.MarcarOrcamentoAprovado(orcamentoAprovado.Id);
                service.MarcarOrcamentoRecusado(orcamentoRecusado.Id);

                var aprovado = new OrcamentoDatabaseService().ObterOrcamentoPorId(orcamentoAprovado.Id)
                    ?? throw new InvalidOperationException("Orcamento aprovado nao foi recarregado.");
                var recusado = new OrcamentoDatabaseService().ObterOrcamentoPorId(orcamentoRecusado.Id)
                    ?? throw new InvalidOperationException("Orcamento recusado nao foi recarregado.");

                if (!string.Equals(aprovado.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) ||
                    !aprovado.DataAprovacao.HasValue ||
                    !aprovado.Observacoes.Contains("Enviado por WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                    !aprovado.Observacoes.Contains("Aprovado pelo cliente", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Fluxo de envio/aprovacao de orcamento por WhatsApp nao persistiu corretamente.");
                }

                if (!string.Equals(recusado.Status, "Recusado", StringComparison.OrdinalIgnoreCase) ||
                    !recusado.Observacoes.Contains("Recusado pelo cliente", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Fluxo de recusa de orcamento nao persistiu corretamente.");
                }
            });

            RunCheck(result, "OficinaKanban:AssinaturaDigitalCanvas", () =>
            {
                var window = new Views.AssinaturaDigitalWindow("smoke-kanban");

                try
                {
                    ShowWindowForInteraction(window);
                    ClickButton(window, "Salvar assinatura");

                    if (string.IsNullOrWhiteSpace(window.SignaturePath) ||
                        !File.Exists(window.SignaturePath) ||
                        new FileInfo(window.SignaturePath).Length == 0)
                    {
                        throw new InvalidOperationException("Assinatura digital por canvas nao gerou arquivo PNG valido.");
                    }
                }
                finally
                {
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                }
            });

            RunCheck(result, "OficinaKanban:TelaCarregaEAcoes", () =>
            {
                var hostWindow = CreateHostWindow(new OficinaKanbanControl(), nameof(OficinaKanbanControl));

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not OficinaKanbanControl control)
                    {
                        throw new InvalidOperationException("Host do Kanban nao carregou o controle esperado.");
                    }

                    if (control.KanbanColumns.Count == 0 || control.ChecklistPadrao.Count < 20 || control.Mensagens.Count < 8)
                    {
                        throw new InvalidOperationException("Tela Kanban nao carregou colunas, checklist e mensagens.");
                    }

                    ClickButton(control, "AtualizarKanbanButton");
                    ClickButton(control, "AbrirOsSelecionadaButton");

                    if (control.Timeline.Count == 0)
                    {
                        throw new InvalidOperationException("Tela Kanban nao carregou timeline para a OS selecionada.");
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

            RunCheck(result, "MainWindow:NavegaOficinaKanban", () =>
            {
                var syntheticUser = _fixture?.Administrator ?? CreateSyntheticAdministrator();
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (!window.IsMenuEnabledForAutomation("OficinaKanban"))
                    {
                        throw new InvalidOperationException("Menu do Kanban deveria estar habilitado para administrador.");
                    }

                    if (!window.NavigateToModuleForAutomation("OficinaKanban"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para OficinaKanban.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "OficinaKanban", StringComparison.OrdinalIgnoreCase) ||
                        window.CurrentContentElement is not OficinaKanbanControl)
                    {
                        throw new InvalidOperationException("Shell nao exibiu o controle OficinaKanban apos navegacao.");
                    }

                    if (!window.IsModuleHighlightedForAutomation("OficinaKanban"))
                    {
                        throw new InvalidOperationException("Menu Kanban nao ficou destacado apos a navegacao.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
