using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Repositories
{
    public interface IToolRepository
    {
        Task<IReadOnlyList<Tool>> ObterTodosAsync(string? busca = null, string? categoria = null, ToolStatus? status = null, int? responsavelId = null, CancellationToken ct = default);
        Task<Tool?> ObterPorIdAsync(Guid toolId, CancellationToken ct = default);
        Task<Tool?> ObterPorCodigoAsync(string codigo, CancellationToken ct = default);
        Task<bool> InserirAsync(Tool tool, CancellationToken ct = default);
        Task<bool> AtualizarAsync(Tool tool, CancellationToken ct = default);
        Task<bool> ExcluirAsync(Guid toolId, CancellationToken ct = default);

        Task<bool> RegistrarRetiradaAsync(ToolCheckout checkout, CancellationToken ct = default);
        Task<bool> RegistrarDevolucaoAsync(Guid toolId, Guid checkoutId, DateTime dataDevolucao, int usuarioId, ToolCondition condicao, string? observacoes, CancellationToken ct = default);
        Task<IReadOnlyList<ToolCheckout>> ObterHistoricoMovimentacoesAsync(Guid toolId, CancellationToken ct = default);
        Task<ToolCheckout?> ObterCheckoutAtivoAsync(Guid toolId, CancellationToken ct = default);

        Task<bool> InserirManutencaoAsync(ToolMaintenance manutencao, CancellationToken ct = default);
        Task<IReadOnlyList<ToolMaintenance>> ObterManutencoesAsync(Guid toolId, CancellationToken ct = default);
    }
}
