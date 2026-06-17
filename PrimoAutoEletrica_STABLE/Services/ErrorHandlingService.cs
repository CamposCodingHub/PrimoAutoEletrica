using PrimoAutoEletrica.Views;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Services
{
    public sealed record FriendlyErrorInfo(
        string CorrelationId,
        string Context,
        string Action,
        string UserMessage,
        string TechnicalDetails);

    public static class ErrorHandlingService
    {
        public static FriendlyErrorInfo HandleException(
            string context,
            string action,
            Exception exception,
            string? userMessage = null,
            bool showWindow = true)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var correlationId = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var safeContext = string.IsNullOrWhiteSpace(context) ? "Sistema" : context.Trim();
            var safeAction = string.IsNullOrWhiteSpace(action) ? "Operacao" : action.Trim();
            var friendlyMessage = string.IsNullOrWhiteSpace(userMessage)
                ? "Nao foi possivel concluir esta operacao. O erro foi registrado e voce pode tentar novamente ou chamar suporte com o codigo abaixo."
                : userMessage.Trim();
            var technicalDetails =
                $"Codigo: {correlationId}{Environment.NewLine}" +
                $"Contexto: {safeContext}{Environment.NewLine}" +
                $"Acao: {safeAction}{Environment.NewLine}" +
                $"Erro: {exception.GetType().FullName}: {exception.Message}{Environment.NewLine}" +
                $"StackTrace:{Environment.NewLine}{exception.StackTrace}";

            try
            {
                App.Logger.LogError($"Erro amigavel registrado. Codigo={correlationId}; Contexto={safeContext}; Acao={safeAction}; Mensagem={exception.Message}", exception, safeContext);
            }
            catch
            {
            }

            try
            {
                App.Audit.RegistrarErro(safeContext, safeAction, exception, criticidade: "Error");
            }
            catch
            {
            }

            var info = new FriendlyErrorInfo(correlationId, safeContext, safeAction, friendlyMessage, technicalDetails);

            if (!App.IsAutomatedTestMode && showWindow)
            {
                var window = new FriendlyErrorWindow(info)
                {
                    Owner = Application.Current?.MainWindow
                };
                window.ShowDialog();
            }

            return info;
        }
    }
}
