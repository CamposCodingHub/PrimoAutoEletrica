using System;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>Tipo de documento fiscal suportado pela fundação.</summary>
    public enum FiscalDocumentType
    {
        NFe = 0,
        NFCe = 1,
        NFSe = 2
    }

    /// <summary>
    /// Estado da operação/documento. HTTP 200 do provedor NÃO implica Authorized.
    /// </summary>
    public enum FiscalDocumentStatus
    {
        Draft = 0,
        Validating = 1,
        Pending = 2,
        Processing = 3,
        Authorized = 4,
        Rejected = 5,
        Cancelled = 6,
        Denied = 7,
        Contingency = 8,
        Failed = 9,
        Unknown = 10,
        NotConfigured = 11,
        NotImplemented = 12,
        ProductionBlocked = 13
    }

    public enum FiscalEnvironment
    {
        Development = 0,
        Homologation = 1,
        Production = 2
    }

    public enum FiscalProviderKind
    {
        None = 0,
        FocusNfe = 1,
        PlugNotas = 2,
        FakeTestOnly = 99
    }

    public enum FiscalErrorKind
    {
        None = 0,
        ValidationError = 1,
        AuthenticationError = 2,
        AuthorizationError = 3,
        NetworkError = 4,
        ProviderError = 5,
        FiscalRejection = 6,
        Timeout = 7,
        ConfigurationError = 8,
        ProductionBlocked = 9,
        NotImplemented = 10,
        UnknownError = 99
    }

    public enum FiscalFakeScenario
    {
        FakeAuthorized = 0,
        FakeRejected = 1,
        FakeTimeout = 2,
        FakeNetworkError = 3,
        FakeDuplicate = 4,
        FakeUnavailable = 5,
        FakeInvalidResponse = 6,
        FakeHttp500 = 7,
        FakeUnauthorized = 8,
        FakeSlowResponse = 9
    }
}
