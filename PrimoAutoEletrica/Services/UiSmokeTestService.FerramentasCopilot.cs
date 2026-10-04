using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.AI;
using PrimoAutoEletrica.Models.BibliotecaTecnica;
using PrimoAutoEletrica.Models.DiagnosticoGuiado;
using PrimoAutoEletrica.Models.Dvi;
using PrimoAutoEletrica.Models.SureTrack;
using PrimoAutoEletrica.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks do modulo de Ferramentaria Especializada, Copilot IA e Benchmark de Mercado

        private static T AwaitTask<T>(Task<T> task, int timeoutSeconds = 15)
        {
            var startedAt = DateTime.UtcNow;
            while (!task.IsCompleted && (DateTime.UtcNow - startedAt).TotalSeconds <= timeoutSeconds)
            {
                PumpDispatcher();
                Thread.Sleep(20);
            }
            if (!task.IsCompleted)
            {
                throw new TimeoutException($"Operação assíncrona excedeu o tempo limite de {timeoutSeconds}s.");
            }
            if (task.IsFaulted)
            {
                throw task.Exception?.GetBaseException() ?? new InvalidOperationException("Operação assíncrona falhou.");
            }
            return task.Result;
        }

        private static void AwaitTask(Task task, int timeoutSeconds = 15)
        {
            var startedAt = DateTime.UtcNow;
            while (!task.IsCompleted && (DateTime.UtcNow - startedAt).TotalSeconds <= timeoutSeconds)
            {
                PumpDispatcher();
                Thread.Sleep(20);
            }
            if (!task.IsCompleted)
            {
                throw new TimeoutException($"Operação assíncrona excedeu o tempo limite de {timeoutSeconds}s.");
            }
            if (task.IsFaulted)
            {
                throw task.Exception?.GetBaseException() ?? new InvalidOperationException("Operação assíncrona falhou.");
            }
        }

        private void RunFerramentasCopilotIaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Ferramentas:CicloCompletoEmprestimoDevolucao", () =>
            {
                var ferramentaService = new FerramentaService(App.Database, App.Logger);
                var codigoTeste = $"FER-SMOKE-{DateTime.Now:HHmmss}";

                var ferramenta = new Ferramenta
                {
                    CodigoPatrimonio = codigoTeste,
                    Nome = "Osciloscópio Hantek Smoke Test",
                    MarcaModelo = "Hantek 1008C",
                    NumeroSerie = $"SN-SMOKE-{DateTime.Now.Ticks % 100000}",
                    Categoria = CategoriaFerramenta.DiagnosticoEletronico,
                    LocalizacaoArmario = "Armário A / Gaveta 1",
                    Status = StatusFerramenta.Disponivel,
                    RequerCalibracaoPeriodica = true,
                    IntervaloCalibracaoDias = 365,
                    UltimaCalibracao = DateTime.Today.AddDays(-10),
                    ProximaCalibracao = DateTime.Today.AddDays(355)
                };

                // 1. Salvar ferramenta
                AwaitTask(ferramentaService.SalvarFerramentaAsync(ferramenta));

                var salva = AwaitTask(ferramentaService.ObterPorCodigoOuSerieAsync(codigoTeste));
                if (salva == null)
                {
                    throw new InvalidOperationException("Falha ao persistir a ferramenta no catalogo.");
                }

                // 2. Registrar Emprestimo / Retirada
                var funcGuid = new Guid(syntheticUser.Id, 0, 0, new byte[8]);
                var movRetirada = AwaitTask(ferramentaService.RegistrarRetiradaAsync(
                    salva.Id,
                    funcGuid,
                    syntheticUser.Nome,
                    null,
                    "OS-SMOKE-999",
                    DateTime.Now.AddHours(4),
                    "OK",
                    "SmokeTest"));

                if (movRetirada == null || movRetirada.Devolvida)
                {
                    throw new InvalidOperationException("Movimentacao de retirada nao foi registrada corretamente.");
                }

                var emUso = AwaitTask(ferramentaService.ObterPorIdAsync(salva.Id));
                if (emUso == null || emUso.Status != StatusFerramenta.EmUso || emUso.FuncionarioPosseAtualId != funcGuid)
                {
                    throw new InvalidOperationException("Status da ferramenta nao mudou para 'EmUso' ou posse nao foi vinculada.");
                }

                // 3. Registrar Devolucao
                var movDevolucao = AwaitTask(ferramentaService.RegistrarDevolucaoAsync(
                    salva.Id,
                    "OK",
                    "Devolvido limpo e guardado no armario A",
                    "SmokeTest"));

                if (movDevolucao == null || !movDevolucao.Devolvida)
                {
                    throw new InvalidOperationException("Movimentacao de devolucao nao foi registrada com conclusao.");
                }

                var devolvida = AwaitTask(ferramentaService.ObterPorIdAsync(salva.Id));
                if (devolvida == null || devolvida.Status != StatusFerramenta.Disponivel || devolvida.FuncionarioPosseAtualId != null)
                {
                    throw new InvalidOperationException("Ferramenta nao retornou ao status 'Disponivel' apos devolucao.");
                }

                // 4. Historico de Movimentacoes
                var historico = AwaitTask(ferramentaService.ObterHistoricoMovimentacoesAsync(salva.Id));
                if (historico.Count < 1)
                {
                    throw new InvalidOperationException("Historico de movimentacoes incompleto para a ferramenta de teste.");
                }

                // Limpeza
                AwaitTask(ferramentaService.ExcluirFerramentaAsync(salva.Id));
            });

            RunCheck(result, "Ferramentas:ResumoEAlertasCalibracao", () =>
            {
                var ferramentaService = new FerramentaService(App.Database, App.Logger);
                var resumo = AwaitTask(ferramentaService.ObterResumoAsync());

                if (resumo.TotalFerramentas < 0)
                {
                    throw new InvalidOperationException("Contador de resumo da ferramentaria retornou valor invalido.");
                }

                var fCalibracao = new Ferramenta
                {
                    Nome = "Alicate Amperímetro Aferição",
                    RequerCalibracaoPeriodica = true,
                    UltimaCalibracao = DateTime.Today.AddDays(-400),
                    ProximaCalibracao = DateTime.Today.AddDays(-35)
                };

                if (!fCalibracao.CalibracaoVencida)
                {
                    throw new InvalidOperationException("Propriedade CalibracaoVencida falhou ao identificar instrumento fora da validade.");
                }
            });

            RunCheck(result, "CopilotIA:RAGDiagnosticoTecnicoEletrico", () =>
            {
                var rag = new AutomotiveDiagnosticRAGService();

                // Teste DTC P0562
                var dtcP0562 = rag.BuscarPorCodigoDTC("P0562");
                if (dtcP0562 == null || !dtcP0562.Title.Contains("Tensão") || !dtcP0562.ReferenceStandard.Contains("13.8V"))
                {
                    throw new InvalidOperationException("RAG falhou ao consultar base de conhecimento de alternador/bateria (P0562).");
                }

                // Teste DTC P0335
                var dtcP0335 = rag.BuscarPorCodigoDTC("P0335");
                if (dtcP0335 == null || !dtcP0335.Title.Contains("Manivelas") || dtcP0335.SuggestedTools.Count == 0)
                {
                    throw new InvalidOperationException("RAG falhou ao consultar sensor de rotacao (P0335).");
                }

                // Teste Fuga de Carga
                var fuga = rag.BuscarPorSintomaOuTermo("fuga bateria parasita");
                if (fuga.Count == 0 || !fuga[0].Title.Contains("Fuga de Corrente"))
                {
                    throw new InvalidOperationException("RAG falhou na busca semantica por fuga de corrente / consumo parasita.");
                }

                // Teste Relé DIN 72552
                var rele = rag.BuscarPorCodigoDTC("TESTE_RELE");
                if (rele == null || !rele.GuidedSteps.Any(s => s.Contains("Pino 30")) || !rele.GuidedSteps.Any(s => s.Contains("Pino 87")))
                {
                    throw new InvalidOperationException("RAG falhou no roteiro de pinagem DIN 72552 de relés.");
                }

                // Teste Rede CAN
                var can = rag.BuscarPorCodigoDTC("REDE_CAN");
                if (can == null || !can.ReferenceStandard.Contains("60Ω"))
                {
                    throw new InvalidOperationException("RAG falhou na especificacao nominal de 60 Ohms da rede CAN Bus.");
                }
            });

            RunCheck(result, "CopilotIA:ProcessamentoConsultasReais", () =>
            {
                var toolRegistry = new AIToolRegistry(
                    ragService: new AutomotiveDiagnosticRAGService(),
                    ferramentaService: new FerramentaService(App.Database, App.Logger));
                var fallbackAi = new DeterministicFallbackAIService(toolRegistry);

                // Pergunta 1: DTC P0562
                var reqDtc = new AIChatRequest
                {
                    Messages = new List<AIChatMessage>
                    {
                        new AIChatMessage { Role = AIRole.User, Content = "DTC P0562 luz da bateria acesa e direcao dura" }
                    }
                };
                var resDtc = AwaitTask(fallbackAi.ProcessarMensagemAsync(reqDtc));
                if (!resDtc.Success || !resDtc.Message.Contains("P0562") || !resDtc.Message.Contains("Queda de Tensão"))
                {
                    throw new InvalidOperationException($"Resposta do Copilot para DTC P0562 nao retornou procedimento esperado: {resDtc.Message}");
                }

                // Pergunta 2: Consumo Parasita
                var reqParasita = new AIChatRequest
                {
                    Messages = new List<AIChatMessage>
                    {
                        new AIChatMessage { Role = AIRole.User, Content = "Como testar consumo parasita que descarrega a bateria de noite?" }
                    }
                };
                var resParasita = AwaitTask(fallbackAi.ProcessarMensagemAsync(reqParasita));
                if (!resParasita.Success || !resParasita.Message.Contains("50mA") || !resParasita.Message.Contains("fusíveis"))
                {
                    throw new InvalidOperationException($"Resposta do Copilot para consumo parasita nao retornou roteiro correto: {resParasita.Message}");
                }

                // Pergunta 3: Ferramentas em uso
                var reqFerramentas = new AIChatRequest
                {
                    Messages = new List<AIChatMessage>
                    {
                        new AIChatMessage { Role = AIRole.User, Content = "Quem está com ferramentas em uso?" }
                    }
                };
                var resFerramentas = AwaitTask(fallbackAi.ProcessarMensagemAsync(reqFerramentas));
                if (!resFerramentas.Success || !resFerramentas.Message.Contains("Ferramentaria"))
                {
                    throw new InvalidOperationException($"Resposta do Copilot para ferramentas em uso falhou: {resFerramentas.Message}");
                }
            });

            RunCheck(result, "CopilotIA:GeminiFallbackEResiliencia", () =>
            {
                var toolRegistry = new AIToolRegistry(ragService: new AutomotiveDiagnosticRAGService());
                var fallback = new DeterministicFallbackAIService(toolRegistry);
                var gemini = new GeminiAIService(toolRegistry, fallback, apiKey: string.Empty);

                // Sem chave deve indicar que online nao esta disponivel
                if (gemini.IsOnlineAvailable)
                {
                    throw new InvalidOperationException("GeminiAIService indicou online disponivel com chave vazia.");
                }

                // Chamada sem chave deve cair no fallback de forma transparente
                var req = new AIChatRequest
                {
                    Messages = new List<AIChatMessage>
                    {
                        new AIChatMessage { Role = AIRole.User, Content = "O que testar quando alternador não carrega?" }
                    }
                };
                var res = AwaitTask(gemini.ProcessarMensagemAsync(req));
                if (!res.Success || string.IsNullOrWhiteSpace(res.Message))
                {
                    throw new InvalidOperationException("Fallback automatico do Gemini falhou ao responder pergunta.");
                }
            });

            RunCheck(result, "Benchmark:DviInspectionService", () =>
            {
                var dviService = new DviInspectionService(App.Database, App.Logger);
                var novaInspecao = new InspecaoDvi
                {
                    PlacaVeiculo = "SMK-9988",
                    ModeloVeiculo = "VW Gol 1.6 G5",
                    ClienteNome = "Cliente Smoke Test",
                    ResponsavelTecnico = syntheticUser.Nome,
                    StatusAprovacao = DviStatusAprovacao.Pendente
                };
                var salva = AwaitTask(dviService.SalvarInspecaoAsync(novaInspecao));
                if (salva == null || salva.Id <= 0)
                {
                    throw new InvalidOperationException("Falha ao salvar inspeção DVI.");
                }
                var checklist = dviService.GerarChecklistPadraoAutoEletrica(salva.Id);
                if (checklist.Count < 5)
                {
                    throw new InvalidOperationException("Falha na geração do checklist padrão DVI.");
                }
                var laudo = dviService.MontarResumoTextoLaudo(salva);
                if (string.IsNullOrWhiteSpace(laudo) || !laudo.Contains("SMK-9988"))
                {
                    throw new InvalidOperationException("Falha na geração do resumo do laudo DVI.");
                }
            });

            RunCheck(result, "Benchmark:SureTrackService", () =>
            {
                var sureTrackService = new SureTrackService(App.Database, App.Logger);
                AwaitTask(sureTrackService.GarantirCasosIniciaisOficinaAsync());
                var estatisticas = AwaitTask(sureTrackService.ConsultarEstatisticasAsync("Onix", "P0300", null));
                if (estatisticas == null || estatisticas.TotalCasosAnalisados == 0)
                {
                    throw new InvalidOperationException("Falha na consulta estatística SureTrack.");
                }
            });

            RunCheck(result, "Benchmark:BibliotecaTecnicaService", () =>
            {
                var biblioService = new BibliotecaTecnicaService(App.Database, App.Logger);
                AwaitTask(biblioService.GarantirCargaInicialAsync());
                var modulos = AwaitTask(biblioService.ObterModulosAsync());
                if (modulos == null || modulos.Count == 0)
                {
                    throw new InvalidOperationException("Falha ao listar módulos da Biblioteca Técnica.");
                }
            });

            RunCheck(result, "Benchmark:TroubleshootingEDropVoltage", () =>
            {
                var flowService = new TroubleshootingFlowService(App.Database, App.Logger);
                AwaitTask(flowService.GarantirCargaInicialAsync());
                var flows = AwaitTask(flowService.ObterFluxogramasAsync());
                if (flows == null || flows.Count == 0)
                {
                    throw new InvalidOperationException("Falha ao listar fluxogramas de diagnóstico guiado.");
                }

                var calc = new CalculadoraQuedaTensaoService(App.Database, App.Logger);
                var res = calc.CalcularQuedaTensao(new ParametrosCalculoQuedaTensao
                {
                    TensaoFonteVolts = 12.0,
                    TensaoCargaVolts = 10.8,
                    CorrenteAmperes = 15.0,
                    ComprimentoCaboMetros = 2.0,
                    TipoCircuito = "Potência (SAE J1128)"
                });
                if (res.StatusConformidade != StatusConformidadeQuedaTensao.NaoConformeCritico || Math.Abs(res.QuedaTensaoVolts - 1.2) > 0.01)
                {
                    throw new InvalidOperationException("Falha no cálculo de queda de tensão SAE/DIN.");
                }
            });
        }
    }
}
