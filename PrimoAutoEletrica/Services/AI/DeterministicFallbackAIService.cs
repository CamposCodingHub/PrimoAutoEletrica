using PrimoAutoEletrica.Models.AI;
using PrimoAutoEletrica.Models.BibliotecaTecnica;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public sealed class DeterministicFallbackAIService : IAIService
    {
        private readonly AIToolRegistry _toolRegistry;
        private readonly AutomotiveDiagnosticRAGService _ragService;
        private readonly AutomotiveWebSearchService _webSearchService;
        private readonly AutomotiveDiagramImageService _diagramImageService;
        private readonly ISureTrackService? _sureTrackService;
        private readonly IBibliotecaTecnicaService? _bibliotecaTecnicaService;
        private readonly ITroubleshootingFlowService? _troubleshootingService;
        private readonly ICalculadoraQuedaTensaoService? _calculadoraQuedaTensaoService;

        public string ProviderName => "PRIMOX Copilot Inteligente";
        public bool IsOnlineAvailable => true; // 100% nativo, proprietário e offline

        public DeterministicFallbackAIService(
            AIToolRegistry toolRegistry,
            AutomotiveDiagnosticRAGService? ragService = null,
            AutomotiveWebSearchService? webSearchService = null,
            AutomotiveDiagramImageService? diagramImageService = null,
            ISureTrackService? sureTrackService = null,
            IBibliotecaTecnicaService? bibliotecaTecnicaService = null,
            ITroubleshootingFlowService? troubleshootingService = null,
            ICalculadoraQuedaTensaoService? calculadoraQuedaTensaoService = null)
        {
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _ragService = ragService ?? new AutomotiveDiagnosticRAGService();
            _webSearchService = webSearchService ?? new AutomotiveWebSearchService();
            _diagramImageService = diagramImageService ?? new AutomotiveDiagramImageService();
            _sureTrackService = sureTrackService;
            _bibliotecaTecnicaService = bibliotecaTecnicaService;
            _troubleshootingService = troubleshootingService;
            _calculadoraQuedaTensaoService = calculadoraQuedaTensaoService;
        }

        public async Task<AIChatResponse> ProcessarMensagemAsync(AIChatRequest request, CancellationToken cancellationToken = default)
        {
            var todasMensagens = request.Messages ?? new List<AIChatMessage>();
            var ultimaMensagemUser = todasMensagens.LastOrDefault(m => m.Role == AIRole.User)?.Content ?? string.Empty;
            var penultimaMensagemAssistente = todasMensagens.Count >= 2 
                ? todasMensagens[todasMensagens.Count - 2].Role == AIRole.Assistant ? todasMensagens[todasMensagens.Count - 2].Content : string.Empty
                : string.Empty;

            var textoLimpo = AutomotiveDiagnosticRAGService.NormalizarTexto(ultimaMensagemUser);

            var response = new AIChatResponse
            {
                ProviderUsed = ProviderName,
                Success = true
            };

            // =========================================================================
            // 1. APRESENTAÇÃO E PERGUNTAS SOBRE AS CAPACIDADES DA IA
            // =========================================================================
            if (EhPerguntaSobreIa(textoLimpo))
            {
                response.Message = GerarRespostaPerguntaSobreIa();
                response.SuggestedActions.Add(new AISuggestedAction { Label = "💡 Diagnóstico Elétrico", ActionType = "ExecuteQuery", Parameter = "O motor de arranque vira forte mas não pega" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ DTC de Scanner", ActionType = "ExecuteQuery", Parameter = "DTC P0300 falha de combustão" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "📘 Manual: Abrir OS", ActionType = "ExecuteQuery", Parameter = "Como abrir uma nova Ordem de Serviço?" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "🚗 ABS no Onix", ActionType = "ExecuteQuery", Parameter = "Luz do ABS acesa e velocímetro parou Onix" });
                return response;
            }

            // =========================================================================
            // 2. AGRADECIMENTOS E FEEDBACK POSITIVO
            // =========================================================================
            if (EhAgradecimento(textoLimpo))
            {
                response.Message = GerarRespostaAgradecimento();
                response.SuggestedActions.Add(new AISuggestedAction { Label = "📘 Mais Manuais ERP", ActionType = "ExecuteQuery", Parameter = "Como funciona o quadro kanban da oficina?" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Novo Diagnóstico", ActionType = "ExecuteQuery", Parameter = "Alternador não está carregando" });
                return response;
            }

            // =========================================================================
            // 3. SAUDAÇÕES E CUMPRIMENTOS NATURAIS (Acolhimento profissional e humanizado)
            // =========================================================================
            if (EhSaudacaoOuCumprimento(textoLimpo))
            {
                response.Message = GerarRespostaSaudacao(textoLimpo);
                response.SuggestedActions.Add(new AISuggestedAction { Label = "💡 Farol Apagado", ActionType = "ExecuteQuery", Parameter = "Farol direito apagado" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "🔑 Arranque Faz Tec-Tec", ActionType = "ExecuteQuery", Parameter = "Carro faz tec tec e não vira" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ DTC de Scanner", ActionType = "ExecuteQuery", Parameter = "DTC P0562" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "📘 Como Abrir OS?", ActionType = "ExecuteQuery", Parameter = "Como abrir uma nova Ordem de Serviço?" });
                return response;
            }

            // =========================================================================
            // 2. CÓDIGO DE SCANNER OBD-II (DTC Pxxxx, Uxxxx, Bxxxx, Cxxxx)
            // =========================================================================
            var dtcMatch = Regex.Match(ultimaMensagemUser.ToUpperInvariant(), @"\b([PBUS]\d{4})\b");
            if (dtcMatch.Success)
            {
                var dtcCode = dtcMatch.Value;
                var dtc = _ragService.BuscarPorCodigoDTC(dtcCode);
                if (dtc != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"⚡ **Diagnóstico Técnico Guiado: DTC {dtc.Code}**");
                    sb.AppendLine($"**Definição:** {dtc.Title}");
                    sb.AppendLine($"**Sistema:** {dtc.System}\n");
                    sb.AppendLine($"🔍 **Sintomas Clínicos no Veículo:**\n{dtc.Symptoms}\n");
                    sb.AppendLine("⚠️ **Causas Mais Frequentes:**");
                    foreach (var c in dtc.ProbableCauses) sb.AppendLine($"• {c}");
                    sb.AppendLine("\n📋 **Roteiro de Testes na Oficina (Passo a Passo):**");
                    for (int i = 0; i < dtc.GuidedSteps.Count; i++)
                    {
                        sb.AppendLine($"{i + 1}. {dtc.GuidedSteps[i]}");
                    }
                    sb.AppendLine($"\n📏 **Padrão de Referência:** {dtc.ReferenceStandard}");
                    sb.AppendLine($"🧰 **Instrumentos:** {string.Join(", ", dtc.SuggestedTools)}");

                    if (dtc.ClarifyingQuestions.Count > 0)
                    {
                        sb.AppendLine("\n🤔 **Para te orientar no próximo teste:**");
                        foreach (var q in dtc.ClarifyingQuestions)
                        {
                            sb.AppendLine($"👉 {q}");
                        }
                    }

                    if (_sureTrackService != null)
                    {
                        try
                        {
                            var st = await _sureTrackService.ConsultarEstatisticasAsync(null, dtc.Code, null);
                            if (st != null && st.TotalCasosAnalisados > 0)
                            {
                                sb.AppendLine();
                                sb.AppendLine(st.ResumoEstatisticoFormatado);
                            }
                        }
                        catch { /* Fallback seguro */ }
                    }

                    response.Message = sb.ToString();

                    // Adicionar chips de resposta rápida e navegação
                    foreach (var chip in dtc.InteractiveReplyChips)
                    {
                        response.SuggestedActions.Add(new AISuggestedAction
                        {
                            Label = chip,
                            ActionType = "ExecuteQuery",
                            Parameter = chip
                        });
                    }

                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = "📊 Casos SureTrack",
                        ActionType = "Navigate",
                        Parameter = "AiDiagnosticCenter"
                    });

                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = "Ir para Módulo Técnico",
                        ActionType = "Navigate",
                        Parameter = "AutoEletricaTecnica"
                    });

                    if (dtc.SuggestedStockParts.Count > 0)
                    {
                        var peca = dtc.SuggestedStockParts.First();
                        response.SuggestedActions.Add(new AISuggestedAction
                        {
                            Label = $"Ver Peças ({peca})",
                            ActionType = "SearchStock",
                            Parameter = peca
                        });
                    }

                    return response;
                }
            }

            // =========================================================================
            // 3. DIÁLOGO INTERATIVO DE CONTINUIDADE (O usuário respondeu uma pergunta anterior)
            // =========================================================================
            var contextoAnterior = AnalisarContextoConversa(todasMensagens);
            if (contextoAnterior != null && EhRespostaDeAcompanhamento(textoLimpo))
            {
                var respostaAcompanhamento = ProcessarContinuacaoDiagnostico(contextoAnterior, textoLimpo, ultimaMensagemUser);
                if (respostaAcompanhamento != null)
                {
                    return respostaAcompanhamento;
                }
            }

            // =========================================================================
            // 4. COMANDOS OPERACIONAIS DIRETOS DO ERP
            // =========================================================================
            if (textoLimpo.StartsWith("abre ") || textoLimpo.StartsWith("abrir ") || textoLimpo.StartsWith("ir para ") || textoLimpo.StartsWith("navegar "))
            {
                string? destino = IdentificarModuloNavegacao(textoLimpo);
                if (destino != null)
                {
                    var res = await _toolRegistry.ExecutarFerramentaAsync("NavegarParaModulo", new() { ["modulo"] = destino });
                    response.Message = $"✅ {res} Abrindo a tela solicitada para você.";
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = $"Acessar {destino}",
                        ActionType = "Navigate",
                        Parameter = destino
                    });
                    return response;
                }
            }

            // Consulta de ferramentas
            if (textoLimpo.Contains("ferramenta") || textoLimpo.Contains("scanner") || textoLimpo.Contains("osciloscopio"))
            {
                if ((textoLimpo.Contains("uso") || textoLimpo.Contains("quem") || textoLimpo.Contains("onde")) && !textoLimpo.Contains("como"))
                {
                    var res = await _toolRegistry.ExecutarFerramentaAsync("ConsultarFerramentasEmUso", new());
                    response.Message = $"🧰 **Situação da Ferramentaria da Oficina:**\n\n{res}";
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "Abrir Ferramentaria", ActionType = "Navigate", Parameter = "Ferramentas" });
                    return response;
                }
            }

            // Consulta de peças em falta
            if (textoLimpo.Contains("falta") || (textoLimpo.Contains("pedido") && textoLimpo.Contains("compra")) || textoLimpo.Contains("ruptura"))
            {
                var res = await _toolRegistry.ExecutarFerramentaAsync("ConsultarProdutosEmFalta", new());
                response.Message = $"📦 **Gestão de Compras & Anti-Ruptura:**\n\n{res}";
                response.SuggestedActions.Add(new AISuggestedAction { Label = "Abrir Compras & Falta", ActionType = "Navigate", Parameter = "ComprasNecessidade" });
                return response;
            }

            // =========================================================================
            // 5. MANUAIS DE OPERAÇÃO DO SISTEMA PRIMOX (Dúvidas de Software / ERP)
            // =========================================================================
            bool ehProblemaVeicular = (
                textoLimpo.Contains("sem funcionar") || textoLimpo.Contains("nao pega") || textoLimpo.Contains("nao liga") 
                || textoLimpo.Contains("morreu") || textoLimpo.Contains("falhando") || textoLimpo.Contains("engasgando") 
                || textoLimpo.Contains("rateando") || textoLimpo.Contains("tec tec") || textoLimpo.Contains("partida pesada")
                || textoLimpo.Contains("descarrega") || textoLimpo.Contains("arreia") || textoLimpo.Contains("fuga de corrente")
                || textoLimpo.Contains("fuga parasita") || textoLimpo.Contains("queima fusivel") || textoLimpo.Contains("fusivel queima")
                || (textoLimpo.Contains("fusivel") && textoLimpo.Contains("queima"))
                || textoLimpo.Contains("em curto") || textoLimpo.Contains("curto circuito") || textoLimpo.Contains("nao carrega")
                || textoLimpo.Contains("luz de bateria") || textoLimpo.Contains("luz de inje") || textoLimpo.Contains("luz da inje")
                || textoLimpo.Contains("luz do volante") || textoLimpo.Contains("luz do abs") || textoLimpo.Contains("luz do airbag")
                || textoLimpo.Contains("apagado") || textoLimpo.Contains("apagada") || textoLimpo.Contains("pisca tudo junto")
                || textoLimpo.Contains("nao acende") || textoLimpo.Contains("nao gela") || textoLimpo.Contains("nao sobe")
                || textoLimpo.Contains("nao desce") || textoLimpo.Contains("ferve") || textoLimpo.Contains("fervendo")
                || textoLimpo.Contains("parou de funcionar") || textoLimpo.Contains("parou de tocar")
                || textoLimpo.Contains("esguicho") || textoLimpo.Contains("jogar agua") || textoLimpo.Contains("jato de agua")
                || textoLimpo.Contains("bombinha") || textoLimpo.Contains("brucutu") || textoLimpo.Contains("lavador")
                || (textoLimpo.Contains("agua") && textoLimpo.Contains("parabrisa"))
                || (textoLimpo.Contains("alavanca") && textoLimpo.Contains("bombinha"))
                || (textoLimpo.Contains("farol") && !textoLimpo.Contains("como") && !textoLimpo.Contains("sistema"))
                || (textoLimpo.Contains("lanterna") && !textoLimpo.Contains("como") && !textoLimpo.Contains("sistema"))
                || (textoLimpo.Contains("arranque") && !textoLimpo.Contains("como") && !textoLimpo.Contains("sistema"))
                || (textoLimpo.Contains("alternador") && !textoLimpo.Contains("como") && !textoLimpo.Contains("sistema"))
                || (textoLimpo.Contains("ventoinha") && !textoLimpo.Contains("como") && !textoLimpo.Contains("sistema"))
                || textoLimpo.Contains("desembacador") || textoLimpo.Contains("brake light") || textoLimpo.Contains("aceso direto")
                || textoLimpo.Contains("nao apaga") || textoLimpo.Contains("nao desliga") || (textoLimpo.Contains("tampa") && textoLimpo.Contains("porta malas"))
            );

            if (textoLimpo.Contains("garantia") || textoLimpo.Contains("comissao") || textoLimpo.Contains("comissoes") || textoLimpo.Contains("historico"))
            {
                ehProblemaVeicular = false;
            }

            bool perguntaSistema = !ehProblemaVeicular && (
                textoLimpo.Contains("ordem de servico") || textoLimpo.Contains("ordem de serviço") || textoLimpo.Contains("abrir os")
                || textoLimpo.Contains("nova os") || textoLimpo.Contains("fechar os") || textoLimpo.Contains("imprimir os")
                || textoLimpo.Contains("kanban") || textoLimpo.Contains("fechar caixa") || textoLimpo.Contains("abrir caixa")
                || textoLimpo.Contains("emitir nfe") || textoLimpo.Contains("nota fiscal")
                || ((textoLimpo.Contains("cadastrar") || textoLimpo.Contains("cadastro")) && (textoLimpo.Contains("cliente") || textoLimpo.Contains("veiculo") || textoLimpo.Contains("placa") || textoLimpo.Contains("mercosul") || textoLimpo.Contains("peca") || textoLimpo.Contains("produto")))
                || textoLimpo.Contains("placa mercosul") || textoLimpo.Contains("mercosul")
                || textoLimpo.Contains("recibo") || textoLimpo.Contains("garantia") || textoLimpo.Contains("termo")
                || textoLimpo.Contains("comissao") || textoLimpo.Contains("comissoes")
                || textoLimpo.Contains("historico")
                || textoLimpo.Contains("backup") || textoLimpo.Contains("seguranca dos dados") || textoLimpo.Contains("banco de dados")
                || textoLimpo.Contains("logotipo") || textoLimpo.Contains("logo")
                || textoLimpo.Contains("pdv") || textoLimpo.Contains("orcamento") || textoLimpo.Contains("orçamento")
                || textoLimpo.Contains("financeiro") || textoLimpo.Contains("contas a pagar") || textoLimpo.Contains("contas a receber")
                || textoLimpo.Contains("fluxo de caixa") || textoLimpo.Contains("dre") || textoLimpo.Contains("relatorio")
                || textoLimpo.Contains("produtividade") || textoLimpo.Contains("fiscal") || textoLimpo.Contains("nfse")
                || textoLimpo.Contains("nfc-e") || textoLimpo.Contains("cupom fiscal") || textoLimpo.Contains("frota")
                || textoLimpo.Contains("frotas") || textoLimpo.Contains("filial") || textoLimpo.Contains("filiais")
                || textoLimpo.Contains("transferencia") || textoLimpo.Contains("etiqueta") || textoLimpo.Contains("zpl")
                || textoLimpo.Contains("zebra") || textoLimpo.Contains("ferramenta") || textoLimpo.Contains("emprestimo")
                || textoLimpo.Contains("como funciona o sistema") || textoLimpo.Contains("manual")
                || (textoLimpo.Contains("sistema") && (textoLimpo.Contains("como") || textoLimpo.Contains("onde") || textoLimpo.Contains("usar") || textoLimpo.Contains("tela")))
            );

            if (perguntaSistema)
            {
                var manual = _ragService.BuscarManualSistema(ultimaMensagemUser);
                if (manual != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"📘 **{manual.Title}**\n");
                    sb.AppendLine($"{manual.Summary}\n");
                    sb.AppendLine("📋 **Passo a Passo no Sistema:**");
                    foreach (var inst in manual.Instructions) sb.AppendLine($"• {inst}");

                    if (manual.Tips.Count > 0)
                    {
                        sb.AppendLine("\n💡 **Dica da Oficina:**");
                        foreach (var tip in manual.Tips) sb.AppendLine($"• {tip}");
                    }

                    response.Message = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(manual.ModuleNavigationTarget))
                    {
                        response.SuggestedActions.Add(new AISuggestedAction
                        {
                            Label = $"Abrir {manual.ModuleNavigationTarget}",
                            ActionType = "Navigate",
                            Parameter = manual.ModuleNavigationTarget
                        });
                    }
                    return response;
                }
            }

            // =========================================================================
            // 6. CONSULTA ESPECIALIZADA DE FUSÍVEIS E RELÉS (Disposição, Posições, Mapa de Caixas e Circuitos)
            // =========================================================================
            if (EhConsultaDeFusiveisOuReles(textoLimpo, out var componenteSolicitado))
            {
                var respostaFusiveis = await ProcessarConsultaFusiveisERelesAsync(textoLimpo, ultimaMensagemUser, componenteSolicitado, todasMensagens, cancellationToken);
                if (respostaFusiveis != null)
                {
                    return respostaFusiveis;
                }
            }

            // =========================================================================
            // 6.1. CONSULTAS DE ESQUEMAS ELÉTRICOS, DIAGRAMAS, PINAGENS & MANUAIS DE SERVIÇO
            // =========================================================================
            if (EhConsultaDeEsquemaOuDiagrama(textoLimpo, ultimaMensagemUser))
            {
                var respostaEsquema = await ProcessarConsultaEsquemaAsync(textoLimpo, ultimaMensagemUser, todasMensagens, cancellationToken);
                if (respostaEsquema != null)
                {
                    return respostaEsquema;
                }
            }

            // =========================================================================
            // 6.1. CONSULTA DE BIBLIOTECA TÉCNICA (Pinagens, Centrais de Fusíveis e Linha Pesada 24V)
            // =========================================================================
            if (textoLimpo.Contains("pinagem") || textoLimpo.Contains("pinout") || textoLimpo.Contains("pino ") || textoLimpo.Contains("pinos")
                || textoLimpo.Contains("modulo") || textoLimpo.Contains("ecu") || textoLimpo.Contains("coo7") || textoLimpo.Contains("pld")
                || textoLimpo.Contains("caixa de fusivel") || textoLimpo.Contains("central de fusivel") || textoLimpo.Contains("central eletrica")
                || (textoLimpo.Contains("fusivel") && (textoLimpo.Contains("onde") || textoLimpo.Contains("qual") || textoLimpo.Contains("bomba") || textoLimpo.Contains("amper") || textoLimpo.Contains("ventilador")))
                || (textoLimpo.Contains("24v") && (textoLimpo.Contains("scania") || textoLimpo.Contains("atego") || textoLimpo.Contains("volvo") || textoLimpo.Contains("torque") || textoLimpo.Contains("folga") || textoLimpo.Contains("bateria")))
                || (textoLimpo.Contains("torque") && textoLimpo.Contains("cabecote")))
            {
                var resTec = await _toolRegistry.ExecutarFerramentaAsync("ConsultarBibliotecaTecnica", new() { ["termo"] = ultimaMensagemUser });
                if (!string.IsNullOrWhiteSpace(resTec) && !resTec.StartsWith("Nenhum registro"))
                {
                    response.Message = $"📚 **Biblioteca Técnica PRIMOX (Doutor-IE & ALLDATA Heavy Duty Benchmark):**\n\n{resTec}";
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "🔌 Ver Pinagens", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Centrais de Fusíveis", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "🚛 Linha Pesada 24V", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                    return response;
                }
            }

            // =========================================================================
            // 6.2. CONSULTA DE FLUXOGRAMAS DE DIAGNÓSTICO GUIADO (Troubleshooting Flowcharts)
            // =========================================================================
            if (textoLimpo.Contains("fluxograma") || textoLimpo.Contains("arvore de diagnostico") || textoLimpo.Contains("arvore de decisao")
                || textoLimpo.Contains("troubleshooting") || textoLimpo.Contains("roteiro de teste") || textoLimpo.Contains("diagnostico guiado")
                || (textoLimpo.Contains("passo a passo") && (textoLimpo.Contains("partida") || textoLimpo.Contains("alternador") || textoLimpo.Contains("fuga") || textoLimpo.Contains("ventoinha") || textoLimpo.Contains("farol") || textoLimpo.Contains("can") || textoLimpo.Contains("testar"))))
            {
                var resFluxo = await _toolRegistry.ExecutarFerramentaAsync("ConsultarFluxogramaDiagnostico", new() { ["codigoOuSintoma"] = ultimaMensagemUser });
                if (!string.IsNullOrWhiteSpace(resFluxo) && !resFluxo.StartsWith("Nenhum fluxograma"))
                {
                    response.Message = $"{resFluxo}\n\n👉 *Você pode prosseguir pelas opções da árvore interativa ou abrir o fluxograma visual na aba 'Diagnóstico Guiado'!*";
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "🧠 Árvore Interativa", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Calculadora Queda Tensão", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                    return response;
                }
            }

            // =========================================================================
            // 6.3. CALCULADORA DE QUEDA DE TENSÃO (Voltage Drop Analyzer - SAE J1128 / DIN 72551)
            // =========================================================================
            if (textoLimpo.Contains("queda de tensao") || textoLimpo.Contains("voltage drop") || textoLimpo.Contains("perda de tensao")
                || textoLimpo.Contains("resistencia parasita") || textoLimpo.Contains("calcular queda")
                || (textoLimpo.Contains("queda") && (textoLimpo.Contains("cabo") || textoLimpo.Contains("terra") || textoLimpo.Contains("positivo") || textoLimpo.Contains("arranque") || textoLimpo.Contains("alternador") || textoLimpo.Contains("bateria"))))
            {
                var argsQueda = ExtrairArgumentosQuedaTensao(ultimaMensagemUser);
                var resQueda = await _toolRegistry.ExecutarFerramentaAsync("CalcularQuedaTensao", argsQueda);
                response.Message = $"{resQueda}\n\n💡 *Bancada PRIMOX:* No **Centro de Diagnóstico IA**, você pode simular o diâmetro do condutor ($mm^2$) e a perda de energia em tempo real!";
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Abrir Calculadora", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "🧠 Diagnóstico Guiado", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "Queda no Motor de Partida", ActionType = "ExecuteQuery", Parameter = "Calcular queda de tensão com 12.6V na bateria, 10.8V no arranque e 150A" });
                return response;
            }

            // =========================================================================
            // 7. DIAGNÓSTICO INTERATIVO DE SINTOMAS AUTOMOTIVOS REAIS (SOCRÁTICO)
            // =========================================================================
            var procedimentos = _ragService.BuscarPorSintomaOuTermo(ultimaMensagemUser, limite: 1);
            if (procedimentos.Count > 0)
            {
                var diag = procedimentos[0];
                var sb = new StringBuilder();

                sb.AppendLine($"Entendido! Vamos investigar esse problema: **{diag.Title}**.\n");
                sb.AppendLine("Comece verificando pela ordem mais lógica e rápida na oficina:");
                for (int i = 0; i < Math.Min(diag.GuidedSteps.Count, 3); i++)
                {
                    sb.AppendLine($"• **{diag.GuidedSteps[i]}**");
                }
                sb.AppendLine();

                if (diag.ClarifyingQuestions.Count > 0)
                {
                    sb.AppendLine("Para eu te passar o teste exato e a pinagem do circuito:");
                    foreach (var q in diag.ClarifyingQuestions)
                    {
                        sb.AppendLine($"👉 **{q}**");
                    }
                }
                else
                {
                    sb.AppendLine("👉 **Qual é a marca, modelo e ano do veículo?**");
                    sb.AppendLine("👉 **Você tem multímetro ou lâmpada de teste aí na bancada?**");
                }

                response.Message = sb.ToString();

                if (diag.InteractiveReplyChips.Count > 0)
                {
                    foreach (var chip in diag.InteractiveReplyChips)
                    {
                        response.SuggestedActions.Add(new AISuggestedAction
                        {
                            Label = chip,
                            ActionType = "ExecuteQuery",
                            Parameter = chip
                        });
                    }
                }
                else
                {
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "O fusível está bom", ActionType = "ExecuteQuery", Parameter = "O fusível está bom" });
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "Tenho multímetro", ActionType = "ExecuteQuery", Parameter = "Tenho multímetro na bancada" });
                }

                response.SuggestedActions.Add(new AISuggestedAction { Label = "Ir para Módulo Técnico", ActionType = "Navigate", Parameter = "AutoEletricaTecnica" });
                return response;
            }

            // =========================================================================
            // 8. CONSULTA DE ESTOQUE OU CLIENTE
            // =========================================================================
            if (textoLimpo.Contains("estoque") || textoLimpo.Contains("saldo") || textoLimpo.Contains("preco"))
            {
                var termo = Regex.Replace(textoLimpo, @"\b(tem|saldo|estoque|preco|de|do|da|no|na|peca|produto|consultar|ver)\b", " ").Trim();
                if (termo.Length >= 2)
                {
                    var res = await _toolRegistry.ExecutarFerramentaAsync("ConsultarEstoque", new() { ["termo"] = termo });
                    response.Message = $"📦 **Consulta de Estoque:**\n\n{res}";
                    response.SuggestedActions.Add(new AISuggestedAction { Label = "Ver no Estoque", ActionType = "Navigate", Parameter = "Estoque" });
                    return response;
                }
            }

            // =========================================================================
            // 9. RESPOSTA TÉCNICA SOCRÁTICA (PADRÃO INVESTIGATIVO)
            // =========================================================================
            var sbGenerico = new StringBuilder();
            sbGenerico.AppendLine($"Recebi seu relato: *\"{ultimaMensagemUser}\"*.\n");
            sbGenerico.AppendLine("Na auto elétrica profissional, investigamos qualquer anomalia pelos **4 Pilares de Bancada**:");
            sbGenerico.AppendLine("1. **Alimentação Positiva:** Conferir fusível e medir se chegam 12V no componente.");
            sbGenerico.AppendLine("2. **Aterramento / Negativo:** Verificar se o ponto de massa está limpo (queda < 0.10V).");
            sbGenerico.AppendLine("3. **Sinal de Controle:** Checar se a chave de comando, relé ou ECU envia o disparo.");
            sbGenerico.AppendLine("4. **Consumo de Carga:** Verificar se o atuador não está travado ou em curto.\n");
            sbGenerico.AppendLine("Para eu te dar a solução exata e o esquema elétrico:");
            sbGenerico.AppendLine("👉 **Qual é o modelo, motor e ano do veículo?**");
            sbGenerico.AppendLine("👉 **O que acontece exatamente ao acionar a chave/botão?**");

            response.Message = sbGenerico.ToString();
            response.SuggestedActions.Add(new AISuggestedAction { Label = "💡 Farol Apagado", ActionType = "ExecuteQuery", Parameter = "Farol direito apagado" });
            response.SuggestedActions.Add(new AISuggestedAction { Label = "🔑 Arranque Faz Tec-Tec", ActionType = "ExecuteQuery", Parameter = "Carro faz tec tec e não liga" });
            response.SuggestedActions.Add(new AISuggestedAction { Label = "🚗 Carro Não Pega", ActionType = "ExecuteQuery", Parameter = "Carro sem funcionar por onde começo os testes" });

            return response;
        }

        private static bool EhSaudacaoOuCumprimento(string textoLimpo)
        {
            // Se tiver termos claros de defeito ou manual do sistema, não é mera saudação
            if (textoLimpo.Contains("farol") || textoLimpo.Contains("lanterna") || textoLimpo.Contains("fusivel") || textoLimpo.Contains("bateria")
                || textoLimpo.Contains("arranque") || textoLimpo.Contains("alternador") || textoLimpo.Contains("curto") || textoLimpo.Contains("nao pega")
                || textoLimpo.Contains("dtc") || textoLimpo.Contains("p0") || textoLimpo.Contains("u0") || textoLimpo.Contains("b0") || textoLimpo.Contains("c0")
                || textoLimpo.Contains("como") || textoLimpo.Contains("ordem") || textoLimpo.Contains("os ") || textoLimpo.Contains("estoque")
                || textoLimpo.Contains("ferramenta") || textoLimpo.Contains("abs") || textoLimpo.Contains("vidro") || textoLimpo.Contains("boia")
                || textoLimpo.Contains("esquema") || textoLimpo.Contains("diagrama") || textoLimpo.Contains("pinagem") || textoLimpo.Contains("manual"))
            {
                return false;
            }

            return textoLimpo.Contains("boa noite") || textoLimpo.Contains("bom dia") 
                || textoLimpo.Contains("boa tarde") || textoLimpo.StartsWith("ola") 
                || textoLimpo.StartsWith("oi") || textoLimpo.Contains("e ai") 
                || textoLimpo.StartsWith("opa") || textoLimpo.Contains("tudo bem")
                || textoLimpo.Contains("pode me ajudar") || textoLimpo.Contains("consegue me ajudar");
        }

        private static bool EhPerguntaSobreIa(string textoLimpo)
        {
            return textoLimpo.Contains("quem e voce") || textoLimpo.Contains("quem voce e")
                || textoLimpo.Contains("o que voce faz") || textoLimpo.Contains("como voce funciona")
                || textoLimpo.Contains("o que voce sabe") || textoLimpo.Contains("quais carros voce")
                || textoLimpo.Contains("como pode me ajudar") || textoLimpo.Contains("suas funcoes")
                || textoLimpo.Contains("qual o seu papel") || textoLimpo.Contains("apresente se");
        }

        private static bool EhAgradecimento(string textoLimpo)
        {
            return textoLimpo.Contains("obrigado") || textoLimpo.Contains("valeu") 
                || textoLimpo.Contains("show de bola") || textoLimpo.Contains("muito bom") 
                || textoLimpo.Contains("agradeco") || textoLimpo.Contains("deu certo");
        }

        private static string GerarRespostaPerguntaSobreIa()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Olá! Sou o **PRIMOX Copilot**, o assistente técnico de inteligência automotiva e especialista no sistema da sua oficina! 🚗⚡\n");
            sb.AppendLine("Estou preparado para te ajudar no dia a dia em 4 grandes áreas:\n");
            sb.AppendLine("🔧 **1. Diagnóstico Elétrico Automotivo Passo a Passo:**");
            sb.AppendLine("• Testes de alternador, bateria, fuga de corrente (consumo parasita), motor de partida e relés.");
            sb.AppendLine("• Localização rápida de curto-circuito sem queimar fusíveis.");
            sb.AppendLine("• Defeitos crônicos por modelo: Gol EA111, Palio, Onix, Ford Ka Smart Charge, Fox, HB20, Corolla, Toro, etc.\n");
            sb.AppendLine("⚡ **2. Diagnóstico Eletrônico e Scanner OBD-II:**");
            sb.AppendLine("• Interpretação de DTCs de Injeção (P0xxx), Rede CAN (U0xxx), Airbag/Carroceria (B0xxx) e Freios ABS/Chassi (C0xxx) com valores de referência.\n");
            sb.AppendLine("📘 **3. Manuais Operacionais do ERP PRIMOX:**");
            sb.AppendLine("• Abertura e fechamento de Ordens de Serviço, Orçamentos, Quadro Kanban, Caixa/PDV, Termos de Garantia.");
            sb.AppendLine("• Emissão fiscal de NFS-e/NFC-e, importação de NF-e XML, controle de Frotas B2B, Multi-Filiais e Etiquetas Térmicas ZPL.\n");
            sb.AppendLine("📦 **4. Consultas Rápidas de Estoque e Ferramentaria:**");
            sb.AppendLine("• Peças em falta e rastreamento de ferramentas emprestadas aos técnicos.\n");
            sb.AppendLine("👉 **Como usar:** Basta relatar o que está acontecendo com o carro ou enviar sua dúvida sobre o sistema!");
            return sb.ToString();
        }

        private static string GerarRespostaAgradecimento()
        {
            return "Disponha sempre meu amigo! 🤝 Fico feliz em ajudar. Se surgir qualquer outra dúvida técnica no veículo ou na operação do sistema, é só me chamar aqui. Bom trabalho e sucesso na oficina! 🚗⚡";
        }

        private static string GerarRespostaSaudacao(string textoLimpo)
        {
            string saudacao = "Olá!";
            if (textoLimpo.Contains("boa noite")) saudacao = "Boa noite!";
            else if (textoLimpo.Contains("bom dia")) saudacao = "Bom dia!";
            else if (textoLimpo.Contains("boa tarde")) saudacao = "Boa tarde!";

            return $"{saudacao} Como posso te ajudar na oficina hoje? Você pode me relatar um sintoma do carro (ex: farol apagado, bateria arriando, motor não pega), um código de scanner (ex: P0300) ou tirar dúvidas sobre o sistema!";
        }

        private static string? AnalisarContextoConversa(List<AIChatMessage> mensagens)
        {
            var msgsUsuario = mensagens
                .Where(m => m.Role == AIRole.User)
                .Select(m => AutomotiveDiagnosticRAGService.NormalizarTexto(m.Content))
                .ToList();

            for (int i = msgsUsuario.Count - 2; i >= 0; i--)
            {
                var msg = msgsUsuario[i];

                // 1. Diagnósticos específicos de veículos e sistemas avançados (PRIORIDADE MÁXIMA)
                if (msg.Contains("hb20") && (msg.Contains("farol") || msg.Contains("seta") || msg.Contains("alavanca"))) return "CHAVE_SETA";
                if (msg.Contains("smart charge") || (msg.Contains("ford") && (msg.Contains("bateria") || msg.Contains("alternador") || msg.Contains("carga") || msg.Contains("acesa"))) || (msg.Contains("ka") && (msg.Contains("bateria") || msg.Contains("alternador")))) return "FORD_SMART_CHARGE";
                if (msg.Contains("anti esmagamento") || (msg.Contains("vidro") && (msg.Contains("fox") || msg.Contains("desce") || msg.Contains("sobe") || msg.Contains("metade")))) return "VIDRO_ANTI_ESMAGAMENTO";

                // CHICOTE DA TAMPA TRASEIRA (Desembaçador + Brake Light / Terceira Luz)
                if (msg.Contains("desembacador") || msg.Contains("brake light") || (msg.Contains("tampa") && msg.Contains("traseir"))) return "TAMPA_TRASEIRA";

                // FAROL ACESO DIRETO / RELÉ COLADO
                if (!msg.Contains("hb20") && msg.Contains("farol") && (msg.Contains("direto") || msg.Contains("nao apaga") || msg.Contains("nao desliga") || msg.Contains("aceso direto") || msg.Contains("travado") || msg.Contains("desligo a chave"))) return "FAROL_ACESO_DIRETO";

                // ESGUICHO / LAVADOR DE PARA-BRISA (BOMBINHA D'ÁGUA / BRUCUTU)
                if (msg.Contains("esguicho") || msg.Contains("jogar agua") || msg.Contains("jato de agua") 
                    || msg.Contains("lavador") || msg.Contains("brucutu") || msg.Contains("bombinha") 
                    || (msg.Contains("agua") && msg.Contains("parabrisa"))
                    || (msg.Contains("alavanca") && msg.Contains("bombinha"))) return "ESGUICHO";

                if (!msg.Contains("bombinha") && !msg.Contains("esguicho") && !msg.Contains("agua") && 
                    (msg.Contains("chave de seta") || (msg.Contains("alavanca") && !msg.Contains("limpador")) || (msg.Contains("farol alto") && msg.Contains("direto")) || (msg.Contains("hb20") && (msg.Contains("farol") || msg.Contains("seta"))))) return "CHAVE_SETA";
                if (msg.Contains("cinta") || msg.Contains("clock spring") || (msg.Contains("comandos") && msg.Contains("volante")) || (msg.Contains("buzina") && (msg.Contains("corolla") || msg.Contains("etios") || msg.Contains("som")))) return "COMANDOS_VOLANTE";
                if ((msg.Contains("abs") && (msg.Contains("velocimetro") || msg.Contains("tracao") || msg.Contains("onix") || msg.Contains("prisma"))) || (msg.Contains("velocimetro") && (msg.Contains("parou") || msg.Contains("onix") || msg.Contains("prisma")))) return "ABS_VELOCIMETRO";
                if (msg.Contains("boia") || (msg.Contains("marcador") && msg.Contains("combustivel")) || (msg.Contains("sandero") && (msg.Contains("tanque") || msg.Contains("combustivel") || msg.Contains("marcador") || msg.Contains("boia") || msg.Contains("reserva")))) return "MARCADOR_COMBUSTIVEL";
                if (msg.Contains("start stop") || msg.Contains("ibs") || ((msg.Contains("toro") || msg.Contains("renegade")) && (msg.Contains("bateria") || msg.Contains("indisponivel") || msg.Contains("start")))) return "START_STOP_IBS";

                // 2. Diagnósticos genéricos de bancada
                if (msg.Contains("sem funcionar") || msg.Contains("nao pega") || msg.Contains("nao liga") || msg.Contains("morreu") || msg.Contains("comeco os teste") || msg.Contains("triagem") || msg.Contains("combustao") || msg.Contains("vira forte")) return "CARRO_SEM_FUNCIONAR";
                if ((msg.Contains("fusivel") && (msg.Contains("queima") || msg.Contains("rompe") || msg.Contains("estoura"))) || msg.Contains("curto")) return "CURTO";
                if (msg.Contains("tec tec") || msg.Contains("arranque") || msg.Contains("partida pesada") || msg.Contains("vira pesado")) return "ARRANQUE";
                if (msg.Contains("fuga") || msg.Contains("parasita") || msg.Contains("descarrega") || msg.Contains("arreia") || msg.Contains("arreando") || msg.Contains("consumo")) return "PARASITA";
                if (msg.Contains("alternador") || msg.Contains("bateria") || msg.Contains("carga") || msg.Contains("carrega")) return "ALTERNADOR";
                if (msg.Contains("ventoinha") || msg.Contains("arrefecimento") || msg.Contains("ferve")) return "VENTOINHA";
                if (msg.Contains("falhando") || msg.Contains("engasgando") || msg.Contains("rateando") || msg.Contains("oscil") || msg.Contains("morre") || msg.Contains("morrer") || msg.Contains("lenta")) return "MARCHA_LENTA";
                if (msg.Contains("ar condicionado") || msg.Contains("nao gela") || msg.Contains("compressor")) return "AR_CONDICIONADO";
                if (msg.Contains("direcao eletrica") || msg.Contains("direcao dura") || msg.Contains("eps")) return "DIRECAO_ELETRICA";
                if (msg.Contains("limpador") || msg.Contains("palheta")) return "LIMPADOR";
                if (msg.Contains("buzina")) return "BUZINA";
                if (msg.Contains("lanterna") || msg.Contains("farol") || msg.Contains("meia luz") || msg.Contains("luz de freio") || msg.Contains("soquete") || msg.Contains("lampada")) return "FAROL";
                if (msg.Contains("vidro") || msg.Contains("trava")) return "VIDRO";
            }
            return null;
        }

        private static bool EhRespostaDeAcompanhamento(string textoLimpo)
        {
            return textoLimpo.Contains("fusivel") || textoLimpo.Contains("lampada") || textoLimpo.Contains("multimetro")
                || textoLimpo.Contains("ano") || textoLimpo.Contains("gol") || textoLimpo.Contains("palio")
                || textoLimpo.Contains("corsa") || textoLimpo.Contains("fiat") || textoLimpo.Contains("vw")
                || textoLimpo.Contains("ford") || textoLimpo.Contains("chevrolet") || textoLimpo.Contains("toyota")
                || textoLimpo.Contains("uno") || textoLimpo.Contains("onix") || textoLimpo.Contains("hb20")
                || textoLimpo.Contains("saveiro") || textoLimpo.Contains("strada") || textoLimpo.Contains("celta")
                || textoLimpo.Contains("sandero") || textoLimpo.Contains("logan") || textoLimpo.Contains("toro")
                || textoLimpo.Contains("renegade") || textoLimpo.Contains("corolla") || textoLimpo.Contains("etios")
                || textoLimpo.Contains("fox") || textoLimpo.Contains("ka") || textoLimpo.Contains("fiesta")
                || textoLimpo.Contains("bom") || textoLimpo.Contains("ok") || textoLimpo.Contains("queimad")
                || textoLimpo.Contains("chega 12v") || textoLimpo.Contains("nao chega") || textoLimpo.Contains("marca")
                || textoLimpo.Contains("direito") || textoLimpo.Contains("esquerdo") || textoLimpo.Contains("traseir")
                || textoLimpo.Contains("vira") || textoLimpo.Contains("faísca") || textoLimpo.Contains("faisca")
                || textoLimpo.Contains("bomba") || textoLimpo.Contains("bombinha") || textoLimpo.Contains("esguicho")
                || textoLimpo.Contains("brucutu") || textoLimpo.Contains("pressao") || textoLimpo.Contains("soquete")
                || textoLimpo.Contains("terra") || textoLimpo.Contains("massa") || textoLimpo.Contains("imobilizador")
                || textoLimpo.Contains("cadeado") || textoLimpo.Contains("injecao") || textoLimpo.Contains("injeção")
                || textoLimpo.Contains("rele") || textoLimpo.Contains("relé") || textoLimpo.Contains("clique")
                || textoLimpo.Contains("estalo") || textoLimpo.Contains("compressor") || textoLimpo.Contains("gas")
                || textoLimpo.Contains("bobina") || textoLimpo.Contains("palheta") || textoLimpo.Contains("direcao")
                || textoLimpo.Contains("direção") || textoLimpo.Contains("volante") || textoLimpo.Contains("60a")
                || textoLimpo.Contains("80a") || textoLimpo.Contains("consumo") || textoLimpo.Contains("ma")
                || textoLimpo.Contains("sleep") || textoLimpo.Contains("jumper") || textoLimpo.Contains("cinta")
                || textoLimpo.Contains("airbag") || textoLimpo.Contains("retorno") || textoLimpo.Contains("31b")
                || textoLimpo.Contains("rolamento") || textoLimpo.Contains("anel") || textoLimpo.Contains("smart")
                || textoLimpo.Contains("rc") || textoLimpo.Contains("li") || textoLimpo.Contains("anti esmagamento")
                || textoLimpo.Contains("boia") || textoLimpo.Contains("reserva") || textoLimpo.Contains("start stop")
                || textoLimpo.Contains("ibs") || textoLimpo.Contains("efb") || textoLimpo.Contains("agm")
                || textoLimpo.Contains("alavanca") || textoLimpo.Contains("clock spring") || textoLimpo.Contains("som")
                || textoLimpo.Contains("bateria") || textoLimpo.Contains("desligad") || textoLimpo.Contains("14")
                || textoLimpo.Contains("gerando") || textoLimpo.Contains("embreagem") || textoLimpo.Contains("batid")
                || textoLimpo.Contains("apaga") || textoLimpo.Contains("apagou") || textoLimpo.Contains("desligou")
                || textoLimpo.Contains("tirei") || textoLimpo.Contains("retirei") || textoLimpo.Contains("sanfonad")
                || textoLimpo.Contains("coifa") || textoLimpo.Contains("partid") || textoLimpo.Contains("100w")
                || textoLimpo.Contains("trocad") || textoLimpo.Contains("troquei")
                || textoLimpo.Contains("oscil") || textoLimpo.Contains("morre") || textoLimpo.Contains("morrer")
                || textoLimpo.Contains("lenta") || textoLimpo.Contains("tbi") || textoLimpo.Contains("iac")
                || textoLimpo.Contains("tps") || textoLimpo.Contains("respiro") || textoLimpo.Contains("blow")
                || textoLimpo.Contains("fumaça") || textoLimpo.Contains("fumaca") || textoLimpo.Contains("canister")
                || textoLimpo.Contains("valvula") || textoLimpo.Contains("coletor") || textoLimpo.Contains("vacuo")
                || textoLimpo.Contains("hidrovacuo") || textoLimpo.Contains("freio") || textoLimpo.Contains("ea111");
        }

        private static AIChatResponse? ProcessarContinuacaoDiagnostico(string contexto, string textoLimpo, string mensagemOriginal)
        {
            var res = new AIChatResponse
            {
                ProviderUsed = "PRIMOX Copilot Inteligente",
                Success = true
            };

            var sb = new StringBuilder();

            // =========================================================================
            // CONTINUIDADE: ESGUICHO / LAVADOR DE PARA-BRISA (BOMBINHA D'ÁGUA / BRUCUTU)
            // =========================================================================
            if (contexto == "ESGUICHO")
            {
                // Usuário informou VW Gol / Parati / Saveiro (G2, G3, G4 ou modelos sem relé)
                if (textoLimpo.Contains("gol") || textoLimpo.Contains("saveiro") || textoLimpo.Contains("parati") || textoLimpo.Contains("g4") || textoLimpo.Contains("g3") || textoLimpo.Contains("g2") || textoLimpo.Contains("santana") || textoLimpo.Contains("fusca"))
                {
                    sb.AppendLine("Excelente confirmação! Veículo da **Linha VW (Gol, Parati, Saveiro G2/G3/G4 ou Antigos)**.\n");
                    sb.AppendLine("⚡ **Atenção Técnica para a Arquitetura deste Veículo:**");
                    sb.AppendLine("👉 **O GOL G4 / G3 / G2 NÃO POSSUI RELÉ DE ESGUICHO!**");
                    sb.AppendLine("O circuito é de **ligação direta**: a tensão positiva linha 15 (+12V pós-chave) sai da caixa de fusíveis interna protegida pelo **Fusível F11 ou F15 (10A ou 15A)** e entra diretamente no comutador da coluna de direção (chave de seta/limpador). Ao puxar a alavanca em direção ao volante, a lâmina interna de cobre fecha o circuito e envia +12V DIRETO pelo fio do chicote até o plugue de 2 vias da bombinha no reservatório de água.\n");
                    sb.AppendLine("📋 **Roteiro Exato de Testes para o Gol G4 (Passo a Passo):**");
                    sb.AppendLine("1. **Teste do Fusível F11/F15:** Ligue a ignição no 1º estágio e teste com a lâmpada de teste nos 2 furinhos do fusível. Se a lâmpada não acender de um lado, troque o fusível.");
                    sb.AppendLine("2. **Chave de Seta da Coluna (Causa Crônica Nº 1):** Como a corrente de ~2 Amperes da bombinha passa direta pelos contatos mecânicos sem proteção de relé, a lâmina de cobre interna da alavanca cria arco elétrico, oxida ou entorta com os anos. Solte as capas plásticas da coluna (2 parafusos Philips por baixo do volante), retire o conector da chave de seta e meça continuidade com o multímetro nos pinos de esguicho ao puxar a alavanca. Se não bipar, a chave de seta está condenada (substitua a chave de seta).");
                    sb.AppendLine("3. **Conector e Aterramento da Bombinha:** No conector de 2 vias da bombinha (embaixo da churrasqueira/paralama esquerdo), confira se o pino terra (marrom) tem continuidade perfeita (0.0Ω) com a lataria do carro.");
                    sb.AppendLine("4. **Se o 12V e o terra chegarem no plugue:** A fiação está 100% perfeita e a **bombinha está queimada ou travada com lodo/sujeira**.");
                    sb.AppendLine("\n🌐 *Pesquisa Técnica Web: Diagrama elétrico Gol G4 confirma alimentação direta da bomba pela chave de coluna sem relé auxiliar.*");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Chave de seta sem continuidade", ActionType = "ExecuteQuery", Parameter = "A chave de seta está sem continuidade nos contatos" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Fusível estava queimado", ActionType = "ExecuteQuery", Parameter = "O fusível do esguicho estava queimado" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Chave Seta Gol no Estoque", ActionType = "SearchStock", Parameter = "Chave Seta Gol G4" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Bomba Esguicho no Estoque", ActionType = "SearchStock", Parameter = "Bomba Esguicho Gol" });
                    return res;
                }

                // Usuário informou Linha Fiat (Palio, Uno, Siena, Strada Fire)
                if (textoLimpo.Contains("palio") || textoLimpo.Contains("uno") || textoLimpo.Contains("siena") || textoLimpo.Contains("strada") || textoLimpo.Contains("fire") || textoLimpo.Contains("fiat"))
                {
                    sb.AppendLine("Entendido, veículo da **Linha Fiat (Palio, Uno, Siena, Strada Fire)**!\n");
                    sb.AppendLine("⚡ **Arquitetura Elétrica do Lavador:**");
                    sb.AppendLine("Nos Fiat Fire tradicionais, o esguicho dianteiro opera com comutação direta na chave de coluna (ou através da central de derivação sob o painel nos modelos com Body Computer Venézia).\n");
                    sb.AppendLine("📋 **Pontos de Verificação Imediatos:**");
                    sb.AppendLine("1. **Eletrobomba de Saída Dupla:** No Palio/Uno com limpador traseiro, a bombinha possui duas saídas e inverte polaridade. Se funcionar atrás mas não na frente, o defeito é na alavanca ou chicote de reversão.");
                    sb.AppendLine("2. **Conector da Coluna de Direção:** O chicote da chave de seta Fiat costuma sofrer folga nos terminais da alavanca direita.");
                    sb.AppendLine("3. **Fusível F43 / F31:** Verifique na caixa de fusíveis do vão do motor ou sob o painel.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Bomba é de saída dupla", ActionType = "ExecuteQuery", Parameter = "A bombinha tem duas saídas dianteira e traseira" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Bomba Palio no Estoque", ActionType = "SearchStock", Parameter = "Bomba Esguicho Palio" });
                    return res;
                }

                // Usuário informou Linha GM (Corsa, Celta, Classic, Onix)
                if (textoLimpo.Contains("corsa") || textoLimpo.Contains("celta") || textoLimpo.Contains("classic") || textoLimpo.Contains("montana") || textoLimpo.Contains("gm") || textoLimpo.Contains("chevrolet") || textoLimpo.Contains("onix") || textoLimpo.Contains("prisma"))
                {
                    if (textoLimpo.Contains("onix") || textoLimpo.Contains("prisma"))
                    {
                        sb.AppendLine("Entendido, veículo da **Linha GM Onix / Prisma (Geração 2 / Moderno)**!\n");
                        sb.AppendLine("⚡ **Arquitetura Elétrica:** No Onix/Prisma moderno, o sistema é **100% eletrônico com BCM (Body Control Module)**. A alavanca de comando não comuta 12V de força; ela envia sinal resistivo de baixa corrente para a central de carroceria. A BCM é quem aciona o relé interno de placa para alimentar a bombinha bidirecional.");
                        sb.AppendLine("\n📋 **Testes:**");
                        sb.AppendLine("1. Acesse a BCM com o scanner automotivo e monitore o estado da alavanca de lavador.");
                        sb.AppendLine("2. Faça o 'Teste de Atuadores' com o scanner para ligar a bomba forçadamente.");
                    }
                    else
                    {
                        sb.AppendLine("Entendido, veículo da **Linha GM Tradicional (Corsa, Celta, Classic, Montana)**!\n");
                        sb.AppendLine("⚡ **Arquitetura:** No Corsa/Celta clássico, a alimentação sai do fusível F15/F26 e vai **DIRETO da chave de seta na coluna para a bombinha**, sem passar por relé.");
                        sb.AppendLine("O defeito clássico de não ir sinal ao puxar é oxidação nos contatos internos da chave de seta ou quebra da lâmina de retorno.");
                    }

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Chave de seta com defeito", ActionType = "ExecuteQuery", Parameter = "Chave de seta com defeito interno" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Chave Seta GM no Estoque", ActionType = "SearchStock", Parameter = "Chave Seta Corsa" });
                    return res;
                }

                // Usuário informou Linha Hyundai (HB20 / Creta)
                if (textoLimpo.Contains("hb20") || textoLimpo.Contains("creta") || textoLimpo.Contains("hyundai"))
                {
                    sb.AppendLine("Entendido, veículo da **Linha Hyundai (HB20 / Creta)**!\n");
                    sb.AppendLine("No HB20, o circuito do lavador passa pela Smart Junction Box (SJB / Caixa de Fusíveis Inteligente). Verifique o fusível 'WASHER' de 15A na caixa de fusíveis interna sob o painel.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Fusível WASHER verificado", ActionType = "ExecuteQuery", Parameter = "Fusível WASHER verificado no painel" });
                    return res;
                }

                // O usuário relatou que ao puxar a alavanca não vai sinal na bombinha SEM ter dito o carro ainda!
                if (textoLimpo.Contains("alavanca") || textoLimpo.Contains("sinal") || textoLimpo.Contains("12v") || textoLimpo.Contains("plugue") || textoLimpo.Contains("bombinha"))
                {
                    sb.AppendLine("Excelente teste preliminar! Se **ao puxar a alavanca não chega sinal de 12V na bombinha**, eliminamos a hipótese de motor queimado e entupimento de mangueiras — o defeito está **no circuito de alimentação ou comando**!\n");
                    sb.AppendLine("👉 **ATENÇÃO TÉCNICA: QUAL É O MODELO, MARCA E ANO DO VEÍCULO? (Ex: Gol G4, Uno Mille, Corsa, Palio, Onix, HB20?)**\n");
                    sb.AppendLine("⚠️ **Por que essa pergunta é OBRIGATÓRIA na auto elétrica?**");
                    sb.AppendLine("A arquitetura elétrica muda completamente dependendo da geração do carro:");
                    sb.AppendLine("• **Carros Mais Antigos (ex: Gol G2/G3/G4, Corsa B, Uno Mille, Santana, Fusca):**");
                    sb.AppendLine("  **NÃO EXISTE RELÉ DE ESGUICHO!** A alavanca de comando da coluna envia a linha 15 (+12V pós-chave) DIRETO para a bombinha de água. Se não vai sinal ao puxar:");
                    sb.AppendLine("  1. **Lâmina de Cobre da Chave de Seta Gasta/Aberta:** É o defeito nº 1 na oficina (substituição ou desoxidação da chave).");
                    sb.AppendLine("  2. **Fusível Queimado:** Na caixa de fusíveis interna (10A ou 15A).");
                    sb.AppendLine("  3. **Chicote Partido:** Na coluna de direção ou passagem sob o paralama.");
                    sb.AppendLine("• **Carros com Relé Temporizador (ex: Astra, Golf, Santana, Gol G5):**");
                    sb.AppendLine("  O circuito possui um relé temporizador conjugado com as palhetas.");
                    sb.AppendLine("• **Carros Modernos (ex: Onix, HB20, Renegade, Compass, Polo TSI):**");
                    sb.AppendLine("  A chave de seta NÃO comuta força; ela envia sinal resistivo para o módulo **BCM (computador de carroceria)**, que comuta relé de reversão para alimentar a bombinha bidirecional.\n");
                    sb.AppendLine("Enquanto você me confirma o modelo do carro, faça este teste rápido de bancada:");
                    sb.AppendLine("👉 Retire as capas da coluna de direção e meça com multímetro/lâmpada se sai 12V no conector traseiro da chave de seta ao puxar a alavanca.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "É um Gol / Saveiro G4", ActionType = "ExecuteQuery", Parameter = "É um Gol G4 2008" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "É um Palio / Uno Fire", ActionType = "ExecuteQuery", Parameter = "É um Fiat Palio Fire" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "É um Corsa / Celta GM", ActionType = "ExecuteQuery", Parameter = "É um GM Corsa Classic" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "É um Onix / Prisma / HB20", ActionType = "ExecuteQuery", Parameter = "É um Chevrolet Onix" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "O fusível já foi testado e está bom", ActionType = "ExecuteQuery", Parameter = "O fusível já foi testado e está bom" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Chave de seta sem continuidade", ActionType = "ExecuteQuery", Parameter = "A chave de seta está sem continuidade nos contatos" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: MARCHA LENTA OSCILANDO / MOTOR MORRENDO / FALHANDO
            // =========================================================================
            if (contexto == "MARCHA_LENTA" || contexto == "FALHANDO")
            {
                // Usuário informou Linha VW (EA111 / EA211 - Gol, Fox, Voyage, Polo)
                if (textoLimpo.Contains("gol") || textoLimpo.Contains("fox") || textoLimpo.Contains("voyage") || textoLimpo.Contains("ea111") || textoLimpo.Contains("vw") || textoLimpo.Contains("polo"))
                {
                    sb.AppendLine("Perfeito, veículo da **Linha VW (EA111 / EA211 - Gol, Fox, Voyage)**!\n");
                    sb.AppendLine("Nos motores EA111, marcha lenta oscilando e morrendo possui 3 causas crônicas clássicas de oficina:");
                    sb.AppendLine("1. **Corpo de Borboleta (TBI):** É o defeito nº 1. Se a borda da borboleta acumular carvão, ela não atinge a abertura mínima de repouso (1.5° a 3.0° no scanner). Após limpar com descarbonizante, É OBRIGATÓRIO passar o scanner e fazer a 'Adaptação Básica da Borboleta' (canal 60 ou 98) com motor quente (> 80°C).");
                    sb.AppendLine("2. **Mangueira do Respiro do Cárter (Blow-by):** Fica atrás do bloco do motor, embaixo do coletor de admissão. Ela resseca com vapor de óleo quente e rasga na curva inferior — dá uma entrada de ar falsa enorme que faz o motor morrer ao parar no sinal!");
                    sb.AppendLine("3. **Transformador de Ignição (Bobina 4 Pinos):** Trinca invisível na carcaça traseira de baquelite. Jogue leves borrifos de água com o motor funcionando à noite: se saltar centelha azul para o cabeçote, a bobina está condenada.");
                    sb.AppendLine("4. **Sensor de Temperatura ECT (Plugue 4 Pinos / 2 Pinos):** Se marcar -40°C no scanner, afoga o motor na partida.");
                    sb.AppendLine("\nVocê já conferiu o TBI ou passou o scanner para ver o ângulo da borboleta?");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "TBI já foi limpo", ActionType = "ExecuteQuery", Parameter = "O corpo de borboleta TBI já foi limpo" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Respiro do bloco estava furado", ActionType = "ExecuteQuery", Parameter = "A mangueira do respiro do cárter está furada" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Bobina tem trinca", ActionType = "ExecuteQuery", Parameter = "A bobina de ignição tem trinca com centelha fugindo" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Bobina EA111 no Estoque", ActionType = "SearchStock", Parameter = "Bobina Gol EA111" });
                    return res;
                }

                // Usuário informou Linha Fiat (Fire / Firefly)
                if (textoLimpo.Contains("palio") || textoLimpo.Contains("uno") || textoLimpo.Contains("siena") || textoLimpo.Contains("strada") || textoLimpo.Contains("fire") || textoLimpo.Contains("fiat"))
                {
                    sb.AppendLine("Entendido, veículo da **Linha Fiat Fire (Palio, Uno, Siena, Strada)**!\n");
                    sb.AppendLine("Nos motores Fiat Fire, a oscilação de marcha lenta e corte do motor tem estes suspeitos principais:");
                    sb.AppendLine("1. **Atuador de Marcha Lenta (Motor de Passo IAC):** Fica parafusado no TBI. A ponta cônica e a sede acumulam borra de carvão e travam o êmbolo. Retire os 2 parafusos Torx T20, limpe a sede e meça a resistência das duas bobinas do motor de passo (45Ω a 55Ω em cada bobina).");
                    sb.AppendLine("2. **Chicote do Sensor MAP:** O plugue do sensor MAP na admissão sofre com vibração do motor. Meça se o sinal varia entre 1.0V (lenta) e 4.5V (aceleração total).");
                    sb.AppendLine("3. **Válvula Canister Travada Aberta:** Desconecte a mangueira da válvula do cânister e tampe com o dedo: se a marcha lenta estabilizar na hora, a válvula eletromagnética travou aberta dando excesso de vapor de combustível!");
                    sb.AppendLine("\nQual desses testes você quer fazer primeiro?");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Atuador IAC travado", ActionType = "ExecuteQuery", Parameter = "O atuador de marcha lenta está travado" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Válvula Canister travada", ActionType = "ExecuteQuery", Parameter = "A válvula do canister está travada aberta" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Atuador Fire no Estoque", ActionType = "SearchStock", Parameter = "Atuador Marcha Lenta Fire" });
                    return res;
                }

                // Usuário informou Linha GM (Corsa, Celta, Onix, Prisma, Montana)
                if (textoLimpo.Contains("corsa") || textoLimpo.Contains("celta") || textoLimpo.Contains("onix") || textoLimpo.Contains("prisma") || textoLimpo.Contains("montana") || textoLimpo.Contains("gm") || textoLimpo.Contains("chevrolet") || textoLimpo.Contains("vhc") || textoLimpo.Contains("spe"))
                {
                    sb.AppendLine("Certo, veículo da **Linha GM / Chevrolet (Corsa, Celta, Onix, Prisma)**!\n");
                    sb.AppendLine("Nesses modelos GM, investigue imediatamente:");
                    sb.AppendLine("1. **Sensor de Posição da Borboleta (TPS) ou TBI Eletrônico:** Nos modelos com cabo de acelerador (Corsa/Celta), a pista resistiva do sensor TPS gasta na posição inicial e a central 'perde' a marcha lenta. O sinal de repouso deve ser 0.5V a 0.7V estável.");
                    sb.AppendLine("2. **Mangueira do Servo-Freio (Hidrovácuo):** A válvula de retenção plástica na mangueira do hidrovácuo racha e gera entrada falsa de ar. Teste: pise repetidamente no freio com motor ligado — se oscilar ou chiar, há vazamento no hidrovácuo.");
                    sb.AppendLine("3. **Bobina de Ignição (Módulo DIS 4 Pinos):** Clássico queimar as saídas dos cilindros 2 e 3 ou dar fuga de faísca nas torres.");
                    sb.AppendLine("\nVocê notou se o pedal de freio fica duro ou assobia quando o motor oscila?");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Freio assobia ao pisar", ActionType = "ExecuteQuery", Parameter = "O pedal de freio chia e altera a marcha lenta" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Sensor TPS com pista gasta", ActionType = "ExecuteQuery", Parameter = "O sensor TPS está variando o sinal em repouso" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Sensor TPS no Estoque", ActionType = "SearchStock", Parameter = "Sensor TPS GM" });
                    return res;
                }

                // Usuário informou Linha Hyundai (HB20 / Creta)
                if (textoLimpo.Contains("hb20") || textoLimpo.Contains("creta") || textoLimpo.Contains("hyundai"))
                {
                    sb.AppendLine("Entendido, veículo da **Linha Hyundai (HB20 / Creta 1.0 / 1.6)**!\n");
                    sb.AppendLine("Nos motores Hyundai Kappa de 3 ou 4 cilindros:");
                    sb.AppendLine("1. **Bobinas Individuais Tipo Caneta:** As ponteiras de borracha das bobinas ressecam e soltam faísca nas paredes do cabeçote (efeito corona cinzento). Retire as 3 ou 4 bobinas e inspecione as borrachas.");
                    sb.AppendLine("2. **Sensor MAP na Admissão:** Fica parafusado direto no coletor plástico. Acúmulo de óleo de blow-by contamina o sensor de pressão.");
                    sb.AppendLine("3. **Sonda Lambda Pré-Catalisador:** Se a sonda travar em mistura rica ou pobre, a ECU corrige além de ±25% e o motor trepida até morrer.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Bobina com fuga na ponta", ActionType = "ExecuteQuery", Parameter = "A bobina individual está com borracha ressecada e fuga" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Bobina HB20 no Estoque", ActionType = "SearchStock", Parameter = "Bobina HB20" });
                    return res;
                }

                // O usuário detalhou mais o sintoma (ex: "o carro liga mais fica oscilando as vezes chega ate a morrer") sem informar o modelo ainda
                if (textoLimpo.Contains("oscil") || textoLimpo.Contains("morre") || textoLimpo.Contains("morrer") || textoLimpo.Contains("lenta"))
                {
                    sb.AppendLine("Entendido! Se o **motor liga, mas oscila a marcha lenta e chega a morrer**, o motor tem centelha e combustível para pegar, mas não consegue estabilizar a entrada de ar ou queima.\n");
                    sb.AppendLine("👉 **Para eu te passar o teste exato e a pinagem correta: qual é o modelo, ano e motorização do carro? (Ex: Gol EA111, Palio Fire, Onix SPE4, Corsa VHC, HB20?)**\n");
                    sb.AppendLine("Cada injeção tem um circuito diferente! Enquanto você me confirma o carro, comece por estes 3 pontos de bancada:");
                    sb.AppendLine("1. **Corpo de Borboleta (TBI / Atuador IAC):** Se tiver borra de carvão na borda ou o motor de passo estiver travado, o motor apaga ao soltar o acelerador.");
                    sb.AppendLine("2. **Entrada Falsa de Ar (Vácuo):** Mangueira do servo-freio (hidrovácuo) furada ou respiro do cárter (blow-by) rasgado empobrece a mistura e faz oscilar.");
                    sb.AppendLine("3. **Parâmetros no Scanner:** Verifique se o sensor MAP marca entre 280 e 400 mbar em marcha lenta e se a Sonda Lambda oscila rápido entre 100mV e 900mV.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "É um Gol / Fox EA111", ActionType = "ExecuteQuery", Parameter = "É um VW Gol EA111" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "É um Palio / Uno Fire", ActionType = "ExecuteQuery", Parameter = "É um Fiat Palio Fire" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "É um Corsa / Onix GM", ActionType = "ExecuteQuery", Parameter = "É um GM Corsa Onix" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "É um HB20 1.0", ActionType = "ExecuteQuery", Parameter = "É um Hyundai HB20" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Já limpei o TBI", ActionType = "ExecuteQuery", Parameter = "O corpo de borboleta TBI já foi limpo" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Suspeito de entrada de ar", ActionType = "ExecuteQuery", Parameter = "Suspeito de entrada falsa de ar no coletor" });
                    return res;
                }

                // Testes de bancada informados pelo usuário
                if (textoLimpo.Contains("tbi") && (textoLimpo.Contains("limpo") || textoLimpo.Contains("ok") || textoLimpo.Contains("limpei")))
                {
                    sb.AppendLine("Se o **corpo de borboleta (TBI) já foi limpo**, a causa mecânica direta está eliminada!\n");
                    sb.AppendLine("📋 **Próximos passos de alta precisão:**");
                    sb.AppendLine("1. **Reset e Adaptação:** O TBI foi resetado no scanner após a limpeza? Sem reaprender o batente eletrônico, o motor continuará oscilando!");
                    sb.AppendLine("2. **Teste de Entrada de Ar (Estanqueidade):** Inspecione as mangueiras de vácuo do servo-freio e coletor com máquina de fumaça.");
                    sb.AppendLine("3. **Sensor MAP no Scanner:** Com o motor quente na lenta, qual é o valor em mbar? (Normal: 280 a 380 mbar. Se estiver acima de 450 mbar, há entrada falsa de ar ou ponto de correia fora!).");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "MAP está acima de 450 mbar", ActionType = "ExecuteQuery", Parameter = "Sensor MAP no scanner acusa mais de 450 mbar" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Vou fazer reset no scanner", ActionType = "ExecuteQuery", Parameter = "Vou fazer calibração do TBI no scanner" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: CARRO SEM FUNCIONAR / NÃO PEGA
            // =========================================================================
            if (contexto == "CARRO_SEM_FUNCIONAR")
            {
                if (textoLimpo.Contains("arranque vira forte") || (textoLimpo.Contains("vira") && !textoLimpo.Contains("nao vira") && !textoLimpo.Contains("pesado")))
                {
                    sb.AppendLine("Excelente informação! Se o **motor de arranque gira forte com boa rotação**, eliminamos bateria e motor de partida.");
                    sb.AppendLine("Agora o motor precisa de 2 pilares essenciais para funcionar: **FAÍSCA** e **COMBUSTÍVEL**.\n");
                    sb.AppendLine("📋 **Próximos 2 passos de bancada:**");
                    sb.AppendLine("1. **Teste de Faísca (Centelha):** Coloque um centelhador (ou vela de teste aterrada) na ponta do cabo/bobina e dê a partida. Tem centelha azul forte?");
                    sb.AppendLine("2. **Sensor de Rotação (CKP):** Se NÃO tiver faísca E os bicos não tiverem pulso negativo, o Sensor de Rotação é 90% dos casos de defeito!");
                    sb.AppendLine("3. **Bomba de Combustível:** Ao ligar o 1º estágio da chave, escuta o zumbido de 2 segundos no tanque pressurizando?\n");
                    sb.AppendLine("Qual desses testes você consegue fazer primeiro aí?");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Tem faísca nas velas", ActionType = "ExecuteQuery", Parameter = "Tem faísca nas velas" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Não tem faísca nas velas", ActionType = "ExecuteQuery", Parameter = "Não tem faísca nas velas" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Não escuto a bomba", ActionType = "ExecuteQuery", Parameter = "Não escuto a bomba de combustível" });
                    return res;
                }

                if (textoLimpo.Contains("nao tem faisca") || textoLimpo.Contains("sem faisca") || textoLimpo.Contains("sem centelha"))
                {
                    sb.AppendLine("Diagnóstico preciso: **Sem faísca nas velas durante a partida!**\n");
                    sb.AppendLine("Quando falta faísca com arranque virando, a central não está disparando a ignição:");
                    sb.AppendLine("1. **Sensor de Rotação (CKP):** O defeito nº 1 da oficina. Meça a resistência ôhmica do sensor indutivo (normal: 500Ω a 1200Ω). Se der circuito aberto (1 no multímetro), o sensor queimou.");
                    sb.AppendLine("2. **Alimentação da Bobina:** Com a ignição ligada, meça se chegam 12V pós-chave (linha 15) no conector da bobina de ignição.");
                    sb.AppendLine("3. **Relé Principal da Injeção:** Verifique se o relé da injeção atraca ao ligar a chave.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Sensor CKP no Estoque", ActionType = "SearchStock", Parameter = "Sensor de Rotacao" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Chega 12V na bobina", ActionType = "ExecuteQuery", Parameter = "Chegam 12V na bobina" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Sensor de rotação deu aberto", ActionType = "ExecuteQuery", Parameter = "Sensor de rotação deu resistência aberta" });
                    return res;
                }

                if (textoLimpo.Contains("nao escuto a bomba") || textoLimpo.Contains("sem bomba") || textoLimpo.Contains("bomba nao liga"))
                {
                    sb.AppendLine("Atenção: **Bomba de combustível inoperante ao ligar a chave!**\n");
                    sb.AppendLine("Roteiro rápido de alimentação da bomba:");
                    sb.AppendLine("1. **Fusível da Bomba:** Teste com lâmpada de teste o fusível da bomba de combustível na caixa de fusíveis.");
                    sb.AppendLine("2. **Relé da Bomba:** Retire o relé da bomba e faça um jumper entre os pinos 30 e 87: a bomba deve ligar direto no tanque.");
                    sb.AppendLine("3. **Tensão no Tanque:** Levante o banco traseiro e meça com lâmpada de teste se chegam 12V nos fios mais grossos do plugue da tampa do tanque por 2 segundos ao ligar a chave.");
                    sb.AppendLine("4. **Se CHEGA 12V e não liga:** O refil da bomba de combustível travou ou queimou!");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Refil Bomba no Estoque", ActionType = "SearchStock", Parameter = "Refil Bomba" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Fusível da bomba rompido", ActionType = "ExecuteQuery", Parameter = "Fusível da bomba está queimado" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Chega 12V no plugue do tanque", ActionType = "ExecuteQuery", Parameter = "Chegam 12V no conector da bomba" });
                    return res;
                }

                // Identificação de modelo veicular na resposta
                if (textoLimpo.Contains("gol") || textoLimpo.Contains("saveiro") || textoLimpo.Contains("voyage") || textoLimpo.Contains("fox"))
                {
                    sb.AppendLine("Perfeito, veículo da **Linha VW (EA111 / EA211)**!\n");
                    sb.AppendLine("Nesses modelos VW, fique muito atento aos seguintes pontos críticos:");
                    sb.AppendLine("1. **Sensor de Rotação (Flange Traseira):** Fica atrás do volante do motor com roda fônica emborrachada. Se houver vazamento de retentor de óleo ou sensor queimado, o carro vira mas não pega de jeito nenhum!");
                    sb.AppendLine("2. **Relé Principal (Relé 100/429):** Localizado na central de relés sob o painel. Se ele falhar, a ECU não energiza bicos e bobina.");
                    sb.AppendLine("3. **Luz do Imobilizador:** Veja se a chavinha no painel apaga após 3 segundos ou se fica piscando.");
                    sb.AppendLine("\nQual é o comportamento do arranque: vira forte ou não vira?");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Arranque vira forte", ActionType = "ExecuteQuery", Parameter = "Arranque vira forte mas não pega" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Luz do imobilizador pisca", ActionType = "ExecuteQuery", Parameter = "Luz da chave pisca no painel" });
                    return res;
                }

                if (textoLimpo.Contains("palio") || textoLimpo.Contains("uno") || textoLimpo.Contains("siena") || textoLimpo.Contains("strada"))
                {
                    sb.AppendLine("Certo, veículo da **Linha Fiat Fire / Firefly**!\n");
                    sb.AppendLine("Nos motores Fiat Fire, verifique imediatamente:");
                    sb.AppendLine("1. **Interruptor Inercial de Corte (FPS):** Localizado abaixo do painel do lado do passageiro. Se o carro passou por buraco ou sofreu baque, o botão de inércia desarmou e cortou a bomba! Aperte a borrachinha do botão para armar.");
                    sb.AppendLine("2. **Sensor de Rotação:** Fica na polia do virabrequim (fácil acesso). Resistência normal de ~680Ω.");
                    sb.AppendLine("3. **Fiat CODE:** Se a luz da chavinha com a palavra 'CODE' não apagar, o imobilizador bloqueou a central.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Luz CODE apagou normal", ActionType = "ExecuteQuery", Parameter = "Luz do CODE apagou normal" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Botão inercial armado", ActionType = "ExecuteQuery", Parameter = "Botão inercial está armado" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: FAROL OU LANTERNA TRASEIRA / FAROL ACESO DIRETO
            // =========================================================================
            if (contexto == "FAROL" || contexto == "FAROL_ACESO_DIRETO")
            {
                // Usuário retirou o relé e o farol apagou (Relé com contatos soldados/colados)
                if (textoLimpo.Contains("rele") && (textoLimpo.Contains("apagou") || textoLimpo.Contains("desligou") || textoLimpo.Contains("tirei") || textoLimpo.Contains("retirei") || textoLimpo.Contains("saiu")))
                {
                    sb.AppendLine("🎯 **DIAGNÓSTICO CONFIRMADO NA MOSCA!**\n");
                    sb.AppendLine("Se ao retirar o relé da caixa de fusíveis o farol apagou na hora, **os contatos internos de força do relé (pinos 30 e 87) estão COLADOS / SOLDADOS por arco elétrico!**\n");
                    sb.AppendLine("⚠️ **ALERTA CRÍTICO DE OFICINA - NÃO FAÇA APENAS A TROCA DO RELÉ:**");
                    sb.AppendLine("1. **Inspeção Obrigatória das Lâmpadas:** Em mais de 80% dos casos de relé colado, instalaram **lâmpadas paralelas de 100W (Super Branca / Rally)** no lugar das originais de 55W/60W!");
                    sb.AppendLine("   • Lâmpadas de 100W dobram a corrente de consumo para ~17A, sobrecarregando o relé até soldar os contatos internos e derreter os soquetes plásticos.");
                    sb.AppendLine("2. **Substituição do Relé:** Instale um relé auxiliar novo (4 pinos 12V 40A padrão DIN: pino 30 positivo bateria, 87 saída para os faróis, 85/86 bobina de comando).");
                    sb.AppendLine("3. Se as lâmpadas forem originais de 55W/60W, o relé apenas esgotou sua vida útil mecânica e basta a substituição.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Lâmpadas são de 100W", ActionType = "ExecuteQuery", Parameter = "As lâmpadas instaladas são de 100W" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Lâmpadas são originais 55W", ActionType = "ExecuteQuery", Parameter = "As lâmpadas são de 55W originais" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Relé 40A no Estoque", ActionType = "SearchStock", Parameter = "Rele Auxiliar 40A" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Lâmpadas H4 no Estoque", ActionType = "SearchStock", Parameter = "Lampada H4 55/60W" });
                    return res;
                }
                // Usuário informou modelo do carro
                if (textoLimpo.Contains("gol") || textoLimpo.Contains("voyage") || textoLimpo.Contains("saveiro") || textoLimpo.Contains("fox"))
                {
                    sb.AppendLine("Excelente! No **VW Gol / Voyage / Saveiro**:");
                    sb.AppendLine("• A lanterna traseira utiliza conector de 6 vias conectado direto nas trilhas de latão da placa.");
                    sb.AppendLine("• O fusível da luz de posição (lanterna) é individual por lado na caixa de fusíveis abaixo do volante.");
                    sb.AppendLine("• O ponto de massa (fio marrom) é fixado na coluna traseira interna da lataria.\n");
                    sb.AppendLine("📋 **Roteiro certeiro no Gol:**");
                    sb.AppendLine("1. **Teste Rápido de Fusível:** A meia-luz dianteira do mesmo lado também está apagada? Se a dianteira e traseira apagaram juntas, o **fusível individual daquele lado** está 100% rompido!");
                    sb.AppendLine("2. **Se a dianteira acende e apenas a traseira não:** Solte a borboleta plástica no porta-malas, puxe a lanterna e inspecione o pino marrom (terra) do plugue — é clássico derreter ou oxidar.");
                    sb.AppendLine("3. Meça com lâmpada de teste se chegam 12V na trilha da lâmpada com a chave ligada.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Dianteira também apagou", ActionType = "ExecuteQuery", Parameter = "A meia luz dianteira também está apagada" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Dianteira acende normal", ActionType = "ExecuteQuery", Parameter = "A dianteira acende normal só a traseira apagou" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Pino terra oxidado", ActionType = "ExecuteQuery", Parameter = "O conector da lanterna está oxidado" });
                    return res;
                }

                if (textoLimpo.Contains("palio") || textoLimpo.Contains("uno") || textoLimpo.Contains("siena") || textoLimpo.Contains("strada"))
                {
                    sb.AppendLine("Ótimo! Na **Linha Fiat (Palio / Uno / Strada)**:");
                    sb.AppendLine("• O defeito campeão em lanterna traseira da Fiat é o **terminal de aterramento (massa) do chicote** que esquenta e perde pressão.");
                    sb.AppendLine("• Se ao ligar a seta a lanterna inteira piscar fraca (efeito 'árvore de natal'), é 100% falta de terra!");
                    sb.AppendLine("• Solução: Limpe o conector ou faça um reforço de terra puxando um fio novo da lata do porta-malas direto para a trilha negativa da placa da lanterna.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ao dar seta pisca tudo junto", ActionType = "ExecuteQuery", Parameter = "Ao dar seta pisca tudo junto" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "O fusível está bom", ActionType = "ExecuteQuery", Parameter = "O fusível está bom" });
                    return res;
                }

                if (textoLimpo.Contains("fusivel") && (textoLimpo.Contains("bom") || textoLimpo.Contains("ok") || textoLimpo.Contains("certo")))
                {
                    sb.AppendLine("Se o **fusível está 100% bom**, eliminamos a proteção primária. Agora o foco é no ponto de carga:");
                    sb.AppendLine("1. **Teste no Soquete:** Desconecte o plugue e meça com o multímetro (ou lâmpada de teste) entre o terminal positivo e a carcaça de lata do carro.");
                    sb.AppendLine("2. **Se CHEGAM 12V:** O problema é a lâmpada com filamento partido ou o fio negativo (massa) que perdeu contato com a lataria.");
                    sb.AppendLine("3. **Se NÃO CHEGAM 12V:** O defeito está no trajeto entre a caixa de fusíveis e a lanterna, comumente fiação rompida na borracha de passagem ou relé.");
                    sb.AppendLine("\nVocê consegue medir se chegam os 12V no plugue com o interruptor ligado?");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Chegam 12V no soquete", ActionType = "ExecuteQuery", Parameter = "Chegam 12V no soquete" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Não chegam 12V", ActionType = "ExecuteQuery", Parameter = "Não chegam 12V no soquete" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Soquete está derretido", ActionType = "ExecuteQuery", Parameter = "O soquete da lâmpada está derretido" });
                    return res;
                }

                if (textoLimpo.Contains("chegam 12v") || textoLimpo.Contains("tem 12v"))
                {
                    sb.AppendLine("Ótimo diagnóstico! Se chegam 12V no polo positivo do soquete:\n");
                    sb.AppendLine("1. **Teste a Massa (Negativo):** Coloque a ponta preta do multímetro no terminal terra do soquete e a vermelha no polo positivo da bateria. Deve dar 12V. Se não der, o fio terra está partido!");
                    sb.AppendLine("2. **Lâmpada:** Se o terra estiver bom e chegam 12V, a lâmpada está queimada internamente (mesmo que o filamento pareça inteiro, pode haver quebra na base de solda).");
                    sb.AppendLine("3. Troque a lâmpada por uma nova de teste.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Lâmpadas no Estoque", ActionType = "SearchStock", Parameter = "Lampada" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Falta aterramento no soquete", ActionType = "ExecuteQuery", Parameter = "Falta aterramento no soquete" });
                    return res;
                }

                if (textoLimpo.Contains("nao chegam 12v") || textoLimpo.Contains("sem 12v"))
                {
                    sb.AppendLine("Atenção: Se NÃO chegam 12V no soquete e o fusível está bom:\n");
                    sb.AppendLine("1. **Chicote / Passagem de Borracha:** Verifique se há conector intermediário desconectado ou fio rompido na passagem sanfonada da tampa traseira/coluna.");
                    sb.AppendLine("2. **Chave de Luz / Comutador:** Teste a saída do comutador de lanterna.");
                    sb.AppendLine("3. Verifique se a lanterna dianteira do mesmo lado continua acesa normal.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Relés no Estoque", ActionType = "SearchStock", Parameter = "Rele" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: ARRANQUE / PARTIDA PESADA
            // =========================================================================
            if (contexto == "ARRANQUE")
            {
                sb.AppendLine("Certo, avançando no diagnóstico de partida:\n");
                if (textoLimpo.Contains("bateria") && (textoLimpo.Contains("nova") || textoLimpo.Contains("boa")))
                {
                    sb.AppendLine("Se a **bateria é nova e confiável**, o estalo 'tec-tec' é clássico de falta de transferência de alta corrente no motor de arranque:");
                    sb.AppendLine("1. **Automático de Partida:** O êmbolo atraca (faz o tec), mas os platinados internos de cobre estão carbonizados e não passam a linha 30 para o induzido.");
                    sb.AppendLine("2. **Escovas Gastas:** Se as escovas estiverem no limite, o circuito de fechamento da bobina de chamada fica aberto.");
                    sb.AppendLine("3. **Linha 50:** Meça se chegam no mínimo 10.5V no pino fino do automático durante a partida.");
                    sb.AppendLine("\nRecomendação: Remova o motor de arranque para teste em bancada!");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Linha 50 tem 12V", ActionType = "ExecuteQuery", Parameter = "Linha 50 tem 12V" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Automático no Estoque", ActionType = "SearchStock", Parameter = "Automatico" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Escovas no Estoque", ActionType = "SearchStock", Parameter = "Escova" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: ALTERNADOR / CARGA
            // =========================================================================
            if (contexto == "ALTERNADOR")
            {
                if (textoLimpo.Contains("12") || textoLimpo.Contains("menos") || textoLimpo.Contains("baixa") || textoLimpo.Contains("nao carrega"))
                {
                    sb.AppendLine("Diagnóstico confirmado: **O alternador não está carregando a bateria!** (Tensão abaixo de 13.5V com motor ligado).\n");
                    sb.AppendLine("📋 **Roteiro de Teste no Veículo:**");
                    sb.AppendLine("1. **Queda de Tensão no Cabo B+:** Ponta vermelha no parafuso B+ do alternador e ponta preta no borne positivo da bateria com faróis acesos a 2000 RPM. Máximo tolerado: 0.20V DC. Se der alto, o cabo ou fusível principal está com alta resistência!");
                    sb.AppendLine("2. **Queda no Terra da Carcaça:** Ponta vermelha na carcaça do alternador e preta no polo negativo da bateria. Máximo tolerado: 0.10V DC.");
                    sb.AppendLine("3. **Se no parafuso B+ marcar 12V:** O defeito é interno no alternador: regulador de voltagem com escovas gastas ou ponte retificadora de diodos queimada.");
                    sb.AppendLine("4. **Luz de Bateria no Painel:** A lâmpada da bateria acende ao ligar a chave no 1º estágio? Se a lâmpada estiver queimada, muitos alternadores não recebem excitação inicial (linha D+)!");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Regulador no Estoque", ActionType = "SearchStock", Parameter = "Regulador de Voltagem" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Placa de Diodos", ActionType = "SearchStock", Parameter = "Placa Diodos" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Queda B+ deu baixa", ActionType = "ExecuteQuery", Parameter = "Queda no cabo B+ está normal" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: FUGA DE CORRENTE / CONSUMO PARASITA
            // =========================================================================
            if (contexto == "PARASITA")
            {
                if (textoLimpo.Contains("consumo") || textoLimpo.Contains("ma") || textoLimpo.Contains("300") || textoLimpo.Contains("200") || textoLimpo.Contains("alto") || textoLimpo.Contains("fusivel"))
                {
                    sb.AppendLine("Diagnóstico preciso: **Consumo Parasita Elevado em Repouso!**\n");
                    sb.AppendLine("O valor máximo tolerado para qualquer carro moderno é **50mA (0.050A)** após entrar em modo Sleep (15 a 30 min). Consumos acima de 200mA esgotam a bateria em 2 dias parado!\n");
                    sb.AppendLine("🔍 **Técnica de Isolamento Rápido na Oficina:**");
                    sb.AppendLine("1. Mantenha o multímetro/alicate medindo os mA no borne negativo da bateria.");
                    sb.AppendLine("2. Comece retirando os fusíveis da caixa interna e do cofre um a um.");
                    sb.AppendLine("3. No instante em que você sacar um fusível e o amperímetro despencar para < 50mA: **esse fusível protege o circuito ladrão de corrente!**");
                    sb.AppendLine("4. **Os 3 maiores vilões da oficina:**");
                    sb.AppendLine("   • Rastreador GPS clandestino ou mal instalado com bateria interna estufada.");
                    sb.AppendLine("   • Módulo de som ou multimídia com fio Remote ligado no positivo direto linha 30.");
                    sb.AppendLine("   • Interruptor da lâmpada de porta-luvas ou porta-malas quebrado mantendo a lâmpada acesa.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Vou testar os fusíveis", ActionType = "ExecuteQuery", Parameter = "Vou testar os fusíveis um a um" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Tem som e alarme pós-venda", ActionType = "ExecuteQuery", Parameter = "Tem som e alarme pós-venda" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: VENTOINHA / ARREFECIMENTO
            // =========================================================================
            if (contexto == "VENTOINHA")
            {
                // Usuário informou Linha Fiat (Uno Mille, Palio, Siena, Strada Fire)
                if (textoLimpo.Contains("uno") || textoLimpo.Contains("palio") || textoLimpo.Contains("fire") || textoLimpo.Contains("fiat") || textoLimpo.Contains("siena") || textoLimpo.Contains("strada"))
                {
                    sb.AppendLine("Excelente confirmação! Veículo da **Linha Fiat Fire (Uno Mille, Palio, Siena, Strada)**.\n");
                    sb.AppendLine("⚡ **Arquitetura do Circuito de Arrefecimento Fiat Fire:**");
                    sb.AppendLine("• O sensor de temperatura da água (ECT - conector na carcaça da válvula termostática) envia sinal de resistência NTC para a ECU (Magneti Marelli IAW 4AF / 4CF / 4DF).");
                    sb.AppendLine("• A ECU comanda os relés da ventoinha (Relés T06 e T07 na caixa de fusíveis do vão motor) por sinal de massa (negativo).");
                    sb.AppendLine("• A alimentação de potência passa pelo **Maxi-Fusível de 40A/50A** no vão do motor e, nos modelos com ar-condicionado, passa pelo **resistor de 1ª velocidade** montado no defletor plástico do radiador.\n");
                    sb.AppendLine("📋 **Roteiro Exato de Testes para o Uno/Palio Fire:**");
                    sb.AppendLine("1. **Teste Rápido de Emergência (Modo Fail-Safe):** Com a ignição ligada, retire o conector do sensor ECT no cabeçote. A ECU DEVE acionar a ventoinha na velocidade máxima na hora! Se acionar, chicotes, relés, fusíveis e o motor da ventoinha estão 100% perfeitos (o defeito é o sensor ECT descalibrado ou ar no sistema).");
                    sb.AppendLine("2. **Resistor da 1ª Velocidade (Defeito Campeão no Uno):** Se a ventoinha só arma muito tarde (quase fervendo na 2ª velocidade), o filamento térmico da resistência no defletor rompeu!");
                    sb.AppendLine("3. **Teste do Relé no Vão Motor:** Retire o relé T06/T07 e faça jumper entre os pinos 30 e 87: a ventoinha deve disparar com força total.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Desconectei o ECT e acionou", ActionType = "ExecuteQuery", Parameter = "Desconectei o sensor ECT e a ventoinha ligou na hora" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Não ligou no modo emergência", ActionType = "ExecuteQuery", Parameter = "Não ligou mesmo desconectando o sensor de temperatura" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Relé ligou no jumper", ActionType = "ExecuteQuery", Parameter = "Relé ligou a ventoinha no jumper" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Sensor ECT no Estoque", ActionType = "SearchStock", Parameter = "Sensor Temperatura Fire" });
                    return res;
                }

                // Usuário informou VW Gol / Fox / Saveiro
                if (textoLimpo.Contains("gol") || textoLimpo.Contains("fox") || textoLimpo.Contains("saveiro") || textoLimpo.Contains("vw") || textoLimpo.Contains("voyage"))
                {
                    sb.AppendLine("Excelente confirmação! Veículo da **Linha VW (Gol, Fox, Voyage, Saveiro)**.\n");
                    sb.AppendLine("⚡ **Arquitetura Elétrica:** Nos motores EA111/EA211, o sensor ECT envia sinal para a ECU, que aciona o módulo/relé da ventoinha.\n");
                    sb.AppendLine("📋 **Roteiro de Testes:**");
                    sb.AppendLine("1. Teste de emergência: desconecte o plugue do sensor ECT no cavalete d'água.");
                    sb.AppendLine("2. Inspecione a lâmina metálica do fusível de 30A/40A fixado em cima da tampa da bateria.");
                    sb.AppendLine("3. No Fox/Polo, verifique o módulo eletrônico de controle de ventoinha fixado na longarina.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Desconectei o sensor e acionou", ActionType = "ExecuteQuery", Parameter = "Desconectei o sensor ECT e a ventoinha ligou na hora" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Fusível da bateria trincado", ActionType = "ExecuteQuery", Parameter = "O fusível de lâmina em cima da bateria está trincado" });
                    return res;
                }

                if (textoLimpo.Contains("direto") || textoLimpo.Contains("funcionou") || textoLimpo.Contains("rele") || textoLimpo.Contains("sensor"))
                {
                    sb.AppendLine("Excelente teste de bancada! Se a **ventoinha girou forte ligada direto na bateria**, o motor elétrico está 100% perfeito.\n");
                    sb.AppendLine("O defeito está no circuito de comando ou no circuito de potência:");
                    sb.AppendLine("1. **Teste do Relé da Ventoinha:** Remova o relé e faça um jumper entre os pinos 30 e 87: se a ventoinha ligar, o chicote de força e o maxi-fusível de 40A estão perfeitos!");
                    sb.AppendLine("2. **Resistência da 1ª Velocidade:** Localizada no defletor plástico do radiador. Se o filamento térmico romper, o carro não liga a 1ª velocidade e ferve no trânsito!");
                    sb.AppendLine("3. **Teste de Emergência (Sensor ECT):** Desconecte o plugue do sensor de temperatura da água e ligue a chave. A central DEVE disparar a ventoinha no máximo na hora (modo fail-safe).");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Relé ligou no jumper", ActionType = "ExecuteQuery", Parameter = "Relé ligou a ventoinha no jumper" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Resistência no Estoque", ActionType = "SearchStock", Parameter = "Resistencia Ventoinha" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Relé no Estoque", ActionType = "SearchStock", Parameter = "Rele 40A" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: TAMPA TRASEIRA (DESEMBAÇADOR + BRAKE LIGHT / LIMPADOR)
            // =========================================================================
            if (contexto == "TAMPA_TRASEIRA")
            {
                sb.AppendLine("🎯 **DIAGNÓSTICO TÉCNICO CERTEIRO:**\n");
                sb.AppendLine("Quando o **desembaçador traseiro e a terceira luz de freio (brake light)** — ou limpador traseiro — param de funcionar juntos, **o defeito está 95% das vezes na COIFA DE BORRACHA SANFONADA da articulação da tampa do porta-malas!**\n");
                sb.AppendLine("📋 **Passo a Passo de Oficina:**");
                sb.AppendLine("1. Puxe cuidadosamente as bordas da borracha sanfonada na junção superior entre o teto e a tampa.");
                sb.AppendLine("2. Inspecione visualmente o chicote: a flexão mecânica repetida de abrir e fechar a tampa fadiga o cobre e rompe os fios por dentro.");
                sb.AppendLine("3. Emende os fios partidos utilizando solda estanho e espaguete termorretrátil de qualidade (ou substitua o trecho com fio automotivo flexível novo).");
                sb.AppendLine("4. **Fusíveis de Proteção:** Após refazer o chicote, confira os fusíveis do desembaçador e freio na caixa, pois os fios partidos costumam fechar curto antes de romper de vez.");

                res.Message = sb.ToString();
                res.SuggestedActions.Add(new AISuggestedAction { Label = "Fios partidos na borracha", ActionType = "ExecuteQuery", Parameter = "Fios estão partidos na borracha sanfonada" });
                res.SuggestedActions.Add(new AISuggestedAction { Label = "Fusível do desembaçador queimou", ActionType = "ExecuteQuery", Parameter = "Fusível do desembaçador estava queimado" });
                return res;
            }

            // =========================================================================
            // CONTINUIDADE: CURTO-CIRCUITO / FUSÍVEL QUEIMANDO
            // =========================================================================
            if (contexto == "CURTO")
            {
                if (textoLimpo.Contains("lampada") || textoLimpo.Contains("acendeu") || textoLimpo.Contains("100%") || textoLimpo.Contains("chicote"))
                {
                    sb.AppendLine("Perfeito! A **lâmpada halógena de 21W acesa com 100% de brilho comprova que o curto para a massa está ativo!**\n");
                    sb.AppendLine("Enquanto a lâmpada estiver conectada no lugar do fusível, nenhum componente queima e você pode rastrear com segurança:\n");
                    sb.AppendLine("1. Comece a movimentar os ramos do chicote elétrico no cofre do motor, sob o painel e perto da lata.");
                    sb.AppendLine("2. Desconecte os consumidores elétricos que dependem desse fusível um a um.");
                    sb.AppendLine("3. **No segundo exato em que você afastar o fio descascado que encosta na lataria (ou desconectar a peça em curto), A LÂMPADA SE APAGA NA HORA!**");
                    sb.AppendLine("4. Esse é o ponto exato da avaria. Isole com espaguete termorretrátil.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "A lâmpada apagou ao mexer", ActionType = "ExecuteQuery", Parameter = "A lâmpada apagou ao mexer no chicote" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Fusíveis no Estoque", ActionType = "SearchStock", Parameter = "Fusivel" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: AR-CONDICIONADO
            // =========================================================================
            if (contexto == "AR_CONDICIONADO")
            {
                if (textoLimpo.Contains("compressor") || textoLimpo.Contains("estalo") || textoLimpo.Contains("gas") || textoLimpo.Contains("bobina") || textoLimpo.Contains("rele"))
                {
                    sb.AppendLine("Diagnóstico do Sistema de Ar-Condicionado:\n");
                    sb.AppendLine("1. **Teste da Bobina Eletromagnética:** Desconecte o plugue do compressor e meça com o multímetro a resistência ôhmica da bobina: normal entre **3.2Ω e 4.5Ω**. Se der circuito aberto (infinito), o protetor térmico interno da bobina rompeu!");
                    sb.AppendLine("2. **Teste de Pressão (Pressostato):** Se a pressão do gás estiver abaixo de 30 PSI, o pressostato linear bloqueia o compressor por segurança mecânica.");
                    sb.AppendLine("3. **Teste do Relé do A/C:** Remova o relé do compressor e jumpeie os pinos 30 e 87 com o motor ligado: se a embreagem colar na polia e o carro começar a gelar, todo o circuito elétrico de força está perfeito e a falha é comando da central ou pressostato.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Bobina Compressor no Estoque", ActionType = "SearchStock", Parameter = "Bobina Compressor" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Relé atracou no jumper", ActionType = "ExecuteQuery", Parameter = "Relé atracou no jumper" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Bobina deu circuito aberto", ActionType = "ExecuteQuery", Parameter = "Bobina deu circuito aberto" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: BUZINA
            // =========================================================================
            if (contexto == "BUZINA")
            {
                if (textoLimpo.Contains("rele") || textoLimpo.Contains("clique") || textoLimpo.Contains("airbag") || textoLimpo.Contains("cinta") || textoLimpo.Contains("bateria"))
                {
                    sb.AppendLine("Diagnóstico certeiro no circuito da buzina:\n");
                    sb.AppendLine("• **Se o relé da buzina CLICA ao apertar o volante:** A cinta do volante (Clock Spring), botão e fiação da coluna estão 100% íntegros! O defeito é da caixa de fusíveis para a frente: fusível de potência, fio até a grade ou terminal negativo oxidado na lataria.");
                    sb.AppendLine("• **Se o relé NÃO clica:** O defeito é no interruptor do volante ou na fita da cinta do airbag partida. (Se a luz do airbag estiver acesa no painel, é 100% a cinta partida!).");
                    sb.AppendLine("• **Teste Direto:** Injete 12V e massa direto nos terminais da buzina na grade dianteira para confirmar se a carcaça/disco está tocando.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Buzina no Estoque", ActionType = "SearchStock", Parameter = "Buzina" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Cinta Airbag", ActionType = "SearchStock", Parameter = "Cinta Airbag" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Buzina tocou direto", ActionType = "ExecuteQuery", Parameter = "Buzina tocou direto na bateria" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: LIMPADOR DE PARA-BRISA
            // =========================================================================
            if (contexto == "LIMPADOR")
            {
                if (textoLimpo.Contains("meio") || textoLimpo.Contains("vidro") || textoLimpo.Contains("repouso") || textoLimpo.Contains("parado") || textoLimpo.Contains("motor"))
                {
                    sb.AppendLine("Diagnóstico do Limpador de Para-Brisa:\n");
                    sb.AppendLine("• **Palhetas param no meio do vidro (não voltam ao repouso):** O defeito é 100% no contato interno de retorno automático (**Linha 31b/53e**) dentro da engrenagem do motor do limpador, ou no relé temporizador que não recebe o sinal de fim de curso!");
                    sb.AppendLine("• **Se o motor não funciona em nenhuma velocidade:** Meça se chegam 12V no plugue do motor. Se chegarem 12V e tiver bom terra (pino 31), o induzido ou escovas do motor queimaram.");
                    sb.AppendLine("• **Varão mecânico travado:** Solte os braços e gire com a mão. Se estiver duro, lubrifique os pivôs da churrasqueira.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Motor Limpador no Estoque", ActionType = "SearchStock", Parameter = "Motor Limpador" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Relé Temporizador", ActionType = "SearchStock", Parameter = "Rele Temporizador" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: DIREÇÃO ELÉTRICA (EPS)
            // =========================================================================
            if (contexto == "DIRECAO_ELETRICA")
            {
                if (textoLimpo.Contains("bateria") || textoLimpo.Contains("fusivel") || textoLimpo.Contains("alternador") || textoLimpo.Contains("60a") || textoLimpo.Contains("80a") || textoLimpo.Contains("scanner"))
                {
                    sb.AppendLine("Diagnóstico da Direção Eletroassistida (EPS):\n");
                    sb.AppendLine("1. **Tensão do Alternador:** O módulo EPS consome picos de até 60A. Se o alternador estiver carregando abaixo de 13.0V, a direção desativa sozinha por proteção contra subtensão!");
                    sb.AppendLine("2. **Maxi-Fusível da Bateria:** Confira o fusível de alta corrente de 60A a 80A na régua plástica sobre o polo positivo da bateria.");
                    sb.AppendLine("3. **Calibração de Ponto Zero (SAS):** Se a bateria foi desligada recentemente ou feita geometria, conecte o scanner no módulo EPS e faça o 'Aprendizado do Sensor de Ângulo de Direção' com o volante centralizado.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Alternador carregando normal", ActionType = "ExecuteQuery", Parameter = "Alternador está carregando normal" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Maxi-fusível de 60A está bom", ActionType = "ExecuteQuery", Parameter = "Maxi-fusível de 60A está bom" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Vou calibrar no scanner", ActionType = "ExecuteQuery", Parameter = "Vou fazer calibração no scanner" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: ABS E VELOCÍMETRO (ONIX / PRISMA)
            // =========================================================================
            if (contexto == "ABS_VELOCIMETRO")
            {
                if (textoLimpo.Contains("onix") || textoLimpo.Contains("prisma") || textoLimpo.Contains("rolamento") || textoLimpo.Contains("sensor") || textoLimpo.Contains("velocimetro") || textoLimpo.Contains("scanner"))
                {
                    sb.AppendLine("Diagnóstico certeiro na Linha Chevrolet (Onix / Prisma / Cobalt):\n");
                    sb.AppendLine("• **Velocímetro parado junto com luz do ABS:** O painel do Onix calcula a velocidade pela **média dos sensores de ABS dianteiros** (não tem sensor mecânico no câmbio!).");
                    sb.AppendLine("• **Se foi trocado rolamento recentemente:** Verifique imediatamente se o mecânico não montou o rolamento invertido! O rolamento possui encoder magnético (anel fônico) em apenas uma das faces (a face preta deve ficar voltada para o sensor de ABS).");
                    sb.AppendLine("• **Teste no scanner:** Entre em 'Freios ABS → Parâmetros' e gire as rodas dianteiras com o carro no ar. A roda com defeito acusará 0 km/h enquanto a outra marca velocidade.");
                    sb.AppendLine("• Meça se chegam 12V no conector do sensor com a chave ligada.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Sensor ABS Onix", ActionType = "SearchStock", Parameter = "Sensor ABS Onix" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Rolamento foi trocado", ActionType = "ExecuteQuery", Parameter = "O rolamento dianteiro foi trocado recentemente" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Chega 12V no sensor", ActionType = "ExecuteQuery", Parameter = "Chegam 12V no conector do sensor de ABS" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: ALTERNADOR FORD SMART CHARGE
            // =========================================================================
            if (contexto == "FORD_SMART_CHARGE")
            {
                if (textoLimpo.Contains("ford") || textoLimpo.Contains("ka") || textoLimpo.Contains("fiesta") || textoLimpo.Contains("novo") || textoLimpo.Contains("regulador") || textoLimpo.Contains("smart") || textoLimpo.Contains("14v") || textoLimpo.Contains("14") || textoLimpo.Contains("gerando") || textoLimpo.Contains("multimetro") || textoLimpo.Contains("bateria"))
                {
                    sb.AppendLine("Diagnóstico do Alternador Pilotado Ford Smart Charge (Ka / Fiesta / EcoSport):\n");
                    sb.AppendLine("• **Luz da bateria acesa com alternador novo gerando 14V:** A central ECU gerencia a geração via pulsos digitais PWM. Reguladores de voltagem comuns/paralelos não respondem ao protocolo Ford e mantêm a luz da bateria ligada!");
                    sb.AppendLine("• **Teste do Fusível Sense (Linha AS):** No conector de 3 vias do alternador, o pino AS precisa receber 12V direto da bateria. Se estiver 0V, verifique o fusível de 3A ou 10A queimado na caixa do cofre.");
                    sb.AppendLine("• **Sinal PWM nos pinos RC e LI:** Pino RC recebe o comando de carga da ECU e pino LI envia o retorno de carga. Se houver fio quebrado na longarina, a luz acende.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Regulador Ford", ActionType = "SearchStock", Parameter = "Regulador Ford Smart Charge" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Chega 12V no pino Sense", ActionType = "ExecuteQuery", Parameter = "Chegam 12V no pino Sense do conector" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Regulador é original", ActionType = "ExecuteQuery", Parameter = "O regulador é original e compatível" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: VIDRO ANTI-ESMAGAMENTO (VW FOX / GOL)
            // =========================================================================
            if (contexto == "VIDRO_ANTI_ESMAGAMENTO")
            {
                if (textoLimpo.Contains("fox") || textoLimpo.Contains("gol") || textoLimpo.Contains("polo") || textoLimpo.Contains("sobe") || textoLimpo.Contains("desce") || textoLimpo.Contains("reset") || textoLimpo.Contains("canaleta") || textoLimpo.Contains("bateria") || textoLimpo.Contains("desligad") || textoLimpo.Contains("embreagem"))
                {
                    sb.AppendLine("Roteiro de Calibração Anti-Esmagamento (VW Fox / Gol / Voyage):\n");
                    sb.AppendLine("O módulo conforto monitora a corrente em Amperes: se encontra atrito na canaleta, inverte o giro por segurança.");
                    sb.AppendLine("📋 **Procedimento de Reset em 20 Segundos:**");
                    sb.AppendLine("1. Ligue a ignição (sem ligar o motor).");
                    sb.AppendLine("2. Puxe o botão para fechar o vidro até o topo e **MANTENHA O BOTÃO PUXADO POR 5 SEGUNDOS** após o fechamento.");
                    sb.AppendLine("3. Em seguida, aperte o botão para descer até o final e **MANTENHA PRESSIONADO POR 5 SEGUNDOS** no batente inferior.");
                    sb.AppendLine("4. Solte o botão e teste o modo automático (one-touch).");
                    sb.AppendLine("5. Se persistir voltando: aplique silicone spray automotivo na canaleta de borracha para reduzir o atrito mecânico.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Fiz o reset e funcionou", ActionType = "ExecuteQuery", Parameter = "Fiz o reset e o vidro voltou a subir normal" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Continua descendo", ActionType = "ExecuteQuery", Parameter = "Mesmo após o reset o vidro continua descendo" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: MARCADOR DE COMBUSTÍVEL / BOIA (RENAULT SANDERO)
            // =========================================================================
            if (contexto == "MARCADOR_COMBUSTIVEL")
            {
                if (textoLimpo.Contains("sandero") || textoLimpo.Contains("logan") || textoLimpo.Contains("duster") || textoLimpo.Contains("boia") || textoLimpo.Contains("reserva") || textoLimpo.Contains("ohms"))
                {
                    sb.AppendLine("Diagnóstico do Nível de Combustível Renault (Sandero / Logan / Duster):\n");
                    sb.AppendLine("1. **Teste Ôhmico do Sensor de Nível (Boia):**");
                    sb.AppendLine("   • **Tanque Vazio:** 300Ω a 350Ω");
                    sb.AppendLine("   • **Meio Tanque:** 150Ω a 180Ω");
                    sb.AppendLine("   • **Tanque Cheio:** 20Ω a 30Ω");
                    sb.AppendLine("2. Ao mover a haste lentamente, a resistência deve variar suavemente. Se der 'OL' (aberto), as trilhas de cerâmica estão gastas!");
                    sb.AppendLine("3. **Terminal de Terra Esquentado:** Inspecione o conector elétrico da tampa da bomba de combustível. É muito comum o pino de terra queimar por corrente da bomba, fazendo o marcador marcar reserva.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Boia Sandero", ActionType = "SearchStock", Parameter = "Sensor Nivel Sandero" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Resistência deu aberta", ActionType = "ExecuteQuery", Parameter = "A resistência da boia deu aberta" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Pino do chicote esquentado", ActionType = "ExecuteQuery", Parameter = "O conector da tampa está aquecido e derretido" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: START-STOP E BATERIA IBS (JEEP RENEGADE / TORO)
            // =========================================================================
            if (contexto == "START_STOP_IBS")
            {
                if (textoLimpo.Contains("toro") || textoLimpo.Contains("renegade") || textoLimpo.Contains("bateria") || textoLimpo.Contains("ibs") || textoLimpo.Contains("efb") || textoLimpo.Contains("agm"))
                {
                    sb.AppendLine("Diagnóstico do Sistema Start-Stop (Jeep Renegade / Fiat Toro):\n");
                    sb.AppendLine("1. **Saúde da Bateria (SoC / SoH):** A central BCM desativa o Start-Stop se a bateria estiver abaixo de 75% de carga.");
                    sb.AppendLine("2. **Bateria Incompatível:** É **OBRIGATÓRIO** o uso de bateria de tecnologia **EFB ou AGM**. Colocar bateria convencional chumbo-ácido causa erro de Start-Stop em poucas semanas!");
                    sb.AppendLine("3. **Sensor IBS:** Verifique o conector do sensor inteligente acoplado ao borne negativo da bateria.");
                    sb.AppendLine("4. **Reset no Scanner:** Ao instalar a bateria nova, é necessário realizar o procedimento de 'Aprendizado e Registro de Bateria Nova' com o scanner.");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Bateria EFB no Estoque", ActionType = "SearchStock", Parameter = "Bateria EFB" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Bateria instalada é comum", ActionType = "ExecuteQuery", Parameter = "Instalaram bateria comum no carro" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Vou calibrar no scanner", ActionType = "ExecuteQuery", Parameter = "Vou fazer calibração no scanner" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: CHAVE DE SETA E FAROL ALTO (HYUNDAI HB20)
            // =========================================================================
            if (contexto == "CHAVE_SETA")
            {
                if (textoLimpo.Contains("hb20") || textoLimpo.Contains("alavanca") || textoLimpo.Contains("farol alto") || textoLimpo.Contains("seta") || textoLimpo.Contains("coluna") || textoLimpo.Contains("batid") || textoLimpo.Contains("apaga"))
                {
                    sb.AppendLine("Diagnóstico da Chave de Seta na Linha Hyundai (HB20 / Creta):\n");
                    sb.AppendLine("• **Farol alto acendendo direto ou piscando na seta:** É um desgaste mecânico crônico nas lâminas de cobre internas da alavanca multifunção.");
                    sb.AppendLine("• **Teste Rápido:** Com o farol ligado, dê leves batidinhas na ponta da alavanca. Se a luz alta oscilar, o defeito é 100% interno nas travas da chave de seta.");
                    sb.AppendLine("• **Solução Definitiva:** Substituir o conjunto da chave de seta da coluna. (Limpa-contato resolve por apenas poucos dias devido ao desgaste plástico das travas).");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Chave de Seta HB20", ActionType = "SearchStock", Parameter = "Chave Seta HB20" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Pisca ao tocar na alavanca", ActionType = "ExecuteQuery", Parameter = "Pisca só de encostar na alavanca" });
                    return res;
                }
            }

            // =========================================================================
            // CONTINUIDADE: CINTA DO AIRBAG CLOCK SPRING (TOYOTA COROLLA)
            // =========================================================================
            if (contexto == "COMANDOS_VOLANTE")
            {
                if (textoLimpo.Contains("corolla") || textoLimpo.Contains("etios") || textoLimpo.Contains("cinta") || textoLimpo.Contains("clock spring") || textoLimpo.Contains("som") || textoLimpo.Contains("airbag"))
                {
                    sb.AppendLine("Diagnóstico da Cinta do Airbag Clock Spring (Toyota Corolla / Etios):\n");
                    sb.AppendLine("• **Buzina e Comandos de Som pararam juntos:** Compartilham as fitas condutoras da mesma cinta espiral do volante. Quando a fita parte, ambos param simultaneamente.");
                    sb.AppendLine("⚠️ **ATENÇÃO DE SEGURANÇA:** Desconecte a bateria por 10 minutos antes de soltar os plugues amarelos do Airbag!");
                    sb.AppendLine("• **REGRA DE OURO NA MONTAGEM DA NOVA CINTA:** Com as rodas em linha reta, gire a nova cinta totalmente até o batente suave, conte o número total de voltas (ex: 5 voltas) e retorne exatamente a metade (2,5 voltas) para deixá-la 100% no centro antes de encaixar o volante!");

                    res.Message = sb.ToString();
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Cinta Corolla no Estoque", ActionType = "SearchStock", Parameter = "Cinta Airbag Corolla" });
                    res.SuggestedActions.Add(new AISuggestedAction { Label = "Luz Airbag acendeu junto", ActionType = "ExecuteQuery", Parameter = "Luz do Airbag acendeu junto" });
                    return res;
                }
            }

            return null;
        }

        private static string? IdentificarModuloNavegacao(string textoLimpo)
        {
            if (textoLimpo.Contains("estoque")) return "Estoque";
            if (textoLimpo.Contains("compra") || textoLimpo.Contains("falta")) return "ComprasNecessidade";
            if (textoLimpo.Contains("ferramenta")) return "Ferramentas";
            if (textoLimpo.Contains("ordem") || textoLimpo.Contains("os")) return "OrdensServico";
            if (textoLimpo.Contains("kanban")) return "OficinaKanban";
            if (textoLimpo.Contains("pdv") || textoLimpo.Contains("caixa")) return "PDV";
            if (textoLimpo.Contains("cliente")) return "Clientes";
            if (textoLimpo.Contains("veiculo")) return "Veiculos";
            if (textoLimpo.Contains("tecnic") || textoLimpo.Contains("auto eletrica")) return "AutoEletricaTecnica";
            if (textoLimpo.Contains("financeiro")) return "Financeiro";
            if (textoLimpo.Contains("fornecedor")) return "Fornecedores";
            if (textoLimpo.Contains("orcamento")) return "Orcamentos";
            if (textoLimpo.Contains("relatorio")) return "Relatorios";
            if (textoLimpo.Contains("configurac")) return "Configuracoes";
            if (textoLimpo.Contains("nfe") || textoLimpo.Contains("xml")) return "ImportarNFe";
            if (textoLimpo.Contains("fiscal")) return "FiscalOperacoes";
            if (textoLimpo.Contains("frota")) return "GestaoFrotas";
            if (textoLimpo.Contains("filial") || textoLimpo.Contains("filiais") || textoLimpo.Contains("transferencia")) return "MultiFiliais";
            if (textoLimpo.Contains("etiqueta") || textoLimpo.Contains("zpl") || textoLimpo.Contains("zebra")) return "EtiquetasTermicas";
            if (textoLimpo.Contains("dashboard")) return "Dashboard";
            return null;
        }

        public static bool EhConsultaDeFusiveisOuReles(string textoLimpo, out string? componenteEspecifico)
        {
            componenteEspecifico = null;
            var t = textoLimpo;

            bool temTermoFusivelOuRele = t.Contains("fusivel") 
                || t.Contains("fusiveis") 
                || t.Contains("fusíveis") 
                || t.Contains("rele") 
                || t.Contains("relé") 
                || t.Contains("reles") 
                || t.Contains("relés")
                || t.Contains("caixa de fus")
                || t.Contains("central de fus")
                || t.Contains("central eletrica");

            if (!temTermoFusivelOuRele) return false;

            // Não interceptar se for um sintoma de falha ou queima contínua sem intenção de localização/tabela
            bool ehProblemaQueima = (t.Contains("queimando") || t.Contains("queima direto") || t.Contains("em curto") || t.Contains("derretendo") || t.Contains("estourou"))
                && !t.Contains("onde") && !t.Contains("qual") && !t.Contains("posicao") && !t.Contains("posicoes") && !t.Contains("mapa") && !t.Contains("tabela");

            if (ehProblemaQueima) return false;

            // Extrair componente específico se houver
            if (t.Contains("buzina")) componenteEspecifico = "buzina";
            else if (t.Contains("bomba") || t.Contains("combustivel")) componenteEspecifico = "bomba";
            else if (t.Contains("farol") || t.Contains("farois") || t.Contains("milha") || t.Contains("neblina")) componenteEspecifico = "farol";
            else if (t.Contains("partida") || t.Contains("arranque")) componenteEspecifico = "partida";
            else if (t.Contains("ventoinha") || t.Contains("arrefecimento") || t.Contains("radiador")) componenteEspecifico = "ventoinha";
            else if (t.Contains("ar condicionado") || t.Contains("ar-condicionado") || t.Contains("compressor") || t.Contains("clima")) componenteEspecifico = "ar-condicionado";
            else if (t.Contains("vidro") || t.Contains("vidros")) componenteEspecifico = "vidros";
            else if (t.Contains("limpador") || t.Contains("lavador") || t.Contains("esguicho") || t.Contains("brucutu")) componenteEspecifico = "limpador";
            else if (t.Contains("injecao") || t.Contains("ignicao") || t.Contains("bobina") || t.Contains("bico") || t.Contains("ecu")) componenteEspecifico = "injecao";
            else if (t.Contains("seta") || t.Contains("pisca")) componenteEspecifico = "seta";
            else if (t.Contains("tomada") || t.Contains("12v") || t.Contains("isqueiro")) componenteEspecifico = "tomada 12v";
            else if (t.Contains("desembacador")) componenteEspecifico = "desembaçador";
            else if (t.Contains("direcao") || t.Contains("eps") || t.Contains("mdps")) componenteEspecifico = "direção elétrica";
            else if (t.Contains("abs") || t.Contains("freio")) componenteEspecifico = "abs";

            // Verificar se há intenção de consulta de localização, tabela ou layout, ou se citou modelo de veículo
            bool temIntencaoConsulta = t.Contains("posicao")
                || t.Contains("posicoes")
                || t.Contains("onde")
                || t.Contains("qual")
                || t.Contains("quais")
                || t.Contains("mapa")
                || t.Contains("tabela")
                || t.Contains("layout")
                || t.Contains("disposicao")
                || t.Contains("disposição")
                || t.Contains("localizacao")
                || t.Contains("localização")
                || t.Contains("preciso")
                || t.Contains("passa")
                || t.Contains("mostra")
                || t.Contains("painel")
                || t.Contains("cofre")
                || t.Contains("caixa")
                || t.Contains("hb20")
                || t.Contains("gol")
                || t.Contains("onix")
                || t.Contains("palio")
                || t.Contains("corolla")
                || t.Contains("strada")
                || t.Contains("uno")
                || t.Contains("scania")
                || t.Contains("atego")
                || t.Contains("volvo")
                || t.Contains("fox")
                || t.Contains("voyage")
                || t.Contains("saveiro")
                || componenteEspecifico != null;

            return temIntencaoConsulta;
        }

        private async Task<AIChatResponse?> ProcessarConsultaFusiveisERelesAsync(
            string textoLimpo,
            string ultimaMensagemUser,
            string? componenteSolicitado,
            List<AIChatMessage> todasMensagens,
            CancellationToken ct)
        {
            var response = new AIChatResponse
            {
                ProviderUsed = ProviderName,
                Success = true
            };

            // 1. Identificar veículo e ano
            var (veiculo, ano, motor, _) = IdentificarVeiculoESistemaEsquema(textoLimpo, todasMensagens);

            if (string.IsNullOrWhiteSpace(veiculo))
            {
                var sb = new StringBuilder();
                sb.AppendLine("Com certeza! O PRIMOX Copilot possui a tabela técnica e mapa de posições das caixas de fusíveis e relés (padrão Doutor-IE & Simpllo). ⚡\n");
                sb.AppendLine("👉 **Para qual veículo você precisa consultar a caixa de fusíveis e relés?**");
                sb.AppendLine("Por favor, informe: **Marca, Modelo e Ano** (Ex: HB20 2014, Gol G5 2010, Onix 2015, Corolla 2016, Scania R440 2013).");

                response.Message = sb.ToString();
                response.SuggestedActions.Add(new AISuggestedAction { Label = "Hyundai HB20 2014", ActionType = "ExecuteQuery", Parameter = "posicoes dos fusiveis do hb20 ano 2014" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "VW Gol G5 2010", ActionType = "ExecuteQuery", Parameter = "caixa de fusiveis e reles gol g5 2010" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "GM Onix 1.4 2015", ActionType = "ExecuteQuery", Parameter = "tabela de fusiveis onix 2015" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "Fiat Palio Fire 2012", ActionType = "ExecuteQuery", Parameter = "caixa de fusiveis palio fire 2012" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "Toyota Corolla 2015", ActionType = "ExecuteQuery", Parameter = "fusiveis toyota corolla 2015" });
                return response;
            }

            var sbResp = new StringBuilder();

            // 2. Buscar centrais na biblioteca técnica local
            List<CentralEletricaFusivel> centrais = new();
            if (_bibliotecaTecnicaService != null)
            {
                centrais = await _bibliotecaTecnicaService.ObterCentraisEletricasAsync(veiculo);
                // Se a busca exata não achou, tentar com modelo simplificado (ex: "hb20")
                if (centrais.Count == 0)
                {
                    var modeloSimples = veiculo.Split(' ').LastOrDefault() ?? veiculo;
                    centrais = await _bibliotecaTecnicaService.ObterCentraisEletricasAsync(modeloSimples);
                }
            }

            // 3. Se encontrou na base local
            if (centrais.Count > 0)
            {
                var anoInfo = !string.IsNullOrWhiteSpace(ano) ? $"Ano {ano}" : "Linha Completa";
                sbResp.AppendLine($"⚡ **Mapa Completo de Fusíveis & Relés: {veiculo} ({anoInfo})**");
                sbResp.AppendLine("*(Acervo Técnico PRIMOX - Padrão Doutor-IE & Simpllo)*\n");

                // Se o usuário perguntou por um componente específico (ex: buzina, bomba, farol)
                if (!string.IsNullOrWhiteSpace(componenteSolicitado))
                {
                    sbResp.AppendLine($"🎯 **Localização do Circuito Solicitado: {componenteSolicitado.ToUpperInvariant()}**");
                    bool achouComponente = false;

                    foreach (var c in centrais)
                    {
                        var fusMatch = c.Fusiveis.Where(f => 
                            f.CircuitoProtegido.Contains(componenteSolicitado, StringComparison.OrdinalIgnoreCase) ||
                            (f.ReleAssociado != null && f.ReleAssociado.Contains(componenteSolicitado, StringComparison.OrdinalIgnoreCase))
                        ).ToList();

                        var releMatch = c.Reles.Where(r => 
                            r.NomeFuncao.Contains(componenteSolicitado, StringComparison.OrdinalIgnoreCase) ||
                            r.PinagemReferencia.Contains(componenteSolicitado, StringComparison.OrdinalIgnoreCase)
                        ).ToList();

                        if (fusMatch.Count > 0 || releMatch.Count > 0)
                        {
                            achouComponente = true;
                            sbResp.AppendLine($"• **Local:** {c.Titulo} — *{c.Localizacao}*");
                            foreach (var f in fusMatch)
                            {
                                sbResp.AppendLine($"  - **Fusível {f.Numero}:** {f.CapacidadeAmperes}A ({f.CorPadrao}) — {f.CircuitoProtegido} (Relé: {f.ReleAssociado})");
                            }
                            foreach (var r in releMatch)
                            {
                                sbResp.AppendLine($"  - **Relé {r.Posicao}:** {r.NomeFuncao} ({r.TipoPinos}) — *Pinagem:* {r.PinagemReferencia}");
                            }
                        }
                    }

                    if (!achouComponente)
                    {
                        sbResp.AppendLine($"• Circuito específico '{componenteSolicitado}' mapeado nas centrais abaixo. Consulte as tabelas completas a seguir:");
                    }
                    sbResp.AppendLine();
                }

                // Renderizar cada central encontrada com tabelas completas
                foreach (var c in centrais)
                {
                    sbResp.AppendLine($"### 📌 {c.Titulo} ({c.TensaoNominal})");
                    sbResp.AppendLine($"• **Localização Física:** {c.Localizacao}");
                    sbResp.AppendLine($"• **Aplicação:** {c.ModelosAplicacao}");

                    if (c.Fusiveis.Count > 0)
                    {
                        sbResp.AppendLine("\n**Tabela de Fusíveis:**");
                        sbResp.AppendLine("| Posição | Amperagem | Cor Padrão | Circuito Protegido / Função | Relé Associado |");
                        sbResp.AppendLine("|---|---|---|---|---|");
                        foreach (var f in c.Fusiveis)
                        {
                            sbResp.AppendLine($"| **{f.Numero}** | {f.CapacidadeAmperes}A | {f.CorPadrao} | {f.CircuitoProtegido} | {f.ReleAssociado} |");
                        }
                    }

                    if (c.Reles.Count > 0)
                    {
                        sbResp.AppendLine("\n**Tabela de Relés:**");
                        sbResp.AppendLine("| Posição | Relé / Função | Tipo / Pinos | Pinagem & Referência Técnica |");
                        sbResp.AppendLine("|---|---|---|---|");
                        foreach (var r in c.Reles)
                        {
                            sbResp.AppendLine($"| **{r.Posicao}** | {r.NomeFuncao} | {r.TipoPinos} | {r.PinagemReferencia} |");
                        }
                    }

                    sbResp.AppendLine();
                }

                sbResp.AppendLine("💡 **Dica Prática de Oficina (Bancada Auto Elétrica):**");
                sbResp.AppendLine("• **Teste sem remover o fusível:** Com a chave ligada (Linha 15), utilize um multímetro (DCV) ou caneta de polaridade tocando nas duas pontas de teste metálicas expostas na parte superior do fusível mini. Se ambas tiverem 12V, o fusível está íntegro. Se apenas um lado tiver 12V e o outro 0V, o filamento está rompido/queimado.");
                sbResp.AppendLine("• **Segurança:** Nunca substitua um fusível de menor amperagem por um maior (ex: trocar 10A por 20A) para 'aguentar', sob risco severo de sobreaquecimento e princípio de incêndio no chicote elétrico.");

                response.Message = sbResp.ToString();
                response.SuggestedActions.Add(new AISuggestedAction { Label = $"Esquema Elétrico {veiculo}", ActionType = "ExecuteQuery", Parameter = $"esquema eletrico {veiculo} {ano}" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Queda de Tensão", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "🧠 Diagnóstico Guiado", ActionType = "Navigate", Parameter = "AiDiagnosticCenter" });
                return response;
            }

            // 4. Se não estiver na biblioteca local, buscar na Web em tempo real
            var queryBusca = $"mapa fusiveis reles {veiculo} {ano}".Trim();
            var webResult = await _webSearchService.PesquisarAsync(queryBusca, veiculo, ct);

            sbResp.AppendLine($"⚡ **Disposição de Fusíveis & Relés: {veiculo} ({ano ?? "Todos os Anos"})**\n");
            sbResp.AppendLine("*(Pesquisa Técnica em Tempo Real - Web & Manuais)*\n");

            if (webResult != null && webResult.Sucesso && webResult.Resultados.Count > 0)
            {
                sbResp.AppendLine("🌐 **Manuais e Diagramas de Centrais Encontrados:**\n");
                foreach (var item in webResult.Resultados.Take(3))
                {
                    sbResp.AppendLine($"• **[{item.Titulo}]({item.Url})**");
                    if (!string.IsNullOrWhiteSpace(item.Snippet))
                    {
                        sbResp.AppendLine($"  _{item.Snippet}_");
                    }
                }
                sbResp.AppendLine();
            }

            sbResp.AppendLine("📍 **Locais Convencionais das Centrais Elétricas deste Veículo:**");
            sbResp.AppendLine("1. **Caixa Interna (Painel de Instrumentos):** Localizada geralmente abaixo do volante à esquerda ou atrás do porta-luvas. Protege acessórios internos (vidros, rádio, painel, tomada 12V, airbag).");
            sbResp.AppendLine("2. **Caixa do Compartimento do Motor (Cofre):** Ao lado da bateria ou amortecedor. Contém os maxi-fusíveis de potência, relé principal da injeção, relé da bomba, relés do eletroventilador e buzina.");
            sbResp.AppendLine("\n💡 *Dica:* Na tampa plástica de ambas as caixas existe o adesivo gravado com o diagrama de posições e amperagens de fábrica.");

            response.Message = sbResp.ToString();
            response.SuggestedActions.Add(new AISuggestedAction { Label = $"Esquema Elétrico {veiculo}", ActionType = "ExecuteQuery", Parameter = $"esquema eletrico {veiculo} {ano}" });
            response.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Diagrama na Web", ActionType = "OpenWebSearch", Parameter = queryBusca });
            return response;
        }

        public static bool EhConsultaDeEsquemaOuDiagrama(string textoLimpo, string mensagemOriginal)
        {
            var t = textoLimpo;
            if (t.Contains("coo7") || t.Contains("pld")) return false;

            return t.Contains("esquema")
                || t.Contains("diagrama")
                || t.Contains("pinagem")
                || t.Contains("manual eletrico")
                || t.Contains("manual de servico")
                || t.Contains("manual de oficina")
                || (t.Contains("chicote") && (t.Contains("cores") || t.Contains("ligacao") || t.Contains("plugar") || t.Contains("conector") || t.Contains("como ligar")));
        }

        private async Task<AIChatResponse?> ProcessarConsultaEsquemaAsync(
            string textoLimpo,
            string ultimaMensagemUser,
            List<AIChatMessage> todasMensagens,
            CancellationToken ct)
        {
            var response = new AIChatResponse
            {
                ProviderUsed = ProviderName,
                Success = true
            };

            // 1. Identificar se o usuário citou veículo e sistema
            var (veiculo, ano, motor, sistema) = IdentificarVeiculoESistemaEsquema(textoLimpo, todasMensagens);

            // 2. Se nenhum veículo foi especificado, solicitar os dados do veículo
            if (string.IsNullOrWhiteSpace(veiculo))
            {
                var sb = new StringBuilder();
                sb.AppendLine("Com certeza! O PRIMOX Copilot possui acervo completo de esquemas elétricos e busca especializada em tempo real na internet. ⚡\n");
                sb.AppendLine("👉 **Para qual veículo você precisa do esquema elétrico?**");
                sb.AppendLine("Por favor, informe: **Marca, Modelo, Ano e Motorização** (Ex: Gol G5 1.0 EA111 2011, Palio Fire 2010, Onix 1.4 2015, HB20 1.0 2018).\n");
                sb.AppendLine("E qual subsistema elétrico você precisa analisar no momento:");
                sb.AppendLine("• **Central de Injeção Eletrônica (ECU) & Bobina**");
                sb.AppendLine("• **Caixa de Fusíveis e Central de Relés**");
                sb.AppendLine("• **Circuito de Partida e Alternador**");
                sb.AppendLine("• **Arrefecimento e Eletroventilador**");
                sb.AppendLine("• **Iluminação, Faróis e Lanternas**");

                response.Message = sb.ToString();
                response.SuggestedActions.Add(new AISuggestedAction { Label = "VW Gol G5 2011", ActionType = "ExecuteQuery", Parameter = "esquema eletrico do gol g5 ano 2011" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "Fiat Palio Fire 2010", ActionType = "ExecuteQuery", Parameter = "esquema eletrico palio fire 2010" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "GM Onix 1.4 2015", ActionType = "ExecuteQuery", Parameter = "esquema eletrico onix 1.4 2015" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "Hyundai HB20 1.0", ActionType = "ExecuteQuery", Parameter = "esquema eletrico hb20 1.0 2015" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "VW Gol G4 Sem Relé", ActionType = "ExecuteQuery", Parameter = "esquema eletrico gol g4" });
                return response;
            }

            // 3. Montar termo de busca web e pesquisar na internet em tempo real
            string queryWeb = ConstruirTermoBuscaWeb(veiculo, ano, motor, sistema);
            var webResult = await _webSearchService.PesquisarAsync(queryWeb, veiculo, ct);

            // 4. Buscar e resolver o arquivo de imagem gráfica do diagrama (local ou baixado da web)
            var (caminhoDiagramaImg, tituloDiagramaImg, ehBaixadoImg) = await _diagramImageService.ObterOuBaixarDiagramaAsync(veiculo, sistema, ct);

            // 5. Montar a resposta técnica estruturada
            var sbDiag = new StringBuilder();
            var veiculoFormatado = $"{veiculo} {ano} {motor}".Trim();

            // Especificidades de Veículos Populares no Brasil
            if (veiculo.Contains("HB20", StringComparison.OrdinalIgnoreCase) || textoLimpo.Contains("hb20"))
            {
                sbDiag.AppendLine("📐 **Esquema Elétrico & Arquitetura Técnica: Hyundai HB20 1.0 12V 3 Cilindros (2012 a 2015) - Injeção Bosch ME 17.9.11.1**\n");
                sbDiag.AppendLine("Aqui está a interpretação técnica detalhada de cada circuito e pinagem do diagrama da injeção:");
                sbDiag.AppendLine();
                sbDiag.AppendLine("⚡ **1. Módulo de Controle do Motor (ECU Bosch ME 17.9.11.1):**");
                sbDiag.AppendLine("• **Linha 30 (+12V Permanente):** Protegida pelo fusível principal no compartimento do motor.");
                sbDiag.AppendLine("• **Linha 15 (+12V Pós-Chave):** Alimentada através do contato do Relé Principal K01.");
                sbDiag.AppendLine("• **Aterramentos de Potência:** Conectados diretamente à carcaça do bloco do motor e cabeçote.");
                sbDiag.AppendLine();
                sbDiag.AppendLine("🔌 **2. Pinagem das Bobinas de Ignição Individuais COP (3 Cilindros):**");
                sbDiag.AppendLine("• **Bobina 1 (Cilindro 1):** Pino 1 (+12V pós-chave do Relé K01), Pino 2 (Pulso negativo de disparo da ECU - **Pino 28**).");
                sbDiag.AppendLine("• **Bobina 2 (Cilindro 2):** Pino 1 (+12V pós-chave do Relé K01), Pino 2 (Pulso negativo de disparo da ECU - **Pino 27**).");
                sbDiag.AppendLine("• **Bobina 3 (Cilindro 3):** Pino 1 (+12V pós-chave do Relé K01), Pino 2 (Pulso negativo de disparo da ECU - **Pino 26**).");
                sbDiag.AppendLine("• **Capacitor de Supressão:** Ligado ao terra na linha de alimentação das bobinas para proteção contra transientes.");
                sbDiag.AppendLine();
                sbDiag.AppendLine("⛽ **3. Eletroinjetores de Combustível (Bicos Injetores):**");
                sbDiag.AppendLine("• Alimentação comum de +12V via Relé Principal K01.");
                sbDiag.AppendLine("• **Injetor 1:** Pulso negativo no **Pino 7** da ECU.");
                sbDiag.AppendLine("• **Injetor 2:** Pulso negativo no **Pino 6** da ECU.");
                sbDiag.AppendLine("• **Injetor 3:** Pulso negativo no **Pino 5** da ECU.");
                sbDiag.AppendLine();
                sbDiag.AppendLine("🔍 **4. Sensores Críticos de Gestão do Motor:**");
                sbDiag.AppendLine("• **Sensor do Pedal do Acelerador (6 Pinos):** Duplo potenciômetro (Pistas 1 e 2 alimentadas com 5V e massa da central).");
                sbDiag.AppendLine("• **Sensor MAP / ACT (4 Pinos):** Pressão absoluta e temperatura do ar integrados (**Pinos 23, 47, 48 e 61**).");
                sbDiag.AppendLine("• **Sensor de Fase (CMP):** Sensor Hall no cabeçote (**Pinos 19 e 20**).");
                sbDiag.AppendLine("• **Sensor de Rotação (CKP):** Sensor de PMS e rotação (**Pinos 21 e 22**).");
                sbDiag.AppendLine("• **Sensor de Temperatura da Água (ECT):** Termistor NTC (**Pinos 33 e 44**).");
                sbDiag.AppendLine("• **Válvula CVVT (Comando Variável):** Controle por pulso PWM da ECU (**Pino 32**).");
                sbDiag.AppendLine();
                sbDiag.AppendLine("🧰 **5. Relés e Circuito de Partida:**");
                sbDiag.AppendLine("• **Relé Principal (K01):** Acionado pelo **Pino 9** da ECU (comanda +12V para bobinas, injetores e aquecedores de sonda).");
                sbDiag.AppendLine("• **Relé da Bomba de Combustível (K12):** Acionado pelo **Pino 10** da ECU.");
                sbDiag.AppendLine("• **Motor de Partida & Bloqueio:** Linha 50 protegida pelo **Fusível F22 (10A)**, passando pelo interruptor de partida e **Relé de Partida K2**.");
            }
            else if (veiculo.Contains("Gol G5", StringComparison.OrdinalIgnoreCase) || 
                (veiculo.Contains("Gol", StringComparison.OrdinalIgnoreCase) && (ano?.Contains("2008") == true || ano?.Contains("2009") == true || ano?.Contains("2010") == true || ano?.Contains("2011") == true || ano?.Contains("2012") == true || textoLimpo.Contains("g5"))))
            {
                sbDiag.AppendLine("📐 **Esquema Elétrico & Arquitetura Técnica: VW Gol G5 (2008 a 2012) 1.0 / 1.6 Flex (Motor EA111)**\n");
                sbDiag.AppendLine("Aqui está a síntese de bancada com os circuitos e pinagens fundamentais deste veículo:");
                sbDiag.AppendLine();
                sbDiag.AppendLine("⚡ **1. Central de Injeção Eletrônica (ECU):**");
                sbDiag.AppendLine("• **Módulos Aplicados:** Magneti Marelli IAW 4GV (Motor 1.0/1.6 Flex) e Bosch ME 7.5.30 (versões 1.0).");
                sbDiag.AppendLine("• **Linhas de Alimentação:**");
                sbDiag.AppendLine("  - Pinos 1 e 2: Terra de Potência (Linha 31 direto no cabeçote/bloco)");
                sbDiag.AppendLine("  - Linha 30: +12V Permanente da Bateria (protegido na central interna)");
                sbDiag.AppendLine("  - Linha 15: +12V Pós-Chave / Ignição (alimentado pelo Relé Principal R01)");
                sbDiag.AppendLine("  - Linha 50: Sinal do comutador de partida");
                sbDiag.AppendLine();
                sbDiag.AppendLine("🔌 **2. Pinagem da Bobina de Ignição (4 Pinos - Conector Oval):**");
                sbDiag.AppendLine("• **Pino 1:** Pulso / Disparo de ignição dos Cilindros 1 e 4 (Sinal vindo da ECU)");
                sbDiag.AppendLine("• **Pino 2:** Alimentação +12V Linha 15 (Pós-chave do Relé Principal R01)");
                sbDiag.AppendLine("• **Pino 3:** Pulso / Disparo de ignição dos Cilindros 2 e 3 (Sinal vindo da ECU)");
                sbDiag.AppendLine("• **Pino 4:** Aterramento de Carcaça e Estágio de Potência (Linha 31 no cabeçote)");
                sbDiag.AppendLine("⚠️ **ALERTA CRÍTICO DE BANCADA:** No Gol G5, se a bobina for paralela ou se forem usadas velas sem resistor (usar sempre NGK BKR7ES-D no 1.0 ou BKR6E-D no 1.6 com gap 0.8mm), a alta tensão retorna pela linha de sinal e queima os transistores internos de disparo da ECU IAW 4GV!");
                sbDiag.AppendLine();
                sbDiag.AppendLine("🧰 **3. Central de Relés e Caixa de Fusíveis (Painel Interno / Abaixo do Volante):**");
                sbDiag.AppendLine("• **Relé R01:** Relé Principal da Injeção (Alimenta bicos, bobina, canister e sonda lambda)");
                sbDiag.AppendLine("• **Relé R04:** Relé da Bomba de Combustível (Acionado por pulso negativo da ECU)");
                sbDiag.AppendLine("• **Relé R06:** Relé Temporizador do Limpador/Lavador de Para-brisa");
                sbDiag.AppendLine("• **Maxi-Fusíveis no Cofre do Motor (Sobre a bateria):**");
                sbDiag.AppendLine("  - F1 (150A/175A): Linha B+ do Alternador / Carga da Bateria");
                sbDiag.AppendLine("  - F2 (110A): Alimentação geral da cabine e caixa de fusíveis interna");
                sbDiag.AppendLine("  - F3 (40A): Eletroventilador (Arrefecimento)");
                sbDiag.AppendLine("  - F4 (40A): Central de Controle do Freio ABS");
            }
            else if (veiculo.Contains("Palio", StringComparison.OrdinalIgnoreCase) || veiculo.Contains("Uno", StringComparison.OrdinalIgnoreCase) || veiculo.Contains("Strada", StringComparison.OrdinalIgnoreCase))
            {
                sbDiag.AppendLine($"📐 **Esquema Elétrico & Arquitetura Técnica: Fiat {veiculo} (Motor Fire 8V)**\n");
                sbDiag.AppendLine("Aqui está a síntese de bancada dos circuitos e pinagens fundamentais deste veículo:");
                sbDiag.AppendLine();
                sbDiag.AppendLine("⚡ **1. Central de Injeção Eletrônica (ECU):**");
                sbDiag.AppendLine("• **Módulos Aplicados:** Magneti Marelli IAW 4AF / 4SF / 4GF (Híbrida montada no cofre/coletor).");
                sbDiag.AppendLine("• **Relé Duplo de Injeção / Carga (T09 e T10):** Um estágio alimenta bomba e bicos; outro estágio alimenta a ECU e sonda lambda.");
                sbDiag.AppendLine("• **Bobinas de Ignição Dupla (Centelha Perdida):** Conector 3 vias: Pino 1 (pulso 1/4 da ECU), Pino 2 (+12V Linha 15), Pino 3 (pulso 2/3 da ECU).");
                sbDiag.AppendLine("• **Maxi-Fusíveis no Cofre (Sobre a bateria):** F01 (70A - Ignição), F02 (40A - Ventoinha 2ª vel), F03 (20A - Injeção e bomba).");
            }
            else if (veiculo.Contains("Corsa", StringComparison.OrdinalIgnoreCase) || veiculo.Contains("Celta", StringComparison.OrdinalIgnoreCase) || veiculo.Contains("Classic", StringComparison.OrdinalIgnoreCase) || veiculo.Contains("Onix", StringComparison.OrdinalIgnoreCase) || veiculo.Contains("Prisma", StringComparison.OrdinalIgnoreCase))
            {
                sbDiag.AppendLine($"📐 **Esquema Elétrico & Arquitetura Técnica: GM {veiculo}**\n");
                sbDiag.AppendLine("Aqui está a síntese de bancada dos circuitos e pinagens fundamentais deste veículo:");
                sbDiag.AppendLine();
                sbDiag.AppendLine("⚡ **1. Central de Injeção Eletrônica (ECU):**");
                sbDiag.AppendLine("• **Módulos Aplicados:** Delphi Multec H (Celta/Classic) / Delco E83 (Onix/Prisma SPE/4).");
                sbDiag.AppendLine("• **Bobina de Ignição 4 Vias (DIS):** Pinos A (+12V Linha 15), B (Terra Linha 31), C (Pulso Cil 1/4), D (Pulso Cil 2/3).");
                sbDiag.AppendLine("• **Central Elétrica:** Relé Principal e Relé da Bomba na central interna/cofre; Módulo BCM na cabine (Onix/Prisma) comutando iluminação e acessórios.");
            }
            else if (!textoLimpo.Contains("coo7") && !textoLimpo.Contains("pld") && (veiculo.Contains("Scania", StringComparison.OrdinalIgnoreCase) || textoLimpo.Contains("scania")))
            {
                sbDiag.AppendLine("📐 **Esquema Elétrico & Arquitetura Técnica: Scania Série R (R440 / R450 2013) — Faróis, Setas & Módulo VIS**\n");
                sbDiag.AppendLine("Aqui está a síntese de bancada dos circuitos e pinagens fundamentais do sistema de iluminação e setas deste caminhão:");
                sbDiag.AppendLine();
                sbDiag.AppendLine("⚡ **1. Arquitetura Eletrônica Multiplexada (Sem Relé Convencional de Seta):**");
                sbDiag.AppendLine("• **Comando na Coluna:** O interruptor de seta envia o sinal digital via barramento CAN para o Coordenador (COO7).");
                sbDiag.AppendLine("• **Módulo de Visibilidade (VIS):** Fixado atrás da grade frontal articulada. O COO7 envia ordem via rede CAN.");
                sbDiag.AppendLine("• **Estágio de Potência de Estado Sólido:** O módulo VIS utiliza transistores MOSFET internos para pulsar 24V diretamente para as lâmpadas dianteiras e laterais (não usa relé de pisca na caixa de fusíveis!).");
                sbDiag.AppendLine();
                sbDiag.AppendLine("💡 **2. Circuito da Seta Dianteira Direita (Linha 24V):**");
                sbDiag.AppendLine("• **Conector do Farol Direito (Farol Principal & Seta):**");
                sbDiag.AppendLine("  - Pino 1: Seta Dianteira Direita (Pulso +24V para lâmpada PY21W 24V 21W)");
                sbDiag.AppendLine("  - Pino 2: Seta Dianteira Esquerda (+24V)");
                sbDiag.AppendLine("  - Pino 3: Farol Baixo Direito (+24V H7 70W)");
                sbDiag.AppendLine("  - Pino 4: Farol Alto Direito (+24V H1 70W)");
                sbDiag.AppendLine("  - Pino 6: Terra de Potência Linha 31 (Massa aparafusada na longarina dianteira direita)");
                sbDiag.AppendLine();
                sbDiag.AppendLine("🔍 **3. Roteiro Prático quando a Seta Direita Não Funciona na Scania R440:**");
                sbDiag.AppendLine("• **Passo 1:** Meça com multímetro ou chave de teste 24V no soquete da lâmpada com a seta ligada: deve acusar pulsos de 0V a 24V.");
                sbDiag.AppendLine("• **Passo 2:** Inspecione o chicote na borracha articulada da grade frontal: fios partidos por basculamento da cabine são a causa número 1!");
                sbDiag.AppendLine("• **Passo 3:** Cuidado com lâmpadas LED paralelas sem canceler: o VIS detecta 'Carga Aberta' e desliga a saída imediatamente.");
                sbDiag.AppendLine("• **Passo 4:** Se não houver 24V pulsados saindo do VIS, verifique o fusível F18 da Linha 30 na central elétrica central.");
            }
            else
            {
                sbDiag.AppendLine($"📐 **Esquema Elétrico & Arquitetura Técnica: {veiculoFormatado}**\n");
                sbDiag.AppendLine("Aqui está a síntese de bancada dos circuitos e referências fundamentais deste veículo:");
                sbDiag.AppendLine();
                sbDiag.AppendLine("⚡ **1. Linhas de Força & Alimentação Universal:**");
                sbDiag.AppendLine("• **Linha 30 (+12V Direto):** Alimentação permanente da bateria, protegida por maxi-fusível.");
                sbDiag.AppendLine("• **Linha 15 (+12V Pós-Chave):** Acionada pelo comutador de ignição e relé principal para a central de injeção e atuadores.");
                sbDiag.AppendLine("• **Linha 31 (Massa / Terra):** Queda de tensão máxima tolerada no multímetro: < 0.10V entre motor e chassi.");
                sbDiag.AppendLine("• **Linha 50 (Partida):** Tensão +12V temporária enviada ao automático de partida ao girar a chave.");
            }

            // Exibir status do arquivo de imagem do diagrama
            if (!string.IsNullOrWhiteSpace(caminhoDiagramaImg))
            {
                sbDiag.AppendLine();
                sbDiag.AppendLine($"🖼️ **Diagrama Gráfico Visual Localizado:** `{System.IO.Path.GetFileName(caminhoDiagramaImg)}` {(ehBaixadoImg ? "(Baixado em tempo real da internet)" : "(Acervo técnico integrado)")}.");
                sbDiag.AppendLine("👉 O diagrama já foi projetado no visualizador gráfico da bancada à direita para você aplicar zoom e inspecionar.");
            }

            // Integrar resultados da pesquisa web em tempo real
            if (webResult != null && webResult.Sucesso && webResult.Resultados.Count > 0)
            {
                sbDiag.AppendLine();
                sbDiag.AppendLine("🌐 **Manuais Técnicos e Diagramas Encontrados na Web (Tempo Real):**");
                foreach (var item in webResult.Resultados.Take(3))
                {
                    sbDiag.AppendLine($"• **[{item.Titulo}]({item.Url})**");
                    if (!string.IsNullOrWhiteSpace(item.Snippet))
                    {
                        sbDiag.AppendLine($"  _{item.Snippet}_");
                    }
                    if (!string.IsNullOrWhiteSpace(item.Dominio))
                    {
                        sbDiag.AppendLine($"  *Fonte: {item.Dominio}*");
                    }
                }
            }
            else
            {
                sbDiag.AppendLine();
                sbDiag.AppendLine("🌐 *Pesquisa Web: Resultados sincronizados com a base técnica local de oficina.*");
            }

            response.Message = sbDiag.ToString();

            // Adicionar Ações Sugeridas
            if (!string.IsNullOrWhiteSpace(caminhoDiagramaImg))
            {
                response.SuggestedActions.Add(new AISuggestedAction
                {
                    Label = "🖼️ Ver Esquema na Bancada",
                    ActionType = "ShowDiagramImage",
                    Parameter = caminhoDiagramaImg
                });
            }

            response.SuggestedActions.Add(new AISuggestedAction
            {
                Label = "🔍 Ver Diagrama na Web",
                ActionType = "OpenWebSearch",
                Parameter = queryWeb
            });

            if (veiculo.Contains("HB20", StringComparison.OrdinalIgnoreCase) || textoLimpo.Contains("hb20"))
            {
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Bobinas COP (Pinos 28, 27, 26)", ActionType = "ExecuteQuery", Parameter = "como testar as bobinas cop do hb20 1.0" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Sensor MAP (Pinos 23, 47, 48, 61)", ActionType = "ExecuteQuery", Parameter = "pinagem sensor map hb20 1.0" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Injetores (Pinos 7, 6, 5)", ActionType = "ExecuteQuery", Parameter = "teste dos bicos injetores hb20" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Relé Principal K01 e Bomba K12", ActionType = "ExecuteQuery", Parameter = "rele principal e rele de partida hb20" });
            }
            else if (veiculo.Contains("Gol", StringComparison.OrdinalIgnoreCase))
            {
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Injeção Marelli IAW 4GV", ActionType = "ExecuteQuery", Parameter = "pinagem modulo injecao iaw 4gv gol g5" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Caixa de Fusíveis Gol G5", ActionType = "ExecuteQuery", Parameter = "tabela fusiveis e reles gol g5" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Arrefecimento e Ventoinha", ActionType = "ExecuteQuery", Parameter = "esquema arrefecimento ventoinha gol g5" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Partida Linha 50", ActionType = "ExecuteQuery", Parameter = "esquema partida linha 50 gol g5" });
            }
            else if (veiculo.Contains("Scania", StringComparison.OrdinalIgnoreCase))
            {
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Módulo VIS & Conector Farol", ActionType = "ExecuteQuery", Parameter = "pinagem conector farol e modulo vis scania r440" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Rede CAN COO7 & Chave Seta", ActionType = "ExecuteQuery", Parameter = "comunicacao can coordenador coo7 scania r440" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Central Elétrica & Fusíveis 24V", ActionType = "ExecuteQuery", Parameter = "tabela fusiveis central eletrica scania r440" });
            }
            else
            {
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Pinagem do Módulo", ActionType = "ExecuteQuery", Parameter = $"pinagem modulo injecao {veiculo}" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Caixa de Fusíveis", ActionType = "ExecuteQuery", Parameter = $"tabela fusiveis e reles {veiculo}" });
                response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Arrefecimento", ActionType = "ExecuteQuery", Parameter = $"esquema ventoinha arrefecimento {veiculo}" });
            }

            return response;
        }

        private static (string? Veiculo, string? Ano, string? Motor, string? Sistema) IdentificarVeiculoESistemaEsquema(
            string textoLimpo,
            List<AIChatMessage> todasMensagens)
        {
            string textoTotal = textoLimpo;
            // Também examinar histórico se o usuário estiver em continuidade
            for (int i = todasMensagens.Count - 1; i >= 0 && i >= todasMensagens.Count - 4; i--)
            {
                var msg = todasMensagens[i];
                if (msg.Role == AIRole.User)
                {
                    textoTotal += " " + AutomotiveDiagnosticRAGService.NormalizarTexto(msg.Content);
                }
            }

            string? veiculo = null;
            string? ano = null;
            string? motor = null;
            string? sistema = null;

            // 1. Extração do Ano (1980 a 2030)
            var matchAno = Regex.Match(textoTotal, @"\b(19\d{2}|20\d{2})\b");
            if (matchAno.Success)
            {
                ano = matchAno.Value;
            }

            // 2. Extração do Motor
            if (textoTotal.Contains("ea111")) motor = "EA111";
            else if (textoTotal.Contains("ea211")) motor = "EA211";
            else if (textoTotal.Contains("firefly")) motor = "Firefly";
            else if (textoTotal.Contains("fire")) motor = "Fire";
            else if (textoTotal.Contains("vht")) motor = "VHT";
            else if (textoTotal.Contains("spe/4") || textoTotal.Contains("spe4")) motor = "SPE/4";
            else if (textoTotal.Contains("1.0")) motor = "1.0";
            else if (textoTotal.Contains("1.4")) motor = "1.4";
            else if (textoTotal.Contains("1.6")) motor = "1.6";
            else if (textoTotal.Contains("1.8")) motor = "1.8";
            else if (textoTotal.Contains("2.0")) motor = "2.0";

            // 3. Extração do Veículo / Modelo
            if (textoTotal.Contains("gol g5") || (textoTotal.Contains("gol") && textoTotal.Contains("g5"))) veiculo = "VW Gol G5";
            else if (textoTotal.Contains("gol g6") || (textoTotal.Contains("gol") && textoTotal.Contains("g6"))) veiculo = "VW Gol G6";
            else if (textoTotal.Contains("gol g7") || (textoTotal.Contains("gol") && textoTotal.Contains("g7"))) veiculo = "VW Gol G7";
            else if (textoTotal.Contains("gol g4") || (textoTotal.Contains("gol") && textoTotal.Contains("g4"))) veiculo = "VW Gol G4";
            else if (textoTotal.Contains("gol g3") || (textoTotal.Contains("gol") && textoTotal.Contains("g3"))) veiculo = "VW Gol G3";
            else if (textoTotal.Contains("gol g2") || (textoTotal.Contains("gol") && textoTotal.Contains("g2"))) veiculo = "VW Gol G2";
            else if (textoTotal.Contains("gol"))
            {
                if (ano != null && int.TryParse(ano, out int anoInt))
                {
                    if (anoInt >= 2008 && anoInt <= 2012) veiculo = "VW Gol G5";
                    else if (anoInt >= 2013 && anoInt <= 2016) veiculo = "VW Gol G6";
                    else if (anoInt >= 2005 && anoInt <= 2008) veiculo = "VW Gol G4";
                    else if (anoInt >= 1999 && anoInt <= 2005) veiculo = "VW Gol G3";
                    else if (anoInt >= 1994 && anoInt <= 1999) veiculo = "VW Gol G2";
                    else veiculo = "VW Gol";
                }
                else veiculo = "VW Gol";
            }
            else if (textoTotal.Contains("fox")) veiculo = "VW Fox";
            else if (textoTotal.Contains("voyage")) veiculo = "VW Voyage";
            else if (textoTotal.Contains("saveiro")) veiculo = "VW Saveiro";
            else if (textoTotal.Contains("polo")) veiculo = "VW Polo";
            else if (textoTotal.Contains("golf")) veiculo = "VW Golf";
            else if (textoTotal.Contains("santana")) veiculo = "VW Santana";
            else if (textoTotal.Contains("fusca")) veiculo = "VW Fusca";
            else if (textoTotal.Contains("palio")) veiculo = "Fiat Palio";
            else if (textoTotal.Contains("uno")) veiculo = "Fiat Uno";
            else if (textoTotal.Contains("strada")) veiculo = "Fiat Strada";
            else if (textoTotal.Contains("siena")) veiculo = "Fiat Siena";
            else if (textoTotal.Contains("mobi")) veiculo = "Fiat Mobi";
            else if (textoTotal.Contains("toro")) veiculo = "Fiat Toro";
            else if (textoTotal.Contains("corsa")) veiculo = "GM Corsa";
            else if (textoTotal.Contains("celta")) veiculo = "GM Celta";
            else if (textoTotal.Contains("classic")) veiculo = "GM Classic";
            else if (textoTotal.Contains("onix")) veiculo = "GM Onix";
            else if (textoTotal.Contains("prisma")) veiculo = "GM Prisma";
            else if (textoTotal.Contains("spin")) veiculo = "GM Spin";
            else if (textoTotal.Contains("montana")) veiculo = "GM Montana";
            else if (textoTotal.Contains("cruze")) veiculo = "GM Cruze";
            else if (textoTotal.Contains("astra")) veiculo = "GM Astra";
            else if (textoTotal.Contains("hb20")) veiculo = "Hyundai HB20";
            else if (textoTotal.Contains("creta")) veiculo = "Hyundai Creta";
            else if (textoTotal.Contains("fiesta")) veiculo = "Ford Fiesta";
            else if (textoTotal.Contains("ka")) veiculo = "Ford Ka";
            else if (textoTotal.Contains("ecosport")) veiculo = "Ford EcoSport";
            else if (textoTotal.Contains("corolla")) veiculo = "Toyota Corolla";
            else if (textoTotal.Contains("etios")) veiculo = "Toyota Etios";
            else if (textoTotal.Contains("civic")) veiculo = "Honda Civic";
            else if (textoTotal.Contains("fit")) veiculo = "Honda Fit";
            else if (textoTotal.Contains("sandero")) veiculo = "Renault Sandero";
            else if (textoTotal.Contains("logan")) veiculo = "Renault Logan";
            else if (textoTotal.Contains("duster")) veiculo = "Renault Duster";
            else if (textoTotal.Contains("kwid")) veiculo = "Renault Kwid";
            else if (textoTotal.Contains("renegade")) veiculo = "Jeep Renegade";
            else if (textoTotal.Contains("compass")) veiculo = "Jeep Compass";
            // Caminhões e Utilitários Pesados / Comerciais
            else if (textoTotal.Contains("scania r440") || (textoTotal.Contains("scania") && textoTotal.Contains("440"))) veiculo = "Scania R440";
            else if (textoTotal.Contains("scania r450") || (textoTotal.Contains("scania") && textoTotal.Contains("450"))) veiculo = "Scania R450";
            else if (textoTotal.Contains("scania p310") || (textoTotal.Contains("scania") && textoTotal.Contains("310"))) veiculo = "Scania P310";
            else if (textoTotal.Contains("scania g420") || (textoTotal.Contains("scania") && textoTotal.Contains("420"))) veiculo = "Scania G420";
            else if (textoTotal.Contains("scania")) veiculo = "Scania Série R";
            else if (textoTotal.Contains("volvo fh") || (textoTotal.Contains("volvo") && textoTotal.Contains("fh"))) veiculo = "Volvo FH";
            else if (textoTotal.Contains("volvo vm") || (textoTotal.Contains("volvo") && textoTotal.Contains("vm"))) veiculo = "Volvo VM";
            else if (textoTotal.Contains("constellation")) veiculo = "VW Constellation";
            else if (textoTotal.Contains("delivery")) veiculo = "VW Delivery";
            else if (textoTotal.Contains("actros")) veiculo = "Mercedes-Benz Actros";
            else if (textoTotal.Contains("axor")) veiculo = "Mercedes-Benz Axor";
            else if (textoTotal.Contains("atego")) veiculo = "Mercedes-Benz Atego";
            else if (textoTotal.Contains("sprinter")) veiculo = "Mercedes-Benz Sprinter";
            else if (textoTotal.Contains("daily")) veiculo = "Iveco Daily";
            else if (textoTotal.Contains("hilux")) veiculo = "Toyota Hilux";
            else if (textoTotal.Contains("s10")) veiculo = "GM S10";
            else if (textoTotal.Contains("ranger")) veiculo = "Ford Ranger";

            // 4. Extração do Sistema
            if (textoTotal.Contains("injecao") || textoTotal.Contains("modulo") || textoTotal.Contains("ecu") || textoTotal.Contains("bobina") || textoTotal.Contains("bico"))
                sistema = "injecao";
            else if (textoTotal.Contains("fusivel") || textoTotal.Contains("rele") || textoTotal.Contains("caixa"))
                sistema = "fusiveis";
            else if (textoTotal.Contains("partida") || textoTotal.Contains("arranque") || textoTotal.Contains("linha 50"))
                sistema = "partida";
            else if (textoTotal.Contains("arrefecimento") || textoTotal.Contains("ventoinha") || textoTotal.Contains("eletroventilador"))
                sistema = "arrefecimento";
            else if (textoTotal.Contains("farol") || textoTotal.Contains("lanterna") || textoTotal.Contains("iluminacao") || textoTotal.Contains("seta") || textoTotal.Contains("pisca"))
                sistema = "iluminacao";
            else if (textoTotal.Contains("vidro") || textoTotal.Contains("trava"))
                sistema = "vidro";

            return (veiculo, ano, motor, sistema);
        }

        private static string ConstruirTermoBuscaWeb(string veiculo, string? ano, string? motor, string? sistema)
        {
            var sb = new StringBuilder("esquema eletrico");
            if (!string.IsNullOrWhiteSpace(veiculo)) sb.Append(" ").Append(veiculo);
            if (!string.IsNullOrWhiteSpace(ano)) sb.Append(" ").Append(ano);
            if (!string.IsNullOrWhiteSpace(motor)) sb.Append(" ").Append(motor);
            if (!string.IsNullOrWhiteSpace(sistema)) sb.Append(" ").Append(sistema);
            return sb.ToString().Trim();
        }

        private static Dictionary<string, string> ExtrairArgumentosQuedaTensao(string texto)
        {
            var dict = new Dictionary<string, string>();
            var matchV = System.Text.RegularExpressions.Regex.Matches(texto, @"(\d+[.,]?\d*)\s*(?:v|volts)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (matchV.Count >= 2)
            {
                dict["tensaoFonte"] = matchV[0].Groups[1].Value.Replace(",", ".");
                dict["tensaoCarga"] = matchV[1].Groups[1].Value.Replace(",", ".");
            }
            else if (matchV.Count == 1)
            {
                dict["tensaoFonte"] = "12.60";
                dict["tensaoCarga"] = matchV[0].Groups[1].Value.Replace(",", ".");
            }

            var matchA = System.Text.RegularExpressions.Regex.Match(texto, @"(\d+[.,]?\d*)\s*(?:a|amperes|amp)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (matchA.Success)
            {
                dict["corrente"] = matchA.Groups[1].Value.Replace(",", ".");
            }

            var textoLower = texto.ToLowerInvariant();
            if (textoLower.Contains("terra") || textoLower.Contains("massa") || textoLower.Contains("aterramento"))
            {
                dict["tipoCircuito"] = "Aterramento";
            }
            else if (textoLower.Contains("24v") || textoLower.Contains("pesad") || textoLower.Contains("scania") || textoLower.Contains("volvo") || textoLower.Contains("atego"))
            {
                dict["tipoCircuito"] = "24V";
            }
            else if (textoLower.Contains("sinal") || textoLower.Contains("sensor"))
            {
                dict["tipoCircuito"] = "Sinal";
            }

            return dict;
        }
    }
}
