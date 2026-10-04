using System;

namespace PRIMOX.Domain.Interfaces
{
    /// <summary>
    /// Abstração para obtenção de data e hora no ecossistema PRIMOX.
    /// Permite testes unitários e determinação precisa de tempo sem acoplamento ao relógio do SO.
    /// </summary>
    public interface ITimeProvider
    {
        DateTimeOffset GetUtcNow();
        DateTimeOffset GetLocalNow();
    }
}
