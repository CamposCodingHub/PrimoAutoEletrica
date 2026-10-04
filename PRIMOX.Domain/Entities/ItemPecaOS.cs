using System;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.ValueObjects;

namespace PRIMOX.Domain.Entities
{
    /// <summary>
    /// Item de peça automotiva associado à Ordem de Serviço.
    /// Protege invariantes de quantidade positiva e valores monetários válidos.
    /// </summary>
    public class ItemPecaOS
    {
        public Guid Id { get; private set; }
        public Guid OrdemServicoId { get; private set; }
        public Guid? ProdutoId { get; private set; }
        public string Codigo { get; private set; }
        public string Descricao { get; private set; }
        public decimal Quantidade { get; private set; }
        public Money ValorUnitario { get; private set; }
        public Money CustoUnitario { get; private set; }

        public Money Subtotal => ValorUnitario * Quantidade;

        private ItemPecaOS()
        {
            Codigo = string.Empty;
            Descricao = string.Empty;
        }

        public ItemPecaOS(
            Guid id,
            Guid ordemServicoId,
            string descricao,
            decimal quantidade,
            Money valorUnitario,
            string codigo = "",
            Money? custoUnitario = null,
            Guid? produtoId = null)
        {
            if (id == Guid.Empty) throw new RegraNegocioException("Id do item de peça não pode ser vazio.");
            if (ordemServicoId == Guid.Empty) throw new RegraNegocioException("OrdemServicoId não pode ser vazio.");
            if (string.IsNullOrWhiteSpace(descricao)) throw new RegraNegocioException("Descrição da peça é obrigatória.");
            if (quantidade <= 0) throw new RegraNegocioException($"Quantidade da peça deve ser maior que zero (recebido: {quantidade}).");
            if (valorUnitario.IsNegative) throw new RegraNegocioException("Valor unitário da peça não pode ser negativo.");

            Id = id;
            OrdemServicoId = ordemServicoId;
            Descricao = descricao.Trim();
            Quantidade = quantidade;
            ValorUnitario = valorUnitario;
            Codigo = codigo?.Trim() ?? string.Empty;
            CustoUnitario = custoUnitario ?? Money.Zero(valorUnitario.Currency);
            ProdutoId = produtoId;
        }

        public void AtualizarQuantidade(decimal novaQuantidade)
        {
            if (novaQuantidade <= 0)
                throw new RegraNegocioException($"Quantidade da peça deve ser maior que zero (recebido: {novaQuantidade}).");

            Quantidade = novaQuantidade;
        }

        public void AtualizarValorUnitario(Money novoValor)
        {
            if (novoValor.IsNegative)
                throw new RegraNegocioException("Valor unitário da peça não pode ser negativo.");

            ValorUnitario = novoValor;
        }
    }
}
