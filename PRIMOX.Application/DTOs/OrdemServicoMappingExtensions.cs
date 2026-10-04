using System.Linq;
using PRIMOX.Domain.Entities;

namespace PRIMOX.Application.DTOs
{
    public static class OrdemServicoMappingExtensions
    {
        public static OrdemServicoDto ToDto(this OrdemServico os)
        {
            return new OrdemServicoDto(
                os.Id,
                os.Numero,
                os.ClienteId,
                os.VeiculoId,
                os.ClienteNomeSnapshot,
                os.VeiculoPlacaSnapshot,
                os.VeiculoModeloSnapshot,
                os.Status.ToString(),
                os.Prioridade.ToString(),
                os.QueixaCliente,
                os.DiagnosticoTecnico,
                os.ObservacoesInternas,
                os.DataAbertura,
                os.DataPrevisaoConclusao,
                os.DataConclusao,
                os.TotalPecas.Amount,
                os.TotalServicos.Amount,
                os.Desconto.Amount,
                os.TotalBruto.Amount,
                os.TotalLiquido.Amount,
                os.Desconto.SafeCurrency.SafeCode,
                os.SessaoDiagnosticoId,
                os.TenantId,
                os.FilialId,
                os.ItensPeca.Select(p => new ItemPecaDto(
                    p.Id,
                    p.OrdemServicoId,
                    p.Codigo,
                    p.Descricao,
                    p.Quantidade,
                    p.ValorUnitario.Amount,
                    p.CustoUnitario.Amount,
                    p.Subtotal.Amount,
                    p.ValorUnitario.SafeCurrency.SafeCode,
                    p.ProdutoId)).ToList(),
                os.ItensServico.Select(s => new ItemServicoDto(
                    s.Id,
                    s.OrdemServicoId,
                    s.Descricao,
                    s.QuantidadeHoras,
                    s.ValorHora.Amount,
                    s.Subtotal.Amount,
                    s.ValorHora.SafeCurrency.SafeCode,
                    s.TecnicoResponsavel,
                    s.ServicoId)).ToList(),
                os.Historico.Select(h => new HistoricoStatusDto(
                    h.Id,
                    h.StatusAnterior.ToString(),
                    h.NovoStatus.ToString(),
                    h.Motivo,
                    h.Responsavel,
                    h.DataHora)).ToList());
        }
    }
}
