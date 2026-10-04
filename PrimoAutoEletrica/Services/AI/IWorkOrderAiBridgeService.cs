using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public interface IWorkOrderAiBridgeService
    {
        Task<List<AIPartOrServiceProposal>> AnalisarTextoEProporItensAsync(string textoDiagnostico, string? modeloVeiculo = null);
        Task<List<OrdemServico>> ListarOrdensServicoAbertasAsync(string? filtro = null);
        Task<int> InserirItensEmOrdemServicoAsync(Guid ordemServicoId, IEnumerable<AIPartOrServiceProposal> itensPropostos);
        Task<Guid?> GerarRequisicaoCompraAsync(AIPartOrServiceProposal itemSemEstoque, string? observacoes = null);
    }
}
