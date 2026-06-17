using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço para gerenciar sessões de usuário no banco de dados (multiusuário).
    /// </summary>
    public class UserSessionService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly AppSessionService _appSession;

        public UserSessionService(DatabaseService databaseService, LoggerService logger, AppSessionService appSession)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSession = appSession ?? throw new ArgumentNullException(nameof(appSession));
        }

        /// <summary>
        /// Cria uma nova sessão no banco.
        /// </summary>
        public void CreateSession()
        {
            if (!_appSession.IsAuthenticated)
            {
                _logger.LogWarning("Tentativa de criar sessão sem usuário autenticado.");
                return;
            }

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO UserSessions
                    (
                        Id,
                        SessionId,
                        UserId,
                        NomeUsuario,
                        Perfil,
                        MachineName,
                        MachineUserName,
                        IpAddress,
                        LoginAt,
                        LastSeenAt,
                        IsActive,
                        AppVersion,
                        DatabaseProvider
                    )
                    VALUES
                    (
                        @Id,
                        @SessionId,
                        @UserId,
                        @NomeUsuario,
                        @Perfil,
                        @MachineName,
                        @MachineUserName,
                        @IpAddress,
                        @LoginAt,
                        @LastSeenAt,
                        @IsActive,
                        @AppVersion,
                        @DatabaseProvider
                    );";

                var id = Guid.NewGuid();
                var sessionId = _appSession.SessionId;
                var userId = _appSession.UserId ?? 0;
                var nomeUsuario = _appSession.UserName;
                var perfil = _appSession.AccessProfile;
                var machineName = Environment.MachineName;
                var machineUserName = Environment.UserName;
                var appVersion = GetAppVersion();
                var databaseProvider = GetDatabaseProvider();

                command.Parameters.AddWithValue("@Id", id.ToString());
                command.Parameters.AddWithValue("@SessionId", sessionId.ToString());
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@NomeUsuario", nomeUsuario);
                command.Parameters.AddWithValue("@Perfil", perfil);
                command.Parameters.AddWithValue("@MachineName", machineName);
                command.Parameters.AddWithValue("@MachineUserName", machineUserName);
                command.Parameters.AddWithValue("@IpAddress", DBNull.Value);
                command.Parameters.AddWithValue("@LoginAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@LastSeenAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@IsActive", 1);
                command.Parameters.AddWithValue("@AppVersion", appVersion);
                command.Parameters.AddWithValue("@DatabaseProvider", databaseProvider);

                command.ExecuteNonQuery();

                _logger.LogInfo($"Sessão criada no banco: Usuario='{nomeUsuario}', SessionId='{sessionId}', Machine='{machineName}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao criar sessão no banco.", ex);
            }
        }

        /// <summary>
        /// Atualiza o LastSeenAt da sessão atual (heartbeat).
        /// </summary>
        public void UpdateHeartbeat()
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
                    UPDATE UserSessions
                    SET LastSeenAt = @LastSeenAt
                    WHERE SessionId = @SessionId AND IsActive = 1;";

                command.Parameters.AddWithValue("@LastSeenAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@SessionId", _appSession.SessionId.ToString());

                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    _logger.LogInfo($"Heartbeat atualizado: SessionId='{_appSession.SessionId}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao atualizar heartbeat: {ex.Message}");
            }
        }

        /// <summary>
        /// Encerra a sessão atual no banco.
        /// </summary>
        public void EndSession()
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
                    UPDATE UserSessions
                    SET LogoutAt = @LogoutAt,
                        IsActive = 0
                    WHERE SessionId = @SessionId AND IsActive = 1;";

                command.Parameters.AddWithValue("@LogoutAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@SessionId", _appSession.SessionId.ToString());

                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    _logger.LogInfo($"Sessão encerrada no banco: SessionId='{_appSession.SessionId}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao encerrar sessão no banco.", ex);
            }
        }

        /// <summary>
        /// Obtém todas as sessões ativas.
        /// </summary>
        public List<UserSessionInfo> GetActiveSessions()
        {
            var sessions = new List<UserSessionInfo>();

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        Id,
                        SessionId,
                        UserId,
                        NomeUsuario,
                        Perfil,
                        MachineName,
                        MachineUserName,
                        IpAddress,
                        LoginAt,
                        LastSeenAt,
                        LogoutAt,
                        IsActive,
                        AppVersion,
                        DatabaseProvider
                    FROM UserSessions
                    WHERE IsActive = 1
                    ORDER BY LoginAt DESC;";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    sessions.Add(new UserSessionInfo
                    {
                        Id = ReadGuid(reader, 0),
                        SessionId = ReadGuid(reader, 1),
                        UserId = ReadInt(reader, 2),
                        NomeUsuario = ReadString(reader, 3),
                        Perfil = ReadString(reader, 4),
                        MachineName = ReadString(reader, 5),
                        MachineUserName = ReadString(reader, 6),
                        IpAddress = ReadString(reader, 7),
                        LoginAt = ReadDate(reader, 8, DateTime.MinValue),
                        LastSeenAt = ReadDate(reader, 9, DateTime.MinValue),
                        LogoutAt = ReadNullableDate(reader, 10),
                        IsActive = ReadBool(reader, 11),
                        AppVersion = ReadString(reader, 12),
                        DatabaseProvider = ReadString(reader, 13)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao obter sessões ativas.", ex);
            }

            return sessions;
        }

        /// <summary>
        /// Limpa sessões expiradas (inativas por mais de X minutos).
        /// </summary>
        public void CleanExpiredSessions(int timeoutMinutes = 60)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE UserSessions
                    SET LogoutAt = @Now,
                        IsActive = 0
                    WHERE IsActive = 1
                    AND LastSeenAt < @CutoffTime;";

                var cutoffTime = DateTime.Now.AddMinutes(-timeoutMinutes);
                command.Parameters.AddWithValue("@Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@CutoffTime", cutoffTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    _logger.LogInfo($"Sessões expiradas limpas: {rowsAffected} sessões.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao limpar sessões expiradas.", ex);
            }
        }

        private static string GetAppVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetDatabaseProvider()
        {
            try
            {
                var settings = DatabaseConnectionSettingsService.LoadOrCreateDefault(App.RuntimeAppDataPath, App.Logger);
                return settings.Provider ?? "SQLite";
            }
            catch
            {
                return "SQLite";
            }
        }

        private static string ReadString(DbDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal)) ?? string.Empty;
        }

        private static int ReadInt(DbDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static bool ReadBool(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return false;
            }

            return reader.GetValue(ordinal) switch
            {
                bool value => value,
                byte value => value != 0,
                short value => value != 0,
                int value => value != 0,
                long value => value != 0,
                _ => bool.TryParse(ReadString(reader, ordinal), out var parsed) && parsed
            };
        }

        private static Guid ReadGuid(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return Guid.Empty;
            }

            var value = reader.GetValue(ordinal);
            return value is Guid guid || Guid.TryParse(Convert.ToString(value), out guid)
                ? guid
                : Guid.Empty;
        }

        private static DateTime ReadDate(DbDataReader reader, int ordinal, DateTime fallback)
        {
            if (reader.IsDBNull(ordinal))
            {
                return fallback;
            }

            var value = reader.GetValue(ordinal);
            return value is DateTime date || DateTime.TryParse(Convert.ToString(value), out date)
                ? date
                : fallback;
        }

        private static DateTime? ReadNullableDate(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetValue(ordinal);
            return value is DateTime date || DateTime.TryParse(Convert.ToString(value), out date)
                ? date
                : null;
        }
    }

    /// <summary>
    /// Informações de uma sessão de usuário.
    /// </summary>
    public class UserSessionInfo
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public int UserId { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string MachineUserName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime LoginAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public bool IsActive { get; set; }
        public string AppVersion { get; set; } = string.Empty;
        public string DatabaseProvider { get; set; } = string.Empty;
    }
}
