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

        private void Write(string level, string message, Exception? ex = null, string? area = null)
        {
            try
            {
                var globalPath = GetLogFilePath();
                var areaPath = string.IsNullOrWhiteSpace(area) ? null : GetLogFilePath(area);
                var areaPrefix = string.IsNullOrWhiteSpace(area) ? string.Empty : $" [{NormalizeArea(area)}]";
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level}{areaPrefix}: {message}";
                if (ex != null)
                {
                    line += Environment.NewLine + ex;
                }

                lock (_syncRoot)
                {
                    Directory.CreateDirectory(_logDirectory);
                    File.AppendAllText(globalPath, line + Environment.NewLine, Encoding.UTF8);

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
