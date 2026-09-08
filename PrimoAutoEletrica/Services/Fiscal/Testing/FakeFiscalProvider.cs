using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.Fiscal.Testing
{
    /// <summary>
    /// TEST ONLY — nunca registrar como provedor comercial no DI de produção.
    /// Cenários explícitos; nunca sucesso silencioso sem cenário.
    /// </summary>
    public sealed class FakeFiscalProvider : IFiscalProvider
    {
        private readonly FiscalFakeScenario _scenario;
        private Guid? _firstOperationId;

        public FakeFiscalProvider(FiscalFakeScenario scenario)
        {
            _scenario = scenario;
        }

        public FiscalProviderKind Kind => FiscalProviderKind.FakeTestOnly;

        public Task<FiscalProviderResult> EmitirAsync(
            FiscalEmissionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var blocked = FiscalProductionGuard.TryDenyProduction(
                request.Environment,
                request.FiscalOperationId,
                request.IdempotencyKey);
            if (blocked != null)
            {
                return Task.FromResult(blocked);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_scenario == FiscalFakeScenario.FakeDuplicate)
            {
                _firstOperationId ??= request.FiscalOperationId;
                return Task.FromResult(FiscalProviderResult.Ok(
                    FiscalDocumentStatus.Authorized,
                    _firstOperationId.Value,
                    request.IdempotencyKey,
                    "FakeDuplicate: mesma operacao retornada.",
                    providerDocumentId: "FAKE-DUP-1",
                    chave: "FAKECHAVE",
                    protocolo: "FAKEPROTO"));
            }

            return Task.FromResult(_scenario switch
            {
                FiscalFakeScenario.FakeAuthorized => FiscalProviderResult.Ok(
                    FiscalDocumentStatus.Authorized,
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    "FakeAuthorized (TEST ONLY).",
                    providerDocumentId: "FAKE-AUTH-1",
                    chave: "35260900000000000000550010000000011000000010",
                    protocolo: "135260000000001"),
                FiscalFakeScenario.FakeRejected => FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Rejected,
                    FiscalErrorKind.FiscalRejection,
                    "Rejeicao fiscal simulada.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FAKE-REJECTED",
                    providerCode: "204",
                    providerMessage: "Duplicidade de NF-e (simulada)"),
                FiscalFakeScenario.FakeTimeout => FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Processing,
                    FiscalErrorKind.Timeout,
                    "Timeout simulado — consultar estado.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FAKE-TIMEOUT"),
                FiscalFakeScenario.FakeNetworkError => FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.NetworkError,
                    "Falha de rede simulada.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FAKE-NETWORK"),
                _ => FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.UnknownError,
                    "Cenario fake nao suportado.",
                    request.FiscalOperationId,
                    request.IdempotencyKey,
                    internalCode: "FISCAL-FAKE-UNKNOWN")
            });
        }

        public Task<FiscalProviderResult> ConsultarAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();

            if (_scenario == FiscalFakeScenario.FakeTimeout)
            {
                return Task.FromResult(FiscalProviderResult.Ok(
                    FiscalDocumentStatus.Authorized,
                    operation.Id,
                    operation.IdempotencyKey,
                    "Consulta apos timeout: autorizada (fake).",
                    providerDocumentId: "FAKE-AFTER-TIMEOUT",
                    protocolo: "135260000000099"));
            }

            return Task.FromResult(FiscalProviderResult.Ok(
                operation.Status == FiscalDocumentStatus.Draft ? FiscalDocumentStatus.Unknown : operation.Status,
                operation.Id,
                operation.IdempotencyKey,
                "Consulta fake: estado atual."));
        }

        public Task<FiscalProviderResult> CancelarAsync(
            FiscalCancellationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Justificativa) || request.Justificativa.Trim().Length < 15)
            {
                return Task.FromResult(FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.ValidationError,
                    "Justificativa de cancelamento invalida (minimo 15 caracteres).",
                    request.FiscalOperationId,
                    string.Empty,
                    internalCode: "FISCAL-FAKE-CANCEL-INVALID"));
            }

            return Task.FromResult(FiscalProviderResult.Ok(
                FiscalDocumentStatus.Cancelled,
                request.FiscalOperationId,
                string.Empty,
                "Cancelamento fake."));
        }

        public Task<FiscalProviderResult> ObterXmlAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
            => Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "Fake nao gera XML fiscal real.",
                operation.Id,
                operation.IdempotencyKey,
                internalCode: "FISCAL-FAKE-XML"));

        public Task<FiscalProviderResult> ObterDanfeAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default)
            => Task.FromResult(FiscalProviderResult.Fail(
                FiscalDocumentStatus.NotImplemented,
                FiscalErrorKind.NotImplemented,
                "Fake nao gera DANFE/PDF.",
                operation.Id,
                operation.IdempotencyKey,
                internalCode: "FISCAL-FAKE-DANFE"));
    }
}
