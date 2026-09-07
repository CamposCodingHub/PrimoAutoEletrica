using System;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço para envio de notificações via SMS e WhatsApp
    /// </summary>
    public class NotificationService
    {
        private readonly LoggerService _logger;
        private readonly DatabaseService _database;

        public NotificationService(LoggerService logger, DatabaseService database)
        {
            _logger = logger;
            _database = database;
        }

        /// <summary>
        /// Envia uma notificação via SMS
        /// </summary>
        public async Task<bool> EnviarSmsAsync(string telefone, string mensagem)
        {
            try
            {
                // TODO: Implementar integração real com provedor de SMS
                // Exemplos de provedores: Twilio, Vonage, Clickatell, etc.
                
                _logger.LogInfo($"[NotificationService] Iniciando envio de SMS para {telefone}");
                
                // Simulação de envio bem-sucedido
                await Task.Delay(100);
                
                // Registrar envio no banco de dados
                RegistrarEnvioNotificacao(telefone, mensagem, "SMS", "Enviado");
                
                _logger.LogInfo($"[NotificationService] SMS enviado com sucesso para {telefone}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[NotificationService] Erro ao enviar SMS para {telefone}", ex);
                RegistrarEnvioNotificacao(telefone, mensagem, "SMS", "Falha", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Envia uma notificação via WhatsApp
        /// </summary>
        public async Task<bool> EnviarWhatsAppAsync(string telefone, string mensagem)
        {
            try
            {
                // TODO: Implementar integração real com API do WhatsApp
                // Exemplos: WhatsApp Business API, Twilio WhatsApp, etc.
                
                _logger.LogInfo($"[NotificationService] Iniciando envio de WhatsApp para {telefone}");
                
                // Simulação de envio bem-sucedido
                await Task.Delay(100);
                
                // Registrar envio no banco de dados
                RegistrarEnvioNotificacao(telefone, mensagem, "WhatsApp", "Enviado");
                
                _logger.LogInfo($"[NotificationService] WhatsApp enviado com sucesso para {telefone}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[NotificationService] Erro ao enviar WhatsApp para {telefone}", ex);
                RegistrarEnvioNotificacao(telefone, mensagem, "WhatsApp", "Falha", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Envia notificação de confirmação de agendamento
        /// </summary>
        public async Task<bool> EnviarConfirmacaoAgendamentoAsync(Agendamento agendamento)
        {
            var mensagem = $"Olá {agendamento.ClienteNome}! Seu agendamento foi confirmado para {agendamento.DataAgendamento:dd/MM/yyyy HH:mm}. Aguardamos você!";
            return await EnviarWhatsAppAsync(agendamento.ClienteTelefone, mensagem);
        }

        /// <summary>
        /// Envia notificação de lembrete de agendamento
        /// </summary>
        public async Task<bool> EnviarLembreteAgendamentoAsync(Agendamento agendamento)
        {
            var mensagem = $"Lembrete: Você tem um agendamento amanhã às {agendamento.DataAgendamento:HH:mm}. Confirme sua presença!";
            return await EnviarWhatsAppAsync(agendamento.ClienteTelefone, mensagem);
        }

        /// <summary>
        /// Envia notificação de orçamento aprovado
        /// </summary>
        public async Task<bool> EnviarOrcamentoAprovadoAsync(Orcamento orcamento)
        {
            var telefone = orcamento.Cliente?.Telefone ?? string.Empty;
            var mensagem = $"Seu orçamento #{orcamento.Numero} foi aprovado! Valor total: {orcamento.Total:C2}. Entraremos em contato para iniciar o serviço.";
            return await EnviarWhatsAppAsync(telefone, mensagem);
        }

        /// <summary>
        /// Envia notificação de ordem de serviço concluída
        /// </summary>
        public async Task<bool> EnviarOsConcluidaAsync(OrdemServico os)
        {
            var telefone = os.TelefoneClienteSnapshot ?? string.Empty;
            var mensagem = $"Sua ordem de serviço #{os.Numero} foi concluída! Você pode retirar seu veículo. Valor final: {os.ValorMaoObra:C2}";
            return await EnviarWhatsAppAsync(telefone, mensagem);
        }

        /// <summary>
        /// Registra o envio de notificação no banco de dados
        /// </summary>
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
                command.Parameters.AddWithValue("@Telefone", telefone);
                command.Parameters.AddWithValue("@Mensagem", mensagem);
                command.Parameters.AddWithValue("@Tipo", tipo);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Erro", erro ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DataEnvio", DateTime.Now);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao registrar notificação enviada", ex);
            }
        }

        /// <summary>
        /// Obtém histórico de notificações enviadas para um telefone
        /// </summary>
        public async Task<List<NotificacaoEnviada>> ObterHistoricoNotificacoesAsync(string telefone, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            try
            {
                using var connection = _database.GetConnection();
                await connection.OpenAsync();

                var sql = @"
                    SELECT Id, Telefone, Mensagem, Tipo, Status, Erro, DataEnvio 
                    FROM NotificacoesEnviadas 
                    WHERE Telefone = @Telefone";

                if (dataInicio.HasValue)
                {
                    sql += " AND DataEnvio >= @DataInicio";
                }

                if (dataFim.HasValue)
                {
                    sql += " AND DataEnvio <= @DataFim";
                }

                sql += " ORDER BY DataEnvio DESC";

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddWithValue("@Telefone", telefone);

                if (dataInicio.HasValue)
                {
                    command.Parameters.AddWithValue("@DataInicio", dataInicio.Value);
                }

                if (dataFim.HasValue)
                {
                    command.Parameters.AddWithValue("@DataFim", dataFim.Value);
                }

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
                _logger.LogError("Erro ao obter histórico de notificações", ex);
                return new List<NotificacaoEnviada>();
            }
        }
    }
    /// <summary>
    /// Representa uma notificação enviada
    /// </summary>
    public class NotificacaoEnviada
    {
        public int Id { get; set; }
        public string Telefone { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // SMS, WhatsApp
        public string Status { get; set; } = string.Empty; // Enviado, Falha, Pendente
        public string? Erro { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}
