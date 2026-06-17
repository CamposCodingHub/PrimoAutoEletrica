using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço para registrar eventos de sincronização entre estações multiusuário.
    /// </summary>
    public class SynchronizationService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly AppSessionService _session;

        public SynchronizationService(DatabaseService databaseService, LoggerService logger, AppSessionService session)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Registra um evento de mudança de estoque para sincronização.
        /// </summary>
        public void RegistrarMudancaEstoque(int produtoId, string produtoNome, int quantidadeAnterior, int quantidadeNova, string motivo)
        {
            try
            {
                var payload = $"ProdutoId={produtoId};Nome={produtoNome};QtdAnterior={quantidadeAnterior};QtdNova={quantidadeNova};Motivo={motivo}";
                RegistrarEvento("EstoqueMudanca", "Produto", produtoId.ToString(), payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao registrar mudanca de estoque: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra um evento de venda PDV para sincronização.
        /// </summary>
        public void RegistrarVendaPDV(string vendaId, string numero, decimal valorTotal, string cliente)
        {
            try
            {
                var payload = $"VendaId={vendaId};Numero={numero};Valor={valorTotal};Cliente={cliente}";
                RegistrarEvento("VendaPDV", "Venda", vendaId.ToString(), payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao registrar venda PDV: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra um evento financeiro para sincronização.
        /// </summary>
        public void RegistrarEventoFinanceiro(string tipo, string descricao, decimal valor)
        {
            try
            {
                var payload = $"Tipo={tipo};Descricao={descricao};Valor={valor}";
                RegistrarEvento("Financeiro", tipo, Guid.NewGuid().ToString(), payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao registrar evento financeiro: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica se ja existe um evento do mesmo tipo e entidade criado dentro de um intervalo de tempo.
        /// </summary>
        public bool EventoExiste(string eventType, string entityId, TimeSpan window)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 1 FROM SystemEvents
                    WHERE EventType = @EventType
                      AND EntityId = @EntityId
                      AND CreatedAt >= @Since
                    LIMIT 1;";
                command.Parameters.AddWithValue("@EventType", eventType);
                command.Parameters.AddWithValue("@EntityId", entityId);
                var since = DateTime.Now.Subtract(window).ToString("yyyy-MM-dd HH:mm:ss.fff");
                command.Parameters.AddWithValue("@Since", since);
                using var reader = command.ExecuteReader();
                return reader.Read();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao verificar existencia de evento de sincronizacao: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Registra um evento de ordem de serviço para sincronização.
        /// </summary>
        public void RegistrarOrdemServico(Guid osId, string numero, string status)
        {
            try
            {
                var payload = $"OSId={osId};Numero={numero};Status={status}";
                RegistrarEvento("OrdemServico", "OrdemServico", osId.ToString(), payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao registrar ordem de serviço: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra um evento genérico no log de sincronização.
        /// </summary>
        private void RegistrarEvento(string eventType, string entityType, string entityId, string payload)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO SystemEvents
                    (
                        Id,
                        EventType,
                        EntityType,
                        EntityId,
                        CreatedAt,
                        CreatedByUserId,
                        MachineName,
                        Payload,
                        Processed
                    )
                    VALUES
                    (
                        @Id,
                        @EventType,
                        @EntityType,
                        @EntityId,
                        @CreatedAt,
                        @CreatedByUserId,
                        @MachineName,
                        @Payload,
                        @Processed
                    );";

                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@EventType", eventType);
                command.Parameters.AddWithValue("@EntityType", entityType);
                command.Parameters.AddWithValue("@EntityId", entityId);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@CreatedByUserId", _session.UserId ?? 0);
                command.Parameters.AddWithValue("@MachineName", Environment.MachineName);
                command.Parameters.AddWithValue("@Payload", payload);
                command.Parameters.AddWithValue("@Processed", 0);

                command.ExecuteNonQuery();

                _logger.LogInfo($"Evento de sincronização registrado: {eventType}/{entityType}/{entityId}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao registrar evento de sincronização: {ex.Message}");
            }
        }

        /// <summary>
        /// Limpa eventos de sincronização processados antigos.
        /// </summary>
        public void LimparEventosProcessados(int diasParaManter = 7)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    DELETE FROM SystemEvents
                    WHERE Processed = 1
                    AND ProcessedAt < datetime(@CutoffDate);";

                var cutoffDate = DateTime.Now.AddDays(-diasParaManter);
                command.Parameters.AddWithValue("@CutoffDate", cutoffDate.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    _logger.LogInfo($"Eventos de sincronização processados limpos: {rowsAffected} eventos.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao limpar eventos de sincronização: {ex.Message}");
            }
        }
    }
}
