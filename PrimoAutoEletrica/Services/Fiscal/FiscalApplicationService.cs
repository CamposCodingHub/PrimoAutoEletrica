using System;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Camada de aplicação fiscal: idempotência, auditoria, persistência e orquestração do IFiscalProvider.
    /// Não emite NF real — delega ao provider (Focus com HTTP off nesta fase).
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

                if (IsTerminalOrInFlight(existing.Status))
                {
                    return ToResultFromOperation(existing, "Operacao existente reutilizada (idempotencia).");
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
            else
            {
                operation.Status = FiscalDocumentStatus.Processing;
                operation.UpdatedAt = now;
                _store.Upsert(operation);
            }

            _audit?.Registrar(
                categoria: "Fiscal",
                acao: "EmitirSolicitado",
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
                result.Success ? "ProviderResponse" : "ProviderError",
                SafeLogMessage(result),
                result.ProviderCode,
                operation.IdempotencyKey);

            if (!string.IsNullOrWhiteSpace(result.ChaveAcesso) || result.Status == FiscalDocumentStatus.Authorized)
            {
                _store.UpsertDocument(new FiscalDocumentRecord
                {
                    Id = Guid.NewGuid(),
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

            _audit?.Registrar(
                categoria: "Fiscal",
                acao: "EmitirResultado",
                entidade: "FiscalOperation",
                entidadeId: operation.Id.ToString("N"),
                detalhes: SafeLogMessage(result),
                severidade: result.Success ? "Info" : "Warning",
                sucesso: result.Success,
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
            _store.AppendEvent(operation.Id, "Consulted", SafeLogMessage(result), result.ProviderCode, operation.IdempotencyKey);
            return result;
        }

        public Task<FiscalProviderResult> CancelarAsync(
            FiscalCancellationRequest request,
            CancellationToken cancellationToken = default)
            => _provider.CancelarAsync(request, cancellationToken);

        private static FiscalEmissionRequest NormalizeRequest(FiscalEmissionRequest request)
        {
            var key = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? request.FiscalOperationId.ToString("N")
                : request.IdempotencyKey.Trim();

            var env = request.Environment;
            if (env == FiscalEnvironment.Production && !FiscalProductionGuard.ProductionEmissionAllowed)
            {
                // Mantém o Environment do request para o guard do provider negar explicitamente.
            }

            return new FiscalEmissionRequest
            {
                FiscalOperationId = request.FiscalOperationId == Guid.Empty ? Guid.NewGuid() : request.FiscalOperationId,
                IdempotencyKey = key,
                DocumentType = request.DocumentType,
                Environment = env,
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

        private static bool IsTerminalOrInFlight(FiscalDocumentStatus status) =>
            status is FiscalDocumentStatus.Authorized
                or FiscalDocumentStatus.Rejected
                or FiscalDocumentStatus.Cancelled
                or FiscalDocumentStatus.Denied
                or FiscalDocumentStatus.Processing
                or FiscalDocumentStatus.Pending
                or FiscalDocumentStatus.Contingency
                or FiscalDocumentStatus.ProductionBlocked
                or FiscalDocumentStatus.NotImplemented
                or FiscalDocumentStatus.NotConfigured
                or FiscalDocumentStatus.Failed;

        private static void ApplyProviderResult(FiscalOperation operation, FiscalProviderResult result)
        {
            operation.Status = result.Status;
            operation.ProviderDocumentId = result.ProviderDocumentId;
            operation.UpdatedAt = DateTime.UtcNow;
            if (!result.Success)
            {
                operation.LastErrorKind = result.ErrorKind.ToString();
                operation.LastErrorMessage = Truncate(result.Message, 500);
            }
            else
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

        private static string SafeLogMessage(FiscalProviderResult result)
        {
            // Nunca incluir token/senha; apenas códigos e status.
            return $"Status={result.Status}; Error={result.ErrorKind}; Code={result.InternalCode}; ProviderCode={result.ProviderCode}; Msg={Truncate(result.Message, 200)}";
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value[..max];
        }
    }
}
