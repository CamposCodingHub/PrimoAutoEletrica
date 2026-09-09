using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Centro operacional fiscal (homologação): saúde, preview, histórico, consulta e cancelamento com guardas.
    /// </summary>
    public sealed class FiscalOperationsCenterService
    {
        private readonly FiscalConfigurationService _configurationService;
        private readonly FiscalHealthCheck _healthCheck;
        private readonly FiscalNFePreviewBuilder _previewBuilder;
        private readonly VendaFiscalNFeMapper _mapper;
        private readonly FiscalApplicationService _fiscal;
        private readonly FiscalOperationStore _store;
        private readonly NFeHomologationService _homologation;

        public FiscalOperationsCenterService(
            FiscalConfigurationService configurationService,
            FiscalHealthCheck healthCheck,
            FiscalNFePreviewBuilder previewBuilder,
            VendaFiscalNFeMapper mapper,
            FiscalApplicationService fiscal,
            FiscalOperationStore store,
            NFeHomologationService homologation)
        {
            _configurationService = configurationService;
            _healthCheck = healthCheck;
            _previewBuilder = previewBuilder;
            _mapper = mapper;
            _fiscal = fiscal;
            _store = store;
            _homologation = homologation;
        }

        public FiscalHealthReport GetHealth(bool requireLiveCredential = true)
            => _healthCheck.Evaluate(requireLiveCredential);

        public FiscalConfiguration LoadConfiguration() => _configurationService.LoadOrCreate();

        public void SaveConfiguration(FiscalConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            configuration.Environment = FiscalEnvironment.Homologation;
            configuration.ProductionUnlocked = false;
            _configurationService.Save(configuration);
        }

        public void SaveHomologationToken(string plainToken)
            => _configurationService.SaveHomologationToken(plainToken);

        public FiscalNFePreview BuildPreviewFromVenda(
            Venda venda,
            Cliente cliente,
            IReadOnlyDictionary<Guid, Produto> produtos,
            bool requireProviderCredential = false)
        {
            var config = _configurationService.LoadOrCreate();
            var document = _mapper.Map(
                venda,
                cliente,
                produtos,
                config.Issuer,
                Guid.NewGuid(),
                NFeHomologationService.BuildIdempotencyKey(venda.Id),
                FiscalEnvironment.Homologation);
            return _previewBuilder.Build(document, config, requireProviderCredential);
        }

        public FiscalValidationResult ValidateVendaOnly(
            Venda venda,
            Cliente cliente,
            IReadOnlyDictionary<Guid, Produto> produtos)
            => _homologation.ValidateVenda(venda, cliente, produtos, requireProviderCredential: false);

        public IReadOnlyList<FiscalOperationListItem> ListHistory(int limit = 50)
        {
            return _store.ListRecent(limit).Select(op =>
            {
                var doc = _store.FindDocumentByOperationId(op.Id);
                return new FiscalOperationListItem
                {
                    OperationId = op.Id,
                    UpdatedAt = op.UpdatedAt,
                    OriginModule = op.OriginModule,
                    Status = op.Status,
                    Provider = op.Provider,
                    Environment = op.Environment,
                    VendaId = op.VendaId,
                    IdempotencyKey = op.IdempotencyKey,
                    Numero = doc?.Numero,
                    Serie = doc?.Serie,
                    ChaveAcesso = doc?.ChaveAcesso,
                    Protocolo = doc?.Protocolo,
                    LastError = op.LastErrorMessage
                };
            }).ToList();
        }

        public FiscalOperationDetail? GetDetail(Guid operationId)
        {
            var op = _store.FindById(operationId);
            if (op == null)
            {
                return null;
            }

            var doc = _store.FindDocumentByOperationId(operationId);
            var events = _store.ListEvents(operationId);
            return new FiscalOperationDetail
            {
                Operation = op,
                Document = doc,
                Timeline = events.Select(e => $"{e.CreatedAt:u} | {e.EventType} | {e.Message}").ToList(),
                CanConsult = FiscalStateMachine.CanConsult(op.Status),
                CanCancel = FiscalStateMachine.CanCancel(op.Status, op.Environment)
            };
        }

        public Task<FiscalProviderResult> ConsultarAsync(Guid operationId, CancellationToken ct = default)
            => _fiscal.ConsultarAsync(operationId, ct);

        public Task<FiscalProviderResult> CancelarHomologacaoAsync(
            Guid operationId,
            string justificativa,
            CancellationToken ct = default)
            => _fiscal.CancelarAsync(new FiscalCancellationRequest
            {
                FiscalOperationId = operationId,
                Justificativa = justificativa,
                Environment = FiscalEnvironment.Homologation
            }, ct);

        public Task<FiscalProviderResult> EmitirHomologacaoFromVendaAsync(
            Venda venda,
            Cliente cliente,
            IReadOnlyDictionary<Guid, Produto> produtos,
            CancellationToken ct = default)
            => _homologation.EmitirHomologacaoFromVendaAsync(venda, cliente, produtos, ct);
    }

    public sealed class FiscalOperationListItem
    {
        public Guid OperationId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public string OriginModule { get; init; } = string.Empty;
        public FiscalDocumentStatus Status { get; init; }
        public FiscalProviderKind Provider { get; init; }
        public FiscalEnvironment Environment { get; init; }
        public Guid? VendaId { get; init; }
        public string IdempotencyKey { get; init; } = string.Empty;
        public string? Numero { get; init; }
        public string? Serie { get; init; }
        public string? ChaveAcesso { get; init; }
        public string? Protocolo { get; init; }
        public string? LastError { get; init; }
    }

    public sealed class FiscalOperationDetail
    {
        public FiscalOperation Operation { get; init; } = new();
        public FiscalDocumentRecord? Document { get; init; }
        public IReadOnlyList<string> Timeline { get; init; } = Array.Empty<string>();
        public bool CanConsult { get; init; }
        public bool CanCancel { get; init; }
    }
}
