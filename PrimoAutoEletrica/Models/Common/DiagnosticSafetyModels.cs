using System;

namespace PrimoAutoEletrica.Models.Common
{
    /// <summary>
    /// Nível de perigo elétrico/mecânico de um procedimento de diagnóstico.
    /// </summary>
    public enum SafetyRiskLevel
    {
        /// <summary>Risco Mínimo: Medições passivas em repouso com o circuito desligado.</summary>
        Low = 0,

        /// <summary>Risco Moderado: Testes com chicote isolado e chave ligada (ignição).</summary>
        Moderate = 1,

        /// <summary>Risco Alto: Energização externa de atuadores, relés ou circuitos de potência.</summary>
        High = 2,

        /// <summary>Risco Crítico: Linhas de alta corrente (partida), manipulação de 24V ou rede CAN sob risco de dano à ECU.</summary>
        Critical = 3
    }

    /// <summary>
    /// Registro de validação e pré-requisito de segurança emitido pelo Diagnostic Safety Layer.
    /// </summary>
    public record DiagnosticSafetyCheck(
        string OperationName,
        SafetyRiskLevel RiskLevel,
        string HazardDescription,
        string RequiredPrerequisite,
        string RequiredTool,
        string StopCondition)
    {
        public bool IsBlocked => RiskLevel == SafetyRiskLevel.Critical;

        public static DiagnosticSafetyCheck Safe(string operationName) =>
            new(operationName, SafetyRiskLevel.Low, "Operação segura", "Nenhum pré-requisito crítico", "Multímetro padrão", "Nenhuma");
    }

    /// <summary>
    /// Representa a evidência física de uma medição realizada no veículo.
    /// </summary>
    public class PhysicalMeasurement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ComponentName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = "V"; // "V", "Ω", "A", "bar", "°C", "ms"
        public string Instrument { get; set; } = "Multímetro";
        public string Condition { get; set; } = "Chave Ligada / Repouso";
        public decimal ExpectedMin { get; set; }
        public decimal ExpectedMax { get; set; }
        
        public bool IsPass => Value >= ExpectedMin && Value <= ExpectedMax;
        public string ResultSummary => IsPass ? "PASS" : "FAIL";
    }
}
