using System;

namespace PrimoAutoEletrica.Helpers
{
    public static class ComercialValidationHelper
    {
        public static string NormalizarTexto(string? valor, string fallback = "")
        {
            return string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
        }

        public static void GarantirTextoObrigatorio(string? valor, string descricao)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new InvalidOperationException(ValidationMessages.Obrigatorio(descricao));
            }
        }

        public static void GarantirValorMaiorQueZero(decimal valor, string descricao)
        {
            if (valor <= 0)
            {
                throw new InvalidOperationException(ValidationMessages.MaiorQueZero(descricao));
            }
        }

        public static void GarantirValorMaiorOuIgualZero(decimal valor, string descricao)
        {
            if (valor < 0)
            {
                throw new InvalidOperationException(ValidationMessages.MaiorOuIgualZero(descricao));
            }
        }

        public static void GarantirQuantidadeInteiraPositiva(int quantidade, string descricao)
        {
            if (quantidade <= 0)
            {
                throw new InvalidOperationException(ValidationMessages.MaiorQueZero(descricao));
            }
        }

        public static void GarantirQuantidadePositiva(decimal quantidade, string descricao)
        {
            if (quantidade <= 0)
            {
                throw new InvalidOperationException(ValidationMessages.MaiorQueZero(descricao));
            }
        }

        public static void GarantirDescontoValido(decimal desconto, decimal baseCalculo, string descricao)
        {
            GarantirValorMaiorOuIgualZero(desconto, descricao);
            GarantirValorMaiorOuIgualZero(baseCalculo, "a base de calculo");

            if (desconto > baseCalculo)
            {
                throw new InvalidOperationException(ValidationMessages.NaoPodeSerMaiorQueBase(descricao));
            }
        }

        public static void GarantirDataFinalNaoAnterior(DateTime? dataInicial, DateTime? dataFinal, string descricaoInicial, string descricaoFinal)
        {
            if (!dataInicial.HasValue || !dataFinal.HasValue)
            {
                return;
            }

            if (dataFinal.Value < dataInicial.Value)
            {
                throw new InvalidOperationException(ValidationMessages.DataFinalAnterior(descricaoFinal, descricaoInicial));
            }
        }

        public static decimal CalcularSubtotal(decimal quantidade, decimal valorUnitario, decimal desconto)
        {
            GarantirQuantidadePositiva(quantidade, "a quantidade");
            GarantirValorMaiorOuIgualZero(valorUnitario, "o valor unitario");

            var valorBruto = quantidade * valorUnitario;
            GarantirDescontoValido(desconto, valorBruto, "O desconto");
            return valorBruto - desconto;
        }
    }
}
