using System;
using System.Threading;
using System.Threading.Tasks;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.Interfaces;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.ValueObjects;

namespace PRIMOX.Application.UseCases.OrdensServico
{
    public record AdicionarItemServicoCommand(
        Guid OrdemServicoId,
        string Descricao,
        decimal QuantidadeHoras,
        decimal ValorHora,
        string? TecnicoResponsavel = null,
        Guid? ServicoId = null,
        string Moeda = "BRL");

    public record AdicionarItemServicoResult(
        bool Sucesso,
        Guid? ItemId,
        OrdemServicoDto? OrdemServicoAtualizada,
        string? MensagemErro);

    public class AdicionarItemServicoUseCase
    {
        private readonly IOrdemServicoRepository _repository;

        public AdicionarItemServicoUseCase(IOrdemServicoRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<AdicionarItemServicoResult> ExecutarAsync(
            AdicionarItemServicoCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
                return new AdicionarItemServicoResult(false, null, null, "Comando não pode ser nulo.");

            if (command.OrdemServicoId == Guid.Empty)
                return new AdicionarItemServicoResult(false, null, null, "OrdemServicoId é obrigatório.");

            if (string.IsNullOrWhiteSpace(command.Descricao))
                return new AdicionarItemServicoResult(false, null, null, "Descrição do serviço é obrigatória.");

            if (command.QuantidadeHoras <= 0)
                return new AdicionarItemServicoResult(false, null, null, "Horas devem ser maiores que zero.");

            if (command.ValorHora < 0)
                return new AdicionarItemServicoResult(false, null, null, "Valor da hora não pode ser negativo.");

            try
            {
                var os = await _repository.ObterPorIdAsync(command.OrdemServicoId, cancellationToken);
                if (os == null)
                    return new AdicionarItemServicoResult(false, null, null, $"Ordem de Serviço '{command.OrdemServicoId}' não encontrada.");

                var moeda = new Currency(command.Moeda, "R$", "pt-BR");
                var valorHora = new Money(command.ValorHora, moeda);

                os.AdicionarServico(
                    command.Descricao,
                    command.QuantidadeHoras,
                    valorHora,
                    command.TecnicoResponsavel,
                    command.ServicoId);

                await _repository.AtualizarAsync(os, cancellationToken);

                var itemCriado = os.ItensServico.LastOrDefault();
                return new AdicionarItemServicoResult(true, itemCriado?.Id, os.ToDto(), null);
            }
            catch (DomainException ex)
            {
                return new AdicionarItemServicoResult(false, null, null, ex.Message);
            }
            catch (Exception ex)
            {
                return new AdicionarItemServicoResult(false, null, null, $"Erro ao adicionar serviço: {ex.Message}");
            }
        }
    }
}
