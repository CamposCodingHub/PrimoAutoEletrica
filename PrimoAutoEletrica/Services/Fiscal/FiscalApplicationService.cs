using System;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Camada de aplicação fiscal: idempotência, auditoria, persistência e orquestração do IFiscalProvider.
    /// Timeout/Unknown → consultar antes de qualquer nova emissão.
    /// </summary>
    public sealed class FiscalApplicationService
    {
        private readonly IFiscalProvider _provider;
        private readonly FiscalOperationStore _store;
        private readonly AuditLogService? _audit;
        private readonly LoggerService? _logger;

        public FiscalApplicationService(
            IFiscalProvider provider,
            FiscalOperationStore store,
            AuditLogService? audit = null,
            LoggerService? logger = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _audit = audit;
            _logger = logger;
        }

        public FiscalOperation? FindByIdempotencyKey(string idempotencyKey)
            => _store.FindByIdempotencyKey(idempotencyKey);

        public FiscalOperation? FindById(Guid id) => _store.FindById(id);

        public async Task<FiscalProviderResult> EmitirAsync(
            FiscalEmissionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var normalized = NormalizeRequest(request);
            var existing = _store.FindByIdempotencyKey(normalized.IdempotencyKey);
            if (existing != null)
            {
                _logger?.LogInfo(
                    $"[Fiscal] Idempotencia hit OperationId={existing.Id:N} Key={normalized.IdempotencyKey} Status={existing.Status}");

                if (IsFinal(existing.Status))
                {
                    return ToResultFromOperation(existing, "Operacao existente reutilizada (idempotencia).");
                }

                if (NeedsConsultBeforeRetry(existing.Status))
                {
                    _audit?.Registrar("Fiscal", "FiscalStatusQueried", "FiscalOperation", existing.Id.ToString("N"),
                        detalhes: "Consulta previa a retry/idempotencia", sucesso: true, correlationId: existing.IdempotencyKey);

                    var consulted = await ConsultarAsync(existing.Id, cancellationToken).ConfigureAwait(false);
                    if (IsFinal(consulted.Status) ||
                        consulted.Status is FiscalDocumentStatus.Processing or FiscalDocumentStatus.Pending)
                    {
                        return consulted with
                        {
                            FiscalOperationId = existing.Id,
                            IdempotencyKey = existing.IdempotencyKey,
                            InternalCode = consulted.InternalCode ?? "FISCAL-RECOVERED-VIA-CONSULT"
                        };
                    }

                    // Ainda sem resultado confiável — não reemite cegamente.
                    return FiscalProviderResult.Fail(
                        FiscalDocumentStatus.Unknown,
                        FiscalErrorKind.Timeout,
                        "Resultado ainda nao confirmado. Consulte novamente antes de tentar uma nova emissao.",
                        existing.Id,
                        existing.IdempotencyKey,
                        internalCode: "FISCAL-AWAIT-CONSULT");
                }

                if (existing.Status is FiscalDocumentStatus.NotConfigured
                    or FiscalDocumentStatus.NotImplemented
                    or FiscalDocumentStatus.Failed
                    or FiscalDocumentStatus.Draft
                    or FiscalDocumentStatus.Validating)
                {
                    // Reutiliza o mesmo OperationId para nova tentativa segura.
                    normalized = new FiscalEmissionRequest
                    {
                        FiscalOperationId = existing.Id,
                        IdempotencyKey = existing.IdempotencyKey,
                        DocumentType = normalized.DocumentType,
                        Environment = normalized.Environment,
                        Provider = normalized.Provider,
                        OriginModule = normalized.OriginModule,
                        OrdemServicoId = normalized.OrdemServicoId,
                        VendaId = normalized.VendaId,
                        OrcamentoId = normalized.OrcamentoId,
                        ClienteDocumento = normalized.ClienteDocumento,
                        ClienteNome = normalized.ClienteNome,
                        Items = normalized.Items,
                        Total = normalized.Total,
                        Observacoes = normalized.Observacoes
                    };
                }
            }

            var now = DateTime.UtcNow;
            var operation = existing ?? new FiscalOperation
            {
                Id = normalized.FiscalOperationId,
                IdempotencyKey = normalized.IdempotencyKey,
                DocumentType = normalized.DocumentType,
                Status = FiscalDocumentStatus.Pending,
                Environment = normalized.Environment,
                Provider = normalized.Provider,
                OriginModule = normalized.OriginModule,
                OrdemServicoId = normalized.OrdemServicoId,
                VendaId = normalized.VendaId,
                OrcamentoId = normalized.OrcamentoId,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (existing == null)
            {
                _store.Upsert(operation);
                _store.AppendEvent(operation.Id, "Created", "Operacao fiscal criada.");
            }

            _audit?.Registrar(
                categoria: "Fiscal",
                acao: "FiscalEmissionSubmitted",
                entidade: "FiscalOperation",
                entidadeId: operation.Id.ToString("N"),
                detalhes: $"Provider={_provider.Kind}; Env={operation.Environment}; Doc={operation.DocumentType}; Origin={operation.OriginModule}",
                severidade: "Info",
                sucesso: true,
                correlationId: operation.IdempotencyKey);

            operation.Status = FiscalDocumentStatus.Processing;
            operation.UpdatedAt = DateTime.UtcNow;
            _store.Upsert(operation);

            var result = await _provider.EmitirAsync(normalized, cancellationToken).ConfigureAwait(false);

            ApplyProviderResult(operation, result);
            _store.Upsert(operation);
            _store.AppendEvent(
                operation.Id,
                MapEventName(result),
                SafeLogMessage(result),
                result.ProviderCode,
                operation.IdempotencyKey);

            PersistDocumentIfNeeded(operation, result);

            _audit?.Registrar(
                categoria: "Fiscal",
                acao: MapAuditAction(result),
                entidade: "FiscalOperation",
                entidadeId: operation.Id.ToString("N"),
                detalhes: SafeLogMessage(result),
                severidade: result.Success ? "Info" : "Warning",
                sucesso: result.Success || result.Status == FiscalDocumentStatus.Processing,
                correlationId: operation.IdempotencyKey);

            return result with { FiscalOperationId = operation.Id, IdempotencyKey = operation.IdempotencyKey };
        }

        public async Task<FiscalProviderResult> ConsultarAsync(
            Guid fiscalOperationId,
            CancellationToken cancellationToken = default)
        {
            var operation = _store.FindById(fiscalOperationId);
            if (operation == null)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Unknown,
                    FiscalErrorKind.ValidationError,
                    "Operacao fiscal nao encontrada.",
                    fiscalOperationId,
                    string.Empty,
                    internalCode: "FISCAL-OP-NOT-FOUND");
            }

            var result = await _provider.ConsultarAsync(operation, cancellationToken).ConfigureAwait(false);
            ApplyProviderResult(operation, result);
            _store.Upsert(operation);
            _store.AppendEvent(operation.Id, "FiscalStatusQueried", SafeLogMessage(result), result.ProviderCode, operation.IdempotencyKey);
            PersistDocumentIfNeeded(operation, result);

            _audit?.Registrar(
                categoria: "Fiscal",
                acao: "FiscalStatusQueried",
                entidade: "FiscalOperation",
                entidadeId: operation.Id.ToString("N"),
                detalhes: SafeLogMessage(result),
                severidade: "Info",
                sucesso: true,
                correlationId: operation.IdempotencyKey);

            return result with { FiscalOperationId = operation.Id, IdempotencyKey = operation.IdempotencyKey };
        }

        public Task<FiscalProviderResult> CancelarAsync(
            FiscalCancellationRequest request,
            CancellationToken cancellationToken = default)
            => _provider.CancelarAsync(request, cancellationToken);

        private void PersistDocumentIfNeeded(FiscalOperation operation, FiscalProviderResult result)
        {
            if (string.IsNullOrWhiteSpace(result.ChaveAcesso) && result.Status != FiscalDocumentStatus.Authorized)
            {
                return;
            }

            // Idempotente: um documento por operação (reutiliza Id estável derivado).
            var documentId = Guid.Parse(operation.Id.ToString("N").Substring(0, 32).PadRight(32, '0'));
            try
            {
                documentId = CreateStableDocumentId(operation.Id);
            }
            catch
            {
                documentId = operation.Id;
            }

            _store.UpsertDocument(new FiscalDocumentRecord
            {
                Id = documentId,
                OperationId = operation.Id,
                DocumentType = operation.DocumentType,
                ChaveAcesso = result.ChaveAcesso,
                Status = result.Status,
                Environment = operation.Environment,
                Provider = operation.Provider,
                Protocolo = result.Protocolo,
                Reason = result.Message,
                OrdemServicoId = operation.OrdemServicoId,
                VendaId = operation.VendaId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        private static Guid CreateStableDocumentId(Guid operationId)
        {
            var bytes = operationId.ToByteArray();
            bytes[0] ^= 0x5A;
            return new Guid(bytes);
        }

        private static FiscalEmissionRequest NormalizeRequest(FiscalEmissionRequest request)
        {
            var key = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? request.FiscalOperationId.ToString("N")
                : request.IdempotencyKey.Trim();

            return new FiscalEmissionRequest
            {
                FiscalOperationId = request.FiscalOperationId == Guid.Empty ? Guid.NewGuid() : request.FiscalOperationId,
                IdempotencyKey = key,
                DocumentType = request.DocumentType,
                Environment = request.Environment,
                Provider = request.Provider,
                OriginModule = request.OriginModule ?? string.Empty,
                OrdemServicoId = request.OrdemServicoId,
                VendaId = request.VendaId,
                OrcamentoId = request.OrcamentoId,
                ClienteDocumento = request.ClienteDocumento,
                ClienteNome = request.ClienteNome,
                Items = request.Items,
                Total = request.Total,
                Observacoes = request.Observacoes
            };
        }

        private static bool IsFinal(FiscalDocumentStatus status) =>
            status is FiscalDocumentStatus.Authorized
                or FiscalDocumentStatus.Rejected
                or FiscalDocumentStatus.Cancelled
                or FiscalDocumentStatus.Denied
                or FiscalDocumentStatus.ProductionBlocked;

        private static bool NeedsConsultBeforeRetry(FiscalDocumentStatus status) =>
            status is FiscalDocumentStatus.Processing
                or FiscalDocumentStatus.Pending
                or FiscalDocumentStatus.Unknown
                or FiscalDocumentStatus.Contingency;

        private static void ApplyProviderResult(FiscalOperation operation, FiscalProviderResult result)
        {
            operation.Status = result.Status;
            operation.ProviderDocumentId = result.ProviderDocumentId ?? operation.ProviderDocumentId;
            operation.UpdatedAt = DateTime.UtcNow;
            if (!result.Success && result.Status != FiscalDocumentStatus.Processing && result.Status != FiscalDocumentStatus.Pending)
            {
                operation.LastErrorKind = result.ErrorKind.ToString();
                operation.LastErrorMessage = Truncate(result.Message, 500);
            }
            else if (result.Success || result.Status == FiscalDocumentStatus.Authorized)
            {
                operation.LastErrorKind = null;
                operation.LastErrorMessage = null;
            }
        }

        private static FiscalProviderResult ToResultFromOperation(FiscalOperation operation, string message) =>
            new()
            {
                Success = operation.Status is FiscalDocumentStatus.Authorized or FiscalDocumentStatus.Processing or FiscalDocumentStatus.Pending,
                Status = operation.Status,
                Message = message,
                FiscalOperationId = operation.Id,
                IdempotencyKey = operation.IdempotencyKey,
                ProviderDocumentId = operation.ProviderDocumentId,
                ErrorKind = string.IsNullOrWhiteSpace(operation.LastErrorKind)
                    ? FiscalErrorKind.None
                    : Enum.TryParse<FiscalErrorKind>(operation.LastErrorKind, out var kind) ? kind : FiscalErrorKind.UnknownError,
                InternalCode = "FISCAL-IDEMPOTENT-REUSE"
            };

        private static string MapEventName(FiscalProviderResult result) => result.Status switch
        {
            FiscalDocumentStatus.Authorized => "FiscalEmissionAuthorized",
            FiscalDocumentStatus.Rejected => "FiscalEmissionRejected",
            FiscalDocumentStatus.Unknown when result.ErrorKind == FiscalErrorKind.Timeout => "FiscalEmissionTimeout",
            FiscalDocumentStatus.Processing => "FiscalEmissionProcessing",
            _ => result.Success ? "ProviderResponse" : "FiscalEmissionError"
        };

        private static string MapAuditAction(FiscalProviderResult result) => result.Status switch
        {
            FiscalDocumentStatus.Authorized => "FiscalEmissionAuthorized",
            FiscalDocumentStatus.Rejected => "FiscalEmissionRejected",
            FiscalDocumentStatus.Unknown when result.ErrorKind == FiscalErrorKind.Timeout => "FiscalEmissionTimeout",
            FiscalDocumentStatus.Processing => "FiscalEmissionProcessing",
            _ => "FiscalEmissionError"
        };

        private static string SafeLogMessage(FiscalProviderResult result)
            => $"Status={result.Status}; Error={result.ErrorKind}; Code={result.InternalCode}; ProviderCode={result.ProviderCode}; Msg={Truncate(result.Message, 200)}";

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value[..max];
        }
    }
}
