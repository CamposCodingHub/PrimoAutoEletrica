using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using System;
using System.Linq;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks da Fase 10: diferenciais especificos de auto eletrica automotiva.

        private void RunAutoEletricaTecnicaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "AutoEletrica:CatalogosTecnicosObrigatorios", () =>
            {
                var service = new AutoEletricaTecnicaService();
                var roteiros = service.ObterRoteirosDiagnostico();
                var biblioteca = service.ObterBibliotecaTecnica();
                var servicos = service.ObterServicosTecnicos();
                var sugestoes = service.ObterSugestoesPecas();

                var titulosObrigatorios = new[]
                {
                    "Veiculo nao da partida",
                    "Bateria descarregando",
                    "Alternador nao carrega",
                    "Motor de partida pesado",
                    "Fusivel queimando",
                    "Farol fraco",
                    "Luz de re nao acende",
                    "Lanterna nao acende",
                    "Limpador nao funciona",
                    "Limpador so funciona uma velocidade",
                    "Vidro eletrico nao funciona",
                    "Trava eletrica nao funciona",
                    "Seta nao funciona",
                    "Painel marcando errado",
                    "Curto intermitente",
                    "Relé nao aciona",
                    "Mau aterramento"
                };

                if (roteiros.Count < 17 ||
                    titulosObrigatorios.Any(titulo => !roteiros.Any(roteiro => string.Equals(roteiro.Titulo, titulo, StringComparison.OrdinalIgnoreCase))))
                {
                    throw new InvalidOperationException("Diagnostico guiado nao contem todos os roteiros obrigatorios da Fase 10.");
                }

                if (roteiros.Any(roteiro =>
                        string.IsNullOrWhiteSpace(roteiro.Sintoma) ||
                        roteiro.PossiveisCausas.Count == 0 ||
                        roteiro.Ferramentas.Count == 0 ||
                        roteiro.SequenciaTestes.Count == 0 ||
                        roteiro.ValoresEsperados.Count == 0 ||
                        string.IsNullOrWhiteSpace(roteiro.Resultado) ||
                        string.IsNullOrWhiteSpace(roteiro.Conclusao) ||
                        string.IsNullOrWhiteSpace(roteiro.FotoAnexa) ||
                        !roteiro.PermiteGerarOrcamento))
                {
                    throw new InvalidOperationException("Algum roteiro de diagnostico guiado esta sem campos tecnicos obrigatorios.");
                }

                var bibliotecaObrigatoria = new[]
                {
                    "Como testar rele 4 pinos",
                    "Como testar rele 5 pinos",
                    "Como testar fusivel",
                    "Como testar aterramento",
                    "Como testar queda de tensao",
                    "Como testar bateria",
                    "Como testar alternador",
                    "Como testar motor de partida",
                    "Como definir bitola de fios",
                    "Cuidados em 12V e 24V",
                    "Como crimpar terminais",
                    "Como recuperar conectores",
                    "Cores de fios automotivos",
                    "Cuidados com rede CAN",
                    "Cuidados com modulos eletronicos"
                };

                if (biblioteca.Count < 15 ||
                    bibliotecaObrigatoria.Any(titulo => !biblioteca.Any(item => string.Equals(item.Titulo, titulo, StringComparison.OrdinalIgnoreCase))) ||
                    biblioteca.Any(item => item.Passos.Count == 0 || item.ValoresReferencia.Count == 0))
                {
                    throw new InvalidOperationException("Biblioteca tecnica de auto eletrica esta incompleta.");
                }

                var servicosObrigatorios = new[]
                {
                    "Diagnostico eletrico",
                    "Revisao alternador",
                    "Revisao motor partida",
                    "Instalacao farol",
                    "Lanterna",
                    "LED",
                    "Reparo chicote",
                    "Relé auxiliar",
                    "Tomada carreta",
                    "Alarme",
                    "Trava eletrica",
                    "Som/acessorio",
                    "Fuga corrente",
                    "Aterramento"
                };

                if (servicos.Count < 14 ||
                    servicosObrigatorios.Any(nome => !servicos.Any(servico => string.Equals(servico.Nome, nome, StringComparison.OrdinalIgnoreCase))) ||
                    servicos.Any(servico => servico.ValorPadrao <= 0 || servico.TempoMedioMinutos <= 0 || servico.GarantiaPadraoDias <= 0 || servico.PecasSugeridas.Count == 0))
                {
                    throw new InvalidOperationException("Cadastro de servicos tecnicos esta incompleto.");
                }

                foreach (var termo in new[] { "Lanterna", "Alternador", "Motor partida", "Chicote farol" })
                {
                    var sugestao = sugestoes.FirstOrDefault(item => item.Servico.Contains(termo, StringComparison.OrdinalIgnoreCase));
                    if (sugestao == null || sugestao.Pecas.Count == 0)
                    {
                        throw new InvalidOperationException($"Sugestao de pecas para '{termo}' nao foi encontrada.");
                    }
                }
            });

            RunCheck(result, "AutoEletrica:ProntuarioPersistenteVeiculo", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("Fixture do smoke nao inicializada para auto eletrica.");
                var veiculo = App.Repositories.Clientes.ObterTodosVeiculos().First(v => v.Id == fixture.Veiculo.Id);

                veiculo.SistemaEletrico = "12V";
                veiculo.BateriaInstalada = "Moura M95 instalada no cofre principal";
                veiculo.BateriaMarca = "Moura";
                veiculo.BateriaAmperagem = "95Ah";
                veiculo.BateriaDataInstalacao = DateTime.Today.AddMonths(-4);
                veiculo.TesteTensaoRepouso = "12,58V";
                veiculo.TesteTensaoPartida = "10,4V";
                veiculo.TesteCargaAlternador = "14,12V";
                veiculo.CorrenteFuga = "0,035A";
                veiculo.EstadoAterramentos = "Limpos e reapertados";
                veiculo.ChicotesReparados = "Chicote frontal farol direito";
                veiculo.FusiveisSubstituidos = "F10 15A";
                veiculo.RelesSubstituidos = "Relé auxiliar farol";
                veiculo.LampadasSubstituidas = "H4 direita";
                veiculo.AcessoriosInstalados = "Tomada 12V auxiliar";
                veiculo.ObservacoesTecnicasEletricas = "Prontuario eletrico validado pelo smoke da Fase 10.";
                veiculo.FotosTecnicas = "foto-bateria.png;foto-aterramento.png";
                App.Repositories.Clientes.SalvarVeiculo(veiculo);

                var recarregado = App.Repositories.Clientes.ObterTodosVeiculos().First(v => v.Id == veiculo.Id);
                var prontuario = new AutoEletricaTecnicaService().CriarProntuario(recarregado);

                if (!string.Equals(recarregado.BateriaMarca, "Moura", StringComparison.OrdinalIgnoreCase) ||
                    !recarregado.BateriaDataInstalacao.HasValue ||
                    !prontuario.Campos.Any(campo => campo.Nome.Contains("Tensao em repouso", StringComparison.OrdinalIgnoreCase) && campo.Valor.Contains("12,58", StringComparison.OrdinalIgnoreCase)) ||
                    !prontuario.Campos.Any(campo => campo.Nome.Contains("Corrente de fuga", StringComparison.OrdinalIgnoreCase)) ||
                    prontuario.FotosTecnicas.Count < 2)
                {
                    throw new InvalidOperationException("Prontuario eletrico do veiculo nao persistiu ou nao foi consolidado corretamente.");
                }
            });

            RunCheck(result, "AutoEletrica:DiagnosticoGeraOrcamento", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("Fixture do smoke nao inicializada para orcamento por diagnostico.");
                var service = new AutoEletricaTecnicaService();
                var roteiro = service.ObterRoteirosDiagnostico().First(item => item.Titulo.Contains("Alternador", StringComparison.OrdinalIgnoreCase));

                var orcamento = service.CriarOrcamentoAPartirDiagnostico(new OrcamentoDiagnosticoDraft
                {
                    ClienteId = fixture.Cliente.Id,
                    VeiculoId = fixture.Veiculo.Id,
                    Roteiro = roteiro.Titulo,
                    Resultado = "Carga medida em 12,1V com motor ligado.",
                    Conclusao = "Alternador sem carga; revisar regulador e escovas.",
                    ValorMaoObra = 280m,
                    Pecas = roteiro.PecasSugeridas
                });

                var recarregado = new OrcamentoDatabaseService().ObterOrcamentoPorId(orcamento.Id)
                    ?? throw new InvalidOperationException("Orcamento gerado pelo diagnostico nao foi recarregado.");

                if (!string.Equals(recarregado.Status, "Rascunho", StringComparison.OrdinalIgnoreCase) ||
                    recarregado.ClienteId != fixture.Cliente.Id ||
                    recarregado.VeiculoId != fixture.Veiculo.Id ||
                    !recarregado.Diagnostico.Contains("Alternador", StringComparison.OrdinalIgnoreCase) ||
                    !recarregado.Itens.Any(item => item.Tipo.Contains("Servico", StringComparison.OrdinalIgnoreCase) && item.Subtotal >= 280m) ||
                    !recarregado.Itens.Any(item => item.ProdutoNome.Contains("Regulador", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Orcamento gerado por diagnostico ficou inconsistente.");
                }
            });

            RunCheck(result, "AutoEletrica:HistoricoDefeitosRecorrentes", () =>
            {
                var fixture = _fixture ?? throw new InvalidOperationException("Fixture do smoke nao inicializada para recorrencias.");
                var ordem1 = CreatePersistedOrdemServico(fixture.Cliente, fixture.Veiculo, fixture.Produto);
                var ordem2 = CreatePersistedOrdemServico(fixture.Cliente, fixture.Veiculo, fixture.Produto);

                PrepararOrdemRecorrente(ordem1, "Alternador nao carrega - retorno recorrente", DateTime.Today.AddDays(-8), 190);
                PrepararOrdemRecorrente(ordem2, "Alternador nao carrega - retorno recorrente", DateTime.Today.AddDays(-2), 220);
                App.Repositories.OrdensServico.Atualizar(ordem1);
                App.Repositories.OrdensServico.Atualizar(ordem2);

                var recorrencias = new AutoEletricaTecnicaService().ObterDefeitosRecorrentes();

                if (!recorrencias.Any(item => item.Tipo == "Mesmo defeito" && item.Descricao.Contains("alternador", StringComparison.OrdinalIgnoreCase) && item.Quantidade >= 2) ||
                    !recorrencias.Any(item => item.Tipo == "Peca que mais falha" && item.Descricao.Contains("Regulador alternador", StringComparison.OrdinalIgnoreCase)) ||
                    !recorrencias.Any(item => item.Tipo == "Garantia ativa" && item.EmGarantia) ||
                    !recorrencias.Any(item => item.Tipo == "Defeito por modelo" && item.Modelo.Contains(fixture.Veiculo.Modelo, StringComparison.OrdinalIgnoreCase)) ||
                    !recorrencias.Any(item => item.Tipo == "Tempo medio de resolucao" && item.TempoMedioMinutos > 0))
                {
                    throw new InvalidOperationException("Historico de defeitos recorrentes nao consolidou retorno, pecas, garantia, modelo e tempo medio.");
                }
            });

            RunCheck(result, "AutoEletrica:TelaCarregaEAcoes", () =>
            {
                var hostWindow = CreateHostWindow(new AutoEletricaTecnicaControl(), nameof(AutoEletricaTecnicaControl));

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not AutoEletricaTecnicaControl control)
                    {
                        throw new InvalidOperationException("Host de auto eletrica tecnica nao carregou o controle esperado.");
                    }

                    var roteiros = FindElementByName<ListBox>(control, "RoteirosDiagnosticoListBox")
                        ?? throw new InvalidOperationException("Lista de roteiros nao localizada.");
                    var biblioteca = FindElementByName<ListBox>(control, "BibliotecaTecnicaListBox")
                        ?? throw new InvalidOperationException("Lista de biblioteca tecnica nao localizada.");
                    var servicos = FindElementByName<DataGrid>(control, "ServicosTecnicosDataGrid")
                        ?? throw new InvalidOperationException("Grade de servicos tecnicos nao localizada.");
                    var sugestoes = FindElementByName<ListBox>(control, "SugestoesPecasListBox")
                        ?? throw new InvalidOperationException("Lista de sugestoes de pecas nao localizada.");

                    WaitForCondition(
                        () => roteiros.Items.Count >= 17 && biblioteca.Items.Count >= 15 && servicos.Items.Count >= 14 && sugestoes.Items.Count >= 4,
                        TimeSpan.FromSeconds(5),
                        "Tela de auto eletrica tecnica nao carregou os acervos obrigatorios.");

                    ClickButton(control, "AtualizarTecnicaButton");
                    ClickButton(control, "GerarOrcamentoDiagnosticoButton");
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });

            RunCheck(result, "MainWindow:NavegaAutoEletricaTecnica", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    ShowWindowForInteraction(window);

                    if (!window.NavigateToModuleForAutomation("AutoEletricaTecnica", forceReload: true) ||
                        !string.Equals(window.CurrentModuleName, "AutoEletricaTecnica", StringComparison.OrdinalIgnoreCase) ||
                        window.CurrentContentElement is not AutoEletricaTecnicaControl ||
                        !window.IsMenuEnabledForAutomation("AutoEletricaTecnica") ||
                        !window.IsModuleHighlightedForAutomation("AutoEletricaTecnica"))
                    {
                        throw new InvalidOperationException("MainWindow nao navegou corretamente para AutoEletricaTecnica.");
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

            static void PrepararOrdemRecorrente(OrdemServico ordem, string defeito, DateTime abertura, int tempoMinutos)
            {
                ordem.ProblemaRelatado = defeito;
                ordem.Diagnostico = "Regulador alternador com falha recorrente.";
                ordem.Status = "Entregue";
                ordem.DataAbertura = abertura;
                ordem.DataAprovacao = abertura.AddMinutes(5);
                ordem.DataInicio = abertura.AddMinutes(10);
                ordem.DataConclusao = abertura.AddMinutes(tempoMinutos);
                ordem.DataEntrega = abertura.AddMinutes(tempoMinutos + 20);
                ordem.TempoRealMinutos = tempoMinutos;
                ordem.GarantiaValidaAte = DateTime.Today.AddDays(45);
                ordem.GarantiaObservacoes = "Garantia ativa sobre revisao de alternador.";

                if (ordem.Itens.Count > 0)
                {
                    ordem.Itens[0].Descricao = "Regulador alternador 12V";
                    ordem.Itens[0].Tipo = "Produto";
                }
            }
        }
    }
}
