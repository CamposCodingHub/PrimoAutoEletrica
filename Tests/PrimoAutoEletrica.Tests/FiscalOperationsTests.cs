using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;
using PrimoAutoEletrica.Services.Fiscal.Testing;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class FiscalOperationsTests : IDisposable
{
    private readonly string _tempRoot;

    public FiscalOperationsTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-fiscal-ops-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        FiscalProductionGuard.SetProductionAllowedForTests(false);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }
        catch
        {
            // ignore cleanup
        }
    }

    [Theory]
    [InlineData(FiscalDocumentStatus.Rejected, FiscalDocumentStatus.Authorized)]
    [InlineData(FiscalDocumentStatus.Cancelled, FiscalDocumentStatus.Processing)]
    [InlineData(FiscalDocumentStatus.Cancelled, FiscalDocumentStatus.Authorized)]
    [InlineData(FiscalDocumentStatus.Authorized, FiscalDocumentStatus.Draft)]
    public void StateMachine_BloqueiaTransicoesInvalidas(FiscalDocumentStatus from, FiscalDocumentStatus to)
    {
        Assert.False(FiscalStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void StateMachine_PermiteAuthorizedParaCancelled()
    {
        Assert.True(FiscalStateMachine.CanTransition(FiscalDocumentStatus.Authorized, FiscalDocumentStatus.Cancelled));
        Assert.True(FiscalStateMachine.CanCancel(FiscalDocumentStatus.Authorized, FiscalEnvironment.Homologation));
        Assert.False(FiscalStateMachine.CanCancel(FiscalDocumentStatus.Authorized, FiscalEnvironment.Production));
        Assert.False(FiscalStateMachine.CanCancel(FiscalDocumentStatus.Rejected, FiscalEnvironment.Homologation));
    }

    [Fact]
    public void HealthCheck_SemEmitente_NaoReady()
    {
        var configService = new FiscalConfigurationService(_tempRoot);
        var health = new FiscalHealthCheck(configService).Evaluate(requireLiveCredential: false);
        Assert.NotEqual(FiscalHealthStatus.Ready, health.Status);
        Assert.False(health.ReadyForHomologEmission);
        Assert.Contains(health.Issues, i => i.Code.Contains("CNPJ", StringComparison.OrdinalIgnoreCase)
                                            || i.Code.Contains("RAZAO", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HealthCheck_CompletoSemCredencial_CredentialMissing()
    {
        var configService = new FiscalConfigurationService(_tempRoot);
        var cfg = configService.LoadOrCreate();
        cfg.Issuer = CreateIssuer();
        cfg.LiveHttpEnabled = true;
        configService.Save(cfg);

        var health = new FiscalHealthCheck(configService).Evaluate(requireLiveCredential: true);
        Assert.Equal(FiscalHealthStatus.CredentialMissing, health.Status);
        Assert.False(health.ReadyForHomologEmission);
    }

    [Fact]
    public void Preview_MostraItensSemEnviar()
    {
        var validator = new FiscalDocumentValidator();
        var builder = new FiscalNFePreviewBuilder(validator);
        var doc = CreateDocument();
        var preview = builder.Build(doc, CreateHomologConfig(), requireProviderCredential: false);
        Assert.Contains("Homolog", preview.TextoCompleto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NCM", preview.TextoCompleto, StringComparison.OrdinalIgnoreCase);
        Assert.True(preview.Validation.IsValid);
    }

    [Fact]
    public void Validator_ExigeSerie()
    {
        var doc = CreateDocument();
        doc.Emitente.SerieNFe = "";
        var result = new FiscalDocumentValidator().ValidateForHomologEmission(
            doc, CreateHomologConfig(), requireProviderCredential: false);
        Assert.Contains(result.Issues, i => i.Code == "FISCAL-EMITENTE-SERIE");
    }

    [Theory]
    [InlineData(FiscalFakeScenario.FakeAuthorized, FiscalDocumentStatus.Authorized)]
    [InlineData(FiscalFakeScenario.FakeRejected, FiscalDocumentStatus.Rejected)]
    [InlineData(FiscalFakeScenario.FakeTimeout, FiscalDocumentStatus.Unknown)]
    [InlineData(FiscalFakeScenario.FakeNetworkError, FiscalDocumentStatus.Failed)]
    [InlineData(FiscalFakeScenario.FakeUnavailable, FiscalDocumentStatus.Failed)]
    [InlineData(FiscalFakeScenario.FakeInvalidResponse, FiscalDocumentStatus.Failed)]
    [InlineData(FiscalFakeScenario.FakeHttp500, FiscalDocumentStatus.Failed)]
    [InlineData(FiscalFakeScenario.FakeUnauthorized, FiscalDocumentStatus.Failed)]
    [InlineData(FiscalFakeScenario.FakeSlowResponse, FiscalDocumentStatus.Processing)]
    public async Task FakeProvider_Cenarios(FiscalFakeScenario scenario, FiscalDocumentStatus expected)
    {
        var provider = new FakeFiscalProvider(scenario);
        var result = await provider.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = $"fake-{scenario}",
            Environment = FiscalEnvironment.Homologation
        });
        Assert.Equal(expected, result.Status);
        if (expected != FiscalDocumentStatus.Authorized)
        {
            Assert.NotEqual(FiscalDocumentStatus.Authorized, result.Status);
        }
    }

    [Fact]
    public async Task Cancel_Rejected_BloqueadoSemCorrupcao()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(database);
        var provider = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(provider, store);

        var opId = Guid.NewGuid();
        store.Upsert(new FiscalOperation
        {
            Id = opId,
            IdempotencyKey = "cancel-rej",
            DocumentType = FiscalDocumentType.NFe,
            Status = FiscalDocumentStatus.Rejected,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly,
            OriginModule = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var result = await app.CancelarAsync(new FiscalCancellationRequest
        {
            FiscalOperationId = opId,
            Justificativa = "Tentativa invalida de cancelamento",
            Environment = FiscalEnvironment.Homologation
        });

        Assert.Equal("FISCAL-CANCEL-STATE-BLOCKED", result.InternalCode);
        Assert.Equal(FiscalDocumentStatus.Rejected, store.FindById(opId)!.Status);
    }

    [Fact]
    public async Task Cancel_Authorized_FakeCancela()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(database);
        var provider = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(provider, store);

        var opId = Guid.NewGuid();
        store.Upsert(new FiscalOperation
        {
            Id = opId,
            IdempotencyKey = "cancel-ok",
            DocumentType = FiscalDocumentType.NFe,
            Status = FiscalDocumentStatus.Authorized,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly,
            OriginModule = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var result = await app.CancelarAsync(new FiscalCancellationRequest
        {
            FiscalOperationId = opId,
            Justificativa = "Cancelamento homologacao teste OK",
            Environment = FiscalEnvironment.Homologation
        });

        Assert.Equal(FiscalDocumentStatus.Cancelled, result.Status);
        Assert.Equal(FiscalDocumentStatus.Cancelled, store.FindById(opId)!.Status);
    }

    [Fact]
    public async Task Concurrent_SameKey_SingleLogicalOperation()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(database);
        var provider = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(provider, store);
        var key = "concurrent-key-1";
        var opId = Guid.NewGuid();

        var t1 = app.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = opId,
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation,
            OriginModule = "Test",
            Total = 10
        });
        var t2 = app.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = opId,
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation,
            OriginModule = "Test",
            Total = 10
        });

        var results = await Task.WhenAll(t1, t2);
        Assert.All(results, r => Assert.Equal(opId, r.FiscalOperationId));
        Assert.True(provider.EmitCount <= 2);
        Assert.Equal(opId, store.FindByIdempotencyKey(key)!.Id);
    }

    private static FiscalIssuerProfile CreateIssuer() => new()
    {
        Cnpj = "11222333000181",
        RazaoSocial = "Empresa Teste LTDA",
        NomeFantasia = "Empresa Teste",
        InscricaoEstadual = "123456789",
        RegimeTributario = "1",
        Logradouro = "Rua Emitente",
        Numero = "100",
        Bairro = "Centro",
        Municipio = "Sao Paulo",
        CodigoMunicipioIbge = "3550308",
        Uf = "SP",
        Cep = "01001000",
        DefaultIcmsSituacaoTributaria = "102",
        DefaultIcmsOrigem = "0",
        SerieNFe = "1"
    };

    private static FiscalConfiguration CreateHomologConfig() => new()
    {
        Environment = FiscalEnvironment.Homologation,
        Provider = FiscalProviderKind.FocusNfe,
        LiveHttpEnabled = false,
        ProductionUnlocked = false,
        HomologationBaseUrl = FiscalConfigurationService.DefaultHomologationBaseUrl,
        Issuer = CreateIssuer()
    };

    private static FiscalNFeDocument CreateDocument() => new()
    {
        FiscalOperationId = Guid.NewGuid(),
        IdempotencyKey = "preview-1",
        Environment = FiscalEnvironment.Homologation,
        VendaId = Guid.NewGuid(),
        Emitente = CreateIssuer(),
        Destinatario = new FiscalNFeDestinatario
        {
            Nome = "Cliente",
            Documento = "12345678909",
            IsCnpj = false,
            Logradouro = "Rua B",
            Numero = "1",
            Bairro = "Centro",
            Municipio = "Sao Paulo",
            Uf = "SP",
            Cep = "01001000"
        },
        Itens = new List<FiscalNFeItem>
        {
            new()
            {
                NumeroItem = 1,
                Codigo = "P1",
                Descricao = "Produto",
                Ncm = "85364100",
                Cfop = "5102",
                Unidade = "UN",
                Quantidade = 1,
                ValorUnitario = 10,
                ValorTotal = 10,
                IcmsOrigem = "0",
                IcmsSituacaoTributaria = "102"
            }
        },
        ValorProdutos = 10,
        ValorDesconto = 0,
        ValorTotal = 10
    };
}
