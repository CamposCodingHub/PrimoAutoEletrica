using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.Fiscal
{
    public sealed class FiscalOperationStore
    {
        private readonly DatabaseService _database;

        public FiscalOperationStore(DatabaseService database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public FiscalOperation? FindByIdempotencyKey(string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return null;
            }

            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, IdempotencyKey, DocumentType, Status, Environment, Provider,
                       OriginModule, OrdemServicoId, VendaId, OrcamentoId,
                       ProviderDocumentId, LastErrorKind, LastErrorMessage, CreatedAt, UpdatedAt
                FROM FiscalOperations
                WHERE IdempotencyKey = @Key
                LIMIT 1;";
            command.Parameters.AddWithValue("@Key", idempotencyKey);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapOperation(reader) : null;
        }

        public FiscalOperation? FindById(Guid id)
        {
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, IdempotencyKey, DocumentType, Status, Environment, Provider,
                       OriginModule, OrdemServicoId, VendaId, OrcamentoId,
                       ProviderDocumentId, LastErrorKind, LastErrorMessage, CreatedAt, UpdatedAt
                FROM FiscalOperations
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id.ToString("N"));

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapOperation(reader) : null;
        }

        public void Upsert(FiscalOperation operation)
        {
            ArgumentNullException.ThrowIfNull(operation);
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO FiscalOperations
                (
                    Id, IdempotencyKey, DocumentType, Status, Environment, Provider,
                    OriginModule, OrdemServicoId, VendaId, OrcamentoId,
                    ProviderDocumentId, LastErrorKind, LastErrorMessage, CreatedAt, UpdatedAt
                )
                VALUES
                (
                    @Id, @IdempotencyKey, @DocumentType, @Status, @Environment, @Provider,
                    @OriginModule, @OrdemServicoId, @VendaId, @OrcamentoId,
                    @ProviderDocumentId, @LastErrorKind, @LastErrorMessage, @CreatedAt, @UpdatedAt
                )
                ON CONFLICT(Id) DO UPDATE SET
                    Status = excluded.Status,
                    ProviderDocumentId = excluded.ProviderDocumentId,
                    LastErrorKind = excluded.LastErrorKind,
                    LastErrorMessage = excluded.LastErrorMessage,
                    UpdatedAt = excluded.UpdatedAt;";

            BindOperation(command, operation);
            command.ExecuteNonQuery();
        }

        public void AppendEvent(
            Guid operationId,
            string eventType,
            string? message,
            string? providerCode = null,
            string? correlationId = null)
        {
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO FiscalEvents
                (Id, OperationId, EventType, Message, ProviderCode, CorrelationId, CreatedAt)
                VALUES
                (@Id, @OperationId, @EventType, @Message, @ProviderCode, @CorrelationId, @CreatedAt);";
            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString("N"));
            command.Parameters.AddWithValue("@OperationId", operationId.ToString("N"));
            command.Parameters.AddWithValue("@EventType", eventType);
            command.Parameters.AddWithValue("@Message", (object?)message ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProviderCode", (object?)providerCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@CorrelationId", (object?)correlationId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
            command.ExecuteNonQuery();
        }

        public void UpsertDocument(FiscalDocumentRecord document)
        {
            ArgumentNullException.ThrowIfNull(document);
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO FiscalDocuments
                (
                    Id, OperationId, DocumentType, Numero, Serie, ChaveAcesso, Status,
                    Environment, Provider, Protocolo, Reason,
                    XmlEnviadoPath, XmlAutorizadoPath, OrdemServicoId, VendaId,
                    CreatedAt, UpdatedAt
                )
                VALUES
                (
                    @Id, @OperationId, @DocumentType, @Numero, @Serie, @ChaveAcesso, @Status,
                    @Environment, @Provider, @Protocolo, @Reason,
                    @XmlEnviadoPath, @XmlAutorizadoPath, @OrdemServicoId, @VendaId,
                    @CreatedAt, @UpdatedAt
                )
                ON CONFLICT(Id) DO UPDATE SET
                    Status = excluded.Status,
                    ChaveAcesso = excluded.ChaveAcesso,
                    Protocolo = excluded.Protocolo,
                    Reason = excluded.Reason,
                    XmlEnviadoPath = excluded.XmlEnviadoPath,
                    XmlAutorizadoPath = excluded.XmlAutorizadoPath,
                    UpdatedAt = excluded.UpdatedAt;";

            command.Parameters.AddWithValue("@Id", document.Id.ToString("N"));
            command.Parameters.AddWithValue("@OperationId", document.OperationId.ToString("N"));
            command.Parameters.AddWithValue("@DocumentType", (int)document.DocumentType);
            command.Parameters.AddWithValue("@Numero", (object?)document.Numero ?? DBNull.Value);
            command.Parameters.AddWithValue("@Serie", (object?)document.Serie ?? DBNull.Value);
            command.Parameters.AddWithValue("@ChaveAcesso", (object?)document.ChaveAcesso ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (int)document.Status);
            command.Parameters.AddWithValue("@Environment", (int)document.Environment);
            command.Parameters.AddWithValue("@Provider", (int)document.Provider);
            command.Parameters.AddWithValue("@Protocolo", (object?)document.Protocolo ?? DBNull.Value);
            command.Parameters.AddWithValue("@Reason", (object?)document.Reason ?? DBNull.Value);
            command.Parameters.AddWithValue("@XmlEnviadoPath", (object?)document.XmlEnviadoPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@XmlAutorizadoPath", (object?)document.XmlAutorizadoPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrdemServicoId", document.OrdemServicoId.HasValue ? document.OrdemServicoId.Value.ToString("N") : DBNull.Value);
            command.Parameters.AddWithValue("@VendaId", document.VendaId.HasValue ? document.VendaId.Value.ToString("N") : DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", document.CreatedAt.ToString("o"));
            command.Parameters.AddWithValue("@UpdatedAt", document.UpdatedAt.ToString("o"));
            command.ExecuteNonQuery();
        }

        public IReadOnlyList<FiscalOperation> ListRecent(int limit = 50)
        {
            limit = Math.Clamp(limit, 1, 200);
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, IdempotencyKey, DocumentType, Status, Environment, Provider,
                       OriginModule, OrdemServicoId, VendaId, OrcamentoId,
                       ProviderDocumentId, LastErrorKind, LastErrorMessage, CreatedAt, UpdatedAt
                FROM FiscalOperations
                ORDER BY UpdatedAt DESC
                LIMIT @Limit;";
            command.Parameters.AddWithValue("@Limit", limit);

            var list = new List<FiscalOperation>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapOperation(reader));
            }

            return list;
        }

        public IReadOnlyList<(string EventType, string? Message, string? ProviderCode, DateTime CreatedAt)> ListEvents(
            Guid operationId,
            int limit = 100)
        {
            limit = Math.Clamp(limit, 1, 500);
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EventType, Message, ProviderCode, CreatedAt
                FROM FiscalEvents
                WHERE OperationId = @OperationId
                ORDER BY CreatedAt ASC
                LIMIT @Limit;";
            command.Parameters.AddWithValue("@OperationId", operationId.ToString("N"));
            command.Parameters.AddWithValue("@Limit", limit);

            var list = new List<(string, string?, string?, DateTime)>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add((
                    reader.GetString(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind)));
            }

            return list;
        }

        public FiscalDocumentRecord? FindDocumentByOperationId(Guid operationId)
        {
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, OperationId, DocumentType, Numero, Serie, ChaveAcesso, Status,
                       Environment, Provider, Protocolo, Reason,
                       XmlEnviadoPath, XmlAutorizadoPath, OrdemServicoId, VendaId,
                       CreatedAt, UpdatedAt
                FROM FiscalDocuments
                WHERE OperationId = @OperationId
                LIMIT 1;";
            command.Parameters.AddWithValue("@OperationId", operationId.ToString("N"));

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new FiscalDocumentRecord
            {
                Id = Guid.Parse(reader.GetString(0)),
                OperationId = Guid.Parse(reader.GetString(1)),
                DocumentType = (FiscalDocumentType)reader.GetInt32(2),
                Numero = reader.IsDBNull(3) ? null : reader.GetString(3),
                Serie = reader.IsDBNull(4) ? null : reader.GetString(4),
                ChaveAcesso = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = (FiscalDocumentStatus)reader.GetInt32(6),
                Environment = (FiscalEnvironment)reader.GetInt32(7),
                Provider = (FiscalProviderKind)reader.GetInt32(8),
                Protocolo = reader.IsDBNull(9) ? null : reader.GetString(9),
                Reason = reader.IsDBNull(10) ? null : reader.GetString(10),
                XmlEnviadoPath = reader.IsDBNull(11) ? null : reader.GetString(11),
                XmlAutorizadoPath = reader.IsDBNull(12) ? null : reader.GetString(12),
                OrdemServicoId = reader.IsDBNull(13) ? null : Guid.Parse(reader.GetString(13)),
                VendaId = reader.IsDBNull(14) ? null : Guid.Parse(reader.GetString(14)),
                CreatedAt = DateTime.Parse(reader.GetString(15), null, System.Globalization.DateTimeStyles.RoundtripKind),
                UpdatedAt = DateTime.Parse(reader.GetString(16), null, System.Globalization.DateTimeStyles.RoundtripKind)
            };
        }

        private static void BindOperation(DbCommand command, FiscalOperation operation)
        {
            command.Parameters.AddWithValue("@Id", operation.Id.ToString("N"));
            command.Parameters.AddWithValue("@IdempotencyKey", operation.IdempotencyKey);
            command.Parameters.AddWithValue("@DocumentType", (int)operation.DocumentType);
            command.Parameters.AddWithValue("@Status", (int)operation.Status);
            command.Parameters.AddWithValue("@Environment", (int)operation.Environment);
            command.Parameters.AddWithValue("@Provider", (int)operation.Provider);
            command.Parameters.AddWithValue("@OriginModule", operation.OriginModule ?? string.Empty);
            command.Parameters.AddWithValue("@OrdemServicoId", operation.OrdemServicoId.HasValue ? operation.OrdemServicoId.Value.ToString("N") : DBNull.Value);
            command.Parameters.AddWithValue("@VendaId", operation.VendaId.HasValue ? operation.VendaId.Value.ToString("N") : DBNull.Value);
            command.Parameters.AddWithValue("@OrcamentoId", operation.OrcamentoId.HasValue ? operation.OrcamentoId.Value.ToString("N") : DBNull.Value);
            command.Parameters.AddWithValue("@ProviderDocumentId", (object?)operation.ProviderDocumentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastErrorKind", (object?)operation.LastErrorKind ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastErrorMessage", (object?)operation.LastErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", operation.CreatedAt.ToString("o"));
            command.Parameters.AddWithValue("@UpdatedAt", operation.UpdatedAt.ToString("o"));
        }

        private static FiscalOperation MapOperation(DbDataReader reader)
        {
            return new FiscalOperation
            {
                Id = Guid.Parse(reader.GetString(0)),
                IdempotencyKey = reader.GetString(1),
                DocumentType = (FiscalDocumentType)reader.GetInt32(2),
                Status = (FiscalDocumentStatus)reader.GetInt32(3),
                Environment = (FiscalEnvironment)reader.GetInt32(4),
                Provider = (FiscalProviderKind)reader.GetInt32(5),
                OriginModule = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                OrdemServicoId = reader.IsDBNull(7) ? null : Guid.Parse(reader.GetString(7)),
                VendaId = reader.IsDBNull(8) ? null : Guid.Parse(reader.GetString(8)),
                OrcamentoId = reader.IsDBNull(9) ? null : Guid.Parse(reader.GetString(9)),
                ProviderDocumentId = reader.IsDBNull(10) ? null : reader.GetString(10),
                LastErrorKind = reader.IsDBNull(11) ? null : reader.GetString(11),
                LastErrorMessage = reader.IsDBNull(12) ? null : reader.GetString(12),
                CreatedAt = DateTime.Parse(reader.GetString(13), null, System.Globalization.DateTimeStyles.RoundtripKind),
                UpdatedAt = DateTime.Parse(reader.GetString(14), null, System.Globalization.DateTimeStyles.RoundtripKind)
            };
        }
    }
}
