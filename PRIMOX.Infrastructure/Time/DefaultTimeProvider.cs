using System;
using PRIMOX.Domain.Interfaces;

namespace PRIMOX.Infrastructure.Time
{
    /// <summary>
    /// Provedor padrão de data e hora baseado no relógio do sistema operacional.
    /// Utilizado em produção. Em testes, substituído por fakes determinísticos.
    /// </summary>
    public class DefaultTimeProvider : ITimeProvider
    {
        public static readonly DefaultTimeProvider Instance = new();

        public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
        public DateTimeOffset GetLocalNow() => DateTimeOffset.Now;
    }
}
