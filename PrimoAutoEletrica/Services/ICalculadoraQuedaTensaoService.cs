using PrimoAutoEletrica.Models.DiagnosticoGuiado;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public interface ICalculadoraQuedaTensaoService
    {
        ResultadoQuedaTensao CalcularQuedaTensao(ParametrosCalculoQuedaTensao parametros);
        Task<bool> SalvarHistoricoAsync(HistoricoCalculoQuedaTensao historico);
        Task<List<HistoricoCalculoQuedaTensao>> ObterHistoricoAsync(int limite = 50);
        double CalcularBitolaIdeal(double correnteA, double comprimentoM, double tensaoNominalV, double maxQuedaVolts = 0.20);
    }
}
