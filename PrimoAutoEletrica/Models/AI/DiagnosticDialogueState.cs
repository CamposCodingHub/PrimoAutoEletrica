using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.AI
{
    /// <summary>
    /// Representa o estado contextual de uma sessão de diagnóstico técnico com o Copilot.
    /// Permite que a IA se lembre do sintoma, veículo e testes já realizados ao longo da conversa.
    /// </summary>
    public sealed class DiagnosticDialogueState
    {
        public string SintomaPrincipal { get; set; } = string.Empty;
        public string SistemaVeiculo { get; set; } = string.Empty;
        public string? ModeloVeiculo { get; set; }
        public string? AnoVeiculo { get; set; }
        public string? CodigoDtc { get; set; }
        public int EtapaDiagnostico { get; set; } = 0; // 0 = Triagem/Identificação, 1 = Fusível/Alimentação, 2 = Componente/Lâmpada, 3 = Chicote/Massa, 4 = Central/Comando
        public List<string> TestesJaRealizados { get; set; } = new();
        public bool AguardandoVeiculo { get; set; }
        public bool AguardandoConfirmacaoTeste { get; set; }
        public string? UltimaPerguntaFeita { get; set; }
    }
}
