using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PrimoAutoEletrica.Services
{
    public sealed class ReportWorkspacePreferences
    {
        public bool IsFavorite { get; set; }
        public bool ExecutiveModeEnabled { get; set; }
        public DateTime DataInicio { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime DataFim { get; set; } = DateTime.Now;
        public string FiltroOperador { get; set; } = string.Empty;
        public string FiltroVendedor { get; set; } = string.Empty;
        public string FiltroCliente { get; set; } = string.Empty;
        public string FiltroCategoria { get; set; } = string.Empty;
        public string FiltroMarca { get; set; } = string.Empty;
        public string FiltroFormaPagamento { get; set; } = string.Empty;
        public string FiltroStatus { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class WorkspacePreferenceService
    {
        private static LoggerService Logger => global::PrimoAutoEletrica.App.Logger;
        private readonly string _preferenceFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public WorkspacePreferenceService(string? preferenceFilePath = null)
        {
            var preferenceDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "Preferences");

            Directory.CreateDirectory(preferenceDirectory);

            _preferenceFilePath = string.IsNullOrWhiteSpace(preferenceFilePath)
                ? Path.Combine(preferenceDirectory, "report-workspace.json")
                : preferenceFilePath;
        }

        public ReportWorkspacePreferences LoadReportPreferences(string userKey)
        {
            try
            {
                var store = ReadStore();
                return store.TryGetValue(NormalizeUserKey(userKey), out var preferences)
                    ? preferences
                    : new ReportWorkspacePreferences();
            }
            catch (Exception ex)
            {
                Logger.LogError("Falha ao carregar preferencias de workspace dos relatorios.", ex);
                return new ReportWorkspacePreferences();
            }
        }

        public void SaveReportPreferences(string userKey, ReportWorkspacePreferences preferences)
        {
            try
            {
                var store = ReadStore();
                preferences.UpdatedAt = DateTime.Now;
                store[NormalizeUserKey(userKey)] = preferences;

                var json = JsonSerializer.Serialize(store, _jsonOptions);
                File.WriteAllText(_preferenceFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError("Falha ao salvar preferencias de workspace dos relatorios.", ex);
                throw;
            }
        }

        private Dictionary<string, ReportWorkspacePreferences> ReadStore()
        {
            if (!File.Exists(_preferenceFilePath))
            {
                return new Dictionary<string, ReportWorkspacePreferences>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(_preferenceFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, ReportWorkspacePreferences>(StringComparer.OrdinalIgnoreCase);
            }

            return JsonSerializer.Deserialize<Dictionary<string, ReportWorkspacePreferences>>(json)
                ?? new Dictionary<string, ReportWorkspacePreferences>(StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeUserKey(string userKey)
        {
            return string.IsNullOrWhiteSpace(userKey) ? "sistema" : userKey.Trim().ToLowerInvariant();
        }
    }
}
