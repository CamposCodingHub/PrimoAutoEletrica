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
    /// Orquestra NF-e Homologação: valida → mapeia Venda → emite via IFiscalProvider.
    /// Não altera estoque/financeiro/venda.
    /// </summary>
    public sealed class NFeHomologationService
    {
        private readonly FiscalConfigurationService _configurationService;
        private readonly FiscalDocumentValidator _validator;
        private readonly VendaFiscalNFeMapper _mapper;
        private readonly FiscalApplicationService _fiscal;
        private readonly IFiscalProvider _provider;
        private readonly AuditLogService? _audit;
        private readonly LoggerService? _logger;

        public NFeHomologationService(
            FiscalConfigurationService configurationService,
            FiscalDocumentValidator validator,
            VendaFiscalNFeMapper mapper,
            FiscalApplicationService fiscal,
            IFiscalProvider provider,
            AuditLogService? audit = null,
            LoggerService? logger = null)
        {
            _configurationService = configurationService;
            _validator = validator;
            _mapper = mapper;
            _fiscal = fiscal;
            _provider = provider;
            _audit = audit;
            _logger = logger;
        }

        public FiscalValidationResult ValidateVenda(
            Venda venda,
            Cliente cliente,
            IReadOnlyDictionary<Guid, Produto> produtos,
            bool requireProviderCredential = true)
        {
            var config = _configurationService.LoadOrCreate();
            var opId = Guid.NewGuid();
            var key = BuildIdempotencyKey(venda.Id);
            var document = _mapper.Map(
                venda,
                cliente,
                produtos,
                config.Issuer,
                opId,
                key,
                FiscalEnvironment.Homologation);

            _audit?.Registrar("Fiscal", "FiscalValidationStarted", "Venda", venda.Id.ToString("N"),
                detalhes: "Homologacao", sucesso: true, correlationId: key);

            var validation = _validator.ValidateForHomologEmission(document, config, requireProviderCredential);
            if (!validation.IsValid)
            {
                _audit?.Registrar("Fiscal", "FiscalValidationFailed", "Venda", venda.Id.ToString("N"),
                    detalhes: Truncate(validation.Summarize(), 500), severidade: "Warning", sucesso: false, correlationId: key);
            }

            return validation;
        }

        public async Task<FiscalProviderResult> EmitirHomologacaoFromVendaAsync(
            Venda venda,
            Cliente cliente,
            IReadOnlyDictionary<Guid, Produto> produtos,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(venda);
            ArgumentNullException.ThrowIfNull(cliente);

            var productionDenied = FiscalProductionGuard.TryDenyProduction(
                FiscalEnvironment.Production,
                Guid.NewGuid(),
                "homolog-entry");
            // Entrada de homologação nunca usa Production; guard permanece como rede de segurança global.

            var config = _configurationService.LoadOrCreate();
            if (config.Environment == FiscalEnvironment.Production)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.ProductionBlocked,
                    FiscalErrorKind.ProductionBlocked,
                    "Entrada de homologacao recusou ambiente de producao.",
                    Guid.Empty,
                    string.Empty,
                    internalCode: "FISCAL-PROD-BLOCKED");
            }

            // Silencia warning de variável não usada em builds que analisam o guard call.
            _ = productionDenied;

            var key = BuildIdempotencyKey(venda.Id);
            var existing = _fiscal.FindByIdempotencyKey(key);
            var opId = existing?.Id ?? Guid.NewGuid();

            var document = _mapper.Map(
                venda,
                cliente,
                produtos,
                config.Issuer,
                opId,
                key,
                FiscalEnvironment.Homologation);

            var validation = _validator.ValidateForHomologEmission(document, config, requireProviderCredential: true);
            if (!validation.IsValid)
            {
                return FiscalProviderResult.Fail(
                    FiscalDocumentStatus.Failed,
                    FiscalErrorKind.ValidationError,
                    validation.Summarize(),
                    opId,
                    key,
                    internalCode: "FISCAL-VALIDATION-FAILED");
            }

            if (_provider is FocusNfeProvider focus)
            {
                focus.SetPendingDocument(document);
            }

            var request = _mapper.ToEmissionRequest(document);
            _audit?.Registrar("Fiscal", "FiscalEmissionRequested", "FiscalOperation", opId.ToString("N"),
                detalhes: $"Venda={venda.Id:N}; Env=Homologation; Provider={_provider.Kind}",
                sucesso: true, correlationId: key);

            var result = await _fiscal.EmitirAsync(request, cancellationToken).ConfigureAwait(false);
            _logger?.LogInfo($"[Fiscal] Homolog emit Status={result.Status} Code={result.InternalCode} Op={result.FiscalOperationId:N}");
            return result;
        }

        public Task<FiscalProviderResult> ConsultarAsync(Guid fiscalOperationId, CancellationToken cancellationToken = default)
            => _fiscal.ConsultarAsync(fiscalOperationId, cancellationToken);

        public static string BuildIdempotencyKey(Guid vendaId) => $"nfe-venda-{vendaId:N}";

        private static string Truncate(string value, int max)
            => string.IsNullOrEmpty(value) ? string.Empty : (value.Length <= max ? value : value[..max]);
    }
}
