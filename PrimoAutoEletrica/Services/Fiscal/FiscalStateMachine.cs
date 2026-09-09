using System;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Transições válidas de status fiscal. Bloqueia caminhos impossíveis
    /// (ex.: Rejected → Authorized, Cancelled → Processing).
    /// </summary>
    public static class FiscalStateMachine
    {
        public static bool CanTransition(FiscalDocumentStatus from, FiscalDocumentStatus to)
        {
            if (from == to)
            {
                return true;
            }

            return from switch
            {
                FiscalDocumentStatus.Draft => to is FiscalDocumentStatus.Validating
                    or FiscalDocumentStatus.Pending
                    or FiscalDocumentStatus.Failed
                    or FiscalDocumentStatus.NotConfigured
                    or FiscalDocumentStatus.ProductionBlocked,

                FiscalDocumentStatus.Validating => to is FiscalDocumentStatus.Pending
                    or FiscalDocumentStatus.Failed
                    or FiscalDocumentStatus.NotConfigured
                    or FiscalDocumentStatus.ProductionBlocked,

                FiscalDocumentStatus.Pending => to is FiscalDocumentStatus.Processing
                    or FiscalDocumentStatus.Failed
                    or FiscalDocumentStatus.Rejected
                    or FiscalDocumentStatus.Authorized
                    or FiscalDocumentStatus.Unknown
                    or FiscalDocumentStatus.ProductionBlocked
                    or FiscalDocumentStatus.NotConfigured
                    or FiscalDocumentStatus.NotImplemented,

                FiscalDocumentStatus.Processing => to is FiscalDocumentStatus.Authorized
                    or FiscalDocumentStatus.Rejected
                    or FiscalDocumentStatus.Failed
                    or FiscalDocumentStatus.Unknown
                    or FiscalDocumentStatus.Cancelled
                    or FiscalDocumentStatus.Denied
                    or FiscalDocumentStatus.ProductionBlocked
                    or FiscalDocumentStatus.NotConfigured
                    or FiscalDocumentStatus.NotImplemented,

                FiscalDocumentStatus.Unknown => to is FiscalDocumentStatus.Authorized
                    or FiscalDocumentStatus.Rejected
                    or FiscalDocumentStatus.Failed
                    or FiscalDocumentStatus.Processing
                    or FiscalDocumentStatus.Cancelled
                    or FiscalDocumentStatus.Pending,

                FiscalDocumentStatus.Authorized => to is FiscalDocumentStatus.Cancelled
                    or FiscalDocumentStatus.Denied,

                FiscalDocumentStatus.Rejected => false,
                FiscalDocumentStatus.Cancelled => false,
                FiscalDocumentStatus.Denied => false,
                FiscalDocumentStatus.ProductionBlocked => false,
                FiscalDocumentStatus.NotConfigured => to is FiscalDocumentStatus.Pending
                    or FiscalDocumentStatus.Failed
                    or FiscalDocumentStatus.Validating
                    or FiscalDocumentStatus.Draft,
                FiscalDocumentStatus.NotImplemented => false,
                FiscalDocumentStatus.Failed => to is FiscalDocumentStatus.Pending
                    or FiscalDocumentStatus.Validating
                    or FiscalDocumentStatus.Draft
                    or FiscalDocumentStatus.Processing,
                FiscalDocumentStatus.Contingency => to is FiscalDocumentStatus.Authorized
                    or FiscalDocumentStatus.Rejected
                    or FiscalDocumentStatus.Failed
                    or FiscalDocumentStatus.Cancelled
                    or FiscalDocumentStatus.Unknown,
                _ => false
            };
        }

        public static void EnsureTransition(FiscalDocumentStatus from, FiscalDocumentStatus to)
        {
            if (!CanTransition(from, to))
            {
                throw new InvalidOperationException(
                    $"Transicao fiscal invalida: {from} → {to}.");
            }
        }

        public static bool CanCancel(FiscalDocumentStatus status, FiscalEnvironment environment)
            => status == FiscalDocumentStatus.Authorized
               && environment != FiscalEnvironment.Production;

        public static bool CanConsult(FiscalDocumentStatus status)
            => status is FiscalDocumentStatus.Processing
                or FiscalDocumentStatus.Pending
                or FiscalDocumentStatus.Unknown
                or FiscalDocumentStatus.Contingency;
    }
}
