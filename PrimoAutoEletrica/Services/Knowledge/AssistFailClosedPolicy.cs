using System;
using System.Collections.Generic;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// Fail-closed policy stubs F1–F4 (C2.1). Cheap, testable, no network.
    /// </summary>
    public static class AssistFailClosedPolicy
    {
        public const string WarningOutOfDomain = "OUT_OF_DOMAIN";
        public const string WarningFinancialDenied = "FINANCIAL_PERMISSION_DENIED";
        public const string WarningCrossClientDenied = "CROSS_CLIENT_DENIED";
        public const string WarningProviderUnavailable = "PROVIDER_UNAVAILABLE";

        public static bool LooksOutOfDomain(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            var q = DeterministicKnowledgeIndex.RemoveDiacritics(query.ToLowerInvariant());
            // Domínio oficina/autoelétrica — se query só tem termos claramente fora, marca OOD.
            string[] ood =
            {
                "receita de bolo", "cotacao de acoes", "criptomoeda", "horoscopo",
                "receita culinaria", "resultado do jogo", "filme netflix"
            };
            return ood.Any(t => q.Contains(t));
        }

        public static bool LooksFinancial(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            var q = DeterministicKnowledgeIndex.RemoveDiacritics(query.ToLowerInvariant());
            string[] finance =
            {
                "faturamento", "lucro", "margem", "fluxo de caixa", "contas a receber",
                "contas a pagar", "salario", "comissao", "dre", "balanco financeiro"
            };
            return finance.Any(t => q.Contains(t));
        }

        /// <summary>F1: sem evidência → INSUFFICIENT_EVIDENCE + MissingInformation.</summary>
        public static AssistantResponse ApplyF1NoEvidence(string query, IReadOnlyList<string>? missing = null)
        {
            return new AssistantResponse
            {
                AnswerMarkdown = "Não encontrei evidência suficiente na base técnica local para fundamentar um diagnóstico. Colete medições e tente novamente.",
                ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                RecommendedActions = new[]
                {
                    "Registrar sintoma observado e condições (motor ligado/parado, carga).",
                    "Medir tensão em repouso e sob carga antes de condenar componente.",
                    "Consultar roteiros D01–D17 em Auto Elétrica Técnica."
                },
                MissingInformation = missing ?? new[]
                {
                    "Tensão da bateria em repouso",
                    "Queda de tensão sob carga (positivo e negativo)",
                    "Modelo/ano/tensão do veículo (12V/24V)"
                },
                Warnings = Array.Empty<string>(),
                Evidence = Array.Empty<EvidenceItem>(),
                CitedSources = Array.Empty<AssistantSourceCitation>(),
                Provider = "PRIMOX_FAIL_CLOSED",
                Timestamp = DateTimeOffset.Now
            };
        }

        /// <summary>F2: fora de domínio.</summary>
        public static AssistantResponse ApplyF2OutOfDomain(string query)
        {
            return new AssistantResponse
            {
                AnswerMarkdown = "A consulta parece fora do domínio técnico da oficina (auto elétrica / diagnóstico veicular). Não há enriquecimento financeiro ou pessoal.",
                ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                Warnings = new[] { WarningOutOfDomain },
                MissingInformation = new[] { "Reformular a pergunta com sintoma elétrico, sistema ou código de roteiro (D01–D17)." },
                RecommendedActions = new[] { "Descrever o sintoma elétrico do veículo." },
                Evidence = Array.Empty<EvidenceItem>(),
                CitedSources = Array.Empty<AssistantSourceCitation>(),
                Provider = "PRIMOX_FAIL_CLOSED",
                Timestamp = DateTimeOffset.Now
            };
        }

        /// <summary>F3: financeiro sem permissão.</summary>
        public static AssistantResponse ApplyF3FinancialDenied(string query)
        {
            return new AssistantResponse
            {
                AnswerMarkdown = "Não posso fornecer números financeiros nesta consulta. Permissão financeira ausente ou não aplicável ao Assist técnico.",
                ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                Warnings = new[] { WarningFinancialDenied },
                MissingInformation = new[] { "Usar módulo Financeiro com perfil autorizado, se a necessidade for operacional." },
                RecommendedActions = Array.Empty<string>(),
                Evidence = Array.Empty<EvidenceItem>(),
                CitedSources = Array.Empty<AssistantSourceCitation>(),
                Provider = "PRIMOX_FAIL_CLOSED",
                Timestamp = DateTimeOffset.Now
            };
        }

        /// <summary>F4: cross-client sem authZ.</summary>
        public static AssistantResponse ApplyF4CrossClientDenied(Guid? requestedClienteId)
        {
            return new AssistantResponse
            {
                AnswerMarkdown = "Acesso a dados de outro cliente foi recusado (fail-closed). Nenhuma informação cross-client foi retornada.",
                ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                Warnings = new[] { WarningCrossClientDenied },
                MissingInformation = new[] { "Autorização explícita / escopo de cliente compatível com a sessão." },
                RecommendedActions = Array.Empty<string>(),
                Evidence = Array.Empty<EvidenceItem>(),
                CitedSources = Array.Empty<AssistantSourceCitation>(),
                Provider = "PRIMOX_FAIL_CLOSED",
                Timestamp = DateTimeOffset.Now
            };
        }

        /// <summary>
        /// Avalia pré-condições fail-closed. Retorna resposta curta se deve short-circuit; null = seguir fluxo normal.
        /// hasFinancePermission: false ⇒ F3 quando query financeira.
        /// sessionClienteId vs contextClienteId: mismatch ⇒ F4.
        /// </summary>
        public static AssistantResponse? EvaluatePreProvider(
            string query,
            bool hasFinancePermission,
            Guid? sessionClienteId,
            Guid? contextClienteId,
            bool providerConfigured = true)
        {
            if (!providerConfigured)
            {
                var f1 = ApplyF1NoEvidence(query, new[] { "Provider Assist configurado" });
                return new AssistantResponse
                {
                    AnswerMarkdown = f1.AnswerMarkdown,
                    ConfidenceLevel = f1.ConfidenceLevel,
                    RecommendedActions = f1.RecommendedActions,
                    MissingInformation = f1.MissingInformation,
                    Warnings = new[] { WarningProviderUnavailable },
                    Evidence = Array.Empty<EvidenceItem>(),
                    CitedSources = Array.Empty<AssistantSourceCitation>(),
                    Provider = "PRIMOX_FAIL_CLOSED",
                    Timestamp = DateTimeOffset.Now
                };
            }

            if (LooksOutOfDomain(query))
            {
                return ApplyF2OutOfDomain(query);
            }

            if (LooksFinancial(query) && !hasFinancePermission)
            {
                return ApplyF3FinancialDenied(query);
            }

            if (contextClienteId.HasValue && sessionClienteId.HasValue && contextClienteId.Value != sessionClienteId.Value)
            {
                return ApplyF4CrossClientDenied(contextClienteId);
            }

            // contextClienteId sem sessão autenticada de cliente ⇒ negar
            if (contextClienteId.HasValue && !sessionClienteId.HasValue)
            {
                return ApplyF4CrossClientDenied(contextClienteId);
            }

            return null;
        }
    }
}
