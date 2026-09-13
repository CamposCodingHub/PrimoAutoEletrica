using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;
using PrimoAutoEletrica.Services.Fiscal.Testing;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class FiscalMegaStressTests : IDisposable
{
    private readonly string _tempRoot;

    public FiscalMegaStressTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-fiscal-mega-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        FiscalProductionGuard.SetProductionAllowedForTests(false);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true); } catch { }
    }

    [Fact]
    public async Task MegaCiclo_10_NFe_SemDuplicidade()
    {
        var db = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(db);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(fake, store);
        var storage = new FiscalArtifactStorage(_tempRoot);
        var danfe = new DanfeInformationalPdfGenerator();

        for (var i = 0; i < 10; i++)
        {
            var key = $"mega-{i:000}";
            var emit = await app.EmitirAsync(new FiscalEmissionRequest
            {
                IdempotencyKey = key,
                Environment = FiscalEnvironment.Homologation,
                DocumentType = FiscalDocumentType.NFe,
                Provider = FiscalProviderKind.FakeTestOnly,
                EmpresaId = Guid.NewGuid()
            });
            Assert.Equal(FiscalDocumentStatus.Authorized, emit.Status);
            var again = await app.EmitirAsync(new FiscalEmissionRequest
            {
                IdempotencyKey = key,
                Environment = FiscalEnvironment.Homologation,
                DocumentType = FiscalDocumentType.NFe,
                Provider = FiscalProviderKind.FakeTestOnly
            });
            Assert.Equal(emit.FiscalOperationId, again.FiscalOperationId);
            await app.ObterXmlAsync(emit.FiscalOperationId, storage);
            await app.ObterDanfeAsync(emit.FiscalOperationId, danfe, storage);
        }

        Assert.Equal(10, fake.EmitCount);
        Assert.Equal(10, store.ListRecent(200).Count);
    }

    [Fact]
    public async Task Stress_100_Fake_Docs()
    {
        var db = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(db);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(fake, store);
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
        {
            var r = await app.EmitirAsync(new FiscalEmissionRequest
            {
                IdempotencyKey = $"stress-{i:0000}",
                Environment = FiscalEnvironment.Homologation,
                DocumentType = i % 3 == 0 ? FiscalDocumentType.NFCe : FiscalDocumentType.NFe,
                Provider = FiscalProviderKind.FakeTestOnly
            });
            Assert.Equal(FiscalDocumentStatus.Authorized, r.Status);
        }
        sw.Stop();
        Assert.Equal(100, fake.EmitCount);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30), $"Stress 100 docs took {sw.Elapsed}");
    }

    [Fact]
    public async Task Restart_Recupera_SemDuplicar()
    {
        var db = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(db);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeTimeout);
        var app = new FiscalApplicationService(fake, store);
        var key = "restart-1";
        var first = await app.EmitirAsync(new FiscalEmissionRequest
        {
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly
        });
        Assert.Equal(FiscalDocumentStatus.Unknown, first.Status);

        // "restart" — novo app service, mesmo store/DB
        var fake2 = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app2 = new FiscalApplicationService(fake2, store);
        var recovered = await app2.EmitirAsync(new FiscalEmissionRequest
        {
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly
        });
        // Timeout/Unknown exige consult antes de reemit — não deve duplicar cegamente
        Assert.Equal(first.FiscalOperationId, recovered.FiscalOperationId);
        Assert.True(recovered.InternalCode is "FISCAL-AWAIT-CONSULT" or "FISCAL-RECOVERED-VIA-CONSULT" or "FISCAL-IDEMPOTENT-REUSE"
            || recovered.Status == FiscalDocumentStatus.Unknown
            || recovered.Status == FiscalDocumentStatus.Authorized);
    }
}
