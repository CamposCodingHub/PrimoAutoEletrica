using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class KnowledgeRepository : IKnowledgeRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly LoggerService _logger;

        public KnowledgeRepository(Func<DbConnection> connectionFactory, LoggerService logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static DbParameter CreateParameter(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }

        public async Task<IReadOnlyList<TechnicalKnowledgeEntry>> ObterArtigosAsync(string? busca = null, string? sistema = null, string? tensao = null, KnowledgeStatus? status = null, CancellationToken ct = default)
        {
            var list = new List<TechnicalKnowledgeEntry>();
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                var sql = @"
                    SELECT KnowledgeId, Code, Title, System, VehicleCategory, Voltage,
                           Symptom, PossibleCauses, DiagnosticProcedure, RecommendedMeasurements,
                           Solution, Warnings, Tags, SourceType, CreatedByUserId, CreatedByUserName,
                           Status, CreatedAt, UpdatedAt
                    FROM TechnicalKnowledgeEntries
                    WHERE 1=1
                ";

                if (!string.IsNullOrWhiteSpace(busca))
                {
                    sql += " AND (Code LIKE @Busca OR Title LIKE @Busca OR Symptom LIKE @Busca OR Solution LIKE @Busca OR Tags LIKE @Busca)";
                    command.Parameters.Add(CreateParameter(command, "@Busca", $"%{busca.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(sistema) && !string.Equals(sistema, "Todos", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND System = @Sistema";
                    command.Parameters.Add(CreateParameter(command, "@Sistema", sistema.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(tensao) && !string.Equals(tensao, "Todas", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND Voltage = @Tensao";
                    command.Parameters.Add(CreateParameter(command, "@Tensao", tensao.Trim()));
                }

                if (status.HasValue)
                {
                    sql += " AND Status = @Status";
                    command.Parameters.Add(CreateParameter(command, "@Status", status.Value.ToString()));
                }

                sql += " ORDER BY Code ASC;";
                command.CommandText = sql;

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    list.Add(MapEntry(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao consultar artigos na base de conhecimento.", ex);
                throw;
            }

            return list;
        }

        public async Task<TechnicalKnowledgeEntry?> ObterArtigoPorIdAsync(Guid knowledgeId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT KnowledgeId, Code, Title, System, VehicleCategory, Voltage,
                           Symptom, PossibleCauses, DiagnosticProcedure, RecommendedMeasurements,
                           Solution, Warnings, Tags, SourceType, CreatedByUserId, CreatedByUserName,
                           Status, CreatedAt, UpdatedAt
                    FROM TechnicalKnowledgeEntries
                    WHERE KnowledgeId = @Id LIMIT 1;
                ";
                command.Parameters.Add(CreateParameter(command, "@Id", knowledgeId.ToString()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    return MapEntry(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter artigo por Id {knowledgeId}", ex);
                throw;
            }

            return null;
        }

        public async Task<TechnicalKnowledgeEntry?> ObterArtigoPorCodigoAsync(string codigo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT KnowledgeId, Code, Title, System, VehicleCategory, Voltage,
                           Symptom, PossibleCauses, DiagnosticProcedure, RecommendedMeasurements,
                           Solution, Warnings, Tags, SourceType, CreatedByUserId, CreatedByUserName,
                           Status, CreatedAt, UpdatedAt
                    FROM TechnicalKnowledgeEntries
                    WHERE lower(Code) = lower(@Code) LIMIT 1;
                ";
                command.Parameters.Add(CreateParameter(command, "@Code", codigo.Trim()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    return MapEntry(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter artigo por Código {codigo}", ex);
                throw;
            }

            return null;
        }

        public async Task<bool> InserirArtigoAsync(TechnicalKnowledgeEntry entry, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO TechnicalKnowledgeEntries (
                        KnowledgeId, Code, Title, System, VehicleCategory, Voltage,
                        Symptom, PossibleCauses, DiagnosticProcedure, RecommendedMeasurements,
                        Solution, Warnings, Tags, SourceType, CreatedByUserId, CreatedByUserName,
                        Status, CreatedAt, UpdatedAt
                    ) VALUES (
                        @Id, @Code, @Title, @System, @Cat, @Voltage, @Symptom, @PossibleCauses,
                        @DiagProc, @RecMeas, @Solution, @Warnings, @Tags, @SourceType,
                        @UserId, @UserName, @Status, @CreatedAt, @UpdatedAt
                    );
                ";
                AddEntryParameters(command, entry);

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao inserir artigo {entry.Code}", ex);
                throw;
            }
        }

        public async Task<bool> AtualizarArtigoAsync(TechnicalKnowledgeEntry entry, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE TechnicalKnowledgeEntries SET
                        Code = @Code,
                        Title = @Title,
                        System = @System,
                        VehicleCategory = @Cat,
                        Voltage = @Voltage,
                        Symptom = @Symptom,
                        PossibleCauses = @PossibleCauses,
                        DiagnosticProcedure = @DiagProc,
                        RecommendedMeasurements = @RecMeas,
                        Solution = @Solution,
                        Warnings = @Warnings,
                        Tags = @Tags,
                        SourceType = @SourceType,
                        Status = @Status,
                        UpdatedAt = @UpdatedAt
                    WHERE KnowledgeId = @Id;
                ";
                AddEntryParameters(command, entry);

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao atualizar artigo {entry.Code}", ex);
                throw;
            }
        }

        public async Task<bool> ExcluirArtigoAsync(Guid knowledgeId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM TechnicalKnowledgeEntries WHERE KnowledgeId = @Id;";
                command.Parameters.Add(CreateParameter(command, "@Id", knowledgeId.ToString()));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao excluir artigo {knowledgeId}", ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<DiagnosticCase>> ObterCasosAsync(string? busca = null, string? sistema = null, Guid? veiculoId = null, Guid? osId = null, CancellationToken ct = default)
        {
            var list = new List<DiagnosticCase>();
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                var sql = @"
                    SELECT CaseId, Code, Title, VehicleId, VehicleModel, VehiclePlate,
                           WorkOrderId, WorkOrderNumber, TechnicianId, TechnicianName,
                           System, Voltage, DtcCodes, Symptom, Measurements, InitialHypotheses,
                           ConfirmedCause, Solution, PartsUsed, TestResult, FinalResult,
                           KnowledgeEntryId, CreatedAt, UpdatedAt
                    FROM DiagnosticCases
                    WHERE 1=1
                ";

                if (!string.IsNullOrWhiteSpace(busca))
                {
                    sql += " AND (Code LIKE @Busca OR Title LIKE @Busca OR VehicleModel LIKE @Busca OR VehiclePlate LIKE @Busca OR Symptom LIKE @Busca OR ConfirmedCause LIKE @Busca OR Solution LIKE @Busca OR DtcCodes LIKE @Busca)";
                    command.Parameters.Add(CreateParameter(command, "@Busca", $"%{busca.Trim()}%"));
                }

                if (!string.IsNullOrWhiteSpace(sistema) && !string.Equals(sistema, "Todos", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND System = @Sistema";
                    command.Parameters.Add(CreateParameter(command, "@Sistema", sistema.Trim()));
                }

                if (veiculoId.HasValue)
                {
                    sql += " AND VehicleId = @VeiculoId";
                    command.Parameters.Add(CreateParameter(command, "@VeiculoId", veiculoId.Value.ToString()));
                }

                if (osId.HasValue)
                {
                    sql += " AND WorkOrderId = @OsId";
                    command.Parameters.Add(CreateParameter(command, "@OsId", osId.Value.ToString()));
                }

                sql += " ORDER BY CreatedAt DESC;";
                command.CommandText = sql;

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    list.Add(MapCase(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao obter casos reais de diagnóstico.", ex);
                throw;
            }

            return list;
        }

        public async Task<DiagnosticCase?> ObterCasoPorIdAsync(Guid caseId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT CaseId, Code, Title, VehicleId, VehicleModel, VehiclePlate,
                           WorkOrderId, WorkOrderNumber, TechnicianId, TechnicianName,
                           System, Voltage, DtcCodes, Symptom, Measurements, InitialHypotheses,
                           ConfirmedCause, Solution, PartsUsed, TestResult, FinalResult,
                           KnowledgeEntryId, CreatedAt, UpdatedAt
                    FROM DiagnosticCases
                    WHERE CaseId = @Id LIMIT 1;
                ";
                command.Parameters.Add(CreateParameter(command, "@Id", caseId.ToString()));

                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    return MapCase(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao obter caso por ID {caseId}", ex);
                throw;
            }

            return null;
        }

        public async Task<bool> InserirCasoAsync(DiagnosticCase caso, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(caso);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO DiagnosticCases (
                        CaseId, Code, Title, VehicleId, VehicleModel, VehiclePlate,
                        WorkOrderId, WorkOrderNumber, TechnicianId, TechnicianName,
                        System, Voltage, DtcCodes, Symptom, Measurements, InitialHypotheses,
                        ConfirmedCause, Solution, PartsUsed, TestResult, FinalResult,
                        KnowledgeEntryId, CreatedAt, UpdatedAt
                    ) VALUES (
                        @Id, @Code, @Title, @VehicleId, @VehicleModel, @VehiclePlate,
                        @WorkOrderId, @WorkOrderNumber, @TechnicianId, @TechnicianName,
                        @System, @Voltage, @DtcCodes, @Symptom, @Measurements, @InitialHypotheses,
                        @ConfirmedCause, @Solution, @PartsUsed, @TestResult, @FinalResult,
                        @KnowledgeEntryId, @CreatedAt, @UpdatedAt
                    );
                ";
                AddCaseParameters(command, caso);

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao inserir caso diagnóstico {caso.Code}", ex);
                throw;
            }
        }

        public async Task<bool> AtualizarCasoAsync(DiagnosticCase caso, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(caso);

            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE DiagnosticCases SET
                        Code = @Code,
                        Title = @Title,
                        VehicleId = @VehicleId,
                        VehicleModel = @VehicleModel,
                        VehiclePlate = @VehiclePlate,
                        WorkOrderId = @WorkOrderId,
                        WorkOrderNumber = @WorkOrderNumber,
                        TechnicianId = @TechnicianId,
                        TechnicianName = @TechnicianName,
                        System = @System,
                        Voltage = @Voltage,
                        DtcCodes = @DtcCodes,
                        Symptom = @Symptom,
                        Measurements = @Measurements,
                        InitialHypotheses = @InitialHypotheses,
                        ConfirmedCause = @ConfirmedCause,
                        Solution = @Solution,
                        PartsUsed = @PartsUsed,
                        TestResult = @TestResult,
                        FinalResult = @FinalResult,
                        KnowledgeEntryId = @KnowledgeEntryId,
                        UpdatedAt = @UpdatedAt
                    WHERE CaseId = @Id;
                ";
                AddCaseParameters(command, caso);

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao atualizar caso diagnóstico {caso.Code}", ex);
                throw;
            }
        }

        public async Task<bool> ExcluirCasoAsync(Guid caseId, CancellationToken ct = default)
        {
            try
            {
                await using var connection = _connectionFactory();
                await connection.OpenAsync(ct).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM DiagnosticCases WHERE CaseId = @Id;";
                command.Parameters.Add(CreateParameter(command, "@Id", caseId.ToString()));

                var affected = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao excluir caso diagnóstico {caseId}", ex);
                throw;
            }
        }

        public async Task<IReadOnlyList<DiagnosticCase>> ObterCasosPorVeiculoAsync(Guid veiculoId, CancellationToken ct = default)
        {
            return await ObterCasosAsync(null, null, veiculoId, null, ct).ConfigureAwait(false);
        }

        private static void AddEntryParameters(DbCommand command, TechnicalKnowledgeEntry entry)
        {
            command.Parameters.Add(CreateParameter(command, "@Id", entry.KnowledgeId.ToString()));
            command.Parameters.Add(CreateParameter(command, "@Code", entry.Code));
            command.Parameters.Add(CreateParameter(command, "@Title", entry.Title));
            command.Parameters.Add(CreateParameter(command, "@System", entry.System));
            command.Parameters.Add(CreateParameter(command, "@Cat", entry.VehicleCategory));
            command.Parameters.Add(CreateParameter(command, "@Voltage", entry.Voltage));
            command.Parameters.Add(CreateParameter(command, "@Symptom", entry.Symptom));
            command.Parameters.Add(CreateParameter(command, "@PossibleCauses", entry.PossibleCauses));
            command.Parameters.Add(CreateParameter(command, "@DiagProc", entry.DiagnosticProcedure));
            command.Parameters.Add(CreateParameter(command, "@RecMeas", entry.RecommendedMeasurements));
            command.Parameters.Add(CreateParameter(command, "@Solution", entry.Solution));
            command.Parameters.Add(CreateParameter(command, "@Warnings", entry.Warnings));
            command.Parameters.Add(CreateParameter(command, "@Tags", entry.Tags));
            command.Parameters.Add(CreateParameter(command, "@SourceType", entry.SourceType.ToString()));
            command.Parameters.Add(CreateParameter(command, "@UserId", entry.CreatedByUserId));
            command.Parameters.Add(CreateParameter(command, "@UserName", entry.CreatedByUserName));
            command.Parameters.Add(CreateParameter(command, "@Status", entry.Status.ToString()));
            command.Parameters.Add(CreateParameter(command, "@CreatedAt", entry.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        private static void AddCaseParameters(DbCommand command, DiagnosticCase caso)
        {
            command.Parameters.Add(CreateParameter(command, "@Id", caso.CaseId.ToString()));
            command.Parameters.Add(CreateParameter(command, "@Code", caso.Code));
            command.Parameters.Add(CreateParameter(command, "@Title", caso.Title));
            command.Parameters.Add(CreateParameter(command, "@VehicleId", caso.VehicleId?.ToString()));
            command.Parameters.Add(CreateParameter(command, "@VehicleModel", caso.VehicleModel));
            command.Parameters.Add(CreateParameter(command, "@VehiclePlate", caso.VehiclePlate));
            command.Parameters.Add(CreateParameter(command, "@WorkOrderId", caso.WorkOrderId?.ToString()));
            command.Parameters.Add(CreateParameter(command, "@WorkOrderNumber", caso.WorkOrderNumber));
            command.Parameters.Add(CreateParameter(command, "@TechnicianId", caso.TechnicianId));
            command.Parameters.Add(CreateParameter(command, "@TechnicianName", caso.TechnicianName));
            command.Parameters.Add(CreateParameter(command, "@System", caso.System));
            command.Parameters.Add(CreateParameter(command, "@Voltage", caso.Voltage));
            command.Parameters.Add(CreateParameter(command, "@DtcCodes", caso.DtcCodes));
            command.Parameters.Add(CreateParameter(command, "@Symptom", caso.Symptom));
            command.Parameters.Add(CreateParameter(command, "@Measurements", caso.Measurements));
            command.Parameters.Add(CreateParameter(command, "@InitialHypotheses", caso.InitialHypotheses));
            command.Parameters.Add(CreateParameter(command, "@ConfirmedCause", caso.ConfirmedCause));
            command.Parameters.Add(CreateParameter(command, "@Solution", caso.Solution));
            command.Parameters.Add(CreateParameter(command, "@PartsUsed", caso.PartsUsed));
            command.Parameters.Add(CreateParameter(command, "@TestResult", caso.TestResult));
            command.Parameters.Add(CreateParameter(command, "@FinalResult", caso.FinalResult.ToString()));
            command.Parameters.Add(CreateParameter(command, "@KnowledgeEntryId", caso.KnowledgeEntryId?.ToString()));
            command.Parameters.Add(CreateParameter(command, "@CreatedAt", caso.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            command.Parameters.Add(CreateParameter(command, "@UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        private static TechnicalKnowledgeEntry MapEntry(DbDataReader reader)
        {
            return new TechnicalKnowledgeEntry
            {
                KnowledgeId = Guid.TryParse(reader["KnowledgeId"]?.ToString(), out var kid) ? kid : Guid.Empty,
                Code = reader["Code"]?.ToString() ?? string.Empty,
                Title = reader["Title"]?.ToString() ?? string.Empty,
                System = reader["System"]?.ToString() ?? "Geral",
                VehicleCategory = reader["VehicleCategory"]?.ToString() ?? "Universal",
                Voltage = reader["Voltage"]?.ToString() ?? "12V",
                Symptom = reader["Symptom"]?.ToString() ?? string.Empty,
                PossibleCauses = reader["PossibleCauses"]?.ToString() ?? string.Empty,
                DiagnosticProcedure = reader["DiagnosticProcedure"]?.ToString() ?? string.Empty,
                RecommendedMeasurements = reader["RecommendedMeasurements"] is DBNull ? null : reader["RecommendedMeasurements"]?.ToString(),
                Solution = reader["Solution"]?.ToString() ?? string.Empty,
                Warnings = reader["Warnings"] is DBNull ? null : reader["Warnings"]?.ToString(),
                Tags = reader["Tags"] is DBNull ? null : reader["Tags"]?.ToString(),
                SourceType = Enum.TryParse<KnowledgeSourceType>(reader["SourceType"]?.ToString(), out var st) ? st : KnowledgeSourceType.FIELD_EXPERIENCE,
                CreatedByUserId = reader["CreatedByUserId"] is DBNull ? 0 : Convert.ToInt32(reader["CreatedByUserId"]),
                CreatedByUserName = reader["CreatedByUserName"]?.ToString() ?? string.Empty,
                Status = Enum.TryParse<KnowledgeStatus>(reader["Status"]?.ToString(), out var ks) ? ks : KnowledgeStatus.PUBLISHED,
                CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var ca) ? ca : DateTime.Now,
                UpdatedAt = DateTime.TryParse(reader["UpdatedAt"]?.ToString(), out var ua) ? ua : DateTime.Now
            };
        }

        private static DiagnosticCase MapCase(DbDataReader reader)
        {
            return new DiagnosticCase
            {
                CaseId = Guid.TryParse(reader["CaseId"]?.ToString(), out var cid) ? cid : Guid.Empty,
                Code = reader["Code"]?.ToString() ?? string.Empty,
                Title = reader["Title"]?.ToString() ?? string.Empty,
                VehicleId = reader["VehicleId"] is DBNull || string.IsNullOrWhiteSpace(reader["VehicleId"]?.ToString()) ? null : Guid.TryParse(reader["VehicleId"]?.ToString(), out var vid) ? vid : null,
                VehicleModel = reader["VehicleModel"]?.ToString() ?? string.Empty,
                VehiclePlate = reader["VehiclePlate"] is DBNull ? null : reader["VehiclePlate"]?.ToString(),
                WorkOrderId = reader["WorkOrderId"] is DBNull || string.IsNullOrWhiteSpace(reader["WorkOrderId"]?.ToString()) ? null : Guid.TryParse(reader["WorkOrderId"]?.ToString(), out var wid) ? wid : null,
                WorkOrderNumber = reader["WorkOrderNumber"] is DBNull ? null : reader["WorkOrderNumber"]?.ToString(),
                TechnicianId = reader["TechnicianId"] is DBNull ? null : Convert.ToInt32(reader["TechnicianId"]),
                TechnicianName = reader["TechnicianName"] is DBNull ? null : reader["TechnicianName"]?.ToString(),
                System = reader["System"]?.ToString() ?? "Geral",
                Voltage = reader["Voltage"]?.ToString() ?? "12V",
                DtcCodes = reader["DtcCodes"] is DBNull ? null : reader["DtcCodes"]?.ToString(),
                Symptom = reader["Symptom"]?.ToString() ?? string.Empty,
                Measurements = reader["Measurements"] is DBNull ? null : reader["Measurements"]?.ToString(),
                InitialHypotheses = reader["InitialHypotheses"] is DBNull ? null : reader["InitialHypotheses"]?.ToString(),
                ConfirmedCause = reader["ConfirmedCause"]?.ToString() ?? string.Empty,
                Solution = reader["Solution"]?.ToString() ?? string.Empty,
                PartsUsed = reader["PartsUsed"] is DBNull ? null : reader["PartsUsed"]?.ToString(),
                TestResult = reader["TestResult"] is DBNull ? null : reader["TestResult"]?.ToString(),
                FinalResult = Enum.TryParse<DiagnosticCaseResult>(reader["FinalResult"]?.ToString(), out var res) ? res : DiagnosticCaseResult.RESOLVED,
                KnowledgeEntryId = reader["KnowledgeEntryId"] is DBNull || string.IsNullOrWhiteSpace(reader["KnowledgeEntryId"]?.ToString()) ? null : Guid.TryParse(reader["KnowledgeEntryId"]?.ToString(), out var kid) ? kid : null,
                CreatedAt = DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var ca) ? ca : DateTime.Now,
                UpdatedAt = DateTime.TryParse(reader["UpdatedAt"]?.ToString(), out var ua) ? ua : DateTime.Now
            };
        }
    }
}
