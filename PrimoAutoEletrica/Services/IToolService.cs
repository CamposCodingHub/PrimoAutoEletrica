using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public interface IToolService
    {
        Task<IReadOnlyList<Tool>> ListarFerramentasAsync(string? busca = null, string? categoria = null, ToolStatus? status = null, int? responsavelId = null, CancellationToken ct = default);
        Task<Tool?> ObterFerramentaPorIdAsync(Guid toolId, CancellationToken ct = default);
        Task<Tool?> ObterFerramentaPorCodigoAsync(string codigo, CancellationToken ct = default);
        Task<bool> SalvarFerramentaAsync(Tool tool, CancellationToken ct = default);
        Task<bool> ExcluirFerramentaAsync(Guid toolId, CancellationToken ct = default);

        Task<bool> RetirarFerramentaAsync(Guid toolId, int usuarioId, string usuarioNome, Guid? osId = null, string? osNumero = null, string? placa = null, DateTime? previsaoDevolucao = null, string? observacao = null, CancellationToken ct = default);
        Task<bool> DevolverFerramentaAsync(Guid toolId, int usuarioId, ToolCondition condicao, string? observacao = null, CancellationToken ct = default);
        Task<IReadOnlyList<ToolCheckout>> ObterHistoricoMovimentacoesAsync(Guid toolId, CancellationToken ct = default);
        Task<ToolCheckout?> ObterCheckoutAtivoAsync(Guid toolId, CancellationToken ct = default);

        Task<bool> RegistrarManutencaoAsync(Guid toolId, ToolMaintenanceType tipo, string descricao, decimal custo, string? prestador = null, string? observacoes = null, CancellationToken ct = default);
        Task<IReadOnlyList<ToolMaintenance>> ObterManutencoesAsync(Guid toolId, CancellationToken ct = default);
    }
}
