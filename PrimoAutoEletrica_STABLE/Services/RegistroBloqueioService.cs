using System;
using System.Data.Common;

namespace PrimoAutoEletrica.Services
{
    public sealed class RegistroBloqueioResultado
    {
        public bool Bloqueado { get; init; }
        public string Mensagem { get; init; } = string.Empty;
        public string? BloqueadoPor { get; init; }
        public DateTime? ExpiraEm { get; init; }
    }

    public sealed class RegistroBloqueioService
    {
        private readonly DatabaseService _databaseService;
        private readonly AppSessionService _session;

        public RegistroBloqueioService(DatabaseService databaseService, AppSessionService session)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public RegistroBloqueioResultado TentarBloquear(string entidade, string entidadeId, string motivo = "", TimeSpan? duracao = null)
        {
            if (string.IsNullOrWhiteSpace(entidade) || string.IsNullOrWhiteSpace(entidadeId))
            {
                return new RegistroBloqueioResultado
                {
                    Bloqueado = false,
                    Mensagem = "Entidade invalida para bloqueio."
                };
            }

            var agora = DateTime.Now;
            var expiraEm = agora.Add(duracao ?? TimeSpan.FromMinutes(15));

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            LimparExpirados(connection, transaction, agora);

            var sessaoAtual = _session.SessionId == Guid.Empty ? string.Empty : _session.SessionId.ToString();
            string? usuarioBloqueioAtivo = null;
            DateTime? expiraBloqueioAtivo = null;
            using (var select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                var isSqlServer = IsSqlServerConnection(connection);
                select.CommandText = isSqlServer
                    ? @"
                    SELECT TOP (1) UsuarioNome, ExpiraEm
                    FROM RegistroBloqueios
                    WHERE Entidade = @Entidade
                      AND EntidadeId = @EntidadeId
                      AND Ativo = 1
                      AND ExpiraEm > @Agora
                      AND COALESCE(SessaoId, '') <> @SessaoId
                    ORDER BY CriadoEm DESC;"
                    : @"
                    SELECT UsuarioNome, ExpiraEm
                    FROM RegistroBloqueios
                    WHERE Entidade = @Entidade
                      AND EntidadeId = @EntidadeId
                      AND Ativo = 1
                      AND datetime(ExpiraEm) > datetime(@Agora)
                      AND COALESCE(SessaoId, '') <> @SessaoId
                    ORDER BY CriadoEm DESC
                    LIMIT 1;";
                select.Parameters.AddWithValue("@Entidade", entidade.Trim());
                select.Parameters.AddWithValue("@EntidadeId", entidadeId.Trim());
                select.Parameters.AddWithValue("@Agora", isSqlServer ? agora : agora.ToString("yyyy-MM-dd HH:mm:ss"));
                select.Parameters.AddWithValue("@SessaoId", sessaoAtual);

                using var reader = select.ExecuteReader();
                if (reader.Read())
                {
                    usuarioBloqueioAtivo = reader.IsDBNull(0) ? "outro usuario" : reader.GetString(0);
                    expiraBloqueioAtivo = ReadNullableDateTime(reader, 1);
                }
            }

            if (!string.IsNullOrWhiteSpace(usuarioBloqueioAtivo))
            {
                transaction.Commit();

                return new RegistroBloqueioResultado
                {
                    Bloqueado = false,
                    BloqueadoPor = usuarioBloqueioAtivo,
                    ExpiraEm = expiraBloqueioAtivo,
                    Mensagem = $"Registro em uso por {usuarioBloqueioAtivo}."
                };
            }

            using (var desativarAnterior = connection.CreateCommand())
            {
                desativarAnterior.Transaction = transaction;
                desativarAnterior.CommandText = @"
                    UPDATE RegistroBloqueios
                    SET Ativo = 0
                    WHERE Entidade = @Entidade
                      AND EntidadeId = @EntidadeId
                      AND COALESCE(SessaoId, '') = @SessaoId;";
                desativarAnterior.Parameters.AddWithValue("@Entidade", entidade.Trim());
                desativarAnterior.Parameters.AddWithValue("@EntidadeId", entidadeId.Trim());
                desativarAnterior.Parameters.AddWithValue("@SessaoId", sessaoAtual);
                desativarAnterior.ExecuteNonQuery();
            }

            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = @"
                    INSERT INTO RegistroBloqueios
                    (
                        Id,
                        Entidade,
                        EntidadeId,
                        UsuarioId,
                        UsuarioNome,
                        SessaoId,
                        Maquina,
                        CriadoEm,
                        ExpiraEm,
                        Motivo,
                        Ativo
                    )
                    VALUES
                    (
                        @Id,
                        @Entidade,
                        @EntidadeId,
                        @UsuarioId,
                        @UsuarioNome,
                        @SessaoId,
                        @Maquina,
                        @CriadoEm,
                        @ExpiraEm,
                        @Motivo,
                        1
                    );";
                insert.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                insert.Parameters.AddWithValue("@Entidade", entidade.Trim());
                insert.Parameters.AddWithValue("@EntidadeId", entidadeId.Trim());
                insert.Parameters.AddWithValue("@UsuarioId", _session.UserId.HasValue ? _session.UserId.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@UsuarioNome", _session.UserName);
                insert.Parameters.AddWithValue("@SessaoId", string.IsNullOrWhiteSpace(sessaoAtual) ? DBNull.Value : sessaoAtual);
                insert.Parameters.AddWithValue("@Maquina", Environment.MachineName);
                insert.Parameters.AddWithValue("@CriadoEm", IsSqlServerConnection(connection) ? agora : agora.ToString("yyyy-MM-dd HH:mm:ss"));
                insert.Parameters.AddWithValue("@ExpiraEm", IsSqlServerConnection(connection) ? expiraEm : expiraEm.ToString("yyyy-MM-dd HH:mm:ss"));
                insert.Parameters.AddWithValue("@Motivo", string.IsNullOrWhiteSpace(motivo) ? DBNull.Value : motivo.Trim());
                insert.ExecuteNonQuery();
            }

            transaction.Commit();

            return new RegistroBloqueioResultado
            {
                Bloqueado = true,
                ExpiraEm = expiraEm,
                Mensagem = "Registro bloqueado para edicao."
            };
        }

        public void LiberarBloqueio(string entidade, string entidadeId)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE RegistroBloqueios
                SET Ativo = 0
                WHERE Entidade = @Entidade
                  AND EntidadeId = @EntidadeId
                  AND COALESCE(SessaoId, '') = @SessaoId;";
            command.Parameters.AddWithValue("@Entidade", entidade.Trim());
            command.Parameters.AddWithValue("@EntidadeId", entidadeId.Trim());
            command.Parameters.AddWithValue("@SessaoId", _session.SessionId == Guid.Empty ? string.Empty : _session.SessionId.ToString());
            command.ExecuteNonQuery();
        }

        private static void LimparExpirados(DbConnection connection, DbTransaction transaction, DateTime agora)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            var isSqlServer = IsSqlServerConnection(connection);
            command.CommandText = isSqlServer
                ? @"
                UPDATE RegistroBloqueios
                SET Ativo = 0
                WHERE Ativo = 1
                  AND ExpiraEm <= @Agora;"
                : @"
                UPDATE RegistroBloqueios
                SET Ativo = 0
                WHERE Ativo = 1
                  AND datetime(ExpiraEm) <= datetime(@Agora);";
            command.Parameters.AddWithValue("@Agora", isSqlServer ? agora : agora.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            var typeName = connection.GetType().FullName ?? string.Empty;
            return typeName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime? ReadNullableDateTime(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            var value = reader.GetValue(index);
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(Convert.ToString(value), out var parsed)
                ? parsed
                : null;
        }
    }
}
