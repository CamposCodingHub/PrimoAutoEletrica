using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço para gerenciar histórico de alterações (audit trail) no sistema
    /// Rastreia todas as mudanças em entidades críticas
    /// </summary>
    public class AuditTrailService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _loggerService;

        public AuditTrailService()
        {
            // Usar App.Database em vez de DatabaseService.Instance
            _databaseService = App.Database ?? new DatabaseService();
            _loggerService = App.Logger ?? new LoggerService();
        }

        /// <summary>
        /// Registra uma alteração no histórico de auditoria
        /// </summary>
        public async Task<bool> RegistrarAlteracaoAsync(
            string entidadeTipo,
            int entidadeId,
            string acao,
            string valoresAntigos,
            string valoresNovos,
            string usuarioId = null)
        {
            try
            {
                usuarioId ??= App.Session?.CurrentUser?.Id.ToString() ?? "SISTEMA";

                var sql = @"
                    INSERT INTO audit_log 
                    (entidade_tipo, entidade_id, acao, valores_antigos, valores_novos, usuario_id, data_hora, ip_origem)
                    VALUES 
                    (@EntidadeTipo, @EntidadeId, @Acao, @ValoresAntigos, @ValoresNovos, @UsuarioId, @DataHora, @IpOrigem)";

                using (var connection = _databaseService.GetConnection())
                {
                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        EntidadeTipo = entidadeTipo,
                        EntidadeId = entidadeId,
                        Acao = acao,
                        ValoresAntigos = valoresAntigos,
                        ValoresNovos = valoresNovos,
                        UsuarioId = usuarioId,
                        DataHora = DateTime.Now,
                        IpOrigem = ObterIpOrigem()
                    });

                    _loggerService?.LogInfo($"Auditoria registrada: {entidadeTipo} ID:{entidadeId} - {acao}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                _loggerService?.LogError($"Erro ao registrar auditoria: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Obtém o histórico de alterações de uma entidade
        /// </summary>
        public async Task<List<AuditLogEntry>> ObterHistoricoAsync(
            string entidadeTipo,
            int entidadeId,
            int maxResultados = 100)
        {
            try
            {
                var sql = @"
                    SELECT 
                        id, entidade_tipo, entidade_id, acao, 
                        valores_antigos, valores_novos, usuario_id, 
                        data_hora, ip_origem
                    FROM audit_log
                    WHERE entidade_tipo = @EntidadeTipo 
                      AND entidade_id = @EntidadeId
                    ORDER BY data_hora DESC
                    LIMIT @MaxResultados";

                using (var connection = _databaseService.GetConnection())
                {
                    var resultado = await connection.QueryAsync<AuditLogEntry>(sql, new
                    {
                        EntidadeTipo = entidadeTipo,
                        EntidadeId = entidadeId,
                        MaxResultados = maxResultados
                    });

                    return resultado.ToList();
                }
            }
            catch (Exception ex)
            {
                _loggerService?.LogError($"Erro ao obter histórico de auditoria: {ex.Message}", ex);
                return new List<AuditLogEntry>();
            }
        }

        /// <summary>
        /// Obtém todas as alterações de um período
        /// </summary>
        public async Task<List<AuditLogEntry>> ObterAlteracoesPorPeriodoAsync(
            DateTime dataInicio,
            DateTime dataFim,
            string entidadeTipo = null,
            string acao = null,
            string usuarioId = null)
        {
            try
            {
                var sql = new StringBuilder(@"
                    SELECT 
                        id, entidade_tipo, entidade_id, acao, 
                        valores_antigos, valores_novos, usuario_id, 
                        data_hora, ip_origem
                    FROM audit_log
                    WHERE data_hora BETWEEN @DataInicio AND @DataFim");

                var parameters = new Dictionary<string, object>
                {
                    { "DataInicio", dataInicio },
                    { "DataFim", dataFim }
                };

                if (!string.IsNullOrEmpty(entidadeTipo))
                {
                    sql.Append(" AND entidade_tipo = @EntidadeTipo");
                    parameters["EntidadeTipo"] = entidadeTipo;
                }

                if (!string.IsNullOrEmpty(acao))
                {
                    sql.Append(" AND acao = @Acao");
                    parameters["Acao"] = acao;
                }

                if (!string.IsNullOrEmpty(usuarioId))
                {
                    sql.Append(" AND usuario_id = @UsuarioId");
                    parameters["UsuarioId"] = usuarioId;
                }

                sql.Append(" ORDER BY data_hora DESC LIMIT 1000");

                using (var connection = _databaseService.GetConnection())
                {
                    var resultado = await connection.QueryAsync<AuditLogEntry>(
                        sql.ToString(),
                        parameters);

                    return resultado.ToList();
                }
            }
            catch (Exception ex)
            {
                _loggerService?.LogError($"Erro ao obter alterações por período: {ex.Message}", ex);
                return new List<AuditLogEntry>();
            }
        }

        /// <summary>
        /// Obtém estatísticas de auditoria
        /// </summary>
        public async Task<AuditStatistics> ObterEstatisticasAsync(DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                var sql = @"
                    SELECT 
                        entidade_tipo,
                        acao,
                        COUNT(*) as total,
                        COUNT(DISTINCT usuario_id) as usuarios_unicos,
                        MIN(data_hora) as primeira_alteracao,
                        MAX(data_hora) as ultima_alteracao
                    FROM audit_log
                    WHERE data_hora BETWEEN @DataInicio AND @DataFim
                    GROUP BY entidade_tipo, acao
                    ORDER BY total DESC";

                using (var connection = _databaseService.GetConnection())
                {
                    var entradas = await connection.QueryAsync(sql, new
                    {
                        DataInicio = dataInicio,
                        DataFim = dataFim
                    });

                    var stats = new AuditStatistics
                    {
                        DataInicio = dataInicio,
                        DataFim = dataFim,
                        TotalAlteracoes = entradas.Sum(x => (int)x.total),
                        Entradas = entradas.ToList()
                    };

                    return stats;
                }
            }
            catch (Exception ex)
            {
                _loggerService?.LogError($"Erro ao obter estatísticas de auditoria: {ex.Message}", ex);
                return new AuditStatistics();
            }
        }

        /// <summary>
        /// Limpa registros de auditoria antigos (older than specified days)
        /// </summary>
        public async Task<int> LimparAuditoriasAntigasAsync(int diasRetencao = 90)
        {
            try
            {
                var dataLimite = DateTime.Now.AddDays(-diasRetencao);

                var sql = @"
                    DELETE FROM audit_log
                    WHERE data_hora < @DataLimite";

                using (var connection = _databaseService.GetConnection())
                {
                    var rowsDeleted = await connection.ExecuteAsync(sql, new
                    {
                        DataLimite = dataLimite
                    });

                    _loggerService?.LogInfo($"Auditoria: {rowsDeleted} registros antigos removidos (anterior a {dataLimite:dd/MM/yyyy})");
                    return rowsDeleted;
                }
            }
            catch (Exception ex)
            {
                _loggerService?.LogError($"Erro ao limpar auditoria antiga: {ex.Message}", ex);
                return 0;
            }
        }

        private string ObterIpOrigem()
        {
            try
            {
                // Em aplicação local, retorna localhost
                // Em aplicação web, obteria do HttpContext
                return "127.0.0.1";
            }
            catch
            {
                return "DESCONHECIDO";
            }
        }
    }

    /// <summary>
    /// Modelo para entrada de log de auditoria
    /// </summary>
    public class AuditLogEntry
    {
        public int Id { get; set; }
        public string EntidadeTipo { get; set; }
        public int EntidadeId { get; set; }
        public string Acao { get; set; }
        public string ValoresAntigos { get; set; }
        public string ValoresNovos { get; set; }
        public string UsuarioId { get; set; }
        public DateTime DataHora { get; set; }
        public string IpOrigem { get; set; }

        public override string ToString()
        {
            return $"[{DataHora:dd/MM/yyyy HH:mm}] {UsuarioId} - {Acao} em {EntidadeTipo} #{EntidadeId}";
        }
    }

    /// <summary>
    /// Estatísticas de auditoria
    /// </summary>
    public class AuditStatistics
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int TotalAlteracoes { get; set; }
        public List<dynamic> Entradas { get; set; } = new();
    }
}
