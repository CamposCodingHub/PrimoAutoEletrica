using System;

namespace PrimoAutoEletrica.Models.Circuitos
{
    /// <summary>
    /// Ponto de teste para medição com multímetro e osciloscópio no esquema elétrico
    /// Fornece tolerâncias normativas esperadas para diagnóstico socrático
    /// </summary>
    public sealed class CircuitProbePoint
    {
        public string Id { get; set; } = string.Empty; // "A", "B", "C", "D", "E"
        public string Title { get; set; } = string.Empty;
        public string TargetComponent { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }

        // Especificações de Medição no Multímetro
        public string MultimeterScale { get; set; } = "20V DC";
        public string KeyOffVoltage { get; set; } = "12.4V a 12.7V";
        public string KeyOnVoltage { get; set; } = "12.0V a 12.5V";
        public string EngineRunningVoltage { get; set; } = "13.8V a 14.5V";
        public string CrankingVoltage { get; set; } = ">= 9.6V (Mínimo no arranque)";

        // Especificações de Osciloscópio
        public string? OscilloscopeWaveform { get; set; }
        public string? OscilloscopeTimebase { get; set; } // ex: "5 ms/div"
        public string? OscilloscopeVoltageScale { get; set; } // ex: "2V / div"

        // Interpretação Técnica e Diagnóstico
        public string DiagnosticInstructions { get; set; } = string.Empty;
        public string FaultConditionInterpretation { get; set; } = string.Empty;

        public string ObterResumoFormatado()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"⚡ **Ponto de Teste {Id}: {Title}**");
            sb.AppendLine($"• Componente: {TargetComponent}");
            sb.AppendLine($"• Escala Recomendada: {MultimeterScale}");
            sb.AppendLine($"• Chave Desligada (Repouso): {KeyOffVoltage}");
            sb.AppendLine($"• Chave Ligada (Linha 15): {KeyOnVoltage}");
            sb.AppendLine($"• Motor em Funcionamento: {EngineRunningVoltage}");
            if (!string.IsNullOrWhiteSpace(CrankingVoltage))
            {
                sb.AppendLine($"• Partida (Linha 50): {CrankingVoltage}");
            }
            if (!string.IsNullOrWhiteSpace(OscilloscopeWaveform))
            {
                sb.AppendLine($"• Sinal no Osciloscópio: {OscilloscopeWaveform} ({OscilloscopeTimebase}, {OscilloscopeVoltageScale})");
            }
            sb.AppendLine($"\n🔍 **Procedimento:** {DiagnosticInstructions}");
            sb.AppendLine($"⚠️ **Se Fora do Padrão:** {FaultConditionInterpretation}");
            return sb.ToString();
        }
    }
}
