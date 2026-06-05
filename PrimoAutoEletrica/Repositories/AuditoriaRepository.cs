using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Diagnostics;

namespace PrimoAutoEletrica.Repositories
{
    public class AuditoriaRepository
    {
        private readonly IDbConnection _connection;

        public AuditoriaRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public void RegistrarEvento(object evento)
        {
            if (evento == null)
                return;

            try
            {
                if (_connection == null)
                    return;

                var json = JsonSerializer.Serialize(evento);
                // Primeiro, tentar inserir em colunas modernas (caso exista AuditLogs.EventoJson)
                try
                {
                    using var cmd = _connection.CreateCommand();
                    cmd.CommandText = "INSERT INTO AuditLogs (EventoJson, CreatedAt) VALUES (@json, @created);";

                    var p1 = cmd.CreateParameter();
                    p1.ParameterName = "@json";
                    p1.Value = json;
                    cmd.Parameters.Add(p1);

                    var p2 = cmd.CreateParameter();
                    p2.ParameterName = "@created";
                    p2.Value = DateTime.UtcNow;
                    cmd.Parameters.Add(p2);

                    try { if (_connection.State != ConnectionState.Open) _connection.Open(); } catch { }
                    cmd.ExecuteNonQuery();
                    return;
                }
                catch
                {
                    // ignore and tentar fallback para schema existente
                }

                // Fallback para schema atual do aplicativo (DataHora, Categoria, Acao, Detalhes)
                try
                {
                    using var cmd2 = _connection.CreateCommand();
                    cmd2.CommandText = "INSERT INTO AuditLogs (Id, DataHora, Categoria, Acao, Detalhes, Severidade, Sucesso) VALUES (@id, @datahora, @categoria, @acao, @detalhes, @severidade, @sucesso);";

                    var idp = cmd2.CreateParameter(); idp.ParameterName = "@id"; idp.Value = Guid.NewGuid().ToString(); cmd2.Parameters.Add(idp);
                    var dtp = cmd2.CreateParameter(); dtp.ParameterName = "@datahora"; dtp.Value = DateTime.UtcNow.ToString("o"); cmd2.Parameters.Add(dtp);
                    var catp = cmd2.CreateParameter(); catp.ParameterName = "@categoria"; catp.Value = "Sistema"; cmd2.Parameters.Add(catp);
                    var acp = cmd2.CreateParameter(); acp.ParameterName = "@acao"; acp.Value = "RegistrarEvento"; cmd2.Parameters.Add(acp);
                    var detp = cmd2.CreateParameter(); detp.ParameterName = "@detalhes"; detp.Value = json; cmd2.Parameters.Add(detp);
                    var sevp = cmd2.CreateParameter(); sevp.ParameterName = "@severidade"; sevp.Value = "Info"; cmd2.Parameters.Add(sevp);
                    var succp = cmd2.CreateParameter(); succp.ParameterName = "@sucesso"; succp.Value = 1; cmd2.Parameters.Add(succp);

                    try { if (_connection.State != ConnectionState.Open) _connection.Open(); } catch { }
                    cmd2.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AuditoriaRepository fallback insert falhou: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AuditoriaRepository.RegistrarEvento falha: {ex.Message}");
            }
        }

        public IEnumerable<object> ObterEventos(DateTime inicio, DateTime fim)
        {
            var resultados = new List<object>();

            try
            {
                if (_connection == null)
                    return resultados;

                // Primeiro, tentar schema moderno
                try
                {
                    using var cmd = _connection.CreateCommand();
                    cmd.CommandText = "SELECT EventoJson, CreatedAt FROM AuditLogs WHERE CreatedAt BETWEEN @inicio AND @fim ORDER BY CreatedAt DESC;";

                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@inicio"; p1.Value = inicio; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@fim"; p2.Value = fim; cmd.Parameters.Add(p2);

                    try { if (_connection.State != ConnectionState.Open) _connection.Open(); } catch { }

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var json = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                        var created = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                        resultados.Add(new { EventoJson = json, CreatedAt = created });
                    }

                    if (resultados.Count > 0)
                        return resultados;
                }
                catch
                {
                    // tentar fallback
                }

                // Fallback: schema atual usado pela aplicacao (DataHora, Detalhes)
                try
                {
                    using var cmd2 = _connection.CreateCommand();
                    cmd2.CommandText = "SELECT Detalhes, DataHora FROM AuditLogs WHERE DataHora BETWEEN @inicio AND @fim ORDER BY DataHora DESC;";

                    var p3 = cmd2.CreateParameter(); p3.ParameterName = "@inicio"; p3.Value = inicio.ToString("o"); cmd2.Parameters.Add(p3);
                    var p4 = cmd2.CreateParameter(); p4.ParameterName = "@fim"; p4.Value = fim.ToString("o"); cmd2.Parameters.Add(p4);

                    try { if (_connection.State != ConnectionState.Open) _connection.Open(); } catch { }

                    using var reader2 = cmd2.ExecuteReader();
                    while (reader2.Read())
                    {
                        var detalhes = reader2.IsDBNull(0) ? string.Empty : reader2.GetString(0);
                        var dataHoraStr = reader2.IsDBNull(1) ? string.Empty : reader2.GetString(1);
                        DateTime? dataHora = null;
                        if (DateTime.TryParse(dataHoraStr, out var parsed)) dataHora = parsed;
                        resultados.Add(new { EventoJson = detalhes, CreatedAt = dataHora });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AuditoriaRepository.ObterEventos fallback falhou: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AuditoriaRepository.ObterEventos falha: {ex.Message}");
            }

            return resultados;
        }
    }
}
