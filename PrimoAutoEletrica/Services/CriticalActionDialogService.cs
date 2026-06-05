using PrimoAutoEletrica.Views;
using System;
using System.Linq;
using System.Windows;

namespace PrimoAutoEletrica.Services
{
    public sealed class CriticalActionRequest
    {
        public string WindowTitle { get; init; } = "Confirmacao critica";
        public string Header { get; init; } = "Revise antes de continuar";
        public string Summary { get; init; } = string.Empty;
        public string Details { get; init; } = string.Empty;
        public string Impact { get; init; } = "Esta acao altera dados importantes do sistema.";
        public string Keyword { get; init; } = "CONFIRMAR";
        public string ConfirmButtonText { get; init; } = "Confirmar";
        public string CancelButtonText { get; init; } = "Voltar";
    }

    public static class CriticalActionDialogService
    {
        public static bool ConfirmarAcao(Window? owner, CriticalActionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
            {
                return true;
            }

            var dialog = new ConfirmacaoCriticaWindow(request);
            var ownerResolvido = owner
                ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                ?? Application.Current?.MainWindow;

            if (ownerResolvido != null && ownerResolvido != dialog)
            {
                dialog.Owner = ownerResolvido;
            }

            return dialog.ShowDialog() == true;
        }

        public static bool ConfirmarExclusao(
            Window? owner,
            string entidade,
            string identificador,
            string details,
            string impact)
        {
            return ConfirmarAcao(owner, new CriticalActionRequest
            {
                WindowTitle = $"Excluir {entidade}",
                Header = $"Exclusao de {entidade}",
                Summary = $"Voce esta prestes a excluir {entidade} '{identificador}'.",
                Details = details,
                Impact = impact,
                Keyword = "EXCLUIR",
                ConfirmButtonText = "Excluir registro"
            });
        }

        public static bool ConfirmarCancelamento(
            Window? owner,
            string contexto,
            string identificador,
            string details,
            string impact)
        {
            return ConfirmarAcao(owner, new CriticalActionRequest
            {
                WindowTitle = $"Cancelar {contexto}",
                Header = $"Cancelamento de {contexto}",
                Summary = $"Voce esta prestes a cancelar {contexto} '{identificador}'.",
                Details = details,
                Impact = impact,
                Keyword = "CANCELAR",
                ConfirmButtonText = "Cancelar agora"
            });
        }
    }
}
