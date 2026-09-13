using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;
using PrimoAutoEletrica.Services.Fiscal.PlugNotas;
using PrimoAutoEletrica.Services.Fiscal.Testing;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class FiscalNet1026FoundationTests : IDisposable
{
    private readonly string _tempRoot;

    public FiscalNet1026FoundationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-fiscal-net1026-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        FiscalProductionGuard.SetProductionAllowedForTests(false);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // ignore cleanup
        }
    }

    private DatabaseService CreateDb()
        => new DatabaseService(_tempRoot, logger: new LoggerService());

    [Fact]
    public void Multiempresa_IsolaDocumentosPorEmpresa()
    {
        var db = CreateDb();
        var empresas = new FiscalEmpresaStore(db);
        var ops = new FiscalOperationStore(db);

        var a = new FiscalEmpresaRecord
        {
            CodigoInterno = "EMP-A",
            NomeExibicao = "Empresa A Fake",
            Emitente = new FiscalIssuerProfile { RazaoSocial = "A Fake Ltda" }
        };
        var b = new FiscalEmpresaRecord
        {
            CodigoInterno = "EMP-B",
            NomeExibicao = "Empresa B Fake",
            Emitente = new FiscalIssuerProfile { RazaoSocial = "B Fake Ltda" }
        };
        empresas.Upsert(a);
        empresas.Upsert(b);

        ops.Upsert(new FiscalOperation
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "emp-a-1",
            DocumentType = FiscalDocumentType.NFe,
            Status = FiscalDocumentStatus.Authorized,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly,
            EmpresaId = a.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        ops.Upsert(new FiscalOperation
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "emp-b-1",
            DocumentType = FiscalDocumentType.NFCe,
            Status = FiscalDocumentStatus.Authorized,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly,
            EmpresaId = b.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var listA = ops.ListByEmpresa(a.Id);
        var listB = ops.ListByEmpresa(b.Id);
        Assert.Single(listA);
        Assert.Single(listB);
        Assert.Equal(a.Id, listA[0].EmpresaId);
        Assert.Equal(b.Id, listB[0].EmpresaId);
        Assert.DoesNotContain(listA, x => x.EmpresaId == b.Id);
    }

    [Fact]
    public void ItemTotaller_Deterministico()
    {
        Assert.Equal(90.00m, FiscalItemTotaller.LineTotal(10, 10, 10));
        var s = FiscalItemTotaller.Summarize(100.005m, 0.004m, 1.111m, 0, 0);
        Assert.Equal(100.01m, s.Produtos);
        Assert.Equal(0.00m, s.Descontos);
        Assert.Equal(1.11m, s.Frete);
        Assert.Equal(101.12m, s.Total);
    }

    [Fact]
    public void ArtifactStorage_BloqueiaPathTraversal()
    {
        var storage = new FiscalArtifactStorage(_tempRoot);
        Assert.ThrowsAny<Exception>(() =>
            storage.SaveXml(Guid.NewGuid(), Guid.NewGuid(), "../evil.xml", "<x/>"));
        var path = storage.SaveXml(Guid.Empty, Guid.NewGuid(), "ok.xml", "<nfe/>");
        Assert.True(File.Exists(path));
        Assert.StartsWith(storage.RootPath, path, StringComparison.OrdinalIgnoreCase);
        Assert.Null(storage.TryReadText(Path.Combine(_tempRoot, "outside.txt")));
    }

    [Fact]
    public async Task Fake_Emit_Xml_Danfe_Ciclo()
    {
        var db = CreateDb();
        var store = new FiscalOperationStore(db);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(fake, store);
        var storage = new FiscalArtifactStorage(_tempRoot);
        var danfe = new DanfeInformationalPdfGenerator();
        var empresaId = Guid.NewGuid();

        var emit = await app.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "ciclo-xml-danfe-1",
            Environment = FiscalEnvironment.Homologation,
            DocumentType = FiscalDocumentType.NFe,
            Provider = FiscalProviderKind.FakeTestOnly,
            EmpresaId = empresaId,
            OriginModule = "Test"
        });

        Assert.Equal(FiscalDocumentStatus.Authorized, emit.Status);
        var xml = await app.ObterXmlAsync(emit.FiscalOperationId, storage);
        Assert.False(string.IsNullOrWhiteSpace(xml.XmlContent));
        Assert.False(string.IsNullOrWhiteSpace(xml.ArtifactLocalPath));
        Assert.True(File.Exists(xml.ArtifactLocalPath!));

        var pdf = await app.ObterDanfeAsync(emit.FiscalOperationId, danfe, storage);
        Assert.NotNull(pdf.PdfBytes);
        Assert.True(pdf.PdfBytes!.Length > 100);
        Assert.Contains("PDF informativo", pdf.Message, StringComparison.OrdinalIgnoreCase);

        var cancel = await app.CancelarAsync(new FiscalCancellationRequest
        {
            FiscalOperationId = emit.FiscalOperationId,
            Environment = FiscalEnvironment.Homologation,
            Justificativa = "Cancelamento fake de teste com mais de quinze chars"
        });
        Assert.Equal(FiscalDocumentStatus.Cancelled, cancel.Status);
    }

    [Fact]
    public async Task Fake_NFCe_NFSe_EmitAuthorize()
    {
        var db = CreateDb();
        var store = new FiscalOperationStore(db);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(fake, store);

        var nfce = await app.EmitirAsync(new FiscalEmissionRequest
        {
            IdempotencyKey = "nfce-fake-1",
            DocumentType = FiscalDocumentType.NFCe,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly
        });
        Assert.Equal(FiscalDocumentStatus.Authorized, nfce.Status);

        var nfse = await app.EmitirAsync(new FiscalEmissionRequest
        {
            IdempotencyKey = "nfse-fake-1",
            DocumentType = FiscalDocumentType.NFSe,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly
        });
        Assert.Equal(FiscalDocumentStatus.Authorized, nfse.Status);

        var scaffold = new ScaffoldNfseProvider();
        var blocked = await scaffold.EmitirRpsAsync(new FiscalNfseDocument(), new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "nfse-scaffold"
        });
        Assert.Equal(FiscalDocumentStatus.NotImplemented, blocked.Status);
    }

    [Fact]
    public async Task Focus_Cancel_Delete_HttpStatuses()
    {
        var handler = new StubHttpHandler(req =>
        {
            Assert.Equal(HttpMethod.Delete, req.Method);
            Assert.Contains("/v2/nfe/", req.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"cancelado\",\"mensagem\":\"ok\"}")
            };
        });
        var http = new FocusNfeHttpClient(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });
        var cfg = new FiscalConfigurationService(_tempRoot);
        var c = cfg.LoadOrCreate();
        c.LiveHttpEnabled = true;
        cfg.Save(c);
        cfg.SaveHomologationToken("token-test-only-not-real");

        var provider = new FocusNfeProvider(cfg, http);
        var result = await provider.CancelarAsync(new FiscalCancellationRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            ProviderReference = "ref-cancel-1",
            Justificativa = "Justificativa valida com mais de quinze",
            Environment = FiscalEnvironment.Homologation
        });

        Assert.Equal(FiscalDocumentStatus.Cancelled, result.Status);
        Assert.True(result.Success);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "FISCAL-FOCUS-401")]
    [InlineData(HttpStatusCode.Forbidden, "FISCAL-FOCUS-403")]
    [InlineData(HttpStatusCode.TooManyRequests, "FISCAL-FOCUS-429")]
    [InlineData(HttpStatusCode.InternalServerError, "FISCAL-FOCUS-500")]
    [InlineData(HttpStatusCode.BadGateway, "FISCAL-FOCUS-502")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "FISCAL-FOCUS-503")]
    public async Task Focus_HttpErrorMapping(HttpStatusCode code, string expectedInternal)
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(code)
        {
            Content = new StringContent("{\"status\":\"erro\",\"mensagem\":\"x\"}")
        });
        var http = new FocusNfeHttpClient(new HttpClient(handler));
        var cfg = new FiscalConfigurationService(_tempRoot);
        var c = cfg.LoadOrCreate();
        c.LiveHttpEnabled = true;
        cfg.Save(c);
        cfg.SaveHomologationToken("token-test-only");
        var provider = new FocusNfeProvider(cfg, http);
        var result = await provider.ConsultarAsync(new FiscalOperation
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "consult-err",
            Environment = FiscalEnvironment.Homologation,
            Status = FiscalDocumentStatus.Processing,
            DocumentType = FiscalDocumentType.NFe,
            Provider = FiscalProviderKind.FocusNfe,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        Assert.Equal(expectedInternal, result.InternalCode);
    }

    [Fact]
    public async Task Webhook_Idempotente_EAssinatura()
    {
        var processor = new FiscalWebhookProcessor();
        var env = new FiscalWebhookEnvelope
        {
            Provider = "Focus",
            EventId = "evt-1",
            SignatureHeader = "sig",
            PayloadJson = "{\"ref\":\"1\"}"
        };
        var first = await processor.ProcessAsync(env);
        var second = await processor.ProcessAsync(env);
        Assert.True(first.Accepted);
        Assert.False(first.Duplicate);
        Assert.True(second.Accepted);
        Assert.True(second.Duplicate);

        var noSig = await processor.ProcessAsync(new FiscalWebhookEnvelope
        {
            Provider = "Focus",
            EventId = "evt-2",
            PayloadJson = "{}"
        });
        Assert.False(noSig.Accepted);
        Assert.Equal("FISCAL-WEBHOOK-SIGNATURE", noSig.InternalCode);
    }

    [Fact]
    public async Task WhatsApp_Manual_BlockedExternal()
    {
        var wa = new ManualWhatsAppProvider();
        var result = await wa.SendAsync(new WhatsAppMessageRequest
        {
            DestinationE164Synthetic = "5511999999999",
            Body = "Teste sintetico"
        });
        Assert.Equal("WHATSAPP-BLOCKED-EXTERNAL", result.InternalCode);
        Assert.Contains("wa.me", result.ManualDeepLink!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Certificate_XmlSigner_Blocked()
    {
        var cert = new NullCertificateProvider();
        Assert.Null(await cert.TryGetCertificateAsync(Guid.NewGuid()));
        var signer = new BlockedXmlSigner();
        var sign = await signer.SignAsync("<x/>", Guid.NewGuid());
        Assert.False(sign.Success);
        Assert.Equal("FISCAL-CERT-BLOCKED-EXTERNAL", sign.InternalCode);
    }

    [Fact]
    public async Task PlugNotas_Scaffold()
    {
        var p = new PlugNotasProvider();
        var r = await p.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "plug-1",
            Environment = FiscalEnvironment.Homologation
        });
        Assert.Equal("FISCAL-PLUGNOTAS-SCAFFOLD", r.InternalCode);
    }

    [Fact]
    public async Task Idempotencia_Emit_NaoDuplica()
    {
        var db = CreateDb();
        var store = new FiscalOperationStore(db);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(fake, store);
        var req = new FiscalEmissionRequest
        {
            IdempotencyKey = "idem-100",
            Environment = FiscalEnvironment.Homologation,
            DocumentType = FiscalDocumentType.NFe,
            Provider = FiscalProviderKind.FakeTestOnly
        };
        var r1 = await app.EmitirAsync(req);
        var r2 = await app.EmitirAsync(req);
        Assert.Equal(r1.FiscalOperationId, r2.FiscalOperationId);
        Assert.Equal(1, fake.EmitCount);
        Assert.Equal("FISCAL-IDEMPOTENT-REUSE", r2.InternalCode);
    }

    [Fact]
    public void Danfe_Pdf_Valido_Header()
    {
        var gen = new DanfeInformationalPdfGenerator();
        var pdf = gen.GenerateInformationalPdf(new FiscalOperation
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "d1",
            Status = FiscalDocumentStatus.Authorized,
            DocumentType = FiscalDocumentType.NFe,
            Environment = FiscalEnvironment.Homologation,
            Provider = FiscalProviderKind.FakeTestOnly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, null);
        Assert.True(pdf.Length > 50);
        Assert.Equal(0x25, pdf[0]); // %
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
