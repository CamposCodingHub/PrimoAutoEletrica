using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3.3 — validate provider output against evidence IDs; strip/reject unsupported claims;
    /// map to Assist response contract. No fake confidence scores.
    /// </summary>
    public sealed class ExternalResponseGroundingValidator
    {
        private static readonly Regex EvidenceIdMention = new(
            @"\b([0-9a-f]{8,32}|KB-[\w-]+|D\d{2}[\w-]*)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly string[] DangerousActionPhrases =
        {
            "compre agora", "autorizar pagamento", "aprovar compra", "emitir nfe",
            "deletar estoque", "alterar preco", "alterar preço", "pagar fornecedor",
            "execute a compra", "pedido de compra criado"
        };

        public AssistantResponse Validate(
            ExternalAssistantRawResponse raw,
            ExternalEvidencePackage package,
            string providerId)
        {
            ArgumentNullException.ThrowIfNull(raw);
            ArgumentNullException.ThrowIfNull(package);

            if (!package.HasEvidence)
            {
                return FailClosed(
                    "Pacote de evidência local vazio — consulta externa recusada (fail-closed).",
                    providerId,
                    new[] { ExternalAssistantWarnings.NoEvidence, AssistFailClosedPolicy.WarningProviderUnavailable },
                    package);
            }

            var allowed = new HashSet<string>(package.AllowedEvidenceIds, StringComparer.OrdinalIgnoreCase);
            foreach (var s in package.Sources)
            {
                if (!string.IsNullOrWhiteSpace(s.SourceId)) allowed.Add(s.SourceId);
            }

            var cited = (raw.CitedEvidenceIds ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var groundedIds = cited.Where(id => allowed.Contains(id)).ToList();
            var hallucinated = cited.Where(id => !allowed.Contains(id)).ToList();

            // Also scan answer body for evidence-looking tokens not in package
            var mentioned = EvidenceIdMention.Matches(raw.AnswerMarkdown ?? string.Empty)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var m in mentioned)
            {
                if (!allowed.Contains(m) && LooksLikeEvidenceToken(m))
                    hallucinated.Add(m);
            }

            // C5.7 — conflict / invented OS enforcement (not CONFIRMED)
            var conflict = DetectConflictOrInventedOs(raw.AnswerMarkdown, package);
            if (conflict != null)
            {
                var cw = new List<string> { ExternalAssistantWarnings.Conflict };
                if (conflict == "INVENTED_OS") cw.Add(ExternalAssistantWarnings.InventedOs);
                return FailClosed(
                    conflict == "INVENTED_OS"
                        ? "Resposta externa rejeitada: OS/ordem inventada sem evidência local (REJECT/UNGROUNDED)."
                        : "Resposta externa em CONFLICT com evidência local — não CONFIRMED. Nenhuma ação aplicada.",
                    providerId,
                    cw,
                    package,
                    missing: new[] { "Alinhamento com evidência local; sem inventar OS/cliente/veículo." });
            }

            var warnings = new List<string>();
            if (package.RedactedFields.Count > 0)
                warnings.Add(ExternalAssistantWarnings.FinanceRedacted);

            if (hallucinated.Count > 0 || groundedIds.Count == 0)
            {
                warnings.Add(ExternalAssistantWarnings.Ungrounded);
                return FailClosed(
                    "Resposta externa rejeitada: claims sem evidência local mapeável (EXTERNAL_UNGROUNDED). Nenhuma ação inventada foi aplicada.",
                    providerId,
                    warnings,
                    package,
                    missing: new[] { "Evidência local citável para as claims do provedor externo." });
            }

            var actions = (raw.SuggestedActions ?? Array.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Where(a => !DangerousActionPhrases.Any(d => a.Contains(d, StringComparison.OrdinalIgnoreCase)))
                .Select(a => a.Trim())
                .Take(8)
                .ToList();

            if (actions.Count == 0)
            {
                actions.Add("Validar medições locais citadas nas evidências antes de qualquer substituição.");
            }

            var evidenceItems = package.Sources
                .Where(s => groundedIds.Contains(s.EvidenceId) || groundedIds.Contains(s.SourceId))
                .Select(s => new EvidenceItem
                {
                    EvidenceId = s.EvidenceId,
                    Kind = Enum.TryParse<EvidenceKind>(s.SourceType, true, out var k) ? k : EvidenceKind.Other,
                    SourceCode = s.SourceId,
                    Title = s.Title,
                    Excerpt = s.Excerpt,
                    RelevanceLabel = s.RelevanceLabel ?? "external-grounded",
                    Classification = s.Classification,
                    ConfidenceContribution = AssistantConfidenceLevel.LOW
                })
                .ToList();

            var citations = evidenceItems.Select(e => new AssistantSourceCitation
            {
                SourceCode = e.SourceCode,
                SourceTitle = e.Title,
                RelevanceExplanation = "Citado pelo provedor externo e validado contra evidência local."
            }).ToList();

            // Honest confidence: never invent HIGH from remote; map by evidence count only.
            var confidence = evidenceItems.Count >= 3
                ? AssistantConfidenceLevel.MEDIUM
                : AssistantConfidenceLevel.LOW;

            var limitations = string.IsNullOrWhiteSpace(raw.Limitations)
                ? "Resposta rephrased/ranked por provedor externo; diagnóstico conclusivo depende de validação física do profissional."
                : raw.Limitations!;

            var answer = (raw.AnswerMarkdown ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(answer))
            {
                return FailClosed(
                    "Resposta externa vazia após parsing — fail-closed.",
                    providerId,
                    new[] { ExternalAssistantWarnings.MalformedResponse },
                    package);
            }

            return new AssistantResponse
            {
                AnswerMarkdown = answer + "\n\n_Limitações:_ " + limitations,
                ConfidenceLevel = confidence,
                Evidence = evidenceItems,
                CitedSources = citations,
                RecommendedActions = actions,
                MissingInformation = raw.MissingInformation ?? Array.Empty<string>(),
                Warnings = warnings,
                Provider = providerId,
                Timestamp = DateTimeOffset.Now,
                Disclaimers = "O PRIMOX Assist atua como copiloto técnico consultivo. O diagnóstico conclusivo e a segurança da operação dependem exclusivamente da validação física do profissional. Provedor externo não executa compra/aprovação/fiscal/estoque."
            };
        }

        private static bool LooksLikeEvidenceToken(string token)
        {
            if (token.StartsWith("KB-", StringComparison.OrdinalIgnoreCase)) return true;
            if (Regex.IsMatch(token, @"^D\d{2}")) return true;
            return token.Length >= 16 && token.All(c => Uri.IsHexDigit(c));
        }

        private static AssistantResponse FailClosed(
            string answer,
            string providerId,
            IReadOnlyList<string> warnings,
            ExternalEvidencePackage package,
            IReadOnlyList<string>? missing = null)
        {
            return new AssistantResponse
            {
                AnswerMarkdown = answer,
                ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                Evidence = Array.Empty<EvidenceItem>(),
                CitedSources = Array.Empty<AssistantSourceCitation>(),
                RecommendedActions = new[]
                {
                    "Continuar com o provedor local grounded (PRIMOX_LOCAL_GROUNDED).",
                    "Coletar medições e evidências locais antes de nova consulta."
                },
                MissingInformation = missing ?? new[] { "Evidência local suficiente e claims mapeáveis." },
                Warnings = warnings.ToArray(),
                Provider = providerId,
                Timestamp = DateTimeOffset.Now
            };
        }
        private static string? DetectConflictOrInventedOs(string? answer, ExternalEvidencePackage package)
        {
            if (string.IsNullOrWhiteSpace(answer)) return null;
            var text = answer;
            // Invent OS numbers/ids not present in local package excerpts/titles
            var osMentions = System.Text.RegularExpressions.Regex.Matches(text, @"\bOS[\s#-]*(\d{3,})\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match m in osMentions)
            {
                var token = m.Value;
                var localBlob = string.Join(" ", package.Sources.Select(s => (s.Title ?? "") + " " + (s.Excerpt ?? "") + " " + (s.SourceId ?? "") + " " + (s.EvidenceId ?? "")));
                if (localBlob.IndexOf(m.Groups[1].Value, StringComparison.OrdinalIgnoreCase) < 0 &&
                    localBlob.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return "INVENTED_OS";
                }
            }

            // Explicit contradiction markers against local evidence
            if (text.Contains("CONTRADIZ_EVIDENCIA_LOCAL", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("IGNORAR EVIDENCIA LOCAL", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("evidência local está errada", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("evidencia local esta errada", StringComparison.OrdinalIgnoreCase))
            {
                return "CONFLICT";
            }

            return null;
        }
    }
}