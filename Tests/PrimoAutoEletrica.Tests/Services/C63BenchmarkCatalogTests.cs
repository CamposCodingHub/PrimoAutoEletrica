using PrimoAutoEletrica.Services.Intelligence;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C63BenchmarkCatalogTests
    {
        [Fact]
        public void C63_Catalog_Has_At_Least_100_Cases_Across_28_Domains()
        {
            Assert.Equal(28, PrimoxBenchmarkCatalog.Domains.Length);
            Assert.True(PrimoxBenchmarkCatalog.Count >= 100, "Count=" + PrimoxBenchmarkCatalog.Count);
            Assert.Equal(112, PrimoxBenchmarkCatalog.Count);
            foreach (var d in PrimoxBenchmarkCatalog.Domains)
                Assert.True(PrimoxBenchmarkCatalog.ByDomain(d).Count >= 4, d);
            var sample = PrimoxBenchmarkCatalog.All[0];
            Assert.False(string.IsNullOrWhiteSpace(sample.Question));
            Assert.False(string.IsNullOrWhiteSpace(sample.ExpectedKnowledge));
            Assert.False(string.IsNullOrWhiteSpace(sample.ExpectedReasoning));
            Assert.NotNull(sample.RequiredEvidence);
            Assert.NotNull(sample.ForbiddenAssumptions);
            Assert.False(string.IsNullOrWhiteSpace(sample.ExpectedAnswerCharacteristics));
            Assert.NotNull(sample.SafetyConstraints);
        }
    }
}