using System;
using System.Threading;
using System.Threading.Tasks;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.Interfaces;
using PRIMOX.Domain.Entities;
using PRIMOX.Domain.Enums;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.Interfaces;

namespace PRIMOX.Application.UseCases.OrdensServico
{
    public record AbrirOrdemServicoCommand(
        Guid ClienteId,
        Guid VeiculoId,
        string QueixaCliente,
        string? Prioridade = "Normal",
        string? ClienteNome = null,
        string? VeiculoPlaca = null,
        string? VeiculoModelo = null,
        Guid? TenantId = null,
        Guid? FilialId = null);

    public record AbrirOrdemServicoResult(
        bool Sucesso,
        Guid? OrdemServicoId,
        string? Numero,
        OrdemServicoDto? Dados,
        string? MensagemErro);

    public class AbrirOrdemServicoUseCase
    {
        private readonly IOrdemServicoRepository _repository;
        private readonly ITimeProvider _timeProvider;

        public AbrirOrdemServicoUseCase(IOrdemServicoRepository repository, ITimeProvider timeProvider)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<AbrirOrdemServicoResult> ExecutarAsync(
            AbrirOrdemServicoCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
                return new AbrirOrdemServicoResult(false, null, null, null, "Comando não pode ser nulo.");

            if (command.ClienteId == Guid.Empty)
                return new AbrirOrdemServicoResult(false, null, null, null, "Cliente é obrigatório para abertura da OS.");

            if (command.VeiculoId == Guid.Empty)
                return new AbrirOrdemServicoResult(false, null, null, null, "Veículo é obrigatório para abertura da OS.");

            if (string.IsNullOrWhiteSpace(command.QueixaCliente))
                return new AbrirOrdemServicoResult(false, null, null, null, "Queixa principal do cliente é obrigatória.");

            try
            {
                var prioridade = Enum.TryParse<PrioridadeOS>(command.Prioridade, true, out var p)
                    ? p
                    : PrioridadeOS.Normal;

                var numero = await _repository.GerarProximoNumeroAsync(cancellationToken);

                var os = new OrdemServico(
                    Guid.NewGuid(),
                    numero,
                    command.ClienteId,
                    command.VeiculoId,
                    command.QueixaCliente,
                    _timeProvider,
                    prioridade,
                    command.ClienteNome ?? string.Empty,
                    command.VeiculoPlaca ?? string.Empty,
                    command.VeiculoModelo ?? string.Empty,
                    command.TenantId,
                    command.FilialId);

                await _repository.AdicionarAsync(os, cancellationToken);

                return new AbrirOrdemServicoResult(true, os.Id, os.Numero, os.ToDto(), null);
            }
            catch (DomainException ex)
            {
                return new AbrirOrdemServicoResult(false, null, null, null, ex.Message);
            }
            catch (Exception ex)
            {
                return new AbrirOrdemServicoResult(false, null, null, null, $"Falha inesperada ao abrir OS: {ex.Message}");
            }
        }
    }
}
