using System;
using System.Text.Json.Serialization;

namespace PrimoAutoEletrica.Models
{
    public enum PosVendaTipoEnum
    {
        RevisaoPreventiva,
        Garantia,
        Retorno,
        Reclamacao,
        FollowUpPosServico
    }

    public enum PosVendaStatusEnum
    {
        Pendente,
        Contatado,
        Agendado,
        Concluido,
        Cancelado
    }

    public sealed class PosVendaItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ClienteId { get; set; }
        public Guid? VeiculoId { get; set; }
        public Guid OrdemServicoId { get; set; }
        
        // Snapshots descritivos apenas para exibição rápida em UI/relatórios (chaves primárias continuam sendo os GUIDs)
        public string ClienteNomeSnapshot { get; set; } = string.Empty;
        public string VeiculoDescricaoSnapshot { get; set; } = string.Empty;
        public string PlacaSnapshot { get; set; } = string.Empty;
        public string OsNumeroSnapshot { get; set; } = string.Empty;
        public string TelefoneSnapshot { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PosVendaTipoEnum Tipo { get; set; } = PosVendaTipoEnum.FollowUpPosServico;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PosVendaStatusEnum Status { get; set; } = PosVendaStatusEnum.Pendente;

        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime DataPrevistaContato { get; set; } = DateTime.Today.AddDays(7);
        public DateTime? DataContatoRealizado { get; set; }
        public string Responsavel { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
        public bool Resolvido { get; set; }
        public bool Reincidencia { get; set; }
        public DateTime? GarantiaValidaAte { get; set; }
        
        public bool EstaVencido => Status == PosVendaStatusEnum.Pendente && DataPrevistaContato.Date < DateTime.Today;
        public bool ContatoRealizado => DataContatoRealizado.HasValue || Status == PosVendaStatusEnum.Contatado || Status == PosVendaStatusEnum.Concluido;
    }
}
