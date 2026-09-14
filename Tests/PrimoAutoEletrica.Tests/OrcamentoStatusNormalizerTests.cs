using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public sealed class OrcamentoStatusNormalizerTests
    {
        [Theory]
        [InlineData(null, "Rascunho")]
        [InlineData("", "Rascunho")]
        [InlineData("Rejeitado", "Recusado")]
        [InlineData("Recusada", "Recusado")]
        [InlineData("Aprovada", "Aprovado")]
        [InlineData("Enviada", "Enviado")]
        [InlineData("Aprovado", "Aprovado")]
        public void Normalizar_CanonicalizesAliases(string? input, string expected)
        {
            Assert.Equal(expected, OrcamentoStatusNormalizer.Normalizar(input));
        }

        [Fact]
        public void Primox360_EhOrcamentoRecusado_AcceptsRejeitadoAlias()
        {
            Assert.True(Primox360Service.EhOrcamentoRecusado("Rejeitado"));
            Assert.True(Primox360Service.EhOrcamentoRecusado("Recusado"));
            Assert.False(Primox360Service.EhOrcamentoRecusado("Aprovado"));
        }
    }
}
