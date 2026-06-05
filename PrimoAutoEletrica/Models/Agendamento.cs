using System;
using System.Collections.Generic;
using PrimoAutoEletrica.Helpers;

namespace PrimoAutoEletrica.Models
{
    public class Agendamento
    {
        public Guid Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime DataAgendamento { get; set; }
        public DateTime? HoraInicio { get; set; }
        public DateTime? HoraTermino { get; set; }
        public TimeSpan DuracaoEstimada { get; set; }
        public TimeSpan DuracaoReal { get; set; }
        public string Status { get; set; } = "Agendado";
        public string Prioridade { get; set; } = "Normal";
        public string TipoServico { get; set; } = string.Empty;
        public string CategoriaServico { get; set; } = string.Empty;
        public string DescricaoServico { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
        
        // Cliente
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteTelefone { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public string ClienteDocumento { get; set; } = string.Empty;
        public bool ClienteVip { get; set; }
        public decimal ClienteTotalGasto { get; set; }
        public int ClienteAtendimentos { get; set; }
        public DateTime? ClienteUltimaVisita { get; set; }
        
        // Veículo
        public Guid VeiculoId { get; set; }
        public string VeiculoPlaca { get; set; } = string.Empty;
        public string VeiculoModelo { get; set; } = string.Empty;
        public string VeiculoMarca { get; set; } = string.Empty;
        public string VeiculoAno { get; set; } = string.Empty;
        public string VeiculoCor { get; set; } = string.Empty;
        public string VeiculoCombustivel { get; set; } = string.Empty;
        public int VeiculoQuilometragem { get; set; }
        public string VeiculoObservacoes { get; set; } = string.Empty;
        
        // Técnico
        public Guid TecnicoId { get; set; }
        public string TecnicoNome { get; set; } = string.Empty;
        public string TecnicoEspecialidade { get; set; } = string.Empty;
        public bool TecnicoAtivo { get; set; }
        
        // Ordem de Serviço
        public Guid? OrdemServicoId { get; set; }
        public string NumeroOS { get; set; } = string.Empty;
        public DateTime? DataInicioOS { get; set; }
        public DateTime? DataConclusaoOS { get; set; }
        
        // Financeiro
        public decimal ValorEstimado { get; set; }
        public decimal ValorReal { get; set; }
        public decimal ValorPago { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public bool Pago { get; set; }
        public DateTime? DataPagamento { get; set; }
        
        // Check-In/Check-Out
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string CheckInObservacoes { get; set; } = string.Empty;
        public string CheckOutObservacoes { get; set; } = string.Empty;
        public string CheckInFotos { get; set; } = string.Empty;
        public string CheckOutFotos { get; set; } = string.Empty;
        
        // Produtos/Peças
        public List<AgendamentoProduto> Produtos { get; set; } = new();
        public decimal ValorProdutos { get; set; }
        
        // Serviços
        public List<AgendamentoServico> Servicos { get; set; } = new();
        public decimal ValorServicos { get; set; }
        
        // Recorrência
        public bool Recorrente { get; set; }
        public string TipoRecorrencia { get; set; } = string.Empty;
        public int IntervaloRecorrencia { get; set; }
        public DateTime? ProximaRecorrencia { get; set; }
        
        // Lembretes
        public bool LembreteWhatsApp { get; set; }
        public bool LembreteEmail { get; set; }
        public DateTime? DataLembrete { get; set; }
        public bool LembreteEnviado { get; set; }
        
        // Alertas
        public bool AlertaAtraso { get; set; }
        public bool AlertaPecaFaltando { get; set; }
        public bool AlertaPronto { get; set; }
        
        // Timeline
        public List<AgendamentoTimeline> Timeline { get; set; } = new();
        
        // Avaliação
        public int AvaliacaoCliente { get; set; }
        public string AvaliacaoComentario { get; set; } = string.Empty;
        
        // Cancelamento
        public DateTime? DataCancelamento { get; set; }
        public string MotivoCancelamento { get; set; } = string.Empty;
        public Guid? CanceladoPor { get; set; }
        
        // Reagendamento
        public DateTime? DataReagendamento { get; set; }
        public DateTime? DataAgendamentoAnterior { get; set; }
        public string MotivoReagendamento { get; set; } = string.Empty;
    }

    public class AgendamentoProduto
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public string ProdutoNome { get; set; } = string.Empty;
        public string ProdutoCodigo { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal PrecoTotal { get; set; }
        public bool Reservado { get; set; }
        public DateTime? DataReserva { get; set; }
    }

    public class AgendamentoServico
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public TimeSpan TempoEstimado { get; set; }
        public TimeSpan TempoReal { get; set; }
        public string TecnicoResponsavel { get; set; } = string.Empty;
        public string Status { get; set; } = "Pendente";
        public string Observacoes { get; set; } = string.Empty;
        public bool Concluido { get; set; }
        public DateTime? DataConclusao { get; set; }
    }

    public class AgendamentoTimeline
    {
        public Guid Id { get; set; }
        public DateTime DataHora { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Detalhes { get; set; } = string.Empty;
        public string TipoAlteracao { get; set; } = string.Empty;
    }

    // Modelos para Dashboard
    public class DashboardAgendamento
    {
        private string _icone = string.Empty;
        private string _titulo = string.Empty;
        private object _valor = string.Empty;
        private string _comparacao = string.Empty;
        private string _tendencia = string.Empty;

        public string Icone
        {
            get => _icone;
            set => _icone = UiTextSanitizer.SanitizeIcon(value);
        }

        public string Titulo
        {
            get => _titulo;
            set => _titulo = UiTextSanitizer.SanitizeText(value);
        }

        public object Valor
        {
            get => _valor;
            set => _valor = UiTextSanitizer.SanitizeValue(value);
        }

        public string Comparacao
        {
            get => _comparacao;
            set => _comparacao = UiTextSanitizer.SanitizeText(value);
        }

        public string Tendencia
        {
            get => _tendencia;
            set => _tendencia = UiTextSanitizer.SanitizeText(value);
        }

        public string Cor { get; set; } = string.Empty;
    }

    public class TecnicoAgendamento
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Foto { get; set; } = string.Empty;
        public string Especialidade { get; set; } = string.Empty;
        public int ServicosAndamento { get; set; }
        public int ServicosConcluidos { get; set; }
        public decimal Produtividade { get; set; }
        public TimeSpan TempoMedio { get; set; }
        public decimal Avaliacao { get; set; }
        public int OcupacaoDiaria { get; set; }
        public bool Disponivel { get; set; }
        public bool EmPausa { get; set; }
        public DateTime? InicioPausa { get; set; }
    }

    public class VeiculoAgendamento
    {
        public Guid Id { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Ano { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;
        public int Quilometragem { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? Entrada { get; set; }
        public DateTime? SaidaPrevista { get; set; }
        public List<string> Servicos { get; set; } = new();
    }

    public class ClienteAgendamento
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public bool Vip { get; set; }
        public decimal TotalGasto { get; set; }
        public int Atendimentos { get; set; }
        public DateTime? UltimaVisita { get; set; }
        public List<string> Veiculos { get; set; } = new();
        public List<string> ServicosRecentes { get; set; } = new();
    }

    public class AlertaAgendamento
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string Severidade { get; set; } = string.Empty;
        public DateTime DataGeracao { get; set; }
        public bool Lido { get; set; }
        public string Origem { get; set; } = string.Empty;
        public Guid? AgendamentoId { get; set; }
    }

    public class RelatorioAgendamento
    {
        public Guid Id { get; set; }
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tecnico { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Veiculo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duracao { get; set; }
    }
}
