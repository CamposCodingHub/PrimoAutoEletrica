using System;
using System.Globalization;
using System.Resources;
using System.Windows;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço de localização para suporte a múltiplos idiomas
    /// </summary>
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        private ResourceManager? _resourceManager;
        private CultureInfo _currentCulture;

        public static LocalizationService Instance
        {
            get
            {
                _instance ??= new LocalizationService();
                return _instance;
            }
        }

        public LocalizationService()
        {
            _currentCulture = CultureInfo.CurrentCulture;
            LoadResources();
        }

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    LoadResources();
                    CultureChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? CultureChanged;

        private void LoadResources()
        {
            // Carrega recursos de localização usando ResourceManager
            _resourceManager = new ResourceManager("PrimoAutoEletrica.Resources.Strings", typeof(LocalizationService).Assembly);
        }

        /// <summary>
        /// Obtém o texto localizado para uma chave
        /// </summary>
        public string GetString(string key, params object[] args)
        {
            try
            {
                if (_resourceManager != null)
                {
                    var resourceValue = _resourceManager.GetString(key, _currentCulture);
                    if (!string.IsNullOrEmpty(resourceValue))
                    {
                        return args.Length > 0 ? string.Format(resourceValue, args) : resourceValue;
                    }
                }
            }
            catch
            {
                // Fallback para tradução manual se ResourceManager falhar
            }

            // Fallback para tradução manual se ResourceManager não encontrar a chave
            var translations = GetTranslations();
            
            if (translations.TryGetValue(key, out var value))
            {
                return args.Length > 0 ? string.Format(value, args) : value;
            }

            return key; // Retorna a chave se não encontrar tradução
        }

        private System.Collections.Generic.Dictionary<string, string> GetTranslations()
        {
            var translations = new System.Collections.Generic.Dictionary<string, string>();

            switch (_currentCulture.TwoLetterISOLanguageName.ToLower())
            {
                case "pt":
                    // Português (Brasil)
                    translations["Dashboard"] = "Painel de Controle";
                    translations["Clients"] = "Clientes";
                    translations["Inventory"] = "Estoque";
                    translations["Employees"] = "Funcionários";
                    translations["Finance"] = "Financeiro";
                    translations["Reports"] = "Relatórios";
                    translations["Settings"] = "Configurações";
                    translations["Login"] = "Login";
                    translations["Logout"] = "Sair";
                    translations["Save"] = "Salvar";
                    translations["Cancel"] = "Cancelar";
                    translations["Delete"] = "Excluir";
                    translations["Edit"] = "Editar";
                    translations["Add"] = "Adicionar";
                    translations["Search"] = "Buscar";
                    translations["Filter"] = "Filtrar";
                    translations["Export"] = "Exportar";
                    translations["Import"] = "Importar";
                    translations["Print"] = "Imprimir";
                    translations["Close"] = "Fechar";
                    translations["Yes"] = "Sim";
                    translations["No"] = "Não";
                    translations["Ok"] = "OK";
                    translations["Error"] = "Erro";
                    translations["Warning"] = "Aviso";
                    translations["Information"] = "Informação";
                    translations["Success"] = "Sucesso";
                    translations["Loading"] = "Carregando...";
                    translations["PleaseWait"] = "Por favor, aguarde...";
                    translations["NoDataFound"] = "Nenhum dado encontrado";
                    translations["AreYouSure"] = "Tem certeza?";
                    translations["OperationCompleted"] = "Operação concluída com sucesso";
                    translations["OperationFailed"] = "Operação falhou";
                    translations["RequiredField"] = "Campo obrigatório";
                    translations["InvalidValue"] = "Valor inválido";
                    translations["SystemName"] = "PrimoAutoEletrica ERP";
                    translations["Refresh"] = "Atualizar";
                    translations["Close"] = "Fechar";
                    break;

                case "en":
                    // English
                    translations["Dashboard"] = "Dashboard";
                    translations["Clients"] = "Clients";
                    translations["Inventory"] = "Inventory";
                    translations["Employees"] = "Employees";
                    translations["Finance"] = "Finance";
                    translations["Reports"] = "Reports";
                    translations["Settings"] = "Settings";
                    translations["Login"] = "Login";
                    translations["Logout"] = "Logout";
                    translations["Save"] = "Save";
                    translations["Cancel"] = "Cancel";
                    translations["Delete"] = "Delete";
                    translations["Edit"] = "Edit";
                    translations["Add"] = "Add";
                    translations["Search"] = "Search";
                    translations["Filter"] = "Filter";
                    translations["Export"] = "Export";
                    translations["Import"] = "Import";
                    translations["Print"] = "Print";
                    translations["Close"] = "Close";
                    translations["Yes"] = "Yes";
                    translations["No"] = "No";
                    translations["Ok"] = "OK";
                    translations["Error"] = "Error";
                    translations["Warning"] = "Warning";
                    translations["Information"] = "Information";
                    translations["Success"] = "Success";
                    translations["Loading"] = "Loading...";
                    translations["PleaseWait"] = "Please wait...";
                    translations["NoDataFound"] = "No data found";
                    translations["AreYouSure"] = "Are you sure?";
                    translations["OperationCompleted"] = "Operation completed successfully";
                    translations["OperationFailed"] = "Operation failed";
                    translations["RequiredField"] = "Required field";
                    translations["InvalidValue"] = "Invalid value";
                    translations["SystemName"] = "PrimoAutoEletrica ERP";
                    translations["Refresh"] = "Refresh";
                    translations["Close"] = "Close";
                    break;

                case "es":
                    // Español
                    translations["Dashboard"] = "Panel de Control";
                    translations["Clients"] = "Clientes";
                    translations["Inventory"] = "Inventario";
                    translations["Employees"] = "Empleados";
                    translations["Finance"] = "Finanzas";
                    translations["Reports"] = "Reportes";
                    translations["Settings"] = "Configuración";
                    translations["Login"] = "Iniciar Sesión";
                    translations["Logout"] = "Cerrar Sesión";
                    translations["Save"] = "Guardar";
                    translations["Cancel"] = "Cancelar";
                    translations["Delete"] = "Eliminar";
                    translations["Edit"] = "Editar";
                    translations["Add"] = "Agregar";
                    translations["Search"] = "Buscar";
                    translations["Filter"] = "Filtrar";
                    translations["Export"] = "Exportar";
                    translations["Import"] = "Importar";
                    translations["Print"] = "Imprimir";
                    translations["Close"] = "Cerrar";
                    translations["Yes"] = "Sí";
                    translations["No"] = "No";
                    translations["Ok"] = "OK";
                    translations["Error"] = "Error";
                    translations["Warning"] = "Advertencia";
                    translations["Information"] = "Información";
                    translations["Success"] = "Éxito";
                    translations["Loading"] = "Cargando...";
                    translations["PleaseWait"] = "Por favor, espere...";
                    translations["NoDataFound"] = "No se encontraron datos";
                    translations["AreYouSure"] = "¿Está seguro?";
                    translations["OperationCompleted"] = "Operación completada con éxito";
                    translations["OperationFailed"] = "Operación fallida";
                    translations["RequiredField"] = "Campo obligatorio";
                    translations["InvalidValue"] = "Valor inválido";
                    translations["SystemName"] = "PrimoAutoEletrica ERP";
                    translations["Refresh"] = "Actualizar";
                    translations["Close"] = "Cerrar";
                    break;

                default:
                    // Default to Portuguese
                    translations["Dashboard"] = "Painel de Controle";
                    translations["Clients"] = "Clientes";
                    translations["Inventory"] = "Estoque";
                    translations["Employees"] = "Funcionários";
                    translations["Finance"] = "Financeiro";
                    translations["Reports"] = "Relatórios";
                    translations["Settings"] = "Configurações";
                    translations["Login"] = "Login";
                    translations["Logout"] = "Sair";
                    translations["Save"] = "Salvar";
                    translations["Cancel"] = "Cancelar";
                    translations["Delete"] = "Excluir";
                    translations["Edit"] = "Editar";
                    translations["Add"] = "Adicionar";
                    translations["Search"] = "Buscar";
                    translations["Filter"] = "Filtrar";
                    translations["Export"] = "Exportar";
                    translations["Import"] = "Importar";
                    translations["Print"] = "Imprimir";
                    translations["Close"] = "Fechar";
                    translations["Yes"] = "Sim";
                    translations["No"] = "Não";
                    translations["Ok"] = "OK";
                    translations["Error"] = "Erro";
                    translations["Warning"] = "Aviso";
                    translations["Information"] = "Informação";
                    translations["Success"] = "Sucesso";
                    translations["Loading"] = "Carregando...";
                    translations["PleaseWait"] = "Por favor, aguarde...";
                    translations["NoDataFound"] = "Nenhum dado encontrado";
                    translations["AreYouSure"] = "Tem certeza?";
                    translations["OperationCompleted"] = "Operação concluída com sucesso";
                    translations["OperationFailed"] = "Operação falhou";
                    translations["RequiredField"] = "Campo obrigatório";
                    translations["InvalidValue"] = "Valor inválido";
                    translations["SystemName"] = "PrimoAutoEletrica ERP";
                    break;
            }

            return translations;
        }

        /// <summary>
        /// Define o idioma da aplicação
        /// </summary>
        public void SetLanguage(string languageCode)
        {
            var culture = new CultureInfo(languageCode);
            CurrentCulture = culture;
            
            // Atualiza a cultura da thread atual
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        /// <summary>
        /// Obtém os idiomas disponíveis
        /// </summary>
        public System.Collections.Generic.List<CultureInfo> GetAvailableLanguages()
        {
            return new System.Collections.Generic.List<CultureInfo>
            {
                new CultureInfo("pt-BR"), // Português (Brasil)
                new CultureInfo("en-US"), // English (United States)
                new CultureInfo("es-ES")  // Español (España)
            };
        }
    }
}