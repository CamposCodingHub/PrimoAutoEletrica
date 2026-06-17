using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Servico simples de logging para registros locais.
    /// Escreve logs em um arquivo por dia na pasta Logs.
    /// </summary>
    public class LoggerService
    {
        private readonly string _logDirectory;
        private readonly object _syncRoot = new();
        private static readonly object ContextSyncRoot = new();
        private static string _currentUserName = "Nao autenticado";
        private static string _currentAccessProfile = "Sem perfil";

        public LoggerService(string? logDirectory = null)
        {
            _logDirectory = string.IsNullOrWhiteSpace(logDirectory)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")
                : logDirectory;

            try
            {
                Directory.CreateDirectory(_logDirectory);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[LoggerService] Falha ao criar diretorio de logs '{_logDirectory}': {ex}");
            }
        }

        private string GetLogFilePath(string? area = null)
        {
            var fileName = $"log-{DateTime.Now:yyyy-MM-dd}.txt";

            if (string.IsNullOrWhiteSpace(area))
            {
                return Path.Combine(_logDirectory, fileName);
            }

            var normalizedArea = NormalizeArea(area);
            return Path.Combine(_logDirectory, "Areas", normalizedArea, fileName);
        }

        private string GetStructuredLogFilePath()
        {
            return Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
        }

        private void Write(string level, string message, Exception? ex = null, string? area = null)
        {
            try
            {
                var globalPath = GetLogFilePath();
                var structuredPath = GetStructuredLogFilePath();
                var areaPath = string.IsNullOrWhiteSpace(area) ? null : GetLogFilePath(area);
                var areaPrefix = string.IsNullOrWhiteSpace(area) ? string.Empty : $" [{NormalizeArea(area)}]";
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level}{areaPrefix}: {message}";
                var structuredLine = BuildStructuredLine(level, message, ex, area);
                if (ex != null)
                {
                    line += Environment.NewLine + ex;
                }

                lock (_syncRoot)
                {
                    Directory.CreateDirectory(_logDirectory);
                    File.AppendAllText(globalPath, line + Environment.NewLine, Encoding.UTF8);
                    File.AppendAllText(structuredPath, structuredLine + Environment.NewLine, Encoding.UTF8);

                    if (!string.IsNullOrWhiteSpace(areaPath))
                    {
                        var areaDirectory = Path.GetDirectoryName(areaPath);
                        if (!string.IsNullOrWhiteSpace(areaDirectory))
                        {
                            Directory.CreateDirectory(areaDirectory);
                        }

                        File.AppendAllText(areaPath, line + Environment.NewLine, Encoding.UTF8);
                    }
                }
            }
            catch (Exception writeException)
            {
                Trace.WriteLine($"[LoggerService] Falha ao gravar log: {writeException}");
                if (ex != null)
                {
                    Trace.WriteLine(ex.ToString());
                }
            }
        }

        private static string BuildStructuredLine(string level, string message, Exception? ex, string? area)
        {
            string user;
            string profile;
            lock (ContextSyncRoot)
            {
                user = _currentUserName;
                profile = _currentAccessProfile;
            }

            var tela = string.IsNullOrWhiteSpace(area) ? "Geral" : NormalizeArea(area);
            var technicalError = ex == null ? string.Empty : ex.GetType().FullName + ": " + ex.Message;
            var stackTrace = ex?.StackTrace ?? string.Empty;

            return string.Join(" | ", new[]
            {
                $"DataHora={DateTime.Now:o}",
                $"Nivel={Escape(level)}",
                $"Usuario={Escape(user)}",
                $"Perfil={Escape(profile)}",
                $"Tela={Escape(tela)}",
                $"Acao={Escape(message)}",
                $"ErroTecnico={Escape(technicalError)}",
                $"StackTrace={Escape(stackTrace)}"
            });
        }

        public static void SetCurrentUserContext(string? userName, string? accessProfile)
        {
            lock (ContextSyncRoot)
            {
                _currentUserName = string.IsNullOrWhiteSpace(userName) ? "Nao autenticado" : userName.Trim();
                _currentAccessProfile = string.IsNullOrWhiteSpace(accessProfile) ? "Sem perfil" : accessProfile.Trim();
            }
        }

        public static void ClearCurrentUserContext()
        {
            SetCurrentUserContext("Nao autenticado", "Sem perfil");
        }

        private static string Escape(string? value)
        {
            return (value ?? string.Empty)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)
                .Replace("|", "/", StringComparison.Ordinal);
        }

        public void LogInfo(string message)
        {
            Write("INFO", message);
        }

        public void LogInfo(string message, string area)
        {
            Write("INFO", message, area: area);
        }

        public void LogWarning(string message)
        {
            Write("WARN", message);
        }

        public void LogWarning(string message, string area)
        {
            Write("WARN", message, area: area);
        }

        public void LogError(string message, Exception? ex = null)
        {
            Write("ERROR", message, ex);
        }

        public void LogError(string message, Exception? ex, string area)
        {
            Write("ERROR", message, ex, area);
        }

        public void LogCritical(string message, Exception? ex = null)
        {
            Write("CRITICAL", message, ex);
        }

        public void LogCritical(string message, Exception? ex, string area)
        {
            Write("CRITICAL", message, ex, area);
        }

        public T Measure<T>(string area, string operation, Func<T> action, int warningThresholdMs = 800)
        {
            ArgumentNullException.ThrowIfNull(action);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = action();
                stopwatch.Stop();
                LogMeasuredDuration(area, operation, stopwatch.ElapsedMilliseconds, warningThresholdMs);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogError($"{operation} falhou apos {stopwatch.ElapsedMilliseconds} ms.", ex, area);
                throw;
            }
        }

        public void Measure(string area, string operation, Action action, int warningThresholdMs = 800)
        {
            ArgumentNullException.ThrowIfNull(action);
            Measure<object?>(area, operation, () =>
            {
                action();
                return null;
            }, warningThresholdMs);
        }

        private void LogMeasuredDuration(string area, string operation, long durationMs, int warningThresholdMs)
        {
            var message = $"{operation} concluida em {durationMs} ms.";
            if (durationMs >= warningThresholdMs)
            {
                LogWarning(message, area);
                return;
            }

            LogInfo(message, area);
        }

        private static string NormalizeArea(string area)
        {
            var buffer = new StringBuilder(area.Length);

            foreach (var character in area.Trim())
            {
                buffer.Append(char.IsLetterOrDigit(character) ? character : '_');
            }

            return buffer.Length == 0 ? "Geral" : buffer.ToString();
        }
    }
}
