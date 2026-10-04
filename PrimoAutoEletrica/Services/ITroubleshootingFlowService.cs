using PrimoAutoEletrica.Models.DiagnosticoGuiado;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public interface ITroubleshootingFlowService
    {
        Task<List<FluxogramaDiagnostico>> ObterFluxogramasAsync(string? categoria = null, string? termo = null);
        Task<FluxogramaDiagnostico?> ObterFluxogramaPorCodigoAsync(string codigo);
        Task<FluxogramaPasso?> ObterPassoAsync(int fluxogramaId, int passoNumero);
        Task<FluxogramaDiagnostico?> BuscarPorSintomaOuDtcAsync(string sintomaOuDtc);
        Task<bool> GarantirCargaInicialAsync();
    }
}
