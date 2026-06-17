using System;

namespace PrimoAutoEletrica.Helpers
{
    public static class ValidationMessages
    {
        public static string Obrigatorio(string descricao) => $"Informe {descricao}.";

        public static string MaiorQueZero(string descricao) => $"Informe {descricao} maior que zero.";

        public static string MaiorOuIgualZero(string descricao) => $"Informe {descricao} maior ou igual a zero.";

        public static string NaoPodeSerMaiorQueBase(string descricao) => $"{descricao} nao pode ser maior que o valor base.";

        public static string DataFinalAnterior(string descricaoFinal, string descricaoInicial) => $"{descricaoFinal} nao pode ser anterior a {descricaoInicial}.";
    }
}
