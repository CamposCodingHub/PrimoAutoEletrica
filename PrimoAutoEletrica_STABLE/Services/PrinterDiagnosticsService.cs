using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;

namespace PrimoAutoEletrica.Services
{
    public sealed class PrinterDiagnosticsService
    {
        private static readonly string[] VirtualPrinterPatterns =
        {
            "pdf",
            "xps",
            "fax",
            "onenote",
            "document writer",
            "portprompt",
            "file:",
            "nul:",
            "snagit",
            "cutepdf",
            "pdfcreator",
            "bullzip"
        };

        public PrinterDiagnosticsSnapshot CaptureSnapshot()
        {
            var snapshot = new PrinterDiagnosticsSnapshot
            {
                CapturedAt = DateTime.Now
            };

            try
            {
                using var printServer = new LocalPrintServer();
                var defaultPrinterName = SafeRead(() => printServer.DefaultPrintQueue?.FullName)
                    ?? SafeRead(() => printServer.DefaultPrintQueue?.Name);

                var queues = printServer
                    .GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections })
                    .OrderBy(queue => queue.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var queue in queues)
                {
                    queue.Refresh();

                    var info = CreateInfo(
                        name: queue.FullName ?? queue.Name,
                        driverName: SafeRead(() => queue.QueueDriver?.Name),
                        portName: SafeRead(() => queue.QueuePort?.Name),
                        isDefault: string.Equals(queue.FullName ?? queue.Name, defaultPrinterName, StringComparison.OrdinalIgnoreCase),
                        isShared: queue.IsShared,
                        isOffline: queue.IsOffline);

                    snapshot.Printers.Add(info);
                }
            }
            catch (Exception ex)
            {
                snapshot.CaptureError = ex.Message;
            }

            return snapshot;
        }

        public PrinterDiagnosticInfo DescribeSelectedPrinter(PrintQueue? queue)
        {
            if (queue == null)
            {
                return CreateInfo("Nao identificado", string.Empty, string.Empty, false, false, false);
            }

            queue.Refresh();
            return CreateInfo(
                name: queue.FullName ?? queue.Name,
                driverName: SafeRead(() => queue.QueueDriver?.Name),
                portName: SafeRead(() => queue.QueuePort?.Name),
                isDefault: false,
                isShared: queue.IsShared,
                isOffline: queue.IsOffline);
        }

        public bool TryResolvePrinterQueue(
            string? printerName,
            out PrintQueue? printQueue,
            out PrinterDiagnosticInfo? printerInfo,
            out string message)
        {
            printQueue = null;
            printerInfo = null;
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(printerName))
            {
                message = "Nenhuma impressora preferencial foi configurada.";
                return false;
            }

            try
            {
                var printServer = new LocalPrintServer();
                var queues = printServer
                    .GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections })
                    .ToList();
                var queue = queues.FirstOrDefault(item =>
                    string.Equals(item.FullName, printerName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Name, printerName, StringComparison.OrdinalIgnoreCase));

                if (queue == null)
                {
                    message = $"Impressora configurada '{printerName}' nao foi encontrada nesta estacao.";
                    return false;
                }

                queue.Refresh();
                printQueue = queue;
                printerInfo = DescribeSelectedPrinter(queue);
                return true;
            }
            catch (Exception ex)
            {
                message = $"Falha ao localizar impressora configurada '{printerName}': {ex.Message}";
                return false;
            }
        }

        private static PrinterDiagnosticInfo CreateInfo(
            string? name,
            string? driverName,
            string? portName,
            bool isDefault,
            bool isShared,
            bool isOffline)
        {
            var safeName = string.IsNullOrWhiteSpace(name) ? "Desconhecida" : name.Trim();
            var safeDriver = driverName?.Trim() ?? string.Empty;
            var safePort = portName?.Trim() ?? string.Empty;
            var isVirtual = IsLikelyVirtual(safeName, safeDriver, safePort);

            return new PrinterDiagnosticInfo
            {
                Name = safeName,
                DriverName = safeDriver,
                PortName = safePort,
                IsDefault = isDefault,
                IsShared = isShared,
                IsOffline = isOffline,
                IsVirtual = isVirtual
            };
        }

        private static bool IsLikelyVirtual(string name, string driverName, string portName)
        {
            var haystack = $"{name} {driverName} {portName}".ToLowerInvariant();
            return VirtualPrinterPatterns.Any(pattern => haystack.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private static string? SafeRead(Func<string?> reader)
        {
            try
            {
                return reader();
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class PrinterDiagnosticsSnapshot
    {
        public DateTime CapturedAt { get; init; }
        public string? CaptureError { get; set; }
        public List<PrinterDiagnosticInfo> Printers { get; } = new();
        public bool HasInstalledPrinters => Printers.Count > 0;
        public bool HasPhysicalPrinter => Printers.Any(printer => !printer.IsVirtual);
        public PrinterDiagnosticInfo? DefaultPrinter => Printers.FirstOrDefault(printer => printer.IsDefault);

        public string BuildSummary()
        {
            if (!HasInstalledPrinters)
            {
                return string.IsNullOrWhiteSpace(CaptureError)
                    ? "Nenhuma impressora instalada foi detectada."
                    : $"Nenhuma impressora instalada foi detectada. Erro: {CaptureError}";
            }

            var printers = string.Join("; ", Printers.Select(printer => printer.BuildSummary()));
            return $"Default={DefaultPrinter?.Name ?? "Nenhuma"} | Fisica={HasPhysicalPrinter} | Instaladas={Printers.Count} | {printers}";
        }
    }

    public sealed class PrinterDiagnosticInfo
    {
        public string Name { get; init; } = string.Empty;
        public string DriverName { get; init; } = string.Empty;
        public string PortName { get; init; } = string.Empty;
        public bool IsDefault { get; init; }
        public bool IsShared { get; init; }
        public bool IsOffline { get; init; }
        public bool IsVirtual { get; init; }

        public string BuildSummary()
        {
            return $"{Name} [Driver={DriverName}; Porta={PortName}; Default={IsDefault}; Virtual={IsVirtual}; Offline={IsOffline}; Shared={IsShared}]";
        }
    }
}
