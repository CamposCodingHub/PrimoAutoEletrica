using System;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>Mensagens amigáveis — sem expor stack técnico ao usuário.</summary>
    public static class FiscalUserMessages
    {
        public static string For(FiscalProviderResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            return result.ErrorKind switch
            {
                FiscalErrorKind.None => result.Message,
                FiscalErrorKind.NetworkError =>
                    $"Nao foi possivel comunicar com o servico fiscal. Verifique sua conexao e tente novamente. Codigo interno: {result.InternalCode ?? "FISCAL-NETWORK-001"}",
                FiscalErrorKind.Timeout =>
                    $"A operacao fiscal excedeu o tempo limite. Consulte o status antes de tentar novamente. Codigo interno: {result.InternalCode ?? "FISCAL-TIMEOUT-001"}",
                FiscalErrorKind.AuthenticationError =>
                    $"Falha de autenticacao com o provedor fiscal. Codigo interno: {result.InternalCode ?? "FISCAL-AUTH-001"}",
                FiscalErrorKind.AuthorizationError =>
                    $"Sem permissao para a operacao fiscal solicitada. Codigo interno: {result.InternalCode ?? "FISCAL-AUTHZ-001"}",
                FiscalErrorKind.ConfigurationError =>
                    $"Configuracao fiscal incompleta. Codigo interno: {result.InternalCode ?? "FISCAL-CONFIG-001"}",
                FiscalErrorKind.ProductionBlocked =>
                    $"Emissao em producao esta bloqueada. Use homologacao. Codigo interno: {result.InternalCode ?? "FISCAL-PROD-BLOCKED"}",
                FiscalErrorKind.NotImplemented =>
                    $"Emissao fiscal ainda nao esta disponivel nesta versao. Codigo interno: {result.InternalCode ?? "FISCAL-NOT-IMPL"}",
                FiscalErrorKind.FiscalRejection =>
                    $"O documento foi rejeitado pelo ambiente fiscal. {result.ProviderMessage ?? result.Message} Codigo: {result.ProviderCode ?? result.InternalCode ?? "FISCAL-REJECT"}",
                FiscalErrorKind.ValidationError =>
                    $"Dados fiscais invalidos. {result.Message} Codigo interno: {result.InternalCode ?? "FISCAL-VAL-001"}",
                FiscalErrorKind.ProviderError =>
                    $"O provedor fiscal retornou um erro tecnico. Codigo interno: {result.InternalCode ?? "FISCAL-PROVIDER-001"}",
                _ =>
                    $"Nao foi possivel concluir a operacao fiscal. Codigo interno: {result.InternalCode ?? "FISCAL-UNKNOWN-001"}"
            };
        }
    }
}
