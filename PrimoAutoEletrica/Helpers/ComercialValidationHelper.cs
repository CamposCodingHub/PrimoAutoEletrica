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
                throw new InvalidOperationException($"Informe {descricao}.");
            }
        }

        public static void GarantirValorMaiorQueZero(decimal valor, string descricao)
        {
            if (valor <= 0)
            {
                throw new InvalidOperationException($"Informe {descricao} maior que zero.");
            }
        }

        public static void GarantirValorMaiorOuIgualZero(decimal valor, string descricao)
        {
            if (valor < 0)
            {
                throw new InvalidOperationException($"Informe {descricao} maior ou igual a zero.");
            }
        }

        public static void GarantirQuantidadeInteiraPositiva(int quantidade, string descricao)
        {
            if (quantidade <= 0)
            {
                throw new InvalidOperationException($"Informe {descricao} maior que zero.");
            }
        }

        public static void GarantirQuantidadePositiva(decimal quantidade, string descricao)
        {
            if (quantidade <= 0)
            {
                throw new InvalidOperationException($"Informe {descricao} maior que zero.");
            }
        }

        public static void GarantirDescontoValido(decimal desconto, decimal baseCalculo, string descricao)
        {
            GarantirValorMaiorOuIgualZero(desconto, descricao);
            GarantirValorMaiorOuIgualZero(baseCalculo, "a base de calculo");

            if (desconto > baseCalculo)
            {
                throw new InvalidOperationException($"{descricao} nao pode ser maior que o valor base.");
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
                throw new InvalidOperationException($"{descricaoFinal} nao pode ser anterior a {descricaoInicial}.");
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
