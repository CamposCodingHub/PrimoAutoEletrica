using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

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

        public IAssistantProvider ActiveProvider => _provider;

        public AssistantService(
            IKnowledgeRepository? knowledgeRepository = null,
            IOrdemServicoRepository? ordemServicoRepository = null,
            PermissionService? permissionService = null,
            LoggerService? logger = null,
            AppSessionService? sessionService = null,
            AuditLogService? auditLogService = null,
            IAssistantProvider? provider = null)
        {
            _knowledgeRepository = knowledgeRepository ?? App.Repositories.Knowledge;
            _ordemServicoRepository = ordemServicoRepository ?? App.Repositories.OrdensServico;
            _logger = logger ?? App.Logger;
            _sessionService = sessionService ?? App.Session;
            _permissionService = permissionService ?? PermissionService.CriarParaSessaoAtual(_logger);
            _auditLogService = auditLogService ?? App.Audit;
            _provider = provider ?? new GroundedLocalRuleAssistantProvider();
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

            // 1. Extração de palavras-chave para Retrieval (RAG Foundation)
            var tokens = query.Split(new[] { ' ', ',', ';', '.', '?' }, StringSplitOptions.RemoveEmptyEntries)
                              .Where(t => t.Length > 2)
                              .ToList();

            var relevantKnowledge = new List<TechnicalKnowledgeEntry>();
            var relevantCases = new List<DiagnosticCase>();

            // Busca por tokens principais
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

            // 2. Montar Contexto
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

            var context = new AssistantQueryContext
            {
                Query = query,
                Vehicle = vehicleContext,
                WorkOrder = workOrderContext,
                RetrievedKnowledge = relevantKnowledge,
                RetrievedCases = relevantCases
            };

            // 3. Executar consulta através da abstração do provedor
            var response = await _provider.AskAsync(context, ct).ConfigureAwait(false);

            _auditLogService.Registrar(
                categoria: "Assist",
                acao: "Consulta",
                entidade: "AssistantQuery",
                entidadeId: (osId ?? veiculoId)?.ToString() ?? string.Empty,
                detalhes: $"Consulta realizada por '{_sessionService.CurrentUser?.Nome ?? "Técnico"}': '{query}'. Evidência: {response.ConfidenceLevel}, Fontes: {response.CitedSources.Count}.");

            return response;
        }
    }
}
