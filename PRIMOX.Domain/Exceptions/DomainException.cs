using System;

namespace PRIMOX.Domain.Exceptions
{
    /// <summary>
    /// Exceção base para todas as violações de regras e invariantes de domínio no PRIMOX.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }

        public DomainException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Lançada quando ocorre tentativa de operar valores com moedas distintas sem conversão explícita.
    /// </summary>
    public class MoedasDivergentesException : DomainException
    {
        public string MoedaOrigem { get; }
        public string MoedaDestino { get; }

        public MoedasDivergentesException(string moedaOrigem, string moedaDestino)
            : base($"Operação monetária inválida entre moedas distintas: '{moedaOrigem}' e '{moedaDestino}'.")
        {
            MoedaOrigem = moedaOrigem;
            MoedaDestino = moedaDestino;
        }
    }

    /// <summary>
    /// Lançada quando uma transição de status na Ordem de Serviço viola a máquina de estados.
    /// </summary>
    public class TransicaoStatusInvalidaException : DomainException
    {
        public string StatusOrigem { get; }
        public string StatusDestino { get; }

        public TransicaoStatusInvalidaException(string statusOrigem, string statusDestino, string motivo = "")
            : base($"Transição de status inválida na Ordem de Serviço: '{statusOrigem}' -> '{statusDestino}'. {motivo}".Trim())
        {
            StatusOrigem = statusOrigem;
            StatusDestino = statusDestino;
        }
    }

    /// <summary>
    /// Lançada quando uma invariante de integridade de dados do agregado é violada.
    /// </summary>
    public class RegraNegocioException : DomainException
    {
        public RegraNegocioException(string message) : base(message)
        {
        }
    }
}
