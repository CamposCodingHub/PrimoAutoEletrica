using PrimoAutoEletrica.Models.AI;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public interface IAIService
    {
        string ProviderName { get; }
        bool IsOnlineAvailable { get; }

        Task<AIChatResponse> ProcessarMensagemAsync(AIChatRequest request, CancellationToken cancellationToken = default);
    }
}
