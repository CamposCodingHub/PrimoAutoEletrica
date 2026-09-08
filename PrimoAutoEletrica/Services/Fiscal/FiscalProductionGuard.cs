using System;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Impede uso acidental de ambiente Production até fase futura explícita.
    /// PRODUCTION permanece BLOCKED nesta fundação.
    /// </summary>
    public static class FiscalProductionGuard
    {
        /// <summary>Gate global: produção fiscal bloqueada até decisão futura.</summary>
        public static bool ProductionEmissionAllowed { get; private set; }

        public static void EnsureEnvironmentAllowed(FiscalEnvironment environment)
        {
            if (environment != FiscalEnvironment.Production)
            {
                return;
            }

            if (!ProductionEmissionAllowed)
            {
                throw new FiscalProductionBlockedException(
                    "Emissao fiscal em PRODUCAO esta bloqueada. Use Homologacao ate a fase de liberacao.");
            }
        }

        public static FiscalProviderResult? TryDenyProduction(
            FiscalEnvironment environment,
            Guid operationId,
            string idempotencyKey)
        {
            if (environment != FiscalEnvironment.Production || ProductionEmissionAllowed)
            {
                return null;
            }

            return FiscalProviderResult.Fail(
                FiscalDocumentStatus.ProductionBlocked,
                FiscalErrorKind.ProductionBlocked,
                "Producao fiscal bloqueada pela fundacao PRIMOX. Homologacao apenas.",
                operationId,
                idempotencyKey,
                internalCode: "FISCAL-PROD-BLOCKED");
        }

        /// <summary>Somente para testes unitários controlados — nunca chamar em runtime comercial.</summary>
        public static void SetProductionAllowedForTests(bool allowed) => ProductionEmissionAllowed = allowed;
    }

    public sealed class FiscalProductionBlockedException : InvalidOperationException
    {
        public FiscalProductionBlockedException(string message) : base(message)
        {
        }
    }
}
