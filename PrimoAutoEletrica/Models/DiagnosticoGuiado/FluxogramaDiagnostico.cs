using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.DiagnosticoGuiado
{
    /// <summary>
    /// Representa um fluxograma de diagnóstico elétrico guiado passo a passo (Troubleshooting Flowchart).
    /// </summary>
    public class FluxogramaDiagnostico
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; // ex: "FLOW-PARTIDA-PESADA"
        public string Titulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = "Partida & Carga"; // Partida & Carga, Injeção, Arrefecimento, etc.
        public string DescricaoSintoma { get; set; } = string.Empty;
        public string SistemaVeicular { get; set; } = "12V Leve / Flex"; // 12V Leve / Flex, 24V Pesado, Universal
        public List<FluxogramaPasso> Passos { get; set; } = new();
    }

    /// <summary>
    /// Representa um passo ou nó de decisão dentro de um fluxograma de diagnóstico.
    /// </summary>
    public class FluxogramaPasso
    {
        public int Id { get; set; }
        public int FluxogramaId { get; set; }
        public int PassoNumero { get; set; } = 1;
        public string TituloPasso { get; set; } = string.Empty;
        public string InstrucaoTeste { get; set; } = string.Empty;
        public string FerramentaRecomendada { get; set; } = "Multímetro Digital (DCV)";
        public string PontoMedicao { get; set; } = string.Empty;
        public string ValorEsperado { get; set; } = string.Empty;
        public string? ObservacaoSeguranca { get; set; }
        public List<FluxogramaOpcaoResposta> Opcoes { get; set; } = new();
    }

    /// <summary>
    /// Representa uma opção de resposta rápida no passo do fluxograma.
    /// </summary>
    public class FluxogramaOpcaoResposta
    {
        public string TextoBotao { get; set; } = string.Empty;
        public int? ProximoPassoNumero { get; set; } // Se null, é o diagnóstico de desfecho final
        public string? ConclusaoDiagnostica { get; set; }
        public string? AcaoRecomendada { get; set; }
        public string? PecaSugerida { get; set; }
        public string? ServicoSugerido { get; set; }
        public string? GravidadeAvaria { get; set; } = "Critica"; // Critica, Moderada, Leve, Normal
        public bool EhConclusao => !ProximoPassoNumero.HasValue;
    }
}
