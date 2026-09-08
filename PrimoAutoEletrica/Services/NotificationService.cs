using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Canal de notificações externas (SMS / WhatsApp API).
    /// Envio automático NÃO está configurado no PRIMOX 1.0.0.
    /// WhatsApp real do produto = abertura manual via link wa.me nas telas.
    /// </summary>
    public class NotificationService
    {
        public const string StatusNaoConfigurado = "NaoConfigurado";
        public const string MotivoPadrao =
            "Envio automatico de SMS/WhatsApp nao esta configurado. Use o botao WhatsApp das telas (abre o aplicativo) quando disponivel.";

        private readonly LoggerService _logger;
        private readonly DatabaseService _database;

        public NotificationService(LoggerService logger, DatabaseService database)
        {
            _logger = logger;
            _database = database;
        }

        /// <summary>Sempre false — integração SMS externa não implementada.</summary>
        public Task<bool> EnviarSmsAsync(string telefone, string mensagem)
            => RegistrarNaoDisponivelAsync(telefone, mensagem, "SMS");

        /// <summary>Sempre false — WhatsApp Cloud API não implementada (wa.me é outro mecanismo).</summary>
        public Task<bool> EnviarWhatsAppAsync(string telefone, string mensagem)
            => RegistrarNaoDisponivelAsync(telefone, mensagem, "WhatsApp");

        public Task<bool> EnviarConfirmacaoAgendamentoAsync(Agendamento agendamento)
        {
            var mensagem = $"Ola {agendamento.ClienteNome}! Seu agendamento foi confirmado para {agendamento.DataAgendamento:dd/MM/yyyy HH:mm}.";
            return EnviarWhatsAppAsync(agendamento.ClienteTelefone, mensagem);
        }

        public Task<bool> EnviarLembreteAgendamentoAsync(Agendamento agendamento)
        {
            var mensagem = $"Lembrete: voce tem um agendamento amanha as {agendamento.DataAgendamento:HH:mm}.";
            return EnviarWhatsAppAsync(agendamento.ClienteTelefone, mensagem);
        }

        public Task<bool> EnviarOrcamentoAprovadoAsync(Orcamento orcamento)
        {
            var telefone = orcamento.Cliente?.Telefone ?? string.Empty;
            var mensagem = $"Seu orcamento #{orcamento.Numero} foi aprovado. Valor total: {orcamento.Total:C2}.";
            return EnviarWhatsAppAsync(telefone, mensagem);
        }

        public Task<bool> EnviarOsConcluidaAsync(OrdemServico os)
        {
            var telefone = os.TelefoneClienteSnapshot ?? string.Empty;
            var mensagem = $"Sua ordem de servico #{os.Numero} foi concluida.";
            return EnviarWhatsAppAsync(telefone, mensagem);
        }

        private Task<bool> RegistrarNaoDisponivelAsync(string telefone, string mensagem, string tipo)
        {
            _logger.LogInfo(
                $"[NotificationService] {tipo} NAO ENVIADO (integracao nao configurada). Destino={telefone}. {MotivoPadrao}");

            RegistrarEnvioNotificacao(telefone, mensagem, tipo, StatusNaoConfigurado, MotivoPadrao);
            return Task.FromResult(false);
        }

        private void RegistrarEnvioNotificacao(string telefone, string mensagem, string tipo, string status, string? erro = null)
        {
            try
            {
                using var connection = _database.GetConnection();
                connection.Open();

                var sql = @"
                    INSERT INTO NotificacoesEnviadas 
                    (Telefone, Mensagem, Tipo, Status, Erro, DataEnvio) 
                    VALUES 
                    (@Telefone, @Mensagem, @Tipo, @Status, @Erro, @DataEnvio)";

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddWithValue("@Telefone", telefone ?? string.Empty);
                command.Parameters.AddWithValue("@Mensagem", mensagem ?? string.Empty);
                command.Parameters.AddWithValue("@Tipo", tipo);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Erro", erro ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DataEnvio", DateTime.Now);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao registrar tentativa de notificacao", ex);
            }
        }

        public async Task<List<NotificacaoEnviada>> ObterHistoricoNotificacoesAsync(
            string telefone, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            try
            {
                using var connection = _database.GetConnection();
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Telefone, Mensagem, Tipo, Status, Erro, DataEnvio 
                    FROM NotificacoesEnviadas 
                    WHERE Telefone = @Telefone";

                if (dataInicio.HasValue) sql += " AND DataEnvio >= @DataInicio";
                if (dataFim.HasValue) sql += " AND DataEnvio <= @DataFim";
                sql += " ORDER BY DataEnvio DESC";

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddWithValue("@Telefone", telefone);
                if (dataInicio.HasValue) command.Parameters.AddWithValue("@DataInicio", dataInicio.Value);
                if (dataFim.HasValue) command.Parameters.AddWithValue("@DataFim", dataFim.Value);

                var notificacoes = new List<NotificacaoEnviada>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    notificacoes.Add(new NotificacaoEnviada
                    {
                        Id = reader.GetInt32(0),
                        Telefone = reader.GetString(1),
                        Mensagem = reader.GetString(2),
                        Tipo = reader.GetString(3),
                        Status = reader.GetString(4),
                        Erro = reader.IsDBNull(5) ? null : reader.GetString(5),
                        DataEnvio = reader.GetDateTime(6)
                    });
                }

                return notificacoes;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao obter historico de notificacoes", ex);
                return new List<NotificacaoEnviada>();
            }
        }
    }

    public class NotificacaoEnviada
    {
        public int Id { get; set; }
        public string Telefone { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Erro { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}
