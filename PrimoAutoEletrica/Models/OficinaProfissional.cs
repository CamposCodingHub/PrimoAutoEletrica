using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PrimoAutoEletrica.Models
{
    public sealed class ChecklistVisualItem
    {
        public string Categoria { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Entrada { get; set; } = "Conferir";
        public string Saida { get; set; } = "Conferir";
        public string Observacoes { get; set; } = string.Empty;
    }

    public sealed class OficinaKanbanColumn
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public ObservableCollection<OficinaKanbanCard> Cards { get; set; } = new();
        public int Quantidade => Cards.Count;
        public decimal TotalEstimado => Cards.Sum(card => card.ValorEstimado);
        public string TotalEstimadoFormatado => TotalEstimado.ToString("C");
    }

    public sealed class OficinaKanbanCard
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public Guid? VeiculoId { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Veiculo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string ServicoPrincipal { get; set; } = string.Empty;
        public string Tecnico { get; set; } = string.Empty;
        public string Prazo { get; set; } = string.Empty;
        public DateTime? DataPrevisao { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Prioridade { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public decimal ValorEstimado { get; set; }
        public string ValorEstimadoFormatado => ValorEstimado.ToString("C");
        public string ResumoOperacional => $"{Cliente} | {Veiculo} | {ServicoPrincipal}";
    }

    public sealed class OficinaTimelineItem
    {
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public decimal? Valor { get; set; }
        public string DataResumo => Data == default ? "-" : Data.ToString("dd/MM/yyyy HH:mm");
        public string ValorResumo => Valor.HasValue ? Valor.Value.ToString("C") : string.Empty;
    }

    public sealed class ModeloMensagemCliente
    {
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Contexto { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
    }

    public sealed class OficinaProfissionalSnapshot
    {
        public IReadOnlyList<ChecklistVisualItem> ChecklistPadrao { get; set; } = Array.Empty<ChecklistVisualItem>();
        public IReadOnlyList<OficinaKanbanColumn> Kanban { get; set; } = Array.Empty<OficinaKanbanColumn>();
        public IReadOnlyList<OficinaTimelineItem> Timeline { get; set; } = Array.Empty<OficinaTimelineItem>();
        public IReadOnlyList<ModeloMensagemCliente> Mensagens { get; set; } = Array.Empty<ModeloMensagemCliente>();
        public string Resumo { get; set; } = string.Empty;
    }
}
