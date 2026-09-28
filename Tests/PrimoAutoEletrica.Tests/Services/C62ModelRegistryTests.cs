using System.Linq;
using PrimoAutoEletrica.Services.Intelligence;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C62ModelRegistryTests
    {
        [Fact]
        public void C62_Registry_Has_Local_Rules_LocalModel_External_All_PRICE_NOT_VERIFIED_By_Default()
        {
            var reg = new ModelRegistry();
            var all = reg.ListAll();
            Assert.True(all.Count >= 3);
            Assert.All(all, m => Assert.Equal("PRICE_NOT_VERIFIED", m.PriceStatus));
            Assert.True(reg.TryGet("primox-grounded-local-rules", out var rules));
            Assert.Equal(IntelligenceExecutionMode.GroundedLocalRule, rules!.ExecutionMode);
            Assert.True(reg.TryGet("local-model-generic", out var local));
            Assert.True(local!.IsLocal);
            Assert.True(reg.TryGet("openai-compatible-external", out var ext));
            Assert.True(ext!.RequiresApiKey);
            Assert.Null(ext.InputPricePer1M);
            Assert.Null(ext.PriceCheckedAt);
            Assert.Equal(PriceSourceKind.PRICE_NOT_VERIFIED, ext.PriceSource);
        }

        [Fact]
        public void C62_Register_Verified_Price_Requires_Source_And_CheckedAt()
        {
            var reg = new ModelRegistry();
            reg.Register(new ModelDefinition
            {
                ModelId = "example-verified",
                DisplayName = "Example",
                ProviderFamily = "Test",
                ExecutionMode = IntelligenceExecutionMode.ExternalModel,
                InputPricePer1M = 0.15m,
                OutputPricePer1M = 0.60m,
                PriceSource = PriceSourceKind.ManualOperatorEntry,
                PriceCheckedAt = System.DateTimeOffset.Parse("2026-09-28T00:00:00-03:00"),
                PriceNotes = "Operator-entered fixture for unit test only — not a LIVE vendor quote"
            });
            var m = reg.GetRequired("example-verified");
            Assert.Equal("PRICE_VERIFIED", m.PriceStatus);
            Assert.NotNull(m.PriceCheckedAt);
            Assert.Equal(PriceSourceKind.ManualOperatorEntry, m.PriceSource);
        }

        [Fact]
        public void C62_Unverified_List_Excludes_Verified()
        {
            var reg = new ModelRegistry();
            reg.Register(new ModelDefinition
            {
                ModelId = "priced",
                DisplayName = "P",
                ProviderFamily = "T",
                ExecutionMode = IntelligenceExecutionMode.ExternalModel,
                InputPricePer1M = 1m,
                PriceSource = PriceSourceKind.VendorPublicPage,
                PriceCheckedAt = System.DateTimeOffset.UtcNow
            });
            Assert.DoesNotContain(reg.ListUnverifiedPrices(), x => x.ModelId == "priced");
            Assert.Contains(reg.ListUnverifiedPrices(), x => x.ModelId == "openai-compatible-external");
        }
    }
}