using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// Low-level HTTP transport for external AI. Injectable HttpMessageHandler for tests.
    /// NEVER logs Authorization / API keys.
    /// </summary>
    public interface IExternalAssistantHttpTransport
    {
        Task<ExternalAssistantRawResponse> SendAsync(
            ExternalAssistantRequestPayload payload,
            string apiKey,
            string endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);
    }

    public sealed class HttpExternalAssistantTransport : IExternalAssistantHttpTransport
    {
        private readonly HttpClient _http;

        public HttpExternalAssistantTransport(HttpMessageHandler? handler = null)
        {
            _http = handler == null
                ? new HttpClient()
                : new HttpClient(handler, disposeHandler: false);
        }

        public async Task<ExternalAssistantRawResponse> SendAsync(
            ExternalAssistantRequestPayload payload,
            string apiKey,
            string endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payload);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("API key missing — refuse network.");
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("Endpoint missing — refuse network.");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var body = BuildOpenAiCompatibleBody(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new ExternalAssistantRawResponse
                {
                    AnswerMarkdown = string.Empty,
                    HttpStatusCode = 408,
                    RawBody = "timeout"
                };
            }

            var raw = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new ExternalAssistantRawResponse
                {
                    AnswerMarkdown = string.Empty,
                    HttpStatusCode = (int)response.StatusCode,
                    RawBody = TruncateSafe(raw)
                };
            }

            return ParseResponse(raw, (int)response.StatusCode);
        }

        internal static string BuildOpenAiCompatibleBody(ExternalAssistantRequestPayload payload)
        {
            var evidenceJson = string.Join("\n", payload.Evidence.Select(e =>
                $"- EvidenceId={e.EvidenceId}; SourceType={e.SourceType}; SourceId={e.SourceId}; Title={e.Title}; Excerpt={e.Excerpt}"));

            var userContent =
                $"{payload.Instruction}\n\nQuery: {payload.Query}\n\nEvidence package (RequestId={payload.RequestId}):\n{evidenceJson}\n\n" +
                "Respond as JSON object with keys: answer, citedEvidenceIds (array of EvidenceId), suggestedActions (array), missingInformation (array), limitations (string).";

            var doc = new Dictionary<string, object?>
            {
                ["model"] = string.IsNullOrWhiteSpace(payload.Model) ? "gpt-4o-mini" : payload.Model,
                ["temperature"] = 0,
                ["messages"] = new object[]
                {
                    new Dictionary<string, string>
                    {
                        ["role"] = "system",
                        ["content"] = "You are PRIMOX External Assist. Fail-closed: only use provided evidence IDs. Never invent sources or financial/fiscal actions."
                    },
                    new Dictionary<string, string>
                    {
                        ["role"] = "user",
                        ["content"] = userContent
                    }
                }
            };
            return JsonSerializer.Serialize(doc);
        }

        internal static ExternalAssistantRawResponse ParseResponse(string raw, int statusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                // OpenAI chat.completions shape
                string? content = null;
                if (root.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array &&
                    choices.GetArrayLength() > 0)
                {
                    var msg = choices[0].GetProperty("message");
                    content = msg.GetProperty("content").GetString();
                }
                else if (root.TryGetProperty("answer", out var ans))
                {
                    content = ans.GetString();
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new ExternalAssistantRawResponse
                    {
                        AnswerMarkdown = string.Empty,
                        HttpStatusCode = statusCode,
                        RawBody = TruncateSafe(raw)
                    };
                }

                // Prefer nested JSON in content
                var trimmed = content.Trim();
                if (trimmed.StartsWith("```"))
                {
                    var start = trimmed.IndexOf('{');
                    var end = trimmed.LastIndexOf('}');
                    if (start >= 0 && end > start)
                        trimmed = trimmed.Substring(start, end - start + 1);
                }

                if (trimmed.StartsWith("{"))
                {
                    using var inner = JsonDocument.Parse(trimmed);
                    var iroot = inner.RootElement;
                    return new ExternalAssistantRawResponse
                    {
                        AnswerMarkdown = iroot.TryGetProperty("answer", out var a) ? (a.GetString() ?? string.Empty) : trimmed,
                        CitedEvidenceIds = ReadStringArray(iroot, "citedEvidenceIds"),
                        SuggestedActions = ReadStringArray(iroot, "suggestedActions"),
                        MissingInformation = ReadStringArray(iroot, "missingInformation"),
                        Limitations = iroot.TryGetProperty("limitations", out var lim) ? lim.GetString() : null,
                        HttpStatusCode = statusCode,
                        RawBody = TruncateSafe(raw)
                    };
                }

                return new ExternalAssistantRawResponse
                {
                    AnswerMarkdown = content,
                    CitedEvidenceIds = Array.Empty<string>(),
                    SuggestedActions = Array.Empty<string>(),
                    MissingInformation = Array.Empty<string>(),
                    HttpStatusCode = statusCode,
                    RawBody = TruncateSafe(raw)
                };
            }
            catch (JsonException)
            {
                return new ExternalAssistantRawResponse
                {
                    AnswerMarkdown = string.Empty,
                    HttpStatusCode = statusCode,
                    RawBody = TruncateSafe(raw)
                };
            }
        }

        private static IReadOnlyList<string> ReadStringArray(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
                return Array.Empty<string>();
            return el.EnumerateArray()
                .Select(x => x.GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private static string TruncateSafe(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            // Never echo bearer tokens if somehow present
            if (raw.Contains("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("sk-", StringComparison.OrdinalIgnoreCase))
            {
                return "[REDACTED]";
            }
            return raw.Length > 4000 ? raw.Substring(0, 4000) + "…" : raw;
        }
    }
}