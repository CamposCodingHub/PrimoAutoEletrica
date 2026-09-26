using System;

namespace PrimoAutoEletrica.Models
{
    public enum KnowledgeSourceType
    {
        FIELD_EXPERIENCE = 0,
        OEM_MANUAL = 1,
        DIAGNOSTIC_CASE = 2,
        AUTO_ELETRICA_TECNICA = 3
    }

    public enum KnowledgeStatus
    {
        DRAFT = 0,
        PUBLISHED = 1,
        ARCHIVED = 2
    }

    public enum DiagnosticCaseResult
    {
        RESOLVED = 0,
        PARTIAL = 1,
        UNRESOLVED = 2
    }

    /// <summary>
    /// Artigo ou boletim técnico estruturado na base de conhecimento técnico da oficina.
    /// </summary>
    public sealed class TechnicalKnowledgeEntry
    {
        public Guid KnowledgeId { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string System { get; set; } = "Auto Elétrica";
        public string VehicleCategory { get; set; } = "Universal";
        public string Voltage { get; set; } = "12V";
        public string Symptom { get; set; } = string.Empty;
        public string PossibleCauses { get; set; } = string.Empty;
        public string DiagnosticProcedure { get; set; } = string.Empty;
        public string? RecommendedMeasurements { get; set; }
        public string Solution { get; set; } = string.Empty;
        public string? Warnings { get; set; }
        public string? Tags { get; set; }
        public KnowledgeSourceType SourceType { get; set; } = KnowledgeSourceType.FIELD_EXPERIENCE;
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public KnowledgeStatus Status { get; set; } = KnowledgeStatus.PUBLISHED;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public bool IsPublished => Status == KnowledgeStatus.PUBLISHED;
    }

    /// <summary>
    /// Caso real de diagnóstico técnico registrado a partir de uma Ordem de Serviço concluída.
    /// </summary>
    public sealed class DiagnosticCase
    {
        public Guid CaseId { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Guid? VehicleId { get; set; }
        public string VehicleModel { get; set; } = string.Empty;
        public string? VehiclePlate { get; set; }
        public Guid? WorkOrderId { get; set; }
        public string? WorkOrderNumber { get; set; }
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public string System { get; set; } = "Auto Elétrica";
        public string Voltage { get; set; } = "12V";
        public string? DtcCodes { get; set; }
        public string Symptom { get; set; } = string.Empty;
        public string? Measurements { get; set; }
        public string? InitialHypotheses { get; set; }
        public string ConfirmedCause { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public string? PartsUsed { get; set; }
        public string? TestResult { get; set; }
        public DiagnosticCaseResult FinalResult { get; set; } = DiagnosticCaseResult.RESOLVED;
        public Guid? KnowledgeEntryId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public bool IsResolved => FinalResult == DiagnosticCaseResult.RESOLVED;
    }
}
