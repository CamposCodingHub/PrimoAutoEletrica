using System;
using System.Threading;
using System.Threading.Tasks;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.Interfaces;
using PRIMOX.Domain.Enums;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.Interfaces;

namespace PRIMOX.Application.UseCases.OrdensServico
{
    public record AlterarStatusOrdemServicoCommand(
        Guid OrdemServicoId,
        string NovoStatus,
        string Motivo,
        string Responsavel);

    public record AlterarStatusOrdemServicoResult(
        bool Sucesso,
        string? StatusAnterior,
        string? StatusAtual,
        OrdemServicoDto? OrdemServicoAtualizada,
        string? MensagemErro);

    public class AlterarStatusOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _repository;
        private readonly ITimeProvider _timeProvider;

        public AlterarStatusOrdemServicoUseCase(
            IOrdemServicoRepository repository,
            ITimeProvider timeProvider)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<AlterarStatusOrdemServicoResult> ExecutarAsync(
            AlterarStatusOrdemServicoCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
                return new AlterarStatusOrdemServicoResult(false, null, null, null, "Comando não pode ser nulo.");

            if (command.OrdemServicoId == Guid.Empty)
                return new AlterarStatusOrdemServicoResult(false, null, null, null, "OrdemServicoId é obrigatório.");

            if (string.IsNullOrWhiteSpace(command.NovoStatus))
                return new AlterarStatusOrdemServicoResult(false, null, null, null, "Novo status é obrigatório.");

            if (!Enum.TryParse<StatusOrdemServico>(command.NovoStatus, true, out var novoStatusEnum))
                return new AlterarStatusOrdemServicoResult(false, null, null, null, $"Status '{command.NovoStatus}' não é reconhecido pelo sistema.");

            try
            {
                var os = await _repository.ObterPorIdAsync(command.OrdemServicoId, cancellationToken);
                if (os == null)
                    return new AlterarStatusOrdemServicoResult(false, null, null, null, $"Ordem de Serviço '{command.OrdemServicoId}' não encontrada.");

                var statusAnterior = os.Status.ToString();

                os.AlterarStatus(
                    novoStatusEnum,
                    command.Motivo,
                    command.Responsavel,
                    _timeProvider);

                await _repository.AtualizarAsync(os, cancellationToken);

                return new AlterarStatusOrdemServicoResult(
                    true,
                    statusAnterior,
                    os.Status.ToString(),
                    os.ToDto(),
                    null);
            }
            catch (DomainException ex)
            {
                return new AlterarStatusOrdemServicoResult(false, null, null, null, ex.Message);
            }
            catch (Exception ex)
            {
                return new AlterarStatusOrdemServicoResult(false, null, null, null, $"Erro ao alterar status: {ex.Message}");
            }
        }
    }
}
