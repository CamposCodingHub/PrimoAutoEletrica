using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Intelligence
{
    /// <summary>C6.6 — generic local model provider options (OpenAI-compatible endpoint).</summary>
    public sealed class LocalModelOptions
    {
        public string Endpoint { get; init; } = "http://127.0.0.1:11434/v1/chat/completions";
        public string ModelId { get; init; } = "local-model-generic";
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
        public int MaxTokens { get; init; } = 512;
        public double Temperature { get; init; } = 0.1;
        public int ContextLimit { get; init; } = 4096;
        public int Concurrency { get; init; } = 1;
        /// <summary>When false, provider reports not configured (ENVIRONMENT_DEPENDENCY).</summary>
        public bool RuntimeAvailable { get; init; }
    }

    /// <summary>
    /// C6.6 LocalModelAssistantProvider — generic. If no local runtime: IsConfigured=false; do not invent results.
    /// </summary>
    public sealed class LocalModelAssistantProvider : IAssistantProvider
    {
        private readonly LocalModelOptions _options;
        private readonly HttpMessageHandler? _handler;
        private readonly SemaphoreSlim _gate;

        public LocalModelAssistantProvider(LocalModelOptions? options = null, HttpMessageHandler? handler = null)
        {
            _options = options ?? new LocalModelOptions();
            _handler = handler;
            _gate = new SemaphoreSlim(Math.Max(1, _options.Concurrency), Math.Max(1, _options.Concurrency));
        }

        public string ProviderId => "PRIMOX_LOCAL_MODEL";
        public string DisplayName => "PRIMOX Local Model (generic OpenAI-compatible)";
        public bool IsConfigured => _options.RuntimeAvailable;
        public LocalModelOptions Options => _options;
        public string EnvironmentDependencyReason =>
            _options.RuntimeAvailable ? string.Empty : "ENVIRONMENT_DEPENDENCY: local model runtime not available/verified";

        public async Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (!_options.RuntimeAvailable)
            {
                return new AssistantResponse
                {
                    AnswerMarkdown = "### Local model unavailable\n\nENVIRONMENT_DEPENDENCY — local runtime not configured. No invented result.",
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Warnings = new[] { "LOCAL_MODEL_ENVIRONMENT_DEPENDENCY" },
                    MissingInformation = new[] { "Local model runtime (Endpoint/ModelId)" },
                    Provider = ProviderId,
                    CorrelationId = context.CorrelationId,
                    Timestamp = DateTimeOffset.UtcNow,
                    RecommendedActions = new[] { "Install/configure local runtime or continue with GroundedLocalRule / MOCK_ONLY" }
                };
            }

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var client = _handler == null ? new HttpClient() : new HttpClient(_handler, disposeHandler: false);
                client.Timeout = _options.Timeout;
                var payload = new
                {
                    model = _options.ModelId,
                    temperature = _options.Temperature,
                    max_tokens = _options.MaxTokens,
                    messages = new object[]
                    {
                        new { role = "system", content = "You are PRIMOX Assist — consultive only. Never buy, mutate stock/OS/finance. Do not invent evidence." },
                        new { role = "user", content = Truncate(context.Query, _options.ContextLimit) }
                    }
                };
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await client.PostAsync(_options.Endpoint, content, cancellationToken).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    return new AssistantResponse
                    {
                        AnswerMarkdown = "Local model HTTP failure — no invented answer.",
                        ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                        Warnings = new[] { "LOCAL_MODEL_HTTP_" + (int)resp.StatusCode },
                        Provider = ProviderId,
                        CorrelationId = context.CorrelationId,
                        Timestamp = DateTimeOffset.UtcNow
                    };
                }

                string answer = ExtractContent(body);
                return new AssistantResponse
                {
                    AnswerMarkdown = string.IsNullOrWhiteSpace(answer) ? "(empty local model content)" : answer,
                    ConfidenceLevel = AssistantConfidenceLevel.LOW,
                    Provider = ProviderId,
                    CorrelationId = context.CorrelationId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Warnings = new[] { "LOCAL_MODEL_RAW — grounding not applied in C6.6 prototype" }
                };
            }
            finally
            {
                _gate.Release();
            }
        }

        private static string ExtractContent(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
                {
                    var msg = choices[0].GetProperty("message");
                    if (msg.TryGetProperty("content", out var c))
                        return c.GetString() ?? string.Empty;
                }
            }
            catch { /* malformed */ }
            return string.Empty;
        }

        private static string Truncate(string s, int contextLimitApproxChars)
        {
            // Rough char budget from token context limit (not a tokenizer).
            var maxChars = Math.Max(256, contextLimitApproxChars * 3);
            if (string.IsNullOrEmpty(s) || s.Length <= maxChars) return s ?? string.Empty;
            return s.Substring(0, maxChars);
        }
    }
}