using System;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.ValueObjects;

namespace PRIMOX.Domain.Entities
{
    /// <summary>
    /// Item de mão de obra / serviço técnico associado à Ordem de Serviço.
    /// </summary>
    public class ItemServicoOS
    {
        public Guid Id { get; private set; }
        public Guid OrdemServicoId { get; private set; }
        public Guid? ServicoId { get; private set; }
        public string Descricao { get; private set; }
        public decimal QuantidadeHoras { get; private set; }
        public Money ValorHora { get; private set; }
        public string? TecnicoResponsavel { get; private set; }

        public Money Subtotal => ValorHora * QuantidadeHoras;

        private ItemServicoOS()
        {
            Descricao = string.Empty;
        }

        public ItemServicoOS(
            Guid id,
            Guid ordemServicoId,
            string descricao,
            decimal quantidadeHoras,
            Money valorHora,
            string? tecnicoResponsavel = null,
            Guid? servicoId = null)
        {
            if (id == Guid.Empty) throw new RegraNegocioException("Id do item de serviço não pode ser vazio.");
            if (ordemServicoId == Guid.Empty) throw new RegraNegocioException("OrdemServicoId não pode ser vazio.");
            if (string.IsNullOrWhiteSpace(descricao)) throw new RegraNegocioException("Descrição do serviço é obrigatória.");
            if (quantidadeHoras <= 0) throw new RegraNegocioException($"Quantidade de horas de serviço deve ser maior que zero (recebido: {quantidadeHoras}).");
            if (valorHora.IsNegative) throw new RegraNegocioException("Valor da hora do serviço não pode ser negativo.");

            Id = id;
            OrdemServicoId = ordemServicoId;
            Descricao = descricao.Trim();
            QuantidadeHoras = quantidadeHoras;
            ValorHora = valorHora;
            TecnicoResponsavel = string.IsNullOrWhiteSpace(tecnicoResponsavel) ? null : tecnicoResponsavel.Trim();
            ServicoId = servicoId;
        }

        public void AtualizarHoras(decimal novasHoras)
        {
            if (novasHoras <= 0)
                throw new RegraNegocioException($"Quantidade de horas de serviço deve ser maior que zero (recebido: {novasHoras}).");

            QuantidadeHoras = novasHoras;
        }

        public void AtualizarValorHora(Money novoValor)
        {
            if (novoValor.IsNegative)
                throw new RegraNegocioException("Valor da hora do serviço não pode ser negativo.");

            ValorHora = novoValor;
        }
    }
}
