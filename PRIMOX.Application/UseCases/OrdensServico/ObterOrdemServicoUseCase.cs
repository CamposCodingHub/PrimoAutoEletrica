using System;
using System.Threading;
using System.Threading.Tasks;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.Interfaces;

namespace PRIMOX.Application.UseCases.OrdensServico
{
    public record ObterOrdemServicoQuery(Guid Id);

    public record ObterOrdemServicoResult(
        bool Sucesso,
        OrdemServicoDto? Dados,
        string? MensagemErro);

    public class ObterOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _repository;

        public ObterOrdemServicoUseCase(IOrdemServicoRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<ObterOrdemServicoResult> ExecutarAsync(
            ObterOrdemServicoQuery query,
            CancellationToken cancellationToken = default)
        {
            if (query == null || query.Id == Guid.Empty)
                return new ObterOrdemServicoResult(false, null, "Id da Ordem de Serviço inválido.");

            try
            {
                var os = await _repository.ObterPorIdAsync(query.Id, cancellationToken);
                if (os == null)
                    return new ObterOrdemServicoResult(false, null, $"Ordem de Serviço '{query.Id}' não encontrada.");

                return new ObterOrdemServicoResult(true, os.ToDto(), null);
            }
            catch (Exception ex)
            {
                return new ObterOrdemServicoResult(false, null, $"Erro ao consultar OS: {ex.Message}");
            }
        }
    }
}
