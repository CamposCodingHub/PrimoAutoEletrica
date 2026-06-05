using Microsoft.Data.Sqlite;
using System;

namespace PrimoAutoEletrica.Services
{
    public class AuditLogService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly AppSessionService _session;

        public AuditLogService(DatabaseService databaseService, LoggerService logger, AppSessionService session)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public void RegistrarSistema(string acao, string detalhes, string severidade = "Info", bool sucesso = true)
        {
            Registrar("Sistema", acao, detalhes: detalhes, severidade: severidade, sucesso: sucesso);
        }

        public void RegistrarLogin(string acao, string usuario, bool sucesso, string detalhes = "")
        {
            Registrar(
                categoria: "Seguranca",
                acao: acao,
                entidade: "Funcionario",
                detalhes: string.IsNullOrWhiteSpace(detalhes) ? usuario : $"{usuario} | {detalhes}",
                severidade: sucesso ? "Info" : "Warning",
                sucesso: sucesso,
                usuarioNomeOverride: usuario);
        }

        public void RegistrarNavegacao(string modulo, bool sucesso, string detalhes = "")
        {
            Registrar(
                categoria: "Navegacao",
                acao: sucesso ? "ModuloCarregado" : "FalhaNavegacao",
                entidade: "Modulo",
                entidadeId: modulo,
                detalhes: detalhes,
                severidade: sucesso ? "Info" : "Warning",
                sucesso: sucesso);
        }

        public void RegistrarAcaoCritica(string categoria, string acao, string entidade, string entidadeId, string detalhes = "")
        {
            Registrar(categoria, acao, entidade, entidadeId, detalhes, "Info", true);
        }

        public void RegistrarErro(string categoria, string acao, Exception exception, string entidade = "", string entidadeId = "", string criticidade = "Error")
        {
            Registrar(
                categoria: categoria,
                acao: acao,
                entidade: entidade,
                entidadeId: entidadeId,
                detalhes: exception.ToString(),
                severidade: criticidade,
                sucesso: false);
        }

        public void Registrar(
            string categoria,
            string acao,
            string entidade = "",
            string entidadeId = "",
            string detalhes = "",
            string severidade = "Info",
            bool sucesso = true,
            string? usuarioNomeOverride = null,
            string? valorAnterior = null,
            string? valorNovo = null,
            string? correlationId = null)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO AuditLogs
                    (
                        Id,
                        DataHora,
                        Categoria,
                        Acao,
                        Entidade,
                        EntidadeId,
                        Detalhes,
                        ValorAnterior,
                        ValorNovo,
                        Severidade,
                        Sucesso,
                        UsuarioId,
                        UsuarioNome,
                        Perfil,
                        SessaoId,
                        Maquina,
                        CorrelationId
                    )
                    VALUES
                    (
                        @Id,
                        @DataHora,
                        @Categoria,
                        @Acao,
                        @Entidade,
                        @EntidadeId,
                        @Detalhes,
                        @ValorAnterior,
                        @ValorNovo,
                        @Severidade,
                        @Sucesso,
                        @UsuarioId,
                        @UsuarioNome,
                        @Perfil,
                        @SessaoId,
                        @Maquina,
                        @CorrelationId
                    );";

                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@DataHora", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@Categoria", Normalizar(categoria, "Sistema"));
                command.Parameters.AddWithValue("@Acao", Normalizar(acao, "Evento"));
                command.Parameters.AddWithValue("@Entidade", ToDbNullableString(entidade));
                command.Parameters.AddWithValue("@EntidadeId", ToDbNullableString(entidadeId));
                command.Parameters.AddWithValue("@Detalhes", ToDbNullableString(detalhes));
                command.Parameters.AddWithValue("@ValorAnterior", ToDbNullableString(valorAnterior));
                command.Parameters.AddWithValue("@ValorNovo", ToDbNullableString(valorNovo));
                command.Parameters.AddWithValue("@Severidade", Normalizar(severidade, "Info"));
                command.Parameters.AddWithValue("@Sucesso", sucesso ? 1 : 0);
                command.Parameters.AddWithValue("@UsuarioId", _session.UserId.HasValue ? _session.UserId.Value : DBNull.Value);
                command.Parameters.AddWithValue("@UsuarioNome", ToDbNullableString(usuarioNomeOverride ?? _session.UserName));
                command.Parameters.AddWithValue("@Perfil", ToDbNullableString(_session.AccessProfile));
                command.Parameters.AddWithValue("@SessaoId", _session.SessionId == Guid.Empty ? DBNull.Value : _session.SessionId.ToString());
                command.Parameters.AddWithValue("@Maquina", Environment.MachineName);
                command.Parameters.AddWithValue("@CorrelationId", ToDbNullableString(correlationId));

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao gravar auditoria '{categoria}/{acao}': {ex.Message}", "Auditoria");
            }
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static string Normalizar(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
