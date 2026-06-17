using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço para gerenciar bloqueio de registros (multiusuário).
    /// </summary>
    public class RecordLockService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly AppSessionService _appSession;
        private readonly AuditLogService _audit;

        private const int DefaultLockTimeoutMinutes = 30;

        public RecordLockService(DatabaseService databaseService, LoggerService logger, AppSessionService appSession, AuditLogService audit)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSession = appSession ?? throw new ArgumentNullException(nameof(appSession));
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        }

        /// <summary>
        /// Tenta bloquear um registro para edição.
        /// </summary>
        /// <param name="entityType">Tipo da entidade (ex: Cliente, Produto, OrdemServico).</param>
        /// <param name="entityId">ID da entidade.</param>
        /// <param name="entityDescription">Descrição da entidade (opcional).</param>
        /// <param name="timeoutMinutes">Tempo de expiração do bloqueio em minutos.</param>
        /// <returns>Resultado da tentativa de bloqueio.</returns>
        public LockResult TryLock(string entityType, string entityId, string? entityDescription = null, int timeoutMinutes = DefaultLockTimeoutMinutes)
        {
            if (!_appSession.IsAuthenticated)
            {
                return LockResult.CreateFailed("Usuário não autenticado.");
            }

            if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
            {
                return LockResult.CreateFailed("EntityType e EntityId são obrigatórios.");
            }

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                // Verificar se já existe lock ativo
                using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = @"
                    SELECT
                        Id,
                        LockedByUserId,
                        LockedByUserName,
                        SessionId,
                        MachineName,
                        LockedAt,
                        ExpiresAt
                    FROM RecordLocks
                    WHERE EntityType = @EntityType
                    AND EntityId = @EntityId
                    AND IsActive = 1
                    AND ExpiresAt > @Now;";

                checkCommand.Parameters.AddWithValue("@EntityType", entityType);
                checkCommand.Parameters.AddWithValue("@EntityId", entityId);
                checkCommand.Parameters.AddWithValue("@Now", DateTime.Now);

                using var reader = checkCommand.ExecuteReader();
                if (reader.Read())
                {
                    var lockedByUserId = reader.GetInt32(1);
                    var lockedByUserName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    var machineName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                    var lockedAt = ReadDateTime(reader, 5);

                    // Se o lock for do próprio usuário, renovar
                    if (lockedByUserId == _appSession.UserId)
                    {
                        RenewLock(entityType, entityId, timeoutMinutes);
                        return LockResult.CreateSuccess("Bloqueio renovado com sucesso.");
                    }

                    // Lock de outro usuário
                    var message = $"Este registro está sendo editado por {lockedByUserName} no computador {machineName} desde {lockedAt:HH:mm}.";
                    _logger.LogInfo($"Bloqueio negado: {entityType}/{entityId} bloqueado por {lockedByUserName} em {machineName}.");
                    return LockResult.CreateFailed(message);
                }

                reader.Dispose();

                // Criar novo lock
                using var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO RecordLocks
                    (
                        Id,
                        EntityType,
                        EntityId,
                        EntityDescription,
                        LockedByUserId,
                        LockedByUserName,
                        SessionId,
                        MachineName,
                        LockedAt,
                        ExpiresAt,
                        LastRenewedAt,
                        IsActive
                    )
                    VALUES
                    (
                        @Id,
                        @EntityType,
                        @EntityId,
                        @EntityDescription,
                        @LockedByUserId,
                        @LockedByUserName,
                        @SessionId,
                        @MachineName,
                        @LockedAt,
                        @ExpiresAt,
                        @LastRenewedAt,
                        @IsActive
                    );";

                var lockId = Guid.NewGuid();
                var expiresAt = DateTime.Now.AddMinutes(timeoutMinutes);

                insertCommand.Parameters.AddWithValue("@Id", lockId.ToString());
                insertCommand.Parameters.AddWithValue("@EntityType", entityType);
                insertCommand.Parameters.AddWithValue("@EntityId", entityId);
                insertCommand.Parameters.AddWithValue("@EntityDescription", entityDescription ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@LockedByUserId", _appSession.UserId ?? 0);
                insertCommand.Parameters.AddWithValue("@LockedByUserName", _appSession.UserName);
                insertCommand.Parameters.AddWithValue("@SessionId", _appSession.SessionId.ToString());
                insertCommand.Parameters.AddWithValue("@MachineName", Environment.MachineName);
                insertCommand.Parameters.AddWithValue("@LockedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                insertCommand.Parameters.AddWithValue("@ExpiresAt", expiresAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                insertCommand.Parameters.AddWithValue("@LastRenewedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                insertCommand.Parameters.AddWithValue("@IsActive", 1);

                insertCommand.ExecuteNonQuery();

                _audit.Registrar(
                    categoria: "Multiusuario",
                    acao: "RegistroBloqueado",
                    entidade: entityType,
                    entidadeId: entityId,
                    detalhes: $"Bloqueio criado por {_appSession.UserName} em {Environment.MachineName}. Expira em {timeoutMinutes} minutos.");

                _logger.LogInfo($"Bloqueio criado: {entityType}/{entityId} por {_appSession.UserName} em {Environment.MachineName}. Expira em {timeoutMinutes} minutos.");
                return LockResult.CreateSuccess("Registro bloqueado com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao bloquear registro {entityType}/{entityId}.", ex);
                return LockResult.CreateFailed($"Erro ao bloquear registro: {ex.Message}");
            }
        }

        /// <summary>
        /// Renova o bloqueio de um registro.
        /// </summary>
        public void RenewLock(string entityType, string entityId, int timeoutMinutes = DefaultLockTimeoutMinutes)
        {
            if (!_appSession.IsAuthenticated)
            {
                return;
            }

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE RecordLocks
                    SET ExpiresAt = @ExpiresAt,
                        LastRenewedAt = @LastRenewedAt
                    WHERE EntityType = @EntityType
                    AND EntityId = @EntityId
                    AND LockedByUserId = @LockedByUserId
                    AND IsActive = 1;";

                var expiresAt = DateTime.Now.AddMinutes(timeoutMinutes);

                command.Parameters.AddWithValue("@ExpiresAt", expiresAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@LastRenewedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@EntityType", entityType);
                command.Parameters.AddWithValue("@EntityId", entityId);
                command.Parameters.AddWithValue("@LockedByUserId", _appSession.UserId ?? 0);

                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    _logger.LogInfo($"Bloqueio renovado: {entityType}/{entityId} por {_appSession.UserName}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao renovar bloqueio {entityType}/{entityId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Libera o bloqueio de um registro.
        /// </summary>
        public void ReleaseLock(string entityType, string entityId)
        {
            if (!_appSession.IsAuthenticated)
            {
                return;
            }

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE RecordLocks
                    SET IsActive = 0
                    WHERE EntityType = @EntityType
                    AND EntityId = @EntityId
                    AND LockedByUserId = @LockedByUserId
                    AND IsActive = 1;";

                command.Parameters.AddWithValue("@EntityType", entityType);
                command.Parameters.AddWithValue("@EntityId", entityId);
                command.Parameters.AddWithValue("@LockedByUserId", _appSession.UserId ?? 0);

                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    _audit.Registrar(
                        categoria: "Multiusuario",
                        acao: "RegistroDesbloqueado",
                        entidade: entityType,
                        entidadeId: entityId,
                        detalhes: $"Bloqueio liberado por {_appSession.UserName} em {Environment.MachineName}.");

                    _logger.LogInfo($"Bloqueio liberado: {entityType}/{entityId} por {_appSession.UserName}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao liberar bloqueio {entityType}/{entityId}.", ex);
            }
        }

        /// <summary>
        /// Libera todos os bloqueios da sessão atual.
        /// </summary>
        public void ReleaseAllSessionLocks()
        {
            if (!_appSession.IsAuthenticated)
            {
                return;
            }

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE RecordLocks
                    SET IsActive = 0
                    WHERE SessionId = @SessionId
                    AND IsActive = 1;";

                command.Parameters.AddWithValue("@SessionId", _appSession.SessionId.ToString());

                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    _logger.LogInfo($"Bloqueios liberados: {rowsAffected} bloqueios da sessão {_appSession.SessionId}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao liberar bloqueios da sessão.", ex);
            }
        }

        /// <summary>
        /// Libera bloqueios expirados.
        /// </summary>
        public void CleanExpiredLocks()
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE RecordLocks
                    SET IsActive = 0
                    WHERE IsActive = 1
                    AND ExpiresAt < @Now;";
                command.Parameters.AddWithValue("@Now", DateTime.Now);

                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    _logger.LogInfo($"Bloqueios expirados limpos: {rowsAffected} bloqueios.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao limpar bloqueios expirados.", ex);
            }
        }

        /// <summary>
        /// Obtém informações de bloqueio de um registro.
        /// </summary>
        public LockInfo? GetLockInfo(string entityType, string entityId)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        Id,
                        EntityType,
                        EntityId,
                        EntityDescription,
                        LockedByUserId,
                        LockedByUserName,
                        SessionId,
                        MachineName,
                        LockedAt,
                        ExpiresAt,
                        LastRenewedAt,
                        IsActive
                    FROM RecordLocks
                    WHERE EntityType = @EntityType
                    AND EntityId = @EntityId
                    AND IsActive = 1
                    AND ExpiresAt > @Now;";

                command.Parameters.AddWithValue("@EntityType", entityType);
                command.Parameters.AddWithValue("@EntityId", entityId);
                command.Parameters.AddWithValue("@Now", DateTime.Now);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return new LockInfo
                    {
                        Id = ReadGuid(reader, 0),
                        EntityType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        EntityId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        EntityDescription = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        LockedByUserId = reader.GetInt32(4),
                        LockedByUserName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        SessionId = ReadGuid(reader, 6),
                        MachineName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        LockedAt = ReadDateTime(reader, 8),
                        ExpiresAt = ReadDateTime(reader, 9),
                        LastRenewedAt = ReadDateTime(reader, 10),
                        IsActive = ReadBool(reader, 11)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter informações de bloqueio {entityType}/{entityId}.", ex);
            }

            return null;
        }

        /// <summary>
        /// Obtém todos os bloqueios ativos.
        /// </summary>
        public List<LockInfo> GetActiveLocks()
        {
            var locks = new List<LockInfo>();

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        Id,
                        EntityType,
                        EntityId,
                        EntityDescription,
                        LockedByUserId,
                        LockedByUserName,
                        SessionId,
                        MachineName,
                        LockedAt,
                        ExpiresAt,
                        LastRenewedAt,
                        IsActive
                    FROM RecordLocks
                    WHERE IsActive = 1
                    AND ExpiresAt > @Now
                    ORDER BY LockedAt DESC;";
                command.Parameters.AddWithValue("@Now", DateTime.Now);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    locks.Add(new LockInfo
                    {
                        Id = ReadGuid(reader, 0),
                        EntityType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        EntityId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        EntityDescription = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        LockedByUserId = reader.GetInt32(4),
                        LockedByUserName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        SessionId = ReadGuid(reader, 6),
                        MachineName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        LockedAt = ReadDateTime(reader, 8),
                        ExpiresAt = ReadDateTime(reader, 9),
                        LastRenewedAt = ReadDateTime(reader, 10),
                        IsActive = ReadBool(reader, 11)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao obter bloqueios ativos.", ex);
            }

            return locks;
        }

        private static Guid ReadGuid(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return Guid.Empty;
            }

            var value = reader.GetValue(ordinal);
            return value is Guid guid ? guid : Guid.Parse(Convert.ToString(value) ?? string.Empty);
        }

        private static DateTime ReadDateTime(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return DateTime.MinValue;
            }

            var value = reader.GetValue(ordinal);
            return value is DateTime dateTime ? dateTime : DateTime.Parse(Convert.ToString(value) ?? string.Empty);
        }

        private static bool ReadBool(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return false;
            }

            var value = reader.GetValue(ordinal);
            return value is bool boolean ? boolean : Convert.ToInt32(value) != 0;
        }
    }

    /// <summary>
    /// Resultado da tentativa de bloqueio.
    /// </summary>
    public class LockResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public static LockResult CreateSuccess(string message) => new LockResult { Success = true, Message = message };
        public static LockResult CreateFailed(string message) => new LockResult { Success = false, Message = message };
    }

    /// <summary>
    /// Informações de um bloqueio de registro.
    /// </summary>
    public class LockInfo
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityDescription { get; set; } = string.Empty;
        public int LockedByUserId { get; set; }
        public string LockedByUserName { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public DateTime LockedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastRenewedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
