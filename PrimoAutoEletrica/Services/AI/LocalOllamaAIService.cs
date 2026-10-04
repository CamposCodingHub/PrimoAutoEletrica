using PrimoAutoEletrica.Models.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public sealed class LocalOllamaAIService : IAIService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private readonly DeterministicFallbackAIService _fallbackService;
        private readonly AutomotiveDiagnosticRAGService _ragService;
        private readonly AutomotiveWebSearchService _webSearchService;
        private readonly AIToolRegistry _toolRegistry;
        private string _baseUrl;
        private string _modelName;
        private bool _enabled = true;

        public string BaseUrl => _baseUrl;
        public string ModelName => _modelName;
        public bool Enabled => _enabled;

        public string ProviderName => _enabled ? $"PRIMOX IA Local Offline ({_modelName})" : _fallbackService.ProviderName;
        public bool IsOnlineAvailable => _enabled;

        public LocalOllamaAIService(
            DeterministicFallbackAIService fallbackService,
            AIToolRegistry toolRegistry,
            AutomotiveDiagnosticRAGService? ragService = null,
            string baseUrl = "http://127.0.0.1:11434",
            string modelName = "llama3.2:3b",
            AutomotiveWebSearchService? webSearchService = null)
        {
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _ragService = ragService ?? new AutomotiveDiagnosticRAGService();
            _webSearchService = webSearchService ?? new AutomotiveWebSearchService();
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "http://127.0.0.1:11434" : baseUrl.TrimEnd('/');
            _modelName = string.IsNullOrWhiteSpace(modelName) ? "llama3.2:3b" : modelName.Trim();
        }

        public void DefinirModelo(string modelName)
        {
            if (!string.IsNullOrWhiteSpace(modelName)) _modelName = modelName.Trim();
        }

        public void DefinirBaseUrl(string baseUrl)
        {
            if (!string.IsNullOrWhiteSpace(baseUrl)) _baseUrl = baseUrl.TrimEnd('/');
        }

        public void DefinirHabilitado(bool habilitado)
        {
            _enabled = habilitado;
        }

        public async Task<(bool Sucesso, string Mensagem, long LatenciaMs)> TestarConexaoAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cts.Token);
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    bool temModelo = content.Contains(_modelName);
                    string detalhe = temModelo 
                        ? $"Servidor Ollama ativo e modelo '{_modelName}' pronto para inferência local!"
                        : $"Servidor Ollama ativo, porém modelo '{_modelName}' não foi encontrado na lista.";
                    return (true, $"{detalhe} (Latência: {sw.ElapsedMilliseconds} ms)", sw.ElapsedMilliseconds);
                }

                return (false, $"Ollama respondeu com status {(int)response.StatusCode} ({response.ReasonPhrase})", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, $"Não foi possível conectar ao servidor Ollama local ({_baseUrl}): {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        public async Task<AIChatResponse> ProcessarMensagemAsync(AIChatRequest request, CancellationToken cancellationToken = default)
        {
            if (!_enabled || request.ForceOffline)
            {
                return await _fallbackService.ProcessarMensagemAsync(request, cancellationToken);
            }

            var todasMensagens = request.Messages ?? new List<AIChatMessage>();
            var ultimaMensagemUser = todasMensagens.LastOrDefault(m => m.Role == AIRole.User)?.Content ?? string.Empty;

            // Se for saudação simples, o motor nativo responde instantaneamente sem latência de LLM
            var textoLimpo = AutomotiveDiagnosticRAGService.NormalizarTexto(ultimaMensagemUser);
            if (textoLimpo.Equals("ola") || textoLimpo.Equals("oi") || textoLimpo.Equals("boa noite") || textoLimpo.Equals("bom dia") || textoLimpo.Equals("boa tarde"))
            {
                return await _fallbackService.ProcessarMensagemAsync(request, cancellationToken);
            }

            // Se for solicitação de esquema elétrico, diagrama ou mapa/tabela de fusíveis e relés,
            // processar com o motor técnico especializado, que consulta centrais locais e web:
            if (DeterministicFallbackAIService.EhConsultaDeEsquemaOuDiagrama(textoLimpo, ultimaMensagemUser)
                || DeterministicFallbackAIService.EhConsultaDeFusiveisOuReles(textoLimpo, out _))
            {
                return await _fallbackService.ProcessarMensagemAsync(request, cancellationToken);
            }

            // Identificar se algum veículo foi citado no histórico
            string? veiculoDetectado = ExtrairVeiculo(todasMensagens);

            // Injetar RAG Automotivo e Manuais do Sistema
            var contextoTecnicoRAG = _ragService.MontarContextoTecnico(ultimaMensagemUser);

            // Realizar busca técnica em tempo real na internet (Web Search) para esquemas e defeitos
            AutomotiveWebSearchResult? resultadoWeb = null;
            try
            {
                using var ctsWeb = new CancellationTokenSource(TimeSpan.FromSeconds(3.5));
                resultadoWeb = await _webSearchService.PesquisarAsync(ultimaMensagemUser, veiculoDetectado, ctsWeb.Token);
            }
            catch { }

            if (resultadoWeb != null && resultadoWeb.Sucesso && !string.IsNullOrWhiteSpace(resultadoWeb.ContextoFormatadoMarkdown))
            {
                contextoTecnicoRAG += $"\n\n{resultadoWeb.ContextoFormatadoMarkdown}";
            }

            try
            {
                var payload = ConstruirPayloadChat(todasMensagens, contextoTecnicoRAG);
                var jsonContent = JsonSerializer.Serialize(payload);

                using var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(50)); // timeout seguro para inferência local no CPU

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", requestContent, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(responseJson);

                    if (doc.RootElement.TryGetProperty("message", out var msgElem) &&
                        msgElem.TryGetProperty("content", out var contentElem))
                    {
                        var generatedText = contentElem.GetString();
                        if (!string.IsNullOrWhiteSpace(generatedText))
                        {
                            var chatResponse = new AIChatResponse
                            {
                                Success = true,
                                Message = generatedText.Trim(),
                                ProviderUsed = ProviderName
                            };

                            if (resultadoWeb != null && resultadoWeb.Sucesso && resultadoWeb.Resultados.Count > 0 && !chatResponse.Message.Contains("🌐"))
                            {
                                var primeiro = resultadoWeb.Resultados.First();
                                chatResponse.Message += $"\n\n🌐 *Pesquisa Técnica Web: Validação com diagramas elétricos e base técnica online ({primeiro.Dominio})*";
                            }

                            // Adicionar ações e chips sugeridos
                            EnriquecerSugestoes(chatResponse, ultimaMensagemUser);
                            return chatResponse;
                        }
                    }
                }
            }
            catch
            {
                // Em caso de falha de conexão, GPU ocupada ou timeout, degrada silenciosamente para o motor especialista determinístico
            }

            return await _fallbackService.ProcessarMensagemAsync(request, cancellationToken);
        }

        private object ConstruirPayloadChat(List<AIChatMessage> historico, string contextoRAG)
        {
            var systemPrompt = new StringBuilder();
            systemPrompt.AppendLine("Você é o PRIMOX Copilot, um especialista sênior em Auto Elétrica Automotiva, Injeção Eletrônica e copiloto operacional do software PRIMOX.");
            systemPrompt.AppendLine("DIRETRIZES MANDATÓRIAS DE ATENDIMENTO DE OFICINA:");
            systemPrompt.AppendLine("1. NUNCA CHUTE SISTEMAS NEM VEÍCULOS NÃO CITADOS: Se o usuário relatou um sintoma geral, NUNCA presuma sistemas como Start-Stop, Jeep Renegade, Fiat Toro, Ford Smart Charge ou outro sistema específico.");
            systemPrompt.AppendLine("2. EXIGÊNCIA MANDATÓRIA DE MARCA, MODELO E ANO: Se o usuário ainda não informou a MARCA, MODELO e ANO do carro, sua PRIMEIRA OBRIGAÇÃO é perguntar claramente qual é o veículo (explicando que a arquitetura elétrica varia drasticamente entre veículos antigos, intermediários e modernos).");
            systemPrompt.AppendLine("3. ARQUITETURAS ELÉTRICAS REAIS (ATENÇÃO ESPECIAL A RELÉS E MÓDULOS):");
            systemPrompt.AppendLine("   • Carros Antigos / Linha Tradicional (ex: Fusca, Gol G2/G3/G4, Uno Mille, Corsa B, Santana): Circuitos como o ESGUICHO/BOMBINHA DO PARA-BRISA NÃO POSSUEM RELÉ — a ligação é DIRETA da alavanca na coluna até o conector da bombinha de água (linha 15 protegida por fusível). Se não vai sinal na bombinha, o defeito crônico é a lâmina interna de cobre da chave de seta gasta/aberta ou fusível queimado. NUNCA diga que o defeito é relé sem saber se o veículo possui relé!");
            systemPrompt.AppendLine("   • Carros Intermediários (ex: Astra, Golf, Santana, Gol G5): Usam relés temporizadores do limpador/lavador.");
            systemPrompt.AppendLine("   • Carros Modernos (ex: Onix, HB20, Renegade, Compass, Polo TSI): Utilizam módulo de carroceria BCM com sinal multiplexado/resistivo e relé de reversão para bomba bidirecional.");
            systemPrompt.AppendLine("4. VOCABULÁRIO TÉCNICO PROFISSIONAL: NUNCA invente termos inexistentes como 'válvula de jato de água' ou 'relé de jato de água'. O componente automotivo correto é a 'Eletrobomba do Lavador de Para-brisa (Bombinha 12V)' e os bicos são 'Brucutus/Ejetores'. Padrão DIN: pino 31 é terra/massa, pino 15 é pós-chave, pino 30 é positivo direto da bateria.");
            systemPrompt.AppendLine("5. PESQUISA TÉCNICA WEB EM TEMPO REAL: Utilize os dados obtidos da pesquisa web por diagramas e fóruns técnicos anexados no contexto para embasar suas respostas.");
            systemPrompt.AppendLine("6. ROTEIRO PRÁTICO DE BANCADA: Indique os primeiros testes com multímetro/lâmpada de teste de forma objetiva de eletricista para eletricista.");
            systemPrompt.AppendLine("7. Idioma: Português do Brasil claro, técnico e direto.");
            systemPrompt.AppendLine("8. PRECISÃO ELÉTRICA RIGOROSA (SEM ALUCINAÇÕES):");
            systemPrompt.AppendLine("   • Fusíveis NÃO possuem ajuste de tensão ou regulagem. Fusíveis são proteções com corrente nominal fixa (10A, 15A, 20A, 30A, 40A) que apenas conduzem ou rompem por sobrecorrente.");
            systemPrompt.AppendLine("   • Motor de arranque que faz 'tec-tec' e não vira NUNCA é causado por pressão de bomba de combustível! O defeito é 100% elétrico: queda de tensão na bateria (< 9.6V na partida), bornes soltos, massa do bloco deficiente, automático com bobina aberta ou escovas do arranque gastas.");
            systemPrompt.AppendLine("   • Se o farol não desliga e apaga ao retirar o relé, explique que os contatos internos 30 e 87 do relé soldaram por arco elétrico e advirta sobre lâmpadas paralelas de 100W fora da especificação original (55/60W).");
            systemPrompt.AppendLine("   • Se múltiplos equipamentos da tampa traseira (desembaçador, terceira luz de freio/brake light, limpador) param juntos, aponte a coifa sanfonada da tampa do porta-malas como causa nº 1 (fios partidos por fadiga mecânica).");

            if (!string.IsNullOrWhiteSpace(contextoRAG))
            {
                systemPrompt.AppendLine("\n[BASE DE CONHECIMENTO TÉCNICA ESPECIALIZADA DA OFICINA]:");
                systemPrompt.AppendLine(contextoRAG);
            }

            var messagesList = new List<object>
            {
                new { role = "system", content = systemPrompt.ToString() }
            };

            // Inclui até as últimas 8 mensagens para manter memória de contexto sem estourar janela
            var ultimas = historico.TakeLast(8);
            foreach (var m in ultimas)
            {
                messagesList.Add(new
                {
                    role = m.Role == AIRole.User ? "user" : "assistant",
                    content = m.Content
                });
            }

            return new
            {
                model = _modelName,
                messages = messagesList,
                stream = false,
                options = new
                {
                    temperature = 0.3,
                    top_p = 0.9,
                    num_predict = 500
                }
            };
        }

        private void EnriquecerSugestoes(AIChatResponse response, string ultimaMensagemUser)
        {
            var diagList = _ragService.BuscarPorSintomaOuTermo(ultimaMensagemUser, 1);
            if (diagList.Count > 0)
            {
                var diag = diagList[0];
                foreach (var chip in diag.InteractiveReplyChips.Take(3))
                {
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = chip,
                        ActionType = "ExecuteQuery",
                        Parameter = chip
                    });
                }

                if (diag.SuggestedStockParts.Count > 0)
                {
                    var peca = diag.SuggestedStockParts.First();
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = $"Ver Estoque ({peca})",
                        ActionType = "SearchStock",
                        Parameter = peca
                    });
                }
            }
            else
            {
                response.SuggestedActions.Add(new AISuggestedAction
                {
                    Label = "Módulo Técnico",
                    ActionType = "Navigate",
                    Parameter = "AutoEletricaTecnica"
                });
            }
        }

        private static string? ExtrairVeiculo(List<AIChatMessage> mensagens)
        {
            var modelos = new[]
            {
                "gol g4", "gol g3", "gol g2", "gol g5", "gol g6", "gol ea111", "gol",
                "palio fire", "palio", "uno fire", "uno mille", "uno", "siena", "strada",
                "corsa vhc", "corsa", "celta", "classic", "onix spe4", "onix", "prisma",
                "hb20 1.0", "hb20", "creta", "renegade", "toro", "compass",
                "fox", "voyage", "saveiro", "parati", "santana", "fusca", "astra", "vectra",
                "ford ka", "ka", "fiesta", "ecosport", "corolla", "etios"
            };

            foreach (var m in mensagens.Where(x => x.Role == AIRole.User).Reverse())
            {
                var txt = AutomotiveDiagnosticRAGService.NormalizarTexto(m.Content);
                foreach (var mod in modelos)
                {
                    if (txt.Contains(mod)) return mod;
                }
            }
            return null;
        }
    }
}
