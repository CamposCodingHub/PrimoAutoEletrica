using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.SureTrack;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public interface ISureTrackService
    {
        /// <summary>
        /// Indexa automaticamente uma Ordem de Serviço concluída/finalizada na base empírica SureTrack.
        /// </summary>
        Task<int> IndexarOrdemServicoConcluidaAsync(OrdemServico ordem);

        /// <summary>
        /// Consulta estatísticas de probabilidade percentual de causas e peças confirmadas para um veículo, DTC ou sintoma.
        /// </summary>
        Task<SureTrackConsultaResultado> ConsultarEstatisticasAsync(string? modelo, string? dtc, string? sintoma);

        /// <summary>
        /// Retorna os últimos casos resolvidos catalogados na oficina.
        /// </summary>
        Task<List<CasoResolvidoSureTrack>> ObterUltimosCasosAsync(int limite = 25);

        /// <summary>
        /// Permite inserir manualmente um caso confirmado de bancada.
        /// </summary>
        Task InserirCasoManualAsync(CasoResolvidoSureTrack caso);

        /// <summary>
        /// Garante que a base possua os casos iniciais de alta relevância da oficina (seed inicial de bancada).
        /// </summary>
        Task GarantirCasosIniciaisOficinaAsync();

        /// <summary>
        /// Retorna o total de casos confirmados cadastrados na base.
        /// </summary>
        Task<int> ObterTotalCasosAsync();
    }
}
