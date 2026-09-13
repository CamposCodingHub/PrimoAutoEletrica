using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.Fiscal
{
    public sealed class FiscalEmpresaStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly DatabaseService _database;

        public FiscalEmpresaStore(DatabaseService database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public IReadOnlyList<FiscalEmpresaRecord> ListAll(bool onlyActive = false)
        {
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = onlyActive
                ? @"SELECT Id, CodigoInterno, NomeExibicao, Ativa, EmitenteJson, ProviderPreferido, AmbientePadrao,
                          SerieNFe, SerieNFCe, CreatedAt, UpdatedAt
                   FROM FiscalEmpresas WHERE Ativa = 1 ORDER BY NomeExibicao;"
                : @"SELECT Id, CodigoInterno, NomeExibicao, Ativa, EmitenteJson, ProviderPreferido, AmbientePadrao,
                          SerieNFe, SerieNFCe, CreatedAt, UpdatedAt
                   FROM FiscalEmpresas ORDER BY NomeExibicao;";

            var list = new List<FiscalEmpresaRecord>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        public FiscalEmpresaRecord? FindById(Guid id)
        {
            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, CodigoInterno, NomeExibicao, Ativa, EmitenteJson, ProviderPreferido, AmbientePadrao,
                       SerieNFe, SerieNFCe, CreatedAt, UpdatedAt
                FROM FiscalEmpresas WHERE Id = @Id LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id.ToString("N"));
            using var reader = command.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public FiscalEmpresaRecord? FindByCodigo(string codigoInterno)
        {
            if (string.IsNullOrWhiteSpace(codigoInterno))
            {
                return null;
            }

            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, CodigoInterno, NomeExibicao, Ativa, EmitenteJson, ProviderPreferido, AmbientePadrao,
                       SerieNFe, SerieNFCe, CreatedAt, UpdatedAt
                FROM FiscalEmpresas WHERE CodigoInterno = @Codigo LIMIT 1;";
            command.Parameters.AddWithValue("@Codigo", codigoInterno.Trim());
            using var reader = command.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public void Upsert(FiscalEmpresaRecord empresa)
        {
            ArgumentNullException.ThrowIfNull(empresa);
            if (empresa.Id == Guid.Empty)
            {
                empresa.Id = Guid.NewGuid();
            }

            if (string.IsNullOrWhiteSpace(empresa.CodigoInterno))
            {
                throw new ArgumentException("CodigoInterno obrigatorio.", nameof(empresa));
            }

            var now = DateTime.UtcNow;
            if (empresa.CreatedAt == default)
            {
                empresa.CreatedAt = now;
            }

            empresa.UpdatedAt = now;

            using var connection = _database.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO FiscalEmpresas
                (Id, CodigoInterno, NomeExibicao, Ativa, EmitenteJson, ProviderPreferido, AmbientePadrao,
                 SerieNFe, SerieNFCe, CreatedAt, UpdatedAt)
                VALUES
                (@Id, @CodigoInterno, @NomeExibicao, @Ativa, @EmitenteJson, @ProviderPreferido, @AmbientePadrao,
                 @SerieNFe, @SerieNFCe, @CreatedAt, @UpdatedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    CodigoInterno = excluded.CodigoInterno,
                    NomeExibicao = excluded.NomeExibicao,
                    Ativa = excluded.Ativa,
                    EmitenteJson = excluded.EmitenteJson,
                    ProviderPreferido = excluded.ProviderPreferido,
                    AmbientePadrao = excluded.AmbientePadrao,
                    SerieNFe = excluded.SerieNFe,
                    SerieNFCe = excluded.SerieNFCe,
                    UpdatedAt = excluded.UpdatedAt;";

            command.Parameters.AddWithValue("@Id", empresa.Id.ToString("N"));
            command.Parameters.AddWithValue("@CodigoInterno", empresa.CodigoInterno.Trim());
            command.Parameters.AddWithValue("@NomeExibicao", empresa.NomeExibicao ?? string.Empty);
            command.Parameters.AddWithValue("@Ativa", empresa.Ativa ? 1 : 0);
            command.Parameters.AddWithValue("@EmitenteJson", JsonSerializer.Serialize(empresa.Emitente ?? new FiscalIssuerProfile(), JsonOptions));
            command.Parameters.AddWithValue("@ProviderPreferido", (int)empresa.ProviderPreferido);
            command.Parameters.AddWithValue("@AmbientePadrao", (int)empresa.AmbientePadrao);
            command.Parameters.AddWithValue("@SerieNFe", (object?)empresa.SerieNFe ?? DBNull.Value);
            command.Parameters.AddWithValue("@SerieNFCe", (object?)empresa.SerieNFCe ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", empresa.CreatedAt.ToString("o"));
            command.Parameters.AddWithValue("@UpdatedAt", empresa.UpdatedAt.ToString("o"));
            command.ExecuteNonQuery();
        }

        private static FiscalEmpresaRecord Map(DbDataReader reader)
        {
            var emitenteJson = reader.IsDBNull(4) ? "{}" : reader.GetString(4);
            FiscalIssuerProfile emitente;
            try
            {
                emitente = JsonSerializer.Deserialize<FiscalIssuerProfile>(emitenteJson, JsonOptions) ?? new FiscalIssuerProfile();
            }
            catch
            {
                emitente = new FiscalIssuerProfile();
            }

            return new FiscalEmpresaRecord
            {
                Id = Guid.Parse(reader.GetString(0)),
                CodigoInterno = reader.GetString(1),
                NomeExibicao = reader.GetString(2),
                Ativa = reader.GetInt32(3) == 1,
                Emitente = emitente,
                ProviderPreferido = (FiscalProviderKind)reader.GetInt32(5),
                AmbientePadrao = (FiscalEnvironment)reader.GetInt32(6),
                SerieNFe = reader.IsDBNull(7) ? null : reader.GetString(7),
                SerieNFCe = reader.IsDBNull(8) ? null : reader.GetString(8),
                CreatedAt = DateTime.Parse(reader.GetString(9), null, System.Globalization.DateTimeStyles.RoundtripKind),
                UpdatedAt = DateTime.Parse(reader.GetString(10), null, System.Globalization.DateTimeStyles.RoundtripKind)
            };
        }
    }
}
