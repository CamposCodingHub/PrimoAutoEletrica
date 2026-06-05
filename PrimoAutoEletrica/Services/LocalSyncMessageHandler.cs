using System;
using System.Net;

namespace PrimoAutoEletrica.Services
{
    public class LocalSyncMessageHandler
    {
        private readonly LoggerService _logger;
        private readonly SynchronizationService? _synchronizationService;

        public LocalSyncMessageHandler(LoggerService logger, SynchronizationService? synchronizationService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _synchronizationService = synchronizationService;
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
                        try
                        {
                            App.Audit?.RegistrarSistema("LocalSync", message);

                            // Format: VendaRegistrada:{vendaId}:{valor}
                            var svcParts = message.Split(':');
                            var vendaId = svcParts.Length > 1 ? svcParts[1] : string.Empty;
                            decimal valor = 0m;
                            if (svcParts.Length > 2) decimal.TryParse(svcParts[2], out valor);

                            if (_synchronizationService != null && !string.IsNullOrWhiteSpace(vendaId))
                            {
                                _synchronizationService.RegistrarVendaPDV(vendaId, string.Empty, valor, string.Empty);
                                _logger.LogInfo($"LocalSync: registered sale event to SynchronizationService for {vendaId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Falha ao processar evento de venda LocalSync: {ex.Message}");
                        }

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
