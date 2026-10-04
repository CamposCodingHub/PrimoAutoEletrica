using PrimoAutoEletrica.Models.Dvi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public interface IDviInspectionService
    {
        Task<InspecaoDvi> SalvarInspecaoAsync(InspecaoDvi inspecao);
        Task<InspecaoDvi?> ObterPorIdAsync(int id);
        Task<InspecaoDvi?> ObterPorOrdemServicoIdAsync(string ordemServicoId);
        Task<List<InspecaoDvi>> ListarRecentesAsync(int limite = 50);
        Task<List<InspecaoDvi>> ListarPorPlacaAsync(string placa);
        Task<InspecaoDviItem> SalvarItemAsync(InspecaoDviItem item);
        Task<InspecaoDviFoto> SalvarFotoAsync(int itemId, byte[] imagemBytes, string nomeOriginal, string? descricao = null);
        Task<bool> ExcluirFotoAsync(int fotoId);
        Task<bool> ExcluirInspecaoAsync(int inspecaoId);
        List<InspecaoDviItem> GerarChecklistPadraoAutoEletrica(int inspecaoId);
        string MontarResumoTextoLaudo(InspecaoDvi inspecao);
        string GerarLinkWhatsApp(InspecaoDvi inspecao);
        Task<int> ConverterItensParaOrdemServicoAsync(int inspecaoId, string ordemServicoId);
    }
}
