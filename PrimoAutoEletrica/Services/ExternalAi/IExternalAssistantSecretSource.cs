using System;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>Resolves external AI secrets from non-repo sources only.</summary>
    public interface IExternalAssistantSecretSource
    {
        /// <summary>Returns the secret value or null/empty if unavailable. Never logs the value.</summary>
        string? TryGetApiKey(string environmentVariableName);
    }

    /// <summary>Environment-variable secret source (default). No file reads of committed secrets.</summary>
    public sealed class EnvironmentExternalAssistantSecretSource : IExternalAssistantSecretSource
    {
        public string? TryGetApiKey(string environmentVariableName)
        {
            if (string.IsNullOrWhiteSpace(environmentVariableName))
                return null;

            var value = Environment.GetEnvironmentVariable(environmentVariableName.Trim());
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }
    }
}
