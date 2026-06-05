using System;
using System.IO;
using System.Text.Json;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Tipos de estação disponíveis no sistema.
    /// </summary>
    public enum StationType
    {
        Servidor,
        Caixa,
        Administrativo,
        Vendas,
        Estoque,
        Oficina
    }

    /// <summary>
    /// Configuração de identificação da estação.
    /// </summary>
    public class StationConfiguration
    {
        public string MachineName { get; set; } = Environment.MachineName;
        public string StationName { get; set; } = string.Empty;
        public StationType StationType { get; set; } = StationType.Administrativo;
        public string? Description { get; set; }
        public bool UseConfiguredPdvPrinter { get; set; }
        public string? PreferredPdvPrinterName { get; set; }
        // Local synchronization between stations (UDP announcements)
        public bool UseLocalSync { get; set; } = false;
        public int LocalSyncPort { get; set; } = 52000;
        public DateTime ConfiguredAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Serviço para gerenciar identificação da estação (multiusuário).
    /// </summary>
    public static class StationService
    {
        private const string ConfigurationFileName = "station-config.json";
        private static StationConfiguration? _configuration;

        /// <summary>
        /// Obtém a configuração da estação.
        /// </summary>
        public static StationConfiguration GetConfiguration(string appDataPath)
        {
            if (_configuration != null)
            {
                return _configuration;
            }

            var configPath = GetConfigurationFilePath(appDataPath);

            if (!File.Exists(configPath))
            {
                // Criar configuração padrão
                _configuration = new StationConfiguration
                {
                    MachineName = Environment.MachineName,
                    StationName = Environment.MachineName,
                    StationType = StationType.Administrativo,
                    ConfiguredAt = DateTime.Now
                };

                SaveConfiguration(appDataPath, _configuration);
                return _configuration;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                _configuration = JsonSerializer.Deserialize<StationConfiguration>(json) ?? new StationConfiguration();
                _configuration.MachineName = Environment.MachineName; // Sempre atualizar MachineName atual
                return _configuration;
            }
            catch (Exception)
            {
                // Em caso de erro, retornar configuração padrão
                _configuration = new StationConfiguration
                {
                    MachineName = Environment.MachineName,
                    StationName = Environment.MachineName,
                    StationType = StationType.Administrativo,
                    ConfiguredAt = DateTime.Now
                };

                return _configuration;
            }
        }

        /// <summary>
        /// Salva a configuração da estação.
        /// </summary>
        public static void SaveConfiguration(string appDataPath, StationConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Directory.CreateDirectory(appDataPath);
            var configPath = GetConfigurationFilePath(appDataPath);

            configuration.MachineName = Environment.MachineName;
            configuration.ConfiguredAt = DateTime.Now;

            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(configPath, json);
            _configuration = configuration;
        }

        /// <summary>
        /// Obtém o nome amigável da estação.
        /// </summary>
        public static string GetStationName(string appDataPath)
        {
            var config = GetConfiguration(appDataPath);
            return string.IsNullOrWhiteSpace(config.StationName) ? config.MachineName : config.StationName;
        }

        /// <summary>
        /// Obtém o tipo da estação.
        /// </summary>
        public static StationType GetStationType(string appDataPath)
        {
            var config = GetConfiguration(appDataPath);
            return config.StationType;
        }

        /// <summary>
        /// Obtém o nome da máquina.
        /// </summary>
        public static string GetMachineName()
        {
            return Environment.MachineName;
        }

        /// <summary>
        /// Obtém o nome do usuário do sistema operacional.
        /// </summary>
        public static string GetMachineUserName()
        {
            return Environment.UserName;
        }

        /// <summary>
        /// Obtém uma descrição completa da estação para logs e auditoria.
        /// </summary>
        public static string GetStationDescription(string appDataPath)
        {
            var config = GetConfiguration(appDataPath);
            return $"{config.StationType} - {config.StationName} ({config.MachineName})";
        }

        /// <summary>
        /// Obtém o caminho do arquivo de configuração.
        /// </summary>
        private static string GetConfigurationFilePath(string appDataPath)
        {
            return Path.Combine(appDataPath, ConfigurationFileName);
        }

        /// <summary>
        /// Verifica se a configuração da estação foi feita.
        /// </summary>
        public static bool IsConfigured(string appDataPath)
        {
            var config = GetConfiguration(appDataPath);
            return !string.IsNullOrWhiteSpace(config.StationName) && config.StationType != StationType.Administrativo;
        }
    }
}
