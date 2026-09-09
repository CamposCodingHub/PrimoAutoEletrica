using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Transporte HTTP Focus NFe — somente Homologação.
    /// Produção: nunca chamada.
    /// </summary>
    public sealed class FocusNfeHttpClient
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public FocusNfeHttpClient(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient { Timeout = DefaultTimeout };
            if (_httpClient.Timeout == Timeout.InfiniteTimeSpan || _httpClient.Timeout <= TimeSpan.Zero)
            {
                _httpClient.Timeout = DefaultTimeout;
            }
        }

        public async Task<FocusNfeHttpResponse> PostNfeAsync(
            string baseUrl,
            string token,
            string reference,
            object payload,
            CancellationToken cancellationToken)
        {
            EnsureHomologUrl(baseUrl);
            var url = Combine(baseUrl, "/v2/nfe") + "?ref=" + Uri.EscapeDataString(reference);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            ApplyAuth(request, token);
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<FocusNfeHttpResponse> GetNfeAsync(
            string baseUrl,
            string token,
            string reference,
            CancellationToken cancellationToken)
        {
            EnsureHomologUrl(baseUrl);
            var url = Combine(baseUrl, "/v2/nfe/" + Uri.EscapeDataString(reference));
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyAuth(request, token);
            return await SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<FocusNfeHttpResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new FocusNfeHttpResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Body = body,
                    IsSuccessStatusCode = response.IsSuccessStatusCode
                };
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return FocusNfeHttpResponse.Timeout();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return FocusNfeHttpResponse.Timeout();
            }
            catch (HttpRequestException ex)
            {
                return FocusNfeHttpResponse.Network(ex.Message);
            }
        }

        private static void ApplyAuth(HttpRequestMessage request, string token)
        {
            var bytes = Encoding.ASCII.GetBytes(token + ":");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }

        private static void EnsureHomologUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("URL Focus vazia.");
            }

            if (FiscalDocumentValidator.IsProductionFocusUrl(baseUrl))
            {
                throw new FiscalProductionBlockedException(
                    "URL de PRODUCAO Focus bloqueada. Use apenas homologacao.focusnfe.com.br.");
            }
        }

        private static string Combine(string baseUrl, string path)
        {
            return baseUrl.TrimEnd('/') + path;
        }
    }

    public sealed class FocusNfeHttpResponse
    {
        public int StatusCode { get; init; }
        public string Body { get; init; } = string.Empty;
        public bool IsSuccessStatusCode { get; init; }
        public bool IsTimeout { get; init; }
        public bool IsNetworkError { get; init; }
        public string? NetworkMessage { get; init; }

        public static FocusNfeHttpResponse Timeout() => new()
        {
            StatusCode = 0,
            IsTimeout = true,
            Body = string.Empty
        };

        public static FocusNfeHttpResponse Network(string message) => new()
        {
            StatusCode = 0,
            IsNetworkError = true,
            NetworkMessage = message,
            Body = string.Empty
        };
    }

    public static class FocusNfePayloadBuilder
    {
        public static Dictionary<string, object?> Build(FiscalNFeDocument document)
        {
            var emitente = document.Emitente;
            var dest = document.Destinatario;
            var items = new List<Dictionary<string, object?>>();
            foreach (var item in document.Itens)
            {
                var row = new Dictionary<string, object?>
                {
                    ["numero_item"] = item.NumeroItem,
                    ["codigo_produto"] = item.Codigo,
                    ["descricao"] = item.Descricao,
                    ["codigo_ncm"] = Digits(item.Ncm),
                    ["cfop"] = Digits(item.Cfop),
                    ["unidade_comercial"] = item.Unidade,
                    ["quantidade_comercial"] = item.Quantidade,
                    ["valor_unitario_comercial"] = item.ValorUnitario,
                    ["valor_unitario_tributavel"] = item.ValorUnitario,
                    ["unidade_tributavel"] = item.Unidade,
                    ["quantidade_tributavel"] = item.Quantidade,
                    ["valor_bruto"] = item.ValorTotal,
                    ["icms_origem"] = item.IcmsOrigem,
                    ["icms_situacao_tributaria"] = item.IcmsSituacaoTributaria
                };
                if (!string.IsNullOrWhiteSpace(item.Cest))
                {
                    row["codigo_cest"] = Digits(item.Cest);
                }

                items.Add(row);
            }

            var payload = new Dictionary<string, object?>
            {
                ["natureza_operacao"] = document.NaturezaOperacao,
                ["data_emissao"] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                ["tipo_documento"] = 1,
                ["finalidade_emissao"] = 1,
                ["local_destino"] = 1,
                ["consumidor_final"] = 1,
                ["presenca_comprador"] = 1,
                ["modalidade_frete"] = 9,
                ["cnpj_emitente"] = Digits(emitente.Cnpj),
                ["nome_emitente"] = emitente.RazaoSocial,
                ["nome_fantasia_emitente"] = string.IsNullOrWhiteSpace(emitente.NomeFantasia) ? emitente.RazaoSocial : emitente.NomeFantasia,
                ["inscricao_estadual_emitente"] = emitente.InscricaoEstadual,
                ["logradouro_emitente"] = emitente.Logradouro,
                ["numero_emitente"] = emitente.Numero,
                ["bairro_emitente"] = emitente.Bairro,
                ["municipio_emitente"] = emitente.Municipio,
                ["uf_emitente"] = emitente.Uf,
                ["cep_emitente"] = Digits(emitente.Cep),
                ["codigo_municipio_emitente"] = Digits(emitente.CodigoMunicipioIbge),
                ["regime_tributario_emitente"] = emitente.RegimeTributario,
                ["nome_destinatario"] = dest.Nome,
                ["indicador_inscricao_estadual_destinatario"] = dest.IndicadorIeDestinatario,
                ["logradouro_destinatario"] = dest.Logradouro,
                ["numero_destinatario"] = dest.Numero,
                ["bairro_destinatario"] = dest.Bairro,
                ["municipio_destinatario"] = dest.Municipio,
                ["uf_destinatario"] = dest.Uf,
                ["cep_destinatario"] = Digits(dest.Cep),
                ["items"] = items
            };

            // Focus NFe: serie/numero opcionais (API pode auto-numerar); enviamos serie do emitente
            // quando configurada — alinhado ao validator FISCAL-EMITENTE-SERIE e à doc Focus.
            if (!string.IsNullOrWhiteSpace(emitente.SerieNFe))
            {
                payload["serie"] = Digits(emitente.SerieNFe);
            }

            if (!string.IsNullOrWhiteSpace(emitente.NumeroInicialNFe) && Digits(emitente.NumeroInicialNFe).Length > 0)
            {
                // Apenas se explicitamente configurado; Focus controla numeração por padrão.
                payload["numero"] = Digits(emitente.NumeroInicialNFe);
            }

            if (dest.IsCnpj)
            {
                payload["cnpj_destinatario"] = Digits(dest.Documento);
            }
            else
            {
                payload["cpf_destinatario"] = Digits(dest.Documento);
            }

            if (!string.IsNullOrWhiteSpace(document.FormaPagamentoCodigo))
            {
                payload["formas_pagamento"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["forma_pagamento"] = document.FormaPagamentoCodigo,
                        ["valor_pagamento"] = document.ValorTotal
                    }
                };
            }

            return payload;
        }

        private static string Digits(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());
    }

    public static class FocusNfeResponseMapper
    {
        public static FiscalProviderResult MapEmissionResponse(
            FocusNfeHttpResponse http,
            Guid operationId,
            string idempotencyKey)
        {
            if (http.IsTimeout)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Unknown,
                    FiscalErrorKind.Timeout,
                    "Timeout na comunicacao com Focus. Consulte o status antes de nova emissao.",
                    operationId,
                    idempotencyKey,
                    internalCode: "FISCAL-FOCUS-TIMEOUT");
            }

            if (http.IsNetworkError)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.NetworkError,
                    "Falha de rede ao comunicar com Focus.",
                    operationId,
                    idempotencyKey,
                    internalCode: "FISCAL-FOCUS-NETWORK",
                    providerMessage: http.NetworkMessage);
            }

            return MapByHttpStatus(http, operationId, idempotencyKey, isConsult: false);
        }

        public static FiscalProviderResult MapConsultResponse(
            FocusNfeHttpResponse http,
            Guid operationId,
            string idempotencyKey)
        {
            if (http.IsTimeout)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Unknown,
                    FiscalErrorKind.Timeout,
                    "Timeout na consulta Focus.",
                    operationId,
                    idempotencyKey,
                    internalCode: "FISCAL-FOCUS-CONSULT-TIMEOUT");
            }

            if (http.IsNetworkError)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.NetworkError,
                    "Falha de rede na consulta Focus.",
                    operationId,
                    idempotencyKey,
                    internalCode: "FISCAL-FOCUS-CONSULT-NETWORK",
                    providerMessage: http.NetworkMessage);
            }

            return MapByHttpStatus(http, operationId, idempotencyKey, isConsult: true);
        }

        private static FiscalProviderResult MapByHttpStatus(
            FocusNfeHttpResponse http,
            Guid operationId,
            string idempotencyKey,
            bool isConsult)
        {
            switch (http.StatusCode)
            {
                case 401:
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Failed,
                        FiscalErrorKind.AuthenticationError,
                        "Credencial Focus invalida ou ausente.",
                        operationId,
                        idempotencyKey,
                        internalCode: "FISCAL-FOCUS-401");
                case 403:
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Failed,
                        FiscalErrorKind.AuthorizationError,
                        "Acesso negado pelo Focus.",
                        operationId,
                        idempotencyKey,
                        internalCode: "FISCAL-FOCUS-403");
                case 404 when isConsult:
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Unknown,
                        FiscalErrorKind.ProviderError,
                        "Referencia fiscal nao encontrada no Focus.",
                        operationId,
                        idempotencyKey,
                        internalCode: "FISCAL-FOCUS-404");
                case 409:
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Failed,
                        FiscalErrorKind.ProviderError,
                        "Conflito/duplicidade reportada pelo Focus.",
                        operationId,
                        idempotencyKey,
                        internalCode: "FISCAL-FOCUS-409");
                case 422:
                case 400:
                    return MapBodyOrReject(http.Body, operationId, idempotencyKey, defaultStatus: FiscalDocumentStatus.Rejected);
                case 429:
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Failed,
                        FiscalErrorKind.ProviderError,
                        "Rate limit Focus (429). Aguarde Retry-After e consulte.",
                        operationId,
                        idempotencyKey,
                        internalCode: "FISCAL-FOCUS-429");
                case >= 500:
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Failed,
                        FiscalErrorKind.ProviderError,
                        $"Focus indisponivel (HTTP {http.StatusCode}).",
                        operationId,
                        idempotencyKey,
                        internalCode: $"FISCAL-FOCUS-{http.StatusCode}");
                case 200:
                case 201:
                case 202:
                    return MapBodyOrReject(http.Body, operationId, idempotencyKey, defaultStatus: FiscalDocumentStatus.Processing);
                default:
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Failed,
                        FiscalErrorKind.ProviderError,
                        $"Resposta HTTP Focus inesperada ({http.StatusCode}).",
                        operationId,
                        idempotencyKey,
                        internalCode: "FISCAL-FOCUS-HTTP-UNEXPECTED");
            }
        }

        private static FiscalProviderResult MapBodyOrReject(
            string body,
            Guid operationId,
            string idempotencyKey,
            FiscalDocumentStatus defaultStatus)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.ProviderError,
                    "Resposta Focus vazia (ProviderInvalidResponse).",
                    operationId,
                    idempotencyKey,
                    internalCode: "FISCAL-FOCUS-INVALID-RESPONSE");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var status = TryGetString(root, "status") ?? TryGetString(root, "situacao");
                var mensagem = TryGetString(root, "mensagem_sefaz")
                               ?? TryGetString(root, "mensagem")
                               ?? TryGetString(root, "message")
                               ?? string.Empty;
                var codigo = TryGetString(root, "codigo_status")
                             ?? TryGetString(root, "codigo_sefaz")
                             ?? TryGetString(root, "codigo")
                             ?? string.Empty;
                var chave = TryGetString(root, "chave_nfe") ?? TryGetString(root, "chave");
                var protocolo = TryGetString(root, "protocolo");
                var numero = TryGetString(root, "numero");
                var refId = TryGetString(root, "ref") ?? TryGetString(root, "referencia");

                if (string.IsNullOrWhiteSpace(status) && string.IsNullOrWhiteSpace(chave) && string.IsNullOrWhiteSpace(refId))
                {
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Failed,
                        FiscalErrorKind.ProviderError,
                        "JSON Focus incompleto (ProviderInvalidResponse).",
                        operationId,
                        idempotencyKey,
                        internalCode: "FISCAL-FOCUS-INVALID-RESPONSE",
                        providerMessage: Truncate(body, 200));
                }

                var mapped = MapFocusStatus(status, defaultStatus);
                if (mapped == FiscalDocumentStatus.Authorized)
                {
                    return FiscalProviderResult.Ok(
                        FiscalDocumentStatus.Authorized,
                        operationId,
                        idempotencyKey,
                        string.IsNullOrWhiteSpace(mensagem) ? "NF-e autorizada pelo Focus (homologacao)." : mensagem,
                        providerDocumentId: refId ?? numero,
                        chave: chave,
                        protocolo: protocolo);
                }

                if (mapped is FiscalDocumentStatus.Rejected or FiscalDocumentStatus.Denied)
                {
                    return FiscalProviderResult.Fail(
                        mapped,
                        FiscalErrorKind.FiscalRejection,
                        string.IsNullOrWhiteSpace(mensagem) ? "NF-e rejeitada." : mensagem,
                        operationId,
                        idempotencyKey,
                        internalCode: "FISCAL-FOCUS-REJECTED",
                        providerCode: codigo,
                        providerMessage: mensagem);
                }

                if (mapped is FiscalDocumentStatus.Processing or FiscalDocumentStatus.Pending)
                {
                    return FiscalProviderResult.Ok(
                        mapped,
                        operationId,
                        idempotencyKey,
                        string.IsNullOrWhiteSpace(mensagem) ? "NF-e em processamento no Focus." : mensagem,
                        providerDocumentId: refId);
                }

                return FiscalProviderResult.Fail(
                    mapped,
                    FiscalErrorKind.ProviderError,
                    string.IsNullOrWhiteSpace(mensagem) ? $"Status Focus: {status}" : mensagem,
                    operationId,
                    idempotencyKey,
                    internalCode: "FISCAL-FOCUS-STATUS",
                    providerCode: codigo,
                    providerMessage: mensagem);
            }
            catch (JsonException)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.ProviderError,
                    "JSON Focus invalido (ProviderInvalidResponse).",
                    operationId,
                    idempotencyKey,
                    internalCode: "FISCAL-FOCUS-INVALID-RESPONSE");
            }
        }

        private static FiscalDocumentStatus MapFocusStatus(string? status, FiscalDocumentStatus fallback)
        {
            if (string.IsNullOrWhiteSpace(status)) return fallback;
            var s = status.Trim().ToLowerInvariant();
            return s switch
            {
                "autorizado" or "autorizada" => FiscalDocumentStatus.Authorized,
                "erro_autorizacao" or "rejeitado" or "rejeitada" => FiscalDocumentStatus.Rejected,
                "denegado" or "denegada" => FiscalDocumentStatus.Denied,
                "cancelado" or "cancelada" => FiscalDocumentStatus.Cancelled,
                "processando_autorizacao" or "processando" or "em_processamento" => FiscalDocumentStatus.Processing,
                _ => FiscalDocumentStatus.Unknown
            };
        }

        private static string? TryGetString(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty(name, out var prop)) return null;
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max];
    }
}
