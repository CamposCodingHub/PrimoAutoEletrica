using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.Services
{
    public sealed class ToolService : IToolService
    {
        private readonly IToolRepository _toolRepository;
        private readonly PermissionService _permissionService;
        private readonly LoggerService _logger;
        private readonly AppSessionService _sessionService;
        private readonly AuditLogService _auditLogService;

        public ToolService(
            IToolRepository? toolRepository = null,
            PermissionService? permissionService = null,
            LoggerService? logger = null,
            AppSessionService? sessionService = null,
            AuditLogService? auditLogService = null)
        {
            _toolRepository = toolRepository ?? App.Repositories.Tools;
            _logger = logger ?? App.Logger;
            _permissionService = permissionService ?? PermissionService.CriarParaSessaoAtual(_logger);
            _sessionService = sessionService ?? App.Session;
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

        public async Task<IReadOnlyList<Tool>> ListarFerramentasAsync(string? busca = null, string? categoria = null, ToolStatus? status = null, int? responsavelId = null, CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_VER", "visualizar o inventário de ferramentas");
            return await _toolRepository.ObterTodosAsync(busca, categoria, status, responsavelId, ct).ConfigureAwait(false);
        }

        public async Task<Tool?> ObterFerramentaPorIdAsync(Guid toolId, CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_VER", "visualizar detalhes da ferramenta");
            return await _toolRepository.ObterPorIdAsync(toolId, ct).ConfigureAwait(false);
        }

        public async Task<Tool?> ObterFerramentaPorCodigoAsync(string codigo, CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_VER", "visualizar ferramenta por código");
            return await _toolRepository.ObterPorCodigoAsync(codigo, ct).ConfigureAwait(false);
        }

        public async Task<bool> SalvarFerramentaAsync(Tool tool, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(tool);

            var existente = await _toolRepository.ObterPorIdAsync(tool.ToolId, ct).ConfigureAwait(false);
            if (existente == null)
            {
                ExigirPermissao("FERRAMENTAS_CRIAR", "cadastrar nova ferramenta no patrimônio");
                tool.CreatedAt = DateTime.Now;
                tool.UpdatedAt = DateTime.Now;
                var inserido = await _toolRepository.InserirAsync(tool, ct).ConfigureAwait(false);
                if (inserido)
                {
                    _auditLogService.Registrar(
                        categoria: "Ferramentas",
                        acao: "Criar",
                        entidade: "Tool",
                        entidadeId: tool.ToolId.ToString(),
                        detalhes: $"Ferramenta '{tool.Code} - {tool.Name}' cadastrada no patrimônio.");
                }
                return inserido;
            }
            else
            {
                ExigirPermissao("FERRAMENTAS_EDITAR", "editar dados de ferramenta existente");
                tool.UpdatedAt = DateTime.Now;
                var atualizado = await _toolRepository.AtualizarAsync(tool, ct).ConfigureAwait(false);
                if (atualizado)
                {
                    _auditLogService.Registrar(
                        categoria: "Ferramentas",
                        acao: "Editar",
                        entidade: "Tool",
                        entidadeId: tool.ToolId.ToString(),
                        detalhes: $"Ferramenta '{tool.Code} - {tool.Name}' atualizada.");
                }
                return atualizado;
            }
        }

        public async Task<bool> ExcluirFerramentaAsync(Guid toolId, CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_EXCLUIR", "excluir ferramenta do patrimônio");

            var tool = await _toolRepository.ObterPorIdAsync(toolId, ct).ConfigureAwait(false);
            if (tool == null)
            {
                return false;
            }

            if (tool.Status == ToolStatus.IN_USE)
            {
                throw new InvalidOperationException($"A ferramenta '{tool.Code} - {tool.Name}' está atualmente em uso e não pode ser excluída.");
            }

            var removido = await _toolRepository.ExcluirAsync(toolId, ct).ConfigureAwait(false);
            if (removido)
            {
                _auditLogService.Registrar(
                    categoria: "Ferramentas",
                    acao: "Excluir",
                    entidade: "Tool",
                    entidadeId: tool.ToolId.ToString(),
                    detalhes: $"Ferramenta '{tool.Code} - {tool.Name}' removida do inventário.");
            }

            return removido;
        }

        public async Task<bool> RetirarFerramentaAsync(
            Guid toolId,
            int usuarioId,
            string usuarioNome,
            Guid? osId = null,
            string? osNumero = null,
            string? placa = null,
            DateTime? previsaoDevolucao = null,
            string? observacao = null,
            CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_RETIRAR", "registrar retirada de ferramenta");

            var tool = await _toolRepository.ObterPorIdAsync(toolId, ct).ConfigureAwait(false);
            if (tool == null)
            {
                throw new InvalidOperationException("Ferramenta não localizada no inventário.");
            }

            if (tool.Status != ToolStatus.AVAILABLE)
            {
                throw new InvalidOperationException($"A ferramenta '{tool.Code} - {tool.Name}' não está disponível para retirada (Situação: {tool.StatusDisplay}).");
            }

            var checkout = new ToolCheckout
            {
                CheckoutId = Guid.NewGuid(),
                ToolId = toolId,
                UserId = usuarioId,
                UserName = usuarioNome,
                WorkOrderId = osId,
                WorkOrderNumber = osNumero,
                VehiclePlate = placa,
                CheckoutDate = DateTime.Now,
                ExpectedReturnDate = previsaoDevolucao,
                CheckoutNotes = observacao,
                Status = ToolCheckoutStatus.OPEN,
                CreatedAt = DateTime.Now
            };

            var sucesso = await _toolRepository.RegistrarRetiradaAsync(checkout, ct).ConfigureAwait(false);
            if (!sucesso)
            {
                throw new InvalidOperationException("Esta ferramenta já está em uso por outro usuário ou sua situação foi alterada concorrentemente. Atualize e tente novamente.");
            }

            _auditLogService.Registrar(
                categoria: "Ferramentas",
                acao: "Retirada",
                entidade: "Tool",
                entidadeId: tool.ToolId.ToString(),
                detalhes: $"Ferramenta '{tool.Code}' retirada por '{usuarioNome}' (OS: {osNumero ?? "N/A"}, Placa: {placa ?? "N/A"}).");

            return true;
        }

        public async Task<bool> DevolverFerramentaAsync(
            Guid toolId,
            int usuarioId,
            ToolCondition condicao,
            string? observacao = null,
            CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_DEVOLVER", "registrar devolução de ferramenta");

            var tool = await _toolRepository.ObterPorIdAsync(toolId, ct).ConfigureAwait(false);
            if (tool == null)
            {
                throw new InvalidOperationException("Ferramenta não localizada no inventário.");
            }

            var checkoutAtivo = await _toolRepository.ObterCheckoutAtivoAsync(toolId, ct).ConfigureAwait(false);
            if (checkoutAtivo == null)
            {
                throw new InvalidOperationException($"Não há registro de retirada aberta para a ferramenta '{tool.Code} - {tool.Name}'.");
            }

            var dataDevolucao = DateTime.Now;
            var sucesso = await _toolRepository.RegistrarDevolucaoAsync(toolId, checkoutAtivo.CheckoutId, dataDevolucao, usuarioId, condicao, observacao, ct).ConfigureAwait(false);
            if (!sucesso)
            {
                throw new InvalidOperationException("Não foi possível concluir a devolução. Atualize a situação da ferramenta e tente novamente.");
            }

            var condicaoTexto = condicao switch
            {
                ToolCondition.OK => "em perfeito estado",
                ToolCondition.DAMAGED => "AVARIADA / DANIFICADA",
                ToolCondition.NEEDS_CALIBRATION => "necessitando aferição / calibração",
                ToolCondition.DIRTY => "entregue suja",
                ToolCondition.MISSING_ACCESSORIES => "faltando pontas / acessórios",
                _ => condicao.ToString()
            };

            _auditLogService.Registrar(
                categoria: "Ferramentas",
                acao: "Devolucao",
                entidade: "Tool",
                entidadeId: tool.ToolId.ToString(),
                detalhes: $"Ferramenta '{tool.Code}' devolvida por usuário ID {usuarioId} com condição '{condicaoTexto}'. Obs: {observacao ?? "Nenhum apontamento"}.");

            return true;
        }

        public async Task<IReadOnlyList<ToolCheckout>> ObterHistoricoMovimentacoesAsync(Guid toolId, CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_VER", "visualizar histórico de movimentações");
            return await _toolRepository.ObterHistoricoMovimentacoesAsync(toolId, ct).ConfigureAwait(false);
        }

        public async Task<ToolCheckout?> ObterCheckoutAtivoAsync(Guid toolId, CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_VER", "visualizar custódia ativa");
            return await _toolRepository.ObterCheckoutAtivoAsync(toolId, ct).ConfigureAwait(false);
        }

        public async Task<bool> RegistrarManutencaoAsync(
            Guid toolId,
            ToolMaintenanceType tipo,
            string descricao,
            decimal custo,
            string? prestador = null,
            string? observacoes = null,
            CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_MANUTENCAO", "registrar manutenção / calibração de ferramenta");

            var tool = await _toolRepository.ObterPorIdAsync(toolId, ct).ConfigureAwait(false);
            if (tool == null)
            {
                throw new InvalidOperationException("Ferramenta não localizada no inventário.");
            }

            var manutencao = new ToolMaintenance
            {
                MaintenanceId = Guid.NewGuid(),
                ToolId = toolId,
                MaintenanceType = tipo,
                Description = descricao,
                CostCents = MoneyCents.FromDecimal(custo).Cents,
                Provider = prestador,
                StartDate = DateTime.Now,
                CompletionDate = DateTime.Now,
                PerformedBy = _sessionService.CurrentUser?.Nome ?? "Técnico Responsável",
                Status = MaintenanceStatus.COMPLETED,
                Notes = observacoes,
                CreatedAt = DateTime.Now
            };

            var sucesso = await _toolRepository.InserirManutencaoAsync(manutencao, ct).ConfigureAwait(false);
            if (sucesso)
            {
                var custoFormatado = custo.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
                _auditLogService.Registrar(
                    categoria: "Ferramentas",
                    acao: "Manutencao",
                    entidade: "Tool",
                    entidadeId: tool.ToolId.ToString(),
                    detalhes: $"Manutenção '{tipo}' registrada para ferramenta '{tool.Code}'. Custo: {custoFormatado}. Prestador: {prestador ?? "Interno"}.");
            }

            return sucesso;
        }

        public async Task<IReadOnlyList<ToolMaintenance>> ObterManutencoesAsync(Guid toolId, CancellationToken ct = default)
        {
            ExigirPermissao("FERRAMENTAS_VER", "visualizar histórico de manutenções");
            return await _toolRepository.ObterManutencoesAsync(toolId, ct).ConfigureAwait(false);
        }
    }
}
