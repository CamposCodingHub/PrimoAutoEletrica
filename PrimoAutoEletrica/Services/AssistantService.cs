using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services.Knowledge;

namespace PrimoAutoEletrica.Services
{
    public interface IAssistantService
    {
        IAssistantProvider ActiveProvider { get; }
        Task<AssistantResponse> ConsultarAsync(string query, Guid? veiculoId = null, Guid? osId = null, CancellationToken ct = default);
    }

    public sealed class AssistantService : IAssistantService
    {
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly PermissionService _permissionService;
        private readonly LoggerService _logger;
        private readonly AppSessionService _sessionService;
        private readonly AuditLogService _auditLogService;
        private readonly IAssistantProvider _provider;
        private readonly IKnowledgeRetrievalService? _retrieval;
        private readonly AssistContextBuilder? _contextBuilder;

        public IAssistantProvider ActiveProvider => _provider;

        public AssistantService(
            IKnowledgeRepository? knowledgeRepository = null,
            IOrdemServicoRepository? ordemServicoRepository = null,
            PermissionService? permissionService = null,
            LoggerService? logger = null,
            AppSessionService? sessionService = null,
            AuditLogService? auditLogService = null,
            IAssistantProvider? provider = null,
            IKnowledgeRetrievalService? retrieval = null)
        {
            _knowledgeRepository = knowledgeRepository ?? App.Repositories.Knowledge;
            _ordemServicoRepository = ordemServicoRepository ?? App.Repositories.OrdensServico;
            _logger = logger ?? App.Logger;
            _sessionService = sessionService ?? App.Session;
            _permissionService = permissionService ?? PermissionService.CriarParaSessaoAtual(_logger);
            _auditLogService = auditLogService ?? App.Audit;
            _provider = provider ?? new GroundedLocalRuleAssistantProvider();
            _retrieval = retrieval;
            if (_retrieval != null)
            {
                _contextBuilder = new AssistContextBuilder(_retrieval, _ordemServicoRepository);
            }
        }

        private void ExigirPermissao(string codigoPermissao, string operacao)
        {
            if (!_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                var usuario = _sessionService.CurrentUser?.Nome ?? "Anônimo";
                _logger.LogWarning($"Permissão negada para '{operacao}'. Usuário: {usuario}, Permissão requerida: {codigoPermissao}");
                throw new UnauthorizedAccessException($"Acesso negado: o perfil atual não possui permissão para {operacao}. ({codigoPermissao})");
            }
        }

        public async Task<AssistantResponse> ConsultarAsync(string query, Guid? veiculoId = null, Guid? osId = null, CancellationToken ct = default)
        {
            ExigirPermissao("ASSIST_UTILIZAR", "utilizar o assistente técnico PRIMOX Assist");

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("A consulta técnica não pode ser vazia.", nameof(query));
            }

            // Fail-closed pré-provider (F2/F3/F5; F4 requer ClienteId explícito no context — cheap stub)
            var hasFinance = _permissionService.TemPermissaoCodigo("FINANCEIRO_VER")
                             || _permissionService.TemPermissaoCodigo("FINANCEIRO_ACESSAR")
                             || string.Equals(_sessionService.CurrentUser?.PerfilAcesso, "Administrador", StringComparison.OrdinalIgnoreCase);

            var pre = AssistFailClosedPolicy.EvaluatePreProvider(
                query,
                hasFinancePermission: hasFinance,
                sessionClienteId: null,
                contextClienteId: null,
                providerConfigured: _provider.IsConfigured);

            if (pre != null)
            {
                _auditLogService.Registrar(
                    categoria: "Assist",
                    acao: "FailClosed",
                    entidade: "AssistantQuery",
                    entidadeId: (osId ?? veiculoId)?.ToString() ?? string.Empty,
                    detalhes: $"Fail-closed: {string.Join(',', pre.Warnings)}. Query hash len={query.Length}.");
                return AssistantContractMapper.Enrich(pre, _provider.ProviderId);
            }

            AssistantQueryContext context;
            IReadOnlyList<EvidenceItem> evidenceFromRetrieval = Array.Empty<EvidenceItem>();

            if (_contextBuilder != null)
            {
                context = await _contextBuilder.BuildAsync(query, veiculoId, osId, ct: ct).ConfigureAwait(false);
                evidenceFromRetrieval = context.RetrievedEvidence;

                // Também popula slots legados via repositório (compat AssistFoundation / provider)
                var relevantKnowledge = new List<TechnicalKnowledgeEntry>();
                var relevantCases = new List<DiagnosticCase>();
                foreach (var hit in (await _retrieval!.SearchAsync(query, maxResults: 12, ct: ct).ConfigureAwait(false)).Hits)
                {
                    if (hit.Item.Type == KnowledgeType.TECHNICAL_CASE && Guid.TryParse(hit.Item.SourceEntityId, out var kid))
                    {
                        var art = await _knowledgeRepository.ObterArtigoPorIdAsync(kid, ct).ConfigureAwait(false);
                        if (art != null && relevantKnowledge.All(x => x.KnowledgeId != art.KnowledgeId))
                            relevantKnowledge.Add(art);
                    }
                    else if (hit.Item.Type == KnowledgeType.DIAGNOSTIC_CASE && Guid.TryParse(hit.Item.SourceEntityId, out var cid))
                    {
                        var caso = await _knowledgeRepository.ObterCasoPorIdAsync(cid, ct).ConfigureAwait(false);
                        if (caso != null && relevantCases.All(x => x.CaseId != caso.CaseId))
                            relevantCases.Add(caso);
                    }
                }

                context = new AssistantQueryContext
                {
                    Query = context.Query,
                    Vehicle = context.Vehicle,
                    WorkOrder = context.WorkOrder,
                    Measurements = context.Measurements,
                    RetrievedKnowledge = relevantKnowledge,
                    RetrievedCases = relevantCases,
                    RetrievedEvidence = evidenceFromRetrieval,
                    ClienteId = context.ClienteId,
                    AllowedClasses = context.AllowedClasses,
                    Parameters = context.Parameters
                };
            }
            else
            {
                // Caminho legado (token-split) — preserva AssistFoundationTests sem retrieval injetado
                var tokens = query.Split(new[] { ' ', ',', ';', '.', '?' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Where(t => t.Length > 2)
                                  .ToList();

                var relevantKnowledge = new List<TechnicalKnowledgeEntry>();
                var relevantCases = new List<DiagnosticCase>();

                foreach (var token in tokens)
                {
                    var kbs = await _knowledgeRepository.ObterArtigosAsync(busca: token, ct: ct).ConfigureAwait(false);
                    foreach (var k in kbs)
                    {
                        if (!relevantKnowledge.Any(x => x.KnowledgeId == k.KnowledgeId))
                        {
                            relevantKnowledge.Add(k);
                        }
                    }

                    var casos = await _knowledgeRepository.ObterCasosAsync(busca: token, ct: ct).ConfigureAwait(false);
                    foreach (var c in casos)
                    {
                        if (!relevantCases.Any(x => x.CaseId == c.CaseId))
                        {
                            relevantCases.Add(c);
                        }
                    }
                }

                AssistantVehicleContext? vehicleContext = null;
                AssistantWorkOrderContext? workOrderContext = null;

                if (osId.HasValue)
                {
                    var os = _ordemServicoRepository.ObterPorId(osId.Value);
                    if (os != null)
                    {
                        workOrderContext = new AssistantWorkOrderContext
                        {
                            Number = os.Numero,
                            Symptom = os.ProblemaRelatado,
                            Status = os.Status,
                            CurrentItems = os.Itens?.Select(i => i.Descricao).ToList() ?? new List<string>()
                        };

                        if (!string.IsNullOrWhiteSpace(os.VeiculoDescricaoSnapshot))
                        {
                            var tensao = os.VeiculoDescricaoSnapshot.Contains("Actros", StringComparison.OrdinalIgnoreCase) ||
                                         os.VeiculoDescricaoSnapshot.Contains("24V", StringComparison.OrdinalIgnoreCase)
                                ? "24V"
                                : "12V";

                            vehicleContext = new AssistantVehicleContext
                            {
                                Plate = os.PlacaSnapshot,
                                Model = os.VeiculoDescricaoSnapshot,
                                Voltage = tensao
                            };
                        }
                    }
                }

                context = new AssistantQueryContext
                {
                    Query = query,
                    Vehicle = vehicleContext,
                    WorkOrder = workOrderContext,
                    RetrievedKnowledge = relevantKnowledge,
                    RetrievedCases = relevantCases
                };
            }

            // F1: sem evidência e sem entidades → fail-closed (ainda deixa provider rodar se houver KB/casos)
            if (context.RetrievedKnowledge.Count == 0 &&
                context.RetrievedCases.Count == 0 &&
                (evidenceFromRetrieval == null || evidenceFromRetrieval.Count == 0))
            {
                // Provider local ainda pode responder INSUFFICIENT; enrich depois
            }

            var response = await _provider.AskAsync(context, ct).ConfigureAwait(false);

            // Se retrieval trouxe evidência de procedimentos mas provider ficou INSUFFICIENT por falta de KB/casos,
            // mantemos a honestidade do provider; Evidence da retrieval é anexada via mapper.
            response = AssistantContractMapper.Enrich(
                response,
                providerId: _provider.ProviderId,
                evidence: evidenceFromRetrieval.Count > 0 ? evidenceFromRetrieval : null);

            _auditLogService.Registrar(
                categoria: "Assist",
                acao: "Consulta",
                entidade: "AssistantQuery",
                entidadeId: (osId ?? veiculoId)?.ToString() ?? string.Empty,
                detalhes: $"Consulta realizada por '{_sessionService.CurrentUser?.Nome ?? "Técnico"}': evidência={response.ConfidenceLevel}, fontes={response.CitedSources.Count}, evidenceItems={response.Evidence.Count}, provider={response.Provider}.");

            return response;
        }
    }
}
