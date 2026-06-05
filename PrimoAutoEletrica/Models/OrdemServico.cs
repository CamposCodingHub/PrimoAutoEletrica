using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class OrdemServico
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Numero { get; set; } = string.Empty;

        public Guid ClienteId { get; set; }
        public Guid? VeiculoId { get; set; }
        public int? TecnicoId { get; set; }
        public Guid? AgendamentoId { get; set; }

        public string ClienteNomeSnapshot { get; set; } = string.Empty;
        public string TelefoneClienteSnapshot { get; set; } = string.Empty;
        public string VeiculoDescricaoSnapshot { get; set; } = string.Empty;
        public string PlacaSnapshot { get; set; } = string.Empty;

        public string Status { get; set; } = "Rascunho";
        public string Prioridade { get; set; } = "Normal";
        public string Origem { get; set; } = "Balcao";
        public string ProblemaRelatado { get; set; } = string.Empty;
        public string Diagnostico { get; set; } = string.Empty;
        public string DiagnosticoInicial { get; set; } = string.Empty;
        public string DiagnosticoFinal { get; set; } = string.Empty;
        public string ObservacoesInternas { get; set; } = string.Empty;
        public string ObservacoesCliente { get; set; } = string.Empty;
        public string ChecklistEntrada { get; set; } = string.Empty;
        public string ChecklistEntrega { get; set; } = string.Empty;
        public string ChecklistSaida { get; set; } = string.Empty;
        public string FotosAntes { get; set; } = string.Empty;
        public string FotosDepois { get; set; } = string.Empty;
        public string GarantiaObservacoes { get; set; } = string.Empty;
        public string AssinaturaClienteUrl { get; set; } = string.Empty;

        public bool AprovadaCliente { get; set; }
        public string MetodoAprovacao { get; set; } = string.Empty;

        public DateTime DataAbertura { get; set; } = DateTime.Now;
        public DateTime? DataPrevisao { get; set; }
        public DateTime? DataAprovacao { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataConclusao { get; set; }
        public DateTime? DataEntrega { get; set; }
        public DateTime? GarantiaValidaAte { get; set; }
        public int TempoPrevistoMinutos { get; set; }
        public int TempoRealMinutos { get; set; }
        public Guid? OrcamentoId { get; set; }

        public decimal ValorMaoObra { get; set; }
        public decimal Desconto { get; set; }
        public bool Ativo { get; set; } = true;

        public List<OrdemServicoItem> Itens { get; set; } = new();
        public List<OrdemServicoEvento> Eventos { get; set; } = new();
    }
}
