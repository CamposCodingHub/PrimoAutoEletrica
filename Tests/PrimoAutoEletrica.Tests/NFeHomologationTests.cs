using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;
using PrimoAutoEletrica.Services.Fiscal.Testing;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class NFeHomologationTests : IDisposable
{
    private readonly string _tempRoot;

    public NFeHomologationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-nfe-homolog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        FiscalProductionGuard.SetProductionAllowedForTests(false);
    }

    [Fact]
    public void Validator_BloqueiaSemNcm_SemInventar()
    {
        var broken = CloneWithItemNcm(CreateValidDocument(), "");
        var config = CreateHomologConfig(liveHttp: false);
        var result = new FiscalDocumentValidator().ValidateForHomologEmission(broken, config, requireProviderCredential: false);
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "FISCAL-ITEM-NCM");
    }

    [Fact]
    public void Validator_BloqueiaNcmFicticio()
    {
        var broken = CloneWithItemNcm(CreateValidDocument(), "00000000");
        var result = new FiscalDocumentValidator().ValidateForHomologEmission(
            broken, CreateHomologConfig(false), requireProviderCredential: false);
        Assert.Contains(result.Issues, i => i.Code == "FISCAL-ITEM-NCM-INVALID");
    }

    [Fact]
    public void FocusPayload_IncluiSerieQuandoConfigurada()
    {
        var doc = CreateValidDocument();
        doc.Emitente.SerieNFe = "1";
        doc.Emitente.NumeroInicialNFe = "10";
        var payload = FocusNfePayloadBuilder.Build(doc);
        Assert.Equal("1", payload["serie"]);
        Assert.Equal("10", payload["numero"]);
    }

    [Fact]
    public void ProductionGuard_BloqueiaEmissaoProducao()
    {
        var denied = FiscalProductionGuard.TryDenyProduction(FiscalEnvironment.Production, Guid.NewGuid(), "k");
        Assert.NotNull(denied);
        Assert.Equal("FISCAL-PROD-BLOCKED", denied!.InternalCode);
    }

    [Fact]
    public async Task FocusProvider_Producao_NaoChamaHttp()
    {
        var handler = new RecordingHandler();
        var http = new FocusNfeHttpClient(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });
        var configService = new FiscalConfigurationService(_tempRoot);
        var cfg = configService.LoadOrCreate();
        cfg.LiveHttpEnabled = true;
        configService.Save(cfg);
        var provider = new FocusNfeProvider(configService, http);

        var result = await provider.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "prod-1",
            Environment = FiscalEnvironment.Production
        });

        Assert.Equal(FiscalDocumentStatus.ProductionBlocked, result.Status);
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task FocusProvider_SemToken_NaoAutoriza()
    {
        var configService = new FiscalConfigurationService(_tempRoot);
        var c = configService.LoadOrCreate();
        c.LiveHttpEnabled = true;
        configService.Save(c);
        var provider = new FocusNfeProvider(configService);

        var result = await provider.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "notoken",
            Environment = FiscalEnvironment.Homologation
        });

        Assert.NotEqual(FiscalDocumentStatus.Authorized, result.Status);
        Assert.Equal("FISCAL-FOCUS-TOKEN-MISSING", result.InternalCode);
    }

    [Fact]
    public async Task Timeout_DepoisConsultar_AutorizaSemNovaEmissaoCega()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(database);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeTimeout);
        var app = new FiscalApplicationService(fake, store, logger: new LoggerService());
        var opId = Guid.NewGuid();
        var key = "timeout-key-1";

        var first = await app.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = opId,
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation
        });
        Assert.Equal(FiscalErrorKind.Timeout, first.ErrorKind);
        Assert.Equal(FiscalDocumentStatus.Unknown, first.Status);

        var second = await app.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation
        });

        Assert.Equal(opId, second.FiscalOperationId);
        Assert.Equal(FiscalDocumentStatus.Authorized, second.Status);
        Assert.Equal(1, fake.EmitCount); // não reemitiu
    }

    [Fact]
    public async Task Rejected_NaoViraAuthorized()
    {
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeRejected);
        var result = await fake.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "rej",
            Environment = FiscalEnvironment.Homologation
        });
        Assert.Equal(FiscalDocumentStatus.Rejected, result.Status);
        Assert.Equal("204", result.ProviderCode);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Network_E_Unavailable_E_InvalidResponse()
    {
        var net = await new FakeFiscalProvider(FiscalFakeScenario.FakeNetworkError)
            .EmitirAsync(NewReq("net"));
        var unavail = await new FakeFiscalProvider(FiscalFakeScenario.FakeUnavailable)
            .EmitirAsync(NewReq("503"));
        var invalid = await new FakeFiscalProvider(FiscalFakeScenario.FakeInvalidResponse)
            .EmitirAsync(NewReq("bad"));

        Assert.Equal(FiscalErrorKind.NetworkError, net.ErrorKind);
        Assert.Equal("FISCAL-FAKE-503", unavail.InternalCode);
        Assert.Equal("FISCAL-FOCUS-INVALID-RESPONSE", invalid.InternalCode);
        Assert.NotEqual(FiscalDocumentStatus.Authorized, net.Status);
    }

    [Fact]
    public async Task Duplicate_MesmaOperationId()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(database);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeAuthorized);
        var app = new FiscalApplicationService(fake, store);
        var opId = Guid.NewGuid();
        var key = "dup-key";

        var a = await app.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = opId,
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation
        });
        var b = await app.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation
        });

        Assert.Equal(a.FiscalOperationId, b.FiscalOperationId);
        Assert.Equal("FISCAL-IDEMPOTENT-REUSE", b.InternalCode);
        Assert.Equal(1, fake.EmitCount);
    }

    [Fact]
    public async Task Restart_RecuperaOperacaoDoBanco()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var store = new FiscalOperationStore(database);
        var fake = new FakeFiscalProvider(FiscalFakeScenario.FakeTimeout);
        var app1 = new FiscalApplicationService(fake, store);
        var opId = Guid.NewGuid();
        var key = "restart-key";

        await app1.EmitirAsync(new FiscalEmissionRequest
        {
            FiscalOperationId = opId,
            IdempotencyKey = key,
            Environment = FiscalEnvironment.Homologation
        });

        // "Reinício": novo service/store sobre o mesmo DB
        var app2 = new FiscalApplicationService(fake, new FiscalOperationStore(database));
        var found = app2.FindByIdempotencyKey(key);
        Assert.NotNull(found);
        Assert.Equal(opId, found!.Id);

        var consult = await app2.ConsultarAsync(opId);
        Assert.Equal(FiscalDocumentStatus.Authorized, consult.Status);
    }

    [Fact]
    public void Mapper_Venda_PreservaTotais_SemInventarNcm()
    {
        var vendaId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var venda = new Venda
        {
            Id = vendaId,
            Desconto = 5m,
            Itens =
            {
                new ItemVenda
                {
                    ProdutoId = produtoId,
                    Tipo = "Produto",
                    Descricao = "Bateria",
                    Quantidade = 2,
                    PrecoUnitario = 100m
                }
            }
        };
        var cliente = new Cliente
        {
            Nome = "Cliente Teste",
            CPF = "12345678909",
            Rua = "Rua A",
            Numero = "10",
            Bairro = "Centro",
            Cidade = "Sao Paulo",
            Estado = "SP",
            CEP = "01001000"
        };
        var produtos = new Dictionary<Guid, Produto>
        {
            [produtoId] = new Produto
            {
                Id = produtoId,
                Codigo = "BAT-1",
                Nome = "Bateria",
                NCMS = "",
                CFOP = "",
                UnidadeMedida = "UN"
            }
        };
        var issuer = CreateIssuer();
        var mapped = new VendaFiscalNFeMapper().Map(
            venda, cliente, produtos, issuer, Guid.NewGuid(), "k", FiscalEnvironment.Homologation);

        Assert.Equal(195m, mapped.ValorTotal); // 200 - 5
        Assert.Equal(string.Empty, mapped.Itens[0].Ncm);

        var validation = new FiscalDocumentValidator().ValidateForHomologEmission(
            mapped, CreateHomologConfig(false), requireProviderCredential: false);
        Assert.False(validation.IsValid);
    }

    [Fact]
    public void ResponseMapper_JsonVazio_NaoAutoriza()
    {
        var mapped = FocusNfeResponseMapper.MapEmissionResponse(
            new FocusNfeHttpResponse { StatusCode = 200, Body = "", IsSuccessStatusCode = true },
            Guid.NewGuid(),
            "k");
        Assert.Equal("FISCAL-FOCUS-INVALID-RESPONSE", mapped.InternalCode);
        Assert.NotEqual(FiscalDocumentStatus.Authorized, mapped.Status);
    }

    [Fact]
    public void ResponseMapper_HttpStatuses()
    {
        Assert.Equal(FiscalErrorKind.AuthenticationError,
            FocusNfeResponseMapper.MapEmissionResponse(Http(401, "{}"), Guid.NewGuid(), "k").ErrorKind);
        Assert.Equal("FISCAL-FOCUS-429",
            FocusNfeResponseMapper.MapEmissionResponse(Http(429, "{}"), Guid.NewGuid(), "k").InternalCode);
        Assert.Equal(FiscalErrorKind.ProviderError,
            FocusNfeResponseMapper.MapEmissionResponse(Http(503, "{}"), Guid.NewGuid(), "k").ErrorKind);
    }

    [Fact]
    public void Config_NaoPermiteUrlProducaoComoHomolog()
    {
        var config = new FiscalConfiguration
        {
            Environment = FiscalEnvironment.Homologation,
            HomologationBaseUrl = "https://api.focusnfe.com.br",
            LiveHttpEnabled = true
        };
        FiscalConfigurationService.Normalize(config);
        Assert.False(config.LiveHttpEnabled);
        Assert.Equal(FiscalConfigurationService.DefaultHomologationBaseUrl, config.HomologationBaseUrl);
    }

    private static FocusNfeHttpResponse Http(int code, string body) => new()
    {
        StatusCode = code,
        Body = body,
        IsSuccessStatusCode = code is >= 200 and < 300
    };

    private static FiscalEmissionRequest NewReq(string key) => new()
    {
        FiscalOperationId = Guid.NewGuid(),
        IdempotencyKey = key,
        Environment = FiscalEnvironment.Homologation
    };

    private FiscalConfiguration CreateHomologConfig(bool liveHttp)
    {
        var service = new FiscalConfigurationService(_tempRoot);
        var config = service.LoadOrCreate();
        config.LiveHttpEnabled = liveHttp;
        config.Issuer = CreateIssuer();
        service.Save(config);
        return service.LoadOrCreate();
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

    private static FiscalNFeDocument CreateValidDocument()
    {
        var issuer = CreateIssuer();
        return new FiscalNFeDocument
        {
            FiscalOperationId = Guid.NewGuid(),
            IdempotencyKey = "doc-1",
            Environment = FiscalEnvironment.Homologation,
            VendaId = Guid.NewGuid(),
            Emitente = issuer,
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
            Itens = new[]
            {
                new FiscalNFeItem
                {
                    NumeroItem = 1,
                    Codigo = "P1",
                    Descricao = "Peca",
                    Ncm = "85071000",
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

    private static FiscalNFeDocument CloneWithItemNcm(FiscalNFeDocument source, string ncm)
    {
        var item = source.Itens[0];
        var newItem = new FiscalNFeItem
        {
            NumeroItem = item.NumeroItem,
            Codigo = item.Codigo,
            Descricao = item.Descricao,
            Ncm = ncm,
            Cfop = item.Cfop,
            Unidade = item.Unidade,
            Quantidade = item.Quantidade,
            ValorUnitario = item.ValorUnitario,
            ValorTotal = item.ValorTotal,
            IcmsOrigem = item.IcmsOrigem,
            IcmsSituacaoTributaria = item.IcmsSituacaoTributaria
        };
        return new FiscalNFeDocument
        {
            FiscalOperationId = source.FiscalOperationId,
            IdempotencyKey = source.IdempotencyKey,
            Environment = source.Environment,
            VendaId = source.VendaId,
            Emitente = source.Emitente,
            Destinatario = source.Destinatario,
            Itens = new[] { newItem },
            ValorProdutos = source.ValorProdutos,
            ValorDesconto = source.ValorDesconto,
            ValorTotal = source.ValorTotal
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }

    public void Dispose()
    {
        FiscalProductionGuard.SetProductionAllowedForTests(false);
        try { Directory.Delete(_tempRoot, true); } catch { /* ignore */ }
    }
}
