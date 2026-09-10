using System;

namespace PrimoAutoEletrica.Services
{
    public enum ShellNotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public sealed class ShellNotificationRequest
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string Details { get; init; } = string.Empty;
        public ShellNotificationType Type { get; init; } = ShellNotificationType.Info;
        public string ActionLabel { get; init; } = string.Empty;
        public string ActionModule { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
    }

    public static class ShellNotificationService
    {
        public static event EventHandler<ShellNotificationRequest>? NotificationPublished;

        public static void Publish(ShellNotificationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            NotificationPublished?.Invoke(null, request);
        }

        public static void PublishNavigationHint(
            string title,
            string message,
            string actionModule,
            string actionLabel,
            string details = "",
            ShellNotificationType type = ShellNotificationType.Info,
            string source = "")
        {
            Publish(new ShellNotificationRequest
            {
                Title = title,
                Message = message,
                Details = details,
                Type = type,
                ActionModule = actionModule,
                ActionLabel = actionLabel,
                Source = source
            });
        }
    }
}
