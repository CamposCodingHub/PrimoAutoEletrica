using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public sealed class ResumoFerramentaria
    {
        public int TotalFerramentas { get; set; }
        public int Disponiveis { get; set; }
        public int EmUso { get; set; }
        public int EmManutencao { get; set; }
        public int Avariadas { get; set; }
        public int CalibracaoVencendo { get; set; }
    }

    public interface IFerramentaService
    {
        Task<List<Ferramenta>> ListarFerramentasAsync(string? termoBusca = null, CategoriaFerramenta? categoria = null, StatusFerramenta? status = null);
        Task<Ferramenta?> ObterPorIdAsync(Guid id);
        Task<Ferramenta?> ObterPorCodigoOuSerieAsync(string codigoOuSerie);
        Task<ResumoFerramentaria> ObterResumoAsync();
        Task SalvarFerramentaAsync(Ferramenta ferramenta);
        Task ExcluirFerramentaAsync(Guid id);

        Task<MovimentacaoFerramenta> RegistrarRetiradaAsync(
            Guid ferramentaId,
            Guid funcionarioId,
            string funcionarioNome,
            Guid? ordemServicoId,
            string? numeroOS,
            DateTime? previsaoDevolucao,
            string estadoConservacao = "OK",
            string registradoPor = "Sistema");

        Task<MovimentacaoFerramenta> RegistrarDevolucaoAsync(
            Guid ferramentaId,
            string estadoConservacaoDevolucao = "OK",
            string? observacoesDevolucao = null,
            string registradoPor = "Sistema");

        Task<List<Ferramenta>> ObterFerramentasPendentesOSAsync(Guid ordemServicoId);
        Task<List<MovimentacaoFerramenta>> ObterHistoricoMovimentacoesAsync(Guid? ferramentaId = null, int limite = 50);
        Task<List<Ferramenta>> ObterFerramentasEmAtrasoDevolucaoAsync();
        Task RegistrarCalibracaoAsync(Guid ferramentaId, DateTime dataCalibracao, int proximoIntervaloDias = 365, string? observacoes = null);
    }
}
