using System.Collections.Generic;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Apresentacao localizada de status de OS / Kanban.
    /// IDs internos permanecem em portugues tecnico (maquina de estados).
    /// </summary>
    public static class WorkOrderStatusLocalizer
    {
        private static readonly Dictionary<string, string> KeyByStatus = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Rascunho"] = "OsStatusDraft",
            ["Recebido"] = "OsStatusReceived",
            ["Em diagnostico"] = "OsStatusInDiagnosis",
            ["Em diagnóstico"] = "OsStatusInDiagnosis",
            ["Aguardando aprovacao"] = "OsStatusAwaitingApproval",
            ["Aguardando aprovação"] = "OsStatusAwaitingApproval",
            ["Aguardando peca"] = "OsStatusAwaitingPart",
            ["Aguardando peça"] = "OsStatusAwaitingPart",
            ["Em execucao"] = "OsStatusInExecution",
            ["Em execução"] = "OsStatusInExecution",
            ["Controle de qualidade"] = "OsStatusQualityCheck",
            ["Pronto"] = "OsStatusReady",
            ["Pronta para entrega"] = "OsStatusReady",
            ["Entregue"] = "OsStatusDelivered",
            ["Cancelado"] = "OsStatusCancelled",
            ["Cancelada"] = "OsStatusCancelled",
            ["Faturado"] = "OsStatusInvoiced",
            ["Faturada"] = "OsStatusInvoiced"
        };

        public static string Display(string? internalStatus)
        {
            if (string.IsNullOrWhiteSpace(internalStatus))
            {
                return string.Empty;
            }

            if (KeyByStatus.TryGetValue(internalStatus.Trim(), out var key))
            {
                return LocalizationService.Instance.GetString(key);
            }

            return internalStatus;
        }
    }
}
