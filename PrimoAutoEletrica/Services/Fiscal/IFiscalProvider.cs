using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Porta de integração fiscal. O domínio PRIMOX depende desta abstração,
    /// nunca de tipos Focus/PlugNotas.
    /// </summary>
    public interface IFiscalProvider
    {
        FiscalProviderKind Kind { get; }

        Task<FiscalProviderResult> EmitirAsync(
            FiscalEmissionRequest request,
            CancellationToken cancellationToken = default);

        Task<FiscalProviderResult> ConsultarAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default);

        Task<FiscalProviderResult> CancelarAsync(
            FiscalCancellationRequest request,
            CancellationToken cancellationToken = default);

        Task<FiscalProviderResult> ObterXmlAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default);

        Task<FiscalProviderResult> ObterDanfeAsync(
            FiscalOperation operation,
            CancellationToken cancellationToken = default);
    }
}
