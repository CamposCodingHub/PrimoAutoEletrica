using System;
using System.Collections.Generic;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// Adapters: entidades reais → KnowledgeItem (retrieval layer). Não substituem o domínio.
    /// </summary>
    public static class KnowledgeItemAdapters
    {
        public static KnowledgeItem FromTechnicalKnowledge(TechnicalKnowledgeEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var tags = SplitTags(entry.Tags);
            var body = string.Join('\n', new[]
            {
                entry.Title,
                entry.Symptom,
                entry.PossibleCauses,
                entry.DiagnosticProcedure,
                entry.RecommendedMeasurements,
                entry.Solution,
                entry.Warnings,
                entry.Tags
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return new KnowledgeItem
            {
                ItemId = entry.KnowledgeId.ToString("N"),
                Type = KnowledgeType.TECHNICAL_CASE,
                Code = entry.Code ?? string.Empty,
                Title = entry.Title ?? string.Empty,
                System = entry.System ?? string.Empty,
                Symptom = entry.Symptom ?? string.Empty,
                Diagnosis = entry.PossibleCauses ?? string.Empty,
                Solution = entry.Solution ?? string.Empty,
                VehicleModel = entry.VehicleCategory ?? string.Empty,
                Tags = tags,
                BodyText = body,
                SourceEntity = nameof(TechnicalKnowledgeEntry),
                SourceEntityId = entry.KnowledgeId.ToString(),
                UpdatedAt = entry.UpdatedAt
            };
        }

        public static KnowledgeItem FromDiagnosticCase(DiagnosticCase caso)
        {
            ArgumentNullException.ThrowIfNull(caso);
            var tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(caso.DtcCodes))
            {
                tags.AddRange(caso.DtcCodes.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }

            var body = string.Join('\n', new[]
            {
                caso.Title,
                caso.Symptom,
                caso.InitialHypotheses,
                caso.ConfirmedCause,
                caso.Solution,
                caso.Measurements,
                caso.PartsUsed,
                caso.TestResult,
                caso.DtcCodes
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return new KnowledgeItem
            {
                ItemId = caso.CaseId.ToString("N"),
                Type = KnowledgeType.DIAGNOSTIC_CASE,
                Code = caso.Code ?? string.Empty,
                Title = caso.Title ?? string.Empty,
                System = caso.System ?? string.Empty,
                Symptom = caso.Symptom ?? string.Empty,
                Diagnosis = caso.ConfirmedCause ?? string.Empty,
                Solution = caso.Solution ?? string.Empty,
                VehicleModel = caso.VehicleModel ?? string.Empty,
                VehiclePlate = caso.VehiclePlate,
                WorkOrderNumber = caso.WorkOrderNumber,
                Tags = tags,
                BodyText = body,
                SourceEntity = nameof(DiagnosticCase),
                SourceEntityId = caso.CaseId.ToString(),
                UpdatedAt = caso.UpdatedAt
            };
        }

        public static KnowledgeItem FromDiagnosticRoteiro(DiagnosticoGuiadoRoteiro roteiro)
        {
            ArgumentNullException.ThrowIfNull(roteiro);
            var tags = new List<string> { roteiro.Codigo ?? string.Empty, "roteiro", "auto-eletrica" };
            tags.AddRange(roteiro.ServicosSugeridos ?? new List<string>());
            tags.AddRange(roteiro.PecasSugeridas ?? new List<string>());

            var diagnosis = string.Join("; ", roteiro.PossiveisCausas ?? new List<string>());
            var solution = string.Join("; ", roteiro.SequenciaTestes ?? new List<string>());
            var body = string.Join('\n', new[]
            {
                roteiro.Titulo,
                roteiro.Sintoma,
                diagnosis,
                solution,
                string.Join("; ", roteiro.ValoresEsperados ?? new List<string>()),
                string.Join("; ", roteiro.Ferramentas ?? new List<string>()),
                string.Join("; ", roteiro.PecasSugeridas ?? new List<string>())
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return new KnowledgeItem
            {
                ItemId = $"procedure-{roteiro.Codigo}",
                Type = KnowledgeType.PROCEDURE,
                Code = roteiro.Codigo ?? string.Empty,
                Title = roteiro.Titulo ?? string.Empty,
                System = "Auto Elétrica",
                Symptom = roteiro.Sintoma ?? string.Empty,
                Diagnosis = diagnosis,
                Solution = solution,
                Tags = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                BodyText = body,
                SourceEntity = nameof(DiagnosticoGuiadoRoteiro),
                SourceEntityId = roteiro.Codigo,
                UpdatedAt = null
            };
        }

        public static KnowledgeItem FromBibliotecaItem(BibliotecaTecnicaItem item, int ordinal)
        {
            ArgumentNullException.ThrowIfNull(item);
            var code = $"LIB-{ordinal:D3}";
            var body = string.Join('\n', new[]
            {
                item.Titulo,
                item.Categoria,
                item.Resumo,
                string.Join("; ", item.Passos ?? new List<string>()),
                string.Join("; ", item.ValoresReferencia ?? new List<string>()),
                string.Join("; ", item.Ferramentas ?? new List<string>())
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return new KnowledgeItem
            {
                ItemId = $"lib-{ordinal:D3}",
                Type = KnowledgeType.PROCEDURE,
                Code = code,
                Title = item.Titulo ?? string.Empty,
                System = item.Categoria ?? "Biblioteca",
                Symptom = item.Resumo ?? string.Empty,
                Diagnosis = string.Join("; ", item.Passos ?? new List<string>()),
                Solution = string.Join("; ", item.ValoresReferencia ?? new List<string>()),
                Tags = new[] { "biblioteca", item.Categoria ?? string.Empty }.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
                BodyText = body,
                SourceEntity = nameof(BibliotecaTecnicaItem),
                SourceEntityId = code
            };
        }

        public static KnowledgeItem? FromWorkOrderSnapshot(OrdemServico os)
        {
            if (os == null) return null;
            if (string.IsNullOrWhiteSpace(os.ProblemaRelatado) && string.IsNullOrWhiteSpace(os.DiagnosticoFinal) && string.IsNullOrWhiteSpace(os.DiagnosticoInicial))
            {
                return null;
            }

            var body = string.Join('\n', new[]
            {
                os.Numero,
                os.ProblemaRelatado,
                os.DiagnosticoInicial,
                os.DiagnosticoFinal,
                os.VeiculoDescricaoSnapshot,
                os.PlacaSnapshot
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return new KnowledgeItem
            {
                ItemId = $"os-{os.Id:N}",
                Type = KnowledgeType.WORK_ORDER,
                Code = os.Numero ?? os.Id.ToString("N")[..8],
                Title = $"OS {os.Numero}: {Truncate(os.ProblemaRelatado, 80)}",
                System = "Oficina",
                Symptom = os.ProblemaRelatado ?? string.Empty,
                Diagnosis = os.DiagnosticoFinal ?? os.DiagnosticoInicial ?? string.Empty,
                Solution = string.Empty,
                VehicleModel = os.VeiculoDescricaoSnapshot ?? string.Empty,
                VehiclePlate = os.PlacaSnapshot,
                WorkOrderNumber = os.Numero,
                Tags = new[] { "os", os.Status ?? string.Empty }.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
                BodyText = body,
                SourceEntity = nameof(OrdemServico),
                SourceEntityId = os.Id.ToString(),
                UpdatedAt = os.DataAbertura
            };
        }

        public static EvidenceItem ToEvidence(KnowledgeSearchHit hit)
        {
            ArgumentNullException.ThrowIfNull(hit);
            var kind = hit.Item.Type switch
            {
                KnowledgeType.TECHNICAL_CASE => EvidenceKind.Knowledge,
                KnowledgeType.DIAGNOSTIC_CASE => EvidenceKind.DiagnosticCase,
                KnowledgeType.PROCEDURE => EvidenceKind.Procedure,
                KnowledgeType.WORK_ORDER => EvidenceKind.WorkOrderNote,
                KnowledgeType.MEASUREMENT => EvidenceKind.Measurement,
                _ => EvidenceKind.Other
            };

            var confidence = hit.Score >= 8 ? AssistantConfidenceLevel.HIGH
                : hit.Score >= 4 ? AssistantConfidenceLevel.MEDIUM
                : AssistantConfidenceLevel.LOW;

            return new EvidenceItem
            {
                EvidenceId = hit.Item.ItemId,
                Kind = kind,
                SourceCode = hit.Item.Code,
                Title = hit.Item.Title,
                Excerpt = hit.Excerpt,
                RelevanceScore = hit.Score,
                RelevanceLabel = hit.MatchLabel,
                ConfidenceContribution = confidence,
                Classification = "TECHNICAL"
            };
        }

        private static IReadOnlyList<string> SplitTags(string? tags)
        {
            if (string.IsNullOrWhiteSpace(tags)) return Array.Empty<string>();
            return tags.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length > 0)
                .ToList();
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= max ? value : value[..(max - 3)] + "...";
        }
    }
}
