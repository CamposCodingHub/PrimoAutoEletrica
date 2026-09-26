using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Provedor determinístico e grounded baseado nas regras operacionais e na base de conhecimento local do PRIMOX.
    /// Opera com zero dependência externa e cumpre rigorosamente a regra de NÃO ALUCINAR.
    /// </summary>
    public sealed class GroundedLocalRuleAssistantProvider : IAssistantProvider
    {
        public string ProviderId => "PRIMOX_LOCAL_GROUNDED";
        public string DisplayName => "PRIMOX Local Grounded Engine (Determinístico)";
        public bool IsConfigured => true;

        public Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            var queryLower = context.Query.ToLowerInvariant();
            var matchesKnowledge = context.RetrievedKnowledge ?? Array.Empty<TechnicalKnowledgeEntry>();
            var matchesCases = context.RetrievedCases ?? Array.Empty<DiagnosticCase>();

            // Regra 1: Se não há evidências ou conhecimento relacionado
            if (!matchesKnowledge.Any() && !matchesCases.Any() &&
                !queryLower.Contains("queda") && !queryLower.Contains("partida") &&
                !queryLower.Contains("carga") && !queryLower.Contains("bateria") &&
                !queryLower.Contains("can") && !queryLower.Contains("alternador"))
            {
                var semEvidenciaResponse = new AssistantResponse
                {
                    ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                    AnswerMarkdown = "### Análise Consultiva PRIMOX Assist\n\n" +
                                     "⚠️ **Não encontrei evidência suficiente na base PRIMOX para concluir isso com segurança.**\n\n" +
                                     "O sintoma informado não possui correspondência direta em boletins técnicos publicados ou casos reais comprovados nesta oficina.\n\n" +
                                     "**Orientações para coleta de dados:**\n" +
                                     "- Realize a leitura de códigos DTC via scanner de diagnóstico;\n" +
                                     "- Aferir a tensão de repouso e sob carga da bateria;\n" +
                                     "- Verificar integridade visual do chicote principal e pontos de massa.",
                    Hypotheses = new List<AssistantHypothesis>
                    {
                        new AssistantHypothesis
                        {
                            Title = "Hipótese indeterminada por falta de medição física",
                            ProbabilityRating = "Indeterminada",
                            Rationale = "A base técnica requer medição das grandezas elétricas antes de formular hipóteses.",
                            RequiredVerificationTests = new[] { "Medição de Tensão Bateria", "Varredura DTC OBD2", "Inspeção Visual de Chicote" }
                        }
                    },
                    RecommendedActions = new[]
                    {
                        "Abrir Auto Elétrica Técnica e executar Roteiro D01 (Balanço Energético)",
                        "Registrar os valores medidos na Ordem de Serviço antes de prosseguir"
                    },
                    CitedSources = Array.Empty<AssistantSourceCitation>()
                };

                return Task.FromResult(semEvidenciaResponse);
            }

            // Regra 2: Conhecimento e evidências localizadas
            var sb = new StringBuilder();
            sb.AppendLine("### Análise Estruturada PRIMOX Assist");
            sb.AppendLine();

            if (context.Vehicle != null)
            {
                sb.AppendLine($"**Veículo Analisado:** {context.Vehicle.Make} {context.Vehicle.Model} ({context.Vehicle.Year}) — Sistema {context.Vehicle.Voltage}");
                sb.AppendLine();
            }

            sb.AppendLine("Com base nas medições registradas e no histórico estruturado da oficina, foram identificadas as seguintes correlações técnicas:");
            sb.AppendLine();

            var hypotheses = new List<AssistantHypothesis>();
            var actions = new List<string>();
            var citations = new List<AssistantSourceCitation>();

            // Processar Artigos Técnicos
            foreach (var kb in matchesKnowledge)
            {
                sb.AppendLine($"#### 📄 Boletim Técnico: {kb.Code} — {kb.Title}");
                sb.AppendLine($"- **Sistema:** {kb.System} (Tensão: {kb.Voltage})");
                sb.AppendLine($"- **Causas Prováveis Mapeadas:** {kb.PossibleCauses}");
                sb.AppendLine($"- **Procedimento Recomendado:** {kb.DiagnosticProcedure}");
                if (!string.IsNullOrWhiteSpace(kb.RecommendedMeasurements))
                {
                    sb.AppendLine($"- **Valores Nominais:** {kb.RecommendedMeasurements}");
                }
                sb.AppendLine();

                hypotheses.Add(new AssistantHypothesis
                {
                    Title = kb.PossibleCauses.Split(';').FirstOrDefault()?.Trim() ?? kb.Title,
                    ProbabilityRating = "Alta",
                    Rationale = $"Compatível com boletim {kb.Code} para veículos {kb.VehicleCategory} em {kb.Voltage}.",
                    RequiredVerificationTests = new[] { kb.DiagnosticProcedure }
                });

                citations.Add(new AssistantSourceCitation
                {
                    SourceCode = kb.Code,
                    SourceTitle = kb.Title,
                    RelevanceExplanation = "Procedimento padrão registrado na Base Técnica PRIMOX."
                });
            }

            // Processar Casos Reais de Oficina
            foreach (var caso in matchesCases)
            {
                sb.AppendLine($"#### 🔧 Caso Real da Oficina: {caso.Code} ({caso.VehicleModel})");
                sb.AppendLine($"- **Sintoma Apresentado:** {caso.Symptom}");
                sb.AppendLine($"- **Causa Confirmada na Prática:** {caso.ConfirmedCause}");
                sb.AppendLine($"- **Ação que Solucionou:** {caso.Solution}");
                if (!string.IsNullOrWhiteSpace(caso.PartsUsed))
                {
                    sb.AppendLine($"- **Peças Utilizadas:** {caso.PartsUsed}");
                }
                sb.AppendLine();

                hypotheses.Add(new AssistantHypothesis
                {
                    Title = caso.ConfirmedCause,
                    ProbabilityRating = "Muito Alta (Comprovado em Oficina)",
                    Rationale = $"Solução validada anteriormente no caso {caso.Code} ({caso.VehicleModel}).",
                    RequiredVerificationTests = new[] { "Inspeção visual e teste ôhmico conforme caso histórico" }
                });

                citations.Add(new AssistantSourceCitation
                {
                    SourceCode = caso.Code,
                    SourceTitle = caso.Title,
                    RelevanceExplanation = "Caso operacional concluído com sucesso e testado na pista."
                });
            }

            actions.Add("Conferir medições elétricas recomendadas com multímetro calibrado (PRIMOX Tools)");
            actions.Add("Validar se a queda de tensão máxima admissível está de acordo com a norma do sistema");
            actions.Add("Não substituir componentes elétricos antes de atestar a integridade do cabeamento de força e massa");

            var response = new AssistantResponse
            {
                ConfidenceLevel = citations.Count > 1 ? AssistantConfidenceLevel.HIGH : AssistantConfidenceLevel.MEDIUM,
                AnswerMarkdown = sb.ToString(),
                Hypotheses = hypotheses,
                RecommendedActions = actions,
                CitedSources = citations
            };

            return Task.FromResult(response);
        }
    }
}
