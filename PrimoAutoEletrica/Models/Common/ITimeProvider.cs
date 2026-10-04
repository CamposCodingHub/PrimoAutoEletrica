using System;

namespace PrimoAutoEletrica.Models.Common
{
    /// <summary>
    /// Abstração de tempo do sistema para desacoplar a regra de negócio de DateTime.Now.
    /// Permite testes previsíveis, auditoria em UTC e suporte a múltiplos fusos horários.
    /// </summary>
    public interface ITimeProvider
    {
        DateTimeOffset UtcNow { get; }
        DateTimeOffset LocalNow { get; }
        TimeZoneInfo LocalTimeZone { get; }
    }

    /// <summary>
    /// Implementação padrão de ITimeProvider baseada no relógio do sistema.
    /// </summary>
    public sealed class DefaultTimeProvider : ITimeProvider
    {
        public static readonly DefaultTimeProvider Instance = new();

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateTimeOffset LocalNow => DateTimeOffset.Now;
        public TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
    }
}
