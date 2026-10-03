using PrimoAutoEletrica.Models.AI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public sealed class GeminiAIService : IAIService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        private readonly DeterministicFallbackAIService _fallbackService;
        private readonly AIToolRegistry _toolRegistry;
        private readonly AutomotiveDiagnosticRAGService _ragService;
        private string _apiKey;
        private string _modelName;

        public string ProviderName => string.IsNullOrWhiteSpace(_apiKey) ? _fallbackService.ProviderName : "Google Gemini 2.5 Flash";
        public bool IsOnlineAvailable => !string.IsNullOrWhiteSpace(_apiKey);

        public GeminiAIService(
            AIToolRegistry toolRegistry,
            DeterministicFallbackAIService fallbackService,
            AutomotiveDiagnosticRAGService? ragService = null,
            string? apiKey = null,
            string modelName = "gemini-2.0-flash")
        {
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
            _ragService = ragService ?? new AutomotiveDiagnosticRAGService();
            _modelName = string.IsNullOrWhiteSpace(modelName) ? "gemini-2.0-flash" : modelName;

            // Prioridade: argumento direto > variável de ambiente > vazio
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
        }

        public void DefinirApiKey(string apiKey)
        {
            _apiKey = apiKey?.Trim() ?? string.Empty;
        }

        public async Task<AIChatResponse> ProcessarMensagemAsync(AIChatRequest request, CancellationToken cancellationToken = default)
        {
            // Se offline forçado ou sem chave de API, vai direto para o motor especialista determinístico local
            if (request.ForceOffline || string.IsNullOrWhiteSpace(_apiKey))
            {
                return await _fallbackService.ProcessarMensagemAsync(request, cancellationToken);
            }

            try
            {
                var ultimaMensagem = request.Messages.Count > 0
                    ? request.Messages[request.Messages.Count - 1].Content
                    : string.Empty;

                // Enriquecimento com RAG Técnico local antes de enviar para a nuvem
                var contextoTecnicoRAG = _ragService.MontarContextoTecnico(ultimaMensagem);

                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

                var systemPrompt = @"Você é o PRIMOX Copilot, um especialista sênior em Auto Elétrica Automotiva e assistente operacional integrado ao ERP PRIMOX Workshop.
Suas responsabilidades:
1. Auxiliar eletricistas e técnicos automotivos com diagnósticos de defeitos elétricos, sistemas de injeção, redes CAN, teste de ripple em alternadores, teste de queda de tensão (voltage drop), consumo de corrente em repouso (fuga de carga) e esquemas de relés.
2. Seja objetivo, técnico, preciso e prático como quem está trabalhando na bancada da oficina mecânica. Dê valores nominais (Volts, Amperes, Ohms, milissegundos).
3. Quando o usuário perguntar sobre funções do sistema (estoque, compras, ferramentas, clientes, ordens de serviço), oriente-o de forma clara e indique qual tela ele deve acessar no PRIMOX.
Formate suas respostas em Markdown estruturado, com tópicos e negrito.";

                if (!string.IsNullOrWhiteSpace(contextoTecnicoRAG))
                {
                    systemPrompt += $"\n\nBase técnica complementar do veículo/procedimento:\n{contextoTecnicoRAG}";
                }

                var contentsList = new List<object>();

                // Montar histórico de conversação
                foreach (var msg in request.Messages)
                {
                    contentsList.Add(new
                    {
                        role = msg.Role == AIRole.User ? "user" : "model",
                        parts = new[] { new { text = msg.Content } }
                    });
                }

                var payload = new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = contentsList,
                    generationConfig = new
                    {
                        temperature = 0.4,
                        maxOutputTokens = 1200
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                using var requestContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync(endpoint, requestContent, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    // Em caso de cota excedida ou erro de rede da nuvem, fallback transparente
                    var fallbackRes = await _fallbackService.ProcessarMensagemAsync(request, cancellationToken);
                    fallbackRes.ProviderUsed = $"{_fallbackService.ProviderName} (Nuvem indisponível)";
                    return fallbackRes;
                }

                var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseBody);

                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? string.Empty;

                    var resp = new AIChatResponse
                    {
                        Message = text,
                        ProviderUsed = ProviderName,
                        Success = true
                    };

                    // Extrair ações sugeridas contextuais
                    if (text.Contains("Estoque", StringComparison.OrdinalIgnoreCase))
                    {
                        resp.SuggestedActions.Add(new AISuggestedAction { Label = "Ir para Estoque", ActionType = "Navigate", Parameter = "Estoque" });
                    }
                    if (text.Contains("Ferramenta", StringComparison.OrdinalIgnoreCase))
                    {
                        resp.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Ferramentaria", ActionType = "Navigate", Parameter = "Ferramentas" });
                    }
                    if (text.Contains("Compras", StringComparison.OrdinalIgnoreCase))
                    {
                        resp.SuggestedActions.Add(new AISuggestedAction { Label = "Ver Falta & Compras", ActionType = "Navigate", Parameter = "ComprasNecessidade" });
                    }
                    if (text.Contains("Auto Elétrica", StringComparison.OrdinalIgnoreCase) || text.Contains("Técnica", StringComparison.OrdinalIgnoreCase))
                    {
                        resp.SuggestedActions.Add(new AISuggestedAction { Label = "Módulo Técnico", ActionType = "Navigate", Parameter = "AutoEletricaTecnica" });
                    }

                    return resp;
                }

                return await _fallbackService.ProcessarMensagemAsync(request, cancellationToken);
            }
            catch
            {
                // Fallback resiliente imediato
                var fallbackRes = await _fallbackService.ProcessarMensagemAsync(request, cancellationToken);
                fallbackRes.ProviderUsed = $"{_fallbackService.ProviderName} (Modo Offline Ativado)";
                return fallbackRes;
            }
        }
    }
}
