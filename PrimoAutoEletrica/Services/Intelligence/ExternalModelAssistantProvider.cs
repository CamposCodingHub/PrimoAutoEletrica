using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.ExternalAi;

namespace PrimoAutoEletrica.Services.Intelligence
{
    public sealed class ExternalModelAdapterOptions
    {
        public string Endpoint { get; init; } = ExternalAssistantOptions.DefaultBaseUrl;
        public string ModelId { get; init; } = "gpt-4o-mini";
        public string ApiKeyEnvironmentVariable { get; init; } = ExternalAssistantOptions.DefaultApiKeyEnvironmentVariable;
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
        public int MaxTokens { get; init; } = 512;
        public double Temperature { get; init; } = 0.1;
        public bool ForceMock { get; init; }
        public string? MockResponseMarkdown { get; init; }
    }

    /// <summary>
    /// C6.7 — External adapter (OpenAI-compatible first, experimental).
    /// Contract/mock without key. LIVE only if secure key present. Never logs secrets.
    /// </summary>
    public sealed class ExternalModelAssistantProvider : IAssistantProvider
    {
        private readonly ExternalModelAdapterOptions _options;
        private readonly IExternalAssistantSecretSource _secrets;
        private readonly HttpMessageHandler? _handler;

        public ExternalModelAssistantProvider(
            ExternalModelAdapterOptions? options = null,
            IExternalAssistantSecretSource? secrets = null,
            HttpMessageHandler? handler = null)
        {
            _options = options ?? new ExternalModelAdapterOptions();
            _secrets = secrets ?? new EnvironmentExternalAssistantSecretSource();
            _handler = handler;
        }

        public string ProviderId => "PRIMOX_EXTERNAL_MODEL_EXPERIMENTAL";
        public string DisplayName => "PRIMOX External Model Adapter (experimental OpenAI-compatible)";
        public string? ModelId => _options.ModelId;

        public bool IsConfigured =>
            _options.ForceMock ||
            !string.IsNullOrWhiteSpace(_secrets.TryGetApiKey(_options.ApiKeyEnvironmentVariable));

        public string LiveStatus =>
            _options.ForceMock ? "MOCK_ONLY"
            : string.IsNullOrWhiteSpace(_secrets.TryGetApiKey(_options.ApiKeyEnvironmentVariable))
                ? "LIVE_NOT_TESTED"
                : "LIVE_ARMED";

        public async Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (_options.ForceMock)
            {
                return new AssistantResponse
                {
                    AnswerMarkdown = _options.MockResponseMarkdown
                        ?? "### MOCK external response\n\nMOCK_ONLY — no LIVE call. Assistive suggestion only.",
                    ConfidenceLevel = AssistantConfidenceLevel.LOW,
                    Provider = ProviderId,
                    CorrelationId = context.CorrelationId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Warnings = new[] { "EXTERNAL_MOCK_ONLY" },
                    RecommendedActions = new[] { "Validate physically; do not treat mock as LIVE quality" }
                };
            }

            var key = _secrets.TryGetApiKey(_options.ApiKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(key))
            {
                return new AssistantResponse
                {
                    AnswerMarkdown = "### External model LIVE_NOT_TESTED\n\nAPI key absent. No remote call. No invented LIVE result.",
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Provider = ProviderId,
                    CorrelationId = context.CorrelationId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Warnings = new[] { ExternalAssistantWarnings.NoKey, "LIVE_NOT_TESTED" },
                    MissingInformation = new[] { "Secure API key in environment (never paste in chat)" }
                };
            }

            using var client = _handler == null ? new HttpClient() : new HttpClient(_handler, disposeHandler: false);
            client.Timeout = _options.Timeout;
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + key);

            var payload = new
            {
                model = _options.ModelId,
                temperature = _options.Temperature,
                max_tokens = _options.MaxTokens,
                messages = new object[]
                {
                    new { role = "system", content = "PRIMOX Assist experimental. Suggest only. Never buy/stock/OS/finance mutations. Do not invent evidence." },
                    new { role = "user", content = context.Query }
                }
            };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(_options.Endpoint, content, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                return new AssistantResponse
                {
                    AnswerMarkdown = "External LIVE HTTP failure — no invented answer.",
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    Warnings = new[] { "EXTERNAL_HTTP_" + (int)resp.StatusCode },
                    Provider = ProviderId,
                    CorrelationId = context.CorrelationId,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            string answer = string.Empty;
            int? promptTokens = null, completionTokens = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var c))
                    answer = c.GetString() ?? string.Empty;
                if (doc.RootElement.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var pt)) promptTokens = pt.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var ctTok)) completionTokens = ctTok.GetInt32();
                }
            }
            catch { /* malformed */ }

            return new AssistantResponse
            {
                AnswerMarkdown = string.IsNullOrWhiteSpace(answer) ? "(empty)" : answer,
                ConfidenceLevel = AssistantConfidenceLevel.LOW,
                Provider = ProviderId,
                CorrelationId = context.CorrelationId,
                Timestamp = DateTimeOffset.UtcNow,
                Warnings = new[] { "EXTERNAL_LIVE_EXPERIMENTAL" },
                RecommendedActions = new[]
                {
                    "Validate physically before any workshop action",
                    promptTokens is null
                        ? "tokens=NOT_MEASURED"
                        : $"prompt_tokens={promptTokens};completion_tokens={completionTokens}"
                }
            };
        }
    }
}