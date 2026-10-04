using PrimoAutoEletrica.Models.BibliotecaTecnica;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public interface IBibliotecaTecnicaService
    {
        Task<List<PinagemModulo>> ObterModulosAsync(string? termoBusca = null, string? montadora = null, string? tensao = null);
        Task<PinagemModulo?> ObterModuloPorCodigoAsync(string codigo);
        Task<List<CentralEletricaFusivel>> ObterCentraisEletricasAsync(string? termoBusca = null, string? montadora = null, string? tensao = null);
        Task<CentralEletricaFusivel?> ObterCentralPorCodigoAsync(string codigo);
        Task<List<VeiculoPesado24VEspecificacao>> ObterEspecificacoesPesadosAsync(string? termoBusca = null);
        Task<VeiculoPesado24VEspecificacao?> ObterEspecificacaoPesadoPorModeloAsync(string modelo);
        Task<bool> GarantirCargaInicialAsync();
    }
}
