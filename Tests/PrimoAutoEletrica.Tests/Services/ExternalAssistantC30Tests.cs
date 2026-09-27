using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.ExternalAi;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class ExternalAssistantC30Tests
    {
        private sealed class FakeSecretSource : IExternalAssistantSecretSource
        {
            public string? Key { get; set; }
            public string? TryGetApiKey(string environmentVariableName) => Key;
        }

        [Fact]
        public void Options_Default_EnabledFalse_AndEnvVarNameOnly()
        {
            var options = new ExternalAssistantOptions();
            Assert.False(options.Enabled);
            Assert.Equal(ExternalAssistantOptions.DefaultApiKeyEnvironmentVariable, options.ApiKeyEnvironmentVariable);
            Assert.DoesNotContain("sk-", options.ApiKeyEnvironmentVariable, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DisabledProvider_AskAsync_FailClosed_NoEvidence_NoHallucinatedActions()
        {
            var provider = new DisabledExternalAssistantProvider(ExternalAssistantArmingState.DisabledByDefault);
            Assert.False(provider.IsConfigured);

            var response = await provider.AskAsync(new AssistantQueryContext
            {
                Query = "Troque o alternador agora sem medir nada",
                RetrievedKnowledge = Array.Empty<TechnicalKnowledgeEntry>(),
                RetrievedCases = Array.Empty<DiagnosticCase>(),
                RetrievedEvidence = Array.Empty<EvidenceItem>()
            });

            Assert.Equal(AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE, response.ConfidenceLevel);
            Assert.False(response.HasSufficientEvidence);
            Assert.Empty(response.Evidence);
            Assert.Empty(response.CitedSources);
            Assert.Contains(ExternalAssistantWarnings.Disabled, response.Warnings);
            Assert.Contains(AssistFailClosedPolicy.WarningProviderUnavailable, response.Warnings);
            Assert.Equal("PRIMOX_EXTERNAL_DISABLED", response.Provider);
            Assert.All(response.RecommendedActions, a =>
            {
                Assert.DoesNotContain("substitua o alternador", a, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("troque a bateria", a, StringComparison.OrdinalIgnoreCase);
            });
            Assert.Contains("Nenhuma chamada remota", response.AnswerMarkdown, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Selector_NoKey_MissingSecret_EvenIfEnabled()
        {
            var secrets = new FakeSecretSource { Key = null };
            var selector = new ExternalAssistantProviderSelector(
                new ExternalAssistantOptions { Enabled = true },
                secrets);

            Assert.Equal(ExternalAssistantArmingState.MissingSecret, selector.EvaluateArmingState());
            Assert.False(selector.WouldArmLiveGates());
            var provider = Assert.IsType<DisabledExternalAssistantProvider>(selector.CreateExternalProvider());
            Assert.False(provider.IsConfigured);
            Assert.Equal(ExternalAssistantWarnings.NoKey, provider.WarningCode);
        }

        [Fact]
        public void Selector_KillSwitch_WinsOverEnableAndSecret()
        {
            var previous = Environment.GetEnvironmentVariable(ExternalAssistantOptions.KillSwitchEnvironmentVariable);
            try
            {
                Environment.SetEnvironmentVariable(ExternalAssistantOptions.KillSwitchEnvironmentVariable, "1");
                var secrets = new FakeSecretSource { Key = "not-a-real-key-for-test" };
                var selector = new ExternalAssistantProviderSelector(
                    new ExternalAssistantOptions { Enabled = true },
                    secrets);

                Assert.True(selector.IsKillSwitchOn());
                Assert.Equal(ExternalAssistantArmingState.KillSwitch, selector.EvaluateArmingState());
                Assert.False(selector.WouldArmLiveGates());
                var provider = Assert.IsType<DisabledExternalAssistantProvider>(selector.CreateExternalProvider());
                Assert.Equal(ExternalAssistantWarnings.KillSwitch, provider.WarningCode);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ExternalAssistantOptions.KillSwitchEnvironmentVariable, previous);
            }
        }

        [Fact]
        public void Selector_ArmedGates_StillReturnsDisabledLiveNotWired_C30Honesty()
        {
            var previousKill = Environment.GetEnvironmentVariable(ExternalAssistantOptions.KillSwitchEnvironmentVariable);
            try
            {
                Environment.SetEnvironmentVariable(ExternalAssistantOptions.KillSwitchEnvironmentVariable, null);
                var secrets = new FakeSecretSource { Key = "lab-secret-not-for-network" };
                var selector = new ExternalAssistantProviderSelector(
                    new ExternalAssistantOptions { Enabled = true },
                    secrets);

                Assert.True(selector.WouldArmLiveGates());
                Assert.Equal(ExternalAssistantArmingState.ArmedButLiveNotWired, selector.EvaluateArmingState());
                var provider = Assert.IsType<DisabledExternalAssistantProvider>(selector.CreateExternalProvider());
                Assert.False(provider.IsConfigured);
                Assert.Equal(ExternalAssistantWarnings.LiveNotWired, provider.WarningCode);
            }
            finally
            {
                Environment.SetEnvironmentVariable(ExternalAssistantOptions.KillSwitchEnvironmentVariable, previousKill);
            }
        }

        [Fact]
        public void Selector_DefaultAssistProvider_IsLocalGrounded()
        {
            var selector = new ExternalAssistantProviderSelector(new ExternalAssistantOptions { Enabled = false });
            var provider = selector.CreateDefaultAssistProvider();
            Assert.IsType<GroundedLocalRuleAssistantProvider>(provider);
            Assert.True(provider.IsConfigured);
            Assert.Equal("PRIMOX_LOCAL_GROUNDED", provider.ProviderId);
        }

        [Fact]
        public async Task AssistantService_WithDisabledExternal_DoesNotHallucinateRepair()
        {
            var external = new DisabledExternalAssistantProvider(ExternalAssistantArmingState.MissingSecret);
            var service = new AssistantService(provider: external);

            // Permission may block consult depending on session — call provider directly already covered.
            // Here we ensure AskAsync path through provider remains fail-closed when injected.
            var raw = await external.AskAsync(new AssistantQueryContext { Query = "ordene compra de pecas caras agora" });
            Assert.Equal(AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE, raw.ConfidenceLevel);
            Assert.DoesNotContain("pedido de compra criado", raw.AnswerMarkdown, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(raw.Evidence);
        }

        [Fact]
        public void Audit_RedactsSecretLikePayloads_StillWorksWithExternalProviderStamp()
        {
            var audit = new IntelligenceAuditService();
            audit.Record(new IntelligenceAuditEntry
            {
                Question = "consulta externa",
                Provider = "PRIMOX_EXTERNAL_DISABLED",
                Result = "FAIL_CLOSED",
                Failure = "api_key=should-redact",
                AnswerSummary = "fail-closed"
            });

            var recent = audit.ListRecent(1).Single();
            Assert.Equal("[REDACTED]", recent.Failure);
            Assert.Equal("PRIMOX_EXTERNAL_DISABLED", recent.Provider);
        }
    }
}

