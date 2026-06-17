using System;
using System.IO;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunRobustezErrosLogsChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Robustez:LogsEstruturadosErroAmigavel", () =>
            {
                var marker = $"robustez-smoke-{DateTime.Now:yyyyMMddHHmmssfff}";
                var exception = new InvalidOperationException($"Falha sintetica {marker}");

                _logger.LogError($"Acao estruturada {marker}", exception, "Robustez");

                var structuredLogPath = Path.Combine(App.RuntimeLogDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
                EnsureGeneratedFile(structuredLogPath, "log estruturado da aplicacao");
                var structuredLog = File.ReadAllText(structuredLogPath);
                foreach (var required in new[]
                {
                    "DataHora=",
                    "Usuario=Smoke Test",
                    "Tela=Robustez",
                    $"Acao=Acao estruturada {marker}",
                    $"ErroTecnico=System.InvalidOperationException: Falha sintetica {marker}",
                    "StackTrace="
                })
                {
                    if (!structuredLog.Contains(required, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Log estruturado nao contem o campo esperado: {required}");
                    }
                }

                var friendly = ErrorHandlingService.HandleException(
                    "Robustez",
                    "OperacaoSintetica",
                    exception,
                    showWindow: false);

                if (string.IsNullOrWhiteSpace(friendly.CorrelationId) ||
                    !friendly.UserMessage.Contains("registrado", StringComparison.OrdinalIgnoreCase) ||
                    !friendly.TechnicalDetails.Contains(marker, StringComparison.Ordinal) ||
                    !friendly.TechnicalDetails.Contains("OperacaoSintetica", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Servico de erro amigavel nao retornou detalhes seguros e copiaveis.");
                }
            });
        }
    }
}
