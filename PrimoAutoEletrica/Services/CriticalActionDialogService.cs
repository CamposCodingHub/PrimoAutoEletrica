using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Views;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

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

            // Smoke headless: nao abre modal (evita travar suite).
            // Smoke visible: abre o modal DS para o operador acompanhar e auto-confirma.
            if (global::PrimoAutoEletrica.App.IsAutomatedTestMode
                && !global::PrimoAutoEletrica.App.IsSmokeVisible)
            {
                return true;
            }

            var dialog = new ConfirmacaoCriticaWindow(request);
            WindowOwnerHelper.ConfigureOwner(dialog, owner);

            if (global::PrimoAutoEletrica.App.IsAutomatedTestMode
                && global::PrimoAutoEletrica.App.IsSmokeVisible)
            {
                dialog.Loaded += (_, __) =>
                {
                    dialog.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1100) };
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            dialog.AutoConfirmForSmoke();
                        };
                        timer.Start();
                    }), DispatcherPriority.ApplicationIdle);
                };
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
