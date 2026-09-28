using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.ExternalAi;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C53CorrelationIdTests
    {
        private static string TempDb()
        {
            var dir = Path.Combine(Path.GetTempPath(), "primox-c53", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "intelligence-audit.db");
        }

        private static void TryDelete(string dbPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch { }
        }

        private static AssistantQueryContext Ctx(string corr, string query, string evidenceId)
        {
            return new AssistantQueryContext
            {
                Query = query,
                CorrelationId = corr,
                RetrievedEvidence = new[]
                {
                    new EvidenceItem
                    {
                        EvidenceId = evidenceId,
                        SourceCode = "KB-" + evidenceId.Substring(0, 4),
                        Title = "t-" + query,
                        Excerpt = "ex-" + query
                    }
                },
                Parameters = new Dictionary<string, object>
                {
                    ["UserId"] = 5,
                    ["SessionId"] = "sess-" + corr.Substring(0, 8)
                }
            };
        }

        [Fact]
        public async Task C53_Response_And_Audit_Share_CorrelationId()
        {
            var path = TempDb();
            try
            {
                var corr = Guid.NewGuid().ToString("N");
                var audit = new PersistentIntelligenceAuditService(path);
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(new ExternalAssistantOptions { Enabled = false }),
                    audit: audit);

                var resp = await router.AskAsync(Ctx(corr, "req-A-alternador", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
                Assert.Equal(corr, resp.CorrelationId);

                var rows = new PersistentIntelligenceAuditService(path)
                    .FilterForReader(5, "Tecnico", correlationId: corr);
                Assert.NotEmpty(rows);
                Assert.All(rows, e => Assert.Equal(corr, e.CorrelationId));
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public async Task C53_RequestA_vs_RequestB_No_Mixing_In_Audit()
        {
            var path = TempDb();
            try
            {
                var corrA = "a" + Guid.NewGuid().ToString("N").Substring(1);
                var corrB = "b" + Guid.NewGuid().ToString("N").Substring(1);
                var audit = new PersistentIntelligenceAuditService(path);
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(new ExternalAssistantOptions { Enabled = false }),
                    audit: audit);

                var respA = await router.AskAsync(Ctx(corrA, "question-A-battery", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
                var respB = await router.AskAsync(Ctx(corrB, "question-B-starter", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

                Assert.Equal(corrA, respA.CorrelationId);
                Assert.Equal(corrB, respB.CorrelationId);
                Assert.NotEqual(corrA, corrB);

                var reader = new PersistentIntelligenceAuditService(path);
                var rowsA = reader.FilterForReader(5, "Administrador", correlationId: corrA, take: 20);
                var rowsB = reader.FilterForReader(5, "Administrador", correlationId: corrB, take: 20);

                Assert.NotEmpty(rowsA);
                Assert.NotEmpty(rowsB);
                Assert.All(rowsA, e =>
                {
                    Assert.Equal(corrA, e.CorrelationId);
                    Assert.DoesNotContain("question-B", e.Question ?? "", StringComparison.OrdinalIgnoreCase);
                });
                Assert.All(rowsB, e =>
                {
                    Assert.Equal(corrB, e.CorrelationId);
                    Assert.DoesNotContain("question-A", e.Question ?? "", StringComparison.OrdinalIgnoreCase);
                });
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public void C53_EvidencePackage_RequestId_Equals_Context_CorrelationId()
        {
            var corr = Guid.NewGuid().ToString("N");
            var ctx = Ctx(corr, "pkg", "cccccccccccccccccccccccccccccccc");
            var pkg = new ExternalEvidencePackageBuilder().Build(ctx);
            Assert.Equal(corr, pkg.RequestId);
        }

        [Fact]
        public void C53_Chain_User_Assist_Context_Evidence_Provider_Grounding_Audit_Documented()
        {
            // Structural proof: CorrelationId exists on context, response, package RequestId, audit entry.
            Assert.NotNull(typeof(AssistantQueryContext).GetProperty("CorrelationId"));
            Assert.NotNull(typeof(AssistantResponse).GetProperty("CorrelationId"));
            Assert.NotNull(typeof(ExternalEvidencePackage).GetProperty("RequestId"));
            Assert.NotNull(typeof(IntelligenceAuditEntry).GetProperty("CorrelationId"));
        }
    }
}