using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.Fiscal.PlugNotas
{
    /// <summary>
    /// Adapter PlugNotas — scaffold controlado.
    /// Endpoints reais nao inventados; requer contrato/documentacao oficial + credencial.
    /// </summary>
    public sealed class PlugNotasProvider : IFiscalProvider
    {
        public FiscalProviderKind Kind => FiscalProviderKind.PlugNotas;

        public Task<FiscalProviderResult> EmitirAsync(FiscalEmissionRequest request, CancellationToken cancellationToken = default)
            => NotImplemented(request.FiscalOperationId, request.IdempotencyKey, "Emitir");

        public Task<FiscalProviderResult> ConsultarAsync(FiscalOperation operation, CancellationToken cancellationToken = default)
            => NotImplemented(operation.Id, operation.IdempotencyKey, "Consultar");

        public Task<FiscalProviderResult> CancelarAsync(FiscalCancellationRequest request, CancellationToken cancellationToken = default)
            => NotImplemented(request.FiscalOperationId, string.Empty, "Cancelar");

        public Task<FiscalProviderResult> ObterXmlAsync(FiscalOperation operation, CancellationToken cancellationToken = default)
            => NotImplemented(operation.Id, operation.IdempotencyKey, "ObterXml");

        public Task<FiscalProviderResult> ObterDanfeAsync(FiscalOperation operation, CancellationToken cancellationToken = default)
            => NotImplemented(operation.Id, operation.IdempotencyKey, "ObterDanfe");

        private static Task<FiscalProviderResult> NotImplemented(Guid operationId, string key, string op)
            => Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                $"PlugNotas {op}: scaffold — contrato/credencial ausentes (BLOCKED_EXTERNAL).",
                operationId,
                key,
                internalCode: "FISCAL-PLUGNOTAS-SCAFFOLD"));
    }
}
