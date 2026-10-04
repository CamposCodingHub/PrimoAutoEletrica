using System;
using System.Threading;
using System.Threading.Tasks;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.Interfaces;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.ValueObjects;

namespace PRIMOX.Application.UseCases.OrdensServico
{
    public record AdicionarItemPecaCommand(
        Guid OrdemServicoId,
        string Descricao,
        decimal Quantidade,
        decimal ValorUnitario,
        string Codigo = "",
        decimal? CustoUnitario = null,
        Guid? ProdutoId = null,
        string Moeda = "BRL");

    public record AdicionarItemPecaResult(
        bool Sucesso,
        Guid? ItemId,
        OrdemServicoDto? OrdemServicoAtualizada,
        string? MensagemErro);

    public class AdicionarItemPecaUseCase
    {
        private readonly IOrdemServicoRepository _repository;

        public AdicionarItemPecaUseCase(IOrdemServicoRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<AdicionarItemPecaResult> ExecutarAsync(
            AdicionarItemPecaCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
                return new AdicionarItemPecaResult(false, null, null, "Comando não pode ser nulo.");

            if (command.OrdemServicoId == Guid.Empty)
                return new AdicionarItemPecaResult(false, null, null, "OrdemServicoId é obrigatório.");

            if (string.IsNullOrWhiteSpace(command.Descricao))
                return new AdicionarItemPecaResult(false, null, null, "Descrição da peça é obrigatória.");

            if (command.Quantidade <= 0)
                return new AdicionarItemPecaResult(false, null, null, "Quantidade deve ser maior que zero.");

            if (command.ValorUnitario < 0)
                return new AdicionarItemPecaResult(false, null, null, "Valor unitário não pode ser negativo.");

            try
            {
                var os = await _repository.ObterPorIdAsync(command.OrdemServicoId, cancellationToken);
                if (os == null)
                    return new AdicionarItemPecaResult(false, null, null, $"Ordem de Serviço '{command.OrdemServicoId}' não encontrada.");

                var moeda = new Currency(command.Moeda, "R$", "pt-BR");
                var valorUnitario = new Money(command.ValorUnitario, moeda);
                var custoUnitario = command.CustoUnitario.HasValue
                    ? new Money(command.CustoUnitario.Value, moeda)
                    : (Money?)null;

                os.AdicionarPeca(
                    command.Descricao,
                    command.Quantidade,
                    valorUnitario,
                    command.Codigo,
                    custoUnitario,
                    command.ProdutoId);

                await _repository.AtualizarAsync(os, cancellationToken);

                var itemCriado = os.ItensPeca.LastOrDefault();
                return new AdicionarItemPecaResult(true, itemCriado?.Id, os.ToDto(), null);
            }
            catch (DomainException ex)
            {
                return new AdicionarItemPecaResult(false, null, null, ex.Message);
            }
            catch (Exception ex)
            {
                return new AdicionarItemPecaResult(false, null, null, $"Erro ao adicionar peça: {ex.Message}");
            }
        }
    }
}
