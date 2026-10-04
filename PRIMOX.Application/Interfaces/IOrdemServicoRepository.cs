using System;
using System.Threading;
using System.Threading.Tasks;
using PRIMOX.Domain.Entities;

namespace PRIMOX.Application.Interfaces
{
    /// <summary>
    /// Porta de persistência para o Agregado Raiz OrdemServico.
    /// Define apenas operações essenciais para o ciclo transacional do Agregado.
    /// Consultas paginadas são segregadas de operações de escrita.
    /// </summary>
    public interface IOrdemServicoRepository
    {
        Task<OrdemServico?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<string> GerarProximoNumeroAsync(CancellationToken cancellationToken = default);
        Task AdicionarAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);
        Task AtualizarAsync(OrdemServico ordemServico, CancellationToken cancellationToken = default);
    }
}
