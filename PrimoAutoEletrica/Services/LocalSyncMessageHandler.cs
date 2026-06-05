using System;
using System.Net;

namespace PrimoAutoEletrica.Services
{
    public class LocalSyncMessageHandler
    {
        private readonly LoggerService _logger;

        public LocalSyncMessageHandler(LoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Handle(string message, IPEndPoint remote)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                var parts = message.Split(':', 3);
                var verb = parts.Length > 0 ? parts[0] : string.Empty;

                switch (verb)
                {
                    case "SIM":
                        _logger.LogInfo($"LocalSync heartbeat from {remote.Address}: {message}");
                        break;

                    case "VendaRegistrada":
                    case "VendaCancelada":
                        _logger.LogInfo($"LocalSync event {verb} received from {remote.Address}: {message}");
                        // Registrar na auditoria para analise; integrações mais profundas podem ser adicionadas aqui
                        try
                        {
                            App.Audit?.RegistrarSistema("LocalSync", message);
                        }
                        catch { }
                        break;

                    default:
                        _logger.LogInfo($"LocalSync message received: {message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao processar mensagem LocalSync: {ex.Message}");
            }
        }
    }
}
