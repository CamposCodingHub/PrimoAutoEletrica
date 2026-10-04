using System;
using PRIMOX.Domain.Enums;

namespace PRIMOX.Domain.Entities
{
    /// <summary>
    /// Registro imutável de transição de status na linha do tempo da Ordem de Serviço.
    /// </summary>
    public class HistoricoStatusOS
    {
        public Guid Id { get; private set; }
        public Guid OrdemServicoId { get; private set; }
        public StatusOrdemServico StatusAnterior { get; private set; }
        public StatusOrdemServico NovoStatus { get; private set; }
        public string Motivo { get; private set; }
        public string Responsavel { get; private set; }
        public DateTimeOffset DataHora { get; private set; }

        private HistoricoStatusOS()
        {
            Motivo = string.Empty;
            Responsavel = string.Empty;
        }

        public HistoricoStatusOS(
            Guid id,
            Guid ordemServicoId,
            StatusOrdemServico statusAnterior,
            StatusOrdemServico novoStatus,
            string motivo,
            string responsavel,
            DateTimeOffset dataHora)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            OrdemServicoId = ordemServicoId;
            StatusAnterior = statusAnterior;
            NovoStatus = novoStatus;
            Motivo = string.IsNullOrWhiteSpace(motivo) ? string.Empty : motivo.Trim();
            Responsavel = string.IsNullOrWhiteSpace(responsavel) ? "Sistema" : responsavel.Trim();
            DataHora = dataHora;
        }
    }
}
