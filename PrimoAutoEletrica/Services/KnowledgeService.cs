using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services
{
    public sealed class KnowledgeService : IKnowledgeService
    {
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly PermissionService _permissionService;
        private readonly LoggerService _logger;
        private readonly AppSessionService _sessionService;
        private readonly AuditLogService _auditLogService;

        public KnowledgeService(
            IKnowledgeRepository? knowledgeRepository = null,
            IOrdemServicoRepository? ordemServicoRepository = null,
            PermissionService? permissionService = null,
            LoggerService? logger = null,
            AppSessionService? sessionService = null,
            AuditLogService? auditLogService = null)
        {
            _knowledgeRepository = knowledgeRepository ?? App.Repositories.Knowledge;
            _ordemServicoRepository = ordemServicoRepository ?? App.Repositories.OrdensServico;
            _logger = logger ?? App.Logger;
            _sessionService = sessionService ?? App.Session;
            _permissionService = permissionService ?? PermissionService.CriarParaSessaoAtual(_logger);
            _auditLogService = auditLogService ?? App.Audit;
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

        public async Task<IReadOnlyList<TechnicalKnowledgeEntry>> ListarArtigosAsync(string? busca = null, string? sistema = null, string? tensao = null, KnowledgeStatus? status = null, CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_VER", "visualizar a base técnica de conhecimento");
            return await _knowledgeRepository.ObterArtigosAsync(busca, sistema, tensao, status, ct).ConfigureAwait(false);
        }

        public async Task<TechnicalKnowledgeEntry?> ObterArtigoPorIdAsync(Guid knowledgeId, CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_VER", "visualizar artigo técnico por ID");
            return await _knowledgeRepository.ObterArtigoPorIdAsync(knowledgeId, ct).ConfigureAwait(false);
        }

        public async Task<TechnicalKnowledgeEntry?> ObterArtigoPorCodigoAsync(string codigo, CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_VER", "visualizar artigo técnico por código");
            return await _knowledgeRepository.ObterArtigoPorCodigoAsync(codigo, ct).ConfigureAwait(false);
        }

        public async Task<bool> SalvarArtigoAsync(TechnicalKnowledgeEntry entry, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var existente = await _knowledgeRepository.ObterArtigoPorIdAsync(entry.KnowledgeId, ct).ConfigureAwait(false);
            if (existente == null)
            {
                ExigirPermissao("CONHECIMENTO_CRIAR", "cadastrar novo artigo técnico");
                entry.CreatedAt = DateTime.Now;
                entry.UpdatedAt = DateTime.Now;
                var inserido = await _knowledgeRepository.InserirArtigoAsync(entry, ct).ConfigureAwait(false);
                if (inserido)
                {
                    _auditLogService.Registrar(
                        categoria: "Conhecimento",
                        acao: "Criar",
                        entidade: "TechnicalKnowledgeEntry",
                        entidadeId: entry.KnowledgeId.ToString(),
                        detalhes: $"Artigo '{entry.Code} - {entry.Title}' cadastrado.");
                }
                return inserido;
            }
            else
            {
                ExigirPermissao("CONHECIMENTO_EDITAR", "editar artigo técnico");
                entry.UpdatedAt = DateTime.Now;
                var atualizado = await _knowledgeRepository.AtualizarArtigoAsync(entry, ct).ConfigureAwait(false);
                if (atualizado)
                {
                    _auditLogService.Registrar(
                        categoria: "Conhecimento",
                        acao: "Editar",
                        entidade: "TechnicalKnowledgeEntry",
                        entidadeId: entry.KnowledgeId.ToString(),
                        detalhes: $"Artigo '{entry.Code} - {entry.Title}' atualizado.");
                }
                return atualizado;
            }
        }

        public async Task<bool> ExcluirArtigoAsync(Guid knowledgeId, CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_ARQUIVAR", "excluir ou arquivar artigo técnico");
            var artigo = await _knowledgeRepository.ObterArtigoPorIdAsync(knowledgeId, ct).ConfigureAwait(false);
            if (artigo == null) return false;

            var removido = await _knowledgeRepository.ExcluirArtigoAsync(knowledgeId, ct).ConfigureAwait(false);
            if (removido)
            {
                _auditLogService.Registrar(
                    categoria: "Conhecimento",
                    acao: "Excluir",
                    entidade: "TechnicalKnowledgeEntry",
                    entidadeId: knowledgeId.ToString(),
                    detalhes: $"Artigo '{artigo.Code} - {artigo.Title}' excluído.");
            }
            return removido;
        }

        public async Task<IReadOnlyList<DiagnosticCase>> ListarCasosAsync(string? busca = null, string? sistema = null, Guid? veiculoId = null, Guid? osId = null, CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_VER", "visualizar casos reais de diagnóstico");
            return await _knowledgeRepository.ObterCasosAsync(busca, sistema, veiculoId, osId, ct).ConfigureAwait(false);
        }

        public async Task<DiagnosticCase?> ObterCasoPorIdAsync(Guid caseId, CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_VER", "visualizar caso de diagnóstico por ID");
            return await _knowledgeRepository.ObterCasoPorIdAsync(caseId, ct).ConfigureAwait(false);
        }

        public async Task<bool> SalvarCasoAsync(DiagnosticCase caso, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(caso);

            var existente = await _knowledgeRepository.ObterCasoPorIdAsync(caso.CaseId, ct).ConfigureAwait(false);
            if (existente == null)
            {
                ExigirPermissao("CONHECIMENTO_CRIAR", "registrar caso real de diagnóstico");
                caso.CreatedAt = DateTime.Now;
                caso.UpdatedAt = DateTime.Now;
                var inserido = await _knowledgeRepository.InserirCasoAsync(caso, ct).ConfigureAwait(false);
                if (inserido)
                {
                    _auditLogService.Registrar(
                        categoria: "Conhecimento",
                        acao: "CriarCaso",
                        entidade: "DiagnosticCase",
                        entidadeId: caso.CaseId.ToString(),
                        detalhes: $"Caso técnico '{caso.Code} - {caso.Title}' registrado.");
                }
                return inserido;
            }
            else
            {
                ExigirPermissao("CONHECIMENTO_EDITAR", "editar caso real de diagnóstico");
                caso.UpdatedAt = DateTime.Now;
                var atualizado = await _knowledgeRepository.AtualizarCasoAsync(caso, ct).ConfigureAwait(false);
                if (atualizado)
                {
                    _auditLogService.Registrar(
                        categoria: "Conhecimento",
                        acao: "EditarCaso",
                        entidade: "DiagnosticCase",
                        entidadeId: caso.CaseId.ToString(),
                        detalhes: $"Caso técnico '{caso.Code} - {caso.Title}' atualizado.");
                }
                return atualizado;
            }
        }

        public async Task<bool> ExcluirCasoAsync(Guid caseId, CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_ARQUIVAR", "excluir caso de diagnóstico");
            var caso = await _knowledgeRepository.ObterCasoPorIdAsync(caseId, ct).ConfigureAwait(false);
            if (caso == null) return false;

            var removido = await _knowledgeRepository.ExcluirCasoAsync(caseId, ct).ConfigureAwait(false);
            if (removido)
            {
                _auditLogService.Registrar(
                    categoria: "Conhecimento",
                    acao: "ExcluirCaso",
                    entidade: "DiagnosticCase",
                    entidadeId: caseId.ToString(),
                    detalhes: $"Caso técnico '{caso.Code} - {caso.Title}' removido.");
            }
            return removido;
        }

        public async Task<IReadOnlyList<DiagnosticCase>> ListarCasosPorVeiculoAsync(Guid veiculoId, CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_VER", "visualizar histórico de diagnósticos do veículo");
            return await _knowledgeRepository.ObterCasosPorVeiculoAsync(veiculoId, ct).ConfigureAwait(false);
        }

        public async Task<DiagnosticCase> CriarCasoAPartirDeOSAsync(
            Guid osId,
            string sistema,
            string causaConfirmada,
            string solucao,
            string? medicoes = null,
            string? pecasUtilizadas = null,
            string? dtcCodes = null,
            CancellationToken ct = default)
        {
            ExigirPermissao("CONHECIMENTO_CRIAR", "salvar diagnóstico da OS como caso técnico");

            var os = _ordemServicoRepository.ObterPorId(osId);
            if (os == null)
            {
                throw new InvalidOperationException($"Ordem de Serviço ID {osId} não localizada.");
            }

            var timestamp = DateTime.Now;
            var code = $"CASO-{timestamp:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
            var modeloVeiculo = string.IsNullOrWhiteSpace(os.VeiculoDescricaoSnapshot) ? "Veículo Não Especificado" : os.VeiculoDescricaoSnapshot;
            var tensao = (modeloVeiculo.Contains("Actros", StringComparison.OrdinalIgnoreCase) ||
                          modeloVeiculo.Contains("Scania", StringComparison.OrdinalIgnoreCase) ||
                          modeloVeiculo.Contains("Volvo", StringComparison.OrdinalIgnoreCase) ||
                          modeloVeiculo.Contains("Constellation", StringComparison.OrdinalIgnoreCase) ||
                          modeloVeiculo.Contains("24V", StringComparison.OrdinalIgnoreCase))
                ? "24V"
                : "12V";

            var caso = new DiagnosticCase
            {
                CaseId = Guid.NewGuid(),
                Code = code,
                Title = $"{modeloVeiculo} - {sistema} ({causaConfirmada})",
                VehicleId = os.VeiculoId,
                VehicleModel = modeloVeiculo,
                VehiclePlate = os.PlacaSnapshot,
                WorkOrderId = os.Id,
                WorkOrderNumber = os.Numero,
                TechnicianId = os.TecnicoId,
                TechnicianName = _sessionService.CurrentUser?.Nome ?? "Técnico Responsável",
                System = sistema,
                Voltage = tensao,
                DtcCodes = dtcCodes,
                Symptom = string.IsNullOrWhiteSpace(os.ProblemaRelatado) ? "Sintoma relatado no atendimento" : os.ProblemaRelatado,
                Measurements = medicoes ?? os.DiagnosticoFinal,
                InitialHypotheses = os.DiagnosticoInicial,
                ConfirmedCause = causaConfirmada,
                Solution = solucao,
                PartsUsed = pecasUtilizadas,
                TestResult = "Testado e aprovado em bancada e pista.",
                FinalResult = DiagnosticCaseResult.RESOLVED,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

            var salvo = await SalvarCasoAsync(caso, ct).ConfigureAwait(false);
            if (!salvo)
            {
                throw new InvalidOperationException("Não foi possível salvar o caso técnico.");
            }

            _auditLogService.Registrar(
                categoria: "Conhecimento",
                acao: "PromoverOSParaCaso",
                entidade: "DiagnosticCase",
                entidadeId: caso.CaseId.ToString(),
                detalhes: $"Caso técnico '{caso.Code}' gerado com sucesso a partir da OS '{os.Numero}' ({caso.VehicleModel}).");

            return caso;
        }
    }
}
