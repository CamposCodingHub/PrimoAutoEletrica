using System;
using System.IO;
using System.Threading.Tasks;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;
using PrimoAutoEletrica.Services.Fiscal.Testing;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class FiscalFoundationTests : IDisposable
{
    private readonly string _tempRoot;

    public FiscalFoundationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-fiscal-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        FiscalProductionGuard.SetProductionAllowedForTests(false);
    }

    [Fact]
    public void ProductionGuard_BloqueiaProducaoPorPadrao()
    {
        var denied = FiscalProductionGuard.TryDenyProduction(
            FiscalEnvironment.Production,
            Guid.NewGuid(),
            "key-1");

        Assert.NotNull(denied);
        Assert.Equal(FiscalDocumentStatus.ProductionBlocked, denied!.Status);
        Assert.Equal(FiscalErrorKind.ProductionBlocked, denied.ErrorKind);
        Assert.Equal("FISCAL-PROD-BLOCKED", denied.InternalCode);
    }

    [Fact]
    public void Homologacao_NaoEProducao()
    {
        var homolog = FiscalProductionGuard.TryDenyProduction(
            FiscalEnvironment.Homologation,
            Guid.NewGuid(),
            "key-h");
        var prod = FiscalProductionGuard.TryDenyProduction(
            FiscalEnvironment.Production,
            Guid.NewGuid(),
            "key-p");

        Assert.Null(homolog);
        Assert.NotNull(prod);
        Assert.NotEqual(FiscalEnvironment.Homologation, FiscalEnvironment.Production);
    }

    [Fact]
    public void Configuracao_DefaultsSeguros_HttpOff_ProducaoLocked()
    {
        var service = new FiscalConfigurationService(_tempRoot);
        var config = service.LoadOrCreate();

        Assert.False(config.LiveHttpEnabled);
        Assert.False(config.ProductionUnlocked);
        Assert.Equal(FiscalEnvironment.Homologation, config.Environment);
        Assert.Equal(FiscalProviderKind.FocusNfe, config.Provider);
        Assert.NotEqual(FiscalProviderKind.FakeTestOnly, config.Provider);
    }

    [Fact]
    public async Task FocusProvider_SemHttp_NaoRetornaAuthorized()
    {
        var configService = new FiscalConfigurationService(_tempRoot);
        var provider = new FocusNfeProvider(configService);
        var request = new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "focus-off-1",
            Environment = FiscalEnvironment.Homologation,
            DocumentType = FiscalDocumentType.NFe
        };

        var result = await provider.EmitirAsync(request);

        Assert.False(result.Success);
        Assert.NotEqual(FiscalDocumentStatus.Authorized, result.Status);
        Assert.Equal(FiscalDocumentStatus.NotConfigured, result.Status);
        Assert.Equal("FISCAL-FOCUS-HTTP-OFF", result.InternalCode);
    }

    [Fact]
    public async Task FocusProvider_Producao_Bloqueada()
    {
        var provider = new FocusNfeProvider(new FiscalConfigurationService(_tempRoot));
        var result = await provider.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "prod-block",
            Environment = FiscalEnvironment.Production
        });

        Assert.Equal(FiscalDocumentStatus.ProductionBlocked, result.Status);
        Assert.Equal(FiscalErrorKind.ProductionBlocked, result.ErrorKind);
    }

    [Fact]
    public async Task Idempotencia_RetryAposTimeout_MantemMesmaOperationId()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(database);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeTimeout);
        var app = new FiscalApplicationService(fake, store, audit: null, logger: new LoggerService());

        var opId = Guid.NewGuid();
        var key = "idem-timeout-1";
        var request = new FiscalEmissionRequest
        {
            FiscalOperationId = opId,
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation,
            OriginModule = "Test"
        };

        var first = await app.EmitirAsync(request);
        var second = await app.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation,
            OriginModule = "Test"
        });

        Assert.Equal(first.FiscalOperationId, second.FiscalOperationId);
        Assert.Equal(opId, first.FiscalOperationId);
        Assert.Equal(FiscalDocumentStatus.Authorized, second.Status);

        var recovered = store.FindByIdempotencyKey(key);
        Assert.NotNull(recovered);
        Assert.Equal(opId, recovered!.Id);

        var consult = await app.ConsultarAsync(opId);
        Assert.Equal(FiscalDocumentStatus.Authorized, consult.Status);
    }

    [Fact]
    public async Task FakeAuthorized_E_FakeRejected_SaoExplicitos()
    {
        var authorized = await new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized)
            .EmitirAsync(new FiscalEmissionRequest
            {
                FiscalOperationId = Guid.NewGuid(),
                IdempotencyKey = "auth",
                Environment = FiscalEnvironment.Homologation
            });
        var rejected = await new FakeFiscalProvider(FiscalFakeScenario.FakeRejected)
            .EmitirAsync(new FiscalEmissionRequest
            {
                FiscalOperationId = Guid.NewGuid(),
                IdempotencyKey = "rej",
                Environment = FiscalEnvironment.Homologation
            });

        Assert.True(authorized.Success);
        Assert.Equal(FiscalDocumentStatus.Authorized, authorized.Status);
        Assert.False(rejected.Success);
        Assert.Equal(FiscalDocumentStatus.Rejected, rejected.Status);
        Assert.Equal(FiscalErrorKind.FiscalRejection, rejected.ErrorKind);
        Assert.Equal("204", rejected.ProviderCode);
    }

    [Fact]
    public async Task FakeNetwork_E_CancelamentoInvalido()
    {
        var network = await new FakeFiscalProvider(FiscalFakeScenario.FakeNetworkError)
            .EmitirAsync(new FiscalEmissionRequest
            {
                FiscalOperationId = Guid.NewGuid(),
                IdempotencyKey = "net",
                Environment = FiscalEnvironment.Homologation
            });
        var cancel = await new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized)
            .CancelarAsync(new FiscalCancellationRequest
            {
                FiscalOperationId = Guid.NewGuid(),
                Justificativa = "curto",
                Environment = FiscalEnvironment.Homologation
            });

        Assert.Equal(FiscalErrorKind.NetworkError, network.ErrorKind);
        Assert.Equal(FiscalErrorKind.ValidationError, cancel.ErrorKind);
        var userMsg = FiscalUserMessages.For(network);
        Assert.Contains("servico fiscal", userMsg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FISCAL-FAKE-NETWORK", userMsg);
    }

    [Fact]
    public void Migration_CriaTabelasFiscais()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var applied = database.GetAppliedMigrations();
        Assert.Contains("202609080001", applied);

        using var connection = database.GetSqliteConnection();
        connection.Open();
        Assert.True(TableExists(connection, "FiscalOperations"));
        Assert.True(TableExists(connection, "FiscalDocuments"));
        Assert.True(TableExists(connection, "FiscalEvents"));
    }

    [Fact]
    public void Serializacao_Status_E_Ambiente_RoundTrip()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var payload = new FiscalStatusEnvelope
        {
            Status = FiscalDocumentStatus.Processing,
            Environment = FiscalEnvironment.Homologation
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload, options);
        var roundTrip = System.Text.Json.JsonSerializer.Deserialize<FiscalStatusEnvelope>(json, options);

        Assert.Contains("Processing", json);
        Assert.Contains("Homologation", json);
        Assert.DoesNotContain("Production", json);
        Assert.NotNull(roundTrip);
        Assert.Equal(FiscalDocumentStatus.Processing, roundTrip!.Status);
        Assert.Equal(FiscalEnvironment.Homologation, roundTrip.Environment);
        Assert.NotEqual(FiscalEnvironment.Homologation, FiscalEnvironment.Production);
    }

    private sealed class FiscalStatusEnvelope
    {
        public FiscalDocumentStatus Status { get; set; }
        public FiscalEnvironment Environment { get; set; }
    }

    private static bool TableExists(Microsoft.Data.Sqlite.SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 1 FROM sqlite_master
            WHERE type = 'table' AND name = @Name LIMIT 1;";
        command.Parameters.AddWithValue("@Name", tableName);
        return command.ExecuteScalar() != null;
    }

    public void Dispose()
    {
        FiscalProductionGuard.SetProductionAllowedForTests(false);
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // ignore cleanup races on Windows
        }
    }
}
